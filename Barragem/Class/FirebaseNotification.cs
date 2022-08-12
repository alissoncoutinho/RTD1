using Newtonsoft.Json;
using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;

public class FirebaseNotification
{
    public void SendNotification<T>(FirebaseNotificationModel<T> data)
    {
        try
        {
            string server_api_key = ConfigurationManager.AppSettings["SERVER_API_KEY"];
            string sender_id = ConfigurationManager.AppSettings["SENDER_ID"];
            var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://fcm.googleapis.com/fcm/send");
            httpWebRequest.Method = "post";
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Headers.Add($"Authorization: key={server_api_key}");
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = new JavaScriptSerializer().Serialize(data);
                streamWriter.Write(json);
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
}

public class FirebaseNotificationModel
{
    [JsonProperty(PropertyName = "to")]
    public string to { get; set; }

    [JsonProperty(PropertyName = "notification")]
    public NotificationModel notification { get; set; }

    [JsonProperty(PropertyName = "data")]
    public DataModel data { get; set; }

}

public class FirebaseNotificationModel<T>
{
    [JsonProperty(PropertyName = "to")]
    public string to { get; set; }

    [JsonProperty(PropertyName = "notification")]
    public NotificationModel notification { get; set; }

    [JsonProperty(PropertyName = "data")]
    public T data { get; set; }
}

public class NotificationModel
{
    [JsonProperty(PropertyName = "body")]
    public string body { get; set; }

    [JsonProperty(PropertyName = "title")]
    public string title { get; set; }
    
}
public class DataToneioModel: DataModel
{
    [JsonProperty(PropertyName = "idRanking")]
    public int idRanking { get; set; }

    [JsonProperty(PropertyName = "torneioId")]
    public int torneioId { get; set; }
}
public class DataLigaModel : DataModel
{
    [JsonProperty(PropertyName = "idRanking")]
    public int idRanking { get; set; }

    [JsonProperty(PropertyName = "ligaId")]
    public int ligaId { get; set; }
}

public class DataModel
{
    [JsonProperty(PropertyName = "title")]
    public string title { get; set; }

    [JsonProperty(PropertyName = "body")]
    public string body { get; set; }

    [JsonProperty(PropertyName = "type")]
    public string type { get; set; }
}





