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
using WebMatrix.WebData;
using Barragem.Class;
using Barragem.Filters;

namespace Barragem.Controllers
{

    
    public class PerfilAPIController : ApiController
    {
        private BarragemDbContext db = new BarragemDbContext();

        // GET: api/JogoAPI
        [Route("api/PerfilAPI/ResetarSenha")]
        [ResponseType(typeof(void))]
        [HttpGet]
        public IHttpActionResult ResetarSenha(string email){
            UserProfile user = null;
            try{
                user = db.UserProfiles.Where(u => u.email == email).FirstOrDefault();
                if (user != null){
                    if (String.IsNullOrEmpty(user.email)){
                        return InternalServerError(new Exception("Este usuário não possui e-mail cadastrado. Por favor, entre em contato com o administrador."));
                    } else {
                        Database.SetInitializer<BarragemDbContext>(null);
                        if (!WebSecurity.Initialized) { 
                                WebSecurity.InitializeDatabaseConnection("DefaultConnection", "UserProfile", "UserId", "UserName", autoCreateTables: false);
                        }
                        string confirmationToken = WebSecurity.GeneratePasswordResetToken(user.UserName);
                        EnviarMailSenha(confirmationToken, user.nome, user.email);
                        return StatusCode(HttpStatusCode.NoContent);
                    }
                }else{
                    return InternalServerError(new Exception("Este usuário não existe."));
                }
            } catch (Exception ex) {
                return InternalServerError(new Exception(ex.Message)); 
            }finally{
                if (db != null)
                    db.Dispose();
            }
        }


        private void EnviarMailSenha(string token, string nomeUsuario, string emailUsuario)
        {
            string strUrl = string.Format("http://www.rankingdetenis.com/Account/ConfirmaSenha/{0}", token);
            string strConteudo = "<html> <head> </head> <body> Prezado(a) " + nomeUsuario + ", <br /> Você fez uma solicitação de reinicio de senha. <br />";
            strConteudo += "Para continuar, clique no link abaixo: <br /> " + strUrl + " </body> </html>";

            Mail email = new Mail();
            email.SendEmail(emailUsuario, "recuperação de senha", strConteudo, Class.Tipos.FormatoEmail.Html);
        }
    }
}
