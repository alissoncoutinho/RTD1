using Barragem.Context;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Linq;
using System.Data;
using Barragem.Models;
using System.Data.Entity;

namespace Barragem.Class
{
    public class Item
    {
        public string item_id { get; set; }
        public string description { get; set; }
        public int quantity { get; set; }
        public int price_cents { get; set; }
    }
    public class Boleto
    {
        public string apiKey { get { return "apk_40465669-WocOWDbeFEYtUuSGyMSrTTwYrWjjsKYP"; } }
        public string order_id { get; set; }
        public string payer_email { get; set; }
        public string payer_name { get; set; }
        public string payer_cpf_cnpj { get; set; }
        public int days_due_date { get; set; }
        public string type_bank_slip { get { return "boletoA4"; } }
        public string notification_url { get { return "http://www.rankingdetenis.com/Notificacao/ReceberPagHiperAsync"; } }
        public bool per_day_interest { get { return false; } }
        public List<Item> items { get; set; }
                
    }

    public class BoletoRetorno
    {
        public Boleto boleto{get; set;}
        public Retorno retorno { get; set; }
    }

    public class Bank_slip {
        public string digitable_line { get; set; }
        public string url_slip { get; set; }
        public string url_slip_pdf { get; set; }
    }

    public class Create_request {
        public string result { get; set; }
        public string response_message { get; set; }
        public string transaction_id { get; set; }
        public string created_date { get; set; }
        public int value_cents { get; set; }
        public string status { get; set; }
        public string order_id { get; set; }
        public string due_date { get; set; }
        public Bank_slip bank_slip { get; set; }
        public int http_code { get; set; }

    }

    public class Status_request
    {
        public string result { get; set; }
        public string response_message { get; set; }
        public string transaction_id { get; set; }
        public string created_date { get; set; }
        public string status { get; set; }
        public List<Item> items { get; set; }
        public int http_code { get; set; }

    }

    public class Notificacao
    {
        public string token { get { return "YOY6V157GUGEH2M8NXZS07DRLC86HWXGLVMHWSPCM5R3"; } }
        public string apiKey { get { return "apk_40465669-WocOWDbeFEYtUuSGyMSrTTwYrWjjsKYP"; } }
        public string transaction_id { get; set; }
        public string notification_id { get; set; }
    }

    public class Retorno
    {
        public Create_request create_request{get; set;}
        
    }

    public class RetornoNotificacao
    {
        public Status_request status_request { get; set; }

    }

    public class ClientePagHiper{
        HttpClient client = new HttpClient();
        public static List<BoletoRetorno> listBoletoRetorno = new List<BoletoRetorno>();
        private BarragemDbContext db = new BarragemDbContext();

        public ClientePagHiper()
        {
            client.BaseAddress = new Uri("http://api.paghiper.com/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task EmitirBoletoAsync(Boleto boleto)
        {
            try
            {
                Retorno retorno = null;
                var response = await client.PostAsJsonAsync(
                    "transaction/create", boleto);

                if (response.IsSuccessStatusCode)
                {
                    retorno = await response.Content.ReadAsAsync<Retorno>();
                }
                else
                {
                    retorno = new Retorno();
                }
                BoletoRetorno br = new BoletoRetorno();
                br.boleto = boleto;
                br.retorno = retorno;
                listBoletoRetorno.Add(br);
            }catch(Exception e){
                BoletoRetorno br = new BoletoRetorno();
                br.boleto = boleto;
                br.retorno = new Retorno();
                listBoletoRetorno.Add(br);
            }
        }

        
        public async Task ConfirmarNotificacao(Notificacao notificacao)
        {
            try
            {
                var response = await client.PostAsJsonAsync(
                    "/transaction/notification", notificacao);

                if (response.IsSuccessStatusCode)
                {
                    var retorno = await response.Content.ReadAsAsync<RetornoNotificacao>();
                    int id = Convert.ToInt32(retorno.status_request.items[0].item_id);
                    var pb = db.PagamentoBarragem.Where(p => p.Id == id).SingleOrDefault();
                    if (retorno.status_request.result == "success"){
                        if (retorno.status_request.status == "paid" || retorno.status_request.status == "reserved" || retorno.status_request.status == "completed"){
                            pb.status = "Pago";
                        }else if (retorno.status_request.status == "canceled"){
                            if (pb.status != "Pago") { 
                                pb.status = retorno.status_request.status;
                                var barragem = db.Barragens.Find(pb.barragemId);
                                barragem.isAtiva = false;
                                db.Entry(barragem).State = EntityState.Modified;
                                db.SaveChanges();
                             }
                        } else if (retorno.status_request.status == "pending"){
                            // não faz nada
                        } else {
                            pb.status = retorno.status_request.status;
                        }
                        db.Entry(pb).State = EntityState.Modified;
                        db.SaveChanges();
                        if (retorno.status_request.status == "paid" || retorno.status_request.status == "reserved" || retorno.status_request.status == "completed"){
                            Pagamento pagamento = db.Pagamento.Find(pb.pagamentoId);
                            pagamento.arrecadado = db.PagamentoBarragem.Where(pg => pg.pagamentoId == pagamento.Id && pg.status == "Pago").Sum(pg => pg.valor);
                            if (pagamento.arrecadado == pagamento.areceber){
                                pagamento.status = "Finalizado";
                            }else
                            {
                                pagamento.status = "Em aberto";
                            }
                            db.Entry(pagamento).State = EntityState.Modified;
                            db.SaveChanges();
                        }
                    }else{
                        pb.status = retorno.status_request.response_message;
                        db.Entry(pb).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                var log2 = new Log();
                log2.descricao = "PagHiper: Exception:" + DateTime.Now + e.Message + ": transactionId:" + notificacao.transaction_id + ":" + notificacao.notification_id;
                db.Log.Add(log2);
                db.SaveChanges();
            }
        }

        //static void Main()
        //{
        //    RunAsync().GetAwaiter().GetResult();
        //}

        /*static async Task RunAsync()
        {
            // Update port # in the following line.
            client.BaseAddress = new Uri("http://localhost:64195/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                // Create a new product
                Product product = new Product
                {
                    Name = "Gizmo",
                    Price = 100,
                    Category = "Widgets"
                };

                var url = await CreateProductAsync(product);
                Console.WriteLine($"Created at {url}");

                // Get the product
                product = await GetProductAsync(url.PathAndQuery);
                ShowProduct(product);

                // Update the product
                Console.WriteLine("Updating price...");
                product.Price = 80;
                await UpdateProductAsync(product);

                // Get the updated product
                product = await GetProductAsync(url.PathAndQuery);
                ShowProduct(product);

                // Delete the product
                var statusCode = await DeleteProductAsync(product.Id);
                Console.WriteLine($"Deleted (HTTP Status = {(int)statusCode})");

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.ReadLine();
        }*/
    }
}