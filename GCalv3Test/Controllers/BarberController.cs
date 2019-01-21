using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace GCalv3Test.Controllers
{
    public class BarberController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Sally()
        {
            return View("Sally");
        }

        public IActionResult Susan()
        {
            return View("Susan");
        }
    }
}