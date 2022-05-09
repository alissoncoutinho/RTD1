using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Barragem.Context;
using Barragem.Helper;
using Barragem.Models;

namespace Barragem.Controllers
{
    [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis")]
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

            var idBarragem = Request.ObterIdBarragemUsuarioLogado();

            var calendarioTorneio =
                db.CalendarioTorneio
                    .Include(c => c.ModalidadeTorneio)
                    .Include(c => c.StatusInscricaoTorneio)
                    .Where(x => (x.DataInicial.Year == filtroAno || x.DataFinal.Year == filtroAno) && x.BarragemId == idBarragem)
                    .OrderBy(o => o.DataInicial).ThenBy(o => o.DataFinal).ThenBy(o => o.Nome);

            var dadosListagem = MapearDadosModelo(calendarioTorneio.ToList());

            return View(dadosListagem);
        }

        public ActionResult Create()
        {
            CarregarDropDownLists(null);
            return View(new CalendarioTorneio());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CalendarioTorneio calendarioTorneio)
        {
            calendarioTorneio.BarragemId = Request.ObterIdBarragemUsuarioLogado();

            if (ValidarDados(calendarioTorneio))
            {
                if (ModelState.IsValid)
                {
                    db.CalendarioTorneio.Add(calendarioTorneio);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            CarregarDropDownLists(calendarioTorneio);
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

            var idBarragem = Request.ObterIdBarragemUsuarioLogado();
            if (calendarioTorneio.BarragemId != idBarragem)
            {
                ViewBag.MsgErro = "Usuário sem permissão para alterar o torneio";
                return RedirectToAction("Index");
            }

            CarregarDropDownLists(calendarioTorneio);
            return View(calendarioTorneio);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CalendarioTorneio calendarioTorneio)
        {
            calendarioTorneio.BarragemId = Request.ObterIdBarragemUsuarioLogado();
            if (ValidarDados(calendarioTorneio))
            {
                if (ModelState.IsValid)
                {
                    db.Entry(calendarioTorneio).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            CarregarDropDownLists(calendarioTorneio);
            return View(calendarioTorneio);
        }

        public ActionResult Delete(int id)
        {
            CalendarioTorneio calendarioTorneio = db.CalendarioTorneio.Find(id);

            var idBarragem = Request.ObterIdBarragemUsuarioLogado();
            if (calendarioTorneio.BarragemId != idBarragem)
            {
                ViewBag.MsgErro = "Usuário sem permissão para alterar o torneio";
                return RedirectToAction("Index");
            }

            db.CalendarioTorneio.Remove(calendarioTorneio);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        private bool ValidarDados(CalendarioTorneio calendarioTorneio)
        {
            if (calendarioTorneio == null)
            {
                ViewBag.MsgErro = "Dados inválidos";
                return false;
            }

            if (calendarioTorneio.DataFinal < calendarioTorneio.DataInicial)
            {
                ViewBag.MsgErro = "Data Final não pode ser menor que a Data Inicial.";
                return false;
            }

            if (calendarioTorneio.StatusInscricaoTorneioId == (int)EnumStatusInscricao.ABERTA && string.IsNullOrEmpty(calendarioTorneio.LinkInscricao))
            {
                ViewBag.MsgErro = "Inscrição Aberta requer que seja informado o link de inscrição.";
                return false;
            }
            return true;
        }

        private CalendarioTorneioModel MapearDadosModelo(List<CalendarioTorneio> torneios)
        {
            var lista = new List<CalendarioTorneioModel.CalendarioTorneioItens>();
            foreach (var item in torneios)
            {
                var mes = item.DataInicial.GetMonthName();
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

        private void CarregarDropDownLists(CalendarioTorneio calendarioTorneio)
        {
            if (calendarioTorneio == null)
            {
                ViewBag.ModalidadeTorneioId = new SelectList(db.ModalidadeTorneio, "Id", "Nome");
                ViewBag.StatusInscricaoTorneioId = new SelectList(db.StatusInscricaoTorneio, "Id", "Nome", (int)EnumStatusInscricao.NAO_ABRIU);
            }
            else
            {
                ViewBag.ModalidadeTorneioId = new SelectList(db.ModalidadeTorneio, "Id", "Nome", calendarioTorneio.ModalidadeTorneioId);
                ViewBag.StatusInscricaoTorneioId = new SelectList(db.StatusInscricaoTorneio, "Id", "Nome", calendarioTorneio.StatusInscricaoTorneioId);
            }
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
