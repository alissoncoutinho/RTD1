using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Barragem.Models;
using Barragem.Context;
using System.Transactions;

namespace Barragem.Controllers
{
    public class ConfiguracaoController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();


        [Authorize(Roles = "admin")]
        public ActionResult Edit(int id = 0, string msg="")
        {
            if (msg.Equals("ok")){
                ViewBag.Ok = msg;
            }else if (!msg.Equals("")){
                ViewBag.MsgErro = msg;
            }
            Configuracao configuracao = db.Configuracao.Find(id);
            if (configuracao == null)
            {
                return HttpNotFound();
            }
            return View(configuracao);
        }

        
        //
        // POST: /Configuracao/Edit/5

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public ActionResult Edit(Configuracao configuracao)
        {
            if (ModelState.IsValid)
            {
                db.Entry(configuracao).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(configuracao);
        }


        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}