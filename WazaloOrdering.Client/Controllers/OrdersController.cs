using System;
using Microsoft.AspNetCore.Mvc;
using WazaloOrdering.Client.Models;
using System.Text.Encodings.Web;
using System.Collections.Generic;
using WazaloOrdering.DataStore;

namespace WazaloOrdering.Client.Controllers
{
    public class OrdersController : Controller
    {
        // 
        // GET: /Orders/
        public IActionResult Index()
        {
            var orders = DataFactory.GetShopifyOrders();
            return View(orders);
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