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
using System.Security.Claims;

namespace Barragem.Controllers
{
    [Authorize]
    public class JogoAPIController : ApiController
    {
        private BarragemDbContext db = new BarragemDbContext();
        private RodadaNegocio rn = new RodadaNegocio();
        private TorneioNegocio tn = new TorneioNegocio();

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
                j.idDesafiante = jogo.desafiante_id;
                j.idDesafiado = jogo.desafiado_id;
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
                j.idDoVencedor = jogo.idDoVencedor;
                j.situacao = jogo.situacao.descricao;
                if (jogo.desafiante_id == 10)
                {
                    j.situacao = "bye";
                }
                jogoRodada.Add(j);
                
            }
            
            return jogoRodada;
        }

        [Route("api/JogoAPI/cabecalho/{userId}")]
        public Cabecalho GetCabecalho(int userId)
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            //var teste = claimsIdentity.FindFirst("sub").Value;
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

        // GET: api/JogoAPI/5
        [HttpGet]
        [Route("api/JogoAPI/JogoTorneio/{userId}")]
        public IList<MeuJogo> GetJogoTorneio(int userId, int torneioId)
        {
            List<MeuJogo> jogosTorneio = new List<MeuJogo>();
            var classesUser = db.InscricaoTorneio.Where(c => c.torneioId == torneioId && c.isAtivo && c.userId == userId).Select(c=>c.classe).ToList();
            var torneio = db.Torneio.Find(torneioId);
            foreach(var i in classesUser)
            {
                    var jogos = db.Jogo.Where(u => u.classeTorneio == i && (u.situacao_Id==1 || u.situacao_Id == 2) && 
                        (u.desafiado_id == userId || u.desafiante_id == userId || u.desafiado2_id == userId || u.desafiante2_id == userId) &&
                        !(u.desafiado_id == 10 || u.desafiante_id == 10)).OrderBy(u => u.Id).ToList(); 
                
                foreach (var jogo in jogos){
                    try { 
                        MeuJogo meuJogo = montarMeuJogo(jogo, userId);
                        meuJogo.naoPodelancarResultado = torneio.jogadorNaoLancaResult;
                        jogosTorneio.Add(meuJogo);
                    }catch (Exception e) { }
                }
            }
            return jogosTorneio;
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
            }else if (jogo.classe != null)
            {
                meuJogo.rodada = jogo.classe.nome;
            }
            meuJogo.temporada = nomeTemporada;
            meuJogo.dataJogo = jogo.dataJogo;
            meuJogo.horaJogo = jogo.horaJogo;
            var quadra = "";
            if ((jogo.quadra != null) && (jogo.quadra != "100")){
                quadra = " quadra " + jogo.quadra;
            }
            var local = "";
            if (jogo.localJogo != null){
                local = jogo.localJogo;
            }
            meuJogo.localJogo = local + quadra;
            meuJogo.idDesafiante = jogo.desafiante_id;
            if (jogo.desafiante_id == 0)
            {
                meuJogo.nomeDesafiante = "Aguardando adversário";
                meuJogo.fotoDesafiante = null;
            } else {
                meuJogo.nomeDesafiante = jogo.desafiante.nome;
                meuJogo.fotoDesafiante = jogo.desafiante.fotoURL;
            }
            meuJogo.posicaoDesafiante = 0;
            meuJogo.idDesafianteDupla = jogo.desafiante2_id;
            if (jogo.desafiante2 != null){
                if (userId == jogo.desafiante2_id){
                    meuJogo.nomeDesafianteDupla = jogo.desafiante.nome;
                    meuJogo.fotoDesafianteDupla = jogo.desafiante.fotoURL;
                    meuJogo.nomeDesafiante = jogo.desafiante2.nome;
                    meuJogo.fotoDesafiante = jogo.desafiante2.fotoURL;
                }
                else{
                    meuJogo.nomeDesafianteDupla = jogo.desafiante2.nome;
                    meuJogo.fotoDesafianteDupla = jogo.desafiante2.fotoURL;
                }
            }
            try {
                var r = db.Rancking.Where(rc => rc.userProfile_id == jogo.desafiante_id
                && rc.posicaoClasse != null).OrderByDescending(rc => rc.rodada_id).FirstOrDefault();
                meuJogo.posicaoDesafiante = (int)r.posicaoClasse;
            } catch (Exception e) { }
            
            meuJogo.idDesafiado = jogo.desafiado_id;
            if (jogo.desafiado_id == 0)
            {
                meuJogo.nomeDesafiado = "Aguardando adversário";
                meuJogo.fotoDesafiado = null;
            }
            else
            {
                meuJogo.nomeDesafiado = jogo.desafiado.nome;
                meuJogo.fotoDesafiado = jogo.desafiado.fotoURL;
            }
            meuJogo.posicaoDesafiado = 0;
            meuJogo.idDesafiadoDupla = jogo.desafiado2_id;
            if (jogo.desafiado2 != null)
            {
                if (userId == jogo.desafiado2_id)
                {
                    meuJogo.nomeDesafiadoDupla = jogo.desafiado.nome;
                    meuJogo.fotoDesafiadoDupla = jogo.desafiado.fotoURL;
                    meuJogo.nomeDesafiado = jogo.desafiado2.nome;
                    meuJogo.fotoDesafiado = jogo.desafiado2.fotoURL;
                }
                else
                {
                    meuJogo.nomeDesafiadoDupla = jogo.desafiado2.nome;
                    meuJogo.fotoDesafiadoDupla = jogo.desafiado2.fotoURL;
                }
            }
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
            if (jogo.desafiante_id==10) {
                meuJogo.situacao = "bye";
            }
            meuJogo.idDoVencedor = jogo.idDoVencedor;
            if (jogo.desafiado_id == userId)
            {
                if (jogo.desafiante_id != 0)
                {
                    meuJogo.numeroWhatsapp = jogo.desafiante.numeroWhatsapp;
                    meuJogo.nomeWhatsapp = jogo.desafiante.nome;
                    meuJogo.linkWhatsapp = jogo.desafiante.linkwhatsapp;
                }
            } else {
                if (jogo.desafiado_id != 0)
                {
                    meuJogo.numeroWhatsapp = jogo.desafiado.numeroWhatsapp;
                    meuJogo.nomeWhatsapp = jogo.desafiado.nome;
                    meuJogo.linkWhatsapp = jogo.desafiado.linkwhatsapp;
                }
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

            headToHead.idDesafiado = jogo.desafiado_id;
            headToHead.idDesafiante = jogo.desafiante_id;
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
            var jogosPendentes = db.Jogo.Where(u => (u.desafiado_id == userId || u.desafiante_id == userId) 
                && u.situacao_Id != 4 && u.situacao_Id != 5 && u.rodada.dataInicio > dataLimite && u.torneioId == null).OrderByDescending(u => u.Id).Take(3).
                Select(jogo => new JogoRodada {
                    Id = jogo.Id,
                    nomeRodada = "Rodada " + jogo.rodada.codigo + jogo.rodada.sequencial,
                    nomeDesafiante = jogo.desafiante.nome,
                    nomeDesafiado = jogo.desafiado.nome,
                    fotoDesafiado = jogo.desafiado.fotoURL,
                    fotoDesafiante = jogo.desafiante.fotoURL
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
        public IHttpActionResult PutLancarResultado(int id, int games1setDesafiante=0, int games2setDesafiante=0, int games3setDesafiante=0, int games1setDesafiado=0, int games2setDesafiado=0, int games3setDesafiado=0){
            var jogo = db.Jogo.Find(id);

            jogo.qtddGames1setDesafiante = games1setDesafiante;
            jogo.qtddGames2setDesafiante = games2setDesafiante;
            jogo.qtddGames3setDesafiante = games3setDesafiante;

            jogo.qtddGames1setDesafiado = games1setDesafiado;
            jogo.qtddGames2setDesafiado = games2setDesafiado;
            jogo.qtddGames3setDesafiado = games3setDesafiado;
            if (jogo.qtddSetsGanhosDesafiado == jogo.qtddSetsGanhosDesafiante)
            {
                return InternalServerError(new Exception("Placar Inválido. Os sets ganhos estão iguais."));
            }
            var claimsIdentity = User.Identity as ClaimsIdentity;
            jogo.usuarioInformResultado = claimsIdentity.FindFirst("sub").Value; // TODO: PEGAR O NOME DO USUÁRIO
            jogo.dataCadastroResultado = DateTime.Now;
            jogo.situacao_Id = 4;

            db.Entry(jogo).State = EntityState.Modified;

            try{

                db.SaveChanges();
                tn.MontarProximoJogoTorneio(jogo);
                rn.ProcessarJogoAtrasado(jogo);
            }catch (Exception){
                return InternalServerError(new Exception("Erro ao lançar resultado."));
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
            var claimsIdentity = User.Identity as ClaimsIdentity;
            jogoAtual.usuarioInformResultado = claimsIdentity.FindFirst("sub").Value;  // TODO User.Identity.Name;
            jogoAtual.dataCadastroResultado = DateTime.Now;
            db.Entry(jogoAtual).State = EntityState.Modified;
            try{
                db.SaveChanges();
                tn.MontarProximoJogoTorneio(jogoAtual);
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