﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Barragem.Models;
using Barragem.Context;
using System.Transactions;
using System.Data.Entity;

namespace Barragem.Class
{
    public class RodadaNegocio
    {
        private BarragemDbContext db = new BarragemDbContext();
        public float calcularPontosDesafiante(Jogo jogo)
        {
            float pontuacao = 0;
            // se a quantidade de games jogados for igual a zero, quer dizer que não houve jogo e o jogador ficará com zero de pontuação nesta rodada
            if (jogo.gamesJogados != 0 && !jogo.desafiante.UserName.Equals("coringa"))
            {
                if (jogo.desafiado.UserName.Equals("coringa"))
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
            if (jogo.gamesJogados != 0 && !jogo.desafiado.UserName.Equals("coringa"))
            {
                if (jogo.desafiante.UserName.Equals("coringa"))
                {
                    pontuacao = 6;
                }
                else if (jogo.idDoVencedor == jogo.desafiado_id && jogo.setsJogados == 2)
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

        private int getBonus(Jogo jogo, bool isDesafiado)
        {
            if ((jogo.situacao_Id != 4) || (jogo.desafiante.UserName.Equals("coringa")) || (jogo.desafiado.UserName.Equals("coringa")))
            {
                return 0;
            }
            if (((jogo.qtddGames1setDesafiado + jogo.qtddGames2setDesafiado) < 3) && isDesafiado)
            {
                return 3;
            }
            else if (((jogo.qtddGames1setDesafiante + jogo.qtddGames2setDesafiante) < 3) && !isDesafiado)
            {
                return 3;
            }
            else
            {
                return 0;
            }
        }

        public void gravarPontuacaoNaRodada(int idRodada, UserProfile jogador, double pontosConquistados, bool isReprocessamento = false)
        {
            try
            {
                if (jogador.UserName.Equals("coringa"))
                {
                    return;
                }
                Rancking ran = null;
                double pontuacaoTotal = 0;
                try
                {
                    int quantidadeDeRodadasParaPontuacao = 9;
                    Rodada rodadaAtual = db.Rodada.Where(r => r.Id == idRodada).Single();
                    if (rodadaAtual.temporada != null && rodadaAtual.temporada.iniciarZerada)
                    {
                        int quantidadeDeRodadasRealizadas = db.Rodada.Where(r => r.temporadaId == rodadaAtual.temporadaId && r.Id != idRodada).Count();
                        if (quantidadeDeRodadasRealizadas < quantidadeDeRodadasParaPontuacao)
                        {
                            quantidadeDeRodadasParaPontuacao = quantidadeDeRodadasRealizadas;
                        }
                    }
                    if (quantidadeDeRodadasParaPontuacao > 0)
                    {
                        pontuacaoTotal = db.Rancking.Where(r => r.rodada.isAberta == false && r.userProfile_id == jogador.UserId && r.rodada_id < idRodada).
                        OrderByDescending(r => r.rodada_id).Take(quantidadeDeRodadasParaPontuacao).Sum(r => r.pontuacao);
                    }
                }
                catch (Exception e)
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
                string suspensoWO = Tipos.Situacao.suspensoWO.ToString();
                List<UserProfile> jogadores = db.UserProfiles.Where(j => j.barragemId == barragemId).ToList();
                foreach (UserProfile user in jogadores)
                {
                    int estaNaRodadaAtual = db.Jogo.Where(j => j.rodada_id == idRodada && (j.desafiante_id == user.UserId || j.desafiado_id == user.UserId)).Count();
                    if (estaNaRodadaAtual == 0)
                    {
                        if ((user.situacao == suspenso) || (user.situacao == suspensoWO))
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

        public List<Jogo> EfetuarSorteio(int classeId, int barragemId, List<UserProfile> jogadores, int rodadaId)
        {
            try
            {
                // if classeId for igual a 0 é porque a lista já virá pronta, inicialmente utilizado para os casos de classe única com sorteio por proximidade
                if (jogadores == null)
                {
                    db.Database.ExecuteSqlCommand("DELETE j fROM jogo j INNER JOIN UserProfile u ON j.desafiado_id=u.UserId WHERE u.classeId = " + classeId + " AND j.rodada_id =" + rodadaId);
                    jogadores = db.UserProfiles.Where(u => u.classeId == classeId && u.situacao == "ativo").ToList();
                }
                // lista para guardar os jogadores já sorteados
                var jogadoresJaEscolhidos = new List<UserProfile>();
                var jogos = new List<Jogo>();
                if (jogadores.Count() < 2) return jogos;
                if (jogadores.Count() == 2)
                {
                    montarJogo(jogos, jogadoresJaEscolhidos, jogadores, jogadores[0], jogadores[1], false);
                    return jogos;
                }
                // verifica se a lista de jogadores é impar, caso seja inclui o coringa na lista
                if (jogadores.Count() % 2 != 0)
                {
                    var coringa = db.UserProfiles.Find(8);
                    jogadores.Add(coringa);
                }
                var qtddJogos = jogadores.Count() / 2;
                for (int i = qtddJogos; i >= 1; i--)
                {
                    // variável utilizada por segurança, para evitar um eventual loop infinito;
                    var limitador = 0;
                    // conforme for montando os jogos vai retirando os jogadores da lista, até não sobrar mais nenhum
                    while (jogadores.Count() > 0)
                    {
                        var jogador = getJogadorRandomicamente(jogadores);
                        jogadores.Remove(jogador);
                        List<UserProfile> jogadoresQueNaoPodeJogar = getUltimosOponentes(jogador.UserId, qtddJogos, barragemId);
                        var jogadoresPermitidos = getJogadoresPermitidos(jogadores, jogadoresQueNaoPodeJogar);
                        if (jogadoresPermitidos.Count() > 0)
                        {
                            int indexPermit = new Random().Next(jogadoresPermitidos.Count());
                            var oponente = jogadoresPermitidos.ElementAt(indexPermit);
                            montarJogo(jogos, jogadoresJaEscolhidos, jogadores, jogador, oponente, true);
                        }
                        else
                        {
                            if (limitador++ > 10) break;
                            jogadoresPermitidos = getJogadoresPermitidos(jogadoresJaEscolhidos, jogadoresQueNaoPodeJogar);
                            int indexPermit = new Random().Next(jogadoresPermitidos.Count());
                            var oponente = jogadoresPermitidos.ElementAt(indexPermit);
                            desfazerJogo(jogadores, jogadoresJaEscolhidos, jogos, oponente);
                            montarJogo(jogos, jogadoresJaEscolhidos, jogadores, jogador, oponente, false);
                        }
                    }
                }

                return jogos;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private List<UserProfile> getJogadoresPermitidos(List<UserProfile> jogadores, List<UserProfile> jogadoresQueNaoPodeJogar)
        {
            List<UserProfile> jogadoresPermitidos = new List<UserProfile>();
            foreach (var jogador in jogadores)
            {
                var temNalista = 0;
                try
                {
                    temNalista = jogadoresQueNaoPodeJogar.Where(j => j.UserId == jogador.UserId).Count();
                }
                catch (Exception e) { }
                if (temNalista == 0)
                {
                    jogadoresPermitidos.Add(jogador);
                }
            }
            return jogadoresPermitidos;
        }

        private UserProfile getJogadorRandomicamente(List<UserProfile> jogadores)
        {
            Random r = new Random();
            int index = r.Next(jogadores.Count());
            var jogador = jogadores[index];
            return jogador;
        }

        private void montarJogo(List<Jogo> jogos, List<UserProfile> jogadoresJaEscolhidos, List<UserProfile> jogadores, UserProfile jogador, UserProfile oponente, bool removeOponenteLista = true)
        {
            jogos.Add(new Jogo { desafiado_id = oponente.UserId, desafiado = oponente, desafiante_id = jogador.UserId, desafiante = jogador });
            jogadoresJaEscolhidos.Add(jogador);
            if (removeOponenteLista)
            {
                jogadoresJaEscolhidos.Add(oponente);
                jogadores.Remove(oponente);
            }
        }

        public void salvarJogos(List<Jogo> jogos, int rodadaId)
        {
            foreach (var jogo in jogos)
            {
                try
                {
                    var j = new Jogo();
                    j.desafiado_id = jogo.desafiado_id;
                    j.desafiante_id = jogo.desafiante_id;
                    j.rodada_id = rodadaId;
                    j.situacao_Id = 1;
                    if (j.desafiante_id == 8)
                    { //coringa
                        j.situacao_Id = 4;
                        j.qtddGames1setDesafiado = 6;
                        j.qtddGames2setDesafiado = 6;
                        j.qtddGames1setDesafiante = 0;
                        j.qtddGames2setDesafiante = 0;
                    }
                    db.Jogo.Add(j);
                    db.Entry(j).State = EntityState.Added;
                    db.SaveChanges();
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException e)
                {
                    Console.Write(e);
                }
            }

        }

        private void gravaLog(string dados)
        {
            var log2 = new Log();
            log2.descricao = dados;
            db.Log.Add(log2);
            db.SaveChanges();
        }

        public void EfetuarSorteioPorProximidade(int barragemId, int classeId, int rodadaId)
        {
            var dadosBaseLog = "Sorteio:" + barragemId + " ";
            var dadosLog = dadosBaseLog + " classe:" + classeId + " rodada:" + rodadaId;
            gravaLog(dadosLog);
            db.Database.ExecuteSqlCommand("DELETE j fROM jogo j INNER JOIN UserProfile u ON j.desafiado_id=u.UserId WHERE u.classeId = " + classeId + " AND j.rodada_id =" + rodadaId);
            var ultimaRodada = db.Rodada.Where(r => r.barragemId == barragemId && r.isAberta == false).Max(r => r.Id);
            dadosLog = dadosBaseLog + "ultima rodada:" + ultimaRodada;
            gravaLog(dadosLog);
            var rankingJogadores = db.Rancking.Include(r => r.userProfile).Include(r => r.rodada).
                Where(r => r.rodada_id == ultimaRodada && r.userProfile.situacao == "ativo" && r.userProfile.classeId == classeId).
                OrderByDescending(r => r.totalAcumulado).Select(rk => new Classificacao()
                {
                    userId = rk.userProfile_id,
                    nomeUser = rk.userProfile.nome,
                    posicaoUser = (int)rk.posicao,
                    pontuacao = rk.totalAcumulado,
                    foto = rk.userProfile.fotoURL
                }).ToList<Classificacao>();

            //List<RankingView> rankingJogadores = db.RankingView.
            //        Where(r => r.barragemId == barragemId && r.classeId == classeId && r.situacao.Equals("ativo")).
            //        OrderByDescending(r => r.totalAcumulado).ToList();
            var jgs = new List<UserProfile>();
            if (rankingJogadores.Count() == 0)
            {
                jgs = db.UserProfiles.Where(u => u.classeId == classeId && u.situacao == "ativo").ToList();
                // esse if é necessário para os casos de jogadores que estavam desativados e voltaram para a barragem, eles estarão sem ranking atualizado
            }
            else if (rankingJogadores.Count() != db.UserProfiles.Where(u => u.classeId == classeId && u.situacao == "ativo").Count())
            {
                var userIds = rankingJogadores.Select(r => r.userId).ToArray<int>();
                var jogadores = db.UserProfiles.Where(u => u.classeId == classeId && u.situacao == "ativo" && !userIds.Contains(u.UserId)).ToList();
                jgs = rankingJogadores.Select(j => new UserProfile() { UserId = j.userId, nome = j.nomeUser }).Distinct().ToList();
                jgs.AddRange(jogadores);
            }
            else
            {
                jgs = rankingJogadores.Select(j => new UserProfile() { UserId = j.userId, nome = j.nomeUser }).Distinct().ToList();
            }
            dadosLog = dadosBaseLog + "qtdd Jogadores:" + jgs.Count();
            gravaLog(dadosLog);
            var jogadoresPorBloco = 11;
            int divisaoPorClasse = 0;
            while (jogadoresPorBloco > 10)
            {
                divisaoPorClasse++;
                jogadoresPorBloco = jgs.Count() / divisaoPorClasse;
            }
            if (jogadoresPorBloco % 2 != 0) jogadoresPorBloco++;
            dadosLog = dadosBaseLog + "qtdd classes:" + divisaoPorClasse + "qtdd Jogadores por bloco:" + jogadoresPorBloco;
            gravaLog(dadosLog);

            var contador = 0;
            var jogadoresParaEnvio = new List<UserProfile>();
            var classeAtual = 1;
            foreach (var jogador in jgs)
            {
                contador++;
                jogadoresParaEnvio.Add(jogador);
                if ((contador == jogadoresPorBloco && divisaoPorClasse != classeAtual) || (jogador.UserId == jgs[jgs.Count() - 1].UserId))
                {
                    dadosLog = dadosBaseLog + "sorteando classe:" + classeAtual + "qtdd Jogadores incluidos:" + jogadoresParaEnvio.Count();
                    gravaLog(dadosLog);
                    classeAtual++;
                    contador = 0;
                    var jogos = EfetuarSorteio(classeId, barragemId, jogadoresParaEnvio, rodadaId);
                    jogos = definirDesafianteDesafiado(jogos, classeId, barragemId, rankingJogadores);
                    salvarJogos(jogos, rodadaId);
                    jogadoresParaEnvio = new List<UserProfile>();
                    dadosLog = dadosBaseLog + "sorteio efetuado - desafiantes definidos - jogos salvos - lista para envio: " + jogadoresParaEnvio.Count();
                    gravaLog(dadosLog);
                }
            }


        }

        private List<UserProfile> getUltimosOponentes(int userId, int qtddJogos, int barragemId)
        {

            var oponentes = new List<UserProfile>();
            var retorno = (from j in db.Jogo
                           join rodada in db.Rodada on j.rodada_id equals rodada.Id
                           where rodada.barragemId == barragemId && j.torneioId == null && j.desafiante_id == userId
                           select j).Union(
                from j in db.Jogo
                join rodada in db.Rodada on j.rodada_id equals rodada.Id
                where rodada.barragemId == barragemId && j.torneioId == null && j.desafiado_id == userId
                select j).Take(qtddJogos).OrderByDescending(j => j.Id).ToList();
            foreach (var jogo in retorno)
            {
                if (jogo.desafiante_id == userId)
                {
                    oponentes.Add(jogo.desafiado);
                }
                else if (jogo.desafiado_id == userId)
                {
                    oponentes.Add(jogo.desafiante);
                }
            }

            if (retorno == null) return new List<UserProfile>();
            return oponentes;
        }

        public List<Jogo> definirDesafianteDesafiado(List<Jogo> jogos, int classeId, int barragemId, List<Classificacao> ranking = null)
        {
            if (ranking == null)
            {
                var ultimaRodada = db.Rodada.Where(r => r.barragemId == barragemId && r.isAberta == false).Max(r => r.Id);
                ranking = db.Rancking.Include(r => r.userProfile).Include(r => r.rodada).
                    Where(r => r.rodada_id == ultimaRodada && r.userProfile.situacao == "ativo" && r.classe.Id == classeId).
                    OrderByDescending(r => r.totalAcumulado).Select(rk => new Classificacao()
                    {
                        userId = rk.userProfile_id,
                        nomeUser = rk.userProfile.nome,
                        posicaoUser = (int)rk.posicao,
                        pontuacao = rk.totalAcumulado,
                        foto = rk.userProfile.fotoURL
                    }).ToList<Classificacao>();
                //ranking = db.RankingView.Where(r => r.classeId == classeId && r.situacao.Equals("ativo")).ToList();
            }
            Classificacao rkDesafiante = null;
            Classificacao rkDesafiado = null;
            foreach (var jogo in jogos)
            {
                try
                {

                    // quer dizer que o coringa está na posição errada.
                    if (jogo.desafiado_id == 8)
                    {
                        var desafiado = jogo.desafiante;
                        var desafiante = jogo.desafiado;
                        jogo.desafiado = desafiado;
                        jogo.desafiado_id = desafiado.UserId;
                        jogo.desafiante = desafiante;
                        jogo.desafiante_id = desafiante.UserId;
                    }
                    else if (jogo.desafiante_id != 8) // não verificar se for coringa
                    {
                        rkDesafiado = ranking.Where(r => r.userId == jogo.desafiado.UserId).FirstOrDefault();
                        rkDesafiante = ranking.Where(r => r.userId == jogo.desafiante.UserId).FirstOrDefault();
                        // se a posicao for igual a zero o cara acabou de entrar ainda não tem posicao então verificar pelo totalAcumulado
                        if ((rkDesafiado != null && rkDesafiante != null) && (rkDesafiante.pontuacao == rkDesafiado.pontuacao) && (rkDesafiante.posicaoUser < rkDesafiado.posicaoUser))
                        {
                            var desafiado = jogo.desafiante;
                            var desafiante = jogo.desafiado;
                            jogo.desafiado = desafiado;
                            jogo.desafiado_id = desafiado.UserId;
                            jogo.desafiante = desafiante;
                            jogo.desafiante_id = desafiante.UserId;
                        }
                        if ((rkDesafiado != null && rkDesafiante != null) && (rkDesafiante.pontuacao > rkDesafiado.pontuacao))
                        {
                            var desafiado = jogo.desafiante;
                            var desafiante = jogo.desafiado;
                            jogo.desafiado = desafiado;
                            jogo.desafiado_id = desafiado.UserId;
                            jogo.desafiante = desafiante;
                            jogo.desafiante_id = desafiante.UserId;
                        }
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            return jogos;
        }

        private void desfazerJogo(List<UserProfile> jogadores, List<UserProfile> jogadoresJaSelecionados, List<Jogo> jogos, UserProfile jogadorSelecionado)
        {
            try
            {
                var jogo = jogos.Where(j => j.desafiado_id == jogadorSelecionado.UserId || j.desafiante_id == jogadorSelecionado.UserId).FirstOrDefault();
                jogos.Remove(jogo);
                if (jogo.desafiante_id == jogadorSelecionado.UserId)
                {
                    jogadores.Add(jogo.desafiado);
                    jogadoresJaSelecionados.Remove(jogo.desafiado);
                }
                else
                {
                    jogadores.Add(jogo.desafiante);
                    jogadoresJaSelecionados.Remove(jogo.desafiante);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public Rodada Create(Rodada rodada)
        {
            try
            {
                var barragem = db.BarragemView.Find(rodada.barragemId);
                if (!barragem.isAtiva)
                {
                    throw new ExceptionRankingDesativado("Não foi possível criar uma nova rodada, pois este ranking encontra-se desativado.");
                }

                List<Rodada> rodadas = db.Rodada.Where(r => r.isAberta == true && r.barragemId == rodada.barragemId).ToList();

                if (rodadas.Count() > 0)
                {
                    throw new ExceptionRodadaEmAberto("Não foi possível criar uma nova rodada, pois ainda existe rodada(s) em aberto.");
                }

                Rodada rd = db.Rodada.Where(r => r.barragemId == rodada.barragemId).OrderByDescending(r => r.Id).Take(1).Single();
                if (rd.sequencial == 10)
                {
                    try
                    {
                        string alfabeto = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                        int pos = alfabeto.IndexOf(rd.codigo);
                        pos++;
                        rodada.sequencial = 1;
                        rodada.codigo = Convert.ToString(alfabeto[pos]);
                    }
                    catch (Exception)
                    {
                        rodada.sequencial = 1;
                        rodada.codigo = "A";
                    }
                }
                else
                {
                    rodada.sequencial = rd.sequencial + 1;
                    rodada.codigo = rd.codigo;
                }

                rodada.isAberta = true;
                rodada.dataFim = new DateTime(rodada.dataFim.Year, rodada.dataFim.Month, rodada.dataFim.Day, 23, 59, 59);
                db.Rodada.Add(rodada);
                db.SaveChanges();

                return rodada;
            }
            catch (Exception e)
            {
                throw new Exception("Houve um erro no processamento da rodada.", e);
            }

        }

        public void FecharRodada(int id)
        {
            string msg = "";
            db.Database.ExecuteSqlCommand("Delete from Rancking where rodada_id=" + id + " and posicaoClasse is not null");
            List<Jogo> jogos = db.Jogo.Where(r => r.rodada_id == id).ToList();
            var pontosDesafiante = 0.0;
            var pontosDesafiado = 0.0;
            Rodada rodada = db.Rodada.Find(id);
            BarragemView barragem = db.BarragemView.Find(rodada.barragemId);
            try
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    foreach (Jogo item in jogos)
                    {
                        pontosDesafiante = calcularPontosDesafiante(item);
                        pontosDesafiado = calcularPontosDesafiado(item);
                        msg = "pontosDesafio" + item.desafiado_id;
                        if (!item.desafiante.UserName.Equals("coringa"))
                        {
                            gravarPontuacaoNaRodada(id, item.desafiante, pontosDesafiante);
                            msg = "gravarPontuacaoNaRodadaDesafiante" + item.desafiante_id;
                        }
                        gravarPontuacaoNaRodada(id, item.desafiado, pontosDesafiado);
                        msg = "gravarPontuacaoNaRodadaDesafiado" + item.desafiado_id;
                        if (barragem.suspensaoPorAtraso)
                        {
                            verificarRegraSuspensaoPorAtraso(item);
                        }
                        if (barragem.suspensaoPorWO)
                        {
                            verificarRegraSuspensaoPorWO(item);
                        }
                        msg = "verificarRegraSuspensao" + item.desafiado_id;
                    }

                    gerarPontuacaoDosJogadoresForaDaRodada(id, rodada.barragemId);
                    msg = "gerarPontuacaoDosJogadoresForaDaRodada";

                    gerarRancking(id);
                    msg = "gerarRanking";
                    List<Classe> classes = db.Classe.Where(c => c.barragemId == rodada.barragemId).ToList();
                    for (int i = 0; i < classes.Count(); i++)
                    {
                        gerarRanckingPorClasse(id, classes[i].Id);
                    }
                    msg = "gerarRankingPorClasse";
                    rodada.isAberta = false;
                    rodada.dataFim = DateTime.Now;
                    db.Entry(rodada).State = EntityState.Modified;
                    db.SaveChanges();
                    scope.Complete();
                    msg = "ok";
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao fechar rodada", ex);
            }
        }

        private void verificarRegraSuspensaoPorAtraso(Jogo jogo)
        {
            // 5.6.O jogador que não receber pontuação ou deixar de jogar, mesmo que justificadamente, por 2 (dois) jogos seguidos
            //será retirado das rodadas e colocado em suspensão automática até que regularize seus jogos de forma que ele saia desta condição.
            if (jogo.gamesJogados == 0)
            {
                // Caso no fechamento da rodada o jogo não tenha sido realizado, verificar a situação da rodada anterior
                verificarSeJogoRealizadoNaRodadaAnterior(jogo.desafiado_id, jogo.rodada_id, jogo.rodada.barragemId);
                verificarSeJogoRealizadoNaRodadaAnterior(jogo.desafiante_id, jogo.rodada_id, jogo.rodada.barragemId);
            }
        }

        private void verificarRegraSuspensaoPorWO(Jogo jogo)
        {
            // Jogador que perder por wo 2 vezes seguidas ficará em suspensão por WO até que o adminstrador retire ele dessa situação.
            if (jogo.situacao_Id == 5)
            {
                var userDerrotado = jogo.idDoPerdedor;
                verificarSeHouveWONaRodadaAnterior(userDerrotado, jogo.rodada_id, jogo.rodada.barragemId);

            }
        }

        private void gerarRancking(int idRodada)
        {
            List<Rancking> listaRancking = db.Rancking.Where(r => r.rodada_id == idRodada && r.userProfile.situacao != "desativado" && r.userProfile.situacao != "inativo").OrderByDescending(r => r.totalAcumulado).ToList();

            OrdenarJogadoresRanking(listaRancking, true, false);

            foreach (Rancking ran in listaRancking)
            {
                db.Entry(ran).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        public void OrdenarJogadoresRanking(List<Rancking> listaRancking, bool ordenarPosicao, bool ordenarPosicaoClasse)
        {
            List<Rancking> rankingOrdenado = new List<Rancking>();
            int posicaoRanking = 1;
            var agrupamentoJogadoresPontuacao = listaRancking.OrderByDescending(o => o.totalAcumulado).GroupBy(g => g.totalAcumulado);
            foreach (var grupoJogadoresEmpatados in agrupamentoJogadoresPontuacao)
            {
                if (grupoJogadoresEmpatados.Count() == 2)
                {
                    var dadosJogador1 = grupoJogadoresEmpatados.First();
                    var dadosJogador2 = grupoJogadoresEmpatados.Last();
                    var rodada = db.Rodada.Find(dadosJogador1.rodada_id);
                    //OBTER AS ULTIMAS 10 rodadas
                    var ultimasRodadas = db.Rodada.Where(x => x.barragemId == rodada.barragemId && x.isAberta == false).OrderByDescending(x => x.Id).Take(10).Select(s => s.Id);

                    //BUSCAR JOGOS PARA VERIFICAR CONFRONTO
                    var confrontosJogadores = db.Jogo.Where(x => ultimasRodadas.Contains(x.rodada_id)
                        && ((x.desafiante_id == dadosJogador1.userProfile_id && x.desafiado_id == dadosJogador2.userProfile_id)
                         || (x.desafiante_id == dadosJogador2.userProfile_id && x.desafiado_id == dadosJogador1.userProfile_id)));

                    //VERIFICAR A QUANTIDADE DE CONFRONTOS ENTRE OS EMPATADOS
                    int partidasGanhasJogador1 = 0;
                    int partidasGanhasJogador2 = 0;
                    foreach (var x in confrontosJogadores)
                    {
                        if ((x.qtddSetsGanhosDesafiante > x.qtddSetsGanhosDesafiado && x.desafiante_id == dadosJogador1.userProfile_id) || (x.qtddSetsGanhosDesafiante < x.qtddSetsGanhosDesafiado && x.desafiado_id == dadosJogador1.userProfile_id))
                        {
                            partidasGanhasJogador1++;
                        }
                        else if ((x.qtddSetsGanhosDesafiante > x.qtddSetsGanhosDesafiado && x.desafiante_id == dadosJogador2.userProfile_id) || (x.qtddSetsGanhosDesafiante < x.qtddSetsGanhosDesafiado && x.desafiado_id == dadosJogador2.userProfile_id))
                        {
                            partidasGanhasJogador2++;
                        }
                    }

                    if (partidasGanhasJogador1 > partidasGanhasJogador2)
                    {
                        AplicarOrdenacao(dadosJogador1, posicaoRanking, ordenarPosicao, ordenarPosicaoClasse);
                        posicaoRanking++;
                        AplicarOrdenacao(dadosJogador2, posicaoRanking, ordenarPosicao, ordenarPosicaoClasse);
                        posicaoRanking++;
                    }
                    else if (partidasGanhasJogador1 < partidasGanhasJogador2)
                    {
                        AplicarOrdenacao(dadosJogador2, posicaoRanking, ordenarPosicao, ordenarPosicaoClasse);
                        posicaoRanking++;
                        AplicarOrdenacao(dadosJogador1, posicaoRanking, ordenarPosicao, ordenarPosicaoClasse);
                        posicaoRanking++;
                    }
                    else
                    {
                        var jogadoresEmpatados = grupoJogadoresEmpatados
                        .Select(s => new { s.userProfile_id, s.userProfile.dataNascimento })
                        .OrderBy(o => o.dataNascimento);

                        foreach (var jogador in jogadoresEmpatados)
                        {
                            AplicarOrdenacao(grupoJogadoresEmpatados.First(x => x.userProfile_id == jogador.userProfile_id), posicaoRanking, ordenarPosicao, ordenarPosicaoClasse);
                            posicaoRanking++;
                        }
                    }

                }
                else if (grupoJogadoresEmpatados.Count() >= 3)
                {
                    var jogadoresEmpatados = grupoJogadoresEmpatados
                        .Select(s => new { s.userProfile_id, s.userProfile.dataNascimento })
                        .OrderBy(o => o.dataNascimento);

                    foreach (var jogador in jogadoresEmpatados)
                    {
                        AplicarOrdenacao(grupoJogadoresEmpatados.First(x => x.userProfile_id == jogador.userProfile_id), posicaoRanking, ordenarPosicao, ordenarPosicaoClasse);
                        posicaoRanking++;
                    }
                }
                else
                {
                    foreach (var jogador in grupoJogadoresEmpatados)
                    {
                        AplicarOrdenacao(jogador, posicaoRanking, ordenarPosicao, ordenarPosicaoClasse);
                        posicaoRanking++;
                    }
                }
            }
        }

        private static void AplicarOrdenacao(Rancking ranking, int posicao, bool ordenarPosicao, bool ordenarPosicaoClasse)
        {
            if (ordenarPosicao)
            {
                ranking.posicao = posicao;
            }
            if (ordenarPosicaoClasse)
            {
                ranking.posicaoClasse = posicao;
            }
        }

        private void gerarRanckingPorClasse(int idRodada, int classeId)
        {
            List<Rancking> listaRancking = db.Rancking.Where(r => r.rodada_id == idRodada && r.userProfile.classeId == classeId && r.userProfile.situacao != "desativado" && r.userProfile.situacao != "inativo").OrderByDescending(r => r.totalAcumulado).ToList();
            
            OrdenarJogadoresRanking(listaRancking, false, true);

            foreach (Rancking ran in listaRancking)
            {
                db.Entry(ran).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        private void verificarSeJogoRealizadoNaRodadaAnterior(int idJogador, int rodada_id, int barragemId)
        {
            int idRodadaAnterior = db.Rodada.Where(r => r.isAberta == false && r.Id < rodada_id && r.barragemId == barragemId).Max(r => r.Id);
            List<Jogo> jogoAnterior = db.Jogo.Where(j => j.rodada_id == idRodadaAnterior && (j.desafiado_id == idJogador || j.desafiante_id == idJogador))
                .ToList();
            if (jogoAnterior.Count > 0)
            {
                if (jogoAnterior[0].gamesJogados == 0)
                {
                    UserProfile jogador = db.UserProfiles.Find(idJogador);
                    jogador.situacao = Tipos.Situacao.suspenso.ToString();
                    db.SaveChanges();
                }
            }

        }

        private void verificarSeHouveWONaRodadaAnterior(int idJogador, int rodada_id, int barragemId)
        {
            int idRodadaAnterior = db.Rodada.Where(r => r.isAberta == false && r.Id < rodada_id && r.barragemId == barragemId).Max(r => r.Id);
            List<Jogo> jogoAnterior = db.Jogo.Where(j => j.rodada_id == idRodadaAnterior && (j.desafiado_id == idJogador || j.desafiante_id == idJogador))
                .ToList();
            if (jogoAnterior.Count > 0)
            {
                if ((jogoAnterior[0].situacao_Id == 5) && (idJogador == jogoAnterior[0].idDoPerdedor))
                {
                    UserProfile jogador = db.UserProfiles.Find(idJogador);
                    jogador.situacao = Tipos.Situacao.suspensoWO.ToString();
                    db.SaveChanges();
                }
            }

        }

        public void SortearJogos(int id, int barragemId, bool notificarApp = true)
        {
            var rodadaNegocio = new RodadaNegocio();
            try
            {
                List<Classe> classes = db.Classe.Where(c => c.barragemId == barragemId && c.ativa == true).ToList();
                var isClasseUnica = db.BarragemView.Find(barragemId).isClasseUnica;
                var jogos = new List<Jogo>();
                for (int i = 0; i < classes.Count(); i++)
                {
                    if (isClasseUnica)
                    {
                        rodadaNegocio.EfetuarSorteioPorProximidade(barragemId, classes[i].Id, id);
                    }
                    else
                    {
                        jogos = rodadaNegocio.EfetuarSorteio(classes[i].Id, barragemId, null, id);
                        jogos = rodadaNegocio.definirDesafianteDesafiado(jogos, classes[i].Id, barragemId);
                        rodadaNegocio.salvarJogos(jogos, id);
                    }

                }
            }
            catch (Exception e)
            {
                throw e;
            }
            if (notificarApp) { NotificacaoApp(barragemId); }

        }

        public void NotificacaoApp(int barragemId)
        {
            try
            {
                var nomeRanking = db.BarragemView.Find(barragemId).nome;
                var titulo = nomeRanking + ": Classificação atualizada e nova rodada gerada!";
                var conteudo = "Clique aqui e entre em contato com seu adversário o mais breve possível e bom jogo.";

                var fbmodel = new FirebaseNotificationModel() { to = "/topics/ranking" + barragemId, notification = new NotificationModel() { title = titulo, body = conteudo }, data = new DataModel() { title = titulo, body = conteudo, type = "nova_rodada_aberta", idRanking = barragemId } };
                new FirebaseNotification().SendNotification(fbmodel);
            }
            catch (Exception e)
            {

            }

        }
    }
}