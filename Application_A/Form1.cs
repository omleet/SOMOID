using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace Application_A
{
    public partial class Form1 : Form
    {
        private const string baseURL = "http://localhost:10654";
        private const string ApplicationName = "door";
        private const string ContainerName = "door";
        private const string SubscriptionName = "door_status";
        private const string HttpSubscriptionName = "door_http_tester";
        private const string MqttSubscriptionName = "door_mqtt_tester";
        private const string HttpEndpoint = "http://localhost:9001/application-a/notify/";
        private const string MqttEndpoint = "mqtt://localhost:1883";
        private const string MqttTopic = "api/somiod/door/door";
        private const string MqttHost = "localhost";
        private const int MqttPort = 1883;

        string statusApp = "close";
        readonly RestClient restClient;
        MqttClient clientMqtt;
        public Form1()
        {
            InitializeComponent();
            restClient = new RestClient(baseURL);

            try
            {
                EnsureApplication();
                EnsureContainer();
                EnsureSubscription();
                EnsureFixedSubscriptions();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            clientMqtt = new MqttClient(MqttHost, MqttPort, false, null, null, MqttSslProtocols.None);

            try
            {
                clientMqtt.Connect($"ApplicationA_{Guid.NewGuid():N}");
                if (clientMqtt.IsConnected)
                {
                    clientMqtt.Subscribe(
                        new string[] { MqttTopic },
                        new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });

                    clientMqtt.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
                }
                else
                {
                    MessageBox.Show("Connection failed.", "Connection Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Error while connecting to the broker. Check the url and topic.",
                    "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        private void EnsureApplication()
        {
            var request = new RestRequest($"api/somiod/{ApplicationName}", Method.Get);
            request.AddHeader("Accept", "application/json");
            var response = restClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
                return;

            if (response.StatusCode != HttpStatusCode.NotFound)
                throw new InvalidOperationException($"Failed to check application: {(int)response.StatusCode} {response.Content}");

            var createRequest = new RestRequest("api/somiod", Method.Post);
            createRequest.AddJsonBody(new { resourceName = ApplicationName });
            var createResponse = restClient.Execute(createRequest);

            if (createResponse.StatusCode != HttpStatusCode.Created)
                throw new InvalidOperationException($"Failed to create application: {(int)createResponse.StatusCode} {createResponse.Content}");
        }

        private void EnsureContainer()
        {
            var request = new RestRequest($"api/somiod/{ApplicationName}/{ContainerName}", Method.Get);
            request.AddHeader("Accept", "application/json");
            var response = restClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
                return;

            if (response.StatusCode != HttpStatusCode.NotFound)
                throw new InvalidOperationException($"Failed to check container: {(int)response.StatusCode} {response.Content}");

            var createRequest = new RestRequest($"api/somiod/{ApplicationName}", Method.Post);
            createRequest.AddJsonBody(new { resourceName = ContainerName });
            var createResponse = restClient.Execute(createRequest);

            if (createResponse.StatusCode != HttpStatusCode.Created)
                throw new InvalidOperationException($"Failed to create container: {(int)createResponse.StatusCode} {createResponse.Content}");
        }

        private void EnsureSubscription()
        {
            EnsureSubscriptionInternal(SubscriptionName, 1, MqttEndpoint);
        }

        private void EnsureFixedSubscriptions()
        {
            EnsureSubscriptionInternal(HttpSubscriptionName, 1, HttpEndpoint);
            EnsureSubscriptionInternal(MqttSubscriptionName, 1, MqttEndpoint);
        }

        private void EnsureSubscriptionInternal(string subscriptionName, int evt, string endpoint)
        {
            var request = new RestRequest($"api/somiod/{ApplicationName}/{ContainerName}/subs/{subscriptionName}", Method.Get);
            request.AddHeader("Accept", "application/json");
            var response = restClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
                return;

            if (response.StatusCode != HttpStatusCode.NotFound)
                throw new InvalidOperationException($"Failed to check subscription {subscriptionName}: {(int)response.StatusCode} {response.Content}");

            var createRequest = new RestRequest($"api/somiod/{ApplicationName}/{ContainerName}/subs", Method.Post);
            var payload = new Dictionary<string, object>
            {
                ["resource-name"] = subscriptionName,
                ["evt"] = evt,
                ["endpoint"] = endpoint
            };
            createRequest.AddStringBody(JsonSerializer.Serialize(payload), DataFormat.Json);
            var createResponse = restClient.Execute(createRequest);

            if (createResponse.StatusCode != HttpStatusCode.Created)
                throw new InvalidOperationException($"Failed to create subscription {subscriptionName}: {(int)createResponse.StatusCode} {createResponse.Content}");
        }


        private void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            var payload = Encoding.UTF8.GetString(e.Message);
            var status = ExtractDoorStatus(payload);

            if (string.IsNullOrWhiteSpace(status))
                return;

            ChangeDoorByStatus(status);
        }

        private string ExtractDoorStatus(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return null;

            try
            {
                using (var document = JsonDocument.Parse(payload))
                {
                    if (document.RootElement.TryGetProperty("resource", out var resourceElement)
                        && resourceElement.TryGetProperty("content", out var contentElement))
                    {
                        var content = contentElement.GetString();
                        return string.IsNullOrWhiteSpace(content)
                            ? null
                            : content.Trim();
                    }
                }
            }
            catch (JsonException)
            {
                return null;
            }

            return null;
        }

        private void ChangeDoorByStatus(string status)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => ChangeDoorByStatus(status)));
                return;
            }

            if (string.Equals(status, "open", StringComparison.OrdinalIgnoreCase))
            {
                pictureBox1.Image = Properties.Resources.door_open;
                statusApp = "open";
            }
            else
            {
                pictureBox1.Image = Properties.Resources.door_close;
                statusApp = "close";
            }
        }
    }
}
