using Barragem.Context;
using Barragem.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;

namespace Barragem.Controllers
{
    public class TorneioAPIController : ApiController
    {
        private BarragemDbContext db = new BarragemDbContext();

        [HttpGet]
        [Route("api/TorneioAPI/{userId}")]
        public IList<TorneioApp> GetTorneio(int userId)
        {
            var dataHoje = DateTime.Now.AddDays(-5);
            var Torneios = (from inscricao in db.InscricaoTorneio
                           join torneio in db.Torneio on inscricao.torneioId equals torneio.Id into colocacaoJogador
                           where inscricao.userId == userId && inscricao.isAtivo && inscricao.torneio.isAtivo && inscricao.torneio.dataFim > dataHoje
                select new TorneioApp
                {
                    Id = inscricao.torneio.Id,
                    logoId = inscricao.torneio.barragemId,
                    nome = inscricao.torneio.nome,
                    dataInicio = inscricao.torneio.dataInicio,
                    dataFim = inscricao.torneio.dataFim,
                    cidade = inscricao.torneio.cidade,
                    premiacao = inscricao.torneio.premiacao,
                    contato = inscricao.torneio.barragem.email
                }).Distinct<TorneioApp>();
            var listTorneio = new List<TorneioApp>(Torneios);
            return listTorneio;
        }

        [Route("api/TorneioAPI/Inscritos/{torneioId}")]
        public IList<Inscrito> GetListarInscritos(int torneioId)
        {
            var torneio = db.Torneio.Find(torneioId);
            var liberarTabelaInscricao = torneio.liberaTabelaInscricao;
            if (!liberarTabelaInscricao)
            {
                throw new Exception(message: "Tabela de inscritos ainda não liberada.");
            }

            var inscricoes = db.InscricaoTorneio.Where(r => r.torneioId == torneioId && r.classeTorneio.isDupla == false).
                OrderBy(r => r.classe).ThenBy(r => r.participante.nome).ToList();
            var inscricoesDupla = db.InscricaoTorneio.Where(r => r.torneioId == torneioId && r.classeTorneio.isDupla == true).
                OrderBy(r => r.classe).ThenBy(r => r.participante.nome).ToList();

            List<InscricaoTorneio> inscricoesRemove = new List<InscricaoTorneio>();
            foreach (var ins in inscricoesDupla) {
                var formouDupla = inscricoesDupla.Where(i => i.parceiroDuplaId == ins.userId && i.classe == ins.classe).Count();
                if (formouDupla > 0) {
                    inscricoesRemove.Add(ins);
                }
            }
            foreach (var ins in inscricoesRemove){
                    inscricoesDupla.Remove(ins);
            }
            inscricoes.AddRange(inscricoesDupla);
            List<Inscrito> inscritos = new List<Inscrito>();
            foreach (var i in inscricoes)
            {
                var inscrito = new Inscrito();
                inscrito.nome = i.participante.nome;
                inscrito.classe = i.classeTorneio.nome;
                inscrito.foto = i.participante.fotoURL;
                if(i.parceiroDupla!=null) inscrito.nomeDupla = i.parceiroDupla.nome;
                inscritos.Add(inscrito);
            }
                        
            return inscritos;
        }

        [ResponseType(typeof(void))]
        [HttpPut]
        [Route("api/TorneioAPI/LancarResultado/{id}")]
        public IHttpActionResult PutLancarResultado(int id, int games1setDesafiante = 0, int games2setDesafiante = 0, int games3setDesafiante = 0, int games1setDesafiado = 0, int games2setDesafiado = 0, int games3setDesafiado = 0)
        {
            var jogo = db.Jogo.Find(id);

            jogo.qtddGames1setDesafiante = games1setDesafiante;
            jogo.qtddGames2setDesafiante = games2setDesafiante;
            jogo.qtddGames3setDesafiante = games3setDesafiante;

            jogo.qtddGames1setDesafiado = games1setDesafiado;
            jogo.qtddGames2setDesafiado = games2setDesafiado;
            jogo.qtddGames3setDesafiado = games3setDesafiado;
            jogo.usuarioInformResultado = ""; //User.Identity.Name; TODO: PEGAR O NOME DO USUÁRIO
            jogo.dataCadastroResultado = DateTime.Now;
            jogo.situacao_Id = 4;

            db.Entry(jogo).State = EntityState.Modified;

            try
            {

                db.SaveChanges();
                MontarProximoJogoTorneio(jogo);
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
            jogoAtual.usuarioInformResultado = ""; // TODO User.Identity.Name;
            jogoAtual.dataCadastroResultado = DateTime.Now;
            db.Entry(jogoAtual).State = EntityState.Modified;
            try
            {
                db.SaveChanges();
                MontarProximoJogoTorneio(jogoAtual);
            }
            catch (Exception e)
            {
                return InternalServerError(new Exception("Erro ao lançar resultado: " + e.Message));
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        [HttpGet]
        [Route("api/TorneioAPI/ListarClasses/{torneioId}")]
        public IList<ClasseTorneio> GetClasses(int torneioId)
        {
            var classes = db.ClasseTorneio.Where(c => c.torneioId == torneioId).OrderBy(c => c.nome).ToList<ClasseTorneio>();

            return classes;
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
                            j.usuarioInformResultado = User.Identity.Name;
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
            var inscricao = db.InscricaoTorneio.Where(i => i.userId == jogo.idDoPerdedor && i.torneioId == jogo.torneioId).ToList();
            if (inscricao.Count() > 0)
            {
                inscricao[0].colocacao = colocacao;
                db.SaveChanges();
            }
        }

        private void MontarProximoJogoTorneio(Jogo jogo)
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
                if (jogo.isPrimeiroJogoTorneio != null)
                {
                    isPrimeiroJogo = (bool)jogo.isPrimeiroJogoTorneio;
                }
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
                    }
                    else
                    {
                        proximoJogo.desafiante_id = jogo.idDoVencedor;
                        proximoJogo.desafiante2_id = getParceiroDuplaProximoJogo(jogo, jogo.idDoVencedor);
                    }
                    proximoJogo.cabecaChave = jogo.cabecaChave;
                    cadastrarColocacaoPerdedorTorneio(jogo);
                    db.SaveChanges();
                }
                else
                {
                    // indicar o vencedor do torneio
                    var inscricao = db.InscricaoTorneio.Where(i => i.userId == jogo.idDoVencedor && i.torneioId == jogo.torneioId).ToList();
                    if (inscricao.Count() > 0)
                    {
                        inscricao[0].colocacao = 0; // vencedor
                        db.SaveChanges();
                    }
                }
            }
        }

    }
}
