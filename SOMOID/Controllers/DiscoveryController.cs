using System;
using System.Collections.Generic;
using System.Web.Http;
using Api.Routing;
using SOMOID.Helpers;


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
                return Ok(SQLHelperInstance.GetAllApplications());
            }catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [GetRoute(
            "{appName:regex(^[^/]+$):applicationexists}",
            discoveryResType: "container",
            false
        )]
        public IHttpActionResult DiscoverContainers(string appName)
        {
            try
            {
                return Ok(SQLHelperInstance.GetAllContainers(appName));
            } catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [GetRoute(
            "{appName:regex(^[^/]+$):applicationexists}",
            discoveryResType: "content-instance",
            false
        )]
        public IHttpActionResult DiscoverContentInstances(string appName)
        {
            try
            {
                return Ok(SQLHelperInstance.GetAllContentInstances(appName));
            }catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [GetRoute(
            "{appName:regex(^[^/]+$):applicationexists}/{containerName:regex(^[^/]+$):containerexists}",
            discoveryResType: "subscription",
            false
        )]
        public IHttpActionResult DiscoverSubscriptions(string appName, string containerName)
        {
            try
            {
                return Ok(SQLHelperInstance.GetAllSubscriptions(appName, containerName));
            } catch(Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        #endregion
    }
}
