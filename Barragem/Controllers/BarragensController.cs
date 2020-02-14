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
    public class BarragensController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();

        //
        // GET: /Rodada/

        [Authorize(Roles = "admin,organizador")]
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
            List<Barragens> barragens = null;
            
            if (Roles.IsUserInRole("organizador")){
                UserProfile usu = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
                barragens = new List<Barragens>();
                barragens.Add(db.Barragens.Find(usu.barragemId));
            }else{
                barragens = db.Barragens.ToList();
            }
            return View(barragens);
        }

        [Authorize(Roles = "admin")]
        public ActionResult Create()
        {
            return View();
        }

       
        [Authorize(Roles = "admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Barragens barragens)
        {
            try
            {
                var codigo=91;
                var sql="";
                if (barragens.valorPorUsuario == null) barragens.valorPorUsuario = 5;
                if (barragens.soTorneio == null) barragens.soTorneio = false;
                using (TransactionScope scope = new TransactionScope())
                {
                    if (ModelState.IsValid){
                        if (!barragens.email.Equals("")) { 
                            if (!Funcoes.IsValidEmail(barragens.email)){
                                ViewBag.MsgErro = string.Format("E-mail inválido. '{0}'", barragens.email);
                                return View(barragens);
                            }
                        }
                        var meuRanking = db.Barragens.Find(8);
                        barragens.regulamento = meuRanking.regulamento;
                        db.Barragens.Add(barragens);
                        db.SaveChanges();
                        for (int i = 1; i <= 10; i++){
                            sql = "INSERT INTO Rodada(codigo, dataInicio, dataFim, isAberta, sequencial, isRodadaCarga, barragemId) " +
                            "VALUES (" + codigo + ",'2000-01-01','2000-01-01', 0, "+ i +", 1, " + barragens.Id + ")";
                            db.Database.ExecuteSqlCommand(sql);
                            codigo = codigo + 1;
                        }
                        for (int i = 1; i <= 5; i++)
                        {
                            sql = "INSERT INTO Classe (nome, nivel, barragemId) VALUES ('" + i + "ª Classe'," + i + ", " + barragens.Id + ")";
                            db.Database.ExecuteSqlCommand(sql);
                        }
                        scope.Complete();
                        return RedirectToAction("Index");
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.MsgErro = ex.Message;
            }
            
            return View(barragens);
        }

        // GET: /Rodada/Edit/5
        [Authorize(Roles = "admin,organizador")]
        public ActionResult Edit(int id = 0)
        {
            Barragens barragens = db.Barragens.Find(id);
            ViewBag.BarragemId = barragens.Id;
            if (barragens == null)
            {
                return HttpNotFound();
            }
            ViewBag.flag = "edit";
            return View(barragens);
        }

        //
        // POST: /Rodada/Edit/5
        [Authorize(Roles = "admin,organizador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Barragens barragens)
        {
            if (Roles.IsUserInRole("organizador")){
                UserProfile usu = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
                if (usu.barragemId != barragens.Id){
                    ViewBag.MsgErro = "Você não pertence a esta barragem.";
                    return View(barragens);
                }
                
                var barraAtual = db.BarragemView.Find(barragens.Id);
                // o organizador não deve alterar os campos abaixo
                barragens.valorPorUsuario = barraAtual.valorPorUsuario;
                barragens.isAtiva = barraAtual.isAtiva;
                barragens.isTeste = barraAtual.isTeste;
                barragens.soTorneio = barraAtual.soTorneio;
            }
            if (barragens.soTorneio == null) barragens.soTorneio = false;
            if (ModelState.IsValid){
                if (barragens.email!=null){
                    if (!Funcoes.IsValidEmail(barragens.email)){
                        ViewBag.MsgErro = string.Format("E-mail inválido. '{0}'", barragens.email);
                        return View(barragens);
                    }
                }
                db.Entry(barragens).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(barragens);
        }

        [Authorize(Roles = "admin,organizador")]
        public ActionResult EditClasse(int barragemId)
        {
            var classes = db.Classe.Where(c => c.barragemId == barragemId).ToList();
            ViewBag.flag = "classes";
            ViewBag.BarragemId = barragemId;

            var list = new[]
            {
                new SelectListItem { Value = "1", Text = "1º" },
                new SelectListItem { Value = "2", Text = "2º" },
                new SelectListItem { Value = "3", Text = "3º" },
                new SelectListItem { Value = "4", Text = "4º" },
                new SelectListItem { Value = "5", Text = "5º" },
            };

            ViewBag.Niveis = new SelectList(list, "Value", "Text");

            return View(classes);
        }

        [HttpPost]
        public ActionResult EditClasse(int Id, string nome, int nivel = 1, bool ativa = true)
        {
            try
            {
                var classe = db.Classe.Find(Id);
                if (!ativa)
                {
                    //só desativa classe se já tiver fechado a rodada
                    //classe.barragemId
                    var qtdeRodadasAbertas = db.Rodada.Where(r => r.barragemId == classe.barragemId && r.isAberta == true).Count();
                    if (qtdeRodadasAbertas>0)
                    {
                        return Json(new { erro = "É necessário fechar as rodadas para desabilitar uma classe.", retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
                    }
                }
                classe.nome = nome;
                classe.ativa = ativa;
                classe.nivel = nivel;
                db.Entry(classe).State = EntityState.Modified;
                db.SaveChanges();
                return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public ActionResult Delete(int id)
        {
            Barragens barragens = db.Barragens.Find(id);
            db.Barragens.Remove(barragens);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "admin")]
        public ActionResult Painel()
        {
            ViewBag.TenistasCadastrados = db.UserProfiles.Count();
            ViewBag.TenistasContribuintes = db.UserProfiles.Where(u=>u.barragem.isAtiva==true && (u.situacao == "ativo" || u.situacao == "licenciado" || u.situacao == "suspenso" || u.situacao == "suspensoWO")).Count();
            ViewBag.TenistasAtivos = db.UserProfiles.Where(u => u.barragem.isAtiva == true && u.situacao == "ativo").Count();
            ViewBag.TenistasLicenciados = db.UserProfiles.Where(u => u.barragem.isAtiva == true && u.situacao == "licenciado").Count();
            ViewBag.TenistasSuspensos = db.UserProfiles.Where(u => u.barragem.isAtiva == true && (u.situacao == "suspenso" || u.situacao == "suspensoWO")).Count();

            ViewBag.RankingsAtivos = db.Barragens.Where(u => u.isAtiva == true).Count();
            ViewBag.RankingsInativos = db.Barragens.Where(u => u.isAtiva == false).Count();

            return View();
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}