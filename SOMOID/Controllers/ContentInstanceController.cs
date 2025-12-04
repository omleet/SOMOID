using System;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Validation.Validators;
using Api.Routing;
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
        private readonly SQLHelper sqlHelper = new SQLHelper();

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
        public IHttpActionResult CreateContentInstance(
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
            value.CreationDatetime = DateTime.UtcNow;

            try
            {
                if (!sqlHelper.ContentInstanceParentExists(appName, containerName))
                    return NotFound();

                if (sqlHelper.ContentInstanceExistsInContainer(containerName, value.ResourceName))
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
        public IHttpActionResult DeleteContentInstance(
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
