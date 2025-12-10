using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Http;
using Api.Routing;
using SOMOID.Helpers;
using Swashbuckle.Swagger.Annotations;

namespace SOMOID.Controllers
{
    public class DiscoveryController : ApiController
    {
        private SQLHelper SQLHelperInstance = new SQLHelper();

        #region Discovery Actions

        [HttpGet]
        [GetRoute("api/somiod", discoveryResType: "application", false)]
        public IHttpActionResult DiscoverApplications()
        {
            try
            {
                var applications = SQLHelperInstance.GetAllApplications();
                if (applications == null || applications.Count == 0)
                    return Content(HttpStatusCode.NotFound, "Error there's no applications yet created");
                return Ok(applications);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [GetRoute("api/somiod", discoveryResType: "container", false)]
        public IHttpActionResult DiscoverAllContainers()
        {
            try
            {
                var containers = SQLHelperInstance.GetAllContainers();
                if (containers == null || containers.Count == 0)
                    return Content(HttpStatusCode.NotFound, "Error there's no containers yet created");
                return Ok(containers);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [GetRoute(
            "api/somiod/{appName:regex(^[^/]+$):applicationexists}",
            discoveryResType: "container",
            false
        )]
        public IHttpActionResult DiscoverContainers(string appName)
        {
            try
            {
                var containers = SQLHelperInstance.GetAllContainers(appName);
                if (containers == null || containers.Count == 0)
                    return Content(HttpStatusCode.NotFound, "No containers found for the application");
                return Ok(containers);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [GetRoute(
            "api/somiod",
            discoveryResType: "content-instance",
            false
        )]
        public IHttpActionResult DiscoverAllContentInstances()
        {
            try
            {
                var contentInstances = SQLHelperInstance.GetAllContentInstances();
                if (contentInstances == null || contentInstances.Count == 0)
                {
                    return Content(HttpStatusCode.NotFound, "No Content Instances created yet");
                }
                return Ok(contentInstances);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [GetRoute(
            "api/somiod/{appName:regex(^[^/]+$):applicationexists}",
            discoveryResType: "content-instance",
            false
        )]
        public IHttpActionResult DiscoverContentInstances(string appName)
        {
            try
            {
                var contentInstances = SQLHelperInstance.GetAllContentInstancesFromApp(appName);
                if (contentInstances == null || contentInstances.Count == 0)
                {
                    return Content(HttpStatusCode.NotFound, $"No Content Instances found for app: '{appName}'");
                }
                return Ok(contentInstances);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [GetRoute(
            "api/somiod",
            discoveryResType: "subscription",
            false
        )]
        public IHttpActionResult DiscoverAllSubscriptions()
        {
            try
            {
                var subscriptions = SQLHelperInstance.GetAllSubscriptions();
                if (subscriptions == null || subscriptions.Count == 0)
                {
                    return Content(HttpStatusCode.NotFound, "No subscriptions created yet");
                }
                return Ok(subscriptions);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [GetRoute(
            "api/somiod/{appName:regex(^[^/]+$):applicationexists}/{containerName:regex(^[^/]+$):containerexists}",
            discoveryResType: "subscription",
            false
        )]
        [SwaggerResponse(HttpStatusCode.OK, "List of subscriptions for the container", typeof(List<string>))]
        [SwaggerResponse(HttpStatusCode.NotFound, "No subscriptions found")]
        public IHttpActionResult DiscoverSubscriptions(string appName, string containerName)
        {
            
            try
            {
                var subscriptions = SQLHelperInstance.GetAllSubscriptions(appName, containerName);
                if (subscriptions == null || subscriptions.Count == 0)
                    return Content(HttpStatusCode.NotFound, "No subscriptions found");
                return Ok(subscriptions);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        #endregion
    }
}