using System;
using System.Collections.Generic;
using System.Text;

namespace Server.RequestProcessorLib
{
    public class HttpStringResponse:IHttpResponse
    {
        public string Body;

        public string Header { get; set; } = "";
        byte[] IHttpResponse.Body { get => Encoding.UTF8.GetBytes(this.Body); }
        public int Status { get; set; }
        public string ContentType { get; set; } = "text/html";

    }
}
