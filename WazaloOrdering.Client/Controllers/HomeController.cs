using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WazaloOrdering.Client.Models;
using WazaloOrdering.DataStore;

namespace WazaloOrdering.Client.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var date = new Date();
            DateTime from = date.CurrentDate.AddDays(-14);
            DateTime to = date.CurrentDate;
            ViewData["dateStart"] = from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            ViewData["dateEnd"] = to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
