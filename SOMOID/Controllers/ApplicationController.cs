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
    /// Controlador para gerenciar Applications no middleware SOMIOD.
    /// Uma application representa uma aplicação específica do mundo real no sistema.
    /// </summary>
    [RoutePrefix("api/somiod/app")]
    public class ApplicationController : ApiController
    {
        string connection = Properties.Settings.Default.ConnectionStr;

        #region Discovery Operation

        /// <summary>
        /// Descobre todas as applications no sistema.
        /// Deve incluir o header "somiod-discovery: application" para ativar a operação de discovery.
        /// </summary>
        /// <returns>Lista de caminhos para todas as applications encontradas</returns>
        /// <remarks>
        /// Exemplos de retorno:
        /// ["/api/somiod/app-1", "/api/somiod/app-2", "/api/somiod/lighting"]
        /// </remarks>
        // [HttpGet]
        // [Route("")]
        //[GetRoute("")]
        // public IHttpActionResult DiscoverApplications()
        // {
        //     // Verificar se o header somiod-discovery está presente
        //     IEnumerable<string> headerValues;
        //     if (
        //         !Request.Headers.TryGetValues("somiod-discovery", out headerValues)
        //         || !headerValues.Any(h => h == "application")
        //     )
        //     {
        //         return NotFound();
        //     }

        //     var applicationPaths = new List<string>();
        //     var conn = new SqlConnection(connection);

        //     string query =
        //         @"
        //         SELECT [resource-name]
        //         FROM [application]
        //         ORDER BY [creation-datetime]";

        //     var cmd = new SqlCommand(query, conn);

        //     try
        //     {
        //         using (conn)
        //         {
        //             conn.Open();
        //             var reader = cmd.ExecuteReader();
        //             using (cmd)
        //             {
        //                 using (reader)
        //                 {
        //                     while (reader.Read())
        //                     {
        //                         string appName = (string)reader["resource-name"];
        //                         string path = $"/api/somiod/{appName}";
        //                         applicationPaths.Add(path);
        //                     }
        //                 }
        //             }
        //         }

        //         return Ok(applicationPaths);
        //     }
        //     catch (Exception ex)
        //     {
        //         return InternalServerError(ex);
        //     }
        // }

        #endregion

        #region GET Operations

        /// <summary>
        /// Obtém uma application específica pelo seu nome.
        /// </summary>
        /// <param name="appName">Nome da application</param>
        /// <returns>Os dados da application solicitada</returns>
        /// <response code="200">Application encontrada</response>
        /// <response code="404">Application não encontrada</response>
        /// <response code="500">Erro interno do servidor</response>
        [HttpGet]
        [Route("{appName}")]
        //[GetRoute("{appName}")]
        public IHttpActionResult GetApplicationByName(string appName)
        {
            Application app = null;
            var conn = new SqlConnection(connection);

            string getQuery =
                @"
                SELECT [resource-name],
                       [creation-datetime],
                       [res-type]
                FROM [application]
                WHERE [resource-name] = @appName";

            var cmd = new SqlCommand(getQuery, conn);
            cmd.Parameters.AddWithValue("@appName", appName);

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
                                app = new Application
                                {
                                    ResourceName = (string)reader["resource-name"],
                                    CreationDatetime = (DateTime)reader["creation-datetime"],
                                    ResType = (string)reader["res-type"],
                                };
                            }
                        }
                    }
                }

                if (app == null)
                    return NotFound();

                return Ok(app);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        #endregion

        #region POST Operations (Create)

        /// <summary>
        /// Cria uma nova application no sistema.
        /// </summary>
        /// <param name="value">Dados da application a criar (pode omitir resource-name para auto-gerar)</param>
        /// <returns>A application criada com todas as suas propriedades</returns>
        /// <response code="201">Application criada com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="409">Application com este nome já existe</response>
        /// <response code="500">Erro interno do servidor</response>
        /// <remarks>
        /// Corpo da requisição:
        /// {
        ///   "resourceName": "lighting"  // Opcional - auto-gerado se omitido
        /// }
        ///
        /// Exemplo de resposta (201 Created):
        /// {
        ///   "resourceName": "lighting",
        ///   "creationDatetime": "2025-12-02T20:02:00",
        ///   "resType": "application"
        /// }
        /// </remarks>
        [HttpPost]
        [Route("")]
        //[PostRoute("")]
        public IHttpActionResult CreateApplication([FromBody] Application value)
        {
            // Validação: body não pode estar vazio
            if (value == null)
                return BadRequest("O corpo da requisição não pode estar vazio.");

            // Auto-gerar nome se não fornecido
            if (string.IsNullOrWhiteSpace(value.ResourceName))
                value.ResourceName =
                    "app-"
                    + DateTime.UtcNow.ToString("yyyyMMddHHmmss")
                    + "-"
                    + Guid.NewGuid().ToString().Substring(0, 8);

            // Configurar propriedades automáticas
            value.ResType = "application";
            value.CreationDatetime = DateTime.UtcNow;

            // SQL Queries
            string sqlCheckDuplicate =
                @"
                SELECT COUNT(*)
                FROM [application]
                WHERE [resource-name] = @appName";

            string sqlInsert =
                @"
                INSERT INTO [application]
                ([resource-name], [creation-datetime], [res-type])
                VALUES (@resourceName, @creationDatetime, @resType)";

            SqlConnection conn = new SqlConnection(connection);

            try
            {
                using (conn)
                {
                    conn.Open();

                    // 1) Verificar se já existe application com este nome
                    var cmdCheckDuplicate = new SqlCommand(sqlCheckDuplicate, conn);
                    cmdCheckDuplicate.Parameters.AddWithValue("@appName", value.ResourceName);

                    int appCount = (int)cmdCheckDuplicate.ExecuteScalar();
                    if (appCount > 0)
                        return Conflict();

                    // 2) Inserir a nova application
                    var cmd = new SqlCommand(sqlInsert, conn);
                    cmd.Parameters.AddWithValue("@resourceName", value.ResourceName);
                    cmd.Parameters.AddWithValue("@creationDatetime", value.CreationDatetime);
                    cmd.Parameters.AddWithValue("@resType", value.ResType);

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
                            resType = value.ResType,
                        };

                        string locationUrl = $"/api/somiod/{value.ResourceName}";
                        return Created(locationUrl, responseValue);
                    }
                    else
                    {
                        return InternalServerError(new Exception("Falha ao criar application."));
                    }
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        #endregion

        #region PUT Operations (Update)

        /// <summary>
        /// Atualiza uma application existente.
        /// Apenas o resource-name pode ser atualizado, alterando a identidade da application.
        /// </summary>
        /// <param name="appName">Nome atual da application a atualizar</param>
        /// <param name="value">Dados atualizados da application (com novo resource-name)</param>
        /// <returns>A application atualizada com todas as suas propriedades</returns>
        /// <response code="200">Application atualizada com sucesso</response>
        /// <response code="400">Dados inválidos ou resource-name não fornecido</response>
        /// <response code="404">Application não encontrada</response>
        /// <response code="409">Novo resource-name já existe</response>
        /// <response code="500">Erro interno do servidor</response>
        /// <remarks>
        /// Corpo da requisição:
        /// {
        ///   "resourceName": "lighting-v2"  // Novo nome para a application
        /// }
        ///
        /// Exemplo: PUT /api/somiod/lighting
        /// Vai renomear "lighting" para "lighting-v2"
        /// </remarks>
        [HttpPut]
        [Route("{appName}")]
        //[PutRoute("{appName}")]
        public IHttpActionResult UpdateApplication(string appName, [FromBody] Application value)
        {
            // Validação: body não pode estar vazio
            if (value == null)
                return BadRequest("O corpo da requisição não pode estar vazio.");

            // Validação: novo resource-name é obrigatório
            if (string.IsNullOrWhiteSpace(value.ResourceName))
                return BadRequest(
                    "O campo 'resourceName' é obrigatório para atualizar uma application."
                );

            // Não permitir update se os nomes são iguais
            if (value.ResourceName.Equals(appName, StringComparison.OrdinalIgnoreCase))
                return BadRequest("O novo resource-name deve ser diferente do atual.");

            var conn = new SqlConnection(connection);

            // SQL Queries
            string sqlCheckExists =
                @"
                SELECT COUNT(*)
                FROM [application]
                WHERE [resource-name] = @appName";

            string sqlCheckNewNameExists =
                @"
                SELECT COUNT(*)
                FROM [application]
                WHERE [resource-name] = @newAppName";

            string sqlUpdate =
                @"
                UPDATE [application]
                SET [resource-name] = @newResourceName
                WHERE [resource-name] = @oldResourceName";

            try
            {
                using (conn)
                {
                    conn.Open();

                    // 1) Verificar se application atual existe
                    var cmdCheckExists = new SqlCommand(sqlCheckExists, conn);
                    cmdCheckExists.Parameters.AddWithValue("@appName", appName);

                    int existsCount = (int)cmdCheckExists.ExecuteScalar();
                    if (existsCount == 0)
                        return NotFound();

                    // 2) Verificar se novo nome já existe
                    var cmdCheckNewName = new SqlCommand(sqlCheckNewNameExists, conn);
                    cmdCheckNewName.Parameters.AddWithValue("@newAppName", value.ResourceName);

                    int newNameCount = (int)cmdCheckNewName.ExecuteScalar();
                    if (newNameCount > 0)
                        return Conflict();

                    // 3) Atualizar a application
                    var cmd = new SqlCommand(sqlUpdate, conn);
                    cmd.Parameters.AddWithValue("@oldResourceName", appName);
                    cmd.Parameters.AddWithValue("@newResourceName", value.ResourceName);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        // Buscar dados atualizados
                        string sqlGetUpdated =
                            @"
                            SELECT [resource-name],
                                   [creation-datetime],
                                   [res-type]
                            FROM [application]
                            WHERE [resource-name] = @appName";

                        var cmdGet = new SqlCommand(sqlGetUpdated, conn);
                        cmdGet.Parameters.AddWithValue("@appName", value.ResourceName);

                        var readerUpdated = cmdGet.ExecuteReader();
                        Application updatedApp = null;

                        if (readerUpdated.Read())
                        {
                            updatedApp = new Application
                            {
                                ResourceName = (string)readerUpdated["resource-name"],
                                CreationDatetime = (DateTime)readerUpdated["creation-datetime"],
                                ResType = (string)readerUpdated["res-type"],
                            };
                        }

                        var responseValue = new
                        {
                            resourceName = updatedApp.ResourceName,
                            creationDatetime = updatedApp.CreationDatetime.ToString(
                                "yyyy-MM-ddTHH:mm:ss"
                            ),
                            resType = updatedApp.ResType,
                        };

                        return Ok(responseValue);
                    }
                    else
                    {
                        return InternalServerError(
                            new Exception("Falha ao atualizar application.")
                        );
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
        /// Deleta uma application específica e todos os seus recursos filhos (containers, content-instances, subscriptions).
        /// </summary>
        /// <param name="appName">Nome da application a deletar</param>
        /// <returns>Sem conteúdo se bem-sucedido</returns>
        /// <response code="200">Application deletada com sucesso</response>
        /// <response code="404">Application não encontrada</response>
        /// <response code="500">Erro interno do servidor</response>
        [HttpDelete]
        [Route("{appName}")]
        //[DeleteRoute("{appName}")]
        public IHttpActionResult DeleteApplication(string appName)
        {
            var conn = new SqlConnection(connection);

            // Queries para validar e deletar em cascata
            string sqlCheckExists =
                @"
                SELECT COUNT(*)
                FROM [application]
                WHERE [resource-name] = @appName";

            string sqlDelete =
                @"
                DELETE ci
                FROM [content-instance] ci
                JOIN [container] c ON c.[resource-name] = ci.[container-resource-name]
                WHERE c.[application-resource-name] = @appName;

                DELETE s
                FROM [subscription] s
                JOIN [container] c ON c.[resource-name] = s.[container-resource-name]
                WHERE c.[application-resource-name] = @appName;

                DELETE [container]
                WHERE [application-resource-name] = @appName;

                DELETE [application]
                WHERE [resource-name] = @appName;";

            try
            {
                using (conn)
                {
                    conn.Open();

                    // 1) Verificar se existe
                    var cmdCheck = new SqlCommand(sqlCheckExists, conn);
                    cmdCheck.Parameters.AddWithValue("@appName", appName);

                    int existsCount = (int)cmdCheck.ExecuteScalar();
                    if (existsCount == 0)
                        return NotFound();

                    // 2) Deletar em cascata
                    // Nota: Em SQL Server, não é possível executar múltiplos comandos em um único cmd.ExecuteNonQuery()
                    // Portanto, é necessário executar cada DELETE individualmente

                    // 2.1) Deletar content-instances
                    string sqlDeleteContentInstances =
                        @"
                        DELETE ci
                        FROM [content-instance] ci
                        JOIN [container] c ON c.[resource-name] = ci.[container-resource-name]
                        WHERE c.[application-resource-name] = @appName";

                    var cmdDeleteCI = new SqlCommand(sqlDeleteContentInstances, conn);
                    cmdDeleteCI.Parameters.AddWithValue("@appName", appName);
                    cmdDeleteCI.ExecuteNonQuery();

                    // 2.2) Deletar subscriptions
                    string sqlDeleteSubscriptions =
                        @"
                        DELETE s
                        FROM [subscription] s
                        JOIN [container] c ON c.[resource-name] = s.[container-resource-name]
                        WHERE c.[application-resource-name] = @appName";

                    var cmdDeleteSub = new SqlCommand(sqlDeleteSubscriptions, conn);
                    cmdDeleteSub.Parameters.AddWithValue("@appName", appName);
                    cmdDeleteSub.ExecuteNonQuery();

                    // 2.3) Deletar containers
                    string sqlDeleteContainers =
                        @"
                        DELETE [container]
                        WHERE [application-resource-name] = @appName";

                    var cmdDeleteCont = new SqlCommand(sqlDeleteContainers, conn);
                    cmdDeleteCont.Parameters.AddWithValue("@appName", appName);
                    cmdDeleteCont.ExecuteNonQuery();

                    // 2.4) Deletar application
                    string sqlDeleteApp =
                        @"
                        DELETE [application]
                        WHERE [resource-name] = @appName";

                    var cmdDeleteApp = new SqlCommand(sqlDeleteApp, conn);
                    cmdDeleteApp.Parameters.AddWithValue("@appName", appName);
                    int rowsAffected = cmdDeleteApp.ExecuteNonQuery();

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
    }
}
