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

        [HttpGet]
        [Route("api/InscricaoAPI/ValidarDisponibilidadeInscricoes")]
        public IHttpActionResult ValidarDisponibilidadeInscricoes(int torneioId, int categoriaId)
        {
            if (torneioId <= 0)
            {
                return Ok(new RetornoValidacaoDisponibInscricaoModel("Id do torneio é obrigatório"));
            }
            else if (categoriaId <= 0)
            {
                return Ok(new RetornoValidacaoDisponibInscricaoModel("Id da categoria é obrigatória"));
            }
            return Ok(tn.ValidarDisponibilidadeInscricoes(torneioId, categoriaId));
        }

        // GET: api/InscricaoAPI
        [HttpGet]
        [Route("api/InscricaoAPI/PrepararInscricao/{torneioId}")]
        public IHttpActionResult GetPrepararInscricao(int torneioId, int userId = 0)
        {
            if (userId > 0)
            {
                var temInscricao = db.InscricaoTorneio.Where(i => i.userId == userId && i.torneioId == torneioId).Count();
                if (temInscricao > 0)
                {
                    return BadRequest("001-Você já possui uma inscrição neste torneio.");
                }
            }
            var torneioClassesApp = new TorneioClassesApp();
            var torneio = db.Torneio.Find(torneioId);
            torneioClassesApp.torneio = montaTorneio(torneio);
            //torneioClassesApp.torneio.Id = torneio.Id;
            var classesTorneio = db.ClasseTorneio.Where(c => c.torneioId == torneioId).ToList();
            torneioClassesApp.classesTorneio = new List<ClasseTorneio>();
            foreach (var item in classesTorneio)
            {
                var classeTorneio = new ClasseTorneio();
                classeTorneio.Id = item.Id;
                classeTorneio.isDupla = item.isDupla;
                classeTorneio.nome = item.nome;
                torneioClassesApp.classesTorneio.Add(classeTorneio);
            }
            return Ok(torneioClassesApp);
        }

        private Torneio montaTorneio(Torneio torneio)
        {
            var torneioDadosReduzidos = new Torneio();
            torneioDadosReduzidos.Id = torneio.Id;
            torneioDadosReduzidos.cidade = torneio.cidade;
            torneioDadosReduzidos.dadosBancarios = torneio.dadosBancarios;
            torneioDadosReduzidos.dataFim = torneio.dataFim;
            torneioDadosReduzidos.StatusInscricao = torneio.StatusInscricao;
            torneioDadosReduzidos.dataFimInscricoes = torneio.DataFinalInscricoes;
            torneioDadosReduzidos.dataInicio = torneio.dataInicio;
            torneioDadosReduzidos.descontoPara = torneio.descontoPara;
            torneioDadosReduzidos.divulgacao = torneio.divulgacao;
            torneioDadosReduzidos.divulgaCidade = torneio.divulgaCidade;
            torneioDadosReduzidos.isDesconto = torneio.isDesconto;
            torneioDadosReduzidos.isGratuitoSocio = torneio.isGratuitoSocio;
            torneioDadosReduzidos.local = torneio.local;
            torneioDadosReduzidos.nome = torneio.nome;
            torneioDadosReduzidos.observacao = torneio.observacao;
            torneioDadosReduzidos.premiacao = torneio.premiacao;
            torneioDadosReduzidos.qtddCategoriasPorJogador = torneio.qtddCategoriasPorJogador;
            torneioDadosReduzidos.valor = torneio.valor;
            if (torneio.qtddCategoriasPorJogador > 1) torneioDadosReduzidos.valor2 = torneio.valor2;
            if (torneio.qtddCategoriasPorJogador > 2) torneioDadosReduzidos.valor3 = torneio.valor3;
            if (torneio.qtddCategoriasPorJogador > 3) torneioDadosReduzidos.valor4 = torneio.valor4;
            torneioDadosReduzidos.valorDescontoFederado = torneio.valorDescontoFederado;
            torneioDadosReduzidos.valorSocio = torneio.valorSocio;

            return torneioDadosReduzidos;
        }

        [HttpGet]
        [Route("api/InscricaoAPI/{Id}")]
        public InscricaoTorneio GetInscricao(int Id)
        {
            return db.InscricaoTorneio.Find(Id);
        }

        [HttpGet]
        [Route("api/InscricaoAPI")]
        public IList<Inscrito> GetInscricao(int torneioId = 0, int classeId = 0, bool semParceiroDupla = false, int userId = 0, bool proximaClasse = false)
        {
            var inscricoesTorneio = db.InscricaoTorneio.Where(i => i.torneioId == torneioId);
            var listInscricaoTorneio = new List<InscricaoTorneio>();
            if (classeId != 0 && !proximaClasse)
            {
                inscricoesTorneio = inscricoesTorneio.Where(i => i.classe == classeId);
            }
            if (semParceiroDupla)
            {
                if (userId != 0)
                {
                    var inscricoesUsuario = new List<InscricaoTorneio>();
                    if (classeId != 0 && proximaClasse)
                    {
                        inscricoesUsuario = inscricoesTorneio.Where(i => i.userId == userId && i.classeTorneio.isDupla && i.parceiroDuplaId == null && i.classe > classeId).OrderBy(i => i.classe).ToList();
                    }
                    else
                    {
                        inscricoesUsuario = inscricoesTorneio.Where(i => i.userId == userId && i.classeTorneio.isDupla && i.parceiroDuplaId == null).OrderBy(i => i.classe).ToList();
                    }

                    var inscricoesUsuAtualizada = new List<InscricaoTorneio>();
                    foreach (var item in inscricoesUsuario)
                    {
                        var jaEstouEmAlgumaDupla = inscricoesTorneio.Where(i => i.classe == item.classe && i.parceiroDuplaId == item.userId).Any();
                        if (!jaEstouEmAlgumaDupla)
                        {
                            inscricoesUsuAtualizada.Add(item);
                        }
                    }
                    if (inscricoesUsuAtualizada.Count() > 0)
                    {
                        classeId = inscricoesUsuAtualizada[0].classe;
                    }
                    else
                    {
                        classeId = 0;
                    }
                }
                listInscricaoTorneio = tn.getInscricoesSemDuplas(classeId);
            }
            else
            {
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
        public IList<Inscrito> GetListarInscritos(int torneioId, int userId = 0)
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
                if (userId != 0)
                {
                    usuarioLogado = userId;
                }
                else
                {
                    usuarioLogado = getUsuarioLogado();
                }
                var inscricoesTorneio = db.InscricaoTorneio.Where(r => r.torneioId == torneioId && r.classeTorneio.isDupla == true && r.userId == usuarioLogado && r.parceiroDuplaId == null).ToList();
                var exibeBotaoFormarDupla = false;
                if (inscricoesTorneio.Count() > 0)
                {
                    foreach (var item in inscricoesTorneio)
                    {
                        var souParceidoDeAlguem = db.InscricaoTorneio.Where(r => r.classe == item.classe && r.parceiroDuplaId == usuarioLogado).Any();
                        if (!souParceidoDeAlguem)
                        {
                            exibeBotaoFormarDupla = true;
                        }
                    }
                }
                else
                {
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
            DateTime hoje = DateTime.Now.Date;
            var inscricoes = db.InscricaoTorneio.Where(i => i.classeTorneio.isDupla && i.parceiroDuplaId == null && i.userId == usuarioLogado && i.torneio.dataFim > hoje).ToList();
            foreach (var item in inscricoes)
            {
                var jaSouParceidoDeDupla = db.InscricaoTorneio.Include(t => t.torneio).Where(i => i.parceiroDuplaId == usuarioLogado && i.classe == item.classe).Any();
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
                torneio.StatusInscricao = item.torneio.StatusInscricao;
                torneio.dataFimInscricoes = item.torneio.DataFinalInscricoes;
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
        public IHttpActionResult PatchInscricao(int Id, int parceiroDuplaId, int userId, int classeId = 0)
        {
            var inscricaoTorneio = new InscricaoTorneio();
            if (Id != 0)
            {
                inscricaoTorneio = db.InscricaoTorneio.Find(Id);
            }
            else
            {

                inscricaoTorneio = db.InscricaoTorneio.Where(i => i.classe == classeId && i.userId == userId).FirstOrDefault();
            }
            var jaEstouEmOutraDupla = db.InscricaoTorneio.Where(i => i.classe == inscricaoTorneio.classe && (i.parceiroDuplaId == inscricaoTorneio.userId)).Any();
            if ((jaEstouEmOutraDupla) || (inscricaoTorneio.parceiroDuplaId != null && inscricaoTorneio.parceiroDuplaId != 0))
            {
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

        private Order montarPedidoPIX(InscricaoTorneio inscricaoTorneio)
        {
            var valor = inscricaoTorneio.valor;
            if ((inscricaoTorneio.valorPendente != null) && (inscricaoTorneio.valorPendente != 0))
            {
                valor = inscricaoTorneio.valorPendente;
            }
            var order = new Order();
            order.reference_id = "T-" + inscricaoTorneio.Id;
            order.customer = new Customer();
            order.customer.name = "Ranking de Tênis Ponto Com"; //inscricaoTorneio.participante.nome;
            order.customer.email = "rankingbeachtennis@gmail.com"; // inscricaoTorneio.participante.email;
            order.customer.tax_id = "13170650009";
            var item = new ItemPedido();
            item.reference_id = inscricaoTorneio.torneioId + "";
            item.name = inscricaoTorneio.torneio.nome;
            item.quantity = 1;
            item.unit_amount = Convert.ToInt32(valor) * 100;
            order.items = new List<ItemPedido>();
            order.items.Add(item);
            var qr_code = new QrCode();
            var amount = new Amount();
            amount.value = Convert.ToInt32(valor) * 100;
            qr_code.amount = amount;
            order.qr_codes = new List<QrCode>();
            order.qr_codes.Add(qr_code);
            order.notification_urls = new string[1];
            order.notification_urls[0] = "https://www.rankingdetenis.com/api/InscricaoAPI/ReceberNotificacao";

            return order;
        }

        [ResponseType(typeof(void))]
        [HttpGet]
        [Route("api/InscricaoAPI/{Id}/CobrancaPIX")]
        public IHttpActionResult CobrancaPIXInscricao(int Id)
        {
            try
            {
                //return BadRequest("token inválido");
                //return Ok("00020126830014br.gov.bcb.pix2561api.pagseguro.com/pix/v2/210387E0-A6BF-45D1-80B5-CFEB9BBCEE2F5204899953039865802BR5921Pagseguro Internet SA6009SAO PAULO62070503***63047E6D");

                var inscricaoTorneio = db.InscricaoTorneio.Find(Id);
                var order = montarPedidoPIX(inscricaoTorneio);
                var cobrancaPix = new PIXPagSeguro().CriarPedido(order, inscricaoTorneio.torneio.barragem.tokenPagSeguro);
                return Ok(cobrancaPix.qr_codes[0].text);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
            //return Ok("00020126830014br.gov.bcb.pix2561api.pagseguro.com/pix/v2/210387E0-A6BF-45D1-80B5-CFEB9BBCEE2F5204899953039865802BR5921Pagseguro Internet SA6009SAO PAULO62070503***63047E6D");
        }

        [ResponseType(typeof(void))]
        [HttpGet]
        [Route("api/InscricaoAPI/{Id}/CobrancaPIXQRCode")]
        public IHttpActionResult CobrancaPIXInscricaoQRCode(int Id)
        {
            try
            {
                //return BadRequest("token inválido");

                var qrcode = new QrCodeCobrancaTorneio();
                //qrcode.text = "00020126830014br.gov.bcb.pix2561api.pagseguro.com/pix/v2/210387E0-A6BF-45D1-80B5-CFEB9BBCEE2F5204899953039865802BR5921Pagseguro Internet SA6009SAO PAULO62070503***63047E6D";
                //qrcode.link = "/Content/image/QRCode.png";
                //return Ok(qrcode);

                var inscricaoTorneio = db.InscricaoTorneio.Find(Id);
                var order = montarPedidoPIX(inscricaoTorneio);
                var cobrancaPix = new PIXPagSeguro().CriarPedido(order, inscricaoTorneio.torneio.barragem.tokenPagSeguro);
                qrcode.text = cobrancaPix.qr_codes[0].text;
                if (cobrancaPix.qr_codes[0].links[0].media == "image/png")
                {
                    qrcode.link = cobrancaPix.qr_codes[0].links[0].href;
                }

                return Ok(qrcode);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
            //return Ok("00020126830014br.gov.bcb.pix2561api.pagseguro.com/pix/v2/210387E0-A6BF-45D1-80B5-CFEB9BBCEE2F5204899953039865802BR5921Pagseguro Internet SA6009SAO PAULO62070503***63047E6D");
        }

        [Obsolete("Utilizar o endpoint RealizarInscricao")]
        [ResponseType(typeof(void))]
        [HttpPost]
        [AllowAnonymous]
        [Route("api/InscricaoAPI")]
        public IHttpActionResult PostInscricao(int userId, int torneioId, int classe1, int classe2, int classe3, int classe4, string observacao = "", bool isSocio = false, bool isFederado = false)
        {
            // validar se já houve inscrição:
            var temInscricao = db.InscricaoTorneio.Where(i => i.userId == userId && i.torneioId == torneioId).Count();
            if (temInscricao > 0)
            {
                return BadRequest("001-Você já possui uma inscrição neste torneio.");
            }

            var inscricaoModel = new InscricaoModel() { UserId = userId, TorneioId = torneioId, IdCategoria1 = classe1, IdCategoria2 = classe2, IdCategoria3 = classe3, IdCategoria4 = classe4, Observacao = observacao, IsSocio = isSocio, IsFederado = isFederado };

            var mensagemRetorno = new TorneioController().InscricaoNegocio(inscricaoModel, "");
            if (mensagemRetorno.tipo == "erro")
            {
                return BadRequest(mensagemRetorno.mensagem);
            }
            else
            {
                var inscricao = new InscricaoTorneio();
                inscricao.Id = mensagemRetorno.id;
                return Ok(inscricao);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("api/InscricaoAPI/RealizarInscricao")]
        public IHttpActionResult RealizarInscricao(InscricaoModel inscricaoModel)
        {
            // validar se já houve inscrição:
            var temInscricao = db.InscricaoTorneio.Count(i => i.userId == inscricaoModel.UserId && i.torneioId == inscricaoModel.TorneioId);
            if (temInscricao > 0)
            {
                return BadRequest("001-Você já possui uma inscrição neste torneio.");
            }
            var mensagemRetorno = new TorneioController().InscricaoNegocio(inscricaoModel, "");
            if (mensagemRetorno.tipo == "erro")
            {
                return BadRequest(mensagemRetorno.mensagem);
            }
            else
            {
                var inscricao = new InscricaoTorneio();
                inscricao.Id = mensagemRetorno.id;
                return Ok(inscricao);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("api/InscricaoAPI/ReceberNotificacao")]
        public IHttpActionResult PostReceberNotificacao(Cobranca cobranca)
        {
            List<string> classesPagtoOk = new List<string>();

            var req = Request.Headers.Authorization;
            //if(req.Parameter != "<<token>>")
            //{
            //    return null;
            //}
            string[] refs = cobranca.reference_id.Split('-');
            if (refs[0].Equals("T"))
            { // se for torneio
                int idInscricao = Convert.ToInt32(refs[1]);
                var inscricao = db.InscricaoTorneio.Find(idInscricao);

                if (cobranca.charges[0].status == "PAID")
                {
                    inscricao.isAtivo = true;
                    if ((inscricao.valorPendente != null) && (inscricao.valorPendente != 0))
                    {
                        inscricao.valor = inscricao.valor + inscricao.valorPendente;
                        inscricao.valorPendente = 0;
                    }
                    classesPagtoOk.Add(inscricao.classeTorneio.nome);
                }
                inscricao.statusPagamento = cobranca.charges[0].status + "";
                inscricao.formaPagamento = "PIX";
                db.Entry(inscricao).State = EntityState.Modified;
                db.SaveChanges();

                var log = new Log();
                log.descricao = "PIX:" + DateTime.Now + ":" + inscricao.Id + ":" + cobranca.charges[0].status;
                db.Log.Add(log);
                db.SaveChanges();

                // ativar outras inscrições caso existam
                var listInscricao = db.InscricaoTorneio.Where(t => t.torneioId == inscricao.torneioId && t.userId == inscricao.userId && t.Id != inscricao.Id).ToList();
                foreach (var item in listInscricao)
                {
                    if (cobranca.charges[0].status == "PAID")
                    {
                        item.isAtivo = true;
                        if ((item.valorPendente != null) && (item.valorPendente != 0))
                        {
                            item.valor = item.valor + item.valorPendente ?? 0;
                            item.valorPendente = 0;
                        }
                        classesPagtoOk.Add(item.classeTorneio.nome);
                    }
                    item.statusPagamento = cobranca.charges[0].status + "";
                    item.formaPagamento = "PIX";
                    //item.valor = (float)transaction.GrossAmount;
                    db.Entry(item).State = EntityState.Modified;
                    db.SaveChanges();

                }

                NotificarUsuarioPagamentoRealizado(classesPagtoOk, inscricao.torneio.nome, inscricao.userId, inscricao.torneioId);
            }

            return Ok();
        }

        [ResponseType(typeof(void))]
        [HttpGet]
        [Route("api/InscricaoAPI/ConsultaLog")]
        public IHttpActionResult ConsultaLog()
        {
            try
            {
                var log = db.Log.Where(d => d.descricao.Contains("token")).ToList();
                return Ok(log);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
            //return Ok("00020126830014br.gov.bcb.pix2561api.pagseguro.com/pix/v2/210387E0-A6BF-45D1-80B5-CFEB9BBCEE2F5204899953039865802BR5921Pagseguro Internet SA6009SAO PAULO62070503***63047E6D");
        }

        private void NotificarUsuarioPagamentoRealizado(List<string> classes, string nomeTorneio, int userId, int torneioId)
        {
            if (classes.Count == 0)
                return;

            var msgConfirmacao = $"O pagamento da inscrição na(s) categoria(s) {string.Join(", ", classes.Distinct())} do torneio {nomeTorneio} foi confirmado.";
            var titulo = "Pagamento da inscrição confirmado.";

            var userFb = db.UsuarioFirebase.FirstOrDefault(x => x.UserId == userId);
            if (userFb == null)
                return;

            var dadosMensagemUsuario = new FirebaseNotificationModel<DataToneioModel>() { to = userFb.Token, notification = new NotificationModel() { title = titulo, body = msgConfirmacao }, data = new DataToneioModel() { torneioId = torneioId } };
            new FirebaseNotification().SendNotification(dadosMensagemUsuario);
        }
    }
}