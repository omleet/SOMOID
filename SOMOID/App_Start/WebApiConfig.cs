using Api.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Routing;

namespace SOMOID
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            var constraintResolver = new DefaultInlineConstraintResolver();
            constraintResolver.ConstraintMap.Add("applicationexists", typeof(ApplicationExistsConstraint));
            constraintResolver.ConstraintMap.Add("containerexists", typeof(ContainerExistsConstraint));
            config.MapHttpAttributeRoutes(constraintResolver);

            // Web API routes

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/somiod/{controller:alpha}/{id:int}/{action:alpha}",
                defaults: new
                {
                    controller = RouteParameter.Optional,
                    id = RouteParameter.Optional,
                    action = RouteParameter.Optional
                }
            );
            config.Formatters.XmlFormatter.UseXmlSerializer = true;
        }
    }
}
