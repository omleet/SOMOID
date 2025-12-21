using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace Application_B
{
    public partial class Form1 : Form
    {
        private const string BaseUri = @"http://localhost:10654";
        private const string ApplicationName = "door";
        private const string ContainerName = "door";
        private const string HttpSubscriptionName = "door_http_tester";
        private const string MqttSubscriptionName = "door_mqtt_tester";
        private const string HttpEndpoint = "http://localhost:9001/application-b/notify/";
        private const string MqttEndpoint = "mqtt://localhost:1883";
        private const string MqttHost = "localhost";
        private const int MqttPort = 1883;
        private const string MqttTopic = "api/somiod/door/door";
        private const string DoorContentResourceName = "door_current_state";
        readonly RestClient client;
        private string currentDoorStatus = "close";
        private HttpListener httpListener;
        private CancellationTokenSource httpListenerToken;
        private Task httpListenerWorker;
        private MqttClient mqttClient;

        public Form1()
        {
            InitializeComponent();

            client = new RestClient(BaseUri);

            try
            {
                EnsureApplication();
                EnsureContainer();
                InitializeNotificationTesting();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Initialization error: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        private void door_open_Click(object sender, EventArgs e)
        {
            if (IsCurrentStatus("open"))
            {
                MessageBox.Show(
                    "The door is already opened",
                    "Door status",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (SendDoorCommand("open"))
            {
                SetDoorStatus("open");
            }
        }

        private void door_close_Click(object sender, EventArgs e)
        {
            if (IsCurrentStatus("close"))
            {
                MessageBox.Show(
                    "The door is already closed",
                    "Door status",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (SendDoorCommand("close"))
            {
                SetDoorStatus("close");
            }
        }

        private bool SendDoorCommand(string status)
        {
            EnsureSingleDoorContentInstance();

            var request = new RestRequest($"api/somiod/{ApplicationName}/{ContainerName}", Method.Post);

            var payload = new Dictionary<string, object>
            {
                ["resource-name"] = DoorContentResourceName,
                ["content-type"] = "text/plain",
                ["res-type"] = "content-instance",
                ["content"] = status
            };

            request.AddStringBody(JsonSerializer.Serialize(payload), DataFormat.Json);

            try
            {
                var response = client.Execute(request);
                if (response.StatusCode == HttpStatusCode.Created)
                {
                    return true;
                }

                MessageBox.Show(
                    $"Error: {response.StatusCode} - {response.Content}",
                    "Request Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error: {ex.Message}",
                    "Request Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            return false;
        }

        private bool IsCurrentStatus(string status)
        {
            return !string.IsNullOrEmpty(status)
                && string.Equals(currentDoorStatus, status, StringComparison.OrdinalIgnoreCase);
        }

        private void SetDoorStatus(string status)
        {
            if (string.Equals(status, "open", StringComparison.OrdinalIgnoreCase))
            {
                currentDoorStatus = "open";
            }
            else if (string.Equals(status, "close", StringComparison.OrdinalIgnoreCase))
            {
                currentDoorStatus = "close";
            }
        }

        private void InitializeNotificationTesting()
        {
            var httpStarted = StartHttpListener();
            if (httpStarted)
            {
                EnsureSubscription(HttpSubscriptionName, 1, HttpEndpoint);
            }

            EnsureSubscription(MqttSubscriptionName, 1, MqttEndpoint);
            ConnectToMqttBroker();
        }

        private bool StartHttpListener()
        {
            if (httpListener != null)
                return true;

            try
            {
                httpListener = new HttpListener();
                httpListener.Prefixes.Add(HttpEndpoint);
                httpListener.IgnoreWriteExceptions = true;
                httpListener.Start();
            }
            catch (HttpListenerException ex)
            {
                httpListener = null;
                MessageBox.Show(
                    "Failed to start HTTP listener. Run as administrator or reserve the URL prefix (netsh).\n" + ex.Message,
                    "HTTP Listener",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                AppendNotification("HTTP", ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                httpListener = null;
                MessageBox.Show(
                    "Failed to start HTTP listener." + Environment.NewLine + ex.Message,
                    "HTTP Listener",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                AppendNotification("HTTP", ex.Message);
                return false;
            }

            httpListenerToken = new CancellationTokenSource();
            httpListenerWorker = Task.Run(() => ListenHttpAsync(httpListenerToken.Token));
            AppendNotification("HTTP", "Listener started");
            return true;
        }

        private async Task ListenHttpAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                HttpListenerContext context = null;
                try
                {
                    context = await httpListener.GetContextAsync().ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (HttpListenerException)
                {
                    if (token.IsCancellationRequested)
                        break;
                    continue;
                }

                if (context == null)
                    continue;

                string body = string.Empty;
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding ?? Encoding.UTF8))
                {
                    body = await reader.ReadToEndAsync().ConfigureAwait(false);
                }

                try
                {
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.Close();
                }
                catch
                {
                }

                var status = ExtractDoorStatus(body);
                AppendNotification("HTTP", status ?? body);
            }
        }

        private void StopHttpListener()
        {
            try
            {
                httpListenerToken?.Cancel();
            }
            catch
            {
            }

            try
            {
                httpListener?.Stop();
                httpListener?.Close();
            }
            catch
            {
            }

            httpListener = null;
            httpListenerWorker = null;
            httpListenerToken = null;
        }

        private void ConnectToMqttBroker()
        {
            try
            {
                mqttClient = new MqttClient(MqttHost, MqttPort, false, null, null, MqttSslProtocols.None);
                mqttClient.MqttMsgPublishReceived += OnMqttMsgPublishReceived;
                mqttClient.Connect($"ApplicationB_{Guid.NewGuid():N}");

                if (mqttClient.IsConnected)
                {
                    mqttClient.Subscribe(new[] { MqttTopic }, new[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
                }
                else
                {
                    throw new InvalidOperationException("MQTT connection failed");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("MQTT initialization error: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DisconnectMqtt()
        {
            try
            {
                if (mqttClient != null)
                {
                    mqttClient.MqttMsgPublishReceived -= OnMqttMsgPublishReceived;
                    if (mqttClient.IsConnected)
                    {
                        mqttClient.Disconnect();
                    }
                    mqttClient = null;
                }
            }
            catch
            {
            }
        }

        private void OnMqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            var payload = Encoding.UTF8.GetString(e.Message);
            var status = ExtractDoorStatus(payload);
            AppendNotification("MQTT", status ?? payload);
        }

        private void EnsureSubscription(string subscriptionName, int evt, string endpoint)
        {
            var fetchRequest = new RestRequest($"api/somiod/{ApplicationName}/{ContainerName}/subs/{subscriptionName}", Method.Get);
            fetchRequest.AddHeader("Accept", "application/json");
            var fetchResponse = client.Execute(fetchRequest);

            if (fetchResponse.StatusCode == HttpStatusCode.OK)
                return;

            if (fetchResponse.StatusCode != HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException($"Failed to check subscription {subscriptionName}: {(int)fetchResponse.StatusCode} {fetchResponse.Content}");
            }

            var createRequest = new RestRequest($"api/somiod/{ApplicationName}/{ContainerName}/subs", Method.Post);
            var payload = new Dictionary<string, object>
            {
                ["resource-name"] = subscriptionName,
                ["evt"] = evt,
                ["endpoint"] = endpoint
            };
            createRequest.AddStringBody(JsonSerializer.Serialize(payload), DataFormat.Json);
            var createResponse = client.Execute(createRequest);

            if (createResponse.StatusCode != HttpStatusCode.Created)
            {
                throw new InvalidOperationException($"Failed to create subscription {subscriptionName}: {(int)createResponse.StatusCode} {createResponse.Content}");
            }
        }

        private void AppendNotification(string channel, string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => AppendNotification(channel, message)));
                return;
            }

            var content = string.IsNullOrWhiteSpace(message) ? "<empty>" : message;
            var entry = $"{DateTime.Now:HH:mm:ss} [{channel}] {content}";

            notificationList.Items.Insert(0, entry);

            const int maxItems = 100;
            while (notificationList.Items.Count > maxItems)
            {
                notificationList.Items.RemoveAt(notificationList.Items.Count - 1);
            }
        }

        private string ExtractDoorStatus(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return null;

            try
            {
                using (var document = JsonDocument.Parse(payload))
                {
                    if (document.RootElement.TryGetProperty("resource", out var resource)
                        && resource.TryGetProperty("content", out var content))
                    {
                        var value = content.GetString();
                        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
                    }
                }
            }
            catch (JsonException)
            {
                return null;
            }

            return null;
        }

        private void EnsureSingleDoorContentInstance()
        {
            var deleteRequest = new RestRequest($"api/somiod/{ApplicationName}/{ContainerName}/{DoorContentResourceName}", Method.Delete);
            deleteRequest.AddHeader("Accept", "application/json");

            try
            {
                var response = client.Execute(deleteRequest);
                if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound)
                    return;
            }
            catch
            {
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            StopHttpListener();
            DisconnectMqtt();
        }


        private void EnsureApplication()
        {
            var request = new RestRequest($"api/somiod/{ApplicationName}", Method.Get);
            request.AddHeader("Accept", "application/json");
            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
                return;

            if (response.StatusCode != HttpStatusCode.NotFound)
                throw new InvalidOperationException($"Failed to check application: {(int)response.StatusCode} {response.Content}");

            var createRequest = new RestRequest("api/somiod", Method.Post);
            createRequest.AddJsonBody(new ApplicationRequest { ResourceName = ApplicationName });
            var createResponse = client.Execute(createRequest);

            if (createResponse.StatusCode != HttpStatusCode.Created)
                throw new InvalidOperationException($"Failed to create application: {(int)createResponse.StatusCode} {createResponse.Content}");
        }

        private void EnsureContainer()
        {
            var request = new RestRequest($"api/somiod/{ApplicationName}/{ContainerName}", Method.Get);
            request.AddHeader("Accept", "application/json");
            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
                return;

            if (response.StatusCode != HttpStatusCode.NotFound)
                throw new InvalidOperationException($"Failed to check container: {(int)response.StatusCode} {response.Content}");

            var createRequest = new RestRequest($"api/somiod/{ApplicationName}", Method.Post);
            createRequest.AddJsonBody(new ContainerRequest { ResourceName = ContainerName });
            var createResponse = client.Execute(createRequest);

            if (createResponse.StatusCode != HttpStatusCode.Created)
                throw new InvalidOperationException($"Failed to create container: {(int)createResponse.StatusCode} {createResponse.Content}");
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
