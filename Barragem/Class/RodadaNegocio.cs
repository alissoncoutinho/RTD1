using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Barragem.Models;
using Barragem.Context;

namespace Barragem.Class
{
    public class RodadaNegocio
    {
        private BarragemDbContext db = new BarragemDbContext();
        public float calcularPontosDesafiante(Jogo jogo)
        {
            float pontuacao = 0;
            // se a quantidade de games jogados for igual a zero, quer dizer que não houve jogo e o jogador ficará com zero de pontuação nesta rodada
            if (jogo.gamesJogados != 0 && !jogo.desafiante.situacao.Equals("curinga"))
            {
                if (jogo.desafiado.situacao.Equals("curinga"))
                {
                    pontuacao = 6;
                }
                else if (jogo.idDoVencedor == jogo.desafiante_id && jogo.setsJogados == 2)
                {
                    pontuacao = 10;
                }
                else if ((jogo.idDoVencedor == jogo.desafiante_id) && (jogo.setsJogados == 3 || jogo.setsJogados == 1))
                {
                    pontuacao = 8;
                }
                else if (jogo.idDoVencedor != jogo.desafiante_id && jogo.setsJogados == 2)
                {
                    pontuacao = 2;
                }
                else if ((jogo.idDoVencedor != jogo.desafiante_id) && (jogo.setsJogados == 3 || jogo.setsJogados == 1))
                {
                    pontuacao = 4;
                }

                if ((jogo.idDoVencedor == jogo.desafiante_id) && (jogo.situacao_Id == 5))
                { // WO
                    pontuacao = 6;
                }
                if ((jogo.idDoVencedor != jogo.desafiante_id) && (jogo.situacao_Id == 5))
                { // WO
                    pontuacao = 0;
                }

                pontuacao = (float)Math.Round(pontuacao, 2);
            }
            return pontuacao + getBonus(jogo, true);
        }

        public float calcularPontosDesafiado(Jogo jogo)
        {
            float pontuacao = 0;
            // se a quantidade de games jogados for igual a zero, quer dizer que não houve jogo e o jogador ficará com zero de pontuação nesta rodada
            if (jogo.gamesJogados != 0 && !jogo.desafiado.situacao.Equals("curinga"))
            {
                if (jogo.desafiante.situacao.Equals("curinga"))
                {
                    pontuacao = 6;
                } else if (jogo.idDoVencedor == jogo.desafiado_id && jogo.setsJogados == 2)
                {
                    pontuacao = 10;
                }
                else if ((jogo.idDoVencedor == jogo.desafiado_id) && (jogo.setsJogados == 3 || jogo.setsJogados == 1))
                {
                    pontuacao = 8;
                }
                else if (jogo.idDoVencedor != jogo.desafiado_id && jogo.setsJogados == 2)
                {
                    pontuacao = 2;
                }
                else if ((jogo.idDoVencedor != jogo.desafiado_id) && (jogo.setsJogados == 3 || jogo.setsJogados == 1))
                {
                    pontuacao = 4;
                }

                if ((jogo.idDoVencedor == jogo.desafiado_id) && (jogo.situacao_Id == 5))
                { // WO
                    pontuacao = 6;
                }
                if ((jogo.idDoVencedor != jogo.desafiado_id) && (jogo.situacao_Id == 5))
                { // WO
                    pontuacao = 0;
                }

                pontuacao = (float)Math.Round(pontuacao, 2);
            }
            return pontuacao + getBonus(jogo, false);
        }

        private int getBonus(Jogo jogo, bool isDesafiado) {
            if((jogo.situacao_Id == 5) || (jogo.desafiante.situacao.Equals("curinga")) || (jogo.desafiado.situacao.Equals("curinga")))
            {
                return 0;
            }
            if (((jogo.qtddGames1setDesafiado + jogo.qtddGames2setDesafiado)<3) && isDesafiado) {
                return 3;
            }else if (((jogo.qtddGames1setDesafiante + jogo.qtddGames2setDesafiante) < 3) && !isDesafiado) {
                return 3;
            }else{
                return 0;
            }
        }

        public void gravarPontuacaoNaRodada(int idRodada, UserProfile jogador, double pontosConquistados, bool isReprocessamento = false)
        {
            try
            {
                if (jogador.situacao.Equals("curinga") || jogador.situacao.Equals("pendente"))
                {
                    return;
                }
                Rancking ran = null;
                double pontuacaoTotal = db.Rancking.Where(r => r.rodada.isAberta == false && r.userProfile_id == jogador.UserId && r.rodada_id < idRodada).
                    OrderByDescending(r => r.rodada_id).Take(9).Sum(r => r.pontuacao);

                if (isReprocessamento)
                {
                    ran = db.Rancking.Where(r => r.rodada_id == idRodada && r.userProfile_id == jogador.UserId).Single();
                    ran.pontuacao = Math.Round(pontosConquistados, 2);
                    //ran.totalAcumulado = Math.Round(pontuacaoTotal + pontosConquistados, 2);
                    db.SaveChanges();
                }
                else
                {
                    var naoExisteRanking = db.Rancking.Where(r => r.rodada_id == idRodada && r.userProfile_id == jogador.UserId).Count();
                    if (naoExisteRanking == 0)
                    {
                        ran = new Rancking();
                        ran.rodada_id = idRodada;
                        ran.pontuacao = Math.Round(pontosConquistados, 2);
                        ran.totalAcumulado = Math.Round(pontuacaoTotal + pontosConquistados, 2);
                        ran.posicao = 0;
                        ran.userProfile_id = jogador.UserId;
                        ran.classeId = jogador.classeId;
                        db.Rancking.Add(ran);
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                System.ArgumentException argEx = new System.ArgumentException("Jogador:" + jogador.UserId, "Jogador:" + jogador.UserId, e);
                throw argEx;
            }

        }

        public void gerarPontuacaoDosJogadoresForaDaRodada(int idRodada, int barragemId = 1)
        {
            try
            {
                string suspenso = Tipos.Situacao.suspenso.ToString();
                List<UserProfile> jogadores = db.UserProfiles.Where(j => j.barragemId == barragemId).ToList();
                foreach (UserProfile user in jogadores)
                {
                    int estaNaRodadaAtual = db.Jogo.Where(j => j.rodada_id == idRodada && (j.desafiante_id == user.UserId || j.desafiado_id == user.UserId)).Count();
                    if (estaNaRodadaAtual == 0)
                    {
                        if (user.situacao == suspenso)
                        {
                            gravarPontuacaoNaRodada(idRodada, user, 0.0);
                        }
                        else if ((user.situacao == "ativo") || (user.situacao == "licenciado") || (user.situacao == "inativo"))
                        {
                            gravarPontuacaoNaRodada(idRodada, user, 3.0);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

    }
}