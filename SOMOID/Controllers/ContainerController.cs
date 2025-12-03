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
    /// Controlador para gerir Containers no middleware SOMIOD.
    /// Um container agrupa content-instances e subscriptions dentro de uma application.
    /// </summary>
    //[RoutePrefix("api/somiod/{container}")]
    public class ContainerController : ApiController
    {
        string connection = Properties.Settings.Default.ConnectionStr;

        #region GET Operations

        /// <summary>
        /// Obtém um container específico de uma application.
        /// </summary>
        /// <param name="appName">Nome da application</param>
        /// <param name="containerName">Nome do container</param>
        /// <returns>Dados do container</returns>
        /// <response code="200">Container encontrado</response>
        /// <response code="404">Application ou container não encontrado</response>
        /// <response code="500">Erro interno</response>
        [HttpGet]
        [GetRoute("api/somiod/{appName}/{containerName}")]
        public IHttpActionResult GetContainer(string appName, string containerName)
        {
            Container cont = null;
            var conn = new SqlConnection(connection);

            string sql =
                @"
                SELECT c.[resource-name],
                       c.[creation-datetime],
                       c.[res-type],
                       c.[application-resource-name]
                FROM [container] c
                JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                WHERE a.[resource-name] = @appName
                  AND c.[resource-name] = @containerName";

            var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@appName", appName);
            cmd.Parameters.AddWithValue("@containerName", containerName);

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
                                cont = new Container
                                {
                                    ResourceName = (string)reader["resource-name"],
                                    CreationDatetime = (DateTime)reader["creation-datetime"],
                                    ResType = (string)reader["res-type"],
                                    ApplicationResourceName = (string)
                                        reader["application-resource-name"],
                                };
                            }
                        }
                    }
                }

                if (cont == null)
                    return NotFound();

                return Ok(cont);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        #endregion

        #region POST Operations (Create)

        /// <summary>
        /// Cria um novo container dentro de uma application.
        /// </summary>
        /// <param name="appName">Nome da application (parent)</param>
        /// <param name="value">Dados do container (resourceName opcional)</param>
        /// <returns>Container criado com todas as propriedades</returns>
        /// <response code="201">Container criado</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="404">Application não encontrada</response>
        /// <response code="409">Já existe container com esse nome na application</response>
        /// <response code="500">Erro interno</response>
        /// <remarks>
        /// POST /api/somiod/app1
        /// Body:
        /// {
        ///   "resourceName": "cont1" // opcional
        /// }
        /// </remarks>
        [HttpPost]
        [PostRoute("api/somiod/{appName:regex(^[^/]+$)}")]
        public IHttpActionResult CreateContainer(string appName, [FromBody] Container value)
        {
            if (value == null)
                return BadRequest("O corpo da requisição não pode estar vazio.");

            if (string.IsNullOrWhiteSpace(value.ResourceName))
                value.ResourceName =
                    "cont-"
                    + DateTime.UtcNow.ToString("yyyyMMddHHmmss")
                    + "-"
                    + Guid.NewGuid().ToString().Substring(0, 8);

            value.ResType = "container";
            value.ApplicationResourceName = appName;
            value.CreationDatetime = DateTime.UtcNow;

            var conn = new SqlConnection(connection);

            string sqlCheckApp =
                @"
                SELECT COUNT(*)
                FROM [application]
                WHERE [resource-name] = @appName";

            string sqlCheckDuplicate =
                @"
                SELECT COUNT(*)
                FROM [container]
                WHERE [resource-name] = @contName
                  AND [application-resource-name] = @appName";

            string sqlInsert =
                @"
                INSERT INTO [container]
                    ([resource-name], [creation-datetime], [res-type], [application-resource-name])
                VALUES (@resourceName, @creationDatetime, @resType, @appResourceName)";

            try
            {
                using (conn)
                {
                    conn.Open();

                    // Verificar se a application existe
                    var cmdCheckApp = new SqlCommand(sqlCheckApp, conn);
                    cmdCheckApp.Parameters.AddWithValue("@appName", appName);
                    int appCount = (int)cmdCheckApp.ExecuteScalar();
                    if (appCount == 0)
                        return NotFound();

                    // Verificar se já existe container com esse nome na mesma app
                    var cmdCheckDup = new SqlCommand(sqlCheckDuplicate, conn);
                    cmdCheckDup.Parameters.AddWithValue("@contName", value.ResourceName);
                    cmdCheckDup.Parameters.AddWithValue("@appName", appName);
                    int contCount = (int)cmdCheckDup.ExecuteScalar();
                    if (contCount > 0)
                        return Conflict();

                    // Inserir container
                    var cmd = new SqlCommand(sqlInsert, conn);
                    cmd.Parameters.AddWithValue("@resourceName", value.ResourceName);
                    cmd.Parameters.AddWithValue("@creationDatetime", value.CreationDatetime);
                    cmd.Parameters.AddWithValue("@resType", value.ResType);
                    cmd.Parameters.AddWithValue("@appResourceName", value.ApplicationResourceName);

                    int rows = cmd.ExecuteNonQuery();
                    if (rows > 0)
                    {
                        var responseValue = new
                        {
                            resourceName = value.ResourceName,
                            creationDatetime = value.CreationDatetime.ToString(
                                "yyyy-MM-ddTHH:mm:ss"
                            ),
                            resType = value.ResType,
                            applicationResourceName = value.ApplicationResourceName,
                        };

                        string locationUrl = $"/api/somiod/{appName}/{value.ResourceName}";
                        return Created(locationUrl, responseValue);
                    }

                    return InternalServerError(new Exception("Falha ao criar container."));
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
        /// Atualiza (renomeia) um container de uma application.
        /// </summary>
        /// <param name="appName">Nome da application</param>
        /// <param name="containerName">Nome atual do container</param>
        /// <param name="value">Dados do container com novo resourceName</param>
        /// <returns>Container atualizado</returns>
        /// <response code="200">Atualizado com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="404">Container ou application não encontrados</response>
        /// <response code="409">Novo nome já existe na mesma application</response>
        /// <response code="500">Erro interno</response>
        [HttpPut]
        [PutRoute("api/somiod/{appName}/{containerName}")]
        public IHttpActionResult UpdateContainer(
            string appName,
            string containerName,
            [FromBody] Container value
        )
        {
            if (value == null)
                return BadRequest("O corpo da requisição não pode estar vazio.");

            if (string.IsNullOrWhiteSpace(value.ResourceName))
                return BadRequest(
                    "O campo 'resourceName' é obrigatório para atualizar o container."
                );

            if (value.ResourceName.Equals(containerName, StringComparison.OrdinalIgnoreCase))
                return BadRequest("O novo resourceName deve ser diferente do atual.");

            var conn = new SqlConnection(connection);

            string sqlCheckExists =
                @"
                SELECT COUNT(*)
                FROM [container] c
                JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                WHERE a.[resource-name] = @appName
                  AND c.[resource-name] = @containerName";

            string sqlCheckNewName =
                @"
                SELECT COUNT(*)
                FROM [container]
                WHERE [resource-name] = @newName
                  AND [application-resource-name] = @appName";

            string sqlUpdate =
                @"
                UPDATE [container]
                SET [resource-name] = @newName
                WHERE [resource-name] = @oldName
                  AND [application-resource-name] = @appName";

            try
            {
                using (conn)
                {
                    conn.Open();

                    // Verificar se container existe
                    var cmdCheck = new SqlCommand(sqlCheckExists, conn);
                    cmdCheck.Parameters.AddWithValue("@appName", appName);
                    cmdCheck.Parameters.AddWithValue("@containerName", containerName);
                    int exists = (int)cmdCheck.ExecuteScalar();
                    if (exists == 0)
                        return NotFound();

                    // Verificar se novo nome já existe
                    var cmdCheckNew = new SqlCommand(sqlCheckNewName, conn);
                    cmdCheckNew.Parameters.AddWithValue("@newName", value.ResourceName);
                    cmdCheckNew.Parameters.AddWithValue("@appName", appName);
                    int newExists = (int)cmdCheckNew.ExecuteScalar();
                    if (newExists > 0)
                        return Conflict();

                    // Atualizar
                    var cmdUpdate = new SqlCommand(sqlUpdate, conn);
                    cmdUpdate.Parameters.AddWithValue("@newName", value.ResourceName);
                    cmdUpdate.Parameters.AddWithValue("@oldName", containerName);
                    cmdUpdate.Parameters.AddWithValue("@appName", appName);

                    int rows = cmdUpdate.ExecuteNonQuery();
                    if (rows > 0)
                    {
                        // buscar dados atualizados
                        string sqlGet =
                            @"
                            SELECT [resource-name], [creation-datetime], [res-type], [application-resource-name]
                            FROM [container]
                            WHERE [resource-name] = @name AND [application-resource-name] = @appName";

                        var cmdGet = new SqlCommand(sqlGet, conn);
                        cmdGet.Parameters.AddWithValue("@name", value.ResourceName);
                        cmdGet.Parameters.AddWithValue("@appName", appName);

                        Container updated = null;
                        var reader = cmdGet.ExecuteReader();
                        if (reader.Read())
                        {
                            updated = new Container
                            {
                                ResourceName = (string)reader["resource-name"],
                                CreationDatetime = (DateTime)reader["creation-datetime"],
                                ResType = (string)reader["res-type"],
                                ApplicationResourceName = (string)
                                    reader["application-resource-name"],
                            };
                        }
                        reader.Close();

                        var resp = new
                        {
                            resourceName = updated.ResourceName,
                            creationDatetime = updated.CreationDatetime.ToString(
                                "yyyy-MM-ddTHH:mm:ss"
                            ),
                            resType = updated.ResType,
                            applicationResourceName = updated.ApplicationResourceName,
                        };

                        return Ok(resp);
                    }

                    return InternalServerError(new Exception("Falha ao atualizar container."));
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
        /// Elimina um container e todos os seus content-instances e subscriptions.
        /// </summary>
        /// <param name="appName">Nome da application</param>
        /// <param name="containerName">Nome do container</param>
        /// <returns>200 OK ou 404 NotFound</returns>
        [HttpDelete]
        [DeleteRoute("api/somiod/{appName}/{containerName}")]
        public IHttpActionResult DeleteContainer(string appName, string containerName)
        {
            var conn = new SqlConnection(connection);

            string sqlCheckExists =
                @"
                SELECT COUNT(*)
                FROM [container] c
                JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                WHERE a.[resource-name] = @appName
                  AND c.[resource-name] = @containerName";

            try
            {
                using (conn)
                {
                    conn.Open();

                    // Verificar se existe
                    var cmdCheck = new SqlCommand(sqlCheckExists, conn);
                    cmdCheck.Parameters.AddWithValue("@appName", appName);
                    cmdCheck.Parameters.AddWithValue("@containerName", containerName);
                    int exists = (int)cmdCheck.ExecuteScalar();
                    if (exists == 0)
                        return NotFound();

                    // 1) Apagar content-instances
                    string sqlDelCI =
                        @"
                        DELETE ci
                        FROM [content-instance] ci
                        JOIN [container] c ON c.[resource-name] = ci.[container-resource-name]
                        JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                        WHERE a.[resource-name] = @appName
                          AND c.[resource-name] = @containerName";
                    var cmdDelCI = new SqlCommand(sqlDelCI, conn);
                    cmdDelCI.Parameters.AddWithValue("@appName", appName);
                    cmdDelCI.Parameters.AddWithValue("@containerName", containerName);
                    cmdDelCI.ExecuteNonQuery();

                    // 2) Apagar subscriptions
                    string sqlDelSub =
                        @"
                        DELETE s
                        FROM [subscription] s
                        JOIN [container] c ON c.[resource-name] = s.[container-resource-name]
                        JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                        WHERE a.[resource-name] = @appName
                          AND c.[resource-name] = @containerName";
                    var cmdDelSub = new SqlCommand(sqlDelSub, conn);
                    cmdDelSub.Parameters.AddWithValue("@appName", appName);
                    cmdDelSub.Parameters.AddWithValue("@containerName", containerName);
                    cmdDelSub.ExecuteNonQuery();

                    // 3) Apagar container
                    string sqlDelCont =
                        @"
                        DELETE c
                        FROM [container] c
                        JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                        WHERE a.[resource-name] = @appName
                          AND c.[resource-name] = @containerName";
                    var cmdDelCont = new SqlCommand(sqlDelCont, conn);
                    cmdDelCont.Parameters.AddWithValue("@appName", appName);
                    cmdDelCont.Parameters.AddWithValue("@containerName", containerName);
                    int rows = cmdDelCont.ExecuteNonQuery();

                    if (rows > 0)
                        return Ok();
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
