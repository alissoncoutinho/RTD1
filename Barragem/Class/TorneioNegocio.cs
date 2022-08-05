using Barragem.Context;
using Barragem.Controllers;
using Barragem.Helper;
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
                    cadastrarColocacaoPerdedorTorneio(jogo);

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
                        if ((jogo.desafiado2_id != null) && (jogo.desafiante2_id != null))
                        {
                            int idVencedorDupla = 0;
                            if (jogo.idDoVencedor == jogo.desafiado_id)
                            {
                                idVencedorDupla = (int)jogo.desafiado2_id;
                            }
                            else if (jogo.idDoVencedor == jogo.desafiante_id)
                            {
                                idVencedorDupla = (int)jogo.desafiante2_id;
                            }
                            cadastrarColocacaoDupla(idVencedorDupla, 0, (int)jogo.torneioId, (int)jogo.classeTorneio);
                        }
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
                cadastrarColocacaoDupla(idPerdedorDupla, colocacao, (int)jogo.torneioId, (int)jogo.classeTorneio);
            }
        }

        private void cadastrarColocacaoDupla(int userId, int colocacao, int torneioId, int classeTorneio, bool isModeloTodosContraTodos = false, int pontuacao = 0)
        {
            var inscricao = db.InscricaoTorneio.Where(i => i.userId == userId
                && i.torneioId == torneioId && i.classe == classeTorneio).ToList();
            if (inscricao.Count() > 0)
            {
                inscricao[0].colocacao = colocacao;
                Torneio torneio = db.Torneio.Where(t => t.Id == torneioId).Single();
                if (!isModeloTodosContraTodos)
                {
                    pontuacao = CalculadoraDePontos.AddTipoTorneio(torneio.TipoTorneio).CalculaPontos(inscricao[0]);
                }
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
            var nomeDupla = "";
            if (inscrito.parceiroDupla != null)
            {
                nomeDupla = inscrito.parceiroDupla.nome;
            }
            var classificadoFaseGrupo = new ClassificacaoFaseGrupo
            {
                inscricao = inscrito,
                userId = inscrito.userId,
                nome = inscrito.participante.nome,
                nomeDupla = nomeDupla,
                saldoSets = setsGanhos - setsPerdidos,
                saldoGames = gamesGanhos - gamesPerdidos,
                averageSets = 1000 * (float)Math.Round(averageSets, 3),
                averageGames = 1000 * (float)Math.Round(averageGames, 3)
            };
            return classificadoFaseGrupo;
        }

        private int calcularPontuacaoFaseGrupo(int userId, int classeId)
        {
            var jogos = db.Jogo.Where(j => j.classeTorneio == classeId && j.rodadaFaseGrupo != 0 && (j.situacao_Id == 4 || j.situacao_Id == 6) && (j.desafiado_id == userId || j.desafiante_id == userId)).ToList();
            var pontuacao = 0;
            foreach (var j in jogos)
            {
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
            return inscricoes.OrderByDescending(i => i.pontuacaoFaseGrupo).ToList();
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
                        if ((jogo.Count() > 0) && (jogo[0].idDoVencedor == jogador1.userId))
                        {
                            jogador1.confrontoDireto = 1;
                            ordemClassificacao.Add(jogador1);
                            ordemClassificacao.Add(jogador2);
                        }
                        else if ((jogo.Count() > 0) && (jogo[0].idDoVencedor == jogador2.userId))
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
                    }
                    else
                    {
                        // 5ª verificação: caso tenha havido empate entre 3 ou mais jogadores: A ordem levará em considereção o jogador com o maior saldo de sets/games    
                        List<ClassificacaoFaseGrupo> temp = new List<ClassificacaoFaseGrupo>();
                        foreach (var item in jogadoresEmpatados)
                        {
                            temp.Add(getDadosClassificatoriosFaseGrupo(item));
                        }
                        temp = temp.OrderByDescending(oc => oc.saldoSets).ThenByDescending(oc => oc.saldoGames).ThenByDescending(x => x.averageGames).ToList();
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
            var primeiraFase = jogo.faseTorneio ?? 0;
            var jogosRodada1 = db.Jogo.Where(j => j.classeTorneio == classeId && j.faseTorneio == primeiraFase).OrderBy(j => j.ordemJogo).ToList();
            return jogosRodada1;
        }

        public void pontuarEliminadosFaseGrupo(ClasseTorneio classe)
        {
            var desclassificados = getDesclassificadosEmCadaGrupo(classe);
            var torneio = db.Torneio.Find(classe.torneioId);
            foreach (var inscricao in desclassificados)
            {
                inscricao.colocacao = 100;
                inscricao.Pontuacao = CalculadoraDePontos.AddTipoTorneio(torneio.TipoTorneio).CalculaPontos(inscricao);
                db.Entry(inscricao).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        public void montarJogosRegraCBT(ClasseTorneio classe)
        {
            var classificados = getClassificadosEmCadaGrupo(classe);
            var montaJogosTorneioRegraCBT = InstanciaClassePorGrupo.getInstancia(getQtddDeGrupos(classe.Id));
            var jogosPrimeiraRodada = getJogosPrimeiraRodada(classe.Id);
            montaJogosTorneioRegraCBT.MontarJogosPrimeiraRodada(jogosPrimeiraRodada, classificados, db);
            return;
        }

        public List<InscricaoTorneio> getInscritosPorClasse(ClasseTorneio classe, bool incluirDesclassificadosFaseGrupo = false)
        {
            var pontuacaoFaseGrupo = 0;
            if (incluirDesclassificadosFaseGrupo)
            {
                pontuacaoFaseGrupo = -100;
            }
            // obs.: (filtro: it.pontuacaoFaseGrupo>=0) existe este filtro pois o jogador que receber um WO em algum jogo da fase de grupo será desclassificado e a indicação de desclassificação é: pontuacaoFaseGrupo = -100 (gambiarra, eu sei.)
            if (classe.isDupla)
            {
                List<InscricaoTorneio> inscritosAtualizada = new List<InscricaoTorneio>();
                var inscritosTorneio = db.InscricaoTorneio.Where(it => it.classe == classe.Id && it.isAtivo && it.parceiroDuplaId != null && it.parceiroDuplaId != 0 && it.pontuacaoFaseGrupo >= pontuacaoFaseGrupo).ToList();
                foreach (var item in inscritosTorneio)
                {
                    var inscricaoParceiroAtiva = db.InscricaoTorneio.Where(it => it.classe == classe.Id && it.isAtivo && it.userId == item.parceiroDuplaId).Count();
                    if (inscricaoParceiroAtiva != 0)
                    {
                        inscritosAtualizada.Add(item);
                    }
                }
                return inscritosAtualizada;
            }
            else
            {
                return db.InscricaoTorneio.Where(it => it.classe == classe.Id && it.isAtivo && it.pontuacaoFaseGrupo >= pontuacaoFaseGrupo).ToList();
            }
        }

        public List<InscricaoTorneio> getInscricoesSemDuplas(int classeId)
        {
            List<InscricaoTorneio> inscritosAtualizada = new List<InscricaoTorneio>();
            if (classeId == 0)
            {
                return inscritosAtualizada;
            }
            var inscritosTorneio = db.InscricaoTorneio.Where(it => it.classe == classeId && (it.parceiroDuplaId == null || it.parceiroDuplaId == 0)).OrderBy(it => it.participante.nome).ToList();
            foreach (var item in inscritosTorneio)
            {
                var temDupla = db.InscricaoTorneio.Where(it => it.classe == classeId && it.parceiroDuplaId == item.userId).Any();
                if (!temDupla)
                {
                    inscritosAtualizada.Add(item);
                }
            }
            return inscritosAtualizada;
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
            var inscritosNoGrupo = inscritos.Where(it => it.grupo == idGrupo && it.pontuacaoFaseGrupo >= 0).ToList();
            return inscritosNoGrupo;
        }

        private List<InscricaoTorneio> getInscritosSemGrupo(ClasseTorneio classeTorneio)
        {
            var inscritos = getInscritosPorClasse(classeTorneio);
            var inscritosSemGrupo = inscritos.Where(it => it.grupo == null || it.grupo == 0).ToList();
            return embaralhar(inscritosSemGrupo);
        }

        private List<InscricaoTorneio> embaralhar(List<InscricaoTorneio> list)
        {
            int n = list.Count;
            Random rnd = new Random();
            while (n > 1)
            {
                int k = (rnd.Next(0, n) % n);
                n--;
                InscricaoTorneio value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
            return list;
        }

        public int getQtddGruposFaseGrupos(int qtddInscritosPorClasse)
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

        public int ObterQtdeCabecasChaveMataMata(int qtdeInscritosClasse)
        {
            if (qtdeInscritosClasse <= 8) return 2;
            else if (qtdeInscritosClasse >= 9 && qtdeInscritosClasse <= 16) return 4;
            else if (qtdeInscritosClasse >= 17 && qtdeInscritosClasse <= 24) return 8;
            else if (qtdeInscritosClasse >= 25 && qtdeInscritosClasse <= 32) return 8;
            else if (qtdeInscritosClasse >= 33 && qtdeInscritosClasse <= 48) return 16;
            else if (qtdeInscritosClasse >= 49 && qtdeInscritosClasse <= 64) return 16;
            else if (qtdeInscritosClasse >= 65 && qtdeInscritosClasse <= 128) return 32;
            else if (qtdeInscritosClasse > 128) return 32;
            else return 0;
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
            foreach (var item in totalDeInscritos)
            {
                item.pontuacaoFaseGrupo = 0;
                if ((item.cabecaChave == null || item.cabecaChave == 100)) item.grupo = 0;
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
                        if (classe.isDupla)
                        {
                            var userIdDupla = inscritosSemGrupo[j].parceiroDuplaId;
                            var parceiroDupla = db.InscricaoTorneio.Where(i => i.userId == userIdDupla && i.isAtivo && i.classe == classe.Id).FirstOrDefault();
                            parceiroDupla.grupo = idGrupo;
                            db.Entry(parceiroDupla).State = EntityState.Modified;
                            db.SaveChanges();
                        }
                    }
                }
            }
            var inscritosRestantes = getInscritosSemGrupo(classe);
            // Os inscritos restantes serão distribuídos nos grupos já existentes
            var grupo = qtddDeGrupos;
            for (int i = 0; i < inscritosRestantes.Count(); i++)
            {
                inscritosRestantes[i].grupo = grupo;
                inscritosRestantes[i].pontuacaoFaseGrupo = 0;
                db.Entry(inscritosRestantes[i]).State = EntityState.Modified;
                if (classe.isDupla)
                {
                    var userIdDupla = inscritosRestantes[i].parceiroDuplaId;
                    var parceiroDupla = db.InscricaoTorneio.Where(ins => ins.userId == userIdDupla && ins.isAtivo && ins.classe == classe.Id).FirstOrDefault();
                    parceiroDupla.grupo = grupo;
                    db.Entry(parceiroDupla).State = EntityState.Modified;
                    db.SaveChanges();
                }
                db.SaveChanges();
                if (grupo > 0)
                {
                    grupo--;
                }
            }
            return qtddDeGrupos;
        }

        public List<ClassificadosEmCadaGrupo> getClassificadosEmCadaGrupo(ClasseTorneio classe)
        {
            List<ClassificadosEmCadaGrupo> listClassificadosEmCadaGrupo = new List<ClassificadosEmCadaGrupo>();

            var qtddGrupos = getQtddDeGrupos(classe.Id);

            var snapshotId = ObterSnapshotRankingMaisRecente(classe.torneioId, classe.Id);
            var ranking = ObterDadosRankingTorneioClasse(classe.torneioId, snapshotId, classe.Id);

            for (int gp = 1; gp <= qtddGrupos; gp++)
            {
                // Pega a ordem de classificação de cada grupo
                var classificacaoFaseGrupo = ordenarClassificacaoFaseGrupo(classe, gp);
                // mantem apenas os 2 primeiros de cada grupo

                bool ehDupla = classificacaoFaseGrupo[0].inscricao.parceiroDuplaId != null;

                var classificadosEmCadaGrupo = new ClassificadosEmCadaGrupo()
                {
                    userId = classificacaoFaseGrupo[0].userId,
                    nomeUser = classificacaoFaseGrupo[0].nome,
                    userIdParceiro = classificacaoFaseGrupo[0].inscricao.parceiroDuplaId,
                    nomeParceiro = ehDupla ? classificacaoFaseGrupo[0].inscricao.parceiroDupla.nome : "",
                    userId2oColocado = (classificacaoFaseGrupo.Count > 1) ? classificacaoFaseGrupo[1].userId : 10,
                    nome2oColocado = (classificacaoFaseGrupo.Count > 1) ? classificacaoFaseGrupo[1].nome : "bye",
                    userIdParceiro2oColocado = (classificacaoFaseGrupo.Count > 1) ? classificacaoFaseGrupo[1].inscricao.parceiroDuplaId : 10,
                    nomeParceiro2oColocado = ((classificacaoFaseGrupo.Count > 1) && (classificacaoFaseGrupo[1].inscricao.parceiroDuplaId != null)) ? classificacaoFaseGrupo[1].inscricao.parceiroDupla.nome : "",
                    grupo = (int)classificacaoFaseGrupo[0].inscricao.grupo,
                    saldoSets = classificacaoFaseGrupo[0].saldoSets,
                    saldoGames = classificacaoFaseGrupo[0].saldoGames,
                    pontuacao = classificacaoFaseGrupo[0].inscricao.pontuacaoFaseGrupo,
                    PontuacaoRanking = ranking.Where(x => x.UserId == classificacaoFaseGrupo[0].userId || (ehDupla && x.UserId == classificacaoFaseGrupo[0].inscricao.parceiroDuplaId)).Sum(s => s.Pontuacao),
                    averageSets = classificacaoFaseGrupo[0].averageSets,
                    averageGames = classificacaoFaseGrupo[0].averageGames
                };
                listClassificadosEmCadaGrupo.Add(classificadosEmCadaGrupo);
            }
            int qtddInscritos = getInscritosPorClasse(classe).Count();
            if (qtddInscritos % 3 == 0 || qtddInscritos % 4 == 0 || qtddGrupos == 1)
            {
                listClassificadosEmCadaGrupo = listClassificadosEmCadaGrupo.OrderByDescending(l => l.pontuacao).ThenByDescending(l => l.saldoSets).ThenByDescending(l => l.saldoGames).ThenByDescending(l => l.PontuacaoRanking).ToList();
            }
            else
            {
                listClassificadosEmCadaGrupo = listClassificadosEmCadaGrupo.OrderByDescending(l => l.averageSets).ThenByDescending(l => l.averageGames).ToList();
            }

            return listClassificadosEmCadaGrupo;
        }

        public List<InscricaoTorneio> getDesclassificadosEmCadaGrupo(ClasseTorneio classe)
        {
            var qtddGrupos = getQtddDeGrupos(classe.Id);
            List<InscricaoTorneio> listDesclassificadosEmCadaGrupo = new List<InscricaoTorneio>();
            for (int gp = 1; gp <= qtddGrupos; gp++)
            {
                // Pega a ordem de classificação de cada grupo
                var classificacaoFaseGrupo = ordenarClassificacaoFaseGrupo(classe, gp);
                var i = 1;
                foreach (var classificado in classificacaoFaseGrupo)
                {
                    if (i > 2)
                    {
                        if ((classificado.inscricao.parceiroDuplaId != null) || (classificado.inscricao.parceiroDuplaId != 0))
                        {
                            var inscricaoParceiro = db.InscricaoTorneio.Where(it => it.isAtivo && it.userId == classificado.inscricao.parceiroDuplaId && it.classe == classe.Id).ToList();
                            if (inscricaoParceiro.Count() > 0)
                            {
                                listDesclassificadosEmCadaGrupo.Add(inscricaoParceiro[0]);
                            }
                        }
                        listDesclassificadosEmCadaGrupo.Add(classificado.inscricao);
                    }
                    i++;
                }
            }
            return listDesclassificadosEmCadaGrupo;
        }

        private int getQtddDeGrupos(int classeId)
        {
            var classe = db.ClasseTorneio.Find(classeId);
            var qtddGrupos = 0;
            if (classe.isDupla)
            {
                qtddGrupos = (int)db.InscricaoTorneio.Where(it => it.classe == classeId && it.isAtivo && it.parceiroDuplaId != null && it.parceiroDuplaId != 0 && it.grupo < 100).Max(it => it.grupo);
            }
            else
            {
                qtddGrupos = (int)db.InscricaoTorneio.Where(it => it.classe == classeId && it.isAtivo && it.grupo < 100).Max(it => it.grupo);
            }

            return qtddGrupos;
        }

        private double calcularSetsAverage(List<Jogo> jogos, int userId)
        {
            //var jogos = db.Jogo.Where(j => j.classeTorneio == classe.Id && (j.desafiado_id == userId || j.desafiante_id == userId) && j.grupoFaseGrupo!=null).ToList();
            var setsJogados = 0;
            var setsGanhos = 0;
            foreach (var j in jogos)
            {
                setsJogados = setsJogados + j.setsJogados;
                setsGanhos = setsGanhos + ((j.desafiado_id == userId) ? j.qtddSetsGanhosDesafiado : j.qtddSetsGanhosDesafiante);
            }
            var setsAverage = setsGanhos / setsJogados;
            return setsAverage;
        }

        private double calcularGamesAverage(List<Jogo> jogos, int userId)
        {
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
                var listJogoCChave = db.JogoCabecaChave.Where(j => j.cabecaChave == numCabecaChave && j.chaveamento == chaveamento && j.temRepescagem == temRepescagem && j.isFaseGrupo == false).ToList();
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

        public void consolidarPontuacaoFaseGrupo(Jogo jogo)
        {
            if ((jogo.rodadaFaseGrupo != 0) && (jogo.torneioId != null))
            {
                var ehClasseSoGrupo = jogo.classe;
                Torneio torneio = db.Torneio.Include(t => t.barragem).Where(t => t.Id == jogo.torneioId).Single();
                if ((torneio.barragem.isModeloTodosContraTodos) || (ehClasseSoGrupo.faseGrupo && !ehClasseSoGrupo.faseMataMata))
                {
                    var existeAlgumjogoAindaEmAberto = db.Jogo.Where(j => j.grupoFaseGrupo != null && j.classeTorneio == jogo.classeTorneio && (j.situacao_Id == 1 || j.situacao_Id == 2)).Count();
                    // se não existir nenhum jogo da fase de grupo em aberto
                    if (existeAlgumjogoAindaEmAberto == 0)
                    {
                        int grupo = (int)jogo.grupoFaseGrupo;
                        var classificacaoFaseGrupo = ordenarClassificacaoFaseGrupo(ehClasseSoGrupo, grupo);
                        var colocacao = 0;
                        foreach (var item in classificacaoFaseGrupo)
                        {
                            var inscricao = db.InscricaoTorneio.Where(i => i.userId == item.userId && i.torneioId == jogo.torneioId && i.classe == jogo.classeTorneio).ToList();
                            if (inscricao.Count() > 0)
                            {
                                inscricao[0].colocacao = colocacao;
                                int pontuacao = item.inscricao.pontuacaoFaseGrupo;
                                if (!torneio.barragem.isModeloTodosContraTodos)
                                {
                                    pontuacao = CalculadoraDePontos.AddTipoTorneio(torneio.TipoTorneio).CalculaPontos(inscricao[0]);
                                }
                                inscricao[0].Pontuacao = pontuacao;
                                db.SaveChanges();
                                if ((inscricao[0].parceiroDuplaId != null) && (inscricao[0].parceiroDuplaId != null))
                                {
                                    cadastrarColocacaoDupla(inscricao[0].parceiroDupla.UserId, colocacao, (int)jogo.torneioId, (int)jogo.classeTorneio, torneio.barragem.isModeloTodosContraTodos, pontuacao);
                                }
                            }
                            colocacao++;
                        }

                        // caso já tenha havido consolidação, consolidar novamente:
                        try
                        {
                            var liga = db.TorneioLiga.Where(t => t.TorneioId == jogo.torneioId).First();
                            var existejogoEmAbertoNoTorneio = db.Jogo.Where(j => j.grupoFaseGrupo != null && j.torneioId == jogo.torneioId && (j.situacao_Id == 1 || j.situacao_Id == 2)).Count();
                            if ((torneio.barragem.isModeloTodosContraTodos && existejogoEmAbertoNoTorneio == 0) || liga.snapshotId != null)
                            {
                                new CalculadoraDePontos().GerarSnapshotDaLiga(jogo);
                            }
                        }
                        catch (Exception e) { }
                    }
                }
            }
        }

        public void montarJogosTodosContraTodos(ClasseTorneio classe)
        {
            db.Database.ExecuteSqlCommand("update inscricaoTorneio set grupo=1 where classe=" + classe.Id);
            var inscritos = getInscritosPorClasse(classe);
            if (inscritos.Count() % 2 != 0)
            {
                inscritos.Add(new InscricaoTorneio());
            }
            var listaDesafiante = inscritos.GetRange(0, inscritos.Count / 2);
            var listaDesafiado = inscritos.GetRange((inscritos.Count / 2), inscritos.Count / 2);
            string primeiroJogo = listaDesafiante[0].Id + "-" + listaDesafiado[0].Id;
            bool aindaFaltaJogos = true;
            bool isPrimeiraRodada = true;
            int rodada = 0;
            while (aindaFaltaJogos)
            {
                rodada++;
                for (int i = 0; i < listaDesafiante.Count(); i++)
                {
                    if (primeiroJogo == listaDesafiante[i].Id + "-" + listaDesafiado[i].Id && !isPrimeiraRodada)
                    {
                        aindaFaltaJogos = false;
                        break;
                    }
                    var j1 = listaDesafiante[i].userId == 0 ? 10 : listaDesafiante[i].userId;
                    var j2 = listaDesafiado[i].userId == 0 ? 10 : listaDesafiado[i].userId;
                    if (j1 == 10)
                    {
                        criarJogo(j2, j1, classe.torneioId, classe.Id, null, i + 1, rodada, 1, listaDesafiante[i].parceiroDuplaId, listaDesafiado[i].parceiroDuplaId);
                    }
                    else
                    {
                        criarJogo(j1, j2, classe.torneioId, classe.Id, null, i + 1, rodada, 1, listaDesafiante[i].parceiroDuplaId, listaDesafiado[i].parceiroDuplaId);
                    }

                }
                if (aindaFaltaJogos)
                {
                    isPrimeiraRodada = false;
                    listaDesafiante.Insert(1, listaDesafiado[0]);
                    listaDesafiado.RemoveAt(0);
                    listaDesafiado.Add(listaDesafiante[listaDesafiante.Count - 1]);
                    listaDesafiante.RemoveAt(listaDesafiante.Count - 1);
                }
            }

        }

        public List<ClasseTorneioQtddInscrito> qtddInscritosEmCadaClasse(List<ClasseTorneio> classes, int torneioId)
        {
            var listQtddInscritosClasseTorneio = new List<ClasseTorneioQtddInscrito>();
            foreach (var item in classes)
            {
                if (item.maximoInscritos > 0)
                {
                    var qtddInscritos = db.InscricaoTorneio.Where(i => i.torneio.Id == torneioId).GroupBy(i => i.classe);
                    foreach (var group in qtddInscritos)
                    {
                        var classeTorneioQtddInscrito = new ClasseTorneioQtddInscrito();
                        classeTorneioQtddInscrito.Id = group.Key;
                        classeTorneioQtddInscrito.qtddInscritos = group.Count();
                        listQtddInscritosClasseTorneio.Add(classeTorneioQtddInscrito);
                    }
                    break;
                }
            }
            return listQtddInscritosClasseTorneio;

        }

        private int ObterSnapshotRankingMaisRecente(int torneioId, int filtroClasse)
        {
            var circuitos = from torneio in db.Torneio
                            join ligaTorneio in db.TorneioLiga
                                on torneio.Id equals ligaTorneio.TorneioId
                            join snapshot in db.Snapshot
                                on ligaTorneio.LigaId equals snapshot.LigaId
                            join classeTorneio in db.ClasseTorneio
                                on torneio.Id equals classeTorneio.torneioId
                            join classeLiga in db.ClasseLiga
                                on new { categoriaId = (int)classeTorneio.categoriaId, ligaId = snapshot.LigaId } equals new { categoriaId = classeLiga.CategoriaId, ligaId = classeLiga.LigaId }
                            join liga in db.Liga
                                on snapshot.LigaId equals liga.Id
                            join snapshotRanking in db.SnapshotRanking
                                on new { snapshotId = snapshot.Id, categoriaId = classeLiga.CategoriaId } equals new { snapshotId = snapshotRanking.SnapshotId, categoriaId = snapshotRanking.CategoriaId }
                            where ligaTorneio.TorneioId == torneioId
                            where classeTorneio.Id == filtroClasse
                            orderby snapshot.Data descending
                            select snapshot.Id;

            if (circuitos == null)
                return 0;

            return circuitos.FirstOrDefault();
        }

        public List<SnapshotRanking> ObterDadosRankingTorneioClasse(int torneioId, int snapshotId, int filtroClasse)
        {
            if (snapshotId == 0)
                return new List<SnapshotRanking>();

            var rankingJogadores = from torneio in db.Torneio
                                   join ligaTorneio in db.TorneioLiga
                                       on torneio.Id equals ligaTorneio.TorneioId
                                   join snapshot in db.Snapshot
                                       on ligaTorneio.LigaId equals snapshot.LigaId
                                   join classeTorneio in db.ClasseTorneio
                                       on torneio.Id equals classeTorneio.torneioId
                                   join classeLiga in db.ClasseLiga
                                       on new { categoriaId = (int)classeTorneio.categoriaId, ligaId = snapshot.LigaId } equals new { categoriaId = classeLiga.CategoriaId, ligaId = classeLiga.LigaId }
                                   join snapshotRanking in db.SnapshotRanking
                                       on new { snapshotId = snapshot.Id, categoriaId = classeLiga.CategoriaId } equals new { snapshotId = snapshotRanking.SnapshotId, categoriaId = snapshotRanking.CategoriaId }
                                   where ligaTorneio.TorneioId == torneioId
                                   where classeTorneio.Id == filtroClasse
                                   where snapshot.Id == snapshotId
                                   orderby snapshotRanking.Posicao
                                   select snapshotRanking;

            if (rankingJogadores == null)
                return new List<SnapshotRanking>();

            return rankingJogadores.ToList();
        }

        public ResponseMessageWithStatus ValidarDisponibilidadeInscricoes(int torneioId, int categoriaId)
        {
            var respostaValidacao = new ResponseMessageWithStatus();
            try
            {
                var categoria = db.ClasseTorneio.Find(categoriaId);

                if (categoria.maximoInscritos == 0)
                {
                    respostaValidacao.status = "OK";
                }
                else
                {
                    var qtdInscritosCategoria = db.InscricaoTorneio.Count(x => x.torneio.Id == torneioId && x.classe == categoriaId);

                    if (categoria.isDupla)
                    {
                        var vagasRestantes = categoria.maximoInscritos - qtdInscritosCategoria;

                        var inscricoes = db.InscricaoTorneio.Where(x => x.torneioId == torneioId && x.classe == categoriaId);

                        var duplasFormadas = inscricoes.Where(x => x.parceiroDuplaId != null);
                        var idsParceirosDupla = duplasFormadas.Select(s => s.parceiroDuplaId);

                        var inscricoesSemParceiro = inscricoes.Where(x => x.parceiroDuplaId == null);
                        var duplasNaoFormadas = inscricoesSemParceiro.Where(x => !idsParceirosDupla.Contains(x.userId)).ToList();

                        var jogadoresAguardandoDupla = duplasNaoFormadas.Count;

                        if (vagasRestantes <= 0)
                        {
                            respostaValidacao.status = "ESGOTADO";
                        }
                        else if (jogadoresAguardandoDupla == 0)
                        {
                            respostaValidacao.status = "OK";
                        }
                        else
                        {
                            respostaValidacao.status = "ESCOLHER_DUPLA";
                            respostaValidacao.retorno = duplasNaoFormadas.Select(s => new FormacaoDuplaInscricao() { Id = s.Id, UserId = s.userId, Nome = s.participante.nome }).ToList();
                        }
                    }
                    else
                    {
                        if (categoria.maximoInscritos > qtdInscritosCategoria)
                        {
                            respostaValidacao.status = "OK";
                        }
                        else
                        {
                            respostaValidacao.status = "ESGOTADO";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                respostaValidacao = new ResponseMessageWithStatus { erro = ex.Message, status = "ERRO" };
            }
            return respostaValidacao;
        }

        public bool ValidarCriacaoDupla(int idInscricao, int userId, int torneioId, int classeId)
        {
            if (idInscricao == 0)
            {
                return true;
            }

            var inscricao = db.InscricaoTorneio.Find(idInscricao);

            var jaTemDupla = db.InscricaoTorneio.Any(i => i.torneioId == torneioId && i.classe == classeId && ((i.userId == userId && i.parceiroDuplaId != null) || (i.parceiroDuplaId == userId)));
            if (!jaTemDupla)
            {
                inscricao.parceiroDuplaId = userId;
                db.Entry(inscricao).State = EntityState.Modified;
                db.SaveChanges();
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}