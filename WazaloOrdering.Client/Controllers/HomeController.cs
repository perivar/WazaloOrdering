using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using WazaloOrdering.Client.Models;
using WazaloOrdering.DataStore;

namespace WazaloOrdering.Client.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration appConfig;

        const string SessionKeyName = "_Name";
        const string SessionKeyFY = "_FY";
        const string SessionKeyDate = "_Date";

        public HomeController(IConfiguration configuration)
        {
            appConfig = configuration;
        }

        [Authorize]
        public IActionResult Index()
        {
            HttpContext.Session.SetString(SessionKeyName, "Per Ivar Nerseth");
            HttpContext.Session.SetInt32(SessionKeyFY, 2018);
            HttpContext.Session.Set<DateTime>(SessionKeyDate, DateTime.Now);

            //return View();
            return Redirect("~/Orders");
        }

        [AllowAnonymous]
        public IActionResult About()
        {
            ViewBag.Name = HttpContext.Session.GetString(SessionKeyName);
            ViewBag.FY = HttpContext.Session.GetInt32(SessionKeyFY);
            ViewBag.Date = HttpContext.Session.Get<DateTime>(SessionKeyDate);

            ViewData["Message"] = "Stored Session State";

            return View();
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [AllowAnonymous, HttpPost]
        public IActionResult Login(LoginData loginData)
        {
            // get shopify configuration parameters
            string username = appConfig["OberloUsername"];
            string password = appConfig["OberloPassword"];

            if (ModelState.IsValid)
            {
                var isValid = (loginData.Username == username && loginData.Password == password);
                if (!isValid)
                {
                    ModelState.AddModelError("", "Login failed. Please check Username and/or password");
                    return View();
                }
                else
                {
                    var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
                    identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, loginData.Username));
                    identity.AddClaim(new Claim(ClaimTypes.Name, loginData.Username));
                    var principal = new ClaimsPrincipal(identity);
                    HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties { IsPersistent = loginData.RememberMe });
                    return Redirect("~/Orders");
                }
            }
            else
            {
                ModelState.AddModelError("", "username or password is blank");
                return View();
            }
        }
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
