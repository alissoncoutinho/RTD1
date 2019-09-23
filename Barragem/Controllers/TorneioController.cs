using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Barragem.Models;
using Barragem.Context;
using Barragem.Filters;
using Barragem.Class;
using System.Web.Security;
using WebMatrix.WebData;
using Uol.PagSeguro.Constants;
using Uol.PagSeguro.Domain;
using Uol.PagSeguro.Exception;

namespace Barragem.Controllers
{
    [InitializeSimpleMembership]
    public class TorneioController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();
        //

        [HttpPost]
        [Authorize(Roles = "admin,usuario,organizador")]
        public ActionResult AlterarClassesTorneio(IEnumerable<InscricaoTorneio> inscricaoTorneio)
        {
            try
            {
                InscricaoTorneio jogador = null;
                foreach (InscricaoTorneio user in inscricaoTorneio)
                {
                    jogador = db.InscricaoTorneio.Find(user.Id);
                    jogador.classe = user.classe;
                    db.Entry(jogador).State = EntityState.Modified;
                    db.SaveChanges();
                }
                return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        [Authorize(Roles = "admin,organizador")]
        public ActionResult ExcluirInscricao(int Id)
        {
            var torneioId = 0;
            try
            {
                InscricaoTorneio inscricao = db.InscricaoTorneio.Find(Id);
                torneioId = inscricao.torneioId;
                db.InscricaoTorneio.Remove(inscricao);
                db.SaveChanges();
                return RedirectToAction("EditInscritos", new { torneioId = torneioId, Msg = "OK" });
            }
            catch (Exception ex)
            {
                return RedirectToAction("EditInscritos", new { torneioId = torneioId, Msg = ex.Message });
            }
        }

        [Authorize(Roles = "admin,organizador")]
        public ActionResult ExcluirClasse(int Id)
        {
            try
            {
                var inscritos = db.InscricaoTorneio.Where(i => i.classe == Id && i.isAtivo).Count();
                if (inscritos > 0)
                {
                    return Json(new { erro = "Já existem jogadores inscritos nesta classe. Para excluí-la é necessário inativar as inscrições.", retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
                }
                ClasseTorneio classe = db.ClasseTorneio.Find(Id);
                db.ClasseTorneio.Remove(classe);
                db.SaveChanges();
                return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        [Authorize(Roles = "admin,usuario,organizador")]
        public ActionResult AlterarClasse(int torneioId)
        {
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            var inscricoes = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.participante.UserId == userId).ToList();
            var classes = db.ClasseTorneio.Where(i => i.torneioId == torneioId).OrderBy(c => c.nome).ToList();
            ViewBag.ClasseTorneio = classes;
            if (inscricoes.Count() > 1)
            {
                ViewBag.Classe2Opcao = inscricoes[1].classe;
                var classes2 = db.ClasseTorneio.Where(i => i.torneioId == torneioId).OrderBy(c => c.nome).ToList();
                ViewBag.ClasseTorneio2 = classes2;
            }
            return View(inscricoes[0]);
        }

        [HttpPost]
        [Authorize(Roles = "admin,usuario,organizador")]
        public ActionResult AlterarClasse(int inscricaoId, int classe, int classe2Opcao = 0)
        {
            var inscricao = db.InscricaoTorneio.Find(inscricaoId);
            if (classe2Opcao == 0)
            {
                inscricao.classe = classe;
                db.Entry(inscricao).State = EntityState.Modified;
            }
            else
            {
                var userId = WebSecurity.GetUserId(User.Identity.Name);
                var inscricoes = db.InscricaoTorneio.Where(i => i.torneioId == inscricao.torneioId && i.participante.UserId == userId).ToList();
                inscricoes[0].classe = classe;
                inscricoes[1].classe = classe2Opcao;
                db.Entry(inscricoes[0]).State = EntityState.Modified;
                db.Entry(inscricoes[1]).State = EntityState.Modified;
            }
            db.SaveChanges();
            return RedirectToAction("Detalhes", new { id = inscricao.torneioId, Msg = "OK" });
        }


        [Authorize(Roles = "admin,organizador")]
        public ActionResult Index()
        {
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            List<Torneio> torneio = null;
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            int barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
            if (perfil.Equals("admin"))
            {
                torneio = db.Torneio.OrderByDescending(c => c.Id).ToList();
            }
            else
            {
                torneio = db.Torneio.Where(r => r.barragemId == barragemId).OrderByDescending(c => c.Id).ToList();
            }
            var barragem = db.BarragemView.Find(barragemId);
            ViewBag.isBarragemAtiva = barragem.isAtiva;
            return View(torneio);
        }

        [Authorize(Roles = "admin,usuario,organizador")]
        public ActionResult TorneiosDisponiveis()
        {
            List<Torneio> torneio = null;
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            var barragem = (from up in db.UserProfiles where up.UserId == userId select up.barragem).Single();
            var agora = DateTime.Now;
            torneio = db.Torneio.Where(r => r.isAtivo && r.dataFim > agora && (r.barragemId == barragem.Id || r.isOpen || (r.divulgaCidade && r.barragem.cidade == barragem.cidade))).OrderByDescending(c => c.Id).ToList();


            return View(torneio);
        }

        [Authorize(Roles = "admin,usuario,organizador")]
        public ActionResult EscolherDupla(int torneioId, int classe = 0, bool segundaClasseDupla = false, int userId=0)
        {

            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            if (perfil.Equals("usuario")|| userId==0){
                userId = WebSecurity.GetUserId(User.Identity.Name);
            }
            var inscricao = db.InscricaoTorneio.Where(i => i.userId == userId && i.torneioId == torneioId && i.classeTorneio.isDupla).OrderBy(i=>i.Id).ToList();
            IQueryable<InscricaoTorneio> inscricoesRealizadas = null;
            List<InscricaoTorneio> selectInscricoesDisp = null;
            List<InscricaoTorneio> inscricoesDuplas = null;
            ViewBag.isDisponivel = false;
            ViewBag.torneioId = torneioId;
            ViewBag.userId = userId;
            if (inscricao.Count() > 0)
            {
                if (classe == 0)
                {
                    classe = inscricao[0].classe;
                    ViewBag.classeNome = inscricao[0].classeTorneio.nome;
                    if (segundaClasseDupla) {
                        classe = inscricao[1].classe;
                        ViewBag.classeNome = inscricao[1].classeTorneio.nome;
                    }
                }
                
                inscricoesRealizadas = db.InscricaoTorneio.Where(c => c.torneioId == torneioId && c.classe == classe && c.parceiroDuplaId > 0).OrderBy(c => c.participante.nome);
                var inscricoesDisponiveis = db.InscricaoTorneio.Where(c => c.torneioId == torneioId && c.classe == classe && (c.parceiroDuplaId == null || c.parceiroDuplaId == 0));
                var inscricoesIndisp = from insD in inscricoesDisponiveis join insR in inscricoesRealizadas on insD.userId equals insR.parceiroDuplaId select insD;
                var InscricoesDisp = (from i in inscricoesDisponiveis where !inscricoesIndisp.Contains(i) select i);
                var isDisponivel = InscricoesDisp.Where(i => i.userId == userId).Count();
                if (isDisponivel > 0) { ViewBag.isDisponivel = true; }
                selectInscricoesDisp = InscricoesDisp.Where(i => i.userId != userId).OrderBy(i => i.participante.nome).ToList();
                inscricoesDuplas = selectInscricoesDisp;
                inscricoesDuplas.AddRange(inscricoesRealizadas.ToList());
            }
            ViewBag.temMaisClassesDuplas = false;
            if ((!segundaClasseDupla)&&(inscricao.Count()>1)){
                ViewBag.temMaisClassesDuplas = true;
            }
            return View(inscricoesDuplas);
        }

        [Authorize(Roles = "admin,organizador")]
        [HttpPost]
        public ActionResult MontarChaveamento(int torneioId, IEnumerable<int> classeIds = null)
        {
            List<ClasseTorneio> classes = new List<ClasseTorneio>();
            if (classeIds == null) { 
                db.Database.ExecuteSqlCommand("delete from jogo where torneioId=" + torneioId);
                classes = db.ClasseTorneio.Where(c => c.torneioId == torneioId).ToList();
            } else {
                foreach (int classeId in classeIds){
                    db.Database.ExecuteSqlCommand("delete from jogo where classeTorneio=" + classeId);
                    var classe = db.ClasseTorneio.Find(classeId);
                    classes.Add(classe);
                }
            }
            List<InscricaoTorneio> inscricoes = null;
            var torneio = db.Torneio.Find(torneioId);
            var qtddJogadores = 0;
            
            foreach (ClasseTorneio classe in classes)
            {
                if (classe.isDupla){
                    inscricoes = db.InscricaoTorneio.Where(r => r.torneioId == torneioId && r.classe == classe.Id && r.isAtivo && r.parceiroDuplaId != null).ToList();
                }else{
                    inscricoes = db.InscricaoTorneio.Where(r => r.torneioId == torneioId && r.classe == classe.Id && r.isAtivo).ToList();
                }
                qtddJogadores = inscricoes.Count();
                if (qtddJogadores < 2) continue;
                //if (torneio.temRepescagem){
                //    qtddJogadores = montarRepescagem(torneio.Id, qtddJogadores, classe.Id);
                //}
                montarFaseEliminatoria(torneio.Id, qtddJogadores, classe.Id, torneio.temRepescagem);
                var qtddByes = getQtddByes(qtddJogadores);
                if (torneio.temRepescagem)
                {
                    qtddJogadores = qtddJogadores + (qtddJogadores / 2);
                    qtddByes = getQtddByes(qtddJogadores);
                    if (qtddJogadores == 16)
                    {
                        qtddJogadores++;
                        qtddByes++;
                    }
                }


                popularJogosIniciais(torneio.Id, classe.Id, torneio.temRepescagem, inscricoes, qtddByes);
            }
            return RedirectToAction("EditJogos", new { torneioId = torneioId });
        }

        private void montarJogosComByes(List<Jogo> jogosRodada1, int qtddByes, bool temRepescagem, int qtddIncritos)
        {
            var qtddJogos = jogosRodada1.Count();
            bool umaVez = true;
            for (int i = 1; i <= qtddByes; i++)
            {
                var listJogoCChave = db.JogoCabecaChave.Where(j => j.cabecaChave == i && j.chaveamento == qtddJogos && j.temRepescagem==temRepescagem).ToList();
                Jogo jogo = null;
                if ((listJogoCChave.Count() > 0) && (!temRepescagem))
                {
                    var ordemJogo = listJogoCChave[0].ordemJogo;
                    jogo = jogosRodada1.Where(j => j.ordemJogo == ordemJogo).FirstOrDefault();
                }
                else
                {
                    if (((qtddIncritos / 2) % 2 != 0) && (umaVez))
                    {
                        umaVez = false;
                        jogo = jogosRodada1.Where(j => j.desafiante_id == 0).OrderByDescending(j => j.ordemJogo).FirstOrDefault();
                    }
                    else
                    {
                        jogo = jogosRodada1.Where(j => j.desafiante_id == 0).OrderBy(j => j.ordemJogo).FirstOrDefault();
                    }
                }
                jogo.desafiante_id = 10;
                db.Entry(jogo).State = EntityState.Modified;
                db.SaveChanges();
            }
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

        private void CadastrarPerdedorNaRepescagem(Jogo jogo)
        {
            // cadastrar perderdor
            int? primeiraFase = db.Jogo.Where(r => r.torneioId == jogo.torneioId && r.classeTorneio == jogo.classeTorneio).Max(r => r.faseTorneio);
            var jogosPrimeiraFase = db.Jogo.Where(r => r.torneioId == jogo.torneioId && r.classeTorneio == jogo.classeTorneio &&
                r.faseTorneio == primeiraFase && (r.desafiado_id == 0 || r.desafiante_id == 0)).OrderBy(r => r.ordemJogo).ToList();
            // para evitar que seja cadastrado duas vezes caso o placar seja alterado
            var isPerderdorJaCadastradoNaRepescagem = jogosPrimeiraFase.Where(j => j.torneioId == jogo.torneioId && j.classeTorneio == jogo.classeTorneio
                && j.faseTorneio == primeiraFase && (j.desafiado_id == jogo.idDoPerdedor || j.desafiado_id == jogo.idDoPerdedor)).Count();
            if (isPerderdorJaCadastradoNaRepescagem < 2)
            {
                foreach (Jogo j in jogosPrimeiraFase)
                {
                    if (j.desafiado_id == 0)
                    {
                        j.desafiado_id = jogo.idDoPerdedor;
                        j.desafiado2_id = getParceiroDuplaProximoJogo(jogo, jogo.idDoPerdedor);
                        db.Entry(j).State = EntityState.Modified;
                        db.SaveChanges();
                        // verificar se caiu com o bye e avançar para próxima fase
                        if (j.desafiante_id == 10)
                        {
                            j.dataCadastroResultado = DateTime.Now;
                            j.usuarioInformResultado = User.Identity.Name;
                            j.situacao_Id = 5;
                            j.qtddGames1setDesafiado = 6;
                            j.qtddGames1setDesafiante = 1;
                            j.qtddGames2setDesafiado = 6;
                            j.qtddGames2setDesafiante = 1;
                            db.Entry(j).State = EntityState.Modified;
                            db.SaveChanges();
                            MontarProximoJogoTorneio(j);
                        }
                        break;
                    }
                    if (j.desafiante_id == 0)
                    {
                        j.desafiante_id = jogo.idDoPerdedor;
                        j.desafiante2_id = getParceiroDuplaProximoJogo(jogo, jogo.idDoPerdedor);
                        db.Entry(j).State = EntityState.Modified;
                        db.SaveChanges();
                        break;
                    }
                }
            }
        }

        private void montarJogosComCabecaDeChave(List<Jogo> jogosRodada1, List<InscricaoTorneio> cabecasDeChave, bool temRepescagem)
        {
            var chaveamento = jogosRodada1.Count();
            var ordemJogo = 0;
            foreach (InscricaoTorneio cabecaDeChave in cabecasDeChave)
            {
                var numCabecaChave = cabecaDeChave.cabecaChave;
                var listJogoCChave = db.JogoCabecaChave.Where(j => j.cabecaChave == numCabecaChave && j.chaveamento == chaveamento && j.temRepescagem == temRepescagem).ToList();
                if (listJogoCChave.Count() > 0)
                {
                    ordemJogo = listJogoCChave[0].ordemJogo;

                    var jogo = jogosRodada1.Where(j => j.ordemJogo == ordemJogo).ToList();
                    if (jogo.Count() > 0)
                    {
                        if (jogo[0].desafiante_id != 10)
                        {
                            jogo[0].isPrimeiroJogoTorneio = true;
                        }
                        jogo[0].desafiado_id = cabecaDeChave.userId;
                        jogo[0].desafiado2_id = cabecaDeChave.parceiroDuplaId;
                        jogo[0].cabecaChave = numCabecaChave;
                        db.Entry(jogo[0]).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
            }
        }

        private void montarJogosPorSorteio(List<Jogo> jogosRodada1, List<InscricaoTorneio> inscritos, bool temRepescagem)
        {
            InscricaoTorneio jogador = null;
            foreach (Jogo jogo in jogosRodada1) { 
                if (jogo.desafiante_id == 0){
                    jogador = selecionarAdversario(inscritos);
                    jogo.desafiante_id = jogador.userId;
                    jogo.isPrimeiroJogoTorneio = true;
                    if (jogador.classeTorneio.isDupla)
                    {
                        jogo.desafiante2_id = jogador.parceiroDuplaId;
                    }
                }
                if (jogo.desafiado_id == 0){
                    jogador = selecionarAdversario(inscritos);
                    if (jogo.desafiante_id != 10){
                        jogo.isPrimeiroJogoTorneio = true;
                    }
                    jogo.desafiado_id = jogador.userId;
                    if ((jogador.classeTorneio != null) && (jogador.classeTorneio.isDupla)){
                        jogo.desafiado2_id = jogador.parceiroDuplaId;
                    }
                }
                db.Entry(jogo).State = EntityState.Modified;
                db.SaveChanges();
            }
            if (temRepescagem)
            {
                // indicar os jogos que são de repescagem
                var jogosRepescagem = jogosRodada1.Where(j => j.desafiado_id == 0 || j.desafiante_id == 0).ToList();
                foreach (Jogo jogo in jogosRepescagem)
                {
                    jogo.isRepescagem = true;
                    db.Entry(jogo).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }
        }

        private void popularJogosFase1(List<Jogo> jogosRodada1, List<InscricaoTorneio> inscritos, int qtddByes, bool temRepescagem)
        {
            montarJogosComByes(jogosRodada1, qtddByes, temRepescagem, inscritos.Count); // montar os byes utilizando a tabela de cabecaChave (colocar os byes no desafiante);
            var cabecasDeChave = inscritos.Where(i => i.cabecaChave != null && i.cabecaChave != 100).ToList();
            montarJogosComCabecaDeChave(jogosRodada1, cabecasDeChave, temRepescagem); //Montar os cabecaChave utilizando a tabela de cabecaChave (colocar os cabecaChave no desafiado);
            var chaveamento = jogosRodada1.Count();
            var qtddCabecaChavesValidos = db.JogoCabecaChave.Where(j => j.chaveamento == chaveamento && j.temRepescagem == temRepescagem).Count();
            var inscritosAleatorios = inscritos.Where(i => i.cabecaChave > qtddCabecaChavesValidos || i.cabecaChave == null).ToList();
            montarJogosPorSorteio(jogosRodada1, inscritosAleatorios, temRepescagem); //Montar o Restante da tabela utilizando um critério de sorteio;
        }

        private void fecharJogosComBye(List<Jogo> jogosRodada1)
        {
            foreach (Jogo jogo in jogosRodada1)
            {
                if (jogo.desafiante_id == 10)
                {
                    jogo.dataCadastroResultado = DateTime.Now;
                    jogo.usuarioInformResultado = User.Identity.Name;
                    jogo.situacao_Id = 5;
                    jogo.qtddGames1setDesafiado = 6;
                    jogo.qtddGames1setDesafiante = 1;
                    jogo.qtddGames2setDesafiado = 6;
                    jogo.qtddGames2setDesafiante = 1;
                    db.Entry(jogo).State = EntityState.Modified;
                    db.SaveChanges();
                    MontarProximoJogoTorneio(jogo);
                }

            }
        }

        private void popularJogosIniciais(int torneioId, int classeId, bool temRepescagem, List<InscricaoTorneio> inscritos, int qtddByes)
        {
            var jogo = db.Jogo.Where(j => j.torneioId == torneioId && j.classeTorneio == classeId).OrderByDescending(j => j.faseTorneio).First<Jogo>();
            var primeiraFase = (int)jogo.faseTorneio;
            var jogosRodada1 = db.Jogo.Where(j => j.torneioId == torneioId && j.classeTorneio == classeId && j.faseTorneio == primeiraFase).OrderBy(j => j.ordemJogo).ToList();
            popularJogosFase1(jogosRodada1, inscritos, qtddByes, temRepescagem);
            fecharJogosComBye(jogosRodada1);
        }

        private void montarFaseEliminatoria(int torneioId, int qtddJogadores, int classeId, bool temRepescagem)
        {
            if (temRepescagem)
            {
                qtddJogadores = qtddJogadores + (qtddJogadores / 2);
                if (qtddJogadores == 16) qtddJogadores++;
            }
            var qtddRodada = getQtddRodada(qtddJogadores);
            var qtddJogosDaRodada = 1;
            for (int fase = 1; fase <= qtddRodada; fase++)
            {
                for (int ordemJogo = 1; ordemJogo <= qtddJogosDaRodada; ordemJogo++)
                {
                    criarJogo(0, 0, torneioId, classeId, fase, ordemJogo);
                }
                qtddJogosDaRodada = qtddJogosDaRodada * 2;
            }
        }

        [Authorize(Roles = "admin,organizador")]
        public ActionResult TesteProcessarJogosComBye()
        {
            ProcessarJogosComBye(1, 1);
            return View();
        }
        private void ProcessarJogosComBye(int torneioId, int classeId)
        {
            var jogos = db.Jogo.Where(r => r.torneioId == torneioId && r.classeTorneio == classeId && r.faseTorneio == 1 && r.desafiante_id == 0 && r.situacao_Id == 4).ToList();
            foreach (Jogo jogo in jogos)
            {
                int ordemJogo = 0;
                if (jogo.ordemJogo % 2 != 0)
                {
                    int var1 = 1;
                    int teste = (var1 / 2) + 1;
                    ordemJogo = (int)(jogo.ordemJogo / 2) + 1;
                    var proximoJogo = db.Jogo.Where(r => r.torneioId == torneioId && r.classeTorneio == classeId && r.faseTorneio == 2 && r.ordemJogo == ordemJogo).Single();
                    proximoJogo.desafiado_id = jogo.desafiado_id;
                    proximoJogo.desafiado2_id = jogo.desafiado2_id;
                }
                else
                {
                    ordemJogo = (int)jogo.ordemJogo / 2;
                    var proximoJogo = db.Jogo.Where(r => r.torneioId == torneioId && r.classeTorneio == classeId && r.faseTorneio == 2 && r.ordemJogo == ordemJogo).Single();
                    proximoJogo.desafiante_id = jogo.desafiado_id;
                    proximoJogo.desafiante2_id = jogo.desafiado2_id;
                }
                db.SaveChanges();
            }
        }

        private int getQtddJogosPorRodada(int numeroRodada)
        {
            if (numeroRodada == 6)
            {
                return 32;
            }
            else if (numeroRodada == 5)
            {
                return 16;
            }
            else if (numeroRodada == 4)
            {
                return 8;
            }
            else if (numeroRodada == 3)
            {
                return 4;
            }
            else if (numeroRodada == 2)
            {
                return 2;
            }
            else if (numeroRodada == 1)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        private int getQtddRodadas(int qtddJogos)
        {
            if (qtddJogos == 64)
            {
                return 6;
            }
            else if (qtddJogos == 32)
            {
                return 5;
            }
            else if (qtddJogos == 16)
            {
                return 4;
            }
            else if (qtddJogos == 8)
            {
                return 3;
            }
            else if (qtddJogos == 4)
            {
                return 2;
            }
            else if (qtddJogos == 2)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        private void criarJogo(int jogador1, int jogador2, int torneioId, int classeTorneio, int faseTorneio, int ordemJogo)
        {
            Jogo jogo = new Jogo();
            jogo.desafiado_id = jogador1;
            jogo.desafiante_id = jogador2;
            jogo.torneioId = torneioId;
            jogo.situacao_Id = 1;
            jogo.classeTorneio = classeTorneio;
            jogo.faseTorneio = faseTorneio;
            jogo.ordemJogo = ordemJogo;
            if ((jogador2 == 10) && (jogador1 != 0))
            {
                jogo.situacao_Id = 5;
                jogo.qtddGames1setDesafiado = 6;
                jogo.qtddGames2setDesafiado = 6;
                jogo.qtddGames1setDesafiante = 1;
                jogo.qtddGames2setDesafiante = 1;

            }
            db.Jogo.Add(jogo);
            db.SaveChanges();
        }

        private InscricaoTorneio selecionarAdversario(List<InscricaoTorneio> participantes)
        {
            var adversario = new InscricaoTorneio();
            if (participantes.Count == 0)
            {
                adversario.Id = 0;
                return adversario;
            }
            else if (participantes.Count == 1)
            {
                adversario = participantes[0]; //add it 
                participantes.RemoveAt(0);
                return adversario;
            }
            Random r = new Random();
            int randomIndex = r.Next(1, participantes.Count); //Choose a random object in the list
            adversario = participantes[randomIndex]; //add it 
            participantes.RemoveAt(randomIndex);
            return adversario;
        }

        [Authorize(Roles = "admin, organizador")]
        public ActionResult ListarJogos(int torneioId)
        {
            string msg = "";
            try
            {
                var jogo = db.Jogo.Include(j => j.desafiado).Include(j => j.desafiante).
                    Where(j => j.torneioId == torneioId && j.desafiado_id != 0 && j.desafiante_id != 0).OrderBy(j => j.classeTorneio).ThenBy(r => r.faseTorneio).ThenBy(r => r.ordemJogo).ToList();
                var torneio = db.Torneio.Find(torneioId);
                ViewBag.Classes = db.Classe.Where(c => c.barragemId == torneio.barragemId).ToList();
                return View(jogo);
            }
            catch (Exception ex)
            {
                ViewBag.MsgErro = msg + " - " + ex.Message;
                return View();
            }

        }

        private void mensagem(string Msg)
        {
            if (Msg != "")
            {
                if (Msg.ToUpper() == "OK")
                {
                    ViewBag.Ok = "Operação realizada com sucesso!";
                }
                else
                {
                    ViewBag.MsgErro = Msg;
                }
            }
        }

        [Authorize(Roles = "admin,organizador")]
        public ActionResult InscricoesTorneio(int torneioId, string Msg = "")
        {
            var inscricoes = db.InscricaoTorneio.Where(r => r.torneioId == torneioId).OrderBy(r => r.classe).ThenBy(r => r.participante.nome).ToList();

            mensagem(Msg);

            return View(inscricoes);
        }

        public ActionResult Tabela(int torneioId = 0, int filtroClasse = 0, string Msg = "", string Url = "", int barra = 0)
        {
            if (Url == "torneio")
            {
                ViewBag.Torneio = "Sim";
            }
            ViewBag.tabelaLiberada = false;
            if (torneioId == 0)
            {
                HttpCookie cookie = Request.Cookies["_barragemId"];
                if (cookie != null)
                {
                    var barragemId = Convert.ToInt32(cookie.Value.ToString());
                    BarragemView barragem = db.BarragemView.Find(barragemId);
                    var tn = db.Torneio.Where(t => t.barragemId == barragemId && t.isAtivo).OrderByDescending(t => t.Id).ToList();
                    if (tn.Count() == 0)
                    {
                        mensagem("Não localizamos nenhum torneio ativo no seu ranking.");
                        return View();
                    }
                    torneioId = tn[0].Id;
                }
                else if (barra != 0)
                {
                    BarragemView barragem = db.BarragemView.Find(barra);
                    var tn = db.Torneio.Where(t => t.barragemId == barra && t.isAtivo).OrderByDescending(t => t.Id).ToList();
                    if (tn.Count() == 0){
                        mensagem("Não localizamos nenhum torneio ativo no seu ranking.");
                        return View();
                    }
                    torneioId = tn[0].Id;
                    Funcoes.CriarCookieBarragem(Response, Server, barragem.Id, barragem.nome);
                }
            }
            var torneio = db.Torneio.Find(torneioId);
            if (torneioId == 0)
            {
                mensagem("Não localizamos nenhum torneio.");
                return View();
            }
            if (torneio.liberarTabela) ViewBag.tabelaLiberada = torneio.liberarTabela;
            if (filtroClasse == 0)
            {
                var classe = db.ClasseTorneio.Where(c => c.torneioId == torneioId).OrderBy(c => c.nivel).ToList();
                filtroClasse = classe[0].Id;
            }
            var jogos = db.Jogo.Where(r => r.torneioId == torneioId && r.classeTorneio == filtroClasse && r.faseTorneio != 100 && r.faseTorneio != 101).OrderByDescending(r => r.faseTorneio).ThenBy(r => r.ordemJogo).ToList();
            ViewBag.Classes = db.ClasseTorneio.Where(c => c.torneioId == torneioId).ToList();
            ViewBag.torneioId = torneioId;
            ViewBag.nomeTorneio = torneio.nome;
            ViewBag.filtroClasse = filtroClasse;

            mensagem(Msg);
            return View(jogos);
        }

        public ActionResult InscricoesTorneio2(int torneioId = 0, string Msg = "", string Url = "", int barra = 0)
        {
            List<InscricaoTorneio> inscricoes = null;
            bool liberarTabelaInscricao = false;
            if (torneioId == 0)
            {
                HttpCookie cookie = Request.Cookies["_barragemId"];
                if (cookie != null)
                {
                    var barragemId = Convert.ToInt32(cookie.Value.ToString());
                    BarragemView barragem = db.BarragemView.Find(barragemId);
                    var torneio = db.Torneio.Where(t => t.barragemId == barragemId && t.isAtivo).OrderByDescending(t => t.Id).ToList();
                    torneioId = torneio[0].Id;
                    liberarTabelaInscricao = torneio[0].liberaTabelaInscricao;
                }
                else if (barra != 0)
                {
                    BarragemView barragem = db.BarragemView.Find(barra);
                    var torneio = db.Torneio.Where(t => t.barragemId == barra && t.isAtivo).OrderByDescending(t => t.Id).ToList();
                    liberarTabelaInscricao = torneio[0].liberaTabelaInscricao;
                    torneioId = torneio[0].Id;
                    Funcoes.CriarCookieBarragem(Response, Server, barragem.Id, barragem.nome);
                }
            }else
            {
                var torneio = db.Torneio.Find(torneioId);
                liberarTabelaInscricao = torneio.liberaTabelaInscricao;
            }
            inscricoes = db.InscricaoTorneio.Where(r => r.torneioId == torneioId && r.classeTorneio.isDupla == false).OrderBy(r => r.classe).ThenBy(r => r.participante.nome).ToList();
            var inscricoesDupla = db.InscricaoTorneio.Where(r => r.torneioId == torneioId && r.classeTorneio.isDupla == true).OrderBy(r => r.classe).ThenBy(r => r.participante.nome).ToList();

            List<InscricaoTorneio> inscricoesRemove = new List<InscricaoTorneio>();
            foreach (var ins in inscricoesDupla){
                var formouDupla = inscricoesDupla.Where(i => i.parceiroDuplaId == ins.userId && i.classe==ins.classe).Count();
                if (formouDupla > 0){
                    inscricoesRemove.Add(ins);
                }
            }
            foreach (var ins in inscricoesRemove){
                inscricoesDupla.Remove(ins);
            }
            ViewBag.inscricoesDupla = inscricoesDupla;
            ViewBag.liberaTabelaInscricao = liberarTabelaInscricao;
            mensagem(Msg);

            if (Url == "torneio"){
                ViewBag.Torneio = "Sim";
            }
            return View(inscricoes);
        }

        [Authorize(Roles = "admin,organizador,usuario")]
        public ActionResult EfetuarPagamento(int inscricaoId)
        {
            string MsgErro = "";
            var inscricao = db.InscricaoTorneio.Find(inscricaoId);
            try
            {
                var barragem = db.BarragemView.Find(inscricao.torneio.barragemId);
                Uri paymentRedirectUri = Register(inscricao, barragem.tokenPagSeguro, barragem.emailPagSeguro);
                Response.Redirect(paymentRedirectUri.AbsoluteUri);
            }
            catch (Exception e)
            {
                MsgErro = e.Message;
            }
            return RedirectToAction("ConfirmacaoInscricao", new { torneioId = inscricao.torneioId, msgErro = MsgErro });
        }

        public ActionResult Regulamento(int barragem)
        {
            Barragens barra = db.Barragens.Find(barragem);

            return View(barra);
        }


        private Uri Register(InscricaoTorneio inscricao, string token, string email)
        {
            //Use global configuration
            //PagSeguroConfiguration.UrlXmlConfiguration = "../../../../../PagSeguroConfig.xml";

            //bool isSandbox = true;
            //EnvironmentConfiguration.ChangeEnvironment(isSandbox);

            // Instantiate a new payment request
            PaymentRequest payment = new PaymentRequest();

            // Sets the currency
            payment.Currency = Currency.Brl;

            // Add an item for this payment request
            payment.Items.Add(new Uol.PagSeguro.Domain.Item(inscricao.torneioId + "", inscricao.torneio.nome, 1, (decimal)inscricao.valor));

            // Sets a reference code for this payment request, it is useful to identify this payment in future notifications.
            payment.Reference = "T-" + inscricao.Id;

            // Sets your customer information.
            //payment.Sender = new Sender(inscricao.participante.nome,inscricao.participante.email,new Phone("61", "99999999"));
            string[] arrayNomes = inscricao.participante.nome.Trim().Split(' ');
            var nome = inscricao.participante.nome.Trim();
            if (arrayNomes.Length == 1)
            {
                nome = nome + " Sobrenome";
            }
            nome = nome.Replace("-", "").Replace(".", "");
            var ddd = "";
            var cel = "";
            var telefone = inscricao.participante.telefoneCelular.Trim().Replace("(", "").Replace(")", "").Replace(" ", "").Replace("-", "");
            if (telefone.Length < 10) {
                ddd = "61";
                cel = "984086580";
            }else{
                ddd = telefone.Substring(0, 2);
                cel = telefone.Substring(2);
            }
            
            payment.Sender = new Sender(nome, inscricao.participante.email.Trim(), new Phone(ddd, cel));

            //SenderDocument document = new SenderDocument(Documents.GetDocumentByType("CPF"), "12345678909");
            //payment.Sender.Documents.Add(document);

            // Sets the url used by PagSeguro for redirect user after ends checkout process
            // payment.RedirectUri = new Uri("http://www.rankingdetenis.com.br/ConfirmacaoPagamento");

            try
            {
                /// Create new account credentials
                /// This configuration let you set your credentials from your ".cs" file.
                //AccountCredentials credentials = new AccountCredentials("backoffice@lojamodelo.com.br", "256422BF9E66458CA3FE41189AD1C94A");

                /// @todo with you want to get credentials from xml config file uncommend the line below and comment the line above.
                //AccountCredentials credentials = PagSeguroConfiguration.Credentials(isSandbox);
                AccountCredentials credentials = new AccountCredentials(email, token);

                return payment.Register(credentials);
            }
            catch (PagSeguroServiceException exception)
            {
                Console.WriteLine(exception.Message + "\n");

                foreach (ServiceError element in exception.Errors)
                {
                    Console.WriteLine(element + "\n");
                }
                throw new ArgumentException("Erro ao registrar pagamento", exception.Message);
                //Console.ReadKey();
            }
        }

        [Authorize(Roles = "admin,organizador")]
        public ActionResult EditInscritos(int torneioId, int filtroClasse = 0, string Msg = "")
        {

            List<InscricaoTorneio> inscricao = db.InscricaoTorneio.Where(i => i.torneioId == torneioId).ToList();
            if (filtroClasse != 0)
            {
                inscricao = inscricao.Where(i => i.classe == filtroClasse).ToList();
            }
            ViewBag.Classes = db.ClasseTorneio.Where(c => c.torneioId == torneioId).ToList();
            ViewBag.filtroClasse = filtroClasse;
            ViewBag.TorneioId = torneioId;
            ViewBag.flag = "inscritos";
            ViewBag.InscIndividuais = db.InscricaoTorneio.Where(i => i.torneioId == torneioId).Select(i => (int)i.userId).Distinct().Count();
            ViewBag.InscIndividuaisSocios = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.isSocio == true).Select(i => (int)i.userId).Distinct().Count();
            ViewBag.TotalPagantes = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.isAtivo == true).Select(i => (int)i.userId).Distinct().Count();
            ViewBag.ValorPago = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.isAtivo == true).Select(i => new { user = (int)i.userId, valor=i.valor }).Distinct().Sum(i => i.valor);
            ViewBag.PagoNoCartao = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.isAtivo == true && (i.statusPagamento == "3" || i.statusPagamento=="4")).
                Select(i => (int)i.userId).Distinct().Count();
            mensagem(Msg);
            return View(inscricao);
        }

        [Authorize(Roles = "admin,organizador")]
        public ActionResult EditClasse(int torneioId)
        {
            var classes = db.ClasseTorneio.Where(c => c.torneioId == torneioId).ToList();
            ViewBag.flag = "classes";
            ViewBag.torneioId = torneioId;
            return View(classes);
        }

        [HttpPost]
        public ActionResult EditClasse(int Id, string nome, bool isSegundaOpcao = false, bool isDupla = false, bool isPrimeiraOpcao = true)
        {
            try
            {
                var classe = db.ClasseTorneio.Find(Id);
                classe.nome = nome;
                classe.isSegundaOpcao = isSegundaOpcao;
                classe.isPrimeiraOpcao = isPrimeiraOpcao;
                classe.isDupla = isDupla;
                db.Entry(classe).State = EntityState.Modified;
                db.SaveChanges();
                return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        [Authorize(Roles = "admin,organizador")]
        public ActionResult CreateClasse(int torneioId)
        {
            ViewBag.torneioId = torneioId;
            return View();
        }

        [HttpPost]
        public ActionResult CreateClasse(ClasseTorneio classe)
        {
            if (ModelState.IsValid)
            {
                db.ClasseTorneio.Add(classe);
                db.SaveChanges();
                return RedirectToAction("EditClasse", new { torneioId = classe.torneioId });
            }
            else
            {
                ViewBag.torneioId = classe.torneioId;
            }
            return View(classe);
        }

        [HttpPost]
        public ActionResult EditInscritos(int Id, int classe, int cabecaChave, bool isAtivo)
        {
            try
            {
                var inscricao = db.InscricaoTorneio.Find(Id);
                inscricao.classe = classe;
                inscricao.isAtivo = isAtivo;
                inscricao.cabecaChave = cabecaChave;
                db.Entry(inscricao).State = EntityState.Modified;
                db.SaveChanges();
                return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        [Authorize(Roles = "admin,organizador")]
        public ActionResult Edit(int id = 0)
        {
            ViewBag.flag = "edit";
            Torneio torneio = db.Torneio.Find(id);
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            var barragemId = 0;
            if (perfil.Equals("admin"))
            {
                barragemId = torneio.barragemId;
            }
            else
            {
                barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
            }
            ViewBag.barraId = barragemId;
            ViewBag.barragemId = new SelectList(db.BarragemView, "Id", "nome", barragemId);
            ViewBag.TorneioId = id;
            ViewBag.JogadoresClasses = db.InscricaoTorneio.Where(i => i.torneioId == id && i.isAtivo == true).OrderBy(i => i.classe).ThenBy(i => i.participante.nome).ToList();

            if (torneio == null)
            {
                return HttpNotFound();
            }
            return View(torneio);
        }

        [Authorize(Roles = "admin,usuario,organizador")]
        public ActionResult Detalhes(int id = 0, String Msg = "", int userId=0)
        {
            Torneio torneio = db.Torneio.Find(id);
            ViewBag.isAceitaCartao = false;
            if (!String.IsNullOrEmpty(torneio.barragem.tokenPagSeguro))
            {
                ViewBag.isAceitaCartao = true;
            }
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            if (perfil.Equals("usuario") || userId==0){
                userId = WebSecurity.GetUserId(User.Identity.Name);
            }
            var inscricao = db.InscricaoTorneio.Where(i => i.torneio.Id == id && i.userId == userId).ToList();
            var classes = db.ClasseTorneio.Where(i => i.torneioId == id && i.isPrimeiraOpcao).OrderBy(c => c.nome).ToList();
            var classes2 = db.ClasseTorneio.Where(i => i.torneioId == id && i.isSegundaOpcao).OrderBy(c => c.nome).ToList();
            ViewBag.Classes = classes;
            ViewBag.Classes2 = classes2;
            ViewBag.ClasseInscricao2 = "";
            ViewBag.ClasseInscricao = 0;
            ViewBag.ClasseInsc2 = 0;

            ViewBag.isGratuito = false;
            if (VerificarGratuidade(torneio, userId))
            {
                ViewBag.isGratuito = true;
            }
            if (inscricao.Count > 0)
            {
                ViewBag.inscricao = inscricao[0];
                ViewBag.ClasseInscricao = inscricao[0].classe;
                if (inscricao.Count > 1)
                {
                    ViewBag.ClasseInscricao2 = inscricao[1].classeTorneio.nome;
                    ViewBag.ClasseInsc2 = inscricao[1].classe;
                }
            }
            //var jogador = db.UserProfiles.Find(userId);
            mensagem(Msg);
            return View(torneio);
        }

        private bool VerificarGratuidade(Torneio torneio, int userId)
        {
            if (!torneio.isGratuitoSocio)
            {
                return false;
            }
            else
            {
                var user = db.UserProfiles.Find(userId);
                if ((user.barragemId == torneio.barragemId) &&
                    (user.situacao.Equals("ativo") || user.situacao.Equals("licenciado") || user.situacao.Equals("suspenso") || user.situacao.Equals("suspensoWO")))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        [Authorize(Roles = "admin,usuario,organizador")]
        public ActionResult ConfirmacaoInscricao(int torneioId, string msg = "", string msgErro = "", int userId=0)
        {
            //torneioId = 1;
            ViewBag.Msg = msg;
            ViewBag.MsgErro = msgErro;
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            if (perfil.Equals("usuario") || userId == 0){
                userId = WebSecurity.GetUserId(User.Identity.Name);
            }
            var torneio = db.Torneio.Find(torneioId);
            ViewBag.isAceitaCartao = false;
            if (!String.IsNullOrEmpty(torneio.barragem.tokenPagSeguro))
            {
                ViewBag.isAceitaCartao = true;
            }
            var inscricao = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.userId == userId).ToList();
            if (inscricao.Count() > 1)
            {
                ViewBag.SegundaOpcaoClasse = inscricao[1].classeTorneio.nome;
            }
            return View(inscricao[0]);
        }
        [Authorize(Roles = "admin,usuario,organizador")]
        [HttpPost]
        public ActionResult Inscricao(int torneioId, int classeInscricao = 0, string operacao = "", bool isMaisDeUmaClasse = false, int classeInscricao2 = 0, string observacao = "", bool isSocio = false, bool isClasseDupla = false, int userId=0)
        {
            try
            {
                var gratuidade = false;
                string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
                if (perfil.Equals("usuario") || userId==0){
                    userId = WebSecurity.GetUserId(User.Identity.Name);
                }
                var torneio = db.Torneio.Find(torneioId);
                var isInscricao = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.userId == userId).Count();
                if (isInscricao > 0)
                {
                    var it = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.userId == userId).ToList();
                    if (operacao == "cancelar")
                    {
                        db.InscricaoTorneio.Remove(it[0]);
                        if (it.Count() > 1)
                        {
                            db.InscricaoTorneio.Remove(it[1]);
                        }
                    }
                    else
                    {
                        if ((classeInscricao == 0) || ((isMaisDeUmaClasse) && (classeInscricao2 == 0)))
                        {

                            return RedirectToAction("Detalhes", new { id = torneioId, Msg = "Selecione uma categoria." });
                        }
                        if (classeInscricao == classeInscricao2)
                        {

                            return RedirectToAction("Detalhes", new { id = torneioId, Msg = "Selecione uma categoria diferente na segunda opção de categoria." });
                        }
                        it[0].classe = classeInscricao;
                        if (operacao == "alterarClasse")
                        {
                            it[0].classe = classeInscricao;
                            db.Entry(it[0]).State = EntityState.Modified;
                            if (it.Count() > 1)
                            {
                                if (isMaisDeUmaClasse)
                                {
                                    it[1].classe = classeInscricao2;
                                    db.Entry(it[1]).State = EntityState.Modified;
                                }
                                else
                                {
                                    //remover 2 classe
                                    db.InscricaoTorneio.Remove(it[1]);
                                    //atualizar valor inscr
                                    it[0].valor = torneio.valor;
                                    db.Entry(it[0]).State = EntityState.Modified;
                                }
                            }
                            else if (isMaisDeUmaClasse)
                            {
                                if (it[0].isAtivo)
                                {
                                    return RedirectToAction("Detalhes", new { id = torneioId });
                                }
                                it[0].valor = torneio.valorMaisClasses;
                                db.Entry(it[0]).State = EntityState.Modified;
                                InscricaoTorneio inscricao2 = new InscricaoTorneio();
                                inscricao2.classe = classeInscricao2;
                                inscricao2.torneioId = torneioId;
                                inscricao2.userId = userId;
                                inscricao2.valor = torneio.valorMaisClasses;
                                inscricao2.observacao = observacao;
                                if (torneio.valor > 0)
                                {
                                    inscricao2.isAtivo = false;
                                }
                                else
                                {
                                    inscricao2.isAtivo = true;
                                }
                                db.InscricaoTorneio.Add(inscricao2);
                            }
                            db.SaveChanges();
                            if (isClasseDupla)
                            {
                                return RedirectToAction("EscolherDupla", new { torneioId = torneioId, userId=userId });
                            }
                            return RedirectToAction("Detalhes", new { id = torneioId, Msg = "ok" });
                        }
                    }
                }
                else
                {
                    if ((classeInscricao == 0) || ((isMaisDeUmaClasse) && (classeInscricao2 == 0)))
                    {

                        return RedirectToAction("Detalhes", new { id = torneioId, Msg = "Selecione uma categoria." });
                    }
                    if (classeInscricao == classeInscricao2)
                    {

                        return RedirectToAction("Detalhes", new { id = torneioId, Msg = "Selecione uma categoria diferente na segunda opção de categoria." });
                    }
                    InscricaoTorneio inscricao = new InscricaoTorneio();
                    inscricao.classe = classeInscricao;
                    inscricao.torneioId = torneioId;
                    inscricao.userId = userId;
                    inscricao.isSocio = isSocio;
                    if (isMaisDeUmaClasse)
                    {
                        if ((isSocio) && (torneio.valorMaisClassesSocio != null || torneio.valorMaisClassesSocio != 0))
                        {
                            inscricao.valor = torneio.valorMaisClassesSocio;
                        }
                        else
                        {
                            inscricao.valor = torneio.valorMaisClasses;
                        }

                    }
                    else
                    {
                        if ((isSocio) && (torneio.valorSocio != null || torneio.valorSocio != 0))
                        {
                            inscricao.valor = torneio.valorSocio;
                        }
                        else
                        {
                            inscricao.valor = torneio.valor;
                        }

                    }
                    gratuidade = VerificarGratuidade(torneio, userId);
                    if (gratuidade)
                    {
                        inscricao.valor = 0;
                    }
                    inscricao.observacao = observacao;
                    if ((torneio.valor == 0) || (gratuidade))
                    {
                        inscricao.isAtivo = true;
                    }
                    else
                    {
                        inscricao.isAtivo = false;
                    }
                    db.InscricaoTorneio.Add(inscricao);
                    if (isMaisDeUmaClasse)
                    {
                        InscricaoTorneio inscricao2 = new InscricaoTorneio();
                        inscricao2.classe = classeInscricao2;
                        inscricao2.torneioId = torneioId;
                        inscricao2.userId = userId;
                        inscricao2.valor = inscricao.valor;
                        inscricao2.observacao = observacao;
                        inscricao2.isSocio = isSocio;
                        if (torneio.valor > 0)
                        {
                            inscricao2.isAtivo = false;
                        }
                        else
                        {
                            inscricao2.isAtivo = true;
                        }
                        db.InscricaoTorneio.Add(inscricao2);
                    }
                }
                db.SaveChanges();
                var Msg = "Inscrição recebida.";
                if ((torneio.valor == 0) || (gratuidade))
                {
                    Msg = "Inscrição realizada com sucesso.";
                }
                if (operacao == "cancelar")
                {
                    return RedirectToAction("Detalhes", new { id = torneioId });
                }
                if (isClasseDupla)
                {
                    return RedirectToAction("EscolherDupla", new { torneioId = torneioId, userId=userId });
                }
                return RedirectToAction("ConfirmacaoInscricao", new { torneioId = torneioId, msg = Msg });
            }
            catch (Exception ex)
            {
                return RedirectToAction("Detalhes", new { id = torneioId, Msg = ex.Message });
            }
        }
        [Authorize(Roles = "admin, organizador")]
        public ActionResult Ativar(int inscricaoId)
        {
            var inscricao = db.InscricaoTorneio.Find(inscricaoId);
            try
            {
                if (inscricao.isAtivo)
                {
                    inscricao.isAtivo = false;
                }
                else
                {
                    inscricao.isAtivo = true;
                }
                db.SaveChanges();
                return RedirectToAction("InscricoesTorneio", new { torneioId = inscricao.torneioId, Msg = "OK" });
            }
            catch (Exception ex)
            {
                return RedirectToAction("InscricoesTorneio", new { torneioId = inscricao.torneioId, Msg = ex.Message });
            }
        }


        [Authorize(Roles = "admin, organizador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Torneio torneio)
        {
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
            if (ModelState.IsValid)
            {
                db.Entry(torneio).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            var barragemId = 0;
            if (perfil.Equals("admin"))
            {
                barragemId = torneio.barragemId;
            }
            else
            {
                barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
            }
            ViewBag.barraId = barragemId;
            ViewBag.barragemId = new SelectList(db.BarragemView, "Id", "nome", barragemId);
            return View(torneio);
        }

        [Authorize(Roles = "admin, organizador")]
        public ActionResult Create()
        {
            var userId = WebSecurity.GetUserId(User.Identity.Name);
            var barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
            ViewBag.barraId = barragemId;
            ViewBag.barragemId = new SelectList(db.BarragemView, "Id", "nome");

            return View();
        }

        //
        // POST: /Rodada/Create

        [Authorize(Roles = "admin, organizador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Torneio torneio)
        {
            var barragem = db.BarragemView.Find(torneio.barragemId);

            if ((ModelState.IsValid) && (barragem.isAtiva))
            {
                db.Torneio.Add(torneio);
                db.SaveChanges();
                ClasseTorneio classe = null;
                for (int i = 1; i <= torneio.qtddClasses; i++)
                {
                    classe = new ClasseTorneio();
                    classe.nivel = i;
                    classe.nome = i + "ª Classe";
                    classe.torneioId = torneio.Id;
                    classe.isPrimeiraOpcao = true;
                    classe.isSegundaOpcao = false;
                    classe.isDupla = false;
                    db.ClasseTorneio.Add(classe);
                    db.SaveChanges();
                }
                return RedirectToAction("Index");
            }
            else
            {
                var userId = WebSecurity.GetUserId(User.Identity.Name);
                var barragemId = (from up in db.UserProfiles where up.UserId == userId select up.barragemId).Single();
                ViewBag.barraId = barragemId;
                ViewBag.barragemId = new SelectList(db.BarragemView, "Id", "nome");
            }

            return View(torneio);
        }

        private int getQtddRodada(int qtddJogador)
        {
            if (qtddJogador == 2)
            {
                return 1;
            }
            else if (qtddJogador >= 3 && qtddJogador <= 4)
            {
                return 2;
            }
            else if (qtddJogador >= 5 && qtddJogador <= 8)
            {
                return 3;
            }
            else if (qtddJogador >= 9 && qtddJogador <= 16)
            {
                return 4;
            }
            else if (qtddJogador >= 17 && qtddJogador <= 32)
            {
                return 5;
            }
            else if (qtddJogador >= 33 && qtddJogador <= 64)
            {
                return 6;
            }
            else if (qtddJogador > 64)
            {
                return 7;
            }

            return 0;
        }

        private int getQtddByes(int qtddJogadores)
        {
            if (qtddJogadores >= 3 && qtddJogadores <= 4)
            {
                return 4 - qtddJogadores;
            }
            else if (qtddJogadores >= 5 && qtddJogadores <= 8)
            {
                return 8 - qtddJogadores;
            }
            else if (qtddJogadores >= 9 && qtddJogadores <= 16)
            {
                return 16 - qtddJogadores;
            }
            else if (qtddJogadores >= 17 && qtddJogadores <= 32)
            {
                return 32 - qtddJogadores;
            }
            else if (qtddJogadores >= 33 && qtddJogadores <= 64)
            {
                return 64 - qtddJogadores;
            }
            return 0;
        }

        private string getDescricaoRodada(int rodada)
        {
            if (rodada == 1)
            {
                return "Final";
            }
            else if (rodada == 2)
            {
                return "Semi-Final";
            }
            else if (rodada == 3)
            {
                return "Quartas-Final";
            }
            else if (rodada == 4)
            {
                return "Oitavas-Final";
            }
            else if (rodada == 5)
            {
                return "2ª Rodada";
            }
            else if (rodada == 6)
            {
                return "1ª Rodada";
            }
            else if (rodada == 101)
            {
                return "1ª Rodada Repescagem";
            }
            else if (rodada == 100)
            {
                return "2ª Rodada Repescagem";
            }
            return "";
        }

        [Authorize(Roles = "admin,organizador")]
        public ActionResult EditJogos(int torneioId, int fClasse = 0, string fData = "", string fHora = "", int fQuadra = 0, int fase = 0)
        {
            List<Jogo> listaJogos = null;
            var classes = db.ClasseTorneio.Where(i => i.torneioId == torneioId).OrderBy(c => c.nome).ToList();
            var classesGeradas = db.Jogo.Where(i => i.torneioId == torneioId).Select(i => (int)i.classeTorneio).Distinct().ToList();
            ViewBag.ClassesGeradas = classesGeradas;
            ViewBag.Classes = classes;
            ViewBag.fClasse = fClasse;
            ViewBag.fData = fData;
            ViewBag.fHora = fHora;
            ViewBag.fQuadra = fQuadra;
            ViewBag.fase = fase;
            if (fClasse == 0)
            {
                fClasse = classes[0].Id;
                ViewBag.filtroClasse = fClasse;
            }
            var jogo = db.Jogo.Where(i => i.torneioId == torneioId);
            listaJogos = filtrarJogos(jogo, fClasse, fData, fHora, fQuadra, fase);
            if (fClasse == 1)
            {
                ViewBag.Inscritos = db.InscricaoTorneio.Where(c => c.torneioId == torneioId && c.isAtivo).ToList();
            }
            else
            {
                ViewBag.Inscritos = db.InscricaoTorneio.Where(c => c.torneioId == torneioId && c.isAtivo && c.classe == fClasse).ToList();
            }

            ViewBag.TorneioId = torneioId;
            ViewBag.flag = "jogos";
            return View(listaJogos);
        }

        [Authorize(Roles = "admin,organizador")]
        public ActionResult ImprimirJogos(int torneioId, int fClasse = 0, string fData = "", string fHora = "", int fQuadra = 0, int fase = 0)
        {
            List<Jogo> listaJogos = null;
            var torneio = db.Torneio.Find(torneioId);
            ViewBag.nomeTorneio = torneio.nome;
            var jogo = db.Jogo.Where(i => i.torneioId == torneioId);
            listaJogos = filtrarJogos(jogo, fClasse, fData, fHora, fQuadra, fase,true);
            return View(listaJogos);
        }

        private List<Jogo> filtrarJogos(IQueryable<Jogo> jogos, int classe, string data, string hora, int quadra, int fase, Boolean isImprimir=false)
        {
            ViewBag.fClasse = classe;
            ViewBag.fData = data;
            ViewBag.fHora = hora;
            ViewBag.fQuadra = quadra;
            if (classe != 1)
            {
                jogos = jogos.Where(j => j.classeTorneio == classe);
            }
            if (!string.IsNullOrEmpty(data))
            {
                var dataJogo = Convert.ToDateTime(data);
                jogos = jogos.Where(j => j.dataJogo == dataJogo);
            }
            if (!string.IsNullOrEmpty(hora))
            {
                jogos = jogos.Where(j => j.horaJogo == hora);
            }
            if (quadra != 0)
            {
                jogos = jogos.Where(j => j.quadra == quadra);
            }
            if (fase != 0)
            {
                jogos = jogos.Where(j => j.faseTorneio == fase);
            }
            if (isImprimir) {
                return jogos.OrderBy(r => r.dataJogo).ThenBy(r => r.horaJogo).ToList();
            } else {
                return jogos.OrderByDescending(r => r.faseTorneio).ThenBy(r => r.ordemJogo).ToList();
            }
            
        }
        [Authorize(Roles = "admin,organizador")]
        [HttpPost]
        public ActionResult EditJogos(int Id, int jogador1, int jogador2, string dataJogo = "", string horaJogo = "", int quadra = 0)
        {
            try
            {
                var jogo = db.Jogo.Find(Id);
                if (jogo.classe.isDupla)
                {
                    var dupla = db.InscricaoTorneio.Where(i => i.userId == jogador1 && i.classe == jogo.classeTorneio && i.parceiroDuplaId != null).ToList();
                    if (dupla.Count() > 0) jogo.desafiante2_id = dupla[0].parceiroDuplaId;
                    var dupla2 = db.InscricaoTorneio.Where(i => i.userId == jogador2 && i.classe == jogo.classeTorneio && i.parceiroDuplaId != null).ToList();
                    if (dupla2.Count() > 0) jogo.desafiado2_id = dupla2[0].parceiroDuplaId;
                }
                jogo.desafiante_id = jogador1;
                jogo.desafiado_id = jogador2;
                if (dataJogo != "")
                {
                    jogo.dataJogo = Convert.ToDateTime(dataJogo);
                    if (jogo.situacao_Id != 4 && jogo.situacao_Id != 5){
                        jogo.situacao_Id = 2;
                    }
                }
                jogo.horaJogo = horaJogo;
                jogo.quadra = quadra;
                db.Entry(jogo).State = EntityState.Modified;
                db.SaveChanges();
                return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }


        [Authorize(Roles = "admin,organizador")]
        public ActionResult EditDuplas(int torneioId, int filtroClasse = 0, bool naoFazNada = false)
        {
            List<InscricaoTorneio> duplas = null;
            var classes = db.ClasseTorneio.Where(i => i.torneioId == torneioId && i.isDupla).OrderBy(c => c.nome).ToList();
            ViewBag.Classes = classes;
            ViewBag.TorneioId = torneioId;
            ViewBag.flag = "duplas";
            if (classes.Count == 0)
            {
                return View(duplas);
            }
            ViewBag.filtroClasse = filtroClasse;
            if (filtroClasse == 0)
            {
                filtroClasse = classes[0].Id;
                ViewBag.filtroClasse = filtroClasse;
            }
            duplas = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.classe == filtroClasse).
                OrderByDescending(i => i.parceiroDuplaId).ToList();

            ViewBag.Inscritos = db.InscricaoTorneio.Where(c => c.torneioId == torneioId && c.classe == filtroClasse).ToList();
            return View(duplas);
        }
        [Authorize(Roles = "admin,organizador")]
        [HttpPost]
        public ActionResult EditDuplas(int inscricaoJogador1, int? jogador2)
        {
            try
            {
                var inscricao = db.InscricaoTorneio.Find(inscricaoJogador1);
                inscricao.parceiroDuplaId = jogador2;
                db.Entry(inscricao).State = EntityState.Modified;
                db.SaveChanges();
                if (jogador2 == null)
                {
                    return RedirectToAction("EditDuplas", "Torneio", new { torneioId = inscricao.torneioId, filtroClasse = inscricao.classe });
                }
                return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        [Authorize(Roles = "admin,usuario,organizador")]
        [HttpPost]
        public ActionResult EscolherDupla(int inscricaoJogador, int torneioId, int classe=0, int userId=0)
        {
            try
            {
                var inscricao = db.InscricaoTorneio.Find(inscricaoJogador);
                string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
                if (perfil.Equals("usuario") || userId == 0){
                    userId = WebSecurity.GetUserId(User.Identity.Name);
                }
                var validar = db.InscricaoTorneio.Where(i => i.torneioId == torneioId && i.classe == classe && ((i.userId == userId && i.parceiroDuplaId != null) || (i.parceiroDuplaId == userId))).Count();
                if (validar == 0)
                {
                    inscricao.parceiroDuplaId = userId;
                    db.Entry(inscricao).State = EntityState.Modified;
                    db.SaveChanges();
                    return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { erro = "Você já possui uma dupla. Para trocar de dupla, favor entrar em contato com o organizador do torneio.", retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }


        [Authorize(Roles = "admin,usuario,organizador")]
        public ActionResult LancarResultado(int id = 0, int barragem = 0, string msg = "")
        {
            Jogo jogo = null;
            var usuario = db.UserProfiles.Find(WebSecurity.GetUserId(User.Identity.Name));
            InscricaoTorneio inscricao = null;
            if (id == 0)
            {
                try
                {
                    HttpCookie cookie = Request.Cookies["_barragemId"];
                    var barragemId = 0;
                    if (cookie != null)
                    {
                        barragemId = Convert.ToInt32(cookie.Value.ToString());
                    }
                    else if (barragem != 0)
                    {
                        BarragemView b = db.BarragemView.Find(barragem);
                        barragemId = barragem;
                        Funcoes.CriarCookieBarragem(Response, Server, b.Id, b.nome);
                    }
                    var agora = DateTime.Now;

                    if (barragemId > 0)
                    {
                        inscricao = db.InscricaoTorneio.Where(i => i.participante.UserId == usuario.UserId && i.isAtivo && i.torneio.dataFimInscricoes < agora && i.torneio.barragemId == barragemId).OrderByDescending(i => i.Id).Take(1).Single();
                    }
                    else
                    {
                        inscricao = db.InscricaoTorneio.Where(i => i.participante.UserId == usuario.UserId && i.isAtivo && i.torneio.dataFimInscricoes < agora).OrderByDescending(i => i.Id).Take(1).Single();
                    }
                    ViewBag.NomeTorneio = inscricao.torneio.nome;
                    jogo = db.Jogo.Where(u => (u.desafiado_id == usuario.UserId || u.desafiante_id == usuario.UserId) && u.torneioId == inscricao.torneioId)
                        .OrderBy(u => u.situacao_Id).ThenBy(u => u.faseTorneio).Take(1).Single();
                }
                catch (System.InvalidOperationException e)
                {
                    ViewBag.MsgAlert = "Não foi possível encontrar jogos em aberto:" + e.Message;
                }
            }
            else
            {
                jogo = db.Jogo.Find(id);
            }
            if (jogo != null)
            {
                //nao permitir edição caso a rodada já esteja fechada e o placar já tenha sido informado
                string perfil = Roles.GetRolesForUser(User.Identity.Name)[0];
                if (!perfil.Equals("admin") && !perfil.Equals("organizador") && (jogo.gamesJogados != 0))
                {
                    ViewBag.Editar = false;
                }
                else
                {
                    ViewBag.Editar = true;
                }
                ViewBag.situacao_Id = new SelectList(db.SituacaoJogo, "Id", "descricao", jogo.situacao_Id);
                // jogos pendentes
                var jogosPendentes = db.Jogo.Where(u => (u.desafiado_id == usuario.UserId || u.desafiante_id == usuario.UserId) && u.torneioId == jogo.torneioId
                    && (u.situacao_Id != 4 && u.situacao_Id != 5)).OrderByDescending(u => u.Id).Take(3).ToList();
                if (jogosPendentes.Count() > 1)
                {
                    ViewBag.JogosPendentes = jogosPendentes;
                }
                // últimos jogos já finalizados
                var ultimosJogosFinalizados = db.Jogo.Where(u => (u.desafiado_id == usuario.UserId || u.desafiante_id == usuario.UserId) && u.torneioId == jogo.torneioId
                    && (u.situacao_Id == 4 || u.situacao_Id == 5)).OrderByDescending(u => u.Id).Take(5).ToList();
                ViewBag.JogosFinalizados = ultimosJogosFinalizados;
            }

            mensagem(msg);
            return View(jogo);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,usuario,organizador")]
        public ActionResult LancarResultado(Jogo jogo)
        {
            var Mensagem = "";
            int setDesafiante = 0;
            int setDesafiado = 0;
            if (ModelState.IsValid)
            {
                jogo.dataCadastroResultado = DateTime.Now;
                jogo.usuarioInformResultado = User.Identity.Name;
                if (jogo.desafiado_id == WebSecurity.GetUserId(User.Identity.Name))
                {
                    setDesafiado = 6;
                    setDesafiante = 1;
                }
                else
                {
                    setDesafiado = 1;
                    setDesafiante = 6;
                }
                jogo.idVencedor = 0;
                jogo.idPerderdor = 0;
                if (jogo.situacao_Id == 5)
                { //WO
                    jogo.qtddGames1setDesafiado = setDesafiado;
                    jogo.qtddGames1setDesafiante = setDesafiante;
                    jogo.qtddGames2setDesafiado = setDesafiado;
                    jogo.qtddGames2setDesafiante = setDesafiante;
                }
                if (jogo.situacao_Id == 1 || jogo.situacao_Id == 2) // pendente ou marcado
                {
                    jogo.qtddGames1setDesafiado = 0;
                    jogo.qtddGames1setDesafiante = 0;
                    jogo.qtddGames2setDesafiado = 0;
                    jogo.qtddGames2setDesafiante = 0;
                    jogo.qtddGames3setDesafiado = 0;
                    jogo.qtddGames3setDesafiante = 0;
                }
                db.Entry(jogo).State = EntityState.Modified;
                db.SaveChanges();
                Mensagem = "ok";
            }
            else
            {
                Mensagem = "Não foi possível alterar os dados.";
            }

            jogo = db.Jogo.Include(j => j.desafiado).Include(j => j.desafiante).Where(j => j.Id == jogo.Id).Single();

            ViewBag.situacao_Id = new SelectList(db.SituacaoJogo, "Id", "descricao", jogo.situacao_Id);

            MontarProximoJogoTorneio(jogo);

            return RedirectToAction("LancarResultado", "Torneio", new { torneioId = jogo.Id, msg = Mensagem });

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,usuario,organizador")]
        public ActionResult LancarWO(int Id, int vencedorWO, int situacao_id=5)
        {
            Jogo jogoAtual = db.Jogo.Find(Id);
            //alterar quantidade de games para desafiado e desafiante
            int gamesDesafiante = 0;
            int gamesDesafiado = 0;
            if (jogoAtual.desafiado_id == vencedorWO)
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
            jogoAtual.situacao_Id = situacao_id;
            jogoAtual.usuarioInformResultado = User.Identity.Name;
            jogoAtual.dataCadastroResultado = DateTime.Now;
            db.Entry(jogoAtual).State = EntityState.Modified;
            db.SaveChanges();

            MontarProximoJogoTorneio(jogoAtual);

            var barragemId = 0;
            return RedirectToAction("LancarResultado", "Torneio", new { Id, barragemId, msg="ok"});

        }

        private void cadastrarColocacaoPerdedorTorneio(Jogo jogo)
        {
            if (jogo.idDoPerdedor == 10)
            {
                return; // sai se for o bye;
            }
            // cadastrar a colocação do perdedor no torneio 
            var qtddFases = db.Jogo.Where(r => r.torneioId == jogo.torneioId && r.classeTorneio == jogo.classeTorneio && r.faseTorneio < 100)
                .Max(r => r.faseTorneio);
            int colocacao = 0;
            if ((jogo.faseTorneio == 100) || (jogo.faseTorneio == 101))
            { // fase repescagem
                colocacao = 100;
            }
            else
            {
                colocacao = (int)jogo.faseTorneio;
            }
            var inscricao = db.InscricaoTorneio.Where(i => i.userId == jogo.idDoPerdedor && i.torneioId == jogo.torneioId).ToList();
            if (inscricao.Count() > 0)
            {
                inscricao[0].colocacao = colocacao;
                db.SaveChanges();
            }
        }

        [Authorize(Roles = "admin, organizador")]
        [HttpPost]
        public ActionResult notificarJogadores(int torneioId, string texto)
        {
            Torneio torneio = db.Torneio.Find(torneioId);
            var retorno = "";
            try
            {
                Mail mail = new Mail();
                mail.de = System.Configuration.ConfigurationManager.AppSettings.Get("UsuarioMail");
                mail.para = "barragemdocerrado@gmail.com";
                mail.assunto = torneio.nome;
                mail.conteudo = texto;
                mail.formato = Tipos.FormatoEmail.Html;
                List<InscricaoTorneio> users = db.InscricaoTorneio.Where(u => u.isAtivo == true && u.torneioId == torneioId).ToList();
                List<string> bcc = new List<string>();
                foreach (InscricaoTorneio user in users)
                {
                    bcc.Add(user.participante.email);
                }
                mail.bcc = bcc;
                mail.EnviarMail();
                retorno = "Notificação enviada com sucesso.";
            }
            catch (Exception ex)
            {
                retorno = "Houve uma falha no envio. Favor entrar em contato com o administrador do sistema.";
            }
            return RedirectToAction("EditNotificacao", new { torneioId = torneioId, msg = retorno });
        }

        [Authorize(Roles = "admin, organizador")]
        public ActionResult EditNotificacao(int torneioId, string msg = "")
        {
            ViewBag.retorno = msg;
            ViewBag.flag = "notificacao";
            ViewBag.TorneioId = torneioId;
            return View();
        }

        private void MontarProximoJogoTorneio(Jogo jogo)
        {
            var ordemJogo = 0;
            if (jogo.torneioId != null)
            {
                if (jogo.ordemJogo % 2 != 0)
                {
                    ordemJogo = (int)(jogo.ordemJogo / 2) + 1;
                }
                else
                {
                    ordemJogo = (int)(jogo.ordemJogo / 2);
                }
                var torneioId = jogo.torneioId;
                var torneio = db.Torneio.Find(torneioId);
                var classeId = jogo.classeTorneio;
                var isPrimeiroJogo = false;
                if (jogo.isPrimeiroJogoTorneio != null)
                {
                    isPrimeiroJogo = (bool)jogo.isPrimeiroJogoTorneio;
                }
                if ((torneio.temRepescagem) && (isPrimeiroJogo))
                {
                    CadastrarPerdedorNaRepescagem(jogo);
                }
                if (db.Jogo.Where(r => r.torneioId == jogo.torneioId && r.classeTorneio == jogo.classeTorneio &&
                   r.faseTorneio == jogo.faseTorneio - 1 && r.ordemJogo == ordemJogo).Count() > 0)
                {
                    var proximoJogo = db.Jogo.Where(r => r.torneioId == jogo.torneioId && r.classeTorneio == jogo.classeTorneio &&
                        r.faseTorneio == jogo.faseTorneio - 1 && r.ordemJogo == ordemJogo).Single();
                    if (jogo.desafiante_id == 10)
                    {
                        proximoJogo.isPrimeiroJogoTorneio = true;
                    }
                    else
                    {
                        proximoJogo.isPrimeiroJogoTorneio = false;
                    }
                    if (jogo.ordemJogo % 2 != 0)
                    {
                        proximoJogo.desafiado_id = jogo.idDoVencedor;
                        proximoJogo.desafiado2_id = getParceiroDuplaProximoJogo(jogo, jogo.idDoVencedor);
                    }
                    else
                    {
                        proximoJogo.desafiante_id = jogo.idDoVencedor;
                        proximoJogo.desafiante2_id = getParceiroDuplaProximoJogo(jogo, jogo.idDoVencedor);
                    }
                    proximoJogo.cabecaChave = jogo.cabecaChave;
                    cadastrarColocacaoPerdedorTorneio(jogo);
                    db.SaveChanges();
                }
                else
                {
                    // indicar o vencedor do torneio
                    var inscricao = db.InscricaoTorneio.Where(i => i.userId == jogo.idDoVencedor && i.torneioId == jogo.torneioId).ToList();
                    if (inscricao.Count() > 0)
                    {
                        inscricao[0].colocacao = 0; // vencedor
                        db.SaveChanges();
                    }
                }
            }
        }

        public ActionResult TabelaImprimir(int torneioId, int fClasse = 0)
        {
            var torneio = db.Torneio.Find(torneioId);
            ViewBag.nomeTorneio = torneio.nome;
            ViewBag.nomeRanking = torneio.barragem.nome;
            ViewBag.idBarragem = torneio.barragemId;
            var jogos = db.Jogo.Where(r => r.torneioId == torneioId && r.classeTorneio == fClasse && r.faseTorneio != 100 && r.faseTorneio != 101).OrderByDescending(r => r.faseTorneio).ThenBy(r => r.ordemJogo).ToList();
            ViewBag.nomeClasse = jogos[0].classe.nome;
            return View(jogos);
        }
    }
}