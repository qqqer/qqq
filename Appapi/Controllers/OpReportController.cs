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
    public class OpReportController : ApiController
    {
        //Get:  /api/OpReport/StartByQR
        [System.Web.Http.HttpGet]
        public string Start(string values) // ApiNum 101
        {
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            string res = OpReportRepository.Start(values);

            return res.Substring(0, 1).Trim() == "1" ? res : res + "|101";
        }



        //Post:  /api/OpReport/ReporterCommit
        [System.Web.Http.HttpPost]
        public string ReporterCommit(OpReport ReportInfo) // ApiNum 102
        {
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            string res = OpReportRepository.ReporterCommit(ReportInfo);

            return res == "处理成功" ? res : res + "|102";
        }



        //Post:  /api/OpReport/CheckerCommit
        [System.Web.Http.HttpPost]
        public string CheckerCommit(OpReport CheckInfo) // ApiNum 201
        {
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            string res = OpReportRepository.CheckerCommit(CheckInfo);

            return res == "处理成功" ? res : res + "|201";
        }


        //Post:  /api/OpReport/TransferCommit
        [System.Web.Http.HttpPost]
        public string TransferCommit(OpReport TransmitInfo) // ApiNum 301
        {
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            string res = OpReportRepository.TransferCommit(TransmitInfo);

            return res == "处理成功" ? res : res + "|301";
        }



        //Post:  /api/OpReport/AccepterCommit
        [System.Web.Http.HttpPost]
        public string AccepterCommit(OpReport AcceptInfo) // ApiNum 401
        {
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            string res = OpReportRepository.AccepterCommit(AcceptInfo);

            return res == "处理成功" ? res : res + "|401";
        }


        //Get:  /api/OpReport/GetProcessOfUser     
        [System.Web.Http.HttpGet]
        public string GetProcessOfUser() // ApiNum 1      null:未进行工序    0|：错误   1|：解析
        {
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            string res = OpReportRepository.GetProcessOfUser();

            return res == null || res.Substring(0, 1).Trim() == "1"  ? res : res + "|1";
        }


        //Get:  /api/OpReport/GetAssemblySeqByJobNum
        [System.Web.Http.HttpGet]
        public DataTable GetAssemblySeqByJobNum(string JobNum) // ApiNum 2     
        {
            return  OpReportRepository.GetAssemblySeqByJobNum(JobNum);
        }


        //Get:  /api/OpReport/GetJobSeq
        [System.Web.Http.HttpGet]
        public DataTable GetJobSeq(string JobNum, int AssemblySeq) // ApiNum 3    
        {
            return OpReportRepository.GetJobSeq(JobNum, AssemblySeq);
        }


        [HttpGet]
        //Get:  /api/OpReport/GetRemainsOfUser
        public IEnumerable<OpReport> GetRemainsOfUser()//ApiNum: 4   获取当前用户的待办事项
        {
            return HttpContext.Current.Session.Count != 0 ? OpReportRepository.GetRemainsOfUser() : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }


        [HttpGet]
        //Get:  /api/OpReport/GetRecordByID
        public DataTable GetRecordByID(int ID)//ApiNum: 5   从bpm表中获取ID指定的记录行
        {
            return HttpContext.Current.Session.Count != 0 ? OpReportRepository.GetRecordByID(ID) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }


        [HttpGet]
        //Get:  /api/OpReport/GetNextUserGroup 
        public DataTable GetNextUserGroup(string OpCode, int ID)//ApiNum: 6   
        {
            return HttpContext.Current.Session.Count != 0 ? OpReportRepository.GetNextUserGroup(OpCode, ID) : throw new HttpResponseException(HttpStatusCode.Forbidden);
            //return OpReportRepository.GetNextUserGroup(OpCode, ID);
        }


        [HttpGet]
        //Get:  /api/OpReport/GetRecordByQR 
        public ScanResult GetRecordByQR(string values)//ApiNum: 7   
        {
            return HttpContext.Current.Session.Count != 0 ? OpReportRepository.GetRecordByQR(values) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }


        [HttpGet]
        //Get:  /api/OpReport/GetReason
        public DataTable GetReason(string type)//ApiNum: 8   
        {
            return OpReportRepository.GetReason(type);
        }
    }
}
