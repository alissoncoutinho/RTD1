using Newtonsoft.Json;
using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Script.Serialization;

public class FirebaseNotification
{
    public void SendNotification(FirebaseNotificationModel data)
    {
        try {
            string server_api_key = ConfigurationManager.AppSettings["SERVER_API_KEY"];
            string sender_id = ConfigurationManager.AppSettings["SENDER_ID"];
            var httpWebRequest = (HttpWebRequest) WebRequest.Create("https://fcm.googleapis.com/fcm/send");
            httpWebRequest.Method = "post";
            httpWebRequest.ContentType = "application/json";
            httpWebRequest. Headers. Add($"Authorization: key={server_api_key}");
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream())){
                string json = new JavaScriptSerializer().Serialize(data);
                streamWriter.Write(json);
            }
            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
            }
        } catch (Exception e) {
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

public class FirebaseNotificationModel
{
    [JsonProperty(PropertyName = "to")]
    public string to { get; set; }

    [JsonProperty(PropertyName = "notification")]
    public NotificationModel notification { get; set; }
    
}

public class NotificationModel
{
    [JsonProperty(PropertyName = "body")]
    public string body { get; set; }

    [JsonProperty(PropertyName = "title")]
    public string title { get; set; }
    
}





