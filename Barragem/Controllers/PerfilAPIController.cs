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
using System.IO;
using System.Drawing;
using System.Web.Hosting;

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
            // gráfico rosca - desempenho nos jogos
            var meusJogos = db.Jogo.Where(j => (j.desafiado_id == userId || j.desafiante_id == userId) && (j.situacao_Id == 5 || j.situacao_Id == 4) && j.torneioId == null).ToList();
            estatistica.qtddTotalDerrotas = meusJogos.Where(j => j.idDoVencedor != userId).Count();
            estatistica.qtddTotalVitorias = meusJogos.Where(j => j.idDoVencedor == userId).Count();
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
            var jogos = db.Jogo.Where(j => (j.desafiado_id == userId || j.desafiante_id == userId) && j.torneioId==null).OrderByDescending(j=>j.Id).Take(10).ToList<Jogo>();
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

        [Route("api/PerfilAPI/Torneio/{userId}")]
        public IList<ColocacaoTorneio> GetTorneio(int userId) {
            var colocacoesEmTorneios =
                from inscricao in db.InscricaoTorneio
                join torneio in db.Torneio on inscricao.torneioId equals torneio.Id into colocacaoJogador
                where inscricao.userId == userId && inscricao.torneio.dataFim < DateTime.Now && inscricao.colocacao != null
                select new ColocacaoTorneio()
                {
                    colocacaoId = inscricao.colocacao,
                    nomeTorneio = inscricao.torneio.nome,
                    classe = inscricao.classeTorneio.nome,
                    dataTorneio = inscricao.torneio.dataInicio,
                    
                };
            var listCT = new List<ColocacaoTorneio>(colocacoesEmTorneios);
            foreach (var c in listCT)
            {
                c.colocacao = c.getDescricaoColocacao(c.colocacaoId);
            }
         

            return listCT;
        }

        [ResponseType(typeof(void))]
        [HttpPut]
        [Route("api/PerfilAPI/AlterarStatus/{userId}")]
        public IHttpActionResult PutAlterarStatus(int userId, string status)
        {
            var user = db.UserProfiles.Find(userId);

            if ((user.situacao == "ativo" || user.situacao == "licenciado") && (status=="ativo" || status== "licenciado")){

                user.situacao = status;
            } else {
                return InternalServerError(new Exception("Status não pode ser alterado nesta situação."));
            }

            db.Entry(user).State = EntityState.Modified;
            try
            {
                db.SaveChanges();
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        [Route("api/PerfilAPI/BuscaOponentes/{rankingId}")]
        public IList<Perfil> GetBuscaOponentes(int rankingId)
        {
            List<UserProfile> oponentes;
            oponentes = db.UserProfiles.Where(j => j.barragemId == rankingId && (j.situacao== "ativo" || j.situacao == "licenciado" || j.situacao == "suspenso")).OrderBy(j => j.nome).ToList<UserProfile>();
            
             IList<Perfil> Listaperfil = new List<Perfil>();
            foreach (var oponente in oponentes)
            {
                var j = new Perfil();
                j.userId = oponente.UserId;
                j.nome = oponente.nome;
                j.fotoPerfil = null;
                Listaperfil.Add(j);

            }

            return Listaperfil;
        }

        private HeadToHead montarHead2Head(int userIdOponente, int userId)
        {
            var jogosHeadToHead = db.Jogo.Where(j => (j.desafiado_id == userId && j.desafiante_id == userIdOponente) ||
                (j.desafiante_id == userId && j.desafiado_id == userIdOponente)).ToList();

            HeadToHead headToHead = new HeadToHead();
            headToHead.Id = 0;

            headToHead.qtddVitoriasDesafiado = jogosHeadToHead.Where(j => j.idDoVencedor == userId).Count();
            headToHead.qtddVitoriasDesafiante = jogosHeadToHead.Where(j => j.idDoVencedor == userIdOponente).Count();

            var userOponente = db.UserProfiles.Find(userIdOponente);
            headToHead.idDesafiado = userId;
            headToHead.idDesafiante = userIdOponente;
            headToHead.alturaDesafiante = userOponente.altura2;
            headToHead.idadeDesafiante = userOponente.idade;
            headToHead.naturalidadeDesafiante = userOponente.naturalidade;
            headToHead.inicioRankingDesafiante = userOponente.dataInicioRancking.Month + "/" + userOponente.dataInicioRancking.Year;
            headToHead.lateralDesafiante = userOponente.lateralidade;
            try
            {
                var melhorRankingDesafiante = db.Rancking.Where(r => r.userProfile_id == userIdOponente && r.posicaoClasse != null && r.classeId != null).OrderBy(r => r.classe.nivel).ThenBy(r => r.posicaoClasse).Take(1).ToList();
                headToHead.melhorPosicaoDesafiante = melhorRankingDesafiante[0].posicaoClasse + "º/" + melhorRankingDesafiante[0].classe.nome;
            }
            catch (Exception e) { }

            return headToHead;
        }


        [ResponseType(typeof(HeadToHead))]
        [HttpGet]
        [Route("api/PerfilAPI/GetHead2Head/{userId}")]
        public IHttpActionResult GetHead2Head(int userId, int userIdOponente)
        {
            HeadToHead headToHead = montarHead2Head(userIdOponente, userId);

            return Ok(headToHead);
        }

        private string ProcessImage(string croppedImage, int userId)  
        {

            string filePath = String.Empty;
            try
            {
                string base64 = croppedImage;
                byte[] bytes = Convert.FromBase64String(base64.Split(',')[1]);
                filePath = "/Content/images/Photo/Pf-" + Guid.NewGuid() + ".jpg";
                MemoryStream stream = new MemoryStream(bytes);
                Image png = Image.FromStream(stream);
                png.Save(HostingEnvironment.MapPath(filePath), System.Drawing.Imaging.ImageFormat.Jpeg);
                png.Dispose();
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao gravar imagem "+ex.Message);
            }
            if (userId != 0)
            {
                string fotoURL = (from up in db.UserProfiles where up.UserId == userId select up.fotoURL).Single();
                if ((fotoURL != null) && (fotoURL != "") && (System.IO.File.Exists(HostingEnvironment.MapPath(fotoURL))))
                {
                    try
                    {
                        System.IO.File.Delete(HostingEnvironment.MapPath(fotoURL));
                    }
                    catch (System.IO.IOException e)
                    {
                        throw new Exception("Erro ao deletar foto anterior " + e.Message);
                    }
                }
            }

            return filePath;
        }

        [ResponseType(typeof(string))]
        [HttpPut]
        [Route("api/PerfilAPI/AlterarFoto")]
        public IHttpActionResult PutAlterarFoto([FromBody] Avatar avatar)
        {
            UserProfile user = null;
            try
            {
                user = db.UserProfiles.Find(avatar.userId);
                if (!String.IsNullOrEmpty(avatar.avatarCropped)){
                    string filePath = ProcessImage(avatar.avatarCropped, avatar.userId);
                    user.fotoURL = filePath;
                    db.Entry(user).State = EntityState.Modified;
                    db.SaveChanges();
                }
            } catch (Exception e) {
                return InternalServerError(e);
            }

            return Ok(user.fotoURL);
        }

        
    }
}