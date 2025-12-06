using System;
using System.Linq;
using System.Net;
using System.Web.Http;
using Api.Routing;
using SOMOID.Helpers;
using SOMOID.Validators;
using SOMOID.Models;

namespace SOMOID.Controllers
{
    /// <summary>
    /// Controlador para gerenciar Subscriptions no middleware SOMIOD.
    /// As subscriptions permitem notificações quando content-instances são criadas ou deletadas em um container.
    /// </summary>
    public class SubscriptionController : ApiController
    {
        private readonly SQLHelper SQLHelperInstance = new SQLHelper();

        #region GET Operations

        /// <summary>
        /// Obtém uma subscription específica pelo seu nome.
        /// </summary>
        /// <param name="appName">Nome do aplicativo</param>
        /// <param name="containerName">Nome do container</param>
        /// <param name="subName">Nome da subscription</param>
        /// <returns>Os dados da subscription solicitada</returns>
        /// <response code="200">Subscription encontrada</response>
        /// <response code="404">Subscription, container ou aplicativo não encontrado</response>
        /// <response code="500">Erro interno do servidor</response>
        [HttpGet]
        [GetRoute("api/somiod/{appName}/{containerName}/subs/{subName}")]
        public IHttpActionResult GetSubscriptionByName(
            string appName,
            string containerName,
            string subName
        )
        {
            try {
                var sub = SQLHelperInstance.GetSubscriptionByAppName(appName, containerName, subName);
                if (sub == null)
                    return NotFound();
                return Ok(sub);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        #endregion

        #region POST Operations (Create)

        /// <summary>
        /// Cria uma nova subscription em um container específico.
        /// As subscriptions permitem notificações quando content-instances são criadas ou deletadas.
        /// </summary>
        /// <param name="appName">Nome do aplicativo</param>
        /// <param name="containerName">Nome do container</param>
        /// <param name="value">Dados da subscription a criar (pode omitir resource-name para auto-gerar)</param>
        /// <returns>A subscription criada com todas as suas propriedades</returns>
        /// <response code="201">Subscription criada com sucesso</response>
        /// <response code="400">Dados inválidos (faltam campos obrigatórios, evt inválido, etc)</response>
        /// <response code="404">Aplicativo ou container não encontrado</response>
        /// <response code="409">Subscription com este nome já existe</response>
        /// <response code="500">Erro interno do servidor</response>
        /// <remarks>
        /// Corpo da requisição:
        /// {
        ///   "resourceName": "sub-notification-1",  // Opcional - auto-gerado se omitido
        ///   "evt": 1,                              // Obrigatório: 1 (criação), 2 (deletion) ou 3 (ambos)
        ///   "endpoint": "http://example.com:8080" // Obrigatório: URL HTTP ou MQTT endpoint
        /// }
        ///
        /// Exemplo de resposta (201 Created):
        /// {
        ///   "resourceName": "sub-notification-1",
        ///   "creationDatetime": "2025-12-02T19:47:00",
        ///   "containerResourceName": "cont-sensors",
        ///   "res-type": "subscription",
        ///   "evt": 1,
        ///   "endpoint": "http://example.com:8080/notify"
        /// }
        /// </remarks>
        [HttpPost]
        [PostRoute("api/somiod/{appName}/{containerName}/subs")]
        public IHttpActionResult CreateSubscription(
            string appName,
            string containerName,
            [FromBody] Subscription value
        )
        {
            var validator = new SubscriptionValidator();
            var errors = validator.Validate(value);

            if (errors.Any())
                return Content(HttpStatusCode.BadRequest, new { errors });


            // Configurar propriedades automáticas
            value.ResType = "subscription";
            value.ContainerResourceName = containerName;
            value.ApplicationResourceName = appName;
            value.CreationDatetime = DateTime.UtcNow;
   
            try
            {
                int containerCount = SQLHelperInstance.CheckIfSubscriptionParentExists(appName, containerName);
                if (containerCount == 0)
                    return NotFound();

                int subCount = SQLHelperInstance.CheckIfSubscriptionAlreadyExists(appName, containerName, value.ResourceName);
                if (subCount > 0)
                    return Conflict();

                int rowsAffected = SQLHelperInstance.InsertNewSubscription(
                    value.ResourceName,
                    value.CreationDatetime,
                    containerName,
                    value.ApplicationResourceName,
                    value.ResType,
                    value.Evt,
                    value.Endpoint
                );
                if (rowsAffected == 0)
                {
                    return InternalServerError(new Exception("Falha ao criar subscription."));
                }
                
                var responseValue = new
                {
                    resourceName = value.ResourceName,
                    creationDatetime = value.CreationDatetime.ToString(
                        "yyyy-MM-ddTHH:mm:ss"
                    ),
                    containerResourceName = value.ContainerResourceName,
                    resType = value.ResType,
                    evt = value.Evt,
                    endpoint = value.Endpoint,
                };

                string locationUrl =
                    $"/api/somiod/{appName}/{containerName}/subs/{value.ResourceName}";

                return Created(locationUrl, responseValue);
                

            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        #endregion

        #region DELETE Operations

        /// <summary>
        /// Deleta uma subscription específica.
        /// Quando uma subscription é deletada, nenhuma notificação será mais disparada para esse endpoint.
        /// </summary>
        /// <param name="appName">Nome do aplicativo</param>
        /// <param name="containerName">Nome do container</param>
        /// <param name="subName">Nome da subscription a deletar</param>
        /// <returns>Sem conteúdo se bem-sucedido</returns>
        /// <response code="200">Subscription deletada com sucesso</response>
        /// <response code="404">Subscription, container ou aplicativo não encontrado</response>
        /// <response code="500">Erro interno do servidor</response>
        [HttpDelete]
        [DeleteRoute("api/somiod/{appName}/{containerName}/subs/{subName}")]
        public IHttpActionResult DeleteSubscription(
            string appName,
            string containerName,
            string subName
        )
        {
            try
            {
                var sub = SQLHelperInstance.GetSubscriptionByAppName(appName, containerName, subName);
                if (sub == null)
                    return NotFound();

                var deleted = SQLHelperInstance.DeleteSubscription(appName, containerName, subName);
                if (deleted)
                    return Ok();
                return NotFound();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        #endregion

    }
}
