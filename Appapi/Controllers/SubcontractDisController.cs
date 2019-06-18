using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Appapi.Models;

namespace Appapi.Controllers
{
    public class SubcontractDisController : ApiController
    {
        [HttpPost]
        //Post:  /api/SubcontractDis/ReceiveSubcontractDisQty
        public string ReceiveSubcontractDisQty(SubcontractDis sd)//ApiNum: 101  对外协不良进行收货
        {
            string res = SubcontractDisRepository.ReceiveSubcontractDisQty(sd);
            return res == "处理成功" ? "101|" + res : res;
        }

        [HttpPost]
        //Post:  /api/SubcontractDis/DMRCommit
        public string DMRCommit(SubcontractDis sd)//ApiNum: 201  对外协不良进行DMR
        {
            string res = SubcontractDisRepository.DMRCommit(sd);
            return res == "处理成功" ? "201|" + res : res;
        }

        [HttpPost]
        //Post:  /api/SubcontractDis/AccepterCommitOfSub
        public string AccepterCommitOfSub(SubcontractDis sd)//ApiNum: 301  对外协不良流程进行结束
        {
            string res = SubcontractDisRepository.AccepterCommitOfSub(sd);
            return res == "处理成功" ? "301|" + res : res;
        }

        [HttpGet]
        //Get:  /api/SubcontractDis/GetRemainsOfUser
        public IEnumerable<SubcontractDis> GetRemainsOfUser()//ApiNum: 1     
        {
            return SubcontractDisRepository.GetDMRRemainsOfUser();
        }

        [HttpGet]
        //Get:  /api/SubcontractDis/GetNextUserGroup
        public DataTable GetNextUserGroup(int s_id)//ApiNum: 2   
        {
            return SubcontractDisRepository.GetNextUserGroup(s_id);
        }
    }
}
