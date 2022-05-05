using Barragem.Class;
using Barragem.Context;
using Barragem.Helper;
using Barragem.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http;
using System.Web.Http.Description;


namespace Barragem.Controllers
{
    public class TorneioAPIController : ApiController
    {
        private BarragemDbContext db = new BarragemDbContext();
        private TorneioNegocio tn = new TorneioNegocio();
        [HttpGet]
        [Route("api/TorneioAPI/{userId}")]
        public IList<TorneioApp> GetTorneio(int userId)
        {
            var dataHoje = DateTime.Now.AddDays(-5);
            List<Patrocinador> patrocinadores = null;
            var Torneios = (from inscricao in db.InscricaoTorneio
                            join torneio in db.Torneio on inscricao.torneioId equals torneio.Id into colocacaoJogador
                            where inscricao.userId == userId && inscricao.torneio.dataFim > dataHoje
                            select new TorneioApp
                            {
                                Id = inscricao.torneio.Id,
                                logoId = inscricao.torneio.barragemId,
                                nome = inscricao.torneio.nome,
                                dataInicio = inscricao.torneio.dataInicio,
                                dataFim = inscricao.torneio.dataFim,
                                dataFimInscricoes = inscricao.torneio.dataFimInscricoes,
                                cidade = inscricao.torneio.cidade,
                                premiacao = inscricao.torneio.premiacao,
                                contato = "",
                                pontuacaoLiga = inscricao.torneio.TipoTorneio,
                                inscricaoSoPeloSite = inscricao.torneio.inscricaoSoPeloSite,
                                isBeachTennis = inscricao.torneio.barragem.isBeachTenis,
                                temPIX = !String.IsNullOrEmpty(inscricao.torneio.barragem.tokenPagSeguro) ? true : false
                            }).Distinct<TorneioApp>().ToList();

            foreach (var item in Torneios)
            {
                item.contato = db.Torneio.Find(item.Id).contato;
                patrocinadores = new List<Patrocinador>();
                var patrocinador = db.Patrocinador.Where(p => p.torneioId == item.Id).ToList();
                foreach (var i in patrocinador)
                {
                    patrocinadores.Add(i);
                }
                item.patrocinadores = patrocinadores;
                try
                {
                    var torneioLiga = db.TorneioLiga.Include(l => l.Liga).Where(t => t.TorneioId == item.Id).FirstOrDefault();
                    item.nomeLiga = torneioLiga.Liga.Nome;
                }
                catch (Exception e) { }
            }
            return Torneios;
        }

        [HttpGet]
        [Route("api/TorneioAPI/PontuacaoLiga/{torneioId}")]
        public IList<PontuacaoLiga> GetPontuacaoLiga(int torneioId)
        {
            List<PontuacaoLiga> listaPontuacaoLiga = new List<PontuacaoLiga>();
            var torneio = db.Torneio.Find(torneioId);
            for (int i = 0; i < 7; i++)
            {
                var inscricao = new InscricaoTorneio();
                inscricao.colocacao = i;
                int pontuacao = CalculadoraDePontos.AddTipoTorneio(torneio.TipoTorneio).CalculaPontos(inscricao);
                var pontuacaoLiga = new PontuacaoLiga();
                pontuacaoLiga.pontuacao = pontuacao + "";
                pontuacaoLiga.descricao = getDescricaoColocacao(i);
                listaPontuacaoLiga.Add(pontuacaoLiga);
            }
            var insc = new InscricaoTorneio();
            insc.colocacao = 100;
            int p = CalculadoraDePontos.AddTipoTorneio(torneio.TipoTorneio).CalculaPontos(insc); //fase grupo
            var pLiga = new PontuacaoLiga();
            pLiga.pontuacao = p + "";
            pLiga.descricao = getDescricaoColocacao(100);
            listaPontuacaoLiga.Add(pLiga);
            // pontuação quando tiver apenas fase de grupo na classe:
            for (int i = 0; i < 5; i++)
            {
                var inscricao = new InscricaoTorneio();
                inscricao.colocacao = i;
                int pontuacao = CalculadoraDePontos.AddTipoTorneio(torneio.TipoTorneio).CalculaPontos(inscricao);
                var pontuacaoLiga = new PontuacaoLiga();
                pontuacaoLiga.pontuacao = pontuacao + "";
                pontuacaoLiga.descricao = (i + 1) + "º";
                listaPontuacaoLiga.Add(pontuacaoLiga);
            }

            return listaPontuacaoLiga;
        }

        private string getDescricaoColocacao(int? colocId)
        {
            if (colocId == null)
            {
                return "Sem informação";
            }
            else if (colocId == 0)
            {
                return "Campeão";
            }
            else if (colocId == 1)
            {
                return "Vice-Campeão";
            }
            else if (colocId == 2)
            {
                return "Semi-finais";
            }
            else if (colocId == 3)
            {
                return "Quartas de final";
            }
            else if (colocId == 4)
            {
                return "Oitavas de final";
            }
            else if (colocId == 5)
            {
                return "R2";
            }
            else if (colocId == 100)
            {
                return "Fase de grupos";
            }
            else
            {
                return "Primeira fase";
            }
        }

        [HttpGet]
        [Route("api/TorneioAPI/CriterioDesempateFaseGrupo")]
        public Regra GetCriterioDesempate()
        {
            return db.Regra.Find(1);
        }

        [HttpGet]
        [Route("api/TorneioAPI/CriterioDesempateConsolidacaoFaseGrupo")]
        public Regra GetCriterioDesempateConsolidacaoFaseGrupo()
        {
            return db.Regra.Find(2);
        }

        [HttpGet]
        [Route("api/TorneioAPI/Regulamento/{torneioId}")]
        public String GetRegulamento(int torneioId)
        {
            var regulamento = db.Torneio.Find(torneioId).regulamento;

            return regulamento;
        }


        [Route("api/TorneioAPI/Inscritos/{torneioId}")]
        public IList<Inscrito> GetListarInscritos(int torneioId, int userId = 0)
        {
            var torneio = db.Torneio.Find(torneioId);
            var liberarTabelaInscricao = torneio.liberaTabelaInscricao;
            //if (!liberarTabelaInscricao)
            //{
            //    throw new Exception(message: "Tabela de inscritos ainda não liberada.");
            //}

            var inscricoes = db.InscricaoTorneio.Where(r => r.torneioId == torneioId && r.classeTorneio.isDupla == false).
                OrderBy(r => r.classe).ThenBy(r => r.participante.nome).ToList();
            var inscricoesDupla = db.InscricaoTorneio.Where(r => r.torneioId == torneioId && r.classeTorneio.isDupla == true).
                OrderBy(r => r.classe).ThenBy(r => r.participante.nome).ToList();

            List<InscricaoTorneio> inscricoesRemove = new List<InscricaoTorneio>();
            foreach (var ins in inscricoesDupla)
            {
                var formouDupla = inscricoesDupla.Where(i => i.parceiroDuplaId == ins.userId && i.classe == ins.classe).Count();
                if (formouDupla > 0)
                {
                    inscricoesRemove.Add(ins);
                }
            }
            foreach (var ins in inscricoesRemove)
            {
                inscricoesDupla.Remove(ins);
            }
            if (inscricoesDupla.Count() > 0)
            {
                inscricoes.AddRange(inscricoesDupla);
                inscricoes = inscricoes.OrderBy(i => i.classe).ThenByDescending(i => i.parceiroDuplaId).ThenBy(i => i.participante.nome).ToList();
            }
            List<Inscrito> inscritos = new List<Inscrito>();
            foreach (var i in inscricoes)
            {
                var inscrito = new Inscrito();
                inscrito.userId = i.userId;
                inscrito.nome = i.participante.nome;
                inscrito.classe = i.classeTorneio.nome;
                inscrito.foto = i.participante.fotoURL;
                if (i.parceiroDupla != null)
                {
                    inscrito.fotoDupla = i.parceiroDupla.fotoURL;
                    inscrito.nomeDupla = i.parceiroDupla.nome;
                }
                inscritos.Add(inscrito);
            }

            // verificar se é para exibir o botão de montar dupla ou não. Se existir classes de dupla no torneio e o usuário estiver inscrito em uma classe de dupla exibe o botão.
            if (inscritos.Count > 0)
            {

                var usuarioLogado = 0;
                if (userId != 0)
                {
                    usuarioLogado = userId;
                }
                else
                {
                    usuarioLogado = getUsuarioLogado();
                }
                var inscricoesTorneio = db.InscricaoTorneio.Where(r => r.torneioId == torneioId && r.classeTorneio.isDupla == true && r.userId == usuarioLogado && r.parceiroDuplaId == null).ToList();
                var exibeBotaoFormarDupla = false;
                if (inscricoesTorneio.Count() > 0)
                {
                    foreach (var item in inscricoesTorneio)
                    {
                        var souParceidoDeAlguem = db.InscricaoTorneio.Where(r => r.classe == item.classe && r.parceiroDuplaId == usuarioLogado).Any();
                        if (!souParceidoDeAlguem)
                        {
                            exibeBotaoFormarDupla = true;
                        }
                    }
                }
                else
                {
                    exibeBotaoFormarDupla = false;
                }
                inscritos[0].exibeBotaoFormarDupla = exibeBotaoFormarDupla;
                inscritos[0].exibeInscritos = liberarTabelaInscricao;


            }

            return inscritos;
        }

        [ResponseType(typeof(void))]
        [HttpPut]
        [Route("api/TorneioAPI/LancarResultado/{id}")]
        public IHttpActionResult PutLancarResultado(int id, string games1setDesafiante = "0", string games2setDesafiante = "0", string games3setDesafiante = "0", string games1setDesafiado = "0", string games2setDesafiado = "0", string games3setDesafiado = "0")
        {
            var jogo = db.Jogo.Find(id);
            double num;
            int set1Desafiante = 0, set2Desafiante = 0, set3Desafiante = 0, set1Desafiado = 0, set2Desafiado = 0, set3Desafiado = 0;
            if (double.TryParse(games1setDesafiante, out num))
            {
                set1Desafiante = Convert.ToInt32(games1setDesafiante);
            }
            if (double.TryParse(games2setDesafiante, out num))
            {
                set2Desafiante = Convert.ToInt32(games2setDesafiante);
            }
            if (double.TryParse(games3setDesafiante, out num))
            {
                set3Desafiante = Convert.ToInt32(games3setDesafiante);
            }
            if (double.TryParse(games1setDesafiado, out num))
            {
                set1Desafiado = Convert.ToInt32(games1setDesafiado);
            }
            if (double.TryParse(games2setDesafiado, out num))
            {
                set2Desafiado = Convert.ToInt32(games2setDesafiado);
            }
            if (double.TryParse(games3setDesafiado, out num))
            {
                set3Desafiado = Convert.ToInt32(games3setDesafiado);
            }

            jogo.qtddGames1setDesafiante = set1Desafiante;
            jogo.qtddGames2setDesafiante = set2Desafiante;
            jogo.qtddGames3setDesafiante = set3Desafiante;

            jogo.qtddGames1setDesafiado = set1Desafiado;
            jogo.qtddGames2setDesafiado = set2Desafiado;
            jogo.qtddGames3setDesafiado = set3Desafiado;
            if (jogo.qtddSetsGanhosDesafiado == jogo.qtddSetsGanhosDesafiante)
            {
                return InternalServerError(new Exception("Placar Inválido. Os sets ganhos estão iguais."));
            }
            jogo.usuarioInformResultado = "app"; //User.Identity.Name; TODO: PEGAR O NOME DO USUÁRIO
            jogo.dataCadastroResultado = DateTime.Now;
            jogo.situacao_Id = 4;

            db.Entry(jogo).State = EntityState.Modified;

            try
            {

                db.SaveChanges();
                tn.MontarProximoJogoTorneio(jogo);
                tn.consolidarPontuacaoFaseGrupo(jogo);
            }
            catch (Exception)
            {
                return InternalServerError(new Exception("Erro ao lançar resultado."));
            }
            return StatusCode(HttpStatusCode.NoContent);
        }

        [ResponseType(typeof(void))]
        [HttpPut]
        [Route("api/TorneioAPI/LancarWO/{id}")]
        public IHttpActionResult PutLancarWO(int id, int userIdVencedor)
        {
            Jogo jogoAtual = db.Jogo.Find(id);
            //alterar quantidade de games para desafiado e desafiante
            int gamesDesafiante = 0;
            int gamesDesafiado = 0;
            if (jogoAtual.desafiado_id == userIdVencedor)
            {
                gamesDesafiado = 6;
                gamesDesafiante = 1;
            }
            else
            {
                gamesDesafiado = 1;
                gamesDesafiante = 6;
            }
            jogoAtual.qtddGames1setDesafiado = gamesDesafiado;
            jogoAtual.qtddGames1setDesafiante = gamesDesafiante;
            jogoAtual.qtddGames2setDesafiado = gamesDesafiado;
            jogoAtual.qtddGames2setDesafiante = gamesDesafiante;
            jogoAtual.qtddGames3setDesafiado = 0;
            jogoAtual.qtddGames3setDesafiante = 0;
            //alterar status do jogo WO
            jogoAtual.situacao_Id = 5;
            jogoAtual.usuarioInformResultado = "app"; // TODO User.Identity.Name;
            jogoAtual.dataCadastroResultado = DateTime.Now;
            db.Entry(jogoAtual).State = EntityState.Modified;
            try
            {
                db.SaveChanges();
                tn.MontarProximoJogoTorneio(jogoAtual);
                tn.consolidarPontuacaoFaseGrupo(jogoAtual);
            }
            catch (Exception e)
            {
                return InternalServerError(new Exception("Erro ao lançar resultado: " + e.Message));
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        private int getUsuarioLogado()
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var userId = 9058;
            if (claimsIdentity.FindFirst("sub") != null)
            {
                userId = Convert.ToInt32(claimsIdentity.FindFirst("sub").Value);
            }
            return userId;
        }

        [HttpGet]
        [Route("api/TorneioAPI/ClassificacaoFaseGrupo/{classeId}")]
        public List<ClassificadosEmCadaGrupo> GetClassificacaoFaseGrupo(int classeId)
        {
            var classe = db.ClasseTorneio.Find(classeId);
            return tn.getClassificadosEmCadaGrupo(classe);
        }

        [HttpGet]
        [Route("api/TorneioAPI/v2/Tabela/{userId}/{torneioId}")]
        public IHttpActionResult GetTabelaV2(int userId, int torneioId)
        {
            try
            {
                return Ok(ObterTabela(userId, torneioId));
            }
            catch (Exception ex)
            {
                var msgErro = $"TORNEIOAPI_V2 - {DateTime.Now} - Id Torneio: {torneioId} UserId: {userId} Mensagem: {ex.Message} StackTrace: {ex.StackTrace}";
                GravarLogErro(msgErro);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("api/TorneioAPI/Tabela/{torneioId}")]
        public IHttpActionResult GetTabela(int torneioId)
        {
            int userId = 0;
            try
            {
                userId = getUsuarioLogado();
                return Ok(ObterTabela(userId, torneioId));

            }
            catch (Exception ex)
            {
                var msgErro = $"TORNEIOAPI_V1 - {DateTime.Now} - Id Torneio: {torneioId} UserId: {userId} Mensagem: {ex.Message} StackTrace: {ex.StackTrace}";
                GravarLogErro(msgErro);
                return BadRequest(ex.Message);
            }
        }

        private void GravarLogErro(string msgErro)
        {
            if (msgErro.Length > 500) msgErro = msgErro.Substring(0, 500);
            db.Log.Add(new Log() { descricao = msgErro });
            db.SaveChanges();
        }

        private TabelaApp ObterTabela(int userId, int torneioId)
        {
            var torneio = db.Torneio.Find(torneioId);
            if (!torneio.liberarTabela)
            {
                throw new Exception(message: "Tabela ainda não liberada.");
            }
            var tabelaApp = new TabelaApp();

            var inscricaoUser = db.InscricaoTorneio.Where(c => c.torneioId == torneioId && c.isAtivo && c.userId == userId).ToList();

            if (inscricaoUser == null || inscricaoUser.Count == 0)
            {
                throw new Exception(message: "Usuário não possui inscrição no torneio");
            }

            var classeUser = inscricaoUser[0].classe;
            var grupoUser = inscricaoUser[0].grupo;
            var classes = db.ClasseTorneio.Where(c => c.torneioId == torneioId).Select(ct => new ClasseTorneioApp
            {
                nome = ct.nome,
                Id = ct.Id,
                faseGrupo = ct.faseGrupo,
                faseMataMata = ct.faseMataMata
            }).OrderBy(c => c.nome).ToList<ClasseTorneioApp>();

            foreach (var c in classes)
            {
                if (c.faseGrupo)
                {
                    var classeTorneio = db.ClasseTorneio.Find(c.Id);
                    var inscricaoTorneio = tn.getInscritosPorClasse(classeTorneio, true);
                    var qtddGrupo = inscricaoTorneio.Max(i => i.grupo);
                    c.qtddGruposFaseGrupo = qtddGrupo != null ? (int)qtddGrupo : 1;
                    var qtddInscritos = inscricaoTorneio.Count();
                    if (qtddInscritos % 2 == 0)
                    {
                        c.qtddRodadaFaseGrupo = (int)qtddInscritos - 1 / c.qtddGruposFaseGrupo;
                    }
                    else
                    {
                        c.qtddRodadaFaseGrupo = qtddInscritos / c.qtddGruposFaseGrupo;
                    }
                }
                if ((c.faseMataMata) || (!c.faseGrupo))
                {
                    c.faseMataMata = true;
                    var jgs = db.Jogo.Where(r => r.classeTorneio == c.Id && r.faseTorneio < 100 && r.faseTorneio != null).ToList();
                    if (jgs.Count() > 0)
                    {
                        c.qtddRodadaMataMata = (int)jgs.Max(r => r.faseTorneio);
                    }
                }
                if (inscricaoUser.Where(i => i.classe == c.Id).Count() > 0)
                {
                    if (classeUser == c.Id)
                    {
                        c.selected = true;
                    }
                    else
                    {
                        c.selected = false;
                    }
                    if (c.faseGrupo)
                    {
                        if ((classeUser == c.Id) && (grupoUser != null))
                        {
                            tabelaApp.classificacaoFaseGrupoApp = getClassificacaoFaseGrupoApp(c.Id, (int)grupoUser);
                        }
                        var inscricao = inscricaoUser.Where(i => i.classe == c.Id).FirstOrDefault();
                        c.grupoUser = (int)inscricao.grupo;
                    }
                }
                else
                {
                    c.selected = false;
                }
            }

            var jogos = montaFaseAtual(tabelaApp, classeUser, grupoUser, 0);

            var ListJogos = new List<MeuJogo>();
            foreach (var j in jogos)
            {
                ListJogos.Add(montaJogoTabela(j));
            }
            tabelaApp.classes = classes;
            tabelaApp.jogos = ListJogos;
            return tabelaApp;
        }

        private List<Jogo> montaFaseAtual(TabelaApp tabelaApp, int classeId, int? grupo, int numeroFaseAtual, string origemChamada = "")
        {
            var jogos = new List<Jogo>();
            if (origemChamada == "comboClasse")
            {
                // verificar se usuário logado está inscrito nesta classe e pega o grupo que ele pertence
                var userId = getUsuarioLogado();
                tabelaApp.userGrupo = getGrupoUsuario(classeId, userId);
                if (tabelaApp.userGrupo != 0) grupo = tabelaApp.userGrupo;
            }
            if (((origemChamada == "comboClasse") || (origemChamada == "comboGrupo")) && (grupo != 0))
            {
                tabelaApp.classificacaoFaseGrupoApp = getClassificacaoFaseGrupoApp(classeId, (int)grupo);
            }
            if ((grupo != null) && (grupo != 0))
            {
                if (numeroFaseAtual == 0)
                {
                    var jgs = db.Jogo.Where(c => c.classeTorneio == classeId && c.grupoFaseGrupo == grupo && (c.situacao_Id == 1 || c.situacao_Id == 2)).ToList();
                    if (jgs.Count() > 0)
                    {
                        numeroFaseAtual = jgs.Min(r => r.rodadaFaseGrupo);
                    }
                }
                if (numeroFaseAtual != 0)
                {
                    jogos = db.Jogo.Where(c => c.classeTorneio == classeId && c.grupoFaseGrupo == grupo && c.rodadaFaseGrupo == numeroFaseAtual).ToList();
                    tabelaApp.descricaoFase = "Rodada " + numeroFaseAtual;
                    tabelaApp.isFaseGrupo = true;
                }
                else
                {
                    var jgs = db.Jogo.Where(r => r.classeTorneio == classeId && r.faseTorneio < 100 && r.faseTorneio != null && (r.situacao_Id == 1 || r.situacao_Id == 2)).ToList();
                    if (jgs.Count() > 0)
                    {
                        numeroFaseAtual = (int)jgs.Max(r => r.faseTorneio);
                    }
                    if (numeroFaseAtual != 0)
                    {
                        jogos = db.Jogo.Where(c => c.classeTorneio == classeId && c.faseTorneio == numeroFaseAtual).ToList();
                        tabelaApp.descricaoFase = getDescricaoFaseTorneio((int)numeroFaseAtual);
                        tabelaApp.isFaseGrupo = false;
                    }
                    else
                    {
                        // caso não ache rodada aberta na fase de grupo e nem no mata-mata, coloca na primeira rodada da fase de grupo
                        numeroFaseAtual = 1;
                        jogos = db.Jogo.Where(c => c.classeTorneio == classeId && c.grupoFaseGrupo == grupo && c.rodadaFaseGrupo == numeroFaseAtual).ToList();
                        tabelaApp.descricaoFase = "Rodada " + numeroFaseAtual;
                        tabelaApp.isFaseGrupo = true;
                    }
                }
            }
            else
            {
                if (numeroFaseAtual == 0)
                {
                    if (origemChamada == "navegacaoEntreFases")
                    {
                        numeroFaseAtual = (int)db.Jogo.Where(r => r.classeTorneio == classeId && r.faseTorneio < 100 && r.faseTorneio != null).Max(r => r.faseTorneio);
                    }
                    else
                    {
                        var jgs = db.Jogo.Where(r => r.classeTorneio == classeId && r.faseTorneio < 100 && r.faseTorneio != null && (r.situacao_Id == 1 || r.situacao_Id == 2)).ToList();
                        if (jgs.Count() > 0)
                        {
                            numeroFaseAtual = (int)jgs.Max(r => r.faseTorneio);
                        }
                    }


                }
                if (numeroFaseAtual != 0)
                {
                    jogos = db.Jogo.Where(c => c.classeTorneio == classeId && c.faseTorneio == numeroFaseAtual).ToList();
                    tabelaApp.descricaoFase = getDescricaoFaseTorneio((int)numeroFaseAtual);
                    tabelaApp.isFaseGrupo = false;
                }
                else
                {
                    // caso não ache rodada aberta na fase de mata-mata, coloca na final
                    numeroFaseAtual = 1;
                    jogos = db.Jogo.Where(c => c.classeTorneio == classeId && c.faseTorneio == numeroFaseAtual).ToList();
                    tabelaApp.descricaoFase = getDescricaoFaseTorneio((int)numeroFaseAtual);
                    tabelaApp.isFaseGrupo = false;
                }
            }
            tabelaApp.faseTorneio = numeroFaseAtual;
            return jogos;
        }

        [Route("api/TorneioAPI/FaseTabela/{classeId}")]
        public TabelaApp GetFaseTabela(int classeId, int faseAtual = 0, string faseSolicitada = "", int grupo = 0, string origemChamada = "")
        {
            var tabelaApp = new TabelaApp();
            var jogos = new List<Jogo>();

            if (grupo != 0)
            {
                if (faseSolicitada == "P") faseAtual++;
                if (faseSolicitada == "A") faseAtual--;
            }
            else if (faseSolicitada == "P")
            {
                try
                {
                    faseAtual = (int)db.Jogo.Where(r => r.classeTorneio == classeId && r.faseTorneio < faseAtual).Max(r => r.faseTorneio);
                }
                catch (Exception e) { }
            }
            else if (faseSolicitada == "A")
            {
                try
                {
                    faseAtual = (int)db.Jogo.Where(r => r.classeTorneio == classeId && r.faseTorneio > faseAtual).Min(r => r.faseTorneio);
                }
                catch (Exception e)
                {
                    var classe = db.ClasseTorneio.Find(classeId);
                    if (classe.faseGrupo)
                    {
                        var userId = getUsuarioLogado();
                        grupo = getGrupoUsuario(classeId, userId);
                        grupo = grupo != 0 ? grupo : 1;
                        tabelaApp.userGrupo = grupo;
                        faseAtual = 3;
                        tabelaApp.classificacaoFaseGrupoApp = getClassificacaoFaseGrupoApp(classeId, (int)grupo);
                    }
                }
            }
            jogos = montaFaseAtual(tabelaApp, classeId, grupo, faseAtual, origemChamada);
            var ListJogos = new List<MeuJogo>();
            foreach (var j in jogos)
            {
                ListJogos.Add(montaJogoTabela(j));
            }
            tabelaApp.jogos = ListJogos;
            return tabelaApp;
        }

        private int getGrupoUsuario(int classeId, int userId)
        {
            var inscricao = db.InscricaoTorneio.Where(i => i.classe == classeId && i.isAtivo && i.userId == userId).ToList();
            if (inscricao.Count() > 0)
            {
                return inscricao[0].grupo != null ? (int)inscricao[0].grupo : 0;
            }
            else
            {
                return 0;
            }

        }

        private List<ClassificacaoFaseGrupoApp> getClassificacaoFaseGrupoApp(int classeId, int grupoUser)
        {
            try
            {
                var classificacaoFaseGrupo = tn.ordenarClassificacaoFaseGrupo(db.ClasseTorneio.Find(classeId), grupoUser);
                var classif = classificacaoFaseGrupo.Select(cfg => new ClassificacaoFaseGrupoApp
                {
                    userId = cfg.userId,
                    nome = cfg.nome,
                    nomeDupla = cfg.nomeDupla,
                    pontuacao = cfg.inscricao.pontuacaoFaseGrupo,
                    saldoSets = cfg.saldoSets,
                    saldoGames = cfg.saldoGames,
                    confrontoDireto = cfg.confrontoDireto
                }).ToList();
                return classif;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private MeuJogo montaJogoTabela(Jogo j)
        {
            var meuJogo = new MeuJogo();
            meuJogo.dataJogo = j.dataJogo;
            meuJogo.horaJogo = j.horaJogo;
            var quadra = "";
            if ((j.quadra != null) && (j.quadra != "100"))
            {
                quadra = " quadra " + j.quadra;
            }
            var local = "";
            if (j.localJogo != null)
            {
                local = j.localJogo;
            }
            meuJogo.localJogo = local + quadra;
            meuJogo.idDesafiante = j.desafiante_id;
            meuJogo.idDesafianteDupla = j.desafiante2_id;
            meuJogo.situacao = j.situacao.descricao;
            if (meuJogo.idDesafiante == 10)
            {
                meuJogo.nomeDesafiante = "bye";
                meuJogo.situacao = "bye";
            }
            else if (meuJogo.idDesafiante == 0)
            {
                meuJogo.nomeDesafiante = "Aguardando Adversário";
            }
            else
            {
                if ((j.cabecaChaveDesafiante != null) && (j.cabecaChaveDesafiante > 0) && (j.cabecaChaveDesafiante < 100))
                {
                    meuJogo.nomeDesafiante = "(" + j.cabecaChaveDesafiante + ")" + j.desafiante.nome;
                }
                else
                {
                    meuJogo.nomeDesafiante = j.desafiante.nome;
                }
                meuJogo.fotoDesafiante = j.desafiante.fotoURL;
                if (j.desafiante2 != null)
                {
                    meuJogo.nomeDesafianteDupla = j.desafiante2.nome;
                    meuJogo.fotoDesafianteDupla = j.desafiante2.fotoURL;
                }
            }
            meuJogo.idDesafiado = j.desafiado_id;
            meuJogo.idDesafiadoDupla = j.desafiado2_id;
            if (meuJogo.idDesafiado == 10)
            {
                meuJogo.nomeDesafiado = "bye";
            }
            else if (meuJogo.idDesafiado == 0)
            {
                meuJogo.nomeDesafiado = "Aguardando Adversário";
            }
            else
            {
                if ((j.cabecaChave != null) && (j.cabecaChave > 0) && (j.cabecaChave < 100))
                {
                    meuJogo.nomeDesafiado = "(" + j.cabecaChave + ")" + j.desafiado.nome;
                }
                else
                {
                    meuJogo.nomeDesafiado = j.desafiado.nome;
                }
                meuJogo.fotoDesafiado = j.desafiado.fotoURL;
                if (j.desafiado2 != null)
                {
                    meuJogo.nomeDesafiadoDupla = j.desafiado2.nome;
                    meuJogo.fotoDesafiadoDupla = j.desafiado2.fotoURL;
                }
            }
            meuJogo.qtddGames1setDesafiado = j.qtddGames1setDesafiado;
            meuJogo.qtddGames1setDesafiante = j.qtddGames1setDesafiante;
            meuJogo.qtddGames2setDesafiado = j.qtddGames2setDesafiado;
            meuJogo.qtddGames2setDesafiante = j.qtddGames2setDesafiante;
            meuJogo.qtddGames3setDesafiado = j.qtddGames3setDesafiado;
            meuJogo.qtddGames3setDesafiante = j.qtddGames3setDesafiante;
            meuJogo.idDoVencedor = j.idDoVencedor;
            return meuJogo;
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

        private string getDescricaoFaseTorneio(int faseTorneio)
        {
            if (faseTorneio == 6)
            {
                return "Rodada 1";
            }
            if (faseTorneio == 5)
            {
                return "Rodada 2";
            }
            if (faseTorneio == 4)
            {
                return "Oitavas de Final";
            }
            if (faseTorneio == 3)
            {
                return "Quartas de Final";
            }
            if (faseTorneio == 2)
            {
                return "Semi-Final";
            }
            if (faseTorneio == 1)
            {
                return "Final";
            }
            if (faseTorneio == 0)
            {
                return "Fase de Grupo";
            }
            return "";
        }

        [HttpGet]
        [Route("api/TorneioAPI/BuscaCidades/nome")]
        public IList<string> GetBuscaCidades(string nome)
        {
            List<string> cidades = new List<string>();
            if (nome.Length > 2)
            {
                var dataHoje = DateTime.Now.AddDays(-1);
                cidades = db.Torneio.Where(j => j.dataFim > dataHoje && j.isAtivo == true).OrderBy(j => j.cidade).Select(j => j.cidade).ToList<string>();
            }
            return cidades;
        }

        [HttpGet]
        [Route("api/TorneioAPI/cidade")]
        // GET: api/TorneioAPI/cidade
        public IList<TorneioApp> GetTorneioByCidade(string nome)
        {
            var dataHoje = DateTime.Now.AddDays(-1);
            var torneios = db.Torneio.Where(b => b.isAtivo == true && b.dataFim > dataHoje && b.cidade.ToLower() == nome.ToLower()).Select(rk => new TorneioApp()
            {
                Id = rk.Id,
                nome = rk.nome,
                dataFim = rk.dataFim,
            }).ToList();

            return torneios;
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("api/TorneioAPI/RegistroInscricao")]
        public MensagemRetorno PostRegistroInscricao(string login, string email, string password, string nome, DateTime dataNascimento, string naturalidade, string celular, string altura, string lateralidade, int classeInscricao2, bool isMaisDeUmaClasse, int rankingId, int classeId, int torneioId, bool isSocio, bool isClasseDupla)
        {
            var model = new RegisterInscricao();
            model.register.UserName = login;
            model.register.Password = password;
            model.register.nome = nome;
            model.register.dataNascimento = dataNascimento;
            model.register.altura2 = altura;
            model.register.telefoneCelular = celular;
            model.register.email = email;
            model.register.lateralidade = lateralidade;
            model.register.barragemId = rankingId;
            model.inscricao.torneioId = torneioId;
            model.inscricao.classe = classeId;
            model.isMaisDeUmaClasse = isMaisDeUmaClasse;
            model.classeInscricao2 = classeInscricao2;

            var mensagemRetorno = new AccountController().RegisterTorneioNegocio(model, torneioId, isSocio, isClasseDupla);
            return mensagemRetorno;
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("api/TorneioAPI/Disponivel/{rankingId}")]
        public IList<TorneioApp> GetTorneioDisponivel(int rankingId, int userId = 0)
        {

            var dataHoje = DateTime.Now.AddDays(-1);
            var cidade = "";
            if (rankingId == 1157)
            {
                cidade = db.UserProfiles.Find(userId).naturalidade;
            }
            else
            {
                cidade = db.BarragemView.Find(rankingId).cidade;
            }

            List<Patrocinador> patrocinadores = null;
            var torneio = (from t in db.Torneio
                           where t.dataFimInscricoes >= dataHoje && t.isAtivo && t.isOpen
                           select new TorneioApp
                           {
                               Id = t.Id,
                               logoId = t.barragemId,
                               nome = t.nome,
                               dataInicio = t.dataInicio,
                               valor = t.valor,
                               valorSocio = t.valorSocio,
                               dataFim = t.dataFim,
                               dataFimInscricoes = t.dataFimInscricoes,
                               cidade = t.cidade,
                               premiacao = t.premiacao,
                               contato = "",
                               pontuacaoLiga = t.TipoTorneio,
                               inscricaoSoPeloSite = t.inscricaoSoPeloSite,
                               isBeachTennis = t.barragem.isBeachTenis,
                               temPIX = !String.IsNullOrEmpty(t.barragem.tokenPagSeguro) ? true : false
                           }).Union(
                            from t in db.Torneio
                            where t.dataFimInscricoes >= dataHoje && t.isAtivo && t.divulgaCidade
                            && t.cidade.ToUpper() == cidade.ToUpper()
                            select new TorneioApp
                            {
                                Id = t.Id,
                                logoId = t.barragemId,
                                nome = t.nome,
                                dataInicio = t.dataInicio,
                                valor = t.valor,
                                valorSocio = t.valorSocio,
                                dataFim = t.dataFim,
                                dataFimInscricoes = t.dataFimInscricoes,
                                cidade = t.cidade,
                                premiacao = t.premiacao,
                                contato = "",
                                pontuacaoLiga = t.TipoTorneio,
                                inscricaoSoPeloSite = t.inscricaoSoPeloSite,
                                isBeachTennis = t.barragem.isBeachTenis,
                                temPIX = !String.IsNullOrEmpty(t.barragem.tokenPagSeguro) ? true : false
                            }).Union(
                            from t in db.Torneio
                            where t.dataFimInscricoes >= dataHoje && t.isAtivo && t.barragemId == rankingId
                            select new TorneioApp
                            {
                                Id = t.Id,
                                logoId = t.barragemId,
                                nome = t.nome,
                                dataInicio = t.dataInicio,
                                valor = t.valor,
                                valorSocio = t.valorSocio,
                                dataFim = t.dataFim,
                                dataFimInscricoes = t.dataFimInscricoes,
                                cidade = t.cidade,
                                premiacao = t.premiacao,
                                contato = "",
                                pontuacaoLiga = t.TipoTorneio,
                                inscricaoSoPeloSite = t.inscricaoSoPeloSite,
                                isBeachTennis = t.barragem.isBeachTenis,
                                temPIX = !String.IsNullOrEmpty(t.barragem.tokenPagSeguro) ? true : false
                            }).Distinct<TorneioApp>().ToList();

            foreach (var item in torneio)
            {
                item.contato = db.Torneio.Find(item.Id).contato;
                patrocinadores = new List<Patrocinador>();
                var patrocinador = db.Patrocinador.Where(p => p.torneioId == item.Id).ToList();
                foreach (var i in patrocinador)
                {
                    patrocinadores.Add(i);
                }
                item.patrocinadores = patrocinadores;
                try
                {
                    var torneioLiga = db.TorneioLiga.Include(l => l.Liga).Where(t => t.TorneioId == item.Id).FirstOrDefault();
                    item.nomeLiga = torneioLiga.Liga.Nome;
                }
                catch (Exception e) { }
            }

            return torneio;

        }

        [HttpPost]
        [AllowAnonymous]
        [Route("api/TorneioAPI/NotificacaoPagamentoOrganizador")]
        public IHttpActionResult NotificacaoPagamentoOrganizador(Cobranca cobranca)
        {
            var req = Request.Headers.Authorization;
            //if(req.Parameter != "<<token>>")
            //{
            //    return null;
            //}
            string[] refs = cobranca.reference_id.Split('-');
            if (refs[0].Equals("COB"))
            { // se for torneio
                int torneioId = Convert.ToInt32(refs[1]);
                var torneio = db.Torneio.Find(torneioId);
                if (cobranca.charges[0].status == "PAID")
                {
                    torneio.torneioFoiPago = true;
                    db.Entry(torneio).State = EntityState.Modified;
                    db.SaveChanges();
                }
                var log = new Log();
                log.descricao = "PIX:" + DateTime.Now + ": COB: " + torneioId + ":" + cobranca.charges[0].status;
                db.Log.Add(log);
                db.SaveChanges();

            }

            return Ok();
        }


    }
}
