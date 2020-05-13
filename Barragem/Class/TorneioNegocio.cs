using Barragem.Context;
using Barragem.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace Barragem.Class
{
    public class TorneioNegocio
    {
        private BarragemDbContext db = new BarragemDbContext();

        public void MontarProximoJogoTorneio(Jogo jogo)
        {
            var ordemJogo = 0;
            if (jogo.torneioId != null)
            {
                if (jogo.ordemJogo % 2 != 0)
                {
                    ordemJogo = (int)(jogo.ordemJogo / 2) + 1;
                }
                else
                {
                    ordemJogo = (int)(jogo.ordemJogo / 2);
                }
                var torneioId = jogo.torneioId;
                var torneio = db.Torneio.Find(torneioId);
                var classeId = jogo.classeTorneio;
                var isPrimeiroJogo = false;
                if (jogo.isPrimeiroJogoTorneio != null) isPrimeiroJogo = (bool)jogo.isPrimeiroJogoTorneio;

                if ((torneio.temRepescagem) && (isPrimeiroJogo))
                {
                    CadastrarPerdedorNaRepescagem(jogo);
                }
                if (db.Jogo.Where(r => r.torneioId == jogo.torneioId && r.classeTorneio == jogo.classeTorneio &&
                   r.faseTorneio == jogo.faseTorneio - 1 && r.ordemJogo == ordemJogo).Count() > 0)
                {
                    var proximoJogo = db.Jogo.Where(r => r.torneioId == jogo.torneioId && r.classeTorneio == jogo.classeTorneio &&
                        r.faseTorneio == jogo.faseTorneio - 1 && r.ordemJogo == ordemJogo).Single();
                    if (jogo.desafiante_id == 10)
                    {
                        proximoJogo.isPrimeiroJogoTorneio = true;
                    }
                    else
                    {
                        proximoJogo.isPrimeiroJogoTorneio = false;
                    }
                    if (jogo.ordemJogo % 2 != 0)
                    {
                        proximoJogo.desafiado_id = jogo.idDoVencedor;
                        proximoJogo.desafiado2_id = getParceiroDuplaProximoJogo(jogo, jogo.idDoVencedor);
                        if (jogo.idDoVencedor == jogo.desafiado_id)
                        {
                            proximoJogo.cabecaChave = jogo.cabecaChave;
                        }
                        else
                        {
                            proximoJogo.cabecaChave = jogo.cabecaChaveDesafiante;
                        }
                    }
                    else
                    {
                        proximoJogo.desafiante_id = jogo.idDoVencedor;
                        proximoJogo.desafiante2_id = getParceiroDuplaProximoJogo(jogo, jogo.idDoVencedor);
                        if (jogo.idDoVencedor == jogo.desafiado_id)
                        {
                            proximoJogo.cabecaChaveDesafiante = jogo.cabecaChave;
                        }
                        else
                        {
                            proximoJogo.cabecaChaveDesafiante = jogo.cabecaChaveDesafiante;
                        }
                    }

                    cadastrarColocacaoPerdedorTorneio(jogo);
                    db.Entry(proximoJogo).State = EntityState.Modified;
                    db.SaveChanges();
                }
                else
                {
                    //cadastrar a pontuacao do vice 
                    var inscricaoPerdedor = db.InscricaoTorneio.Where(i => i.userId == jogo.idDoPerdedor
                        && i.torneioId == jogo.torneioId && i.classe == jogo.classeTorneio).ToList();
                    if (inscricaoPerdedor.Count() > 0)
                    {
                        inscricaoPerdedor[0].colocacao = 1; // vice
                        jogo.faseTorneio = 1;
                        int pontuacao = CalculadoraDePontos.AddTipoTorneio(torneio.TipoTorneio).CalculaPontos(inscricaoPerdedor[0]);
                        inscricaoPerdedor[0].Pontuacao = pontuacao;
                        db.SaveChanges();
                    }
                    // indicar o vencedor do torneio
                    var inscricao = db.InscricaoTorneio.Where(i => i.userId == jogo.idDoVencedor
                        && i.torneioId == jogo.torneioId && i.classe == jogo.classeTorneio).ToList();
                    if (inscricao.Count() > 0)
                    {
                        inscricao[0].colocacao = 0; // vencedor
                        jogo.faseTorneio = 1;
                        int pontuacao = CalculadoraDePontos.AddTipoTorneio(torneio.TipoTorneio).CalculaPontos(inscricao[0]);
                        inscricao[0].Pontuacao = pontuacao;
                        db.SaveChanges();
                    }
                    //
                    new CalculadoraDePontos().GerarSnapshotDaLiga(jogo);
                }
            }
        }

        private void CadastrarPerdedorNaRepescagem(Jogo jogo)
        {
            // cadastrar perderdor
            int? primeiraFase = db.Jogo.Where(r => r.torneioId == jogo.torneioId && r.classeTorneio == jogo.classeTorneio).Max(r => r.faseTorneio);
            var jogosPrimeiraFase = db.Jogo.Where(r => r.torneioId == jogo.torneioId && r.classeTorneio == jogo.classeTorneio &&
                r.faseTorneio == primeiraFase && (r.desafiado_id == 0 || r.desafiante_id == 0)).OrderBy(r => r.ordemJogo).ToList();
            // para evitar que seja cadastrado duas vezes caso o placar seja alterado
            var isPerderdorJaCadastradoNaRepescagem = jogosPrimeiraFase.Where(j => j.torneioId == jogo.torneioId && j.classeTorneio == jogo.classeTorneio
                && j.faseTorneio == primeiraFase && (j.desafiado_id == jogo.idDoPerdedor || j.desafiado_id == jogo.idDoPerdedor)).Count();
            if (isPerderdorJaCadastradoNaRepescagem < 2)
            {
                foreach (Jogo j in jogosPrimeiraFase)
                {
                    if (j.desafiado_id == 0)
                    {
                        j.desafiado_id = jogo.idDoPerdedor;
                        j.desafiado2_id = getParceiroDuplaProximoJogo(jogo, jogo.idDoPerdedor);
                        db.Entry(j).State = EntityState.Modified;
                        db.SaveChanges();
                        // verificar se caiu com o bye e avançar para próxima fase
                        if (j.desafiante_id == 10)
                        {
                            j.dataCadastroResultado = DateTime.Now;
                            j.usuarioInformResultado = "SISTEMA";
                            j.situacao_Id = 5;
                            j.qtddGames1setDesafiado = 6;
                            j.qtddGames1setDesafiante = 1;
                            j.qtddGames2setDesafiado = 6;
                            j.qtddGames2setDesafiante = 1;
                            db.Entry(j).State = EntityState.Modified;
                            db.SaveChanges();
                            MontarProximoJogoTorneio(j);
                        }
                        break;
                    }
                    if (j.desafiante_id == 0)
                    {
                        j.desafiante_id = jogo.idDoPerdedor;
                        j.desafiante2_id = getParceiroDuplaProximoJogo(jogo, jogo.idDoPerdedor);
                        db.Entry(j).State = EntityState.Modified;
                        db.SaveChanges();
                        break;
                    }
                }
            }
        }

        private int? getParceiroDuplaProximoJogo(Jogo jogoAnterior, int idJogadorPrincipal)
        {
            if (jogoAnterior.desafiado_id == idJogadorPrincipal)
            {
                return jogoAnterior.desafiado2_id;
            }
            else if (jogoAnterior.desafiante_id == idJogadorPrincipal)
            {
                return jogoAnterior.desafiante2_id;
            }
            else
            {
                return null;
            }
        }

        private void cadastrarColocacaoPerdedorTorneio(Jogo jogo)
        {
            if (jogo.idDoPerdedor == 10)
            {
                return; // sai se for o bye;
            }
            // cadastrar a colocação do perdedor no torneio 
            var qtddFases = db.Jogo.Where(r => r.torneioId == jogo.torneioId && r.classeTorneio == jogo.classeTorneio && r.faseTorneio < 100)
                .Max(r => r.faseTorneio);
            int colocacao = 0;
            if ((jogo.faseTorneio == 100) || (jogo.faseTorneio == 101))
            { // fase repescagem
                colocacao = 100;
            }
            else
            {
                colocacao = (int)jogo.faseTorneio;
            }
            var inscricao = db.InscricaoTorneio.Where(i => i.userId == jogo.idDoPerdedor
                && i.torneioId == jogo.torneioId && i.classe == jogo.classeTorneio).ToList();
            if (inscricao.Count() > 0)
            {
                inscricao[0].colocacao = colocacao;
                Torneio torneio = db.Torneio.Where(t => t.Id == jogo.torneioId).Single();
                int pontuacao = CalculadoraDePontos.AddTipoTorneio(torneio.TipoTorneio).CalculaPontos(inscricao[0]);
                inscricao[0].Pontuacao = pontuacao;
                db.SaveChanges();
            }
            if ((jogo.desafiado2_id != null) && (jogo.desafiante2_id != null))
            {
                int idPerdedorDupla = 0;
                if (jogo.idDoPerdedor == jogo.desafiado_id)
                {
                    idPerdedorDupla = (int)jogo.desafiado2_id;
                }
                else if (jogo.idDoPerdedor == jogo.desafiante_id)
                {
                    idPerdedorDupla = (int)jogo.desafiante2_id;
                }
                cadastrarColocacaoPerdedorDupla(idPerdedorDupla, colocacao, (int)jogo.torneioId, (int)jogo.classeTorneio);
            }
        }

        private void cadastrarColocacaoPerdedorDupla(int idPerdedor, int colocacao, int torneioId, int classeTorneio)
        {
            var inscricao = db.InscricaoTorneio.Where(i => i.userId == idPerdedor
                && i.torneioId == torneioId && i.classe == classeTorneio && i.isAtivo).ToList();
            if (inscricao.Count() > 0)
            {
                inscricao[0].colocacao = colocacao;
                Torneio torneio = db.Torneio.Where(t => t.Id == torneioId).Single();
                int pontuacao = CalculadoraDePontos.AddTipoTorneio(torneio.TipoTorneio).CalculaPontos(inscricao[0]);
                inscricao[0].Pontuacao = pontuacao;
                db.SaveChanges();
            }
        }

        //////////////////////////////// FASE DE GRUPO //////////////////////////////////////////

        private ClassificacaoFaseGrupo getSaldoDeSetsEGamesFaseGrupo(InscricaoTorneio inscrito)
        {
            var jogos = db.Jogo.Where(j => j.classeTorneio == inscrito.classe && j.desafiante_id != 10 && j.grupoFaseGrupo != null &&
            (j.desafiado_id == inscrito.userId || j.desafiante_id == inscrito.userId)).ToList();
            var setsGanhos = 0;
            var setsPerdidos = 0;
            var gamesGanhos = 0;
            var gamesPerdidos = 0;
            foreach (var item in jogos)
            {
                if (item.desafiado_id == inscrito.userId)
                {
                    setsGanhos = setsGanhos + item.qtddSetsGanhosDesafiado;
                    setsPerdidos = setsPerdidos + item.qtddSetsGanhosDesafiante;
                    gamesGanhos = gamesGanhos + item.gamesGanhosDesafiado;
                    gamesPerdidos = gamesPerdidos + item.gamesGanhosDesafiante;
                }
                else
                {
                    setsGanhos = setsGanhos + item.qtddSetsGanhosDesafiante;
                    setsPerdidos = setsPerdidos + item.qtddSetsGanhosDesafiado;
                    gamesGanhos = gamesGanhos + item.gamesGanhosDesafiante;
                    gamesPerdidos = gamesPerdidos + item.gamesGanhosDesafiado;
                }
            }
            var classificadoFaseGrupo = new ClassificacaoFaseGrupo { inscricao = inscrito, userId = inscrito.userId, nome = inscrito.participante.nome, saldoSets = setsGanhos - setsPerdidos, saldoGames = gamesGanhos - gamesPerdidos };
            return classificadoFaseGrupo;
        }

        public List<ClassificacaoFaseGrupo> ordenarClassificacaoFaseGrupo(ClasseTorneio classe, int grupo)
        {
            var inscritosGrupo = db.InscricaoTorneio.Where(it => it.classe == classe.Id && it.grupo == grupo);
            List<InscricaoTorneio> classificacaoGrupo;
            if (classe.isDupla) {
                classificacaoGrupo = inscritosGrupo.Where(i => i.parceiroDuplaId != null && i.parceiroDuplaId != 0).
                    OrderByDescending(it => it.pontuacaoFaseGrupo).ThenBy(it => it.participante.nome).ToList();
            } else {
                classificacaoGrupo = inscritosGrupo.OrderByDescending(it => it.pontuacaoFaseGrupo).ThenBy(it => it.participante.nome).ToList();
            }
            var ordemClassificacao = new List<ClassificacaoFaseGrupo>();
            // percorre todos os inscritos em determinado grupo ordenado pela maior pontuação conquistada
            for (int i = 0; i < classificacaoGrupo.Count(); i++)
            {
                var isUltimo = i == classificacaoGrupo.Count() - 1;
                // caso seja a última verificação e ainda exista vaga, adiciona na lista de classificados
                //if (isUltimo) {
                //ordemClassificacao.Add(new ClassificacaoFaseGrupo { inscricao = inscritos[i], userId = inscritos[i].userId, nome= inscritos[i].participante.nome, saldoSets = 0, saldoGames = 0 });
                //  break;
                //}
                // 1ª verificação: caso jogador fez uma maior pontuação sozinho, adicionar na lista de classificados
                if ((isUltimo) || (classificacaoGrupo[i].pontuacaoFaseGrupo > classificacaoGrupo[i + 1].pontuacaoFaseGrupo))
                {
                    ordemClassificacao.Add(getSaldoDeSetsEGamesFaseGrupo(classificacaoGrupo[i]));
                    //ordemClassificacao.Add(new ClassificacaoFaseGrupo {inscricao=inscritos[i], userId = inscritos[i].userId, nome = inscritos[i].participante.nome, saldoSets = 0, saldoGames = 0 });
                    // 2ª verificação: caso não tenha feito maior pontuação sozinho:
                }
                else
                {
                    // 3ª verificação: verificar quantidade de empates com esta mesma pontuação:
                    var pontuacaoAtual = classificacaoGrupo[i].pontuacaoFaseGrupo;
                    var jogadoresEmpatados = classificacaoGrupo.Where(it => it.pontuacaoFaseGrupo == pontuacaoAtual).ToList<InscricaoTorneio>();
                    // 4ª verificação: caso tenha havido empate entre 2 jogadores apenas: Irá se classificar o vencedor do confronto direto
                    if (jogadoresEmpatados.Count == 2)
                    {
                        var jogador1 = getSaldoDeSetsEGamesFaseGrupo(classificacaoGrupo[i]);
                        var jogador2 = getSaldoDeSetsEGamesFaseGrupo(classificacaoGrupo[i + 1]);
                        var jogo = db.Jogo.Where(j => j.classeTorneio == classe.Id && (j.desafiado_id == jogador1.userId && j.desafiante_id == jogador2.userId) ||
                            ((j.desafiado_id == jogador2.userId && j.desafiante_id == jogador1.userId))).ToList();
                        if (jogo[0].idDoVencedor == jogador1.userId)
                        {
                            jogador1.confrontoDireto = 1;
                            ordemClassificacao.Add(jogador1);
                            ordemClassificacao.Add(jogador2);
                        }
                        else if (jogo[0].idDoVencedor == jogador2.userId)
                        {
                            jogador2.confrontoDireto = 1;
                            ordemClassificacao.Add(jogador2);
                            ordemClassificacao.Add(jogador1);
                        }
                        else
                        {
                            // caso o jogo ainda não tenha sido realizado
                            if ((jogador1.saldoSets > jogador2.saldoSets) || ((jogador1.saldoSets == jogador2.saldoSets) && (jogador1.saldoGames > jogador2.saldoGames)))
                            {
                                ordemClassificacao.Add(jogador1);
                                ordemClassificacao.Add(jogador2);
                            }
                            else
                            {
                                ordemClassificacao.Add(jogador2);
                                ordemClassificacao.Add(jogador1);
                            }
                        }
                        // como já foi incluído os 2 jogadores, já pula para o próximo:
                        i++;
                        // 5ª verificação: caso tenha havido empate entre 3 ou mais jogadores: A ordem levará em considereção o jogador com o maior saldo de sets/games    
                    }
                    else
                    {
                        List<ClassificacaoFaseGrupo> temp = new List<ClassificacaoFaseGrupo>();
                        foreach (var item in jogadoresEmpatados)
                        {
                            temp.Add(getSaldoDeSetsEGamesFaseGrupo(item));
                        }
                        temp = temp.OrderByDescending(oc => oc.saldoSets).ThenByDescending(oc => oc.saldoGames).ToList();
                        ordemClassificacao.AddRange(temp);
                        i = i + (jogadoresEmpatados.Count() - 1);
                    }
                }
            }
            return ordemClassificacao;
        }
    }
}