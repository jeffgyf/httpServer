using System;
using System.Collections.Generic;
using System.Text;

namespace Server.JSP
{
    public enum HttpMethod
    {
        GET,
        POST
    }
    public abstract class HttpMethodAttribute : Attribute 
    {
        public abstract HttpMethod Method { get; }
    }
    public class GetMethodAttribute : HttpMethodAttribute
    {
        public override HttpMethod Method => HttpMethod.GET;
    }

    public class PostMethodAttribute : HttpMethodAttribute
    {
        public override HttpMethod Method => HttpMethod.POST;
    }

    public class RouteAttribute: Attribute
    {
        public readonly string Path;
        public RouteAttribute(string path) 
        {
            this.Path = path;
        }
    }
}
