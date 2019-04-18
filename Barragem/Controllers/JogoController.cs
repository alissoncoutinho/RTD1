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

        [Authorize(Roles = "admin, organizador, usuario")]
        public ActionResult Index(int rodadaId = 0)
        {
            string msg = "";
            int barragemId = 0;
            try
            {
                if (rodadaId == 0)
                {
                    UserProfile usuario = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
                    barragemId = usuario.barragemId;
                    rodadaId = db.Rodada.Where(r => r.isRodadaCarga == false && r.barragemId == usuario.barragemId).Max(r => r.Id);
                    if (usuario == null)
                    {
                        if (Request.Cookies["_barragemId"] != null)
                        {
                            HttpCookie cookie = new HttpCookie("_barragemId");
                            barragemId = Convert.ToInt32(cookie.Value.ToString());
                        }
                        else
                        {
                            barragemId = 1;
                        }
                    }
                    else
                    {
                        barragemId = usuario.barragemId;
                    }
                }
            }
            catch (InvalidOperationException) { }
            try
            {
                var jogo = db.Jogo.Include(j => j.desafiado).Include(j => j.desafiante).Include(j => j.rodada).
                    Where(j => j.rodada_id == rodadaId).OrderBy(j => j.desafiado.classe.nivel).ToList();
                msg = "jogos";
                if ((rodadaId > 0) && (jogo.Count() > 0))
                {
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
            ViewBag.desafiante_id = new SelectList(db.UserProfiles.Where(u => u.situacao == "ativo" && u.barragemId == jogo.rodada.barragemId).OrderBy(u => u.nome), "UserId", "nome", jogo.desafiante_id);
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
        // POST: /Jogo/Edit/5

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,usuario,organizador")]
        public ActionResult ControlarJogo(Jogo jogo)
        {
            int setDesafiante = 0;
            int setDesafiado = 0;
            if (ModelState.IsValid)
            {
                jogo.dataCadastroResultado = DateTime.Now;
                jogo.usuarioInformResultado = User.Identity.Name;
                if (jogo.desafiado_id == WebSecurity.GetUserId(User.Identity.Name))
                {
                    setDesafiado = 6;
                    setDesafiante = 1;
                }
                else
                {
                    setDesafiado = 1;
                    setDesafiante = 6;
                }
                jogo.idVencedor = 0;
                jogo.idPerderdor = 0;
                if (jogo.situacao_Id == 5)
                { //WO
                    jogo.qtddGames1setDesafiado = setDesafiado;
                    jogo.qtddGames1setDesafiante = setDesafiante;
                    jogo.qtddGames2setDesafiado = setDesafiado;
                    jogo.qtddGames2setDesafiante = setDesafiante;
                }
                if (jogo.situacao_Id == 1 || jogo.situacao_Id == 2) // pendente ou marcado
                {
                    jogo.qtddGames1setDesafiado = 0;
                    jogo.qtddGames1setDesafiante = 0;
                    jogo.qtddGames2setDesafiado = 0;
                    jogo.qtddGames2setDesafiante = 0;
                    jogo.qtddGames3setDesafiado = 0;
                    jogo.qtddGames3setDesafiante = 0;
                }
                db.Entry(jogo).State = EntityState.Modified;
                db.SaveChanges();
                ViewBag.Ok = "ok";
            }
            else
            {
                ViewBag.MsgAlert = "Não foi possível alterar os dados.";
            }

            jogo = db.Jogo.Include(j => j.rodada).Include(j => j.desafiado).Include(j => j.desafiante).Where(j => j.Id == jogo.Id).Single();

            ViewBag.situacao_Id = new SelectList(db.SituacaoJogo, "Id", "descricao", jogo.situacao_Id);

            MontarProximoJogoTorneio(jogo);

            ProcessarJogoAtrasado(jogo);

            return RedirectToAction("Index3", "Home");

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,usuario,organizador")]
        public ActionResult ControlarJogo2(Jogo jogo)
        {
            int setDesafiante = 0;
            int setDesafiado = 0;
            if (ModelState.IsValid)
            {
                jogo.dataCadastroResultado = DateTime.Now;
                jogo.usuarioInformResultado = User.Identity.Name;
                if (jogo.desafiado_id == WebSecurity.GetUserId(User.Identity.Name))
                {
                    setDesafiado = 6;
                    setDesafiante = 1;
                }
                else
                {
                    setDesafiado = 1;
                    setDesafiante = 6;
                }
                jogo.idVencedor = 0;
                jogo.idPerderdor = 0;
                if (jogo.situacao_Id == 5)
                { //WO
                    jogo.qtddGames1setDesafiado = setDesafiado;
                    jogo.qtddGames1setDesafiante = setDesafiante;
                    jogo.qtddGames2setDesafiado = setDesafiado;
                    jogo.qtddGames2setDesafiante = setDesafiante;
                }
                if (jogo.situacao_Id == 1 || jogo.situacao_Id == 2) // pendente ou marcado
                {
                    jogo.qtddGames1setDesafiado = 0;
                    jogo.qtddGames1setDesafiante = 0;
                    jogo.qtddGames2setDesafiado = 0;
                    jogo.qtddGames2setDesafiante = 0;
                    jogo.qtddGames3setDesafiado = 0;
                    jogo.qtddGames3setDesafiante = 0;
                }
                db.Entry(jogo).State = EntityState.Modified;
                db.SaveChanges();
                ViewBag.Sucesso = true;
                ViewBag.MsgAlerta = "Atualização realizada com sucesso";
            }
            else
            {
                ViewBag.Sucesso = false;
                ViewBag.MsgAlerta = "Não foi possível alterar os dados.";
            }

            jogo = db.Jogo.Include(j => j.rodada).Include(j => j.desafiado).Include(j => j.desafiante).Where(j => j.Id == jogo.Id).Single();

            ViewBag.situacao_Id = new SelectList(db.SituacaoJogo, "Id", "descricao", jogo.situacao_Id);

            MontarProximoJogoTorneio(jogo);

            ProcessarJogoAtrasado(jogo);

            return RedirectToAction("Index3", "Home", new { ViewBag.Sucesso, ViewBag.MsgAlerta });

        }

        private void MontarProximoJogoTorneio2(Jogo jogo)
        {
            if (jogo.torneioId != null)
            {
                int ordemJogo = 0;
                if (jogo.ordemJogo % 2 != 0)
                {
                    ordemJogo = (int)(jogo.ordemJogo / 2) + 1;
                }
                else
                {
                    ordemJogo = (int)(jogo.ordemJogo / 2);
                }
                Jogo proximoJogo = null;
                if (jogo.ordemJogo == 101)
                { // 1 fase repescagem
                    var jogoRepescagem = db.Jogo.Where(r => r.torneioId == jogo.torneioId && r.classeTorneio == jogo.classeTorneio &&
                            r.faseTorneio == 100 && r.ordemJogo == ordemJogo).Single();
                    if (jogo.ordemJogo % 2 != 0)
                    {
                        jogoRepescagem.desafiado_id = jogo.idDoPerdedor;
                    }
                    else
                    {
                        jogoRepescagem.desafiante_id = jogo.idDoPerdedor;
                    }
                    proximoJogo = db.Jogo.Where(r => r.torneioId == jogo.torneioId && r.classeTorneio == jogo.classeTorneio &&
                            r.faseTorneio < 100 && r.ordemJogo == ordemJogo).OrderByDescending(r => r.faseTorneio).Single();
                }
                else if (jogo.ordemJogo == 100)
                {
                    proximoJogo = db.Jogo.Where(r => r.torneioId == jogo.torneioId && r.classeTorneio == jogo.classeTorneio &&
                                r.faseTorneio < 100 && r.ordemJogo == ordemJogo).OrderByDescending(r => r.faseTorneio).Single();
                    cadastrarColocacaoPerdedorTorneio(jogo);
                }
                else
                {
                    cadastrarColocacaoPerdedorTorneio(jogo);
                    if (jogo.faseTorneio == 1)
                    {
                        var inscricao = db.InscricaoTorneio.Where(i => i.userId == jogo.idDoVencedor && i.torneioId == jogo.torneioId).ToList();
                        if (inscricao.Count() > 0)
                        {
                            inscricao[0].colocacao = 0; // vencedor
                        }
                    }
                    else
                    {
                        proximoJogo = db.Jogo.Where(r => r.torneioId == jogo.torneioId && r.classeTorneio == jogo.classeTorneio &&
                                r.faseTorneio < jogo.faseTorneio - 1 && r.ordemJogo == ordemJogo).OrderByDescending(r => r.faseTorneio).Single();
                    }
                }
                if (jogo.faseTorneio != 1)
                {
                    if (jogo.ordemJogo % 2 != 0)
                    {
                        proximoJogo.desafiado_id = jogo.idDoVencedor;
                    }
                    else
                    {
                        proximoJogo.desafiante_id = jogo.idDoVencedor;
                    }
                }
                db.SaveChanges();
            }
        }


        private void MontarProximoJogoTorneio(Jogo jogo)
        {
            var ordemJogo = 0;
            if (jogo.torneioId != null)
            {
                if (jogo.ordemJogo % 2 != 0)
                {
                    ordemJogo = (int)(jogo.ordemJogo / 2) + 1;
                }
                else
                {
                    ordemJogo = (int)(jogo.ordemJogo / 2);
                }
                if (jogo.faseTorneio == 101) // 1ª fase da repescagem
                {
                    MontarProximoJogoRepescagem(jogo);
                }
                else if (jogo.faseTorneio == 100)
                {

                }
                else if (db.Jogo.Where(r => r.torneioId == jogo.torneioId && r.classeTorneio == jogo.classeTorneio &&
                   r.faseTorneio == jogo.faseTorneio - 1 && r.ordemJogo == ordemJogo).Count() > 0)
                {
                    var proximoJogo = db.Jogo.Where(r => r.torneioId == jogo.torneioId && r.classeTorneio == jogo.classeTorneio &&
                        r.faseTorneio == jogo.faseTorneio - 1 && r.ordemJogo == ordemJogo).Single();
                    if (jogo.ordemJogo % 2 != 0)
                    {
                        proximoJogo.desafiado_id = jogo.idDoVencedor;
                    }
                    else
                    {
                        proximoJogo.desafiante_id = jogo.idDoVencedor;
                    }
                    cadastrarColocacaoPerdedorTorneio(jogo);
                    db.SaveChanges();
                }
                else
                {
                    // indicar o vencedor do torneio
                    var inscricao = db.InscricaoTorneio.Where(i => i.userId == jogo.idDoVencedor && i.torneioId == jogo.torneioId).ToList();
                    if (inscricao.Count() > 0)
                    {
                        inscricao[0].colocacao = 0; // vencedor
                        db.SaveChanges();
                    }
                }
            }
        }

        private void MontarProximoJogoRepescagem(Jogo jogo)
        {
            var torneioId = jogo.torneioId;
            var classeId = jogo.classeTorneio;
            var userId = jogo.idDoVencedor;
            var jogoFase1 = db.Jogo.Where(j => j.torneioId == torneioId && j.classeTorneio == classeId && j.faseTorneio < 100).OrderByDescending(j => j.faseTorneio).First<Jogo>();
            var primeiraFase = (int)jogoFase1.faseTorneio;
            var jogosRodada1 = db.Jogo.Where(j => j.torneioId == torneioId && j.classeTorneio == classeId && j.faseTorneio == primeiraFase).ToList();
            var inscricao = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.classe == classeId && i.isAtivo && i.userId == userId).Single();
            var ehCabecaChave = false;
            if (inscricao.cabecaChave < 100)
            {
                var cabecaC = inscricao.cabecaChave;
                var chave = jogosRodada1.Count();
                var cabecaChave = db.JogoCabecaChave.Where(j => j.cabecaChave == cabecaC && j.chaveamento == chave).ToList();
                if (cabecaChave.Count() > 0)
                {
                    var ordemJogo = cabecaChave[0].ordemJogo;
                    var proximoJogo = jogosRodada1.Where(j => j.ordemJogo == ordemJogo).Single();
                    proximoJogo.desafiado_id = userId;
                    db.Entry(proximoJogo).State = EntityState.Modified;
                    db.SaveChanges();
                    ehCabecaChave = true;
                }
            }
            if (ehCabecaChave == false)
            {

            }
        }

        private void cadastrarColocacaoPerdedorTorneio(Jogo jogo)
        {
            // cadastrar a colocação do perdedor no torneio 
            var qtddFases = db.Jogo.Where(r => r.torneioId == jogo.torneioId && r.classeTorneio == jogo.classeTorneio && r.faseTorneio < 100).Max(r => r.faseTorneio);
            var colocacao = qtddFases - (jogo.faseTorneio - 1);
            if (jogo.faseTorneio == 100)
            { // fase 1
                colocacao = 101;
            }
            else if (jogo.faseTorneio == 101)
            { // repescagem
                colocacao = 100;
            }
            else if (colocacao > 4)
            {
                colocacao = jogo.faseTorneio + 500;
            }
            var inscricao = db.InscricaoTorneio.Where(i => i.userId == jogo.idDoPerdedor && i.torneioId == jogo.torneioId).ToList();
            if (inscricao.Count() > 0)
            {
                inscricao[0].colocacao = colocacao;
                db.SaveChanges();
            }
        }

        private void ProcessarJogoAtrasado(Jogo jogo)
        {
            string msg = "";
            //situação: 4: finalizado -- 5: wo
            //List<Jogo> jogos = db.Jogo.Where(r => r.rodada_id == id && r.dataCadastroResultado > r.rodada.dataFim && (r.situacao_Id == 4 || r.situacao_Id == 5)).ToList();
            if (jogo.torneioId == null && jogo.dataCadastroResultado > jogo.rodada.dataFim && (jogo.situacao_Id == 4 || jogo.situacao_Id == 5))
            {
                var pontosDesafiante = 0.0;
                var pontosDesafiado = 0.0;
                try
                {
                    using (TransactionScope scope = new TransactionScope())
                    {
                        pontosDesafiante = rn.calcularPontosDesafiante(jogo);
                        pontosDesafiado = rn.calcularPontosDesafiado(jogo);

                        rn.gravarPontuacaoNaRodada(jogo.rodada_id, jogo.desafiante, pontosDesafiante, true);
                        rn.gravarPontuacaoNaRodada(jogo.rodada_id, jogo.desafiado, pontosDesafiado, true);
                        jogo.dataCadastroResultado = jogo.rodada.dataFim;
                        if (jogo.desafiante.situacao.Equals("suspenso"))
                        {
                            UserProfile desafiante = db.UserProfiles.Find(jogo.desafiante_id);
                            desafiante.situacao = "ativo";
                        }
                        if (jogo.desafiado.situacao.Equals("suspenso"))
                        {
                            UserProfile desafiado = db.UserProfiles.Find(jogo.desafiado_id);
                            desafiado.situacao = "ativo";
                        }
                        db.SaveChanges();
                        scope.Complete();
                        msg = "ok";
                    }
                }
                catch (Exception ex)
                {
                    msg = ex.Message;
                }
            }
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

            MontarProximoJogoTorneio(jogoAtual);
            ProcessarJogoAtrasado(jogoAtual);

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

            MontarProximoJogoTorneio(jogoAtual);
            ProcessarJogoAtrasado(jogoAtual);

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