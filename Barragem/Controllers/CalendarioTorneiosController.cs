using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Barragem.Context;
using Barragem.Models;

namespace Barragem.Controllers
{
    public class CalendarioTorneiosController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();

        public ActionResult Index(int? filtroAno)
        {
            if (filtroAno == null)
            {
                filtroAno = DateTime.Now.Year;
            }

            ViewBag.FiltroAno = filtroAno;

            var calendarioTorneio =
                db.CalendarioTorneio
                    .Include(c => c.ModalidadeTorneio)
                    .Include(c => c.StatusInscricaoTorneio)
                    .Where(x => x.DataInicial.Year == filtroAno || x.DataFinal.Year == filtroAno);

            var dadosListagem = MapearDadosModelo(calendarioTorneio.ToList());

            return View(dadosListagem);
        }

        private CalendarioTorneioModel MapearDadosModelo(List<CalendarioTorneio> torneios)
        {
            CultureInfo culture = new CultureInfo("pt-BR");
            DateTimeFormatInfo dtfi = culture.DateTimeFormat;

            var lista = new List<CalendarioTorneioModel.CalendarioTorneioItens>();
            foreach (var item in torneios)
            {
                var mes = culture.TextInfo.ToTitleCase(dtfi.GetMonthName(item.DataInicial.Month));
                var torneioModel = new CalendarioTorneioModel.CalendarioTorneioItens()
                {
                    Id = item.Id,
                    Mes = mes,
                    ModalidadeTorneio = item.ModalidadeTorneio.Nome,
                    Nome = item.Nome,
                    Pontuacao = item.Pontuacao,
                    StatusInscricaoTorneio = item.StatusInscricaoTorneio.Nome
                };
                lista.Add(torneioModel);
            }

            var listaFiltroAnos = new List<SelectListItem>()
            {
                new SelectListItem(){Value=DateTime.Now.AddYears(-1).Year.ToString(), Text=DateTime.Now.AddYears(-1).Year.ToString() },
                new SelectListItem(){Value=DateTime.Now.Year.ToString(), Text=DateTime.Now.Year.ToString(), Selected=true },
                new SelectListItem(){Value=DateTime.Now.AddYears(1).Year.ToString(), Text=DateTime.Now.AddYears(1).Year.ToString() },
                new SelectListItem(){Value=DateTime.Now.AddYears(2).Year.ToString(), Text=DateTime.Now.AddYears(2).Year.ToString() }
            };

            return new CalendarioTorneioModel() { Registros = lista, FiltroAno = listaFiltroAnos };
        }

        public ActionResult Create()
        {
            ViewBag.ModalidadeTorneioId = new SelectList(db.ModalidadeTorneio, "Id", "Nome");
            ViewBag.StatusInscricaoTorneioId = new SelectList(db.StatusInscricaoTorneio, "Id", "Nome");
            return View(new CalendarioTorneio());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CalendarioTorneio calendarioTorneio)
        {
            if (ModelState.IsValid)
            {
                db.CalendarioTorneio.Add(calendarioTorneio);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.ModalidadeTorneioId = new SelectList(db.ModalidadeTorneio, "Id", "Nome", calendarioTorneio.ModalidadeTorneioId);
            ViewBag.StatusInscricaoTorneioId = new SelectList(db.StatusInscricaoTorneio, "Id", "Nome", calendarioTorneio.StatusInscricaoTorneioId);
            return View(calendarioTorneio);
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CalendarioTorneio calendarioTorneio = db.CalendarioTorneio.Find(id);
            if (calendarioTorneio == null)
            {
                return HttpNotFound();
            }
            ViewBag.ModalidadeTorneioId = new SelectList(db.ModalidadeTorneio, "Id", "Nome", calendarioTorneio.ModalidadeTorneioId);
            ViewBag.StatusInscricaoTorneioId = new SelectList(db.StatusInscricaoTorneio, "Id", "Nome", calendarioTorneio.StatusInscricaoTorneioId);
            return View(calendarioTorneio);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CalendarioTorneio calendarioTorneio)
        {
            if (ModelState.IsValid)
            {
                db.Entry(calendarioTorneio).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.ModalidadeTorneioId = new SelectList(db.ModalidadeTorneio, "Id", "Nome", calendarioTorneio.ModalidadeTorneioId);
            ViewBag.StatusInscricaoTorneioId = new SelectList(db.StatusInscricaoTorneio, "Id", "Nome", calendarioTorneio.StatusInscricaoTorneioId);
            return View(calendarioTorneio);
        }

        public ActionResult Delete(int id)
        {
            CalendarioTorneio calendarioTorneio = db.CalendarioTorneio.Find(id);
            db.CalendarioTorneio.Remove(calendarioTorneio);
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
