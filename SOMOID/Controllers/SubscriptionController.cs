using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Api.Routing;
using SOMOID.Models;

namespace SOMOID.Controllers
{
    /// <summary>
    /// Controlador para gerenciar Subscriptions no middleware SOMIOD.
    /// As subscriptions permitem notificações quando content-instances são criadas ou deletadas em um container.
    /// </summary>
    [RoutePrefix("api/somiod/sub")]
    public class SubscriptionController : ApiController
    {
        string connection = Properties.Settings.Default.ConnectionStr;

        #region Discovery Operation

        /// <summary>
        /// Descobre subscriptions em um container específico.
        /// Deve incluir o header "somiod-discovery: subscription" para ativar a operação de discovery.
        /// </summary>
        /// <param name="appName">Nome do aplicativo</param>
        /// <param name="containerName">Nome do container</param>
        /// <returns>Lista de caminhos para todas as subscriptions encontradas</returns>
        /// <remarks>
        /// Exemplos de retorno:
        /// ["/api/somiod/app-x/cont-y/subs/sub-1", "/api/somiod/app-x/cont-y/subs/sub-2"]
        /// </remarks>
        //[HttpGet]
        //[Route("{appName}/{containerName}")]
        ////[GetRoute("{appName}/{containerName}")]
        //public IHttpActionResult DiscoverSubscriptions(string appName, string containerName)
        //{
        //    // Verificar se o header somiod-discovery está presente
        //    IEnumerable<string> headerValues;
        //    if (
        //        !Request.Headers.TryGetValues("somiod-discovery", out headerValues)
        //        || !headerValues.Any(h => h == "subscription")
        //    )
        //    {
        //        return NotFound();
        //    }

        //    var subscriptionPaths = new List<string>();
        //    var conn = new SqlConnection(connection);

        //    string query =
        //        @"
        //        SELECT s.[resource-name]
        //        FROM [subscription] s
        //        JOIN [container] c ON c.[resource-name] = s.[container-resource-name]
        //        JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
        //        WHERE a.[resource-name] = @appName
        //        AND c.[resource-name] = @containerName
        //        ORDER BY s.[creation-datetime]";

        //    var cmd = new SqlCommand(query, conn);
        //    cmd.Parameters.AddWithValue("@appName", appName);
        //    cmd.Parameters.AddWithValue("@containerName", containerName);

        //    try
        //    {
        //        using (conn)
        //        {
        //            conn.Open();
        //            var reader = cmd.ExecuteReader();
        //            using (cmd)
        //            {
        //                using (reader)
        //                {
        //                    while (reader.Read())
        //                    {
        //                        string subName = (string)reader["resource-name"];
        //                        string path =
        //                            $"/api/somiod/{appName}/{containerName}/subs/{subName}";
        //                        subscriptionPaths.Add(path);
        //                    }
        //                }
        //            }
        //        }

        //        return Ok(subscriptionPaths);
        //    }
        //    catch (Exception ex)
        //    {
        //        return InternalServerError(ex);
        //    }
        //}

        #endregion

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
        [Route("{appName}/{containerName}/subs/{subName}")]
        //[GetRoute("{appName}/{containerName}/subs/{subName}")]
        public IHttpActionResult GetSubscriptionByName(
            string appName,
            string containerName,
            string subName
        )
        {
            Subscription sub = null;
            var conn = new SqlConnection(connection);

            string getQuery =
                @"
                SELECT s.[resource-name],
                       s.[creation-datetime],
                       s.[container-resource-name],
                       s.[res-type],
                       s.[evt],
                       s.[endpoint]
                FROM [subscription] s
                JOIN [container] c ON c.[resource-name] = s.[container-resource-name]
                JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                WHERE a.[resource-name] = @appName
                AND c.[resource-name] = @containerName
                AND s.[resource-name] = @subName";

            var cmd = new SqlCommand(getQuery, conn);
            cmd.Parameters.AddWithValue("@appName", appName);
            cmd.Parameters.AddWithValue("@containerName", containerName);
            cmd.Parameters.AddWithValue("@subName", subName);

            try
            {
                using (conn)
                {
                    conn.Open();
                    var reader = cmd.ExecuteReader();
                    using (cmd)
                    {
                        using (reader)
                        {
                            if (reader.Read())
                            {
                                sub = new Subscription
                                {
                                    ResourceName = (string)reader["resource-name"],
                                    CreationDatetime = (DateTime)reader["creation-datetime"],
                                    ContainerResourceName = (string)
                                        reader["container-resource-name"],
                                    ResType = (string)reader["res-type"],
                                    Evt = (int)reader["evt"],
                                    Endpoint = (string)reader["endpoint"],
                                };
                            }
                        }
                    }
                }

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
        ///   "resType": "subscription",
        ///   "evt": 1,
        ///   "endpoint": "http://example.com:8080/notify"
        /// }
        /// </remarks>
        [HttpPost]
        [Route("{appName}/{containerName}")]
        //[PostRoute("{appName}/{containerName}")]
        public IHttpActionResult CreateSubscription(
            string appName,
            string containerName,
            [FromBody] Subscription value
        )
        {
            // Validação: body não pode estar vazio
            if (value == null)
                return BadRequest("O corpo da requisição não pode estar vazio.");

            // Auto-gerar nome se não fornecido
            if (string.IsNullOrWhiteSpace(value.ResourceName))
                value.ResourceName =
                    "sub-"
                    + DateTime.UtcNow.ToString("yyyyMMddHHmmss")
                    + "-"
                    + Guid.NewGuid().ToString().Substring(0, 8);

            // Validação: evt deve ser 1, 2 ou 3 (combinação)
            if (value.Evt != 1 && value.Evt != 2 && value.Evt != 3)
                return BadRequest("O campo 'evt' deve ser 1 (criação), 2 (deletion) ou 3 (ambos).");

            // Validação: endpoint é obrigatório
            if (string.IsNullOrWhiteSpace(value.Endpoint))
                return BadRequest("O campo 'endpoint' é obrigatório.");

            // Validação: endpoint deve ser URL válida (HTTP ou MQTT)
            if (!IsValidEndpoint(value.Endpoint))
                return BadRequest(
                    "O 'endpoint' deve ser uma URL válida (http://, https:// ou mqtt://)."
                );

            // Configurar propriedades automáticas
            value.ResType = "subscription";
            value.ContainerResourceName = containerName;
            value.CreationDatetime = DateTime.UtcNow;

            // SQL Queries
            string sqlCheckParent =
                @"
                SELECT COUNT(*)
                FROM [container] c 
                JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                WHERE a.[resource-name] = @applicationName 
                AND c.[resource-name] = @containerName";

            string sqlCheckDuplicate =
                @"
                SELECT COUNT(*)
                FROM [subscription]
                WHERE [resource-name] = @subName
                AND [container-resource-name] = @containerName";

            string sqlInsert =
                @"
                INSERT INTO [subscription]
                ([resource-name], [creation-datetime], [container-resource-name], [res-type], [evt], [endpoint])
                VALUES (@resourceName, @creationDatetime, @containerResourceName, @resType, @evt, @endpoint)";

            SqlConnection conn = new SqlConnection(connection);

            try
            {
                using (conn)
                {
                    conn.Open();

                    // 1) Verificar se app + container existem
                    var cmdCheckParent = new SqlCommand(sqlCheckParent, conn);
                    cmdCheckParent.Parameters.AddWithValue("@applicationName", appName);
                    cmdCheckParent.Parameters.AddWithValue("@containerName", containerName);

                    int containerCount = (int)cmdCheckParent.ExecuteScalar();
                    if (containerCount == 0)
                        return NotFound();

                    // 2) Verificar se já existe subscription com este nome e container
                    var cmdCheckDuplicate = new SqlCommand(sqlCheckDuplicate, conn);
                    cmdCheckDuplicate.Parameters.AddWithValue("@subName", value.ResourceName);
                    cmdCheckDuplicate.Parameters.AddWithValue("@containerName", containerName);

                    int subCount = (int)cmdCheckDuplicate.ExecuteScalar();
                    if (subCount > 0)
                        return Conflict();

                    // 3) Inserir a nova subscription
                    var cmd = new SqlCommand(sqlInsert, conn);
                    cmd.Parameters.AddWithValue("@resourceName", value.ResourceName);
                    cmd.Parameters.AddWithValue("@creationDatetime", value.CreationDatetime);
                    cmd.Parameters.AddWithValue(
                        "@containerResourceName",
                        value.ContainerResourceName
                    );
                    cmd.Parameters.AddWithValue("@resType", value.ResType);
                    cmd.Parameters.AddWithValue("@evt", value.Evt);
                    cmd.Parameters.AddWithValue("@endpoint", value.Endpoint);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        // Formatar o timestamp para ISO 8601
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
                    else
                    {
                        return InternalServerError(new Exception("Falha ao criar subscription."));
                    }
                }
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
        [Route("{appName}/{containerName}/subs/{subName}")]
        //[DeleteRoute("{appName}/{containerName}/subs/{subName}")]
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
