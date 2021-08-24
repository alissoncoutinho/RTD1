using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Barragem.Models;
using Barragem.Context;
using Barragem.Filters;
using System.Transactions;
using Barragem.Class;
using WebMatrix.WebData;
using System.Web.Security;

namespace Barragem.Controllers
{
    [Authorize]
    [InitializeSimpleMembership]
    public class RegraController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();

        //
        // GET: /Rodada/

        [Authorize(Roles = "admin")]
        public ActionResult Index(string msg = "", string detalheErro = "")
        {
            if (msg.Equals("ok"))
            {
                ViewBag.Ok = msg;
            }
            else if (!msg.Equals(""))
            {
                ViewBag.MsgErro = msg;
                ViewBag.DetalheErro = detalheErro;
            }
            List<Regra> regra = db.Regra.ToList();
            
            return View(regra);
        }

        [Authorize(Roles = "admin")]
        public ActionResult Create()
        {
            return View();
        }


        [Authorize(Roles = "admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Regra Regra)
        {
            if (ModelState.IsValid) { 
                db.Regra.Add(Regra);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(Regra);
        }

        // GET: /Rodada/Edit/5
        [Authorize(Roles = "admin")]
        public ActionResult Edit(int id = 0)
        {
            Regra Regra = db.Regra.Find(id);
            if (Regra == null)
            {
                return HttpNotFound();
            }
            ViewBag.flag = "edit";
            return View(Regra);
        }

        //
        // POST: /Rodada/Edit/5
        [Authorize(Roles = "admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Regra Regra)
        {
            if (ModelState.IsValid)
            {
                db.Entry(Regra).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(Regra);
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}