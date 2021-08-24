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
using Barragem.Class;

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
            var users = db.UserProfiles.Where(u => u.email.ToLower() == email.Trim().ToLower() && u.situacao!="inativo" && u.situacao != "desativado").OrderBy(r=> r.situacao).ToList();
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
                ranking.cidade = Funcoes.RemoveAcentosEspacosMaiusculas(item.barragem.cidade);
                ranking.userName = item.UserName;
                ranking.userId = item.UserId;
                ranking.situacao = item.situacao;
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
            return Ok("<span style='color: #212224;font-size: 16px;'><p>A pontua&ccedil;&atilde;o por jogo depende de alguns fatores explicados abaixo.</p><p>&Eacute; considerado um <strong>Desafiante</strong> no confronto aquele que est&aacute; abaixo do seu oponente na classifica&ccedil;&atilde;o. J&aacute; o <strong>Desafiado</strong> &eacute; aquele que est&aacute; acima de seu advers&aacute;rio.</p><p><strong>VIT&Oacute;RIAS</strong></p><p>O&nbsp;<strong>Desafiante</strong>&nbsp;que vencer pelo seguinte placar em sets:<br /> 2x0 - ganhar&aacute; 10 pontos<br /> 2x1 - ganhar&aacute; 08 pontos</p><p> O&nbsp;<strong>Desafiado</strong>&nbsp;que vencer pelo seguinte placar em sets:<br /> 2x0 - ganhar&aacute; 09 pontos<br /> 2x1 - ganhar&aacute; 07 pontos</p><p><strong>DERROTAS</strong></p><p>O&nbsp;<strong>Desafiante</strong>&nbsp;que perder pelo seguinte placar em sets:<br /> 2x0 - ganhar&aacute; 02 pontos<br /> 2x1 - ganhar&aacute; 04 pontos</p><p><br /> O&nbsp;<strong>Desafiado</strong>&nbsp;que perder pelo seguinte placar em sets:<br /> 2x0 - ganhar&aacute; 01 ponto<br /> 2x1 - ganhar&aacute; 03 pontos</p><p><strong>B&Ocirc;NUS</strong></p><p>Nos jogos em que o perdedor fizer no m&aacute;ximo 2 games na partida o ganhador do jogo receber&aacute; 3 pontos a mais de b&ocirc;nus na sua pontua&ccedil;&atilde;o do jogo.</p><p><strong>PR&Oacute;-SET</strong></p><p>A pontua&ccedil;&atilde;o nos jogos de PR&Oacute;-SET ser&aacute; igual aos resultados de jogos com placares 2x1.</p><p><strong>WO e JOGO COM CURINGA</strong></p><p>O Vencedor ganhar&aacute; - 06 pontos</p><p>O Perderdor n&atilde;o pontuar&aacute;</p><p><strong>Jogador Licenciado</strong></p><p>ganhar&aacute; 3 pontos</p><p>&nbsp;</p></span>");
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

        [HttpGet]
        [Route("api/RankingAPI/BuscaCidades/nome")]
        public IList<string> GetBuscaCidades(string nome)
        {
            List<string> cidades = new List<string>();
            if (nome.Length > 2) { 
                cidades = db.BarragemView.Where(j => j.cidade.StartsWith(nome) && j.isAtiva==true).OrderBy(j => j.cidade).Select(j=>j.cidade).ToList<string>();
            }
            return cidades;
        }

        [HttpGet]
        [Route("api/RankingAPI/cidade")]
        // GET: api/RankingAPI/cidade
        public IList<LoginRankingModel> GetRankingsByCidade(string nome)
        {
            var rankings = db.BarragemView.Where(b => b.isAtiva==true && b.cidade.ToLower() == nome.ToLower()).Select(rk => new LoginRankingModel()
            {
                idRanking = rk.Id,
                nomeRanking = rk.nome
            }).ToList();

            return rankings;
        }

        [Route("api/RankingAPI/Teste")]
        [HttpGet]
        [ResponseType(typeof(string))]
        public IHttpActionResult Teste()
        {
            try
            {
                var nomeRanking = db.BarragemView.Find(8).nome;
                var titulo = nomeRanking + ": classificação atualizada e nova rodada gerada!";
                var conteudo = "Clique aqui e entre em contato com seu adversário o mais breve possível e bom jogo.";

                var fbmodel = new FirebaseNotificationModel() { to = "/topics/ranking8", notification = new NotificationModel() { title = titulo, body = conteudo }, data = new DataModel() { title = titulo, body = conteudo, type = "nova_rodada_aberta", idRanking = 8 } };
                new FirebaseNotification().SendNotification(fbmodel);
                                
            }
            catch (Exception e)
            {
                return InternalServerError(new Exception("Erro: " + e.Message));
            }
            return Ok("Teste");
            
        }


        [HttpGet]
        [Route("api/RankingAPI/Ligas/{userId}")]
        // GET: api/RankingAPI/Ligas
        public IList<LoginRankingModel> GetRankingsLigas(int userId)
        {
            var ligas = (from liga in db.Liga
                     join tl in db.TorneioLiga on liga.Id equals tl.LigaId
                     join t in db.Torneio on tl.TorneioId equals t.Id
                     join it in db.InscricaoTorneio on t.Id equals it.torneioId
                     where it.userId == userId && it.isAtivo
                     select new LoginRankingModel
                     {
                         idRanking = liga.Id,
                         nomeRanking = liga.Nome,
                         userId = userId
                     }).Distinct<LoginRankingModel>().ToList();

            foreach (var item in ligas)
            {
                var barragens = db.BarragemLiga.Where(b => b.LigaId == item.idRanking).ToList();
                if (barragens.Count() == 1) {
                    item.logoId = barragens[0].BarragemId;
                } else {
                    item.logoId = 10;
                }
            }

            return ligas;
        }

        [Route("api/RankingAPI/Liga/cabecalho/{ligaId}")]
        public Cabecalho GetCabecalhoLiga(int ligaId, int userId)
        {
            var ultsnapshot = db.Snapshot.Where(snap => snap.LigaId == ligaId).Max(s => s.Id);
            var snapshot = db.Snapshot.Find(ultsnapshot); 
            var categorias = db.SnapshotRanking.Where(sr => sr.SnapshotId == ultsnapshot)
            .Include(sr => sr.Categoria).Select(sr => sr.Categoria).OrderBy(sr=> sr.ordemExibicao).Distinct().ToList();

            var classeLiga = (from tl in db.TorneioLiga
                               join it in db.InscricaoTorneio on tl.TorneioId equals it.torneioId
                               join cl in db.ClasseLiga on tl.LigaId equals cl.LigaId
                               join ct in db.ClasseTorneio on cl.Id equals ct.categoriaId
                               where it.userId == userId && it.isAtivo
                               select cl).ToList();


            var liga = db.Liga.Find(ligaId).Nome;
            var classesLg = db.ClasseLiga.Where(c => c.LigaId == ligaId).ToList();
            var classes = new List<Classe>();
            Cabecalho rankingCabecalho = new Cabecalho();
            rankingCabecalho.temporada = liga;
            //rankingCabecalho.categoria = categorias;
            Classe classe = null;
            foreach (var cat in categorias)
            {
                classe = new Classe();
                classe.Id = cat.Id;
                try{
                    classe.nome = classesLg.Where(c => c.CategoriaId == cat.Id).SingleOrDefault().Nome; //cat.Nome;
                }catch(Exception e){
                    classe.nome = cat.Nome;
                }
                classes.Add(classe);
            }
            rankingCabecalho.classes = classes;
            rankingCabecalho.dataRodada = snapshot.Data;
            if (classeLiga.Count() > 0) rankingCabecalho.classeUserId = classeLiga[0].CategoriaId;
            return rankingCabecalho;
        }

        [HttpGet]
        [Route("api/RankingAPI/ClassificacaoLiga/{ligaId}")]
        // GET: api/RankingAPI/ClassificacaoLiga/
        public IList<Classificacao> GetRankingLigas(int ligaId, int categoriaId=0)
        {
            var ultsnapshot = db.Snapshot.Where(snap => snap.LigaId == ligaId).Max(s => s.Id);
            List<Classificacao> ranking = new List<Classificacao>();
            if (ultsnapshot > 0)
            {
                ranking = db.SnapshotRanking.Where(sp => sp.SnapshotId == ultsnapshot && sp.CategoriaId == categoriaId)
                .Include(s => s.Categoria).Include(s => s.Jogador).OrderBy(sp => sp.Posicao).ThenBy(sp => sp.Jogador.nome).
                Select(rk => new Classificacao()
                {
                    userId = rk.Jogador.UserId,
                    nomeUser = rk.Jogador.nome,
                    posicaoUser = rk.Posicao,
                    pontuacao = rk.Pontuacao,
                    foto = rk.Jogador.fotoURL,
                    dataRodada = rk.Snapshot.Data
                }).ToList();
            }

            return ranking;
        }

        


    }
}