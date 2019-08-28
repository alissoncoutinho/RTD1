using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Barragem.Models;
using Barragem.Context;
using System.Transactions;

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
                    pontuacao = 9;
                }
                else if ((jogo.idDoVencedor == jogo.desafiado_id) && (jogo.setsJogados == 3 || jogo.setsJogados == 1))
                {
                    pontuacao = 7;
                }
                else if (jogo.idDoVencedor != jogo.desafiado_id && jogo.setsJogados == 2)
                {
                    pontuacao = 1;
                }
                else if ((jogo.idDoVencedor != jogo.desafiado_id) && (jogo.setsJogados == 3 || jogo.setsJogados == 1))
                {
                    pontuacao = 3;
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
            if((jogo.situacao_Id != 4) || (jogo.desafiante.situacao.Equals("curinga")) || (jogo.desafiado.situacao.Equals("curinga")))
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
                double pontuacaoTotal = 0;
                try
                {
                    pontuacaoTotal = db.Rancking.Where(r => r.rodada.isAberta == false && r.userProfile_id == jogador.UserId && r.rodada_id < idRodada).
                    OrderByDescending(r => r.rodada_id).Take(9).Sum(r => r.pontuacao);
                }catch(Exception e)
                {
                    return;
                }
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

        public void ProcessarJogoAtrasado(Jogo jogo)
        {
            string msg = "";
            //situação: 4: finalizado -- 5: wo
            //List<Jogo> jogos = db.Jogo.Where(r => r.rodada_id == id && r.dataCadastroResultado > r.rodada.dataFim && (r.situacao_Id == 4 || r.situacao_Id == 5)).ToList();
            if (jogo.torneioId == null && jogo.dataCadastroResultado > jogo.rodada.dataFim && (jogo.situacao_Id == 4 || jogo.situacao_Id == 5))
            {
                var pontosDesafiante = 0.0;
                var pontosDesafiado = 0.0;
                try
                {
                    using (TransactionScope scope = new TransactionScope())
                    {
                        pontosDesafiante = calcularPontosDesafiante(jogo);
                        pontosDesafiado = calcularPontosDesafiado(jogo);

                        gravarPontuacaoNaRodada(jogo.rodada_id, jogo.desafiante, pontosDesafiante, true);
                        gravarPontuacaoNaRodada(jogo.rodada_id, jogo.desafiado, pontosDesafiado, true);
                        jogo.dataCadastroResultado = jogo.rodada.dataFim;
                        if (jogo.desafiante.situacao.Equals("suspenso"))
                        {
                            UserProfile desafiante = db.UserProfiles.Find(jogo.desafiante_id);
                            desafiante.situacao = "ativo";
                        }
                        if (jogo.desafiado.situacao.Equals("suspenso"))
                        {
                            UserProfile desafiado = db.UserProfiles.Find(jogo.desafiado_id);
                            desafiado.situacao = "ativo";
                        }
                        db.SaveChanges();
                        scope.Complete();
                        msg = "ok";
                    }
                }
                catch (Exception ex)
                {
                    msg = ex.Message;
                }
            }
        }

        public void EfetuarSorteio(int classeId) {
            var jogadores = db.UserProfiles.Where(u => u.classeId == classeId && u.situacao == "ativo").ToList();
            var jogadoresJaEscolhidos = new List<UserProfile>();
            if (jogadores.Count() % 2 != 0){
                var coringa = db.UserProfiles.Find(8);
                jogadores.Add(coringa);
            }
            
            while (jogadores.Count() > 0){
                Random r = new Random();
                int index = r.Next(jogadores.Count());
                var jogador = jogadores[index];
                var mesesRecentes = DateTime.Now.AddMonths(-3);
                var jogadoresQueNaoPodeJogar = (from u in db.UserProfiles
                        join jogo in db.Jogo on u.UserId equals jogo.desafiado_id
                        join rodada in db.Rodada on jogo.rodada_id equals rodada.Id
                        where u.barragemId == jogador.barragemId && jogo.torneioId == null
                        && jogo.desafiante_id == jogador.UserId && u.situacao == "ativo" && u.classeId == jogador.classeId
                        && rodada.dataFim > mesesRecentes select u).Distinct().ToList();
                var jogadoresPermitidos = jogadores.Except(jogadoresQueNaoPodeJogar).Except(jogadoresJaEscolhidos);
                foreach (var oponente in jogadoresPermitidos){
                    if (oponente.UserId != jogador.UserId){
                        jogadoresJaEscolhidos.Add(oponente);
                        jogadoresJaEscolhidos.Add(jogador);
                        //montaJogo(jogador, oponente);
                        jogadores.Remove(jogador);
                        jogadores.Remove(oponente);
                        break;
                    }                        
                }
                                

            }
        }

    }
}