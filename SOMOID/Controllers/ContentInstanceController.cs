using System;
using System.Collections.Generic;
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
    public class ContentInstanceController : ApiController
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private readonly SQLHelper sqlHelper = new SQLHelper();

        private enum SubscriptionEventType
        {
            Creation = 1,
            Deletion = 2,
        }

        #region GET Operations

        /// <summary>
        /// Obtém uma content-instance específica num container.
        /// </summary>
        /// <param name="appName">Nome da application</param>
        /// <param name="containerName">Nome do container</param>
        /// <param name="ciName">Nome da content-instance</param>
        /// <returns>Dados completos da content-instance</returns>
        /// <response code="200">Encontrada</response>
        /// <response code="404">Application, container ou content-instance não encontrados</response>
        /// <response code="500">Erro interno</response>
        [HttpGet]
        [GetRoute("api/somiod/{appName}/{containerName}/{ciName}")]
        public IHttpActionResult GetContentInstance(
            string appName,
            string containerName,
            string ciName
        )
        {
            try
            {
                var ci = sqlHelper.GetContentInstance(appName, containerName, ciName);
                if (ci == null)
                    return NotFound();
                return Ok(ci);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        #endregion

        #region POST Operations (Create)

        /// <summary>
        /// Cria uma nova content-instance num container.
        /// </summary>
        /// <param name="appName">Nome da application</param>
        /// <param name="containerName">Nome do container</param>
        /// <param name="value">Dados da content-instance (resourceName opcional)</param>
        /// <returns>Content-instance criada com todas as propriedades</returns>
        /// <response code="201">Criada com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="404">Application ou container não encontrado</response>
        /// <response code="409">Já existe content-instance com este nome neste container</response>
        /// <response code="500">Erro interno</response>
        /// <remarks>
        /// POST /api/somiod/app1/cont1
        /// Body:
        /// {
        ///   "resourceName": "ci1",         // opcional
        ///   "contentType": "application/json",
        ///   "content": "{\"temp\": 25}"
        /// }
        /// </remarks>
        [HttpPost]
        [PostRoute(
            "api/somiod/{appName:regex(^[^/]+$):applicationexists}/{containerName:regex(^[^/]+$):containerexists}"
        )]
        public async Task<IHttpActionResult> CreateContentInstance(
            string appName,
            string containerName,
            [FromBody] ContentInstance value
        )
        {
            var validator = new CreateContentInstanceValidator();
            var errors = validator.Validate(value);
            if (errors.Any())
            {
                return Content(HttpStatusCode.BadRequest, new { errors });
            }

            value.ResType = "content-instance";
            value.ContainerResourceName = containerName;
            value.ApplicationResourceName = appName;
            value.CreationDatetime = DateTime.UtcNow;

            try
            {
                if (!sqlHelper.ContentInstanceParentExists(appName, containerName))
                    return NotFound();

                if (sqlHelper.ContentInstanceExistsInContainer(appName, containerName, value.ResourceName))
                    return Conflict();

                var created = sqlHelper.InsertContentInstance(value);
                if (created)
                {
                    var responseValue = new
                    {
                        resourceName = value.ResourceName,
                        creationDatetime = value.CreationDatetime.ToString(
                            "yyyy-MM-ddTHH:mm:ss"
                        ),
                        containerResourceName = value.ContainerResourceName,
                        resType = value.ResType,
                        contentType = value.ContentType,
                        content = value.Content,
                    };

                    string locationUrl =
                        $"/api/somiod/{appName}/{containerName}/{value.ResourceName}";
                    await NotifySubscriptionsAsync(
                        appName,
                        containerName,
                        value,
                        SubscriptionEventType.Creation
                    );
                    return Created(locationUrl, responseValue);
                }

                return InternalServerError(new Exception("Falha ao criar content-instance."));
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        #endregion

        #region DELETE Operations

        /// <summary>
        /// Elimina uma content-instance específica de um container.
        /// </summary>
        /// <param name="appName">Nome da application</param>
        /// <param name="containerName">Nome do container</param>
        /// <param name="ciName">Nome da content-instance</param>
        /// <returns>200 OK ou 404 NotFound</returns>
        [HttpDelete]
        [DeleteRoute("api/somiod/{appName}/{containerName}/{ciName}")]
        public async Task<IHttpActionResult> DeleteContentInstance(
            string appName,
            string containerName,
            string ciName
        )
        {
            try
            {
                var existing = sqlHelper.GetContentInstance(appName, containerName, ciName);
                if (existing == null)
                    return NotFound();

                var deleted = sqlHelper.DeleteContentInstance(appName, containerName, ciName);
                if (deleted)
                {
                    await NotifySubscriptionsAsync(
                        appName,
                        containerName,
                        existing,
                        SubscriptionEventType.Deletion
                    );
                    return Ok();
                }
                return NotFound();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        #endregion

        private async Task NotifySubscriptionsAsync(
            string appName,
            string containerName,
            ContentInstance contentInstance,
            SubscriptionEventType eventType
        )
        {
            if (contentInstance == null)
                return;

            var subscriptions = sqlHelper.GetSubscriptionsForContainer(appName, containerName);
            if (subscriptions == null || subscriptions.Count == 0)
                return;

            int eventCode = (int)eventType;

            // Filter subscriptions that match the event type
            var relevantSubscriptions = subscriptions
                .Where(sub => sub.Evt == 3 || sub.Evt == eventCode)
                .Where(sub => !string.IsNullOrWhiteSpace(sub.Endpoint))
                .ToList();

            if (relevantSubscriptions.Count == 0)
                return;

            // Separate HTTP and MQTT subscriptions
            var httpSubscriptions = relevantSubscriptions
                .Where(sub =>
                    sub.Endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    || sub.Endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                )
                .ToList();

            var mqttSubscriptions = relevantSubscriptions
                .Where(sub => sub.Endpoint.StartsWith("mqtt://", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Prepare payload
            var payload = new
            {
                eventType = eventType == SubscriptionEventType.Creation ? "creation" : "deletion",
                eventCode,
                subscription = new
                {
                    resourceName = "",
                    evt = 0,
                    endpoint = "",
                },
                resource = new
                {
                    resourceName = contentInstance.ResourceName,
                    creationDatetime = contentInstance.CreationDatetime.ToString("yyyy-MM-ddTHH:mm:ss"),
                    resType = contentInstance.ResType,
                    containerResourceName = contentInstance.ContainerResourceName,
                    applicationResourceName = appName,
                    contentType = contentInstance.ContentType,
                    content = contentInstance.Content,
                    path = $"/api/somiod/{appName}/{containerName}/{contentInstance.ResourceName}",
                },
                triggeredAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
            };

            var tasks = new List<Task>();

            // Send HTTP notifications
            foreach (var subscription in httpSubscriptions)
            {
                var httpPayload = new
                {
                    eventType = payload.eventType,
                    eventCode = payload.eventCode,
                    subscription = new
                    {
                        resourceName = subscription.ResourceName,
                        evt = subscription.Evt,
                        endpoint = subscription.Endpoint,
                    },
                    resource = payload.resource,
                    triggeredAt = payload.triggeredAt,
                };

                tasks.Add(SendHttpNotificationAsync(subscription.Endpoint, httpPayload));
            }

            // Send MQTT notifications
            foreach (var subscription in mqttSubscriptions)
            {
                var mqttPayload = new
                {
                    eventType = payload.eventType,
                    eventCode = payload.eventCode,
                    subscription = new
                    {
                        resourceName = subscription.ResourceName,
                        evt = subscription.Evt,
                        endpoint = subscription.Endpoint,
                    },
                    resource = payload.resource,
                    triggeredAt = payload.triggeredAt,
                };

                // MQTT topic format: api/somiod/{appName}/{containerName}
                var mqttTopic = $"api/somiod/{appName}/{containerName}";
                tasks.Add(Task.Run(() => SendMqttNotification(
                    subscription.Endpoint,
                    mqttTopic,
                    mqttPayload
                )));
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }
        }

        private async Task SendHttpNotificationAsync(string endpoint, object payload)
        {
            try
            {
                var body = JsonConvert.SerializeObject(payload);
                using (var content = new StringContent(body, Encoding.UTF8, "application/json"))
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                {
                    var response = await HttpClient.PostAsync(endpoint, content, cts.Token);
                    if (!response.IsSuccessStatusCode)
                    {
                        Debug.WriteLine(
                            $"HTTP notification to {endpoint} failed with status {(int)response.StatusCode}"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"HTTP notification to {endpoint} failed: {ex.Message}");
            }
        }

        private void SendMqttNotification(string brokerEndpoint, string topic, object payload)
        {
            try
            {
                var jsonPayload = JsonConvert.SerializeObject(payload);
                var success = MqttHelper.PublishNotification(brokerEndpoint, topic, jsonPayload);
                
                if (!success)
                {
                    Debug.WriteLine($"MQTT notification to {brokerEndpoint} on topic '{topic}' failed");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MQTT notification to {brokerEndpoint} failed: {ex.Message}");
            }
        }
    }
}
