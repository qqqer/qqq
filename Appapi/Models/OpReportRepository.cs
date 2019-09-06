using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Configuration;

namespace Appapi.Models
{
    public static class OpReportRepository
    {
        private static readonly object lock_report = new object();
        private static List<int> report_IDs = new List<int>();

        private static readonly object lock_check = new object();
        private static List<int> check_IDs = new List<int>();

        private static readonly object lock_accept_main = new object();
        private static List<int> accept_IDs_main = new List<int>();

        private static readonly object lock_accept_sub = new object();
        private static List<int> accept_IDs_sub = new List<int>();

        private static readonly object BPMPrintIDLock = new object();


        private static string deleteTimeAndCostByBPMID(int bpmid, string plant) //只返回错误信息
        {
            string error = "";

            try
            {
                string ss = @"select *  from  BPMID_LabrSeq where BPMID = " + bpmid + " order by DtlSeq desc ";
                DataTable seq = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, ss);

                if (seq != null)
                {
                    for (int i = 0; i < seq.Rows.Count; i++)
                    {
                        string ret = ErpAPI.OpReportRepository.deleteTimeAndCost((int)seq.Rows[i]["HedSeq"], (int)seq.Rows[i]["DtlSeq"], plant);

                        if (ret != "OK" && ret != "Labor record not found.")
                        {
                            error = ret;
                            return error;
                        }
                        else //OK
                        {
                            ss = "  delete from BPMID_LabrSeq where BPMID = " + bpmid + " and HedSeq = " + seq.Rows[i]["HedSeq"] + " and DtlSeq = " + seq.Rows[i]["DtlSeq"];
                            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, ss, null);
                        }
                    }
                }
                return error;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }



        private static long GetNextRole(int id)
        {
            long nextRole = 0;//2^60

            string sql = "select * from bpm where id = " + id + "";
            var t = CommonRepository.DataTableToList<OpReport>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql));
            OpReport OpInfo = t?.First(); //获取该批次记录

            int nextStatus = (OpInfo != null ? (int)OpInfo.Status : 1) + 1;

            if (nextStatus == 2)
                nextRole = 256;

            else if (nextStatus == 3)
                nextRole = 512;

            else if (nextStatus == 4 || nextStatus == 5) //nextStatus == 5 第四节点获取下工序最新办理人
            {
                string NextSetpInfo = GetNextValidSetpInfo(OpInfo.JobNum, (int)OpInfo.AssemblySeq, (int)OpInfo.JobSeq, "001");
                if (NextSetpInfo.Substring(0, 1).Trim() == "0")
                    return 0;

                //再回写主表
                string[] arr = NextSetpInfo.Split('~');


                if (arr[0] == "仓库") //由仓库接收人员处理 设置8
                    nextRole = 8;
                else if (arr[1].Substring(0, 2) == "WX")//外协
                    nextRole = 16;
                else if (arr[1].Substring(0, 2) != "WX")//厂内
                    nextRole = 128;
            }

            return nextRole;
        }


        private static DataTable GetLatestOprInfo(string jobnum, int asmSeq, int oprseq)
        {
            string sql = @"select jh.Company, Plant,OpDesc, jo.OpCode from erp.JobHead jh left join erp.JobOper jo on jh.Company = jo.Company and jh.JobNum =jo.JobNum where jo.JobNum = '" + jobnum + "' and jo.AssemblySeq = " + asmSeq + " and  jo.OprSeq = " + oprseq + "";
            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);

            return dt;
        }


        public static string CheckBinNum(string company, string binnum, string WarehouseCode)
        {
            string sql = "select count(*) from erp.WhseBin where Company = '{0}' and  WarehouseCode = '{1}' and BinNum = '{2}'";
            sql = string.Format(sql, company, WarehouseCode, binnum);
            int exist = (int)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);

            return exist == 0 ? "错误：库位与仓库不匹配" : "ok";
        }


        private static void InsertConcessionRecord(int Id, decimal DMRQualifiedQty, string TransformUserGroup, int dmrid, string DMRWarehouseCode, string DMRBinNum, string DMRUnQualifiedReason, string Responsibility, string DMRUnQualifiedReasonRemark, string ResponsibilityRemark)
        {
            string sql = @"
                   insert into BPMSub select 
                   StartUser
                  ,[CreateUser]
                  ,'{4}'
                  ,null
                  ,null
                  ,[CreateDate]
                  ,getdate()
                  ,null
                  ,null                 
                  ,[CheckUserGroup]
                  ,[UnQualifiedGroup]
                  ,'{3}'
                  ,null
                  ,[PartNum]
                  ,[PartDesc]
                  ,[JobNum]
                  ,[AssemblySeq]
                  ,[JobSeq]
                  ,[OpCode]
                  ,[OpDesc]
                  ,[FirstQty]
                  ,[NextJobSeq]
                  ,[NextOpCode]
                  ,[NextOpDesc]
                  ,[QualifiedQty]
                  ,[UnQualifiedQty]
                  ,[UnQualifiedReason]
                  ,[StartDate]
                  ,[EndDate]
                  ,AverageEndDate
                  ,[LaborHrs]
                  ,AverageLaborHrs
                  ,0
                  ,0
                  ,3
                  ,2
                  ,[Remark]
                  ,512
                  ,[Plant]
                  ,[Company]
                  ,[IsPrint]
                  ,[PrintID]
                  ,[BinNum]
                  ,null
                  ,[Character05]
                  ,[TranID]
                  ,{5}
                  ,{0}
                  ,null
                  ,null
                  ,'{6}'
                  ,null
                  ,'{7}'
                  ,'{8}'
                  ,{1}
                  ,{2}
                  ,[ReturnThree]
                  ,null
                  ,1
                  ,null
                  ,null
                  ,'{9}'
                  ,[DefectNO]
                  ,[CheckRemark]
,'{10}'
,'{11}'
,'{12}'
,UnQualifiedReasonRemark
,UnQualifiedReasonDesc
             from bpm where id = " + Id + "";

            sql = string.Format(sql, DMRQualifiedQty, Id, 1, TransformUserGroup, HttpContext.Current.Session["UserId"].ToString(), dmrid, DMRUnQualifiedReason, DMRWarehouseCode, DMRBinNum, Responsibility, DMRUnQualifiedReasonRemark, CommonRepository.GetReasonDesc(DMRUnQualifiedReason), ResponsibilityRemark);

            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
        }


        private static void InsertRepairRecord(int Id, decimal DMRRepairQty, string DMRJobNum, int DMRID, string TransformUserGroup, string DMRWarehouseCode, string DMRBinNum, string DMRUnQualifiedReason, string Responsibility, string DMRUnQualifiedReasonRemark, string ResponsibilityRemark)
        {
            string sql = @"
                   insert into BPMSub   select StartUser,[CreateUser]
                  ,'{6}'
                  ,null
                  ,null
                  ,[CreateDate]
                  ,getdate()
                  ,null
                  ,null  
                  ,[CheckUserGroup]
                  ,[UnQualifiedGroup]
                  ,'{5}' 
                  ,null
                  ,[PartNum]
                  ,[PartDesc]
                  ,[JobNum]
                  ,[AssemblySeq]
                  ,[JobSeq]
                  ,[OpCode]
                  ,[OpDesc]
                  ,[FirstQty]
                  ,[NextJobSeq]
                  ,[NextOpCode]
                  ,[NextOpDesc]
                  ,[QualifiedQty]
                  ,[UnQualifiedQty]
                  ,[UnQualifiedReason]
                  ,[StartDate]
                    ,AverageEndDate
                  ,[EndDate]
                  ,[LaborHrs]
                , AverageLaborHrs
                  ,0
                  ,0
                  ,3
                  ,2
                  ,[Remark]
                  ,512
                  ,[Plant]
                  ,[Company]
                  ,[IsPrint]
                  ,[PrintID]
                  ,[BinNum]
                  ,null
                  ,[Character05]
                  ,[TranID]
                  ,{4}
                  ,null
                  ,{0}
                  ,null
                  ,'{7}'
                  ,'{3}'
                  ,'{8}'
                  ,'{9}'
                  ,{1}
                  ,{2}
                  ,[ReturnThree]
                  ,null
                  ,1
                  ,null
                  ,null
                  ,'{10}'
                  ,[DefectNO]
                  ,[CheckRemark]
,'{11}'
,'{12}'
,'{13}'
,UnQualifiedReasonRemark
,UnQualifiedReasonDesc
             from bpm where id = " + Id + "";

            sql = string.Format(sql, DMRRepairQty, Id, 1, DMRJobNum, DMRID, TransformUserGroup, HttpContext.Current.Session["UserId"].ToString(), DMRUnQualifiedReason, DMRWarehouseCode, DMRBinNum, Responsibility, DMRUnQualifiedReasonRemark, CommonRepository.GetReasonDesc(DMRUnQualifiedReason), ResponsibilityRemark);

            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
        }


        public static void InsertDiscardRecord(int Id, decimal DMRUnQualifiedQty, string DMRUnQualifiedReason, int DMRID, string DMRWarehouseCode, string DMRBinNum, string TransformUserGroup, string Responsibility, string DMRUnQualifiedReasonRemark, string ResponsibilityRemark, string opuserid)
        {
            string sql = @"
               insert into BPMSub   select StartUser, [CreateUser]
              ,'{8}'
              ,null
              ,null
              ,[CreateDate]
              ,getdate()
              ,null
              ,null     
              ,[CheckUserGroup]
              ,[UnQualifiedGroup]
              ,'{7}'
              ,null
              ,[PartNum]
              ,[PartDesc]
              ,[JobNum]
              ,[AssemblySeq]
              ,[JobSeq]
              ,[OpCode]
              ,[OpDesc]
              ,[FirstQty]
              ,[NextJobSeq]
              ,[NextOpCode]
              ,[NextOpDesc]
              ,[QualifiedQty]
              ,[UnQualifiedQty]
              ,[UnQualifiedReason]
              ,[StartDate]
              ,[EndDate]
            ,AverageEndDate
              ,[LaborHrs]
            ,AverageLaborHrs
              ,0
              ,0
              ,3
              ,2
              ,[Remark]
              ,512
              ,[Plant]
              ,[Company]
              ,[IsPrint]
              ,[PrintID]
              ,[BinNum]
              ,null
              ,[Character05]
              ,[TranID]
              ,{6}
              ,null
              ,null
              ,{0}
              ,'{3}'
              ,null
              ,'{4}'
              ,'{5}'
              ,{1}
              ,{2}
              ,[ReturnThree]
              ,null
              ,1
              ,null
              ,null
              ,'{9}'
              ,[DefectNO]
              ,[CheckRemark]
,'{10}'
,'{11}'
,'{12}'
,UnQualifiedReasonRemark
,UnQualifiedReasonDesc
         from bpm where id = " + Id + "";

            sql = string.Format(sql, DMRUnQualifiedQty, Id, 1, DMRUnQualifiedReason, DMRWarehouseCode, DMRBinNum, DMRID, TransformUserGroup, opuserid, Responsibility, DMRUnQualifiedReasonRemark, CommonRepository.GetReasonDesc(DMRUnQualifiedReason), ResponsibilityRemark);

            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
        }


        private static string GetNextValidSetpInfo(string jobnum, int asmSeq, int oprseq, string companyId)
        {
            int nextAssemblySeq, nextJobSeq;
            string NextOpCode, nextOpDesc;

            string res = ErpAPI.CommonRepository.getJobNextOprTypes(jobnum, asmSeq, oprseq, out nextAssemblySeq, out nextJobSeq, out NextOpCode, out nextOpDesc, companyId);
            if (res.Substring(0, 1).Trim() == "0")
                return res;

            if (nextJobSeq != -1 && ConfigurationManager.AppSettings["InvalidOprCode"].Contains(NextOpCode)) //跳过虚拟检验工序
            {
                res = ErpAPI.CommonRepository.getJobNextOprTypes(jobnum, nextAssemblySeq, nextJobSeq, out nextAssemblySeq, out nextJobSeq, out NextOpCode, out nextOpDesc, companyId);
                if (res.Substring(0, 1).Trim() == "0")
                    return res;
            }


            return (nextJobSeq != -1 ? nextJobSeq.ToString() : "仓库") + "~" + NextOpCode + "~" + nextOpDesc + "~" + nextAssemblySeq;
        }


        private static string GetNextSetpInfo(string jobnum, int asmSeq, int oprseq, string companyId)
        {
            int nextAssemblySeq, nextJobSeq;
            string NextOpCode, nextOpDesc;

            string res = ErpAPI.CommonRepository.getJobNextOprTypes(jobnum, asmSeq, oprseq, out nextAssemblySeq, out nextJobSeq, out NextOpCode, out nextOpDesc, companyId);
            if (res.Substring(0, 1).Trim() == "0")
                return res;

            return (nextJobSeq != -1 ? nextJobSeq.ToString() : "仓库") + "~" + NextOpCode + "~" + nextOpDesc + "~" + nextAssemblySeq;
        }



        private static decimal GetSumOfAcceptedQty(string jobnum, int asmSeq, int PreOprSeq)  //该指定工序的累积接收数
        {
            decimal SumOfAcceptedQty = 0;


            string sql = @"select sum(QualifiedQty) from bpm where IsComplete = 0 and status > 2 and isdelete != 1  and  jobnum = '" + jobnum + "' and AssemblySeq = " + asmSeq + " and  JobSeq = " + PreOprSeq + "";
            object BPMNotAcceptQty = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
            BPMNotAcceptQty = Convert.IsDBNull(BPMNotAcceptQty) || BPMNotAcceptQty == null ? 0 : BPMNotAcceptQty;


            sql = @"select sum(DMRQualifiedQty) from bpmsub where IsComplete = 0 and isdelete != 1 and UnQualifiedType = 1 and DMRQualifiedQty is not null   and  jobnum = '" + jobnum + "' and AssemblySeq = " + asmSeq + " and  JobSeq = " + PreOprSeq + "";
            object BPMSubNotAcceptQty = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
            BPMSubNotAcceptQty = Convert.IsDBNull(BPMSubNotAcceptQty) || BPMSubNotAcceptQty == null ? 0 : BPMSubNotAcceptQty;


            decimal ERPCompletedQty = CommonRepository.GetOpSeqCompleteQty(jobnum, asmSeq, PreOprSeq);


            SumOfAcceptedQty = ERPCompletedQty - Convert.ToDecimal(BPMNotAcceptQty) - Convert.ToDecimal(BPMSubNotAcceptQty);


            return SumOfAcceptedQty;
        }


        private static decimal GetSumOfReportedQty(string jobnum, int asmSeq, int oprseq) //该指定工序的累积报工数
        {
            string sql = @"select sum(FirstQty) from bpm where isdelete != 1  and status < 3 and  jobnum = '" + jobnum + "' and AssemblySeq = " + asmSeq + " and  JobSeq = " + oprseq + "";
            object SumOfReportQtyBefore3 = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
            SumOfReportQtyBefore3 = Convert.IsDBNull(SumOfReportQtyBefore3) || SumOfReportQtyBefore3 == null ? 0 : SumOfReportQtyBefore3;



            sql = @"select sum(UnQualifiedQty) - sum(DMRQualifiedQty) from bpm where isdelete != 1  and status > 2 and  jobnum = '" + jobnum + "' and AssemblySeq = " + asmSeq + " and  JobSeq = " + oprseq + "";
            object SumOfReportQtyAfter2 = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
            SumOfReportQtyAfter2 = Convert.IsDBNull(SumOfReportQtyAfter2) || SumOfReportQtyAfter2 == null ? 0 : SumOfReportQtyAfter2;


            sql = @"select sum(ISNULL(Qty,0))  from process where  jobnum = '" + jobnum + "' and AssemblySeq = " + asmSeq + " and  JobSeq = " + oprseq + "";
            object SumOfReportQtyInProcess = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
            SumOfReportQtyInProcess = Convert.IsDBNull(SumOfReportQtyInProcess) || SumOfReportQtyInProcess == null ? 0 : SumOfReportQtyInProcess;


            decimal ERPCompletedQty = CommonRepository.GetOpSeqCompleteQty(jobnum, asmSeq, oprseq);


            return Convert.ToDecimal(SumOfReportQtyAfter2) + Convert.ToDecimal(SumOfReportQtyBefore3) + Convert.ToDecimal(SumOfReportQtyInProcess) + ERPCompletedQty;
        }



        public static string Start(string values) //retun 工单号~阶层号~工序序号~工序代码~工序描述~NextJobSeq NextOpCode NextOpDesc startdate CompleteQty ~ProcessID~IsParallel
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string[] arr = values.Split(' '); //工单号~阶层号~工序序号~工序代码


            string res = CommonRepository.GetJobHeadState(arr[0]);
            if (res != "正常")
                return "0|错误：" + res;

            DataTable dt = GetLatestOprInfo(arr[0], int.Parse(arr[1]), int.Parse(arr[2]));
            if (dt == null)
                return "0|错误：当前工序不存在";


            string OpDesc = dt.Rows[0]["OpDesc"].ToString();

            if (!HttpContext.Current.Session["Company"].ToString().Contains(dt.Rows[0]["Company"].ToString()))
                return "0|错误：该账号没有相应的公司权限";

            if (!HttpContext.Current.Session["Plant"].ToString().Contains(dt.Rows[0]["Plant"].ToString()))
                return "0|错误：该账号没有相应的工厂权限";


            int IsOpCodeExist = (int)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, @" Select count(*)  from BPMOpCode where  OpCode = '" + arr[3] + "' ", null);
            if (!Convert.ToBoolean(IsOpCodeExist))
                return "0|错误：当前工序 " + arr[3] + " 未添加，请联系管理员";


            string sql = @"SELECT count(*) FROM [JobLimit] where Company = '{0}' and Plant = '{1}' and JobNum= '{2}' and AssemblySeq={3} and JobSeq = {4} and OpCode ='{5}' and disabled = 0";
            sql = string.Format(sql, dt.Rows[0]["Company"].ToString(), dt.Rows[0]["Plant"].ToString(), arr[0], arr[1], arr[2], arr[3]);

            int IsValid = (int)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
            //if (!Convert.ToBoolean(IsValid))
            //    return "0|错误：当前工序 " + arr[3] + " 未在计划中，请联系计划部";

            string CreateUser = (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, @" Select CreateUser  from BPMOpCode where  OpCode = '" + arr[3] + "' ", null);
            if (!CreateUser.ToUpper().Contains(HttpContext.Current.Session["UserId"].ToString()))
                return "0|错误：该账号没有该工序操作权限";


            sql = "select count(*) from BPMsub where DMRJobNum = '" + arr[0] + "'";
            bool IsDMRJobNum = Convert.ToBoolean(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null));

            if (IsDMRJobNum)
            {
                sql = "select iscomplete from BPMsub where DMRJobNum = '" + arr[0] + "'";
                bool isSubComplete = Convert.ToBoolean(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null));
                if (!isSubComplete)
                    return "0|错误：该返修工单所属的返修流程未结束。原工单的对应工序没有结束，请用查询工具查询对应的记录，让相应的人员接收确认";
            }

            object ValidPreOpSeq = CommonRepository.GetValidPreOpSeq(arr[0], int.Parse(arr[1]), int.Parse(arr[2]));
            if (ValidPreOpSeq != null && GetSumOfAcceptedQty(arr[0], int.Parse(arr[1]), (int)ValidPreOpSeq) == 0)
            {
                return "0|错误：该工序接收数量为0，无法开始当前工序";
            }


            string NextValidSetpInfo = GetNextValidSetpInfo(arr[0], int.Parse(arr[1]), int.Parse(arr[2]), dt.Rows[0]["Company"].ToString());
            if (NextValidSetpInfo.Substring(0, 1).Trim() == "0")
                return "0|错误：无法获取工序最终去向，" + NextValidSetpInfo;


            sql = @"select * from process where userid = '" + HttpContext.Current.Session["UserId"].ToString() + "'";
            DataTable UserProcess = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql);
            if (UserProcess != null && !Convert.ToBoolean(UserProcess.Rows[0]["IsParallel"])) //只有一道工序在进行且该工序非并发
            {
                return "0|错误：该账号正在进行独立工序：" + UserProcess.Rows[0]["OpCode"].ToString();
            }
            else ////无工序在进行，或 有工序在进行且都是并发
            {
                sql = "select IsParallel from BPMOpCode where OpCode = '" + arr[3] + "'";
                int IsParallel = Convert.ToInt32(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null));

                if (UserProcess != null)//有工序在进行且都是并发
                {
                    if (IsParallel == 0)
                        return "0|错误：该账号已有正在进行的作业，当前申请开始的工序为独立工序。请检查待办事项中已开始的工序";

                    string ret = "";
                    if ((ret = GetDuplicateError(new OpReport { JobNum = arr[0], AssemblySeq = int.Parse(arr[1]), JobSeq = int.Parse(arr[2]) })).Contains("错误"))
                        return "0|" + ret;
                }


                sql = "select IsShare from BPMOpCode where OpCode = '" + arr[3] + "'";
                int IsShare = Convert.ToInt32(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null));

                //多可见开关
                string ShareSwitch = ConfigurationManager.AppSettings["ShareSwitch"];
                string ShareUserGroup = IsShare == 1 && ShareSwitch == "true" ? CreateUser : "";

                sql = "insert into process values('" + HttpContext.Current.Session["UserId"].ToString() + "', '" + OpDate + "', null, null, '" + arr[0].ToUpperInvariant() + "', " + int.Parse(arr[1]) + ", " + int.Parse(arr[2]) + ",  '" + arr[3] + "', '" + OpDesc + "', " + IsParallel + ", '" + ShareUserGroup + "', '" + dt.Rows[0]["Plant"].ToString() + "',1,null,null)";
            }
            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
            AddOpLog(null, arr[0].ToUpperInvariant(), int.Parse(arr[1]), int.Parse(arr[2]), 101, OpDate, sql);


            sql = @"select top 1 * from process where userid = '" + HttpContext.Current.Session["UserId"].ToString() + "' order by startdate desc";
            UserProcess = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql); //获取刚才插入的作业申请记录 以取得processid 和 并发标记
            string SumOfReportQty = GetSumOfReportedQty(arr[0], int.Parse(arr[1]), int.Parse(arr[2])).ToString("N2");

            string[] brr = NextValidSetpInfo.Split('~');
            arr[1] += "|" + (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, @" select PartNum from erp.JobAsmbl where JobNum = '" + arr[0] + "' and AssemblySeq = " + int.Parse(arr[1]) + "", null); //阶层号后追加物料编码
            return "1|" + arr[0] + "~" + arr[1] + "~" + arr[2] + "~" + arr[3] + "~" + OpDesc + "~" + brr[0] + "~" + brr[1] + "~" + brr[2] + "~" + OpDate + "~" + SumOfReportQty + "~" + UserProcess.Rows[0]["ProcessId"] + "~" + Convert.ToInt32(UserProcess.Rows[0]["IsParallel"]);
        }



        internal static string ReporterCommit(OpReport opReport)
        {
            lock (lock_report)
            {
                if (report_IDs.Contains((int)opReport.ProcessId))
                    return "错误：其他账号正在提交该待办事项";
                report_IDs.Add((int)opReport.ProcessId);
            }

            try
            {
                string sql = @"select * from process where processid = " + opReport.ProcessId + "";
                OpReport process = CommonRepository.DataTableToList<OpReport>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First();


                if (process == null) return "错误：记录不存在";


                process.Qty = Convert.ToDecimal(opReport.FirstQty);
                process.CheckUserGroup = opReport.CheckUserGroup;
                process.PrintQty = opReport.PrintQty;
                process.EndDate = DateTime.Now;
                //至此process拥有process表中所有字段的值,成为一个缓存


                string ret;
                if ((ret = CommonRepository.GetJobHeadState(process.JobNum)) != "正常") return "错误：" + ret;

                if (process.Qty <= 0) return "错误：报工数量需大于0";

                if (process.CheckUserGroup == "") return "错误：下步接收人不能为空";

                if ((ret = GetExceedError(process)).Contains("错误")) return ret;

                if ((ret = GetBC_WarehouseIssueError(process)).Contains("错误")) return ret;

                if ((ret = ChemicalIssue(process)).Contains("错误")) return ret;

                if ((ret = GetConsistentError(process)).Contains("错误")) return ret;



                List<OpReport> CacheList = new List<OpReport> { process };
                SetAverageTime(CacheList);


                if ((ret = WriteCacheToBPM(CacheList[0])) == "true") //true
                {
                    //清除该完结缓存
                    DeleteCache((int)process.ProcessId);
                    AddOpLog(null, process.JobNum, (int)process.AssemblySeq, (int)process.JobSeq, 102, process.EndDate.ToString("yyyy-MM-dd HH:mm:ss.fff"), "报工提交成功，自动清除process");
                    return "处理成功";
                }
                else return ret;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                report_IDs.Remove((int)opReport.ProcessId);
            }
        }

        private static string GetBC_WarehouseIssueError(OpReport process)
        {
            string sql2 = @"SELECT sum(sumoutqty) FROM BC_Warehouse where  JobNum= '{0}' and AssemblySeq={1} and JobSeq = {2}";
            sql2 = string.Format(sql2, process.JobNum, process.AssemblySeq, process.JobSeq);

            object sumoutqty = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql2, null);
            decimal SumOfReportedQty = GetSumOfReportedQty(process.JobNum, (int)process.AssemblySeq, (int)process.JobSeq);


            sql2 = @"SELECT sum(onhandqty) FROM BC_Warehouse where  JobNum= '{0}' and AssemblySeq={1} and JobSeq = {2}";
            sql2 = string.Format(sql2, process.JobNum, process.AssemblySeq, process.JobSeq);

            object onhandqty = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql2, null);

            if (onhandqty is DBNull || Convert.ToDecimal(onhandqty) == 0) //现编仓上线之前的数量全部看作已发
                return "";

            if (sumoutqty is DBNull || sumoutqty == null)
                return "";

            if (SumOfReportedQty + process.Qty > (decimal)sumoutqty)
                return "错误：表处现编仓发料数不足，还需补发：" + (SumOfReportedQty + process.Qty - (decimal)sumoutqty);

            return "";
        }

        internal static void SetAverageTime(List<OpReport> CacheList)
        {
            decimal average_LaborHrs = Convert.ToDecimal((((CacheList[0].EndDate - CacheList[0].StartDate).TotalHours) / CacheList.Count).ToString("N2"));


            for (int i = 0; i < CacheList.Count; i++)
            {
                CacheList[i].LaborHrs = (decimal)((CacheList[i].EndDate - CacheList[i].StartDate).TotalHours);
                CacheList[i].AverageLaborHrs = average_LaborHrs;
                CacheList[i].AverageEndDate = CacheList[i].StartDate.AddHours((double)average_LaborHrs);
            }
        }

        internal static string CleanStartTime()
        {
            List<OpReport> CacheList = GetCacheList();
            if (CacheList == null)
            {
                string sql = @"delete from starttime where userid = '" + HttpContext.Current.Session["UserId"].ToString() + "'";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                return "取消开始时间成功";
            }
            return "错误：列表中存在未提交工单，取消开始时间失败";
        }

        internal static string SetStartTime()
        {
            try
            {
                string sql = @"insert into StartTime(userid, StartTime) values('" + HttpContext.Current.Session["UserId"].ToString() + "', getdate())";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                return GetStartTime();
            }
            catch
            {
                return "";
            }
        }

        internal static string GetStartTime()
        {
            try
            {
                string sql = @"select StartTime from starttime where userid = '" + HttpContext.Current.Session["UserId"].ToString() + "'";
                object o = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                string start_date = o is DBNull || o == null ? "" : Convert.ToDateTime(o).ToString("yyyy-MM-dd HH:mm:ss.fff");
                return start_date;
            }
            catch
            {
                return "";
            }
        }

        internal static List<OpReport> GetCacheList()
        {
            string startdate = GetStartTime();
            string sql = @"select * from process where startdate = '" + startdate + "' and userid = '" + HttpContext.Current.Session["UserId"].ToString() + "' and processtype = 2 order by Enddate desc";

            List<OpReport> CacheList = CommonRepository.DataTableToList<OpReport>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql));

            if (CacheList != null)
            {
                for (int i = 0; i < CacheList.Count; i++)
                    CacheList[i].Company = "001";
            }

            return CacheList;
        }

        public static void DeleteCache(int ProcessID)
        {
            string sql = "delete from process where  processid = " + ProcessID + "";
            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
        }

        public static string AddCache(OpReport process)
        {
            string sql = "select count(*) from BPMsub where DMRJobNum = '" + process.JobNum + "'";
            bool IsDMRJobNum = Convert.ToBoolean(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null));

            if (IsDMRJobNum)
            {
                sql = "select iscomplete from BPMsub where DMRJobNum = '" + process.JobNum + "'";
                bool isSubComplete = Convert.ToBoolean(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null));
                if (!isSubComplete)
                    return "错误：该返修工单所属的返修流程未结束";
            }


            string ret;
            if ((ret = CommonRepository.GetJobHeadState(process.JobNum)) != "正常") return "错误：" + ret;

            if (Convert.ToDecimal(process.Qty) <= 0) return "错误：报工数量需大于0";

            if (process.CheckUserGroup == "") return "错误：下步接收人不能为空";

            if ((ret = GetExceedError(process)).Contains("错误")) return ret;

            if ((ret = GetDuplicateError(process)).Contains("错误")) return ret;

            if ((ret = GetBC_WarehouseIssueError(process)).Contains("错误")) return ret;

            if ((ret = ChemicalIssue(process)).Contains("错误")) return ret;

            try
            {
                sql = @"insert into process values( " +
                                "'" + HttpContext.Current.Session["UserId"].ToString() + "' ," +
                                "'" + process.StartDate.ToString("yyyy-MM-dd HH:mm:ss.fff") + "', " +
                                "getdate()," + //[EndDate]
                                "" + process.Qty + "," +
                                "'" + process.JobNum + "'," +
                                "" + process.AssemblySeq + "," +
                                "" + process.JobSeq + "," +
                                "'" + process.OpCode + "', " +
                                "'" + process.OpDesc + "', " +
                                "1, " + //IsParallel
                                "'', " +//ShareUserGroup
                                "'" + process.Plant + "', " + //Plant    WriteToBPM中获取
                                "" + 2 + "," +
                                "" + process.PrintQty + ", " + //前端获取
                                "'" + process.CheckUserGroup + "')";

                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                AddOpLog(null, process.JobNum, (int)process.AssemblySeq, (int)process.JobSeq, 105, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), "添加缓存|" + sql);

                return "添加成功";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public static string CommitCacheList()
        {
            List<OpReport> CacheList = GetCacheList();

            if (CacheList == null) return "错误：报工列表为空";


            SetAverageTime(CacheList);

            string error = "";
            for (int i = 0; i < CacheList.Count; i++)
            {
                string ret = WriteCacheToBPM(CacheList[i]);
                if (ret != "true")
                    error += CacheList[i].JobNum + "," + CacheList[i].AssemblySeq + "," + CacheList[i].JobSeq + "|" + ret + "\n";
                else //true
                {
                    //清除该完结缓存
                    DeleteCache((int)CacheList[i].ProcessId);
                    AddOpLog(null, CacheList[i].JobNum, (int)CacheList[i].AssemblySeq, (int)CacheList[i].JobSeq, 104, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), "报工提交成功，自动清除process");
                }
            }

            if (error == "")
            {
                CleanStartTime();
                return "全部提交成功";
            }
            else return error;
        }

        public static string GetPartInfoError(OpReport process)
        {
            object ValidPreOpSeq = CommonRepository.GetValidPreOpSeq(process.JobNum, (int)process.AssemblySeq, (int)process.JobSeq);
            if (ValidPreOpSeq == null)
            {
                decimal ReqQtyOfAssemblySeq = CommonRepository.GetReqQtyOfAssemblySeq(process.JobNum, (int)process.AssemblySeq);
                decimal SumOfReportedQty = GetSumOfReportedQty(process.JobNum, (int)process.AssemblySeq, (int)process.JobSeq);

                if (ReqQtyOfAssemblySeq < process.Qty + SumOfReportedQty)
                    return "错误：累计已转数将超出该阶层的可生产数。该工序的累计已转数：" + SumOfReportedQty.ToString("N2") + "(+" + process.Qty + ")，该阶层的可生产数为：" + ReqQtyOfAssemblySeq.ToString("N2");
                else
                    return "";
            }
            else
            {
                decimal SumOfReportedQty = GetSumOfReportedQty(process.JobNum, (int)process.AssemblySeq, (int)process.JobSeq);
                decimal SumOfAcceptedQty = GetSumOfAcceptedQty(process.JobNum, (int)process.AssemblySeq, (int)ValidPreOpSeq);

                if (SumOfAcceptedQty < SumOfReportedQty + process.Qty)
                    return "错误：累计已转数将超出累计接收数。该工序的累计已转数：" + (SumOfReportedQty.ToString("N2") + "(+" + process.Qty) + ")，该工序的累计接收数：" + SumOfAcceptedQty.ToString("N2");
                else
                    return "";
            }
        }

        public static string GetExceedError(OpReport process)
        {
            object ValidPreOpSeq = CommonRepository.GetValidPreOpSeq(process.JobNum, (int)process.AssemblySeq, (int)process.JobSeq);
            if (ValidPreOpSeq == null)
            {
                decimal ReqQtyOfAssemblySeq = CommonRepository.GetReqQtyOfAssemblySeq(process.JobNum, (int)process.AssemblySeq);
                decimal SumOfReportedQty = GetSumOfReportedQty(process.JobNum, (int)process.AssemblySeq, (int)process.JobSeq);

                if (ReqQtyOfAssemblySeq < process.Qty + SumOfReportedQty)
                    return "错误：累计已转数将超出该阶层的可生产数。该工序的累计已转数：" + SumOfReportedQty.ToString("N2") + "(+" + process.Qty + ")，该阶层的可生产数为：" + ReqQtyOfAssemblySeq.ToString("N2");
                else
                    return "";
            }
            else
            {
                decimal SumOfReportedQty = GetSumOfReportedQty(process.JobNum, (int)process.AssemblySeq, (int)process.JobSeq);
                decimal SumOfAcceptedQty = GetSumOfAcceptedQty(process.JobNum, (int)process.AssemblySeq, (int)ValidPreOpSeq);

                if (SumOfAcceptedQty < SumOfReportedQty + process.Qty)
                    return "错误：累计已转数将超出累计接收数。该工序的累计已转数：" + (SumOfReportedQty.ToString("N2") + "(+" + process.Qty) + ")，该工序的累计接收数：" + SumOfAcceptedQty.ToString("N2");
                else
                    return "";
            }
        }

        public static string GetDuplicateError(OpReport process)
        {
            string sql = @"select * from process where userid = '" + HttpContext.Current.Session["UserId"].ToString() + "'";
            DataTable UserProcess = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql);

            if (UserProcess != null)
            {
                foreach (DataRow dr in UserProcess.Rows)
                {
                    if (dr["JobNum"].ToString().ToUpper() == process.JobNum.ToUpper() && (int)dr["AssemblySeq"] == process.AssemblySeq && (int)dr["JobSeq"] == process.JobSeq)
                        return "错误：工序重复发起或重复添加";
                }
            }

            return "";
        }

        public static string GetConsistentError(OpReport process)
        {
            DataTable LatestOprInfo = GetLatestOprInfo(process.JobNum, (int)process.AssemblySeq, (int)process.JobSeq);

            if (LatestOprInfo.Rows[0]["OpCode"].ToString() != process.OpCode)
                return "错误：原工序编号" + process.OpCode + "， 现工序编号：" + LatestOprInfo.Rows[0]["OpCode"].ToString();

            return "";
        }

        public static string ChemicalIssue(OpReport process)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string issue_res = "";
            DataTable mtls = CommonRepository.GetMtlsOfOpSeq(process.JobNum, (int)process.AssemblySeq, (int)process.JobSeq, "001");

            if (mtls != null)
            {
                for (int j = 0; j < mtls.Rows.Count; j++)
                {
                    if (mtls.Rows[j]["partnum"].ToString().Substring(0, 1).Trim().ToLower() == "c")
                    {
                        string res = ErpAPI.MtlIssueRepository.Issue(process.JobNum, (int)process.AssemblySeq, (int)process.JobSeq, (int)mtls.Rows[j]["mtlseq"], mtls.Rows[j]["partnum"].ToString(), (decimal)mtls.Rows[j]["qtyper"] * (decimal)process.Qty, DateTime.Parse(OpDate), "001", process.Plant);
                        issue_res += mtls.Rows[j]["partnum"].ToString() + " ";
                        issue_res += (res == "true") ? (decimal)mtls.Rows[j]["qtyper"] * process.Qty + ", " : res.Substring(2);

                        AddOpLog(null, process.JobNum, (int)process.AssemblySeq, (int)process.JobSeq, 102, OpDate, issue_res);
                        if (res != "true")
                            return "错误：" + issue_res;
                    }
                }
            }
            return "";
        }

        public static string PrintReportQR(OpReport process)
        {
            int PrintID = 0;
            string sql;
            lock (BPMPrintIDLock)//获取并更新BPMPrintID
            {
                sql = "select BPMPrintID from SerialNumber where name = 'BAT'";
                PrintID = (int)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = "UPDATE SerialNumber SET BPMPrintID = BPMPrintID+1  where name = 'BAT'";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
            }

            string jsonStr = " text1: '{0}', text2: '{12}', text3: '{1}', text4: '{2}', text5: '{3}', text6: '" + process.Plant + "', text7: '{4}', text8: '{5}', text9: '{6}', text10: '{7}', text11: '{8}', text12: '{9}', text13: '', text14: '{10}', text15: '{11}', text16: '', text17: '', text18: '', text19: '', text20: '', text21: '', text22: '', text23: '', text24: '', text25: '', text26: '', text27: '', text28: '', text29: '', text30: '' ";
            jsonStr = string.Format(jsonStr, process.PartNum, process.JobNum, process.JobNum, process.AssemblySeq.ToString(), PrintID.ToString(), "", "", process.Qty.ToString(), "", "001", process.JobSeq.ToString(), "", process.PartDesc);
            jsonStr = "[{" + jsonStr + "}]";


            sql = "select Printer from Userprinter where userid = '" + HttpContext.Current.Session["UserId"].ToString() + "' ";
            string printer = (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
            ServiceReference_Print.WebServiceSoapClient client = new ServiceReference_Print.WebServiceSoapClient();

            string res;
            if ((res = client.Print(@"C:\D0201.btw", printer, (int)process.PrintQty, jsonStr)) == "1|处理成功")
            {
                client.Close();
                return PrintID.ToString();
            }
            else
            {
                client.Close();
                return "错误：打印失败，" + res;
            }

        }

        public static string GetCachePageDetailByOprInfo(OpReport process) //工单号 ~阶层号~工序序号~工序代码~工序描述~NextJobSeq NextOpCode NextOpDesc startdate~累计已转~Qty~Plant~processid~CheckUserGroup~EndDate;
        {
            DataTable LatestOprInfo = GetLatestOprInfo(process.JobNum, (int)process.AssemblySeq, (int)process.JobSeq);
            string NextOprInfo = GetNextValidSetpInfo(process.JobNum, (int)process.AssemblySeq, (int)process.JobSeq, "001");
            string startdate = GetStartTime();
            decimal SumOfReportedQty = GetSumOfReportedQty(process.JobNum, (int)process.AssemblySeq, (int)process.JobSeq);


            if (NextOprInfo.Substring(0, 1).Trim() == "0") return "错误：获取下工序去向失败";



            string sql = @"select  PartNum  from erp.JobAsmbl where jobnum = '" + process.JobNum + "' and AssemblySeq = " + (int)process.AssemblySeq + "";
            string partnum = "|" + (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);




            string userids = CommonRepository.GetValueAsString(process.CheckUserGroup);
            string[] useridArry = userids.Split(',');
            string usernames = "";
            foreach (string s in useridArry)
            {
                if (s != "")
                    usernames += CommonRepository.GetUserName(s.Trim()) + ",";
            }


            string[] arr = NextOprInfo.Split('~');
            string detail = "1|" + process.JobNum + "~" + process.AssemblySeq + partnum + "~" + process.JobSeq + "~" + LatestOprInfo.Rows[0]["OpCode"] + "~" +
                LatestOprInfo.Rows[0]["OpDesc"] + "~" + arr[0] + "~" + arr[1] + "~" + arr[2] + "~" + startdate + "~" + SumOfReportedQty + "~" + process.Qty + "~" + LatestOprInfo.Rows[0]["Plant"] + "~" + process.ProcessId + "~" + usernames + "~" + process.EndDate;

            return detail;
        }

        public static string GetCachePageDetailByCacheID(int processid)
        {
            string sql = @"select * from process where processid = " + processid + "";
            OpReport process = CommonRepository.DataTableToList<OpReport>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First();

            string detail = GetCachePageDetailByOprInfo(process);

            return detail;
        }

        public static string WriteCacheToBPM(OpReport process)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");


            //获取其余数据
            string sql = @" Select PartNum, Description from erp.JobAsmbl  where  JobNum = '" + process.JobNum + "' and AssemblySeq = " + process.AssemblySeq + "";
            DataTable PartInfo = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);


            process.PartNum = PartInfo.Rows[0]["PartNum"].ToString();
            process.PartDesc = PartInfo.Rows[0]["Description"].ToString();


            sql = "select  Character05 from OpMaster where Company = '001' and OpCode = '" + process.OpCode + "'";
            string Character05 = (string)(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null));


            process.Character05 = Character05;


            string NextValidOprInfo = GetNextValidSetpInfo(process.JobNum, (int)process.AssemblySeq, (int)process.JobSeq, "001");
            if (NextValidOprInfo.Substring(0, 1).Trim() == "0")
                return "错误：无法获取工序最终去向，" + NextValidOprInfo;


            //去向仓库打印
            if (NextValidOprInfo.Contains("仓库"))
            {
                string ret = PrintReportQR(process);

                if (ret.Contains("错误")) return ret;
                else process.PrintID = int.Parse(ret);
            }


            //写入主表
            sql = @" insert into bpm(
                   StartUser
                  ,[CreateUser]
                  ,[CreateDate]
                  ,[CheckUserGroup]
                  ,[PartNum]
                  ,[PartDesc]
                  ,[JobNum]
                  ,[AssemblySeq]
                  ,[JobSeq]
                  ,[OpCode]
                  ,[OpDesc]
                  ,[FirstQty]
                  ,[NextJobSeq]
                  ,[NextOpCode]
                  ,[NextOpDesc]
                  ,[StartDate]
                  ,[EndDate]
                  ,AverageEndDate
                  ,[LaborHrs]
                  ,AverageLaborHrs
                  ,[IsComplete]
                  ,[IsDelete]
                  ,[Status]
                  ,[PreStatus]
                  ,[Remark]
                  ,[AtRole]
                  ,[Plant]
                  ,[Company]
                  ,[IsPrint]
                  ,[PrintID]
                  ,[ErpCounter]
                  ,[RelatedID]
                  ,[IsSubProcess]
                  ,[ReturnThree]
                  ,[CheckCounter]
                  ,[DMRQualifiedQty]
                  ,[DMRRepairQty]
                  ,[DMRUnQualifiedQty]
                  ,Character05) values({0}) ";
            string valueStr = CommonRepository.ConstructInsertValues(new ArrayList
                {
                    process.UserID,
                    HttpContext.Current.Session["UserId"].ToString(),
                    OpDate,
                    process.CheckUserGroup,
                    process.PartNum,
                    process.PartDesc,
                    process.JobNum.ToUpperInvariant(),
                    process.AssemblySeq,
                    process.JobSeq,
                    process.OpCode,
                    process.OpDesc,
                    process.Qty,
                    NextValidOprInfo.Contains("仓库") ? process.JobSeq : int.Parse(NextValidOprInfo.Split('~')[0]), //Nextprocess.JobSeq,
                    NextValidOprInfo.Split('~')[1],             //Nextprocess.OpCode,
                    NextValidOprInfo.Split('~')[2],             //Nextprocess.OpDesc,
                    process.StartDate.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    process.EndDate.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    process.AverageEndDate.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    ((decimal)process.LaborHrs).ToString("N2"),
                    ((decimal)process.AverageLaborHrs).ToString("N2"),
                    0,
                    0,
                    2,
                    1,
                    "",
                    256,
                    process.Plant,
                    "001",
                    NextValidOprInfo.Contains("仓库") ? 1 : 0,
                    NextValidOprInfo.Contains("仓库") ? process.PrintID : null,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    process.Character05
                });
            sql = string.Format(sql, valueStr);
            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

            AddOpLog(null, process.JobNum, (int)process.AssemblySeq, (int)process.JobSeq, 102, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), sql);

            return "true";
        }

        public static string CheckerCommit(OpReport CheckInfo)
        {
            lock (lock_check)
            {
                if (check_IDs.Contains((int)CheckInfo.ID))
                    return "错误：其他账号正在提交该待办事项";
                check_IDs.Add((int)CheckInfo.ID);
            }

            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string sql = @"select * from BPM where Id = " + CheckInfo.ID + "";

            OpReport theReport = CommonRepository.DataTableToList<OpReport>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

            try
            {


                if (theReport.IsDelete == true)
                    return "错误：该批次的流程已删除";

                if (theReport.IsComplete == true)
                    return "错误：该批次的流程已结束";

                if (theReport.Status != 2)
                    return "错误：流程未在当前节点上，在 " + theReport.Status + "节点";

                if (CheckInfo.QualifiedQty != 0 && CheckInfo.TransformUserGroup == "")
                    return "错误：下步接收人不能为空";

                string res = CommonRepository.GetJobHeadState(theReport.JobNum);
                if (res != "正常")
                    return "0|错误：" + res;

                DataTable dt = GetLatestOprInfo(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq); //erp抓取该工序的最新信息
                if (dt == null)
                {
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, "UPDATE BPm SET isdelete = 1  where ID = " + theReport.ID + "", null);
                    return "0|错误：当前工序不存在，该报工流程已被自动删除";
                }

                if (dt.Rows[0]["OpCode"].ToString() != theReport.OpCode)
                {
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, "UPDATE BPm SET isdelete = 1  where ID = " + theReport.ID + "", null);
                    return "0|错误：原工序编号" + theReport.OpCode + "， 现工序编号：" + dt.Rows[0]["OpCode"].ToString() + "， 该报工流程已被自动删除";
                }

                string NextValidSetpInfo = GetNextValidSetpInfo(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, dt.Rows[0]["Company"].ToString());
                if (NextValidSetpInfo.Substring(0, 1).Trim() == "0")
                    return "0|错误：获取下工序去向失败，" + NextValidSetpInfo;


                CheckInfo.QualifiedQty = Convert.ToDecimal(CheckInfo.QualifiedQty);
                CheckInfo.UnQualifiedQty = Convert.ToDecimal(CheckInfo.UnQualifiedQty);

                if (CheckInfo.QualifiedQty < 0)
                    return "错误：合格数量不能为负";

                if (CheckInfo.UnQualifiedQty < 0)
                    return "错误：不合格数量不能为负";

                if (CheckInfo.UnQualifiedQty > 0 && (CheckInfo.UnQualifiedReason.Trim() == "" || CheckInfo.UnQualifiedReasonRemark.Trim() == ""))
                    return "错误：不合格原因和备注不能为空";

                if (CheckInfo.QualifiedQty + CheckInfo.UnQualifiedQty != theReport.FirstQty)
                    return "错误：不合格数 + 合格数 不等于报工数";


                //string error = CleanALLLaborOftheReport(theReport);      //提交时若存在过时间费用则删除该条流程的所有历史时间费用    
                //if (error != "")
                //{
                //    AddOpLog(theReport.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 201, OpDate, "历史时间费用清理失败|" + error);
                //    return "错误：时间费用已存在，请尝试重新提交";
                //}


                sql = @"select count(*) from  BPMID_LabrSeq where  BPMID = " + theReport.ID + " ";
                bool IsWriteLabor = (int)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null) > 0;

                if (!IsWriteLabor) //还未写时间费用
                {
                    res = ErpAPI.OpReportRepository.TimeAndCost((int)theReport.ID, "", theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, (decimal)CheckInfo.QualifiedQty, (decimal)CheckInfo.UnQualifiedQty, CheckInfo.UnQualifiedReason, "", theReport.StartDate, theReport.AverageEndDate, theReport.Company, theReport.Plant);

                    if (res.Substring(0, 1).Trim() != "1")
                    {
                        AddOpLog(theReport.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 201, OpDate, "时间费用写入失败|" + res);
                        return "错误：时间费用写入失败，" + res;
                    }

                    AddOpLog(theReport.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 201, OpDate, "时间费用写入成功|" + res);
                }




                string NextSetpInfo = GetNextSetpInfo(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, dt.Rows[0]["Company"].ToString());
                if (NextSetpInfo.Substring(0, 1).Trim() == "0")
                    return "0|错误：获取下工序去向失败，" + NextSetpInfo;

                if (NextSetpInfo != NextValidSetpInfo) //下工序虚拟检验
                {
                    sql = @"select count(*) from  BPMID_LabrSeq where BPMID = " + theReport.ID + " ";
                    IsWriteLabor = (int)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null) > 1;

                    if (!IsWriteLabor) //虚拟检验还未写时间费用
                    {
                        string[] arr = NextSetpInfo.Split('~');
                        res = ErpAPI.OpReportRepository.TimeAndCost((int)theReport.ID, "", theReport.JobNum, int.Parse(arr[3]), int.Parse(arr[0]), (decimal)CheckInfo.QualifiedQty, 0, "", "", theReport.StartDate, theReport.AverageEndDate, theReport.Company, theReport.Plant);

                        if (res.Substring(0, 1).Trim() != "1")
                        {
                            AddOpLog(theReport.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 201, OpDate, "下工序虚拟检验时间费用写入失败|" + res);
                            return "错误：下工序" + arr[2] + "时间费用自动写入失败，" + res;
                        }

                        AddOpLog(theReport.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 201, OpDate, "下工序虚拟检验时间费用写入成功|" + res);
                    }
                }


                sql = @"select TranID, Character05 from BPMID_LabrSeq where BPMID = " + theReport.ID + " order by DtlSeq asc";
                DataTable dt3 = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql);

                string Character05 = dt3.Rows[0]["Character05"].ToString();
                int TranID = int.Parse(dt3.Rows[0]["TranID"].ToString());

                sql = "update bpm set ErpCounter = 1 ," +
                        "CheckUser = '" + HttpContext.Current.Session["UserId"].ToString() + "', " +
                        "CheckDate = '" + OpDate + "'," +
                        "TransformUserGroup = '" + (CheckInfo.QualifiedQty > 0 ? CheckInfo.TransformUserGroup : "") + "'," +
                        "tranid = " + (TranID == -1 ? "null" : TranID.ToString()) + "," +
                        "QualifiedQty = " + CheckInfo.QualifiedQty + ", " +
                        "UnQualifiedReason = '" + (CheckInfo.UnQualifiedQty > 0 ? CommonRepository.GetValueAsString(CheckInfo.UnQualifiedReason) : "") + "'," +
                        "UnQualifiedReasonDesc = '" + (CheckInfo.UnQualifiedQty > 0 ? CommonRepository.GetReasonDesc(CheckInfo.UnQualifiedReason) : "") + "', " +
                        "UnQualifiedReasonRemark = '" + CheckInfo.UnQualifiedReasonRemark + "', " +
                        "Character05 = '" + Character05 + "'," +
                        "CheckCounter = " + (CheckInfo.UnQualifiedQty > 0 ? CheckInfo.UnQualifiedQty : 0) + ", " +
                        "UnQualifiedQty = " + CheckInfo.UnQualifiedQty + ", " +
                        "DefectNO = '" + CheckInfo.DefectNO + "', " +
                        "CheckRemark = '" + CheckInfo.CheckRemark + "', " +
                        "Status = " + (CheckInfo.QualifiedQty > 0 ? theReport.Status + 1 : 99) + "," +
                        "PreStatus = " + theReport.Status + "," +
                        "AtRole = 512, " +
                        "IsComplete = " + (CheckInfo.QualifiedQty > 0 ? 0 : 1) + " " +
                        " where id = " + CheckInfo.ID + "";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                AddOpLog(theReport.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 201, OpDate, sql);


                return "处理成功";
            }
            catch (Exception ex)
            {
                AddOpLog(theReport.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 201, OpDate, ex.Message);
                return "错误：提交失败，请尝试重新提交. " + ex.Message;
            }
            finally
            {
                check_IDs.Remove((int)CheckInfo.ID);
            }
        }

        private static string CleanALLLaborOftheReport(OpReport theReport)
        {
            return deleteTimeAndCostByBPMID((int)theReport.ID, theReport.Plant);
        }

        public static string DMRCommit(OpReport DMRInfo) //apinum 601
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string sql = @"select * from BPM where Id = " + DMRInfo.ID + "";

            OpReport theReport = CommonRepository.DataTableToList<OpReport>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

            if (theReport.IsDelete == true)
                return "错误：所属的主流程已删除";
            if (theReport.CheckCounter == 0)
                return "错误：该报工流程下的所有不良品已处理完毕";

            if (DMRInfo.TransformUserGroup == "")
                return "错误：下步接收人不能为空";


            DMRInfo.DMRQualifiedQty = Convert.ToDecimal(DMRInfo.DMRQualifiedQty);
            DMRInfo.DMRRepairQty = Convert.ToDecimal(DMRInfo.DMRRepairQty);
            DMRInfo.DMRUnQualifiedQty = Convert.ToDecimal(DMRInfo.DMRUnQualifiedQty);
            DMRInfo.DMRJobNum = DMRInfo.DMRJobNum.Trim();


            decimal determinedQty = Convert.ToDecimal(theReport.DMRQualifiedQty) + Convert.ToDecimal(theReport.DMRRepairQty) + Convert.ToDecimal(theReport.DMRUnQualifiedQty);

            if (DMRInfo.DMRQualifiedQty < 0)
                return "错误：让步数量不能为负";

            if (DMRInfo.DMRRepairQty < 0)
                return "错误：返修数量不能为负";

            if (DMRInfo.DMRUnQualifiedQty < 0)
                return "错误：废弃数量不能为负";

            if (DMRInfo.DMRQualifiedQty + DMRInfo.DMRUnQualifiedQty + DMRInfo.DMRRepairQty == 0)
                return "错误：数量不能都为0";

            if (DMRInfo.DMRQualifiedQty + DMRInfo.DMRRepairQty + DMRInfo.DMRUnQualifiedQty > theReport.UnQualifiedQty - determinedQty)
                return "错误：让步数 + 返修数 + 废弃数 超过剩余待检数：" + (theReport.UnQualifiedQty - determinedQty);

            if (DMRInfo.DMRRepairQty > 0 && DMRInfo.DMRJobNum == "")
                return "错误：返修工单号不能为空";

            if (DMRInfo.DMRRepairQty > 0 && CommonRepository.GetJobHeadState(DMRInfo.DMRJobNum) != "工单不存在,请联系计划部")
                return "错误：返修工单号已存在";

            if ((DMRInfo.DMRUnQualifiedQty > 0 && DMRInfo.DMRUnQualifiedReason == ""))
                return "错误：报废原因不能为空";

            if ((DMRInfo.DMRUnQualifiedQty > 0 && DMRInfo.DMRWarehouseCode == ""))
                return "错误：仓库不能为空";

            if (DMRInfo.DMRUnQualifiedQty > 0 && (DMRInfo.DMRBinNum == ""))
                return "错误：库位不能为空";

            if (DMRInfo.DMRUnQualifiedQty > 0 && CheckBinNum(theReport.Company, DMRInfo.DMRBinNum, DMRInfo.DMRWarehouseCode) != "ok")
                return "错误：库位与仓库不匹配";


            string res;
            if (theReport.DMRID is DBNull || theReport.DMRID == null)//产生dmrid前允许删除时间费用
            {
                int DMRID;

                if (theReport.TranID is DBNull || theReport.TranID == null)
                    return "错误：TranID is NULL";


                sql = "select DMRNum from erp.NonConf where TranID = " + theReport.TranID + "";
                object o = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);
                DMRID = o == null || o is DBNull ? 0 : Convert.ToInt32(o);

                if (DMRID == 0)
                {
                    res = ErpAPI.CommonRepository.StartInspProcessing((int)theReport.TranID, 0, (decimal)theReport.UnQualifiedQty, "D22", "BLPC", "01", "报工", theReport.Plant, "", 0, out DMRID); //产品其它不良 D22  D
                    if (res.Substring(0, 1).Trim() != "1")
                    {
                        AddOpLog(DMRInfo.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 601, OpDate, "检验处理返回结果|" + res);
                        return "错误：" + res;
                    }
                }

                sql = " update bpm set ErpCounter = 2, DMRID = " + (DMRID == 0 ? "null" : DMRID.ToString()) + " where id = " + DMRInfo.ID + "";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                theReport.DMRID = DMRID;
            }


            if (DMRInfo.DMRQualifiedQty > 0)
            {
                res = ErpAPI.CommonRepository.ConcessionDMRProcessing((int)theReport.DMRID, theReport.Company, theReport.Plant, theReport.PartNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, (decimal)DMRInfo.DMRQualifiedQty, theReport.JobNum, "报工");
                if (res.Substring(0, 1).Trim() != "1")
                    return "错误：" + res + ". 请重新提交让步数量、返修数量、报废数量";

                InsertConcessionRecord((int)DMRInfo.ID, (decimal)DMRInfo.DMRQualifiedQty, DMRInfo.TransformUserGroup, (int)theReport.DMRID, DMRInfo.DMRWarehouseCode, DMRInfo.DMRBinNum, DMRInfo.DMRUnQualifiedReason, DMRInfo.Responsibility, DMRInfo.DMRUnQualifiedReasonRemark, DMRInfo.ResponsibilityRemark);

                sql = " update bpm set checkcounter = checkcounter - " + DMRInfo.DMRQualifiedQty + ",DMRQualifiedQty = ISNULL(DMRQualifiedQty,0) + " + DMRInfo.DMRQualifiedQty + "  where id = " + (DMRInfo.ID) + "";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                AddOpLog(DMRInfo.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 601, OpDate, "让步接收子流程生成|" + theReport.DMRQualifiedQty + " + " + DMRInfo.DMRQualifiedQty);
            }

            if (DMRInfo.DMRRepairQty > 0)
            {
                sql = @"select IUM  from erp.JobAsmbl where JobNum = '" + theReport.JobNum + "' and AssemblySeq = " + theReport.AssemblySeq + "";
                object IUM = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);

                res = ErpAPI.CommonRepository.RepairDMRProcessing((int)theReport.DMRID, theReport.Company, theReport.Plant, theReport.PartNum, (decimal)DMRInfo.DMRRepairQty, DMRInfo.DMRJobNum, IUM.ToString(), theReport.JobNum);
                if (res.Substring(0, 1).Trim() != "1")
                    return "错误：" + res + ". 请重新提交返修数量、报废数量";

                InsertRepairRecord((int)DMRInfo.ID, (decimal)DMRInfo.DMRRepairQty, DMRInfo.DMRJobNum, (int)theReport.DMRID, DMRInfo.TransformUserGroup, DMRInfo.DMRWarehouseCode, DMRInfo.DMRBinNum, DMRInfo.DMRUnQualifiedReason, DMRInfo.Responsibility, DMRInfo.DMRUnQualifiedReasonRemark, DMRInfo.ResponsibilityRemark);


                sql = " update bpm set checkcounter = checkcounter - " + DMRInfo.DMRRepairQty + ",DMRRepairQty = ISNULL(DMRRepairQty,0) + " + DMRInfo.DMRRepairQty + "  where id = " + (DMRInfo.ID) + "";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                AddOpLog(DMRInfo.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 601, OpDate, "返修子流程生成|" + theReport.DMRRepairQty + " + " + DMRInfo.DMRRepairQty);


                string XML = OA_XML_Template.Create2188XML(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, theReport.OpCode, theReport.OpDesc, (decimal)DMRInfo.DMRRepairQty,
                    theReport.Plant, DMRInfo.DMRJobNum, HttpContext.Current.Session["UserId"].ToString(), OpDate, "制程不良返工", DMRInfo.Responsibility,
                    theReport.DefectNO, DMRInfo.DMRUnQualifiedReasonRemark, CommonRepository.GetReasonDesc(DMRInfo.DMRUnQualifiedReason), DMRInfo.ResponsibilityRemark, theReport.PartNum, theReport.PartDesc, "", CommonRepository.GetUserName(theReport.CheckUser));


                OAServiceReference.WorkflowServiceXmlPortTypeClient client = new OAServiceReference.WorkflowServiceXmlPortTypeClient();
                res = client.doCreateWorkflowRequest(XML, 1012);

                if (Convert.ToInt32(res) <= 0)
                {
                    AddOpLog(DMRInfo.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 601, OpDate, "返修转发OA失败:" + res);
                    return "错误：返修转发OA失败:" + res;
                }

                AddOpLog(DMRInfo.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 601, OpDate, "返修转发OA成功，OA流程id：" + res);
            }

            if (DMRInfo.DMRUnQualifiedQty > 0)
            {
                sql = @"select IUM  from erp.JobAsmbl where JobNum = '" + theReport.JobNum + "' and AssemblySeq = " + theReport.AssemblySeq + "";
                object IUM = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);


                decimal amount = GetProductionUnitCost(theReport.JobNum, (int)theReport.AssemblySeq) * (decimal)DMRInfo.DMRUnQualifiedQty;
                int OARequestID;
                int StatusCode;
                string OAReviewer;
                int BPMSubID;

                if (amount >= Decimal.Parse(ConfigurationManager.AppSettings["PROTopLimit"]))
                {
                    string XML = OA_XML_Template.Create2199XML(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, theReport.OpCode, theReport.OpDesc, (decimal)DMRInfo.DMRUnQualifiedQty,
                     theReport.Plant, amount, Decimal.Parse(ConfigurationManager.AppSettings["PROTopLimit"]), HttpContext.Current.Session["UserId"].ToString(), OpDate, "制程不良报废", DMRInfo.Responsibility,
                     "", DMRInfo.DMRUnQualifiedReasonRemark, CommonRepository.GetReasonDesc(DMRInfo.DMRUnQualifiedReason), DMRInfo.ResponsibilityRemark, theReport.PartNum, theReport.PartDesc,
                    "", CommonRepository.GetUserName(theReport.CheckUser),"","");

                    OAServiceReference.WorkflowServiceXmlPortTypeClient client = new OAServiceReference.WorkflowServiceXmlPortTypeClient();
                    res = client.doCreateWorkflowRequest(XML, 1012);

                    if (Convert.ToInt32(res) <= 0)
                    {
                        AddOpLog(DMRInfo.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 601, OpDate, "报废转发OA失败:" + res);

                        return "错误：报废转发OA失败:" + res;
                    }
                    AddOpLog(DMRInfo.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 601, OpDate, "报废转发OA成功，OA流程id：" + res);


                    sql = " update bpm set checkcounter = checkcounter - " + DMRInfo.DMRUnQualifiedQty + "  where id = " + (DMRInfo.ID) + "";
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                    AddOpLog(DMRInfo.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 601, OpDate, "checkcounter -= " + DMRInfo.DMRUnQualifiedQty + " 更新成功");

                    StatusCode = 1; //OA处理中
                    OARequestID = int.Parse(res);
                    OAReviewer = "";
                    BPMSubID = 0;
                }
                else //自动处理
                {
                    res = ErpAPI.CommonRepository.RefuseDMRProcessing(theReport.Company, theReport.Plant, (decimal)DMRInfo.DMRUnQualifiedQty, DMRInfo.DMRUnQualifiedReason, (int)theReport.DMRID, IUM.ToString());
                if (res.Substring(0, 1).Trim() != "1")
                {
                    AddOpLog(DMRInfo.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 601, OpDate, res + ". 请重新提交报废数量");
                    return "错误：" + res + ". 请重新提交报废数量";
                }

                InsertDiscardRecord((int)DMRInfo.ID, (decimal)DMRInfo.DMRUnQualifiedQty, DMRInfo.DMRUnQualifiedReason, (int)theReport.DMRID, DMRInfo.DMRWarehouseCode, DMRInfo.DMRBinNum, DMRInfo.TransformUserGroup,
                    DMRInfo.Responsibility, DMRInfo.DMRUnQualifiedReasonRemark, DMRInfo.ResponsibilityRemark, HttpContext.Current.Session["UserId"].ToString());

                sql = " update bpm set checkcounter = checkcounter - " + DMRInfo.DMRUnQualifiedQty + ",DMRUnQualifiedQty = ISNULL(DMRUnQualifiedQty,0) + " + DMRInfo.DMRUnQualifiedQty + "  where id = " + (DMRInfo.ID) + "";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                AddOpLog(DMRInfo.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 601, OpDate, "报废子流程生成|" + theReport.DMRUnQualifiedQty + " + " + DMRInfo.DMRUnQualifiedQty);

                    OAReviewer = "System";
                    OARequestID = 0;
                    StatusCode = 4;

                    sql = @"select id from BPMSub where UnQualifiedType = 1 and RelatedID  = " + DMRInfo.ID + " order by CheckDate desc";
                    object bpmsubid = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                    BPMSubID = Convert.ToInt32(bpmsubid);
                }


                sql = @"INSERT INTO [dbo].[DiscardReview]
                       ([bpmID]
                       ,[ReviewCreateUserID]
                       ,[ReviewCreateDate]
                       ,[ReviewQty]
                       ,[TopLimit]
                       ,[Amount]
                       ,[StatusCode]
                       ,OARequestID
                        ,DR_DMRUnQualifiedReason
                        ,DR_DMRWarehouseCode
                        ,DR_DMRBinNum
                        ,DR_TransformUserGroup
                        ,DR_Responsibility
                        ,DR_DMRUnQualifiedReasonRemark
                        ,DR_DMRUnQualifiedReasonDesc
                        ,DR_ResponsibilityRemark
                        ,BPMSubID
                        ,OAReviewer)
                 VALUES(
                       {0}
                       ,'{1}'
                       ,getdate()
                       ,{2}
                       ,{3}
                       ,{4}
                       ,{5}
                       ,{6}
                    ,'{7}'
                    ,'{8}'
                    ,'{9}'
                    ,'{10}'
                    ,'{11}'
                    ,'{12}'
                    ,'{13}'
                    ,'{14}'
                    ,{15}
                    ,'{16}')";
                sql = string.Format(sql, theReport.ID, HttpContext.Current.Session["UserId"].ToString(), DMRInfo.DMRUnQualifiedQty, Decimal.Parse(ConfigurationManager.AppSettings["PROTopLimit"]),
                    amount, StatusCode, OARequestID, DMRInfo.DMRUnQualifiedReason, DMRInfo.DMRWarehouseCode, DMRInfo.DMRBinNum,
                    DMRInfo.TransformUserGroup, DMRInfo.Responsibility, DMRInfo.DMRUnQualifiedReasonRemark,
                    CommonRepository.GetReasonDesc(DMRInfo.DMRUnQualifiedReason), DMRInfo.ResponsibilityRemark, BPMSubID, OAReviewer);

                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                AddOpLog(DMRInfo.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 601, OpDate, "报废缓存记录生成成功");
            }

            return "处理成功";
        }

        public static decimal GetProductionUnitCost(string jobnum, int asseq)
        {
            decimal cost = 0;

            string ss = @"  select sum(TLABurdenCost +TLALaborCost + TLASubcontractCost + TLAMaterialCost + TLAMtlBurCost) TActTotalCost
                 from erp.JobAsmbl where  Company = '001' and JobNum = '" + jobnum + "' and AssemblySeq >= " + asseq + "";
            cost = (decimal)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, ss, null);

            cost /= CommonRepository.GetReqQtyOfAssemblySeq(jobnum, 0);
            return cost;
        }

        private static string TransferCommitOfSub(OpReport TransmitInfo)//apinum 302
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string sql = @"select * from bpmsub where Id = " + TransmitInfo.ID + "";
            OpReport theSubReport = CommonRepository.DataTableToList<OpReport>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

            if (theSubReport.IsComplete == true)
                return "错误：该批次的流程已结束";

            if (theSubReport.IsDelete == true)
                return "错误：该批次的流程已删除";

            if (theSubReport.Status != 3)
                return "错误：流程未在当前节点上，在 " + theSubReport.Status + "节点";

            if ((theSubReport.DMRQualifiedQty != null || theSubReport.DMRRepairQty != null) && TransmitInfo.NextUserGroup == "")
                return "错误：下步接收人不能为空";

            //以下只会执行一个if
            if (theSubReport.DMRQualifiedQty != null)
            {
                string res = CommonRepository.GetJobHeadState(theSubReport.JobNum);
                if (res != "正常")
                    return "0|错误：" + res;

                sql = "select isdelete from bpm where ID = " + theSubReport.RelatedID + "";
                bool mainIsDeleted = (bool)(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null));
                if (mainIsDeleted)
                {
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, "UPDATE bpmsub SET isdelete = 1  where ID = " + theSubReport.ID + "", null);
                    AddOpLog(theSubReport.ID, theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq, 302, OpDate, "当前工序不存在，该报工子流程已被自动删除|" + sql);

                    return "0|错误：所属的主流程已被删除，该子流程已被自动删除";
                }

                DataTable dt = GetLatestOprInfo(theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq); //erp抓取该工序的最新信息
                if (dt == null)
                {
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, "UPDATE bpmsub SET isdelete = 1  where ID = " + theSubReport.ID + "", null);
                    AddOpLog(theSubReport.ID, theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq, 302, OpDate, "当前工序不存在，该报工子流程已被自动删除|" + sql);

                    return "0|错误：当前工序不存在，该报工流程已被自动删除";
                }

                if (dt.Rows[0]["OpCode"].ToString() != theSubReport.OpCode)
                {
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, "UPDATE bpmsub SET isdelete = 1  where ID = " + theSubReport.ID + "", null);
                    return "0|错误：原工序编号" + theSubReport.OpCode + "， 现工序编号：" + dt.Rows[0]["OpCode"].ToString() + "， 该报工流程已被自动删除";
                }

                //if (CommonRepository.IsOpSeqComplete(theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq))
                //    return "错误：该工序已完成";


                string NextValidSetpInfo = GetNextValidSetpInfo(theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq, dt.Rows[0]["Company"].ToString());
                if (NextValidSetpInfo.Substring(0, 1).Trim() == "0")
                    return "0|错误：获取下工序去向失败，" + NextValidSetpInfo;

                string[] arr = NextValidSetpInfo.Split('~');
                long nextRole = -1;
                if (arr[0] == "仓库") //由仓库接收人员处理 设置8
                    nextRole = 8;
                else if (arr[1].Substring(0, 2) == "WX")//外协
                    nextRole = 16;
                else if (arr[1].Substring(0, 2) != "WX")//厂内
                    nextRole = 128;



                sql = " update bpmsub set " +
                        "TransformUser = '" + HttpContext.Current.Session["UserId"].ToString() + "', " +
                        "NextJobSeq = " + (arr[0] == "仓库" ? theSubReport.JobSeq : int.Parse(arr[0])) + "," +
                        "NextOpCode = '" + arr[1] + "'," +
                        "NextOpDesc = '" + arr[2] + "'," +
                        "TransformDate = '" + OpDate + "'," +
                        "NextUserGroup = '" + TransmitInfo.NextUserGroup + "'," +
                        "Status = " + (theSubReport.Status + 1) + "," +
                        "PreStatus = " + (theSubReport.Status) + "," +
                        "AtRole = " + nextRole + " " +
                        "where id = " + (theSubReport.ID) + "";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                AddOpLog(theSubReport.ID, theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq, 301, OpDate, "让步提交|" + sql);
            }

            if (theSubReport.DMRUnQualifiedQty != null)
            {
                sql = " update bpmsub set " +
                        "TransformUser = '" + HttpContext.Current.Session["UserId"].ToString() + "', " +
                        "TransformDate = '" + OpDate + "'," +
                        "Status = " + 99 + "," +
                        "PreStatus = " + 3 + "," +
                        "IsComplete = 1, " +
                        "AtRole = " + 512 + " " +
                        "where id = " + (theSubReport.ID) + "";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                AddOpLog(theSubReport.ID, theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq, 301, OpDate, "报废提交|" + sql);
            }

            if ((theSubReport.DMRRepairQty) != null)
            {
                sql = @"select count(*) from erp.JobOper  where JobNum = '" + theSubReport.DMRJobNum + "'";
                bool IsExistOprSeq = Convert.ToBoolean(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null));

                if (!IsExistOprSeq) return "错误：返修工单工序为空，请联系计划部";

                sql = @"select  SubContract  from erp.JobOper where jobnum = '" + theSubReport.DMRJobNum + "' order by OprSeq asc ";
                bool IsSubContract = Convert.ToBoolean(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null));

                sql = " update bpmsub set " +
                        "TransformUser = '" + HttpContext.Current.Session["UserId"].ToString() + "', " +
                        "TransformDate = '" + OpDate + "'," +
                        "Status = " + 4 + "," +
                        "NextUserGroup = '" + TransmitInfo.NextUserGroup + "'," +
                        "PreStatus = " + 3 + "," +
                        "AtRole = " + (IsSubContract ? 16 : 128) + " " +
                        "where id = " + (theSubReport.ID) + "";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                AddOpLog(theSubReport.ID, theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq, 302, OpDate, "返修提交|" + sql);
            }

            return "处理成功";
        }



        private static string TransferCommitOfMain(OpReport TransmitInfo)//apinum 301
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string sql = @"select * from BPM where Id = " + TransmitInfo.ID + "";

            DataTable dt;

            OpReport theReport = CommonRepository.DataTableToList<OpReport>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

            if (theReport.IsDelete == true)
                return "错误：该批次的流程已删除";

            if (theReport.IsComplete == true)
                return "错误：该批次的流程已结束";

            if (theReport.Status != 3)
                return "错误：流程未在当前节点上，在 " + theReport.Status + "节点";

            if (TransmitInfo.NextUserGroup == "")
                return "错误：下步接收人不能为空";

            string res = CommonRepository.GetJobHeadState(theReport.JobNum);
            if (res != "正常")
                return "0|错误：" + res;

            dt = GetLatestOprInfo(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq); //erp抓取该工序的最新信息

            if (dt == null)
            {
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, "UPDATE BPm SET isdelete = 1  where ID = " + theReport.ID + "", null);
                AddOpLog(theReport.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 301, OpDate, "当前工序不存在，该报工主流程已被自动删除|" + sql);

                return "0|错误：当前工序不存在，该报工流程已被自动删除";
            }

            if (dt.Rows[0]["OpCode"].ToString() != theReport.OpCode)
            {
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, "UPDATE BPm SET isdelete = 1  where ID = " + theReport.ID + "", null);
                return "0|错误：原工序编号" + theReport.OpCode + "， 现工序编号：" + dt.Rows[0]["OpCode"].ToString() + "， 该报工流程已被自动删除";
            }


            string NextValidSetpInfo = GetNextValidSetpInfo(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, dt.Rows[0]["Company"].ToString());
            if (NextValidSetpInfo.Substring(0, 1).Trim() == "0")
                return "0|错误：获取下工序去向失败，" + NextValidSetpInfo;

            //再回写主表
            string[] arr = NextValidSetpInfo.Split('~');
            sql = " update bpm set " +
                    "TransformUser = '" + HttpContext.Current.Session["UserId"].ToString() + "', " +
                    "NextJobSeq = " + (arr[0] == "仓库" ? theReport.JobSeq : int.Parse(arr[0])) + "," +
                    "NextOpCode = '" + arr[1] + "'," +
                    "NextOpDesc = '" + arr[2] + "'," +
                    "TransformDate = '" + OpDate + "'," +
                    "NextUserGroup = '" + TransmitInfo.NextUserGroup + "'," +
                    "Status = " + (theReport.Status + 1) + "," +
                    "PreStatus = " + (theReport.Status) + "," +
                    "AtRole = " + GetNextRole((int)theReport.ID) + " " +
                    "where id = " + (theReport.ID) + "";
            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);


            AddOpLog(theReport.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 301, OpDate, sql);

            return "处理成功";
        }


        public static string TransferCommit(OpReport TransmitInfo)//apinum 300
        {
            string res;
            if (!(bool)TransmitInfo.IsSubProcess)
                res = TransferCommitOfMain(TransmitInfo);
            else
                res = TransferCommitOfSub(TransmitInfo);

            return res;
        }


        private static string AccepterCommitOfSub(OpReport AcceptInfo)//apinum 402
        {
            lock (lock_accept_sub)
            {
                if (accept_IDs_sub.Contains((int)AcceptInfo.ID))
                    return "错误：其他账号正在提交该待办事项";
                accept_IDs_sub.Add((int)AcceptInfo.ID);
            }


            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string sql = @"select * from bpmsub where Id = " + AcceptInfo.ID + "";
            OpReport theSubReport = CommonRepository.DataTableToList<OpReport>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录
            try
            {
                if (theSubReport.IsComplete == true)
                    return "错误：该批次的流程已结束";

                if (theSubReport.IsDelete == true)
                    return "错误：该批次的流程已删除";

                if (theSubReport.Status != 4)
                    return "错误：流程未在当前节点上，在 " + theSubReport.Status + "节点";


                string append = "";

                if ((theSubReport.DMRQualifiedQty) != null)
                {
                    string res = CommonRepository.GetJobHeadState(theSubReport.JobNum);
                    if (res != "正常")
                        return "错误：" + res;

                    sql = "select isdelete from bpm where ID = " + theSubReport.RelatedID + "";
                    bool mainIsDeleted = (bool)(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null));
                    if (mainIsDeleted)
                    {
                        Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, "UPDATE bpmsub SET isdelete = 1  where ID = " + theSubReport.ID + "", null);
                        AddOpLog(theSubReport.ID, theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq, 402, OpDate, "当前工序不存在，该报工子流程已被自动删除|" + sql);

                        return "0|错误：所属的主流程已被删除，该子流程已被自动删除";
                    }

                    DataTable dt = GetLatestOprInfo(theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq); //erp抓取该工序的最新信息
                    if (dt == null)
                    {
                        Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, "UPDATE bpmsub SET isdelete = 1  where ID = " + theSubReport.ID + "", null);
                        AddOpLog(theSubReport.ID, theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq, 402, OpDate, "当前工序不存在，该报工子流程已被自动删除|" + sql);

                        return "0|错误：当前工序不存在，该报工流程已被自动删除";
                    }

                    if (dt.Rows[0]["OpCode"].ToString() != theSubReport.OpCode)
                    {
                        Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, "UPDATE bpmsub SET isdelete = 1  where ID = " + theSubReport.ID + "", null);
                        return "0|错误：原工序编号" + theSubReport.OpCode + "， 现工序编号：" + dt.Rows[0]["OpCode"].ToString() + "， 该报工流程已被自动删除";
                    }


                    string NextValidSetpInfo = GetNextValidSetpInfo(theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq, dt.Rows[0]["Company"].ToString());
                    if (NextValidSetpInfo.Substring(0, 1).Trim() == "0")
                        return "0|错误：获取下工序去向失败，" + NextValidSetpInfo;



                    //自动回退检测
                    DataTable NextUserGroup = GetNextUserGroupOfSub((int)theSubReport.ID); ////抓取最新接收人
                    if (!NextUserGroupContains(NextUserGroup, HttpContext.Current.Session["UserId"].ToString())) //判断当前账号是否在最新接收人里面
                    {
                        ReturnStatus((bool)theSubReport.IsSubProcess, (int)theSubReport.ID, (int)theSubReport.Status, 11, "工序去向已更改，子流程自动回退", 402);
                        return "错误： 工序去向已更改，流程已自动回退至上一节点";
                    }

                    string[] arr = NextValidSetpInfo.Split('~');
                    if (arr[0] == "仓库")
                        theSubReport.AtRole = 8;
                    else if (arr[0] != "仓库" && arr[1].Substring(0, 2) == "WX")
                        theSubReport.AtRole = 16;
                    else if (arr[0] != "仓库" && arr[1].Substring(0, 2) != "WX")
                        theSubReport.AtRole = 128;


                    theSubReport.NextOpCode = arr[1];
                    theSubReport.NextOpDesc = arr[2];


                    //若去向仓库
                    if (theSubReport.AtRole == 8)
                    {
                        sql = @"select count(*) from  bpmlog  where ApiNum = 402 and OpDetail = '最后工序入库成功' and  BPMID = " + theSubReport.ID + " ";
                        bool IsStocked = Convert.ToBoolean(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null));

                        if (!IsStocked)
                        {
                            res = ErpAPI.CommonRepository.D0506_01(null, theSubReport.JobNum, (int)theSubReport.AssemblySeq, (decimal)theSubReport.DMRQualifiedQty, theSubReport.JobNum, theSubReport.NextOpCode, AcceptInfo.BinNum, theSubReport.Company, theSubReport.Plant);
                            if (res != "1|处理成功")
                                return "错误：" + res;

                            sql = "update bpmsub set ErpCounter = 5 where id = " + (theSubReport.ID) + "";
                            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                            AddOpLog(theSubReport.ID, theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq, 402, OpDate, "最后工序入库成功");
                        }
                    }



                    if (theSubReport.AtRole == 128 && theSubReport.NextOpCode.Substring(0, 2) == "BC" && theSubReport.Plant != "RRSite")
                    {
                        sql = @"select count(*) from  bpmlog  where ApiNum = 402 and OpDetail = '下工序表处物料入库成功' and  BPMID = " + theSubReport.ID + " ";
                        bool IsStocked = Convert.ToBoolean(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null));

                        if (!IsStocked)
                        {
                            if (AcceptInfo.BinNum.Trim() == "")
                            {
                                return "错误：下工序表处，请填写表处现场仓库位";
                            }
                            InputToBC_Warehouse(theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.NextJobSeq, AcceptInfo.BinNum, theSubReport.NextOpCode, theSubReport.NextOpDesc, theSubReport.PartNum, theSubReport.PartDesc, theSubReport.Plant, theSubReport.Company, (decimal)theSubReport.DMRQualifiedQty, "报工DMR让步接收");
                            AddOpLog(theSubReport.ID, theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq, 402, OpDate, "下工序表处物料入库成功");
                        }
                    }



                    string NextSetpInfo = GetNextSetpInfo(theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq, dt.Rows[0]["Company"].ToString());
                    if (NextSetpInfo.Substring(0, 1).Trim() == "0")
                        return "0|错误：获取下工序去向失败，" + NextSetpInfo;

                    if (NextSetpInfo != NextValidSetpInfo) //下工序虚拟检验
                    {
                        sql = @"select count(*) from  bpmlog  where ApiNum = 402 and OpDetail like '下工序虚拟检验时间费用写入成功%' and  BPMID = " + theSubReport.ID + " ";
                        bool IsWriteLabor = Convert.ToBoolean(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null));

                        if (!IsWriteLabor) //还未写时间费用
                        {
                            arr = NextSetpInfo.Split('~');
                            res = ErpAPI.OpReportRepository.TimeAndCost(0, "", theSubReport.JobNum, int.Parse(arr[3]), int.Parse(arr[0]), (decimal)theSubReport.DMRQualifiedQty, 0, "", "", theSubReport.StartDate, theSubReport.AverageEndDate, theSubReport.Company, theSubReport.Plant);

                            if (res.Substring(0, 1).Trim() != "1")
                            {
                                AddOpLog(theSubReport.ID, theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq, 402, OpDate, "下工序虚拟检验时间费用写入失败|" + res);
                                return "错误：下工序" + arr[2] + "时间费用自动写入失败，" + res;
                            }

                            AddOpLog(theSubReport.ID, theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq, 402, OpDate, "下工序虚拟检验时间费用写入成功|" + res);
                        }
                    }


                    sql = " update bpmsub set " +
                           "NextUser = '" + HttpContext.Current.Session["UserId"].ToString() + "', " +
                           "NextJobSeq = " + (arr[0] == "仓库" ? theSubReport.JobSeq : int.Parse(arr[0])) + "," +
                            "NextOpCode = '" + arr[1] + "'," +
                            "NextOpDesc = '" + arr[2] + "'," +
                           "NextDate = '" + OpDate + "'," +
                           "Status = 99," +
                           "PreStatus = " + (theSubReport.Status) + "," +
                           "IsComplete = 1," +
                           "BinNum = '" + CommonRepository.GetValueAsString(AcceptInfo.BinNum) + "' " +
                           "where id = " + (theSubReport.ID) + "";
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                    AddOpLog(theSubReport.ID, theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq, 402, OpDate, "让步提交|" + sql);

                    if (theSubReport.AtRole == 128 && theSubReport.Plant != "RRSite")
                    {
                        string sql2 = @"SELECT schedule FROM BC_Plan where Company = '{0}' and Plant = '{1}' and JobNum= '{2}' and AssemblySeq={3} and JobSeq = {4}";
                        sql = string.Format(sql2, "001", theSubReport.Plant, theSubReport.JobNum, (int)theSubReport.AssemblySeq, int.Parse(arr[0]));

                        object schedule = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                        if (schedule != null)
                            append = "。下工序在计划中，请尽快出货," + schedule;
                    }
                }

                if ((theSubReport.DMRRepairQty) != null)
                {
                    sql = @"select OpCode,  OprSeq, jo.PartNum, OpDesc, ja.Description from erp.JobOper jo left join erp.JobAsmbl ja on ja.JobNum = jo.JobNum 
                                    where ja.Company = '001'  and jo.JobNum = '" + theSubReport.DMRJobNum + "'  order by OprSeq asc";
                    DataTable nextinfo = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);

                    if (((string)nextinfo.Rows[0]["OpCode"]).Substring(0, 2) == "BC" && theSubReport.Plant != "RRSite")
                    {
                        if (AcceptInfo.BinNum.Trim() == "")
                        {
                            return "错误：下工序表处，请填写表处现场仓库位";
                        }
                        InputToBC_Warehouse(theSubReport.DMRJobNum, 0, (int)nextinfo.Rows[0]["OprSeq"], AcceptInfo.BinNum,
                        (string)nextinfo.Rows[0]["OpCode"], (string)nextinfo.Rows[0]["OpDesc"], theSubReport.PartNum,
                        (string)nextinfo.Rows[0]["Description"], theSubReport.Plant, theSubReport.Company, (decimal)theSubReport.DMRRepairQty, "报工DMR返修接收");

                        AddOpLog(theSubReport.ID, theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq, 402, OpDate, "下工序表处物料入库成功");
                    }

                    sql = " update bpmsub set " +
                           "NextUser = '" + HttpContext.Current.Session["UserId"].ToString() + "', " +
                           "NextDate = '" + OpDate + "'," +
                           "Status = 99," +
                           "PreStatus = " + (theSubReport.Status) + "," +
                           "IsComplete = 1," +
                           "BinNum = '" + CommonRepository.GetValueAsString(AcceptInfo.BinNum) + "' " +
                           "where id = " + (theSubReport.ID) + "";
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);


                    AddOpLog(theSubReport.ID, theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq, 402, OpDate, "返修提交|" + sql);

                    if (theSubReport.Plant != "RRSite")
                    {
                        string sql2 = @"SELECT schedule FROM BC_Plan where Company = '{0}' and Plant = '{1}' and JobNum= '{2}' and AssemblySeq={3} and JobSeq = {4}";
                        sql = string.Format(sql2, "001", theSubReport.Plant, theSubReport.DMRJobNum, 0, (int)nextinfo.Rows[0]["OprSeq"]);

                        object schedule = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                        if (schedule != null)
                            append = "。下工序在计划中，请尽快出货," + schedule;
                    }
                }

                return "处理成功" + append;
            }
            catch (Exception ex)
            {
                AddOpLog(theSubReport.ID, theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq, 402, OpDate, ex.Message);

                return "错误：" + ex.Message;
            }
            finally
            {
                accept_IDs_sub.Remove((int)AcceptInfo.ID);
            }
        }


        private static string AccepterCommitOfMain(OpReport AcceptInfo)//apinum 401
        {
            lock (lock_accept_main)
            {
                if (accept_IDs_main.Contains((int)AcceptInfo.ID))
                    return "错误：其他账号正在提交该待办事项";
                accept_IDs_main.Add((int)AcceptInfo.ID);
            }


            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string sql = @"select * from BPM where Id = " + AcceptInfo.ID + "";

            OpReport theReport = CommonRepository.DataTableToList<OpReport>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录
            try
            {
                if (theReport.IsDelete == true)
                    return "错误：该批次的流程已删除";

                if (theReport.IsComplete == true)
                    return "错误：该批次的流程已结束";

                if (theReport.Status != 4)
                    return "错误：流程未在当前节点上，在 " + theReport.Status + "节点";

                string res = CommonRepository.GetJobHeadState(theReport.JobNum);
                if (res != "正常")
                    return "错误：" + res;

                DataTable dt = GetLatestOprInfo(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq); //erp抓取该工序的最新信息

                if (dt == null)
                {
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, "UPDATE BPm SET isdelete = 1  where ID = " + theReport.ID + "", null);
                    AddOpLog(theReport.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 401, OpDate, "当前工序不存在，该报工主流程已被自动删除|" + sql);
                    return "0|错误：当前工序不存在，该报工流程已被自动删除";
                }

                if (dt.Rows[0]["OpCode"].ToString() != theReport.OpCode)
                {
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, "UPDATE BPm SET isdelete = 1  where ID = " + theReport.ID + "", null);
                    return "0|错误：原工序编号" + theReport.OpCode + "， 现工序编号：" + dt.Rows[0]["OpCode"].ToString() + "， 该报工流程已被自动删除";
                }


                string NextValidSetpInfo = GetNextValidSetpInfo(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, dt.Rows[0]["Company"].ToString());
                if (NextValidSetpInfo.Substring(0, 1).Trim() == "0")
                    return "0|错误：获取下工序去向失败，" + NextValidSetpInfo;



                //自动回退检测
                DataTable NextUserGroup = GetNextUserGroup("", (int)theReport.ID, ""); ////抓取最新接收人
                if (!NextUserGroupContains(NextUserGroup, HttpContext.Current.Session["UserId"].ToString())) //判断当前账号是否在最新接收人里面
                {
                    ReturnStatus((bool)theReport.IsSubProcess, (int)theReport.ID, (int)theReport.Status, 11, "工序去向已更改，主流程自动回退", 401);
                    return "错误： 工序去向已更改，流程已自动回退至上一节点";
                }



                string[] arr = NextValidSetpInfo.Split('~');
                if (arr[0] == "仓库")
                {
                    theReport.AtRole = 8;
                }
                else if (arr[0] != "仓库" && arr[1].Substring(0, 2) == "WX")
                    theReport.AtRole = 16;
                else if (arr[0] != "仓库" && arr[1].Substring(0, 2) != "WX")
                    theReport.AtRole = 128;


                theReport.NextOpCode = arr[1];
                theReport.NextOpDesc = arr[2];


                //若去向仓库
                if (theReport.AtRole == 8)
                {
                    sql = @"select count(*) from  bpmlog  where ApiNum = 401 and OpDetail = '最后工序入库成功' and  BPMID = " + theReport.ID + " ";
                    bool IsStocked = Convert.ToBoolean(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null));

                    if (!IsStocked)
                    {
                        res = ErpAPI.CommonRepository.D0506_01(null, theReport.JobNum, (int)theReport.AssemblySeq, (decimal)theReport.QualifiedQty, theReport.JobNum, theReport.NextOpCode, AcceptInfo.BinNum, theReport.Company, theReport.Plant);
                        if (res != "1|处理成功")
                        {
                            AddOpLog(theReport.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 401, OpDate, "最后工序入库失败" + res);
                            return "错误：" + res;
                        }

                        AddOpLog(theReport.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 401, OpDate, "最后工序入库成功");
                    }
                }

                if (theReport.AtRole == 128 && theReport.NextOpCode.Substring(0, 2) == "BC" && theReport.Plant != "RRSite")
                {
                    sql = @"select count(*) from  bpmlog  where ApiNum = 401 and OpDetail = '下工序表处物料入库成功' and  BPMID = " + theReport.ID + " ";
                    bool IsStocked = Convert.ToBoolean(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null));

                    if (!IsStocked)
                    {
                        if (AcceptInfo.BinNum.Trim() == "")
                        {
                            return "错误：下工序表处，请填写表处现场仓库位";
                        }
                        InputToBC_Warehouse(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.NextJobSeq, AcceptInfo.BinNum, theReport.NextOpCode, theReport.NextOpDesc, theReport.PartNum, theReport.PartDesc, theReport.Plant, theReport.Company, (decimal)theReport.QualifiedQty, "报工主流程接收");
                        AddOpLog(theReport.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 401, OpDate, "下工序表处物料入库成功");
                    }
                }

                //再回写主表
                sql = " update bpm set " +
                       "NextUser = '" + HttpContext.Current.Session["UserId"].ToString() + "', " +
                       "NextDate = '" + OpDate + "'," +
                       "NextJobSeq = " + (arr[0] == "仓库" ? theReport.JobSeq : int.Parse(arr[0])) + "," +
                       "NextOpCode = '" + arr[1] + "'," +
                        "NextOpDesc = '" + arr[2] + "'," +
                       "Status = 99," +
                       "atrole =  " + theReport.AtRole + "," +
                       "PreStatus = " + (theReport.Status) + "," +
                       "IsComplete = 1," +
                       "BinNum = '" + CommonRepository.GetValueAsString(AcceptInfo.BinNum) + "' " +
                       "where id = " + (theReport.ID) + "";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);


                AddOpLog(theReport.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 401, OpDate, sql);


                string append = "";
                if (theReport.AtRole == 128 && theReport.Plant != "RRSite")
                {
                    string sql2 = @"SELECT schedule FROM BC_Plan where Company = '{0}' and Plant = '{1}' and JobNum= '{2}' and AssemblySeq={3} and JobSeq = {4}";
                    sql = string.Format(sql2, "001", theReport.Plant, theReport.JobNum, (int)theReport.AssemblySeq, int.Parse(arr[0]));

                    object schedule = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                    if (schedule != null)
                        append = "。下工序在计划中，请尽快出货," + schedule;
                }

                return "处理成功" + append;
            }
            catch (Exception ex)
            {
                AddOpLog(theReport.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 401, OpDate, ex.Message);
                return "错误：" + ex.Message;
            }
            finally
            {
                accept_IDs_main.Remove((int)AcceptInfo.ID);
            }
        }


        private static bool NextUserGroupContains(DataTable NextUserGroup, string CurrUserID)
        {
            if (NextUserGroup == null) return false;
            for (int i = 0; i < NextUserGroup.Rows.Count; i++)
            {
                if (NextUserGroup.Rows[i]["UserID"].ToString().ToUpper() == CurrUserID)
                    return true;
            }
            return false;
        }


        public static string AccepterCommit(OpReport AcceptInfo) //apinum 400
        {
            string res;
            if (!(bool)AcceptInfo.IsSubProcess)
                res = AccepterCommitOfMain(AcceptInfo);
            else
                res = AccepterCommitOfSub(AcceptInfo);

            return res;
        }


        public static IEnumerable<OpReport> GetDMRRemainsOfUser()
        {
            if (((int)HttpContext.Current.Session["RoleId"] & 1024) != 0)
            {
                string sql = @"select * from BPM where CHARINDEX(company, '{0}') > 0   and   CHARINDEX(Plant, '{1}') > 0 and checkcounter > 0 and status > 2 and isdelete != 1 order by CreateDate desc";
                sql = string.Format(sql, HttpContext.Current.Session["Company"].ToString(), HttpContext.Current.Session["Plant"].ToString());
                DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql);


                if (dt == null) return null;


                List<OpReport> Remains = CommonRepository.DataTableToList<OpReport>(dt);
                if (Remains != null)
                {
                    for (int i = 0; i < Remains.Count; i++)
                    {
                        string userid = "";
                        if (Remains[i].Status == 3 || (Remains[i].PreStatus == 2 && Remains[i].Status == 99))
                            userid = Remains[i].CheckUser;
                        if (Remains[i].Status == 4 || (Remains[i].PreStatus == 4 && Remains[i].Status == 99))
                            userid = Remains[i].TransformUser;

                        Remains[i].FromUser = CommonRepository.GetUserName(userid);
                    }
                }


                return Remains;
            }


            else return null;
        }


        public static IEnumerable<OpReport> GetRemainsOfUser()
        {
            string sql = @"select * from BPM where AtRole & {0} != 0 and isdelete != 1 and isComplete != 1 and CHARINDEX(company, '{1}') > 0   and   CHARINDEX(Plant, '{2}') > 0 order by CreateDate desc";
            sql = string.Format(sql, (int)HttpContext.Current.Session["RoleId"], HttpContext.Current.Session["Company"].ToString(), HttpContext.Current.Session["Plant"].ToString());
            DataTable dt1 = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql);


            sql = @"select * from BPMsub where AtRole & {0} != 0 and UnQualifiedType = 1 and isdelete != 1 and isComplete != 1 and CHARINDEX(company, '{1}') > 0   and   CHARINDEX(Plant, '{2}') > 0 order by CreateDate desc";
            sql = string.Format(sql, (int)HttpContext.Current.Session["RoleId"], HttpContext.Current.Session["Company"].ToString(), HttpContext.Current.Session["Plant"].ToString());
            DataTable dt2 = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql);

            DataTable dt = CommonRepository.UnionDataTable(dt1, dt2);

            if (dt == null) return null;

            for (int i = dt.Rows.Count - 1; i >= 0; i--)
            {
                if ((int)dt.Rows[i]["Status"] == 2 && (dt.Rows[i]["CheckUserGroup"].ToString()).Contains(HttpContext.Current.Session["UserId"].ToString())) continue;

                else if ((int)dt.Rows[i]["Status"] == 3 && ((string)dt.Rows[i]["TransformUserGroup"]).Contains((string)HttpContext.Current.Session["UserId"])) continue;

                else if ((int)dt.Rows[i]["Status"] == 4 && ((string)dt.Rows[i]["NextUserGroup"]).Contains((string)HttpContext.Current.Session["UserId"])) continue;

                else
                    dt.Rows[i].Delete();//当前节点群组未包含改用户
            }


            List<OpReport> Remains = CommonRepository.DataTableToList<OpReport>(dt);
            if (Remains != null)
            {
                for (int i = 0; i < Remains.Count; i++)
                {
                    string userid = "";
                    if (Remains[i].Status == 2)
                        userid = Remains[i].CreateUser;
                    if (Remains[i].Status == 3)
                        userid = Remains[i].CheckUser;
                    if (Remains[i].Status == 4)
                        userid = Remains[i].TransformUser;
                    Remains[i].FromUser = CommonRepository.GetUserName(userid);
                }
            }

            return Remains;
        }


        public static string GetProcessOfUser(int processID) //retun 工单号~阶层号~工序序号~工序代码~工序描述~NextJobSeq~NextOpCode~NextOpDesc~startdate~Qty~CompleteQty~processid~IsParallel
        {
            string sql = "";
            if (processID > 0)//从待办事项调用，获取指定记录
                sql = @"select * from process where processid = " + processID + "";

            else //从报工界面调用， 获取该用户所有正在进行的记录
                sql = @"select * from process where userid = '" + HttpContext.Current.Session["UserId"].ToString() + "' and processtype = 1";



            DataTable UserProcess = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql);
            if (UserProcess == null || (processID == 0 && Convert.ToInt32(UserProcess.Rows[0]["IsParallel"]) == 1))//没有正在进行的工序,或是从报工界面调用且有工序为并发
                return null;



            string NextOprInfo = GetNextValidSetpInfo(UserProcess.Rows[0]["JobNum"].ToString(), (int)UserProcess.Rows[0]["AssemblySeq"], (int)UserProcess.Rows[0]["JobSeq"], "001");
            if (NextOprInfo.Substring(0, 1).Trim() == "0")
                return "0|错误：无法获取工序最终去向，" + NextOprInfo;

            sql = @"select  PartNum  from erp.JobAsmbl where jobnum = '" + UserProcess.Rows[0]["JobNum"].ToString() + "' and AssemblySeq = " + (int)UserProcess.Rows[0]["AssemblySeq"] + "";
            string partnum = "|" + (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);

            string[] arr = NextOprInfo.Split('~');
            return "1|" + (string)UserProcess.Rows[0]["JobNum"]
                + "~" + UserProcess.Rows[0]["AssemblySeq"].ToString() + partnum
                + "~" + UserProcess.Rows[0]["JobSeq"].ToString()
                + "~" + (string)UserProcess.Rows[0]["OpCode"]
                + "~" + (string)UserProcess.Rows[0]["OpDesc"]
                + "~" + arr[0]
                + "~" + arr[1]
                + "~" + arr[2]
                + "~" + ((DateTime)UserProcess.Rows[0]["StartDate"]).ToString("yyyy-MM-dd HH:mm:ss.fff")
                + "~" + (UserProcess.Rows[0]["Qty"].ToString() == "" ? "0" : UserProcess.Rows[0]["Qty"].ToString())
                + "~" + GetSumOfReportedQty(UserProcess.Rows[0]["JobNum"].ToString(), (int)UserProcess.Rows[0]["AssemblySeq"], (int)UserProcess.Rows[0]["JobSeq"]).ToString("N2")
                + "~" + UserProcess.Rows[0]["ProcessId"]
                + "~" + Convert.ToInt32(UserProcess.Rows[0]["IsParallel"]);
        }


        public static DataTable GetMultipleProcessOfUser()
        {
            string sql = @"select * from process where userid = '" + HttpContext.Current.Session["UserId"].ToString() + "' and  IsParallel = 1 and processtype = 1";
            DataTable dt1 = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql);

            sql = @"select * from process where userid != '" + HttpContext.Current.Session["UserId"].ToString() + "' and   CHARINDEX('" + HttpContext.Current.Session["UserId"].ToString() + "', ShareUserGroup) > 0 and processtype = 1";
            DataTable dt2 = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql);

            DataTable dt = CommonRepository.UnionDataTable(dt1, dt2);

            return dt;
        }



        public static DataTable GetNextUserGroup(string OpCode, int id, string jobnum)
        {
            DataTable dt = null;
            string sql = null;
            long nextRole = GetNextRole(id);
            string NextOpCode = "";

            if (nextRole == 8)//从拥有权值8的人员表中，选出可以操作指定仓库的人
            {
                sql = "select * from bpm where id = " + id + "";
                dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql);

                jobnum = dt.Rows[0]["JobNum"].ToString();

                string NextSetpInfo = GetNextValidSetpInfo(dt.Rows[0]["JobNum"].ToString(), (int)dt.Rows[0]["AssemblySeq"], (int)dt.Rows[0]["JobSeq"], dt.Rows[0]["Company"].ToString());
                if (NextSetpInfo.Substring(0, 1).Trim() == "0" || NextSetpInfo.Split('~')[0] != "仓库")//获取仓库失败
                    return null;

                object Warehouse = NextSetpInfo.Split('~')[1].Trim();


                sql = "select * from userfile where CHARINDEX('" + dt.Rows[0]["Company"].ToString() + "', company) > 0 and CHARINDEX('" + dt.Rows[0]["Plant"].ToString() + "', plant) > 0 and disabled = 0 and RoleID & " + nextRole + " != 0 and RoleID != 2147483647";
                dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表

                for (int i = dt.Rows.Count - 1; i >= 0; i--)
                {
                    if (dt.Rows[i]["WhseGroup"] != null)
                    {
                        string[] WhseGroups = dt.Rows[i]["WhseGroup"].ToString().Split(',');
                        if (!WhseGroups.Contains(Warehouse.ToString().Trim()))
                            dt.Rows.RemoveAt(i);
                    }
                }
            }
            else if (nextRole == 16)
            {
                dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, "select * from bpm where id = " + id + "");
                jobnum = dt.Rows[0]["JobNum"].ToString();

                sql = "select * from userfile where disabled = 0  and  CHARINDEX('" + dt.Rows[0]["Company"].ToString() + "', company) > 0 and CHARINDEX('" + dt.Rows[0]["Plant"].ToString() + "', plant) > 0 and RoleID & " + nextRole + " != 0 and RoleID != 2147483647";
                dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表
            }
            else if (nextRole == 256)
            {
                sql = @"select Plant from erp.JobHead where jobnum = '" + jobnum + "'";
                string Plant = (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);

                sql = "select CheckUser from BPMOpCode where OpCode = '" + OpCode + "'";
                string CheckUser = (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = "select * from userfile where disabled = 0 and CHARINDEX(userid, '" + CheckUser + "') > 0 and CHARINDEX('" + Plant + "', plant) > 0 and RoleID & " + nextRole + " != 0 and RoleID != 2147483647";
                dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表
            }
            else if (nextRole == 512)
            {
                dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, "select * from bpm where id = " + id + "");
                jobnum = dt.Rows[0]["JobNum"].ToString();

                sql = "select TransformUser from BPMOpCode where OpCode = '" + OpCode + "'";
                string TransformUser = (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = "select * from userfile where disabled = 0 and CHARINDEX(userid, '" + TransformUser + "') > 0 and CHARINDEX('" + dt.Rows[0]["Plant"].ToString() + "', plant) > 0 and RoleID & " + nextRole + " != 0 and RoleID != 2147483647";
                dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表
            }
            else if (nextRole == 128)//
            {
                dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, "select * from bpm where id = " + id + "");
                jobnum = dt.Rows[0]["JobNum"].ToString();

                string NextSetpInfo = GetNextValidSetpInfo(dt.Rows[0]["JobNum"].ToString(), (int)dt.Rows[0]["AssemblySeq"], (int)dt.Rows[0]["JobSeq"], dt.Rows[0]["Company"].ToString());
                if (NextSetpInfo.Substring(0, 1).Trim() == "0" || NextSetpInfo.Split('~')[0] == "仓库")//获取下工序失败
                    return null;

                NextOpCode = NextSetpInfo.Split('~')[1].Trim();

                sql = "select NextUser from BPMOpCode where OpCode = '" + NextOpCode + "'";
                string NextUser = (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = "select * from userfile where disabled = 0  and CHARINDEX('" + dt.Rows[0]["Plant"].ToString() + "', plant) > 0 and CHARINDEX(userid, '" + NextUser + "') > 0 and RoleID & " + nextRole + " != 0 and RoleID != 2147483647";
                dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表
            }


            if (nextRole == 128 && !NextOpCode.Contains("JJ"))
            {
                dt = CommonRepository.NPI_Handler(jobnum.ToUpper(), dt);
                dt = CommonRepository.WD_Handler(jobnum.ToUpper(), dt);

            }
            else if ((nextRole == 256 || nextRole == 512) && !OpCode.Contains("JJ") && !OpCode.Contains("WL0101"))
            {
                dt = CommonRepository.NPI_Handler(jobnum.ToUpper(), dt);
                dt = CommonRepository.WD_Handler(jobnum.ToUpper(), dt);
            }


            return dt == null || dt.Rows.Count == 0 ? null : dt;
        }


        public static DataTable GetNextUserGroupOfSub(int id)//三选四
        {
            string sql = "select * from bpmsub where id = " + id + "";
            OpReport theSubReport = CommonRepository.DataTableToList<OpReport>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

            long nextRole = -1;
            string nextOpCode = "";
            if (theSubReport.DMRQualifiedQty != null)
            {
                string NextValidSetpInfo = GetNextValidSetpInfo(theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq, "001");
                if (NextValidSetpInfo.Substring(0, 1).Trim() == "0")
                    return null;

                string[] arr = NextValidSetpInfo.Split('~');

                if (arr[0] == "仓库") //由仓库接收人员处理 设置8
                    nextRole = 8;
                else if (arr[1].Substring(0, 2) == "WX")//外协
                    nextRole = 16;
                else if (arr[1].Substring(0, 2) != "WX")//厂内
                    nextRole = 128;

                nextOpCode = arr[1];

            }

            if (theSubReport.DMRUnQualifiedQty != null)
                return null;

            if (theSubReport.DMRRepairQty != null)
            {
                sql = @"select count(*) from erp.JobOper  where JobNum = '" + theSubReport.DMRJobNum + "'";
                bool IsExistOprSeq = Convert.ToBoolean(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null));

                if (!IsExistOprSeq) return null;

                sql = @"select  SubContract  from erp.JobOper where jobnum = '" + theSubReport.DMRJobNum + "' order by OprSeq asc ";
                bool IsSubContract = Convert.ToBoolean(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null));

                if (IsSubContract)
                    nextRole = 16;
                else
                {
                    nextRole = 128;
                    sql = @"select top 1  OpCode  from erp.JobOper  where JobNum = '" + theSubReport.DMRJobNum + "' order by OprSeq asc";
                    nextOpCode = (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);
                }
            }


            DataTable dt = null;
            if (nextRole == 8)//从拥有权值8的人员表中，选出可以操作指定仓库的人
            {
                sql = @"select WarehouseCode from erp.partplant pp  LEFT JOIN erp.Warehse wh on pp.PrimWhse = wh.WarehouseCode and pp.Company = wh.Company and pp.Plant = wh.Plant   
                      where pp.company = '" + theSubReport.Company + "'   and pp.plant = '" + theSubReport.Plant + "'   and   pp.PartNum = '" + theSubReport.PartNum + "'";
                object Warehouse = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);

                if (Warehouse == null) return null;

                sql = "select * from userfile where CHARINDEX('" + theSubReport.Company + "', company) > 0 and CHARINDEX('" + theSubReport.Plant + "', plant) > 0 and disabled = 0 and RoleID & " + nextRole + " != 0 and RoleID != 2147483647";
                dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表

                for (int i = dt.Rows.Count - 1; i >= 0; i--)
                {
                    if (dt.Rows[i]["WhseGroup"] != null)
                    {
                        string[] WhseGroups = dt.Rows[i]["WhseGroup"].ToString().Split(',');
                        if (!WhseGroups.Contains(Warehouse.ToString().Trim()))
                            dt.Rows.RemoveAt(i);
                    }
                }
            }

            else if (nextRole == 16)
            {
                sql = "select * from userfile where disabled = 0 and  CHARINDEX('" + theSubReport.Company + "', company) > 0 and CHARINDEX('" + theSubReport.Plant + "', plant) > 0 and RoleID & " + nextRole + " != 0 and RoleID != 2147483647";
                dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表
            }

            else if (nextRole == 128)
            {
                sql = "select NextUser from BPMOpCode where OpCode = '" + nextOpCode + "'";
                string NextUser = (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = "select * from userfile where disabled = 0  and CHARINDEX('" + theSubReport.Plant + "', plant) > 0 and CHARINDEX(userid, '" + NextUser + "') > 0 and RoleID & " + nextRole + " != 0 and RoleID != 2147483647";
                dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表
            }


            if (nextRole == 128 && !nextOpCode.Contains("JJ") && !nextOpCode.Contains("WL0101"))
            {
                dt = CommonRepository.NPI_Handler(theSubReport.JobNum.ToUpper(), dt);
                dt = CommonRepository.WD_Handler(theSubReport.JobNum.ToUpper(), dt);
            }


            return dt == null || dt.Rows.Count == 0 ? null : dt;
        }


        public static DataTable GetDMRNextUserGroup(string OpCode, int id)
        {
            string sql = "select TransformUser from BPMOpCode where OpCode = '" + OpCode + "'";
            string TransformUsers = (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);


            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, "select * from bpm where id = " + id + "");
            string jobnum = dt.Rows[0]["JobNum"].ToString();


            sql = "select * from userfile where disabled = 0 and CHARINDEX('" + dt.Rows[0]["Plant"].ToString() + "', plant) > 0 and CHARINDEX(userid, '" + TransformUsers + "') > 0 and RoleID & " + 512 + " != 0 and RoleID != 2147483647";
            dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表


            if (!OpCode.Contains("JJ") && !OpCode.Contains("WL0101"))
            {
                dt = CommonRepository.NPI_Handler(jobnum.ToUpper(), dt);
                dt = CommonRepository.WD_Handler(jobnum.ToUpper(), dt);
            }

            return dt == null || dt.Rows.Count == 0 ? null : dt;
        }


        public static DataTable GetAssemblySeqByJobNum(string JobNum)
        {
            string sql = @"select AssemblySeq, PartNum, Description  from erp.JobAsmbl where jobnum = '" + JobNum + "' order by AssemblySeq asc";
            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);

            return dt;
        }


        public static DataTable GetJobSeq(string JobNum, int AssemblySeq)
        {
            string sql = @"select OprSeq,OpDesc,OpCode, jo.QtyCompleted from erp.JobAsmbl ja left join erp.JobOper jo on ja.JobNum = jo.JobNum and ja.AssemblySeq = jo.AssemblySeq where  ja.jobnum = '" + JobNum + "' and ja.AssemblySeq= '" + AssemblySeq + "' and  jo.Opcode not in ("+ ConfigurationManager.AppSettings["InvalidOprCode"] + ") order by OprSeq asc";
            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);

            decimal ReqQtyOfAssemblySeq = CommonRepository.GetReqQtyOfAssemblySeq(JobNum, AssemblySeq);

            if (dt != null)
            {
                for (int i = dt.Rows.Count - 1; i >= 0; i--)
                {
                    if ((decimal)dt.Rows[i]["QtyCompleted"] >= ReqQtyOfAssemblySeq)
                        dt.Rows.RemoveAt(i);
                }
            }

            return dt.Rows.Count == 0 ? null : dt;
        }


        public static DataTable GetRecordByID(int ID)
        {
            string sql;
            if (ID > 0)
            {
                sql = "select * from bpm where ID = " + ID + "";
            }
            else
                sql = "select * from bpmsub where ID = " + (-ID) + "";

            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql);

            return dt;
        }


        public static ScanResult GetRecordByQR(string values) //公司ID%物料编码 % 描述 % 批次号 %《工单号 % 半成品号 % 标签识别码 %》供应商ID % 订单号 % 行号 % 交货数量 % 发货行%工序号%炉批号
        {
            ScanResult sr = new ScanResult();
            sr.batch = null;
            sr.error = null;

            string[] arr = values.Split('~');

            int printid = -1;
            if (arr.Length > 10 && int.TryParse(arr[7], out printid) == false)
            {
                sr.error = "错误：二维码格式有误";
                return sr;
            }


            string sql = "select * from bpm where printID = " + printid.ToString();
            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql);
            if (dt == null)
            {
                sr.error = "错误：该二维码数据有误，请重新打印该工序二维码";
                return sr;
            }

            OpReport theReport = CommonRepository.DataTableToList<OpReport>(dt).First();
            sr.batch = theReport;


            string userid = "";
            if (sr.batch.Status == 2)
                userid = sr.batch.CreateUser;
            if (sr.batch.Status == 3)
                userid = sr.batch.CheckUser;
            if (sr.batch.Status == 4)
                userid = sr.batch.TransformUser;

            sr.batch.FromUser = CommonRepository.GetUserName(userid);



            if (!HttpContext.Current.Session["Company"].ToString().Contains(theReport.Company))
                sr.error = "错误：该账号没有相应的公司权限";
            if (!HttpContext.Current.Session["Plant"].ToString().Contains(theReport.Plant))
                sr.error = "错误：该账号没有相应的工厂权限";
            if (theReport.IsComplete == true)
            {
                sr.error = "错误：当前报工批次的流程以完结";
            }
            if (theReport.IsDelete == true)
            {
                sr.error = "错误：当前报工批次的流程已删除";
            }
            if ((theReport.AtRole & (int)HttpContext.Current.Session["RoleId"]) == 0)
            {
                sr.error = "错误：当前批次的流程未在你的节点 或 你的角色无权操作当前批次";
            }

            return sr;
        }



        public static DataTable GetReason(string type)
        {
            string sql = "select ReasonCode, Description from erp.Reason where Company = '001' and ReasonType = '" + type + "' ";
            DataTable Reasons = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);

            return Reasons;
        }



        public static string ReturnStatus(bool IsSubProcess, int ID, int oristatus, int ReasonID, string remark, int apinum)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string table = IsSubProcess ? "bpmsub" : "bpm", processtype = IsSubProcess ? "子流程" : "主流程";
            string sql = "select * from " + table + " where ID = " + ID + " ";
            OpReport theReport = CommonRepository.DataTableToList<OpReport>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录


            if ((bool)theReport.IsDelete == true)
                return "错误：该批次的流程已删除";

            if ((bool)(theReport.IsComplete) == true)
                return "错误：该批次的流程已结束";

            if ((int)(theReport.Status) != oristatus)
                return "错误：流程未在当前节点上，在 " + theReport.Status + "节点";

            if (oristatus == 4)
            {
                sql = @"update " + table + " set NextUserGroup=null, PreStatus = " + theReport.Status + ", AtRole = " + 512 + ", status = " + theReport.PreStatus + ", ReturnThree = ReturnThree+1  where id = " + ID + " ";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                //获取更新后的回退编号
                sql = "select ReturnThree from  " + table + "  where id = " + ID + "  ";
                int c = (int)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                //把该回退编号的原因插入到ReasonRecord表中
                sql = @"insert into BPMReturn(BPMID, ReturnThree, ReturnReasonId, ReasonRemark, Date,IsSubProcess) Values(" + ID + ", " + c + ", " + ReasonID + ",'" + remark + "', '" + OpDate + "', " + Convert.ToInt32(IsSubProcess) + ")";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
            }


            AddOpLog(ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, apinum, OpDate, processtype + "从" + oristatus.ToString() + "回退成功");
            return "处理成功";
        }


        public static IEnumerable<OpReport> GetRecordsForPrint(string JobNum, int? AssemblySeq, int? JobSeq)
        {
            string sql = @"select * from BPM where isdelete != 1 and printid is not null and JobNum = '" + JobNum + "' ";

            if (AssemblySeq != null)
                sql += " and AssemblySeq = " + AssemblySeq + " ";
            if (JobSeq != null)
                sql += "and JobSeq = " + JobSeq + " ";

            sql += " order by CreateDate desc";

            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql);

            List<OpReport> Remains = CommonRepository.DataTableToList<OpReport>(dt);

            return Remains;

        } //取消已开始的工序


        public static void ClearProcess(int ProcessId)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string sql = @"select * from process where processid = " + ProcessId + "";
            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql);

            sql = "delete  from  process  where processid = " + ProcessId + "";
            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);


            AddOpLog(null, dt.Rows[0]["JobNum"].ToString(), (int)dt.Rows[0]["AssemblySeq"], (int)dt.Rows[0]["JobSeq"], 12, OpDate, "用户手动取消发起的作业");


        } //取消已开始的工序



        public static void DeleteProcess(int ID)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");


            string sql = "select * from bpm where ID = " + ID + " ";
            OpReport theReport = CommonRepository.DataTableToList<OpReport>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

            if (theReport.Status == 2)
            {
                string error = CleanALLLaborOftheReport(theReport);      //提交时若存在过时间费用则删除该条流程的所有历史时间费用    
                if (error != "")
                {
                    AddOpLog(theReport.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 16, OpDate, "历史时间费用清理失败|" + error);
                    return;
                }

                sql = "update bpm set isdelete = 1 where id = " + ID + "";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                AddOpLog(ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 16, OpDate, "二节点删除流程成功");
            }

        }


        public static string CopyQR(int id, int printqty)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string sql = @"select * from bpm where Id = " + id + "";
            OpReport theReport = CommonRepository.DataTableToList<OpReport>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录


            string jsonStr = " text1: '{0}', text2: '{12}', text3: '{1}', text4: '{2}', text5: '{3}', text6: '" + theReport.Plant + "', text7: '{4}', text8: '{5}', text9: '{6}', text10: '{7}', text11: '{8}', text12: '{9}', text13: '', text14: '{10}', text15: '{11}', text16: '', text17: '', text18: '', text19: '', text20: '', text21: '', text22: '', text23: '', text24: '', text25: '', text26: '', text27: '', text28: '', text29: '', text30: '' ";
            jsonStr = string.Format(jsonStr, theReport.PartNum, theReport.JobNum, theReport.JobNum, theReport.AssemblySeq.ToString(), theReport.PrintID.ToString(), "", "", theReport.FirstQty.ToString(), "", theReport.Company, theReport.JobSeq.ToString(), "", theReport.PartDesc);
            jsonStr = "[{" + jsonStr + "}]";


            sql = "select Printer from Userprinter where userid = '" + HttpContext.Current.Session["UserId"].ToString() + "' ";
            string printer = (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
            string res = "";
            ServiceReference_Print.WebServiceSoapClient client = new ServiceReference_Print.WebServiceSoapClient();
            if ((res = client.Print(@"C:\D0201.btw", printer, printqty, jsonStr)) == "1|处理成功")
            {
                client.Close();
                AddOpLog(id, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 14, OpDate, "复制二维码");
                return "处理成功";
            }
            else
            {
                client.Close();
                return "错误：打印失败  " + res;
            }
        }


        public static DataTable GetRelatedJobNum(string JobNum)
        {
            JobNum = JobNum.Split('-')[0];

            string sql = " select jobnum from erp.JobHead where jobnum like  '{0}%'";
            sql = string.Format(sql, JobNum);

            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);

            return dt;
        }


        public static void InputToBC_Warehouse(string JobNum, int AssemblySeq, int NextJobSeq, string BinNum, string NextOpCode, string NextOpDesc, string PartNum, string PartDesc, string Plant, string Company, decimal Qty, string comefrom)
        {
            string sql = @"select ID from BC_Warehouse where JobNum = '" + JobNum + "' and AssemblySeq = " + AssemblySeq + " and JobSeq = " + NextJobSeq + " and BinNum = '" + BinNum.Trim() + "'";
            int ID = Convert.ToInt32(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null));
            if (ID != 0)
            {
                sql = " update BC_Warehouse set OnHandQty = OnHandQty + " + Qty + " where id = " + ID + "";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                AddBC_WarehouseLog(JobNum, AssemblySeq, NextJobSeq, sql, ID, Qty, BinNum, comefrom);
            }
            else
            {
                sql = @"INSERT INTO [dbo].[BC_Warehouse]
                               ([JobNum]
                               ,[AssemblySeq]
                               ,[JobSeq]
                               ,[OpCode]
                               ,[OpDesc]
                               ,[PartNum]
                               ,[PartDesc]
                               ,[OnHandQty]
                               ,[SumOutQty]
                               ,[BinNum]
                               ,[Plant]
                               ,[Company])
                                VALUES({0})";
                string values = CommonRepository.ConstructInsertValues(new ArrayList
                                {
                                    JobNum,
                                    AssemblySeq,
                                    NextJobSeq,
                                    NextOpCode,
                                    NextOpDesc,
                                    PartNum,
                                    PartDesc,
                                    Qty,
                                    0,
                                    BinNum,
                                    Plant,
                                    Company
                                });
                sql = string.Format(sql, values);
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                AddBC_WarehouseLog(JobNum, AssemblySeq, NextJobSeq, sql, 0, Qty, BinNum, comefrom);

            }
        }


        public static DataTable GetBC_WarehouseInfo(OpReport condition)
        {
            string sql = @"select * from BC_Warehouse where onhandqty != 0  ";

            if (!string.IsNullOrEmpty(condition.JobNum))
                sql += " and JobNum = '" + condition.JobNum + "' ";
            if (condition.AssemblySeq != null)
                sql += " and AssemblySeq = " + condition.AssemblySeq + " ";
            if (condition.JobSeq != null)
                sql += " and JobSeq = " + condition.JobSeq + " ";
            if (!string.IsNullOrEmpty(condition.OpDesc))
                sql += " and OpDesc like '%" + condition.OpDesc + "%' ";
            if (!string.IsNullOrEmpty(condition.PartNum))
                sql += " and PartNum = '" + condition.PartNum + "' ";
            if (!string.IsNullOrEmpty(condition.PartDesc))
                sql += " and PartDesc like '%" + condition.PartDesc + "%' ";
            if (!string.IsNullOrEmpty(condition.BinNum))
                sql += " and BinNum = '" + condition.BinNum + "' ";

            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql);

            return dt;
        }


        public static string OutOfBC_Warehouse(OpReport opReport)
        {
            if ((Convert.ToInt64(HttpContext.Current.Session["RoleID"]) & 4096) == 0)
                return "0|错误：该账号没有权限操作表处临时仓";


            string sql = @"select * from BC_Warehouse where id = " + opReport.ID + " ";
            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql);

            if (opReport.Qty > (decimal)dt.Rows[0]["OnHandQty"]) return "错误：库存数不足";

            sql = " update BC_Warehouse set OnHandQty = OnHandQty - " + opReport.Qty + " , sumoutqty  = sumoutqty + " + opReport.Qty + " where id = " + opReport.ID + "";
            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

            AddBC_WarehouseLog(dt.Rows[0]["JobNum"].ToString(), (int)dt.Rows[0]["AssemblySeq"], (int)dt.Rows[0]["JobSeq"], sql, (int)opReport.ID, (decimal)-opReport.Qty, dt.Rows[0]["BinNum"].ToString(), "");

            return "处理成功";
        }



        private static void AddBC_WarehouseLog(string JobNum, int AssemblySeq, int JobSeq, string OpDetail, int id, decimal qty, string binnum, string comefrom)
        {
            string sql = @"insert into BC_WarehouseLog(JobNum, AssemblySeq, JobSeq,  Opdate, OpDetail, UserId, ID, Qty,binnum,comefrom) Values('{0}', {1}, {2}, {3},  @OpDetail, '{4}',{5}, {6}, '{7}', '{8}') ";
            sql = string.Format(sql, JobNum, AssemblySeq, JobSeq, "getdate()", HttpContext.Current.Session["UserId"].ToString(), id, qty, binnum, comefrom);

            SqlParameter[] ps = new SqlParameter[] { new SqlParameter("@OpDetail", OpDetail) };
            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, ps);

        }//添加操作记录


        public static void AddOpLog(int? id, string JobNum, int AssemblySeq, int JobSeq, int ApiNum, string OpDate, string OpDetail)
        {
            string sql = @"insert into BPMLog(JobNum, AssemblySeq, UserId, Opdate, ApiNum, JobSeq, OpDetail,bpmid) Values('{0}', {1}, '{2}', {3}, {4}, {5}, @OpDetail,{6}) ";

            string opuser = HttpContext.Current.Session == null ? "OA_WebService" : HttpContext.Current.Session["UserId"].ToString();

            sql = string.Format(sql, JobNum, AssemblySeq, opuser, "getdate()", ApiNum, JobSeq, id == null ? "null" : id.ToString());

            SqlParameter[] ps = new SqlParameter[] { new SqlParameter("@OpDetail", OpDetail) };

            try
            {
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, ps);
            }
            catch { }

        }//添加操作记录
    }
}