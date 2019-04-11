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
            var users = db.UserProfiles.Where(u => u.email == email.Trim()).ToList();
            if (users.Count() == 0)
            {
                throw (new Exception("Não foi encontrado ranking com este email."));
            }
            List<LoginRankingModel> loginRankings = new List<LoginRankingModel>();
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
        public IQueryable<BarragemView> GetBarragemView()
        {
            return db.BarragemView;
        }

        // GET: api/RankingAPI/5
        [ResponseType(typeof(BarragemView))]
        public IHttpActionResult GetBarragemView(int id)
        {
            BarragemView barragemView = db.BarragemView.Find(id);
            if (barragemView == null)
            {
                return NotFound();
            }

            return Ok(barragemView);
        }

        // PUT: api/RankingAPI/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PutBarragemView(int id, BarragemView barragemView)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != barragemView.Id)
            {
                return BadRequest();
            }

            db.Entry(barragemView).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BarragemViewExists(id))
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

        // POST: api/RankingAPI
        [ResponseType(typeof(BarragemView))]
        public IHttpActionResult PostBarragemView(BarragemView barragemView)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.BarragemView.Add(barragemView);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = barragemView.Id }, barragemView);
        }

        // DELETE: api/RankingAPI/5
        [ResponseType(typeof(BarragemView))]
        public IHttpActionResult DeleteBarragemView(int id)
        {
            BarragemView barragemView = db.BarragemView.Find(id);
            if (barragemView == null)
            {
                return NotFound();
            }

            db.BarragemView.Remove(barragemView);
            db.SaveChanges();

            return Ok(barragemView);
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