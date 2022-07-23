using System;
using System.Net;
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
using System.Configuration;
using Newtonsoft.Json;
using System.IO;

namespace Barragem.Controllers
{
    [Authorize]
    [InitializeSimpleMembership]
    public class BarragensController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();

        //
        // GET: /Rodada/

        [Authorize(Roles = "admin,organizador,parceiroBT")]
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

            if (Roles.IsUserInRole("organizador"))
            {
                UserProfile usu = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
                barragens = new List<Barragens>();
                barragens.Add(db.Barragens.Find(usu.barragemId));
            }
            else if (Roles.IsUserInRole("parceiroBT"))
            {
                barragens = db.Barragens.Where(b => b.isBeachTenis == true && !b.nome.ToUpper().Contains("TESTE")).ToList();
            }
            else
            {
                barragens = db.Barragens.ToList();
            }
            return View(barragens);
        }

        [Authorize(Roles = "admin")]
        public ActionResult Create()
        {
            ViewBag.PaginaEspecialId = ObterDadosDropDownPaginaEspecial(null);
            return View(new Barragens());
        }


        [Authorize(Roles = "admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Barragens barragens)
        {
            try
            {
                var codigo = 91;
                var sql = "";
                if (barragens.PaginaEspecialId == (int)EnumPaginaEspecial.Selecione) barragens.PaginaEspecialId = null;
                if (barragens.valorPorUsuario == null) barragens.valorPorUsuario = 5;
                if (barragens.soTorneio == null) barragens.soTorneio = false;

                TryValidateModel(barragens);

                using (TransactionScope scope = new TransactionScope())
                {
                    if (ModelState.IsValid)
                    {
                        if (!barragens.email.Equals(""))
                        {
                            if (!Funcoes.IsValidEmail(barragens.email))
                            {
                                ViewBag.MsgErro = string.Format("E-mail inválido. '{0}'", barragens.email);
                                return View(barragens);
                            }
                        }
                        if (!(bool)barragens.soTorneio)
                        {
                            var regulamentoTorneioTenis = db.Regra.Find(6);
                            barragens.regulamento = regulamentoTorneioTenis?.descricao;
                        }
                        db.Barragens.Add(barragens);
                        db.SaveChanges();
                        for (int i = 1; i <= 10; i++)
                        {
                            sql = "INSERT INTO Rodada(codigo, dataInicio, dataFim, isAberta, sequencial, isRodadaCarga, barragemId) " +
                            "VALUES (" + codigo + ",'2000-01-01','2000-01-01', 0, " + i + ", 1, " + barragens.Id + ")";
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
            ViewBag.PaginaEspecialId = ObterDadosDropDownPaginaEspecial(barragens.PaginaEspecialId);
            return View(barragens);
        }

        // GET: /Rodada/Edit/5
        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult EditPagSeguro()
        {
            UserProfile usu = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
            var barraAtual = db.Barragens.Find(usu.barragemId);
            if (barraAtual != null && !String.IsNullOrEmpty(barraAtual.tokenPagSeguro))
            {
                ViewBag.tokenPagSeguroConfigurado = "OK";
            }
            return View();
        }

        // GET: /Rodada/Edit/5
        [Authorize(Roles = "admin,organizador,parceiroBT")]
        public ActionResult Edit(int id = 0)
        {
            Barragens barragens = db.Barragens.Find(id);
            ViewBag.BarragemId = barragens.Id;
            if (barragens == null)
            {
                return HttpNotFound();
            }
            ViewBag.flag = "edit";
            ViewBag.PaginaEspecialId = ObterDadosDropDownPaginaEspecial(barragens.PaginaEspecialId);

            return View(barragens);
        }

        //
        // POST: /Rodada/Edit/5
        [Authorize(Roles = "admin,organizador,parceiroBT")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Barragens barragens)
        {
            var barraAtual = db.BarragemView.Find(barragens.Id);
            if (Roles.IsUserInRole("organizador"))
            {
                UserProfile usu = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
                if (usu.barragemId != barragens.Id)
                {
                    ViewBag.MsgErro = "Você não pertence a esta barragem.";
                    return View(barragens);
                }

                // o organizador não deve alterar os campos abaixo
                barragens.valorPorUsuario = barraAtual.valorPorUsuario;
                barragens.isAtiva = barraAtual.isAtiva;
                barragens.isTeste = barraAtual.isTeste;
                barragens.soTorneio = barraAtual.soTorneio;
                barragens.PaginaEspecialId = barraAtual.PaginaEspecialId;
            }

            if (barragens.PaginaEspecialId == (int)EnumPaginaEspecial.Selecione)
                barragens.PaginaEspecialId = null;

            barragens.isBeachTenis = barraAtual.isBeachTenis;
            if (barragens.soTorneio == null) barragens.soTorneio = false;
            if (ModelState.IsValid)
            {
                if (barragens.email != null)
                {
                    if (!Funcoes.IsValidEmail(barragens.email))
                    {
                        ViewBag.MsgErro = string.Format("E-mail inválido. '{0}'", barragens.email);
                        return View(barragens);
                    }
                }
                db.Entry(barragens).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.PaginaEspecialId = ObterDadosDropDownPaginaEspecial(barragens.PaginaEspecialId);
            return View(barragens);
        }

        [Authorize(Roles = "admin,adminTorneio,adminTorneioTenis,organizador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditNomeBarragem(string nome)
        {
            UserProfile usu = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
            var barraAtual = db.Barragens.Find(usu.barragemId);
            barraAtual.nome = nome;
            db.Entry(barraAtual).State = EntityState.Modified;
            db.SaveChanges();
            Funcoes.CriarCookieBarragem(Response, Server, barraAtual.Id, barraAtual.nome);
            return RedirectToAction("PainelControle", "Torneio");
        }


        [Authorize(Roles = "admin,organizador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditPagSeguro(Barragens barragens)
        {
            if (Roles.IsUserInRole("organizador"))
            {
                UserProfile usu = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
                if (usu.barragemId != barragens.Id)
                {
                    ViewBag.MsgErro = "Você não pertence a esta barragem.";
                    return View(barragens);
                }
            }
            var barraAtual = db.Barragens.Find(barragens.Id);

            barraAtual.tokenPagSeguro = barragens.tokenPagSeguro;
            barraAtual.emailPagSeguro = barragens.emailPagSeguro;

            db.Entry(barraAtual).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("PainelControle", "Torneio", new { msg = "ok" });

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
                    if (qtdeRodadasAbertas > 0)
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
            ViewBag.TenistasContribuintes = db.UserProfiles.Where(u => u.barragem.isAtiva == true && (u.situacao == "ativo" || u.situacao == "licenciado" || u.situacao == "suspenso" || u.situacao == "suspensoWO")).Count();
            ViewBag.TenistasAtivos = db.UserProfiles.Where(u => u.barragem.isAtiva == true && u.situacao == "ativo").Count();
            ViewBag.TenistasLicenciados = db.UserProfiles.Where(u => u.barragem.isAtiva == true && u.situacao == "licenciado").Count();
            ViewBag.TenistasSuspensos = db.UserProfiles.Where(u => u.barragem.isAtiva == true && (u.situacao == "suspenso" || u.situacao == "suspensoWO")).Count();

            ViewBag.RankingsAtivos = db.Barragens.Where(u => u.isAtiva == true).Count();
            ViewBag.RankingsInativos = db.Barragens.Where(u => u.isAtiva == false).Count();

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis")]
        public ActionResult SolicitarAutorizacaoPagSeguro(string emailPagSeguro)
        {
            if (!Funcoes.IsValidEmail(emailPagSeguro))
            {
                return Json(new { erro = "Email Inválido", retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            var userProfile = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
            var ranking = db.Barragens.Find(userProfile.barragemId);
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;
                string token = ConfigurationManager.AppSettings["TOKEN_PAGSEGURO"];
                string urlAutoSMS = ConfigurationManager.AppSettings["URL_PAGSEGURO"] + "/oauth2/authorize/sms";
                var jsonBody = "{\"email\":\"" + emailPagSeguro + "\"}";
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(urlAutoSMS);
                httpWebRequest.Method = "post";
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Accept = "application/json";
                httpWebRequest.Headers.Add("X_CLIENT_ID", ConfigurationManager.AppSettings["X_CLIENT_ID"]);
                httpWebRequest.Headers.Add("X_CLIENT_SECRET", ConfigurationManager.AppSettings["X_CLIENT_SECRET"]);
                httpWebRequest.Headers.Add("Authorization", "Bearer " + token); // produção
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(jsonBody);
                }
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    var telefoneSms = JsonConvert.DeserializeObject<TelefoneSMS>(result);
                    ranking.emailPagSeguro = emailPagSeguro;
                    db.Entry(ranking).State = EntityState.Modified;
                    db.SaveChanges();
                    return Json(new { mensagem = "Enviamos um sms para o número: " + telefoneSms.phone_number + ". Informe o código recebido no campo abaixo.", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
                }
            }
            catch (WebException e)
            {
                var result = new StreamReader(e.Response.GetResponseStream()).ReadToEnd();
                var erroMsg = JsonConvert.DeserializeObject<ErrorMessage>(result);
                return Json(new { erro = "Erro ao solicitar autorização: " + erroMsg.codeDescription[0].description, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis")]
        public ActionResult SolicitarToken(string code)
        {
            if (String.IsNullOrEmpty(code))
            {
                return Json(new { erro = "Código Inválido", retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            try
            {
                var userProfile = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
                var ranking = db.Barragens.Find(userProfile.barragemId);

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;
                //string token = ConfigurationManager.AppSettings["TOKEN_PAGSEGURO_SANDBOX"];
                //string urlToken = ConfigurationManager.AppSettings["URL_PAGSEGURO_SANDBOX"] + "/oauth2/token";
                string token = ConfigurationManager.AppSettings["TOKEN_PAGSEGURO"];
                string urlToken = ConfigurationManager.AppSettings["URL_PAGSEGURO"] + "/oauth2/token";
                var jsonBody = "{\"grant_type\":\"sms\",\"email\":\"" + ranking.emailPagSeguro + "\",\"sms_code\":\"" + code + "\"}";
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(urlToken);
                httpWebRequest.Method = "post";
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Accept = "application/json";
                httpWebRequest.Headers.Add("X_CLIENT_ID", ConfigurationManager.AppSettings["X_CLIENT_ID"]);
                httpWebRequest.Headers.Add("X_CLIENT_SECRET", ConfigurationManager.AppSettings["X_CLIENT_SECRET"]);
                httpWebRequest.Headers.Add("Authorization", "Bearer " + token); // produção
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(jsonBody);
                }
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    var tokenParceiro = JsonConvert.DeserializeObject<TokenParceiro>(result);
                    ranking.tokenPagSeguro = tokenParceiro.token;
                    db.Entry(ranking).State = EntityState.Modified;
                    db.SaveChanges();
                    var log2 = new Log();
                    log2.descricao = "token: " + tokenParceiro.token + ", expires_in: " + tokenParceiro.expires_in + ", refresh_token:" + tokenParceiro.refresh_token + ", scope:" + tokenParceiro.scope;
                    db.Log.Add(log2);
                    db.SaveChanges();
                    return Json(new { mensagem = "Cadastro finalizado com sucesso. Seu ranking já está apto a fazer cobranças em noso site.", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
                }
            }
            catch (WebException e)
            {
                var result = new StreamReader(e.Response.GetResponseStream()).ReadToEnd();
                if (result == null)
                {
                    return Json(new { erro = "Erro ao solicitar autorização: " + e.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
                }
                var erroMsg = JsonConvert.DeserializeObject<ErrorMessage>(result);
                return Json(new { erro = "Erro ao solicitar autorização: " + erroMsg.codeDescription[0].description, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        [Authorize(Roles = "admin")]
        public ActionResult CriarApp()
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;
                string token = ConfigurationManager.AppSettings["TOKEN_PAGSEGURO"];
                //string urlOrder = ConfigurationManager.AppSettings["URL_PAGSEGURO_ORDER"];
                string urlapp = ConfigurationManager.AppSettings["URL_PAGSEGURO"] + "/oauth2/application";
                var jsonBody = "{\"name\":\"Ranking de Tênis/Beach Tennis gostaria de sua permissão para acessar a criação de ordem de pagamento via PIX no seu pagseguro\"}";
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(urlapp);
                httpWebRequest.Method = "post";
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Accept = "application/json";
                httpWebRequest.Headers.Add("Authorization", "Bearer " + token); // produção
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(jsonBody);
                }
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                }
                return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { erro = "Erro ao solicitar autorização: " + e.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

        public SelectList ObterDadosDropDownPaginaEspecial(int? idPaginaEspecial)
        {
            return new SelectList(new[] { new PaginaEspecial() { Id = (int)EnumPaginaEspecial.Selecione, Nome = "Selecione" } }
                           .Union(db.PaginaEspecial), "Id", "Nome", idPaginaEspecial == null ? (int)EnumPaginaEspecial.Selecione : idPaginaEspecial);
        }
    }

    public class TelefoneSMS
    {
        public string phone_number { get; set; }
    }
    public class ErrorMessage
    {
        [JsonProperty(PropertyName = "error_messages")]
        public List<CodeDescription> codeDescription { get; set; }
    }
    public class CodeDescription
    {
        public string code { get; set; }
        public string description { get; set; }
    }

    public class TokenParceiro
    {
        [JsonProperty(PropertyName = "access_token")]
        public string token { get; set; }
        [JsonProperty(PropertyName = "expires_in")]
        public string expires_in { get; set; }
        [JsonProperty(PropertyName = "refresh_token")]
        public string refresh_token { get; set; }
        [JsonProperty(PropertyName = "scope")]
        public string scope { get; set; }
        [JsonProperty(PropertyName = "account_id")]
        public string account_id { get; set; }
    }
}