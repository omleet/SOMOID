using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http.Routing;

namespace Api.Routing
{
    // Search in https://stackoverflow.com/questions/23094584/...
    public class MethodConstraintedRouteAttribute : RouteFactoryAttribute
    {
        public MethodConstraintedRouteAttribute(string template, HttpMethod method)
            : base(template)
        {
            Method = method;
        }

        public HttpMethod Method { get; private set; }

        public override IDictionary<string, object> Constraints
        {
            get
            {
                var constraints = new HttpRouteValueDictionary();
                constraints.Add("method", new MethodConstraint(Method));
                return constraints;
            }
        }
    }
}
