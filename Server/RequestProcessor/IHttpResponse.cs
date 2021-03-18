using System;
using System.Collections.Generic;
using System.Text;

namespace Server.RequestProcessorLib
{
    public interface IHttpResponse
    {
        public string Header { get; }
        public byte[] Body { get; }
        public int Status { get; }
        public string ContentType { get; }
    }
}
