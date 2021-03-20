using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Commons
{
    public class HttpBytesResponse:IHttpResponse
    {
        public string Header { get; set; } = "";
        public byte[] Body { get; set; }
        public int Status { get; set; }
        public string ContentType { get; set; }
    }
}
