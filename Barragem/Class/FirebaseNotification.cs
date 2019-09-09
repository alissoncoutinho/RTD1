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
    public void SendNotification(object data)
    {
        var serializer = new JavaScriptSerializer();
        var json = serializer.Serialize(data);
        Byte[] byteArray = Encoding.UTF8.GetBytes(json);
        SendNotification(byteArray);
    }
    public void SendNotification(Byte[] byteArray)
    {
        try {
            string server_api_key = ConfigurationManager.AppSettings["SERVER_API_KEY"];
            string sender_id = ConfigurationManager.AppSettings["SENDER_ID"];
            WebRequest httpRequest = WebRequest.Create("https://fcm.googleapis.com/fcm/send");
            httpRequest.Method = "post";
            httpRequest.ContentType = "application/json";
            httpRequest. Headers. Add($"Authorization: key={server_api_key}");
            httpRequest.Headers.Add($"Sender: id={sender_id}");

            httpRequest.ContentLength = byteArray.Length;
            //System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            Stream dataStream = httpRequest.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();

            WebResponse tresponse = httpRequest.GetResponse();
            dataStream = tresponse.GetResponseStream();
            StreamReader tReader = new StreamReader(dataStream);

            string sResponseFromServer = tReader.ReadToEnd();
            tReader.Close();
            dataStream.Close();
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
    [JsonProperty(PropertyName = "message")]
    public Message message { get; set; }
}

public class Message
{
    [JsonProperty(PropertyName = "topic")]
    public string topic { get; set; }

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




/*{
  "message":{
    "topic":"subscriber-updates",
    "notification":{
      "body" : "This week's edition is now available.",
      "title" : "NewsMagazine.com",
    },
    "data" : {
      "volume" : "3.21.15",
      "contents" : "http://www.news-magazine.com/world-week/21659772"
    },
    "android":{
      "priority":"normal"
    },
    "apns":{
      "headers":{
        "apns-priority":"5"
      }
    },
    "webpush": {
      "headers": {
        "Urgency": "high"
      }
    }
  }
}*/
