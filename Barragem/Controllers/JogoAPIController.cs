using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Description;
using Barragem.Context;
using Barragem.Models;
using Barragem.Class;
using System.Net.Http;

namespace Barragem.Controllers
{
    
    public class JogoAPIController : ApiController
    {
        private BarragemDbContext db = new BarragemDbContext();
        private RodadaNegocio rn = new RodadaNegocio();

        // GET: api/JogoAPI
        [Route("api/JogoAPI/ListarJogos/{classeId}")]
        public IList<JogoRodada> GetListarJogos(int classeId, int rankingId)
        {
            var rodadaId = db.Rodada.Where(r => r.isRodadaCarga == false && r.barragemId == rankingId).Max(r => r.Id);
            var jogos = db.Jogo.Where(j => j.rodada_id == rodadaId && j.desafiado.classeId == classeId).ToList<Jogo>();
            IList<JogoRodada> jogoRodada = new List<JogoRodada>();
            foreach (var jogo in jogos)
            {
                var j = new JogoRodada();
                j.Id = jogo.Id;
                j.nomeDesafiante = jogo.desafiante.nome;
                j.nomeDesafiado = jogo.desafiado.nome;
                j.dataJogo = jogo.dataJogo;
                j.horaJogo = jogo.horaJogo;
                j.localJogo = jogo.localJogo;
                j.fotoDesafiado = jogo.desafiado.fotoURL;
                j.fotoDesafiante = jogo.desafiante.fotoURL;
                j.qtddGames1setDesafiante = jogo.qtddGames1setDesafiante;
                j.qtddGames2setDesafiante = jogo.qtddGames2setDesafiante;
                j.qtddGames3setDesafiante = jogo.qtddGames3setDesafiante;
                j.qtddGames1setDesafiado = jogo.qtddGames1setDesafiado;
                j.qtddGames2setDesafiado = jogo.qtddGames2setDesafiado;
                j.qtddGames3setDesafiado = jogo.qtddGames3setDesafiado;
                j.idVencedor = jogo.idDoVencedor;
                jogoRodada.Add(j);
                
            }
            
            return jogoRodada;
        }

        [Route("api/JogoAPI/cabecalho/{userId}")]
        public Cabecalho GetCabecalho(int userId)
        {
            var user = db.UserProfiles.Find(userId);
            int barragemId = user.barragemId;
            var qtddRodada = 0;
            var nomeTemporada = "";
            var idRodada = 0;
            Rodada rodada = null;
            Cabecalho cabecalho = new Cabecalho();
            try
            {
                idRodada = db.Rodada.Where(r => r.isRodadaCarga == false && r.barragemId == barragemId).Max(r => r.Id);
                rodada = db.Rodada.Find(idRodada);
            }
            catch (Exception e) { }
            var classes = db.Classe.Where(c => c.barragemId == barragemId && c.ativa).OrderBy(c => c.nivel).ToList<Classe>();
            if (rodada != null)
            {
                cabecalho.rodada = "Rodada " + rodada.codigoSeq;
                if (rodada.temporadaId != null)
                {
                    qtddRodada = db.Rodada.Where(rd => rd.temporadaId == rodada.temporadaId && rd.Id <= rodada.Id
                    && rd.barragemId == rodada.barragemId).Count();
                    nomeTemporada = rodada.temporada.nome;
                    cabecalho.rodada = "Rodada " + qtddRodada + "/" + rodada.temporada.qtddRodadas;
                }
                cabecalho.dataRodada = rodada.dataFim;
            }
            cabecalho.temporada = nomeTemporada;
            cabecalho.classes = classes;
            cabecalho.classeUserId = (int)user.classeId;
            return cabecalho;
        }

        // GET: api/JogoAPI/5
        [ResponseType(typeof(MeuJogo))]
        public IHttpActionResult GetJogo(int id, int userId=0)
        {
            Jogo jogo = null;
            if (id == 0) {
                try{
                    jogo = db.Jogo.Where(u => (u.desafiado_id == userId || u.desafiante_id == userId) && u.torneioId == null)
                            .OrderByDescending(u => u.Id).Take(1).Single();
                }catch(Exception e) { }
            } else {
                jogo = db.Jogo.Find(id);
            }
            if (jogo == null){
                return InternalServerError(new Exception("Jogo não encontrado.")); 
            }
            MeuJogo meuJogo = montarMeuJogo(jogo, userId);
            
            return Ok(meuJogo);
        }
        

        private MeuJogo montarMeuJogo(Jogo jogo, int userId) {
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
            if (jogo.desafiado_id == userId)
            {
                meuJogo.linkWhatsapp = jogo.desafiante.linkwhatsapp;
            } else {
                meuJogo.linkWhatsapp = jogo.desafiado.linkwhatsapp;
            }
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
            headToHead.naturalidadeDesafiado = jogo.desafiado.naturalidade;
            headToHead.naturalidadeDesafiante = jogo.desafiante.naturalidade;
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

       
        [ResponseType(typeof(HeadToHead))]
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
            try
            {
                var jogo = db.Jogo.Find(id);
                if (jogo == null){
                    return InternalServerError(new Exception("Jogo não encontrado."));
                }
                jogo.dataJogo = dataJogo;
                jogo.horaJogo = horaJogo;
                jogo.localJogo = localJogo;
                if (jogo.situacao_Id != 4 && jogo.situacao_Id != 5){
                    jogo.situacao_Id = 2;
                }
                db.Entry(jogo).State = EntityState.Modified;
                db.SaveChanges();
            }
            catch (Exception e){
                return InternalServerError(e);
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