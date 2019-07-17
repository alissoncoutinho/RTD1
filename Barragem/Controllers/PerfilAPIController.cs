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

        [ResponseType(typeof(HeadToHead))]
        [HttpGet]
        [Route("api/PerfilAPI/GetHead2Head/{userId}")]
        public IHttpActionResult GetHead2Head(int userId)
        {
            var user = db.UserProfiles.Find(userId);
            if (user == null)
            {
                return NotFound();
            }
            HeadToHead headToHead = new HeadToHead();
            headToHead.idadeDesafiado = user.idade;
            headToHead.naturalidadeDesafiado = user.naturalidade;
            headToHead.lateralDesafiado = user.lateralidade;
            headToHead.alturaDesafiado = user.altura2;
            headToHead.inicioRankingDesafiado = user.dataInicioRancking.Month + "/" + user.dataInicioRancking.Year;
            var melhorRankingDesafiado = db.Rancking.Where(r => r.userProfile_id == userId && r.posicaoClasse != null && r.classeId != null).OrderBy(r => r.classe.nivel).ThenBy(r => r.posicaoClasse).Take(1).ToList();
            headToHead.melhorPosicaoDesafiado = melhorRankingDesafiado[0].posicaoClasse + "º/" + melhorRankingDesafiado[0].classe.nome;

            return Ok(headToHead);
        }

        // GET: api/RankingAPI/
        [Route("api/PerfilAPI/{userId}")]
        [HttpGet]
        [ResponseType(typeof(Perfil))]
        public IHttpActionResult GetPerfil(int userId){
            var user = db.UserProfiles.Find(userId);
            var perfil = new Perfil();
            perfil.login = user.UserName;
            perfil.nome = user.nome;
            perfil.email = user.email;
            perfil.telefone = user.telefoneCelular;
            perfil.naturalidade = user.naturalidade;
            perfil.dataNascimento = user.dataNascimento;
            perfil.altura = user.altura2;
            perfil.lateralidade = user.lateralidade;
            perfil.informacoesAdicionais = user.matriculaClube;
            perfil.userId = user.UserId;

            return Ok(perfil);
        }

        [ResponseType(typeof(void))]
        [HttpPut]
        [Route("api/PerfilAPI/AlterarPerfil/{userId}")]
        public IHttpActionResult PutAlterarPerfil(int userId, string nome, string email, string celular, string naturalidade, DateTime dataNascimento, string altura, string lateralidade, string informacoesAdicionais)
        {
            var user = db.UserProfiles.Find(userId);

            user.nome = nome;
            user.email = email;
            user.telefoneCelular = celular;
            user.naturalidade = naturalidade;
            user.dataNascimento = dataNascimento;
            user.altura2 = altura;
            user.lateralidade = lateralidade;
            user.matriculaClube = informacoesAdicionais;

            db.Entry(user).State = EntityState.Modified;
            try{
                db.SaveChanges();
            }catch (Exception e){
                return InternalServerError(e);
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        [Route("api/PerfilAPI/Ranking/{userId}")]
        public IList<JogoRodada> GetRanking(int userId)
        {
            var jogos = db.Jogo.Where(j => j.desafiado_id == userId || j.desafiante_id == userId).OrderByDescending(j=>j.Id).Take(10).ToList<Jogo>();
            IList<JogoRodada> jogoRodada = new List<JogoRodada>();
            foreach (var jogo in jogos){
                var j = new JogoRodada();
                j.Id = jogo.Id;
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
                j.nomeRodada = "Rodada " + jogo.rodada.codigoSeq;
                var rankingDesafiado = db.Rancking.Where(r => r.rodada_id == jogo.rodada_id && r.userProfile_id == jogo.desafiado_id).FirstOrDefault();
                var rankingDesafiante = db.Rancking.Where(r => r.rodada_id == jogo.rodada_id && r.userProfile_id == jogo.desafiante_id).FirstOrDefault();
                if (rankingDesafiado != null)
                {
                    j.rankingDesafiado = rankingDesafiado.posicaoClasse!=null ? (int)rankingDesafiado.posicaoClasse:0;
                    j.pontuacaoDesafiado = rankingDesafiado.pontuacao;
                }
                if (rankingDesafiante != null)
                {
                    j.rankingDesafiante = rankingDesafiante.posicaoClasse != null ? (int)rankingDesafiante.posicaoClasse : 0;
                    j.pontuacaoDesafiante = rankingDesafiante.pontuacao;
                }
                jogoRodada.Add(j);

            }

            return jogoRodada;
        }

        /*
            FALTANDO: ALTERAR IMAGEM;   ALTERAR STATUS;     ESCOLHER OPONENTE;
         */

    }
}