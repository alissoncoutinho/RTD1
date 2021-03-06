﻿using System;
using System.Data;
using System.Data.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebMatrix.WebData;
using Barragem.Filters;
using Barragem.Models;
using Barragem.Context;
using System.Web.Security;
using System.Transactions;
using Barragem.Class;
using System.Dynamic;

namespace Barragem.Controllers
{
    public class LigaController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();

        [Authorize(Roles = "admin,organizador")]
        public ActionResult Index(string msg = "", string detalheErro = "")
        {
            List<BarragemLiga> ligasDaBarragem = null;
            List<Liga> ligas = null;
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            int barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
            if (perfil.Equals("admin"))
            {
                ligas = db.Liga.OrderByDescending(l => l.Id).ToList();
            }
            else
            {
                ligasDaBarragem = db.BarragemLiga.Where(r => r.BarragemId == barragemId).OrderByDescending(c => c.Id).ToList();
                foreach(BarragemLiga ligaBarragem in ligasDaBarragem)
                {
                    ligas.Add(ligaBarragem.Liga);
                }
            }

            return View(ligas);
        }

        [Authorize(Roles = "admin, organizador, usuario")]
        public ActionResult Create()
        {
            
            Liga liga = new Liga();
            /*liga.Nome = "liga teste";
            var classes = new List<ClasseLiga>();
            classes.Add(new ClasseLiga { Nome = "1 classe", Nivel = "1" });
            dynamic model = new ExpandoObject();
            model.Classes = classes;
            model.Liga = liga;
            model.Rankings = new List<BarragemLiga>();*/
            return View(liga);
        }

        [Authorize(Roles = "admin, organizador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Liga liga)
        {
            if (ModelState.IsValid)
            {

                db.Liga.Add(liga);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(liga);
        }

        public ActionResult Edit(int idLiga = 0)
        {
            Liga liga = db.Liga.Find(idLiga);
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            
            ViewBag.ClassesDaLiga = db.ClasseLiga.Where(c => c.LigaId == idLiga).ToList();
            ViewBag.BarragensDaLiga = db.BarragemLiga.Where(bl => bl.LigaId == idLiga).ToList();

            ViewBag.flag = "classes";

            return View(liga);
        }

        public ActionResult EditClasses(int idLiga = 0)
        {
            Liga liga = db.Liga.Find(idLiga);
            ViewBag.idLiga = idLiga;
            ViewBag.nomeLiga = liga.Nome;
            
            List<Categoria> categorias = new List<Categoria>();
            categorias.Add(new Categoria { Id = 0, Nome = "" });
            categorias.AddRange(db.Categoria.OrderBy(c => c.Nome).ToList());
            ViewBag.Categorias = categorias;

            var classes = db.ClasseLiga.Where(c => c.LigaId == idLiga).ToList();
            ViewBag.flag = "classes";
            return View(classes);
        }

        [HttpPost]
        public ActionResult AddClasse(String idLiga, String nome, String idCategoria)
        {
            try {
                int idCat = Int32.Parse(idCategoria);
                if (nome == "" || idCategoria == "" || idCat == 0)
                {
                    throw new Exception("Por favor preencha os campos obrigatórios(nome e categoria).");
                }
                ClasseLiga cl = new ClasseLiga();
                cl.LigaId = Int32.Parse(idLiga);
                cl.Nome = nome;
                cl.CategoriaId = idCat;
                db.ClasseLiga.Add(cl);
                db.SaveChanges();

                Categoria cat = db.Categoria.Find(idCat);
                return Json(new { erro = "", retorno = 1 , nome = cl.Nome, categoria = cat.Nome, IdClasseLiga = cl.Id}, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult RemoveClasse(String id)
        {
            try
            {
                ClasseLiga cl = db.ClasseLiga.Find(Int32.Parse(id));
                db.ClasseLiga.Remove(cl);
                db.SaveChanges();
                return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult EditBarragens(int idLiga = 0)
        {
            ViewBag.flag = "barragens";

            Liga liga = db.Liga.Find(idLiga);
            ViewBag.idLiga = idLiga;
            ViewBag.nomeLiga = liga.Nome;

            List<Barragens> barragens = new List<Barragens>();
            barragens.Add(new Barragens { Id = 0, nome = "" });
            barragens.AddRange(db.Barragens.OrderBy(b => b.nome).ToList());
            ViewBag.Barragens = barragens;

            var barragensDaLiga = db.BarragemLiga.Where(bl => bl.LigaId == idLiga).ToList();
            return View(barragensDaLiga);
        }

        [HttpPost]
        public ActionResult AddBarragem(String idLiga, String idBarragem, String TipoTorneio)
        {
            try
            {
                int idBarra = Int32.Parse(idBarragem);
                if (idBarragem == "" || idBarra == 0 || TipoTorneio == "")
                {
                    throw new Exception("Por favor selecione o ranking a ser adicionado e o tipo de torneio.");
                }
                BarragemLiga bl = new BarragemLiga();
                bl.LigaId = Int32.Parse(idLiga);
                bl.BarragemId = idBarra;
                bl.TipoTorneio = TipoTorneio;
                db.BarragemLiga.Add(bl);
                db.SaveChanges();
                Barragens barra = db.Barragens.Find(idBarra);
                return Json(new { erro = "", retorno = 1, Nome = barra.nome, IdBarragemLiga = bl.Id , TipoTorneio = TipoTorneio}, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult RemoveBarragem(String id)
        {
            try
            {
                BarragemLiga bl = db.BarragemLiga.Find(Int32.Parse(id));
                db.BarragemLiga.Remove(bl);
                db.SaveChanges();
                return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }
    }
}