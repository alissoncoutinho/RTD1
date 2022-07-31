using Barragem.Class;
using Barragem.Context;
using Barragem.Filters;
using Barragem.Models;
using DotNetOpenAuth.AspNet;
using Microsoft.Web.WebPages.OAuth;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using WebMatrix.WebData;
using System.Data.Entity;
using Barragem.Helper;

namespace Barragem.Controllers
{
    [Authorize]
    [InitializeSimpleMembership]
    public class AccountController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();
        //
        // GET: /Account/Login

        [AllowAnonymous]
        public ActionResult LoginPassword(string returnUrl = "", string userName = "", string Msg = "", int torneioId = 0)
        {
            if (User.Identity.IsAuthenticated)
            {
                string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
                if (perfil.Equals("admin") || perfil.Equals("organizador"))
                {
                    return RedirectToAction("Dashboard", "Home");
                }
                else if (perfil.Equals("adminTorneio") || perfil.Equals("adminTorneioTenis"))
                {
                    return RedirectToAction("PainelControle", "Torneio");
                }
                else if (perfil.Equals("parceiroBT"))
                {
                    return RedirectToAction("Index", "Torneio");
                }
                return RedirectToAction("Index3", "Home");
            }
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.userName = userName;
            ViewBag.torneioId = torneioId;
            ViewBag.Msg = Msg;
            return View();
        }

        //
        // POST: /Account/Login

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult LoginPassword(LoginModel model, string returnUrl, int torneioId = 0)
        {
            if (ModelState.IsValid && WebSecurity.Login(model.UserName, model.Password, persistCookie: model.RememberMe))
            {
                if (returnUrl == "cadastro_usuario_barragem")
                {
                    return RedirectToAction("RegisterUserBarragem", "Account", new { barragemId = Request.ObterIdBarragem(), userName = model.UserName });
                }
                else if ((!returnUrl.Equals("torneio")) && (!returnUrl.Contains("/Torneio/LancarResultado")))
                {
                    var usuario = db.UserProfiles.Find(WebSecurity.GetUserId(model.UserName));
                    Funcoes.CriarCookieBarragem(Response, Server, usuario.barragemId, usuario.barragem.nome);
                    if ((Roles.GetRolesForUser(model.UserName)[0]).Equals("parceiroBT"))
                    {
                        return RedirectToAction("Index", "Torneio");
                    }
                }
                else if (torneioId == 0)
                {
                    HttpCookie cookie = Request.Cookies["_barragemId"];
                    if (cookie != null)
                    {
                        var barragemId = Convert.ToInt32(cookie.Value.ToString());
                        var tn = db.Torneio.Where(t => t.barragemId == barragemId && t.isAtivo).OrderByDescending(t => t.Id).ToList();
                        if (tn.Count() > 0) { torneioId = tn[0].Id; }
                    }
                }
                if ((!String.IsNullOrEmpty(returnUrl)) && (returnUrl.Equals("EscolherDupla")) && (torneioId != 0))
                {
                    return RedirectToAction("EscolherDupla", "Torneio", new { id = torneioId });
                }
                if ((!String.IsNullOrEmpty(returnUrl)) && (returnUrl.Equals("torneio")) && (torneioId != 0))
                {
                    return RedirectToAction("Detalhes", "Torneio", new { id = torneioId });
                }
                else if ((!String.IsNullOrEmpty(returnUrl)) && (returnUrl.Contains("/Torneio/LancarResultado")))
                {
                    return RedirectToAction("LancarResultado", "Torneio");
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
                //return RedirectToLocal(returnUrl);
            }

            // If we got this far, something failed, redisplay form
            ModelState.AddModelError("", "O login ou a senha estão incorretos.");
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.userName = model.UserName;
            ViewBag.torneioId = torneioId;
            return View(model);
        }

        //
        // POST: /Account/LogOff

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff(string isTorneio = "")
        {
            int torneioId = 0;
            if (isTorneio == "torneio")
            {
                HttpCookie cookie = Request.Cookies["_barragemId"];
                if (cookie != null)
                {
                    var barragemId = Convert.ToInt32(cookie.Value.ToString());

                    torneioId = db.Torneio.Where(t => t.barragemId == barragemId).OrderByDescending(t => t.Id).Take(1).Single().Id;
                }
            }
            var isBeachTennis = false;
            if (User.Identity.IsAuthenticated)
            {
                string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
                if (perfil.Equals("adminTorneio"))
                {
                    isBeachTennis = true;
                }
            }
            WebSecurity.Logout();
            if (isBeachTennis)
            {
                return RedirectToAction("IndexBT", "Home");
            }
            if (isTorneio == "torneio")
            {
                return RedirectToAction("IndexTorneioRedirect", "Home", new { id = torneioId });
            }
            return RedirectToAction("IndexBarragens", "Home");
        }

        //
        // GET: /Account/Register

        [AllowAnonymous]
        public ActionResult Register(int barragemId = 0, string email = "")
        {
            if (barragemId == 0)
            {
                barragemId = Request.ObterIdBarragem();
            }
            ViewBag.barragemId = barragemId;
            ViewBag.classeId = new SelectList(db.Classe.Where(c => c.barragemId == barragemId && c.ativa == true).ToList(), "Id", "nome");
            if (!string.IsNullOrEmpty(email))
            {
                return View(new RegisterModel() { email = email });
            }
            else
            {
                return View();
            }
        }

        [AllowAnonymous]
        public ActionResult RegisterUserBarragem(int barragemId, string userName)
        {
            ViewBag.classeId = new SelectList(db.Classe.Where(c => c.barragemId == barragemId && c.ativa).ToList(), "Id", "nome");
            return View(new RegisterModel() { UserName = userName, barragemId = barragemId });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult RegisterUserBarragem(RegisterModel model)
        {
            var mensagemErro = ValidarCamposObrigatoriosFinalizacaoCadastroBarragem(model);
            if (!string.IsNullOrEmpty(mensagemErro))
            {
                ViewBag.barragemId = model.barragemId;
                ViewBag.classeId = new SelectList(db.Classe.Where(c => c.barragemId == model.barragemId && c.ativa == true).ToList(), "Id", "nome");
                ViewBag.MsgErro = mensagemErro;
                return View(model);
            }

            var usuarioAlteracao = db.UserProfiles.FirstOrDefault(x => x.UserName == model.UserName);
            usuarioAlteracao.barragemId = model.barragemId;
            usuarioAlteracao.classeId = model.classeId;
            usuarioAlteracao.naturalidade = model.naturalidade;
            usuarioAlteracao.matriculaClube = model.matriculaClube;
            usuarioAlteracao.bairro = model.bairro;
            usuarioAlteracao.situacao = Tipos.Situacao.pendente.ToString();

            db.Entry(usuarioAlteracao).State = EntityState.Modified;
            db.SaveChanges();

            try
            {
                notificarJogador(model.nome, model.email, model.barragemId);
                notificarOrganizadorCadastro(model.nome, model.barragemId, model.telefoneCelular);
            }
            catch { }
            return RedirectToAction("Index", "Home");
        }

        private string ValidarCamposObrigatoriosFinalizacaoCadastroBarragem(RegisterModel model)
        {
            if (string.IsNullOrEmpty(model.bairro))
            {
                return "O campo Bairro é obrigatório";
            }
            if (model.classeId == null || model.classeId <= 0)
            {
                return "O campo Classe é obrigatório";
            }
            return string.Empty;
        }

        [AllowAnonymous]
        public ActionResult RegisterOrganizador()
        {
            if (User.Identity.IsAuthenticated)
            {
                UserProfile usu = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
                if (usu.barragemId == 0)
                {
                    return RedirectToAction("CreateRankingLiga", "Liga");
                }
                else
                {
                    return RedirectToAction("PainelControle", "Torneio");
                }
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult RegisterOrganizador(RegisterModel model)
        {
            model.dataNascimento = DateTime.Now.AddYears(-35);

            if (ModelState.IsValid)
            {
                // Attempt to register the user
                try
                {
                    if (!WebSecurity.UserExists(model.UserName))
                    {
                        if (!Funcoes.IsValidEmail(model.email))
                        {
                            ViewBag.MsgErro = string.Format("E-mail inválido. '{0}'", model.email);
                            return View(model);
                        }

                        WebSecurity.CreateUserAndAccount(model.UserName, model.Password, new
                        {
                            nome = model.nome,
                            dataNascimento = model.dataNascimento,
                            altura2 = "0",
                            altura = 0,
                            telefoneCelular = model.telefoneCelular,
                            email = model.email,
                            situacao = Tipos.Situacao.torneio.ToString(),
                            bairro = model.bairro,
                            dataInicioRancking = DateTime.Now,
                            naturalidade = "não informada",
                            lateralidade = "destro",
                            barragemId = model.barragemId,
                            classeId = model.classeId
                        });

                        Roles.AddUserToRole(model.UserName, "adminTorneio");

                        WebSecurity.Login(model.UserName, model.Password);
                        try
                        {
                            notificarAdmin(model.nome, model.telefoneCelular);
                        }
                        catch (Exception ex) { } // não tratar o erro pois caso não seja possível notificar o administrador não prejudicará o cadastro do usuário

                        return RedirectToAction("CreateRankingLiga", "Liga");
                    }
                    else
                    {
                        ViewBag.MsgErro = string.Format("Login já existente. Favor escolha outro nome. '{0}'", model.UserName);
                    }

                }
                catch (MembershipCreateUserException e)
                {
                    ModelState.AddModelError("", ErrorCodeToString(e.StatusCode));
                }

            }
            return View(model);
        }

        [AllowAnonymous]
        public ActionResult Login(int torneioId = 0, string returnUrl = "", string Msg = "")
        {
            if ((User.Identity.IsAuthenticated) && (torneioId > 0))
            {
                if (returnUrl == "torneio")
                {
                    return RedirectToAction("Detalhes", "Torneio", new { id = torneioId });
                }
                return RedirectToAction(returnUrl, "Torneio", new { torneioId = torneioId });
            }
            var model = new VerificacaoCadastro();
            model.torneioId = torneioId;
            model.returnUrl = returnUrl;
            ViewBag.Msg = Msg;
            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult VerificarEmail(VerificacaoCadastro model)
        {
            if (ModelState.IsValid)
            {
                if (!Funcoes.IsValidEmail(model.email))
                {
                    ViewBag.MsgErro = string.Format("E-mail inválido. '{0}'", model.email);
                    return View(model);
                }
                else
                {
                    var registers = db.UserProfiles.Where(u => u.email.Equals(model.email) && !u.situacao.Equals("desativado")).ToList();
                    if (registers.Count() > 0)
                    {
                        var usuario = registers[0];
                        return RedirectToAction("Login", new
                        {
                            returnUrl = model.returnUrl,
                            userName = usuario.UserName,
                            Msg = "Olá, " + usuario.nome + " seu login foi localizado no ranking: " + usuario.barragem.nome + " entre com a sua senha ou faça um novo cadastro se desejar.",
                            torneioId = model.torneioId
                        });
                    }
                    else
                    {
                        // direcionar para a tela de cadastro ou a tela de Inscrição de torneio
                        if (!String.IsNullOrEmpty(model.returnUrl) && model.returnUrl.Equals("torneio"))
                        {
                            return RedirectToAction("RegisterTorneio", new { email = model.email, torneioId = model.torneioId });
                        }
                        else
                        {
                            return RedirectToAction("Login", new { returnUrl = model.returnUrl, userName = "", Msg = "Olá, Não encontramos nenhum cadastro com esse email. Clique no botão Cadastre-se para efetuar um novo cadastro.", torneioId = model.torneioId });
                        }
                    }
                }
            }
            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult Login(VerificacaoCadastro model)
        {
            if (ModelState.IsValid)
            {
                var registers = db.UserProfiles.Where(u => (u.email.Equals(model.email) || u.UserName.ToLower() == model.email.ToLower())).ToList();

                if (model.returnUrl == "cadastro_usuario_barragem")
                {
                    if (registers.Any(x => string.Equals(x.situacao, "torneio", StringComparison.OrdinalIgnoreCase)))
                    {
                        var usuarioTorneioMaisRecente = registers.OrderByDescending(o => o.dataInicioRancking).FirstOrDefault(x => x.situacao == "torneio");
                        return RedirectToAction("LoginPassword", new
                        {
                            returnUrl = model.returnUrl,
                            userName = usuarioTorneioMaisRecente.UserName,
                            Msg = $"Olá, {usuarioTorneioMaisRecente.nome} já encontramos um usuário para este e-mail. Faça o login para completar seu cadastro no {usuarioTorneioMaisRecente.barragem.nome}.",
                            barragemId = Request.ObterIdBarragem()
                        });
                    }
                    else
                    {
                        return RedirectToAction("Register", new { email = model.email, barragemId = Request.ObterIdBarragem() });
                    }
                }
                else
                {
                    if (registers.Count() > 1)
                    {
                        return RedirectToAction("ListaLogins", "Account", model);
                    }
                    else if (registers.Count() > 0)
                    {
                        var usuario = registers[0];
                        return RedirectToAction("LoginPassword", new
                        {
                            returnUrl = model.returnUrl,
                            userName = usuario.UserName,
                            Msg = "Olá, " + usuario.nome + " seu login foi localizado no ranking: " + usuario.barragem.nome + " entre com a sua senha.",
                            torneioId = model.torneioId
                        });
                    }
                    else if (!String.IsNullOrEmpty(model.returnUrl) && model.returnUrl.Equals("torneio"))
                    {
                        return RedirectToAction("RegisterTorneio", new { email = model.email, torneioId = model.torneioId });
                    }
                    else
                    {
                        return RedirectToAction("Login", new { returnUrl = model.returnUrl, Msg = "Olá, Não encontramos nenhum cadastro com esse email ou usuário.", torneioId = model.torneioId });
                    }
                }
            }
            return View(model);
        }
        [AllowAnonymous]
        public ActionResult ListaLogins(VerificacaoCadastro model)
        {
            var registers = db.UserProfiles.Where(u => u.situacao != "inativo" && (u.email.Equals(model.email) || u.UserName.ToLower() == model.email.ToLower())).ToList();
            var loginRegisters = new List<LoginRankingModel>();
            foreach (var item in registers)
            {
                var loginRankingModel = new LoginRankingModel();
                loginRankingModel.logoId = item.barragemId;
                loginRankingModel.userName = item.UserName;
                loginRankingModel.nomeRanking = item.barragem.nome;
                loginRankingModel.idRanking = 0;
                var snapshotRanking = db.SnapshotRanking.Where(s => s.UserId == item.UserId).Include(s => s.Liga).OrderByDescending(s => s.Id).Take(1).ToList();
                if (snapshotRanking.Count() > 0)
                {
                    loginRankingModel.nomeLiga = snapshotRanking[0].Liga.Nome;
                    loginRankingModel.idRanking = 1;
                }
                loginRegisters.Add(loginRankingModel);
            }
            loginRegisters = loginRegisters.OrderByDescending(l => l.idRanking).ToList();
            ViewBag.ReturnUrl = model.returnUrl;
            ViewBag.torneioId = model.torneioId;
            return View(loginRegisters);
        }

        [AllowAnonymous]
        public ActionResult RegisterTorneio(int torneioId, string email = "")
        {
            if (torneioId == 0)
            {
                HttpCookie cookie = Request.Cookies["_barragemId"];
                if (cookie != null)
                {
                    var barragemId = Convert.ToInt32(cookie.Value.ToString());
                    var tn = db.Torneio.Where(t => t.barragemId == barragemId && t.isAtivo).OrderByDescending(t => t.Id).ToList();
                    if (tn.Count() > 0) { torneioId = tn[0].Id; }
                }
            }
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Detalhes", "Torneio", new { id = torneioId });
            }
            ViewBag.torneio = db.Torneio.Find(torneioId);
            var classes = db.ClasseTorneio.Where(i => i.torneioId == torneioId).OrderBy(c => c.nivel).ToList();
            ViewBag.Classes = classes;

            ViewBag.Classes2 = classes;
            foreach (var item in classes)
            {
                if (item.maximoInscritos > 0)
                {
                    var listQtddInscritosClasseTorneio = new List<ClasseTorneioQtddInscrito>();
                    var qtddInscritos = db.InscricaoTorneio.Where(i => i.torneio.Id == torneioId).GroupBy(i => i.classe);
                    foreach (var group in qtddInscritos)
                    {
                        var classeTorneioQtddInscrito = new ClasseTorneioQtddInscrito();
                        classeTorneioQtddInscrito.Id = group.Key;
                        classeTorneioQtddInscrito.qtddInscritos = group.Count();
                        listQtddInscritosClasseTorneio.Add(classeTorneioQtddInscrito);
                    }
                    ViewBag.qtddInscritos = listQtddInscritosClasseTorneio;
                    break;
                }
            }
            ViewBag.email = "";
            ViewBag.login = "";
            if (Funcoes.ValidateEmail(email))
            {
                ViewBag.email = email;
            }
            else
            {
                ViewBag.login = email;
            }

            return View();
        }


        [AllowAnonymous]
        public MensagemRetorno RegisterTorneioNegocio(RegisterInscricao model, int torneioId, bool isSocio = false, bool isClasseDupla = false, bool isFederado = false)
        {
            var torneioController = new TorneioController();
            var torneio = db.Torneio.Find(torneioId);
            var classesBarragem = db.Classe.Where(c => c.barragemId == torneio.barragemId).ToList();
            var mensagemRetorno = new MensagemRetorno();
            if (ModelState.IsValid)
            {
                try
                {
                    if (WebSecurity.UserExists(model.register.UserName))
                    {
                        mensagemRetorno.mensagem = "Login já existente.Favor escolha outro nome." + model.register.UserName;
                        mensagemRetorno.tipo = "erro";
                        return mensagemRetorno;
                    }
                    if (!Funcoes.IsValidEmail(model.register.email))
                    {
                        mensagemRetorno.mensagem = "E-mail inválido. " + model.register.email;
                        mensagemRetorno.tipo = "erro";
                        return mensagemRetorno;
                    }
                    string msgValidacaoClasse = torneioController.validarEscolhasDeClasses(model.inscricao.classe, model.classeInscricao2, model.classeInscricao3, model.classeInscricao4);
                    if (msgValidacaoClasse != "")
                    {
                        mensagemRetorno.nomePagina = "Detalhes";
                        mensagemRetorno.tipo = "erro";
                        mensagemRetorno.mensagem = msgValidacaoClasse;
                        return mensagemRetorno;
                    }
                    msgValidacaoClasse = torneioController.validarLimiteDeInscricao(model.inscricao.classe, model.classeInscricao2, model.classeInscricao3, model.classeInscricao4, torneio.Id);
                    if (msgValidacaoClasse != "")
                    {
                        mensagemRetorno.nomePagina = "Detalhes";
                        mensagemRetorno.tipo = "erro";
                        mensagemRetorno.mensagem = msgValidacaoClasse;
                        return mensagemRetorno;
                    }
                    WebSecurity.CreateUserAndAccount(model.register.UserName, model.register.Password, new
                    {
                        nome = model.register.nome,
                        dataNascimento = model.register.dataNascimento,
                        altura2 = model.register.altura2,
                        altura = model.register.altura,
                        telefoneFixo = "",
                        telefoneCelular = model.register.telefoneCelular,
                        telefoneCelular2 = "",
                        email = model.register.email,
                        situacao = Tipos.Situacao.torneio.ToString(),
                        bairro = model.register.bairro,
                        dataInicioRancking = DateTime.Now,
                        naturalidade = "",
                        lateralidade = model.register.lateralidade,
                        nivelDeJogo = model.register.nivelDeJogo,
                        barragemId = model.register.barragemId,
                        classeId = (classesBarragem.Count() > 0) ? classesBarragem[0].Id : 2722,

                    });
                    Roles.AddUserToRole(model.register.UserName, "usuario");
                    WebSecurity.Login(model.register.UserName, model.register.Password);

                    // incluir inscrição
                    model.inscricao.userId = WebSecurity.GetUserId(model.register.UserName);

                    double valorInscricao = torneioController.calcularValorInscricao(model.classeInscricao2, model.classeInscricao3, model.classeInscricao4, isSocio, torneio, model.inscricao.userId, isFederado);

                    InscricaoTorneio insc = torneioController.preencherInscricaoTorneio(model.inscricao.torneioId, model.inscricao.userId, model.inscricao.classe, valorInscricao, model.inscricao.observacao, isSocio, isFederado);
                    db.InscricaoTorneio.Add(insc);
                    if (model.classeInscricao2 > 0)
                    {
                        InscricaoTorneio insc2 = torneioController.preencherInscricaoTorneio(model.inscricao.torneioId, model.inscricao.userId, model.classeInscricao2, valorInscricao, model.inscricao.observacao, isSocio, isFederado);
                        db.InscricaoTorneio.Add(insc2);
                    }
                    if (model.classeInscricao3 > 0)
                    {
                        InscricaoTorneio insc3 = torneioController.preencherInscricaoTorneio(model.inscricao.torneioId, model.inscricao.userId, model.classeInscricao3, valorInscricao, model.inscricao.observacao, isSocio, isFederado);
                        db.InscricaoTorneio.Add(insc3);
                    }
                    if (model.classeInscricao4 > 0)
                    {
                        InscricaoTorneio insc4 = torneioController.preencherInscricaoTorneio(model.inscricao.torneioId, model.inscricao.userId, model.classeInscricao4, valorInscricao, model.inscricao.observacao, isSocio, isFederado);
                        db.InscricaoTorneio.Add(insc4);
                    }
                    db.SaveChanges();
                    if (isClasseDupla)
                    {
                        mensagemRetorno.mensagem = "";
                        mensagemRetorno.tipo = "redirect";
                        mensagemRetorno.nomePagina = "EscolherDupla";
                        return mensagemRetorno;
                    }
                    mensagemRetorno.mensagem = "Inscrição realizada.";
                    mensagemRetorno.tipo = "redirect";
                    mensagemRetorno.nomePagina = "ConfirmacaoInscricao";
                    return mensagemRetorno;

                }
                catch (MembershipCreateUserException e)
                {
                    ModelState.AddModelError("", ErrorCodeToString(e.StatusCode));
                    mensagemRetorno.mensagem = e.Message;
                    mensagemRetorno.tipo = "erro";
                    return mensagemRetorno;
                }
            }
            else
            {
                var message = string.Join(" | ", ModelState.Values
                                            .SelectMany(v => v.Errors)
                                            .Select(e => e.ErrorMessage));
                mensagemRetorno.mensagem = message.ToString();
                mensagemRetorno.tipo = "erro";
                return mensagemRetorno;
            }

        }

        public ActionResult RegisterCoordenador()
        {
            ViewBag.barragemId = new SelectList(db.BarragemView.ToList(), "Id", "nome");
            return View();
        }


        [HttpPost]
        [AllowAnonymous]
        public ActionResult RegisterTorneio(RegisterInscricao model, int torneioId, bool isSocio = false, bool isClasseDupla = false, bool isFederado = false)
        {
            var torneio = db.Torneio.Find(torneioId);
            ViewBag.torneio = torneio;
            ViewBag.email = model.register.email;
            ViewBag.login = model.register.UserName;
            ViewBag.isSocio = isSocio;
            ViewBag.isClasseDupla = isClasseDupla;
            ViewBag.ClasseInscricao = model.inscricao.classe;
            ViewBag.ClasseInscricao2 = model.classeInscricao2;
            var classes = db.ClasseTorneio.Where(i => i.torneioId == torneioId).OrderBy(c => c.nivel).ToList();
            ViewBag.Classes = classes;
            ViewBag.Classes2 = classes;
            ViewBag.qtddInscritos = new TorneioNegocio().qtddInscritosEmCadaClasse(classes, torneioId);
            var mensagemRetorno = RegisterTorneioNegocio(model, torneioId, isSocio, isClasseDupla, isFederado);
            if (mensagemRetorno.tipo == "erro")
            {
                ViewBag.MsgErro = mensagemRetorno.mensagem;
                return View(model);
            }
            else if (mensagemRetorno.tipo == "redirect")
            {
                if (mensagemRetorno.nomePagina == "ConfirmacaoInscricao")
                {
                    return RedirectToAction(mensagemRetorno.nomePagina, "Torneio", new { torneioId = torneioId, msg = mensagemRetorno.mensagem, msgErro = "" });
                }
                else
                {
                    return RedirectToAction(mensagemRetorno.nomePagina, "Torneio", new { id = torneioId, Msg = mensagemRetorno.mensagem });
                }
            }
            else
            {
                return View(model);
            }
        }

        //
        // POST: /Account/Register

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                // Attempt to register the user
                try
                {
                    if (!WebSecurity.UserExists(model.UserName))
                    {
                        if (!Funcoes.IsValidEmail(model.email))
                        {
                            ViewBag.MsgErro = string.Format("E-mail inválido. '{0}'", model.email);
                            ViewBag.barragemId = model.barragemId;
                            ViewBag.classeId = new SelectList(db.Classe.Where(c => c.barragemId == model.barragemId && c.ativa == true).ToList(), "Id", "nome");
                            return View(model);
                        }

                        WebSecurity.CreateUserAndAccount(model.UserName, model.Password, new
                        {
                            nome = model.nome,
                            dataNascimento = model.dataNascimento,
                            altura2 = model.altura2,
                            altura = model.altura,
                            telefoneFixo = model.telefoneFixo,
                            telefoneCelular = model.telefoneCelular,
                            telefoneCelular2 = model.telefoneCelular2,
                            email = model.email,
                            situacao = Tipos.Situacao.pendente.ToString(),
                            bairro = model.bairro,
                            dataInicioRancking = DateTime.Now,
                            naturalidade = model.naturalidade,
                            lateralidade = model.lateralidade,
                            nivelDeJogo = model.nivelDeJogo,
                            barragemId = model.barragemId,
                            classeId = model.classeId,
                            matriculaClube = model.matriculaClube
                        });

                        if (model.organizador)
                        {
                            var barragem = db.Barragens.Find(model.barragemId);
                            if (barragem != null && barragem.soTorneio == true)
                            {
                                Roles.AddUserToRole(model.UserName, "adminTorneioTenis");
                            }
                            else
                            {
                                Roles.AddUserToRole(model.UserName, "organizador");
                            }
                        }
                        else
                        {
                            Roles.AddUserToRole(model.UserName, "usuario");
                        }
                        WebSecurity.Login(model.UserName, model.Password);
                        try
                        {
                            notificarJogador(model.nome, model.email, model.barragemId);
                            notificarOrganizadorCadastro(model.nome, model.barragemId, model.telefoneCelular);
                        }
                        catch (Exception ex) { } // não tratar o erro pois caso não seja possível notificar o administrador não prejudicará o cadastro do usuário

                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        ViewBag.barragemId = model.barragemId;
                        ViewBag.classeId = new SelectList(db.Classe.Where(c => c.barragemId == model.barragemId && c.ativa == true).ToList(), "Id", "nome");
                        ViewBag.MsgErro = string.Format("Login já existente. Favor escolha outro nome. '{0}'", model.UserName);
                    }

                }
                catch (MembershipCreateUserException e)
                {
                    ModelState.AddModelError("", ErrorCodeToString(e.StatusCode));
                }

            }
            ViewBag.barragemId = model.barragemId;
            ViewBag.classeId = new SelectList(db.Classe.Where(c => c.barragemId == model.barragemId && c.ativa == true).ToList(), "Id", "nome");
            // If we got this far, something failed, redisplay form
            return View(model);
        }

        private string ProcessImage(string croppedImage, int userId)
        {

            string filePath = String.Empty;
            try
            {
                string base64 = croppedImage;
                byte[] bytes = Convert.FromBase64String(base64.Split(',')[1]);
                filePath = "/Content/images/Photo/Pf-" + Guid.NewGuid() + ".jpg";
                MemoryStream stream = new MemoryStream(bytes);
                Image png = Image.FromStream(stream);
                png.Save(Server.MapPath(filePath), System.Drawing.Imaging.ImageFormat.Jpeg);
                png.Dispose();
                //using (FileStream stream = new FileStream(Server.MapPath(filePath), FileMode.Create)){
                //    stream.Write(bytes, 0, bytes.Length);
                //    stream.Flush();
                //}
            }
            catch (Exception ex)
            {
                string st = ex.Message;
            }
            if (userId != 0)
            {
                string fotoURL = (from up in db.UserProfiles where up.UserId == userId select up.fotoURL).Single();
                if ((fotoURL != null) && (System.IO.File.Exists(Server.MapPath(fotoURL))))
                {
                    try
                    {
                        System.IO.File.Delete(Server.MapPath(fotoURL));
                    }
                    catch (System.IO.IOException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }

            return filePath;
        }

        public void notificarOrganizadorCadastro(string nome, int idBarragem, string telefoneCelular)
        {
            Mail mail = new Mail();
            mail.de = System.Configuration.ConfigurationManager.AppSettings.Get("UsuarioMail");
            var barragem = db.BarragemView.Find(idBarragem);
            if (barragem.email.Equals(""))
            {
                mail.para = "barragemdocerrado@gmail.com";
            }
            else
            {
                mail.para = barragem.email;
            }
            mail.assunto = "Um Novo cadastro foi realizado";
            //mail.conteudo = nome + " fez uma soliticação de adesão à barragem do cerrado.<br><br>Entre no sistema e modifique o status de "+ nome + " de pendente para ativo.";
            mail.conteudo = nome + " acabou de se cadastrar no site.<br> Contato: " + telefoneCelular + "<br>";
            mail.formato = Tipos.FormatoEmail.Html;
            mail.EnviarMail();
        }

        private void notificarOrganizadorSolicitacaoAtivar(string nome, int idBarragem, string telefoneCelular)
        {
            Mail mail = new Mail();
            mail.de = System.Configuration.ConfigurationManager.AppSettings.Get("UsuarioMail");
            var barragem = db.BarragemView.Find(idBarragem);
            if (barragem.email.Equals(""))
            {
                mail.para = "barragemdocerrado@gmail.com";
            }
            else
            {
                mail.para = barragem.email;
            }
            mail.assunto = "Solicitação de ativação";
            mail.conteudo = nome + " fez uma soliticação de ativação à " + barragem.nome + ".<br><br>Entre no sistema e modifique o status de " + nome + " de ativamento solicitado para ativo. Contato: " + telefoneCelular;
            mail.formato = Tipos.FormatoEmail.Html;
            mail.EnviarMail();
        }

        public void notificarJogador(string nome, string email, int barragemId)
        {
            var barragem = db.BarragemView.Find(barragemId);
            Mail mail = new Mail();
            mail.de = System.Configuration.ConfigurationManager.AppSettings.Get("UsuarioMail");
            mail.para = email;
            mail.assunto = "Cadastro realizado com sucesso.";
            mail.conteudo = "Olá " + nome + ",<br> Parabéns você acabou de se cadastrar no " + barragem.nome + ".<br><br>" +
            "Em breve o organizador do ranking entrará em contato para confirmar sua ativação no ranking. " +
            "Após a ativação do seu cadastro você será notificado por email e já estará apto a participar dos jogos do ranking. <br><br>" +
            "Atenciosamente," + barragem.nome + ".<br>";
            mail.formato = Tipos.FormatoEmail.Html;
            mail.EnviarMail();
        }

        private void notificarAdmin(string nome, string fone)
        {
            try
            {
                Mail e = new Mail();
                e.assunto = "Cadastro de novo organizador realizado";
                e.conteudo = "Um novo cadastro de organizador de ranking foi realizado. <br>Nome do contato: " + nome + "<br>telefone de contato: " + fone;
                e.formato = Class.Tipos.FormatoEmail.Html;
                e.de = "postmaster@rankingdetenis.com";
                e.para = "tecnologia.btd@gmail.com";
                e.bcc = new List<String>() { "coutinho.alisson@gmail.com" };
                e.EnviarMail();
            }
            catch (Exception e)
            {

            }
        }

        [Authorize(Roles = "admin,organizador,usuario,adminTorneio,adminTorneioTenis")]
        public ActionResult Detalhes(int userId, bool mostrarClasse = true)
        {
            ViewBag.mostrarClasse = mostrarClasse;
            UserProfile jogador = db.UserProfiles.Find(userId);
            UserProfile usu = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            if (!perfil.Equals("admin"))
            {
                if (usu.barragemId != jogador.barragemId)
                {
                    //userId = usu.UserId;
                    //jogador = usu;
                }
            }
            List<Rancking> ranckingJogador = db.Rancking.Where(r => r.userProfile_id == userId && r.posicaoClasse != null).OrderByDescending(r => r.rodada_id).ToList();
            ViewBag.RanckingJogador = ranckingJogador;
            List<Jogo> jogosJogador = db.Jogo.Where(r => (r.desafiante_id == userId || r.desafiado_id == userId) && r.torneioId == null)
                .OrderByDescending(r => r.rodada_id).Take(30).ToList();
            ViewBag.jogosJogador = jogosJogador;

            if (ranckingJogador.Count > 0)
            {
                ViewBag.posicao = ranckingJogador[0].posicaoClasse + "º";
                ViewBag.pontos = Math.Round(ranckingJogador[0].totalAcumulado, 2);
            }
            else
            {
                ViewBag.pontos = 0;
                ViewBag.posicao = "sem rancking";
            }

            //carregar colocações em torneios
            //db.InscricaoTorneio.Where
            var colocacoesEmTorneios =
                from inscricao in db.InscricaoTorneio
                join torneio in db.Torneio on inscricao.torneioId equals torneio.Id into colocacaoJogador
                where inscricao.userId == userId && inscricao.torneio.dataFim < DateTime.Now && inscricao.colocacao != null
                select new
                {
                    inscricao.colocacao,
                    inscricao.torneio.nome,
                    classe = inscricao.classeTorneio.nome,
                    dataInicio = inscricao.torneio.dataInicio,
                    dataFim = inscricao.torneio.dataFim
                };

            ViewBag.colocacoesEmTorneios = colocacoesEmTorneios;

            return View(jogador);
        }

        [HttpGet]
        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis")]
        public ActionResult EditPontuacao(int Id)
        {
            Rancking r = db.Rancking.Find(Id);
            return View(r);
        }

        [HttpPost]
        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis")]
        public ActionResult EditPontuacao(Rancking r)
        {
            double pontuacaoTotal = db.Rancking.Where(ran => ran.rodada.isAberta == false && ran.userProfile_id == r.userProfile_id
                && ran.rodada_id < r.rodada_id).OrderByDescending(ran => ran.Id).Take(9).Sum(ran => ran.pontuacao);
            r.totalAcumulado = r.pontuacao + pontuacaoTotal;
            db.Entry(r).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Detalhes", new { userId = r.userProfile_id });
        }


        //
        // POST: /Account/Disassociate

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Disassociate(string provider, string providerUserId)
        {
            string ownerAccount = OAuthWebSecurity.GetUserName(provider, providerUserId);
            ManageMessageId? message = null;

            // Only disassociate the account if the currently logged in user is the owner
            if (ownerAccount == User.Identity.Name)
            {
                // Use a transaction to prevent the user from deleting their last login credential
                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.Serializable }))
                {
                    bool hasLocalAccount = OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(User.Identity.Name));
                    if (hasLocalAccount || OAuthWebSecurity.GetAccountsFromUserName(User.Identity.Name).Count > 1)
                    {
                        OAuthWebSecurity.DeleteAccount(provider, providerUserId);
                        scope.Complete();
                        message = ManageMessageId.RemoveLoginSuccess;
                    }
                }
            }

            return RedirectToAction("Manage", new { Message = message });
        }

        //
        // GET: /Account/Manage

        public ActionResult Manage(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Sua senha foi alterada com sucesso."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : "";
            ViewBag.HasLocalPassword = OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(User.Identity.Name));
            ViewBag.ReturnUrl = Url.Action("Manage");
            return View();
        }

        //
        // POST: /Account/Manage

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Manage(LocalPasswordModel model)
        {
            bool hasLocalAccount = OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(User.Identity.Name));
            ViewBag.HasLocalPassword = hasLocalAccount;
            ViewBag.ReturnUrl = Url.Action("Manage");

            if (hasLocalAccount)
            {
                if (ModelState.IsValid)
                {
                    // ChangePassword will throw an exception rather than return false in certain failure scenarios.
                    bool changePasswordSucceeded;
                    try
                    {
                        changePasswordSucceeded = WebSecurity.ChangePassword(User.Identity.Name, model.OldPassword, model.NewPassword);
                    }
                    catch (Exception)
                    {
                        changePasswordSucceeded = false;
                    }

                    if (changePasswordSucceeded)
                    {
                        return RedirectToAction("Manage", new { Message = ManageMessageId.ChangePasswordSuccess });
                    }
                    else
                    {
                        ModelState.AddModelError("", "The current password is incorrect or the new password is invalid.");
                    }
                }
            }
            else
            {
                // User does not have a local password so remove any validation errors caused by a missing
                // OldPassword field
                ModelState state = ModelState["OldPassword"];
                if (state != null)
                {
                    state.Errors.Clear();
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        WebSecurity.CreateAccount(User.Identity.Name, model.NewPassword);
                        return RedirectToAction("Manage", new { Message = ManageMessageId.SetPasswordSuccess });
                    }
                    catch (Exception)
                    {
                        ModelState.AddModelError("", String.Format("Unable to create local account. An account with the name \"{0}\" may already exist.", User.Identity.Name));
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // POST: /Account/ExternalLogin

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            return new ExternalLoginResult(provider, Url.Action("ExternalLoginCallback", new { ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/ExternalLoginCallback

        [AllowAnonymous]
        public ActionResult ExternalLoginCallback(string returnUrl)
        {
            AuthenticationResult result = OAuthWebSecurity.VerifyAuthentication(Url.Action("ExternalLoginCallback", new { ReturnUrl = returnUrl }));
            if (!result.IsSuccessful)
            {
                return RedirectToAction("ExternalLoginFailure");
            }

            if (OAuthWebSecurity.Login(result.Provider, result.ProviderUserId, createPersistentCookie: false))
            {
                return RedirectToLocal(returnUrl);
            }

            if (User.Identity.IsAuthenticated)
            {
                // If the current user is logged in add the new account
                OAuthWebSecurity.CreateOrUpdateAccount(result.Provider, result.ProviderUserId, User.Identity.Name);
                return RedirectToLocal(returnUrl);
            }
            else
            {
                // User is new, ask for their desired membership name
                string loginData = OAuthWebSecurity.SerializeProviderUserId(result.Provider, result.ProviderUserId);
                ViewBag.ProviderDisplayName = OAuthWebSecurity.GetOAuthClientData(result.Provider).DisplayName;
                ViewBag.ReturnUrl = returnUrl;
                return View("ExternalLoginConfirmation", new RegisterExternalLoginModel { UserName = result.UserName, ExternalLoginData = loginData });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLoginConfirmation(RegisterExternalLoginModel model, string returnUrl)
        {
            string provider = null;
            string providerUserId = null;

            if (User.Identity.IsAuthenticated || !OAuthWebSecurity.TryDeserializeProviderUserId(model.ExternalLoginData, out provider, out providerUserId))
            {
                return RedirectToAction("Manage");
            }

            if (ModelState.IsValid)
            {
                // Insert a new user into the database
                using (BarragemDbContext db = new BarragemDbContext())
                {
                    UserProfile user = db.UserProfiles.FirstOrDefault(u => u.UserName.ToLower() == model.UserName.ToLower());
                    // Check if user already exists
                    if (user == null)
                    {
                        // Insert name into the profile table
                        db.UserProfiles.Add(new UserProfile { UserName = model.UserName });
                        db.SaveChanges();

                        OAuthWebSecurity.CreateOrUpdateAccount(provider, providerUserId, model.UserName);
                        OAuthWebSecurity.Login(provider, providerUserId, createPersistentCookie: false);

                        return RedirectToLocal(returnUrl);
                    }
                    else
                    {
                        ModelState.AddModelError("UserName", "User name already exists. Please enter a different user name.");
                    }
                }
            }

            ViewBag.ProviderDisplayName = OAuthWebSecurity.GetOAuthClientData(provider).DisplayName;
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        // GET: /Account/ExternalLoginFailure

        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        [AllowAnonymous]
        [ChildActionOnly]
        public ActionResult ExternalLoginsList(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return PartialView("_ExternalLoginsListPartial", OAuthWebSecurity.RegisteredClientData);
        }

        [ChildActionOnly]
        public ActionResult RemoveExternalLogins()
        {
            ICollection<OAuthAccount> accounts = OAuthWebSecurity.GetAccountsFromUserName(User.Identity.Name);
            List<ExternalLogin> externalLogins = new List<ExternalLogin>();
            foreach (OAuthAccount account in accounts)
            {
                AuthenticationClientData clientData = OAuthWebSecurity.GetOAuthClientData(account.Provider);

                externalLogins.Add(new ExternalLogin
                {
                    Provider = account.Provider,
                    ProviderDisplayName = clientData.DisplayName,
                    ProviderUserId = account.ProviderUserId,
                });
            }

            ViewBag.ShowRemoveButton = externalLogins.Count > 1 || OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(User.Identity.Name));
            return PartialView("_RemoveExternalLoginsPartial", externalLogins);
        }


        [HttpGet]
        [Authorize(Roles = "admin,usuario,organizador,adminTorneio,adminTorneioTenis")]
        public ActionResult Excluir(int Id)
        {
            string nomeUsuario = string.Empty;
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            if ((perfil.Equals("admin")) || (perfil.Equals("organizador")) || (WebSecurity.GetUserId(User.Identity.Name) == Id))
            {
                var usuario = db.UserProfiles.Find(Id);
                nomeUsuario = usuario.nome;
                if (usuario.situacao == "pendente")
                {
                    var listaJogos = db.Jogo.Where(j => j.desafiado_id == Id || j.desafiante_id == Id).ToList();
                    if (listaJogos.Count == 0)
                    {
                        db.Database.ExecuteSqlCommand("delete from rancking where USERPROFILE_ID=" + Id);
                        db.Database.ExecuteSqlCommand("Delete from webpages_UsersInRoles where UserId=" + Id);
                        db.Database.ExecuteSqlCommand("Delete from UserProfile where UserId=" + Id);
                        db.Database.ExecuteSqlCommand("delete from inscricaotorneio where UserId=" + Id);
                    }
                    else
                    {
                        ViewBag.MsgErro = "Não foi possível excluir o usuário pois ele já possui jogo(s) realizado(s).";
                    }
                }
            }
            return RedirectToAction("ListarUsuarios", new { msg = $"Jogador {nomeUsuario} excluído com sucesso." });
        }

        [HttpGet]
        [Authorize(Roles = "admin,organizador,usuario,adminTorneio,adminTorneioTenis")]
        public ActionResult EditaUsuario(string UserName, bool isAlterarFoto = false, string ConfirmaSenha = "", string ConfirmaUnificacaoConta = "")
        {
            ViewBag.isAlterarFoto = isAlterarFoto;
            var usuario = db.UserProfiles.Find(WebSecurity.GetUserId(UserName));
            ViewBag.solicitarAtivacao = "";
            ViewBag.classeId = new SelectList(db.Classe.Where(c => c.barragemId == usuario.barragemId && c.ativa == true).ToList(), "Id", "nome", usuario.classeId);
            if (ConfirmaSenha != "" && ConfirmaSenha == "OK")
            {
                ViewBag.ConfirmaSenha = "OK";
                ViewBag.ConfirmaSenhaOKMsg = "Senha alterada com sucesso.";
            }
            if (ConfirmaSenha != "" && ConfirmaSenha == "NOTOK")
            {
                ViewBag.ConfirmaSenha = "NOTOK";
                ViewBag.ConfirmaSenhaNOTOKMsg = "Não foi possível trocar a senha!";
            }
            if (ConfirmaUnificacaoConta == "ok")
            {
                ViewBag.ConfirmaUnificacaoConta = "Conta unificada com sucesso.";
            }
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            if (perfil.Equals("admin"))
            {
                var contaDuplicada = db.UserProfiles.Where(u => u.email == usuario.email && u.UserId != usuario.UserId && u.situacao == "torneio").ToList();
                if (contaDuplicada.Count() > 0)
                {
                    ViewBag.MigrarConta = contaDuplicada[0].UserId;
                }
            }
            return View(usuario);
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public ActionResult UnificarConta(int userIdOrigem, int userIdDestino, string UserName)
        {
            EfetuarUnificacaoContas(userIdOrigem, userIdDestino);
            return RedirectToAction("EditaUsuario", "Account", new { UserName = UserName, isAlterarFoto = false, ConfirmaSenha = "", ConfirmaUnificacaoConta = "ok" });
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public ActionResult UnificarContaUsuario(int userIdOrigem, int userIdDestino)
        {
            EfetuarUnificacaoContas(userIdOrigem, userIdDestino);
            return RedirectToAction("UsuariosDuplicado");
        }

        private void EfetuarUnificacaoContas(int userIdOrigem, int userIdDestino)
        {
            db.Database.ExecuteSqlCommand("update userprofile set situacao = 'inativo' where userId =" + userIdOrigem);
            db.Database.ExecuteSqlCommand("update jogo set desafiante_id =" + userIdDestino + " where desafiante_id =" + userIdOrigem);
            db.Database.ExecuteSqlCommand("update jogo set desafiado_id =" + userIdDestino + " where desafiado_id =" + userIdOrigem);
            db.Database.ExecuteSqlCommand("update inscricaotorneio set userId =" + userIdDestino + " where userId =" + userIdOrigem);
            db.Database.ExecuteSqlCommand("update snapshotranking set userid =" + userIdDestino + " where userid =" + userIdOrigem);

            var rankingsUsuario = db.SnapshotRanking
                .Where(x => x.UserId == userIdDestino)
                .GroupBy(g => new { g.UserId, g.CategoriaId, g.SnapshotId, g.LigaId })
                .Select(s => new { UserId = s.Key.UserId, CategoriaId = s.Key.CategoriaId, SnapshotId = s.Key.SnapshotId, LigaId = s.Key.LigaId, QtdeRegistros = s.Count() })
                .ToList();

            foreach (var ranking in rankingsUsuario.Where(x => x.QtdeRegistros > 1))
            {
                //Soma pontuação do usuário e mantém somente 1 registro com a pontuação total
                var snapshots = db.SnapshotRanking
                    .Where(x => x.UserId == ranking.UserId && x.CategoriaId == ranking.CategoriaId && x.SnapshotId == ranking.SnapshotId && x.LigaId == ranking.LigaId)
                    .ToList();

                var itemAtualizacao = snapshots.First();
                itemAtualizacao.Pontuacao = snapshots.Sum(x => x.Pontuacao);
                db.Entry(itemAtualizacao).State = EntityState.Modified;
                db.SaveChanges();

                foreach (var itemExcluir in snapshots.Where(x => x.Id != itemAtualizacao.Id))
                {
                    db.SnapshotRanking.Remove(itemExcluir);
                    db.SaveChanges();
                }

                //Refaz a posição dos usuários no ranking
                List<SnapshotRanking> rankingAtual = db.SnapshotRanking
                    .Where(sr => sr.LigaId == itemAtualizacao.LigaId && sr.CategoriaId == itemAtualizacao.CategoriaId && sr.SnapshotId == itemAtualizacao.SnapshotId)
                    .OrderByDescending(sr => sr.Pontuacao)
                    .ToList();

                SnapshotRankingUtil.GerarPosicoesRanking(rankingAtual);
                foreach (SnapshotRanking rankingPosicao in rankingAtual)
                {
                    db.Entry(rankingPosicao).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }
        }

        [Authorize(Roles = "admin")]
        public ActionResult acertoDuplicacao()
        {
            List<Rancking> ranking = db.Rancking.ToList();
            int qtdd = 0;
            foreach (var item in ranking)
            {
                int existe = db.Rancking.Where(r => r.rodada_id == item.rodada_id && r.userProfile_id == item.userProfile_id).Count();
                if (existe > 1)
                {
                    db.Database.ExecuteSqlCommand("delete from rancking where id=" + item.Id);
                    qtdd = qtdd + 1;
                }
            }
            ViewBag.Retorno = qtdd;
            return View();
        }


        public ActionResult ConverterImagem()
        {

            string filePath = "/Content/images/Photo";
            foreach (string file in System.IO.Directory.GetFiles(Server.MapPath(filePath)))
            {
                string extension = System.IO.Path.GetExtension(file);
                if (extension == ".png")
                {
                    string name = System.IO.Path.GetFileNameWithoutExtension(file);
                    string path = System.IO.Path.GetDirectoryName(file);
                    Image png = Image.FromFile(file);
                    png.Save(Server.MapPath(filePath + "/" + name + ".jpg"), System.Drawing.Imaging.ImageFormat.Jpeg);
                    png.Dispose();
                }
            }

            return null;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,organizador,usuario,adminTorneio,adminTorneioTenis")]
        public ActionResult EditaUsuario(UserProfile model, string avatarCropped)
        {
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            string situacaoAtual = "";

            if (model.barragem == null)
            {
                model.barragem = db.BarragemView.Find(model.barragemId);
            }

            if ((!perfil.Equals("admin")) && (!perfil.Equals("organizador")) && (WebSecurity.GetUserId(User.Identity.Name) != model.UserId))
            {
                ViewBag.MsgErro = string.Format("Você não tem permissão para alterar este usuário '{0}'", model.nome);
                ViewBag.classeId = new SelectList(db.Classe.Where(c => c.barragemId == model.barragemId && c.ativa == true).ToList(), "Id", "nome");
                return View(model);
            }
            if (perfil.Equals("organizador"))
            {
                var userId = WebSecurity.GetUserId(User.Identity.Name);
                var brId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
                if (brId != model.barragemId)
                {
                    ViewBag.MsgErro = string.Format("Você não tem permissão para alterar este usuário '{0}'", model.nome);
                    ViewBag.classeId = new SelectList(db.Classe.Where(c => c.barragemId == model.barragemId && c.ativa == true).ToList(), "Id", "nome");
                    return View(model);
                }
            }
            if (ModelState.IsValid)
            {
                //UserProfile usuario = null;
                try
                {
                    if (!Funcoes.IsValidEmail(model.email))
                    {
                        ViewBag.MsgErro = string.Format("E-mail inválido. '{0}'", model.email);
                        ViewBag.classeId = new SelectList(db.Classe.Where(c => c.barragemId == model.barragemId && c.ativa == true).ToList(), "Id", "nome");
                        return View(model);
                    }
                    if (!String.IsNullOrEmpty(avatarCropped))
                    {
                        string filePath = ProcessImage(avatarCropped, model.UserId);
                        model.fotoURL = filePath;
                    }
                    else
                    {
                        model.fotoURL = (from up in db.UserProfiles where up.UserId == model.UserId select up.fotoURL).Single();
                    }
                    model.dataInicioRancking = (from up in db.UserProfiles where up.UserId == model.UserId select up.dataInicioRancking).Single();
                    situacaoAtual = (from up in db.UserProfiles where up.UserId == model.UserId select up.situacao).Single();
                    db.Entry(model).State = EntityState.Modified;
                    db.SaveChanges();
                }
                catch (MembershipCreateUserException ex)
                {
                    ViewBag.MsgErro = ex.Message;
                }
                try
                {
                    if ((situacaoAtual.ToLower().Equals("desativado")) && (model.situacao.ToLower().Equals("ativo")))
                    {
                        model.isRanckingGerado = false;
                    }
                    gerarRankingInicial(model, perfil);
                    ViewBag.Ok = "ok";
                }
                catch (Exception e)
                {
                    ViewBag.MsgErro = "";
                    if (e.InnerException == null)
                    {
                        ViewBag.MsgErro = "Erro ao gerar a pontuação inicial do usuário: " + e.Message;
                        ViewBag.DetalheErro = e.StackTrace;
                    }
                    else
                    {
                        ViewBag.MsgErro = "Erro ao gerar a pontuação inicial do usuário: " + e.Message + ", " + e.InnerException.Message;
                        ViewBag.DetalheErro = e.InnerException.StackTrace;
                    }
                    ViewBag.classeId = new SelectList(db.Classe.Where(c => c.barragemId == model.barragemId && c.ativa == true).ToList(), "Id", "nome");
                    return View(model);

                }

            }
            if (((perfil.Equals("admin")) || (!perfil.Equals("organizador"))) && (WebSecurity.GetUserId(User.Identity.Name) != model.UserId))
            {
                return RedirectToAction("ListarUsuarios", "Account", new { filtroSituacao = "todos", filtroBarragem = model.barragemId, msg = "ok" });
            }
            ViewBag.classeId = new SelectList(db.Classe.Where(c => c.barragemId == model.barragemId && c.ativa == true).ToList(), "Id", "nome");

            return View(model);
        }

        private int getPontuacaoPorNivel(Rodada rodada, string nivelDeJogo)
        {
            int qtddJogadores = 0;
            int posicao = 0;
            var rodadaId = 0;
            if (rodada != null)
            {
                rodadaId = rodada.Id;
            }
            try
            {
                var ranking = db.Rancking.Where(r => r.rodada_id == rodadaId && r.posicao != 0).ToList();
                qtddJogadores = ranking.Count();
                int totalAcumulado = 50;
                if (nivelDeJogo == "intermediario")
                {
                    totalAcumulado = 40;
                    posicao = qtddJogadores / 2;
                }
                else if (nivelDeJogo == "avancado")
                {
                    totalAcumulado = 60;
                    posicao = qtddJogadores / 6;
                }
                else
                {
                    totalAcumulado = 20;
                    posicao = qtddJogadores - (qtddJogadores / 3);
                }
                if (posicao == 0)
                {
                    return totalAcumulado;
                }
                var rankingPosicao = ranking.Where(p => p.posicao == posicao).FirstOrDefault();
                totalAcumulado = Convert.ToInt32(rankingPosicao.totalAcumulado);
                return totalAcumulado;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message + ": qtddJogadores:" + qtddJogadores + ", posicao:" + posicao + ", rodada: " + rodada.Id);
            }
        }

        private int getPontuacaoPorClasse(Rodada rodada, int? classeId)
        {
            int qtddJogadores = 0;
            int posicao = 0;
            var rodadaId = 0;
            int totalAcumulado = 50;
            if (rodada != null)
            {
                rodadaId = rodada.Id;
            }
            try
            {
                var ranking = db.Rancking.Where(r => r.rodada_id == rodadaId && r.posicao != 0 && r.classeId == classeId).OrderByDescending(r => r.totalAcumulado).ToList();
                qtddJogadores = ranking.Count();
                if (qtddJogadores > 1)
                {
                    posicao = (qtddJogadores / 2);
                    var rankingPosicao = ranking[posicao];// .ToArray()[posicao];
                    totalAcumulado = Convert.ToInt32(rankingPosicao.totalAcumulado);
                }
                return totalAcumulado;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message + ": qtddJogadores:" + qtddJogadores + ", posicao:" + posicao + ", rodada: " + rodada.Id);
            }
        }

        private void gerarRankingInicial(UserProfile model, string perfil)
        {
            if (((perfil.Equals("admin")) || (perfil.Equals("organizador"))) && (!model.isRanckingGerado))
            {
                List<Rodada> rodadas = db.Rodada.Where(r => r.isAberta == false && r.barragemId == model.barragemId).OrderByDescending(r => r.Id).Take(10).ToList();
                Rancking ranking = null;
                Rodada rodada = null;
                if (rodadas.Count() != 0)
                {
                    rodada = rodadas[0];
                }
                int totalAcumulado = getPontuacaoPorClasse(rodada, model.classeId);
                int qtddRodadasSeraoGeradas = 0;
                double pontuacaoAtual = 0;
                double pontuacao = 0;
                foreach (var item in rodadas)
                {
                    int existe = db.Rancking.Where(r => r.rodada_id == item.Id && r.userProfile_id == model.UserId).Count();
                    if (existe == 0)
                    {
                        qtddRodadasSeraoGeradas++;
                    }
                    else
                    {
                        var pontuacaoNaRodada = db.Rancking.Where(r => r.rodada_id == item.Id && r.userProfile_id == model.UserId).SingleOrDefault().pontuacao;
                        pontuacaoAtual = pontuacaoAtual + pontuacaoNaRodada;
                    }
                }
                if (qtddRodadasSeraoGeradas > 0)
                {
                    pontuacao = Math.Round((totalAcumulado - pontuacaoAtual) / qtddRodadasSeraoGeradas, 1);
                }
                foreach (var item in rodadas)
                {
                    int existe = db.Rancking.Where(r => r.rodada_id == item.Id && r.userProfile_id == model.UserId).Count();
                    if (existe == 0)
                    {
                        ranking = new Rancking();
                        ranking.rodada_id = item.Id;
                        ranking.pontuacao = pontuacao;
                        ranking.posicao = 0;
                        ranking.totalAcumulado = totalAcumulado;
                        ranking.userProfile_id = model.UserId;
                        ranking.classeId = model.classeId;
                        db.Rancking.Add(ranking);
                        db.SaveChanges();
                    }
                }
                model.isRanckingGerado = true;
                db.Entry(model).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,organizador,usuario,adminTorneio,adminTorneioTenis")]
        public ActionResult AtualizaStatus(String situacao, int userId)
        {
            try
            {
                ViewBag.MsgStatusSucesso = false;
                var user = db.UserProfiles.Where(u => u.UserId == userId).FirstOrDefault();
                if (user != null)
                {
                    user.situacao = situacao;
                    user.logAlteracao = User.Identity.Name;
                    db.Entry(user).State = EntityState.Modified;
                    db.SaveChanges();
                    ModelState.AddModelError(String.Empty, "You must complete.");
                    ViewBag.Sucesso = true;
                    ViewBag.MsgAlerta = "Status atualizado com sucesso";
                }
            }
            catch
            {

            }
            return RedirectToAction("Index3", "Home", new { ViewBag.Sucesso, ViewBag.MsgAlerta });
        }


        [HttpPost]
        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis")]
        public ActionResult PenderJogador(int userId)
        {
            try
            {
                var user = db.UserProfiles.Where(u => u.UserId == userId).FirstOrDefault();
                if (user != null)
                {
                    user.situacao = "pendente";
                    user.logAlteracao = User.Identity.Name;

                    db.Entry(user).State = EntityState.Modified;
                    db.SaveChanges();
                    return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { erro = "Usuário não encontrado", retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis")]
        public ActionResult AtivaUsuario(int userId)
        {
            try
            {
                var user = db.UserProfiles.Where(u => u.UserId == userId).FirstOrDefault();
                if (user != null)
                {
                    // se o jogador estiver desativado e for ativar novamente o sistema deverar gerar o ranking dele novamente
                    if ((user.situacao.ToLower().Equals("desativado")) && (user.isRanckingGerado))
                    {
                        user.isRanckingGerado = false;
                    }
                    user.situacao = "ativo";
                    user.logAlteracao = User.Identity.Name;
                    try
                    {
                        gerarRankingInicial(user, "organizador");
                    }
                    catch (Exception e)
                    {
                        if (e.InnerException == null)
                        {
                            return Json(new { erro = "Erro ao gerar a pontuação inicial do usuário: " + e.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
                        }
                        else
                        {
                            return Json(new { erro = "Erro ao gerar a pontuação inicial do usuário: " + e.Message + ", " + e.InnerException.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
                        }
                    }
                    db.Entry(user).State = EntityState.Modified;
                    db.SaveChanges();
                    return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { erro = "Usuário não encontrado", retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        [Authorize(Roles = "admin, organizador, usuario")]
        public ActionResult MinhaPontuacao()
        {
            var userProfile = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
            var userId = userProfile.UserId;
            List<Rancking> ranckingJogador = db.Rancking.Where(r => r.userProfile_id == userId).OrderByDescending(r => r.rodada_id).Take(10).ToList();
            //
            int quantidadeDeRodadasParaPontuacao = 10;
            int idRodada = ranckingJogador[0].rodada_id;
            Rodada rodadaAtual = db.Rodada.Where(r => r.Id == idRodada).Single();
            if (rodadaAtual.temporada.iniciarZerada)
            {
                int quantidadeDeRodadasRealizadas = db.Rodada.Where(r => r.temporadaId == rodadaAtual.temporadaId && r.isAberta == false).Count();
                if (quantidadeDeRodadasRealizadas < quantidadeDeRodadasParaPontuacao)
                {
                    quantidadeDeRodadasParaPontuacao = quantidadeDeRodadasRealizadas;
                }
                ranckingJogador = ranckingJogador.Take(quantidadeDeRodadasParaPontuacao).ToList();
            }
            //
            var dataJogos = db.Jogo.Where(r => (r.desafiado_id == userId || r.desafiante_id == userId) && r.rodada.isAberta == false).OrderByDescending(r => r.rodada_id).Take(quantidadeDeRodadasParaPontuacao).Select(r => r.dataCadastroResultado).ToList<DateTime?>();
            ViewBag.RanckingJogador = ranckingJogador;
            ViewBag.posicaoJogador = ranckingJogador[0].posicaoClasse + "º";
            ViewBag.pontuacaoAtual = ranckingJogador[0].totalAcumulado;
            //ViewBag.dataFimRodada = ranckingJogador[0].rodada.dataFim;
            //ViewBag.dataJogos = dataJogos;
            //ViewBag.pontuacaoAtual = ranckingJogador.Sum(r=>r.pontuacao);
            foreach (var rkg in ranckingJogador)
            {
                var dataRealizacaoJogo = db.Jogo.Where(r => (r.desafiado_id == userId || r.desafiante_id == userId) && r.rodada_id == rkg.rodada_id && (r.situacao_Id == 4 || r.situacao_Id == 5)).Select(r => r.dataCadastroResultado).FirstOrDefault();
                if (dataRealizacaoJogo != null && dataRealizacaoJogo > ranckingJogador[0].rodada.dataFim)
                {
                    rkg.classeId = 1; // se jogo atrasado, usando este campo como gambiarra, não deveria.
                }
                else
                {
                    rkg.classeId = 0; // se jogo não atrasado, usando este campo como gambiarra, não deveria.
                }
            }



            return View(userProfile);
        }

        #region Helpers
        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index3", "Home");
            }
        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
        }

        internal class ExternalLoginResult : ActionResult
        {
            public ExternalLoginResult(string provider, string returnUrl)
            {
                Provider = provider;
                ReturnUrl = returnUrl;
            }

            public string Provider { get; private set; }
            public string ReturnUrl { get; private set; }

            public override void ExecuteResult(ControllerContext context)
            {
                OAuthWebSecurity.RequestAuthentication(Provider, ReturnUrl);
            }
        }

        public string HaveFoto(string userName)
        {
            UserProfile usuario = null;
            usuario = db.UserProfiles.Where(u => u.UserName.Equals(userName)).Single();
            if (usuario.foto != null)
            {
                return "true";
            }
            return "false";

        }
        [AllowAnonymous]
        public FilePathResult BuscaFoto(int id = 0, string userName = "")
        {
            UserProfile usuario = null;
            if (id == 0)
            {
                usuario = db.UserProfiles.Where(u => u.UserName.Equals(userName)).Single();
            }
            else
            {
                usuario = db.UserProfiles.Find(id);
            }
            if (!String.IsNullOrEmpty(usuario.fotoURL))
            {
                return File(usuario.fotoURL, "image/jpg");
            }
            return File("/Content/image/sem-foto.png", "image/png");


        }

        [Authorize(Roles = "admin")]
        public ActionResult UsuariosDuplicado()
        {
            var situacoesExcecao = new List<string>();
            situacoesExcecao.Add("torneio");
            situacoesExcecao.Add("inativo");

            var dadosListagem =
                from usuario in db.UserProfiles
                join usuarioTorneio in (from usuario in db.UserProfiles where usuario.situacao == "torneio" select usuario)
                on usuario.email equals usuarioTorneio.email
                join barragem in db.Barragens
                on usuario.barragemId equals barragem.Id
                join barragemTorneio in db.Barragens
                on usuarioTorneio.barragemId equals barragemTorneio.Id
                where !situacoesExcecao.Contains(usuario.situacao)
                orderby usuario.dataInicioRancking descending
                select new UsuarioDuplicadoModel
                {
                    DataInicioRanking = usuario.dataInicioRancking,
                    Email = usuario.email,
                    NomeUsuarioBarragem = usuario.UserName,
                    NomeUsuarioTorneio = usuarioTorneio.UserName,
                    UsuarioBarragemId = usuario.UserId,
                    UsuarioTorneioId = usuarioTorneio.UserId,
                    NomeBarragemUsuarioBarragem = barragem.nome,
                    NomeBarragemUsuarioTorneio = barragemTorneio.nome
                };

            return View(dadosListagem);
        }

        public ActionResult ListarUsuarios(String filtroSituacao = "", int filtroBarragem = 0, int filtroCategoria = 0, string msg = "")
        {
            int idBarragem = filtroBarragem;
            if (msg == "ok")
            {
                ViewBag.Ok = "ok";
            }
            else if (!string.IsNullOrEmpty(msg))
            {
                ViewBag.Mensagem = msg;
                msg = "";
            }

            UserProfile usu = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
            ViewBag.situacao = usu.situacao;
            ViewBag.filtro = filtroSituacao;
            //ViewBag.filtroBarragem = filtroBarragem;

            List<UserProfile> usuarios;
            IQueryable<UserProfile> consulta = null;

            if (filtroSituacao == "")
            {
                consulta = db.UserProfiles.Where(u => u.situacao.Equals("ativo") || u.situacao.Equals("licenciado") || u.situacao.Equals("suspenso") || u.situacao.Equals("suspensoWO"));
            }
            else if (filtroSituacao == "todos")
            {
                consulta = db.UserProfiles.Where(u => !u.UserName.Equals("coringa"));
            }
            else
            {
                consulta = db.UserProfiles.Where(u => u.situacao.Equals(filtroSituacao));
            }

            if (filtroBarragem != 0)
            {
                consulta = consulta.Where(u => u.barragemId == filtroBarragem);
            }

            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];

            if (!perfil.Equals("admin"))
            {
                idBarragem = usu.barragemId;
                filtroBarragem = idBarragem;
                usuarios = consulta.Where(u => u.barragemId == usu.barragemId).OrderBy(u => u.nome).ToList();
            }
            else if (filtroBarragem == 0)
            {
                usuarios = consulta.OrderBy(u => u.nome).Take(100).ToList();
            }
            else
            {
                usuarios = consulta.OrderBy(u => u.nome).ToList();
            }

            if (idBarragem > 0)
            {
                var categorias = db.Classe.Where(c => c.barragemId == idBarragem && c.ativa == true).ToList();
                ViewBag.Categorias = categorias;
                ViewBag.filtroCategoria = filtroCategoria;

                if (filtroCategoria > 0)
                {
                    var categoriaSelecionada = categorias.FirstOrDefault(x => x.Id == filtroCategoria);
                    usuarios = consulta.Where(x => x.classe.nivel == categoriaSelecionada.nivel || (x.classeId == null && categoriaSelecionada.nivel == 1)).OrderBy(u => u.nome).ToList();
                }
            }
            else
            {
                ViewBag.Categorias = new List<Classe>();
                ViewBag.filtroCategoria = filtroCategoria;
            }

            ViewBag.filtroBarragem = new SelectList(db.BarragemView.OrderBy(x => x.nome), "Id", "nome", filtroBarragem);

            return View(usuarios);
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult ResetarSenha()
        {
            ViewBag.MsgErro = "";
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetarSenha(UserProfile model)
        {
            UserProfile user = null;
            try
            {

                user = db.UserProfiles.Where(u => u.UserName == model.UserName).FirstOrDefault();
                if (user != null)
                {
                    if (String.IsNullOrEmpty(user.email))
                    {
                        ViewBag.MsgErro = "Este usuário não possui e-mail cadastrado. Por favor, entre em contato com o administrador";
                        return View();
                    }
                    else
                    {
                        string confirmationToken = WebSecurity.GeneratePasswordResetToken(user.UserName);
                        EnviarMailSenha(confirmationToken, user.nome, user.email);
                        return View("ConfirmaEnvio");
                    }
                }
                else
                {
                    ViewBag.MsgErro = "Este usuário não existe.";
                    return View();
                }
            }
            catch (Exception ex)
            {
                ViewBag.MsgErro = ex.Message;
                return View();
            }
            finally
            {
                if (db != null)
                    db.Dispose();
            }
            //return View();
        }


        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis")]
        public ActionResult ResetarSenhaPeloOrganizador(string userName)
        {
            UserProfile user = null;
            try
            {

                user = db.UserProfiles.Where(u => u.UserName == userName).FirstOrDefault();
                if (user != null)
                {
                    if (String.IsNullOrEmpty(user.email))
                    {
                        ViewBag.MsgErro = "Este usuário não possui e-mail cadastrado. Por favor, entre em contato com o administrador";
                        return View();
                    }
                    else
                    {
                        string confirmationToken = WebSecurity.GeneratePasswordResetToken(userName);
                        return RedirectToAction("ConfirmaSenha", new { id = confirmationToken, userName = userName });
                    }
                }
                else
                {
                    ViewBag.MsgErro = "Este usuário não existe.";
                    return View();
                }
            }
            catch (Exception ex)
            {
                ViewBag.MsgErro = ex.Message;
                return View();
            }
            finally
            {
                if (db != null)
                    db.Dispose();
            }
        }

        private void EnviarMailSenha(string token, string nomeUsuario, string emailUsuario)
        {
            string hostHeader = this.Request.Headers["host"];
            string strUrl = string.Format("{0}://{1}/Account/ConfirmaSenha/{2}", this.Request.Url.Scheme, hostHeader, token);
            string strConteudo = "<html> <head> </head> <body> Prezado(a) " + nomeUsuario + ", <br /> Você fez uma solicitação de reinicio de senha. <br />";
            strConteudo += "Para continuar, clique no link abaixo: <br /> " + strUrl + " </body> </html>";

            Mail email = new Mail();
            email.SendEmail(emailUsuario, "recuperação de senha", strConteudo, Class.Tipos.FormatoEmail.Html);
            //email.assunto = "Solicitação de troca de senha";
            //email.conteudo = strConteudo;
            //email.formato = Class.Tipos.FormatoEmail.Html;
            //email.para = emailUsuario;
            //email.EnviarMail();

        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult ConfirmaSenha(string id, string userName = "")
        {
            ViewBag.MsgErro = "";
            if (userName != "")
            {
                ViewBag.username = userName;
            }
            ConfirmaSenhaModel model = new ConfirmaSenhaModel();
            model.TokenId = id;
            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmaSenha(ConfirmaSenhaModel model, string userName = "")
        {
            ViewBag.MsgErro = "";
            try
            {
                if (WebSecurity.ResetPassword(model.TokenId, model.Senha))
                {
                    ViewBag.MsgSucesso = "Senha alterada com sucesso.";
                    if (userName != "")
                    {
                        return Redirect(String.Format("/Account/EditaUsuario?UserName={0}&ConfirmaSenha=OK", userName));
                    }
                    return RedirectToAction("Login");
                }
                else
                {
                    if (userName != "")
                    {
                        return Redirect(String.Format("/Account/EditaUsuario?UserName={0}&ConfirmaSenha=NOTOK", userName));
                    }
                    ViewBag.MsgErro = "Não foi possível trocar a senha!";
                }
            }
            catch (InvalidOperationException ex)
            {
                ViewBag.MsgErro = "Não foi possível trocar a senha. Erro: " + ex.Message;
            }

            return View(model);
        }

        [AllowAnonymous]
        public ActionResult LoginBT(int torneioId = 0, string returnUrl = "", string Msg = "")
        {
            if ((User.Identity.IsAuthenticated) && (torneioId > 0))
            {
                if (returnUrl == "torneio")
                {
                    return RedirectToAction("Detalhes", "Torneio", new { id = torneioId });
                }
                return RedirectToAction(returnUrl, "Torneio", new { torneioId = torneioId });
            }
            var model = new VerificacaoCadastro();
            model.torneioId = torneioId;
            model.returnUrl = returnUrl;
            ViewBag.Msg = Msg;
            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult LoginBT(VerificacaoCadastro model)
        {
            if (ModelState.IsValid)
            {
                var registers = db.UserProfiles.Where(u => (u.email.Equals(model.email) || u.UserName.ToLower() == model.email.ToLower())).ToList();
                if (registers.Count() > 1)
                {
                    return RedirectToAction("ListaLoginsBT", "Account", model);
                }
                else if (registers.Count() > 0)
                {
                    var usuario = registers[0];
                    var msg = "";
                    if (usuario.barragem != null)
                    {
                        msg = "Olá, " + usuario.nome + " seu login foi localizado no ranking: " + usuario.barragem.nome + " entre com a sua senha.";
                    }
                    return RedirectToAction("LoginPasswordBT", new
                    {
                        returnUrl = model.returnUrl,
                        userName = usuario.UserName,
                        Msg = msg,
                        torneioId = model.torneioId
                    });
                }
                else if (!String.IsNullOrEmpty(model.returnUrl) && model.returnUrl.Equals("torneio"))
                {
                    return RedirectToAction("RegisterTorneio", new { email = model.email, torneioId = model.torneioId });
                }
                else
                {
                    return RedirectToAction("LoginBT", new { returnUrl = model.returnUrl, Msg = "Olá, Não encontramos nenhum cadastro com esse email ou usuário.", torneioId = model.torneioId });
                }

            }
            return View(model);
        }

        [AllowAnonymous]
        public ActionResult ListaLoginsBT(VerificacaoCadastro model)
        {
            var registers = db.UserProfiles.Where(u => u.situacao != "inativo" && (u.email.Equals(model.email) || u.UserName.ToLower() == model.email.ToLower())).ToList();
            var loginRegisters = new List<LoginRankingModel>();
            foreach (var item in registers)
            {
                var loginRankingModel = new LoginRankingModel();
                loginRankingModel.logoId = item.barragemId;
                loginRankingModel.userName = item.UserName;
                loginRankingModel.nomeRanking = item.barragem.nome;
                loginRankingModel.idRanking = 0;
                var snapshotRanking = db.SnapshotRanking.Where(s => s.UserId == item.UserId).Include(s => s.Liga).OrderByDescending(s => s.Id).Take(1).ToList();
                if (snapshotRanking.Count() > 0)
                {
                    loginRankingModel.nomeLiga = snapshotRanking[0].Liga.Nome;
                    loginRankingModel.idRanking = 1;
                }
                loginRegisters.Add(loginRankingModel);
            }
            loginRegisters = loginRegisters.OrderByDescending(l => l.idRanking).ToList();
            ViewBag.ReturnUrl = model.returnUrl;
            ViewBag.torneioId = model.torneioId;
            return View(loginRegisters);
        }

        [AllowAnonymous]
        public ActionResult LoginPasswordBT(string returnUrl = "", string userName = "", string Msg = "", int torneioId = 0)
        {
            if (User.Identity.IsAuthenticated)
            {
                string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
                if (perfil.Equals("admin") || perfil.Equals("organizador"))
                {
                    return RedirectToAction("Dashboard", "Home");
                }
                else if (perfil.Equals("adminTorneio") || perfil.Equals("adminTorneioTenis"))
                {
                    // se a mensagem estiver em branco, quer dizer que o organizador ainda não tem um ranking
                    if (Msg == "")
                    {
                        return RedirectToAction("CreateRankingLiga", "Liga");
                    }
                    return RedirectToAction("PainelControle", "Torneio");
                }
                return RedirectToAction("Index3", "Home");
            }
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.userName = userName;
            ViewBag.torneioId = torneioId;
            ViewBag.Msg = Msg;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult LoginPasswordBT(LoginModel model, string returnUrl, int torneioId = 0)
        {
            if (ModelState.IsValid && WebSecurity.Login(model.UserName, model.Password, persistCookie: model.RememberMe))
            {
                if ((!returnUrl.Equals("torneio")) && (!returnUrl.Contains("/Torneio/LancarResultado")))
                {
                    var usuario = db.UserProfiles.Find(WebSecurity.GetUserId(model.UserName));
                    if ((Roles.GetRolesForUser(model.UserName)[0]).Equals("adminTorneio") && usuario.barragemId == 0)
                    {
                        return RedirectToAction("CreateRankingLiga", "Liga");
                    }
                    Funcoes.CriarCookieBarragem(Response, Server, usuario.barragemId, usuario.barragem.nome, usuario.barragem.isBeachTenis);
                    if ((Roles.GetRolesForUser(model.UserName)[0]).Equals("parceiroBT"))
                    {
                        return RedirectToAction("Index", "Torneio");
                    }
                }
                else if (torneioId == 0)
                {
                    HttpCookie cookie = Request.Cookies["_barragemId"];
                    if (cookie != null)
                    {
                        var barragemId = Convert.ToInt32(cookie.Value.ToString());
                        var tn = db.Torneio.Where(t => t.barragemId == barragemId && t.isAtivo).OrderByDescending(t => t.Id).ToList();
                        if (tn.Count() > 0) { torneioId = tn[0].Id; }
                    }
                }
                if ((!String.IsNullOrEmpty(returnUrl)) && (returnUrl.Equals("EscolherDupla")) && (torneioId != 0))
                {
                    return RedirectToAction("EscolherDupla", "Torneio", new { id = torneioId });
                }
                if ((!String.IsNullOrEmpty(returnUrl)) && (returnUrl.Equals("torneio")) && (torneioId != 0))
                {
                    return RedirectToAction("Detalhes", "Torneio", new { id = torneioId });
                }
                else if ((!String.IsNullOrEmpty(returnUrl)) && (returnUrl.Contains("/Torneio/LancarResultado")))
                {
                    return RedirectToAction("LancarResultado", "Torneio");
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
                //return RedirectToLocal(returnUrl);
            }

            // If we got this far, something failed, redisplay form
            ViewBag.Msg = "O login ou a senha estão incorretos.";
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.userName = model.UserName;
            ViewBag.torneioId = torneioId;
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult ValidarDisponibilidadeInscricao(int torneioId, int categoriaId)
        {
            var respostaValidacao = new ResponseMessageWithStatus();
            try
            {
                var categoria = db.ClasseTorneio.Find(categoriaId);

                if (categoria.maximoInscritos == 0)
                {
                    respostaValidacao.status = "OK";
                }
                else
                {
                    var qtdInscritosCategoria = db.InscricaoTorneio.Count(x => x.torneio.Id == torneioId && x.classe == categoriaId);

                    if (categoria.isDupla)
                    {
                        if (categoria.maximoInscritos > qtdInscritosCategoria)
                        {
                            respostaValidacao.status = "OK";
                        }
                        else
                        {
                            var inscricoes = db.InscricaoTorneio.Where(x => x.torneioId == torneioId && x.classe == categoriaId);

                            var duplasFormadas = inscricoes.Where(x => x.parceiroDuplaId != null);
                            var idsParceirosDupla = duplasFormadas.Select(s => s.parceiroDuplaId);

                            var inscricoesSemParceiro = inscricoes.Where(x => x.parceiroDuplaId == null);
                            var duplasNaoFormadas = inscricoesSemParceiro.Where(x => !idsParceirosDupla.Contains(x.userId)).ToList();

                            if (!duplasNaoFormadas.Any())
                            {
                                respostaValidacao.status = "ESGOTADO";
                            }
                            else
                            {
                                respostaValidacao.status = "ESCOLHER_DUPLA";
                                respostaValidacao.retorno = duplasNaoFormadas;
                            }
                        }
                    }
                    else
                    {
                        if (categoria.maximoInscritos > qtdInscritosCategoria)
                        {
                            respostaValidacao.status = "OK";
                        }
                        else
                        {
                            respostaValidacao.status = "ESGOTADO";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                respostaValidacao = new ResponseMessageWithStatus { erro = ex.Message, status = "ERRO" };
            }

            return Json(respostaValidacao, "text/plain", JsonRequestBehavior.AllowGet);
        }

        private static string ErrorCodeToString(MembershipCreateStatus createStatus)
        {
            // See http://go.microsoft.com/fwlink/?LinkID=177550 for
            // a full list of status codes.
            switch (createStatus)
            {
                case MembershipCreateStatus.DuplicateUserName:
                    return "User name already exists. Please enter a different user name.";

                case MembershipCreateStatus.DuplicateEmail:
                    return "A user name for that e-mail address already exists. Please enter a different e-mail address.";

                case MembershipCreateStatus.InvalidPassword:
                    return "The password provided is invalid. Please enter a valid password value.";

                case MembershipCreateStatus.InvalidEmail:
                    return "The e-mail address provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidAnswer:
                    return "The password retrieval answer provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidQuestion:
                    return "The password retrieval question provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidUserName:
                    return "The user name provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.ProviderError:
                    return "The authentication provider returned an error. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                case MembershipCreateStatus.UserRejected:
                    return "The user creation request has been canceled. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                default:
                    return "An unknown error occurred. Please verify your entry and try again. If the problem persists, please contact your system administrator.";
            }
        }
        #endregion
    }

}
