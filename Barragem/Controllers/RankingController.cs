using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Barragem.Models;
using Barragem.Context;
using WebMatrix.WebData;
using System.Web.Security;


namespace Barragem.Controllers
{
    [Authorize]
    public class RankingController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();

        //
        // GET: /Rancking/
        [AllowAnonymous]
        public ActionResult Index(int id = 0, string nome = "")
        {
            List<Rancking> rancking;
            BarragemView bV = null;
            try
            {
                if ((id == 0) && (nome == "") && (User.Identity.Name != ""))
                {
                    UserProfile usuario = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
                    bV = usuario.barragem;
                    id = db.Rodada.Where(r => r.isAberta == false && r.isRodadaCarga == false && r.barragemId == bV.Id).Max(r => r.Id);
                    ViewBag.IdBarragem = bV.Id;
                    ViewBag.NomeBarragem = bV.nome;
                }
                else if (nome != "")
                {
                    if (nome.All(char.IsDigit))
                    {
                        var barragemId = Int32.Parse(nome);
                        bV = db.BarragemView.Find(barragemId);
                    }
                    else
                    {
                        bV = db.BarragemView.Where(b => b.dominio == nome).FirstOrDefault();
                    }
                    //id = db.Rancking.Where(r => r.rodada.isAberta == false && r.rodada.isRodadaCarga == false && r.rodada.barragemId == bV.Id).Max(r => r.rodada_id);
                    ViewBag.IdBarragem = bV.Id;
                    ViewBag.NomeBarragem = bV.nome;
                }
                else if (id == 0)
                {
                    return RedirectToAction("Login", "Account");
                }
                else
                {
                    HttpCookie cookie = new HttpCookie("_barragemId");
                    var barragemId = Convert.ToInt32(cookie.Value.ToString());
                    bV = db.BarragemView.Find(barragemId);
                }
            }
            catch (Exception e)
            {
                return RedirectToAction("Login", "Account");
            }
            rancking = db.Rancking.Include(r => r.userProfile).Include(r => r.rodada).
                Where(r => r.rodada_id == id && r.posicao > 0 && r.userProfile.situacao != "desativado" && r.userProfile.situacao != "inativo").OrderBy(r => r.classe.nivel).ThenBy(r => r.posicaoClasse).ToList();
            ViewBag.Classes = db.Classe.Where(c => c.barragemId == bV.Id && c.ativa == true).OrderBy(c => c.nivel).ToList();
            if (rancking.Count() > 0)
            {
                var barragem = rancking[0].rodada.barragemId;
                ViewBag.Rodada = rancking[0].rodada.codigoSeq;
                ViewBag.RodadaId = rancking[0].rodada.Id;
                ViewBag.dataRodada = (rancking[0].rodada.dataFim + "").Substring(0, 10);

                if (rancking[0].rodada.temporadaId != null)
                {
                    var temporadaId = rancking[0].rodada.temporadaId;
                    var rodadaId = rancking[0].rodada.Id;
                    var qtddRodada = db.Rodada.Where(r => r.temporadaId == temporadaId && r.Id <= rodadaId && r.barragemId == barragem).Count();
                    ViewBag.Temporada = rancking[0].rodada.temporada.nome + " - Rodada " + qtddRodada + " de " +
                        rancking[0].rodada.temporada.qtddRodadas;
                }
                else { ViewBag.Temporada = ""; }
            }

            return View(rancking);
        }


        public ActionResult listarRancking(int idRodadaAtual)
        {
            UserProfile usuario = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
            int barragemId = 0;
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
            var listaRancking = db.Rodada.Where(c => c.isAberta == false && c.isRodadaCarga == false && c.Id != idRodadaAtual && c.barragemId == barragemId).OrderByDescending(c => c.Id).ToList();
            return View(listaRancking);
        }

        public ActionResult historicoRanking(int idJogador)
        {
            List<Rancking> ranckingJogador = db.Rancking.Where(r => r.userProfile_id == idJogador && r.rodada.isRodadaCarga == false).OrderByDescending(r => r.rodada_id).ToList();
            return View(ranckingJogador);
        }

        public ActionResult RegraPontuacao()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

        [AllowAnonymous]
        public ActionResult RankingDasLigas(int idLiga = 0, int idSnapshot = 0, int idLigaAtual = 0, int idSnapshotAtual = 0, bool showOneLiga = false)
        {
            // UserProfile usuario = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
            // string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            List<Liga> ligas;
            if (showOneLiga)
            {
                ligas = db.Liga.Where(l => l.Id == idLiga).OrderBy(l => l.Nome).ToList();
            }
            else
            {
                ligas = db.Liga.Where(l => l.isAtivo).OrderBy(l => l.Nome).ToList();
            }
            //else
            //{
            //    ligas = (from liga in db.Liga
            //    join tl in db.TorneioLiga on liga.Id equals tl.LigaId
            //    join t in db.Torneio on tl.TorneioId equals t.Id
            //    join it in db.InscricaoTorneio on t.Id equals it.torneioId
            //    join up in db.UserProfiles on it.userId equals up.UserId
            //             where it.userId == usuario.UserId
            //    select liga).ToList();
            //}
            if (idLiga == 0 && ligas.Count() > 0)
            {
                idLiga = ligas.First().Id;
            }
            List<Snapshot> snapshotsDaLiga = db.Snapshot.Where(snap => snap.LigaId == idLiga).OrderByDescending(s => s.Id).ToList();
            List<SnapshotRanking> ranking = new List<SnapshotRanking>();
            List<Categoria> categorias = new List<Categoria>();
            if (snapshotsDaLiga.Count() > 0)
            {
                if ((idSnapshot == 0) || (snapshotsDaLiga.Where(s => s.Id == idSnapshot).Count() == 0))
                {
                    idSnapshot = snapshotsDaLiga.First().Id;
                }
                ranking = db.SnapshotRanking.Where(snapR => snapR.SnapshotId == idSnapshot)
                .Include(s => s.Categoria).Include(s => s.Jogador)
                .OrderBy(snap => snap.Categoria.Nome).ThenBy(snap => snap.Posicao).ThenBy(snap => snap.Jogador.nome)
                .ToList();
                categorias = db.SnapshotRanking.Where(sr => sr.SnapshotId == idSnapshot)
                    .Include(sr => sr.Categoria).Select(sr => sr.Categoria).OrderBy(sr => sr.ordemExibicao).Distinct().ToList();
            }
            var classesLg = db.ClasseLiga.Where(c => c.LigaId == idLiga).ToList();
            foreach (var cat in categorias)
            {
                try
                {
                    cat.Nome = classesLg.Where(c => c.CategoriaId == cat.Id).SingleOrDefault().Nome; //cat.Nome;
                }
                catch (Exception e) { }
            }
            ViewBag.Ligas = ligas;
            ViewBag.SnapshotsDaLiga = snapshotsDaLiga;
            ViewBag.Categorias = categorias;
            ViewBag.idLiga = idLiga;
            ViewBag.idSnapshot = idSnapshot;
            ViewBag.showOneLiga = showOneLiga;
            return View(ranking);
        }

        [AllowAnonymous]
        public ActionResult RankingDasLigasBT(int idLiga = 0, int idSnapshot = 0)
        {
            Liga liga = db.Liga.Find(idLiga);
            List<Snapshot> snapshotsDaLiga = db.Snapshot.Where(snap => snap.LigaId == idLiga).OrderByDescending(s => s.Id).ToList();
            List<SnapshotRanking> ranking = new List<SnapshotRanking>();
            List<Categoria> categorias = new List<Categoria>();
            if (snapshotsDaLiga.Count() > 0)
            {
                if ((idSnapshot == 0) || (snapshotsDaLiga.Where(s => s.Id == idSnapshot).Count() == 0))
                {
                    idSnapshot = snapshotsDaLiga.First().Id;
                }
                ranking = db.SnapshotRanking.Where(snapR => snapR.SnapshotId == idSnapshot)
                .Include(s => s.Categoria).Include(s => s.Jogador)
                .OrderBy(snap => snap.Categoria.Nome).ThenBy(snap => snap.Posicao).ThenBy(snap => snap.Jogador.nome)
                .ToList();
                categorias = db.SnapshotRanking.Where(sr => sr.SnapshotId == idSnapshot)
                    .Include(sr => sr.Categoria).Select(sr => sr.Categoria).OrderBy(sr => sr.ordemExibicao).Distinct().ToList();
            }
            var classesLg = db.ClasseLiga.Where(c => c.LigaId == idLiga).ToList();
            foreach (var cat in categorias)
            {
                try
                {
                    cat.Nome = classesLg.Where(c => c.CategoriaId == cat.Id).SingleOrDefault().Nome; //cat.Nome;
                }
                catch (Exception e) { }
            }
            ViewBag.idLiga = idLiga;
            ViewBag.nomeLiga = liga.Nome;
            ViewBag.SnapshotsDaLiga = snapshotsDaLiga;
            ViewBag.Categorias = categorias;
            ViewBag.idSnapshot = idSnapshot;
            return View(ranking);
        }

        [HttpPost]
        public ActionResult uploadLogoMarca(int Id)
        {
            HttpPostedFileBase filePosted = Request.Files["fileLogo"];
            if (filePosted != null && filePosted.ContentLength > 0)
            {
                var path = "/Content/image/";
                string fileNameApplication = System.IO.Path.GetFileName(filePosted.FileName);
                string fileExtensionApplication = System.IO.Path.GetExtension(fileNameApplication);

                // generating a random guid for a new file at server for the uploaded file
                string newFile = "logo" + Request.Form["Id"] + fileExtensionApplication;
                // getting a valid server path to save
                string filePath = System.IO.Path.Combine(Server.MapPath(path), newFile);

                if (fileNameApplication != String.Empty)
                {
                    filePosted.SaveAs(filePath);
                    //p.urlImagem = path + newFile;
                }
            }
            return RedirectToAction("PainelControle", "Torneio");
        }

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult NotificarAppRankingLiga(int ligaId)
        {
            try
            {
                var liga = db.Liga.Find(ligaId);

                var segmentacao = "liga_classificacao";
                var titulo = "Classificação do ranking atualizada!";
                var conteudo = $"Confira as novas pontuações do Ranking {liga.Nome}.";

                var fbmodel = new FirebaseNotificationModel<DataLigaModel>()
                {
                    to = "/topics/" + segmentacao,
                    notification = new NotificationModel() { title = titulo, body = conteudo },
                    data = new DataLigaModel() { title = titulo, body = conteudo, type = "tabela_liberada", idRanking = liga.barragemId ?? 0, ligaId = ligaId }
                };
                new FirebaseNotification().SendNotification(fbmodel);
                return Json(new { erro = "", retorno = 1, segmento = segmentacao }, "application/json", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "application/json", JsonRequestBehavior.AllowGet);
            }
        }
    }
}