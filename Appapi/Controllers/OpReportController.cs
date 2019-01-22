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
            //throw new HttpResponseException(HttpStatusCode.Forbidden);
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            string res = OpReportRepository.Start(values);

            return res.Substring(0, 1).Trim() == "1" ? res : res + "|101";
        }



        //Post:  /api/OpReport/ReporterCommit
        [System.Web.Http.HttpPost]
        public string ReporterCommit(OpReport ReportInfo) // ApiNum 102
        {
            //throw new HttpResponseException(HttpStatusCode.Forbidden);
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            string res = OpReportRepository.ReporterCommit(ReportInfo);

            return res == "处理成功" ? res : res + "|102";
        }



        //Post:  /api/OpReport/CheckerCommit
        [System.Web.Http.HttpPost]
        public string CheckerCommit(OpReport CheckInfo) // ApiNum 201
        {
            //throw new HttpResponseException(HttpStatusCode.Forbidden);
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            string res = OpReportRepository.CheckerCommit(CheckInfo);

            return res == "处理成功" ? res : res + "|201";
        }


        //Post:  /api/OpReport/TransferCommit
        [System.Web.Http.HttpPost]
        public string TransferCommit(OpReport TransmitInfo) // ApiNum 301 or 302
        {
            //throw new HttpResponseException(HttpStatusCode.Forbidden);
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            string res = OpReportRepository.TransferCommit(TransmitInfo);
            string apinum = (bool)TransmitInfo.IsSubProcess ? "|302" : "|301";

            return res == "处理成功" ? res : res + apinum;
        }



        //Post:  /api/OpReport/AccepterCommit
        [System.Web.Http.HttpPost]
        public string AccepterCommit(OpReport AcceptInfo) // ApiNum 401 or 402
        {
            //throw new HttpResponseException(HttpStatusCode.Forbidden);
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            string res = OpReportRepository.AccepterCommit(AcceptInfo);

            string apinum = (bool)AcceptInfo.IsSubProcess ? "|402" : "|401";
            return res == "处理成功" ? res : res + apinum;
        }



        //Post:  /api/OpReport/DMRCommit
        [System.Web.Http.HttpPost]
        public string DMRCommit(OpReport DMRInfo) // ApiNum 601
        {
            //throw new HttpResponseException(HttpStatusCode.Forbidden);
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            string res = OpReportRepository.DMRCommit(DMRInfo);

            return res == "处理成功" ? res : res + "|601";
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
        public DataTable GetNextUserGroup(string OpCode, int ID, params string[] pa)//ApiNum: 6   
        {
            return HttpContext.Current.Session.Count != 0 ? OpReportRepository.GetNextUserGroup(OpCode, ID, null) : throw new HttpResponseException(HttpStatusCode.Forbidden);
            //return OpReportRepository.GetNextUserGroup(OpCode, ID);
        }


        [HttpGet]
        //Get:  /api/OpReport/GetNextUserGroup 
        public DataTable GetNextUserGroup(string OpCode, int ID, string JobNum)//ApiNum: 6  
        {
            return HttpContext.Current.Session.Count != 0 ? OpReportRepository.GetNextUserGroup(OpCode, ID, JobNum) : throw new HttpResponseException(HttpStatusCode.Forbidden);
            //return OpReportRepository.GetNextUserGroup(OpCode, ID);
        }



        [HttpGet]
        //Get:  /api/OpReport/GetReason
        public DataTable GetReason(string type)//ApiNum: 8   
        {
            return OpReportRepository.GetReason(type);
        }


        [HttpGet]
        //Get:  /api/OpReport/GetDMRNextUserGroup
        public DataTable GetDMRNextUserGroup(string OpCode, int ID)//ApiNum: 9   
        {
            return HttpContext.Current.Session.Count != 0 ? OpReportRepository.GetDMRNextUserGroup(OpCode, ID) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }


        [HttpGet]
        //Get:  /api/OpReport/GetDMRRemainsOfUser
        public IEnumerable<OpReport> GetDMRRemainsOfUser()//ApiNum: 10   获取当前用户的待办事项
        {
            return HttpContext.Current.Session.Count != 0 ? OpReportRepository.GetDMRRemainsOfUser() : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }


        [HttpGet]
        //Get:  /api/OpReport/GetNextUserGroupOfSub 
        public DataTable GetNextUserGroupOfSub(int ID)//ApiNum: 11  子流程选人 
        {
            return HttpContext.Current.Session.Count != 0 ? OpReportRepository.GetNextUserGroupOfSub(ID) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }

        [HttpGet]
        //Get:  /api/OpReport/ClearProcess
        public string ClearProcess() //ApiNum: 12 强制清空当前开始的工序
        {
            OpReportRepository.ClearProcess();
            return "取消成功";
        }


        [HttpGet]
        //Get:  /api/OpReport/GetRecordsForPrint
        public IEnumerable<OpReport> GetRecordsForPrint(string JobNum, int? AssemblySeq, int? JobSeq) //ApiNum: 13 获取可选的打印记录集合
        {
            return OpReportRepository.GetRecordsForPrint(JobNum, AssemblySeq, JobSeq);
        }


        [HttpGet]
        //Get:  /api/OpReport/PrintQR
        public string PrintQR(int id, int printqty) //ApiNum: 14 打印复制二维码
        {
            return  OpReportRepository.PrintQR(id, printqty);
        }


        [HttpGet]
        //Get:  /api/OpReport/GetRecordByQR
        public ScanResult GetRecordByQR(string values) //ApiNum: 15
        {
            return HttpContext.Current.Session.Count != 0 ? OpReportRepository.GetRecordByQR(values) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }


        [HttpGet]
        //Get:  /api/OpReport/DeleteProcess
        public string DeleteProcess(int ID) //ApiNum: 16 二节点强制删除当前报工流程
        {
            OpReportRepository.DeleteProcess(ID);
            return "删除成功";
        }


        [HttpGet]
        //Get:  /api/OpReport/GetRelatedJobNum
        public DataTable GetRelatedJobNum(string JobNum) //ApiNum: 17 所有返修工单
        {
            return OpReportRepository.GetRelatedJobNum(JobNum);
        }
    }
}
