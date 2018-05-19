using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using WazaloOrdering.DataStore;
using WazaloOrdering.Client.Models;
using ShopifySharp.Filters;

namespace WazaloOrdering.Client.Controllers
{
    public class OrdersController : Controller
    {
        private readonly IConfiguration appConfig;
        public OrdersController(IConfiguration configuration)
        {
            appConfig = configuration;
        }

        // GET: /Orders?dateStart=2018-04-16&dateEnd=2018-05-06[&filter=abcd][&statusIds=2,3]
        [Authorize]
        [HttpGet]
        public IActionResult Index(string dateStart, string dateEnd, string filter = null, int[] fulfillmentStatusIds = null, int[] financialStatusIds = null, int[] statusIds = null)
        {
            var model = new OrdersViewModel();

            Tuple<DateTime, DateTime> fromto = GetDateFromTo(dateStart, dateEnd);
            model.DateStart = fromto.Item1;
            model.DateEnd = Utils.AbsoluteEnd(fromto.Item2);
            model.Filter = filter;
       
            FillStatusLists(model, fulfillmentStatusIds, financialStatusIds, statusIds);

            // add date filter, created_at_min and created_at_max
            string querySuffix = string.Format(CultureInfo.InvariantCulture, "created_at_min={0:yyyy-MM-ddTHH:mm:sszzz}&created_at_max={1:yyyy-MM-ddTHH:mm:sszzz}", model.DateStart, model.DateEnd);

            // fulfillment
            string fulfillmentQuery = "";
            if (model.FulfillmentStatusIds.Contains(1))
            {
                fulfillmentQuery = "&fulfillment_status=any";
            }
            if (model.FulfillmentStatusIds.Contains(2))
            {
                fulfillmentQuery = "&fulfillment_status=shipped";
            }
            if (model.FulfillmentStatusIds.Contains(3))
            {
                fulfillmentQuery = "&fulfillment_status=partial";
            }
            if (model.FulfillmentStatusIds.Contains(4))
            {
                fulfillmentQuery = "&fulfillment_status=unshipped";
            }
            querySuffix += fulfillmentQuery;

            // financial
            string financialQuery = "";
            if (model.FinancialStatusIds.Contains(1))
            {
                financialQuery = "&financial_status=any";
            }
            if (model.FinancialStatusIds.Contains(2))
            {
                financialQuery = "&financial_status=partially_paid";
            }
            if (model.FinancialStatusIds.Contains(3))
            {
                financialQuery = "&financial_status=paid";
            }
            if (model.FinancialStatusIds.Contains(4))
            {
                financialQuery = "&financial_status=partially_refunded";
            }
            if (model.FinancialStatusIds.Contains(5))
            {
                financialQuery = "&financial_status=refunded";
            }
            querySuffix += financialQuery;

            // status
            string statusQuery = "";
            if (model.StatusIds.Contains(1))
            {
                statusQuery = "&status=open";
            }
            if (model.StatusIds.Contains(2))
            {
                statusQuery = "&status=closed";
            }
            if (model.StatusIds.Contains(3))
            {
                statusQuery = "&status=cancelled";
            }
            if (model.StatusIds.Contains(4))
            {
                statusQuery = "&status=any";
            }
            querySuffix += statusQuery;

/*
            var orderFilter = new OrderFilter()
            {
                CreatedAtMin = model.DateStart,
                CreatedAtMax = model.DateEnd,
                FulfillmentStatus = "any",
                FinancialStatus = "any",
                Status = "any"
            };
            var testorders = DataFactory.GetShopifyOrders(appConfig, orderFilter);
*/
            var orders = DataFactory.GetShopifyOrders(appConfig, querySuffix);
            model.ShopifyOrders = orders;

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(OrdersViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Something wasn't valid on the model
                var message = string.Join(" | ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                Console.WriteLine("ERROR: " + message);
                return View(model);
            }

            // The model passed validation, do something with it

            //var json = JsonConvert.SerializeObject(model);
            //return Content(json);

            return RedirectToAction("Index", new
            {
                DateStart = model.DateStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                DateEnd = model.DateEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Filter = model.Filter,
                FulfillmentStatusIds = model.FulfillmentStatusIds,
                FinancialStatusIds = model.FinancialStatusIds,
                StatusIds = model.StatusIds
            });
        }

        // GET: /Orders/Order/123134
        [Authorize]
        [HttpGet]
        public IActionResult Order(string id)
        {
            // add field filter
            string querySuffix = "";

            var order = DataFactory.GetShopifyOrder(appConfig, id, querySuffix);

            ViewData["id"] = id;
            return View(order);
        }

        // GET: /Orders/PurchaseOrder/123134
        [Authorize]
        [HttpGet]
        public IActionResult PurchaseOrder(string id)
        {
            // add field filter
            string querySuffix = "";
            var order = DataFactory.GetShopifyOrder(appConfig, id, querySuffix);
            var purchaseOrders = GetPurchaseOrderFromShopifyOrder(order);

            // read multiline string from jsong config file
            var purchaseOrderEmailNote = appConfig.GetSection("PurchaseOrderEmailNote").Get<string[]>();
            var emailNote = string.Join("\n", purchaseOrderEmailNote);

            ViewData["id"] = id;
            ViewData["purchaseOrderEmailNote"] = emailNote;
            return View(purchaseOrders);
        }

        [Authorize]
        [HttpGet]
        public FileStreamResult ExportPurchaseOrder(string id)
        {
            // add field filter
            string querySuffix = "";
            var order = DataFactory.GetShopifyOrder(appConfig, id, querySuffix);
            var purchaseOrders = GetPurchaseOrderFromShopifyOrder(order);

            // Deserialize model here 
            var result = Utils.WriteCsvToMemory(purchaseOrders, typeof(PurchaseOrderMap));
            var memoryStream = new MemoryStream(result);

            string fileDownloadName = GetFileDownloadName(order);
            return new FileStreamResult(memoryStream, "text/csv") { FileDownloadName = fileDownloadName };
        }

        [Authorize]
        [HttpGet]
        public IActionResult MailPurchaseOrder(string id)
        {
            var purchaseOrderEmailTo = appConfig["PurchaseOrderEmailTo"];
            var purchaseOrderEmailCC = appConfig["PurchaseOrderEmailCC"];

            // read multiline string from jsong config file
            var purchaseOrderEmailNote = appConfig.GetSection("PurchaseOrderEmailNote").Get<string[]>();
            var emailNote = string.Join("\n", purchaseOrderEmailNote);

            if (purchaseOrderEmailTo == null || purchaseOrderEmailNote == null)
                return Content("Missing to address or email note!");

            // add field filter
            string querySuffix = "";
            var order = DataFactory.GetShopifyOrder(appConfig, id, querySuffix);
            var purchaseOrders = GetPurchaseOrderFromShopifyOrder(order);

            // Deserialize model here 
            var result = Utils.WriteCsvToMemory(purchaseOrders, typeof(PurchaseOrderMap));
            var memoryStream = new MemoryStream(result);
            byte[] bytes = memoryStream.ToArray();
            memoryStream.Close();

            try
            {
                string to = purchaseOrderEmailTo;
                string cc = purchaseOrderEmailCC;
                string subject = string.Format("Purchase Order {0}", order.Name);
                string body = emailNote;
                string fileDownloadName = GetFileDownloadName(order);

                Utils.SendMailWithAttachment(appConfig, subject, body, to, cc, fileDownloadName, bytes);
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

        private static string GetFileDownloadName(ShopifyOrder order)
        {
            // remove all non digit characters from the order id (#2020 => 2020)
            var orderId = Regex.Replace(order.Name, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
            string fileDownloadName = string.Format("wazalo_purchaseorder_{0}.csv", orderId);
            return fileDownloadName;
        }

        private void FillStatusLists(OrdersViewModel model, int[] fulfillmentStatusIds, int[] financialStatusIds, int[] statusIds) {

            model.FulfillmentStatusList = GetOrderFulfillmentStatusList();
            model.FinancialStatusList = GetOrderFinancialStatusList();
            model.StatusList = GetOrderStatusList();

            if (fulfillmentStatusIds == null || fulfillmentStatusIds.Count() == 0)
            {
                model.FulfillmentStatusIds = new int[] { 1 }; // any
            }
            else
            {
                model.FulfillmentStatusIds = fulfillmentStatusIds;
            }

            if (financialStatusIds == null || financialStatusIds.Count() == 0)
            {
                model.FinancialStatusIds = new int[] { 1 }; // any   
            }
            else
            {
                model.FinancialStatusIds = financialStatusIds;
            }

            if (statusIds == null || statusIds.Count() == 0)
            {
                model.StatusIds = new int[] { 1 }; // open  
            }
            else
            {
                model.StatusIds = statusIds;
            }
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
                purchaseOrder.Telephone = appConfig["PurchaseOrderTelephone"];
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

        private List<SelectListItem> GetOrderFulfillmentStatusList()
        {
            var statusList = new List<SelectListItem>() {
                new SelectListItem() { Text = "Any", Value = "1" },
                new SelectListItem() { Text = "Shipped", Value = "2" },
                new SelectListItem() { Text = "Partially Shipped", Value = "3" },
                new SelectListItem() { Text = "Unshipped", Value = "4" },
            };

            return statusList;
        }
        private List<SelectListItem> GetOrderFinancialStatusList()
        {
            var statusList = new List<SelectListItem>() {
                new SelectListItem() { Text = "Any", Value = "1" },
                new SelectListItem() { Text = "Partially Paid", Value = "2" },
                new SelectListItem() { Text = "Paid", Value = "3" },
                new SelectListItem() { Text = "Partially Refunded", Value = "4" },
                new SelectListItem() { Text = "Refunded", Value = "5" },
            };

            return statusList;
        }
        private List<SelectListItem> GetOrderStatusList()
        {
            var statusList = new List<SelectListItem>() {
                new SelectListItem() { Text = "Open", Value = "1" },
                new SelectListItem() { Text = "Closed", Value = "2" },
                new SelectListItem() { Text = "Cancelled", Value = "3" },
                new SelectListItem() { Text = "Any", Value = "4" },
            };

            return statusList;
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