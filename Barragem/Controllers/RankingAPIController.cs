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
                usuario = db.UserProfiles.Find(2030); // TODO : GET USERID WebSecurity.GetUserId(User.Identity.Name)
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
                    pontuacao = rk.pontuacao
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
                OrderByDescending(r => r.rodada_id).Take(10).Select(rk=> new Classificacao() {
                    pontuacao= rk.pontuacao,
                    posicaoUser = rk.posicaoClasse,
                    nomeUser = rk.userProfile.nome,
                    rodada = "Rodada " + rk.rodada.codigo + rk.rodada.sequencial,
                    dataRodada = rk.rodada.dataFim
                }).ToList();
            minhaPontuacao.classificacao = classificacao;
            if (classificacao.Count() > 0){
                minhaPontuacao.nomeUser = classificacao[0].nomeUser;
                minhaPontuacao.pontuacaoAtual = classificacao.Sum(c=> c.pontuacao);
                minhaPontuacao.posicao = classificacao[0].posicaoUser;
            }
            return minhaPontuacao;
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