using System;
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

namespace Barragem.Controllers
{
    [Authorize]
    [InitializeSimpleMembership]
    public class JogoController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();
        private RodadaNegocio rn = new RodadaNegocio();
        //
        // GET: /Jogo/

        [AllowAnonymous]
        public ActionResult Index(int rodadaId = 0, string ranking="")
        {
            string msg = "";
            BarragemView bV = null;
            var barragemId = 0;
            try{
                if ((rodadaId == 0) && (ranking == "") && (User.Identity.Name != "")){
                    UserProfile usuario = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
                    bV = usuario.barragem;
                    rodadaId = db.Rodada.Where(r => r.isRodadaCarga == false && r.barragemId == usuario.barragemId).Max(r => r.Id);
                    ViewBag.IdBarragem = bV.Id;
                    ViewBag.NomeBarragem = bV.nome;
                } else if (ranking != ""){
                    bV = db.BarragemView.Where(b => b.dominio == ranking).FirstOrDefault();
                    rodadaId = db.Rodada.Where(r => r.isRodadaCarga == false && r.barragemId == bV.Id).Max(r => r.Id);
                    ViewBag.IdBarragem = bV.Id;
                    ViewBag.NomeBarragem = bV.nome;
                } else if (rodadaId == 0)            {
                    return RedirectToAction("Login", "Account");
                } else {
                    HttpCookie cookie = new HttpCookie("_barragemId");
                    bV = db.BarragemView.Find(barragemId);
                }
            }
            catch (Exception e) {
                return RedirectToAction("Login", "Account");
            }
            try{
                var jogo = db.Jogo.Include(j => j.desafiado).Include(j => j.desafiante).Include(j => j.rodada).
                    Where(j => j.rodada_id == rodadaId).OrderBy(j => j.desafiado.classe.nivel).ToList();
                msg = "jogos";
                if ((rodadaId > 0) && (jogo.Count() > 0)){
                    barragemId = jogo[0].rodada.barragemId;
                }
                ViewBag.Classes = db.Classe.Where(c => c.barragemId == barragemId && c.ativa == true).ToList();
                if (jogo.Count() > 0)
                {
                    barragemId = jogo[0].rodada.barragemId;
                    ViewBag.Rodada = jogo[0].rodada.codigoSeq;
                    ViewBag.DataInicial = jogo[0].rodada.dataInicio;
                    ViewBag.DataFinal = jogo[0].rodada.dataFim;
                    msg = "rodada";
                    if (jogo[0].rodada.temporadaId != null)
                    {
                        var temporadaId = jogo[0].rodada.temporadaId;
                        var qtddRodada = db.Rodada.Where(r => r.temporadaId == temporadaId && r.Id <= rodadaId && r.barragemId == barragemId).Count();
                        ViewBag.Temporada = jogo[0].rodada.temporada.nome + " - Rodada " + qtddRodada + " de " + jogo[0].rodada.temporada.qtddRodadas;
                        msg = "temporada";
                    }
                    else { ViewBag.Temporada = ""; }
                }
                return View(jogo);
            }
            catch (Exception ex)
            {
                ViewBag.MsgErro = msg + " - " + ex.Message;
                return View();
            }

        }

        //
        // GET: /Jogo/Details/5

        [Authorize(Roles = "admin, organizador, usuario")]
        public ActionResult ListarJogosJogador(int idJogador = 0)
        {
            if (idJogador == 0)
            {
                UserProfile usuario = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
                idJogador = usuario.UserId;
            }
            List<Jogo> jogos = db.Jogo.Include(j => j.desafiado).Include(j => j.desafiante).Include(j => j.rodada).Where(j => j.desafiante_id == idJogador || j.desafiado_id == idJogador).OrderByDescending(j => j.rodada_id).ToList();
            return View(jogos);
        }


        //
        // GET: /Jogo/Create
        [Authorize(Roles = "admin, organizador")]
        public ActionResult Create()
        {
            ViewBag.desafiado_id = new SelectList(db.UserProfiles, "UserId", "UserName");
            ViewBag.desafiante_id = new SelectList(db.UserProfiles, "UserId", "UserName");
            ViewBag.rodada_id = new SelectList(db.Rodada, "Id", "codigo");
            return View();
        }

        //
        // POST: /Jogo/Create
        [Authorize(Roles = "admin, organizador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Jogo jogo)
        {
            if (ModelState.IsValid)
            {
                db.Jogo.Add(jogo);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.desafiado_id = new SelectList(db.UserProfiles, "UserId", "UserName", jogo.desafiado_id);
            ViewBag.desafiante_id = new SelectList(db.UserProfiles, "UserId", "UserName", jogo.desafiante_id);
            ViewBag.rodada_id = new SelectList(db.Rodada, "Id", "codigo", jogo.rodada_id);
            return View(jogo);
        }

        //
        // GET: /Jogo/Edit/5
        [Authorize(Roles = "admin, organizador")]
        public ActionResult Edit(int id = 0)
        {
            Jogo jogo = db.Jogo.Find(id);
            if (jogo == null)
            {
                return HttpNotFound();
            }
            ViewBag.desafiado_id = new SelectList(db.UserProfiles.Where(u => u.situacao == "ativo" && u.barragemId == jogo.rodada.barragemId).OrderBy(u => u.nome), "UserId", "nome", jogo.desafiado_id);
            ViewBag.desafiante_id = new SelectList(db.UserProfiles.Where(u => (u.situacao == "ativo" && u.barragemId == jogo.rodada.barragemId) || u.UserId == 8).OrderBy(u => u.nome), "UserId", "nome", jogo.desafiante_id);
            ViewBag.rodada_id = new SelectList(db.Rodada, "Id", "codigo", jogo.rodada_id);
            return View(jogo);
        }

        //
        // POST: /Jogo/Edit/5

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin, organizador")]
        public ActionResult Edit(Jogo jogo)
        {
            if (ModelState.IsValid)
            {
                db.Entry(jogo).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.desafiado_id = new SelectList(db.UserProfiles, "UserId", "UserName", jogo.desafiado_id);
            ViewBag.desafiante_id = new SelectList(db.UserProfiles, "UserId", "UserName", jogo.desafiante_id);
            ViewBag.rodada_id = new SelectList(db.Rodada, "Id", "codigo", jogo.rodada_id);
            return View(jogo);
        }

        [Authorize(Roles = "admin,usuario,organizador")]
        public ActionResult ControlarJogo(int id = 0)
        {
            Jogo jogo = null;
            if (id == 0)
            {
                try
                {
                    var usuario = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
                    jogo = db.Jogo.Where(u => u.desafiado_id == usuario.UserId || u.desafiante_id == usuario.UserId)
                        .OrderByDescending(u => u.Id).Take(1).Single();
                }
                catch (System.InvalidOperationException e)
                {
                    ViewBag.MsgAlert = "Não foi possível encontrar jogos em aberto:" + e.Message;
                }
            }
            else
            {
                jogo = db.Jogo.Find(id);
            }
            if (jogo != null)
            {
                //nao permitir edição caso a rodada já esteja fechada e o placar já tenha sido informado
                string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
                if (!perfil.Equals("admin") && !perfil.Equals("organizador") && (jogo.rodada.isAberta == false) && (jogo.gamesJogados != 0))
                {
                    ViewBag.Editar = false;
                }
                else
                {
                    ViewBag.Editar = true;
                }
                ViewBag.situacao_Id = new SelectList(db.SituacaoJogo, "Id", "descricao", jogo.situacao_Id);
                ViewBag.ptDefendidosDesafiado = getPontosDefendidos(jogo.desafiado_id, jogo.rodada_id);
                ViewBag.ptDefendidosDesafiante = getPontosDefendidos(jogo.desafiante_id, jogo.rodada_id);

            }
            return View(jogo);
        }


        private double getPontosDefendidos(int UserId, int rodada_id)
        {
            double pontosDefendidos = 0;
            List<Rancking> listRanking = db.Rancking.Where(r => r.userProfile_id == UserId && r.rodada_id < rodada_id).Take(10).OrderByDescending(r => r.Id).ToList();
            foreach (Rancking ranking in listRanking)
            {
                pontosDefendidos = ranking.pontuacao;
            }


            return pontosDefendidos;
        }

        
        //
        // GET: /Jogo/Delete/5
        [Authorize(Roles = "admin, organizador")]
        public ActionResult Delete(int id = 0)
        {
            Jogo jogo = db.Jogo.Find(id);
            if (jogo == null)
            {
                return HttpNotFound();
            }
            return View(jogo);
        }

        //
        // POST: /Jogo/Delete/5

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin, organizador")]
        public ActionResult DeleteConfirmed(int id)
        {
            Jogo jogo = db.Jogo.Find(id);
            db.Jogo.Remove(jogo);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,usuario,organizador")]
        public ActionResult InserirResultado(Jogo jogo, bool isTorneio = false)
        {
            Jogo jogoAtual = db.Jogo.Find(jogo.Id);
            //games
            if (jogo.situacao_Id == 1) {
                jogoAtual.qtddGames1setDesafiado  = 0;
                jogoAtual.qtddGames2setDesafiado  = 0;
                jogoAtual.qtddGames3setDesafiado  = 0;
                jogoAtual.qtddGames1setDesafiante = 0;
                jogoAtual.qtddGames2setDesafiante = 0;
                jogoAtual.qtddGames3setDesafiante = 0;
                //informações do jogo
                jogoAtual.usuarioInformResultado = "";
                jogoAtual.situacao_Id = 1;
                jogoAtual.horaJogo = "";
                jogoAtual.localJogo = "";
                jogoAtual.dataJogo = null;
                jogoAtual.dataCadastroResultado = null;
            }
            else{
                jogoAtual.qtddGames1setDesafiado = jogo.qtddGames1setDesafiado;
                jogoAtual.qtddGames2setDesafiado = jogo.qtddGames2setDesafiado;
                jogoAtual.qtddGames3setDesafiado = jogo.qtddGames3setDesafiado;
                jogoAtual.qtddGames1setDesafiante = jogo.qtddGames1setDesafiante;
                jogoAtual.qtddGames2setDesafiante = jogo.qtddGames2setDesafiante;
                jogoAtual.qtddGames3setDesafiante = jogo.qtddGames3setDesafiante;
                //informações do jogo
                jogoAtual.usuarioInformResultado = User.Identity.Name;
                jogoAtual.dataCadastroResultado = DateTime.Now;
                jogoAtual.situacao_Id = 4;
            }
            

            //
            db.Entry(jogoAtual).State = EntityState.Modified;
            db.SaveChanges();

            rn.ProcessarJogoAtrasado(jogoAtual);

            ViewBag.Sucesso = true;
            ViewBag.MsgAlerta = "Resultado lançado com sucesso.";
            var barragemId = 0;
            if (isTorneio) return RedirectToAction("LancarResultado2", "Torneio", new { jogo.Id, barragemId, ViewBag.Sucesso, ViewBag.MsgAlerta });
            return RedirectToAction("Index3", "Home", new { ViewBag.Sucesso, ViewBag.MsgAlerta });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,usuario,organizador")]
        public ActionResult LancarWO(int Id, int vencedorWO, bool isTorneio = false)
        {
            Jogo jogoAtual = db.Jogo.Find(Id);
            //alterar quantidade de games para desafiado e desafiante
            int gamesDesafiante = 0;
            int gamesDesafiado = 0;
            if (jogoAtual.desafiado_id == vencedorWO)
            {
                gamesDesafiado = 6;
                gamesDesafiante = 1;
            }
            else
            {
                gamesDesafiado = 1;
                gamesDesafiante = 6;
            }
            jogoAtual.qtddGames1setDesafiado  = gamesDesafiado;
            jogoAtual.qtddGames1setDesafiante = gamesDesafiante;
            jogoAtual.qtddGames2setDesafiado  = gamesDesafiado;
            jogoAtual.qtddGames2setDesafiante = gamesDesafiante;
            jogoAtual.qtddGames3setDesafiado  = 0;
            jogoAtual.qtddGames3setDesafiante = 0;
            //alterar status do jogo WO
            jogoAtual.situacao_Id = 5;
            jogoAtual.usuarioInformResultado = User.Identity.Name;
            jogoAtual.dataCadastroResultado = DateTime.Now;
            db.Entry(jogoAtual).State = EntityState.Modified;
            db.SaveChanges();

            rn.ProcessarJogoAtrasado(jogoAtual);

            ViewBag.Sucesso = true;
            ViewBag.MsgAlerta = "WO lançado com sucesso.";
            var barragemId = 0;
            if (isTorneio) return RedirectToAction("LancarResultado2", "Torneio", new { Id, barragemId, ViewBag.Sucesso, ViewBag.MsgAlerta });
            return RedirectToAction("Index3", "Home", new { ViewBag.Sucesso, ViewBag.MsgAlerta });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,usuario,organizador")]
        public ActionResult MarcarJogo(int Id, DateTime dataJogo, string horaJogo, string localJogo="", bool isTorneio=false, bool desmarcar=false)
        {
            Jogo jogoAtual = db.Jogo.Find(Id);
            if (desmarcar)
            {
                jogoAtual.dataJogo = null;
                jogoAtual.horaJogo = null;
                jogoAtual.localJogo = null;
                jogoAtual.situacao_Id = 1;
                ViewBag.MsgAlerta = "Jogo desmarcado.";
            }
            else
            {
                jogoAtual.dataJogo = dataJogo;
                jogoAtual.horaJogo = horaJogo;
                jogoAtual.localJogo = localJogo;
                //alterar a situação do jogo para marcado
                jogoAtual.situacao_Id = 2;
                ViewBag.MsgAlerta = "Jogo marcado com sucesso.";
            }
            db.Entry(jogoAtual).State = EntityState.Modified;
            db.SaveChanges();
            ViewBag.Sucesso = true;
            
            var barragemId = 0;
            if (isTorneio) return RedirectToAction("LancarResultado2", "Torneio", new { Id, barragemId, ViewBag.Sucesso, ViewBag.MsgAlerta });

            return RedirectToAction("Index3", "Home", new { ViewBag.Sucesso, ViewBag.MsgAlerta });
        }


    }

}