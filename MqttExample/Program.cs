using MQTTnet;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MqttExample
{
    public class MessageHandler : MQTTnet.Client.Receiving.IMqttApplicationMessageReceivedHandler
    {
        public Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            // this handler is installed below as the recipient of mqtt messages on topics that we subscribe to
            Console.WriteLine("received message");
            Console.WriteLine(".. topic = {0}", e.ApplicationMessage.Topic);

            // in a real application, the payload here can be parsed as JSON and then used to drive whatever application behavior is desired
            Console.WriteLine(".. payload = {0}", Encoding.UTF8.GetString(e.ApplicationMessage.Payload));
            Console.WriteLine(".. qos = {0}", e.ApplicationMessage.QualityOfServiceLevel);
            Console.WriteLine(".. retain = {0}", e.ApplicationMessage.Retain);
            return Task.Run(() => { });
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            // get configuration info from the command-line
            if (args.Length < 4)
            {
                Console.WriteLine("usage: MqttExample.exe <you@corp.com> <password> <orgId> <acuId>");
                Environment.Exit(1);
            }
            string username = args[0];
            string password = args[1];
            int orgId = int.Parse(args[2]);
            int acuId = int.Parse(args[3]);
            string apiBase = "https://api.openpath.com";
            if (args.Length >= 5)
            {
                apiBase = args[4];
            }

            Console.WriteLine("hello");

            // build http basic auth header from username and password, for authentication to Openpath API
            string opAuth = String.Format("Basic {0}", System.Convert.ToBase64String(Encoding.UTF8.GetBytes(username + ":" + password)));
            Console.WriteLine("built auth string {0}", opAuth);

            // send Openpath API request to fetch temp credentials for connecting to the AWS MQTT broker
            string url = String.Format("{0}/orgs/{1}/mqttCredentials?options=withShadows", apiBase, orgId);
            WebRequest webRequest = WebRequest.Create(url);
            webRequest.Headers.Add("Authorization", opAuth);
            HttpWebResponse resp = (HttpWebResponse)webRequest.GetResponse();
            Console.WriteLine("got mqttCredentials response -> status {0} {1}",
                (int)resp.StatusCode, resp.StatusDescription);
            
            string websocketsUrl = null;
            string clientId = null;
            List<object> subscribeTopics = null;
            using (System.IO.Stream s = resp.GetResponseStream())
            {
                using (var sr = new System.IO.StreamReader(s))
                {
                    string responseText = sr.ReadToEnd();

                    // Console.WriteLine(body);
                    dynamic response = JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(responseText, new ExpandoObjectConverter());
                    websocketsUrl = response.data.websocketsUrl;
                    Console.WriteLine("websocketsUrl {0}", websocketsUrl);
                    clientId = response.data.clientId;
                    Console.WriteLine("clientId {0}", clientId);
                    subscribeTopics = response.data.subscribeTopics;
                    Console.WriteLine("subscribeTopics {0}", subscribeTopics);
                }
            }

            // create mqtt client instance and connect to mqtt broker
            var factory = new MqttFactory();
            var mqttClient = factory.CreateMqttClient();
            Console.WriteLine("created mqtt client");
            MQTTnet.Client.Options.IMqttClientOptions options = new MQTTnet.Client.Options.MqttClientOptionsBuilder()
                .WithClientId(clientId)
                .WithWebSocketServer(websocketsUrl)
                .WithTls()
                .WithCleanSession()
                .Build();
            Console.WriteLine("built connect options");
            await mqttClient.ConnectAsync(options, CancellationToken.None);
            Console.WriteLine("connected");

            // subscribe to the topics indicated in the mqttCredentials response
            mqttClient.ApplicationMessageReceivedHandler = new MessageHandler();
            foreach (string topic in subscribeTopics)
            {
                var subOptions = new MQTTnet.Client.Subscribing.MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter(topic)
                    .Build();
                await mqttClient.SubscribeAsync(subOptions, CancellationToken.None);
                Console.WriteLine("subscribed {0}", topic);
            }

            // now send another Openpath API call, to request that the given ACU send an update to its shadow state
            // if all goes well, this should result in an mqtt message being received by this program within a few seconds
            string url2 = String.Format("{0}/orgs/{1}/acus/{2}/refreshShadow", apiBase, orgId, acuId);
            Console.WriteLine("shadow refresh url {0}", url2);
            WebRequest req2 = WebRequest.Create(url2);
            req2.Method = "POST";
            req2.Headers.Add("Authorization", opAuth);
            HttpWebResponse resp2 = (HttpWebResponse)req2.GetResponse();
            Console.WriteLine("requested shadow refresh -> status {0} {1}",
                (int)resp2.StatusCode, resp2.StatusDescription);

            // sleep loop, waiting for mqtt messages to be received
            while (true)
            {
                Console.WriteLine("sleeping...");
                Thread.Sleep(1000);
            }
        }
    }
}
