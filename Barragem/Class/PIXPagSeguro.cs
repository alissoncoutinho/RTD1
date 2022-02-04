using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using Barragem.Models;
using Barragem.Context;

public class PIXPagSeguro
{
    private BarragemDbContext db = new BarragemDbContext();
    public void CriarTokenPIX(FirebaseNotificationModel data)
    {
        try
        {
            //string server_api_key = ConfigurationManager.AppSettings["SERVER_API_KEY"];
            //string sender_id = ConfigurationManager.AppSettings["SENDER_ID"];
            var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://secure.sandbox.api.pagseguro.com/pix/oauth2");
            httpWebRequest.Method = "post";
            httpWebRequest.ContentType = "text/plain";
            string authInfo = "<username>" + ":" + "<password>";
            authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
            httpWebRequest.Headers.Add("Authorization", "Basic " + authInfo);
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string body = @"""grant_type"": ""client_credentials"",
                    " + "\n" +
                    @"""scope"": ""pix.write pix.read""";
                streamWriter.Write(body);
            }
            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
            }
        }
        catch (Exception e)
        {
            throw e;
        }
    }

    public string GerarCobranca(Cobranca data)
    {
        try
        {
            //string server_api_key = ConfigurationManager.AppSettings["SERVER_API_KEY"];
            string token = ConfigurationManager.AppSettings["TOKEN_PIX"];
            var jsonBody = JsonConvert.SerializeObject(data);
            var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://secure.sandbox.api.pagseguro.com/instant-payments/cob/" + data.id);
            httpWebRequest.Method = "put";
            httpWebRequest.ContentType = "text/plain";
            httpWebRequest.Headers.Add("Authorization", "Bearer " + token);
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(jsonBody);
            }
            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
                return result;
            }
        }
        catch (Exception e)
        {
            throw e;
        }
    }

    private string jsonPedidoTeste()
    {
        var body = @"{
" + "\n" +
@"    ""reference_id"": ""ex-00001"",
" + "\n" +
@"    ""customer"": {
" + "\n" +
@"        ""name"": ""Jose da Silva"",
" + "\n" +
@"        ""email"": ""email@test.com"",
" + "\n" +
@"        ""tax_id"": ""12345678909"",
" + "\n" +
@"        ""phones"": [
" + "\n" +
@"            {
" + "\n" +
@"                ""country"": ""55"",
" + "\n" +
@"                ""area"": ""11"",
" + "\n" +
@"                ""number"": ""999999999"",
" + "\n" +
@"                ""type"": ""MOBILE""
" + "\n" +
@"            }
" + "\n" +
@"        ]
" + "\n" +
@"    },
" + "\n" +
@"    ""items"": [
" + "\n" +
@"        {
" + "\n" +
@"            ""reference_id"": ""referencia do item"",
" + "\n" +
@"            ""name"": ""nome do item"",
" + "\n" +
@"            ""quantity"": 1,
" + "\n" +
@"            ""unit_amount"": 500
" + "\n" +
@"        }
" + "\n" +
@"    ],
" + "\n" +
@"    ""qr_codes"": [
" + "\n" +
@"        {
" + "\n" +
@"            ""amount"": {
" + "\n" +
@"                ""value"": 500
" + "\n" +
@"            }
" + "\n" +
@"        }
" + "\n" +
@"    ],
" + "\n" +
@"    ""shipping"": {
" + "\n" +
@"        ""address"": {
" + "\n" +
@"            ""street"": ""Avenida Brigadeiro Faria Lima"",
" + "\n" +
@"            ""number"": ""1384"",
" + "\n" +
@"            ""complement"": ""apto 12"",
" + "\n" +
@"            ""locality"": ""Pinheiros"",
" + "\n" +
@"            ""city"": ""São Paulo"",
" + "\n" +
@"            ""region_code"": ""SP"",
" + "\n" +
@"            ""country"": ""BRA"",
" + "\n" +
@"            ""postal_code"": ""01452002""
" + "\n" +
@"        }
" + "\n" +
@"    },
" + "\n" +
@"    ""notification_urls"": [
" + "\n" +
@"        ""https://meusite.com/notificacoes""
" + "\n" +
@"    ]
" + "\n" +
@"}";
        return body;
    }

    public Cobranca CriarPedido(Order order, string token="")
    {
        try
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;
            if (String.IsNullOrEmpty(token))
            {
                token = ConfigurationManager.AppSettings["TOKEN_PAGSEGURO"];
            }
            string urlOrder = ConfigurationManager.AppSettings["URL_PAGSEGURO"] + "/orders";
            var jsonBody = JsonConvert.SerializeObject(order);
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(urlOrder);
            httpWebRequest.Method = "post";
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Headers.Add("Authorization", "Bearer " + token); // produção
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(jsonBody);
            }
            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
                var cobranca = JsonConvert.DeserializeObject<Cobranca>(result);
                return cobranca;
            }
        }
        catch (WebException e)
        {
            var result = new StreamReader(e.Response.GetResponseStream()).ReadToEnd();
            var erroMsg = JsonConvert.DeserializeObject<ErrorMessage>(result);
            throw new Exception(erroMsg.codeDescription[0].description);
        }
        catch (Exception e)
        {
            throw e;
        }
    }
        
}

public class Cobranca
{
    [JsonProperty(PropertyName = "id")]
    public string id { get; set; }

    [JsonProperty(PropertyName = "reference_id")]
    public string reference_id { get; set; }

    [JsonProperty(PropertyName = "charges")]
    public List<Charge> charges { get; set; }

    [JsonProperty(PropertyName = "qr_codes")]
    public List<QrCode> qr_codes { get; set; }

}

public class Charge
{
    [JsonProperty(PropertyName = "status")]
    public string status { get; set; }
}

public class QrCode
{
    [JsonProperty(PropertyName = "text")]
    public string text { get; set; }
    public Amount amount { get; set; }
    public List<linkQRCODE> links { get; set; }
}

public class linkQRCODE
{
    public string href  { get; set; }
    public string media { get; set; }
}

public class Customer { 
    [JsonProperty(PropertyName = "name")]
    public string name { get; set; }
    [JsonProperty(PropertyName = "email")]
    public string email { get; set; }
    [JsonProperty(PropertyName = "tax_id")]
    public string tax_id { get; set; }
    
}

public class Amount
{
    [JsonProperty(PropertyName = "value")]
    public int value { get; set; }
}

public class ItemPedido
{
    [JsonProperty(PropertyName = "reference_id")]
    public string reference_id { get; set; }
    [JsonProperty(PropertyName = "name")]
    public string name { get; set; }
    [JsonProperty(PropertyName = "quantity")]
    public int quantity { get; set; }
    [JsonProperty(PropertyName = "unit_amount")]
    public int unit_amount { get; set; }

}

public class Order{

    [JsonProperty(PropertyName = "reference_id")]
    public string reference_id { get; set; }

    [JsonProperty(PropertyName = "customer")]
    public Customer customer { get; set; }

    [JsonProperty(PropertyName = "items")]
    public List<ItemPedido> items { get; set; }

    [JsonProperty(PropertyName = "qr_codes")]
    public List<QrCode> qr_codes { get; set; }

    [JsonProperty(PropertyName = "notification_urls")]
    public string[] notification_urls { get; set; }
    
}

public class ErrorMessage
{
    [JsonProperty(PropertyName = "error_messages")]
    public List<CodeDescription> codeDescription { get; set; }
}

public class CodeDescription
{
    public string code { get; set; }
    public string description { get; set; }
}





