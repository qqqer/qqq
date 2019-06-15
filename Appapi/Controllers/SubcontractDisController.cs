using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Appapi.Controllers
{
    public class SubcontractDisController : ApiController
    {
        [HttpPost]
        //Post:  /api/Receipt/ReceiveSubcontractDisQty
        public string ReceiveSubcontractDisQty()//ApiNum: 101  对外协不良进行收货
        {
            string res =
               return res == "处理成功" ? "701|" + res : res;
        }
    }
}
