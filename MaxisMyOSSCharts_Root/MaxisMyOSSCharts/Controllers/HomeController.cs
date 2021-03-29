using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Globalization;

namespace MaxisMyOSSCharts.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.ApplicationWeekNO ="";
            ViewBag.DepartmentWeekNO ="";
            return View();
        }
        public ActionResult Application(string WeekNo)
        {
            ViewBag.ApplicationWeekNO = WeekNo; 
            return View();
        }
        public ActionResult Department(string WeekNo)
        {
            ViewBag.ApplicationWeekNO = WeekNo; 
            return View();
        }
        public ActionResult Smiley()
        {
            return View();
        }
        public ActionResult UCR()
        {
            return View();
        }
    }
}