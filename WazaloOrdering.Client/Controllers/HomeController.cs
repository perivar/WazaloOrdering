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
using Microsoft.AspNetCore.Mvc;
using WazaloOrdering.Client.Models;
using WazaloOrdering.DataStore;

namespace WazaloOrdering.Client.Controllers
{
    public class HomeController : Controller
    {
        [Authorize]
        public IActionResult Index()
        {
            var date = new Date();
            DateTime from = date.CurrentDate.AddDays(-14);
            DateTime to = date.CurrentDate;
            ViewData["dateStart"] = from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            ViewData["dateEnd"] = to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return View();
        }

        [AllowAnonymous]
        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

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
            if (ModelState.IsValid)
            {
                var isValid = (loginData.Username == "username" && loginData.Password == "password");
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
                    return Redirect("~/Home/Index");
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
