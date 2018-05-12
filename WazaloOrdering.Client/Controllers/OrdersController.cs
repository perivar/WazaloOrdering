using System;
using Microsoft.AspNetCore.Mvc;
using WazaloOrdering.Client.Models;
using System.Text.Encodings.Web;
using System.Collections.Generic;
using WazaloOrdering.DataStore;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text.RegularExpressions;

namespace WazaloOrdering.Client.Controllers
{
    public class OrdersController : Controller
    {
        // GET: /Orders?dateStart=2018-04-16&dateEnd=2018-05-06
        [HttpGet]
        public IActionResult Index(string dateStart, string dateEnd)
        {
            Tuple<DateTime, DateTime> fromto = GetDateFromTo(dateStart, dateEnd);
            DateTime from = fromto.Item1;
            DateTime to = Utils.AbsoluteEnd(fromto.Item2);

            // add date filter, created_at_min and created_at_max
            string querySuffix = string.Format(CultureInfo.InvariantCulture, "status=any&created_at_min={0:yyyy-MM-ddTHH:mm:sszzz}&created_at_max={1:yyyy-MM-ddTHH:mm:sszzz}", from, to);

            var orders = DataFactory.GetShopifyOrders(querySuffix);
            ViewData["dateStart"] = from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            ViewData["dateEnd"] = to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(IFormCollection formCollection)
        {
            try
            {
                string filter = HttpContext.Request.Form["filter"];
                string dateStart = HttpContext.Request.Form["dateStart"];
                string dateEnd = HttpContext.Request.Form["dateEnd"];
                return Index(dateStart, dateEnd);
            }
            catch
            {
                return Index(null, null);
            }
        }

        // GET: /Orders/Order/123134
        public IActionResult Order(string id)
        {
            // add field filter
            string querySuffix = "";

            var order = DataFactory.GetShopifyOrder(id, querySuffix);

            ViewData["id"] = id;
            return View(order);
        }

        // GET: /Orders/PurchaseOrder/123134
        public IActionResult PurchaseOrder(string id)
        {
            // add field filter
            string querySuffix = "";
            var order = DataFactory.GetShopifyOrder(id, querySuffix);
            var purchaseOrders = GetPurchaseOrderFromShopifyOrder(order);

            ViewData["id"] = id;
            return View(purchaseOrders);
        }

        [HttpGet]
        public FileStreamResult ExportPurchaseOrder(string id)
        {
            // add field filter
            string querySuffix = "";
            var order = DataFactory.GetShopifyOrder(id, querySuffix);
            var purchaseOrders = GetPurchaseOrderFromShopifyOrder(order);

            // Deserialize model here 
            var result = Utils.WriteCsvToMemory(purchaseOrders, typeof(PurchaseOrderMap));
            var memoryStream = new MemoryStream(result);

            // remove all non digit characters from the order id (#2020 => 2020)
            var orderId = Regex.Replace(order.Name, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
            string fileDownloadName = string.Format("wazalo_purchaseorder_{0}.csv", orderId);
            return new FileStreamResult(memoryStream, "text/csv") { FileDownloadName = fileDownloadName };
        }

        [HttpGet]
        public IActionResult MailPurchaseOrder(string id)
        {
            // add field filter
            string querySuffix = "";
            var order = DataFactory.GetShopifyOrder(id, querySuffix);
            var purchaseOrders = GetPurchaseOrderFromShopifyOrder(order);

            // Deserialize model here 
            var result = Utils.WriteCsvToMemory(purchaseOrders, typeof(PurchaseOrderMap));
            var memoryStream = new MemoryStream(result);
            byte[] bytes = memoryStream.ToArray();
            memoryStream.Close();

            try
            {
                string to = "perivar@nerseth.com";
                string cc = null;

                // remove all non digit characters from the order id (#2020 => 2020)
                var orderId = Regex.Replace(order.Name, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
                string fileDownloadName = string.Format("wazalo_purchaseorder_{0}.csv", orderId);

                string subject = string.Format("Purchase Order {0}", order.Name);
                string body = @"
    Hi Aiminyz,<br>
    Attached is a new purchase order from Wazalo.com.<br>
    It has already been paid using PayPal.<br>
    Please confirm this order and send us the tracking number(s) as soon as the order has been processed.<br>
    <br>
    <strong>Note!</strong><br>
    We are dropshipping! Please, don't include any invoices or promo materials into the package.<br>
    Please also ensure the total shipment value never exceed $40 including shipment cost.<br> 
    If so, please separate into more shipments. Also, always use ePacket for shipment.<br>
    If you have any questions, please send an email to shop@wazalo.com<br>
    <br>
    Best Regards,<br>
    Wazalo                          
                    ";

                Utils.SendMailWithAttachment(subject, body, to, cc, fileDownloadName, bytes);
                ViewData["emailSent"] = true;
                ViewData["to"] = to;
            }
            catch (System.Exception e)
            {
                ViewData["emailSent"] = false;
                ViewData["errorMessage"] = e.ToString();
            }

            ViewData["id"] = id;
            return View(purchaseOrders);
        }

        private List<PurchaseOrder> GetPurchaseOrderFromShopifyOrder(ShopifyOrder order)
        {
            // generate the list of purchase order elements
            var purchaseOrders = new List<PurchaseOrder>();
            foreach (ShopifyOrderLineItem lineItem in order.LineItems)
            {
                var purchaseOrder = new PurchaseOrder();
                purchaseOrder.OrderID = order.Name;
                purchaseOrder.SKU = string.Format("{0} ({1})", lineItem.Sku, lineItem.VariantTitle);
                purchaseOrder.Quantity = lineItem.Quantity;

                // get agreed usd price
                var priceUSD = GetLineItemAgreedPriceUSD(lineItem);
                purchaseOrder.PriceUSD = string.Format(new CultureInfo("en-US", false), "{0:C}", priceUSD);

                purchaseOrder.Name = Utils.GetNormalizedEquivalentPhrase(order.CustomerName);
                purchaseOrder.Address1 = Utils.GetNormalizedEquivalentPhrase(order.CustomerAddress);
                purchaseOrder.City = Utils.GetNormalizedEquivalentPhrase(order.CustomerCity);
                purchaseOrder.Country = Utils.GetNormalizedEquivalentPhrase(order.CustomerCountry);
                purchaseOrder.ZipCode = order.CustomerZipCode;
                purchaseOrder.Telephone = "+47 41 31 88 53";
                purchaseOrder.Remarks = Utils.GetNormalizedEquivalentPhrase(order.Note);

                purchaseOrders.Add(purchaseOrder);
            }

            return purchaseOrders;
        }

        private static decimal GetLineItemAgreedPriceUSD(ShopifyOrderLineItem lineItem)
        {
            Regex sizeRegEx = new Regex(@"(\d+)-(\d+)");
            if (lineItem.Price == 449)
            {
                // either child or parent 
                // extract size from text like "115-125 cm" to find what price to use
                string size = "";
                Match match = sizeRegEx.Match(lineItem.VariantTitle);
                if (match.Success)
                {
                    size = string.Format("{0}-{1}", match.Groups[1].Value, match.Groups[2].Value);
                    switch (size)
                    {
                        case "95-105":
                        case "105-115":
                        case "115-125":
                        case "125-135":
                        case "135-145":
                            return 9;
                        default:
                            return 18;
                    }
                }
                else
                {
                    Console.WriteLine("ERROR: Could not get pajama size!");
                }
                return 18;
            }
            else if (lineItem.Price == 199)
            {
                // shoes
                return 9;
            }
            return 0;
        }
        private Tuple<DateTime, DateTime> GetDateFromTo(string dateStart, string dateEnd)
        {
            var date = new Date();

            DateTime from = date.CurrentDate.AddDays(-14);
            DateTime to = date.CurrentDate;
            if (dateStart != null)
            {
                try
                {
                    DateTime.TryParseExact(dateStart,
                       "yyyy-MM-dd",
                       CultureInfo.InvariantCulture,
                       DateTimeStyles.None,
                       out from);
                }
                catch (System.Exception)
                {
                }
            }

            if (dateEnd != null)
            {
                try
                {
                    DateTime.TryParseExact(dateEnd,
                       "yyyy-MM-dd",
                       CultureInfo.InvariantCulture,
                       DateTimeStyles.None,
                       out to);
                }
                catch (System.Exception)
                {
                }
            }
            return new Tuple<DateTime, DateTime>(from, to);
        }
    }
}