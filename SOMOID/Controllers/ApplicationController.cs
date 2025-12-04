using System;
using System.Net;
using System.Web.Http;
using Api.Routing;
using SOMOID.Helpers;
using SOMOID.Models;

namespace SOMOID.Controllers
{
    /// <summary>
    /// Controlador para gerenciar Applications no middleware SOMIOD.
    /// Uma application representa uma aplicação específica do mundo real no sistema.
    /// </summary>
    public class ApplicationController : ApiController
    {
        private readonly SQLHelper sqlHelper = new SQLHelper();
        private const string ApplicationResType = "application";
        private const string DeletedApplicationResType = "application-deleted";

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
        [GetRoute("api/somiod/{appName}")]
        public IHttpActionResult GetApplicationByName(string appName)
        {
            try
            {
                var app = sqlHelper.GetApplication(appName);
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
        /// "resourceName": "lighting" // Opcional - auto-gerado se omitido
        /// }
        ///
        /// Exemplo de resposta (201 Created):
        /// {
        /// "resourceName": "lighting",
        /// "creationDatetime": "2025-12-02T20:02:00",
        /// "resType": "application"
        /// }
        /// </remarks>
        [HttpPost]
        [PostRoute("api/somiod")]
        public IHttpActionResult CreateApplication([FromBody] Application value)
        {
            if (value == null)
                return BadRequest("O corpo da requisição não pode estar vazio.");
            if (string.IsNullOrWhiteSpace(value.ResourceName))
                value.ResourceName =
                    "app-"
                    + DateTime.UtcNow.ToString("yyyyMMddHHmmss")
                    + "-"
                    + Guid.NewGuid().ToString().Substring(0, 8);
            value.ResType = ApplicationResType;
            value.CreationDatetime = DateTime.UtcNow;
            try
            {
                var existingResType = sqlHelper.GetApplicationResTypeValue(value.ResourceName);
                if (!string.IsNullOrEmpty(existingResType))
                {
                    if (existingResType.Equals(ApplicationResType, StringComparison.OrdinalIgnoreCase))
                        return Content(HttpStatusCode.Conflict, "Error the name for the application already exists try another");
                    if (existingResType.Equals(DeletedApplicationResType, StringComparison.OrdinalIgnoreCase))
                        return Content(HttpStatusCode.Conflict, "Application with that name can't be created try another");
                    return Content(HttpStatusCode.Conflict, "Error the name for the application already exists try another");
                }

                var created = sqlHelper.InsertApplication(value);
                if (created)
                {
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

                return InternalServerError(new Exception("Falha ao criar application."));
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
        /// "resourceName": "lighting-v2" // Novo nome para a application
        /// }
        ///
        /// Exemplo: PUT /api/somiod/lighting
        /// Vai renomear "lighting" para "lighting-v2"
        /// </remarks>
        [HttpPut]
        [PutRoute("api/somiod/{appName:regex(^[^/]+$)}")]
        public IHttpActionResult UpdateApplication(string appName, [FromBody] Application value)
        {
            if (value == null)
                return BadRequest("O corpo da requisição não pode estar vazio.");
            if (string.IsNullOrWhiteSpace(value.ResourceName))
                return BadRequest(
                    "O campo 'resourceName' é obrigatório para atualizar uma application."
                );
            if (value.ResourceName.Equals(appName, StringComparison.OrdinalIgnoreCase))
                return BadRequest("O novo resource-name deve ser diferente do atual.");
            try
            {
                var existingApp = sqlHelper.GetApplication(appName);
                if (existingApp == null)
                    return Content(HttpStatusCode.NotFound, "The application does not exist try another");

                var targetResType = sqlHelper.GetApplicationResTypeValue(value.ResourceName);
                if (!string.IsNullOrEmpty(targetResType) && targetResType.Equals(ApplicationResType, StringComparison.OrdinalIgnoreCase))
                    return Conflict();

                var updatedApp = sqlHelper.RenameApplication(appName, value.ResourceName);
                if (updatedApp == null)
                    return InternalServerError(new Exception("Falha ao atualizar application."));

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
        [DeleteRoute("api/somiod/{appName:regex(^[^/]+$)}")]
        public IHttpActionResult DeleteApplication(string appName)
        {
            try
            {
                var deleted = sqlHelper.SoftDeleteApplication(appName);
                if (!deleted)
                    return Content(HttpStatusCode.NotFound, "The application does not exist try another");
                return Ok("Application deleted with sucess");
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        #endregion
    }
}
