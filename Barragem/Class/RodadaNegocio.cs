using System;
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
            if ((jogo.situacao_Id != 4) || (jogo.desafiante.situacao.Equals("curinga")) || (jogo.desafiado.situacao.Equals("curinga")))
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
            foreach(var jogador in jogadores)
            {
                var temNalista = 0;
                try{
                    temNalista = jogadoresQueNaoPodeJogar.Where(j => j.UserId == jogador.UserId).Count();
                } catch (Exception e) { }
                    if (temNalista == 0){
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

        public void EfetuarSorteioPorProximidade(int barragemId, int classeId, int rodadaId)
        {
            db.Database.ExecuteSqlCommand("DELETE j fROM jogo j INNER JOIN UserProfile u ON j.desafiado_id=u.UserId WHERE u.classeId = " + classeId + " AND j.rodada_id =" + rodadaId);
            var ultimaRodada = db.Rodada.Where(r => r.barragemId == barragemId && r.isAberta == false).Max(r => r.Id);
            var rankingJogadores = db.Rancking.Include(r => r.userProfile).Include(r => r.rodada).
                Where(r => r.rodada_id == ultimaRodada && r.userProfile.situacao == "ativo" && r.classe.Id == classeId).
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
            if (rankingJogadores.Count() == 0) {
                jgs = db.UserProfiles.Where(u => u.classeId == classeId && u.situacao == "ativo").ToList();
            // esse if é necessário para os casos de jogadores que estavam desativados e voltaram para a barragem, eles estarão sem ranking atualizado
            } else if (rankingJogadores.Count() != db.UserProfiles.Where(u => u.classeId == classeId && u.situacao == "ativo").Count()){
                var userIds = rankingJogadores.Select(r => r.userId).ToArray<int>();
                var jogadores = db.UserProfiles.Where(u => u.classeId == classeId && u.situacao == "ativo" && !userIds.Contains(u.UserId)).ToList();
                jgs = rankingJogadores.Select(j => new UserProfile() { UserId = j.userId, nome = j.nomeUser }).Distinct().ToList();
                jgs.AddRange(jogadores);
            } else {
                jgs = rankingJogadores.Select(j => new UserProfile() { UserId = j.userId, nome = j.nomeUser }).Distinct().ToList();
            }
            var jogadoresPorBloco = 11;
            int divisaoPorClasse = 0;
            while (jogadoresPorBloco > 10)
            {
                divisaoPorClasse++;
                jogadoresPorBloco = jgs.Count() / divisaoPorClasse;
            }
            if (jogadoresPorBloco % 2 != 0) jogadoresPorBloco++;

            
            var contador = 0;
            var jogadoresParaEnvio = new List<UserProfile>();
            var classeAtual = 1;
            foreach (var jogador in jgs)
            {
                contador++;
                jogadoresParaEnvio.Add(jogador);
                if ((contador == jogadoresPorBloco && divisaoPorClasse != classeAtual) || (jogador.UserId == jgs[jgs.Count() - 1].UserId))
                {
                    classeAtual++;
                    contador = 0;
                    var jogos = EfetuarSorteio(classeId, barragemId, jogadoresParaEnvio, rodadaId);
                    jogos = definirDesafianteDesafiado(jogos, classeId, barragemId, rankingJogadores);
                    salvarJogos(jogos, rodadaId);
                    jogadoresParaEnvio = new List<UserProfile>();
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
                if (jogo.desafiante_id == userId){
                    oponentes.Add(jogo.desafiado);
                } else if (jogo.desafiado_id == userId){
                    oponentes.Add(jogo.desafiante);
                }
            }

            if (retorno == null) return new List<UserProfile>();
            return oponentes;
        }

        public List<Jogo> definirDesafianteDesafiado(List<Jogo> jogos, int classeId, int barragemId, List<Classificacao> ranking = null)
        {
            if (ranking == null){
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
                        if ((rkDesafiado != null && rkDesafiante != null) && (rkDesafiante.totalAcumulado == rkDesafiado.totalAcumulado) && (rkDesafiante.posicaoUser < rkDesafiado.posicaoUser))
                        {
                            var desafiado = jogo.desafiante;
                            var desafiante = jogo.desafiado;
                            jogo.desafiado = desafiado;
                            jogo.desafiado_id = desafiado.UserId;
                            jogo.desafiante = desafiante;
                            jogo.desafiante_id = desafiante.UserId;
                        }
                        if ((rkDesafiado != null && rkDesafiante != null) && (rkDesafiante.totalAcumulado > rkDesafiado.totalAcumulado))
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
    }
}