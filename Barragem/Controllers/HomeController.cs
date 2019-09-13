using Barragem.Class;
using Barragem.Context;
using Barragem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using WebMatrix.WebData;
using System.Data.Entity;

namespace Barragem.Controllers
{
    public class HomeController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();
        public ActionResult Index(String msg = "")
        {
            if (User.Identity.IsAuthenticated)
            {
                string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
                if (perfil.Equals("admin") || perfil.Equals("organizador"))
                {
                    return RedirectToAction("Dashboard", "Home");
                }

                return RedirectToAction("Index3", "Home");
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
            if (cookie != null)
            {
                barragemId = Convert.ToInt32(cookie.Value.ToString());
            }
            Barragens barragem = db.Barragens.Find(barragemId);
            return RedirectToAction("IndexTorneioRedirect", "Home", new { id = barragem.dominio });
        }

        private Torneio montarDadosTorneio(Torneio torneio) {
            var torneioLess = new Torneio();
            torneioLess.nome = torneio.nome;
            torneioLess.dataInicio = torneio.dataInicio;
            torneioLess.dataFim = torneio.dataFim;
            torneioLess.local = torneio.local;
            torneioLess.cidade = torneio.cidade;
            torneioLess.Id = torneio.Id;
            torneioLess.liberarEscolhaDuplas = torneio.liberarEscolhaDuplas;
            torneioLess.dataFimInscricoes = torneio.dataFimInscricoes;
            torneioLess.valor = torneio.valor;
            torneioLess.valorSocio = torneio.valorSocio;
            torneioLess.isMaisUmaClasse = torneio.isMaisUmaClasse;
            torneioLess.valorMaisClasses = torneio.valorMaisClasses;
            torneioLess.valorMaisClassesSocio = torneio.valorMaisClassesSocio;
            torneioLess.dataFimInscricoes = torneio.dataFimInscricoes;
            torneioLess.premiacao = torneio.premiacao;
            torneioLess.barragemId = torneio.barragemId;
            return torneioLess;

        }

        public ActionResult IndexTorneioRedirect(string id)
        {
            var barragem = db.BarragemView.Where(b => b.dominio.ToLower().Equals(id.ToLower())).ToList<BarragemView>();
            if (barragem.Count() > 0)
            {
                Funcoes.CriarCookieBarragem(Response, Server, barragem[0].Id, barragem[0].nome);
                var barragemId = barragem[0].Id;
                var torneio = db.Torneio.Where(t => t.barragemId == barragemId && t.isAtivo).OrderByDescending(t => t.Id).ToList();
                if (torneio.Count() > 0)
                {
                    var torneioLess = montarDadosTorneio(torneio[0]);

                    return RedirectToAction("IndexTorneio", "Home", torneioLess);
                }
                else
                {
                    return RedirectToAction("Index", "Home", new { msg = "Desculpe mas não encontramos um torneio com esse nome. Favor verificar se o nome do torneio foi digitado corretamente." });
                }
            }
            else
            {
                return RedirectToAction("Index", "Home", new { msg = "Desculpe mas não encontramos um torneio com esse nome. Favor verificar se o nome do torneio foi digitado corretamente." });
            }
        }

        public ActionResult IndexTorneio(Torneio tr)
        {
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
                    //e.SendEmail("esmartins@gmail.com", "Solicitação de contato Ranking de tenis", "Nome do contato: " + nome + "<br>telefone de contato: " + fone, Class.Tipos.FormatoEmail.Html);
                    e.assunto = "Solicitação de contato Ranking de tenis";
                    e.conteudo = "Nome do contato: " + nome + "<br>telefone de contato: " + fone;
                    e.formato = Class.Tipos.FormatoEmail.Html;
                    e.de = "postmaster@rankingdetenis.com";
                    e.para = "esmartins@gmail.com";
                    e.bcc = new List<String>() { "coutinho.alisson@gmail.com" };
                    e.EnviarMail();
                }
                catch (Exception e)
                {
                    var log2 = new Log();
                    log2.descricao = "Email :" + e.Message;
                    db.Log.Add(log2);
                    db.SaveChanges();
                    return RedirectToAction("Index", "Home", new { msg = "Desculpe. Casdastro temporariamente indisponível." });
                }
            }
            return RedirectToAction("Index", "Home", new { msg = mensagem });

        }

        public ActionResult IndexBarragens(string id = "")
        {
            HttpCookie cookie = Request.Cookies["_barragemId"];
            if (User.Identity.IsAuthenticated)
            {
                if (cookie == null)
                {
                    var usuario = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
                    Funcoes.CriarCookieBarragem(Response, Server, usuario.barragemId, usuario.barragem.nome);
                }
                string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
                if (perfil.Equals("admin") || perfil.Equals("organizador")){
                    return RedirectToAction("Dashboard", "Home");
                }
                return RedirectToAction("Index3", "Home");
            }

            if (cookie != null)
            {
                var barragemId = Convert.ToInt32(cookie.Value.ToString());
                return View();
            }
            else
            {
                if (id == "")
                {
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
                }
                else
                {
                    var barragem = db.BarragemView.Find(barragemId);
                    torneioAberto = db.Torneio.Where(t => t.barragem.cidade == barragem.cidade && t.dataFimInscricoes > dtNow && t.isAtivo && t.divulgaCidade).OrderByDescending(t => t.dataInicio).ToList();
                    if (torneioAberto.Count() > 0)
                    {
                        return torneioAberto[0];
                    }
                    else
                    {
                        torneioAberto = db.Torneio.Where(t => t.dataFimInscricoes > dtNow && t.isAtivo && t.isOpen).OrderByDescending(t => t.dataInicio).ToList();
                        if (torneioAberto.Count() > 0)
                        {
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
            if ((barragem.soTorneio) != null)
            {
                ViewBag.SoTorneio = barragem.soTorneio;
            }
            ViewBag.NomeBarragem = barragemName;
            ViewBag.IdBarragem = barragemId;
            ViewBag.solicitarAtivacao = "";
            ViewBag.Torneio = getTorneioAberto(barragemId);
            ViewBag.TemFotoPerfil = true;
            if (String.IsNullOrEmpty(usuario.fotoURL))
            {
                ViewBag.TemFotoPerfil = false;
            }

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

        private string diaDaSemanaEmPortugues(string diaDaSemanaEmIngles) {
            var diaDaSemanaEmPt = "";

            if (diaDaSemanaEmIngles == "Sunday")
            {
                diaDaSemanaEmPt = "Domingo";
            }
            else if(diaDaSemanaEmIngles == "Monday")
            {
                diaDaSemanaEmPt = "Segunda-feira";
            }
            else if(diaDaSemanaEmIngles == "Tuesday")
            {
                diaDaSemanaEmPt = "Terça-feira";
            }
            else if(diaDaSemanaEmIngles == "Wednesday")
            {
                diaDaSemanaEmPt = "Quarta-feira";
            }
            else if(diaDaSemanaEmIngles == "Thursday")
            {
                diaDaSemanaEmPt = "Quinta-feira";
            }
            else if(diaDaSemanaEmIngles == "Friday")
            {
                diaDaSemanaEmPt = "Sexta-feira";
            }
            else if(diaDaSemanaEmIngles == "Saturday")
            {
                diaDaSemanaEmPt = "Sábado";
            }
            return diaDaSemanaEmPt;
        }

        [Authorize(Roles="admin, organizador")]
        public ActionResult Dashboard() {
            HttpCookie cookie = Request.Cookies["_barragemId"];
            var barragemId = Convert.ToInt32(cookie.Value.ToString());
            // caobrança
            var pb = db.PagamentoBarragem.Where(p => (bool)p.cobrar && p.barragemId == barragemId && (p.status == "Aguardando" || p.status == "canceled")).OrderByDescending(o => o.Id).ToList();
            if (pb.Count() > 0)
            {
                if (pb[0].status == "Aguardando")
                {
                    ViewBag.cobranca = "Olá, o boleto da sua mensalidade já está disponível para pagamento. Clique no link para acessar o boleto ou copie o número do código de barras:";
                    ViewBag.boleto = pb[0].linkBoleto;
                    ViewBag.numeroCodigoBarras = pb[0].digitableLine;
                    if (((DateTime.Now.Day > 10) && (DateTime.Now.DayOfWeek != DayOfWeek.Saturday) &&
                        (DateTime.Now.DayOfWeek != DayOfWeek.Sunday) && (DateTime.Now.DayOfWeek != DayOfWeek.Monday))
                        || (DateTime.Now.Day > 13))
                    {
                        var brg = db.Barragens.Find(barragemId);
                        brg.isAtiva = false;
                        db.Entry(brg).State = EntityState.Modified;
                        db.SaveChanges();
                        ViewBag.cobranca = "Olá, Você possui uma mensalidade em atrasado que ocasionou o bloqueio do seu ranking. Não será possível gerar temporadas e rodadas. Clique no link para acessar o boleto ou copie o número do código de barras para realizar o pagamento:";
                    }
                }
                if (pb[0].status == "canceled")
                {
                    ViewBag.cobranca = "Seu ranking está inativo por falta de pagamento do boleto. Não será possível gerar temporadas e rodadas. Favor consultar os administradores do rankingdetenis.com";
                }
            }

            // seção da Rodada atual:
            try
            {
                var rodadaAtual = db.Rodada.Where(r => r.barragemId == barragemId && r.isRodadaCarga == false).OrderByDescending(r => r.Id).FirstOrDefault();
                ViewBag.rodadaAtual = rodadaAtual;
                ViewBag.diaDaSemana = diaDaSemanaEmPortugues(rodadaAtual.dataFim.DayOfWeek.ToString());
                var jogosEmAberto = db.Jogo.Where(j => j.rodada_id == rodadaAtual.Id && j.situacao_Id != 4 && j.situacao_Id != 5).Count();
                ViewBag.jogosEmAberto = jogosEmAberto;
            }catch (Exception) { }
            // seção da Temporada atual:
            try {
                var temporada = db.Temporada.Where(t => t.barragemId == barragemId && t.isAtivo == true).OrderByDescending(t => t.Id).FirstOrDefault();
                ViewBag.temporada = temporada;
            } catch (Exception) { }
            // seção dos suspensos por WO:
            try{
                string suspensoWO = Tipos.Situacao.suspensoWO.ToString();
                string suspenso = Tipos.Situacao.suspenso.ToString();
                var jogadoresSuspensosWO = db.UserProfiles.Where(u => u.barragemId == barragemId && u.situacao == suspensoWO).Count();
                var jogadoresSuspensos = db.UserProfiles.Where(u => u.barragemId == barragemId && u.situacao == suspenso).Count();
                ViewBag.jogadoresSuspensosWO = jogadoresSuspensosWO;
                ViewBag.jogadoresSuspensos = jogadoresSuspensos;
            }
            catch (Exception) { }
            // seção das novas solicitações de participação:
            try
            {
                string pendente = Tipos.Situacao.pendente.ToString();
                var jogadorespendentes = db.UserProfiles.Where(u => u.barragemId == barragemId && u.situacao == pendente).Count();
                ViewBag.jogadorespendentes = jogadorespendentes;
            }
            catch (Exception) { }
            return View();
        }

        [Authorize(Roles = "admin, organizador, usuario")]
        public ActionResult Index3(int idJogo = 0, bool Sucesso = false, string MsgAlerta = "")
        {

            ViewBag.Sucesso = Sucesso;
            ViewBag.MsgAlerta = MsgAlerta;
            ViewBag.SoTorneio = false;
            ViewBag.ViewOrganizador = false;
            var usuario = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
            Funcoes.CriarCookieBarragem(Response, Server, usuario.barragemId, usuario.barragem.nome);
            int barragemId = usuario.barragemId;
            BarragemView barragem = db.BarragemView.Find(barragemId);
            var barragemName = barragem.nome;
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
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
            if (perfil.Equals("admin") || perfil.Equals("organizador")) {
                var pb = db.PagamentoBarragem.Where(p => (bool)p.cobrar && p.barragemId == barragemId && (p.status == "Aguardando" || p.status== "canceled")).OrderByDescending(o => o.Id).ToList();
                if (pb.Count() > 0){
                    if (pb[0].status=="Aguardando") { 
                        ViewBag.cobranca = "Olá, o boleto da sua mensalidade já está disponível para pagamento. Clique no link para acessar o boleto ou copie o número do código de barras:";
                        ViewBag.boleto = pb[0].linkBoleto;
                        ViewBag.numeroCodigoBarras = pb[0].digitableLine;
                        if (((DateTime.Now.Day > 10)&&(DateTime.Now.DayOfWeek!=DayOfWeek.Saturday) && 
                            (DateTime.Now.DayOfWeek != DayOfWeek.Sunday) && (DateTime.Now.DayOfWeek != DayOfWeek.Monday)) 
                            || (DateTime.Now.Day > 13))
                        {
                            var brg = db.Barragens.Find(barragemId);
                            brg.isAtiva = false;
                            db.Entry(brg).State = EntityState.Modified;
                            db.SaveChanges();
                            ViewBag.cobranca = "Olá, Você possui uma mensalidade em atrasado que ocasionou o bloqueio do seu ranking. Não será possível gerar temporadas e rodadas. Clique no link para acessar o boleto ou copie o número do código de barras para realizar o pagamento:";
                        }
                    }
                    if (pb[0].status == "canceled"){
                        ViewBag.cobranca = "Seu ranking está inativo por falta de pagamento do boleto. Não será possível gerar temporadas e rodadas. Favor consultar os administradores do rankingdetenis.com";
                    }
                }
            }

            Jogo jogo = null;
            if (idJogo == 0)
            {
                try
                {
                    jogo = db.Jogo.Where(u => (u.desafiado_id == usuario.UserId || u.desafiante_id == usuario.UserId) && u.torneioId == null)
                             .OrderByDescending(u => u.Id).Take(1).Single();
                    ViewBag.isRodadaAtual = true;
                }
                catch (System.InvalidOperationException e)
                {
                    ViewBag.isRodadaAtual = false;
                    //ViewBag.MsgAlert = "Não foi possível encontrar jogos em aberto:" + e.Message;
                }
            }
            else
            {
                jogo = db.Jogo.Find(idJogo);
                ViewBag.isRodadaAtual = false;
                if (perfil.Equals("admin") || perfil.Equals("organizador")){
                    ViewBag.ViewOrganizador = true;
                }
            }
            if (jogo != null)
            {
                //nao permitir edição caso a rodada já esteja fechada e o placar já tenha sido informado
                if (!perfil.Equals("admin") && !perfil.Equals("organizador") && (jogo.rodada.isAberta == false) && (jogo.gamesJogados != 0)){
                    ViewBag.Editar = false;
                }else{
                    ViewBag.Editar = true;
                }
                ViewBag.Placar = "";
                if ((jogo.situacao_Id == 4) || (jogo.situacao_Id == 5))
                {
                    if (perfil.Equals("admin") || perfil.Equals("organizador")) {
                        ViewBag.VoltarPendente = true;
                    }
                    var placar = jogo.qtddGames1setDesafiado + "/" + jogo.qtddGames1setDesafiante;
                    if (jogo.qtddGames2setDesafiado != 0 || jogo.qtddGames2setDesafiante != 0)
                    {
                        placar = placar + " " + jogo.qtddGames2setDesafiado + "/" + jogo.qtddGames2setDesafiante;
                    }
                    if (jogo.qtddGames3setDesafiado != 0 || jogo.qtddGames3setDesafiante != 0)
                    {
                        placar = placar + " " + jogo.qtddGames3setDesafiado + "/" + jogo.qtddGames3setDesafiante;
                    }
                    ViewBag.Placar = ": " + placar;
                }
                if ((jogo.torneioId != null) && (jogo.torneioId > 0))
                {
                    var torneioId = jogo.torneioId;
                    ViewBag.NomeTorneio = db.Torneio.Find(torneioId).nome;
                }

                ViewBag.ptDefendidosDesafiado = getPontosDefendidos(jogo.desafiado_id, jogo.rodada_id);
                ViewBag.ptDefendidosDesafiante = getPontosDefendidos(jogo.desafiante_id, jogo.rodada_id);

                if (jogo.torneioId == null)
                {
                    var temporadaId = jogo.rodada.temporadaId;
                    var rodadaId = jogo.rodada_id;
                    var qtddRodada = db.Rodada.Where(r => r.temporadaId == temporadaId && r.Id <= rodadaId && r.barragemId == barragemId).Count();
                    if (temporadaId > 0)
                    {
                        ViewBag.Temporada = jogo.rodada.temporada.nome;
                        ViewBag.NumeroRodada = "Rodada " + qtddRodada + " de " + jogo.rodada.temporada.qtddRodadas;
                    }
                }
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


            try
            {
                List<Rancking> ranckingJogadorDesafiado = db.Rancking.Where(r => r.userProfile_id == jogo.desafiado_id && r.posicaoClasse != null).OrderByDescending(r => r.rodada_id).Take(1).ToList();
                List<Rancking> ranckingJogadorDesafiante = db.Rancking.Where(r => r.userProfile_id == jogo.desafiante_id && r.posicaoClasse != null).OrderByDescending(r => r.rodada_id).Take(1).ToList();

                if (ranckingJogadorDesafiado.Count > 0)
                {
                    ViewBag.posicaoDesafiado = ranckingJogadorDesafiado[0].posicaoClasse + "º";
                }
                else
                {
                    ViewBag.posicaoDesafiado = "sem ranking";
                }

                if (ranckingJogadorDesafiante.Count > 0)
                {
                    ViewBag.posicaoDesafiante = ranckingJogadorDesafiante[0].posicaoClasse + "º";
                }
                else
                {
                    ViewBag.posicaoDesafiante = "sem ranking";
                }

                var jogosHeadToHead = db.Jogo.Where(j => (j.desafiado_id == jogo.desafiado_id && j.desafiante_id == jogo.desafiante_id) ||
                (j.desafiante_id == jogo.desafiado_id && j.desafiado_id == jogo.desafiante_id)).ToList();

                ViewBag.qtddVitoriasDesafiado = jogosHeadToHead.Where(j => j.idDoVencedor == jogo.desafiado_id).Count();
                ViewBag.qtddVitoriasDesafiante = jogosHeadToHead.Where(j => j.idDoVencedor == jogo.desafiante_id).Count();
            }catch(Exception e){}
            try
            {
                ViewBag.melhorRankingDesafiado = "";
                var melhorRankingDesafiado = db.Rancking.Where(r => r.userProfile_id == jogo.desafiado_id && r.posicaoClasse != null && r.classeId != null).OrderBy(r => r.classe.nivel).ThenBy(r => r.posicaoClasse).Take(1).ToList();
                ViewBag.melhorRankingDesafiado = melhorRankingDesafiado[0].posicaoClasse + "º/" + melhorRankingDesafiado[0].classe.nome;
            }
            catch (Exception e) { }

            try
            {
                ViewBag.melhorRankingDesafiante = "";
                var melhorRankingDesafiante = db.Rancking.Where(r => r.userProfile_id == jogo.desafiante_id && r.posicaoClasse != null && r.classeId != null).OrderBy(r => r.classe.nivel).ThenBy(r => r.posicaoClasse).Take(1).ToList();
                ViewBag.melhorRankingDesafiante = melhorRankingDesafiante[0].posicaoClasse + "º/" + melhorRankingDesafiante[0].classe.nome;
            }
            catch (Exception e) { }
            // gráfico linha - desempenho no ranking
            var meuRanking = db.Rancking.Where(r => r.userProfile_id == usuario.UserId && !r.rodada.isRodadaCarga && r.posicaoClasse != null && r.classeId != null).OrderByDescending(r => r.rodada.dataInicio).Take(7).ToList();
            var labels = "";
            var dados = "";
            var primeiraVez = true;
            foreach(var rk in meuRanking){
                if (primeiraVez) {
                    primeiraVez = false;
                    labels = "'"+rk.rodada.codigoSeq + ": " + rk.classe.nome.Replace("Classe","Cl.") + "'";
                    dados = "" + rk.posicaoClasse;
                }else{
                    dados =  rk.posicaoClasse + "," + dados;
                    labels = "'"+rk.rodada.codigoSeq + ": " + rk.classe.nome.Replace("Classe", "Cl.") + "',"+ labels;
                }
            }
            ViewBag.ChartLinelabels = labels;
            ViewBag.ChartLineData = dados;
            ViewBag.meuRanking = meuRanking;
            // gráfico rosca - desempenho nos jogos
            var meusJogos = db.Jogo.Where(j => (j.desafiado_id == usuario.UserId || j.desafiante_id == usuario.UserId) && (j.situacao_Id==5 || j.situacao_Id==4) && j.torneioId==null).ToList();
            ViewBag.qtddTotalDerrotas = meusJogos.Where(j => j.idDoVencedor != usuario.UserId).Count();
            ViewBag.qtddTotalVitorias = meusJogos.Where(j => j.idDoVencedor == usuario.UserId).Count();
            //ViewBag.qtddTotalWos = meusJogos.Where(j => j.situacao_Id == 5).Count();

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
