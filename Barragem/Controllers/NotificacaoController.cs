using Barragem.Class;
using Barragem.Context;
using Barragem.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Uol.PagSeguro.Domain;
using Uol.PagSeguro.Resources;
using Uol.PagSeguro.Service;
using System.Threading.Tasks;

namespace Barragem.Controllers
{
    public class NotificacaoController : Controller
    {
        private BarragemDbContext db = new BarragemDbContext();
        //
        // GET: /Notificacao/

        public ActionResult RetornoPagamento()
        {
            return View();
        }

        public ActionResult TesteRequest()
        {
            return View();
        }
        [HttpPost]
        public void Receber(string notificationCode, string notificationType, int ranking=0)
        {
            var log = new Log();
            log.descricao = "Chegou:" + DateTime.Now + ":" + notificationCode;
            db.Log.Add(log);
            db.SaveChanges();
            
            var barragem = db.BarragemView.Find(ranking);
            AccountCredentials credentials = new AccountCredentials(barragem.emailPagSeguro, barragem.tokenPagSeguro);

            if (notificationType == "transaction")
            {
                // obtendo o objeto transaction a partir do código de notificação
                Transaction transaction = NotificationService.CheckTransaction(credentials, notificationCode);
                // Data da criação 
                DateTime date = transaction.Date;
                // Data da última atualização 
                DateTime lastEventDate = transaction.LastEventDate;
                // Código da transação 
                string code = transaction.Code;
                // Refência 
                string reference = transaction.Reference;
                // Valor bruto  
                decimal grossAmount = transaction.GrossAmount;
                // Tipo 
                int type = transaction.TransactionType;
                // Status 
                /* Código	Significado
                1	Aguardando pagamento: o comprador iniciou a transação, mas até o momento o PagSeguro não recebeu nenhuma informação sobre o pagamento.
                2	Em análise: o comprador optou por pagar com um cartão de crédito e o PagSeguro está analisando o risco da transação.
                3	Paga: a transação foi paga pelo comprador e o PagSeguro já recebeu uma confirmação da instituição financeira responsável pelo processamento.
                4	Disponível: a transação foi paga e chegou ao final de seu prazo de liberação sem ter sido retornada e sem que haja nenhuma disputa aberta.
                5	Em disputa: o comprador, dentro do prazo de liberação da transação, abriu uma disputa.
                6	Devolvida: o valor da transação foi devolvido para o comprador.
                7	Cancelada: a transação foi cancelada sem ter sido finalizada.
                8	Debitado: o valor da transação foi devolvido para o comprador.
                9	Retenção temporária: o comprador contestou o pagamento junto à operadora do cartão de crédito ou abriu uma demanda judicial ou administrativa (Procon).
                */
                int status = transaction.TransactionStatus;
                // Valor líquido  
                decimal netAmount = transaction.NetAmount;
                // Valor das taxas cobradas  
                decimal feeAmount = transaction.FeeAmount;
                // Valor extra ou desconto
                decimal extraAmount = transaction.ExtraAmount;
                // Tipo de meio de pagamento
                PaymentMethod paymentMethod = transaction.PaymentMethod;

                string[] refs = reference.Split('-');
                if (refs[0].Equals("T"))
                { // se for torneio
                    int idInscricao = Convert.ToInt32(refs[1]);
                    var inscricao = db.InscricaoTorneio.Find(idInscricao);
                    if (status == 3)
                    {
                        inscricao.isAtivo = true;
                    }
                    inscricao.statusPagamento = status + "";
                    inscricao.formaPagamento = paymentMethod.PaymentMethodType + "";
                    inscricao.valor = (float)transaction.GrossAmount;
                    db.Entry(inscricao).State = EntityState.Modified;
                    db.SaveChanges();
                    var log2 = new Log();
                    log2.descricao = ranking + " movimentacao ok " + status + ":" + DateTime.Now + ":" + notificationCode;
                    db.Log.Add(log2);
                    db.SaveChanges();

                    // ativar segunda inscrição caso exista
                    var listInscricao = db.InscricaoTorneio.Where(t => t.torneioId == inscricao.torneioId && t.userId == inscricao.userId && t.Id != inscricao.Id).ToList();
                    if (listInscricao.Count() > 0)
                    {
                        var inscricao2 = listInscricao[0];
                        if (status == 3){
                            inscricao2.isAtivo = true;
                        }
                        inscricao2.statusPagamento = status + "";
                        inscricao2.formaPagamento = paymentMethod.PaymentMethodType + "";
                        inscricao2.valor = (float)transaction.GrossAmount;
                        db.Entry(inscricao2).State = EntityState.Modified;
                        db.SaveChanges();

                    }
                }

            }
            
        }

        [HttpPost]
        public async Task ReceberPagHiperAsync(string apiKey, string transaction_id, string notification_id, DateTime notification_date) {
            var log = new Log();
            log.descricao = "PagHiper:" + DateTime.Now + ":" + apiKey + "::" + transaction_id + "::" + notification_id + "::" + notification_date;
            db.Log.Add(log);
            db.SaveChanges();
            var notificacao = new Notificacao();
            notificacao.transaction_id = transaction_id;
            notificacao.notification_id = notification_id;

            await new ClientePagHiper().ConfirmarNotificacao(notificacao);
        }

        
    }    
}
