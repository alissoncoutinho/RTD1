using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Barragem.Models;
using Barragem.Context;
using Barragem.Filters;
using System.Web.Security;
using WebMatrix.WebData;

namespace Barragem.Controllers
{
    [Authorize]
    [InitializeSimpleMembership]
    public class TemporadaController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();
        //
        
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

            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            List<Temporada> temporada = null;
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            int barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
            if (perfil.Equals("admin")){
                temporada = db.Temporada.OrderByDescending(c => c.Id).ToList();
            }else{
                temporada = db.Temporada.Where(r => r.barragemId == barragemId).OrderByDescending(c => c.Id).ToList();
            }

            return View(temporada);
        }

        public ActionResult Edit(int id = 0)
        {
            Temporada temporada = db.Temporada.Find(id);
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            var barragemId = 0;
            if (perfil.Equals("admin")){
                barragemId = temporada.barragemId;
            } else {
                barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
            }
            ViewBag.barraId = barragemId;
            ViewBag.barragemId = new SelectList(db.BarragemView, "Id", "nome",barragemId);
            ViewBag.JogadoresClasses = db.RankingView.Where(r => r.barragemId == barragemId && (r.situacao.Equals("ativo") ||r.situacao.Equals("suspenso") || r.situacao.Equals("suspensoWO") || r.situacao.Equals("licenciado"))).OrderBy(r=>r.nivel).ThenByDescending(r=>r.totalAcumulado).ToList();
            ViewBag.Classes = db.Classe.Where(c => c.barragemId == barragemId).ToList();
            ViewBag.temRodadaAberta = db.Rodada.Where(u => u.isAberta && u.barragemId == barragemId && !u.isRodadaCarga).Count();
            
            if (temporada == null){
                return HttpNotFound();
            }
            return View(temporada);
        }
        [HttpPost]
        public ActionResult AlterarClassesJogadores(IEnumerable<RankingView> rankingView)
        {
            try
            {
                UserProfile jogador = null;
                foreach (RankingView user in rankingView)
                {
                    jogador = db.UserProfiles.Find(user.userProfile_id);
                    var barraId = jogador.barragemId;
                    var classe = db.Classe.Where(c => c.barragemId == barraId && c.nivel == user.classeId).Single();
                    jogador.classeId = classe.Id;
                    db.Entry(jogador).State = EntityState.Modified;
                    db.SaveChanges();
                }
                return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult Edit2()
        {
            return View();
        }

        [Authorize(Roles = "admin, organizador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Temporada temporada)
        {
            if (ModelState.IsValid)
            {
                db.Entry(temporada).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(temporada);
        }

        [Authorize(Roles = "admin, organizador")]
        public ActionResult Create()
        {
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            var barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
            ViewBag.barraId = barragemId;
            ViewBag.barragemId = new SelectList(db.BarragemView, "Id", "nome");

            Temporada temporada = new Temporada();
            temporada.isAutomatico = false;
            temporada.iniciarZerada = false;

            return View(temporada);
        }

        //
        // POST: /Rodada/Create

        [Authorize(Roles = "admin, organizador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Temporada temporada)
        {
            if (ModelState.IsValid)
            {

                db.Temporada.Add(temporada);
                db.SaveChanges();
                return RedirectToAction("Index");
            } else {
                var userId = WebSecurity.GetUserId(User.Identity.Name);
                var barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
                ViewBag.barraId = barragemId;
                ViewBag.barragemId = new SelectList(db.BarragemView, "Id", "nome");
            }

            return View(temporada);
        }

        [HttpGet]
        public void GerarRodadasAutomaticas()
        {
            DateTime hoje = DateTime.Now;
            RodadaController rodadaController = new RodadaController();
            //consultar as temporadas automaticas
            List<Temporada> temporadas = db.Temporada.Where(t => t.isAutomatico
                       && t.dataInicio <= hoje && hoje <= t.dataFim).ToList();

            //para cada temporada
            //  1. tratar zeramento do ranking se for o caso(a implementar)
            //  2.fechar rodada
            //  3.criar nova rodada
            //  4.sortear jogos da rodada
            //  5.enviar push(a implementar)

            foreach (Temporada temporada in temporadas)
            {
                List<Rodada> rodadas = db.Rodada.Where(r => r.temporadaId == temporada.Id).ToList();
                int quantidadeDeRodadasRealizadas = rodadas.Count;
                if (quantidadeDeRodadasRealizadas == 0)
                {
                    criarNovaRodadaComJogos(rodadaController, temporada, hoje);
                    continue;
                }
                Rodada rodadaAtual = rodadas.Last();
                if (hoje == temporada.dataFim)
                {
                    temporada.isAutomatico = false;
                    temporada.isAtivo = false;
                    db.Entry(temporada).State = EntityState.Modified;
                    db.SaveChanges();
                    continue;
                }
                if (hoje.Subtract(rodadaAtual.dataFim).Days == temporada.frequencia)
                {
                    rodadaController.FecharRodada(rodadaAtual.Id);
                    criarNovaRodadaComJogos(rodadaController, temporada, hoje);
                }
            }
        }

        private static void criarNovaRodadaComJogos(RodadaController rodadaController, Temporada temporada, DateTime hoje)
        {
            Rodada novaRodada = new Rodada();
            novaRodada.temporadaId = temporada.Id;
            novaRodada.barragemId = temporada.barragemId;
            novaRodada.dataInicio = hoje;
            novaRodada.dataFim = hoje.AddDays(temporada.frequencia.Value);
            rodadaController.Create(novaRodada);
            rodadaController.SortearJogos(novaRodada.Id, temporada.barragemId);
        }

    }
}
