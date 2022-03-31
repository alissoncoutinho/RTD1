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
                case "100":
                    return new Torneio100();
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
            //verifica se todas as finais foram lançadas
            List<Jogo> finaisDoTorneio = db.Jogo.Where(j => j.torneioId == jogo.torneioId && j.faseTorneio == 1).ToList();
            foreach(Jogo finalDeClasse in finaisDoTorneio)
            {
                if (!(finalDeClasse.situacao_Id==4 || finalDeClasse.situacao_Id == 5 || finalDeClasse.situacao_Id == 6))
                {
                    //não gera novo ranking enquanto nao finalizar todas as classes do torneio
                    return;
                }
            }
            List<TorneioLiga> ligasDoTorneio = db.TorneioLiga.Include(tl => tl.Liga).Where(tl => tl.TorneioId == jogo.torneioId).ToList();
            foreach(TorneioLiga tl in ligasDoTorneio)
            {
                Snapshot ultimoSnap;
                try
                {
                    ultimoSnap = db.Snapshot.Where(s => s.LigaId == tl.LigaId).OrderByDescending(s => s.Id).ToList().First();
                    if ((tl.snapshotId != null) && (tl.snapshotId != ultimoSnap.Id)){
                        continue;
                    }else if ((tl.snapshotId != null) && (tl.snapshotId == ultimoSnap.Id)){
                        db.Database.ExecuteSqlCommand("delete from SnapshotRanking where SnapshotId=" + ultimoSnap.Id);
                        db.Database.ExecuteSqlCommand("delete from Snapshot where Id=" + ultimoSnap.Id);
                        ultimoSnap = db.Snapshot.Where(s => s.LigaId == tl.LigaId).OrderByDescending(s => s.Id).ToList().First();
                    }
                }
                catch (Exception e)
                {
                    ultimoSnap = null;
                }
                //para cada liga, gerar um snapshot
                Liga liga = tl.Liga;
                DateTime dateTime = DateTime.UtcNow;
                TimeZoneInfo horaBrasilia = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
                DateTime dateTimeBrasilia = TimeZoneInfo.ConvertTimeFromUtc(dateTime, horaBrasilia);
                Snapshot novoSnap = new Snapshot { Data = dateTimeBrasilia, LigaId = liga.Id};
                db.Snapshot.Add(novoSnap);
                db.SaveChanges();
                tl.snapshotId = novoSnap.Id;
                db.Entry(tl).State = EntityState.Modified;
                db.SaveChanges();
                //para cada categoria da liga, gerar um ranking
                List<Categoria> categoriasDaLiga = db.ClasseLiga.Include(cl => cl.Categoria).Where(cl => cl.LigaId == liga.Id).Select(u => u.Categoria).Distinct().ToList(); 
                foreach(Categoria categoriaDaLiga in categoriasDaLiga)
                {
                    //Categoria categoriaDaLiga = cl.Categoria;
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
                        novoResultado.Posicao = ultimoResultado.Posicao;
                        db.SnapshotRanking.Add(novoResultado);
                        db.SaveChanges();
                    }
                    //atualiza o ranking com os resultados do torneio
                    ClasseTorneio classeTorneio = null;
                    try
                    {
                        classeTorneio = db.ClasseTorneio
                            .Where(ct => ct.torneioId == jogo.torneioId && ct.categoriaId == categoriaDaLiga.Id)
                            .ToList().First();
                        // esta linha foi inserida para que não inclua classes que não houveram jogos: se não houveram jogos dará erro e cairá na exception para dar continue;
                        var jgs = db.Jogo
                            .Where(j => j.classeTorneio == classeTorneio.Id).First();

                    }
                    catch(Exception e)
                    {
                        continue;
                    }
                    List<InscricaoTorneio> resultadosDoTorneio = db.InscricaoTorneio
                        .Where(it => it.torneioId == jogo.torneioId && it.classe == classeTorneio.Id && it.isAtivo).ToList();
                    foreach(InscricaoTorneio resultado in resultadosDoTorneio)
                    {
                        try
                        {
                            SnapshotRanking ultimoRankingDoJogador = db.SnapshotRanking.Where(sr => sr.LigaId == liga.Id 
                                && sr.CategoriaId == categoriaDaLiga.Id 
                                && sr.UserId == resultado.userId
                                && sr.SnapshotId == novoSnap.Id).Single();
                            if (resultado.Pontuacao != null){
                                int pontuacaoAtualizada = ultimoRankingDoJogador.Pontuacao + resultado.Pontuacao ?? 0;
                                ultimoRankingDoJogador.Pontuacao = pontuacaoAtualizada;
                            }
                            db.SaveChanges();
                        }
                        catch (Exception e)
                        {
                            SnapshotRanking novoRanking = new SnapshotRanking();
                            novoRanking.SnapshotId = novoSnap.Id;
                            novoRanking.LigaId = liga.Id;
                            novoRanking.CategoriaId = categoriaDaLiga.Id;
                            novoRanking.UserId = resultado.userId;
                            novoRanking.Pontuacao = resultado.Pontuacao ?? 0;
                            db.SnapshotRanking.Add(novoRanking);
                            db.SaveChanges();
                        }
                    }
                    //coloca as posicoes do ranking
                    List<SnapshotRanking> rankingAtual = db.SnapshotRanking.Where(sr => sr.LigaId == liga.Id
                        && sr.CategoriaId == categoriaDaLiga.Id
                        && sr.SnapshotId == novoSnap.Id).OrderByDescending(sr => sr.Pontuacao).ToList();
                    int i = 1;
                    int pontuacaoAnterior = 0;
                    int posicaoAnterior = 0;
                    bool isPrimeiraVez = true;
                    foreach (SnapshotRanking ranking in rankingAtual)
                    {
                        if ((ranking.Pontuacao == pontuacaoAnterior)&&(!isPrimeiraVez)) {
                            ranking.Posicao = posicaoAnterior;
                        } else {
                            ranking.Posicao = i;
                            posicaoAnterior = i;
                        }
                        db.SaveChanges();
                        pontuacaoAnterior = ranking.Pontuacao;
                        if (isPrimeiraVez){
                            posicaoAnterior = i;
                            isPrimeiraVez = false;
                        }
                        i++;
                    }
                }
            }
        }
    }
}