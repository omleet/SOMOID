using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Validation.Validators;
using Api.Routing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SOMOID.Helpers;
using SOMOID.Models;
using SOMOID.Validators;

namespace SOMOID.Controllers
{
    /// <summary>
    /// Controlador para gerir Content-Instances no middleware SOMIOD.
    /// Uma content-instance representa um registo de dados criado num container.
    /// </summary>
    // [RoutePrefix("api/somiod")]
    /// <summary>
    /// Controlador para gerir Content-Instances no middleware SOMIOD.
    /// Uma content-instance representa um registo de dados criado num container.
    /// </summary>
    public class ContentInstanceController : System.Web.Http.ApiController
    {
        private static readonly System.Net.Http.HttpClient HttpClient = new System.Net.Http.HttpClient();
        private const string NotificationDateFormat = "yyyy-MM-ddTHH:mm:ss";
        private readonly SOMOID.Helpers.SQLHelper sqlHelper = new SOMOID.Helpers.SQLHelper();

        private enum SubscriptionEventType
        {
            Creation = 1,
            Deletion = 2,
        }

        #region GET Operations

        [HttpGet]
        [GetRoute("api/somiod/{appName}/{containerName}/{ciName}")]
        public System.Web.Http.IHttpActionResult GetContentInstance(
            string appName,
            string containerName,
            string ciName
        )
        {
            try
            {
                SOMOID.Models.ContentInstance ci = sqlHelper.GetContentInstance(appName, containerName, ciName);
                if (ci == null)
                    return NotFound();
                return Ok(ci);
            }
            catch (System.Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        #endregion

        #region POST Operations (Create)

        [HttpPost]
        [PostRoute("api/somiod/{appName}/{containerName}")]
        public System.Web.Http.IHttpActionResult CreateResource(
    string appName,
    string containerName,
    [FromBody] Newtonsoft.Json.Linq.JObject body)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] -> {DateTime.UtcNow} BODY: {body?.ToString()}");

            if (body == null)
                return BadRequest("Invalid or missing JSON body.");

            string resType = body["res-type"]?.ToString()?.ToLower();
            if (string.IsNullOrEmpty(resType))
                return BadRequest("Missing 'res-type' field.");

            switch (resType)
            {
                case "content-instance":
                    // Convert JSON to ContentInstance
                    SOMOID.Models.ContentInstance ci = body.ToObject<SOMOID.Models.ContentInstance>();

                    var service = new ContentInstanceSubscriptionPostController();
                    OperationResult result = service.CreateContentInstance(appName, containerName, ci);

                    if (!result.Success)
                    {
                        if (result.Error == "Parent container not found") return NotFound();
                        if (result.Error == "Content instance already exists") return Conflict();
                        return InternalServerError(new Exception(result.Error));
                    }

                    string locationUrl = $"/api/somiod/{appName}/{containerName}/{ci.ResourceName}";
                    var responseValue = new
                    {
                        resourceName = ci.ResourceName,
                        creationDatetime = ci.CreationDatetime.ToString("yyyy-MM-ddTHH:mm:ss"),
                        containerResourceName = ci.ContainerResourceName,
                        resType = ci.ResType,
                        contentType = ci.ContentType,
                        content = ci.Content,
                    };

                    return Created(locationUrl, responseValue);

                case "subscription":
                    // Convert JSON to Subscription
                    SOMOID.Models.Subscription sub = body.ToObject<SOMOID.Models.Subscription>();

                    // If you want, you can refactor Subscription creation through ResourceService too
                    var subService = new ContentInstanceSubscriptionPostController();
                    OperationResult subResult = subService.CreateSubscription(appName, containerName, sub);

                    if (subResult.ValidationErrors != null)
                        return Content(System.Net.HttpStatusCode.BadRequest, new { errors = subResult.ValidationErrors });

                    if (!subResult.Success)
                    {
                        if (subResult.Error == "Parent container not found") return NotFound();
                        if (subResult.Error == "Subscription already exists") return Conflict();
                        return InternalServerError(new Exception(subResult.Error));
                    }

                    string subLocationUrl = $"/api/somiod/{appName}/{containerName}/subs/{sub.ResourceName}";
                    var subResponseValue = new
                    {
                        resourceName = sub.ResourceName,
                        creationDatetime = sub.CreationDatetime.ToString("yyyy-MM-ddTHH:mm:ss"),
                        containerResourceName = sub.ContainerResourceName,
                        resType = sub.ResType,
                        evt = sub.Evt,
                        endpoint = sub.Endpoint,
                    };

                    return Created(subLocationUrl, subResponseValue);

                default:
                    return BadRequest("Invalid res-type. Expected 'content-instance' or 'subscription'.");
            }
        }


        public System.Web.Http.IHttpActionResult CreateContentInstance(
            string appName,
            string containerName,
            [System.Web.Http.FromBody] SOMOID.Models.ContentInstance value
        )
        {
            value.ContainerResourceName = containerName;
            value.ApplicationResourceName = appName;
            value.CreationDatetime = System.DateTime.UtcNow;
            System.Diagnostics.Debug.WriteLine($"[DEBUG value] -> {DateTime.UtcNow} BODY: {value?.ToString()}");


            try
            {
                if (!sqlHelper.ContentInstanceParentExists(appName, containerName))
                    return NotFound();

                if (sqlHelper.ContentInstanceExistsInContainer(appName, containerName, value.ResourceName))
                    return Conflict();

                bool created = sqlHelper.InsertContentInstance(value);
                if (!created)
                    return InternalServerError(new System.Exception("Falha ao criar content-instance."));

                // Send notifications asynchronously without blocking response
                _ = NotifySubscriptionsAsync(appName, containerName, value, SubscriptionEventType.Creation);

                var responseValue = new
                {
                    resourceName = value.ResourceName,
                    creationDatetime = value.CreationDatetime.ToString("yyyy-MM-ddTHH:mm:ss"),
                    containerResourceName = value.ContainerResourceName,
                    resType = value.ResType,
                    contentType = value.ContentType,
                    content = value.Content,
                };

                string locationUrl = $"/api/somiod/{appName}/{containerName}/{value.ResourceName}";
                return Created(locationUrl, responseValue);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] -> {System.DateTime.UtcNow} SQL error when creating content-instance");
                return InternalServerError(ex);
            }
        }

        #endregion

        #region DELETE Operations

        [System.Web.Http.HttpDelete]
        [Api.Routing.DeleteRoute("api/somiod/{appName}/{containerName}/{ciName}")]
        public async System.Threading.Tasks.Task<System.Web.Http.IHttpActionResult> DeleteContentInstance(
            string appName,
            string containerName,
            string ciName
        )
        {
            try
            {
                SOMOID.Models.ContentInstance existing = sqlHelper.GetContentInstance(appName, containerName, ciName);
                if (existing == null)
                    return NotFound();

                bool deleted = sqlHelper.DeleteContentInstance(appName, containerName, ciName);
                if (!deleted)
                    return NotFound();

                await NotifySubscriptionsAsync(appName, containerName, existing, SubscriptionEventType.Deletion);
                return Ok();
            }
            catch (System.Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        #endregion

        private async System.Threading.Tasks.Task NotifySubscriptionsAsync(
            string appName,
            string containerName,
            SOMOID.Models.ContentInstance contentInstance,
            SubscriptionEventType eventType
        )
        {
            if (contentInstance == null)
                return;

            System.Collections.Generic.List<SOMOID.Models.Subscription> subscriptions =
                sqlHelper.GetSubscriptionsForContainer(appName, containerName);
            if (subscriptions == null || subscriptions.Count == 0)
                return;

            int eventCode = (int)eventType;
            string eventTypeName = eventType == SubscriptionEventType.Creation ? "creation" : "deletion";
            string triggeredAt = System.DateTime.UtcNow.ToString(NotificationDateFormat);

            var relevantSubscriptions = subscriptions
                .FindAll(sub => sub.Evt == 3 || sub.Evt == eventCode)
                .FindAll(sub => !string.IsNullOrWhiteSpace(sub.Endpoint));

            if (relevantSubscriptions.Count == 0)
                return;

            var httpSubscriptions = relevantSubscriptions
                .FindAll(sub =>
                    sub.Endpoint.StartsWith("http://", System.StringComparison.OrdinalIgnoreCase)
                    || sub.Endpoint.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase)
                );

            var mqttSubscriptions = relevantSubscriptions
                .FindAll(sub => sub.Endpoint.StartsWith("mqtt://", System.StringComparison.OrdinalIgnoreCase));

            SOMOID.Models.NotificationResourceInfo resourceInfo =
                BuildResourceInfo(appName, containerName, contentInstance);

            string mqttTopic = $"api/somiod/{appName}/{containerName}";
            var tasks = new System.Collections.Generic.List<System.Threading.Tasks.Task>();

            foreach (SOMOID.Models.Subscription subscription in httpSubscriptions)
            {
                SOMOID.Models.NotificationPayload payload = CreateNotificationPayload(subscription, eventTypeName, eventCode, resourceInfo, triggeredAt);
                PersistNotificationPayload(payload, appName);
                tasks.Add(SendHttpNotificationAsync(subscription.Endpoint, payload));
            }

            foreach (SOMOID.Models.Subscription subscription in mqttSubscriptions)
            {
                SOMOID.Models.NotificationPayload payload = CreateNotificationPayload(subscription, eventTypeName, eventCode, resourceInfo, triggeredAt);
                PersistNotificationPayload(payload, appName);
                tasks.Add(System.Threading.Tasks.Task.Run(() => SendMqttNotification(subscription.Endpoint, mqttTopic, payload)));
            }

            if (tasks.Count > 0)
                await System.Threading.Tasks.Task.WhenAll(tasks);
        }

        private static SOMOID.Models.NotificationPayload CreateNotificationPayload(
            SOMOID.Models.Subscription subscription,
            string eventTypeName,
            int eventCode,
            SOMOID.Models.NotificationResourceInfo resourceInfo,
            string triggeredAt
        )
        {
            return new SOMOID.Models.NotificationPayload
            {
                EventType = eventTypeName,
                EventCode = eventCode,
                Subscription = new SOMOID.Models.NotificationSubscriptionInfo
                {
                    ResourceName = subscription.ResourceName,
                    Evt = subscription.Evt,
                    Endpoint = subscription.Endpoint,
                },
                Resource = resourceInfo,
                TriggeredAt = triggeredAt,
            };
        }

        private SOMOID.Models.NotificationResourceInfo BuildResourceInfo(
            string appName,
            string containerName,
            SOMOID.Models.ContentInstance contentInstance
        )
        {
            return new SOMOID.Models.NotificationResourceInfo
            {
                ResourceName = contentInstance.ResourceName,
                CreationDatetime = contentInstance.CreationDatetime.ToString(NotificationDateFormat),
                ResType = contentInstance.ResType,
                ContainerResourceName = contentInstance.ContainerResourceName,
                ApplicationResourceName = appName,
                ContentType = contentInstance.ContentType,
                Content = contentInstance.Content,
                Path = $"/api/somiod/{appName}/{containerName}/{contentInstance.ResourceName}",
            };
        }

        private void PersistNotificationPayload(SOMOID.Models.NotificationPayload payload, string appName)
        {
            try
            {
                SOMOID.Helpers.NotificationXmlHelper.SerializeAndSave(payload, appName);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to persist notification XML for {appName}: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task SendHttpNotificationAsync(string endpoint, SOMOID.Models.NotificationPayload payload)
        {
            try
            {
                string body = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
                using (var content = new System.Net.Http.StringContent(body, System.Text.Encoding.UTF8, "application/json"))
                using (var cts = new System.Threading.CancellationTokenSource(System.TimeSpan.FromSeconds(5)))
                {
                    System.Net.Http.HttpResponseMessage response = await HttpClient.PostAsync(endpoint, content, cts.Token);
                    if (!response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"HTTP notification to {endpoint} failed with status {(int)response.StatusCode}"
                        );
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HTTP notification to {endpoint} failed: {ex.Message}");
            }
        }

        private void SendMqttNotification(string brokerEndpoint, string topic, SOMOID.Models.NotificationPayload payload)
        {
            try
            {
                string jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
                bool success = SOMOID.Helpers.MqttHelper.PublishNotification(brokerEndpoint, topic, jsonPayload);

                if (!success)
                {
                    System.Diagnostics.Debug.WriteLine($"MQTT notification to {brokerEndpoint} on topic '{topic}' failed");
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MQTT notification to {brokerEndpoint} failed: {ex.Message}");
            }
        }
    }
}
