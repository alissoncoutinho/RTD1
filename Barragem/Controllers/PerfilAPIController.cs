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
        public IHttpActionResult ResetarSenha(string email) {
            UserProfile user = null;
            try {
                user = db.UserProfiles.Where(u => u.email == email).FirstOrDefault();
                if (user != null) {
                    if (String.IsNullOrEmpty(user.email)) {
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
                } else {
                    return InternalServerError(new Exception("Este usuário não existe."));
                }
            } catch (Exception ex) {
                return InternalServerError(new Exception(ex.Message));
            } finally {
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

        [Route("api/PerfilAPI/Cabecalho/{userId}")]
        public CabecalhoPerfil GetCabecalho(int userId)
        {
            CabecalhoPerfil cabecalho = db.Rancking.Where(r => r.userProfile_id == userId).
                OrderByDescending(r => r.rodada_id).Take(1).Select(rk => new CabecalhoPerfil()
                {
                    posicaoUser = rk.posicaoClasse,
                    nomeUser = rk.userProfile.nome,
                    totalAcumulado = rk.totalAcumulado,
                    fotoPerfil = rk.userProfile.fotoURL,
                    statusUser = rk.userProfile.situacao
                }).FirstOrDefault();
            return cabecalho;
        }

        [Route("api/PerfilAPI/Estatistica/{userId}")]
        public Estatistica GetEstatistica(int userId)
        {
            // gráfico linha - desempenho no ranking
            var estatistica = new Estatistica();
            var meuRanking = db.Rancking.Where(r => r.userProfile_id == userId && !r.rodada.isRodadaCarga && r.posicaoClasse != null && r.classeId != null).
                OrderByDescending(r => r.rodada.dataInicio).Take(7).ToList();
            var labels = "";
            var dados = "";
            var primeiraVez = true;
            foreach (var rk in meuRanking){
                if (primeiraVez){
                    primeiraVez = false;
                    labels = "'" + rk.rodada.codigoSeq + ": " + rk.classe.nome.Replace("Classe", "Cl.") + "'";
                    dados = "" + rk.posicaoClasse;
                }else{
                    dados = rk.posicaoClasse + "," + dados;
                    labels = "'" + rk.rodada.codigoSeq + ": " + rk.classe.nome.Replace("Classe", "Cl.") + "'," + labels;
                }
            }
            if (meuRanking.Count() > 0) {
                estatistica.labels = labels;
                estatistica.dados = dados;
            }
            return estatistica; 
        }
    }
}