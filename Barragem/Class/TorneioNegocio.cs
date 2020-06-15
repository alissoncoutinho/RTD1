﻿using Barragem.Context;
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
            if (jogo.rodadaFaseGrupo != 0)
            {
                return;
            }
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

        private ClassificacaoFaseGrupo getDadosClassificatoriosFaseGrupo(InscricaoTorneio inscrito)
        {
            var jogos = db.Jogo.Where(j => j.classeTorneio == inscrito.classe && j.desafiante_id != 10 && j.grupoFaseGrupo != null 
            && (j.situacao_Id == 4 || j.situacao_Id == 6) && (j.desafiado_id == inscrito.userId || j.desafiante_id == inscrito.userId)).ToList();
            var setsGanhos = 0;
            var setsPerdidos = 0;
            var gamesGanhos = 0;
            var gamesPerdidos = 0;
            float averageSets = 0.0f;
            float averageGames = 0.0f;
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
            averageSets = (setsGanhos + setsPerdidos) != 0 ? setsGanhos / (float)(setsGanhos + setsPerdidos) : 0;
            averageGames = (gamesGanhos + gamesPerdidos) != 0 ? gamesGanhos / (float)(gamesGanhos + gamesPerdidos) : 0;
            var classificadoFaseGrupo = new ClassificacaoFaseGrupo { inscricao = inscrito, userId = inscrito.userId, nome = inscrito.participante.nome,
                saldoSets = setsGanhos - setsPerdidos, saldoGames = gamesGanhos - gamesPerdidos,
                averageSets = (float)Math.Round(averageSets, 1), averageGames= (float)Math.Round(averageGames,1) };
            return classificadoFaseGrupo;
        }

        private int calcularPontuacaoFaseGrupo(int userId, int classeId)
        {
            var jogos = db.Jogo.Where(j => j.classeTorneio == classeId && j.rodadaFaseGrupo != 0 && (j.situacao_Id == 4 || j.situacao_Id == 6) && (j.desafiado_id == userId || j.desafiante_id == userId)).ToList();
            var pontuacao = 0;
            foreach (var j in jogos){
                if (j.idDoVencedor == userId) pontuacao++;
            }
            return pontuacao;
        }

        private List<InscricaoTorneio> inserirPontuacaoFaseGrupo(List<InscricaoTorneio> inscricoes)
        {
            foreach (var i in inscricoes)
            {
                i.pontuacaoFaseGrupo = calcularPontuacaoFaseGrupo(i.userId, i.classe);
            }
            return inscricoes.OrderByDescending(i=>i.pontuacaoFaseGrupo).ToList();
        }

        public List<ClassificacaoFaseGrupo> ordenarClassificacaoFaseGrupo(ClasseTorneio classe, int grupo)
        {
            var classificacaoGrupo = inserirPontuacaoFaseGrupo(getInscritosNoGrupo(classe, grupo));
            var ordemClassificacao = new List<ClassificacaoFaseGrupo>();
            // percorre todos os inscritos em determinado grupo ordenado pela maior pontuação conquistada
            for (int i = 0; i < classificacaoGrupo.Count(); i++)
            {
                var isUltimo = i == classificacaoGrupo.Count() - 1;
                // 1ª verificação: caso jogador fez uma maior pontuação sozinho, adicionar na lista de classificados
                if ((isUltimo) || (classificacaoGrupo[i].pontuacaoFaseGrupo > classificacaoGrupo[i + 1].pontuacaoFaseGrupo))
                {
                    ordemClassificacao.Add(getDadosClassificatoriosFaseGrupo(classificacaoGrupo[i]));
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
                        var jogador1 = getDadosClassificatoriosFaseGrupo(classificacaoGrupo[i]);
                        var jogador2 = getDadosClassificatoriosFaseGrupo(classificacaoGrupo[i + 1]);
                        var jogo = db.Jogo.Where(j => j.classeTorneio == classe.Id && ((j.desafiado_id == jogador1.userId && j.desafiante_id == jogador2.userId) ||
                            ((j.desafiado_id == jogador2.userId && j.desafiante_id == jogador1.userId)))).ToList();
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
                            temp.Add(getDadosClassificatoriosFaseGrupo(item));
                        }
                        temp = temp.OrderByDescending(oc => oc.saldoSets).ThenByDescending(oc => oc.saldoGames).ToList();
                        ordemClassificacao.AddRange(temp);
                        i = i + (jogadoresEmpatados.Count() - 1);
                    }
                }
            }
            return ordemClassificacao;
        }

        public List<Jogo> getJogosPrimeiraRodada(int classeId)
        {
            var jogo = db.Jogo.Where(j => j.classeTorneio == classeId).OrderByDescending(j => j.faseTorneio).First<Jogo>();
            var primeiraFase = (int)jogo.faseTorneio;
            var jogosRodada1 = db.Jogo.Where(j => j.classeTorneio == classeId && j.faseTorneio == primeiraFase).OrderBy(j => j.ordemJogo).ToList();
            return jogosRodada1;
        }

        public void montarJogosRegraCBT(ClasseTorneio classe)
        {
            var classificados = getClassificadosEmCadaGrupo(classe);
            var montaJogosTorneioRegraCBT = InstanciaClassePorGrupo.getInstancia(getQtddDeGrupos(classe.Id));
            var jogosPrimeiraRodada = getJogosPrimeiraRodada(classe.Id);
            montaJogosTorneioRegraCBT.MontarJogosPrimeiraRodada(jogosPrimeiraRodada, classificados, db);
            return;
        }

        public List<InscricaoTorneio> getInscritosPorClasse(ClasseTorneio classe, bool incluirDesclassificadosFaseGrupo=false)
        {
            var pontuacaoFaseGrupo = 0;
            if (incluirDesclassificadosFaseGrupo){
                pontuacaoFaseGrupo = -100;
            }
            // obs.: (filtro: it.pontuacaoFaseGrupo>=0) existe este filtro pois o jogador que receber um WO em algum jogo da fase de grupo será desclassificado e a indicação de desclassificação é: pontuacaoFaseGrupo = -100 (gambiarra, eu sei.)
            if (classe.isDupla)
            {
                return db.InscricaoTorneio.Where(it => it.classe == classe.Id && it.isAtivo && it.parceiroDuplaId != null && it.parceiroDuplaId != 0 && it.pontuacaoFaseGrupo>= pontuacaoFaseGrupo).ToList();
            }
            else
            {
                return db.InscricaoTorneio.Where(it => it.classe == classe.Id && it.isAtivo && it.pontuacaoFaseGrupo >= pontuacaoFaseGrupo).ToList();
            }
        }

        public void fecharJogosComBye(List<Jogo> jogosRodada1)
        {
            foreach (Jogo jogo in jogosRodada1)
            {
                if (jogo.desafiante_id == 10)
                {
                    jogo.dataCadastroResultado = DateTime.Now;
                    jogo.usuarioInformResultado = "sistema";
                    jogo.situacao_Id = 5;
                    jogo.qtddGames1setDesafiado = 6;
                    jogo.qtddGames1setDesafiante = 1;
                    jogo.qtddGames2setDesafiado = 6;
                    jogo.qtddGames2setDesafiante = 1;
                    db.Entry(jogo).State = EntityState.Modified;
                    db.SaveChanges();
                    MontarProximoJogoTorneio(jogo);
                }

            }
        }

        private List<InscricaoTorneio> getInscritosNoGrupo(ClasseTorneio classeTorneio, int idGrupo)
        {
            var inscritos = getInscritosPorClasse(classeTorneio);
            var inscritosNoGrupo = inscritos.Where(it => it.grupo == idGrupo && it.pontuacaoFaseGrupo>=0).ToList();
            return inscritosNoGrupo;
        }

        private List<InscricaoTorneio> getInscritosSemGrupo(ClasseTorneio classeTorneio)
        {
            var inscritos = getInscritosPorClasse(classeTorneio);
            var inscritosSemGrupo = inscritos.Where(it => it.grupo == null || it.grupo == 0).ToList();
            return inscritosSemGrupo;
        }

        private int getQtddGruposFaseGrupos(int qtddInscritosPorClasse)
        {
            if ((qtddInscritosPorClasse >= 3) && (qtddInscritosPorClasse <= 5))
            {
                return 1;
            }
            else if ((qtddInscritosPorClasse >= 6) && (qtddInscritosPorClasse <= 8))
            {
                return 2;
            }
            else if ((qtddInscritosPorClasse >= 9) && (qtddInscritosPorClasse <= 11))
            {
                return 3;
            }
            else if ((qtddInscritosPorClasse >= 12) && (qtddInscritosPorClasse <= 14))
            {
                return 4;
            }
            else if ((qtddInscritosPorClasse >= 15) && (qtddInscritosPorClasse <= 17))
            {
                return 5;
            }
            else if ((qtddInscritosPorClasse >= 18) && (qtddInscritosPorClasse <= 20))
            {
                return 6;
            }
            else if ((qtddInscritosPorClasse >= 21) && (qtddInscritosPorClasse <= 23))
            {
                return 7;
            }
            else if ((qtddInscritosPorClasse >= 24) && (qtddInscritosPorClasse <= 26))
            {
                return 8;
            }
            else if ((qtddInscritosPorClasse >= 27) && (qtddInscritosPorClasse <= 29))
            {
                return 9;
            }
            else if ((qtddInscritosPorClasse >= 30) && (qtddInscritosPorClasse <= 32))
            {
                return 10;
            }
            else if ((qtddInscritosPorClasse >= 33) && (qtddInscritosPorClasse <= 35))
            {
                return 11;
            }
            else if ((qtddInscritosPorClasse >= 36) && (qtddInscritosPorClasse <= 38))
            {
                return 12;
            }
            return 0;
        }

        public void MontarJogosFaseGrupo(ClasseTorneio classe)
        {
            var inscritos = getInscritosPorClasse(classe);
            var qtddGrupos = inscritos.Max(it => it.grupo);
            for (int i = 1; i <= qtddGrupos; i++)
            {
                var inscritosGrupo = inscritos.Where(it => it.grupo == i).ToList();
                criarRodadasJogosFaseGrupo(inscritosGrupo, classe.torneioId, classe.Id, i);
            }
        }

        private void criarRodadasJogosFaseGrupo(List<InscricaoTorneio> inscritos, int torneioId, int classeId, int grupo)
        {
            var qtddInscritos = inscritos.Count();
            if (qtddInscritos == 3 || qtddInscritos == 4)
            {
                var j1 = inscritos[0].participante.UserId;
                var j1Dupla = inscritos[0].parceiroDuplaId;
                var j2 = inscritos[1].participante.UserId;
                var j2Dupla = inscritos[1].parceiroDuplaId;
                var j3 = inscritos[2].participante.UserId;
                var j3Dupla = inscritos[2].parceiroDuplaId;
                var j4 = 10;
                int? j4Dupla = null;
                if (qtddInscritos == 4)
                {
                    j4 = inscritos[3].participante.UserId;
                    j4Dupla = inscritos[3].parceiroDuplaId;
                }

                criarJogo(j1, j2, torneioId, classeId, null, 1, 1, grupo, j1Dupla, j2Dupla);
                criarJogo(j3, j4, torneioId, classeId, null, 2, 1, grupo, j3Dupla, j4Dupla);

                criarJogo(j1, j3, torneioId, classeId, null, 1, 2, grupo, j1Dupla, j3Dupla);
                criarJogo(j2, j4, torneioId, classeId, null, 2, 2, grupo, j2Dupla, j4Dupla);

                criarJogo(j1, j4, torneioId, classeId, null, 1, 3, grupo, j1Dupla, j4Dupla);
                criarJogo(j2, j3, torneioId, classeId, null, 2, 3, grupo, j2Dupla, j3Dupla);
            }
            else if (qtddInscritos == 5 || qtddInscritos == 6)
            {
                var j1 = inscritos[0].participante.UserId;
                var j1Dupla = inscritos[0].parceiroDuplaId;
                var j2 = inscritos[1].participante.UserId;
                var j2Dupla = inscritos[1].parceiroDuplaId;
                var j3 = inscritos[2].participante.UserId;
                var j3Dupla = inscritos[2].parceiroDuplaId;
                var j4 = inscritos[3].participante.UserId;
                var j4Dupla = inscritos[3].parceiroDuplaId;
                var j5 = inscritos[4].participante.UserId;
                var j5Dupla = inscritos[4].parceiroDuplaId;
                var j6 = 10;
                int? j6Dupla = null;
                if (qtddInscritos == 6)
                {
                    j6 = inscritos[5].participante.UserId;
                    j6Dupla = inscritos[5].parceiroDuplaId;
                }

                criarJogo(j1, j2, torneioId, classeId, null, 1, 1, grupo, j1Dupla, j2Dupla);
                criarJogo(j3, j5, torneioId, classeId, null, 2, 1, grupo, j3Dupla, j5Dupla);
                criarJogo(j4, j6, torneioId, classeId, null, 3, 1, grupo, j4Dupla, j6Dupla);

                criarJogo(j1, j3, torneioId, classeId, null, 1, 2, grupo, j1Dupla, j3Dupla);
                criarJogo(j2, j4, torneioId, classeId, null, 2, 2, grupo, j2Dupla, j4Dupla);
                criarJogo(j5, j6, torneioId, classeId, null, 3, 2, grupo, j5Dupla, j6Dupla);

                criarJogo(j1, j4, torneioId, classeId, null, 1, 3, grupo, j1Dupla, j4Dupla);
                criarJogo(j2, j5, torneioId, classeId, null, 2, 3, grupo, j2Dupla, j5Dupla);
                criarJogo(j3, j6, torneioId, classeId, null, 3, 3, grupo, j3Dupla, j6Dupla);

                criarJogo(j1, j5, torneioId, classeId, null, 1, 4, grupo, j1Dupla, j5Dupla);
                criarJogo(j2, j6, torneioId, classeId, null, 2, 4, grupo, j2Dupla, j6Dupla);
                criarJogo(j3, j4, torneioId, classeId, null, 3, 4, grupo, j3Dupla, j4Dupla);

                criarJogo(j1, j6, torneioId, classeId, null, 1, 5, grupo, j1Dupla, j6Dupla);
                criarJogo(j2, j3, torneioId, classeId, null, 2, 5, grupo, j2Dupla, j3Dupla);
                criarJogo(j4, j5, torneioId, classeId, null, 3, 5, grupo, j4Dupla, j5Dupla);
            }

        }

        public int MontarGruposFaseGrupo(ClasseTorneio classe)
        {
            var totalDeInscritos = getInscritosPorClasse(classe, true);
            if (totalDeInscritos.Count() < 3)
            {
                return 0;
            }
            // zera a pontuação caso o sorteio esteja sendo feito novamente
            foreach (var item in totalDeInscritos.Where(t => t.grupo != 0).ToList())
            {
                item.pontuacaoFaseGrupo = 0;
                db.Entry(item).State = EntityState.Modified;
                db.SaveChanges();
            }
            var qtddDeGrupos = getQtddGruposFaseGrupos(totalDeInscritos.Count());
            for (int idGrupo = 1; idGrupo <= qtddDeGrupos; idGrupo++)
            {
                var inscritosNoGrupo = getInscritosNoGrupo(classe, idGrupo);
                var inscritosSemGrupo = getInscritosSemGrupo(classe);

                var vagasRestantesNoGrupo = 3 - inscritosNoGrupo.Count();
                // pode acontecer caso o organizador cadastre mais de 3 participantes no grupo ou esteja realizando o sorteio novamente
                if (vagasRestantesNoGrupo < 0)
                {
                    inscritosNoGrupo[2].grupo = 0;
                    db.Entry(inscritosNoGrupo[2]).State = EntityState.Modified;
                    db.SaveChanges();
                }
                else
                {
                    for (int j = 0; j < vagasRestantesNoGrupo; j++)
                    {
                        inscritosSemGrupo[j].grupo = idGrupo;
                        inscritosSemGrupo[j].pontuacaoFaseGrupo = 0;
                        db.Entry(inscritosSemGrupo[j]).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
            }
            var inscritosRestantes = getInscritosSemGrupo(classe);
            // Os inscritos restantes serão distribuídos nos grupos já existentes
            var grupo = 1;
            for (int i = 0; i < inscritosRestantes.Count(); i++)
            {
                inscritosRestantes[i].grupo = grupo;
                inscritosRestantes[i].pontuacaoFaseGrupo = 0;
                db.Entry(inscritosRestantes[i]).State = EntityState.Modified;
                db.SaveChanges();
                if (grupo < qtddDeGrupos)
                {
                    grupo++;
                }
            }
            return qtddDeGrupos;
        }

        public List<ClassificadosEmCadaGrupo> getClassificadosEmCadaGrupo(ClasseTorneio classe)
        {
            var qtddGrupos = getQtddDeGrupos(classe.Id);
            List<ClassificadosEmCadaGrupo> listClassificadosEmCadaGrupo = new List<ClassificadosEmCadaGrupo>();
            for (int gp = 1; gp <= qtddGrupos; gp++) {
                // Pega a ordem de classificação de cada grupo
                var classificacaoFaseGrupo = ordenarClassificacaoFaseGrupo(classe, gp);
                // mantem apenas os 2 primeiros de cada grupo
                var classificadosEmCadaGrupo = new ClassificadosEmCadaGrupo()
                {
                    userId = classificacaoFaseGrupo[0].userId,
                    nomeUser = classificacaoFaseGrupo[0].nome,
                    userIdParceiro = classificacaoFaseGrupo[0].inscricao.parceiroDuplaId,
                    nomeParceiro = (classificacaoFaseGrupo[0].inscricao.parceiroDuplaId!=null) ? classificacaoFaseGrupo[0].inscricao.parceiroDupla.nome : "" ,
                    userId2oColocado = classificacaoFaseGrupo[1].userId,
                    nome2oColocado = classificacaoFaseGrupo[1].nome,
                    userIdParceiro2oColocado = classificacaoFaseGrupo[1].inscricao.parceiroDuplaId,
                    nomeParceiro2oColocado = (classificacaoFaseGrupo[1].inscricao.parceiroDuplaId != null) ? classificacaoFaseGrupo[1].inscricao.parceiroDupla.nome : "",
                    grupo = (int)classificacaoFaseGrupo[0].inscricao.grupo,
                    saldoSets = classificacaoFaseGrupo[0].saldoSets,
                    saldoGames = classificacaoFaseGrupo[0].saldoGames,
                    pontuacao = classificacaoFaseGrupo[0].inscricao.pontuacaoFaseGrupo,
                    averageSets = classificacaoFaseGrupo[0].averageSets,
                    averageGames = classificacaoFaseGrupo[0].averageGames
                };
                listClassificadosEmCadaGrupo.Add(classificadosEmCadaGrupo);
            }
            int qtddInscritos = getInscritosPorClasse(classe).Count();
            if (qtddInscritos % 3 == 0 || qtddInscritos % 4 == 0){
                listClassificadosEmCadaGrupo = listClassificadosEmCadaGrupo.OrderByDescending(l => l.pontuacao).ThenByDescending(l => l.saldoSets).ThenByDescending(l => l.saldoGames).ToList();
            } else {
                listClassificadosEmCadaGrupo = listClassificadosEmCadaGrupo.OrderByDescending(l => l.averageSets).ThenByDescending(l => l.averageGames).ToList();
            }
            
            
            return listClassificadosEmCadaGrupo;
        }

        private int getQtddDeGrupos(int classeId)
        {
            var qtddGrupos = (int) db.InscricaoTorneio.Where(it => it.classe == classeId && it.isAtivo).Max(it => it.grupo);
            return qtddGrupos;
        }

        private double calcularSetsAverage(List<Jogo> jogos, int userId){
            //var jogos = db.Jogo.Where(j => j.classeTorneio == classe.Id && (j.desafiado_id == userId || j.desafiante_id == userId) && j.grupoFaseGrupo!=null).ToList();
            var setsJogados = 0;
            var setsGanhos = 0;
            foreach (var j in jogos){
                setsJogados = setsJogados + j.setsJogados;
                setsGanhos = setsGanhos + ((j.desafiado_id == userId) ? j.qtddSetsGanhosDesafiado : j.qtddSetsGanhosDesafiante);
            }
            var setsAverage = setsGanhos / setsJogados;
            return setsAverage;
        }

        private double calcularGamesAverage(List<Jogo> jogos, int userId){
            var gamesJogados = 0;
            var gamesGanhos = 0;
            foreach (var j in jogos)
            {
                gamesJogados = gamesJogados + (j.gamesGanhosDesafiado + j.gamesGanhosDesafiante);
                gamesGanhos = gamesGanhos + ((j.desafiado_id == userId) ? j.gamesGanhosDesafiado : j.gamesGanhosDesafiante);
            }
            var gamesAverage = gamesGanhos / gamesJogados;
            return gamesAverage;
        }

        private InscricaoTorneio selecionarAdversario(List<InscricaoTorneio> participantes)
        {
            var adversario = new InscricaoTorneio();
            if (participantes.Count == 0)
            {
                adversario.Id = 0;
                return adversario;
            }
            else if (participantes.Count == 1)
            {
                adversario = participantes[0]; //add it 
                participantes.RemoveAt(0);
                return adversario;
            }
            Random r = new Random();
            int randomIndex = r.Next(1, participantes.Count); //Choose a random object in the list
            adversario = participantes[randomIndex]; //add it 
            participantes.RemoveAt(randomIndex);
            return adversario;
        }

        public void montarJogosPorSorteio(List<Jogo> jogosRodada1, List<InscricaoTorneio> inscritos, bool temRepescagem)
        {
            InscricaoTorneio jogador = null;
            foreach (Jogo jogo in jogosRodada1)
            {
                if (jogo.desafiante_id == 0)
                {
                    jogador = selecionarAdversario(inscritos);
                    jogo.desafiante_id = jogador.userId;
                    jogo.isPrimeiroJogoTorneio = true;
                    if ((jogador.classeTorneio != null) && (jogador.classeTorneio.isDupla))
                    {
                        jogo.desafiante2_id = jogador.parceiroDuplaId;
                    }
                }
                if (jogo.desafiado_id == 0)
                {
                    jogador = selecionarAdversario(inscritos);
                    if (jogo.desafiante_id != 10)
                    {
                        jogo.isPrimeiroJogoTorneio = true;
                    }
                    jogo.desafiado_id = jogador.userId;
                    if ((jogador.classeTorneio != null) && (jogador.classeTorneio.isDupla))
                    {
                        jogo.desafiado2_id = jogador.parceiroDuplaId;
                    }
                }
                db.Entry(jogo).State = EntityState.Modified;
                db.SaveChanges();
            }
            if (temRepescagem)
            {
                // indicar os jogos que são de repescagem
                var jogosRepescagem = jogosRodada1.Where(j => j.desafiado_id == 0 || j.desafiante_id == 0).ToList();
                foreach (Jogo jogo in jogosRepescagem)
                {
                    jogo.isRepescagem = true;
                    db.Entry(jogo).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }
        }

        public void montarJogosComCabecaDeChave(List<Jogo> jogosRodada1, List<InscricaoTorneio> cabecasDeChave, bool temRepescagem)
        {
            var chaveamento = jogosRodada1.Count();
            var ordemJogo = 0;
            foreach (InscricaoTorneio cabecaDeChave in cabecasDeChave)
            {
                var numCabecaChave = cabecaDeChave.cabecaChave;
                var listJogoCChave = db.JogoCabecaChave.Where(j => j.cabecaChave == numCabecaChave && j.chaveamento == chaveamento && j.temRepescagem == temRepescagem && j.isFaseGrupo==false).ToList();
                if (listJogoCChave.Count() > 0)
                {
                    ordemJogo = listJogoCChave[0].ordemJogo;

                    var jogo = jogosRodada1.Where(j => j.ordemJogo == ordemJogo).ToList();
                    if (jogo.Count() > 0)
                    {
                        if (jogo[0].desafiante_id != 10)
                        {
                            jogo[0].isPrimeiroJogoTorneio = true;
                        }
                        jogo[0].desafiado_id = cabecaDeChave.userId;
                        jogo[0].desafiado2_id = cabecaDeChave.parceiroDuplaId;
                        jogo[0].cabecaChave = numCabecaChave;
                        db.Entry(jogo[0]).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
            }
        }

        public void montarJogosComByes(List<Jogo> jogosRodada1, int qtddByes, bool temRepescagem, int qtddIncritos, bool isFaseGrupo = false)
        {
            var qtddJogos = jogosRodada1.Count();
            bool umaVez = true;
            for (int i = 1; i <= qtddByes; i++)
            {
                var listJogoCChave = db.JogoCabecaChave.Where(j => j.cabecaChave == i && j.chaveamento == qtddJogos && j.temRepescagem == temRepescagem && j.isFaseGrupo == isFaseGrupo).ToList();
                Jogo jogo = null;
                if ((listJogoCChave.Count() > 0) && (!temRepescagem))
                {
                    var ordemJogo = listJogoCChave[0].ordemJogo;
                    jogo = jogosRodada1.Where(j => j.ordemJogo == ordemJogo).FirstOrDefault();
                }
                else
                {
                    if (((qtddIncritos / 2) % 2 != 0) && (umaVez))
                    {
                        umaVez = false;
                        jogo = jogosRodada1.Where(j => j.desafiante_id == 0).OrderByDescending(j => j.ordemJogo).FirstOrDefault();
                    }
                    else
                    {
                        jogo = jogosRodada1.Where(j => j.desafiante_id == 0).OrderBy(j => j.ordemJogo).FirstOrDefault();
                    }
                }
                jogo.desafiante_id = 10;
                db.Entry(jogo).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        public void criarJogo(int jogador1, int jogador2, int torneioId, int classeTorneio, int? faseTorneio,
            int ordemJogo, int rodadaFaseGrupo = 0, int? grupo = null, int? jogador1Dupla = null, int? jogador2Dupla = null)
        {
            Jogo jogo = new Jogo();
            jogo.desafiado_id = jogador1;
            jogo.desafiado2_id = jogador1Dupla;
            jogo.desafiante_id = jogador2;
            jogo.desafiante2_id = jogador2Dupla;
            jogo.torneioId = torneioId;
            jogo.situacao_Id = 1;
            jogo.classeTorneio = classeTorneio;
            jogo.faseTorneio = faseTorneio;
            jogo.rodadaFaseGrupo = rodadaFaseGrupo;
            jogo.ordemJogo = ordemJogo;
            jogo.grupoFaseGrupo = grupo;
            if ((jogador2 == 10) && (jogador1 != 0))
            {
                jogo.situacao_Id = 5;
                jogo.qtddGames1setDesafiado = 6;
                jogo.qtddGames2setDesafiado = 6;
                jogo.qtddGames1setDesafiante = 1;
                jogo.qtddGames2setDesafiante = 1;

            }
            db.Jogo.Add(jogo);
            db.SaveChanges();
        }


    }
}