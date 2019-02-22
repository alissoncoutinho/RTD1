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
using System.Data.EntityClient;
using System.Transactions;
using Barragem.Class;
using WebMatrix.WebData;
using System.Web.Security;
using System.Threading.Tasks;

namespace Barragem.Controllers
{
    [Authorize(Roles = "admin")]
    public class PagamentoController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();

        //
        // GET: /Rodada/

        public ActionResult Index(string msg = "", string detalheErro = "")
        {
            if (msg.Equals("ok"))
            {
                ViewBag.Ok = msg;
            }
            else if (!msg.Equals(""))
            {
                ViewBag.MsgErro = msg;
                ViewBag.DetalheErro = detalheErro;
            }
            List<Pagamento> pagamentos = db.Pagamento.OrderByDescending(p => p.Id).ToList();

            return View(pagamentos);
        }

        public ActionResult Create()
        {
            var data = DateTime.Now;
            var ano = data.Year;
            ViewBag.anoAnterior = ano - 1;
            ViewBag.anoAtual = ano;
            ViewBag.proximoAno = ano + 1;
            ViewBag.mes = data.Month;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Pagamento pagamento)
        {
            try
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    if (ModelState.IsValid)
                    {
                        pagamento.status = "Criado";
                        db.Pagamento.Add(pagamento);
                        db.SaveChanges();

                        var barragens = db.BarragemView.Where(b => b.isAtiva && !b.isTeste && (bool)!b.soTorneio).ToList();

                        foreach(BarragemView barragem in barragens){
                            var pgBarragem = new PagamentoBarragem();
                            pgBarragem.barragemId = barragem.Id;
                            pgBarragem.pagamentoId = pagamento.Id;
                            pgBarragem.cobrar = true;
                            pgBarragem.status = "Criado";
                            db.PagamentoBarragem.Add(pgBarragem);
                        }
                        db.SaveChanges();
                        scope.Complete();
                        return RedirectToAction("Index");
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.MsgErro = ex.Message;
            }

            return View(pagamento);
        }

        // GET: /Rodada/Edit/5
        public ActionResult Edit(int id = 0, string Msg="", string MsgErro="")
        {
            ViewBag.Ok = Msg;
            ViewBag.MsgErro = MsgErro;
            var ano = DateTime.Now.Year;
            ViewBag.anoAnterior = ano - 1;
            ViewBag.anoAtual = ano;
            ViewBag.proximoAno = ano + 1;
            Pagamento pagamento = db.Pagamento.Find(id);
            ViewBag.PagamentoBarragens = db.PagamentoBarragem.Where(pb => pb.pagamentoId == id).ToList();
            
            return View(pagamento);
        }

        [HttpPost]
        public ActionResult AlterarStatus(int Id, string status)
        {
            try
            {
                var pagamentoBarragem = db.PagamentoBarragem.Find(Id);
                pagamentoBarragem.status = status;
                db.Entry(pagamentoBarragem).State = EntityState.Modified;
                db.SaveChanges();
                Pagamento pagamento = db.Pagamento.Find(pagamentoBarragem.pagamentoId);
                pagamento.arrecadado = db.PagamentoBarragem.Where(pg => pg.pagamentoId == pagamento.Id && pg.status == "Pago").Sum(pg=>pg.valor);
                if (pagamento.arrecadado == pagamento.areceber)
                {
                    pagamento.status = "Finalizado";
                }
                db.Entry(pagamento).State = EntityState.Modified;
                db.SaveChanges();

                return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
        }

        private Boleto montarDadosBoleto(PagamentoBarragem pb)
        {
            var barragemId = pb.barragemId;
            var barragem = db.BarragemView.Find(barragemId);
            var boleto = new Boleto();
            boleto.order_id = "ranking-"+pb.barragemId+"-"+pb.Id;
            boleto.payer_email = barragem.email;
            boleto.payer_name = barragem.nomeResponsavel;
            boleto.payer_cpf_cnpj = barragem.cpfResponsavel;
            boleto.days_due_date = 5;
            var item = new Item();
            item.item_id = pb.Id + "";
            item.price_cents = Convert.ToInt32(pb.valor*100);
            item.description = pb.barragem.nome;
            item.quantity = 1;
            boleto.items = new List<Item>();
            boleto.items.Add(item);
            return boleto;
        }

        public async Task<ActionResult> EnviarBoleto(int Id)
        {
            try
            {
                ClientePagHiper.listBoletoRetorno = new List<BoletoRetorno>();
                var pagamentoBarragem = db.PagamentoBarragem.Where(pb => pb.pagamentoId == Id && (bool)pb.cobrar).ToList();
                foreach (PagamentoBarragem pb in pagamentoBarragem){
                    var boleto = montarDadosBoleto(pb);
                    await new ClientePagHiper().EmitirBoletoAsync(boleto);
                }
                while (pagamentoBarragem.Count() > ClientePagHiper.listBoletoRetorno.Count()) {

                }
                var conferencia = 0;
                foreach (BoletoRetorno bR in ClientePagHiper.listBoletoRetorno)
                {
                    if (bR.retorno.create_request.result == "success")
                    {
                        int id = Convert.ToInt32(bR.boleto.items[0].item_id);
                        var pb = pagamentoBarragem.Where(p => p.Id == id).SingleOrDefault();
                        pb.linkBoleto = bR.retorno.create_request.bank_slip.url_slip_pdf;
                        pb.digitableLine = bR.retorno.create_request.bank_slip.digitable_line;
                        pb.status = "Aguardando";
                        db.Entry(pb).State = EntityState.Modified;
                        db.SaveChanges();
                        //EnviarEmail(bR.boleto.payer_email, bR.boleto.payer_name, pb.barragem.nome, pb.linkBoleto);
                        conferencia++;
                    }else if (bR.retorno.create_request.result == "reject")
                    {
                        int id = Convert.ToInt32(bR.boleto.items[0].item_id);
                        var pb = pagamentoBarragem.Where(p => p.Id == id).SingleOrDefault();
                        pb.status = bR.retorno.create_request.response_message;
                        db.Entry(pb).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
                var pagamento = db.Pagamento.Find(Id);
                if (conferencia == pagamentoBarragem.Count()) {
                    pagamento.status = "Em Andamento";
                } else {
                    pagamento.status = "Erro no envio";
                }
                
                db.Entry(pagamento).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Edit", "Pagamento", new { Id = Id, Msg = "Boletos gerados com sucesso." });
            }
            catch (Exception ex)
            {
                return RedirectToAction("Edit", "Pagamento", new { Id = Id, MsgErro = ex.InnerException });
            }

        }


        public ActionResult Apurar(int Id)
        {
            try
            {
                var pagamentoBarragem = db.PagamentoBarragem.Where(pb=>pb.pagamentoId==Id).ToList();
                foreach(PagamentoBarragem pb in pagamentoBarragem){
                    if ((bool)pb.cobrar) { 
                        int qtddUsuario = db.UserProfiles.Where(u => u.barragemId == pb.barragemId && (u.situacao == "ativo" || u.situacao == "suspenso" || u.situacao == "licenciado")).Count();
                        pb.qtddUsuario = qtddUsuario;
                        pb.valor = qtddUsuario * pb.barragem.valorPorUsuario;
                        db.Entry(pb).State = EntityState.Modified;
                    }
                }
                db.SaveChanges();
                var pagamento = db.Pagamento.Find(Id);
                pagamento.areceber = db.PagamentoBarragem.Where(pg => pg.pagamentoId == pagamento.Id).Sum(pg => pg.valor);
                pagamento.status = "Apurado";
                db.Entry(pagamento).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Edit", "Pagamento", new { Id = Id, Msg = "Apuração realizada com sucesso." }); 
            } catch (Exception ex){
                return RedirectToAction("Edit", "Pagamento", new { Id = Id, MsgErro = ex.InnerException });
            }

        }

        public ActionResult InativarBarragensPendentes(int Id)
        {
            try
            {
                var pagamentoBarragem = db.PagamentoBarragem.Where(pb => pb.pagamentoId == Id && (bool) pb.cobrar).ToList();
                foreach (PagamentoBarragem pb in pagamentoBarragem)
                {
                    if (pb.status != "Pago")
                    {
                        var barragem = db.Barragens.Find(pb.barragemId);
                        barragem.isAtiva = false;
                        db.Entry(barragem).State = EntityState.Modified;
                    }
                }
                db.SaveChanges();
                return RedirectToAction("Edit", "Pagamento", new { Id = Id, Msg = "Inativação realizada com sucesso." });
            }
            catch (Exception ex)
            {
                return RedirectToAction("Edit", "Pagamento", new { Id = Id, MsgErro = ex.InnerException });
            }

        }

        [HttpPost]
        public ActionResult CobrarBarragem(int Id){
            try
            {
                var pagamentoBarragem = db.PagamentoBarragem.Find(Id);
                pagamentoBarragem.cobrar = !pagamentoBarragem.cobrar;
                db.Entry(pagamentoBarragem).State = EntityState.Modified;
                db.SaveChanges();
                return Json(new { erro = "", retorno = 1 }, "text/plain", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { erro = ex.Message, retorno = 0 }, "text/plain", JsonRequestBehavior.AllowGet);
            }

        }

        //
        // POST: /Rodada/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Pagamento pagamento)
        {
            if (ModelState.IsValid)
            {
                db.Entry(pagamento).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(pagamento);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public ActionResult Delete(int id)
        {
            Barragens barragens = db.Barragens.Find(id);
            db.Barragens.Remove(barragens);
            db.SaveChanges();
            return RedirectToAction("Index");
        }



        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}