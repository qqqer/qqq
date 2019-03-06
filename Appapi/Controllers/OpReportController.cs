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
        /// <summary>
        /// 开始作业申请
        /// </summary>
        /// <param name="values">工单号~阶层号~工序序号~工序代码</param>
        /// <returns>工单号~阶层号~工序序号~工序代码~工序描述~NextJobSeq~NextOpCode~NextOpDesc~startdate~累计已报数量</returns>
        //Get:  /api/OpReport/StartByQR
        [System.Web.Http.HttpGet]
        public string Start(string values) // ApiNum 101
        {
            //throw new HttpResponseException(HttpStatusCode.Forbidden);
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            string res = OpReportRepository.Start(values);

            return res.Substring(0, 1).Trim() == "1" ? res : "101|" + res;
        }



        /// <summary>
        /// 作业完成报工申请
        /// </summary>
        /// <param name="ReportInfo">json</param>
        /// <returns>处理成功或错误提示</returns>
        //Post:  /api/OpReport/ReporterCommit
        [System.Web.Http.HttpPost]
        public string ReporterCommit(OpReport ReportInfo) // ApiNum 102
        {
            //throw new HttpResponseException(HttpStatusCode.Forbidden);
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            string res = OpReportRepository.ReporterCommit(ReportInfo);

            return res == "处理成功" ? res : "102|" + res;
        }


        /// <summary>
        /// 品检提交
        /// </summary>
        /// <param name="CheckInfo">json</param>
        /// <returns>处理成功或错误提示</returns>
        //Post:  /api/OpReport/CheckerCommit
        [System.Web.Http.HttpPost]
        public string CheckerCommit(OpReport CheckInfo) // ApiNum 201
        {
            //throw new HttpResponseException(HttpStatusCode.Forbidden);
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            string res = OpReportRepository.CheckerCommit(CheckInfo);

            return res == "处理成功" ? res : "201|" + res;
        }


        /// <summary>
        /// 主流程或子流程的转序提交
        /// </summary>
        /// <param name="TransmitInfo">json</param>
        /// <returns>处理成功或错误提示</returns>
        //Post:  /api/OpReport/TransferCommit
        [System.Web.Http.HttpPost]
        public string TransferCommit(OpReport TransmitInfo) // ApiNum 301 or 302
        {
            //throw new HttpResponseException(HttpStatusCode.Forbidden);
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            string res = OpReportRepository.TransferCommit(TransmitInfo);
            string apinum = (bool)TransmitInfo.IsSubProcess ? "302|" : "301|";

            return res == "处理成功" ? res : apinum + res;
        }


        /// <summary>
        /// 主流程或子流程的接收提交
        /// </summary>
        /// <param name="AcceptInfo">json</param>
        /// <returns>处理成功或错误提示</returns>
        //Post:  /api/OpReport/AccepterCommit
        [System.Web.Http.HttpPost]
        public string AccepterCommit(OpReport AcceptInfo) // ApiNum 401 or 402
        {
            //throw new HttpResponseException(HttpStatusCode.Forbidden);
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            string res = OpReportRepository.AccepterCommit(AcceptInfo);

            string apinum = (bool)AcceptInfo.IsSubProcess ? "402|" : "401|";
            return res == "处理成功" ? res : apinum + res;
        }



        /// <summary>
        /// dmr提交
        /// </summary>
        /// <param name="DMRInfo">json</param>
        /// <returns>处理成功或错误提示</returns>
        //Post:  /api/OpReport/DMRCommit
        [System.Web.Http.HttpPost]
        public string DMRCommit(OpReport DMRInfo) // ApiNum 601
        {
            //throw new HttpResponseException(HttpStatusCode.Forbidden);
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            string res = OpReportRepository.DMRCommit(DMRInfo);

            return res == "处理成功" ? res : "601|"+ res;
        }


        /// <summary>
        /// 获取属于该请求用户的正在进行的作业信息
        /// </summary>
        /// <returns>返回属于该请求用户的正在进行的作业信息：工单号~阶层号~工序序号~工序代码~工序描述~NextJobSeq~NextOpCode~NextOpDesc~startdate~累计已报数量
        /// 若无正在进行的作业 则返回null</returns>
        //Get:  /api/OpReport/GetProcessOfUser     
        [System.Web.Http.HttpGet]
        public string GetProcessOfUser() // ApiNum 1      null:未进行工序    0|：错误   1|：解析
        {
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            string res = OpReportRepository.GetProcessOfUser();

            return res == null || res.Substring(0, 1).Trim() == "1"  ? res : res + "|1";
        }


        /// <summary>
        /// 获取该工单所有AssemblySeq, PartNum, 物料描述
        /// </summary>
        /// <param name="JobNum"></param>
        /// <returns>返回该工单所有AssemblySeq, PartNum, 物料Description</returns>
        //Get:  /api/OpReport/GetAssemblySeqByJobNum
        [System.Web.Http.HttpGet]
        public DataTable GetAssemblySeqByJobNum(string JobNum) // ApiNum 2     
        {
            return  OpReportRepository.GetAssemblySeqByJobNum(JobNum);
        }


        /// <summary>
        /// 获取参数锁定的工序的信息OprSeq,OpDesc,OpCode, erp.joboper.QtyCompleted
        /// </summary>
        /// <param name="JobNum"></param>
        /// <param name="AssemblySeq"></param>
        /// <returns>返回erp.joboper中参数锁定的所有工序的OprSeq,OpDesc,OpCode, QtyCompleted字段值</returns>
        //Get:  /api/OpReport/GetJobSeq
        [System.Web.Http.HttpGet]
        public DataTable GetJobSeq(string JobNum, int AssemblySeq) // ApiNum 3    
        {
            return OpReportRepository.GetJobSeq(JobNum, AssemblySeq);
        }


        /// <summary>
        /// 获取属于请求账号的所有代表事项（不包括dmr待办事项）
        /// </summary>
        /// <returns>返回属于请求账号的所有代办事项（不包括dmr待办事项）</returns>
        [HttpGet]
        //Get:  /api/OpReport/GetRemainsOfUser
        public IEnumerable<OpReport> GetRemainsOfUser()//ApiNum: 4   获取当前用户的待办事项
        {
            return HttpContext.Current.Session.Count != 0 ? OpReportRepository.GetRemainsOfUser() : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }


        /// <summary>
        /// 获取参数指定的记录的所有字段
        /// </summary>
        /// <param name="ID">若无负号代表bpm ID， 若有负号代表BPMSub Id</param>
        /// <returns>返回该条记录的所有字段</returns>
        [HttpGet]
        //Get:  /api/OpReport/GetRecordByID
        public DataTable GetRecordByID(int ID)//ApiNum: 5   从bpm表中获取ID指定的记录行
        {
            return HttpContext.Current.Session.Count != 0 ? OpReportRepository.GetRecordByID(ID) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }


        /// <summary>
        /// 主流程 二选三，三选四  重载版本
        /// </summary>
        /// <param name="OpCode"></param>
        /// <param name="ID"></param>
        /// <param name="pa">可选参数</param>
        /// <returns>返回下步可选办理人</returns>
        [HttpGet]
        //Get:  /api/OpReport/GetNextUserGroup 
        public DataTable GetNextUserGroup(string OpCode, int ID, params string[] pa)//ApiNum: 6   
        {
            return HttpContext.Current.Session.Count != 0 ? OpReportRepository.GetNextUserGroup(OpCode, ID, null) : throw new HttpResponseException(HttpStatusCode.Forbidden);
            //return OpReportRepository.GetNextUserGroup(OpCode, ID);
        }


        /// <summary>
        /// 主流程 一选二 需要工单号 重载版本
        /// </summary>
        /// <param name="OpCode"></param>
        /// <param name="ID"></param>
        /// <param name="JobNum">传工单号</param>
        /// <returns></returns>
        [HttpGet]
        //Get:  /api/OpReport/GetNextUserGroup 
        public DataTable GetNextUserGroup(string OpCode, int ID, string JobNum)//ApiNum: 6  
        {
            return HttpContext.Current.Session.Count != 0 ? OpReportRepository.GetNextUserGroup(OpCode, ID, JobNum) : throw new HttpResponseException(HttpStatusCode.Forbidden);
            //return OpReportRepository.GetNextUserGroup(OpCode, ID);
        }



        /// <summary>
        /// 返回erp.Reason表中原因代码和原因描述
        /// </summary>
        /// <param name="type">erp.Reason表中的ReasonType</param>
        /// <returns>ReasonCode, Description字段</returns>
        [HttpGet]
        //Get:  /api/OpReport/GetReason
        public DataTable GetReason(string type)//ApiNum: 8   
        {
            return OpReportRepository.GetReason(type);
        }


        /// <summary>
        /// dmr选三
        /// </summary>
        /// <param name="OpCode"></param>
        /// <param name="ID">bpm表 id</param>
        /// <returns>返回下步可选办理人</returns>
        [HttpGet]
        //Get:  /api/OpReport/GetDMRNextUserGroup
        public DataTable GetDMRNextUserGroup(string OpCode, int ID)//ApiNum: 9   
        {
            return HttpContext.Current.Session.Count != 0 ? OpReportRepository.GetDMRNextUserGroup(OpCode, ID) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }


        /// <summary>
        /// 为不良品小组账号获取所有报工不良dmr待办事项
        /// </summary>
        /// <returns>返回所有报工不良dmr待办事项</returns>
        [HttpGet]
        //Get:  /api/OpReport/GetDMRRemainsOfUser
        public IEnumerable<OpReport> GetDMRRemainsOfUser()//ApiNum: 10   获取当前用户的待办事项
        {
            return HttpContext.Current.Session.Count != 0 ? OpReportRepository.GetDMRRemainsOfUser() : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }


        /// <summary>
        /// 子流程 三选四
        /// </summary>
        /// <param name="ID">bpmsub id</param>
        /// <returns>返回下步可选办理人</returns>
        [HttpGet]
        //Get:  /api/OpReport/GetNextUserGroupOfSub 
        public DataTable GetNextUserGroupOfSub(int ID)//ApiNum: 11  子流程选人 
        {
            return HttpContext.Current.Session.Count != 0 ? OpReportRepository.GetNextUserGroupOfSub(ID) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }


        /// <summary>
        /// 强制清除该账号当前开始的工序
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        //Get:  /api/OpReport/ClearProcess
        public string ClearProcess() //ApiNum: 12 强制清空当前开始的工序
        {
            OpReportRepository.ClearProcess();
            return "取消成功";
        }



        /// <summary>
        /// 获取要打印的记录的所有字段，工单号必须，阶层号和工序号可选
        /// </summary>
        /// <param name="JobNum"></param>
        /// <param name="AssemblySeq"></param>
        /// <param name="JobSeq"></param>
        /// <returns>返回每条记录的所有字段值</returns>
        [HttpGet]     
        //Get:  /api/OpReport/GetRecordsForPrint
        public IEnumerable<OpReport> GetRecordsForPrint(string JobNum, int? AssemblySeq, int? JobSeq) //ApiNum: 13 获取可选的打印记录集合
        {
            return OpReportRepository.GetRecordsForPrint(JobNum, AssemblySeq, JobSeq);
        }


        /// <summary>
        /// 复制主流程二维码
        /// </summary>
        /// <param name="id">bpm id</param>
        /// <param name="printqty">打印数量</param>
        /// <returns>处理成功或错误提示</returns>
        [HttpGet]
        //Get:  /api/OpReport/PrintQR
        public string PrintQR(int id, int printqty) //ApiNum: 14 打印复制二维码
        {
            return  OpReportRepository.PrintQR(id, printqty);
        }


        /// <summary>
        /// 扫码获取主流程的信息，提取参数中的PrintID，找到在bpm表中匹配的记录
        /// </summary>
        /// <param name="values">公司ID%物料编码 % 描述 % 批次号 %《工单号 % 半成品号 % 标签识别码 % PrintID % 订单号 % 行号 % 交货数量 % 发货行%工序号%炉批号</param>
        /// <returns>返回ScanResult对象， 若成功则ScanResult.error为null，ScanResult.batch不为null， 否则ScanResult.error != null，ScanResult.batch为null</returns>
        [HttpGet]
        //Get:  /api/OpReport/GetRecordByQR
        public ScanResult GetRecordByQR(string values) //ApiNum: 15
        {
            return HttpContext.Current.Session.Count != 0 ? OpReportRepository.GetRecordByQR(values) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }


        /// <summary>
        /// 二节点强制删除当前报工流程
        /// </summary>
        /// <param name="ID">bpm id</param>
        /// <returns>"删除成功"</returns>
        [HttpGet]
        //Get:  /api/OpReport/DeleteProcess
        public string DeleteProcess(int ID) //ApiNum: 16 二节点强制删除当前报工流程
        {
            OpReportRepository.DeleteProcess(ID);
            return "删除成功";
        }



        /// <summary>
        /// 获取包含该子串的所有工单号
        /// </summary>
        /// <param name="JobNum">工单号子串</param>
        /// <returns>返回包含该子串的所有工单号</returns>
        [HttpGet]
        //Get:  /api/OpReport/GetRelatedJobNum
        public DataTable GetRelatedJobNum(string JobNum) //ApiNum: 17 所有返修工单
        {
            return OpReportRepository.GetRelatedJobNum(JobNum);
        }
    }
}
