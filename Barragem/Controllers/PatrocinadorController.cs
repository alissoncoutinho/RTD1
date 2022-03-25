using Barragem.Context;
using Barragem.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace Barragem.Controllers
{
    [Authorize(Roles = "admin")]
    public class PatrocinadorController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();
        public ActionResult Index()
        {
            return View(db.Patrocinio.ToList());
        }

        [Authorize(Roles = "admin")]
        public ActionResult Create()
        {
            return View();
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,UrlImagem,UrlPatrocinador")] Patrocinio patrocinio)
        {
            if (ModelState.IsValid)
            {
                db.Patrocinio.Add(patrocinio);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(patrocinio);
        }

        [Authorize(Roles = "admin")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Patrocinio patrocinio = db.Patrocinio.Find(id);

            if (patrocinio == null)
            {
                return HttpNotFound();
            }

            var patrocinioModel = new PatrocinioModel()
            {
                Id = patrocinio.Id,
                UrlImagem = patrocinio.UrlImagem,
                UrlPatrocinador = patrocinio.UrlPatrocinador
            };

            return View(patrocinioModel);
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,UrlImagem,UrlPatrocinador,FileImage")] PatrocinioModel patrocinio)
        {
            //TODO: VALIDAR...

            var path = "/Content/image/patrocinios";
            string fileNameApplication = System.IO.Path.GetFileName(patrocinio.FileImage.FileName);
            string fileExtensionApplication = System.IO.Path.GetExtension(fileNameApplication);
            string newFile = $"logo_patrocinio_{patrocinio.Id}{fileExtensionApplication}";
            string filePath = System.IO.Path.Combine(Server.MapPath(path), newFile);

            if (fileNameApplication != String.Empty)
            {
                patrocinio.FileImage.SaveAs(filePath);
                patrocinio.UrlImagem = $"{path}/{newFile}";
            }

            var entidadePatrocinio = new Patrocinio()
            {
                Id = patrocinio.Id,
                UrlImagem = patrocinio.UrlImagem,
                UrlPatrocinador = patrocinio.UrlPatrocinador
            };

            if (ModelState.IsValid)
            {
                db.Entry(entidadePatrocinio).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(patrocinio);
        }

        [Authorize(Roles = "admin")]
        public ActionResult Delete(int id)
        {
            Patrocinio patrocinio = db.Patrocinio.Find(id);
            db.Patrocinio.Remove(patrocinio);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}