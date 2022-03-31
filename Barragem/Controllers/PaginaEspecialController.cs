using Barragem.Class;
using Barragem.Context;
using Barragem.Helper;
using Barragem.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace Barragem.Controllers
{
    public class PaginaEspecialController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();
        private const string MSG_DOMINIO_NAO_ENCONTRADO = "Desculpe mas não encontramos um ranking com esse nome. Favor verificar se o nome do ranking foi digitado corretamente.";

        public ActionResult Index(int idBarragem, EnumPaginaEspecial idPaginaEspecial)
        {
            var barragem = BuscarBarragemPorId(idBarragem, idPaginaEspecial);
            var patrocinadores = BuscarPatrocinadores();
            var ranking = BuscarDadosRanking(idBarragem);
            var modalidadesCalendario = BuscarModalidadesCalendario();

            var model = new PaginaEspecialModel()
            {
                TipoPaginaEspecial = idPaginaEspecial,
                IdBarragem = barragem.Id,
                NomeBarragem = barragem.nome,
                Regulamento = barragem.regulamento,
                Contato = barragem.contato,
                Patrocinadores = patrocinadores,
                TituloFilieSeOuQuemSomos = idPaginaEspecial == EnumPaginaEspecial.Federacao ? "Filie-se" : "Quem Somos",
                TextoFilieSeOuQuemSomos = barragem.quemsomos,
                Rankings = ranking,
                ModalidadesCalendario = modalidadesCalendario
            };
            CarregarDropDownMesesAno();
            return View(model);
        }

        public PartialViewResult ObterCalendarioMensal(int mes, int idmodalidade)
        {
            return PartialView("_PartialCalendarioMes", ObterTorneios(mes, idmodalidade));
        }


        private PaginaEspecialModel.CalendarioTorneioMes ObterTorneios(int mes, int idmodalidade)
        {
            var dataInicialMes = new DateTime(DateTime.Now.Year, mes, 1);
            var dataFinalMes = new DateTime(DateTime.Now.Year, mes, DateTime.DaysInMonth(dataInicialMes.Year, dataInicialMes.Month));

            var calendarioTorneios = db.CalendarioTorneio
                .Include(i => i.ModalidadeTorneio)
                .Include(i => i.StatusInscricaoTorneio)
                .Where(x => x.DataInicial >= dataInicialMes && x.DataInicial <= dataFinalMes && x.ModalidadeTorneioId == idmodalidade)
                .Select(s => new PaginaEspecialModel.CalendarioTorneioItem()
                {
                    DataInicial = s.DataInicial,
                    DataFinal = s.DataFinal,
                    Local = s.Local,
                    Nome = s.Nome,
                    Pontuacao = s.Pontuacao,
                    StatusInscricaoTorneio = s.StatusInscricaoTorneio.Nome,
                    IdStatusInscricaoTorneio = s.StatusInscricaoTorneio.Id,
                    LinkInscricao = s.LinkInscricao
                }).OrderBy(o => o.DataInicial).ThenBy(o => o.DataFinal).ToList();

            return new PaginaEspecialModel.CalendarioTorneioMes() { Torneios = calendarioTorneios };
        }

        public ActionResult LigaRedirect(string key)
        {
            var barragem = BuscarBarragemPorDominio(key, EnumPaginaEspecial.Liga);
            if (barragem == null)
                return RedirectToAction("Index", "Home", new { msg = MSG_DOMINIO_NAO_ENCONTRADO });
            //Funcoes.CriarCookieBarragem(Response, Server, barragem.Id, barragem.nome);
            return RedirectToAction("Index", "PaginaEspecial", new { idBarragem = barragem.Id, idPaginaEspecial = (int)EnumPaginaEspecial.Liga });
        }

        public ActionResult CircuitoRedirect(string key)
        {
            var barragem = BuscarBarragemPorDominio(key, EnumPaginaEspecial.Circuito);
            if (barragem == null)
                return RedirectToAction("Index", "Home", new { msg = MSG_DOMINIO_NAO_ENCONTRADO });

            return RedirectToAction("Index", "PaginaEspecial", new { idBarragem = barragem.Id, idPaginaEspecial = (int)EnumPaginaEspecial.Circuito });
        }

        public ActionResult FederacaoRedirect(string key)
        {
            var barragem = BuscarBarragemPorDominio(key, EnumPaginaEspecial.Federacao);
            if (barragem == null)
                return RedirectToAction("Index", "Home", new { msg = MSG_DOMINIO_NAO_ENCONTRADO });

            return RedirectToAction("Index", "PaginaEspecial", new { idBarragem = barragem.Id, idPaginaEspecial = (int)EnumPaginaEspecial.Federacao });
        }

        private BarragemView BuscarBarragemPorDominio(string dominio, EnumPaginaEspecial idPaginaEspecial)
        {
            return db.BarragemView
                        .FirstOrDefault(b => b.dominio.Equals(dominio, System.StringComparison.OrdinalIgnoreCase) && b.PaginaEspecialId == (int)idPaginaEspecial);
        }

        private Barragens BuscarBarragemPorId(int id, EnumPaginaEspecial idPaginaEspecial)
        {
            return db.Barragens
                        .FirstOrDefault(b => b.Id == id && b.PaginaEspecialId == (int)idPaginaEspecial);
        }

        private void CarregarDropDownMesesAno()
        {
            List<dynamic> mesesAno = new List<dynamic>();
            var ano = DateTime.Now.Year;
            for (int i = 1; i <= 12; i++)
            {
                var mes = new DateTime(ano, i, 1);
                mesesAno.Add(new { Id = mes.Month, Nome = mes.GetMonthName() });
            }
            ViewBag.MesesAno = new SelectList(mesesAno, "Id", "Nome");
        }

        private List<Patrocinio> BuscarPatrocinadores()
        {
            return db.Patrocinio.ToList();
        }

        private List<PaginaEspecialModel.CalendarioModalidades> BuscarModalidadesCalendario()
        {
            return db.ModalidadeTorneio.Select(s => new PaginaEspecialModel.CalendarioModalidades() { IdModalidade = s.Id, Modalidade = s.Nome }).ToList();
        }

        private List<PaginaEspecialModel.RankingModel> BuscarDadosRanking(int idBarragem)
        {
            var rankings = new List<PaginaEspecialModel.RankingModel>();

            var ligas = db.Liga.Include(i => i.ModalidadeTorneio).Where(x => x.barragemId == idBarragem && x.isAtivo == true);

            foreach (var liga in ligas)
            {
                var itemRanking = new PaginaEspecialModel.RankingModel();

                var snapshotsDaLiga = db.Snapshot.Where(snap => snap.LigaId == liga.Id).OrderByDescending(s => s.Id).FirstOrDefault();
                if (snapshotsDaLiga != null)
                {
                    var ranking = db.SnapshotRanking.Where(snapR => snapR.SnapshotId == snapshotsDaLiga.Id)
                                                    .Include(s => s.Categoria)
                                                    .Include(s => s.Jogador)
                                                    .OrderBy(snap => snap.Categoria.Nome)
                                                    .ThenBy(snap => snap.Posicao)
                                                    .ThenBy(snap => snap.Jogador.nome)
                                                    .ToList();

                    var categorias = db.SnapshotRanking.Where(sr => sr.SnapshotId == snapshotsDaLiga.Id)
                                                        .Include(sr => sr.Categoria)
                                                        .Select(sr => sr.Categoria)
                                                        .OrderBy(sr => sr.ordemExibicao)
                                                        .Distinct()
                                                        .Select(s => new PaginaEspecialModel.RankingModel.RankingModalidade.CategoriaModalidade()
                                                        {
                                                            IdCategoria = s.Id,
                                                            NomeCategoria = s.Nome
                                                        })
                                                        .ToList();

                    foreach (var categoria in categorias)
                    {
                        categoria.Jogadores = ranking.Where(x => x.CategoriaId == categoria.IdCategoria).Select(s => new PaginaEspecialModel.RankingModel.RankingModalidade.CategoriaModalidade.Jogador()
                        {
                            NomeJogador = s.Jogador.nome,
                            Pontuacao = s.Pontuacao,
                            Posicao = s.Posicao
                        }).ToList();
                    }

                    itemRanking.DataRanking = snapshotsDaLiga.Data;
                    itemRanking.IdModalidade = liga.ModalidadeTorneio.Id;
                    itemRanking.Modalidade = liga.ModalidadeTorneio.Nome;
                    itemRanking.Ranking = new PaginaEspecialModel.RankingModel.RankingModalidade()
                    {
                        NomeLiga = liga.Nome,
                        Categoria = categorias
                    };
                    rankings.Add(itemRanking);
                }
            }

            foreach (var gruposPorModalidade in rankings.GroupBy(g => g.Modalidade))
            {
                if (gruposPorModalidade.Count() > 1)
                {
                    var menorData = gruposPorModalidade.Min(x => x.DataRanking);
                    PaginaEspecialModel.RankingModel rankingMaisAntigo = gruposPorModalidade.First(x => x.DataRanking == menorData);
                    rankings.Remove(rankingMaisAntigo);
                }
            }

            return rankings;
        }
    }
}