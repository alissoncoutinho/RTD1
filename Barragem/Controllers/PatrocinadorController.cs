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
        const string DIR_IMAGES_PATROCINADOR = "/Content/image/patrocinios";

        private BarragemDbContext db = new BarragemDbContext();
        public ActionResult Index()
        {
            return View(db.Patrocinio.Select(s => new PatrocinioModel { Id = s.Id, UrlImagem = s.UrlImagem, UrlPatrocinador = s.UrlPatrocinador }).ToList());
        }

        public ActionResult Create()
        {
            return View(new PatrocinioModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(PatrocinioModel patrocinio)
        {
            if (!ValidarDados(patrocinio))
                return View(patrocinio);

            SalvarImagemPatrocinador(patrocinio);
            var entidadePatrocinio = MapearEntidade(patrocinio);

            TryValidateModel(entidadePatrocinio);

            if (ModelState.IsValid)
            {
                db.Patrocinio.Add(entidadePatrocinio);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(patrocinio);
        }

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
                UrlImagemAnterior = patrocinio.UrlImagem,
                UrlPatrocinador = patrocinio.UrlPatrocinador
            };

            return View(patrocinioModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(PatrocinioModel patrocinio)
        {
            if (!ValidarDados(patrocinio))
                return View(patrocinio);

            SalvarImagemPatrocinador(patrocinio);
            var entidadePatrocinio = MapearEntidade(patrocinio);

            TryValidateModel(entidadePatrocinio);

            if (ModelState.IsValid)
            {
                db.Entry(entidadePatrocinio).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                var message = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                ViewBag.MsgErro = message;
            }
            return View(patrocinio);
        }

        public ActionResult Delete(int id)
        {
            Patrocinio patrocinio = db.Patrocinio.Find(id);
            db.Patrocinio.Remove(patrocinio);
            db.SaveChanges();
            return RedirectToAction("Index");
        }


        private bool ValidarDados(PatrocinioModel patrocinio)
        {
            if (patrocinio == null)
            {
                ViewBag.MsgErro = "Dados de patrocinador inválidos";
                return false;
            }

            if (patrocinio.FileImage != null && (patrocinio.FileImage.ContentLength / 1024) > 2048)
            {
                ViewBag.MsgErro = "O tamanho máximo permitido para imagens é de 2Mb";
                return false;
            }
            return true;
        }

        private void SalvarImagemPatrocinador(PatrocinioModel patrocinio)
        {
            if (patrocinio.FileImage != null)
            {
                var fileName = GetFileImageName(patrocinio.FileImage);
                var filePath = GetFileImagePath(fileName);
                patrocinio.FileImage.SaveAs(filePath);
                patrocinio.UrlImagem = $"{DIR_IMAGES_PATROCINADOR}/{fileName}";
            }
            else
            {
                patrocinio.UrlImagem = patrocinio.UrlImagemAnterior;
            }
        }

        private Patrocinio MapearEntidade(PatrocinioModel patrocinio)
        {
            return new Patrocinio()
            {
                Id = patrocinio.Id,
                UrlImagem = patrocinio.UrlImagem,
                UrlPatrocinador = patrocinio.UrlPatrocinador
            };
        }

        private string GetFileImageName(HttpPostedFileBase file)
        {
            string fileNameApplication = System.IO.Path.GetFileName(file.FileName);
            string fileExtensionApplication = System.IO.Path.GetExtension(fileNameApplication);
            return $"logo_patrocinio_{Guid.NewGuid().ToString("N")}{fileExtensionApplication}";
        }

        private string GetFileImagePath(string fileName)
        {
            return System.IO.Path.Combine(Server.MapPath(DIR_IMAGES_PATROCINADOR), fileName);
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