using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Barragem.Models;
using Barragem.Context;
using Barragem.Filters;
using System.Transactions;
using Barragem.Class;
using System.Web.Security;
using WebMatrix.WebData;
using System.Diagnostics;

namespace Barragem.Controllers
{
    [Authorize]
    [InitializeSimpleMembership]
    public class RodadaController : Controller
    {
        private bool isClasseUnica;
        private BarragemDbContext db = new BarragemDbContext();
        private RodadaNegocio rn = new RodadaNegocio();
        string erroSorteio = "";
        //
        // GET: /Rodada/

        [Authorize(Roles = "admin,usuario,organizador")]
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
            List<Rodada> rodada = null;
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            int barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
            if (perfil.Equals("admin"))
            {
                rodada = db.Rodada.Where(r => r.isRodadaCarga == false).OrderByDescending(c => c.Id).ToList();
            }
            else
            {
                rodada = db.Rodada.Where(r => r.isRodadaCarga == false && r.barragemId == barragemId).OrderByDescending(c => c.Id).ToList();
            }
            var barragem = db.BarragemView.Find(barragemId);
            ViewBag.isBarragemAtiva = barragem.isAtiva;
            return View(rodada);
        }

        //
        // GET: /Rodada/Create

        [Authorize(Roles = "admin, organizador")]
        public ActionResult Create(int barragemId = 0)
        {
            try
            {
                if (barragemId == 0)
                {
                    var userId = WebSecurity.GetUserId(User.Identity.Name);
                    barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
                }
                ViewBag.barraId = barragemId;
                ViewBag.barragemId = new SelectList(db.BarragemView, "Id", "nome");

                ViewBag.temporadaId = new SelectList(db.Temporada.Where(c => c.barragemId == barragemId).OrderByDescending(c => c.Id), "Id", "nome");

            }
            catch (InvalidOperationException)
            {
                ViewBag.sequencial = 1;
                ViewBag.codigo = "A";
            }
            return View();
        }

        //
        // POST: /Rodada/Create

        [Authorize(Roles = "admin, organizador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Rodada rodada)
        {
            string mensagem = "ok";
            if (ModelState.IsValid)
            {
                try
                {
                    new RodadaNegocio().Create(rodada);
                }
                catch (Exception e)
                {
                    mensagem = e.Message;
                }
                return RedirectToAction("Index", new { msg = mensagem });
            }

            return View(rodada);
        }



        [Authorize(Roles = "admin, organizador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CriaESorteia(Rodada rodada, String notificarApp = "off")
        {
            string mensagem = "";
            if (ModelState.IsValid)
            {
                var rodadaNegocio = new RodadaNegocio();
                try
                {
                    rodadaNegocio.Create(rodada);
                    mensagem = "ok";
                    rodadaNegocio.SortearJogos(rodada.Id, rodada.barragemId, notificarApp == "on" ? true : false);
                }
                catch (Exception e)
                {
                    if (e.InnerException != null) { mensagem = e.Message + ": " + e.InnerException.Message; } else { mensagem = e.Message; }
                }
                return RedirectToAction("Index", new { msg = mensagem });

            }
            return RedirectToAction("Create");

        }

        private void setClasseUnica(int barragemId)
        {
            var barragem = db.BarragemView.Find(barragemId);
            this.isClasseUnica = barragem.isClasseUnica;
        }

        private bool getClasseUnica()
        {
            return isClasseUnica;
        }

        [Authorize(Roles = "admin, organizador")]
        public ActionResult SortearJogos(int id, int barragemId)
        {
            string mensagem = "ok";
            try
            {
                new RodadaNegocio().SortearJogos(id, barragemId, false);
            }
            catch (Exception e)
            {
                mensagem = e.Message;
            }
            return RedirectToAction("Index", new { msg = mensagem });
        }

        [Authorize(Roles = "admin, organizador")]
        public ActionResult notificarViaApp(int barragemId)
        {
            new RodadaNegocio().NotificacaoApp(barragemId);

            return RedirectToAction("Index", new { msg = "ok" });
        }

        [Authorize(Roles = "admin, organizador")]
        public ActionResult notificarGeracaoRodada(int Id)
        {
            Rodada rodada = db.Rodada.Find(Id);
            try
            {
                Mail mail = new Mail();
                mail.de = System.Configuration.ConfigurationManager.AppSettings.Get("UsuarioMail");
                mail.para = "barragemdocerrado@gmail.com";
                mail.assunto = "RDT - Nova Rodada - " + rodada.codigoSeq;
                mail.conteudo = "Olá Pessoal,<br><br>A nova rodada " + rodada.codigoSeq + " já está disponível no site e vai até o dia:" + rodada.dataFim + ".<br><br>Bons jogos a todos.";
                mail.formato = Tipos.FormatoEmail.Html;
                List<UserProfile> users = db.UserProfiles.Where(u => u.situacao == "ativo" && u.barragemId == rodada.barragemId).ToList();
                List<string> bcc = new List<string>();
                foreach (UserProfile user in users)
                {
                    bcc.Add(user.email);
                }
                mail.bcc = bcc;
                mail.EnviarMail();
            }
            catch (Exception ex) { }
            return RedirectToAction("Index", new { msg = "ok" });
        }

        //
        // GET: /Rodada/Edit/5
        [Authorize(Roles = "admin, organizador")]
        public ActionResult Edit(int id = 0)
        {
            Rodada rodada = db.Rodada.Find(id);
            if (rodada == null)
            {
                return HttpNotFound();
            }
            return View(rodada);
        }

        [Authorize(Roles = "admin, organizador")]
        public ActionResult ExcluirRodada(int id)
        {
            db.Database.ExecuteSqlCommand("delete from rancking where rodada_id=" + id);
            db.Database.ExecuteSqlCommand("delete from jogo where rodada_id=" + id);
            db.Database.ExecuteSqlCommand("delete from rodada where id=" + id);
            return RedirectToAction("Index", new { msg = "ok" });
        }

        //
        // POST: /Rodada/Edit/5
        [Authorize(Roles = "admin, organizador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Rodada rodada)
        {
            if (ModelState.IsValid)
            {
                rodada.dataFim = new DateTime(rodada.dataFim.Year, rodada.dataFim.Month, rodada.dataFim.Day, 23, 59, 59);
                db.Entry(rodada).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(rodada);
        }

        // este método não está mais sendo utilizado, mas pode vir a ser útil no futuro
        private UserProfile SorteiaJogadorTorneio(List<Rancking> ranckiados)
        {
            UserProfile ranckiado;
            Random r = new Random();
            int randomIndex = 1;
            if (ranckiados.Count() == 1)
            {
                UserProfile curinga = db.UserProfiles.Where(u => u.UserName.Equals("coringa")).Single();
                ranckiados.RemoveAt(0);
                return curinga;
                // caso só reste duas opções não há mais como aplicar as regras
            }
            else if (ranckiados.Count == 2)
            {
                ranckiado = ranckiados[1].userProfile;
                ranckiados.RemoveAt(1);
                ranckiados.RemoveAt(0);
                return ranckiado;
            }

            randomIndex = r.Next(1, ranckiados.Count); //Choose a random object in the list
            ranckiado = ranckiados[randomIndex].userProfile; //add it 
            ranckiados.RemoveAt(randomIndex); //remove to avoid duplicates
            ranckiados.RemoveAt(0);
            return ranckiado;

        }

        [Authorize(Roles = "admin, organizador")]
        public ActionResult ProcessarJogosAtrasados(int id)
        {
            string msg = "";
            //situação: 4: finalizado -- 5: wo
            List<Jogo> jogos = db.Jogo.Where(r => r.rodada_id == id && r.dataCadastroResultado > r.rodada.dataFim && (r.situacao_Id == 4 || r.situacao_Id == 5)).ToList();
            var pontosDesafiante = 0.0;
            var pontosDesafiado = 0.0;
            try
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    foreach (Jogo item in jogos)
                    {
                        pontosDesafiante = rn.calcularPontosDesafiante(item);
                        pontosDesafiado = rn.calcularPontosDesafiado(item);

                        rn.gravarPontuacaoNaRodada(id, item.desafiante, pontosDesafiante, true);
                        rn.gravarPontuacaoNaRodada(id, item.desafiado, pontosDesafiado, true);
                        item.dataCadastroResultado = item.rodada.dataFim;
                        if (item.desafiante.situacao.Equals("suspenso"))
                        {
                            UserProfile desafiante = db.UserProfiles.Find(item.desafiante_id);
                            desafiante.situacao = "ativo";
                        }
                        if (item.desafiado.situacao.Equals("suspenso"))
                        {
                            UserProfile desafiado = db.UserProfiles.Find(item.desafiado_id);
                            desafiado.situacao = "ativo";
                        }
                        db.SaveChanges();
                    }
                    scope.Complete();
                    msg = "ok";
                }
            }
            catch (Exception ex)
            {
                msg = ex.Message;
            }
            return RedirectToAction("Index", new { msg = msg });
        }

        [Authorize(Roles = "admin, organizador")]
        public ActionResult FecharRodada(int id)
        {
            string msg = "";
            string detalheErro = "";
            db.Database.ExecuteSqlCommand("Delete from Rancking where rodada_id=" + id + " and posicaoClasse is not null");
            List<Jogo> jogos = db.Jogo.Where(r => r.rodada_id == id).ToList();
            var pontosDesafiante = 0.0;
            var pontosDesafiado = 0.0;
            Rodada rodada = db.Rodada.Find(id);
            BarragemView barragem = db.BarragemView.Find(rodada.barragemId);
            var log2 = new Log();
            log2.descricao = "Fecha data:" + DateTime.Now + " " + barragem.nome + " Fecha barragem";
            db.Log.Add(log2);
            db.SaveChanges();
            try
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    foreach (Jogo item in jogos)
                    {
                        pontosDesafiante = rn.calcularPontosDesafiante(item);
                        pontosDesafiado = rn.calcularPontosDesafiado(item);
                        msg = "pontosDesafio" + item.desafiado_id;
                        if (!item.desafiante.UserName.Equals("coringa"))
                        {
                            rn.gravarPontuacaoNaRodada(id, item.desafiante, pontosDesafiante);
                            msg = "gravarPontuacaoNaRodadaDesafiante" + item.desafiante_id;
                        }
                        rn.gravarPontuacaoNaRodada(id, item.desafiado, pontosDesafiado);
                        msg = "gravarPontuacaoNaRodadaDesafiado" + item.desafiado_id;
                        if (barragem.suspensaoPorAtraso)
                        {
                            verificarRegraSuspensaoPorAtraso(item);
                        }
                        if (barragem.suspensaoPorWO)
                        {
                            verificarRegraSuspensaoPorWO(item);
                        }
                        msg = "verificarRegraSuspensao" + item.desafiado_id;
                    }

                    rn.gerarPontuacaoDosJogadoresForaDaRodada(id, rodada.barragemId);
                    msg = "gerarPontuacaoDosJogadoresForaDaRodada";

                    gerarRancking(id);
                    msg = "gerarRanking";
                    List<Classe> classes = db.Classe.Where(c => c.barragemId == rodada.barragemId).ToList();
                    for (int i = 0; i < classes.Count(); i++)
                    {
                        gerarRanckingPorClasse(id, classes[i].Id);
                    }
                    msg = "gerarRankingPorClasse";
                    rodada.isAberta = false;
                    rodada.dataFim = DateTime.Now;
                    db.Entry(rodada).State = EntityState.Modified;
                    db.SaveChanges();
                    scope.Complete();
                    msg = "ok";
                }
            }
            catch (Exception ex)
            {
                msg = msg + ": " + ex.Message;
                if (ex.InnerException == null) { ViewBag.DetalheErro = ex.StackTrace; } else { ViewBag.DetalheErro = ex.InnerException.StackTrace; }
                ViewBag.MsgErro = msg;
                return View();
            }
            return RedirectToAction("Index", new { msg = msg, detalheErro = detalheErro });
        }

        [HttpGet]
        public ActionResult ValidarExclusaoRodada(int id)
        {
            try
            {
                var torneio = db.Rodada.Find(id);

                var possuiJogosJaIniciados = db.Jogo.Any(x => x.rodada_id == id && x.desafiante_id != 8 && x.situacao_Id != 1);
                if (possuiJogosJaIniciados)
                    return Json(new { erro = "", retorno = "MSG" }, "text/plain", JsonRequestBehavior.AllowGet);
                else
                    return Json(new { erro = "", retorno = "OK" }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = "ERRO" }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult ValidarFechamentoRodada(int id)
        {
            try
            {
                var torneio = db.Rodada.Find(id);

                var possuiJogosJaIniciados = db.Jogo.Any(x => x.rodada_id == id && x.desafiante_id != 8 && x.situacao_Id != 1);
                if (!possuiJogosJaIniciados)
                    return Json(new { erro = "", retorno = "MSG" }, "text/plain", JsonRequestBehavior.AllowGet);
                else
                    return Json(new { erro = "", retorno = "OK" }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = "ERRO" }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult ValidarRegrasGeracaoRodada(int idTemporada)
        {
            try
            {
                var temporada = db.Temporada.Find(idTemporada);
                var qtdeRodadasExistente = db.Rodada.Count(x => x.temporadaId == idTemporada);

                var usuariosPendentes = db.UserProfiles.Where(x => x.situacao == "pendente" && x.barragemId == temporada.barragemId);

                if (temporada.qtddRodadas < qtdeRodadasExistente)
                {
                    return Json(new { erro = "", retorno = "REGRA_EXCEDEU_QTDE_RODADAS" }, "text/plain", JsonRequestBehavior.AllowGet);

                }

                if (usuariosPendentes.Count() - 1 > 0)
                {
                    return Json(new { erro = "", retorno = "REGRA_USUARIOS_PENDENTES", qtdeJogadores = usuariosPendentes.Count() - 1 }, "text/plain", JsonRequestBehavior.AllowGet);
                }

                return Json(new { erro = "", retorno = "OK" }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = "ERRO" }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        private void verificarRegraSuspensaoPorAtraso(Jogo jogo)
        {
            // 5.6.O jogador que não receber pontuação ou deixar de jogar, mesmo que justificadamente, por 2 (dois) jogos seguidos
            //será retirado das rodadas e colocado em suspensão automática até que regularize seus jogos de forma que ele saia desta condição.
            if (jogo.gamesJogados == 0)
            {
                // Caso no fechamento da rodada o jogo não tenha sido realizado, verificar a situação da rodada anterior
                verificarSeJogoRealizadoNaRodadaAnterior(jogo.desafiado_id, jogo.rodada_id, jogo.rodada.barragemId);
                verificarSeJogoRealizadoNaRodadaAnterior(jogo.desafiante_id, jogo.rodada_id, jogo.rodada.barragemId);
            }
        }

        private void verificarRegraSuspensaoPorWO(Jogo jogo)
        {
            // Jogador que perder por wo 2 vezes seguidas ficará em suspensão por WO até que o adminstrador retire ele dessa situação.
            if (jogo.situacao_Id == 5)
            {
                var userDerrotado = jogo.idDoPerdedor;
                verificarSeHouveWONaRodadaAnterior(userDerrotado, jogo.rodada_id, jogo.rodada.barragemId);

            }
        }

        private void verificarSeJogoRealizadoNaRodadaAnterior(int idJogador, int rodada_id, int barragemId)
        {
            int idRodadaAnterior = db.Rodada.Where(r => r.isAberta == false && r.Id < rodada_id && r.barragemId == barragemId).Max(r => r.Id);
            List<Jogo> jogoAnterior = db.Jogo.Where(j => j.rodada_id == idRodadaAnterior && (j.desafiado_id == idJogador || j.desafiante_id == idJogador))
                .ToList();
            if (jogoAnterior.Count > 0)
            {
                if (jogoAnterior[0].gamesJogados == 0)
                {
                    UserProfile jogador = db.UserProfiles.Find(idJogador);
                    jogador.situacao = Tipos.Situacao.suspenso.ToString();
                    db.SaveChanges();
                }
            }

        }

        private void verificarSeHouveWONaRodadaAnterior(int idJogador, int rodada_id, int barragemId)
        {
            if (idJogador == 8) // se for coringa não suspende-lo;
            {
                return;
            }
            int idRodadaAnterior = db.Rodada.Where(r => r.isAberta == false && r.Id < rodada_id && r.barragemId == barragemId).Max(r => r.Id);
            List<Jogo> jogoAnterior = db.Jogo.Where(j => j.rodada_id == idRodadaAnterior && (j.desafiado_id == idJogador || j.desafiante_id == idJogador))
                .ToList();
            if (jogoAnterior.Count > 0)
            {
                if ((jogoAnterior[0].situacao_Id == 5) && (idJogador == jogoAnterior[0].idDoPerdedor))
                {
                    UserProfile jogador = db.UserProfiles.Find(idJogador);
                    jogador.situacao = Tipos.Situacao.suspensoWO.ToString();
                    db.SaveChanges();
                }
            }

        }

        private void gerarRancking(int idRodada)
        {
            int posicao = 1;
            List<Rancking> listaRancking = db.Rancking.Where(r => r.rodada_id == idRodada && r.userProfile.situacao != "desativado" && r.userProfile.situacao != "inativo").OrderByDescending(r => r.totalAcumulado).ToList();
            foreach (Rancking ran in listaRancking)
            {
                ran.posicao = posicao;
                db.Entry(ran).State = EntityState.Modified;
                db.SaveChanges();
                posicao++;
            }
        }

        private void gerarRanckingPorClasse(int idRodada, int classeId)
        {
            int posicao = 1;
            List<Rancking> listaRancking = db.Rancking.Where(r => r.rodada_id == idRodada && r.userProfile.classeId == classeId && r.userProfile.situacao != "desativado" && r.userProfile.situacao != "inativo").OrderByDescending(r => r.totalAcumulado).ToList();
            foreach (Rancking ran in listaRancking)
            {
                ran.posicaoClasse = posicao;
                db.Entry(ran).State = EntityState.Modified;
                db.SaveChanges();
                posicao++;
            }
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}