using Appapi.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace Appapi.Controllers
{
    public class MtlReportController : ApiController
    {
        //Get:  /api/MtlReport/GetMtlInfo
        [System.Web.Http.HttpGet]
        public DataTable GetMtlInfo(string JobNum, int AssemblySeq) // ApiNum 1     
        {
            return MtlReportRepository.GetMtlInfo(JobNum, AssemblySeq);
        }


      
        //Get:  /api/MtlReport/GetPartLots
        [System.Web.Http.HttpGet]
        public DataTable GetPartLots(string PartNum) // ApiNum 3     
        {
            return MtlReportRepository.GetPartLots(PartNum);
        }


        //Post:  /api/MtlReport/ReportCommit
        [System.Web.Http.HttpPost]
        public string ReportCommit(OpReport CreateInfo) // ApiNum 101   
        {
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            string res = MtlReportRepository.ReportCommit(CreateInfo);

            return res == "处理成功" ? res : res + "|101";
        }


        //Post:  /api/MtlReport/DMRCommit
        [System.Web.Http.HttpPost]
        public string DMRCommit(OpReport DMRInfo) // ApiNum 201
        {
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            string res = MtlReportRepository.DMRCommit(DMRInfo);

            return res == "处理成功" ? res : res + "|201";
        }


        //Post:  /api/MtlReport/TransferCommit
        [System.Web.Http.HttpPost]
        public string TransferCommit(OpReport TransferInfo) // ApiNum 301   
        {
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            string res = MtlReportRepository.TransferCommit(TransferInfo);

            return res == "处理成功" ? res : res + "|301";
        }


        //Post:  /api/MtlReport/AcceptCommit
        [System.Web.Http.HttpPost]
        public string AcceptCommit(OpReport AcceptInfo) // ApiNum 401   
        {
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            string res = MtlReportRepository.AcceptCommit(AcceptInfo);

            return res == "处理成功" ? res : res + "|401";
        }


        [HttpGet]
        //Get:  /api/MtlReport/GetRemainsOfUser
        public IEnumerable<OpReport> GetRemainsOfUser()
        {
            return HttpContext.Current.Session.Count != 0 ? MtlReportRepository.GetRemainsOfUser() : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }


        [HttpGet]
        //Get:  /api/MtlReport/GetDMRRemainsOfUser
        public IEnumerable<OpReport> GetDMRRemainsOfUser()
        {
            return HttpContext.Current.Session.Count != 0 ? MtlReportRepository.GetDMRRemainsOfUser() : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }


        [HttpGet]
        //Get:  /api/MtlReport/GetNextUserGroup 
        public DataTable GetNextUserGroup(int ID, bool IsSubProcess)
        {
            return HttpContext.Current.Session.Count != 0 ? MtlReportRepository.GetNextUserGroup(ID, IsSubProcess) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }


        [HttpGet]
        //Get:  /api/MtlReport/GetRecordByID
        public DataTable GetRecordByID(int ID)//
        {
            return HttpContext.Current.Session.Count != 0 ? MtlReportRepository.GetRecordByID(ID) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }


        //[HttpGet]
        ////Get:  /api/MtlReport/Get1
        //public IEnumerable<Receipt> Get1()//获取当前用户的待办事项
        //{
        //    string sql = @"select top 3 * from Receipt where status = 3";
        //    DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql);
        //    List<Receipt> RBs = CommonRepository.DataTableToList<Receipt>(dt);
        //    return RBs;
        //}

        //[HttpGet]
        ////Get:  /api/MtlReport/Get2
        //public IEnumerable<OpReport> Get2()//获取当前用户的待办事项
        //{
        //    string sql = @"select top 3 * from bpmsub";
        //    DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql);
        //    List<OpReport> RBs = CommonRepository.DataTableToList<OpReport>(dt);
        //    return RBs;
        //}

    }
}