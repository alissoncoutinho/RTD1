using System;
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
using System.Text;
using Barragem.Helper;
using System.Threading;

namespace Barragem.Controllers
{
    [InitializeSimpleMembership]
    public class TorneioController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();
        private TorneioNegocio tn = new TorneioNegocio();
        //

        [HttpPost]
        public ActionResult ImportarCabecasChave(int torneioId, int ligaId)
        {
            try
            {
                var listaClasses = db.ClasseTorneio.Where(c => c.torneioId == torneioId).ToList();

                foreach (var classe in listaClasses)
                {
                    var importacaoCabecas = new List<ImportacaoCabecaChaveModel>();
                    var qtdeCabecasChave = getOpcoesCabecaDeChave(classe.Id);
                    var snapshotsDaLiga = db.Snapshot.Where(snap => snap.LigaId == ligaId).OrderByDescending(s => s.Id).FirstOrDefault();

                    var ranking = tn.ObterDadosRankingTorneioClasse(torneioId, snapshotsDaLiga.Id, classe.Id);

                    List<InscricaoTorneio> inscricao = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.classe == classe.Id).ToList();

                    if (classe.isDupla)
                    {
                        var duplasFormadas = inscricao.Where(d => d.parceiroDuplaId != null).ToList();

                        foreach (var inscricaoDupla in duplasFormadas)
                        {
                            bool iscricaoParceiroPaga = inscricao.Count(x => x.userId == inscricaoDupla.parceiroDuplaId && x.isAtivo) == 1;
                            var item = new ImportacaoCabecaChaveModel()
                            {
                                IdInscricao = inscricaoDupla.Id,
                                TotalPontuacao = ranking.Where(x => x.UserId == inscricaoDupla.userId || x.UserId == inscricaoDupla.parceiroDuplaId).Sum(s => s.Pontuacao),
                                InscricaoPaga = inscricaoDupla.isAtivo && iscricaoParceiroPaga
                            };
                            importacaoCabecas.Add(item);
                        }
                    }
                    else
                    {
                        foreach (var inscricaoJogador in inscricao)
                        {
                            var item = new ImportacaoCabecaChaveModel()
                            {
                                IdInscricao = inscricaoJogador.Id,
                                TotalPontuacao = ranking.Where(x => x.UserId == inscricaoJogador.userId).Sum(s => s.Pontuacao),
                                InscricaoPaga = inscricaoJogador.isAtivo
                            };
                            importacaoCabecas.Add(item);
                        }
                    }

                    //Caso tenha cabeças de chave já setadas para a chasse então remove
                    foreach (var item in importacaoCabecas)
                    {
                        var inscricaoCabecaChave = inscricao.FirstOrDefault(x => x.Id == item.IdInscricao);
                        if (inscricaoCabecaChave.cabecaChave > 0)
                        {
                            AtualizarCabecaChave(inscricaoCabecaChave, null);
                        }
                    }

                    var rankingPontuacao = importacaoCabecas
                        .Where(x => x.InscricaoPaga)
                        .OrderByDescending(o => o.TotalPontuacao)
                        .Take(qtdeCabecasChave);

                    int cabecaChave = 1;
                    foreach (var item in rankingPontuacao)
                    {
                        var inscricaoCabecaChave = inscricao.FirstOrDefault(x => x.Id == item.IdInscricao);
                        AtualizarCabecaChave(inscricaoCabecaChave, cabecaChave);
                        cabecaChave++;
                    }
                }

                return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        private void AtualizarCabecaChave(InscricaoTorneio inscricao, int? cabecaChave)
        {
            if (inscricao.classeTorneio.faseGrupo)
            {
                inscricao.grupo = cabecaChave;
            }
            inscricao.cabecaChave = cabecaChave;

            db.Entry(inscricao).State = EntityState.Modified;
            db.SaveChanges();
        }

        private List<SelectListItem> ObterCircuitosImportacaoCabecaChave(int torneioId, int filtroClasse)
        {
            var circuitos = from torneio in db.Torneio
                            join ligaTorneio in db.TorneioLiga
                                on torneio.Id equals ligaTorneio.TorneioId
                            join snapshot in db.Snapshot
                                on ligaTorneio.LigaId equals snapshot.LigaId
                            join classeTorneio in db.ClasseTorneio
                                on torneio.Id equals classeTorneio.torneioId
                            join classeLiga in db.ClasseLiga
                                on new { categoriaId = (int)classeTorneio.categoriaId, ligaId = snapshot.LigaId } equals new { categoriaId = classeLiga.CategoriaId, ligaId = classeLiga.LigaId }
                            join liga in db.Liga
                                on snapshot.LigaId equals liga.Id
                            join snapshotRanking in db.SnapshotRanking
                                on new { snapshotId = snapshot.Id, categoriaId = classeLiga.CategoriaId } equals new { snapshotId = snapshotRanking.SnapshotId, categoriaId = snapshotRanking.CategoriaId }
                            where ligaTorneio.TorneioId == torneioId
                            where classeTorneio.Id == filtroClasse
                            orderby liga.Nome
                            group liga by new { Id = liga.Id, Nome = liga.Nome } into gLiga
                            select new SelectListItem { Text = gLiga.Key.Nome, Value = gLiga.Key.Id.ToString() };

            if (circuitos == null)
                return new List<SelectListItem>();

            return circuitos.ToList();
        }

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

        private void DesfazerDupla(InscricaoTorneio inscricao, int idTorneio)
        {
            //Verifica se o jogador é parceiro de dupla de alguém
            var inscricoesParceiroDupla = db.InscricaoTorneio.FirstOrDefault(i => i.torneioId == idTorneio && i.parceiroDuplaId == inscricao.userId);
            if (inscricoesParceiroDupla != null)
            {
                inscricoesParceiroDupla.parceiroDuplaId = null;
                db.Entry(inscricoesParceiroDupla).State = EntityState.Modified;
                db.SaveChanges();
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

                var inscritoPossuiJogos = db.Jogo.Any(j => j.classeTorneio == inscricao.classe && (j.desafiado_id == inscricao.userId || j.desafiante_id == inscricao.userId));
                var inscritoDuplaPossuiJogos = db.Jogo.Any(j => j.classeTorneio == inscricao.classe && (j.desafiado2_id == inscricao.userId || j.desafiante2_id == inscricao.userId));

                if (inscritoPossuiJogos || inscritoDuplaPossuiJogos)
                {
                    return RedirectToAction("EditInscritos", new { torneioId = torneioId, Msg = "Não é possível excluir jogador que já tem jogos." });
                }

                DesfazerDupla(inscricao, torneioId);

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
                var inscritos = db.InscricaoTorneio.Where(i => i.classe == Id).Count();
                if (inscritos > 0)
                {
                    return Json(new { erro = "Não é possível excluir categoria que tem jogadores inscritos. Para excluí-la é necessário excluir as inscrições ou alterar os jogadores de categoria.", retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
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

        [Authorize(Roles = "admin")]
        public ActionResult ExcluirTorneio(int id)
        {
            var torneio = db.Torneio.Find(id);
            var ligasTorneio = db.TorneioLiga.Where(x => x.TorneioId == id);
            var classesTorneio = db.ClasseTorneio.Where(x => x.torneioId == id);

            if (ligasTorneio != null && ligasTorneio.Any())
            {
                db.TorneioLiga.RemoveRange(ligasTorneio);
            }
            if (classesTorneio != null && classesTorneio.Any())
            {
                db.ClasseTorneio.RemoveRange(classesTorneio);
            }
            db.Torneio.Remove(torneio);
            db.SaveChanges();

            return RedirectToAction("Index");
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
            List<Torneio> torneios = null;
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            int barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
            if (perfil.Equals("admin"))
            {
                torneios = db.Torneio.OrderByDescending(c => c.Id).ToList();
            }
            else if (perfil.Equals("parceiroBT"))
            {
                torneios = db.Torneio.Where(r => r.barragem.isBeachTenis == true && !r.nome.ToUpper().Contains("TESTE")).OrderByDescending(c => c.Id).ToList();
            }
            else
            {
                torneios = db.Torneio.Where(r => r.barragemId == barragemId).OrderByDescending(c => c.Id).ToList();
            }
            var barragem = db.BarragemView.Find(barragemId);
            ViewBag.isBarragemAtiva = barragem.isAtiva;

            List<ListagemTorneioModel> listagemTorneios = new List<ListagemTorneioModel>();
            if (torneios != null)
            {
                var administradoresBarragem = db.AdministradorBarragemView.ToList();

                foreach (var torneio in torneios)
                {
                    var adminBarragemTorneio = administradoresBarragem.FirstOrDefault(x => x.idBarragem == torneio.barragemId);
                    listagemTorneios.Add(new ListagemTorneioModel()
                    {
                        Id = torneio.Id,
                        Nome = torneio.nome,
                        DataInicio = torneio.dataInicio,
                        NomeBarragem = torneio.barragem.nome,
                        TipoBarragem = torneio.barragem.isBeachTenis ? "Beach Tennis" : "Tênis",
                        NomeUsuarioAdmin = adminBarragemTorneio?.userName,
                        TelefoneCelular = adminBarragemTorneio?.telefone
                    });
                }
            }
            return View(listagemTorneios);
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
            ViewBag.TorneioId = torneioId;
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

        private void ExcluirJogosPorClasse(ClasseTorneio classe, bool faseGrupoFinalizada)
        {
            if (!classe.faseGrupo || !faseGrupoFinalizada)
                db.Database.ExecuteSqlCommand("delete from jogo where classeTorneio=" + classe.Id);
        }

        private void ResetarJogosMataMataPorClasse(int classeId, bool ehMataMata)
        {
            if (ehMataMata)
            {
                var query = $@"
                    Update Jogo 
                    set dataCadastroResultado = null,
                        usuarioInformResultado = null, 
                        desafiante_id = 0,
                        desafiado_id = 0,
                        qtddGames1setDesafiante = 0,
                        qtddGames2setDesafiante = 0,
                        qtddGames3setDesafiante = 0,
                        qtddGames1setDesafiado = 0,
                        qtddGames2setDesafiado = 0,
                        qtddGames3setDesafiado = 0,
                        idVencedor = 0,
                        idPerderdor = 0,
                        situacao_Id = 1,
                        dataJogo = null,
                        horaJogo = null,
                        quadra = null,
                        isPrimeiroJogoTorneio = null
                    where classeTorneio = {classeId}
                    and grupoFaseGrupo is null
                ";
                db.Database.ExecuteSqlCommand(query);
            }

        }

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        [HttpPost]
        public ActionResult MontarChaveamento(int torneioId, IEnumerable<int> classeIds)
        {
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
                    ExcluirJogosPorClasse(classe, faseGrupoFinalizada);
                    if (torneio.barragem.isModeloTodosContraTodos)
                    {
                        tn.montarJogosTodosContraTodos(classe);
                    }
                    else if ((classe.faseGrupo) && (!faseGrupoFinalizada))
                    {
                        int qtddGrupo = tn.MontarGruposFaseGrupo(classe);
                        if (qtddGrupo == 0)
                        {
                            continue;
                        }

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
            return RedirectToAction("EditJogos", new { torneioId = torneioId, fClasse = 0, fData = "", fNomeJogador = "", fGrupo = "0", fase = 0 });
        }

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        [HttpPost]
        public ActionResult ExcluirTabelaJogos(int torneioId, IEnumerable<int> idsClassesExclusao)
        {
            if (idsClassesExclusao != null)
            {
                foreach (int classeId in idsClassesExclusao)
                {
                    var classe = db.ClasseTorneio.Find(classeId);
                    RemoverGrupoInscricao(classeId, torneioId);
                    ExcluirJogosPorClasse(classe, false);
                }
            }
            return RedirectToAction("EditJogos", new { torneioId = torneioId, fClasse = 0, fData = "", fNomeJogador = "", fGrupo = "0", fase = 0 });
        }

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        [HttpPost]
        public JsonResult SalvarAlteracaoClassesGeracaoJogos(ICollection<CategoriaValidarQtdeJogadoresRequestModel> dadosClasses)
        {
            try
            {
                foreach (var classeItem in dadosClasses)
                {
                    var classe = db.ClasseTorneio.Find(classeItem.IdClasse);

                    if (classeItem.ConfigSelecionadaClasse == 2)
                    {
                        classe.faseGrupo = true;
                        classe.faseMataMata = false;
                    }
                    else if (classeItem.ConfigSelecionadaClasse == 3 || classeItem.ConfigSelecionadaClasse == 6)
                    {
                        classe.faseGrupo = false;
                        classe.faseMataMata = true;
                    }
                    else if (classeItem.ConfigSelecionadaClasse == 5)
                    {
                        classe.faseGrupo = true;
                        classe.faseMataMata = true;
                    }

                    if (classeItem.ConfigSelecionadaClasse != 1)
                    {
                        db.Entry(classe).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }

                return Json(new { erro = "", retorno = "OK", mensagem = "Classes do torneio atualizadas com sucesso!" }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = "ERRO" }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        [HttpPost]
        public JsonResult AlterarParceiroDupla(DadosAlteracaoParceiroDuplaModel dados)
        {
            try
            {
                if (dados.IdInscricao <= 0)
                {
                    return Json(new { erro = "Identificador da inscrição inválido", retorno = "ERRO" }, "text/plain", JsonRequestBehavior.AllowGet);
                }
                else if (dados.IdTorneio <= 0)
                {
                    return Json(new { erro = "Identificador do torneio inválido", retorno = "ERRO" }, "text/plain", JsonRequestBehavior.AllowGet);
                }
                else if (dados.IdClasse <= 0)
                {
                    return Json(new { erro = "Identificador da classe inválido", retorno = "ERRO" }, "text/plain", JsonRequestBehavior.AllowGet);
                }
                else if (dados.IdJogador <= 0)
                {
                    return Json(new { erro = "Informe o Jogador para alteração", retorno = "ERRO" }, "text/plain", JsonRequestBehavior.AllowGet);
                }

                int? userIdAnterior = 0;
                var inscricaoDupla = db.InscricaoTorneio.Find(dados.IdInscricao);
                var inscricaoDuplaJogadorEscolhido = db.InscricaoTorneio.FirstOrDefault(x => x.torneioId == dados.IdTorneio && x.classe == dados.IdClasse && x.userId == dados.IdJogador && x.parceiroDuplaId == null);

                if (inscricaoDupla == null)
                {
                    return Json(new { erro = "Inscrição da dupla não encontrada", retorno = "ERRO" }, "text/plain", JsonRequestBehavior.AllowGet);
                }

                if (dados.JogadorAlterado == 1)
                {
                    userIdAnterior = inscricaoDupla.userId;

                    inscricaoDupla.userId = dados.IdJogador;
                    db.Entry(inscricaoDupla).State = EntityState.Modified;
                    db.SaveChanges();

                    if (inscricaoDuplaJogadorEscolhido != null)
                    {
                        inscricaoDuplaJogadorEscolhido.userId = userIdAnterior.Value;
                        db.Entry(inscricaoDuplaJogadorEscolhido).State = EntityState.Modified;
                        db.SaveChanges();
                    }

                    db.Database.ExecuteSqlCommand($"update Jogo set desafiado_id  = {dados.IdJogador} where torneioId = {dados.IdTorneio} and classeTorneio = {dados.IdClasse} and desafiado_id  = {userIdAnterior} ");
                    db.Database.ExecuteSqlCommand($"update Jogo set desafiante_id = {dados.IdJogador} where torneioId = {dados.IdTorneio} and classeTorneio = {dados.IdClasse} and desafiante_id = {userIdAnterior} ");
                }
                else
                {
                    userIdAnterior = inscricaoDupla.parceiroDuplaId;

                    inscricaoDupla.parceiroDuplaId = dados.IdJogador;
                    db.Entry(inscricaoDupla).State = EntityState.Modified;
                    db.SaveChanges();

                    db.Database.ExecuteSqlCommand($"update Jogo set desafiado2_id  = {dados.IdJogador} where torneioId = {dados.IdTorneio} and classeTorneio = {dados.IdClasse} and desafiado2_id  = {userIdAnterior} ");
                    db.Database.ExecuteSqlCommand($"update Jogo set desafiante2_id = {dados.IdJogador} where torneioId = {dados.IdTorneio} and classeTorneio = {dados.IdClasse} and desafiante2_id = {userIdAnterior} ");
                }

                return Json(new { erro = "", retorno = "OK", mensagem = "Inscrição da dupla atualizada com sucesso!" }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = "ERRO" }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        private void RemoverGrupoInscricao(int classeId, int torneioId)
        {
            db.Database.ExecuteSqlCommand($"update InscricaoTorneio set grupo = null where torneioid = {torneioId} and classe = {classeId}");
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
                ViewBag.InscritosWO = ObterInscritosComWO(classe, grupo);

                var criterioEmpate = ValidarCriterioEmpate(classificacaoGrupo);
                ViewBag.EhTriploEmpate = criterioEmpate.EhTriploEmpate;
                ViewBag.EhDuploEmpate = criterioEmpate.EhDuploEmpate;

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
                    ViewBag.InscritosWO = ObterInscritosComWO(classe, grupo);
                    
                    var criterioEmpate = ValidarCriterioEmpate(classificacaoGrupo);
                    ViewBag.EhTriploEmpate = criterioEmpate.EhTriploEmpate;
                    ViewBag.EhDuploEmpate = criterioEmpate.EhDuploEmpate;

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

            ViewBag.Regra = db.Regra.Find(1).descricao;
            ViewBag.Classes = db.ClasseTorneio.Where(c => c.torneioId == torneioId).OrderBy(c => c.nivel).ToList();
            ViewBag.TorneioId = torneioId;
            ViewBag.nomeTorneio = torneio.nome;
            ViewBag.filtroClasse = filtroClasse;
            ViewBag.ClasseEhFaseGrupo = classe != null ? classe.faseGrupo : false;
            extrairPrimeiroNomeJogosDupla(jogos);

            mensagem(Msg);
            return View(jogos);
        }

        private List<ClassificacaoFaseGrupo> ObterInscritosComWO(ClasseTorneio classe, int grupo)
        {
            if (classe.isDupla)
            {
                return db.InscricaoTorneio
                    .Where(it => it.torneioId == classe.torneioId && it.classe == classe.Id && it.grupo == grupo && it.isAtivo && it.parceiroDuplaId != null && it.parceiroDuplaId != 0 && it.pontuacaoFaseGrupo == -100)
                    .Select(s => new ClassificacaoFaseGrupo() { userId = s.userId, inscricao = s, nome = s.participante.nome, nomeDupla = s.parceiroDupla != null ? s.parceiroDupla.nome : "", averageGames = 0, averageSets = 0, confrontoDireto = 0, saldoGames = 0, saldoSets = 0 })
                    .ToList();
            }
            else
            {
                return db.InscricaoTorneio
                        .Where(it => it.torneioId == classe.torneioId && it.classe == classe.Id && it.grupo == grupo && it.isAtivo && it.pontuacaoFaseGrupo == -100)
                        .Select(s => new ClassificacaoFaseGrupo() { userId = s.userId, inscricao = s, nome = s.participante.nome, nomeDupla = s.parceiroDupla != null ? s.parceiroDupla.nome : "", averageGames = 0, averageSets = 0, confrontoDireto = 0, saldoGames = 0, saldoSets = 0 })
                        .ToList();
            }
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

        private ValidacaoEmpateResponseModel ValidarCriterioEmpate(List<ClassificacaoFaseGrupo> classificacao)
        {
            var gruposClassificatorios = classificacao.Where(x => x.saldoSets != 0 || x.saldoGames != 0 || x.averageGames != 0).GroupBy(g => new { g.saldoSets, g.saldoGames, g.averageGames });
            return new ValidacaoEmpateResponseModel()
            {
                EhDuploEmpate = gruposClassificatorios.Any(x => x.Count() == 2),
                EhTriploEmpate = gruposClassificatorios.Any(x => x.Count() == 3)
            };
        }

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
                    if (torneio.Count != 0)
                    {
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
        public ActionResult EditInscritos(int torneioId, int filtroClasse = 0, string filtroJogador = "", string Msg = "", int filtroStatusPagamento = -1)
        {

            List<InscricaoTorneio> inscricao = db.InscricaoTorneio.Where(i => i.torneioId == torneioId).ToList();
            var torneio = db.Torneio.Find(torneioId);
            if (filtroClasse != 0)
            {
                inscricao = inscricao.Where(i => i.classe == filtroClasse).ToList();
            }
            if (filtroJogador != "")
            {
                inscricao = inscricao.Where(i => i.participante.nome.ToUpper().Contains(filtroJogador.ToUpper())).ToList();
            }
            if (filtroStatusPagamento != -1)
            {
                inscricao = inscricao.Where(i => i.isAtivo == (filtroStatusPagamento == 1)).ToList();
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
            ViewBag.FiltroStatusPagamento = filtroStatusPagamento;

            mensagem(Msg);

            CarregarDadosEssenciais(torneioId, "inscritos");
            return View(inscricao);
        }

        private CobrancaTorneio getDadosDeCobrancaTorneio(int torneioId)
        {
            var torneio = db.Torneio.Find(torneioId);
            return ObterDadosCobrancaTorneio(torneio);
        }

        private CobrancaTorneio ObterDadosCobrancaTorneio(Torneio torneio)
        {
            var cobrancaTorneio = new CobrancaTorneio();

            var ativo = Tipos.Situacao.ativo.ToString();
            var licenciado = Tipos.Situacao.licenciado.ToString();
            var suspenso = Tipos.Situacao.suspenso.ToString();
            var suspensoWO = Tipos.Situacao.suspensoWO.ToString();

            var barragem = db.Barragens.Find(torneio.barragemId);

            decimal valorPorUsuario = barragem.valorPorUsuario.HasValue ? (decimal)barragem.valorPorUsuario : 5;

            var inscritosPagtoOk = db.InscricaoTorneio.Where(i => i.torneioId == torneio.Id && i.isAtivo);

            cobrancaTorneio.qtddInscritos = inscritosPagtoOk.Select(i => (int)i.userId).Distinct().Count();

            var inscritosNaoPagantes = inscritosPagtoOk.Where(i => i.torneio.barragemId == i.participante.barragemId
                    && (i.participante.situacao == ativo || i.participante.situacao == suspenso || i.participante.situacao == licenciado || i.participante.situacao == suspensoWO)).Select(i => (int)i.userId).Distinct().Count();

            cobrancaTorneio.valorDescontoParaRanking = inscritosNaoPagantes * valorPorUsuario;
            cobrancaTorneio.valorASerPago = (cobrancaTorneio.qtddInscritos * valorPorUsuario) - cobrancaTorneio.valorDescontoParaRanking;
            cobrancaTorneio.valorPorUsuario = valorPorUsuario;
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
            else if (classe.faseMataMata && !classe.faseGrupo)
            {
                qtddCabecaChave = tn.ObterQtdeCabecasChaveMataMata(tn.getInscritosPorClasse(classe, true).Count());
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

            CarregarDadosEssenciais(torneioId, "obs");
            return View(inscricao);
        }

        [Authorize(Roles = "admin")]
        public ActionResult EditTeste(int torneioId, string msg = "")
        {
            var classes = db.ClasseTorneio.Where(c => c.torneioId == torneioId).ToList();
            mensagem(msg);
            CarregarDadosEssenciais(torneioId, "teste");
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
                    }
                    else
                    {
                        item.Categoria = classesLiga[0].Categoria;
                        item.categoriaId = classesLiga[0].CategoriaId;
                        break;
                    }
                }
            }

            CarregarComboCategoriasCircuito(torneioId);
            CarregarDadosEssenciais(torneioId, "classes");
            return View(classes);
        }

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult EditPatrocinadores(int torneioId, string msg = "")
        {
            mensagem(msg);
            var patrocinadores = db.Patrocinador.Where(p => p.torneioId == torneioId).ToList();

            CarregarDadosEssenciais(torneioId, "patrocinio");
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

        [HttpPost]
        public ActionResult AlterarCategoriaCircuitoClasse(int idClasse, int idTorneio, int idCategoria)
        {
            if (idClasse <= 0 || idCategoria <= 0 || idTorneio <= 0)
                throw new Exception("Parâmetros inválidos");

            var classeTorneio = db.ClasseTorneio.Find(idClasse);
            classeTorneio.categoriaId = idCategoria;

            db.Entry(classeTorneio).State = EntityState.Modified;
            db.SaveChanges();

            var ligasTorneio = ObterLigasTorneio(idTorneio);

            foreach (var ligaTorneio in ligasTorneio)
            {
                if (idCategoria > 0 && !ValidarCategoriaExistenteLiga(idCategoria, ligaTorneio.LigaId))
                {
                    SalvarClasseLiga(idCategoria, ligaTorneio.LigaId);
                }
            }

            return Json(new { redirectToUrl = Url.Action("EditClasse", "Torneio", new { torneioId = idTorneio }) });
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
            ViewBag.TorneioId = torneioId;
            ViewBag.qtddClasses = qtddClasses + 1;
            ///ViewBag.Categorias
            List<Categoria> categorias = new List<Categoria>();
            categorias.Add(new Categoria() { Id = 0, Nome = "Não contará pontos" });
            List<TorneioLiga> ligasDotorneio = db.TorneioLiga.Where(tl => tl.TorneioId == torneioId).ToList();
            List<int> ligas = new List<int>();
            foreach (TorneioLiga tl in ligasDotorneio)
            {
                ligas.Add(tl.LigaId);
            }

            var categoriasJaVinculadas = ObterClassesTorneio(torneioId);

            foreach (ClasseLiga cl in db.ClasseLiga.Where(classeLiga => ligas.Contains(classeLiga.LigaId)).GroupBy(cl => cl.CategoriaId).Select(c => c.FirstOrDefault()).ToList())
            {
                if (!categoriasJaVinculadas.Any(x => x.categoriaId == cl.CategoriaId))
                {
                    categorias.Add(db.Categoria.Find(cl.CategoriaId));
                }
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
                ViewBag.TorneioId = classe.torneioId;
            }

            return View(classe);
        }

        [HttpPost]
        public ActionResult EditInscritos(int Id, int classe, bool isAtivo)
        {
            try
            {
                List<string> classesPagtoOk = new List<string>();

                var inscricao = db.InscricaoTorneio.Find(Id);

                var inscritoPossuiJogos = db.Jogo.Any(j => j.classeTorneio == inscricao.classe && (j.desafiado_id == inscricao.userId || j.desafiante_id == inscricao.userId));
                var inscritoDuplaPossuiJogos = db.Jogo.Any(j => j.classeTorneio == inscricao.classe && (j.desafiado2_id == inscricao.userId || j.desafiante2_id == inscricao.userId));

                if (inscricao.classe != classe && (inscritoPossuiJogos || inscritoDuplaPossuiJogos))
                {
                    return Json(new { erro = "Não é possível alterar jogador que já tem jogos.", retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
                }

                inscricao.classe = classe;
                inscricao.isAtivo = isAtivo;

                if ((isAtivo) && (inscricao.statusPagamento != "3") && (inscricao.statusPagamento != "4"))
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

                if (isAtivo)
                {
                    var classeT = db.ClasseTorneio.Find(classe);
                    classesPagtoOk.Add(classeT.nome);
                }

                db.Entry(inscricao).State = EntityState.Modified;
                db.SaveChanges();

                NotificarUsuarioPagamentoRealizado(classesPagtoOk, inscricao.torneio.nome, inscricao.userId, inscricao.torneioId);

                return Json(new { erro = "", retorno = 1, statusPagamento = inscricao.descricaoStatusPag }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        private void NotificarUsuarioPagamentoRealizado(List<string> classes, string nomeTorneio, int userId, int torneioId)
        {
            if (classes.Count == 0)
                return;

            var msgConfirmacao = $"O pagamento da inscrição na(s) categoria(s) {string.Join(", ", classes.Distinct())} do torneio {nomeTorneio} foi confirmado.";
            var titulo = "Pagamento da inscrição confirmado.";

            var userFb = db.UsuarioFirebase.FirstOrDefault(x => x.UserId == userId);
            if (userFb == null)
                return;

            var dadosMensagemUsuario = new FirebaseNotificationModel() { to = userFb.Token, notification = new NotificationModel() { title = titulo, body = msgConfirmacao }, data = new DataModel() { torneioId = torneioId } };
            new FirebaseNotification().SendNotification(dadosMensagemUsuario);
        }

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult EditTorneio(int id = 0)
        {
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
            ViewBag.PagSeguroAtivo = torneio.PagSeguroAtivo;
            ViewBag.tokenPagSeguro = barragem.tokenPagSeguro;
            ViewBag.barraId = barragemId;
            ViewBag.barragemId = new SelectList(db.BarragemView, "Id", "nome", barragemId);
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
            ViewBag.LinkParaCopia = ObterLinkTorneio(torneio, barragemId);

            CarregarDadosEssenciais(id, "edit");
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
            if (torneio.PagSeguroAtivo)
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
            if (torneio.PagSeguroAtivo)
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
        public ActionResult Inscricao(int torneioId, int classeInscricao = 0, string operacao = "", int classeInscricao2 = 0, int classeInscricao3 = 0, int classeInscricao4 = 0, string observacao = "", bool isSocio = false, bool isFederado = false, int userId = 0, int idInscricaoParceiroDupla = 0, int idInscricaoParceiroDupla2 = 0, int idInscricaoParceiroDupla3 = 0, int idInscricaoParceiroDupla4 = 0)
        {
            var inscricaoModel = new InscricaoModel() { UserId = userId, TorneioId = torneioId, IdCategoria1 = classeInscricao, IdCategoria2 = classeInscricao2, IdCategoria3 = classeInscricao3, IdCategoria4 = classeInscricao4, Observacao = observacao, IsSocio = isSocio, IsFederado = isFederado, IdInscricaoParceiroDupla1 = idInscricaoParceiroDupla, IdInscricaoParceiroDupla2 = idInscricaoParceiroDupla2, IdInscricaoParceiroDupla3 = idInscricaoParceiroDupla3, IdInscricaoParceiroDupla4 = idInscricaoParceiroDupla4 };

            var mensagemRetorno = InscricaoNegocio(inscricaoModel, operacao);
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

        public MensagemRetorno InscricaoNegocio(InscricaoModel inscricaoModel, string operacao = "")
        {
            var mensagemRetorno = new MensagemRetorno();
            try
            {
                var gratuidade = false;
                if (inscricaoModel.UserId == 0)
                {
                    string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
                    if (!perfil.Equals(""))
                    {
                        inscricaoModel.UserId = WebSecurity.GetUserId(User.Identity.Name);
                    }
                }
                mudarStatusSeDesativadoParaStatusTorneio(inscricaoModel.UserId);
                var torneio = db.Torneio.Find(inscricaoModel.TorneioId);
                var isInscricao = db.InscricaoTorneio.Count(i => i.torneioId == inscricaoModel.TorneioId && i.userId == inscricaoModel.UserId);
                InscricaoTorneio inscricao = null;
                if (isInscricao > 0)
                {
                    var it = db.InscricaoTorneio.Where(i => i.torneioId == inscricaoModel.TorneioId && i.userId == inscricaoModel.UserId).ToList();
                    if (operacao == "cancelar")
                    {
                        foreach (var item in it)
                        {
                            DesfazerDupla(item, inscricaoModel.TorneioId);
                            db.InscricaoTorneio.Remove(item);
                        }
                    }
                    else
                    {
                        string msgValidacaoClasse = validarEscolhasDeClasses(inscricaoModel.IdCategoria1, inscricaoModel.IdCategoria2, inscricaoModel.IdCategoria3, inscricaoModel.IdCategoria4);

                        inscricaoModel.IsSocio = (it[0].isSocio == null) ? false : (bool)it[0].isSocio;
                        inscricaoModel.IsFederado = (it[0].isFederado == null) ? false : (bool)it[0].isFederado;
                        var valorInscricao = calcularValorInscricao(inscricaoModel.IdCategoria2, inscricaoModel.IdCategoria3, inscricaoModel.IdCategoria4, inscricaoModel.IsSocio, torneio, inscricaoModel.UserId, inscricaoModel.IsFederado);
                        if (operacao == "alterarClasse")
                        {
                            double valorPendente = 0;
                            it[0].classe = inscricaoModel.IdCategoria1;
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
                                alterarClasseInscricao(it[1], inscricaoModel.IdCategoria2, valorInscricao, valorPendente);
                            }
                            else if (inscricaoModel.IdCategoria2 != 0)
                            {
                                msgValidacaoClasse = validarLimiteDeInscricao(inscricaoModel.IdCategoria2, inscricaoModel.TorneioId);
                                if (msgValidacaoClasse != "")
                                {
                                    mensagemRetorno.nomePagina = "Detalhes";
                                    mensagemRetorno.mensagem = msgValidacaoClasse;
                                    return mensagemRetorno;
                                }

                                InscricaoTorneio insc2 = preencherInscricaoTorneio(inscricaoModel.TorneioId, inscricaoModel.UserId, inscricaoModel.IdCategoria2, valorInscricao, inscricaoModel.Observacao, inscricaoModel.IsSocio, inscricaoModel.IsFederado, valorPendente, it[0].isAtivo);
                                db.InscricaoTorneio.Add(insc2);
                                db.SaveChanges();
                                tn.ValidarCriacaoDupla(inscricaoModel.IdInscricaoParceiroDupla2, inscricaoModel.UserId, inscricaoModel.TorneioId, inscricaoModel.IdCategoria2);

                            }
                            if (it.Count() > 2)
                            {
                                alterarClasseInscricao(it[2], inscricaoModel.IdCategoria3, valorInscricao);
                            }
                            else if (inscricaoModel.IdCategoria3 != 0)
                            {
                                msgValidacaoClasse = validarLimiteDeInscricao(inscricaoModel.IdCategoria3, inscricaoModel.TorneioId);
                                if (msgValidacaoClasse != "")
                                {
                                    mensagemRetorno.nomePagina = "Detalhes";
                                    mensagemRetorno.mensagem = msgValidacaoClasse;
                                    return mensagemRetorno;
                                }
                                InscricaoTorneio insc3 = preencherInscricaoTorneio(inscricaoModel.TorneioId, inscricaoModel.UserId, inscricaoModel.IdCategoria3, valorInscricao, inscricaoModel.Observacao, inscricaoModel.IsSocio, inscricaoModel.IsFederado, valorPendente, it[0].isAtivo);
                                db.InscricaoTorneio.Add(insc3);
                                db.SaveChanges();
                                tn.ValidarCriacaoDupla(inscricaoModel.IdInscricaoParceiroDupla3, inscricaoModel.UserId, inscricaoModel.TorneioId, inscricaoModel.IdCategoria3);
                            }
                            if (it.Count() > 3)
                            {
                                alterarClasseInscricao(it[3], inscricaoModel.IdCategoria4, valorInscricao);
                            }
                            else if (inscricaoModel.IdCategoria4 != 0)
                            {
                                msgValidacaoClasse = validarLimiteDeInscricao(inscricaoModel.IdCategoria4, inscricaoModel.TorneioId);
                                if (msgValidacaoClasse != "")
                                {
                                    mensagemRetorno.nomePagina = "Detalhes";
                                    mensagemRetorno.mensagem = msgValidacaoClasse;
                                    return mensagemRetorno;
                                }
                                InscricaoTorneio insc4 = preencherInscricaoTorneio(inscricaoModel.TorneioId, inscricaoModel.UserId, inscricaoModel.IdCategoria4, valorInscricao, inscricaoModel.Observacao, inscricaoModel.IsSocio, inscricaoModel.IsFederado, valorPendente, it[0].isAtivo);
                                db.InscricaoTorneio.Add(insc4);
                                db.SaveChanges();
                                tn.ValidarCriacaoDupla(inscricaoModel.IdInscricaoParceiroDupla4, inscricaoModel.UserId, inscricaoModel.TorneioId, inscricaoModel.IdCategoria4);
                            }
                            db.SaveChanges();

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
                    string msgValidacaoClasse = validarEscolhasDeClasses(inscricaoModel.IdCategoria1, inscricaoModel.IdCategoria2, inscricaoModel.IdCategoria3, inscricaoModel.IdCategoria4);
                    if (msgValidacaoClasse != "")
                    {
                        mensagemRetorno.nomePagina = "Detalhes";
                        mensagemRetorno.mensagem = msgValidacaoClasse;
                        return mensagemRetorno;
                    }
                    msgValidacaoClasse = validarLimiteDeInscricao(inscricaoModel.IdCategoria1, inscricaoModel.IdCategoria2, inscricaoModel.IdCategoria3, inscricaoModel.IdCategoria4, torneio.Id);
                    if (msgValidacaoClasse != "")
                    {
                        mensagemRetorno.nomePagina = "Detalhes";
                        mensagemRetorno.mensagem = msgValidacaoClasse;
                        return mensagemRetorno;
                    }
                    double valorInscricao = calcularValorInscricao(inscricaoModel.IdCategoria2, inscricaoModel.IdCategoria3, inscricaoModel.IdCategoria4, inscricaoModel.IsSocio, torneio, inscricaoModel.UserId, inscricaoModel.IsFederado);

                    inscricao = preencherInscricaoTorneio(inscricaoModel.TorneioId, inscricaoModel.UserId, inscricaoModel.IdCategoria1, valorInscricao, inscricaoModel.Observacao, inscricaoModel.IsSocio, inscricaoModel.IsFederado);
                    db.InscricaoTorneio.Add(inscricao);
                    db.SaveChanges();
                    tn.ValidarCriacaoDupla(inscricaoModel.IdInscricaoParceiroDupla1, inscricaoModel.UserId, inscricaoModel.TorneioId, inscricaoModel.IdCategoria1);

                    if (inscricaoModel.IdCategoria2 > 0)
                    {
                        InscricaoTorneio insc2 = preencherInscricaoTorneio(inscricaoModel.TorneioId, inscricaoModel.UserId, inscricaoModel.IdCategoria2, valorInscricao, inscricaoModel.Observacao, inscricaoModel.IsSocio, inscricaoModel.IsFederado);
                        db.InscricaoTorneio.Add(insc2);
                        db.SaveChanges();
                        tn.ValidarCriacaoDupla(inscricaoModel.IdInscricaoParceiroDupla2, inscricaoModel.UserId, inscricaoModel.TorneioId, inscricaoModel.IdCategoria2);
                    }
                    if (inscricaoModel.IdCategoria3 > 0)
                    {
                        InscricaoTorneio insc3 = preencherInscricaoTorneio(inscricaoModel.TorneioId, inscricaoModel.UserId, inscricaoModel.IdCategoria3, valorInscricao, inscricaoModel.Observacao, inscricaoModel.IsSocio, inscricaoModel.IsFederado);
                        db.InscricaoTorneio.Add(insc3);
                        db.SaveChanges();
                        tn.ValidarCriacaoDupla(inscricaoModel.IdInscricaoParceiroDupla3, inscricaoModel.UserId, inscricaoModel.TorneioId, inscricaoModel.IdCategoria3);
                    }
                    if (inscricaoModel.IdCategoria4 > 0)
                    {
                        InscricaoTorneio insc4 = preencherInscricaoTorneio(inscricaoModel.TorneioId, inscricaoModel.UserId, inscricaoModel.IdCategoria4, valorInscricao, inscricaoModel.Observacao, inscricaoModel.IsSocio, inscricaoModel.IsFederado);
                        db.InscricaoTorneio.Add(insc4);
                        db.SaveChanges();
                        tn.ValidarCriacaoDupla(inscricaoModel.IdInscricaoParceiroDupla4, inscricaoModel.UserId, inscricaoModel.TorneioId, inscricaoModel.IdCategoria4);
                    }
                }

                db.SaveChanges();

                if (operacao != "cancelar")
                {
                    mensagemRetorno.id = inscricao.Id;
                    mensagemRetorno.mensagem = "Inscrição recebida.";
                    gratuidade = VerificarGratuidade(torneio, inscricaoModel.UserId);
                }
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

        public String validarLimiteDeInscricao(int classeInscricao, int classeInscricao2, int classeInscricao3, int classeInscricao4, int torneioId)
        {
            var listaCategorias = new List<int> { { classeInscricao }, { classeInscricao2 }, { classeInscricao3 }, { classeInscricao4 } };

            foreach (var categoria in listaCategorias)
            {
                if (categoria <= 0)
                {
                    continue;
                }

                var resposta = tn.ValidarDisponibilidadeInscricoes(torneioId, categoria);
                if (resposta.status == "ESGOTADO")
                {
                    var classeSelecionada = db.ClasseTorneio.Where(x => x.torneioId == torneioId && x.Id == categoria).FirstOrDefault();
                    return "Vagas esgotadas para a classe: " + classeSelecionada?.nome + ".";
                }
            }
            return string.Empty;
        }

        private String validarLimiteDeInscricao(int categoria, int torneioId)
        {
            if (categoria <= 0)
            {
                return string.Empty;
            }

            var resposta = tn.ValidarDisponibilidadeInscricoes(torneioId, categoria);
            if (resposta.status == "ESGOTADO")
            {
                var classeSelecionada = db.ClasseTorneio.Where(x => x.torneioId == torneioId && x.Id == categoria).FirstOrDefault();
                return "Vagas esgotadas para a classe: " + classeSelecionada?.nome + ".";
            }
            return string.Empty;

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

        public InscricaoTorneio preencherInscricaoTorneio(int torneioId, int userId, int classeInscricao, double? valorInscricao, string observacao, bool isSocio, bool isFederado, double valorPendente = 0, bool isInscricaoAtiva = false)
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
            inscricao.isAtivo = false;
            if ((valorInscricao == 0) || ((isInscricaoAtiva) && (valorPendente == 0)))
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
        public ActionResult EditTorneio(Torneio torneio, bool transferencia = false, bool cartao = false, string pontuacaoCircuito = "100")
        {
            torneio.inscricaoSoPeloSite = false;
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            torneio.liberarEscolhaDuplas = true;
            torneio.isAtivo = true;
            torneio.divulgaCidade = false;
            torneio.isOpen = false;
            torneio.TipoTorneio = pontuacaoCircuito;

            var dadosBarragem = db.Barragens.Find(torneio.barragemId);

            ValidarFormaPgtoTransferenciaBancaria(torneio, transferencia);
            ValidarFormaPgtoPix(torneio, cartao, dadosBarragem);

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
                ValidarLimitadorInscricoesTorneio(torneio.temLimiteDeInscricao == true, torneio.Id);

                ViewBag.tokenPagSeguro = dadosBarragem.tokenPagSeguro;
                ViewBag.PagSeguroAtivo = torneio.PagSeguroAtivo;

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
            ViewBag.CobrancaTorneio = getDadosDeCobrancaTorneio(torneio.Id);
            ViewBag.LinkParaCopia = ObterLinkTorneio(torneio, barragemId);

            CarregarDadosEssenciais(torneio.Id, "edit");
            return View(torneio);
        }

        private static void ValidarFormaPgtoPix(Torneio torneio, bool cartao, Barragens dadosBarragem)
        {
            if (string.IsNullOrEmpty(dadosBarragem.tokenPagSeguro) && cartao == true)
            {
                torneio.PagSeguroAtivo = false;
            }
            else
            {
                torneio.PagSeguroAtivo = cartao;
            }
        }

        private static void ValidarFormaPgtoTransferenciaBancaria(Torneio torneio, bool transferencia)
        {
            if (transferencia == false)
            {
                torneio.dadosBancarios = string.Empty;
                torneio.Agencia = string.Empty;
                torneio.ContaCorrente = string.Empty;
                torneio.ChavePix = string.Empty;
                torneio.NomeBanco = string.Empty;
                torneio.NomeOrganizador = string.Empty;
                torneio.ContatoOrganizador = string.Empty;
                torneio.CpfConta = string.Empty;
            }
            else
            {
                var htmlDadosBancarios = new StringBuilder();
                if (!string.IsNullOrEmpty(torneio.ChavePix))
                {
                    htmlDadosBancarios.AppendLine($"<p><b>PIX:</b>{torneio.ChavePix}</p>");
                }
                if (!string.IsNullOrEmpty(torneio.NomeBanco))
                {
                    htmlDadosBancarios.AppendLine($"<p>{torneio.NomeBanco}</p>");
                }
                if (!string.IsNullOrEmpty(torneio.Agencia) && !string.IsNullOrEmpty(torneio.ContaCorrente))
                {
                    htmlDadosBancarios.AppendLine($"<p><b>Ag.:</b>{torneio.Agencia}  <b>Conta.:</b>{torneio.ContaCorrente}</p>");
                }
                if (!string.IsNullOrEmpty(torneio.CpfConta))
                {
                    htmlDadosBancarios.AppendLine($"<p><b>CPF.:</b>{torneio.CpfConta}</p>");
                }
                if (!string.IsNullOrEmpty(torneio.NomeOrganizador) && !string.IsNullOrEmpty(torneio.ContatoOrganizador))
                {
                    htmlDadosBancarios.AppendLine("<p>Ao finalizar envie o comprovante para:</p>");
                    htmlDadosBancarios.AppendLine($"<p>{torneio.NomeOrganizador} - {torneio.ContatoOrganizador}</p>");
                }
                torneio.dadosBancarios = htmlDadosBancarios.ToString();
            }
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

        [HttpPost]
        public ActionResult SalvarDadosCadastraisPendentesPagto(int torneioId, string nome, string cpfCnpj)
        {
            try
            {
                var torneio = db.Torneio.Find(torneioId);
                var barragem = db.Barragens.Find(torneio.barragemId);

                barragem.nomeResponsavel = nome;
                barragem.cpfResponsavel = cpfCnpj;

                db.Entry(barragem).State = EntityState.Modified;
                db.SaveChanges();

                return Json(new { erro = "", status = "OK" }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, status = "ERRO" }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult ValidarPagamentoTorneio(int torneioId)
        {
            try
            {
                var cobrancaTorneio = new CobrancaTorneio();
                bool pendenciaDePagamento = false;
                var torneio = db.Torneio.Find(torneioId);

                if (temPendenciaDePagamentoTorneio(torneio))
                {
                    cobrancaTorneio = ObterDadosCobrancaTorneio(torneio);
                    if (cobrancaTorneio.valorASerPago > 0)
                    {
                        pendenciaDePagamento = true;
                    }

                    if (cobrancaTorneio.qtddInscritos > 0 && pendenciaDePagamento)
                    {
                        cobrancaTorneio.Nome = torneio.barragem.nomeResponsavel;
                        cobrancaTorneio.CpfCnpj = torneio.barragem.cpfResponsavel;

                        if (string.IsNullOrEmpty(cobrancaTorneio.Nome) || string.IsNullOrEmpty(cobrancaTorneio.CpfCnpj))
                        {
                            //Falta informar dados para pagamento, então solicitar a atualização de dados
                            return Json(new { erro = "", retorno = cobrancaTorneio, status = "PENDENCIA_CADASTRO" }, "text/plain", JsonRequestBehavior.AllowGet);
                        }
                        else
                        {
                            //dados ok, então gerar qr code para pagamento
                            try
                            {
                                cobrancaTorneio.qrCode = GetQrCodeCobrancaPIX(torneio, cobrancaTorneio);
                                return Json(new { erro = "", retorno = cobrancaTorneio, status = "PENDENCIA_PAGAMENTO" }, "text/plain", JsonRequestBehavior.AllowGet);
                            }
                            catch (Exception e)
                            {
                                cobrancaTorneio.qrCode = new QrCodeCobrancaTorneio();
                                cobrancaTorneio.qrCode.erroGerarQrCode = "Erro ao gerar o QrCode de pagamento. Favor tente novamente mais tarde:" + e.Message;
                                return Json(new { erro = cobrancaTorneio.qrCode.erroGerarQrCode, retorno = cobrancaTorneio, status = "ERRO_QRCODE" }, "text/plain", JsonRequestBehavior.AllowGet);
                            }
                        }
                    }
                }
                //Tudo ok, então pode gerar tabela
                return Json(new { erro = "", retorno = cobrancaTorneio, status = "OK" }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = "ERRO", status = "ERRO" }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult ValidarAlteracaoPlacar(int jogoId, int situacaoId)
        {
            AlteracaoPlacarResponseModel response = new AlteracaoPlacarResponseModel();
            try
            {
                var jogo = db.Jogo.Find(jogoId);

                if (jogo != null)
                {
                    var classe = jogo.classe;

                    #region Validação CONSOLIDACAO 
                    Torneio torneio = db.Torneio.Include(t => t.barragem).FirstOrDefault(t => t.Id == jogo.torneioId);
                    if (torneio != null && jogo.rodadaFaseGrupo != 0 && (torneio.barragem.isModeloTodosContraTodos || (classe.faseGrupo && !classe.faseMataMata)))
                    {
                        if (ObterQtdeJogosPendentesFaseGrupo(jogo.torneioId.Value, jogo.classeTorneio.Value) == 0)
                        {
                            response.StatusConsolidacao = "CONSOLIDAR";
                        }
                    }
                    else if (classe.faseMataMata)
                    {
                        var lancamentoResultUltimoJogoMataMata = db.Jogo.Any(x => x.torneioId == jogo.torneioId && x.classeTorneio == jogo.classeTorneio && x.grupoFaseGrupo == null && x.faseTorneio == 1 && x.Id == jogo.Id);
                        if (lancamentoResultUltimoJogoMataMata)
                        {
                            response.StatusConsolidacao = "CONSOLIDAR";
                        }
                    }

                    if (string.IsNullOrEmpty(response.StatusConsolidacao))
                    {
                        response.StatusConsolidacao = "OK";
                    }
                    #endregion Validação CONSOLIDACAO 

                    #region Validação de lançamento placar jogo fase grupo com mata mata
                    response.StatusAlteracaoPlacar = ValidarAlteracaoJogoFaseGrupoComMataMataExistente(jogo, situacaoId);
                    #endregion Validação de lançamento placar jogo fase grupo com mata mata
                }
                response.RequisicaoOk = true;
                return Json(response, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                response = new AlteracaoPlacarResponseModel() { Erro = ex.Message, RequisicaoOk = false };
                return Json(response, "text/plain", JsonRequestBehavior.AllowGet);
            }
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

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult ImprimirInscritos(int torneioId, int classeId = 0, string jogador = "", int filtroStatusPagamento = -1)
        {
            var inscricoes = db.InscricaoTorneio
                .Include(i => i.torneio)
                .Include(i => i.participante)
                .Include(i => i.classeTorneio)
                .Where(x => x.torneioId == torneioId && ((classeId > 0 && x.classe == classeId) || classeId == 0) && (x.participante.nome.ToUpper().Contains(jogador.ToUpper()) || string.IsNullOrEmpty(jogador)) && ((filtroStatusPagamento == -1) || x.isAtivo == (filtroStatusPagamento == 1)))
                .OrderBy(o => o.participante.nome).ThenBy(o => o.classe).ToList();

            if (inscricoes != null && inscricoes.Any())
            {
                var inscricoesImpressao = inscricoes
                    .GroupBy(g => g.participante.nome)
                    .Select(s => new ImpressaoInscritosModel() { Inscrito = s.Key, Categorias = string.Join(" / ", s.Select(sc => sc.classeTorneio.nome)), Pago = string.Join(" / ", s.Select(sc => sc.isAtivo ? "Sim" : "Não")) }).ToList();

                ViewBag.NomeTorneio = inscricoes.FirstOrDefault().torneio.nome;
                ViewBag.DadosNaoEncontrados = false;
                return View(inscricoesImpressao);
            }
            else
            {
                ViewBag.NomeTorneio = string.Empty;
                ViewBag.DadosNaoEncontrados = true;
                return View(new List<ImpressaoInscritosModel>());
            }
        }

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult ImprimirGrupos(int torneioId)
        {
            ImprimirGruposModel dadosImpressao = new ImprimirGruposModel();
            var torneio = db.Torneio.Find(torneioId);
            var classes = db.ClasseTorneio.Where(i => i.torneioId == torneioId && i.faseGrupo).OrderBy(c => c.nome).ToList();
            dadosImpressao.NomeTorneio = torneio.nome;
            foreach (var categoria in classes)
            {
                var dadosCategoria = new ImprimirGruposModel.ItemCategoriaModel() { NomeCategoria = categoria.nome };


                var inscritos = tn.getInscritosPorClasse(categoria).OrderBy(i => i.grupo).ToList();

                foreach (var grupoInsc in inscritos.GroupBy(g => g.grupo))
                {
                    var dadosGrupo = new ImprimirGruposModel.ItemGruposModel() { NomeGrupo = $"GRUPO {grupoInsc.Key}" };

                    foreach (var inscrito in grupoInsc)
                    {
                        if (categoria.isDupla)
                        {
                            dadosGrupo.Inscritos.Add($"{inscrito.participante.nome} / {inscrito.parceiroDupla.nome}");
                        }
                        else
                        {
                            dadosGrupo.Inscritos.Add(inscrito.participante.nome);
                        }
                    }
                    dadosCategoria.Grupos.Add(dadosGrupo);
                }
                dadosImpressao.Categorias.Add(dadosCategoria);
            }
            return View(dadosImpressao);
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
                jogos = jogos.Where(j => j.grupoFaseGrupo == numeroGrupo);
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
                jogos = jogos.Where(j => j.desafiante.nome.ToUpper().Contains(nomeJogador.ToUpper()) || j.desafiado.nome.ToUpper().Contains(nomeJogador.ToUpper()) || ((j.desafiante2 != null && j.desafiante2.nome.ToUpper().Contains(nomeJogador.ToUpper())) || (j.desafiado2 != null && j.desafiado2.nome.ToUpper().Contains(nomeJogador.ToUpper()))));
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

        private ResponseMessage VerificarSeJaTemByeNoGrupo(int classeId, int grupo)
        {
            var jaTemByeNesteGrupo = db.Jogo.Where(j => j.desafiante_id == 10 && j.classeTorneio == classeId && j.grupoFaseGrupo == grupo).Any();

            if (jaTemByeNesteGrupo)
            {
                return new ResponseMessage { erro = "Não é possível incluir bye neste grupo pois já existe um bye neste grupo.", retorno = 0 };
            }
            else
            {
                return new ResponseMessage { retorno = 1 };
            }
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
                        if (porEsteJogador == Constantes.Jogo.BYE)
                        {
                            jogo.AlterarJogoParaBye(TipoJogador.DESAFIANTE);
                        }
                        else
                        {
                            jogo.AlterarJogador(TipoJogador.DESAFIANTE, porEsteJogador, porEsteParceiroDupla);
                        }
                    }
                    else if (substituirEsteJogador == jogo.desafiado_id)
                    {
                        if (porEsteJogador == Constantes.Jogo.BYE)
                        {
                            jogo.AlterarJogoParaBye(TipoJogador.DESAFIADO);
                        }
                        else
                        {
                            jogo.AlterarJogador(TipoJogador.DESAFIADO, porEsteJogador, porEsteParceiroDupla);
                        }
                    }
                    if (substituirEsteJogador == Constantes.Jogo.BYE)
                    {
                        jogo.AlterarJogoParaPendente();
                    }
                    if (!(jogo.desafiante_id == Constantes.Jogo.BYE && jogo.desafiado_id == Constantes.Jogo.BYE))
                    {
                        db.Entry(jogo).State = EntityState.Modified;
                        db.SaveChanges();

                        var userIdLog = WebSecurity.GetUserId(User.Identity.Name);
                        var msg = $"ALTERACAO_JOGADOR (B) {DateTimeHelper.GetDateTimeBrasilia()} - ORGANIZADOR: ID:{userIdLog} NOME: {User.Identity.Name} JOGO AFETADO NA TROCA: {jogo.Id}";
                        GravarLogErro(msg);
                    }

                }
                try
                {
                    if (substituirEsteJogador != Constantes.Jogo.BYE)
                    {
                        var inscricao = db.InscricaoTorneio.Where(i => i.classe == classeId && i.userId == substituirEsteJogador).First();
                        if (inscricao.grupo == grupo)
                        {
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

        private void SubstituirJogadorMataMata(int idJogo, int faseTorneio, int classeId, int substituirEsteJogador, int porEsteJogador, int porEsteParceiroDupla)
        {
            //substituirEsteJogador: Novo Jogador informado no jogo alterado
            //porEsteJogador: Jogador anterior do jogo alterado
            if (substituirEsteJogador > 0)
            {
                var listaJogos = db.Jogo.Where(j => j.Id != idJogo && j.grupoFaseGrupo == null && j.faseTorneio == faseTorneio && j.classeTorneio == classeId && (j.desafiante_id == substituirEsteJogador || j.desafiado_id == substituirEsteJogador)).ToList();
                foreach (var jogo in listaJogos)
                {
                    if (substituirEsteJogador == jogo.desafiante_id)
                    {
                        if (porEsteJogador == Constantes.Jogo.BYE)
                        {
                            jogo.AlterarJogoParaBye(TipoJogador.DESAFIANTE);
                        }
                        else
                        {
                            jogo.AlterarJogador(TipoJogador.DESAFIANTE, porEsteJogador, porEsteParceiroDupla);
                        }
                    }
                    else
                    {
                        if (porEsteJogador == Constantes.Jogo.BYE)
                        {
                            jogo.AlterarJogoParaBye(TipoJogador.DESAFIADO);
                        }
                        else
                        {
                            jogo.AlterarJogador(TipoJogador.DESAFIADO, porEsteJogador, porEsteParceiroDupla);
                        }
                    }

                    if (substituirEsteJogador == Constantes.Jogo.BYE)
                    {
                        jogo.AlterarJogoParaPendente();
                    }

                    if (!(jogo.desafiante_id == Constantes.Jogo.BYE && jogo.desafiado_id == Constantes.Jogo.BYE))
                    {
                        db.Entry(jogo).State = EntityState.Modified;
                        db.SaveChanges();

                        var userIdLog = WebSecurity.GetUserId(User.Identity.Name);
                        var msg = $"ALTERACAO_JOGADOR (B) {DateTimeHelper.GetDateTimeBrasilia()} - ORGANIZADOR: ID:{userIdLog} NOME: {User.Identity.Name} JOGO AFETADO NA TROCA: {jogo.Id}";
                        GravarLogErro(msg);

                        //Atualiza o próximo jogo do mata
                        tn.MontarProximoJogoTorneio(jogo);
                    }
                }
            }
        }

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult EditDuplas(int torneioId, int filtroClasse = 0, bool naoFazNada = false)
        {
            List<InscricaoTorneio> duplas = null;
            var classes = db.ClasseTorneio.Where(i => i.torneioId == torneioId && i.isDupla).OrderBy(c => c.Id).ToList();
            ViewBag.Classes = classes;
            if (classes.Count == 0)
            {
                CarregarDadosEssenciais(torneioId, "duplas");
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
            ViewBag.InscricaoSemDupla = duplasNaoFormadas.OrderBy(o => o.participante.nome).ToList();
            ////////////////////////////////
            ViewBag.Inscritos = db.InscricaoTorneio.Where(c => c.torneioId == torneioId && c.classe == filtroClasse).ToList();

            ViewBag.DuplasFormadasComJogos = ObterDuplasJogosFormados(torneioId, filtroClasse, duplasFormadas);

            CarregarDadosEssenciais(torneioId, "duplas");
            return View(duplas);
        }

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult EditFaseGrupo(int torneioId, int filtroClasse = 0)
        {
            List<InscricaoTorneio> inscritos = null;
            var classes = db.ClasseTorneio.Where(i => i.torneioId == torneioId && i.faseGrupo).OrderBy(c => c.nome).ToList();
            ViewBag.Classes = classes;
            if (classes.Count == 0)
            {
                CarregarDadosEssenciais(torneioId, "fasegrupo");
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
            ViewBag.Regra = db.Regra.Find(2).descricao;
            if (verificarSeAFaseDeGrupoFoiFinalizada(classe))
            {
                ViewBag.Classificados = tn.getClassificadosEmCadaGrupo(classe);
            }
            CarregarDadosEssenciais(torneioId, "fasegrupo");
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
                }
                else
                {
                    var jogos = db.Jogo.Where(j => j.classeTorneio == inscricao.classe && (j.desafiado_id == inscricao.userId || j.desafiante_id == inscricao.userId)).ToList();
                    foreach (var item in jogos)
                    {
                        if (item.desafiante_id == inscricao.userId)
                        {
                            item.desafiante2_id = jogador2;
                        }
                        else
                        {
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
                        inscricao = db.InscricaoTorneio.Where(i => i.participante.UserId == usuario.UserId && i.isAtivo && (i.torneio.StatusInscricao == (int)StatusInscricaoPainelTorneio.ABERTA || (i.torneio.StatusInscricao == (int)StatusInscricaoPainelTorneio.LIBERADA_ATE && i.torneio.dataFimInscricoes >= DateTime.Now.Date)) && i.torneio.barragemId == barragemId).OrderByDescending(i => i.Id).Take(1).Single();
                    }
                    else
                    {
                        inscricao = db.InscricaoTorneio.Where(i => i.participante.UserId == usuario.UserId && i.isAtivo && (i.torneio.StatusInscricao == (int)StatusInscricaoPainelTorneio.ABERTA || (i.torneio.StatusInscricao == (int)StatusInscricaoPainelTorneio.LIBERADA_ATE && i.torneio.dataFimInscricoes >= DateTime.Now.Date))).OrderByDescending(i => i.Id).Take(1).Single();
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
        public ActionResult LancarResultado(Jogo j, int atualizarJogosMataMata = 0, int gerarJogosMataMata = 0)
        {
            try
            {
                int perdedorWO = 0;
                Jogo jogo = db.Jogo.Find(j.Id);

                var jogoEraWO = jogo.situacao_Id == 5;
                if (jogoEraWO)
                {
                    var usuarioPerdedorWo = db.InscricaoTorneio.FirstOrDefault(x => x.torneioId == jogo.torneioId && (x.userId == jogo.desafiante_id || x.userId == jogo.desafiado_id) && x.pontuacaoFaseGrupo == -100);
                    if (usuarioPerdedorWo != null)
                    {
                        perdedorWO = usuarioPerdedorWo.userId;
                    }
                    else if (jogo.desafiante_id == Constantes.Jogo.BYE)
                    {
                        perdedorWO = jogo.desafiado_id;
                    }
                    else
                    {
                        perdedorWO = jogo.idDoPerdedor;
                    }
                }

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
                    {
                        //WO
                        jogo.qtddGames1setDesafiado = setDesafiado;
                        jogo.qtddGames1setDesafiante = setDesafiante;
                        jogo.qtddGames2setDesafiado = setDesafiado;
                        jogo.qtddGames2setDesafiante = setDesafiante;
                    }
                    if (jogo.situacao_Id == 1 || jogo.situacao_Id == 2) // pendente ou marcado
                    {
                        jogo.ZerarPontuacao();
                    }
                    db.Entry(jogo).State = EntityState.Modified;
                    db.SaveChanges();
                }
                else
                {
                    //Mensagem = "Não foi possível alterar os dados.";
                    return Json(new { erro = "Dados Inválidos.", retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
                }

                if (jogoEraWO)
                {
                    DesfazerWOFaseGrupo(perdedorWO, (int)jogo.classeTorneio, jogo.Id);
                }

                //calcula os pontos, posicao e monta proximo jogo
                tn.MontarProximoJogoTorneio(jogo);

                tn.consolidarPontuacaoFaseGrupo(jogo);

                VerificarNecessidadeAtualizarMataMata(atualizarJogosMataMata, gerarJogosMataMata, jogo);

                int ligaConsolidadaComSucesso = ValidaConsolidacaoLiga(jogo);
                return Json(new { erro = "", retorno = 1, pontuacaoLiga = ligaConsolidadaComSucesso }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [Authorize(Roles = "admin,usuario,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult LancarWO(int Id, int vencedorWO = 0, int situacao_id = 5, int atualizarJogosMataMata = 0, int gerarJogosMataMata = 0)
        {
            try
            {
                int perderdorWO = 0;

                if (vencedorWO == 0)
                {
                    return Json(new { erro = "Informe o vencedor do jogo.", retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
                }

                Jogo jogoAtual = db.Jogo.Find(Id);

                if ((jogoAtual.rodadaFaseGrupo != 0) && (jogoAtual.situacao_Id == 5) && (jogoAtual.idDoVencedor != vencedorWO))
                {
                    desfazerJogosWOFaseGrupo(jogoAtual.idDoPerdedor, (int)jogoAtual.classeTorneio);
                }
                //alterar quantidade de games para desafiado e desafiante
                if (jogoAtual.desafiado_id == vencedorWO)
                {
                    jogoAtual.AlterarSituacaoJogoParaWO(TipoJogador.DESAFIADO);
                    perderdorWO = jogoAtual.desafiante_id;
                }
                else
                {
                    jogoAtual.AlterarSituacaoJogoParaWO(TipoJogador.DESAFIANTE);
                    perderdorWO = jogoAtual.desafiado_id;
                }

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

                VerificarNecessidadeAtualizarMataMata(atualizarJogosMataMata, gerarJogosMataMata, jogoAtual);

                return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }

        }

        [HttpPost]
        [Authorize(Roles = "admin,usuario,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult LancarDesistencia(Jogo jogoPlacar, int perdedorDesistencia = 0, int atualizarJogosMataMata = 0, int gerarJogosMataMata = 0)
        {
            try
            {
                int perdedorWO = 0;
                int vencedorDesistencia = 0;
                TipoJogador tipoVencedor;

                if (perdedorDesistencia == 0)
                {
                    return Json(new { erro = "Informe o jogador que desistiu do jogo.", retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
                }

                Jogo jogo = db.Jogo.Find(jogoPlacar.Id);

                var jogoEraWO = jogo.situacao_Id == 5;
                if (jogoEraWO)
                {
                    var usuarioPerdedorWo = db.InscricaoTorneio.FirstOrDefault(x => x.torneioId == jogo.torneioId && (x.userId == jogo.desafiante_id || x.userId == jogo.desafiado_id) && x.pontuacaoFaseGrupo == -100);
                    if (usuarioPerdedorWo != null)
                    {
                        perdedorWO = usuarioPerdedorWo.userId;
                    }
                    else if (jogo.desafiante_id == Constantes.Jogo.BYE)
                    {
                        perdedorWO = jogo.desafiado_id;
                    }
                    else
                    {
                        perdedorWO = jogo.idDoPerdedor;
                    }
                }

                jogo.situacao_Id = jogoPlacar.situacao_Id;
                if (jogo.desafiado_id == perdedorDesistencia)
                {
                    tipoVencedor = TipoJogador.DESAFIANTE;
                    vencedorDesistencia = jogo.desafiante_id;
                }
                else
                {
                    tipoVencedor = TipoJogador.DESAFIADO;
                    vencedorDesistencia = jogo.desafiado_id;
                }

                if (jogoPlacar.qtddGames2setDesafiado <= 0 && jogoPlacar.qtddGames2setDesafiante <= 0)
                {
                    //Desistencia no 1º Set
                    if (tipoVencedor == TipoJogador.DESAFIANTE)
                    {
                        jogo.qtddGames1setDesafiante = jogoPlacar.qtddGames1setDesafiado == 6 ? 7 : 6;
                        jogo.qtddGames1setDesafiado = jogoPlacar.qtddGames1setDesafiado;
                        jogo.qtddGames2setDesafiante = 6;
                        jogo.qtddGames2setDesafiado = 0;
                        jogo.qtddGames3setDesafiante = 6;
                        jogo.qtddGames3setDesafiado = 0;
                    }
                    else
                    {
                        jogo.qtddGames1setDesafiante = jogoPlacar.qtddGames1setDesafiante;
                        jogo.qtddGames1setDesafiado = jogoPlacar.qtddGames1setDesafiante == 6 ? 7 : 6;
                        jogo.qtddGames2setDesafiante = 0;
                        jogo.qtddGames2setDesafiado = 6;
                        jogo.qtddGames3setDesafiante = 0;
                        jogo.qtddGames3setDesafiado = 6;
                    }
                }
                else
                {
                    //Desistencia 2º Set
                    if (tipoVencedor == TipoJogador.DESAFIANTE)
                    {
                        jogo.qtddGames1setDesafiante = jogoPlacar.qtddGames1setDesafiante;
                        jogo.qtddGames1setDesafiado = jogoPlacar.qtddGames1setDesafiado;
                        jogo.qtddGames2setDesafiante = 6;
                        jogo.qtddGames2setDesafiado = jogoPlacar.qtddGames2setDesafiado;
                        jogo.qtddGames3setDesafiante = 6;
                        jogo.qtddGames3setDesafiado = 0;
                    }
                    else
                    {
                        jogo.qtddGames1setDesafiado = jogoPlacar.qtddGames1setDesafiado;
                        jogo.qtddGames1setDesafiante = jogoPlacar.qtddGames1setDesafiante;
                        jogo.qtddGames2setDesafiante = jogoPlacar.qtddGames2setDesafiante;
                        jogo.qtddGames2setDesafiado = 6;
                        jogo.qtddGames3setDesafiante = 0;
                        jogo.qtddGames3setDesafiado = 6;
                    }
                }

                if (jogo.qtddSetsGanhosDesafiado == jogo.qtddSetsGanhosDesafiante)
                {
                    return Json(new { erro = "Placar Inválido. Os sets ganhos estão iguais.", retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
                }

                jogo.usuarioInformResultado = User.Identity.Name;
                jogo.dataCadastroResultado = DateTime.Now;

                db.Entry(jogo).State = EntityState.Modified;
                db.SaveChanges();

                if (jogoEraWO)
                {
                    DesfazerWOFaseGrupo(perdedorWO, (int)jogo.classeTorneio, jogo.Id);
                }

                tn.MontarProximoJogoTorneio(jogo);
                tn.consolidarPontuacaoFaseGrupo(jogo);

                VerificarNecessidadeAtualizarMataMata(atualizarJogosMataMata, gerarJogosMataMata, jogo);

                int ligaConsolidadaComSucesso = ValidaConsolidacaoLiga(jogo);
                return Json(new { erro = "", retorno = 1, pontuacaoLiga = ligaConsolidadaComSucesso }, "text/plain", JsonRequestBehavior.AllowGet);
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

        private void DesfazerWOFaseGrupo(int perdedorId, int classeId, int idJogo)
        {
            var jogos = db.Jogo.Where(j => j.classeTorneio == classeId && j.rodadaFaseGrupo != 0 && (j.desafiado_id == perdedorId || j.desafiante_id == perdedorId) && j.Id != idJogo).ToList();
            foreach (var jogo in jogos)
            {
                jogo.situacao_Id = 1;
                jogo.usuarioInformResultado = User.Identity.Name;
                jogo.dataCadastroResultado = DateTime.Now;
                db.Entry(jogo).State = EntityState.Modified;
                db.SaveChanges();
            }
            var inscrito = db.InscricaoTorneio.Where(i => i.userId == perdedorId && i.classe == classeId && i.isAtivo).ToList();
            inscrito[0].pontuacaoFaseGrupo = 0;
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
            CarregarDadosEssenciais(torneioId, "notificacao");
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
            if (jogos.Count > 0)
            {
                ViewBag.nomeClasse = jogos[0].classe.nome;
                return View(jogos);
            }
            else
            {
                return RedirectToAction("Tabela", "Torneio", new { torneioId = torneioId, filtroClasse = fClasse, Msg = "Não existe tabela para esta categoria portanto não é possível imprimir." });
            }
        }

        [HttpGet]
        public ActionResult ImprimirTabelaFaseGrupo(int torneioId, int filtroClasse = 0)
        {
            var dadosImpressao = new ImpressaoJogoFaseGrupoModel();
            var torneio = db.Torneio.Find(torneioId);

            dadosImpressao.NomeTorneio = torneio.nome;
            dadosImpressao.NomeRanking = torneio.barragem.nome;
            dadosImpressao.IdBarragem = torneio.barragemId;

            var jogos = db.Jogo.Where(r => r.torneioId == torneioId && r.classeTorneio == filtroClasse && r.rodadaFaseGrupo != 0).OrderBy(r => r.rodadaFaseGrupo).ToList();

            if (jogos.Count > 0)
            {
                dadosImpressao.NomeClasse = jogos[0].classe.nome;

                dadosImpressao.Grupos = new List<ImpressaoJogoFaseGrupoModel.GrupoJogosModel>();

                foreach (var grupo in jogos.GroupBy(x => x.grupoFaseGrupo))
                {
                    var grupoImpressao = new ImpressaoJogoFaseGrupoModel.GrupoJogosModel();
                    grupoImpressao.Grupo = $"GRUPO {grupo.Key}";
                    grupoImpressao.Jogos = grupo?.ToList() ?? new List<Jogo>();
                    dadosImpressao.Grupos.Add(grupoImpressao);
                }
                return View(dadosImpressao);
            }
            else
            {
                return RedirectToAction("Tabela", "Torneio", new { torneioId = torneioId, filtroClasse = filtroClasse, Msg = "Não existe tabela para esta categoria portanto não é possível imprimir." });
            }
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
        public ActionResult CreateTorneio(Torneio torneio, bool transferencia = false, string pontuacaoLiga = "100")
        {
            torneio.inscricaoSoPeloSite = false;
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            var barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
            torneio.barragemId = barragemId;
            var barragem = db.Barragens.Find(torneio.barragemId);
            ViewBag.isModeloTodosContraTodos = barragem.isModeloTodosContraTodos;

            ValidarFormaPgtoTransferenciaBancaria(torneio, transferencia);

            torneio.StatusInscricao = (int)StatusInscricaoPainelTorneio.LIBERADA_ATE;
            torneio.isAtivo = true;
            torneio.liberarEscolhaDuplas = true;
            torneio.divulgaCidade = false;
            torneio.isOpen = false;
            if (pontuacaoLiga != "-")
            {
                torneio.TipoTorneio = pontuacaoLiga;
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
            if (barragem?.soTorneio == true)
            {
                torneio.regulamento = ObterRegulamentoTorneio(barragem);
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
                            // se o torneio pontua para uma liga de federação a pontuação do torneio será a cadastrada previamente nesta liga
                            var lg = db.Liga.Find(idLiga);
                            if (lg.barragemId == null || lg.barragemId == 1)
                            {
                                torneio.TipoTorneio = db.BarragemLiga.Where(b => b.LigaId == lg.Id && b.BarragemId == barragem.Id).FirstOrDefault().TipoTorneio;
                                db.Entry(torneio).State = EntityState.Modified;
                                db.SaveChanges();
                            }
                            // verificar se é o primeiro torneio do circuito. Se for, cadastrar as categorias na liga
                            var torneiosCriados = db.TorneioLiga.Where(t => t.LigaId == idLiga && t.Liga.barragemId == torneio.barragemId).Count();
                            if (torneiosCriados == 1)
                            {
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
                            if (ligaDoRanking != 0)
                            {
                                classeLiga = new ClasseLiga
                                {
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
                ViewBag.barragemId = barragemId;
            }
            return View();
        }

        private string ObterRegulamentoTorneio(Barragens barragem)
        {
            if (barragem.isBeachTenis)
            {
                if (barragem.isModeloTodosContraTodos)
                {
                    return db.Regra.Find(4)?.descricao;
                }
                else
                {
                    return db.Regra.Find(3)?.descricao;
                }
            }
            else
            {
                return db.Regra.Find(5)?.descricao;
            }
        }

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis")]
        public ActionResult IncluirCategoriaNoCircuito(int categoriaId)
        {
            try
            {
                var circuito = ObterUltimaLigaBarragem();
                if (circuito == null)
                {
                    return Json(new { erro = "Você ainda não possui um circuito próprio.", retorno = 1 }, "application/json", JsonRequestBehavior.AllowGet);
                }

                if (!ValidarCategoriaExistenteLiga(categoriaId, circuito.Id))
                {
                    SalvarClasseLiga(categoriaId, circuito.Id);
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
                categorias = db.Categoria.Where(c => (c.rankingId == 0 || c.rankingId == barragemId) && c.isDupla == true).OrderBy(c => c.ordemExibicao).ThenBy(c => c.Nome).ToList();
            }
            else
            {
                categorias = db.Categoria.Where(c => c.rankingId == 0 || c.rankingId == barragemId).OrderBy(c => c.ordemExibicao).ThenBy(c => c.isDupla).ThenBy(c => c.Nome).ToList();
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
            ViewBag.UnicoCircuitoBeachTennis = ValidarRegraUnicoCircuitoBeachTennis(barragem.isBeachTenis, ligasId.Count, qtddTorneios);
            ViewBag.Categorias = categoriasDeLiga;

            return View();
        }

        private bool ValidarRegraUnicoCircuitoBeachTennis(bool isBeachTennis, int qtdeCircuitos, int qtdeTorneios)
        {
            return isBeachTennis && qtdeCircuitos == 1 && qtdeTorneios == 0;
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

                if (dataHoje.DayOfYear == torneio.DataFinalInscricoes.DayOfYear)
                {
                    conteudo = "Último dia para fazer sua inscrição";
                }
                else
                {
                    if (torneio.StatusInscricao == (int)StatusInscricaoPainelTorneio.ABERTA)
                    {
                        conteudo = "Inscrições abertas";
                    }
                    else
                    {
                        conteudo = "Faça sua inscrição até o dia " + torneio.DataFinalInscricoes;
                    }
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
        public ActionResult PainelControle(string msg = "")
        {
            if (msg != "")
            {
                ViewBag.sucesso = "ok";
            }
            List<Torneio> torneios = new List<Torneio>();
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            var barragem = (from up in db.UserProfiles where up.UserId == userId select up.barragem).Single();
            var agora = DateTime.Now.AddDays(-1);
            var torneiosAndamento = db.Torneio.Where(t => t.dataFim > agora && t.barragemId == barragem.Id).OrderByDescending(c => c.Id).ToList();
            foreach (var item in torneiosAndamento)
            {
                item.LinkCopia = ObterLinkTorneio(item, item.barragemId);
                // estou colocando estes dados em outros campos do objeto. Gambiarra!!!
                item.qtddClasses = db.InscricaoTorneio.Where(i => i.torneioId == item.Id).Select(i => (int)i.userId).Distinct().Count();
                item.valor = db.InscricaoTorneio.Where(i => i.torneioId == item.Id && i.isAtivo == true).Select(i => new { user = (int)i.userId, valor = i.valor }).Distinct().Sum(i => i.valor);

            }
            ViewBag.torneiosEmAndamento = torneiosAndamento;
            torneios = db.Torneio.Where(r => r.barragemId == barragem.Id && r.dataFim < agora).OrderByDescending(c => c.Id).Take(4).ToList();
            ViewBag.circuitos = db.Liga.Where(b => b.barragemId == barragem.Id).OrderByDescending(b => b.Id).ToList();
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
                if (i == 0)
                {
                    db.Entry(r).State = EntityState.Modified;
                }
                else
                {
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

        private QrCodeCobrancaTorneio GetQrCodeCobrancaPIX(Torneio torneio, CobrancaTorneio cobrancaTorneio)
        {
            try
            {
                var order = montarPedidoPIX(torneio, cobrancaTorneio);

                var cobrancaPix = new PIXPagSeguro().CriarPedido(order);
                var qrcode = new QrCodeCobrancaTorneio();
                qrcode.text = cobrancaPix.qr_codes[0].text;
                if (cobrancaPix.qr_codes[0].links[0].media == "image/png")
                {
                    qrcode.link = cobrancaPix.qr_codes[0].links[0].href;
                }
                return qrcode;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private Order montarPedidoPIX(Torneio torneio, CobrancaTorneio cobrancaTorneio)
        {
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
            item.unit_amount = (int)cobrancaTorneio.valorASerPago * 100;
            order.items = new List<ItemPedido>();
            order.items.Add(item);
            var qr_code = new QrCode();
            var amount = new Amount();
            amount.value = (int)cobrancaTorneio.valorASerPago * 100;
            qr_code.amount = amount;
            order.qr_codes = new List<QrCode>();
            order.qr_codes.Add(qr_code);
            order.notification_urls = new string[1];
            order.notification_urls[0] = "https://www.rankingdetenis.com/api/TorneioAPI/NotificacaoPagamentoOrganizador";

            return order;
        }

        private void CarregarComboCategoriasCircuito(int torneioId)
        {
            List<Categoria> categoriasLiga = new List<Categoria>();
            List<TorneioLiga> ligasDotorneio = db.TorneioLiga.Where(tl => tl.TorneioId == torneioId).ToList();

            if (ligasDotorneio != null)
            {
                List<int> ligas = new List<int>();
                foreach (TorneioLiga tl in ligasDotorneio)
                {
                    ligas.Add(tl.LigaId);
                }
                foreach (ClasseLiga cl in db.ClasseLiga.Where(classeLiga => ligas.Contains(classeLiga.LigaId)).GroupBy(cl => cl.CategoriaId).Select(c => c.FirstOrDefault()).ToList())
                {
                    categoriasLiga.Add(db.Categoria.Find(cl.CategoriaId));
                }
            }

            var categoriasPadrao = ObterCategoriasPadraoSistema(string.Empty);

            var todasCategorias = categoriasPadrao;
            todasCategorias.AddRange(categoriasLiga.Select(s => new CategoriaAutoComplete { id = s.Id, label = s.Nome, value = s.Nome }));
            todasCategorias = todasCategorias.GroupBy(x => x.id).Select(s => s.FirstOrDefault()).OrderBy(x => x.label).ToList();
            var categoriasDisponiveis = ValidarCategoriasDisponiveis(torneioId, todasCategorias);

            ViewBag.Categorias = categoriasDisponiveis;
        }

        [HttpGet]
        public ActionResult ValidarJogosJaGerados(int torneioId, int[] classeIds)
        {
            try
            {
                var classesComJogosGerados = ObterClassesJogosJaGeradosEFinalizados(torneioId, classeIds);
                string dadosMensagem;
                if (classesComJogosGerados.Count > 0)
                {
                    dadosMensagem = string.Join("</br>", classesComJogosGerados);
                }
                else
                {
                    dadosMensagem = "OK";
                }

                return Json(new { erro = "", retorno = dadosMensagem }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = "ERRO" }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        private List<string> ObterClassesJogosJaGeradosEFinalizados(int torneioId, int[] classeIds, ICollection<ClasseTorneio> classesTorneio = null)
        {
            List<string> classesComJogosGerados = new List<string>();
            List<int> idsSituacaoJogosFinalizados = new List<int>()
            {
                { Constantes.Jogo.Situacao.EM_ANDAMENTO },
                { Constantes.Jogo.Situacao.FINALIZADO },
                { Constantes.Jogo.Situacao.WO },
                { Constantes.Jogo.Situacao.DESISTENCIA }
            };

            bool ehMataMataSeguidoFaseGrupo = false;
            bool ehMataMata = false;

            if (classeIds != null)
            {
                if (classesTorneio == null)
                {
                    classesTorneio = db.ClasseTorneio.Where(x => x.torneioId == torneioId).ToList();
                }

                foreach (var classeId in classeIds)
                {
                    //obter informacoes da classe e jogos
                    var classe = classesTorneio.FirstOrDefault(x => x.Id == classeId);
                    var possuiJogos = db.Jogo.Any(x => x.torneioId == torneioId && x.classeTorneio == classeId);
                    var qtdeJogosGeradosFaseGrupo = db.Jogo.Count(x => x.torneioId == torneioId && x.classeTorneio == classeId && x.grupoFaseGrupo != null && (x.situacao_Id == 1 || x.situacao_Id == 2));

                    //obter tipo a checar os jogos gerados
                    if (classe.faseGrupo)
                    {
                        ehMataMataSeguidoFaseGrupo = false;
                    }

                    if (classe.faseMataMata)
                    {
                        ehMataMata = true;
                    }

                    if (possuiJogos)
                    {
                        if (classe.faseGrupo && classe.faseMataMata && qtdeJogosGeradosFaseGrupo == 0)
                        {
                            ehMataMataSeguidoFaseGrupo = true;
                        }
                    }

                    //Validar jogos gerados
                    var classeComJogosGerados = db.Jogo.Any(x => x.torneioId == torneioId && x.classeTorneio == classeId
                      && ((x.rodadaFaseGrupo != 0 && !ehMataMataSeguidoFaseGrupo) || (x.rodadaFaseGrupo == 0 && ehMataMataSeguidoFaseGrupo) || (x.rodadaFaseGrupo == 0 && ehMataMata))
                      &&
                      (
                          (x.dataJogo != null && x.horaJogo != null && !ehMataMataSeguidoFaseGrupo)
                          ||
                          (idsSituacaoJogosFinalizados.Contains(x.situacao_Id) && (x.qtddGames1setDesafiado > 0 || x.qtddGames1setDesafiante > 0 || x.qtddGames2setDesafiado > 0 || x.qtddGames2setDesafiante > 0 || x.qtddGames3setDesafiado > 0 || x.qtddGames3setDesafiante > 0) && x.desafiante_id != 10 && x.desafiado_id != 10)
                      )
                     );

                    if (classeComJogosGerados)
                    {
                        classesComJogosGerados.Add(classe.nome);
                    }
                }
            }
            return classesComJogosGerados;
        }

        private string ValidarAlteracaoJogoFaseGrupoComMataMataExistente(Jogo jogo, int situacaoId)
        {
            List<string> classesComJogosGerados = new List<string>();

            bool ehMataMataSeguidoFaseGrupo = jogo.classe.faseGrupo && jogo.classe.faseMataMata;
            bool lancamentoUltimoJogoFaseGrupo = false;
            bool todosJogosFaseGrupoFinalizados = false;
            var jogoEhFaseGrupo = jogo.grupoFaseGrupo != null;

            if (!jogoEhFaseGrupo)
            {
                return "OK";
            }

            var qtdeJogosPendentes = ObterQtdeJogosPendentesFaseGrupo(jogo.torneioId.Value, jogo.classeTorneio.Value);
            if (ehMataMataSeguidoFaseGrupo && qtdeJogosPendentes == 0 && situacaoId != Constantes.Jogo.Situacao.PENDENTE && situacaoId != Constantes.Jogo.Situacao.MARCADO)
            {
                todosJogosFaseGrupoFinalizados = true;
            }
            else if (ehMataMataSeguidoFaseGrupo && qtdeJogosPendentes == 1)
            {
                var jogosPendentes = ObterJogosPendentesFaseGrupo(jogo.torneioId.Value, jogo.classeTorneio.Value);
                if (jogosPendentes.Any(x => x.Id == jogo.Id))
                {
                    lancamentoUltimoJogoFaseGrupo = true;
                }
            }

            //Validar jogos gerados
            var temJogosGerados = db.Jogo.Any(x => x.torneioId == jogo.torneioId && x.classeTorneio == jogo.classe.Id && x.rodadaFaseGrupo == 0 && ehMataMataSeguidoFaseGrupo
                      && x.desafiante_id != Constantes.Jogo.BYE && x.desafiante_id != Constantes.Jogo.AGUARDANDO_JOGADOR && x.desafiado_id != Constantes.Jogo.AGUARDANDO_JOGADOR);

            if (todosJogosFaseGrupoFinalizados && temJogosGerados)
                return "NECESSITA_ATUALIZAR_MATA_MATA";
            else if (lancamentoUltimoJogoFaseGrupo && !temJogosGerados)
                return "SOLICITAR_GERACAO_MATA_MATA";
            else
                return "OK";
        }

        public ActionResult ObterCategorias(int torneioId, string filtro)
        {
            var categorias = ObterCategoriasPadraoSistema(filtro);
            var categoriasDisponiveis = ValidarCategoriasDisponiveis(torneioId, categorias);
            return Json(categoriasDisponiveis.ToArray(), JsonRequestBehavior.AllowGet);
        }

        private IEnumerable<CategoriaAutoComplete> ValidarCategoriasDisponiveis(int torneioId, List<CategoriaAutoComplete> categorias)
        {
            var categoriasJaVinculadas = ObterClassesTorneio(torneioId);
            var categoriasDisponiveis = categorias.Where(x => !categoriasJaVinculadas.Any(y => y.categoriaId == x.id));

            if (categoriasDisponiveis == null)
                categoriasDisponiveis = new List<CategoriaAutoComplete>();

            return categoriasDisponiveis;
        }

        private List<CategoriaAutoComplete> ObterCategoriasPadraoSistema(string filtro)
        {
            var barragemId = ObterIdBarragemUsuario();
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            bool ehAdminTorneio = false;

            if (perfil.Equals("adminTorneio"))
            {
                ehAdminTorneio = true;
            }

            return db.Categoria
                    .Where(x => (x.rankingId == 0 || x.rankingId == barragemId) && ((ehAdminTorneio && x.isDupla) || !ehAdminTorneio) && ((x.Nome.ToUpper().StartsWith(filtro.ToUpper()) && !string.IsNullOrEmpty(filtro)) || string.IsNullOrEmpty(filtro)))
                    .OrderBy(o => o.ordemExibicao)
                    .ThenBy(o => o.isDupla)
                    .ThenBy(o => o.Nome)
                    .Select(s => new CategoriaAutoComplete { id = s.Id, label = s.Nome, value = s.Nome })
                    .ToList();
        }


        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis")]
        public ActionResult VincularCategoriaCircuito(int torneioId, int categoriaId, string nomeCategoria, bool isDupla)
        {
            try
            {
                var ligasTorneio = ObterLigasTorneio(torneioId);

                if (categoriaId == 0 && !string.IsNullOrEmpty(nomeCategoria))
                {
                    categoriaId = SalvarCategoria(nomeCategoria, ObterIdBarragemUsuario(), isDupla);
                }

                foreach (var ligaTorneio in ligasTorneio)
                {
                    if (categoriaId > 0 && !ValidarCategoriaExistenteLiga(categoriaId, ligaTorneio.LigaId))
                    {
                        SalvarClasseLiga(categoriaId, ligaTorneio.LigaId);
                    }
                }

                return Json(new { erro = "", retorno = 1, categoria = new { id = categoriaId, nome = nomeCategoria } }, "application/json", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = "Falha ao incluir classe no circuito:" + ex.Message, retorno = 0 }, "application/json", JsonRequestBehavior.AllowGet);
            }
        }

        public int ObterIdBarragemUsuario()
        {
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            return (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
        }

        public Liga ObterUltimaLigaBarragem()
        {
            var barragemId = ObterIdBarragemUsuario();
            return db.Liga.Where(l => l.barragemId == barragemId).OrderByDescending(l => l.Id).FirstOrDefault();
        }

        public List<TorneioLiga> ObterLigasTorneio(int torneioId)
        {
            return db.TorneioLiga.Where(l => l.TorneioId == torneioId).ToList();
        }

        public bool ValidarCategoriaExistenteLiga(int categoriaId, int circuitoId)
        {
            return db.ClasseLiga.Any(c => c.CategoriaId == categoriaId && c.LigaId == circuitoId);
        }

        public bool SalvarClasseLiga(int categoriaId, int circuitoId)
        {
            var categoria = db.Categoria.Find(categoriaId);
            var classeLiga = new ClasseLiga
            {
                Nome = categoria.Nome,
                CategoriaId = categoriaId,
                LigaId = circuitoId
            };
            db.ClasseLiga.Add(classeLiga);
            return db.SaveChanges() > 0;
        }

        public int SalvarCategoria(string nomeCategoria, int barragemId, bool isDupla)
        {
            var categoria = new Categoria
            {
                Nome = nomeCategoria,
                isDupla = isDupla,
                rankingId = barragemId,
                ordemExibicao = 0
            };
            db.Categoria.Add(categoria);
            db.SaveChanges();
            return categoria.Id;
        }

        public IEnumerable<ClasseTorneio> ObterClassesTorneio(int torneioId)
        {
            return db.ClasseTorneio.Where(x => x.torneioId == torneioId && x.categoriaId != null);
        }

        public void ValidarLimitadorInscricoesTorneio(bool qtdeInscricoesLimitada, int torneioId)
        {
            if (!qtdeInscricoesLimitada)
            {
                var classesTorneio = db.ClasseTorneio.Where(x => x.torneioId == torneioId && x.maximoInscritos > 0);
                if (classesTorneio.Any())
                {
                    foreach (var item in classesTorneio)
                    {
                        item.maximoInscritos = 0;
                        db.Entry(item).State = EntityState.Modified;
                    }
                    db.SaveChanges();
                }
            }
        }

        public ActionResult EditCabecaChave(int torneioId, int filtroClasse = 0, string filtroJogador = "", string Msg = "")
        {
            List<CabecaChaveModel> dadosTela = new List<CabecaChaveModel>();
            List<InscricaoTorneio> inscricao = db.InscricaoTorneio.Where(i => i.torneioId == torneioId).ToList();
            var torneio = db.Torneio.Find(torneioId);

            var listaClasses = db.ClasseTorneio.Where(c => c.torneioId == torneioId).ToList();
            ViewBag.filtroClasse = filtroClasse;

            if (filtroClasse == 0)
            {
                var primeiraClasse = listaClasses.FirstOrDefault();
                filtroClasse = primeiraClasse.Id;
            }

            inscricao = inscricao.Where(i => i.classe == filtroClasse).ToList();

            if (filtroJogador != "")
            {
                inscricao = inscricao.Where(i => i.participante.nome.ToUpper().Contains(filtroJogador.ToUpper())).ToList();
            }

            var classe = listaClasses.FirstOrDefault(x => x.Id == filtroClasse);
            if (classe.isDupla)
            {
                var duplasFormadas = inscricao.Where(d => d.parceiroDuplaId != null).ToList();
                var duplasNaoFormadas = inscricao.Where(d => d.parceiroDuplaId == null).ToList();

                foreach (var ins in duplasFormadas)
                {
                    var usuarioParceiroDupla = duplasNaoFormadas.Where(i => i.userId == ins.parceiroDuplaId).FirstOrDefault();
                    if (usuarioParceiroDupla != null)
                        duplasNaoFormadas.Remove(usuarioParceiroDupla);
                }

                var dadosInscricaoDupla = duplasFormadas.ToList();

                dadosTela = PopularDadosCabecaChave(dadosInscricaoDupla, inscricao, true);
            }
            else
            {
                dadosTela = PopularDadosCabecaChave(inscricao, inscricao, false);
            }

            var possuiJogos = db.Jogo.Any(x => x.torneioId == torneioId && x.classeTorneio == classe.Id);

            ViewBag.ClasseJogosJaGerados = possuiJogos;
            ViewBag.CabecasDeChave = getOpcoesCabecaDeChave(filtroClasse);
            ViewBag.CircuitosImpCabecaChave = ObterCircuitosImportacaoCabecaChave(torneioId, filtroClasse);
            ViewBag.Classes = listaClasses;
            ViewBag.filtroClasse = filtroClasse;
            CarregarDadosEssenciais(torneioId, "cabecachave");
            mensagem(Msg);
            return View(dadosTela);
        }

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult PainelTorneio(int torneioId)
        {
            var dadosPagina = new PainelTorneioModel();

            var torneio = db.Torneio.Find(torneioId);

            List<SelectListItem> opcoesStatusInscricao = new List<SelectListItem>()
            {
                { new SelectListItem() { Text = "Recebendo inscrições", Value = ((int)StatusInscricaoPainelTorneio.ABERTA).ToString() } },
                { new SelectListItem() { Text = "Não receber inscrições", Value = ((int)StatusInscricaoPainelTorneio.ENCERRADA).ToString() } },
                { new SelectListItem() { Text = "Receber inscrições só até:", Value = ((int)StatusInscricaoPainelTorneio.LIBERADA_ATE).ToString() } }
            };

            List<SelectListItem> opcoesDivulgacao = new List<SelectListItem>()
            {
                { new SelectListItem() { Text = "Não divulgar por enquanto", Value = "nao divulgar" } },
                { new SelectListItem() { Text = "No meu ranking", Value = "ranking" } },
                { new SelectListItem() { Text = "Na minha cidade", Value = "cidade" } }
            };

            dadosPagina.TorneioId = torneioId;
            dadosPagina.DataFimInscricoes = torneio.dataFimInscricoes;
            dadosPagina.IsAtivo = torneio.isAtivo;
            dadosPagina.LiberaVisualizacaoTabela = torneio.liberarTabela;
            dadosPagina.LiberaVisualizacaoInscritos = torneio.liberaTabelaInscricao;
            dadosPagina.LinkParaCopia = ObterLinkTorneio(torneio, torneio.barragemId);
            dadosPagina.ListaOpcoesStatusInscricao = new SelectList(opcoesStatusInscricao, "Value", "Text", torneio.StatusInscricao);
            dadosPagina.ListaOpcoesDivulgacao = new SelectList(opcoesDivulgacao, "Value", "Text", torneio.divulgacao);


            dadosPagina.InscIndividuais = db.InscricaoTorneio.Where(i => i.torneioId == torneioId).Select(i => (int)i.userId).Distinct().Count();
            dadosPagina.InscIndividuaisSocios = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.isSocio == true).Select(i => (int)i.userId).Distinct().Count();
            dadosPagina.InscIndividuaisFederados = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.isFederado == true).Select(i => (int)i.userId).Distinct().Count();
            dadosPagina.TotalPagantes = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.isAtivo == true).Select(i => (int)i.userId).Distinct().Count();
            if (dadosPagina.TotalPagantes != 0)
            {
                dadosPagina.ValorPago = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.isAtivo == true).Select(i => new { user = (int)i.userId, valor = i.valor }).Distinct().Sum(i => i.valor) ?? 0;
            }
            dadosPagina.PagoNoCartao = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.isAtivo == true && (i.statusPagamento == "3" || i.statusPagamento == "4" || i.statusPagamento == "PAID")).
                Select(i => (int)i.userId).Distinct().Count();

            CarregarDadosEssenciais(torneioId, "painelTorneio");
            return View(dadosPagina);
        }

        private List<CabecaChaveModel> PopularDadosCabecaChave(List<InscricaoTorneio> inscricoes, List<InscricaoTorneio> todasIncricoes, bool classeDupla)
        {
            List<CabecaChaveModel> dadosTela = new List<CabecaChaveModel>();


            foreach (var item in inscricoes)
            {
                var itemCabecaChave = new CabecaChaveModel();
                itemCabecaChave.IdTorneio = item.torneioId;
                itemCabecaChave.IdInscricao = item.Id;
                itemCabecaChave.IdClasse = item.classe;
                itemCabecaChave.CabecaChave = item.cabecaChave;
                itemCabecaChave.EhDupla = classeDupla;
                itemCabecaChave.IdParticipante = item.userId;
                itemCabecaChave.NomeParticipante = item.participante.nome;
                itemCabecaChave.UserNameParticipante = item.participante.UserName;
                itemCabecaChave.InscricaoParticipantePaga = item.isAtivo;

                if (classeDupla)
                {
                    if (item.parceiroDupla != null)
                    {
                        itemCabecaChave.IdParceiroDupla = item.parceiroDuplaId.Value;
                        itemCabecaChave.NomeParceiroDupla = item.parceiroDupla.nome;
                        itemCabecaChave.UserNameParceiroDupla = item.parceiroDupla.UserName;

                        bool iscricaoParceiroPaga = todasIncricoes.Count(x => x.userId == itemCabecaChave.IdParceiroDupla && x.isAtivo) == 1;
                        itemCabecaChave.InscricaoParceiroDuplaPaga = iscricaoParceiroPaga;

                        itemCabecaChave.TodaInscricaoPaga = itemCabecaChave.InscricaoParceiroDuplaPaga && itemCabecaChave.InscricaoParticipantePaga;
                    }
                    else
                    {
                        itemCabecaChave.EhDupla = false;
                        itemCabecaChave.TodaInscricaoPaga = itemCabecaChave.InscricaoParticipantePaga;
                    }
                }
                else
                {
                    itemCabecaChave.TodaInscricaoPaga = itemCabecaChave.InscricaoParticipantePaga;
                }
                dadosTela.Add(itemCabecaChave);
            }
            return dadosTela;
        }

        [HttpPost]
        public ActionResult EditCabecaChave(int Id, int cabecaChave)
        {
            try
            {
                List<string> classesPagtoOk = new List<string>();
                var inscricao = db.InscricaoTorneio.Find(Id);
                var cabecaChaveAnterior = inscricao.cabecaChave;
                AtualizarCabecaChave(inscricao, cabecaChave);
                if (db.Jogo.Any(x => x.torneioId == inscricao.torneioId && x.classeTorneio == inscricao.classe))
                {
                    //Se já tinha jogos para a classe grava no log
                    var msg = $"ALTERACAO_CABECA_CHAVE_JOGOS_JA_EXISTENTES - Id Inscrição: {inscricao.Id} Id User: {inscricao.userId}. CABECA CHAVE ANTERIOR: {cabecaChaveAnterior} CABECA CHAVE ATUAL: {inscricao.cabecaChave}";
                    GravarLogErro(msg);
                }

                return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult LiberarVisualizacaoTabela(int torneioId, bool liberar)
        {
            try
            {
                var torneio = db.Torneio.Find(torneioId);
                torneio.liberarTabela = liberar;
                db.Entry(torneio).State = EntityState.Modified;
                db.SaveChanges();

                return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult LiberarVisualizacaoInscritos(int torneioId, bool liberar)
        {
            try
            {
                var torneio = db.Torneio.Find(torneioId);
                torneio.liberaTabelaInscricao = liberar;
                db.Entry(torneio).State = EntityState.Modified;
                db.SaveChanges();

                return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult AtualizarStatusInscricao(int torneioId, StatusInscricaoPainelTorneio statusInscricao, string dataFimInscricao)
        {
            try
            {
                var torneio = db.Torneio.Find(torneioId);

                if (statusInscricao == StatusInscricaoPainelTorneio.LIBERADA_ATE)
                {
                    torneio.dataFimInscricoes = DateTime.ParseExact(dataFimInscricao, "dd/MM/yyyy", null);
                }
                torneio.StatusInscricao = (int)statusInscricao;

                db.Entry(torneio).State = EntityState.Modified;
                db.SaveChanges();

                return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult AtualizarDivulgacaoTorneio(int torneioId, string opcaoSelecionada)
        {
            try
            {
                var torneio = db.Torneio.Find(torneioId);

                if (!string.Equals(opcaoSelecionada, "nao divulgar", StringComparison.OrdinalIgnoreCase) && !torneio.PagSeguroAtivo && string.IsNullOrEmpty(torneio.dadosBancarios) && !torneio.isGratuitoSocio)
                {
                    return Json(new { erro = "Não foi possível liberar inscrições, selecione uma forma de pagamento na aba Informações do torneio.", retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
                }

                torneio.isAtivo = true;
                torneio.divulgaCidade = false;
                torneio.isOpen = false;

                if (opcaoSelecionada == "nao divulgar")
                {
                    torneio.isAtivo = false;
                }
                if (opcaoSelecionada == "cidade")
                {
                    torneio.divulgaCidade = true;
                }

                torneio.divulgacao = opcaoSelecionada;

                db.Entry(torneio).State = EntityState.Modified;
                db.SaveChanges();

                return Json(new { erro = "", retorno = 1, isAtivo = torneio.isAtivo }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        private void GravarLogErro(string msgErro)
        {
            if (msgErro.Length > 500) msgErro = msgErro.Substring(0, 500);
            db.Log.Add(new Log() { descricao = msgErro });
            db.SaveChanges();
        }

        private void CarregarDadosEssenciais(int torneioId, string abaSelecionada)
        {
            var torneio = db.Torneio.Find(torneioId);

            ViewBag.flag = abaSelecionada;
            ViewBag.TorneioId = torneioId;
            ViewBag.NomeTorneio = torneio.nome;
        }

        private string ObterLinkTorneio(Torneio torneio, int barragemId)
        {
            var hoje = DateTime.Now.Date;
            var qtdeTorneiosEmAndamento = db.Torneio.Count(t => t.dataFim >= hoje && t.barragemId == barragemId);

            if (qtdeTorneiosEmAndamento == 1 && !string.IsNullOrEmpty(torneio.barragem.dominio))
            {
                return "https://" + HttpContext.Request.Url.Host + "/torneio-" + torneio.barragem.dominio;
            }
            else
            {
                return "https://" + HttpContext.Request.Url.Host + "/torneio-" + torneio.Id;
            }
        }

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        public ActionResult EditJogos(int torneioId, int fClasse = 0, string fData = "", string fNomeJogador = "", string fGrupo = "0", int fase = 0)
        {
            var jogosTorneio = db.Jogo.Where(i => i.torneioId == torneioId);
            var classes = db.ClasseTorneio.Where(i => i.torneioId == torneioId).OrderBy(c => c.Id).ToList();
            var classesGeradas = jogosTorneio.Select(i => (int)i.classeTorneio).Distinct().ToList();

            if (fClasse == 0)
            {
                fClasse = classes.FirstOrDefault()?.Id ?? 0;
                ViewBag.fClasse = fClasse;
            }
            else if (fClasse > 1)
            {
                ViewBag.permitirEdicaoDeJogador = true;
                ViewBag.PrimeiraFase = jogosTorneio.Where(r => r.classeTorneio == fClasse).Max(r => r.faseTorneio);

                var cl = classes.Where(c => c.Id == fClasse).FirstOrDefault();
                if (cl == null)
                {
                    ViewBag.Inscritos = new List<InscricaoTorneio>();
                }
                else if (cl.isDupla)
                {
                    ViewBag.Inscritos = db.InscricaoTorneio.Where(c => c.torneioId == torneioId && c.isAtivo && c.parceiroDuplaId != null && c.classe == fClasse).ToList();
                }
                else
                {
                    ViewBag.Inscritos = db.InscricaoTorneio.Where(c => c.torneioId == torneioId && c.isAtivo && c.classe == fClasse).ToList();
                }
            }

            var listaJogos = filtrarJogos(jogosTorneio, fClasse, fData, fGrupo, fase, false, fNomeJogador);

            var classesFaseGrupoNaoFinalizadas = jogosTorneio
                .Where(i => i.grupoFaseGrupo != null && (i.situacao_Id == 1 || i.situacao_Id == 2))
                .Select(i => (int)i.classeTorneio).Distinct().ToList();

            var classesGeradasMataMata = jogosTorneio.Where(x => x.grupoFaseGrupo == null && x.desafiante_id != Constantes.Jogo.BYE && x.desafiante_id != Constantes.Jogo.AGUARDANDO_JOGADOR && x.desafiado_id != Constantes.Jogo.AGUARDANDO_JOGADOR)
                .Select(i => (int)i.classeTorneio)
                .Distinct().ToList();

            ViewBag.CategoriasValidarQtdeJogadores = ObterCategoriasValidarQtdeJogadores(torneioId);
            ViewBag.CategoriasInscricoesNaoPagas = ObterCategoriasInscricoesNaoPagas(torneioId);
            var classeSelecionada = classes.FirstOrDefault(x => x.Id == fClasse);
            ViewBag.ClasseEhFaseGrupo = classeSelecionada != null ? classeSelecionada.faseGrupo : false;

            ViewBag.CategoriasGeracaoJogos = ObterListaCategoriasGeracaoJogos(torneioId, classes, classesGeradas, classesFaseGrupoNaoFinalizadas, classesGeradasMataMata);

            ViewBag.ClassesGeradas = classesGeradas;
            ViewBag.Classes = classes;
            ViewBag.fClasse = fClasse;
            ViewBag.fData = fData;
            ViewBag.fNomeJogador = fNomeJogador;
            ViewBag.fGrupo = fGrupo;
            ViewBag.fase = fase;

            CarregarDadosEssenciais(torneioId, "jogos");
            return View(listaJogos);
        }

        private List<CategoriaGeracaoJogoModel> ObterListaCategoriasGeracaoJogos(int torneioId, List<ClasseTorneio> categorias, List<int> classesGeradas, List<int> classesFaseGrupoNaoFinalizadas, List<int> classesGeradasMataMata)
        {
            var categoriasGeracaoJogos = new List<CategoriaGeracaoJogoModel>();

            var torneio = db.Torneio.Find(torneioId);

            foreach (var categoriaTorneio in categorias)
            {
                var catItem = new CategoriaGeracaoJogoModel();
                catItem.Id = categoriaTorneio.Id;
                catItem.Nome = categoriaTorneio.nome;
                if (categoriaTorneio.faseGrupo || torneio.barragem.isModeloTodosContraTodos)
                {
                    catItem.Tipo = "FG";
                }
                else if (!categoriaTorneio.faseGrupo && categoriaTorneio.faseMataMata)
                {
                    catItem.Tipo = "MM";
                }

                var jogosGerados = false;
                if (classesGeradas.Contains(categoriaTorneio.Id))
                {
                    jogosGerados = true;
                    if (categoriaTorneio.faseGrupo && categoriaTorneio.faseMataMata && !classesFaseGrupoNaoFinalizadas.Contains(categoriaTorneio.Id))
                    {
                        catItem.Tipo = "MM";
                        if (!classesGeradasMataMata.Contains(categoriaTorneio.Id))
                        {
                            jogosGerados = false;
                        }
                    }
                }
                catItem.JogosJaGerados = jogosGerados;

                categoriasGeracaoJogos.Add(catItem);
            }
            return categoriasGeracaoJogos;
        }

        [Authorize(Roles = "admin,organizador,adminTorneio,adminTorneioTenis,parceiroBT")]
        [HttpPost]
        public ActionResult EditJogos(int Id, int jogador1, int jogador2, string dataJogo = "", string horaJogo = "", string quadra = "0")
        {
            try
            {
                bool alterarJogosFaseGrupo = false;
                bool alterarJogosMataMata = false;

                //Dados jogador antigo
                int substituirEsteDesafiante = 0;
                int substituirEsteDesafiado = 0;
                int? substituirEsteDesafiante2 = null;
                int? substituirEsteDesafiado2 = null;

                //Dados novo jogador
                var porEsteDesafiante = 0;
                var porEsteDesafiado = 0;
                int? porEsteDesafiante2 = null;
                int? porEsteDesafiado2 = null;

                int? grupoPorEsteDesafiante = 0;
                int? grupoPorEsteDesafiado = 0;

                var jogo = db.Jogo.Find(Id);

                substituirEsteDesafiante2 = jogo.desafiante2_id;
                substituirEsteDesafiado2 = jogo.desafiado2_id;

                if (jogo?.grupoFaseGrupo > 0 && (jogo.desafiante_id != jogador1 || jogo.desafiado_id != jogador2))
                {
                    //Troca de jogador durante a fase de grupos

                    alterarJogosFaseGrupo = true;

                    if (jogo.desafiante_id != jogador1)
                    {
                        // não permitir trocar se o jogador já pertence a este grupo
                        var inscricaoNovoJogador = ObterInscricaoJogador(jogador1, jogo.classeTorneio);
                        grupoPorEsteDesafiante = inscricaoNovoJogador?.grupo;

                        var resultadoValidacao = ValidarParticipanteAlocadoMesmoGrupo(inscricaoNovoJogador, jogo.grupoFaseGrupo);
                        if (resultadoValidacao.retorno == 0)
                            return Json(resultadoValidacao, "text/plain", JsonRequestBehavior.AllowGet);

                        substituirEsteDesafiante = (int)jogo.desafiante_id;
                        porEsteDesafiante = jogador1;

                        var podeEfetuarTroca = ValidarTrocaDeJogadorPermitida(jogo.torneioId.Value, jogo.classeTorneio.Value, jogo.grupoFaseGrupo, inscricaoNovoJogador?.grupo, substituirEsteDesafiante, porEsteDesafiante);
                        if (podeEfetuarTroca.retorno == 0)
                            return Json(podeEfetuarTroca, "text/plain", JsonRequestBehavior.AllowGet);

                        if (substituirEsteDesafiante == Constantes.Jogo.BYE)
                        {
                            jogo.AlterarJogoParaPendente();
                        }

                        var resultadoValidacaoJogo = ValidarExistenciaJogosFinalizados(substituirEsteDesafiante, jogo.ObterNomeJogador(TipoJogador.DESAFIANTE), jogo.torneioId.Value, jogo.classeTorneio.Value, false, false);
                        if (resultadoValidacaoJogo.retorno == 0)
                            return Json(resultadoValidacaoJogo, "text/plain", JsonRequestBehavior.AllowGet);

                        resultadoValidacaoJogo = ValidarExistenciaJogosFinalizados(porEsteDesafiante, inscricaoNovoJogador?.participante.nome, jogo.torneioId.Value, jogo.classeTorneio.Value, false, true);
                        if (resultadoValidacaoJogo.retorno == 0)
                            return Json(resultadoValidacaoJogo, "text/plain", JsonRequestBehavior.AllowGet);

                    }

                    if (jogo.desafiado_id != jogador2)
                    {
                        var inscricaoNovoJogador = ObterInscricaoJogador(jogador2, jogo.classeTorneio);
                        grupoPorEsteDesafiado = inscricaoNovoJogador?.grupo;
                        var resultadoValidacao = ValidarParticipanteAlocadoMesmoGrupo(inscricaoNovoJogador, jogo.grupoFaseGrupo);
                        if (resultadoValidacao.retorno == 0)
                            return Json(resultadoValidacao, "text/plain", JsonRequestBehavior.AllowGet);

                        substituirEsteDesafiado = (int)jogo.desafiado_id;
                        porEsteDesafiado = jogador2;

                        var grupoId = inscricaoNovoJogador == null ? jogo.grupoFaseGrupo : inscricaoNovoJogador.grupo;

                        var podeEfetuarTroca = ValidarTrocaDeJogadorPermitida(jogo.torneioId.Value, jogo.classeTorneio.Value, jogo.grupoFaseGrupo, inscricaoNovoJogador?.grupo, substituirEsteDesafiado, porEsteDesafiado);
                        if (podeEfetuarTroca.retorno == 0)
                            return Json(podeEfetuarTroca, "text/plain", JsonRequestBehavior.AllowGet);

                        var resultadoValidacaoJogo = ValidarExistenciaJogosFinalizados(substituirEsteDesafiado, jogo.ObterNomeJogador(TipoJogador.DESAFIADO), jogo.torneioId.Value, jogo.classeTorneio.Value, false, false);
                        if (resultadoValidacaoJogo.retorno == 0)
                            return Json(resultadoValidacaoJogo, "text/plain", JsonRequestBehavior.AllowGet);

                        resultadoValidacaoJogo = ValidarExistenciaJogosFinalizados(porEsteDesafiado, inscricaoNovoJogador?.participante.nome, jogo.torneioId.Value, jogo.classeTorneio.Value, false, true);
                        if (resultadoValidacaoJogo.retorno == 0)
                            return Json(resultadoValidacaoJogo, "text/plain", JsonRequestBehavior.AllowGet);

                    }

                    if (porEsteDesafiante == Constantes.Jogo.BYE || porEsteDesafiado == Constantes.Jogo.BYE)
                    {
                        //Troca de jogador por BYE
                        var resultadoValidacao = VerificarSeJaTemByeNoGrupo((int)jogo.classeTorneio, (int)jogo.grupoFaseGrupo);
                        if (resultadoValidacao.retorno == 0)
                            return Json(resultadoValidacao, "text/plain", JsonRequestBehavior.AllowGet);

                        jogo.AlterarJogoParaBye(porEsteDesafiante == Constantes.Jogo.BYE ? TipoJogador.DESAFIANTE : TipoJogador.DESAFIADO);
                    }

                }
                else if (jogo.grupoFaseGrupo == null && (jogo.desafiante_id != jogador1 || jogo.desafiado_id != jogador2))
                {
                    //MATA-MATA
                    alterarJogosMataMata = true;

                    if (jogo.desafiante_id != jogador1)
                    {
                        substituirEsteDesafiante = jogo.desafiante_id;
                        porEsteDesafiante = jogador1;

                        if (substituirEsteDesafiante == Constantes.Jogo.BYE)
                        {
                            jogo.AlterarJogoParaPendente();
                        }

                        var inscricaoNovoJogador = ObterInscricaoJogador(porEsteDesafiante, jogo.classeTorneio);

                        var resultadoValidacaoJogo = ValidarExistenciaJogosFinalizados(substituirEsteDesafiante, jogo.ObterNomeJogador(TipoJogador.DESAFIANTE), jogo.torneioId.Value, jogo.classeTorneio.Value, true, false);
                        if (resultadoValidacaoJogo.retorno == 0)
                            return Json(resultadoValidacaoJogo, "text/plain", JsonRequestBehavior.AllowGet);

                        resultadoValidacaoJogo = ValidarExistenciaJogosFinalizados(porEsteDesafiante, inscricaoNovoJogador?.participante.nome, jogo.torneioId.Value, jogo.classeTorneio.Value, true, true);
                        if (resultadoValidacaoJogo.retorno == 0)
                            return Json(resultadoValidacaoJogo, "text/plain", JsonRequestBehavior.AllowGet);

                        var resultadoValidacaoTrocaMataMata = ValidarTrocaDeJogadorPermitidaMataMata(jogo.torneioId.Value, jogo.classeTorneio.Value, jogo.Id, jogo.faseTorneio.Value, jogo.desafiante_id, porEsteDesafiante);
                        if (resultadoValidacaoTrocaMataMata.retorno == 0)
                            return Json(resultadoValidacaoTrocaMataMata, "text/plain", JsonRequestBehavior.AllowGet);
                    }

                    if (jogo.desafiado_id != jogador2)
                    {
                        substituirEsteDesafiado = jogo.desafiado_id;
                        porEsteDesafiado = jogador2;

                        var inscricaoNovoJogador = ObterInscricaoJogador(porEsteDesafiante, jogo.classeTorneio);

                        var resultadoValidacaoJogo = ValidarExistenciaJogosFinalizados(substituirEsteDesafiado, jogo.ObterNomeJogador(TipoJogador.DESAFIADO), jogo.torneioId.Value, jogo.classeTorneio.Value, true, false);
                        if (resultadoValidacaoJogo.retorno == 0)
                            return Json(resultadoValidacaoJogo, "text/plain", JsonRequestBehavior.AllowGet);

                        resultadoValidacaoJogo = ValidarExistenciaJogosFinalizados(porEsteDesafiado, inscricaoNovoJogador?.participante.nome, jogo.torneioId.Value, jogo.classeTorneio.Value, true, true);
                        if (resultadoValidacaoJogo.retorno == 0)
                            return Json(resultadoValidacaoJogo, "text/plain", JsonRequestBehavior.AllowGet);
                    }

                    if (porEsteDesafiante == Constantes.Jogo.BYE || porEsteDesafiado == Constantes.Jogo.BYE)
                    {
                        jogo.AlterarJogoParaBye(porEsteDesafiante == Constantes.Jogo.BYE ? TipoJogador.DESAFIANTE : TipoJogador.DESAFIADO);
                    }
                }

                if (jogo.classe.isDupla)
                {
                    porEsteDesafiante2 = ObterIdentificadorDuplaJogador(jogador1, jogo.classeTorneio);
                    porEsteDesafiado2 = ObterIdentificadorDuplaJogador(jogador2, jogo.classeTorneio);
                }

                //Efetua a troca dos jogadores do jogo
                jogo.AlterarJogador(TipoJogador.DESAFIANTE, jogador1, porEsteDesafiante2);
                jogo.AlterarJogador(TipoJogador.DESAFIADO, jogador2, porEsteDesafiado2);

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

                var userIdLog = WebSecurity.GetUserId(User.Identity.Name);

                var msg = $"ALTERACAO_JOGADOR (A) {DateTimeHelper.GetDateTimeBrasilia()} - ORGANIZADOR: ID:{userIdLog} NOME: {User.Identity.Name} AÇÃO REALIZADA: DESAFIANTE: [{substituirEsteDesafiante} => {porEsteDesafiante}] DESAFIADO: [{substituirEsteDesafiado} => {porEsteDesafiado}]";
                GravarLogErro(msg);

                if (jogo.desafiante_id == Constantes.Jogo.BYE)
                {
                    tn.MontarProximoJogoTorneio(jogo);
                }

                if (alterarJogosFaseGrupo)
                {
                    //substitui o jogador nos jogos do grupo
                    substituirJogadorFaseGrupo((int)jogo.grupoFaseGrupo, (int)jogo.classeTorneio, substituirEsteDesafiante, porEsteDesafiante, porEsteDesafiante2 ?? 0);
                    substituirJogadorFaseGrupo((int)jogo.grupoFaseGrupo, (int)jogo.classeTorneio, substituirEsteDesafiado, porEsteDesafiado, porEsteDesafiado2 ?? 0);

                    //Em caso de ter sido efetuada a troca de jogador por outro que estava em outro grupo
                    if ((grupoPorEsteDesafiante > 0 && grupoPorEsteDesafiante != jogo.grupoFaseGrupo) || (grupoPorEsteDesafiado > 0 && grupoPorEsteDesafiado != jogo.grupoFaseGrupo))
                    {
                        //Os jogos antigos do novo jogador serão alterados para o jogador de origem
                        substituirJogadorFaseGrupo(grupoPorEsteDesafiante.HasValue ? grupoPorEsteDesafiante.Value : 0, (int)jogo.classeTorneio, porEsteDesafiante, substituirEsteDesafiante, substituirEsteDesafiante2 ?? 0);
                        substituirJogadorFaseGrupo(grupoPorEsteDesafiado.HasValue ? grupoPorEsteDesafiado.Value : 0, (int)jogo.classeTorneio, porEsteDesafiado, substituirEsteDesafiado, substituirEsteDesafiado2 ?? 0);
                    }

                    return Json(new { erro = "", retorno = 2 }, "text/plain", JsonRequestBehavior.AllowGet);
                }
                else if (alterarJogosMataMata)
                {
                    //Atualiza próximo jogo mata-mata conforme jogo recém atualizado
                    tn.MontarProximoJogoTorneio(jogo);

                    //Atualiza demais jogos
                    SubstituirJogadorMataMata(jogo.Id, jogo.faseTorneio.Value, (int)jogo.classeTorneio, porEsteDesafiante, substituirEsteDesafiante, substituirEsteDesafiante2 ?? 0);
                    SubstituirJogadorMataMata(jogo.Id, jogo.faseTorneio.Value, (int)jogo.classeTorneio, porEsteDesafiado, substituirEsteDesafiado, substituirEsteDesafiado2 ?? 0);

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

        private ResponseMessage ValidarParticipanteAlocadoMesmoGrupo(InscricaoTorneio inscricao, int? idGrupo)
        {
            if (inscricao?.grupo == idGrupo)
            {
                return new ResponseMessage { erro = "O jogador: " + inscricao.participante.nome + " já está em outros jogos neste grupo. Não é possível acrescentá-lo em mais jogos.", retorno = 0 };
            }
            return new ResponseMessage { retorno = 1 };
        }

        private ResponseMessage ValidarExistenciaJogosFinalizados(int userId, string nomeParticipante, int torneioId, int classeId, bool ehMataMata, bool ehJogadorNovo)
        {
            var possuiJogosFinalizados = db.Jogo.Any(j => (j.desafiado_id == userId || j.desafiante_id == userId) && j.desafiante_id != 10
                       && (j.situacao_Id == 5 || j.situacao_Id == 4 || j.situacao_Id == 6)
                       && ((j.grupoFaseGrupo == null && ehMataMata) || (j.grupoFaseGrupo != null && !ehMataMata))
                       && j.torneioId == torneioId && j.classeTorneio == classeId);

            if (possuiJogosFinalizados)
            {
                if (ehJogadorNovo)
                    return new ResponseMessage { erro = $"O novo jogador informado: {nomeParticipante} já possui jogos finalizados. Não é permitido substituí-lo.", retorno = 0 };
                else
                    return new ResponseMessage { erro = $"O jogador: {nomeParticipante} já possui jogos finalizados. Não é possível alterá-lo.", retorno = 0 };
            }
            return new ResponseMessage { retorno = 1 };
        }

        private ResponseMessage ValidarTrocaDeJogadorPermitida(int torneioId, int classeId, int? grupoIdJogo, int? grupoIdInscrito, int substituirEste, int porEste)
        {
            if (grupoIdJogo == grupoIdInscrito || grupoIdInscrito == null)
            {
                //validar a qtde de inscritos somente se o grupo do jogo for diferente do grupo do "porEste" e caso o grupo for nulo (no caso de não ter encontrado o inscrito pq é bye tbm não validar..
                return new ResponseMessage { retorno = 1 };
            }

            int grupo = grupoIdInscrito.HasValue ? grupoIdInscrito.Value : grupoIdJogo.Value;

            int qtdeInscricoesGrupo = db.InscricaoTorneio.Count(x => x.isAtivo && x.torneioId == torneioId && x.classe == classeId && x.grupo == grupo);

            if (qtdeInscricoesGrupo % 3 == 0)
            {
                if (substituirEste == Constantes.Jogo.BYE || porEste == Constantes.Jogo.BYE)
                {
                    return new ResponseMessage { erro = "Não é possível realizar a troca de jogador de um jogo bye com apenas 3 inscritos no grupo.", retorno = 0 };
                }
            }
            return new ResponseMessage { retorno = 1 };
        }

        private ResponseMessage ValidarTrocaDeJogadorPermitidaMataMata(int torneioId, int classeId, int jogoId, int faseTorneio, int desafiante, int porEsteDesafiante)
        {
            if (desafiante == Constantes.Jogo.BYE)
            {
                //Se esta ocorrendo a troca de desafiante bye p/ jogador, verificar se o jogador já tem um jogo vs bye
                bool jaPossuiJogosVsBye = db.Jogo.Any(x => x.Id != jogoId && x.desafiado_id == porEsteDesafiante && x.desafiante_id == Constantes.Jogo.BYE && x.grupoFaseGrupo == null && x.classeTorneio == classeId && x.torneioId == torneioId && x.faseTorneio == faseTorneio);
                if (jaPossuiJogosVsBye)
                {
                    return new ResponseMessage { erro = "Não é possível realizar a troca de jogador pois esta situação gera um jogo bye vs bye.", retorno = 0 };
                }
            }
            return new ResponseMessage { retorno = 1 };
        }

        public InscricaoTorneio ObterInscricaoJogador(int idJogador, int? idClasseTorneio)
        {
            return db.InscricaoTorneio.FirstOrDefault(i => i.userId == idJogador && i.classe == idClasseTorneio);
        }

        private int? ObterIdentificadorDuplaJogador(int idJogador, int? idClasseTorneio)
        {
            var dupla = db.InscricaoTorneio.FirstOrDefault(i => i.userId == idJogador && i.classe == idClasseTorneio && i.parceiroDuplaId != null);
            if (dupla != null)
            {
                return (int)dupla.parceiroDuplaId;
            }
            else
            {
                return null;
            }
        }

        public JsonResult ObterJogadores(int torneioId, int classeId = 0)
        {
            List<InscricaoTorneio> inscritos;
            var dadosRetorno = new ListaOpcoesJogadoresModel();
            bool classeMataMata = false;
            bool classeFaseGrupo = false;
            bool todosContraTodos = false;


            var torneio = db.Torneio.Find(torneioId);
            var cl = db.ClasseTorneio.FirstOrDefault(x => x.torneioId == torneioId && x.Id == classeId);

            if (torneio != null)
            {
                todosContraTodos = torneio.barragem.isModeloTodosContraTodos;
            }

            if (cl == null)
            {
                inscritos = new List<InscricaoTorneio>();
            }
            else if (cl.isDupla)
            {
                inscritos = db.InscricaoTorneio.Where(c => c.torneioId == torneioId && c.isAtivo && c.parceiroDuplaId != null && c.classe == classeId).ToList();
                classeMataMata = cl.faseMataMata;
                classeFaseGrupo = cl.faseGrupo;
            }
            else
            {
                inscritos = db.InscricaoTorneio.Where(c => c.torneioId == torneioId && c.isAtivo && c.classe == classeId).ToList();
                classeMataMata = cl.faseMataMata;
                classeFaseGrupo = cl.faseGrupo;
            }

            var jogosClasseTorneio = db.Jogo.Where(x => x.classeTorneio == classeId);

            #region Jogadores Fora da Tabela
            var listaForaTabela = new List<AutoCompleteOption>();
            foreach (var inscrito in inscritos.OrderBy(x => x.participante.nome))
            {
                if (!jogosClasseTorneio.Any(x => x.desafiado_id == inscrito.userId || x.desafiante_id == inscrito.userId))
                {
                    listaForaTabela.Add(new AutoCompleteOption("FORA DA TABELA", inscrito.participante.nome, inscrito.userId.ToString()));
                }
            }
            #endregion Jogadores Fora da Tabela

            bool faseGrupoSeguidoMataMata = classeFaseGrupo && classeMataMata;

            var listaFG = new List<AutoCompleteOption>();
            var listaMM = new List<AutoCompleteOption>();

            if (classeMataMata)
            {
                listaMM.Add(new AutoCompleteOption("  ", "Aguardando adversário", Constantes.Jogo.AGUARDANDO_JOGADOR.ToString()));
                listaMM.Add(new AutoCompleteOption("  ", "bye", Constantes.Jogo.BYE.ToString()));

                foreach (var inscrito in inscritos.OrderBy(x => x.participante.nome))
                {
                    AutoCompleteOption itemInscrito;

                    if (jogosClasseTorneio.Any(x => (x.desafiado_id == inscrito.userId || x.desafiante_id == inscrito.userId) && x.grupoFaseGrupo == null))
                    {
                        var grupo = "NA TABELA";

                        if (cl.isDupla)
                        {
                            itemInscrito = new AutoCompleteOption(grupo, $"{inscrito.participante.nome} / {inscrito.parceiroDupla.nome}", inscrito.userId.ToString());
                        }
                        else
                        {
                            itemInscrito = new AutoCompleteOption(grupo, inscrito.participante.nome, inscrito.userId.ToString());
                        }

                        itemInscrito.Grupo = inscrito.grupo;
                        itemInscrito.JogadorAlocado = false;
                        listaMM.Add(itemInscrito);
                    }
                }
                if (!faseGrupoSeguidoMataMata)
                {
                    listaMM.AddRange(listaForaTabela);
                }
            }

            if (classeFaseGrupo || todosContraTodos)
            {
                listaFG.Add(new AutoCompleteOption("  ", "Aguardando adversário", Constantes.Jogo.AGUARDANDO_JOGADOR.ToString()));
                listaFG.Add(new AutoCompleteOption("  ", "bye", Constantes.Jogo.BYE.ToString()));

                foreach (var inscrito in inscritos.OrderBy(x => x.grupo).ThenBy(x => x.participante.nome))
                {
                    AutoCompleteOption itemInscrito;
                    var jogadorAlocadoNoGrupo = jogosClasseTorneio.Any(x => (x.desafiante_id == inscrito.userId || x.desafiado_id == inscrito.userId) && x.grupoFaseGrupo == inscrito.grupo);

                    if (jogosClasseTorneio.Any(x => x.desafiado_id == inscrito.userId || x.desafiante_id == inscrito.userId))
                    {
                        var grupo = (inscrito.grupo != null && inscrito.grupo > 0) ? $"GRUPO {inscrito.grupo}" : "NA TABELA";

                        if (cl.isDupla)
                        {
                            itemInscrito = new AutoCompleteOption(grupo, $"{inscrito.participante.nome} / {inscrito.parceiroDupla.nome}", inscrito.userId.ToString());
                        }
                        else
                        {
                            itemInscrito = new AutoCompleteOption(grupo, inscrito.participante.nome, inscrito.userId.ToString());
                        }
                        itemInscrito.Grupo = inscrito.grupo;
                        itemInscrito.JogadorAlocado = jogadorAlocadoNoGrupo;
                        listaFG.Add(itemInscrito);
                    }
                }

                listaFG.AddRange(listaForaTabela);

                #region Regra Grupo Unico
                var inscritosComGrupo = inscritos.Where(x => x.grupo != null).GroupBy(g => g.grupo);

                if (inscritosComGrupo.Count() == 1)
                {
                    dadosRetorno.EhGrupoUnico = true;
                }
                #endregion Regra Grupo Unico
            }
            dadosRetorno.FaseGrupoSeguidoMataMata = faseGrupoSeguidoMataMata;
            dadosRetorno.OpcoesJogador = listaFG;
            dadosRetorno.OpcoesJogadorMataMata = listaMM;
            dadosRetorno.Jogos = jogosClasseTorneio.Select(s => new ListaOpcoesJogadoresModel.DadosJogosModel { JogoId = s.Id, IdDesafiado = s.desafiado_id, IdDesafiante = s.desafiante_id, Grupo = s.grupoFaseGrupo }).ToList();

            return Json(dadosRetorno, JsonRequestBehavior.AllowGet);
        }

        private static void RemoverJogadorDaListagemJogadores(string tipojogador, List<InscricaoTorneio> inscritos, Jogo jogo, int idJogador)
        {
            if (tipojogador == "desafiante")
            {
                var inscritoRemover = inscritos.FirstOrDefault(x => x.userId == jogo.desafiado_id && x.userId != idJogador);
                if (inscritoRemover != null)
                {
                    inscritos.Remove(inscritoRemover);
                }
            }
            else
            {
                var inscritoRemover = inscritos.FirstOrDefault(x => x.userId == jogo.desafiante_id && x.userId != idJogador);
                if (inscritoRemover != null)
                {
                    inscritos.Remove(inscritoRemover);
                }
            }
        }

        private int ObterIdJogador(string tipojogador, List<InscricaoTorneio> inscritos, Jogo jogo)
        {
            if (tipojogador == "desafiante")
            {
                return inscritos.FirstOrDefault(x => x.userId == jogo.desafiante_id)?.userId ?? 0;
            }
            else
            {
                return inscritos.FirstOrDefault(x => x.userId == jogo.desafiado_id)?.userId ?? 0;
            }
        }

        private List<CategoriaValidarQtdeJogadores> ObterCategoriasValidarQtdeJogadores(int torneioId)
        {
            int qtdeInscricoes = 0;
            var categorias = new List<CategoriaValidarQtdeJogadores>();
            var categoriasTorneio = db.ClasseTorneio.Where(x => x.torneioId == torneioId).ToList();
            foreach (var categoria in categoriasTorneio)
            {
                if (categoria.faseGrupo && categoria.faseMataMata)
                {
                    var classesComJogosGerados = ObterClassesJogosJaGeradosEFinalizados(torneioId, new List<int> { { categoria.Id } }.ToArray(), categoriasTorneio);
                    if (classesComJogosGerados.Count > 0)
                        continue;

                    qtdeInscricoes = ObterQuantidadeInscritosCategoria(torneioId, categoria);

                    if (qtdeInscricoes > 0 && qtdeInscricoes <= 5)
                    {
                        categorias.Add(new CategoriaValidarQtdeJogadores()
                        {
                            IdClasse = categoria.Id,
                            IdTorneio = torneioId,
                            NomeClasse = categoria.nome,
                            QtdeInscricoes = qtdeInscricoes,
                            Tipo = TipoValidacaoCategoria.MENOS_DE_SEIS_JOGADORES
                        });
                    }
                }
                else if (categoria.faseGrupo)
                {
                    var classesComJogosGerados = ObterClassesJogosJaGeradosEFinalizados(torneioId, new List<int> { { categoria.Id } }.ToArray(), categoriasTorneio);
                    if (classesComJogosGerados.Count > 0)
                        continue;

                    qtdeInscricoes = ObterQuantidadeInscritosCategoria(torneioId, categoria);

                    if (qtdeInscricoes > 5)
                    {
                        categorias.Add(new CategoriaValidarQtdeJogadores()
                        {
                            IdClasse = categoria.Id,
                            IdTorneio = torneioId,
                            NomeClasse = categoria.nome,
                            QtdeInscricoes = qtdeInscricoes,
                            Tipo = TipoValidacaoCategoria.MAIS_DE_CINCO_JOGADORES
                        });
                    }
                }
            }
            return categorias;
        }

        private List<CategoriaInscricaoNaoPagaModel> ObterCategoriasInscricoesNaoPagas(int torneioId)
        {
            var categoriasTorneio = db.ClasseTorneio.Where(x => x.torneioId == torneioId).ToList();
            var categoriasInscritosPendentePgto = new List<CategoriaInscricaoNaoPagaModel>();
            foreach (var categoria in categoriasTorneio)
            {
                var item = new CategoriaInscricaoNaoPagaModel()
                {
                    IdCategoria = categoria.Id,
                    NomeCategoria = categoria.nome,
                    EhDupla = categoria.isDupla
                };
                var listaInscricoesCategoria = db.InscricaoTorneio.Include(i => i.participante).Include(i => i.parceiroDupla).Where(x => x.torneioId == torneioId && x.classe == categoria.Id);
                var duplas = listaInscricoesCategoria.Where(x => x.isAtivo == false && x.parceiroDuplaId != null);

                if (categoria.isDupla)
                {
                    foreach (var dupla in duplas)
                    {
                        if (!dupla.isAtivo || listaInscricoesCategoria.Any(x => !x.isAtivo && x.userId == dupla.parceiroDuplaId))
                        {
                            item.Jogadores.Add($"{dupla.participante.nome} e {dupla.parceiroDupla.nome}");
                        }
                    }
                }
                else
                {
                    var inscricoes = listaInscricoesCategoria.Where(x => !x.isAtivo);
                    foreach (var inscrito in inscricoes)
                    {
                        item.Jogadores.Add($"{inscrito.participante.nome}");
                    }
                }
                if (item.Jogadores.Any())
                {
                    categoriasInscritosPendentePgto.Add(item);
                }
            }
            return categoriasInscritosPendentePgto.OrderBy(o => o.NomeCategoria).ToList();
        }

        private int ObterQuantidadeInscritosCategoria(int torneioId, ClasseTorneio categoria)
        {
            int qtdeInscricoes;
            if (categoria.isDupla)
            {
                qtdeInscricoes = db.InscricaoTorneio.Count(x => x.isAtivo && x.torneioId == torneioId && x.classe == categoria.Id && x.parceiroDuplaId != null);
            }
            else
            {
                qtdeInscricoes = db.InscricaoTorneio.Count(x => x.isAtivo && x.torneioId == torneioId && x.classe == categoria.Id);
            }

            return qtdeInscricoes;
        }

        private List<int> ObterDuplasJogosFormados(int idTorneio, int classeId, List<InscricaoTorneio> duplas)
        {
            var duplasJogos = new List<int>();
            var jogos = db.Jogo.Where(x => x.torneioId == idTorneio && (x.classeTorneio == classeId || classeId == 0));
            foreach (var dupla in duplas)
            {
                if (jogos.Any(x => x.desafiante_id == dupla.userId || x.desafiado_id == dupla.userId))
                {
                    duplasJogos.Add(dupla.userId);
                }
            }
            return duplasJogos;
        }

        private int ObterQtdeJogosPendentesFaseGrupo(int torneioId, int classeId)
        {
            return db.Jogo.Count(x => x.torneioId == torneioId && x.classeTorneio == classeId && x.grupoFaseGrupo != null && (x.situacao_Id == 1 || x.situacao_Id == 2));
        }

        private IEnumerable<Jogo> ObterJogosPendentesFaseGrupo(int torneioId, int classeId)
        {
            return db.Jogo.Where(x => x.torneioId == torneioId && x.classeTorneio == classeId && x.grupoFaseGrupo != null && (x.situacao_Id == 1 || x.situacao_Id == 2));
        }

        private void VerificarNecessidadeAtualizarMataMata(int atualizarJogosMataMata, int gerarJogosMataMata, Jogo jogo)
        {
            if (atualizarJogosMataMata == 1)
            {
                ResetarJogosMataMataPorClasse(jogo.classeTorneio.Value, true);
                MontarChaveamento(jogo.torneioId.Value, new int[] { jogo.classeTorneio.Value });
            }
            else if (gerarJogosMataMata == 1)
            {
                MontarChaveamento(jogo.torneioId.Value, new int[] { jogo.classeTorneio.Value });
            }
        }

        private int ValidaConsolidacaoLiga(Jogo jogo)
        {
            var ligaConsolidadaComSucesso = 0;
            if (jogo.faseTorneio == 1)
            {
                var liga = db.TorneioLiga.Where(t => t.TorneioId == jogo.torneioId).FirstOrDefault();
                if (liga != null)
                {
                    var snapshot = db.Snapshot.Count(s => s.LigaId == liga.LigaId && s.Id == liga.snapshotId);
                    if (snapshot > 0)
                    {
                        ligaConsolidadaComSucesso = 1;
                    }
                }
            }
            return ligaConsolidadaComSucesso;
        }
    }
}