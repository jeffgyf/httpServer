using Server.Commons;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Server.JSP
{
    public class TestController:ControllerBase
    {
        [GetMethod]
        [Route("/ok")]
        public async Task<IHttpResponse> Ok(HttpRequest request) 
        {
            return new HttpStringResponse { Body = "ok", Status = 200 };
        }

        [GetMethod]
        [Route("/echo")]
        public async Task<IHttpResponse> Echo(HttpRequest request)
        {
            return new HttpStringResponse { Body = request.Uri, Status = 200 };
        }
    }
}
