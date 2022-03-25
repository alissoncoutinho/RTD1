using Barragem.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Barragem.Controllers
{
    [Authorize(Roles = "admin")]
    public class PatrocinadorController : Controller
    {
        public ActionResult Index()
        {
              var db = new BarragemDbContext();

              var patrocinadores = db.Patrocinio.ToList();

              return View(patrocinadores);
            //return View();
        }
    }
}