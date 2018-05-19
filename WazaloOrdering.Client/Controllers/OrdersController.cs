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
using ShopifySharp;
using System.Threading.Tasks;

namespace WazaloOrdering.Client.Controllers
{
    public class OrdersController : Controller
    {
        private readonly IConfiguration appConfig;
        public OrdersController(IConfiguration configuration)
        {
            appConfig = configuration;
        }

        // GET: /Orders?dateStart=2018-04-16&dateEnd=2018-05-06[&filter=abcd][&statusId=any]
        [Authorize]
        [HttpGet]
        public IActionResult Index(string dateStart, string dateEnd, string filter = null, string fulfillmentStatusId = null, string financialStatusId = null, string statusId = null)
        {
            var model = new OrdersViewModel();

            Tuple<DateTime, DateTime> fromto = GetDateFromTo(dateStart, dateEnd);
            model.DateStart = fromto.Item1;
            model.DateEnd = Utils.AbsoluteEnd(fromto.Item2);
            model.Filter = filter;

            FillStatusLists(model, fulfillmentStatusId, financialStatusId, statusId);

            // add date filter, created_at_min and created_at_max
            var orderFilter = new OrderFilter()
            {
                CreatedAtMin = model.DateStart,
                CreatedAtMax = model.DateEnd,
                FulfillmentStatus = model.FulfillmentStatusId,
                FinancialStatus = model.FinancialStatusId,
                Status = model.StatusId
            };

            var orders = DataFactory.GetShopifyOrders(appConfig, orderFilter);
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
                FulfillmentStatusId = model.FulfillmentStatusId,
                FinancialStatusId = model.FinancialStatusId,
                StatusId = model.StatusId
            });
        }

        // GET: /Orders/Order/123134
        [Authorize]
        [HttpGet]
        public IActionResult Order(string id)
        {
            long orderId = long.Parse(id);
            var order = DataFactory.GetShopifyOrder(appConfig, orderId);

            ViewData["id"] = id;
            return View(order);
        }

        // GET: /Images/Product/123134
        [Authorize]
        [HttpGet("/Images/Product/{id}", Name = "Images")]
        public async Task<ActionResult> GetImages(long id)
        {
            var productImages = await Shopify.GetShopifyProductImages(appConfig, id);

            return Ok(productImages);
        }

        // GET: /Orders/PurchaseOrder/123134
        [Authorize]
        [HttpGet]
        public IActionResult PurchaseOrder(string id)
        {
            long orderId = long.Parse(id);
            var order = DataFactory.GetShopifyOrder(appConfig, orderId);
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
            long orderId = long.Parse(id);
            var order = DataFactory.GetShopifyOrder(appConfig, orderId);
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

            long orderId = long.Parse(id);
            var order = DataFactory.GetShopifyOrder(appConfig, orderId);
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

        private static string GetFileDownloadName(Order order)
        {
            // remove all non digit characters from the order id (#2020 => 2020)
            var orderId = Regex.Replace(order.Name, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
            string fileDownloadName = string.Format("wazalo_purchaseorder_{0}.csv", orderId);
            return fileDownloadName;
        }

        private void FillStatusLists(OrdersViewModel model, string fulfillmentStatusId, string financialStatusId, string statusId)
        {

            model.FulfillmentStatusList = GetOrderFulfillmentStatusList();
            model.FinancialStatusList = GetOrderFinancialStatusList();
            model.StatusList = GetOrderStatusList();

            if (fulfillmentStatusId == null)
            {
                model.FulfillmentStatusId = "any";
            }
            else
            {
                model.FulfillmentStatusId = fulfillmentStatusId;
            }

            if (financialStatusId == null)
            {
                model.FinancialStatusId = "any";
            }
            else
            {
                model.FinancialStatusId = financialStatusId;
            }

            if (statusId == null)
            {
                model.StatusId = "open";
            }
            else
            {
                model.StatusId = statusId;
            }
        }

        private List<PurchaseOrder> GetPurchaseOrderFromShopifyOrder(Order order)
        {
            // generate the list of purchase order elements
            var purchaseOrders = new List<PurchaseOrder>();
            foreach (LineItem lineItem in order.LineItems)
            {
                var purchaseOrder = new PurchaseOrder();
                purchaseOrder.OrderID = order.Name;
                purchaseOrder.SKU = string.Format("{0} ({1})", lineItem.SKU, lineItem.VariantTitle);
                purchaseOrder.Quantity = (lineItem.Quantity.HasValue ? lineItem.Quantity.Value : 0);

                // get agreed usd price
                var priceUSD = GetLineItemAgreedPriceUSD(lineItem);
                purchaseOrder.PriceUSD = string.Format(new CultureInfo("en-US", false), "{0:C}", priceUSD);

                purchaseOrder.Name = Utils.GetNormalizedEquivalentPhrase(order.Customer.FirstName + " " + order.Customer.LastName);
                purchaseOrder.Address1 = Utils.GetNormalizedEquivalentPhrase(order.Customer.DefaultAddress.Address1);
                purchaseOrder.City = Utils.GetNormalizedEquivalentPhrase(order.Customer.DefaultAddress.City);
                purchaseOrder.Country = Utils.GetNormalizedEquivalentPhrase(order.Customer.DefaultAddress.Country);
                purchaseOrder.ZipCode = order.Customer.DefaultAddress.Zip;
                purchaseOrder.Telephone = appConfig["PurchaseOrderTelephone"];
                purchaseOrder.Remarks = Utils.GetNormalizedEquivalentPhrase(order.Note);

                purchaseOrders.Add(purchaseOrder);
            }

            return purchaseOrders;
        }

        private static decimal GetLineItemAgreedPriceUSD(LineItem lineItem)
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
                new SelectListItem() { Text = "Any", Value = "any" },
                new SelectListItem() { Text = "Shipped", Value = "shipped" },
                new SelectListItem() { Text = "Partially Shipped", Value = "partial" },
                new SelectListItem() { Text = "Unshipped", Value = "unshipped" },
            };

            return statusList;
        }
        private List<SelectListItem> GetOrderFinancialStatusList()
        {
            var statusList = new List<SelectListItem>() {
                new SelectListItem() { Text = "Any", Value = "any" },
                new SelectListItem() { Text = "Partially Paid", Value = "partially_paid" },
                new SelectListItem() { Text = "Paid", Value = "paid" },
                new SelectListItem() { Text = "Partially Refunded", Value = "partially_refunded" },
                new SelectListItem() { Text = "Refunded", Value = "refunded" },
            };

            return statusList;
        }
        private List<SelectListItem> GetOrderStatusList()
        {
            var statusList = new List<SelectListItem>() {
                new SelectListItem() { Text = "Open", Value = "open" },
                new SelectListItem() { Text = "Closed", Value = "closed" },
                new SelectListItem() { Text = "Cancelled", Value = "cancelled" },
                new SelectListItem() { Text = "Any", Value = "any" },
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