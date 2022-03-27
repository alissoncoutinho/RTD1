using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Barragem.Controllers
{
    public class LandingPageController : Controller
    {
        public ActionResult Index(int id)
        {
            return View();
        }

        public ActionResult LigaRedirect(string key)
        {
            return RedirectToAction("Index", "LandingPage", new { id = 0 });
        }
        
        public ActionResult CircuitoRedirect(string key)
        {
            return RedirectToAction("Index", "LandingPage", new { id = 0 });
        }

        public ActionResult FederacaoRedirect(string key)
        {
            return RedirectToAction("Index", "LandingPage", new { id = 0 });
        }
    }
}