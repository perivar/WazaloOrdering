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
        // 
        // GET: /Orders?dateStart=2018-04-16&dateEnd=2018-05-06
        public IActionResult Index(string dateStart, string dateEnd)
        {
            var date = new Date();

            DateTime from = date.CurrentDate.AddDays(-20);
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

            // add date filter, created_at_min and created_at_max
            string querySuffix = string.Format(CultureInfo.InvariantCulture, "status=any&created_at_min={0:yyyy-MM-ddTHH:mm:sszzz}&created_at_max={1:yyyy-MM-ddTHH:mm:sszzz}", from, to);

            var orders = DataFactory.GetShopifyOrders(querySuffix);
            ViewData["dateStart"] = from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            ViewData["dateEnd"] = to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return View(orders);
        }

        [HttpPost]
        public IActionResult Index(FormCollection frmobj)
        {
            string dateStart = frmobj["dateStart"];
            string dateEnd = frmobj["dateEnd"];
            return Content("Hello");
        }

        // 
        // GET: /Orders/Order/ 
        public IActionResult Order(string orderId, int numTimes = 1)
        {
            ViewData["Message"] = "OrderId " + orderId;
            ViewData["NumTimes"] = numTimes;
            return View();
        }
    }
}