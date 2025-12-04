using System.Net.Http;

namespace Api.Routing
{
    public class GetRouteAttribute : MethodConstraintedRouteAttribute
    {
        public GetRouteAttribute(
            string template,
            string discoveryResType = null,
            bool noDiscovery = false
        )
            : base(template ?? "", HttpMethod.Get, discoveryResType, noDiscovery) { }
    }

    public class PostRouteAttribute : MethodConstraintedRouteAttribute
    {
        public PostRouteAttribute(
            string template,
            string discoveryResType = null,
            bool noDiscovery = false
        )
            : base(template ?? "", HttpMethod.Post, discoveryResType, noDiscovery) { }
    }

    public class PutRouteAttribute : MethodConstraintedRouteAttribute
    {
        public PutRouteAttribute(
            string template,
            string discoveryResType = null,
            bool noDiscovery = false
        )
            : base(template ?? "", HttpMethod.Put, discoveryResType, noDiscovery) { }
    }

    public class DeleteRouteAttribute : MethodConstraintedRouteAttribute
    {
        public DeleteRouteAttribute(
            string template,
            string discoveryResType = null,
            bool noDiscovery = false
        )
            : base(template ?? "", HttpMethod.Delete, discoveryResType, noDiscovery) { }
    }
}
