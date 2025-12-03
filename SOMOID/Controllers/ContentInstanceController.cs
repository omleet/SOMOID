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
    /// Controlador para gerir Content-Instances no middleware SOMIOD.
    /// Uma content-instance representa um registo de dados criado num container.
    /// </summary>
    [RoutePrefix("api/somiod/cint")]
    public class ContentInstanceController : ApiController
    {
        string connection = Properties.Settings.Default.ConnectionStr;


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
        [Route("{appName}/{containerName}/{ciName}")]
        //[GetRoute("{appName}/{containerName}/{ciName}")]
        public IHttpActionResult GetContentInstance(
            string appName,
            string containerName,
            string ciName
        )
        {
            ContentInstance ci = null;
            var conn = new SqlConnection(connection);

            string sql =
                @"
                SELECT ci.[resource-name],
                       ci.[creation-datetime],
                       ci.[container-resource-name],
                       ci.[res-type],
                       ci.[content-type],
                       ci.[content]
                FROM [content-instance] ci
                JOIN [container] c ON c.[resource-name] = ci.[container-resource-name]
                JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                WHERE a.[resource-name] = @appName
                  AND c.[resource-name] = @contName
                  AND ci.[resource-name] = @ciName";

            var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@appName", appName);
            cmd.Parameters.AddWithValue("@contName", containerName);
            cmd.Parameters.AddWithValue("@ciName", ciName);

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
                                ci = new ContentInstance
                                {
                                    ResourceName = (string)reader["resource-name"],
                                    CreationDatetime = (DateTime)reader["creation-datetime"],
                                    ContainerResourceName = (string)
                                        reader["container-resource-name"],
                                    ResType = (string)reader["res-type"],
                                    ContentType = (string)reader["content-type"],
                                    Content = (string)reader["content"],
                                };
                            }
                        }
                    }
                }

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
        [Route("{appName}/{containerName}")]
        //[PostRoute("{appName}/{containerName}")]
        public IHttpActionResult CreateContentInstance(
            string appName,
            string containerName,
            [FromBody] ContentInstance value
        )
        {
            if (value == null)
                return BadRequest("O corpo da requisição não pode estar vazio.");

            if (string.IsNullOrWhiteSpace(value.ResourceName))
                value.ResourceName =
                    "ci-"
                    + DateTime.UtcNow.ToString("yyyyMMddHHmmss")
                    + "-"
                    + Guid.NewGuid().ToString().Substring(0, 8);

            if (string.IsNullOrWhiteSpace(value.ContentType))
                return BadRequest("O campo 'contentType' é obrigatório.");

            if (string.IsNullOrWhiteSpace(value.Content))
                return BadRequest("O campo 'content' é obrigatório.");

            value.ResType = "content-instance";
            value.ContainerResourceName = containerName;
            value.CreationDatetime = DateTime.UtcNow;

            var conn = new SqlConnection(connection);

            string sqlCheckParent =
                @"
                SELECT COUNT(*)
                FROM [container] c
                JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                WHERE a.[resource-name] = @appName
                  AND c.[resource-name] = @contName";

            string sqlCheckDuplicate =
                @"
                SELECT COUNT(*)
                FROM [content-instance]
                WHERE [resource-name] = @ciName
                  AND [container-resource-name] = @contName";

            string sqlInsert =
                @"
                INSERT INTO [content-instance]
                    ([resource-name], [creation-datetime], [container-resource-name],
                     [res-type], [content-type], [content])
                VALUES (@resourceName, @creationDatetime, @containerResourceName,
                        @resType, @contentType, @content)";

            try
            {
                using (conn)
                {
                    conn.Open();

                    // Verificar se app+container existem
                    var cmdCheckParent = new SqlCommand(sqlCheckParent, conn);
                    cmdCheckParent.Parameters.AddWithValue("@appName", appName);
                    cmdCheckParent.Parameters.AddWithValue("@contName", containerName);
                    int parentCount = (int)cmdCheckParent.ExecuteScalar();
                    if (parentCount == 0)
                        return NotFound();

                    // Verificar duplicado
                    var cmdCheckDup = new SqlCommand(sqlCheckDuplicate, conn);
                    cmdCheckDup.Parameters.AddWithValue("@ciName", value.ResourceName);
                    cmdCheckDup.Parameters.AddWithValue("@contName", containerName);
                    int dupCount = (int)cmdCheckDup.ExecuteScalar();
                    if (dupCount > 0)
                        return Conflict();

                    // Inserir
                    var cmd = new SqlCommand(sqlInsert, conn);
                    cmd.Parameters.AddWithValue("@resourceName", value.ResourceName);
                    cmd.Parameters.AddWithValue("@creationDatetime", value.CreationDatetime);
                    cmd.Parameters.AddWithValue(
                        "@containerResourceName",
                        value.ContainerResourceName
                    );
                    cmd.Parameters.AddWithValue("@resType", value.ResType);
                    cmd.Parameters.AddWithValue("@contentType", value.ContentType);
                    cmd.Parameters.AddWithValue("@content", value.Content);

                    int rows = cmd.ExecuteNonQuery();
                    if (rows > 0)
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
                        return Created(locationUrl, responseValue);
                    }

                    return InternalServerError(new Exception("Falha ao criar content-instance."));
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
        /// Elimina uma content-instance específica de um container.
        /// </summary>
        /// <param name="appName">Nome da application</param>
        /// <param name="containerName">Nome do container</param>
        /// <param name="ciName">Nome da content-instance</param>
        /// <returns>200 OK ou 404 NotFound</returns>
        [HttpDelete]
        [Route("{appName}/{containerName}/{ciName}")]
        //[DeleteRoute("{appName}/{containerName}/{ciName}")]
        public IHttpActionResult DeleteContentInstance(
            string appName,
            string containerName,
            string ciName
        )
        {
            var conn = new SqlConnection(connection);

            string sqlCheckExists =
                @"
                SELECT COUNT(*)
                FROM [content-instance] ci
                JOIN [container] c ON c.[resource-name] = ci.[container-resource-name]
                JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                WHERE a.[resource-name] = @appName
                  AND c.[resource-name] = @contName
                  AND ci.[resource-name] = @ciName";

            string sqlDelete =
                @"
                DELETE ci
                FROM [content-instance] ci
                JOIN [container] c ON c.[resource-name] = ci.[container-resource-name]
                JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                WHERE a.[resource-name] = @appName
                  AND c.[resource-name] = @contName
                  AND ci.[resource-name] = @ciName";

            try
            {
                using (conn)
                {
                    conn.Open();

                    var cmdCheck = new SqlCommand(sqlCheckExists, conn);
                    cmdCheck.Parameters.AddWithValue("@appName", appName);
                    cmdCheck.Parameters.AddWithValue("@contName", containerName);
                    cmdCheck.Parameters.AddWithValue("@ciName", ciName);
                    int exists = (int)cmdCheck.ExecuteScalar();
                    if (exists == 0)
                        return NotFound();

                    var cmdDel = new SqlCommand(sqlDelete, conn);
                    cmdDel.Parameters.AddWithValue("@appName", appName);
                    cmdDel.Parameters.AddWithValue("@contName", containerName);
                    cmdDel.Parameters.AddWithValue("@ciName", ciName);
                    int rows = cmdDel.ExecuteNonQuery();

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
