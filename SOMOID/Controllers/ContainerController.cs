using System;
using System.Net;
using System.Web.Http;
using Api.Routing;
using SOMOID.Helpers;
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
        private readonly SQLHelper sqlHelper = new SQLHelper();

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
            try
            {
                var container = sqlHelper.GetContainer(appName, containerName);
                if (container == null)
                    return NotFound();
                return Ok(container);
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

            try
            {
                if (!sqlHelper.ApplicationExists(appName))
                    return NotFound();

                if (sqlHelper.ContainerNameExists(appName, value.ResourceName))
                    return Conflict();

                var created = sqlHelper.InsertContainer(value);
                if (created)
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

            try
            {
                if (!sqlHelper.ContainerExists(appName, containerName))
                    return NotFound();

                if (sqlHelper.ContainerNameExists(appName, value.ResourceName))
                    return Conflict();

                var updated = sqlHelper.RenameContainer(appName, containerName, value.ResourceName);
                if (updated == null)
                    return InternalServerError(new Exception("Falha ao atualizar container."));

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
            try
            {
                if (!sqlHelper.ContainerExists(appName, containerName))
                    return NotFound();

                var deleted = sqlHelper.DeleteContainerCascade(appName, containerName);
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
