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
    
    public class RankingAPIController : ApiController
    {
        private BarragemDbContext db = new BarragemDbContext();
        [HttpGet]
        [Route("api/RankingAPI/userEmail")]
        // GET: api/RankingAPI/userEmail
        public IList<LoginRankingModel> GetRankingsByUserEmail(string email)
        {
            List<LoginRankingModel> loginRankings = new List<LoginRankingModel>();
            var users = db.UserProfiles.Where(u => u.email.ToLower() == email.Trim().ToLower()).ToList();
            if (users.Count() == 0){
                return loginRankings;
                //throw (new Exception("Não foi encontrado ranking com este email."));
            }
            
            foreach (var item in users)
            {
                var ranking = new LoginRankingModel();
                ranking.idRanking = item.barragemId;
                ranking.nomeRanking = item.barragem.nome;
                ranking.userName = item.UserName;
                ranking.userId = item.UserId;
                loginRankings.Add(ranking);
            }
            return loginRankings;
        }
        
        // GET: api/RankingAPI/
        [Route("api/RankingAPI/{classeId}")]
        public IList<Classificacao> GetRanking(int classeId){
            //List<RanckingView> rancking;
            int barragemId = 1; // TODO: get barragem usuario
            var idRodada = 0;
            UserProfile usuario = null;
            try
            {
                //usuario = db.UserProfiles.Find(2030); // TODO : GET USERID WebSecurity.GetUserId(User.Identity.Name)
                idRodada = db.Rancking.Where(r => r.rodada.isAberta == false && r.rodada.isRodadaCarga == false && r.rodada.barragemId == barragemId).Max(r => r.rodada_id);
            }
            catch (InvalidOperationException)
            {

            }
            var rancking = db.Rancking.Include(r => r.userProfile).Include(r => r.rodada).
                Where(r => r.rodada_id == idRodada && r.posicao > 0 && r.posicaoClasse != null && r.userProfile.situacao != "desativado" && r.userProfile.situacao != "inativo" && r.classe.Id == classeId).
                OrderBy(r => r.classe.nivel).ThenBy(r => r.posicaoClasse).Select(rk => new Classificacao()
                {
                    nomeUser = rk.userProfile.nome,
                    posicaoUser = (int)rk.posicaoClasse,
                    pontuacao = rk.totalAcumulado,
                    foto = rk.userProfile.fotoURL
                }).ToList<Classificacao>();
            
            return rancking;
        }

        [Route("api/RankingAPI/cabecalho/{userId}")]
        public Cabecalho GetCabecalho(int userId){
            var user = db.UserProfiles.Find(userId);
            int barragemId = user.barragemId;
            var qtddRodada = 0;
            var nomeTemporada = "";
            var idRodada = 0;
            Rodada rodada = null;
            Cabecalho rankingCabecalho = new Cabecalho();
            try
            {
                idRodada = db.Rodada.Where(r => r.isAberta == false && r.isRodadaCarga == false && r.barragemId == barragemId).Max(r => r.Id);
                rodada = db.Rodada.Find(idRodada);
            } catch(Exception e) { }
            var classes = db.Classe.Where(c => c.barragemId == barragemId && c.ativa).OrderBy(c => c.nivel).ToList<Classe>(); 
            if (rodada != null) {
                rankingCabecalho.rodada = "Rodada " + rodada.codigoSeq;
                if (rodada.temporadaId != null){
                    qtddRodada = db.Rodada.Where(rd => rd.temporadaId == rodada.temporadaId && rd.Id <= rodada.Id
                    && rd.barragemId == rodada.barragemId).Count();
                    nomeTemporada = rodada.temporada.nome;
                    rankingCabecalho.rodada = "Rodada " + qtddRodada + "/" + rodada.temporada.qtddRodadas;
                }
                rankingCabecalho.dataRodada = rodada.dataFim;
            }
            rankingCabecalho.temporada = nomeTemporada;
            rankingCabecalho.classes = classes;
            rankingCabecalho.classeUserId = (int)user.classeId;
            return rankingCabecalho;
        }

        [Route("api/RankingAPI/MinhaPontuacao/{userId}")]
        public MinhaPontuacao GetMinhaPontuacao(int userId)
        {
            MinhaPontuacao minhaPontuacao = new MinhaPontuacao();
            List<Classificacao> classificacao = db.Rancking.Where(r => r.userProfile_id == userId).
                OrderByDescending(r => r.rodada_id).Take(10).Select(rk => new Classificacao() {
                    pontuacao = rk.pontuacao,
                    posicaoUser = rk.posicaoClasse,
                    nomeUser = rk.userProfile.nome,
                    rodada = rk.rodada.codigo + rk.rodada.sequencial,
                    totalAcumulado = rk.totalAcumulado,
                    dataRodada = rk.rodada.dataFim,
                    rodadaId = rk.rodada_id
                }).ToList();
            minhaPontuacao.classificacao = classificacao;
            if (classificacao.Count() > 0){
                minhaPontuacao.nomeUser = classificacao[0].nomeUser;
                minhaPontuacao.pontuacaoAtual = classificacao[0].totalAcumulado;
                minhaPontuacao.posicao = classificacao[0].posicaoUser;
                var dataUltimaRodada = classificacao[0].dataRodada;
                foreach(var classific in classificacao)
                {
                    var dataRealizacaoJogo = db.Jogo.Where(r => (r.desafiado_id == userId || r.desafiante_id == userId) && r.rodada_id == classific.rodadaId && (r.situacao_Id==4 || r.situacao_Id==5)).Select(r => r.dataCadastroResultado).FirstOrDefault();
                    if (dataRealizacaoJogo!=null && dataRealizacaoJogo > dataUltimaRodada)
                    {
                        classific.jogoAtrasado = "S";
                    }else{
                        classific.jogoAtrasado = "N";
                    }
                }
            }
            
            return minhaPontuacao;
        }

        [Route("api/RankingAPI/Sobre/{rankingId}")]
        [HttpGet]
        [ResponseType(typeof(Barragens))]
        public IHttpActionResult Sobre(int rankingId)
        {
            var barragem = db.Barragens.Find(rankingId);
            return Ok(barragem);
        }

        [Route("api/RankingAPI/RegrasPontuacao")]
        [HttpGet]
        [ResponseType(typeof(string))]
        public IHttpActionResult RegrasPontuacao()
        {
            return Ok("<p><b>Vitórias</b></p><p>O <b>Desafiante</b> que vencer pelo seguinte placar em sets:<br>2x0 - ganhará 10 pontos<br>2x1 - ganhará 08 pontos<br>O <b>Desafiado</b> que vencer pelo seguinte placar em sets:<br>2x0 - ganhará 09 pontos<br>2x1 - ganhará 07 pontos<br></p><p><b>Derrotas</b></p><p>O <b>Desafiante</b> que perder pelo seguinte placar em sets:<br>2x0 - ganhará 02 pontos<br>2x1 - ganhará 04 pontos<br>O <b>Desafiado</b> que perder pelo seguinte placar em sets:<br>2x0 - ganhará 01 ponto<br>2x1 - ganhará 03 pontos</p><p><b>Bônus</b></p><p>Nos jogos em que o perdedor fizer no máximo 2 games na partida o ganhador do jogo receberá 3 pontos a mais de bônus na sua pontuação do jogo.</p><p><b>PRÓ-SET</b></p><p>A pontuação nos jogos de PRÓ-SET será igual aos resultados de jogos com placares 2x1.</p><p><b>WO e Jogo com o Curinga</b></p><p>O Vencedor ganhará - 06 pontos</p><p>O Perderdor não pontuará</p><p><b>Jogador Licenciado</b></p><p>ganhará 3 pontos</p>");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool BarragemViewExists(int id)
        {
            return db.BarragemView.Count(e => e.Id == id) > 0;
        }
    }
}