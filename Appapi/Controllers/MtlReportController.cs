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
        /// <summary>
        /// 获取物料序号,PartNum,物料描述,JobNum,AssemblySeq
        /// </summary>
        /// <param name="JobNum"></param>
        /// <param name="AssemblySeq"></param>
        /// <returns>返回MtlSeq,PartNum,Description,JobNum,AssemblySeq</returns>
        [System.Web.Http.HttpGet]
        public DataTable GetMtlInfo(string JobNum, int AssemblySeq) // ApiNum 1     
        {
            return MtlReportRepository.GetMtlInfo(JobNum, AssemblySeq);
        }


        /// <summary>
        /// 获取参数锁定的物料的所有批次号
        /// </summary>
        /// <param name="PartNum"></param>
        /// <param name="MtlSeq"></param>
        /// <param name="JobNum"></param>
        /// <param name="AssemblySeq"></param>
        /// <returns>返回参数锁定的物料的所有批次号</returns>
        //Get:  /api/MtlReport/GetPartLots
        [System.Web.Http.HttpGet]
        public DataTable GetPartLots(string PartNum, int MtlSeq, string JobNum, int AssemblySeq) // ApiNum 3     
        {
            return MtlReportRepository.GetPartLots(PartNum, MtlSeq, JobNum, AssemblySeq);
        }


        /// <summary>
        /// 物料不良一节点提交接口
        /// </summary>
        /// <param name="CreateInfo">json串</param>
        /// <returns>处理成功或错误提示</returns>
        //Post:  /api/MtlReport/ReportCommit
        [System.Web.Http.HttpPost]
        public string ReportCommit(OpReport CreateInfo) // ApiNum 101   
        {
            //throw new HttpResponseException(HttpStatusCode.Forbidden);
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            string res = MtlReportRepository.ReportCommit(CreateInfo);

            return res == "处理成功" ? res : res + "|101";
        }


        /// <summary>
        /// 物料不良dmr提交接口
        /// </summary>
        /// <param name="DMRInfo">json串</param>
        /// <returns>处理成功或错误提示</returns>
        //Post:  /api/MtlReport/DMRCommit
        [System.Web.Http.HttpPost]
        public string DMRCommit(OpReport DMRInfo) // ApiNum 201
        {
            //throw new HttpResponseException(HttpStatusCode.Forbidden);
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            string res = MtlReportRepository.DMRCommit(DMRInfo);

            return res == "处理成功" ? res : res + "|201";
        }


        /// <summary>
        /// 物料不良转序三节点提交接口
        /// </summary>
        /// <param name="TransferInfo">json</param>
        /// <returns>处理成功或错误提示</returns>
        //Post:  /api/MtlReport/TransferCommit
        [System.Web.Http.HttpPost]
        public string TransferCommit(OpReport TransferInfo) // ApiNum 301   
        {
            //throw new HttpResponseException(HttpStatusCode.Forbidden);
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            string res = MtlReportRepository.TransferCommit(TransferInfo);

            return res == "处理成功" ? res : res + "|301";
        }


        /// <summary>
        /// 物料不良接收提交接口
        /// </summary>
        /// <param name="AcceptInfo">json</param>
        /// <returns>处理成功或错误提示</returns>
        //Post:  /api/MtlReport/AcceptCommit
        [System.Web.Http.HttpPost]
        public string AcceptCommit(OpReport AcceptInfo) // ApiNum 401   
        {
            //throw new HttpResponseException(HttpStatusCode.Forbidden);
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            string res = MtlReportRepository.AcceptCommit(AcceptInfo);

            return res == "处理成功" ? res : res + "|401";
        }


        /// <summary>
        /// 获取属于请求账号的所有物料不良代表事项（不包括dmr待办事项）
        /// </summary>
        /// <returns>返回属于请求账号的所有物料不良代表事项（不包括dmr待办事项）</returns>
        [HttpGet]
        //Get:  /api/MtlReport/GetRemainsOfUser
        public IEnumerable<OpReport> GetRemainsOfUser()
        {
            return HttpContext.Current.Session.Count != 0 ? MtlReportRepository.GetRemainsOfUser() : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }


        /// <summary>
        /// 获取所有物料不良dmr待办事项
        /// </summary>
        /// <returns>返回所有物料不良dmr待办事项 </returns>
        [HttpGet]
        //Get:  /api/MtlReport/GetDMRRemainsOfUser
        public IEnumerable<OpReport> GetDMRRemainsOfUser()
        {
            return HttpContext.Current.Session.Count != 0 ? MtlReportRepository.GetDMRRemainsOfUser() : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }


        /// <summary>
        /// 获取二选三或三选四的下步可选办理人
        /// </summary>
        /// <param name="ID">MtlReport ID 或 BPMSub Id</param>
        /// <param name="IsSubProcess">true：MtlReport ID， false：BPMSub Id </param>
        /// <returns>返回二选三或三选四的下步可选办理人</returns>
        [HttpGet]
        //Get:  /api/MtlReport/GetNextUserGroup 
        public DataTable GetNextUserGroup(int ID, bool IsSubProcess)
        {
            return HttpContext.Current.Session.Count != 0 ? MtlReportRepository.GetNextUserGroup(ID, IsSubProcess) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }


        /// <summary>
        /// 获取指定记录的所有字段
        /// </summary>
        /// <param name="ID">若无负号代表MtlReport ID， 若有负号代表BPMSub Id</param>
        /// <returns>返回该条记录的所有字段</returns>
        [HttpGet]
        //Get:  /api/MtlReport/GetRecordByID
        public DataTable GetRecordByID(int ID)//
        {
            return HttpContext.Current.Session.Count != 0 ? MtlReportRepository.GetRecordByID(ID) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }
    }
}