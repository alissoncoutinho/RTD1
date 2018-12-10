﻿using Barragem.Class;
using Barragem.Context;
using Barragem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using WebMatrix.WebData;

namespace Barragem.Controllers
{
    public class HomeController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();
        public ActionResult Index(String msg = "")
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index2", "Home");
            }
            string url = HttpContext.Request.Url.AbsoluteUri;
            string path = HttpContext.Request.Url.AbsolutePath;
            url = url.Replace("/", "").Replace("http:", "").Replace("www.", "");
            if ((url.Equals("barragemdocerrado.com.br")) && (path.Equals("/")))
            {
                return RedirectToAction("Index", "Masculina");
            }
            ViewBag.Msg = msg;

            ViewBag.Path = HttpRuntime.AppDomainAppPath;


            return View();


        }

        public ActionResult IndexBarragem(string id)
        {
            var barragem = db.BarragemView.Where(b => b.dominio.ToLower().Equals(id.ToLower())).ToList<BarragemView>();
            if (barragem.Count() > 0)
            {
                Funcoes.CriarCookieBarragem(Response, Server, barragem[0].Id, barragem[0].nome);
                return RedirectToAction("IndexBarragens", "Home", new { id = id });
            }
            else
            {
                return RedirectToAction("Index", "Home", new { msg = "Desculpe mas não encontramos um ranking com esse nome. Favor verificar se o nome do ranking foi digitado corretamente." });
            }



        }

        public ActionResult IndexTorneio2()
        {
            HttpCookie cookie = Request.Cookies["_barragemId"];
            var barragemId = 1;
            if (cookie != null){
                barragemId = Convert.ToInt32(cookie.Value.ToString());
            }
            Barragens barragem = db.Barragens.Find(barragemId);
            return RedirectToAction("IndexTorneioRedirect", "Home", new { id = barragem.dominio });
        }

        public ActionResult IndexTorneioRedirect(string id){
            var barragem = db.BarragemView.Where(b => b.dominio.ToLower().Equals(id.ToLower())).ToList<BarragemView>();
            if (barragem.Count() > 0){
                Funcoes.CriarCookieBarragem(Response, Server, barragem[0].Id, barragem[0].nome);
                var barragemId = barragem[0].Id;
                var torneio = db.Torneio.Where(t => t.barragemId == barragemId && t.isAtivo).OrderByDescending(t=>t.Id).ToList();
                if (torneio.Count() > 0){
                    return RedirectToAction("IndexTorneio", "Home", torneio[0]);
                }else{
                    return RedirectToAction("Index", "Home", new { msg = "Desculpe mas não encontramos um torneio com esse nome. Favor verificar se o nome do torneio foi digitado corretamente." });
                }
            }else{
                return RedirectToAction("Index", "Home", new { msg = "Desculpe mas não encontramos um torneio com esse nome. Favor verificar se o nome do torneio foi digitado corretamente." });
            }
        }

        public ActionResult IndexTorneio(Torneio tr){
            var barragemId = tr.barragemId;
            ViewBag.contato = (from bg in db.Barragens where bg.Id == barragemId select bg.contato).Single();
            return View(tr);
        }

        [HttpPost]
        public ActionResult EnviarEmail(String nome, String fone)
        {
            var mensagem = "";
            if (String.IsNullOrEmpty(nome))
            {
                mensagem = "Favor informar seu nome.";
            }
            else if (String.IsNullOrEmpty(fone))
            {
                mensagem = "Favor informar um telefone de contato.";
            }
            else if (fone.Length < 15)
            {
                mensagem = "Número de celular incompleto. Verifique se foi preenchido o DDD + número de celular.";
            }
            else
            {
                mensagem = "Parabéns!!! Seu cadastro foi realizado com sucesso. Entraremos em contato em breve.";
                try
                {
                    Mail e = new Mail();
                    e.assunto = "Solicitação de contato Ranking de tenis";
                    e.conteudo = "Nome do contato: " + nome + "<br>telefone de contato: " + fone;
                    e.formato = Class.Tipos.FormatoEmail.Html;
                    e.de = "postmaster@rankingdetenis.com";
                    e.para = "esmartins@gmail.com";
                    e.bcc = new List<String>() { "coutinho.alisson@gmail.com", "barragemdocerrago@gmail.com" };
                    e.EnviarMail();
                }
                catch (Exception e)
                {
                    return RedirectToAction("Index", "Home", new { msg = "Desculpe. Casdastro temporariamente indisponível." + e.InnerException + " - " + e.Message });
                }
            }
            return RedirectToAction("Index", "Home", new { msg = mensagem });

        }

        public ActionResult IndexBarragens(string id="")
        {
            HttpCookie cookie = Request.Cookies["_barragemId"];
            if (User.Identity.IsAuthenticated){
                if (cookie == null) {
                    var usuario = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
                    Funcoes.CriarCookieBarragem(Response, Server, usuario.barragemId, usuario.barragem.nome);
                }
                return RedirectToAction("Index2", "Home");
            }

            if (cookie != null){
                var barragemId = Convert.ToInt32(cookie.Value.ToString());
                return View();
            }else{
                if (id == "") {
                    return RedirectToAction("Index", "Home");
                }
                return RedirectToAction("IndexBarragem", "Home", new { id = id });
            }





        }

        public ActionResult FuncJogos()
        {
            return View();
        }

        public ActionResult FuncTorneio()
        {
            return View();
        }

        public ActionResult FuncEventos()
        {
            return View();
        }

        public ActionResult FuncRanking()
        {
            return View();
        }

        public ActionResult FuncEstatistica()
        {
            return View();
        }

        public ActionResult QuemSomos()
        {
            return View();
        }

        public ActionResult FaleConosco()
        {
            return View();
        }

        public ActionResult Regulamento()
        {
            return View();
        }


        public ActionResult FuncJogos2()
        {
            return View();
        }

        public ActionResult FuncTorneio2()
        {
            return View();
        }

        public ActionResult FuncEventos2()
        {
            return View();
        }

        public ActionResult FuncRanking2()
        {
            return View();
        }

        public ActionResult FuncEstatistica2()
        {
            return View();
        }

        public ActionResult QuemSomos2()
        {
            HttpCookie cookie = Request.Cookies["_barragemId"];
            var barragemId = 1;
            if (cookie != null)
            {
                barragemId = Convert.ToInt32(cookie.Value.ToString());
            }
            Barragens barragem = db.Barragens.Find(barragemId);

            return View(barragem);
        }

        public ActionResult FaleConosco2()
        {
            HttpCookie cookie = Request.Cookies["_barragemId"];
            var barragemId = 1;
            if (cookie != null)
            {
                barragemId = Convert.ToInt32(cookie.Value.ToString());
            }
            Barragens barragem = db.Barragens.Find(barragemId);

            return View(barragem);
        }

        public ActionResult Regulamento2()
        {
            HttpCookie cookie = Request.Cookies["_barragemId"];
            var barragemId = 1;
            if (cookie != null)
            {
                barragemId = Convert.ToInt32(cookie.Value.ToString());
            }
            Barragens barragem = db.Barragens.Find(barragemId);

            return View(barragem);
        }

        private Torneio getTorneioAberto(int barragemId)
        {
            try
            {
                DateTime dtNow = DateTime.Now.AddDays(-1);
                var torneioAberto = db.Torneio.Where(t => t.barragemId == barragemId && t.dataFimInscricoes > dtNow && t.isAtivo).OrderByDescending(t => t.dataInicio).ToList();
                if (torneioAberto.Count() > 0)
                {
                    return torneioAberto[0];
                } else {
                    var barragem = db.BarragemView.Find(barragemId);
                    torneioAberto = db.Torneio.Where(t => t.barragem.cidade == barragem.cidade && t.dataFimInscricoes > dtNow && t.isAtivo && t.divulgaCidade).OrderByDescending(t => t.dataInicio).ToList();
                    if (torneioAberto.Count() > 0){
                        return torneioAberto[0];
                    }else{
                        torneioAberto = db.Torneio.Where(t => t.dataFimInscricoes > dtNow && t.isAtivo && t.isOpen).OrderByDescending(t => t.dataInicio).ToList();
                        if (torneioAberto.Count() > 0){
                            return torneioAberto[0];
                        }
                    }
                }
                return null;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        [Authorize(Roles = "admin, organizador, usuario")]
        public ActionResult Index2(int idJogo = 0)
        {
            ViewBag.SoTorneio = false;
            var usuario = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
            Funcoes.CriarCookieBarragem(Response, Server, usuario.barragemId, usuario.barragem.nome);
            int barragemId = usuario.barragemId;
            BarragemView barragem = db.BarragemView.Find(barragemId);
            var barragemName = barragem.nome;
            ViewBag.linkPagSeguro = barragem.linkPagSeguro;
            if ((barragem.soTorneio) != null){
                ViewBag.SoTorneio = barragem.soTorneio;
            }
            ViewBag.NomeBarragem = barragemName;
            ViewBag.IdBarragem = barragemId;
            ViewBag.solicitarAtivacao = "";
            ViewBag.Torneio = getTorneioAberto(barragemId);
            ViewBag.TemFotoPerfil = true;
            if (String.IsNullOrEmpty(usuario.fotoURL)) {
                ViewBag.TemFotoPerfil = false;
            }

            Jogo jogo = null;
            if (idJogo == 0){
                try{
                    jogo = db.Jogo.Where(u => (u.desafiado_id == usuario.UserId || u.desafiante_id == usuario.UserId) && u.torneioId==null)
                             .OrderByDescending(u => u.Id).Take(1).Single();
                }catch (System.InvalidOperationException e){
                    //ViewBag.MsgAlert = "Não foi possível encontrar jogos em aberto:" + e.Message;
                }
            }else{
                jogo = db.Jogo.Find(idJogo);
            }
            if (jogo != null){
                //nao permitir edição caso a rodada já esteja fechada e o placar já tenha sido informado
                //string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
                //if (!perfil.Equals("admin") && !perfil.Equals("organizador") && (jogo.rodada.isAberta == false) && (jogo.gamesJogados != 0)){
                //    ViewBag.Editar = false;
                //}else{
                //    ViewBag.Editar = true;
                //}
                if ((jogo.torneioId != null) && (jogo.torneioId > 0)){
                    var torneioId = jogo.torneioId;
                    ViewBag.NomeTorneio = db.Torneio.Find(torneioId).nome;
                }

                ViewBag.situacao_Id = new SelectList(db.SituacaoJogo, "Id", "descricao", jogo.situacao_Id);
                ViewBag.ptDefendidosDesafiado = getPontosDefendidos(jogo.desafiado_id, jogo.rodada_id);
                ViewBag.ptDefendidosDesafiante = getPontosDefendidos(jogo.desafiante_id, jogo.rodada_id);
                
                if (jogo.torneioId == null){
                    var temporadaId = jogo.rodada.temporadaId;
                    var rodadaId = jogo.rodada_id;
                    var qtddRodada = db.Rodada.Where(r => r.temporadaId == temporadaId && r.Id <= rodadaId && r.barragemId == barragemId).Count();
                    if (temporadaId > 0) { 
                        ViewBag.Temporada = jogo.rodada.temporada.nome + " - Rodada " + qtddRodada + " de " + jogo.rodada.temporada.qtddRodadas;
                    }
                }
            }
            if (usuario.situacao == "pendente")
            {
                ViewBag.solicitarAtivacao = "solicitar ativacao";
            }
            else if (usuario.situacao == "Ativamento solicitado")
            {
                ViewBag.solicitarAtivacao = "Ativamento solicitado";
            }

            // jogos pendentes
            var dataLimite = DateTime.Now.AddMonths(-10);
            var jogosPendentes = db.Jogo.Where(u => (u.desafiado_id == usuario.UserId || u.desafiante_id == usuario.UserId) && !u.rodada.isAberta
                && u.situacao_Id != 4 && u.situacao_Id != 5 && u.rodada.dataInicio > dataLimite).OrderByDescending(u => u.Id).Take(3).ToList();
            if (jogosPendentes.Count()>0){
                ViewBag.JogosPendentes = jogosPendentes;
            }
            //var jogosPendentesTorneio = db.Jogo.Where(u => (u.desafiado_id == usuario.UserId || u.desafiante_id == usuario.UserId) && u.situacao_Id != 4 && u.situacao_Id != 5 && u.torneioId > 0).OrderByDescending(u => u.Id).Take(3).ToList();
            //if (jogosPendentesTorneio.Count() > 0){
            //    ViewBag.JogosPendentesTorneio = jogosPendentesTorneio;
            //}


            // últimos jogos já finalizados
            var ultimosJogosFinalizados = db.Jogo.Where(u => (u.desafiado_id == usuario.UserId || u.desafiante_id == usuario.UserId) && !u.rodada.isAberta
                && (u.situacao_Id == 4 || u.situacao_Id == 5)).OrderByDescending(u => u.Id).Take(5).ToList();
            ViewBag.JogosFinalizados = ultimosJogosFinalizados;


            return View(jogo);


        }

        [Authorize(Roles = "admin, organizador, usuario")]
        public ActionResult Index3(int idJogo = 0, bool Sucesso=false, string MsgAlerta="")
        {
            ViewBag.Sucesso = Sucesso;
            ViewBag.MsgAlerta = MsgAlerta;
            ViewBag.SoTorneio = false;
            var usuario = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
            Funcoes.CriarCookieBarragem(Response, Server, usuario.barragemId, usuario.barragem.nome);
            int barragemId = usuario.barragemId;
            BarragemView barragem = db.BarragemView.Find(barragemId);
            var barragemName = barragem.nome;
            ViewBag.linkPagSeguro = barragem.linkPagSeguro;
            if ((barragem.soTorneio) != null)
            {
                ViewBag.SoTorneio = barragem.soTorneio;
            }
            ViewBag.NomeBarragem = barragemName;
            ViewBag.IdBarragem = barragemId;
            ViewBag.solicitarAtivacao = "";
            ViewBag.Torneio = getTorneioAberto(barragemId);

            ViewBag.situacaoJogador = usuario.situacao;
            ViewBag.userId = usuario.UserId;

            Jogo jogo = null;
            if (idJogo == 0)
            {
                try
                {
                    jogo = db.Jogo.Where(u => (u.desafiado_id == usuario.UserId || u.desafiante_id == usuario.UserId) && u.torneioId == null)
                             .OrderByDescending(u => u.Id).Take(1).Single();
                }
                catch (System.InvalidOperationException e)
                {
                    //ViewBag.MsgAlert = "Não foi possível encontrar jogos em aberto:" + e.Message;
                }
            }
            else
            {
                jogo = db.Jogo.Find(idJogo);
            }
            if (jogo != null)
            {
                //nao permitir edição caso a rodada já esteja fechada e o placar já tenha sido informado
                //string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
                //if (!perfil.Equals("admin") && !perfil.Equals("organizador") && (jogo.rodada.isAberta == false) && (jogo.gamesJogados != 0)){
                //    ViewBag.Editar = false;
                //}else{
                //    ViewBag.Editar = true;
                //}
                if ((jogo.torneioId != null) && (jogo.torneioId > 0))
                {
                    var torneioId = jogo.torneioId;
                    ViewBag.NomeTorneio = db.Torneio.Find(torneioId).nome;
                }

                ViewBag.situacao_Id = new SelectList(db.SituacaoJogo, "Id", "descricao", jogo.situacao_Id);
                ViewBag.ptDefendidosDesafiado = getPontosDefendidos(jogo.desafiado_id, jogo.rodada_id);
                ViewBag.ptDefendidosDesafiante = getPontosDefendidos(jogo.desafiante_id, jogo.rodada_id);

                if (jogo.torneioId == null)
                {
                    var temporadaId = jogo.rodada.temporadaId;
                    var rodadaId = jogo.rodada_id;
                    var qtddRodada = db.Rodada.Where(r => r.temporadaId == temporadaId && r.Id <= rodadaId && r.barragemId == barragemId).Count();
                    if (temporadaId > 0)
                    {
                        ViewBag.Temporada = jogo.rodada.temporada.nome + " - Rodada " + qtddRodada + " de " + jogo.rodada.temporada.qtddRodadas;
                    }
                }
            }
            if (usuario.situacao == "pendente")
            {
                ViewBag.solicitarAtivacao = "solicitar ativacao";
            }
            else if (usuario.situacao == "Ativamento solicitado")
            {
                ViewBag.solicitarAtivacao = "Ativamento solicitado";
            }

            // jogos pendentes
            var dataLimite = DateTime.Now.AddMonths(-10);
            var jogosPendentes = db.Jogo.Where(u => (u.desafiado_id == usuario.UserId || u.desafiante_id == usuario.UserId) && !u.rodada.isAberta
                && u.situacao_Id != 4 && u.situacao_Id != 5 && u.rodada.dataInicio > dataLimite).OrderByDescending(u => u.Id).Take(3).ToList();
            if (jogosPendentes.Count() > 0)
            {
                ViewBag.JogosPendentes = jogosPendentes;
            }
            //var jogosPendentesTorneio = db.Jogo.Where(u => (u.desafiado_id == usuario.UserId || u.desafiante_id == usuario.UserId) && u.situacao_Id != 4 && u.situacao_Id != 5 && u.torneioId > 0).OrderByDescending(u => u.Id).Take(3).ToList();
            //if (jogosPendentesTorneio.Count() > 0){
            //    ViewBag.JogosPendentesTorneio = jogosPendentesTorneio;
            //}


            // últimos jogos já finalizados
            var ultimosJogosFinalizados = db.Jogo.Where(u => (u.desafiado_id == usuario.UserId || u.desafiante_id == usuario.UserId) && !u.rodada.isAberta
                && (u.situacao_Id == 4 || u.situacao_Id == 5)).OrderByDescending(u => u.Id).Take(5).ToList();
            ViewBag.JogosFinalizados = ultimosJogosFinalizados;

            return View(jogo);


        }


        /*public ActionResult TestePonto(int UserId, int rodada_id)
        {
            double pontosDefendidos = 0;
            List<Rancking> listRanking = db.Rancking.Where(r => r.userProfile_id == UserId && r.rodada_id < rodada_id).
                OrderByDescending(r => r.rodada_id).Take(10).ToList();
            foreach (Rancking ranking in listRanking)
            {
                pontosDefendidos = ranking.pontuacao;
            }
            ViewBag.pontosDefendidos = pontosDefendidos;
            return View(listRanking);
        
        }*/

        private String getPontosDefendidos(int UserId, int rodada_id)
        {
            string pontosDefendidos = "";
            List<Rancking> listRanking = db.Rancking.Where(r => r.userProfile_id == UserId && r.rodada_id < rodada_id).
                OrderByDescending(r => r.rodada_id).Take(10).ToList();
            foreach (Rancking ranking in listRanking)
            {
                pontosDefendidos = ranking.pontuacao + " pt. a defender da rodada " + ranking.rodada.codigoSeq;
            }


            return pontosDefendidos;
        }

        public ActionResult LimpaCookie()
        {
            if (Request.Cookies["_barragemId"] != null)
            {
                HttpCookie myCookie = new HttpCookie("_barragemId");
                myCookie.Expires = DateTime.Now.AddDays(-1d);
                Response.Cookies.Add(myCookie);
            }
            return RedirectToAction("Index", "Home");

        }

        public ActionResult About()
        {
            ViewBag.Message = "Your app description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}
