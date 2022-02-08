﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Barragem.Models;
using Barragem.Context;
using Barragem.Filters;
using Barragem.Class;
using System.Web.Security;
using WebMatrix.WebData;
using Uol.PagSeguro.Constants;
using Uol.PagSeguro.Domain;
using Uol.PagSeguro.Exception;
using System.Transactions;

namespace Barragem.Controllers
{
    [InitializeSimpleMembership]
    public class TorneioController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();
        private TorneioNegocio tn = new TorneioNegocio();
        //

        [HttpPost]
        [Authorize(Roles = "admin,usuario,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult AlterarClassesTorneio(IEnumerable<InscricaoTorneio> inscricaoTorneio)
        {
            try
            {
                InscricaoTorneio jogador = null;
                foreach (InscricaoTorneio user in inscricaoTorneio)
                {
                    jogador = db.InscricaoTorneio.Find(user.Id);
                    jogador.classe = user.classe;
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

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult ExcluirInscricao(int Id)
        {
            var torneioId = 0;
            try
            {
                InscricaoTorneio inscricao = db.InscricaoTorneio.Find(Id);
                var userId = inscricao.userId;
                torneioId = inscricao.torneioId;
                db.InscricaoTorneio.Remove(inscricao);
                db.SaveChanges();
                var inscricoes = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.userId == userId).ToList();
                if (inscricoes.Count() > 0)
                {
                    var torneio = db.Torneio.Find(torneioId);
                    var isSocio = (inscricoes[0].isSocio == null) ? false : (bool)inscricoes[0].isSocio;
                    var isFederado = (inscricoes[0].isFederado == null) ? false : (bool)inscricoes[0].isFederado;
                    var valorInscricao = calcularValorInscricao(0, 0, 0, isSocio, torneio, userId, isFederado, inscricoes.Count());
                    foreach (var item in inscricoes)
                    {
                        item.valor = valorInscricao;
                        db.Entry(item).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
                return RedirectToAction("EditInscritos", new { torneioId = torneioId, Msg = "OK" });
            }
            catch (Exception ex)
            {
                return RedirectToAction("EditInscritos", new { torneioId = torneioId, Msg = ex.Message });
            }
        }

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult ExcluirClasse(int Id)
        {
            try
            {
                var inscritos = db.InscricaoTorneio.Where(i => i.classe == Id && i.isAtivo).Count();
                if (inscritos > 0)
                {
                    return Json(new { erro = "Já existem jogadores inscritos nesta classe. Para excluí-la é necessário inativar as inscrições.", retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
                }
                ClasseTorneio classe = db.ClasseTorneio.Find(Id);
                db.ClasseTorneio.Remove(classe);
                db.SaveChanges();
                return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult ExcluirPatrocinador(int Id)
        {
            try
            {
                var p = db.Patrocinador.Find(Id);
                db.Patrocinador.Remove(p);
                db.SaveChanges();
                return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }


        [Authorize(Roles = "admin,usuario,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult AlterarClasse(int torneioId)
        {
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            var inscricoes = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.participante.UserId == userId).ToList();
            var classes = db.ClasseTorneio.Where(i => i.torneioId == torneioId).OrderBy(c => c.nivel).ToList();
            ViewBag.ClasseTorneio = classes;
            if (inscricoes.Count() > 1)
            {
                ViewBag.Classe2Opcao = inscricoes[1].classe;
                var classes2 = db.ClasseTorneio.Where(i => i.torneioId == torneioId).OrderBy(c => c.nivel).ToList();
                ViewBag.ClasseTorneio2 = classes2;
            }
            return View(inscricoes[0]);
        }

        [HttpPost]
        [Authorize(Roles = "admin,usuario,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult AlterarClasse(int inscricaoId, int classe, int classe2Opcao = 0)
        {
            var inscricao = db.InscricaoTorneio.Find(inscricaoId);
            if (classe2Opcao == 0)
            {
                inscricao.classe = classe;
                db.Entry(inscricao).State = EntityState.Modified;
            }
            else
            {
                var userId = WebSecurity.GetUserId(User.Identity.Name);
                var inscricoes = db.InscricaoTorneio.Where(i => i.torneioId == inscricao.torneioId && i.participante.UserId == userId).ToList();
                inscricoes[0].classe = classe;
                inscricoes[1].classe = classe2Opcao;
                db.Entry(inscricoes[0]).State = EntityState.Modified;
                db.Entry(inscricoes[1]).State = EntityState.Modified;
            }
            db.SaveChanges();
            return RedirectToAction("Detalhes", new { id = inscricao.torneioId, Msg = "OK" });
        }


        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult Index()
        {
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            List<Torneio> torneio = null;
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            int barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
            if (perfil.Equals("admin"))
            {
                torneio = db.Torneio.OrderByDescending(c => c.Id).ToList();
            }
            else if (perfil.Equals("parceiroBT"))
            {
                torneio = db.Torneio.Where(r => r.barragem.isBeachTenis == true && !r.nome.ToUpper().Contains("TESTE")).OrderByDescending(c => c.Id).ToList();
            }
            else
            {
                torneio = db.Torneio.Where(r => r.barragemId == barragemId).OrderByDescending(c => c.Id).ToList();
            }
            var barragem = db.BarragemView.Find(barragemId);
            ViewBag.isBarragemAtiva = barragem.isAtiva;
            return View(torneio);
        }

        [Authorize(Roles = "admin,usuario,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult TorneiosDisponiveis()
        {
            List<Torneio> torneio = null;
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            var barragem = (from up in db.UserProfiles where up.UserId == userId select up.barragem).Single();
            var agora = DateTime.Now;
            torneio = db.Torneio.Where(r => r.isAtivo && r.dataFim > agora && (r.barragemId == barragem.Id || r.isOpen || (r.divulgaCidade && r.barragem.cidade == barragem.cidade))).OrderByDescending(c => c.Id).ToList();


            return View(torneio);
        }

        //[Authorize(Roles = "admin,usuario,organizador")]
        public ActionResult EscolherDupla(int id, int classe = 0, int userId = 0)
        {
            var torneioId = id;
            string perfil = "";
            try
            {
                perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            }
            catch (Exception e) { }
            if (perfil.Equals("usuario") || userId == 0)
            {
                userId = WebSecurity.GetUserId(User.Identity.Name);
            }
            var inscricao = db.InscricaoTorneio.Where(i => i.userId == userId && i.torneioId == torneioId && i.classeTorneio.isDupla).OrderBy(i => i.classe).ToList();
            IQueryable<InscricaoTorneio> inscricoesRealizadas = null;
            List<InscricaoTorneio> selectInscricoesDisp = null;
            List<InscricaoTorneio> inscricoesDuplas = null;
            ViewBag.isDisponivel = false;
            ViewBag.torneioId = torneioId;
            ViewBag.userId = userId;
            if (inscricao.Count() > 0)
            {
                if (classe == 0)
                {
                    classe = inscricao[0].classe;
                    ViewBag.classeNome = inscricao[0].classeTorneio.nome;
                    if (inscricao.Count() > 1)
                    {
                        ViewBag.proximaClasse = inscricao[1].classe;
                    }
                    else
                    {
                        ViewBag.proximaClasse = 0;
                    }
                }
                else
                {
                    ViewBag.classeNome = inscricao.Where(i => i.classe == classe).Single().classeTorneio.nome;
                    var inscProximaClasse = inscricao.Where(i => i.classe > classe).OrderBy(i => i.classe).ToList();
                    ViewBag.proximaClasse = 0;
                    if (inscProximaClasse.Count() > 0)
                    {
                        ViewBag.proximaClasse = inscProximaClasse[0].classe;
                    }
                }

                inscricoesRealizadas = db.InscricaoTorneio.Where(c => c.torneioId == torneioId && c.classe == classe && c.parceiroDuplaId > 0).OrderBy(c => c.participante.nome);
                var inscricoesDisponiveis = db.InscricaoTorneio.Where(c => c.torneioId == torneioId && c.classe == classe && (c.parceiroDuplaId == null || c.parceiroDuplaId == 0));
                var inscricoesIndisp = from insD in inscricoesDisponiveis join insR in inscricoesRealizadas on insD.userId equals insR.parceiroDuplaId select insD;
                var InscricoesDisp = (from i in inscricoesDisponiveis where !inscricoesIndisp.Contains(i) select i);
                var isDisponivel = InscricoesDisp.Where(i => i.userId == userId).Count();
                if (isDisponivel > 0) { ViewBag.isDisponivel = true; }
                selectInscricoesDisp = InscricoesDisp.Where(i => i.userId != userId).OrderBy(i => i.participante.nome).ToList();
                inscricoesDuplas = selectInscricoesDisp;
                inscricoesDuplas.AddRange(inscricoesRealizadas.ToList());
            }

            return View(inscricoesDuplas);
        }


        private bool verificarSeAFaseDeGrupoFoiFinalizada(ClasseTorneio classe)
        {
            if (classe.faseGrupo)
            {
                var jogosFaseGrupo = db.Jogo.Where(i => i.classeTorneio == classe.Id && i.grupoFaseGrupo != null).ToList();
                // existe fase de grupo mas os jogos ainda não foram nem gerados
                if (jogosFaseGrupo.Count() == 0)
                {
                    return false;
                }
                else
                {
                    var jogosFaseGrupoPendentes = jogosFaseGrupo.Where(j => j.situacao_Id == 1 || j.situacao_Id == 2).Count(); // 1-pendente 2-marcado
                    // os jogos foram gerados e não existe mais nenhum jogo pendente:
                    if (jogosFaseGrupoPendentes == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else
            {
                return true;
            }
        }

        private void excluirJogosPorClasse(ClasseTorneio classe, bool faseGrupoFinalizada)
        {
            if (!classe.faseGrupo)
            {
                db.Database.ExecuteSqlCommand("delete from jogo where classeTorneio=" + classe.Id);
            }
            else
            {
                if (!faseGrupoFinalizada)
                {
                    db.Database.ExecuteSqlCommand("delete from jogo where classeTorneio=" + classe.Id);
                }
            }
        }

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        [HttpPost]
        public ActionResult MontarChaveamento(int torneioId, IEnumerable<int> classeIds)
        {
            //try { 
                string Msg = "";
                var torneio = db.Torneio.Find(torneioId);
                CobrancaTorneio cobrancaTorneio = new CobrancaTorneio();
                bool pendenciaDePagamento = false;
                if (temPendenciaDePagamentoTorneio(torneio))
                {
                    cobrancaTorneio = getDadosDeCobrancaTorneio(torneioId);
                    if (cobrancaTorneio.valorASerPago > 0)
                    {
                        pendenciaDePagamento = true;
                    }
                }
                if (!pendenciaDePagamento)
                {
                    foreach (int classeId in classeIds)
                    {
                        var classe = db.ClasseTorneio.Find(classeId);
                        var faseGrupoFinalizada = verificarSeAFaseDeGrupoFoiFinalizada(classe);
                        excluirJogosPorClasse(classe, faseGrupoFinalizada);
                        if (torneio.barragem.isModeloTodosContraTodos)
                        {
                            tn.montarJogosTodosContraTodos(classe);
                        }
                        else if ((classe.faseGrupo) && (!faseGrupoFinalizada))
                        {
                            int qtddGrupo = tn.MontarGruposFaseGrupo(classe);
                            var qtddParticipantes = qtddGrupo * 2;
                            tn.MontarJogosFaseGrupo(classe);
                            if (classe.faseMataMata)
                            {
                                montarFaseEliminatoria(torneio.Id, qtddParticipantes, classe.Id, false);
                                var jogosPrimeiraRodada = tn.getJogosPrimeiraRodada(classe.Id);
                                tn.montarJogosComByes(jogosPrimeiraRodada, getQtddByes(qtddParticipantes), false, qtddParticipantes, true);
                            }
                        }
                        else if ((classe.faseGrupo) && (faseGrupoFinalizada) && (classe.faseMataMata))
                        {
                            tn.pontuarEliminadosFaseGrupo(classe);
                            tn.montarJogosRegraCBT(classe);
                            tn.fecharJogosComBye(tn.getJogosPrimeiraRodada(classe.Id));
                        }
                        else if (!classe.faseGrupo)
                        {
                            var inscricoes = tn.getInscritosPorClasse(classe);
                            var qtddJogadores = inscricoes.Count();
                            if (qtddJogadores < 2) continue;
                            montarFaseEliminatoria(torneio.Id, qtddJogadores, classe.Id, torneio.temRepescagem);
                            var qtddByes = getQtddByes(qtddJogadores);
                            if (torneio.temRepescagem)
                            {
                                var onzejogadores = qtddJogadores;
                                qtddJogadores = qtddJogadores + (qtddJogadores / 2);
                                qtddByes = getQtddByes(qtddJogadores);
                                if ((qtddJogadores == 16) && (onzejogadores != 11))
                                {
                                    qtddJogadores++;
                                    qtddByes++;
                                }
                            }
                            popularJogosIniciais(torneio.Id, classe.Id, torneio.temRepescagem, inscricoes, qtddByes);
                        }
                    }
                    ViewBag.classesFaseGrupoNaoFinalizadas = db.Jogo.Where(i => i.torneioId == torneioId && i.grupoFaseGrupo != null && (i.situacao_Id == 1 || i.situacao_Id == 2)).
                        Select(i => (int)i.classeTorneio).Distinct().ToList();
                }
            return RedirectToAction("EditJogos", new { torneioId = torneioId, fClasse = 0, fData = "", fNomeJogador = "", fGrupo = "0", fase = 0, qtddInscritos = cobrancaTorneio.qtddInscritos, valorASerPago = cobrancaTorneio.valorASerPago, valorDescontoParaRanking = cobrancaTorneio.valorDescontoParaRanking });
            //return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            //}
            //catch (Exception ex)
            //{
            //   return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            // }

        }

        private bool temPendenciaDePagamentoTorneio(Torneio torneio)
        {
            if (torneio.torneioFoiPago)
            {
                return false;
            }
            else if (torneio.barragem.isTeste)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        //private List<InscricaoTorneio> getClassificadosFaseGrupo(ClasseTorneio classe)
        //{
        //    //var qtddClassificadosPorgrupo = classe.qtddPassamFase;
        //    var qtddGrupos = db.InscricaoTorneio.Where(i => i.classe == classe.Id && i.isAtivo).Max(i => i.grupo);
        //    var totalClassificados = new List<InscricaoTorneio>();
        //    for (int i = 1; i <= qtddGrupos; i++)
        //    {
        //        var classificacao = tn.ordenarClassificacaoFaseGrupo(classe, i);

        //        var classificados = classificacao.Take(2).ToList();
        //        foreach (var item in classificados)
        //        {
        //            totalClassificados.Add(item.inscricao);
        //        }
        //    }
        //    return totalClassificados;
        //}

        private void popularJogosFase1(List<Jogo> jogosRodada1, List<InscricaoTorneio> inscritos, int qtddByes, bool temRepescagem)
        {
            tn.montarJogosComByes(jogosRodada1, qtddByes, temRepescagem, inscritos.Count); // montar os byes utilizando a tabela de cabecaChave (colocar os byes no desafiante);
            var cabecasDeChave = inscritos.Where(i => i.cabecaChave != null && i.cabecaChave != 100).ToList();
            tn.montarJogosComCabecaDeChave(jogosRodada1, cabecasDeChave, temRepescagem); //Montar os cabecaChave utilizando a tabela de cabecaChave (colocar os cabecaChave no desafiado);
            var chaveamento = jogosRodada1.Count();
            var qtddCabecaChavesValidos = db.JogoCabecaChave.Where(j => j.chaveamento == chaveamento && j.temRepescagem == temRepescagem && j.isFaseGrupo == false).Count();
            var inscritosAleatorios = inscritos.Where(i => i.cabecaChave > qtddCabecaChavesValidos || i.cabecaChave == null).ToList();
            tn.montarJogosPorSorteio(jogosRodada1, inscritosAleatorios, temRepescagem); //Montar o Restante da tabela utilizando um critério de sorteio;
        }

        private void popularJogosIniciais(int torneioId, int classeId, bool temRepescagem, List<InscricaoTorneio> inscritos, int qtddByes)
        {
            var jogosRodada1 = tn.getJogosPrimeiraRodada(classeId);
            popularJogosFase1(jogosRodada1, inscritos, qtddByes, temRepescagem);
            tn.fecharJogosComBye(jogosRodada1);
        }

        private void montarFaseEliminatoria(int torneioId, int qtddJogadores, int classeId, bool temRepescagem)
        {
            if (temRepescagem)
            {
                var onzejogadores = qtddJogadores;
                qtddJogadores = qtddJogadores + (qtddJogadores / 2);
                if ((qtddJogadores == 16) && (onzejogadores != 11)) qtddJogadores++;
            }
            var qtddRodada = getQtddRodada(qtddJogadores);
            var qtddJogosDaRodada = 1;
            for (int fase = 1; fase <= qtddRodada; fase++)
            {
                for (int ordemJogo = 1; ordemJogo <= qtddJogosDaRodada; ordemJogo++)
                {
                    tn.criarJogo(0, 0, torneioId, classeId, fase, ordemJogo);
                }
                qtddJogosDaRodada = qtddJogosDaRodada * 2;
            }
        }

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis")]
        public ActionResult TesteProcessarJogosComBye()
        {
            ProcessarJogosComBye(1, 1);
            return View();
        }
        private void ProcessarJogosComBye(int torneioId, int classeId)
        {
            var jogos = db.Jogo.Where(r => r.torneioId == torneioId && r.classeTorneio == classeId && r.faseTorneio == 1 && r.desafiante_id == 0 && r.situacao_Id == 4).ToList();
            foreach (Jogo jogo in jogos)
            {
                int ordemJogo = 0;
                if (jogo.ordemJogo % 2 != 0)
                {
                    int var1 = 1;
                    int teste = (var1 / 2) + 1;
                    ordemJogo = (int)(jogo.ordemJogo / 2) + 1;
                    var proximoJogo = db.Jogo.Where(r => r.torneioId == torneioId && r.classeTorneio == classeId && r.faseTorneio == 2 && r.ordemJogo == ordemJogo).Single();
                    proximoJogo.desafiado_id = jogo.desafiado_id;
                    proximoJogo.desafiado2_id = jogo.desafiado2_id;
                }
                else
                {
                    ordemJogo = (int)jogo.ordemJogo / 2;
                    var proximoJogo = db.Jogo.Where(r => r.torneioId == torneioId && r.classeTorneio == classeId && r.faseTorneio == 2 && r.ordemJogo == ordemJogo).Single();
                    proximoJogo.desafiante_id = jogo.desafiado_id;
                    proximoJogo.desafiante2_id = jogo.desafiado2_id;
                }
                db.SaveChanges();
            }
        }

        private int getQtddJogosPorRodada(int numeroRodada)
        {
            if (numeroRodada == 6)
            {
                return 32;
            }
            else if (numeroRodada == 5)
            {
                return 16;
            }
            else if (numeroRodada == 4)
            {
                return 8;
            }
            else if (numeroRodada == 3)
            {
                return 4;
            }
            else if (numeroRodada == 2)
            {
                return 2;
            }
            else if (numeroRodada == 1)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        private int getQtddRodadas(int qtddJogos)
        {
            if (qtddJogos == 64)
            {
                return 6;
            }
            else if (qtddJogos == 32)
            {
                return 5;
            }
            else if (qtddJogos == 16)
            {
                return 4;
            }
            else if (qtddJogos == 8)
            {
                return 3;
            }
            else if (qtddJogos == 4)
            {
                return 2;
            }
            else if (qtddJogos == 2)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        [Authorize(Roles = "admin, organizador")]
        public ActionResult ListarJogos(int torneioId)
        {
            string msg = "";
            try
            {
                var jogo = db.Jogo.Include(j => j.desafiado).Include(j => j.desafiante).
                    Where(j => j.torneioId == torneioId && j.desafiado_id != 0 && j.desafiante_id != 0).OrderBy(j => j.classeTorneio).ThenBy(r => r.faseTorneio).ThenBy(r => r.ordemJogo).ToList();
                var torneio = db.Torneio.Find(torneioId);
                ViewBag.Classes = db.Classe.Where(c => c.barragemId == torneio.barragemId).ToList();
                return View(jogo);
            }
            catch (Exception ex)
            {
                ViewBag.MsgErro = msg + " - " + ex.Message;
                return View();
            }

        }

        private void mensagem(string Msg)
        {
            if (Msg != "")
            {
                if (Msg.ToUpper() == "OK")
                {
                    ViewBag.Ok = "Operação realizada com sucesso!";
                }
                else
                {
                    ViewBag.MsgErro = Msg;
                }
            }
        }

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult InscricoesTorneio(int torneioId, string Msg = "")
        {
            var inscricoes = db.InscricaoTorneio.Where(r => r.torneioId == torneioId).OrderBy(r => r.classe).ThenBy(r => r.participante.nome).ToList();

            mensagem(Msg);

            return View(inscricoes);
        }

        public ActionResult Tabela(int torneioId = 0, int filtroClasse = 0, string Msg = "", string Url = "", int barra = 0, int grupo = 1)
        {
            if (Url == "torneio")
            {
                ViewBag.Torneio = "Sim";
            }
            ViewBag.tabelaLiberada = false;
            if (torneioId == 0)
            {
                HttpCookie cookie = Request.Cookies["_barragemId"];
                if (cookie != null)
                {
                    var barragemId = Convert.ToInt32(cookie.Value.ToString());
                    BarragemView barragem = db.BarragemView.Find(barragemId);
                    var tn = db.Torneio.Where(t => t.barragemId == barragemId && t.isAtivo).OrderByDescending(t => t.Id).ToList();
                    if (tn.Count() == 0)
                    {
                        mensagem("Não localizamos nenhum torneio ativo no seu ranking.");
                        return View();
                    }
                    torneioId = tn[0].Id;
                }
                else if (barra != 0)
                {
                    BarragemView barragem = db.BarragemView.Find(barra);
                    var tn = db.Torneio.Where(t => t.barragemId == barra && t.isAtivo).OrderByDescending(t => t.Id).ToList();
                    if (tn.Count() == 0)
                    {
                        mensagem("Não localizamos nenhum torneio ativo no seu ranking.");
                        return View();
                    }
                    torneioId = tn[0].Id;
                    Funcoes.CriarCookieBarragem(Response, Server, barragem.Id, barragem.nome);
                }
            }
            var torneio = db.Torneio.Find(torneioId);
            if (torneioId == 0)
            {
                mensagem("Não localizamos nenhum torneio.");
                return View();
            }
            if (torneio.liberarTabela) ViewBag.tabelaLiberada = torneio.liberarTabela;
            if (filtroClasse == 0)
            {
                var cl = db.ClasseTorneio.Where(c => c.torneioId == torneioId).OrderBy(c => c.nivel).ToList();
                filtroClasse = cl[0].Id;
            }
            var classe = db.ClasseTorneio.Find(filtroClasse);
            var jogos = new List<Jogo>();
            ViewBag.viewFaseGrupo = false;
            if (torneio.barragem.isModeloTodosContraTodos)
            {
                //ViewBag.viewFaseGrupo = true;
                ViewBag.classeFaseGrupo = true;
                ViewBag.qtddGrupos = 1;
                grupo = 1;
                var classificacaoGrupo = tn.ordenarClassificacaoFaseGrupo(classe, grupo);
                ViewBag.classificacaoGrupo = classificacaoGrupo;
                var qtddJogosPorRodada = (classificacaoGrupo.Count() > 0) ? (int)classificacaoGrupo.Count() / 2 : 2;
                qtddJogosPorRodada = (qtddJogosPorRodada % 2 != 0) ? qtddJogosPorRodada + 1 : qtddJogosPorRodada;

                ViewBag.height = (classificacaoGrupo.Count() > 0) ? qtddJogosPorRodada * 110 : 0;
                ViewBag.grupo = grupo;
                jogos = db.Jogo.Where(r => r.torneioId == torneioId && r.classeTorneio == filtroClasse).OrderBy(r => r.rodadaFaseGrupo).ToList();
            }
            else if (classe.faseGrupo)
            {
                ViewBag.classeFaseGrupo = true;
                //var faseNaoFinalizada = db.Jogo.Where(i => i.classeTorneio == filtroClasse && i.grupoFaseGrupo != null && (i.situacao_Id == 1 || i.situacao_Id == 2)).Count();
                jogos = db.Jogo.Where(r => r.torneioId == torneioId && r.classeTorneio == filtroClasse
                    && r.faseTorneio != 100 && r.faseTorneio != 101 && r.rodadaFaseGrupo == 0).OrderByDescending(r => r.faseTorneio).ThenBy(r => r.ordemJogo).ToList();
                if (grupo != 1000)
                {
                    jogos = db.Jogo.Where(r => r.torneioId == torneioId && r.classeTorneio == filtroClasse && r.rodadaFaseGrupo != 0 && r.grupoFaseGrupo == grupo).OrderBy(r => r.rodadaFaseGrupo).ToList();
                    //var inscritosGrupo = db.InscricaoTorneio.Where(it => it.torneioId == torneioId && it.classe == filtroClasse && it.grupo == grupo && it.isAtivo);
                    //List<InscricaoTorneio> classificacaoGrupo;
                    if (classe.isDupla)
                    {
                        ViewBag.qtddGrupos = db.InscricaoTorneio.Where(it => it.torneioId == torneioId && it.classe == filtroClasse && it.isAtivo && it.parceiroDuplaId != null && it.parceiroDuplaId != 0).Max(it => it.grupo);
                        //classificacaoGrupo = inscritosGrupo.Where(i => i.parceiroDuplaId != null && i.parceiroDuplaId != 0).OrderByDescending(it => it.pontuacaoFaseGrupo).ThenBy(it => it.participante.nome).ToList();
                    }
                    else
                    {
                        ViewBag.qtddGrupos = db.InscricaoTorneio.Where(it => it.torneioId == torneioId && it.classe == filtroClasse && it.isAtivo).Max(it => it.grupo);
                        //classificacaoGrupo = inscritosGrupo.OrderByDescending(it => it.pontuacaoFaseGrupo).ThenBy(it => it.participante.nome).ToList();
                    }
                    var classificacaoGrupo = tn.ordenarClassificacaoFaseGrupo(classe, grupo);
                    ViewBag.classificacaoGrupo = classificacaoGrupo;
                    var qtddJogosPorRodada = (classificacaoGrupo.Count() > 0) ? (int)classificacaoGrupo.Count() / 2 : 2;
                    qtddJogosPorRodada = (qtddJogosPorRodada % 2 != 0) ? qtddJogosPorRodada + 1 : qtddJogosPorRodada;
                    ViewBag.grupo = grupo;
                }
                else
                {
                    ViewBag.classeFaseGrupo = false;
                    ViewBag.viewFaseGrupo = true;
                    jogos = db.Jogo.Where(r => r.torneioId == torneioId && r.classeTorneio == filtroClasse
                        && r.faseTorneio != 100 && r.faseTorneio != 101 && r.rodadaFaseGrupo == 0).OrderByDescending(r => r.faseTorneio).ThenBy(r => r.ordemJogo).ToList();
                }
                if (classe.faseMataMata)
                {
                    ViewBag.temMataMata = true;
                }
            }
            else
            {
                ViewBag.classeFaseGrupo = false;
                jogos = db.Jogo.Where(r => r.torneioId == torneioId && r.classeTorneio == filtroClasse
                && r.faseTorneio != 100 && r.faseTorneio != 101 && r.rodadaFaseGrupo == 0).OrderByDescending(r => r.faseTorneio).ThenBy(r => r.ordemJogo).ToList();
            }

            ViewBag.Classes = db.ClasseTorneio.Where(c => c.torneioId == torneioId).OrderBy(c => c.nivel).ToList();
            ViewBag.torneioId = torneioId;
            ViewBag.nomeTorneio = torneio.nome;
            ViewBag.filtroClasse = filtroClasse;

            extrairPrimeiroNomeJogosDupla(jogos);

            mensagem(Msg);
            return View(jogos);
        }

        private void extrairPrimeiroNomeJogosDupla(List<Jogo> jogos)
        {
            foreach (var jogo in jogos)
            {
                if (jogo.desafiado != null && jogo.desafiado2 != null && jogo.desafiante != null && jogo.desafiante2 != null)
                {
                    var nomesDesafiado = jogo.desafiado.nome.Split(' ');
                    jogo.desafiado.nome = nomesDesafiado[0];

                    var nomesDesafiado2 = jogo.desafiado2.nome.Split(' ');
                    jogo.desafiado2.nome = nomesDesafiado2[0];

                    var nomesDesafiante = jogo.desafiante.nome.Split(' ');
                    jogo.desafiante.nome = nomesDesafiante[0];

                    var nomesDesafiante2 = jogo.desafiante2.nome.Split(' ');
                    jogo.desafiante2.nome = nomesDesafiante2[0];

                }
            }
        }

        //public ActionResult TabelaFaseGrupo(int torneioId = 0, int filtroClasse = 0, int grupo=1, string Msg = "", string Url = "", int barra = 0)
        //{
        //    if (Url == "torneio")
        //    {
        //        ViewBag.Torneio = "Sim";
        //    }
        //    ViewBag.tabelaLiberada = false;
        //    if (torneioId == 0)
        //    {
        //        HttpCookie cookie = Request.Cookies["_barragemId"];
        //        if (cookie != null)
        //        {
        //            var barragemId = Convert.ToInt32(cookie.Value.ToString());
        //            BarragemView barragem = db.BarragemView.Find(barragemId);
        //            var tn = db.Torneio.Where(t => t.barragemId == barragemId && t.isAtivo).OrderByDescending(t => t.Id).ToList();
        //            if (tn.Count() == 0)
        //            {
        //                mensagem("Não localizamos nenhum torneio ativo no seu ranking.");
        //                return View();
        //            }
        //            torneioId = tn[0].Id;
        //        }
        //        else if (barra != 0)
        //        {
        //            BarragemView barragem = db.BarragemView.Find(barra);
        //            var tn = db.Torneio.Where(t => t.barragemId == barra && t.isAtivo).OrderByDescending(t => t.Id).ToList();
        //            if (tn.Count() == 0)
        //            {
        //                mensagem("Não localizamos nenhum torneio ativo no seu ranking.");
        //                return View();
        //            }
        //            torneioId = tn[0].Id;
        //            Funcoes.CriarCookieBarragem(Response, Server, barragem.Id, barragem.nome);
        //        }
        //    }
        //    var torneio = db.Torneio.Find(torneioId);
        //    if (torneioId == 0)
        //    {
        //        mensagem("Não localizamos nenhum torneio.");
        //        return View();
        //    }
        //    if (torneio.liberarTabela) ViewBag.tabelaLiberada = torneio.liberarTabela;
        //    var classes = db.ClasseTorneio.Where(c => c.torneioId == torneioId && c.faseGrupo).OrderBy(c => c.nivel).ToList();
        //    ViewBag.Classes = classes;
        //    if (filtroClasse == 0)
        //    {
        //        filtroClasse = classes[0].Id;
        //    }
        //    var jogos = db.Jogo.Where(r => r.torneioId == torneioId && r.classeTorneio == filtroClasse && r.rodadaFaseGrupo!=0 && r.grupoFaseGrupo==grupo).OrderBy(r => r.rodadaFaseGrupo).ToList();
        //    ViewBag.qtddGrupos = db.InscricaoTorneio.Where(it => it.torneioId == torneioId && it.classe == filtroClasse).Max(it => it.grupo);
        //    var inscritosGrupo = db.InscricaoTorneio.Where(it => it.torneioId == torneioId && it.classe == filtroClasse
        //                       && it.grupo == grupo);
        //    List<InscricaoTorneio> classificacaoGrupo;
        //    if (classes.Where(c=> c.Id == filtroClasse).Single().isDupla)
        //    {
        //        classificacaoGrupo = inscritosGrupo.Where(i=>i.parceiroDuplaId!=null && i.parceiroDuplaId!=0).
        //            OrderByDescending(it => it.pontuacaoFaseGrupo).ThenBy(it => it.participante.nome).ToList();
        //    }else{
        //        classificacaoGrupo = inscritosGrupo.OrderByDescending(it => it.pontuacaoFaseGrupo).ThenBy(it => it.participante.nome).ToList();
        //    }

        //    ViewBag.classificacaoGrupo = ordenarClassificacaoFaseGrupo(classificacaoGrupo);
        //    ViewBag.torneioId = torneioId;
        //    ViewBag.nomeTorneio = torneio.nome;
        //    ViewBag.filtroClasse = filtroClasse;
        //    ViewBag.grupo = grupo;

        //    mensagem(Msg);
        //    return View(jogos);
        //}

        public ActionResult InscricoesTorneio2(int torneioId = 0, string Msg = "", string Url = "", int barra = 0)
        {
            List<InscricaoTorneio> inscricoes = null;
            bool liberarTabelaInscricao = false;
            if (torneioId == 0)
            {
                HttpCookie cookie = Request.Cookies["_barragemId"];
                if (cookie != null)
                {
                    var barragemId = Convert.ToInt32(cookie.Value.ToString());
                    BarragemView barragem = db.BarragemView.Find(barragemId);
                    var torneio = db.Torneio.Where(t => t.barragemId == barragemId && t.isAtivo).OrderByDescending(t => t.Id).ToList();
                    if (torneio.Count != 0) {
                        torneioId = torneio[0].Id;
                        liberarTabelaInscricao = torneio[0].liberaTabelaInscricao;
                    }
                    
                }
                else if (barra != 0)
                {
                    BarragemView barragem = db.BarragemView.Find(barra);
                    var torneio = db.Torneio.Where(t => t.barragemId == barra && t.isAtivo).OrderByDescending(t => t.Id).ToList();
                    liberarTabelaInscricao = torneio[0].liberaTabelaInscricao;
                    torneioId = torneio[0].Id;
                    Funcoes.CriarCookieBarragem(Response, Server, barragem.Id, barragem.nome);
                }
            }
            else
            {
                var torneio = db.Torneio.Find(torneioId);
                ViewBag.TorneioId = torneioId;
                liberarTabelaInscricao = torneio.liberaTabelaInscricao;
            }
            inscricoes = db.InscricaoTorneio.Where(r => r.torneioId == torneioId && r.classeTorneio.isDupla == false).OrderBy(r => r.classe).ThenBy(r => r.participante.nome).ToList();
            var inscricoesDupla = db.InscricaoTorneio.Where(r => r.torneioId == torneioId && r.classeTorneio.isDupla == true).OrderBy(r => r.classe).ThenBy(r => r.participante.nome).ToList();

            List<InscricaoTorneio> inscricoesRemove = new List<InscricaoTorneio>();
            foreach (var ins in inscricoesDupla)
            {
                var formouDupla = inscricoesDupla.Where(i => i.parceiroDuplaId == ins.userId && i.classe == ins.classe).Count();
                if (formouDupla > 0)
                {
                    var insdupla = inscricoesDupla.Where(i => i.parceiroDuplaId == ins.userId && i.classe == ins.classe).First();
                    insdupla.isSocio = (bool)ins.isAtivo; // fazendo esta ganbiarra para a tela saber se o parceiro está ativo ou não.
                    inscricoesRemove.Add(ins);
                }
            }
            foreach (var ins in inscricoesRemove)
            {
                inscricoesDupla.Remove(ins);
            }
            ViewBag.inscricoesDupla = inscricoesDupla;
            ViewBag.liberaTabelaInscricao = liberarTabelaInscricao;
            mensagem(Msg);

            if (Url == "torneio")
            {
                ViewBag.Torneio = "Sim";
            }
            return View(inscricoes);
        }

        [Authorize(Roles = "admin,organizador,usuario,adminTorneio,adminTorneioTenis")]
        public ActionResult EfetuarPagamento(int inscricaoId)
        {
            string MsgErro = "";
            var inscricao = db.InscricaoTorneio.Find(inscricaoId);
            if (inscricao.valorPendente != null && inscricao.valorPendente != 0 && inscricao.isAtivo)
            {
                var inscricoes = db.InscricaoTorneio.Where(i => i.torneioId == inscricao.torneioId && i.userId == inscricao.userId && i.isAtivo == false).ToList();
                inscricao = inscricoes[0];
            }
            try
            {
                var barragem = db.BarragemView.Find(inscricao.torneio.barragemId);
                Uri paymentRedirectUri = Register(inscricao, barragem.tokenPagSeguro, barragem.emailPagSeguro);
                Response.Redirect(paymentRedirectUri.AbsoluteUri);
            }
            catch (Exception e)
            {
                MsgErro = e.Message;
            }
            return RedirectToAction("ConfirmacaoInscricao", new { torneioId = inscricao.torneioId, msgErro = MsgErro });
        }

        public ActionResult Regulamento(int barragem)
        {
            var tn = db.Torneio.Where(t => t.barragemId == barragem && t.isAtivo).OrderByDescending(t => t.Id).ToList();
            return View(tn[0]);
        }


        private Uri Register(InscricaoTorneio inscricao, string token, string email)
        {
            //Use global configuration
            //PagSeguroConfiguration.UrlXmlConfiguration = "../../../../../PagSeguroConfig.xml";

            //bool isSandbox = true;
            //EnvironmentConfiguration.ChangeEnvironment(isSandbox);

            // Instantiate a new payment request
            PaymentRequest payment = new PaymentRequest();

            // Sets the currency
            payment.Currency = Currency.Brl;

            // Add an item for this payment request
            if ((inscricao.valorPendente != null) && (inscricao.valorPendente != 0))
            {
                payment.Items.Add(new Uol.PagSeguro.Domain.Item(inscricao.torneioId + "", inscricao.torneio.nome, 1, (decimal)inscricao.valorPendente));
            }
            else
            {
                payment.Items.Add(new Uol.PagSeguro.Domain.Item(inscricao.torneioId + "", inscricao.torneio.nome, 1, (decimal)inscricao.valor));
            }

            // Sets a reference code for this payment request, it is useful to identify this payment in future notifications.
            payment.Reference = "T-" + inscricao.Id;

            // Sets your customer information.
            //payment.Sender = new Sender(inscricao.participante.nome,inscricao.participante.email,new Phone("61", "99999999"));
            string[] arrayNomes = inscricao.participante.nome.Trim().Split(' ');
            var nome = inscricao.participante.nome.Trim();
            if (arrayNomes.Length == 1)
            {
                nome = nome + " Sobrenome";
            }
            nome = nome.Replace("-", "").Replace(".", "");
            var ddd = "";
            var cel = "";
            var telefone = inscricao.participante.telefoneCelular.Trim().Replace("(", "").Replace(")", "").Replace(" ", "").Replace("-", "");
            if (telefone.Length < 10)
            {
                ddd = "61";
                cel = "984086580";
            }
            else
            {
                ddd = telefone.Substring(0, 2);
                cel = telefone.Substring(2);
            }

            payment.Sender = new Sender(nome, inscricao.participante.email.Trim(), new Phone(ddd, cel));

            //SenderDocument document = new SenderDocument(Documents.GetDocumentByType("CPF"), "12345678909");
            //payment.Sender.Documents.Add(document);

            // Sets the url used by PagSeguro for redirect user after ends checkout process
            // payment.RedirectUri = new Uri("http://www.rankingdetenis.com.br/ConfirmacaoPagamento");

            try
            {
                /// Create new account credentials
                /// This configuration let you set your credentials from your ".cs" file.
                //AccountCredentials credentials = new AccountCredentials("backoffice@lojamodelo.com.br", "256422BF9E66458CA3FE41189AD1C94A");

                /// @todo with you want to get credentials from xml config file uncommend the line below and comment the line above.
                //AccountCredentials credentials = PagSeguroConfiguration.Credentials(isSandbox);
                AccountCredentials credentials = new AccountCredentials(email, token);

                return payment.Register(credentials);
            }
            catch (PagSeguroServiceException exception)
            {
                Console.WriteLine(exception.Message + "\n");

                foreach (ServiceError element in exception.Errors)
                {
                    Console.WriteLine(element + "\n");
                }
                throw new ArgumentException("Erro ao registrar pagamento", exception.Message);
                //Console.ReadKey();
            }
        }

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult EditInscritos(int torneioId, int filtroClasse = 0, string filtroJogador = "", string Msg = "")
        {

            List<InscricaoTorneio> inscricao = db.InscricaoTorneio.Where(i => i.torneioId == torneioId).ToList();
            var torneio = db.Torneio.Find(torneioId);
            if (filtroClasse != 0)
            {
                inscricao = inscricao.Where(i => i.classe == filtroClasse).ToList();
                ViewBag.CabecasDeChave = getOpcoesCabecaDeChave(filtroClasse);
            }
            if (filtroJogador != "")
            {
                inscricao = inscricao.Where(i => i.participante.nome.ToUpper().Contains(filtroJogador.ToUpper())).ToList();
            }
            ViewBag.descricaoTipoDesconto = torneio.descontoPara;
            if (torneio.valorDescontoFederado > torneio.valorSocio)
            {
                ViewBag.descontoFederadoMaior = true;
            }
            else
            {
                ViewBag.descontoFederadoMaior = false;
            }
            ViewBag.Classes = db.ClasseTorneio.Where(c => c.torneioId == torneioId).ToList();
            ViewBag.filtroClasse = filtroClasse;
            ViewBag.TorneioId = torneioId;
            ViewBag.flag = "inscritos";
            ViewBag.InscIndividuais = db.InscricaoTorneio.Where(i => i.torneioId == torneioId).Select(i => (int)i.userId).Distinct().Count();
            ViewBag.InscIndividuaisSocios = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.isSocio == true).Select(i => (int)i.userId).Distinct().Count();
            ViewBag.InscIndividuaisFederados = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.isFederado == true).Select(i => (int)i.userId).Distinct().Count();
            ViewBag.TotalPagantes = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.isAtivo == true).Select(i => (int)i.userId).Distinct().Count();
            ViewBag.ValorPago = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.isAtivo == true).Select(i => new { user = (int)i.userId, valor = i.valor }).Distinct().Sum(i => i.valor);
            ViewBag.PagoNoCartao = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.isAtivo == true && (i.statusPagamento == "3" || i.statusPagamento == "4")).
                Select(i => (int)i.userId).Distinct().Count();
            mensagem(Msg);
            return View(inscricao);
        }

        private CobrancaTorneio getDadosDeCobrancaTorneio(int torneioId)
        {
            var cobrancaTorneio = new CobrancaTorneio();
            var ativo = Tipos.Situacao.ativo.ToString();
            var licenciado = Tipos.Situacao.licenciado.ToString();
            var suspenso = Tipos.Situacao.suspenso.ToString();
            var suspensoWO = Tipos.Situacao.suspensoWO.ToString();

            //cobrancaTorneio.qtddInscritosPagantes = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.valor>0 && i.isAtivo).Select(i => (int)i.userId).Distinct().Count();
            cobrancaTorneio.qtddInscritos = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.isAtivo).Select(i => (int)i.userId).Distinct().Count();
            var inscritosNaoPagantes = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.isAtivo && i.torneio.barragemId == i.participante.barragemId
            && (i.participante.situacao == ativo || i.participante.situacao == suspenso || i.participante.situacao == licenciado || i.participante.situacao == suspensoWO)).Select(i => (int)i.userId).Distinct().Count();
            cobrancaTorneio.valorDescontoParaRanking = inscritosNaoPagantes * 5;
            cobrancaTorneio.valorASerPago = (cobrancaTorneio.qtddInscritos * 5) - cobrancaTorneio.valorDescontoParaRanking;
            return cobrancaTorneio;
        }

        private int getOpcoesCabecaDeChave(int classeId)
        {
            var classe = db.ClasseTorneio.Find(classeId);
            int qtddCabecaChave = 0;
            if (classe.faseGrupo)
            {
                qtddCabecaChave = tn.getQtddGruposFaseGrupos(tn.getInscritosPorClasse(classe, true).Count());
            }
            else
            {
                qtddCabecaChave = 16;
            }
            return qtddCabecaChave;
        }

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult EditObs(int torneioId)
        {
            List<InscricaoTorneio> inscricao = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.observacao != null && i.observacao != "").ToList();
            ViewBag.flag = "obs";
            ViewBag.TorneioId = torneioId;
            return View(inscricao);
        }

        [Authorize(Roles = "admin")]
        public ActionResult EditTeste(int torneioId, string msg = "")
        {
            var classes = db.ClasseTorneio.Where(c => c.torneioId == torneioId).ToList();
            ViewBag.flag = "teste";
            ViewBag.torneioId = torneioId;
            mensagem(msg);
            return View(classes);
        }

        [Authorize(Roles = "admin")]
        public ActionResult GerarPlacaresTeste(int torneioId, int classeId, string fase)
        {
            var rodadaFaseGrupo = 0;
            var isFaseGrupo = fase.Split('-')[0];
            List<Jogo> jogos = null;
            if (isFaseGrupo == "700")
            {
                rodadaFaseGrupo = Convert.ToInt32(fase.Split('-')[1]);
                jogos = db.Jogo.Where(c => c.classeTorneio == classeId && c.rodadaFaseGrupo == rodadaFaseGrupo && c.desafiante_id != 10).ToList();
            }
            else
            {
                var faseInt = Convert.ToInt32(fase);
                jogos = db.Jogo.Where(c => c.classeTorneio == classeId && c.faseTorneio == faseInt && c.desafiante_id != 10).ToList();
            }
            foreach (var j in jogos)
            {
                gerarPlacarAleatorio(j);
            }
            return RedirectToAction("EditTeste", new { torneioId = torneioId, msg = "OK" });
        }

        private void gerarPlacarAleatorio(Jogo jogo)
        {
            var set1 = new Random().Next(5);
            var set2 = new Random().Next(5);
            var set3 = new Random().Next(5);
            var setV1 = 6;
            var setV2 = 6;
            var setV3 = 6;
            jogo.situacao_Id = 4;
            if (new Random().Next(2) == 1)
            {
                jogo.qtddGames1setDesafiado = set1;
                jogo.qtddGames2setDesafiado = setV2;
                jogo.qtddGames3setDesafiado = set3;
                jogo.qtddGames1setDesafiante = setV1;
                jogo.qtddGames2setDesafiante = set2;
                jogo.qtddGames3setDesafiante = setV3;
            }
            else
            {
                jogo.qtddGames1setDesafiado = setV1;
                jogo.qtddGames2setDesafiado = setV2;
                jogo.qtddGames3setDesafiado = 0;
                jogo.qtddGames1setDesafiante = set1;
                jogo.qtddGames2setDesafiante = set2;
                jogo.qtddGames3setDesafiante = 0;
            }
            db.Entry(jogo).State = EntityState.Modified;
            db.SaveChanges();
            tn.MontarProximoJogoTorneio(jogo);
        }


        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult EditClasse(int torneioId)
        {
            var classes = db.ClasseTorneio.Where(c => c.torneioId == torneioId).OrderBy(c => c.nivel).ToList();
            ViewBag.isLiga = db.TorneioLiga.Where(tl => tl.TorneioId == torneioId).ToList().Count > 0;
            ViewBag.flag = "classes";
            ViewBag.torneioId = torneioId;
            var torneio = db.Torneio.Find(torneioId);
            ViewBag.isModeloTodosContraTodos = torneio.barragem.isModeloTodosContraTodos;
            ViewBag.temLimiteDeInscricao = torneio.temLimiteDeInscricao;
            List<TorneioLiga> lts = db.TorneioLiga.Where(tl => tl.TorneioId == torneioId).ToList();
            foreach (var item in classes)
            {
                foreach (var liga in lts)
                {
                    var classesLiga = db.ClasseLiga.Where(cl => cl.LigaId == liga.LigaId && cl.CategoriaId == item.categoriaId).ToList();
                    if (classesLiga.Count == 0)
                    {
                        item.Categoria = null;
                        item.categoriaId = null;
                    } else {
                        item.Categoria = classesLiga[0].Categoria;
                        item.categoriaId = classesLiga[0].CategoriaId;
                        break;
                    }
                }
            }
            return View(classes);
        }

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult EditPatrocinadores(int torneioId, string msg = "")
        {
            mensagem(msg);
            ViewBag.torneioId = torneioId;
            var patrocinadores = db.Patrocinador.Where(p => p.torneioId == torneioId).ToList();
            return View(patrocinadores);
        }

        [HttpPost]
        public ActionResult EditPatrocinadores(int Id, string urllink, int torneioId)
        {
            try
            {
                if (urllink.IndexOf("http") < 0)
                {
                    urllink = "http://" + urllink;
                }
                if (Id == 0)
                {
                    if (ModelState.IsValid)
                    {
                        var patrocinador = new Patrocinador();
                        patrocinador.urllink = urllink;
                        patrocinador.torneioId = torneioId;
                        db.Patrocinador.Add(patrocinador);
                        db.SaveChanges();
                    }
                }
                else
                {
                    var p = db.Patrocinador.Find(Id);
                    HttpPostedFileBase filePosted = Request.Files["filepatrocinador"];
                    if (filePosted != null && filePosted.ContentLength > 0)
                    {
                        var path = "/Content/images/patrocinadores/";
                        string fileNameApplication = System.IO.Path.GetFileName(filePosted.FileName);
                        string fileExtensionApplication = System.IO.Path.GetExtension(fileNameApplication);

                        // generating a random guid for a new file at server for the uploaded file
                        string newFile = "logo" + Request.Form["Id"] + fileExtensionApplication;
                        // getting a valid server path to save
                        string filePath = System.IO.Path.Combine(Server.MapPath(path), newFile);

                        if (fileNameApplication != String.Empty)
                        {
                            filePosted.SaveAs(filePath);
                            p.urlImagem = path + newFile;
                        }
                    }
                    p.urllink = urllink;
                    db.Entry(p).State = EntityState.Modified;
                    db.SaveChanges();
                }
                return RedirectToAction("EditPatrocinadores", new { torneioId = torneioId, msg = "OK" });
                //return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return RedirectToAction("EditPatrocinadores", new { torneioId = torneioId, msg = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult EditClasse(int Id, string nome, bool isDupla = false, bool faseGrupo = false, bool faseMataMata = false, bool isModeloTodosContraTodos = false, int maximoInscritos = 0)
        {
            try
            {
                var classe = db.ClasseTorneio.Find(Id);
                classe.nome = nome;
                classe.isDupla = isDupla;
                if (!isModeloTodosContraTodos)
                {
                    classe.faseGrupo = faseGrupo;
                    classe.faseMataMata = faseMataMata;
                }
                classe.isPrimeiraOpcao = true;
                classe.isSegundaOpcao = true;
                classe.maximoInscritos = maximoInscritos;
                db.Entry(classe).State = EntityState.Modified;
                db.SaveChanges();
                return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult EditOrdemExibicaoClasse(int torneioId, IList<int> ordemExibicao)
        {
            var classes = db.ClasseTorneio.Where(c => c.torneioId == torneioId).OrderBy(c => c.nivel).ToList();
            int i = 0;
            foreach (var item in classes)
            {
                //var classe = db.ClasseTorneio.Find(item.Id);
                item.nivel = ordemExibicao[i];
                i++;
                db.Entry(item).State = EntityState.Modified;
                db.SaveChanges();
            }
            return RedirectToAction("EditClasse", new { torneioId = torneioId });
        }

        //[HttpPost]
        //public ActionResult EditClasseFaseGrupo(int Id, int qtddJogadoresPorGrupo, int qtddPassamFase)
        //{
        //    try
        //    {
        //        var classe = db.ClasseTorneio.Find(Id);
        //        classe.qtddJogadoresPorGrupo = qtddJogadoresPorGrupo;
        //        classe.qtddPassamFase = qtddPassamFase;
        //        db.Entry(classe).State = EntityState.Modified;
        //        db.SaveChanges();
        //        return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
        //    }
        //}

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult CreateClasse(int torneioId, int qtddClasses)
        {
            ViewBag.torneioId = torneioId;
            ViewBag.qtddClasses = qtddClasses + 1;
            ///ViewBag.Categorias
            List<Categoria> categorias = new List<Categoria>();
            categorias.Add(new Categoria() { Id = 0, Nome = "Criar classe que não contará pontos para a liga" });
            List<TorneioLiga> ligasDotorneio = db.TorneioLiga.Where(tl => tl.TorneioId == torneioId).ToList();
            List<int> ligas = new List<int>();
            foreach (TorneioLiga tl in ligasDotorneio)
            {
                ligas.Add(tl.LigaId);
            }
            foreach (ClasseLiga cl in db.ClasseLiga.Where(classeLiga => ligas.Contains(classeLiga.LigaId)).GroupBy(cl => cl.CategoriaId).Select(c => c.FirstOrDefault()).ToList())
            {
                categorias.Add(db.Categoria.Find(cl.CategoriaId));
            }
            ViewBag.Categorias = categorias;

            return View();
        }

        [HttpPost]
        public ActionResult CreateClasse(ClasseTorneio classe)
        {
            classe.isSegundaOpcao = true;
            classe.isPrimeiraOpcao = true;
            if (ModelState.IsValid)
            {
                if (classe.categoriaId == 0)
                    classe.categoriaId = null;
                db.ClasseTorneio.Add(classe);
                db.SaveChanges();
                return RedirectToAction("EditClasse", new { torneioId = classe.torneioId });
            }
            else
            {
                ViewBag.torneioId = classe.torneioId;
            }
            return View(classe);
        }

        [HttpPost]
        public ActionResult EditInscritos(int Id, int classe, int cabecaChave, bool isAtivo)
        {
            try
            {
                var inscricao = db.InscricaoTorneio.Find(Id);
                if (inscricao.classeTorneio.faseGrupo)
                {
                    inscricao.grupo = cabecaChave;
                }
                inscricao.classe = classe;
                inscricao.isAtivo = isAtivo;
                inscricao.cabecaChave = cabecaChave;
                if ((isAtivo) && (inscricao.statusPagamento!="3") && (inscricao.statusPagamento!="4"))
                {
                    inscricao.statusPagamento = "0";
                }
                if ((inscricao.valorPendente != null) && (inscricao.valorPendente != 0) && (isAtivo))
                {
                    var listInscricao = db.InscricaoTorneio.Where(t => t.torneioId == inscricao.torneioId && t.userId == inscricao.userId && t.Id != inscricao.Id && t.isAtivo).ToList();
                    foreach (var item in listInscricao)
                    {
                        if ((item.valorPendente != null) && (item.valorPendente != 0))
                        {
                            item.valor = item.valor + item.valorPendente;
                            item.valorPendente = 0;
                            db.Entry(item).State = EntityState.Modified;
                            db.SaveChanges();
                        }
                    }
                    inscricao.valor = inscricao.valor + inscricao.valorPendente;
                    inscricao.valorPendente = 0;
                }
                db.Entry(inscricao).State = EntityState.Modified;
                db.SaveChanges();
                return Json(new { erro = "", retorno = 1, statusPagamento = inscricao.descricaoStatusPag }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult EditTorneio(int id = 0)
        {
            ViewBag.flag = "edit";
            Torneio torneio = db.Torneio.Find(id);
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            var barragemId = 0;
            if (perfil.Equals("admin"))
            {
                barragemId = torneio.barragemId;
            }
            else
            {
                barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
            }
            var barragem = db.BarragemView.Find(barragemId);
            ViewBag.isModeloTodosContraTodos = barragem.isModeloTodosContraTodos;
            ViewBag.tokenPagSeguro = barragem.tokenPagSeguro;
            ViewBag.barraId = barragemId;
            ViewBag.barragemId = new SelectList(db.BarragemView, "Id", "nome", barragemId);
            ViewBag.TorneioId = id;
            ViewBag.JogadoresClasses = db.InscricaoTorneio.Where(i => i.torneioId == id && i.isAtivo == true).OrderBy(i => i.classe).ThenBy(i => i.participante.nome).ToList();
            ViewBag.CobrancaTorneio = getDadosDeCobrancaTorneio(id);
            List<BarragemLiga> ligasDoRanking = db.BarragemLiga.Include(l => l.Liga).Where(bl => bl.BarragemId == barragemId && bl.Liga.isAtivo).ToList();
            ViewBag.LigasDisponiveis = ligasDoRanking;
            List<TorneioLiga> lts = db.TorneioLiga.Include(l => l.Liga).Where(tl => tl.TorneioId == torneio.Id).ToList();
            List<Liga> ligasDoTorneio = new List<Liga>();
            foreach (TorneioLiga tl in lts)
            {
                ligasDoTorneio.Add(db.Liga.Find(tl.LigaId));
            }
            ViewBag.LigasDoTorneio = ligasDoTorneio;

            if (torneio == null)
            {
                return HttpNotFound();
            }
            ViewBag.LinkParaCopia = "https://" + HttpContext.Request.Url.Host + "/torneio-" + torneio.Id;
            return View(torneio);
        }

        [AllowAnonymous]
        public ActionResult LoginInscricaoApp(int torneioId, string credenciais)
        {
            try
            {
                var bytes = Convert.FromBase64String(credenciais);
                var userPass = System.Text.Encoding.UTF8.GetString(bytes);
                var userName = userPass.Split('|')[0];
                var password = userPass.Split('|')[1];
                if (WebSecurity.Login(userName, password))
                {
                    var torneio = db.Torneio.Find(torneioId);
                    Funcoes.CriarCookieBarragem(Response, Server, torneio.barragemId, torneio.barragem.nome);
                    return RedirectToAction("Detalhes", new { id = torneioId });
                }
                else
                {
                    return RedirectToAction("Login", "Account", new { torneioId = torneioId, returnUrl = "torneio" });
                }
            }
            catch (Exception e)
            {
                return RedirectToAction("Login", "Account", new { torneioId = torneioId, returnUrl = "torneio" });
            }
        }

        [Authorize(Roles = "admin,usuario,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult Detalhes(int id = 0, String Msg = "", int userId = 0)
        {
            Torneio torneio = db.Torneio.Find(id);
            ViewBag.isAceitaCartao = false;
            if (!String.IsNullOrEmpty(torneio.barragem.tokenPagSeguro))
            {
                ViewBag.isAceitaCartao = true;
            }
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            if (perfil.Equals("usuario") || userId == 0)
            {
                userId = WebSecurity.GetUserId(User.Identity.Name);
            }
            var inscricao = db.InscricaoTorneio.Where(i => i.torneio.Id == id && i.userId == userId).ToList();
            var classes = db.ClasseTorneio.Where(i => i.torneioId == id).OrderBy(c => c.nivel).ToList();
            //var classes2 = db.ClasseTorneio.Where(i => i.torneioId == id && i.isSegundaOpcao).OrderBy(c => c.nivel).ToList();
            ViewBag.Classes = classes;
            ViewBag.Classes2 = classes;
            ViewBag.ClasseInscricao2 = 0;
            ViewBag.ClasseInscricao3 = 0;
            ViewBag.ClasseInscricao4 = 0;
            ViewBag.ClasseInscricao = 0;

            ViewBag.qtddInscritos = tn.qtddInscritosEmCadaClasse(classes, id);

            ViewBag.isGratuito = false;
            if (VerificarGratuidade(torneio, userId))
            {
                ViewBag.isGratuito = true;
            }
            if (inscricao.Count > 0)
            {
                ViewBag.inscricao = inscricao[0];
                ViewBag.ClasseInscricao = inscricao[0].classe;
                if (inscricao.Count > 1) ViewBag.ClasseInscricao2 = inscricao[1].classeTorneio.Id;
                if (inscricao.Count > 2) ViewBag.ClasseInscricao3 = inscricao[2].classeTorneio.Id;
                if (inscricao.Count > 3) ViewBag.ClasseInscricao4 = inscricao[3].classeTorneio.Id;
            }
            mensagem(Msg);
            return View(torneio);
        }




        private bool VerificarGratuidade(Torneio torneio, int userId)
        {
            if (!torneio.isGratuitoSocio)
            {
                return false;
            }
            else
            {
                var user = db.UserProfiles.Find(userId);
                if ((user.barragemId == torneio.barragemId) &&
                    (user.situacao.Equals("ativo") || user.situacao.Equals("licenciado") || user.situacao.Equals("suspenso") || user.situacao.Equals("suspensoWO")))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        [Authorize(Roles = "admin,usuario,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult ConfirmacaoInscricao(int torneioId, string msg = "", string msgErro = "", int userId = 0)
        {
            //torneioId = 1;
            ViewBag.Msg = msg;
            ViewBag.MsgErro = msgErro;
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            if (perfil.Equals("usuario") || userId == 0)
            {
                userId = WebSecurity.GetUserId(User.Identity.Name);
            }
            var torneio = db.Torneio.Find(torneioId);
            ViewBag.isAceitaCartao = false;
            if (!String.IsNullOrEmpty(torneio.barragem.tokenPagSeguro))
            {
                ViewBag.isAceitaCartao = true;
            }
            var procurarIdDaInscricaoNaoPaga = false;
            var inscricoes = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.userId == userId).ToList();
            if (inscricoes[0].valorPendente != null && inscricoes[0].valorPendente != 0)
            {
                procurarIdDaInscricaoNaoPaga = true;
            }
            if (procurarIdDaInscricaoNaoPaga)
            {
                foreach (var item in inscricoes)
                {
                    if (!item.isAtivo)
                    {
                        ViewBag.InscricaoId = item.Id;
                        break;
                    }
                }
            }
            if (inscricoes.Count() > 1) ViewBag.SegundaOpcaoClasse = inscricoes[1].classeTorneio.nome;
            if (inscricoes.Count() > 2) ViewBag.TerceiraOpcaoClasse = inscricoes[2].classeTorneio.nome;
            if (inscricoes.Count() > 3) ViewBag.QuartaOpcaoClasse = inscricoes[3].classeTorneio.nome;
            return View(inscricoes[0]);
        }
        [Authorize(Roles = "admin,usuario,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        [HttpPost]
        public ActionResult Inscricao(int torneioId, int classeInscricao = 0, string operacao = "", int classeInscricao2 = 0, int classeInscricao3 = 0, int classeInscricao4 = 0, string observacao = "", bool isSocio = false, bool isFederado = false, bool isClasseDupla = false, int userId = 0)
        {
            var mensagemRetorno = InscricaoNegocio(torneioId, classeInscricao, operacao, classeInscricao2, classeInscricao3, classeInscricao4, observacao, isSocio, isClasseDupla, userId, isFederado);
            if (mensagemRetorno.nomePagina == "ConfirmacaoInscricao")
            {
                return RedirectToAction(mensagemRetorno.nomePagina, new { torneioId = torneioId, msg = mensagemRetorno.mensagem, msgErro = "", userId = userId });
            }
            else
            {
                return RedirectToAction(mensagemRetorno.nomePagina, new { id = torneioId, userId = userId, Msg = mensagemRetorno.mensagem });
            }
        }

        private void mudarStatusSeDesativadoParaStatusTorneio(int userId)
        {
            var usuario = db.UserProfiles.Find(userId);
            if (usuario != null)
            {
                if (usuario.situacao == "desativado")
                {
                    usuario.situacao = "torneio";
                    db.Entry(usuario).State = EntityState.Modified;
                    db.SaveChanges();

                }
            }
        }

        public MensagemRetorno InscricaoNegocio(int torneioId, int classeInscricao = 0, string operacao = "", int classeInscricao2 = 0, int classeInscricao3 = 0, int classeInscricao4 = 0, string observacao = "", bool isSocio = false, bool isClasseDupla = false, int userId = 0, bool isFederado = false)
        {
            var mensagemRetorno = new MensagemRetorno();
            try
            {
                var gratuidade = false;
                if (userId == 0)
                {
                    string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
                    if (!perfil.Equals(""))
                    {
                        userId = WebSecurity.GetUserId(User.Identity.Name);
                    }
                }
                mudarStatusSeDesativadoParaStatusTorneio(userId);
                var torneio = db.Torneio.Find(torneioId);
                var isInscricao = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.userId == userId).Count();
                InscricaoTorneio inscricao = null;
                if (isInscricao > 0)
                {
                    var it = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.userId == userId).ToList();
                    if (operacao == "cancelar")
                    {
                        foreach (var item in it)
                        {
                            db.InscricaoTorneio.Remove(item);
                        }
                    }
                    else
                    {
                        string msgValidacaoClasse = validarEscolhasDeClasses(classeInscricao, classeInscricao2, classeInscricao3, classeInscricao4);
                        
                        isSocio = (it[0].isSocio == null) ? false : (bool)it[0].isSocio;
                        isFederado = (it[0].isFederado == null) ? false : (bool)it[0].isFederado;
                        var valorInscricao = calcularValorInscricao(classeInscricao2, classeInscricao3, classeInscricao4, isSocio, torneio, userId, isFederado);
                        if (operacao == "alterarClasse")
                        {
                            double valorPendente = 0;
                            it[0].classe = classeInscricao;
                            if ((valorInscricao > it[0].valor) && (it[0].isAtivo))
                            {
                                valorPendente = valorInscricao - (double)it[0].valor;
                                it[0].valorPendente = valorPendente;
                                valorInscricao = (double)it[0].valor;
                            }
                            else
                            {
                                it[0].valor = valorInscricao;
                            }
                            db.Entry(it[0]).State = EntityState.Modified;
                            if (it.Count() > 1)
                            {
                                alterarClasseInscricao(it[1], classeInscricao2, valorInscricao, valorPendente);
                            }
                            else if (classeInscricao2 != 0)
                            {
                                msgValidacaoClasse = validarLimiteDeInscricao(classeInscricao2);
                                if (msgValidacaoClasse != "")
                                {
                                    mensagemRetorno.nomePagina = "Detalhes";
                                    mensagemRetorno.mensagem = msgValidacaoClasse;
                                    return mensagemRetorno;
                                }

                                InscricaoTorneio insc2 = preencherInscricaoTorneio(torneioId, userId, classeInscricao2, valorInscricao, observacao, isSocio, isFederado, valorPendente);
                                db.InscricaoTorneio.Add(insc2);
                            }
                            if (it.Count() > 2)
                            {
                                alterarClasseInscricao(it[2], classeInscricao3, valorInscricao);
                            }
                            else if (classeInscricao3 != 0)
                            {
                                msgValidacaoClasse = validarLimiteDeInscricao(classeInscricao3);
                                if (msgValidacaoClasse != "")
                                {
                                    mensagemRetorno.nomePagina = "Detalhes";
                                    mensagemRetorno.mensagem = msgValidacaoClasse;
                                    return mensagemRetorno;
                                }
                                InscricaoTorneio insc3 = preencherInscricaoTorneio(torneioId, userId, classeInscricao3, valorInscricao, observacao, isSocio, isFederado, valorPendente);
                                db.InscricaoTorneio.Add(insc3);
                            }
                            if (it.Count() > 3)
                            {
                                alterarClasseInscricao(it[3], classeInscricao4, valorInscricao);
                            }
                            else if (classeInscricao4 != 0)
                            {
                                msgValidacaoClasse = validarLimiteDeInscricao(classeInscricao4);
                                if (msgValidacaoClasse != "")
                                {
                                    mensagemRetorno.nomePagina = "Detalhes";
                                    mensagemRetorno.mensagem = msgValidacaoClasse;
                                    return mensagemRetorno;
                                }
                                InscricaoTorneio insc4 = preencherInscricaoTorneio(torneioId, userId, classeInscricao4, valorInscricao, observacao, isSocio, isFederado, valorPendente);
                                db.InscricaoTorneio.Add(insc4);
                            }
                            db.SaveChanges();
                            if (isClasseDupla)
                            {
                                mensagemRetorno.nomePagina = "EscolherDupla";
                                mensagemRetorno.mensagem = "";
                                return mensagemRetorno;
                            }
                            if (valorPendente > 0)
                            {
                                mensagemRetorno.nomePagina = "ConfirmacaoInscricao";
                            }
                            else
                            {
                                mensagemRetorno.nomePagina = "Detalhes";
                            }
                            mensagemRetorno.mensagem = "ok";
                            return mensagemRetorno;
                        }
                    }
                }
                else
                {
                    string msgValidacaoClasse = validarEscolhasDeClasses(classeInscricao, classeInscricao2, classeInscricao3, classeInscricao4);
                    if (msgValidacaoClasse != "")
                    {
                        mensagemRetorno.nomePagina = "Detalhes";
                        mensagemRetorno.mensagem = msgValidacaoClasse;
                        return mensagemRetorno;
                    }
                    msgValidacaoClasse = validarLimiteDeInscricao(classeInscricao, classeInscricao2, classeInscricao3, classeInscricao4, torneio.Id);
                    if (msgValidacaoClasse != "")
                    {
                        mensagemRetorno.nomePagina = "Detalhes";
                        mensagemRetorno.mensagem = msgValidacaoClasse;
                        return mensagemRetorno;
                    }
                    double valorInscricao = calcularValorInscricao(classeInscricao2, classeInscricao3, classeInscricao4, isSocio, torneio, userId, isFederado);

                    inscricao = preencherInscricaoTorneio(torneioId, userId, classeInscricao, valorInscricao, observacao, isSocio, isFederado);
                    db.InscricaoTorneio.Add(inscricao);
                    if (classeInscricao2 > 0)
                    {
                        InscricaoTorneio insc2 = preencherInscricaoTorneio(torneioId, userId, classeInscricao2, valorInscricao, observacao, isSocio, isFederado);
                        db.InscricaoTorneio.Add(insc2);
                    }
                    if (classeInscricao3 > 0)
                    {
                        InscricaoTorneio insc3 = preencherInscricaoTorneio(torneioId, userId, classeInscricao3, valorInscricao, observacao, isSocio, isFederado);
                        db.InscricaoTorneio.Add(insc3);
                    }
                    if (classeInscricao4 > 0)
                    {
                        InscricaoTorneio insc4 = preencherInscricaoTorneio(torneioId, userId, classeInscricao4, valorInscricao, observacao, isSocio, isFederado);
                        db.InscricaoTorneio.Add(insc4);
                    }
                }
                db.SaveChanges();
                mensagemRetorno.id = inscricao.Id;
                mensagemRetorno.mensagem = "Inscrição recebida.";
                gratuidade = VerificarGratuidade(torneio, userId);
                if ((torneio.valor == 0) || (gratuidade))
                {
                    mensagemRetorno.mensagem = "Inscrição realizada com sucesso.";
                }
                if (operacao == "cancelar")
                {
                    mensagemRetorno.nomePagina = "Detalhes";
                    mensagemRetorno.mensagem = "OK";
                    return mensagemRetorno;
                }
                if (isClasseDupla)
                {
                    mensagemRetorno.nomePagina = "EscolherDupla";
                    mensagemRetorno.mensagem = "";
                    return mensagemRetorno;
                }
                mensagemRetorno.nomePagina = "ConfirmacaoInscricao";
                return mensagemRetorno;

            }
            catch (Exception ex)
            {
                mensagemRetorno.nomePagina = "Detalhes";
                mensagemRetorno.mensagem = ex.Message;
                mensagemRetorno.tipo = "erro";
                return mensagemRetorno;
            }
        }

        private void alterarClasseInscricao(InscricaoTorneio it, int classeInscricao, double valorInscricao, double valorPendente = 0)
        {
            if (classeInscricao != 0)
            {
                it.classe = classeInscricao;
                it.valorPendente = valorPendente;
                it.valor = valorInscricao;
                db.Entry(it).State = EntityState.Modified;
            }
            else
            {
                db.InscricaoTorneio.Remove(it);
            }
        }

        public String validarLimiteDeInscricao(int classeInscricao, int classeInscricao2, int classeInscricao3, int classeInscricao4, int torneioId) {
            var classes = db.ClasseTorneio.Where(c => c.torneioId == torneioId && c.maximoInscritos > 0).ToList();
            for (int i = 0; i < 4; i++) {
                var clInscrito = 0;
                if (i == 0)
                {
                    clInscrito = classeInscricao;
                }else if (i == 1)
                {
                    clInscrito = classeInscricao2;
                }
                else if (i == 2)
                {
                    clInscrito = classeInscricao3;
                }
                else if (i == 3)
                {
                    clInscrito = classeInscricao4;
                }
                try
                {
                    var classeSelecionada = classes.Where(j => j.Id == clInscrito).First();
                    var qtddInscritos = db.InscricaoTorneio.Where(j => j.classe == clInscrito).Count();
                    if (qtddInscritos >= classeSelecionada.maximoInscritos)
                    {
                        return "Vagas esgotadas para a classe: " + classeSelecionada.nome + ".";
                    }
                }
                catch (Exception e) { }
                
            }
            return "";

        }

        private String validarLimiteDeInscricao(int classeInscricao)
        {
            
            try
            {
                var classeSelecionada = db.ClasseTorneio.Find(classeInscricao);
                if (classeSelecionada.maximoInscritos > 0)
                {
                    var qtddInscritos = db.InscricaoTorneio.Where(j => j.classe == classeInscricao).Count();
                    if (qtddInscritos >= classeSelecionada.maximoInscritos)
                    {
                        return "Vagas esgotadas para a classe: " + classeSelecionada.nome + ".";
                    }
                }
            }
            catch (Exception e) { }
                        
            return "";

        }

        public String validarEscolhasDeClasses(int classeInscricao, int classeInscricao2, int classeInscricao3, int classeInscricao4)
        {
            if (classeInscricao == 0)
            {
                return "Escolha uma categoria para jogar o torneio.";
            }
            else if (classeInscricao == classeInscricao2)
            {
                return "Escolha uma categoria diferente na 2ª opção.";
            }
            else if (classeInscricao == classeInscricao3)
            {
                return "Escolha uma categoria diferente na 3ª opção.";
            }
            else if (classeInscricao == classeInscricao4)
            {
                return "Escolha uma categoria diferente na 4ª opção.";
            }
            else if ((classeInscricao2 != 0) && (classeInscricao2 == classeInscricao3))
            {
                return "Escolha uma categoria diferente na 3ª opção.";
            }
            else if ((classeInscricao2 != 0) && (classeInscricao2 == classeInscricao4))
            {
                return "Escolha uma categoria diferente na 4ª opção.";
            }
            else if ((classeInscricao3 != 0) && (classeInscricao3 == classeInscricao4))
            {
                return "Escolha uma categoria diferente na 4ª opção.";
            }
            return "";
        }

        public InscricaoTorneio preencherInscricaoTorneio(int torneioId, int userId, int classeInscricao, double? valorInscricao, string observacao, bool isSocio, bool isFederado, double valorPendente = 0)
        {
            InscricaoTorneio inscricao = new InscricaoTorneio();
            inscricao.classe = classeInscricao;
            inscricao.torneioId = torneioId;
            inscricao.userId = userId;
            inscricao.valor = valorInscricao;
            inscricao.valorPendente = valorPendente;
            inscricao.observacao = observacao;
            inscricao.isSocio = isSocio;
            inscricao.isFederado = isFederado;
            if (valorInscricao > 0)
            {
                inscricao.isAtivo = false;
            }
            else
            {
                inscricao.isAtivo = true;
            }
            return inscricao;
        }

        public double calcularValorInscricao(int classeInscricao2, int classeInscricao3, int classeInscricao4, bool isSocio, Torneio torneio, int userId, bool isFederado, int qtddInscricao = 1)
        {
            int qtddInscricoes = qtddInscricao;
            double valorInscricao = 0.0;
            double valorInscricaoSocio = 0.0;
            double valorInscricaoFederado = 0.0;
            bool gratuidade = VerificarGratuidade(torneio, userId);
            if (gratuidade)
            {
                return 0.0;
            }

            if (classeInscricao2 != 0) qtddInscricoes++;
            if (classeInscricao3 != 0) qtddInscricoes++;
            if (classeInscricao4 != 0) qtddInscricoes++;

            if (qtddInscricoes == 1) valorInscricao = (double)torneio.valor;
            if (qtddInscricoes == 2) valorInscricao = (double)torneio.valor2;
            if (qtddInscricoes == 3) valorInscricao = (double)torneio.valor3;
            if (qtddInscricoes == 4) valorInscricao = (double)torneio.valor4;
            if ((isSocio) && (torneio.valorSocio != null))
            {
                valorInscricaoSocio = valorInscricao - (double)torneio.valorSocio;
                if (valorInscricaoSocio < 0) valorInscricaoSocio = 0;
            }
            if ((isFederado) && ((torneio.valorDescontoFederado != null) && (torneio.valorDescontoFederado > 0)))
            {
                valorInscricaoFederado = valorInscricao - (double)torneio.valorDescontoFederado;
                if (valorInscricaoFederado < 0) valorInscricaoFederado = 0;
                if ((isSocio) && (valorInscricaoSocio < valorInscricaoFederado))
                {
                    return valorInscricaoSocio;
                }
                else
                {
                    return valorInscricaoFederado;
                }
            }
            else if (isSocio)
            {
                return valorInscricaoSocio;
            }
            return valorInscricao;


        }

        [Authorize(Roles = "admin, organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult Ativar(int inscricaoId)
        {
            var inscricao = db.InscricaoTorneio.Find(inscricaoId);
            try
            {
                if (inscricao.isAtivo)
                {
                    inscricao.isAtivo = false;
                }
                else
                {
                    inscricao.isAtivo = true;
                }
                db.SaveChanges();
                return RedirectToAction("InscricoesTorneio", new { torneioId = inscricao.torneioId, Msg = "OK" });
            }
            catch (Exception ex)
            {
                return RedirectToAction("InscricoesTorneio", new { torneioId = inscricao.torneioId, Msg = ex.Message });
            }
        }


        [Authorize(Roles = "admin, organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Torneio torneio)
        {
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            if (ModelState.IsValid)
            {
                if (torneio.divulgacao == null) torneio.divulgacao = "";
                db.Entry(torneio).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            var barragemId = 0;
            if (perfil.Equals("admin"))
            {
                barragemId = torneio.barragemId;
            }
            else
            {
                barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
            }
            ViewBag.barraId = barragemId;
            ViewBag.barragemId = new SelectList(db.BarragemView, "Id", "nome", barragemId);
            return View(torneio);
        }


        [Authorize(Roles = "admin, organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditTorneio(Torneio torneio, bool transferencia = false)
        {
            torneio.inscricaoSoPeloSite = true;
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            torneio.liberarEscolhaDuplas = true;
            torneio.isAtivo = true;
            torneio.divulgaCidade = false;
            torneio.isOpen = false;
            if (transferencia == false)
            {
                torneio.dadosBancarios = "";
            }
            if (torneio.divulgacao == "nao divulgar")
            {
                torneio.isAtivo = false;
            }
            if (torneio.divulgacao == "cidade")
            {
                torneio.divulgaCidade = true;
            }
            if (torneio.divulgacao == "brasil")
            {
                torneio.isOpen = true;
            }
            if (ModelState.IsValid)
            {
                if (torneio.divulgacao == null) torneio.divulgacao = "";

                IList<int> ligas = torneio.liga;
                var ligasTorneios = db.TorneioLiga.Where(t => t.TorneioId == torneio.Id).ToList();
                foreach (var item in ligasTorneios)
                {
                    db.TorneioLiga.Remove(item);
                    db.SaveChanges();
                }
                if (ligas != null)
                {
                    foreach (int idLiga in ligas)
                    {
                        TorneioLiga tl = new TorneioLiga
                        {
                            LigaId = idLiga,
                            TorneioId = torneio.Id
                        };
                        db.TorneioLiga.Add(tl);
                        db.SaveChanges();
                        //pesquisa o tipo de torneio
                        BarragemLiga barraliga = db.BarragemLiga.Where(bl => bl.LigaId == idLiga && bl.BarragemId == torneio.barragemId).Single();
                        if (String.IsNullOrEmpty(torneio.TipoTorneio))
                        {
                            torneio.TipoTorneio = barraliga.TipoTorneio;
                        }
                        
                    }
                }
                db.Entry(torneio).State = EntityState.Modified;
                db.SaveChanges();
                mensagem("OK");
                //return RedirectToAction("Index");
            }
            var barragemId = 0;
            if (perfil.Equals("admin"))
            {
                barragemId = torneio.barragemId;
            }
            else
            {
                barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
            }
            ViewBag.barraId = barragemId;
            ViewBag.barragemId = new SelectList(db.BarragemView, "Id", "nome", barragemId);
            List<BarragemLiga> ligasDoRanking = db.BarragemLiga.Include(l => l.Liga).Where(bl => bl.BarragemId == barragemId && bl.Liga.isAtivo).ToList();
            //List<Liga> ligasDisponiveis = new List<Liga>();
            //foreach (BarragemLiga bl in ligasDoRanking)
            //{
            //ligasDisponiveis.Add(db.Liga.Find(bl.LigaId));
            //}
            ViewBag.LigasDisponiveis = ligasDoRanking;
            List<TorneioLiga> lts = db.TorneioLiga.Include(l => l.Liga).Where(tl => tl.TorneioId == torneio.Id).ToList();
            List<Liga> ligasDoTorneio = new List<Liga>();
            foreach (TorneioLiga tl in lts)
            {
                ligasDoTorneio.Add(db.Liga.Find(tl.LigaId));
            }
            ViewBag.LigasDoTorneio = ligasDoTorneio;
            torneio.barragem = db.BarragemView.Find(torneio.barragemId);
            ViewBag.isModeloTodosContraTodos = torneio.barragem.isModeloTodosContraTodos;
            ViewBag.TorneioId = torneio.Id;
            ViewBag.CobrancaTorneio = getDadosDeCobrancaTorneio(torneio.Id);
            ViewBag.flag = "edit";
            ViewBag.LinkParaCopia = "https://" + HttpContext.Request.Url.Host + "/torneio-" + torneio.Id;
            return View(torneio);
        }

        [Authorize(Roles = "admin, organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult Create()
        {
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            var barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
            ViewBag.barraId = barragemId;
            ViewBag.barragemId = new SelectList(db.BarragemView, "Id", "nome");

            List<BarragemLiga> ligasDoRanking = db.BarragemLiga.Include(l => l.Liga).Where(bl => bl.BarragemId == barragemId).ToList();
            List<Liga> ligasDisponiveis = new List<Liga>();
            foreach (BarragemLiga bl in ligasDoRanking)
            {
                ligasDisponiveis.Add(db.Liga.Find(bl.LigaId));
            }
            ViewBag.LigasDisponiveis = ligasDisponiveis;
            if (ligasDisponiveis.Count > 0)
                ViewBag.mostraLigas = true;

            return View();
        }

        //
        // POST: /Rodada/Create

        [Authorize(Roles = "admin, organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Torneio torneio)
        {
            var barragem = db.BarragemView.Find(torneio.barragemId);

            if ((ModelState.IsValid) && (barragem.isAtiva))
            {
                db.Torneio.Add(torneio);
                db.SaveChanges();
                IList<int> ligas = torneio.liga;
                if (ligas != null)
                {
                    foreach (int idLiga in ligas)
                    {
                        TorneioLiga tl = new TorneioLiga
                        {
                            LigaId = idLiga,
                            TorneioId = torneio.Id
                        };
                        db.TorneioLiga.Add(tl);
                        db.SaveChanges();
                        //pesquisa o tipo de torneio
                        BarragemLiga barraliga = db.BarragemLiga.Where(bl => bl.LigaId == idLiga && bl.BarragemId == torneio.barragemId).Single();
                        torneio.TipoTorneio = barraliga.TipoTorneio;
                        db.SaveChanges();
                    }
                    int i = 1;
                    ClasseTorneio classe = null;
                    foreach (ClasseLiga cl in db.ClasseLiga.Where(classeLiga => ligas.Contains(classeLiga.LigaId)).GroupBy(cl => cl.CategoriaId).Select(c => c.FirstOrDefault()).ToList())
                    {
                        classe = new ClasseTorneio
                        {
                            nivel = i,
                            nome = cl.Nome,
                            categoriaId = cl.CategoriaId,
                            torneioId = torneio.Id,
                            isPrimeiraOpcao = true,
                            isSegundaOpcao = false,
                            isDupla = false
                        };
                        i++;
                        db.ClasseTorneio.Add(classe);
                        db.SaveChanges();
                    }
                }
                else
                {
                    ClasseTorneio classe = null;
                    for (int i = 1; i <= torneio.qtddClasses; i++)
                    {
                        classe = new ClasseTorneio();
                        classe.nivel = i;
                        classe.nome = i + "ª Classe";
                        classe.torneioId = torneio.Id;
                        classe.isPrimeiraOpcao = true;
                        classe.isSegundaOpcao = false;
                        classe.isDupla = false;
                        db.ClasseTorneio.Add(classe);
                        db.SaveChanges();
                    }
                }
                return RedirectToAction("Index");
            }
            else
            {
                var userId = WebSecurity.GetUserId(User.Identity.Name);
                var barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
                ViewBag.barraId = barragemId;
                ViewBag.barragemId = new SelectList(db.BarragemView, "Id", "nome");

                List<BarragemLiga> ligasDoRanking = db.BarragemLiga.Include(l => l.Liga).Where(bl => bl.BarragemId == barragemId).ToList();
                List<Liga> ligasDisponiveis = new List<Liga>();
                foreach (BarragemLiga bl in ligasDoRanking)
                {
                    ligasDisponiveis.Add(db.Liga.Find(bl.LigaId));
                }
                ViewBag.LigasDisponiveis = ligasDisponiveis;
                if (ligasDisponiveis.Count > 0)
                    ViewBag.mostraLigas = true;
            }

            return View(torneio);
        }

        private int getQtddRodada(int qtddJogador)
        {
            if (qtddJogador == 2)
            {
                return 1;
            }
            else if (qtddJogador >= 3 && qtddJogador <= 4)
            {
                return 2;
            }
            else if (qtddJogador >= 5 && qtddJogador <= 8)
            {
                return 3;
            }
            else if (qtddJogador >= 9 && qtddJogador <= 16)
            {
                return 4;
            }
            else if (qtddJogador >= 17 && qtddJogador <= 32)
            {
                return 5;
            }
            else if (qtddJogador >= 33 && qtddJogador <= 64)
            {
                return 6;
            }
            else if (qtddJogador > 64)
            {
                return 7;
            }

            return 0;
        }

        private int getQtddByes(int qtddJogadores)
        {
            if (qtddJogadores >= 3 && qtddJogadores <= 4)
            {
                return 4 - qtddJogadores;
            }
            else if (qtddJogadores >= 5 && qtddJogadores <= 8)
            {
                return 8 - qtddJogadores;
            }
            else if (qtddJogadores >= 9 && qtddJogadores <= 16)
            {
                return 16 - qtddJogadores;
            }
            else if (qtddJogadores >= 17 && qtddJogadores <= 32)
            {
                return 32 - qtddJogadores;
            }
            else if (qtddJogadores >= 33 && qtddJogadores <= 64)
            {
                return 64 - qtddJogadores;
            }
            return 0;
        }

        private string getDescricaoRodada(int rodada)
        {
            if (rodada == 1)
            {
                return "Final";
            }
            else if (rodada == 2)
            {
                return "Semi-Final";
            }
            else if (rodada == 3)
            {
                return "Quartas-Final";
            }
            else if (rodada == 4)
            {
                return "Oitavas-Final";
            }
            else if (rodada == 5)
            {
                return "2ª Rodada";
            }
            else if (rodada == 6)
            {
                return "1ª Rodada";
            }
            else if (rodada == 101)
            {
                return "1ª Rodada Repescagem";
            }
            else if (rodada == 100)
            {
                return "2ª Rodada Repescagem";
            }
            return "";
        }

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult EditJogos(int torneioId, int fClasse = 0, string fData = "", string fNomeJogador = "", string fGrupo = "0", int fase = 0, int qtddInscritos = 0, int valorASerPago = 0, int valorDescontoParaRanking = 0)
        {
            if (qtddInscritos > 0 && valorASerPago > 0)
            {
                var cobrancaTorneio = new CobrancaTorneio();
                cobrancaTorneio.qtddInscritos = qtddInscritos;
                cobrancaTorneio.valorASerPago = valorASerPago;
                cobrancaTorneio.valorDescontoParaRanking = valorDescontoParaRanking;
                
                try
                {
                    cobrancaTorneio.qrCode = GetQrCodeCobrancaPIX(torneioId);
                }
                catch (Exception e)
                {
                    cobrancaTorneio.qrCode = new QrCodeCobrancaTorneio();
                    cobrancaTorneio.qrCode.erroGerarQrCode = "Erro ao gerar o QrCode de pagamento. Favor tente novamente mais tarde:" + e.Message;
                }
                
                ViewBag.CobrancaTorneio = cobrancaTorneio;
            }
            List<Jogo> listaJogos = null;
            var classes = db.ClasseTorneio.Where(i => i.torneioId == torneioId).OrderBy(c => c.Id).ToList();
            var classesGeradas = db.Jogo.Where(i => i.torneioId == torneioId).Select(i => (int)i.classeTorneio)
                .Distinct().ToList();
            ViewBag.classesFaseGrupoNaoFinalizadas = db.Jogo.Where(i => i.torneioId == torneioId && i.grupoFaseGrupo != null && (i.situacao_Id == 1 || i.situacao_Id == 2)).
                Select(i => (int)i.classeTorneio).Distinct().ToList();
            ViewBag.ClassesGeradasMataMata = db.Jogo.Where(i => i.torneioId == torneioId && i.grupoFaseGrupo == null).Select(i => (int)i.classeTorneio)
                .Distinct().ToList();
            ViewBag.ClassesGeradas = classesGeradas;
            ViewBag.Classes = classes;
            ViewBag.fClasse = fClasse;
            ViewBag.fData = fData;
            ViewBag.fNomeJogador = fNomeJogador;
            ViewBag.fGrupo = fGrupo;
            ViewBag.fase = fase;
            if (fClasse == 0)
            {
                fClasse = classes[0].Id;
                ViewBag.filtroClasse = fClasse;
            }
            if (fClasse != 1)
            {
                ViewBag.permitirEdicaoDeJogador = true;
                ViewBag.primeirafase = db.Jogo.Where(r => r.torneioId == torneioId && r.classeTorneio == fClasse).Max(r => r.faseTorneio);
            }
            var jogo = db.Jogo.Where(i => i.torneioId == torneioId);
            listaJogos = filtrarJogos(jogo, fClasse, fData, fGrupo, fase, false, fNomeJogador);
            if (fClasse != 1)
            {
                var cl = classes.Where(c=>c.Id==fClasse).First();
                if (cl.isDupla)
                {
                    ViewBag.Inscritos = db.InscricaoTorneio.Where(c => c.torneioId == torneioId && c.isAtivo && c.parceiroDuplaId != null && c.classe==fClasse).ToList();
                } else {
                    ViewBag.Inscritos = db.InscricaoTorneio.Where(c => c.torneioId == torneioId && c.isAtivo && c.classe == fClasse).ToList();
                }
                
            }

            ViewBag.TorneioId = torneioId;
            ViewBag.flag = "jogos";
            return View(listaJogos);
        }

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult ImprimirJogos(int torneioId, int fClasse = 0, string fData = "", string fNomeJogador = "", string fGrupo = "0", int fase = 0)
        {
            List<Jogo> listaJogos = null;
            var torneio = db.Torneio.Find(torneioId);
            ViewBag.nomeTorneio = torneio.nome;
            var jogo = db.Jogo.Where(i => i.torneioId == torneioId);
            listaJogos = filtrarJogos(jogo, fClasse, fData, fGrupo, fase, true, fNomeJogador);
            return View(listaJogos);
        }

        private List<Jogo> filtrarJogos(IQueryable<Jogo> jogos, int classe, string data, string grupo, int fase, Boolean isImprimir = false, string nomeJogador = "")
        {
            ViewBag.fClasse = classe;
            ViewBag.fData = data;
            ViewBag.fGrupo = grupo;
            if (classe != 1)
            {
                jogos = jogos.Where(j => j.classeTorneio == classe);
            }
            if (!string.IsNullOrEmpty(data))
            {
                var dataJogo = Convert.ToDateTime(data);
                jogos = jogos.Where(j => j.dataJogo == dataJogo);
            }
            if (grupo != "0")
            {
                var numeroGrupo = Convert.ToInt32(grupo);
                jogos = jogos.Where(j => j.grupoFaseGrupo ==  numeroGrupo);
            }
            if ((fase != 0) && (fase != 700)) // 700 = fase de grupo
            {
                jogos = jogos.Where(j => j.faseTorneio == fase);
            }
            else if (fase == 700)
            {
                jogos = jogos.Where(j => j.grupoFaseGrupo != null);
            }
            if (nomeJogador != "")
            {
                jogos = jogos.Where(j => j.desafiante.nome.ToUpper().Contains(nomeJogador.ToUpper()) || j.desafiado.nome.ToUpper().Contains(nomeJogador.ToUpper()));
            }

            if (isImprimir)
            {
                return jogos.OrderBy(r => r.dataJogo).ThenBy(r => r.horaJogo).ToList();
            }
            else
            {
                var jogosFaseGrupo = jogos.Where(j => j.rodadaFaseGrupo != 0).OrderBy(r => r.rodadaFaseGrupo).ThenBy(j => j.grupoFaseGrupo).ToList();
                var jogosMataMata = jogos.Where(j => j.rodadaFaseGrupo == 0).OrderByDescending(r => r.faseTorneio).ThenBy(r => r.ordemJogo).ToList();
                jogosFaseGrupo.AddRange(jogosMataMata);
                return jogosFaseGrupo;
            }

        }
        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        [HttpPost]
        public ActionResult EditJogos(int Id, int jogador1, int jogador2, string dataJogo = "", string horaJogo = "", string quadra = "0")
        {
            try
            {
                var jogo = db.Jogo.Find(Id);
                var alterarJogosFaseGrupo = false;
                var substituirEsteDesafiante = 0;
                var porEsteDesafiante = 0;
                var substituirEsteDesafiado = 0;
                var porEsteDesafiado = 0;
                var porEsteDesafiante2 = 0;
                var porEsteDesafiado2 = 0;
                if (jogo.classe.isDupla)
                {
                    var dupla = db.InscricaoTorneio.Where(i => i.userId == jogador1 && i.classe == jogo.classeTorneio && i.parceiroDuplaId != null).ToList();
                    if (dupla.Count() > 0){
                        porEsteDesafiante2 = (int)dupla[0].parceiroDuplaId;
                        jogo.desafiante2_id = dupla[0].parceiroDuplaId;
                    }
                    var dupla2 = db.InscricaoTorneio.Where(i => i.userId == jogador2 && i.classe == jogo.classeTorneio && i.parceiroDuplaId != null).ToList();
                    if (dupla2.Count() > 0){
                        porEsteDesafiado2 = (int)dupla2[0].parceiroDuplaId;
                        jogo.desafiado2_id = dupla2[0].parceiroDuplaId;
                    }
                }
                if((jogo.grupoFaseGrupo!=null && jogo.grupoFaseGrupo > 0) && (jogo.desafiante_id != jogador1 || jogo.desafiado_id != jogador2)){
                    alterarJogosFaseGrupo = true;
                    if (jogo.desafiante_id != jogador1){
                        // não permitir trocar se o jogador já pertence a este grupo
                        var inscricao = db.InscricaoTorneio.Where(i => i.userId == jogador1 && i.classe == jogo.classeTorneio).ToList();
                        if(inscricao.Count()>0 && inscricao[0].grupo == jogo.grupoFaseGrupo)
                        {
                            return Json(new { erro = "O jogador: " + inscricao[0].participante.nome + " já está em outros jogos neste grupo. Não é possível acrescentá-lo em mais jogos.", retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
                        }
                        substituirEsteDesafiante = (int)jogo.desafiante_id;
                        porEsteDesafiante = jogador1;
                        if (substituirEsteDesafiante == 10)
                        {
                            jogo.situacao_Id = 1;
                            jogo.qtddGames1setDesafiado = 0;
                            jogo.qtddGames2setDesafiado = 0;
                            jogo.qtddGames1setDesafiante = 0;
                            jogo.qtddGames2setDesafiante = 0;

                        }
                    }
                    if (jogo.desafiado_id != jogador2){
                        // não permitir trocar se o jogador já pertence a este grupo
                        var inscricao = db.InscricaoTorneio.Where(i => i.userId == jogador2 && i.classe == jogo.classeTorneio).ToList();
                        if (inscricao.Count() > 0 && inscricao[0].grupo == jogo.grupoFaseGrupo)
                        {
                            return Json(new { erro = "O jogador: " + inscricao[0].participante.nome + " já está em outros jogos neste grupo. Não é possível acrescentá-lo em mais jogos.", retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
                        }
                        substituirEsteDesafiado = (int)jogo.desafiado_id;
                        porEsteDesafiado = jogador2;
                    }
                    if ((porEsteDesafiante == 10) || (porEsteDesafiado == 10))
                    {
                        if (verificarSeJaTemByeNoGrupo((int)jogo.classeTorneio, (int)jogo.grupoFaseGrupo)) {
                            return Json(new { erro = "Não é possível incluir bye neste grupo pois já existe um bye neste grupo.", retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
                        }
                        jogo.situacao_Id = 5;
                        jogo.qtddGames1setDesafiado = 6;
                        jogo.qtddGames2setDesafiado = 6;
                        jogo.qtddGames1setDesafiante = 1;
                        jogo.qtddGames2setDesafiante = 1;
                    }

                }
                if (porEsteDesafiado == 10){
                    jogo.desafiado_id = jogo.desafiante_id;
                    jogo.desafiado2_id = jogo.desafiante2_id;
                    jogo.desafiante_id = 10;
                    jogo.desafiante2_id = null;
                } else {
                    jogo.desafiante_id = jogador1;
                    jogo.desafiado_id = jogador2;
                }
                if (dataJogo != "")
                {
                    jogo.dataJogo = Convert.ToDateTime(dataJogo);
                    if (jogo.situacao_Id != 4 && jogo.situacao_Id != 5)
                    {
                        jogo.situacao_Id = 2;
                    }
                }
                
                jogo.horaJogo = horaJogo;
                jogo.quadra = quadra;
                db.Entry(jogo).State = EntityState.Modified;
                db.SaveChanges();
                if (jogo.desafiante_id == 10)
                {
                    tn.MontarProximoJogoTorneio(jogo);
                }

                if (alterarJogosFaseGrupo)
                {
                    substituirJogadorFaseGrupo((int)jogo.grupoFaseGrupo, (int)jogo.classeTorneio, substituirEsteDesafiante, porEsteDesafiante, porEsteDesafiante2);
                    substituirJogadorFaseGrupo((int)jogo.grupoFaseGrupo, (int)jogo.classeTorneio, substituirEsteDesafiado, porEsteDesafiado, porEsteDesafiado2);
                    return Json(new { erro = "", retorno = 2 }, "text/plain", JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
                }

                
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        private bool verificarSeJaTemByeNoGrupo(int classeId, int grupo) {
            var jaTemByeNesteGrupo = db.Jogo.Where(j => j.desafiante_id == 10 && j.classeTorneio == classeId && j.grupoFaseGrupo == grupo).Any();
            return jaTemByeNesteGrupo;
        }

        private void substituirJogadorFaseGrupo(int grupo, int classeId, int substituirEsteJogador, int porEsteJogador, int porEsteParceiroDupla)
        {
            if (substituirEsteJogador > 0)
            {
                var listaJogos = db.Jogo.Where(j => j.grupoFaseGrupo == grupo && j.classeTorneio == classeId && (j.desafiante_id == substituirEsteJogador || j.desafiado_id == substituirEsteJogador)).ToList();
                foreach (var jogo in listaJogos)
                {
                    if (substituirEsteJogador == jogo.desafiante_id)
                    {
                        jogo.desafiante_id = porEsteJogador;
                        if (porEsteParceiroDupla > 0)
                        {
                            jogo.desafiante2_id = porEsteParceiroDupla;
                        }
                    }
                    else if (substituirEsteJogador == jogo.desafiado_id)
                    {
                        if (porEsteJogador == 10)
                        {
                            jogo.desafiado_id = jogo.desafiante_id;
                            jogo.desafiante_id = porEsteJogador;
                            if (jogo.desafiante2_id != null)
                            {
                                jogo.desafiado2_id = jogo.desafiante2_id;
                                jogo.desafiante2_id = null;
                            }
                            jogo.situacao_Id = 5;
                            jogo.qtddGames1setDesafiado = 6;
                            jogo.qtddGames2setDesafiado = 6;
                            jogo.qtddGames1setDesafiante = 1;
                            jogo.qtddGames2setDesafiante = 1;
                        }
                        else
                        {
                            jogo.desafiado_id = porEsteJogador;
                            if (porEsteParceiroDupla > 0)
                            {
                                jogo.desafiado2_id = porEsteParceiroDupla;
                            }
                        }
                        
                    }
                    if (substituirEsteJogador == 10)
                    {
                        jogo.situacao_Id = 1;
                        jogo.qtddGames1setDesafiado = 0;
                        jogo.qtddGames2setDesafiado = 0;
                        jogo.qtddGames1setDesafiante = 0;
                        jogo.qtddGames2setDesafiante = 0;

                    }
                    if (!(jogo.desafiante_id==10 && jogo.desafiado_id == 10)){
                        db.Entry(jogo).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                    
                }
                try
                {
                    if (substituirEsteJogador != 10)
                    {
                        var inscricao = db.InscricaoTorneio.Where(i => i.classe == classeId && i.userId == substituirEsteJogador).First();
                        if (inscricao.grupo == grupo) {
                            inscricao.grupo = 0;
                        }
                        db.Entry(inscricao).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
                catch (Exception e) { }

                try
                {
                    var inscricao = db.InscricaoTorneio.Where(i => i.classe == classeId && i.userId == porEsteJogador).First();
                    inscricao.grupo = grupo;
                    db.Entry(inscricao).State = EntityState.Modified;
                    db.SaveChanges();
                }
                catch (Exception e) { }
            }
        }

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult EditDuplas(int torneioId, int filtroClasse = 0, bool naoFazNada = false)
        {
            List<InscricaoTorneio> duplas = null;
            var classes = db.ClasseTorneio.Where(i => i.torneioId == torneioId && i.isDupla).OrderBy(c => c.Id).ToList();
            ViewBag.Classes = classes;
            ViewBag.TorneioId = torneioId;
            ViewBag.flag = "duplas";
            if (classes.Count == 0)
            {
                return View(duplas);
            }
            ViewBag.filtroClasse = filtroClasse;
            if (filtroClasse == 0)
            {
                filtroClasse = classes[0].Id;
                ViewBag.filtroClasse = filtroClasse;
            }
            duplas = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.classe == filtroClasse).
                OrderByDescending(i => i.parceiroDuplaId).ToList();
            ////////////////////////////////

            var duplasNaoFormadas = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.classe == filtroClasse && i.parceiroDuplaId == null).
                OrderByDescending(i => i.participante.nome).ToList();

            List<InscricaoTorneio> inscricoesRemove = new List<InscricaoTorneio>();
            var duplasFormadas = duplas.Where(d => d.parceiroDuplaId != null).ToList();
            foreach (var ins in duplasFormadas)
            {
                try
                {
                    var formouDupla = duplasNaoFormadas.Where(i => i.userId == ins.parceiroDuplaId).First();
                    duplasNaoFormadas.Remove(formouDupla);
                }
                catch (Exception e)
                {

                }
            }
            ViewBag.InscricaoSemDupla = duplasNaoFormadas;
            ////////////////////////////////
            ViewBag.Inscritos = db.InscricaoTorneio.Where(c => c.torneioId == torneioId && c.classe == filtroClasse).ToList();
            return View(duplas);
        }

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult EditFaseGrupo(int torneioId, int filtroClasse = 0)
        {
            List<InscricaoTorneio> inscritos = null;
            var classes = db.ClasseTorneio.Where(i => i.torneioId == torneioId && i.faseGrupo).OrderBy(c => c.nome).ToList();
            ViewBag.Classes = classes;
            ViewBag.TorneioId = torneioId;
            ViewBag.flag = "fasegrupo";
            if (classes.Count == 0)
            {
                return View(inscritos);
            }
            ViewBag.filtroClasse = filtroClasse;
            if (filtroClasse == 0)
            {
                filtroClasse = classes[0].Id;
                ViewBag.filtroClasse = filtroClasse;
            }
            var classe = db.ClasseTorneio.Find(filtroClasse);
            inscritos = tn.getInscritosPorClasse(classe).OrderBy(i => i.grupo).ToList();
            ViewBag.Inscritos = db.InscricaoTorneio.Where(c => c.torneioId == torneioId && c.classe == filtroClasse).ToList();

            if (verificarSeAFaseDeGrupoFoiFinalizada(classe))
            {
                ViewBag.Classificados = tn.getClassificadosEmCadaGrupo(classe);
            }
            return View(inscritos);
        }

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        [HttpPost]
        public ActionResult IncluirJogadorGrupo(int inscricaoJogador, int? grupo)
        {
            try
            {
                var inscricao = db.InscricaoTorneio.Find(inscricaoJogador);
                inscricao.grupo = grupo;
                db.Entry(inscricao).State = EntityState.Modified;
                db.SaveChanges();
                return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        [HttpPost]
        public ActionResult EditDuplas(int inscricaoJogador1, int? jogador2)
        {
            try
            {
                var inscricao = db.InscricaoTorneio.Find(inscricaoJogador1);
                inscricao.parceiroDuplaId = jogador2;
                db.Entry(inscricao).State = EntityState.Modified;
                db.SaveChanges();
                if (jogador2 == null)
                {
                    return RedirectToAction("EditDuplas", "Torneio", new { torneioId = inscricao.torneioId, filtroClasse = inscricao.classe });
                } else {
                    var jogos = db.Jogo.Where(j => j.classeTorneio == inscricao.classe && (j.desafiado_id == inscricao.userId || j.desafiante_id == inscricao.userId)).ToList();
                    foreach (var item in jogos)
                    {
                        if (item.desafiante_id == inscricao.userId)
                        {
                            item.desafiante2_id = jogador2;
                        } else {
                            item.desafiado2_id = jogador2;
                        }
                        db.Entry(item).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
                return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        //[Authorize(Roles = "admin,usuario,organizador")]
        [HttpPost]
        public ActionResult EscolherDupla(int inscricaoJogador, int torneioId, int classe = 0, int userId = 0)
        {
            try
            {
                var inscricao = db.InscricaoTorneio.Find(inscricaoJogador);
                string perfil = "";
                try
                {
                    perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
                    if (perfil.Equals("usuario") || userId == 0)
                    {
                        userId = WebSecurity.GetUserId(User.Identity.Name);
                    }
                }
                catch (Exception e) { }
                var validar = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.classe == classe && ((i.userId == userId && i.parceiroDuplaId != null) || (i.parceiroDuplaId == userId))).Count();
                if (validar == 0)
                {
                    inscricao.parceiroDuplaId = userId;
                    db.Entry(inscricao).State = EntityState.Modified;
                    db.SaveChanges();
                    return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { erro = "Você já possui uma dupla. Para trocar de dupla, favor entrar em contato com o organizador do torneio.", retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }


        [Authorize(Roles = "admin,usuario,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult LancarResultado(int id = 0, int barragem = 0, string msg = "")
        {
            Jogo jogo = null;
            var usuario = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
            InscricaoTorneio inscricao = null;
            if (id == 0)
            {
                try
                {
                    HttpCookie cookie = Request.Cookies["_barragemId"];
                    var barragemId = 0;
                    if (cookie != null)
                    {
                        barragemId = Convert.ToInt32(cookie.Value.ToString());
                    }
                    else if (barragem != 0)
                    {
                        BarragemView b = db.BarragemView.Find(barragem);
                        barragemId = barragem;
                        Funcoes.CriarCookieBarragem(Response, Server, b.Id, b.nome);
                    }
                    var agora = DateTime.Now;

                    if (barragemId > 0)
                    {
                        inscricao = db.InscricaoTorneio.Where(i => i.participante.UserId == usuario.UserId && i.isAtivo && i.torneio.dataFimInscricoes < agora && i.torneio.barragemId == barragemId).OrderByDescending(i => i.Id).Take(1).Single();
                    }
                    else
                    {
                        inscricao = db.InscricaoTorneio.Where(i => i.participante.UserId == usuario.UserId && i.isAtivo && i.torneio.dataFimInscricoes < agora).OrderByDescending(i => i.Id).Take(1).Single();
                    }
                    ViewBag.NomeTorneio = inscricao.torneio.nome;
                    jogo = db.Jogo.Where(u => (u.desafiado_id == usuario.UserId || u.desafiante_id == usuario.UserId) && u.torneioId == inscricao.torneioId)
                        .OrderBy(u => u.situacao_Id).ThenBy(u => u.faseTorneio).Take(1).Single();
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
                if (!perfil.Equals("admin") && !perfil.Equals("organizador") && (jogo.gamesJogados != 0))
                {
                    ViewBag.Editar = false;
                }
                else
                {
                    ViewBag.Editar = true;
                }
                ViewBag.situacao_Id = new SelectList(db.SituacaoJogo, "Id", "descricao", jogo.situacao_Id);
                // jogos pendentes
                var jogosPendentes = db.Jogo.Where(u => (u.desafiado_id == usuario.UserId || u.desafiante_id == usuario.UserId) && u.torneioId == jogo.torneioId
                    && (u.situacao_Id != 4 && u.situacao_Id != 5)).OrderByDescending(u => u.Id).Take(3).ToList();
                if (jogosPendentes.Count() > 1)
                {
                    ViewBag.JogosPendentes = jogosPendentes;
                }
                // últimos jogos já finalizados
                var ultimosJogosFinalizados = db.Jogo.Where(u => (u.desafiado_id == usuario.UserId || u.desafiante_id == usuario.UserId) && u.torneioId == jogo.torneioId
                    && (u.situacao_Id == 4 || u.situacao_Id == 5)).OrderByDescending(u => u.Id).Take(5).ToList();
                ViewBag.JogosFinalizados = ultimosJogosFinalizados;
            }

            mensagem(msg);
            return View(jogo);
        }


        [HttpPost]
        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult LancarResultado(Jogo j)
        {
            try
            {
                Jogo jogo = db.Jogo.Find(j.Id);

                jogo.situacao_Id = j.situacao_Id;
                if (((j.situacao_Id == 1) || (j.situacao_Id == 2)) && ((j.qtddGames1setDesafiado != 0) || (j.qtddGames1setDesafiante != 0)))
                {
                    jogo.situacao_Id = 4;
                }
                int setDesafiante = 0;
                int setDesafiado = 0;
                jogo.qtddGames1setDesafiado = j.qtddGames1setDesafiado;
                jogo.qtddGames2setDesafiado = j.qtddGames2setDesafiado;
                jogo.qtddGames3setDesafiado = j.qtddGames3setDesafiado;
                jogo.qtddGames1setDesafiante = j.qtddGames1setDesafiante;
                jogo.qtddGames2setDesafiante = j.qtddGames2setDesafiante;
                jogo.qtddGames3setDesafiante = j.qtddGames3setDesafiante;
                if (jogo.qtddSetsGanhosDesafiado == jogo.qtddSetsGanhosDesafiante && jogo.situacao_Id != 1 && jogo.situacao_Id != 2)
                {
                    return Json(new { erro = "Placar Inválido. Os sets ganhos estão iguais.", retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
                }
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
                }
                else
                {
                    //Mensagem = "Não foi possível alterar os dados.";
                    return Json(new { erro = "Dados Inválidos.", retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
                }

                //jogo = db.Jogo.Include(j => j.desafiado).Include(j => j.desafiante).Where(j => j.Id == jogo.Id).Single();
                //ViewBag.situacao_Id = new SelectList(db.SituacaoJogo, "Id", "descricao", jogo.situacao_Id);
                //calcula os pontos, posicao e monta proximo jogo
                tn.MontarProximoJogoTorneio(jogo);

                tn.consolidarPontuacaoFaseGrupo(jogo);
                var ligaConsolidadaComSucesso = 0;
                if (jogo.faseTorneio == 1)
                {
                    try
                    {
                        var liga = db.TorneioLiga.Where(t => t.TorneioId == jogo.torneioId).First();
                        var snapshot = db.Snapshot.Where(s => s.LigaId == liga.LigaId && s.Id == liga.snapshotId).Count();
                        if (snapshot > 0)
                        {
                            ligaConsolidadaComSucesso = 1;
                        }
                    }
                    catch (Exception e) { }
                }

                return Json(new { erro = "", retorno = 1, pontuacaoLiga = ligaConsolidadaComSucesso }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }

            //return RedirectToAction("LancarResultado", "Torneio", new { torneioId = jogo.Id, msg = Mensagem });
        }
        //private void GravarPontuacaoFaseGrupo(Jogo jogo, int idVencedorAnterior = 0)
        //{
        //    var torneioId = jogo.torneioId;
        //    var classeId = jogo.classeTorneio;
        //    var userId = jogo.idDoVencedor;
        //    var inscricao = new InscricaoTorneio();
        //    if (jogo.situacao_Id != 2)
        //    {
        //        if((jogo.situacao_Id == 1)&&(idVencedorAnterior != 0)) {
        //            inscricao = db.InscricaoTorneio.Where(it => it.torneioId == torneioId &&
        //                        it.classe == classeId && it.isAtivo && it.userId == idVencedorAnterior).Single();
        //            inscricao.pontuacaoFaseGrupo--;
        //        } else { 
        //            inscricao = db.InscricaoTorneio.Where(it => it.torneioId == torneioId &&
        //                        it.classe == classeId && it.isAtivo && it.userId == userId).Single();
        //            if (idVencedorAnterior == 0)
        //            {
        //                inscricao.pontuacaoFaseGrupo++;
        //            }
        //            else if (idVencedorAnterior == jogo.idDoPerdedor)
        //            {
        //                inscricao.pontuacaoFaseGrupo++;
        //            }
        //        }

        //        db.Entry(inscricao).State = EntityState.Modified;
        //        db.SaveChanges();
        //        if ((jogo.situacao_Id != 1) && (idVencedorAnterior == jogo.idDoPerdedor)){
        //            var inscPerderdor = db.InscricaoTorneio.Where(it => it.torneioId == torneioId &&
        //                it.classe == classeId && it.isAtivo && it.userId == idVencedorAnterior).Single();
        //            inscPerderdor.pontuacaoFaseGrupo--;
        //            db.Entry(inscPerderdor).State = EntityState.Modified;
        //            db.SaveChanges();
        //        }
        //    }
        //}

        [HttpPost]
        [Authorize(Roles = "admin,usuario,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult LancarWO(int Id, int vencedorWO = 0, int situacao_id = 5)
        {
            try
            {
                if (vencedorWO == 0)
                {
                    return Json(new { erro = "Informe o vencedor do jogo.", retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
                }
                var perderdorWO = 0;
                Jogo jogoAtual = db.Jogo.Find(Id);
                if ((jogoAtual.rodadaFaseGrupo != 0) && (jogoAtual.situacao_Id == 5) && (jogoAtual.idDoVencedor!=vencedorWO))
                {
                    desfazerJogosWOFaseGrupo(jogoAtual.idDoPerdedor, (int)jogoAtual.classeTorneio);
                }
                //alterar quantidade de games para desafiado e desafiante
                int gamesDesafiante = 0;
                int gamesDesafiado = 0;
                if (jogoAtual.desafiado_id == vencedorWO)
                {
                    gamesDesafiado = 6;
                    gamesDesafiante = 1;
                    perderdorWO = jogoAtual.desafiante_id;
                }
                else
                {
                    gamesDesafiado = 1;
                    gamesDesafiante = 6;
                    perderdorWO = jogoAtual.desafiado_id;
                }
                jogoAtual.qtddGames1setDesafiado = gamesDesafiado;
                jogoAtual.qtddGames1setDesafiante = gamesDesafiante;
                jogoAtual.qtddGames2setDesafiado = gamesDesafiado;
                jogoAtual.qtddGames2setDesafiante = gamesDesafiante;
                jogoAtual.qtddGames3setDesafiado = 0;
                jogoAtual.qtddGames3setDesafiante = 0;
                //alterar status do jogo WO
                jogoAtual.situacao_Id = situacao_id;
                jogoAtual.usuarioInformResultado = User.Identity.Name;
                jogoAtual.dataCadastroResultado = DateTime.Now;
                db.Entry(jogoAtual).State = EntityState.Modified;
                db.SaveChanges();
                if ((jogoAtual.rodadaFaseGrupo != 0) && (situacao_id == 5))
                {
                    tratarWOFaseGrupo(perderdorWO, (int)jogoAtual.classeTorneio);
                }
                tn.MontarProximoJogoTorneio(jogoAtual);
                tn.consolidarPontuacaoFaseGrupo(jogoAtual);

                return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }

        }

        private void desfazerJogosWOFaseGrupo(int userId, int classeId)
        {
            var jogos = db.Jogo.Where(j => j.classeTorneio == classeId && j.rodadaFaseGrupo != 0 && (j.desafiado_id == userId || j.desafiante_id == userId)).ToList();
            foreach (var jogo in jogos)
            {
                if (jogo.situacao_Id == 5)
                {
                    jogo.situacao_Id = 1;
                    jogo.usuarioInformResultado = User.Identity.Name;
                    jogo.dataCadastroResultado = DateTime.Now;
                    jogo.qtddGames1setDesafiado = 0;
                    jogo.qtddGames2setDesafiado = 0;
                    jogo.qtddGames3setDesafiado = 0;
                    jogo.qtddGames1setDesafiante = 0;
                    jogo.qtddGames2setDesafiante = 0;
                    jogo.qtddGames3setDesafiante = 0;
                    db.Entry(jogo).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }
            var inscrito = db.InscricaoTorneio.Where(i => i.userId == userId && i.classe == classeId).ToList();
            inscrito[0].pontuacaoFaseGrupo = 0;
            db.Entry(inscrito[0]).State = EntityState.Modified;
            db.SaveChanges();
        }

        private void tratarWOFaseGrupo(int perdedorId, int classeId)
        {
            var jogos = db.Jogo.Where(j => j.classeTorneio == classeId && j.rodadaFaseGrupo != 0 && (j.desafiado_id == perdedorId || j.desafiante_id == perdedorId)).ToList();
            foreach (var jogo in jogos)
            {
                jogo.situacao_Id = 5;
                jogo.usuarioInformResultado = User.Identity.Name;
                jogo.dataCadastroResultado = DateTime.Now;
                db.Entry(jogo).State = EntityState.Modified;
                db.SaveChanges();
            }
            var inscrito = db.InscricaoTorneio.Where(i => i.userId == perdedorId && i.classe == classeId && i.isAtivo).ToList();
            inscrito[0].pontuacaoFaseGrupo = -100;
            db.Entry(inscrito[0]).State = EntityState.Modified;
            db.SaveChanges();
        }

        [Authorize(Roles = "admin, organizador")]
        [HttpPost]
        public ActionResult notificarJogadores(int torneioId, string texto)
        {
            Torneio torneio = db.Torneio.Find(torneioId);
            var retorno = "";
            try
            {
                Mail mail = new Mail();
                mail.de = System.Configuration.ConfigurationManager.AppSettings.Get("UsuarioMail");
                mail.para = "barragemdocerrado@gmail.com";
                mail.assunto = torneio.nome;
                mail.conteudo = texto;
                mail.formato = Tipos.FormatoEmail.Html;
                List<string> bcc = db.InscricaoTorneio.Where(u => u.isAtivo == true && u.torneioId == torneioId).Select(u => u.participante.email).Distinct().ToList();
                mail.bcc = bcc;
                mail.EnviarMail();
                retorno = "Notificação enviada com sucesso.";
            }
            catch (Exception ex)
            {
                retorno = "Houve uma falha no envio. Favor entrar em contato com o administrador do sistema.";
            }
            return RedirectToAction("EditNotificacao", new { torneioId = torneioId, msg = retorno });
        }

        [Authorize(Roles = "admin, organizador")]
        public ActionResult EditNotificacao(int torneioId, string msg = "")
        {
            ViewBag.retorno = msg;
            ViewBag.flag = "notificacao";
            ViewBag.TorneioId = torneioId;
            return View();
        }

        private int getCabecaChaveVencedor(Jogo jogo)
        {
            return 0;
        }

        public ActionResult TabelaImprimir(int torneioId, int fClasse = 0)
        {
            var torneio = db.Torneio.Find(torneioId);
            ViewBag.nomeTorneio = torneio.nome;
            ViewBag.nomeRanking = torneio.barragem.nome;
            ViewBag.idBarragem = torneio.barragemId;
            var jogos = db.Jogo.Where(r => r.torneioId == torneioId && r.classeTorneio == fClasse && r.faseTorneio != 100 && r.faseTorneio != 101 && r.rodadaFaseGrupo == 0).OrderByDescending(r => r.faseTorneio).ThenBy(r => r.ordemJogo).ToList();
            ViewBag.nomeClasse = jogos[0].classe.nome;
            return View(jogos);
        }
        [Authorize(Roles = "admin")]
        public ActionResult ajustarPontuacaoLiga(int torneioId)
        {
            var jogos = db.Jogo.Where(j => j.torneioId == torneioId && j.desafiante_id != 10 && j.faseTorneio != 1).ToList();
            Torneio torneio = db.Torneio.Where(t => t.Id == torneioId).Single();
            foreach (var jogo in jogos)
            {
                var inscricao = db.InscricaoTorneio.Where(i => i.userId == jogo.idDoPerdedor
                && i.torneioId == jogo.torneioId && i.classe == jogo.classeTorneio).ToList();
                if (inscricao.Count() > 0)
                {
                    int pontuacao = CalculadoraDePontos.AddTipoTorneio(torneio.TipoTorneio).CalculaPontos(inscricao[0]);
                    inscricao[0].Pontuacao = pontuacao;
                    db.SaveChanges();
                }
            }

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult CreateTorneio(Torneio torneio, bool transferencia = false, int pontuacaoLiga = 100)
        {
            torneio.inscricaoSoPeloSite = true;
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            var barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
            torneio.barragemId = barragemId;
            var barragem = db.Barragens.Find(torneio.barragemId);
            ViewBag.isModeloTodosContraTodos = barragem.isModeloTodosContraTodos;
            if (transferencia == false)
            {
                torneio.dadosBancarios = "";
            }
            torneio.isAtivo = true;
            torneio.liberarEscolhaDuplas = true;
            torneio.divulgaCidade = false;
            torneio.isOpen = false;
            torneio.TipoTorneio = pontuacaoLiga+"";
            if (torneio.divulgacao == "nao divulgar")
            {
                torneio.isAtivo = false;
            }
            if (torneio.divulgacao == "cidade")
            {
                torneio.divulgaCidade = true;
            }
            if (torneio.divulgacao == "brasil")
            {
                torneio.isOpen = true;
            }
            if (barragem.soTorneio != null && (bool)barragem.soTorneio)
            {
                torneio.regulamento = barragem.regulamento;
            }
            if ((ModelState.IsValid) && (barragem.isAtiva))
            {
                using (System.Transactions.TransactionScope scope = new TransactionScope())
                {
                    db.Torneio.Add(torneio);
                    db.SaveChanges();
                    IList<int> ligas = torneio.liga;
                    int ligaDoRanking = 0;
                    int i = 1;
                    ClasseTorneio classe = null;
                    ClasseLiga classeLiga = null;

                    if (ligas != null)
                    {
                        foreach (int idLiga in ligas)
                        {
                            TorneioLiga tl = new TorneioLiga
                            {
                                LigaId = idLiga,
                                TorneioId = torneio.Id
                            };
                            db.TorneioLiga.Add(tl);
                            db.SaveChanges();
                            // verificar se é o primeiro torneio do circuito. Se for, cadastrar as categorias na liga
                            var torneiosCriados = db.TorneioLiga.Where(t =>t.LigaId==idLiga && t.Liga.barragemId == torneio.barragemId).Count();
                            if (torneiosCriados == 1){
                                ligaDoRanking = idLiga;
                            }
                        }
                    }
                    
                    if (torneio.classes != null)
                    {
                        foreach (int idCategoria in torneio.classes)
                        {
                            var categoria = db.Categoria.Find(idCategoria);
                            classe = new ClasseTorneio
                            {
                                nivel = i,
                                nome = categoria.Nome,
                                categoriaId = idCategoria,
                                torneioId = torneio.Id,
                                isPrimeiraOpcao = true,
                                isSegundaOpcao = true,
                                faseMataMata = true,
                                faseGrupo = barragem.isBeachTenis ? true : false,
                                isDupla = categoria.isDupla
                            };
                            if (ligaDoRanking!=0)
                            {
                                classeLiga = new ClasseLiga {
                                    Nome = categoria.Nome,
                                    CategoriaId = idCategoria,
                                    LigaId = ligaDoRanking
                                };
                                db.ClasseLiga.Add(classeLiga);
                            }
                            i++;
                            db.ClasseTorneio.Add(classe);
                            db.SaveChanges();
                        }
                    }
                    scope.Complete();
                }
                return RedirectToAction("EditClasse", "Torneio", new { torneioId = torneio.Id });
            }
            else
            {
                List<BarragemLiga> ligasDoRanking = db.BarragemLiga.Include(l => l.Liga).Where(bl => bl.BarragemId == barragemId).ToList();
                //List<Liga> ligasDisponiveis = new List<Liga>();
                List<int> ligasId = new List<int>();
                foreach (BarragemLiga bl in ligasDoRanking)
                {
                    //ligasDisponiveis.Add(db.Liga.Find(bl.LigaId));
                    ligasId.Add(bl.LigaId);
                }
                ViewBag.LigasDisponiveis = ligasDoRanking;

                var categorias = db.Categoria.Where(c => c.rankingId == 0 && c.rankingId == barragemId).OrderBy(c => c.ordemExibicao).ToList();
                List<CategoriaDeLiga> categoriasDeLiga = new List<CategoriaDeLiga>();
                var classesLiga = db.ClasseLiga.Include(l => l.Liga).Where(cl => ligasId.Contains(cl.Liga.Id)).ToList();
                foreach (var categoria in categorias)
                {
                    var categoriaDeLiga = new CategoriaDeLiga();
                    categoriaDeLiga.Id = categoria.Id;
                    categoriaDeLiga.Nome = categoria.Nome;
                    categoriaDeLiga.Ligas = new List<String>();
                    foreach (var classeLiga in classesLiga.Where(c => c.CategoriaId == categoria.Id).ToList())
                    {
                        categoriaDeLiga.Ligas.Add(classeLiga.Liga.Nome);
                    }
                    categoriasDeLiga.Add(categoriaDeLiga);
                }
                ViewBag.Categorias = categoriasDeLiga;
            }
            return View();
        }


        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis")]
        public ActionResult IncluirCategoriaNoCircuito(int categoriaId)
        {
            try { 
                var userId = WebSecurity.GetUserId(User.Identity.Name);
                var barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
                Liga circuito = null;
                try
                {
                    circuito = db.Liga.Where(l => l.barragemId == barragemId).OrderByDescending(l => l.Id).Take(1).Single();
                }catch(Exception ex)
                {
                    return Json(new { erro = "Você ainda não possui um circuito próprio.", retorno = 1 }, "application/json", JsonRequestBehavior.AllowGet);
                }
                var categoria = db.Categoria.Find(categoriaId);
                if (!db.ClasseLiga.Where(c => c.CategoriaId == categoriaId && c.LigaId == circuito.Id).Any())
                {
                    var classeLiga = new ClasseLiga
                    {
                        Nome = categoria.Nome,
                        CategoriaId = categoriaId,
                        LigaId = circuito.Id
                    };
                    db.ClasseLiga.Add(classeLiga);
                    db.SaveChanges();
                }
                return Json(new { erro = "", retorno = 1 }, "application/json", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = "Falha ao incluir classe no circuito:" + ex.Message, retorno = 0 }, "application/json", JsonRequestBehavior.AllowGet);
            }

            
        }

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult CreateTorneio()
        {
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            var barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
            ViewBag.barragemId = barragemId;
            var barragem = db.BarragemView.Find(barragemId);
            ViewBag.isModeloTodosContraTodos = barragem.isModeloTodosContraTodos;
            ViewBag.tokenPagSeguro = barragem.tokenPagSeguro;
            List<BarragemLiga> ligasDoRanking = db.BarragemLiga.Include(l => l.Liga).Where(bl => bl.BarragemId == barragemId && bl.Liga.isAtivo).ToList();

            var qtddTorneios = db.Torneio.Where(r => r.barragemId == barragemId).Count();
            // se for o segundo torneio, termina o período de testes
            if (qtddTorneios == 1)
            {
                var barragens = db.Barragens.Find(barragemId);
                barragens.isTeste = false;
                db.SaveChanges();
            }
            ViewBag.qtddTorneios = qtddTorneios;

            // List<Liga> ligasDisponiveis = new List<Liga>();
            List<int> ligasId = new List<int>();
            foreach (BarragemLiga bl in ligasDoRanking)
            {
                //ligasDisponiveis.Add(db.Liga.Find(bl.LigaId));
                ligasId.Add(bl.LigaId);
            }
            ViewBag.LigasDisponiveis = ligasDoRanking; // ligasDisponiveis;
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            List<Categoria> categorias = null;
            if (perfil.Equals("adminTorneio"))
            {
                categorias = db.Categoria.Where(c => (c.rankingId == 0 || c.rankingId == barragemId) && c.isDupla==true).OrderBy(c => c.ordemExibicao).ThenBy(c => c.Nome).ToList();
            }
            else {
                categorias = db.Categoria.Where(c => c.rankingId == 0 || c.rankingId == barragemId).OrderBy(c => c.ordemExibicao).ThenBy(c=> c.isDupla).ThenBy(c => c.Nome).ToList();
            }
            List<CategoriaDeLiga> categoriasDeLiga = new List<CategoriaDeLiga>();
            var classesLiga = db.ClasseLiga.Include(l => l.Liga).Where(cl => ligasId.Contains(cl.Liga.Id)).ToList();
            foreach (var categoria in categorias)
            {
                var categoriaDeLiga = new CategoriaDeLiga();
                categoriaDeLiga.Id = categoria.Id;
                categoriaDeLiga.Nome = categoria.Nome;
                categoriaDeLiga.Ligas = new List<String>();
                foreach (var classeLiga in classesLiga.Where(c => c.CategoriaId == categoria.Id).ToList())
                {
                    categoriaDeLiga.Ligas.Add(classeLiga.Liga.Nome);
                }
                categoriasDeLiga.Add(categoriaDeLiga);
            }
            ViewBag.Categorias = categoriasDeLiga;
            return View();
        }

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult NotificarViaApp(int torneioId)
        {
            try
            {
                var torneio = db.Torneio.Find(torneioId);
                var segmentacao = "torneio_" + Funcoes.RemoveAcentosEspacosMaiusculas(torneio.cidade.Split('-')[0]);
                //var segmentacao = "ranking_" + torneio.barragem.cidade + "_geral";
                var titulo = "Inscrições do " + torneio.nome + " abertas.";
                var dataHoje = DateTime.Now;
                var conteudo = "";
                if (dataHoje.DayOfYear == torneio.dataFimInscricoes.DayOfYear)
                {
                    conteudo = "Último dia para fazer sua inscrição";
                }
                else
                {
                    conteudo = "Faça sua inscrição até o dia " + torneio.dataFimInscricoes;
                }


                var fbmodel = new FirebaseNotificationModel()
                {
                    to = "/topics/" + segmentacao,
                    notification = new NotificationModel() { title = titulo, body = conteudo },
                    data = new DataModel() { title = titulo, body = conteudo, type = "novo_torneio_aberto", idRanking = torneio.barragemId }
                };
                new FirebaseNotification().SendNotification(fbmodel);
                return Json(new { erro = "", retorno = 1, segmento = segmentacao }, "application/json", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "application/json", JsonRequestBehavior.AllowGet);
            }

        }

        [Authorize(Roles = "admin,organizador, adminTorneio, adminTorneioTenis")]
        public ActionResult PainelControle(string msg="")
        {
            if (msg!="") {
                ViewBag.sucesso = "ok";
            }
            List<Torneio> torneios = new List<Torneio>();
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            var barragem = (from up in db.UserProfiles where up.UserId == userId select up.barragem).Single();
            var agora = DateTime.Now.AddDays(-1);
            var torneiosAndamento = db.Torneio.Where(t => t.dataFim > agora && t.barragemId == barragem.Id).OrderByDescending(c => c.Id).ToList();
            foreach (var item in torneiosAndamento)
            {
                // estou colocando estes dados em outros campos do objeto. Gambiarra!!!
                item.qtddClasses = db.InscricaoTorneio.Where(t => t.torneioId == item.Id).Count();
                item.valor = db.InscricaoTorneio.Where(i => i.torneioId == item.Id && i.isAtivo == true).Select(i => new { user = (int)i.userId, valor = i.valor }).Distinct().Sum(i => i.valor);
                
            }
            ViewBag.torneiosEmAndamento = torneiosAndamento;
            torneios = db.Torneio.Where(r => r.barragemId == barragem.Id && r.dataFim < agora).OrderByDescending(c => c.Id).Take(4).ToList();
            ViewBag.circuitos = db.Liga.Where(b => b.barragemId == barragem.Id).OrderByDescending(b=> b.Id).ToList();
            ViewBag.barragemId = barragem.Id;
            ViewBag.nomeAgremiacao = barragem.nome;
            if (String.IsNullOrEmpty(barragem.tokenPagSeguro))
            {
                ViewBag.tokenPagSeguro = false;
            }
            else
            {
                ViewBag.tokenPagSeguro = true;
            }
            

            return View(torneios);
        }

        [Authorize(Roles = "admin,organizador, adminTorneio, adminTorneioTenis")]
        public ActionResult Manuais()
        {
            return View();
        }

        public ActionResult Teste()
        {
            DateTime dateTime = DateTime.UtcNow;
            DateTime dateTimeLocal = DateTime.Now;
            TimeZoneInfo horaBrasilia = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
            DateTime dateTimeBrasilia = TimeZoneInfo.ConvertTimeFromUtc(dateTime, horaBrasilia);
            ViewBag.DataBrasilia = dateTimeBrasilia;
            ViewBag.DataLocal = dateTimeLocal;

            return View();
        }
        [Authorize(Roles = "admin")]
        public ActionResult GerarPosicaoUltimoRanking(int id, int categoriaId)
        {
            //coloca as posicoes do ranking
            List<SnapshotRanking> rankingAtual = db.SnapshotRanking.Where(sr => sr.CategoriaId == categoriaId
                && sr.SnapshotId == id).OrderByDescending(sr => sr.Pontuacao).ToList();
            int i = 1;
            int pontuacaoAnterior = 0;
            int posicaoAnterior = 0;
            bool isPrimeiraVez = true;
            foreach (SnapshotRanking ranking in rankingAtual)
            {
                if ((ranking.Pontuacao == pontuacaoAnterior) && (!isPrimeiraVez))
                {
                    ranking.Posicao = posicaoAnterior;
                }
                else
                {
                    ranking.Posicao = i;
                    posicaoAnterior = i;
                }
                db.SaveChanges();
                pontuacaoAnterior = ranking.Pontuacao;
                if (isPrimeiraVez)
                {
                    posicaoAnterior = i;
                    isPrimeiraVez = false;
                }
                i++;
            }
            return View();
        }
        [Authorize(Roles = "admin")]
        public ActionResult AjustarPontuacaoDuplicadaRanking(int id, int categoriaId, string nome)
        {
            //coloca as posicoes do ranking
            List<SnapshotRanking> rankingAtual = db.SnapshotRanking.Where(sr => sr.CategoriaId == categoriaId
                && sr.SnapshotId == id && sr.Jogador.nome.ToUpper().Equals(nome.ToUpper())).OrderByDescending(sr => sr.Pontuacao).ToList();
            int pontuacao = 0;
            foreach (SnapshotRanking ranking in rankingAtual)
            {
                pontuacao = pontuacao + ranking.Pontuacao;
            }
            for (int i = 0; i < rankingAtual.Count(); i++)
            {
                var r = rankingAtual[i];
                r.Pontuacao = pontuacao;
                if (i == 0){
                    db.Entry(r).State = EntityState.Modified;
                } else {
                    db.SnapshotRanking.Remove(r);
                }
                db.SaveChanges();
            }

            return View();
        }

        
        public ActionResult BaixeApp()
        {
            return View();
        }

            private QrCodeCobrancaTorneio GetQrCodeCobrancaPIX(int torneioId)
        {
            try
            {
                var torneio = db.Torneio.Find(torneioId);

                var order = montarPedidoPIX(torneio);

                var cobrancaPix = new PIXPagSeguro().CriarPedido(order);
                var qrcode = new QrCodeCobrancaTorneio();
                qrcode.text = cobrancaPix.qr_codes[0].text;
                if (cobrancaPix.qr_codes[0].links[0].media == "image/png"){
                    qrcode.link = cobrancaPix.qr_codes[0].links[0].href;
                }
                return qrcode;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private Order montarPedidoPIX(Torneio torneio)
        {
            var cobrancaTorneio = getDadosDeCobrancaTorneio(torneio.Id);
            var order = new Order();
            order.reference_id = "COB-" + torneio.Id;
            order.customer = new Customer();
            order.customer.name = torneio.barragem.nomeResponsavel;
            order.customer.email = torneio.barragem.email;
            order.customer.tax_id = torneio.barragem.cpfResponsavel;
            var item = new ItemPedido();
            item.reference_id = torneio.Id + "";
            item.name = torneio.nome;
            item.quantity = 1;
            item.unit_amount = cobrancaTorneio.valorASerPago * 100;
            order.items = new List<ItemPedido>();
            order.items.Add(item);
            var qr_code = new QrCode();
            var amount = new Amount();
            amount.value = cobrancaTorneio.valorASerPago * 100;
            qr_code.amount = amount;
            order.qr_codes = new List<QrCode>();
            order.qr_codes.Add(qr_code);
            order.notification_urls = new string[1];
            order.notification_urls[0] = "https://www.rankingdetenis.com/api/TorneioAPI/NotificacaoPagamentoOrganizador";

            return order;
        }
    }
}