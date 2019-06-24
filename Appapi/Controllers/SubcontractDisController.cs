using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
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
            return res == "处理成功" ?  res : "101|" + res;
        }

        [HttpPost]
        //Post:  /api/SubcontractDis/DMRCommit
        public string DMRCommit(SubcontractDis sd)//ApiNum: 201  对外协不良进行DMR
        {
            string res = SubcontractDisRepository.DMRCommit(sd);
            return res == "处理成功" ?  res : "201|" + res;
        }

        [HttpPost]
        //Post:  /api/SubcontractDis/TransferCommitOfSub
        public string TransferCommitOfSub(SubcontractDis sd)//ApiNum: 301  
        {
            string res = SubcontractDisRepository.TransferCommitOfSub(sd);
            return res == "处理成功" ?  res : "301|" + res;
        }


        [System.Web.Http.HttpPost]
        //Post:  /api/SubcontractDis/AccepterCommitOfSub
        public string AccepterCommitOfSub(SubcontractDis sd) // ApiNum 401
        {
            string res = SubcontractDisRepository.AccepterCommitOfSub(sd);
            return res == "处理成功" ? res : "401|" + res;
        }


        [HttpGet]
        //Get:  /api/SubcontractDis/GetDMRRemainsOfUser
        public IEnumerable<SubcontractDis> GetDMRRemainsOfUser()//ApiNum: 1     
        {
            return SubcontractDisRepository.GetDMRRemainsOfUser();
        }

        [HttpGet]
        //Get:  /api/SubcontractDis/GetRemainsOfUser
        public IEnumerable<SubcontractDis> GetRemainsOfUser()//ApiNum: 2    
        {
            return SubcontractDisRepository.GetRemainsOfUser();
        }


        [HttpGet]
        //Get:  /api/SubcontractDis/GetTransferUserGroup
        public DataTable GetTransferUserGroup(int m_id)//ApiNum:   3
        {
            return SubcontractDisRepository.GetTransferUserGroup(m_id);
        }

        [HttpGet]
        //Get:  /api/SubcontractDis/GetAccepterUserGroup
        public DataTable GetAccepterUserGroup(int s_id)//ApiNum:   4
        {
            return SubcontractDisRepository.GetAccepterUserGroup(s_id);
        }

        [HttpGet]
        //Get:  /api/SubcontractDis/GetRecordByID
        public DataTable GetRecordByID(int id)//ApiNum: 5   从bpm表中获取ID指定的记录行
        {
            return HttpContext.Current.Session.Count != 0 ? SubcontractDisRepository.GetRecordByID(id) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }
    }
}
