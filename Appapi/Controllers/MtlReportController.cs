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
        [System.Web.Http.HttpGet]
        public string ReportCommit(OpReport CreateInfo) // ApiNum 101   
        {
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            string res = MtlReportRepository.ReportCommit(CreateInfo);

            return res == "处理成功" ? res : res + "|101";
        }
    }
}