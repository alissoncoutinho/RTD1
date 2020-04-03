using Barragem.Context;
using Barragem.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http;
using System.Web.Http.Description;

namespace Barragem.Controllers
{
    public class TorneioAPIController : ApiController
    {
        private BarragemDbContext db = new BarragemDbContext();
        private TorneioController tc = new TorneioController();
        [HttpGet]
        [Route("api/TorneioAPI/{userId}")]
        public IList<TorneioApp> GetTorneio(int userId)
        {
            var dataHoje = DateTime.Now.AddDays(-5);
            var Torneios = (from inscricao in db.InscricaoTorneio
                           join torneio in db.Torneio on inscricao.torneioId equals torneio.Id into colocacaoJogador
                           where inscricao.userId == userId && inscricao.torneio.isAtivo && inscricao.torneio.dataFim > dataHoje
                select new TorneioApp
                {
                    Id = inscricao.torneio.Id,
                    logoId = inscricao.torneio.barragemId,
                    nome = inscricao.torneio.nome,
                    dataInicio = inscricao.torneio.dataInicio,
                    dataFim = inscricao.torneio.dataFim,
                    cidade = inscricao.torneio.cidade,
                    premiacao = inscricao.torneio.premiacao,
                    contato = inscricao.torneio.barragem.email
                }).Distinct<TorneioApp>();
            var listTorneio = new List<TorneioApp>(Torneios);
            return listTorneio;
        }

        [Route("api/TorneioAPI/Inscritos/{torneioId}")]
        public IList<Inscrito> GetListarInscritos(int torneioId)
        {
            var torneio = db.Torneio.Find(torneioId);
            var liberarTabelaInscricao = torneio.liberaTabelaInscricao;
            if (!liberarTabelaInscricao)
            {
                throw new Exception(message: "Tabela de inscritos ainda não liberada.");
            }

            var inscricoes = db.InscricaoTorneio.Where(r => r.torneioId == torneioId && r.classeTorneio.isDupla == false).
                OrderBy(r => r.classe).ThenBy(r => r.participante.nome).ToList();
            var inscricoesDupla = db.InscricaoTorneio.Where(r => r.torneioId == torneioId && r.classeTorneio.isDupla == true).
                OrderBy(r => r.classe).ThenBy(r => r.participante.nome).ToList();

            List<InscricaoTorneio> inscricoesRemove = new List<InscricaoTorneio>();
            foreach (var ins in inscricoesDupla) {
                var formouDupla = inscricoesDupla.Where(i => i.parceiroDuplaId == ins.userId && i.classe == ins.classe).Count();
                if (formouDupla > 0) {
                    inscricoesRemove.Add(ins);
                }
            }
            foreach (var ins in inscricoesRemove){
                    inscricoesDupla.Remove(ins);
            }
            inscricoes.AddRange(inscricoesDupla);
            List<Inscrito> inscritos = new List<Inscrito>();
            foreach (var i in inscricoes)
            {
                var inscrito = new Inscrito();
                inscrito.userId = i.userId;
                inscrito.nome = i.participante.nome;
                inscrito.classe = i.classeTorneio.nome;
                inscrito.foto = i.participante.fotoURL;
                if(i.parceiroDupla!=null) inscrito.nomeDupla = i.parceiroDupla.nome;
                inscritos.Add(inscrito);
            }
                        
            return inscritos;
        }

        [ResponseType(typeof(void))]
        [HttpPut]
        [Route("api/TorneioAPI/LancarResultado/{id}")]
        public IHttpActionResult PutLancarResultado(int id, int games1setDesafiante = 0, int games2setDesafiante = 0, int games3setDesafiante = 0, int games1setDesafiado = 0, int games2setDesafiado = 0, int games3setDesafiado = 0)
        {
            var jogo = db.Jogo.Find(id);

            jogo.qtddGames1setDesafiante = games1setDesafiante;
            jogo.qtddGames2setDesafiante = games2setDesafiante;
            jogo.qtddGames3setDesafiante = games3setDesafiante;

            jogo.qtddGames1setDesafiado = games1setDesafiado;
            jogo.qtddGames2setDesafiado = games2setDesafiado;
            jogo.qtddGames3setDesafiado = games3setDesafiado;
            jogo.usuarioInformResultado = ""; //User.Identity.Name; TODO: PEGAR O NOME DO USUÁRIO
            jogo.dataCadastroResultado = DateTime.Now;
            jogo.situacao_Id = 4;

            db.Entry(jogo).State = EntityState.Modified;

            try
            {

                db.SaveChanges();
                tc.MontarProximoJogoTorneio(jogo);
            }
            catch (Exception)
            {
                return InternalServerError(new Exception("Erro ao lançar resultado."));
            }
            return StatusCode(HttpStatusCode.NoContent);
        }

        [ResponseType(typeof(void))]
        [HttpPut]
        [Route("api/TorneioAPI/LancarWO/{id}")]
        public IHttpActionResult PutLancarWO(int id, int userIdVencedor)
        {
            Jogo jogoAtual = db.Jogo.Find(id);
            //alterar quantidade de games para desafiado e desafiante
            int gamesDesafiante = 0;
            int gamesDesafiado = 0;
            if (jogoAtual.desafiado_id == userIdVencedor)
            {
                gamesDesafiado = 6;
                gamesDesafiante = 1;
            }
            else
            {
                gamesDesafiado = 1;
                gamesDesafiante = 6;
            }
            jogoAtual.qtddGames1setDesafiado = gamesDesafiado;
            jogoAtual.qtddGames1setDesafiante = gamesDesafiante;
            jogoAtual.qtddGames2setDesafiado = gamesDesafiado;
            jogoAtual.qtddGames2setDesafiante = gamesDesafiante;
            jogoAtual.qtddGames3setDesafiado = 0;
            jogoAtual.qtddGames3setDesafiante = 0;
            //alterar status do jogo WO
            jogoAtual.situacao_Id = 5;
            jogoAtual.usuarioInformResultado = ""; // TODO User.Identity.Name;
            jogoAtual.dataCadastroResultado = DateTime.Now;
            db.Entry(jogoAtual).State = EntityState.Modified;
            try
            {
                db.SaveChanges();
                tc.MontarProximoJogoTorneio(jogoAtual);
            }
            catch (Exception e)
            {
                return InternalServerError(new Exception("Erro ao lançar resultado: " + e.Message));
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        [HttpGet]
        [Route("api/TorneioAPI/Tabela/{torneioId}")]
        public TabelaApp GetTabela(int torneioId)
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var userId = Convert.ToInt32(claimsIdentity.FindFirst("sub").Value);
            //var userId = 2030;
            var classeUser = db.InscricaoTorneio.Where(c => c.torneioId == torneioId && c.isAtivo && c.userId == userId).FirstOrDefault().classe;
                //.Select(c=>c.classe).FirstOrDefault();
            var classes = db.ClasseTorneio.Where(c => c.torneioId == torneioId).Select(ct => new ClasseTorneioApp
            {
                nome = ct.nome,
                Id = ct.Id
            }).OrderBy(c => c.nome).ToList<ClasseTorneioApp>();
            //var listClasses = new List<ClasseTorneioApp>(classes);
            foreach (var c in classes){
                if (classeUser==c.Id) {
                    c.selected = true;
                }else{
                    c.selected = false;
                }
            }
            var primeiraFase = db.Jogo.Where(r => r.classeTorneio == classeUser && r.faseTorneio < 100).Max(r => r.faseTorneio);
            var jogos = db.Jogo.Where(c => c.classeTorneio == classeUser && c.faseTorneio == primeiraFase).ToList();

            var ListJogos = new List<MeuJogo>();
            foreach (var j in jogos)
            {
                ListJogos.Add(montaJogoTabela(j));
            }
            
            var tabelaApp = new TabelaApp();
            tabelaApp.classes = classes;
            tabelaApp.descricaoFase = getDescricaoFaseTorneio((int)primeiraFase);
            tabelaApp.jogos = ListJogos;
            tabelaApp.faseTorneio = (int)primeiraFase;
            return tabelaApp;
        }

        [Route("api/TorneioAPI/FaseTabela/{classeId}")]
        public TabelaApp GetFaseTabela(int classeId, int faseAtual=0, string faseSolicitada="")
        {
            int? fase = 0;
            if (faseAtual == 0) {
                fase = db.Jogo.Where(r => r.classeTorneio == classeId && r.faseTorneio < 100).Max(r => r.faseTorneio);
            } else if (faseSolicitada == "P") {
                fase = db.Jogo.Where(r => r.classeTorneio == classeId && r.faseTorneio < faseAtual).Max(r => r.faseTorneio);
            } else if (faseSolicitada=="A") {
                fase = db.Jogo.Where(r => r.classeTorneio == classeId && r.faseTorneio > faseAtual).Min(r => r.faseTorneio);
                if ((fase == null)||(fase == 0)){ fase = faseAtual; }
            } else {
                fase = faseAtual;
            }

            var jogos = db.Jogo.Where(c => c.classeTorneio == classeId && c.faseTorneio == fase).ToList();

            var ListJogos = new List<MeuJogo>();
            foreach(var j in jogos){
                ListJogos.Add(montaJogoTabela(j));
            }
            var tabelaApp = new TabelaApp();
            tabelaApp.descricaoFase = getDescricaoFaseTorneio((int)fase);
            tabelaApp.jogos = ListJogos;
            tabelaApp.faseTorneio = (int)fase;
            return tabelaApp;
        }

        private MeuJogo montaJogoTabela(Jogo j) {
            var meuJogo = new MeuJogo();
            meuJogo.dataJogo = j.dataJogo;
            meuJogo.horaJogo = j.horaJogo;
            var quadra = "";
            if ((j.quadra != null) && (j.quadra != 100))
            {
                quadra = " quadra " + j.quadra;
            }
            var local = "";
            if (j.localJogo != null)
            {
                local = j.localJogo;
            }
            meuJogo.localJogo = local + quadra;
            meuJogo.idDesafiante = j.desafiante_id;
            meuJogo.idDesafianteDupla = j.desafiante2_id;
            if (meuJogo.idDesafiante == 10)
            {
                meuJogo.nomeDesafiante = "bye";
            }
            else if (meuJogo.idDesafiante == 0)
            {
                meuJogo.nomeDesafiante = "Aguardando Adversário";
            }
            else
            {
                if ((j.cabecaChaveDesafiante != null) && (j.cabecaChaveDesafiante > 0) && (j.cabecaChaveDesafiante < 100))
                {
                    meuJogo.nomeDesafiante = "(" + j.cabecaChaveDesafiante + ")" + j.desafiante.nome;
                }
                else
                {
                    meuJogo.nomeDesafiante = j.desafiante.nome;
                }
                meuJogo.fotoDesafiante = j.desafiante.fotoURL;
                if (j.desafiante2 != null)
                {
                    meuJogo.nomeDesafianteDupla = j.desafiante2.nome;
                    meuJogo.fotoDesafianteDupla = j.desafiante2.fotoURL;
                }
            }
            meuJogo.idDesafiado = j.desafiado_id;
            meuJogo.idDesafiadoDupla = j.desafiado2_id;
            if (meuJogo.idDesafiado == 10)
            {
                meuJogo.nomeDesafiado = "bye";
            }
            else if (meuJogo.idDesafiado == 0)
            {
                meuJogo.nomeDesafiado = "Aguardando Adversário";
            }
            else
            {
                if ((j.cabecaChave != null) && (j.cabecaChave > 0) && (j.cabecaChave < 100))
                {
                    meuJogo.nomeDesafiado = "(" + j.cabecaChave + ")" + j.desafiado.nome;
                }
                else
                {
                    meuJogo.nomeDesafiado = j.desafiado.nome;
                }
                meuJogo.fotoDesafiado = j.desafiado.fotoURL;
                if (j.desafiado2 != null)
                {
                    meuJogo.nomeDesafiadoDupla = j.desafiado2.nome;
                    meuJogo.fotoDesafiadoDupla = j.desafiado2.fotoURL;
                }
            }
            meuJogo.qtddGames1setDesafiado = j.qtddGames1setDesafiado;
            meuJogo.qtddGames1setDesafiante = j.qtddGames1setDesafiante;
            meuJogo.qtddGames2setDesafiado = j.qtddGames2setDesafiado;
            meuJogo.qtddGames2setDesafiante = j.qtddGames2setDesafiante;
            meuJogo.qtddGames3setDesafiado = j.qtddGames3setDesafiado;
            meuJogo.qtddGames3setDesafiante = j.qtddGames3setDesafiante;
            meuJogo.idDoVencedor = j.idDoVencedor;
            return meuJogo;
        }

        private int? getParceiroDuplaProximoJogo(Jogo jogoAnterior, int idJogadorPrincipal)
        {
            if (jogoAnterior.desafiado_id == idJogadorPrincipal)
            {
                return jogoAnterior.desafiado2_id;
            }
            else if (jogoAnterior.desafiante_id == idJogadorPrincipal)
            {
                return jogoAnterior.desafiante2_id;
            }
            else
            {
                return null;
            }
        }

        //private void CadastrarPerdedorNaRepescagem(Jogo jogo)
        //{
        //    // cadastrar perderdor
        //    int? primeiraFase = db.Jogo.Where(r => r.torneioId == jogo.torneioId && r.classeTorneio == jogo.classeTorneio).Max(r => r.faseTorneio);
        //    var jogosPrimeiraFase = db.Jogo.Where(r => r.torneioId == jogo.torneioId && r.classeTorneio == jogo.classeTorneio &&
        //        r.faseTorneio == primeiraFase && (r.desafiado_id == 0 || r.desafiante_id == 0)).OrderBy(r => r.ordemJogo).ToList();
        //    // para evitar que seja cadastrado duas vezes caso o placar seja alterado
        //    var isPerderdorJaCadastradoNaRepescagem = jogosPrimeiraFase.Where(j => j.torneioId == jogo.torneioId && j.classeTorneio == jogo.classeTorneio
        //        && j.faseTorneio == primeiraFase && (j.desafiado_id == jogo.idDoPerdedor || j.desafiado_id == jogo.idDoPerdedor)).Count();
        //    if (isPerderdorJaCadastradoNaRepescagem < 2)
        //    {
        //        foreach (Jogo j in jogosPrimeiraFase)
        //        {
        //            if (j.desafiado_id == 0)
        //            {
        //                j.desafiado_id = jogo.idDoPerdedor;
        //                j.desafiado2_id = getParceiroDuplaProximoJogo(jogo, jogo.idDoPerdedor);
        //                db.Entry(j).State = EntityState.Modified;
        //                db.SaveChanges();
        //                // verificar se caiu com o bye e avançar para próxima fase
        //                if (j.desafiante_id == 10)
        //                {
        //                    j.dataCadastroResultado = DateTime.Now;
        //                    j.usuarioInformResultado = User.Identity.Name;
        //                    j.situacao_Id = 5;
        //                    j.qtddGames1setDesafiado = 6;
        //                    j.qtddGames1setDesafiante = 1;
        //                    j.qtddGames2setDesafiado = 6;
        //                    j.qtddGames2setDesafiante = 1;
        //                    db.Entry(j).State = EntityState.Modified;
        //                    db.SaveChanges();
        //                    tc.MontarProximoJogoTorneio(j);
        //                }
        //                break;
        //            }
        //            if (j.desafiante_id == 0)
        //            {
        //                j.desafiante_id = jogo.idDoPerdedor;
        //                j.desafiante2_id = getParceiroDuplaProximoJogo(jogo, jogo.idDoPerdedor);
        //                db.Entry(j).State = EntityState.Modified;
        //                db.SaveChanges();
        //                break;
        //            }
        //        }
        //    }
        //}

        //private void cadastrarColocacaoPerdedorTorneio(Jogo jogo)
        //{
        //    if (jogo.idDoPerdedor == 10)
        //    {
        //        return; // sai se for o bye;
        //    }
        //    // cadastrar a colocação do perdedor no torneio 
        //    var qtddFases = db.Jogo.Where(r => r.torneioId == jogo.torneioId && r.classeTorneio == jogo.classeTorneio && r.faseTorneio < 100)
        //        .Max(r => r.faseTorneio);
        //    int colocacao = 0;
        //    if ((jogo.faseTorneio == 100) || (jogo.faseTorneio == 101))
        //    { // fase repescagem
        //        colocacao = 100;
        //    }
        //    else
        //    {
        //        colocacao = (int)jogo.faseTorneio;
        //    }
        //    var inscricao = db.InscricaoTorneio.Where(i => i.userId == jogo.idDoPerdedor && i.torneioId == jogo.torneioId).ToList();
        //    if (inscricao.Count() > 0)
        //    {
        //        inscricao[0].colocacao = colocacao;
        //        db.SaveChanges();
        //    }
        //}

        //private void MontarProximoJogoTorneio(Jogo jogo)
        //{
        //    var ordemJogo = 0;
        //    if (jogo.torneioId != null)
        //    {
        //        if (jogo.ordemJogo % 2 != 0)
        //        {
        //            ordemJogo = (int)(jogo.ordemJogo / 2) + 1;
        //        }
        //        else
        //        {
        //            ordemJogo = (int)(jogo.ordemJogo / 2);
        //        }
        //        var torneioId = jogo.torneioId;
        //        var torneio = db.Torneio.Find(torneioId);
        //        var classeId = jogo.classeTorneio;
        //        var isPrimeiroJogo = false;
        //        if (jogo.isPrimeiroJogoTorneio != null)
        //        {
        //            isPrimeiroJogo = (bool)jogo.isPrimeiroJogoTorneio;
        //        }
        //        if ((torneio.temRepescagem) && (isPrimeiroJogo))
        //        {
        //            CadastrarPerdedorNaRepescagem(jogo);
        //        }
        //        if (db.Jogo.Where(r => r.torneioId == jogo.torneioId && r.classeTorneio == jogo.classeTorneio &&
        //           r.faseTorneio == jogo.faseTorneio - 1 && r.ordemJogo == ordemJogo).Count() > 0)
        //        {
        //            var proximoJogo = db.Jogo.Where(r => r.torneioId == jogo.torneioId && r.classeTorneio == jogo.classeTorneio &&
        //                r.faseTorneio == jogo.faseTorneio - 1 && r.ordemJogo == ordemJogo).Single();
        //            if (jogo.desafiante_id == 10)
        //            {
        //                proximoJogo.isPrimeiroJogoTorneio = true;
        //            }
        //            else
        //            {
        //                proximoJogo.isPrimeiroJogoTorneio = false;
        //            }
        //            if (jogo.ordemJogo % 2 != 0)
        //            {
        //                proximoJogo.desafiado_id = jogo.idDoVencedor;
        //                proximoJogo.desafiado2_id = getParceiroDuplaProximoJogo(jogo, jogo.idDoVencedor);
        //                if (jogo.idDoVencedor == jogo.desafiado_id)
        //                {
        //                    proximoJogo.cabecaChave = jogo.cabecaChave;
        //                }
        //                else
        //                {
        //                    proximoJogo.cabecaChave = jogo.cabecaChaveDesafiante;
        //                }
        //            }
        //            else
        //            {
        //                proximoJogo.desafiante_id = jogo.idDoVencedor;
        //                proximoJogo.desafiante2_id = getParceiroDuplaProximoJogo(jogo, jogo.idDoVencedor);
        //                if (jogo.idDoVencedor == jogo.desafiado_id)
        //                {
        //                    proximoJogo.cabecaChaveDesafiante = jogo.cabecaChave;
        //                }
        //                else
        //                {
        //                    proximoJogo.cabecaChaveDesafiante = jogo.cabecaChaveDesafiante;
        //                }
        //            }
        //            cadastrarColocacaoPerdedorTorneio(jogo);
        //            db.SaveChanges();
        //        }
        //        else
        //        {
        //            // indicar o vencedor do torneio
        //            var inscricao = db.InscricaoTorneio.Where(i => i.userId == jogo.idDoVencedor && i.torneioId == jogo.torneioId).ToList();
        //            if (inscricao.Count() > 0)
        //            {
        //                inscricao[0].colocacao = 0; // vencedor
        //                db.SaveChanges();
        //            }
        //        }
        //    }
        //}

        private string getDescricaoFaseTorneio(int faseTorneio)
        {
            if (faseTorneio == 6)
            {
                return "Rodada 1";
            }
            if (faseTorneio == 5)
            {
                return "Rodada 2";
            }
            if (faseTorneio == 4)
            {
                return "Oitavas de Final";
            }
            if (faseTorneio == 3)
            {
                return "Quartas de Final";
            }
            if (faseTorneio == 2)
            {
                return "Semi-Final";
            }
            if (faseTorneio == 1)
            {
                return "Final";
            }
            return "";
        }

        [HttpGet]
        [Route("api/TorneioAPI/BuscaCidades/nome")]
        public IList<string> GetBuscaCidades(string nome)
        {
            List<string> cidades = new List<string>();
            if (nome.Length > 2)
            {
                var dataHoje = DateTime.Now;
                cidades = db.Torneio.Where(j => j.dataFim> dataHoje && j.isAtivo == true).OrderBy(j => j.cidade).Select(j => j.cidade).ToList<string>();
            }
            return cidades;
        }

        [HttpGet]
        [Route("api/TorneioAPI/cidade")]
        // GET: api/TorneioAPI/cidade
        public IList<TorneioApp> GetTorneioByCidade(string nome)
        {
            var dataHoje = DateTime.Now;
            var torneios = db.Torneio.Where(b => b.isAtivo == true && b.dataFim > dataHoje && b.cidade.ToLower() == nome.ToLower()).Select(rk => new TorneioApp()
            {
                Id = rk.Id,
                nome = rk.nome,
                dataFim = rk.dataFimInscricoes
            }).ToList();

            return torneios;
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("api/TorneioAPI/RegistroInscricao")]
        public MensagemRetorno PostRegistroInscricao(string login, string email, string password, string nome, DateTime dataNascimento, string naturalidade, string celular, string altura, string lateralidade, int classeInscricao2,  bool isMaisDeUmaClasse, int rankingId, int classeId, int torneioId, bool isSocio, bool isClasseDupla)
        {
            var model = new RegisterInscricao();
            model.register.UserName = login;
            model.register.Password = password;
            model.register.nome =nome;
            model.register.dataNascimento =dataNascimento;
            model.register.altura2 =altura;
            model.register.telefoneCelular =celular;
            model.register.email = email;
            model.register.lateralidade = lateralidade;
            model.register.barragemId = rankingId;
            model.inscricao.torneioId = torneioId;
            model.inscricao.classe = classeId;
            model.isMaisDeUmaClasse = isMaisDeUmaClasse;
            model.classeInscricao2 = classeInscricao2;

            var mensagemRetorno = new AccountController().RegisterTorneioNegocio(model, torneioId, isSocio, isClasseDupla);
            return mensagemRetorno;
        }

        [ResponseType(typeof(void))]
        [HttpPost]
        [AllowAnonymous]
        [Route("api/TorneioAPI/Inscricao")]
        public IHttpActionResult PostInscricao(int torneioId, int classeInscricao, string operacao, bool isMaisDeUmaClasse = false, int classeInscricao2 = 0, string observacao = "", bool isSocio = false, bool isClasseDupla = false, int userId = 0)
        {
            var mensagemRetorno = new TorneioController().InscricaoNegocio(torneioId, classeInscricao, operacao, isMaisDeUmaClasse, classeInscricao2, observacao, isSocio, isClasseDupla, userId);
            if (mensagemRetorno.nomePagina == "ConfirmacaoInscricao")
            {
                return StatusCode(HttpStatusCode.NoContent);
            }
            else {
                return InternalServerError(new Exception(mensagemRetorno.mensagem));
            }
            
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("api/TorneioAPI/Disponivel/{rankingId}")]
        public IList<TorneioApp> GetTorneioDisponivel(int rankingId)
        {

            var dataHoje = DateTime.Now;
            var ranking = db.BarragemView.Find(rankingId);
            var torneio = (from t in db.Torneio
                           where t.dataFimInscricoes >= dataHoje && t.isAtivo && t.isOpen
                           select new TorneioApp
                           {
                               Id = t.Id, logoId = t.barragemId, nome = t.nome, dataInicio = t.dataInicio, valor = t.valor, valorSocio = t.valorSocio,
                               dataFim = t.dataFim, cidade = t.cidade, premiacao = t.premiacao, contato = t.barragem.email
                           }).Union(
                            from t in db.Torneio
                            where t.dataFimInscricoes >= dataHoje && t.isAtivo && t.divulgaCidade 
                            && t.cidade.ToUpper() == ranking.cidade.ToUpper()
                            select new TorneioApp
                            {
                                Id = t.Id, logoId = t.barragemId, nome = t.nome, dataInicio = t.dataInicio, valor = t.valor, valorSocio = t.valorSocio,
                                dataFim = t.dataFim, cidade = t.cidade, premiacao = t.premiacao, contato = t.barragem.email
                            }).Union(
                            from t in db.Torneio
                            where t.dataFimInscricoes >= dataHoje && t.isAtivo && t.barragemId == rankingId
                            select new TorneioApp
                            {
                                Id = t.Id, logoId = t.barragemId, nome = t.nome, dataInicio = t.dataInicio, valor = t.valor, valorSocio = t.valorSocio,
                                dataFim = t.dataFim, cidade = t.cidade, premiacao = t.premiacao, contato = t.barragem.email
                            }).Distinct<TorneioApp>().ToList();

            return torneio;
           
        }

    }
}
