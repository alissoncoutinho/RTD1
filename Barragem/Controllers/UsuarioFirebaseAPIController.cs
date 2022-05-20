using Barragem.Context;
using Barragem.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Description;

namespace Barragem.Controllers
{
    public class UsuarioFirebaseAPIController : ApiController
    {
        private BarragemDbContext db = new BarragemDbContext();

        [HttpGet]
        [ResponseType(typeof(string))]
        [Route("api/UsuarioFirebase/ObterTokenUsuarioFirebase/{userId}")]
        public IHttpActionResult ObterTokenUsuarioFirebase(int userId)
        {
            var usuarioFirebase = db.UsuarioFirebase.FirstOrDefault(x => x.UserId == userId);

            if (usuarioFirebase == null)
                return NotFound();

            return Ok(usuarioFirebase.Token);
        }

        [HttpPost]
        [ResponseType(typeof(bool))]
        [Route("api/UsuarioFirebase/GravarTokenUsuarioFirebase")]
        public IHttpActionResult GravarTokenUsuarioFirebase(UsuarioFirebaseCreateModel dadosUsuarioFirebase)
        {
            bool gravacaoOk = false;
            if (dadosUsuarioFirebase == null)
                throw new ArgumentNullException("Dados de entrada inválidos");
            if (dadosUsuarioFirebase.UserId <= 0)
                throw new ArgumentNullException("Id do usuário inválido");
            if (string.IsNullOrEmpty(dadosUsuarioFirebase.Token))
                throw new ArgumentNullException("Token inválido");

            var usuarioFirebase = db.UsuarioFirebase.FirstOrDefault(x => x.UserId == dadosUsuarioFirebase.UserId);

            if (usuarioFirebase != null)
            {
                usuarioFirebase.Token = dadosUsuarioFirebase.Token;
                usuarioFirebase.DataAtualizacao = DateTime.Now;
                db.Entry(usuarioFirebase).State = EntityState.Modified;
                gravacaoOk = db.SaveChanges() > 0;
            }
            else
            {
                var dadosInclusao = new UsuarioFirebase()
                {
                    UserId = dadosUsuarioFirebase.UserId,
                    Token = dadosUsuarioFirebase.Token,
                    DataAtualizacao = DateTime.Now
                };
                db.UsuarioFirebase.Add(dadosInclusao);
                gravacaoOk = db.SaveChanges() > 0;
            }
            return Ok(gravacaoOk);
        }

    }
}