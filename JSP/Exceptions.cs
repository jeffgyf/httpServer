using System;
using System.Collections.Generic;
using System.Text;

namespace Server.JSP
{
    public class NotFoundException:Exception
    {
        public NotFoundException(string msg) : base(msg) 
        { 
        }
    }
}
