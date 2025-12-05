using Api.Routing;
using Swashbuckle.Swagger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Description;

namespace SOMOID.Swagger
{
    public class DiscoveryHeaderOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            var actionDesc = apiDescription.ActionDescriptor as ReflectedHttpActionDescriptor;
            if (actionDesc == null) return;

            // Get your MethodConstraintedRouteAttribute
            var attr = actionDesc.MethodInfo
                .GetCustomAttributes(true)
                .OfType<MethodConstraintedRouteAttribute>()
                .FirstOrDefault();

            // No discovery? Skip.
            if (attr == null || attr.NoDiscovery)
                return;

            // We only care about routes with a DiscoveryResType defined
            if (string.IsNullOrEmpty(attr.DiscoveryResType))
                return;

            // Add header parameter to Swagger
            operation.parameters = operation.parameters ?? new List<Parameter>();

            operation.parameters.Add(new Parameter
            {
                name = "somiod-discovery",
                @in = "header",
                type = "string",
                required = true,
                @default = attr.DiscoveryResType,
                description = $"Required discovery header. Must be '{attr.DiscoveryResType}'."
            });

            // Make Swagger show them as different routes
            operation.summary = $"{operation.summary} [Discovery: {attr.DiscoveryResType}]";
            operation.operationId += $"_{attr.DiscoveryResType}";
        }
    }

}