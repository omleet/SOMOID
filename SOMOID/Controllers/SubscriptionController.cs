using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
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
        private SQLHelper SQLHelperInstance = new SQLHelper();
        string connection = Properties.Settings.Default.ConnectionStr;

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
            value.CreationDatetime = DateTime.UtcNow;

            // SQL Queries
           
            

           

            SqlConnection conn = new SqlConnection(connection);

            try
            {
                int containerCount = SQLHelperInstance.CheckIfSubscriptionParentExists(appName, containerName);
                if (containerCount == 0)
                    return NotFound();

                int subCount = SQLHelperInstance.CheckIfSubscriptionAlreadyExists(value.ResourceName, containerName);
                if (subCount > 0)
                    return Conflict();

                int rowsAffected = SQLHelperInstance.InsertNewSubscription(value.ResourceName, value.CreationDatetime, containerName, value.ResType, value.Evt, value.Endpoint);
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
            var conn = new SqlConnection(connection);

            // Primeiro fazer GET para verificar se existe
            string getQuery =
                @"
                SELECT COUNT(*)
                FROM [subscription] s
                JOIN [container] c ON c.[resource-name] = s.[container-resource-name]
                JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                WHERE a.[resource-name] = @appName
                AND c.[resource-name] = @containerName
                AND s.[resource-name] = @subName";

            string deleteQuery =
                @"
                DELETE s
                FROM [subscription] s
                JOIN [container] c ON c.[resource-name] = s.[container-resource-name]
                JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                WHERE a.[resource-name] = @appName
                AND c.[resource-name] = @containerName
                AND s.[resource-name] = @subName";

            try
            {
                using (conn)
                {
                    conn.Open();

                    // Verificar se existe
                    var cmdGet = new SqlCommand(getQuery, conn);
                    cmdGet.Parameters.AddWithValue("@appName", appName);
                    cmdGet.Parameters.AddWithValue("@containerName", containerName);
                    cmdGet.Parameters.AddWithValue("@subName", subName);

                    int existsCount = (int)cmdGet.ExecuteScalar();
                    if (existsCount == 0)
                        return NotFound();

                    // Deletar
                    var cmdDelete = new SqlCommand(deleteQuery, conn);
                    cmdDelete.Parameters.AddWithValue("@appName", appName);
                    cmdDelete.Parameters.AddWithValue("@containerName", containerName);
                    cmdDelete.Parameters.AddWithValue("@subName", subName);

                    int rowsAffected = cmdDelete.ExecuteNonQuery();

                    if (rowsAffected > 0)
                        return Ok();
                    else
                        return NotFound();
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Valida se uma string é um endpoint válido (HTTP ou MQTT).
        /// </summary>
        /// <param name="endpoint">String a validar</param>
        /// <returns>true se é um endpoint válido, false caso contrário</returns>
        private bool IsValidEndpoint(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
                return false;

            try
            {
                // Verificar se começa com http://, https:// ou mqtt://
                if (
                    endpoint.StartsWith("http://")
                    || endpoint.StartsWith("https://")
                    || endpoint.StartsWith("mqtt://")
                )
                {
                    // Tentar fazer parse como URI
                    var uri = new Uri(endpoint);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
