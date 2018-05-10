using System;
using Microsoft.AspNetCore.Mvc;
using WazaloOrdering.Client.Models;
using System.Text.Encodings.Web;
using System.Collections.Generic;
using WazaloOrdering.DataStore;
using System.Globalization;
using Microsoft.AspNetCore.Http;

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

            ViewData["id"] = id;
            return View(order);
        }

        private Tuple<DateTime, DateTime> GetDateFromTo(string dateStart, string dateEnd) {
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