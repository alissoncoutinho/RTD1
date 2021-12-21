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
using System.Dynamic;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using System.IO;

namespace Barragem.Controllers
{
    public class LigaController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();

        [Authorize(Roles = "admin,organizador, adminTorneio")]
        public ActionResult Index(string msg = "", string detalheErro = "")
        {
            List<BarragemLiga> ligasDaBarragem = null;
            List<Liga> ligas = null;
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            int barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
            if (perfil.Equals("admin"))
            {
                ligas = db.Liga.OrderByDescending(l => l.Id).ToList();
            }
            else
            {
                ligasDaBarragem = db.BarragemLiga.Include(r=>r.Liga).Where(r => r.BarragemId == barragemId).OrderByDescending(c => c.Id).ToList();
                ligas = new List<Liga>();
                foreach (BarragemLiga ligaBarragem in ligasDaBarragem)
                {
                    ligas.Add(ligaBarragem.Liga);
                }
            }

            return View(ligas);
        }

        [Authorize(Roles = "admin, organizador, usuario, adminTorneio")]
        public ActionResult Create()
        {
            
            Liga liga = new Liga();
            /*liga.Nome = "liga teste";
            var classes = new List<ClasseLiga>();
            classes.Add(new ClasseLiga { Nome = "1 classe", Nivel = "1" });
            dynamic model = new ExpandoObject();
            model.Classes = classes;
            model.Liga = liga;
            model.Rankings = new List<BarragemLiga>();*/
            return View(liga);
        }

        [Authorize(Roles = "admin, organizador, adminTorneio")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Liga liga, string modalidadeBarragem)
        {
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            var user = db.UserProfiles.Find(userId);
            liga.barragemId = user.barragemId;
            if (modalidadeBarragem == "1") {
                liga.isModeloTodosContraTodos = false;
            } else {
                liga.isModeloTodosContraTodos = true;
            }
            if (ModelState.IsValid) {
                db.Liga.Add(liga);
                db.SaveChanges();
                if (perfil.Equals("adminTorneio") || perfil.Equals("organizador")) {
                    var bL = new BarragemLiga();
                    bL.BarragemId = user.barragemId;
                    bL.LigaId = liga.Id;
                    db.BarragemLiga.Add(bL);
                    db.SaveChanges();
                }
                return RedirectToAction("PainelControle", "Torneio");
            }

            return View(liga);
        }

        public ActionResult Edit(int idLiga = 0)
        {
            Liga liga = db.Liga.Find(idLiga);
            ViewBag.idLiga = idLiga;
            ViewBag.nomeLiga = liga.Nome;

            var classes = db.ClasseLiga.Where(c => c.LigaId == idLiga).ToList();
            ViewBag.flag = "classes";
            return View(classes);
        }

        [Authorize(Roles = "admin, organizador, adminTorneio")]
        [HttpPost]
        public ActionResult EditNome(int idLiga, string nomeLiga, string modalidadeBarragem)
        {
            try { 
                Liga liga = db.Liga.Find(idLiga);
                liga.Nome = nomeLiga;
                
                var jaExisteTorneio = db.TorneioLiga.Where(l => l.LigaId == idLiga).Any();
                if (jaExisteTorneio)
                {
                    if (((liga.isModeloTodosContraTodos) && (modalidadeBarragem=="1")) || ((!liga.isModeloTodosContraTodos) && (modalidadeBarragem == "2")))
                    {
                        ViewBag.MsgErro = "Não é permitido alterar a modalidade do circuito, pois já existem torneios em andamento vinculados a ele. ";
                        return View("Edit");
                    }
                }
                if (modalidadeBarragem == "1")
                {
                    liga.isModeloTodosContraTodos = false;
                }
                else
                {
                    liga.isModeloTodosContraTodos = true;
                }
                db.Entry(liga).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Edit", new { idLiga = idLiga });
                //return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Edit", new { idLiga = idLiga });
                //return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            
        }

        [Authorize(Roles = "admin, organizador, adminTorneio")]
        public ActionResult EditNomeClasse(int id, string nomeClasse)
        {
            ClasseLiga classeLiga = null;
            try
            {
                classeLiga = db.ClasseLiga.Find(id);
                classeLiga.Nome = nomeClasse;
                db.Entry(classeLiga).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Edit", new { idLiga = classeLiga.LigaId });
                //return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Edit", new { idLiga = classeLiga.LigaId });
                //return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }

        }

        public ActionResult EditClasses(int idLiga = 0)
        {
            Liga liga = db.Liga.Find(idLiga);
            ViewBag.idLiga = idLiga;
            ViewBag.nomeLiga = liga.Nome;
            
            List<Categoria> categorias = new List<Categoria>();
            categorias.Add(new Categoria { Id = 0, Nome = "" });
            categorias.AddRange(db.Categoria.OrderBy(c => c.Nome).ToList());
            ViewBag.Categorias = categorias;

            var classes = db.ClasseLiga.Where(c => c.LigaId == idLiga).ToList();
            ViewBag.flag = "classes";
            return View(classes);
        }

        [HttpPost]
        public ActionResult AddClasse(int idLiga, String nome, String idCategoria, bool isDupla=false)
        {
            try {
                int idCat = Int32.Parse(idCategoria);
                if (nome == "")
                {
                    throw new Exception("Por favor preencha os campos obrigatórios(nome e categoria).");
                }
                Categoria cat = null;
                if (idCat == 0)
                {
                    Liga liga = db.Liga.Find(idLiga);
                    cat = new Categoria();
                    cat.Nome = nome;
                    cat.isDupla = isDupla;
                    cat.rankingId = (int) liga.barragemId;
                    db.Categoria.Add(cat);
                    db.SaveChanges();
                    idCat = cat.Id;
                } else {
                    cat = db.Categoria.Find(idCat);
                }
                ClasseLiga cl = new ClasseLiga();
                cl.LigaId = idLiga;
                cl.Nome = nome;
                cl.CategoriaId = idCat;
                db.ClasseLiga.Add(cl);
                db.SaveChanges();
                return RedirectToAction("Edit", new { idLiga = idLiga });
                //return Json(new { erro = "", retorno = 1 , nome = cl.Nome, categoria = cat.Nome, IdClasseLiga = cl.Id}, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Edit", new { idLiga = idLiga });
                //return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult RemoveClasse(String id)
        {
            ClasseLiga cl = null;
            try
            {
                cl = db.ClasseLiga.Find(Int32.Parse(id));
                db.ClasseLiga.Remove(cl);
                db.SaveChanges();
                return RedirectToAction("Edit", new { idLiga = cl.LigaId });
                //return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Edit", new { idLiga = cl.LigaId });
                //return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult EditBarragens(int idLiga = 0)
        {
            ViewBag.flag = "barragens";

            Liga liga = db.Liga.Find(idLiga);
            ViewBag.idLiga = idLiga;
            ViewBag.nomeLiga = liga.Nome;

            List<Barragens> barragens = new List<Barragens>();
            barragens.Add(new Barragens { Id = 0, nome = "" });
            barragens.AddRange(db.Barragens.OrderBy(b => b.nome).ToList());
            ViewBag.Barragens = barragens;

            var barragensDaLiga = db.BarragemLiga.Where(bl => bl.LigaId == idLiga).ToList();
            return View(barragensDaLiga);
        }

        [HttpPost]
        public ActionResult AddBarragem(String idLiga, String idBarragem, String TipoTorneio)
        {
            try
            {
                int idBarra = Int32.Parse(idBarragem);
                if (idBarragem == "" || idBarra == 0 || TipoTorneio == "")
                {
                    throw new Exception("Por favor selecione o ranking a ser adicionado e o tipo de torneio.");
                }
                var ligaId = Int32.Parse(idLiga);
                var seJaExiste = db.BarragemLiga.Where(b => b.LigaId == ligaId && b.BarragemId == idBarra).Count();
                if (seJaExiste > 0)
                {
                    throw new Exception("Este ranking já está adicionado a esta liga.");
                }
                BarragemLiga bl = new BarragemLiga();
                bl.LigaId = ligaId;
                bl.BarragemId = idBarra;
                bl.TipoTorneio = TipoTorneio;
                db.BarragemLiga.Add(bl);
                db.SaveChanges();
                Barragens barra = db.Barragens.Find(idBarra);
                return Json(new { erro = "", retorno = 1, Nome = barra.nome, IdBarragemLiga = bl.Id , TipoTorneio = TipoTorneio}, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult RemoveBarragem(String id)
        {
            try
            {
                BarragemLiga bl = db.BarragemLiga.Find(Int32.Parse(id));
                db.BarragemLiga.Remove(bl);
                db.SaveChanges();
                return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult AlterarTipoTorneio(String id, String TipoTorneio)
        {
            try
            {
                BarragemLiga bl = db.BarragemLiga.Find(Int32.Parse(id));
                bl.TipoTorneio = TipoTorneio;
                db.Entry(bl).State = EntityState.Modified;
                db.SaveChanges();
                return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        [Authorize(Roles = "admin, organizador, adminTorneio")]
        public ActionResult CreateRankingLiga()
        {
            return View();
        }

        [Authorize(Roles = "admin, organizador, adminTorneio")]
        public ActionResult FazerCargaCidades()
        {
            var filePath = "/Content/cidades3.json";
            var path = Server.MapPath(filePath);
            var arquivoExterno = System.IO.File.ReadAllText(path);
            JsonTextReader reader = new JsonTextReader(new StringReader(arquivoExterno));
            reader.SupportMultipleContent = true;
            while (true)
            {
                if (!reader.Read())
                {
                    break;
                }

                JsonSerializer serializer = new JsonSerializer();
                CidadeTemp cidade = serializer.Deserialize<CidadeTemp>(reader);
                Cidade cd = new Cidade
                {
                    nome = cidade.nome,
                    uf = cidade.microrregiao.mesorregiao.uf.sigla
                };
                db.Cidade.Add(cd);
                db.SaveChanges();
            }



            return View();
        }

        [Authorize(Roles = "admin, organizador, adminTorneio")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateRankingLiga(CreateBarragemLiga createBarragemLiga)
        {
            try
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    if (ModelState.IsValid)
                    {
                        Barragens barragens = new Barragens();
                        barragens.nome = createBarragemLiga.nomeBarragem;
                        barragens.cidade = createBarragemLiga.cidade;
                        if (createBarragemLiga.modalidadeBarragem == "1")
                        {
                            barragens.isModeloTodosContraTodos = false;
                        } else {
                            barragens.isModeloTodosContraTodos = true;
                        }
                        barragens.soTorneio = true;
                        barragens.isBeachTenis = true;
                        barragens.isTeste = true;
                        barragens.isAtiva = true;

                        UserProfile usuario = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
                        barragens.email = usuario.email;
                        barragens.regulamento = db.Regra.Find(3).descricao;
                        db.Barragens.Add(barragens);
                        db.SaveChanges();

                        var liga = new Liga();
                        liga.Nome = createBarragemLiga.nomeLiga;
                        liga.isAtivo = true;
                        liga.barragemId = barragens.Id;
                        db.Liga.Add(liga);
                        db.SaveChanges();
                        var barragemLiga = new BarragemLiga();
                        barragemLiga.LigaId = liga.Id;
                        barragemLiga.BarragemId = barragens.Id;
                        db.BarragemLiga.Add(barragemLiga);

                        usuario.barragemId = barragens.Id;
                        db.Entry(usuario).State = EntityState.Modified;
                        db.SaveChanges();

                        scope.Complete();
                        Funcoes.CriarCookieBarragem(Response, Server, barragens.Id, barragens.nome);
                        return RedirectToAction("PainelControle", "Torneio", new { msg="ok" });
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.MsgErro = ex.Message;
            }

            return View(createBarragemLiga);
        }
    }
}