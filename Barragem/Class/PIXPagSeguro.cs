using Newtonsoft.Json;
using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Script.Serialization;

public class PIXPagSeguro
{
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
            var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://secure.sandbox.api.pagseguro.com/instant-payments/cob/" + data.to);
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


    public static async void Send(FirebaseNotificationModel firebaseModel)
    {
        HttpRequestMessage httpRequest = null;
        HttpClient httpClient = null;
        //var authorizationKey = string.Format("key={0}", "YourFirebaseServerKey");
        var jsonBody = JsonConvert.SerializeObject(firebaseModel);

        try
        {
            string server_api_key = ConfigurationManager.AppSettings["SERVER_API_KEY"];
            string sender_id = ConfigurationManager.AppSettings["SENDER_ID"];
            httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://fcm.googleapis.com/fcm/send");

            var authorizationKey = string.Format("key={0}", server_api_key);
            httpRequest.Headers.TryAddWithoutValidation("Authorization", authorizationKey);
            httpRequest.Headers.Add("Sender", sender_id);

            httpRequest.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            httpClient = new HttpClient();
            using (await httpClient.SendAsync(httpRequest))
            {
            }
        }
        catch (Exception e)
        {
            throw e;
        }
        finally
        {
            httpRequest.Dispose();
            httpClient.Dispose();
        }
    }
}

public class Cobranca
{
    [JsonProperty(PropertyName = "to")]
    public string to { get; set; }

    [JsonProperty(PropertyName = "notification")]
    public NotificationModelx notification { get; set; }

    [JsonProperty(PropertyName = "data")]
    public DataModelx data { get; set; }

}

public class NotificationModelx
{
    [JsonProperty(PropertyName = "body")]
    public string body { get; set; }

    [JsonProperty(PropertyName = "title")]
    public string title { get; set; }

}
public class DataModelx
{
    [JsonProperty(PropertyName = "title")]
    public string title { get; set; }

    [JsonProperty(PropertyName = "body")]
    public string body { get; set; }

    [JsonProperty(PropertyName = "type")]
    public string type { get; set; }

    [JsonProperty(PropertyName = "idRanking")]
    public int idRanking { get; set; }


}





