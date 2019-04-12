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

namespace Barragem.Controllers
{
    public class JogoAPIController : ApiController
    {
        private BarragemDbContext db = new BarragemDbContext();

        // GET: api/JogoAPI
        public IList<dynamic> GetJogo()
        {
            var barragemId = 1; // TODO pegar o id da barragem no claim
            var rodadaId = db.Rodada.Where(r => r.isRodadaCarga == false && r.barragemId == barragemId).Max(r => r.Id);
            var jogos = db.Jogo.Where(j => j.rodada_id == rodadaId).Select(jogo=> new { Id = jogo.Id,
                nomeDesafiante = jogo.desafiante.nome, nomeDasafiado = jogo.desafiado.nome }).ToList<dynamic>();
            
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

        [HttpGet]
        [Route("api/JogoAPI/ListarJogosPendentes")]
        // GET: api/JogoAPI/ListarJogosPendentes
        public IList<dynamic> ListarJogosPendentes(int userId)
        {
            var dataLimite = DateTime.Now.AddMonths(-10);
            var jogosPendentes = db.Jogo.Where(u => (u.desafiado_id == userId || u.desafiante_id == userId) && !u.rodada.isAberta
                && u.situacao_Id != 4 && u.situacao_Id != 5 && u.rodada.dataInicio > dataLimite && u.torneioId == null).OrderByDescending(u => u.Id).Take(3).Select(jogo => new {
                    Id = jogo.Id,
                    rodada = "Rodada " + jogo.rodada.codigo + " " + jogo.rodada.sequencial,
                    nomeDesafiante = jogo.desafiante.nome,
                    nomeDasafiado = jogo.desafiado.nome
                }).ToList<dynamic>(); 

            return jogosPendentes;
        }

        // PUT: api/JogoAPI/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PutJogo(int id, Jogo jogo)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != jogo.Id)
            {
                return BadRequest();
            }

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

        // PUT: api/JogoAPI/DefinirHorario
        [ResponseType(typeof(void))]
        public IHttpActionResult DefinirHorario(int id, DateTime dataJogo, string horaJogo="", string localJogo="")
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

        // POST: api/JogoAPI
        [ResponseType(typeof(Jogo))]
        public IHttpActionResult PostJogo(Jogo jogo)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.Jogo.Add(jogo);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = jogo.Id }, jogo);
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