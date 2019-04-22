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
                loginRankings.Add(ranking);
            }
            return loginRankings;
        }
        
        // GET: api/RankingAPI/
        [Route("api/RankingAPI/{classeId}")]
        public IList<RanckingView> GetRanking(int classeId){
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
                OrderBy(r => r.classe.nivel).ThenBy(r => r.posicaoClasse).Select(rk => new RanckingView()
                {
                    rodada = rk.rodada.codigo + rk.rodada.sequencial,
                    dataRodada = rk.rodada.dataFim,
                    nome = rk.userProfile.nome,
                    posicao = (int)rk.posicaoClasse,
                    nomeClasse = rk.classe.nome,
                    pontuacao = rk.pontuacao
                }).ToList<RanckingView>();
            
            return rancking;
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