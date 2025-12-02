using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http.Routing;

namespace Api.Routing
{
    /// <summary>
    /// Constraint that requires the 'somiod-discovery' header to be present with a specific value (res-type).
    /// </summary>
    public class DiscoveryConstraint : IHttpRouteConstraint
    {
        private readonly string _resType;

        public DiscoveryConstraint(string resType)
        {
            _resType = resType ?? throw new ArgumentNullException(nameof(resType));
        }

        public bool Match(
            HttpRequestMessage request,
            IHttpRoute route,
            string parameterName,
            IDictionary<string, object> values,
            HttpRouteDirection routeDirection
        )
        {
            if (routeDirection == HttpRouteDirection.UriGeneration)
                return true;

            if (request.Headers.TryGetValues("somiod-discovery", out IEnumerable<string> headers))
            {
                return headers.Any(h =>
                    string.Equals(h, _resType, StringComparison.OrdinalIgnoreCase)
                );
            }
            return false;
        }
    }

    /// <summary>
    /// Constraint that requires the 'somiod-discovery' header to be absent (for regular/non-discovery routes).
    /// </summary>
    public class NoDiscoveryConstraint : IHttpRouteConstraint
    {
        public bool Match(
            HttpRequestMessage request,
            IHttpRoute route,
            string parameterName,
            IDictionary<string, object> values,
            HttpRouteDirection routeDirection
        )
        {
            if (routeDirection == HttpRouteDirection.UriGeneration)
                return true;
            return !request.Headers.Contains("somiod-discovery");
        }
    }
}
