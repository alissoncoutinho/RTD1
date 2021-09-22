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
using System.Web.Security;
using System.Security.Claims;

namespace Barragem.Controllers
{


    public class InscricaoAPIController : ApiController
    {
        private BarragemDbContext db = new BarragemDbContext();
        private TorneioNegocio tn = new TorneioNegocio();

        // GET: api/InscricaoAPI
        [HttpGet]
        [Route("api/InscricaoAPI/PrepararInscricao/{torneioId}")]
        public TorneioClassesApp GetPrepararInscricao(int torneioId)
        {
            var torneioClassesApp = new TorneioClassesApp();
            torneioClassesApp.torneio = db.Torneio.Find(torneioId);
            torneioClassesApp.classesTorneio = db.ClasseTorneio.Where(c => c.torneioId == torneioId).ToList();
            return torneioClassesApp;
        }


        [HttpGet]
        [Route("api/InscricaoAPI/{Id}")]
        public InscricaoTorneio GetInscricao(int Id)
        {
            return db.InscricaoTorneio.Find(Id);
        }

        [HttpGet]
        [Route("api/InscricaoAPI")]
        public IList<Inscrito> GetInscricao(int torneioId=0, int classeId=0, bool semParceiroDupla=false, int userId=0, bool proximaClasse=false){
            var inscricoesTorneio = db.InscricaoTorneio.Where(i => i.torneioId == torneioId);
            var listInscricaoTorneio = new List<InscricaoTorneio>();
            if (classeId != 0 && !proximaClasse){
                inscricoesTorneio = inscricoesTorneio.Where(i => i.classe == classeId);
            }
            if (semParceiroDupla){
                if (userId != 0)
                {
                    var inscricoesUsuario = new List<InscricaoTorneio>();
                    if(classeId!=0 && proximaClasse)
                    {
                        inscricoesUsuario = inscricoesTorneio.Where(i => i.userId == userId && i.classeTorneio.isDupla && i.parceiroDuplaId == null && i.classe>classeId).OrderBy(i => i.classe).ToList();
                    }
                    else
                    {
                        inscricoesUsuario = inscricoesTorneio.Where(i => i.userId == userId && i.classeTorneio.isDupla && i.parceiroDuplaId == null).OrderBy(i => i.classe).ToList();
                    }
                    
                    var inscricoesUsuAtualizada = new List<InscricaoTorneio>();
                    foreach (var item in inscricoesUsuario) {
                        var jaEstouEmAlgumaDupla = inscricoesTorneio.Where(i => i.classe == item.classe && i.parceiroDuplaId == item.userId).Any();
                        if (!jaEstouEmAlgumaDupla){
                            inscricoesUsuAtualizada.Add(item);
                        }
                    }
                    if (inscricoesUsuAtualizada.Count() > 0)
                    {
                        classeId = inscricoesUsuAtualizada[0].classe;
                    } else {
                        classeId = 0;
                    }
                }
                listInscricaoTorneio = tn.getInscricoesSemDuplas(classeId);
            } else {
                listInscricaoTorneio = inscricoesTorneio.ToList();
            }
            return montarListaInscritos(listInscricaoTorneio);
        }

        private IList<Inscrito> montarListaInscritos(IList<InscricaoTorneio> inscricoesTorneio)
        {
            List<Inscrito> inscritos = new List<Inscrito>();
            foreach (var i in inscricoesTorneio)
            {
                var inscrito = new Inscrito();
                inscrito.userId = i.userId;
                inscrito.nome = i.participante.nome;
                inscrito.classe = i.classeTorneio.nome;
                inscrito.classeId = i.classe;
                inscrito.foto = i.participante.fotoURL;
                if (i.parceiroDupla != null) inscrito.nomeDupla = i.parceiroDupla.nome;
                inscritos.Add(inscrito);
            }
            return inscritos;
        }

        [Route("api/InscricaoAPI/Inscritos/{torneioId}")]
        public IList<Inscrito> GetListarInscritos(int torneioId, int userId=0){
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
            foreach (var ins in inscricoesDupla)
            {
                var formouDupla = inscricoesDupla.Where(i => i.parceiroDuplaId == ins.userId && i.classe == ins.classe).Count();
                if (formouDupla > 0)
                {
                    inscricoesRemove.Add(ins);
                }
            }
            foreach (var ins in inscricoesRemove)
            {
                inscricoesDupla.Remove(ins);
            }
            if (inscricoesDupla.Count() > 0)
            {
                inscricoes.AddRange(inscricoesDupla);
                inscricoes = inscricoes.OrderBy(i => i.classe).ThenByDescending(i => i.parceiroDuplaId).ThenBy(i => i.participante.nome).ToList();
            }
            // verificar se é para exibir o botão de montar dupla ou não. Se existir classes de dupla no torneio e o usuário estiver inscrito em uma classe de dupla exibe o botão.
            var inscritos = montarListaInscritos(inscricoes);

            if (inscritos.Count > 0)
            {
                var usuarioLogado = 0;
                if (userId != 0) {
                    usuarioLogado = userId;
                } else {
                    usuarioLogado = getUsuarioLogado();
                }
                var inscricoesTorneio = db.InscricaoTorneio.Where(r => r.torneioId == torneioId && r.classeTorneio.isDupla == true && r.userId == usuarioLogado && r.parceiroDuplaId==null).ToList();
                var exibeBotaoFormarDupla = false;
                if (inscricoesTorneio.Count()>0)
                {
                    foreach (var item in inscricoesTorneio)
                    {
                        var souParceidoDeAlguem = db.InscricaoTorneio.Where(r => r.classe == item.classe && r.parceiroDuplaId == usuarioLogado).Any();
                        if (!souParceidoDeAlguem)
                        {
                            exibeBotaoFormarDupla = true;
                        }
                    }
                } else {
                    exibeBotaoFormarDupla = false;
                }
                inscritos[0].exibeBotaoFormarDupla = exibeBotaoFormarDupla;
            }
            return inscritos;
            
        }

        [HttpGet]
        [Route("api/InscricaoAPI/DuplasPendente/{usuarioLogado}")]
        public IList<TorneioApp> duplasPendentes(int usuarioLogado)
        {
            var inscritos = new List<InscricaoTorneio>();
            var inscricoes = db.InscricaoTorneio.Where(i => i.classeTorneio.isDupla && i.parceiroDuplaId == null && i.userId == usuarioLogado).ToList();
            foreach (var item in inscricoes){
                var jaSouParceidoDeDupla = db.InscricaoTorneio.Include(t=>t.torneio).Where(i => i.parceiroDuplaId == usuarioLogado && i.classe == item.classe).Any();
                if (!jaSouParceidoDeDupla)
                {
                    inscritos.Add(item);
                }
            }
            var torneios = new List<TorneioApp>();
            foreach (var item in inscritos)
            {
                var torneio = new TorneioApp();
                torneio.Id = item.torneioId;
                torneio.nome = item.torneio.nome;
                torneio.logoId = item.torneio.barragemId;
                torneio.dataFim =
                torneio.dataInicio = item.torneio.dataInicio;
                torneio.valor = item.torneio.valor;
                torneio.valorSocio = item.torneio.valorSocio;
                torneio.dataFim = item.torneio.dataFim;
                torneio.dataFimInscricoes = item.torneio.dataFimInscricoes;
                torneio.cidade = item.torneio.cidade;
                torneio.premiacao = item.torneio.premiacao;
                torneio.contato = "";
                torneio.pontuacaoLiga = item.torneio.TipoTorneio;
                torneio.contato = db.Torneio.Find(item.torneioId).contato;
                var patrocinadores = new List<Patrocinador>();
                var patrocinador = db.Patrocinador.Where(p => p.torneioId == item.torneioId).ToList();
                foreach (var i in patrocinador)
                {
                    patrocinadores.Add(i);
                }
                torneio.patrocinadores = patrocinadores;
                try
                {
                    var torneioLiga = db.TorneioLiga.Include(l => l.Liga).Where(t => t.TorneioId == item.torneioId).FirstOrDefault();
                    torneio.nomeLiga = torneioLiga.Liga.Nome;
                }
                catch (Exception e) { }
               torneios.Add(torneio);
            }
            return torneios;
        }
        

            private int getUsuarioLogado()
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var userId = 0; //9058;
            if (claimsIdentity.FindFirst("sub") != null)
            {
                userId = Convert.ToInt32(claimsIdentity.FindFirst("sub").Value);
            }
            return userId;
        }

        [ResponseType(typeof(void))]
        [HttpPatch]
        [Route("api/InscricaoAPI/{Id}")]
        public IHttpActionResult PatchInscricao(int Id, int parceiroDuplaId, int userId, int classeId=0)
        {
            var inscricaoTorneio = new InscricaoTorneio();
            if (Id != 0){
                inscricaoTorneio = db.InscricaoTorneio.Find(Id);
            } else {
                
                inscricaoTorneio = db.InscricaoTorneio.Where(i=> i.classe==classeId && i.userId== userId).FirstOrDefault();
            }
            var jaEstouEmOutraDupla = db.InscricaoTorneio.Where(i => i.classe == inscricaoTorneio.classe && (i.parceiroDuplaId == inscricaoTorneio.userId)).Any();
            if ((jaEstouEmOutraDupla)||(inscricaoTorneio.parceiroDuplaId != null && inscricaoTorneio.parceiroDuplaId != 0)){
                return BadRequest("Você já possui uma dupla. Para trocar de dupla, favor entrar em contato com o organizador do torneio.");
            }

            var parceiroJaEstaEmOutraDupla = db.InscricaoTorneio.Where(i => i.classe == inscricaoTorneio.classe && ((i.userId == parceiroDuplaId && i.parceiroDuplaId != null) || (i.parceiroDuplaId == parceiroDuplaId))).Any();
            if (parceiroJaEstaEmOutraDupla)
            {
                return BadRequest("Seu parceiro já possui uma dupla. Para trocar de dupla, favor entrar em contato com o organizador do torneio.");
            }
            
            inscricaoTorneio.parceiroDuplaId = parceiroDuplaId;

            db.Entry(inscricaoTorneio).State = EntityState.Modified;
            db.SaveChanges();
            
            return StatusCode(HttpStatusCode.Created);
        }

        [ResponseType(typeof(void))]
        [HttpPost]
        [AllowAnonymous]
        [Route("api/InscricaoAPI")]
        public IHttpActionResult PostInscricao(int userId, int torneioId, int classe1, int classe2, int classe3, int classe4, string observacao = "", bool isSocio = false, bool isFederado=false)
        {
            var mensagemRetorno = new TorneioController().InscricaoNegocio(torneioId, classe1, "", classe2, classe3, classe4, observacao, isSocio, false, userId, isFederado);
            if (mensagemRetorno.tipo == "erro")
            {
                return InternalServerError(new Exception(mensagemRetorno.mensagem));
            }
            else
            {
                return Ok();
            }
        }
        
    }
}