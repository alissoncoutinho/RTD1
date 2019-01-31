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
    [Authorize]
    public class BarragemController : ApiController
    {
        private BarragemDbContext db = new BarragemDbContext();

        // GET: api/Barragem
        public IQueryable<BarragemView> GetBarragemView()
        {
            return db.BarragemView;
        }

        // GET: api/Barragem/5
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

        // PUT: api/Barragem/5
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

        // POST: api/Barragem
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

        // DELETE: api/Barragem/5
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