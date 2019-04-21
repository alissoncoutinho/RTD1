using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using Barragem.Context;
using Barragem.Models;
using System.Runtime.Serialization;
using Barragem.Class;

namespace Barragem.Controllers
{
    public class JogoAPIController : ApiController
    {
        private BarragemDbContext db = new BarragemDbContext();
        private RodadaNegocio rn = new RodadaNegocio();

        // GET: api/JogoAPI
        public IList<JogoRodada> GetJogo()
        {
            var barragemId = 1; // TODO pegar o id da barragem no claim
            var rodadaId = db.Rodada.Where(r => r.isRodadaCarga == false && r.barragemId == barragemId).Max(r => r.Id);
            var jogos = db.Jogo.Where(j => j.rodada_id == rodadaId).Select(jogo=> new JogoRodada() {Id=jogo.Id, nomeDesafiante = jogo.desafiante.nome, nomeDesafiado = jogo.desafiado.nome }).ToList<JogoRodada>();
            
            return jogos;
        }

        // GET: api/JogoAPI/5
        [ResponseType(typeof(MeuJogo))]
        public IHttpActionResult GetJogo(int id)
        {
            Jogo jogo = db.Jogo.Find(id);
            if (jogo == null)
            {
                return NotFound();
            }
            MeuJogo meuJogo = montarMeuJogo(jogo);
            
            return Ok(meuJogo);
        }

        private MeuJogo montarMeuJogo(Jogo jogo) {
            var qtddRodada = 0;
            var nomeTemporada = "";
            MeuJogo meuJogo = new MeuJogo();
            meuJogo.Id = jogo.Id;
            if (jogo.rodada != null) { 
                meuJogo.rodada = "Rodada " + jogo.rodada.codigoSeq;
                if (jogo.rodada.temporadaId != null){
                    qtddRodada = db.Rodada.Where(rd => rd.temporadaId == jogo.rodada.temporadaId && rd.Id <= jogo.rodada_id
                    && rd.barragemId == jogo.rodada.barragemId).Count();
                    nomeTemporada = jogo.rodada.temporada.nome;
                    meuJogo.rodada = "Rodada " + qtddRodada + "/" + jogo.rodada.temporada.qtddRodadas;
                }
                meuJogo.dataFinalRodada = jogo.rodada.dataFim;
            }
            meuJogo.temporada = nomeTemporada;
            meuJogo.dataJogo = jogo.dataJogo;
            meuJogo.horaJogo = jogo.horaJogo;
            meuJogo.localJogo = jogo.localJogo;
            meuJogo.idDesafiante = jogo.desafiante_id;
            meuJogo.nomeDesafiante = jogo.desafiante.nome;
            meuJogo.fotoDesafiante = jogo.desafiante.fotoURL;
            meuJogo.posicaoDesafiante = 0;
            try {
                var r = db.Rancking.Where(rc => rc.userProfile_id == jogo.desafiante_id
                && rc.posicaoClasse != null).OrderByDescending(rc => rc.rodada_id).FirstOrDefault();
                meuJogo.posicaoDesafiante = (int)r.posicaoClasse;
            } catch (Exception e) { }
            
            meuJogo.idDesafiado = jogo.desafiado_id;
            meuJogo.nomeDesafiado = jogo.desafiado.nome;
            meuJogo.fotoDesafiado = jogo.desafiado.fotoURL;
            meuJogo.posicaoDesafiado = 0;
            try {
                var r2 = db.Rancking.Where(rc => rc.userProfile_id == jogo.desafiado_id
                && rc.posicaoClasse != null).OrderByDescending(rc => rc.rodada_id).FirstOrDefault();
                meuJogo.posicaoDesafiado = (int)r2.posicaoClasse;
            }
            catch (Exception e) { }
            meuJogo.qtddGames1setDesafiado = jogo.qtddGames1setDesafiado;
            meuJogo.qtddGames1setDesafiante = jogo.qtddGames1setDesafiante;
            meuJogo.qtddGames2setDesafiado = jogo.qtddGames2setDesafiado;
            meuJogo.qtddGames2setDesafiante = jogo.qtddGames2setDesafiante;
            meuJogo.qtddGames3setDesafiado = jogo.qtddGames3setDesafiado;
            meuJogo.qtddGames3setDesafiante = jogo.qtddGames3setDesafiante;
            meuJogo.situacao = jogo.situacao.descricao;
            meuJogo.idDoVencedor = jogo.idDoVencedor;
            return meuJogo;
        }

        private HeadToHead montarHead2Head(Jogo jogo)
        {
            var jogosHeadToHead = db.Jogo.Where(j => (j.desafiado_id == jogo.desafiado_id && j.desafiante_id == jogo.desafiante_id) ||
                (j.desafiante_id == jogo.desafiado_id && j.desafiado_id == jogo.desafiante_id)).ToList();

            HeadToHead headToHead = new HeadToHead();
            headToHead.Id = jogo.Id;

            headToHead.qtddVitoriasDesafiado = jogosHeadToHead.Where(j => j.idDoVencedor == jogo.desafiado_id).Count();
            headToHead.qtddVitoriasDesafiante = jogosHeadToHead.Where(j => j.idDoVencedor == jogo.desafiante_id).Count();

            headToHead.alturaDesafiado = jogo.desafiado.altura2;
            headToHead.alturaDesafiante = jogo.desafiante.altura2;
            headToHead.idadeDesafiado = jogo.desafiado.idade;
            headToHead.idadeDesafiante = jogo.desafiante.idade;
            headToHead.inicioRankingDesafiado = jogo.desafiado.dataInicioRancking.Month + "/" + jogo.desafiado.dataInicioRancking.Year;
            headToHead.inicioRankingDesafiante = jogo.desafiante.dataInicioRancking.Month + "/" + jogo.desafiante.dataInicioRancking.Year;
            headToHead.lateralDesafiado = jogo.desafiado.lateralidade;
            headToHead.lateralDesafiante = jogo.desafiante.lateralidade;
            try{
                var melhorRankingDesafiado = db.Rancking.Where(r => r.userProfile_id == jogo.desafiado_id && r.posicaoClasse != null && r.classeId != null).OrderBy(r => r.classe.nivel).ThenBy(r => r.posicaoClasse).Take(1).ToList();
                headToHead.melhorPosicaoDesafiado = melhorRankingDesafiado[0].posicaoClasse + "º/" + melhorRankingDesafiado[0].classe.nome;
            }catch (Exception e) { }
            try{
                var melhorRankingDesafiante = db.Rancking.Where(r => r.userProfile_id == jogo.desafiante_id && r.posicaoClasse != null && r.classeId != null).OrderBy(r => r.classe.nivel).ThenBy(r => r.posicaoClasse).Take(1).ToList();
                headToHead.melhorPosicaoDesafiante = melhorRankingDesafiante[0].posicaoClasse + "º/" + melhorRankingDesafiante[0].classe.nome;
            }catch (Exception e) { }
            
            return headToHead;
        }

        // GET: api/JogoAPI/5
        [ResponseType(typeof(MeuJogo))]
        [HttpGet]
        [Route("api/JogoAPI/GetHead2Head/{id}")]
        public IHttpActionResult GetHead2Head(int id)
        {
            Jogo jogo = db.Jogo.Find(id);
            if (jogo == null){
                return NotFound();
            }
            HeadToHead headToHead = montarHead2Head(jogo);

            return Ok(headToHead);
        }

        [HttpGet]
        [Route("api/JogoAPI/ListarJogosPendentes")]
        // GET: api/JogoAPI/ListarJogosPendentes
        public IList<JogoRodada> ListarJogosPendentes(int userId)
        {
            var dataLimite = DateTime.Now.AddMonths(-10);
            var jogosPendentes = db.Jogo.Where(u => (u.desafiado_id == userId || u.desafiante_id == userId) && !u.rodada.isAberta
                && u.situacao_Id != 4 && u.situacao_Id != 5 && u.rodada.dataInicio > dataLimite && u.torneioId == null).OrderByDescending(u => u.Id).Take(3).
                Select(jogo => new JogoRodada {
                    Id = jogo.Id,
                    nomeRodada = "Rodada " + jogo.rodada.codigo + jogo.rodada.sequencial,
                    nomeDesafiante = jogo.desafiante.nome,
                    nomeDesafiado = jogo.desafiado.nome
                }).ToList<JogoRodada>(); 

            return jogosPendentes;
        }

        
        [ResponseType(typeof(void))]
        [HttpPut]
        [Route("api/JogoAPI/DefinirHorario/{id}")]
        public IHttpActionResult PutDefinirHorario(int id, DateTime dataJogo, string horaJogo="", string localJogo="")
        {
            var jogo = db.Jogo.Find(id);

            jogo.dataJogo = dataJogo;
            jogo.horaJogo = horaJogo;
            jogo.localJogo = localJogo;

            db.Entry(jogo).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!JogoExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        [ResponseType(typeof(void))]
        [HttpPut]
        [Route("api/JogoAPI/LancarResultado/{id}")]
        public IHttpActionResult PutLancarResultado(int id, int games1setDesafiante, int games2setDesafiante, int games3setDesafiante, int games1setDesafiado, int games2setDesafiado, int games3setDesafiado)
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

            try{

                db.SaveChanges();
                rn.ProcessarJogoAtrasado(jogo);

            }
            catch (DbUpdateConcurrencyException){
                if (!JogoExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        [ResponseType(typeof(void))]
        [HttpPut]
        [Route("api/JogoAPI/LancarWO/{id}")]
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
            try{
                db.SaveChanges();
                rn.ProcessarJogoAtrasado(jogoAtual);
            }catch (DbUpdateConcurrencyException){
                if (!JogoExists(id)){
                    return NotFound();
                } else {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // DELETE: api/JogoAPI/5
        [ResponseType(typeof(Jogo))]
        public IHttpActionResult DeleteJogo(int id)
        {
            Jogo jogo = db.Jogo.Find(id);
            if (jogo == null)
            {
                return NotFound();
            }

            db.Jogo.Remove(jogo);
            db.SaveChanges();

            return Ok(jogo);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool JogoExists(int id)
        {
            return db.Jogo.Count(e => e.Id == id) > 0;
        }
    }
}