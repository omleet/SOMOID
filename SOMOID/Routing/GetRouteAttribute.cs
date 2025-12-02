using System.Net.Http;

namespace Api.Routing
{
    // Search in https://stackoverflow.com/questions/23094584/...
    public class GetRouteAttribute : MethodConstraintedRouteAttribute
    {
        public GetRouteAttribute(string template)
            : base(template ?? "", HttpMethod.Get) { }
    }

    public class PostRouteAttribute : MethodConstraintedRouteAttribute
    {
        public PostRouteAttribute(string template)
            : base(template ?? "", HttpMethod.Post) { }
    }

    public class PutRouteAttribute : MethodConstraintedRouteAttribute
    {
        public PutRouteAttribute(string template)
            : base(template ?? "", HttpMethod.Put) { }
    }

    public class DeleteRouteAttribute : MethodConstraintedRouteAttribute
    {
        public DeleteRouteAttribute(string template)
            : base(template ?? "", HttpMethod.Delete) { }
    }
}
