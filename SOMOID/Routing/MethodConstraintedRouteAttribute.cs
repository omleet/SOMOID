using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http.Routing;

namespace Api.Routing
{
    // Search in https://stackoverflow.com/questions/23094584/...
    public class MethodConstraintedRouteAttribute : RouteFactoryAttribute
    {
        public MethodConstraintedRouteAttribute(
            string template,
            HttpMethod method,
            string discoveryResType = null,
            bool noDiscovery = false
        )
            : base(template)
        {
            Method = method;
            DiscoveryResType = discoveryResType;
            NoDiscovery = noDiscovery;
        }

        public HttpMethod Method { get; private set; }
        public string DiscoveryResType { get; private set; }
        public bool NoDiscovery { get; private set; }

        public override IDictionary<string, object> Constraints
        {
            get
            {
                var constraints = new HttpRouteValueDictionary();
                constraints.Add("method", new MethodConstraint(Method));

                if (!string.IsNullOrEmpty(DiscoveryResType))
                {
                    constraints.Add("discovery", new DiscoveryConstraint(DiscoveryResType));
                }
                else if (NoDiscovery)
                {
                    constraints.Add("nodiscovery", new NoDiscoveryConstraint());
                }

                return constraints;
            }
        }
    }
}
