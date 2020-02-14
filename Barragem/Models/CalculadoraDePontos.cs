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

namespace Barragem.Models
{
    public class CalculadoraDePontos
    {
        private BarragemDbContext db = new BarragemDbContext();

        public static TipoTorneio AddTipoTorneio(String tipo)
        {
            switch (tipo)
            {
                case "250":
                    return new Torneio250();
                case "500":
                    return new Torneio500();
                case "1000":
                    return new Torneio1000();
                default:
                    return new TorneioGenerico();
            }
        }

        public void GerarSnapshotDaLiga(Jogo jogo)
        {
            //testa se é o jogo da final
            /*if (!"0".Equals(jogo.faseTorneio))
            {
                return;
            }*/
            List<TorneioLiga> ligasDoTorneio = db.TorneioLiga.Include(tl => tl.Liga).Where(tl => tl.TorneioId == jogo.torneioId).ToList();
            foreach(TorneioLiga tl in ligasDoTorneio)
            {
                Snapshot ultimoSnap;
                try
                {
                    ultimoSnap = db.Snapshot.Where(s => s.LigaId == tl.LigaId).OrderByDescending(s => s.Id).ToList().First();
                }
                catch (Exception e)
                {
                    ultimoSnap = null;
                }
                //para cada liga, gerar um snapshot
                Liga liga = tl.Liga;
                Snapshot novoSnap = new Snapshot { Data = DateTime.Now, LigaId = liga.Id};
                db.Snapshot.Add(novoSnap);
                db.SaveChanges();
                //para cada categoria da liga, gerar um ranking
                List<ClasseLiga> classesDaLiga = db.ClasseLiga.Include(cl => cl.Categoria).Where(cl => cl.LigaId == liga.Id).ToList();
                foreach(ClasseLiga cl in classesDaLiga)
                {
                    Categoria categoriaDaLiga = cl.Categoria;
                    //copia os resultados que ja existem para o novo ranking
                    List<SnapshotRanking> ultimoRankingDaCategoriaNaLiga = new List<SnapshotRanking>();
                    if (ultimoSnap != null)
                    {
                        ultimoRankingDaCategoriaNaLiga = db.SnapshotRanking
                            .Where(sr => sr.CategoriaId == categoriaDaLiga.Id 
                                && sr.LigaId == liga.Id && sr.SnapshotId == ultimoSnap.Id).ToList();
                    }
                    foreach(SnapshotRanking ultimoResultado in ultimoRankingDaCategoriaNaLiga)
                    {
                        SnapshotRanking novoResultado = new SnapshotRanking();
                        novoResultado.SnapshotId = novoSnap.Id;
                        novoResultado.LigaId = ultimoResultado.LigaId;
                        novoResultado.CategoriaId = ultimoResultado.CategoriaId;
                        novoResultado.UserId = ultimoResultado.UserId;
                        novoResultado.Pontuacao = ultimoResultado.Pontuacao;
                        db.SnapshotRanking.Add(novoResultado);
                        db.SaveChanges();
                    }
                    //atualiza o ranking com os resultados do torneio
                    ClasseTorneio classeTorneio = db.ClasseTorneio
                        .Where(ct => ct.torneioId == jogo.torneioId && ct.categoriaId == categoriaDaLiga.Id)
                        .ToList().First();
                    List<InscricaoTorneio> resultadosDoTorneio = db.InscricaoTorneio
                        .Where(it => it.torneioId == jogo.torneioId && it.classe == classeTorneio.Id).ToList();
                    foreach(InscricaoTorneio resultado in resultadosDoTorneio)
                    {
                        try
                        {
                            SnapshotRanking ultimoRankingDoJogador = db.SnapshotRanking.Where(sr => sr.LigaId == liga.Id 
                                && sr.CategoriaId == categoriaDaLiga.Id 
                                && sr.UserId == resultado.userId
                                && sr.SnapshotId == novoSnap.Id).Single();
                            int pontuacaoAtualizada = ultimoRankingDoJogador.Pontuacao + resultado.Pontuacao;
                            ultimoRankingDoJogador.Pontuacao = pontuacaoAtualizada;
                            db.SaveChanges();
                        }
                        catch (Exception e)
                        {
                            SnapshotRanking novoRanking = new SnapshotRanking();
                            novoRanking.SnapshotId = novoSnap.Id;
                            novoRanking.LigaId = liga.Id;
                            novoRanking.CategoriaId = categoriaDaLiga.Id;
                            novoRanking.UserId = resultado.userId;
                            novoRanking.Pontuacao = resultado.Pontuacao;
                            db.SnapshotRanking.Add(novoRanking);
                            db.SaveChanges();
                        }
                    }
                    //coloca as posicoes do ranking
                    List<SnapshotRanking> rankingAtual = db.SnapshotRanking.Where(sr => sr.LigaId == liga.Id
                        && sr.CategoriaId == categoriaDaLiga.Id
                        && sr.SnapshotId == novoSnap.Id).OrderByDescending(sr => sr.Pontuacao).ToList();
                    int i = 1;
                    foreach (SnapshotRanking ranking in rankingAtual)
                    {
                        ranking.Posicao = i;
                        db.SaveChanges();
                        i++;
                    }
                }
            }
        }
    }
}