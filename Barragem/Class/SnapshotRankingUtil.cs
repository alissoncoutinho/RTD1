using Barragem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Barragem.Class
{
    public static class SnapshotRankingUtil
    {
        public static void GerarPosicoesRanking(List<SnapshotRanking> snapshotRankings) 
        {
            int i = 1;
            int pontuacaoAnterior = 0;
            int posicaoAnterior = 0;
            bool isPrimeiraVez = true;
            foreach (SnapshotRanking ranking in snapshotRankings)
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
                pontuacaoAnterior = ranking.Pontuacao;
                if (isPrimeiraVez)
                {
                    posicaoAnterior = i;
                    isPrimeiraVez = false;
                }
                i++;
            }
        }
    }
}