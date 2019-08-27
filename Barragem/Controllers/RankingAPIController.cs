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
            var users = db.UserProfiles.Where(u => u.email.ToLower() == email.Trim().ToLower() && u.situacao!="desativado" && u.situacao!="inativo").ToList();
            if (users.Count() == 0){
                return loginRankings;
                //throw (new Exception("Não foi encontrado ranking com este email."));
            }
            var qtdd = users.Count();
            foreach (var item in users)
            {
                var ranking = new LoginRankingModel();
                ranking.idRanking = item.barragemId;
                if (qtdd > 1)
                {
                    ranking.nomeRanking = item.barragem.nome + " - " + item.UserName;
                } else {
                    ranking.nomeRanking = item.barragem.nome;
                }
                
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
            var classe = db.Classe.Find(classeId);
            int barragemId = classe.barragemId;
            var idRodada = 0;
            try {
                idRodada = db.Rancking.Where(r => r.rodada.isAberta == false && r.rodada.isRodadaCarga == false && r.rodada.barragemId == barragemId).Max(r => r.rodada_id);
            } catch (InvalidOperationException){}

            var rancking = db.Rancking.Include(r => r.userProfile).Include(r => r.rodada).
                Where(r => r.rodada_id == idRodada && r.posicao > 0 && r.posicaoClasse != null && r.userProfile.situacao != "desativado" && r.userProfile.situacao != "inativo" && r.classe.Id == classeId).
                OrderBy(r => r.classe.nivel).ThenBy(r => r.posicaoClasse).Select(rk => new Classificacao()
                {
                    userId = rk.userProfile_id,
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
            return Ok("<span style='color: #212224;font-size: 16px;'><p>A pontua&ccedil;&atilde;o por jogo depende de alguns fatores explicados abaixo.</p><p>&Eacute; considerado um <strong>Desafiante</strong> no confronto aquele que est&aacute; abaixo do seu oponente na classifica&ccedil;&atilde;o. J&aacute; o <strong>Desafiado</strong> &eacute; aquele que est&aacute; acima de seu advers&aacute;rio.</p><p><strong>VIT&Oacute;RIAS</strong></p><p>O&nbsp;<strong>Desafiante</strong>&nbsp;que vencer pelo seguinte placar em sets:<br /> 2x0 - ganhar&aacute; 10 pontos<br /> 2x1 - ganhar&aacute; 08 pontos</p><p><br /> O&nbsp;<strong>Desafiado</strong>&nbsp;que vencer pelo seguinte placar em sets:<br /> 2x0 - ganhar&aacute; 09 pontos<br /> 2x1 - ganhar&aacute; 07 pontos</p><p><strong>DERROTAS</strong></p><p>O&nbsp;<strong>Desafiante</strong>&nbsp;que perder pelo seguinte placar em sets:<br /> 2x0 - ganhar&aacute; 02 pontos<br /> 2x1 - ganhar&aacute; 04 pontos</p><p><br /> O&nbsp;<strong>Desafiado</strong>&nbsp;que perder pelo seguinte placar em sets:<br /> 2x0 - ganhar&aacute; 01 ponto<br /> 2x1 - ganhar&aacute; 03 pontos</p><p><strong>B&Ocirc;NUS</strong></p><p>Nos jogos em que o perdedor fizer no m&aacute;ximo 2 games na partida o ganhador do jogo receber&aacute; 3 pontos a mais de b&ocirc;nus na sua pontua&ccedil;&atilde;o do jogo.</p><p><strong>PR&Oacute;-SET</strong></p><p>A pontua&ccedil;&atilde;o nos jogos de PR&Oacute;-SET ser&aacute; igual aos resultados de jogos com placares 2x1.</p><p><strong>WO e JOGO COM CURINGA</strong></p><p>O Vencedor ganhar&aacute; - 06 pontos</p><p>O Perderdor n&atilde;o pontuar&aacute;</p><p><strong>Jogador Licenciado</strong></p><p>ganhar&aacute; 3 pontos</p><p>&nbsp;</p></span>");
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