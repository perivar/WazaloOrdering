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
        [HttpGet("/Images/Product/{id}")]
        public async Task<ActionResult> GetImages(long id)
        {
            var productImages = await Shopify.GetShopifyProductImages(appConfig, id);
            // only return the first image's source attribute
            // https://github.com/nozzlegear/ShopifySharp/blob/master/ShopifySharp/Entities/ProductImage.cs
            return Content(productImages.FirstOrDefault().Src);
        }

        // GET: /Orders/PurchaseOrder/123134
        [Authorize]
        [HttpGet]
        public IActionResult PurchaseOrder(string id)
        {
            long orderId = long.Parse(id);
            var order = DataFactory.GetShopifyOrder(appConfig, orderId);
            var purchaseOrders = GetPurchaseOrderLineItemsFromShopifyOrder(order);

            var purchaseOrderEmailTo = appConfig["PurchaseOrderEmailTo"];
            var purchaseOrderEmailCC = appConfig["PurchaseOrderEmailCC"];

            // read multiline string from jsong config file
            var purchaseOrderEmailNote = appConfig.GetSection("PurchaseOrderEmailNote").Get<string[]>();
            var emailNote = string.Join("\n", purchaseOrderEmailNote);

            var model = new PurchaseOrderViewModel()
            {
                OrderId = orderId,
                EmailTo = purchaseOrderEmailTo,
                EmailCC = purchaseOrderEmailCC,
                EmailBody = emailNote,
                PurchaseOrderLineItems = purchaseOrders
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PurchaseOrder(PurchaseOrderViewModel model)
        {
            return Content($"{model.EmailTo}\n{model.EmailCC}\n{model.EmailBody}");
        }

        // A Json Method to support gijgo grid
        // It requires int? page, int? limit, string sortBy, string direction, long id
        // pluss variables used for filtering on fields
        [Authorize]
        [HttpGet]
        public JsonResult Get(int? page, int? limit, string sortBy, string direction, long id, string orderId, string sku, string name)
        {
            List<PurchaseOrderLineItem> records;
            int total;

            // Get the list from the session; if it doesn't exist, create a new one:           
            //List<PurchaseOrder> purchaseOrders = HttpContext.Session.GetObjectFromJson<List<PurchaseOrder>>("PurchaseOrders") ?? new List<PurchaseOrder>();

            var order = DataFactory.GetShopifyOrder(appConfig, id);
            var purchaseOrderLineItems = GetPurchaseOrderLineItemsFromShopifyOrder(order);

            // project a sequence into a new sequence which may or may not be different types and/or values.
            // select the fields required into a new list
            var query = purchaseOrderLineItems.Select(p => new PurchaseOrderLineItem
            {
                ID = p.ID,
                OrderID = p.OrderID,
                SKU = p.SKU,
                Quantity = p.Quantity,
                PriceUSD = p.PriceUSD,
                Price = p.Price,
                Name = p.Name,
                Address1 = p.Address1,
                // Address2 = p.Address2, // Address2 is not used
                City = p.City,
                Province = p.Province,
                Country = p.Country,
                ZipCode = p.ZipCode,
                Telephone = p.Telephone,
                Remarks = p.Remarks,
                BuyerName = p.BuyerName,
                BuyerEmail = p.BuyerEmail
            });

            // Filter using passed variables (search)
            if (!string.IsNullOrWhiteSpace(orderId))
            {
                query = query.Where(q => q.OrderID.Contains(orderId));
            }
            if (!string.IsNullOrWhiteSpace(sku))
            {
                query = query.Where(q => q.SKU.Contains(sku));
            }
            if (!string.IsNullOrWhiteSpace(name))
            {
                query = query.Where(q => q.Name.Contains(name));
            }

            // Sort using sortBy and direction 
            if (!string.IsNullOrEmpty(sortBy) && !string.IsNullOrEmpty(direction))
            {
                if (direction.Trim().ToLower() == "asc")
                {
                    switch (sortBy.Trim().ToLower())
                    {
                        case "orderid":
                            query = query.OrderBy(q => q.OrderID);
                            break;
                        case "sku":
                            query = query.OrderBy(q => q.SKU);
                            break;
                        case "name":
                            query = query.OrderBy(q => q.Name);
                            break;
                    }
                }
                else
                {
                    switch (sortBy.Trim().ToLower())
                    {
                        case "orderid":
                            query = query.OrderByDescending(q => q.OrderID);
                            break;
                        case "sku":
                            query = query.OrderByDescending(q => q.SKU);
                            break;
                        case "name":
                            query = query.OrderByDescending(q => q.Name);
                            break;
                    }
                }
            }
            else
            {
                query = query.OrderBy(q => q.OrderID);
            }

            // use paging if relevant
            total = query.Count();
            if (page.HasValue && limit.HasValue)
            {
                int start = (page.Value - 1) * limit.Value;
                records = query.Skip(start).Take(limit.Value).ToList();
            }
            else
            {
                records = query.ToList();
            }

            return this.Json(new { records, total });
        }

        [Authorize]
        [HttpPost]
        public JsonResult Save(PurchaseOrderLineItem record, long id)
        {

            var order = DataFactory.GetShopifyOrder(appConfig, id);
            var purchaseOrderLineItems = GetPurchaseOrderLineItemsFromShopifyOrder(order);

            PurchaseOrderLineItem entity;
            if (record.ID > 0)
            {
                entity = purchaseOrderLineItems.First(p => p.ID == record.ID);
                entity.OrderID = record.OrderID;
                entity.SKU = record.SKU;
                entity.Quantity = record.Quantity;
                entity.PriceUSD = record.PriceUSD;
                entity.Name = record.Name;
                entity.Address1 = record.Address1;
                entity.City = record.City;
                entity.Country = record.Country;
                entity.ZipCode = record.ZipCode;
                entity.Telephone = record.Telephone;
                entity.Remarks = record.Remarks;
            }
            else
            {
                // Add a new PurchaseOrderLineItem
                purchaseOrderLineItems.Add(new PurchaseOrderLineItem
                {
                    OrderID = record.OrderID,
                    SKU = record.SKU,
                    Quantity = record.Quantity,
                    PriceUSD = record.PriceUSD,
                    Name = record.Name,
                    Address1 = record.Address1,
                    City = record.City,
                    Country = record.Country,
                    ZipCode = record.ZipCode,
                    Telephone = record.Telephone,
                    Remarks = record.Remarks
                });
            }
            //SaveChanges();

            return Json(new { result = true });
        }

        [Authorize]
        [HttpPost]
        public JsonResult Delete(long id)
        {
            var order = DataFactory.GetShopifyOrder(appConfig, id);
            var purchaseOrderLineItems = GetPurchaseOrderLineItemsFromShopifyOrder(order);

            PurchaseOrderLineItem entity = purchaseOrderLineItems.First(p => p.ID == id);
            purchaseOrderLineItems.Remove(entity);
            //SaveChanges();

            return Json(new { result = true });
        }

        [Authorize]
        [HttpGet]
        public FileStreamResult ExportPurchaseOrder(string id)
        {
            long orderId = long.Parse(id);
            var order = DataFactory.GetShopifyOrder(appConfig, orderId);
            var purchaseOrderLineItems = GetPurchaseOrderLineItemsFromShopifyOrder(order);

            // Deserialize model here 
            var result = Utils.WriteCsvToMemory(purchaseOrderLineItems, typeof(PurchaseOrderLineItemMap));
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
            var purchaseOrderLineItems = GetPurchaseOrderLineItemsFromShopifyOrder(order);

            // Deserialize model here 
            var result = Utils.WriteCsvToMemory(purchaseOrderLineItems, typeof(PurchaseOrderLineItemMap));
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
            return View(purchaseOrderLineItems);
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

        private List<PurchaseOrderLineItem> GetPurchaseOrderLineItemsFromShopifyOrder(Order order)
        {
            // generate the list of purchase order elements
            var purchaseOrderLineItems = new List<PurchaseOrderLineItem>();
            foreach (LineItem lineItem in order.LineItems)
            {
                var purchaseOrderLineItem = new PurchaseOrderLineItem();
                purchaseOrderLineItem.ID = (lineItem.Id.HasValue ? lineItem.Id.Value : 0);
                purchaseOrderLineItem.OrderID = order.Name;
                purchaseOrderLineItem.SKU = string.Format("{0} ({1})", lineItem.SKU, lineItem.VariantTitle);
                purchaseOrderLineItem.Quantity = (lineItem.Quantity.HasValue ? lineItem.Quantity.Value : 0);

                // get agreed usd price
                var priceUSD = GetLineItemAgreedPriceUSD(lineItem);
                purchaseOrderLineItem.PriceUSD = string.Format(new CultureInfo("en-US", false), "{0:C}", priceUSD);

                purchaseOrderLineItem.Name = Utils.GetNormalizedEquivalentPhrase(order.Customer.FirstName + " " + order.Customer.LastName);
                purchaseOrderLineItem.Address1 = Utils.GetNormalizedEquivalentPhrase(order.Customer.DefaultAddress.Address1);
                purchaseOrderLineItem.City = Utils.GetNormalizedEquivalentPhrase(order.Customer.DefaultAddress.City);
                purchaseOrderLineItem.Country = Utils.GetNormalizedEquivalentPhrase(order.Customer.DefaultAddress.Country);
                purchaseOrderLineItem.ZipCode = order.Customer.DefaultAddress.Zip;
                purchaseOrderLineItem.Telephone = appConfig["PurchaseOrderTelephone"];
                purchaseOrderLineItem.Remarks = Utils.GetNormalizedEquivalentPhrase(order.Note);

                purchaseOrderLineItems.Add(purchaseOrderLineItem);
            }

            return purchaseOrderLineItems;
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