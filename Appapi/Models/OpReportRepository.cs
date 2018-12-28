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
        private static long GetNextRole(int id)
        {
            long nextRole = 1152921504606846976;//2^60

            string sql = "select * from bpm where id = " + id + "";
            var t = CommonRepository.DataTableToList<OpReport>(SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql));
            OpReport OpInfo = t?.First(); //获取该批次记录

            int nextStatsu = (OpInfo != null ? (int)OpInfo.Status : 1) + 1;

            if (nextStatsu == 2)
                nextRole = 256;

            else if (nextStatsu == 3)
                nextRole = 512;

            else if (nextStatsu == 4)
            {
                int a, b;//凑个数，无意义
                string c;//凑个数，无意义
                string OpCode, res;

                res = ErpAPI.Common.getJobNextOprTypes(OpInfo.JobNum, (int)OpInfo.AssemblySeq, (int)OpInfo.JobSeq, out a, out b, out OpCode, out c, OpInfo.Company);

                if (res.Substring(0, 1).Trim().ToLower() == "p") //由仓库接收人员处理 设置8
                    nextRole = 8;
                else if (res.Substring(0, 1).Trim().ToLower() == "s")//外协
                    nextRole = 16;
                else if (res.Substring(0, 1).Trim().ToLower() == "m")//场内
                    nextRole = 128;
            }

            return nextRole;
        }


        private static bool IsOpSeqComplete(string JobNum, int AssemblySeq, int JobSeq)
        {
            string sql = @"select OpComplete  from erp.JobOper where jobnum = '" + JobNum + "' and AssemblySeq = " + AssemblySeq + " and  OprSeq = " + JobSeq + "";

            return Convert.ToBoolean(SQLRepository.ExecuteScalarToObject(SQLRepository.ERP_strConn, CommandType.Text, sql, null));
        }


        private static string CheckJobHeadState(string jobnum)
        {
            string sql = @"select jh.jobClosed,jh.jobComplete, jh.JobEngineered, jh.JobReleased from erp.JobHead jh where jh.JobNum = '" + jobnum + "'";
            DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.ERP_strConn, sql);

            if (dt == null)
                return "工单不存在";
            else if ((bool)dt.Rows[0]["jobClosed"] == true)
                return "关联的工单已关闭";
            else if ((bool)dt.Rows[0]["jobComplete"] == true)
                return "关联的工单已完成";
            else if ((bool)dt.Rows[0]["JobEngineered"] == false)
                return "关联的工单未设计";
            else if ((bool)dt.Rows[0]["JobReleased"] == false)
                return "关联的工单未发放";

            return "正常";
        }


        private static DataTable GetJobSeqInfo(string jobnum, int asmSeq, int oprseq)
        {
            string sql = @"select jh.Company, Plant,OpDesc, jo.OpCode from erp.JobHead jh left join erp.JobOper jo on jh.Company = jo.Company and jh.JobNum =jo.JobNum where jo.JobNum = '" + jobnum + "' and jo.AssemblySeq = " + asmSeq + " and  jo.OprSeq = " + oprseq + "";
            DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.ERP_strConn, sql);

            return dt;
        }


        private static void InsertConcessionRecord(int Id, decimal DMRQualifiedQty, string TransformUserGroup)
        {
            string sql = @"
                   insert into BPMSub   select [CreateUser]
                  ,[CheckUser]
                  ,null
                  ,null
                  ,[CreateDate]
                  ,[CheckDate]
                  ,null
                  ,null
                  ,[UnQualifiedGroup]
                  ,[CheckUserGroup]
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
                  ,[LaborHrs]
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
                  ,[ErpCounter]
                  ,[Character05]
                  ,[TranID]
                  ,null
                  ,{0}
                  ,null
                  ,null
                  ,null
                  ,null
                  ,null
                  ,null
                  ,{1}
                  ,{2}
                  ,[ReturnThree]
                  ,[CheckCounter]
             from bpm where id = " + Id + "";

            sql = string.Format(sql,DMRQualifiedQty, Id, 1,TransformUserGroup);

            SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
        }


        private static void InsertRepairRecord(int Id, decimal DMRRepairQty, string DMRJobNum, int DMRID, string TransformUserGroup)
        {
            string sql = @"
       insert into BPMSub   select [CreateUser]
      ,[CheckUser]
      ,null
      ,null
      ,[CreateDate]
      ,[CheckDate]
      ,null
      ,null
      ,[UnQualifiedGroup]
      ,[CheckUserGroup]
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
      ,[EndDate]
      ,[LaborHrs]
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
      ,[ErpCounter]
      ,[Character05]
      ,[TranID]
      ,{4}
      ,null
      ,{0}
      ,null
      ,null
      ,'{3}'
      ,null
      ,null
      ,{1}
      ,{2}
      ,[ReturnThree]
      ,[CheckCounter]
 from bpm where id = " + Id + "";

            sql = string.Format(sql, DMRRepairQty, Id, 1, DMRJobNum, DMRID, TransformUserGroup);

            SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
        }


        private static void InsertDiscardRecord(int Id, decimal DMRUnQualifiedQty, string DMRUnQualifiedReason, int DMRID, string DMRWarehouseCode, string DMRBinNum, string TransformUserGroup)
        {
            string sql = @"
       insert into BPMSub   select [CreateUser]
      ,[CheckUser]
      ,null
      ,null
      ,[CreateDate]
      ,[CheckDate]
      ,null
      ,null
      ,[UnQualifiedGroup]
      ,[CheckUserGroup]
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
      ,[LaborHrs]
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
      ,[ErpCounter]
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
      ,[CheckCounter]
 from bpm where id = " + Id + "";

            sql = string.Format(sql, DMRUnQualifiedQty, Id, 1, DMRUnQualifiedReason, DMRWarehouseCode, DMRBinNum, DMRID, TransformUserGroup);

            SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
        }



        private static string GetNextSetpInfo(string jobnum, int asmSeq, int oprseq, string companyId)
        {
            int nextAssemblySeq, nextJobSeq;
            string NextOpCode, nextOpDesc;

            string res = ErpAPI.Common.getJobNextOprTypes(jobnum, asmSeq, oprseq, out nextAssemblySeq, out nextJobSeq, out NextOpCode, out nextOpDesc, companyId);
            if (res.Substring(0, 1).Trim() == "0")
                return res;

            return (nextJobSeq != -1 ? nextJobSeq.ToString() : "仓库") + "~" + NextOpCode + "~" + nextOpDesc;
        }


        private static decimal GetTotalQtyOfJobSeq(string jobnum, int asmSeq, int oprseq, int id) //该工序的 在跑+erp 数量, 不包括本次报工数量
        {
            string sql = @"select sum(firstqty) from bpm where id != " + id + " and  isdelete != 1 and isComplete != 1  and  jobnum = '" + jobnum + "' and AssemblySeq = " + asmSeq + " and  JobSeq = " + oprseq + " and IsSubProcess = 0";
            object o = SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);
            decimal bpm_qty = o is DBNull ? 0 : Convert.ToDecimal(o);
            decimal erp_qty = CommonRepository.GetOpSeqCompleteQty(jobnum, asmSeq, oprseq);

            return bpm_qty + erp_qty;
        }


        public static string Start(string values) //retun 工单号~阶层号~工序序号~工序代码~工序描述~NextJobSeq NextOpCode NextOpDesc startdate CompleteQty
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string[] arr = values.Split(' '); //工单号~阶层号~工序序号~工序代码


            string res = CheckJobHeadState(arr[0]);
            if (res != "正常")
                return "0|错误：" + res;

            DataTable dt = GetJobSeqInfo(arr[0], int.Parse(arr[1]), int.Parse(arr[2]));
            if (dt == null)
                return "0|错误：当前工序不存在";

            string OpDesc = dt.Rows[0]["OpDesc"].ToString();

            if (!HttpContext.Current.Session["Company"].ToString().Contains(dt.Rows[0]["Company"].ToString()))
                return "0|错误：该账号没有相应的公司权限";

            if (!HttpContext.Current.Session["Plant"].ToString().Contains(dt.Rows[0]["Plant"].ToString()))
                return "0|错误：该账号没有相应的工厂权限";

            string CreateUser = (string)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, @" Select CreateUser  from BPMOpCode where  OpCode = '" + arr[3] + "' ", null);
            if (!CreateUser.Contains(HttpContext.Current.Session["UserId"].ToString()))
                return "0|错误：该账号没有该工序操作权限";

            //object PreOpSeq = CommonRepository.GetPreOpSeq(arr[0], int.Parse(arr[1]), int.Parse(arr[2]));
            //if (PreOpSeq != null && !IsOpSeqComplete(arr[0], int.Parse(arr[1]), (int)PreOpSeq))
            //    return "错误：上一道工序未完成";

            if (IsOpSeqComplete(arr[0], int.Parse(arr[1]), int.Parse(arr[2])))
                return "错误：该工序已完成";

            string ss = GetNextSetpInfo(arr[0], int.Parse(arr[1]), int.Parse(arr[2]), dt.Rows[0]["Company"].ToString());
            if (ss.Substring(0, 1).Trim() == "0")
                return "0|错误：无法获取工序最终去向，" + ss;




            string sql = @"select count(*) from process where userid = '" + HttpContext.Current.Session["UserId"].ToString() + "'";
            bool IsExistUserId = Convert.ToBoolean(SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null));
            if (IsExistUserId)
            {
                sql = @"select StartDate, EndDate, OpCode from process where userid = '" + HttpContext.Current.Session["UserId"].ToString() + "'";
                DataTable dt2 = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql);

                if (Convert.IsDBNull(dt2.Rows[0]["EndDate"]) && Convert.IsDBNull(dt2.Rows[0]["StartDate"]))//已结束
                    sql = "update process set qty = null, JobNum = '" + arr[0] + "', AssemblySeq = " + int.Parse(arr[1]) + ", JobSeq = " + int.Parse(arr[2]) + ", OpCode = '" + arr[3] + "', OpDesc = '" + OpDesc + "', StartDate ='" + OpDate + "',EndDate = null  where userid = '" + HttpContext.Current.Session["UserId"].ToString() + "'";
                else
                    return "0|错误：该账号正在进行工序：" + dt2.Rows[0]["OpCode"].ToString();
            }
            else
            {
                sql = "insert into process values('" + HttpContext.Current.Session["UserId"].ToString() + "', '" + OpDate + "', null, null, '" + arr[0] + "', " + int.Parse(arr[1]) + ", " + int.Parse(arr[2]) + ",  '" + arr[3] + "', '" + OpDesc + "')";
            }

            SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);


            sql = sql.Replace("'", "");
            AddOpLog(null, arr[0], int.Parse(arr[1]), int.Parse(arr[2]), 101, OpDate, sql);


            return "1|" + arr[0] + "~" + arr[1] + "~" + arr[2] + "~" + arr[3] + "~" + OpDesc + "~" + ss + "~" + OpDate + "~" + GetTotalQtyOfJobSeq(arr[0], int.Parse(arr[1]), int.Parse(arr[2]), 0).ToString();
        }


        public static string ReporterCommit(OpReport ReportInfo)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            DataTable dt, dt2;

            string res = CheckJobHeadState(ReportInfo.JobNum);
            if (res != "正常")
                return "0|错误：" + res;

            if ((dt = GetJobSeqInfo(ReportInfo.JobNum, (int)ReportInfo.AssemblySeq, (int)ReportInfo.JobSeq)) == null)
                return "0|错误：当前工序不存在";

            dt2 = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, @"select * from process where userid = '" + HttpContext.Current.Session["UserId"].ToString() + "'");
            if (dt.Rows[0]["OpCode"].ToString() != dt2.Rows[0]["OpCode"].ToString())
                return "0|错误：原工序编号" + dt2.Rows[0]["OpCode"].ToString() + "， 现工序编号：" + dt.Rows[0]["OpCode"].ToString();

            if (IsOpSeqComplete(ReportInfo.JobNum, (int)ReportInfo.AssemblySeq, (int)ReportInfo.JobSeq))
                return "错误：该工序已完成";

            if (ReportInfo.FirstQty <= 0)
                return "错误：报工数量需大于0";

            object PreOpSeq = CommonRepository.GetPreOpSeq(ReportInfo.JobNum, (int)ReportInfo.AssemblySeq, (int)ReportInfo.JobSeq);
            if (PreOpSeq == null && CommonRepository.GetReqQtyOfAssemblySeq(ReportInfo.JobNum, (int)ReportInfo.AssemblySeq) < ReportInfo.FirstQty + GetTotalQtyOfJobSeq(ReportInfo.JobNum, (int)ReportInfo.AssemblySeq, (int)ReportInfo.JobSeq, 0))
                return "错误： 报工数超出该阶层物料的需求数量";
            if (PreOpSeq != null && CommonRepository.GetOpSeqCompleteQty(ReportInfo.JobNum, (int)ReportInfo.AssemblySeq, (int)PreOpSeq) < ReportInfo.FirstQty + GetTotalQtyOfJobSeq(ReportInfo.JobNum, (int)ReportInfo.AssemblySeq, (int)ReportInfo.JobSeq, 0))
                return "错误： 报工数超出上一道工序的完成数量";

            string ss = GetNextSetpInfo(ReportInfo.JobNum, (int)ReportInfo.AssemblySeq, (int)ReportInfo.JobSeq, dt.Rows[0]["Company"].ToString());
            if (ss.Substring(0, 1).Trim() == "0")
                return "0|错误：获取下工序去向失败，" + ss;



            //////提交， 首先回写process
            string sql = "update process set EndDate='" + OpDate + "' ,Qty= " + ReportInfo.FirstQty + " where userid = '" + HttpContext.Current.Session["UserId"].ToString() + "' and EndDate is null";
            SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

            sql = sql.Replace("'", "");
            AddOpLog(null, ReportInfo.JobNum, (int)ReportInfo.AssemblySeq, (int)ReportInfo.JobSeq, 102, OpDate, sql);
            /////


            sql = @" Select PartNum, Description from erp.JobAsmbl  where  jobnum = '" + ReportInfo.JobNum + "' and AssemblySeq = " + ReportInfo.AssemblySeq + "";
            DataTable partinfo = SQLRepository.ExecuteQueryToDataTable(SQLRepository.ERP_strConn, sql);


            //去向仓库打印
            int PrintID = 0;
            bool IsPrint = false;
            if (ss.Contains("仓库"))
            {
                sql = "select BPMPrintID from SerialNumber where name = 'BAT'";
                PrintID = (int)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                string jsonStr = " text1: '{0}', text2: '{12}', text3: '{1}', text4: '{2}', text5: '{3}', text6: '', text7: '{4}', text8: '{5}', text9: '{6}', text10: '{7}', text11: '{8}', text12: '{9}', text13: '', text14: '{10}', text15: '{11}', text16: '', text17: '', text18: '', text19: '', text20: '', text21: '', text22: '', text23: '', text24: '', text25: '', text26: '', text27: '', text28: '', text29: '', text30: '' ";
                jsonStr = string.Format(jsonStr, partinfo.Rows[0]["PartNum"].ToString(), ReportInfo.JobNum, ReportInfo.JobNum, ReportInfo.AssemblySeq.ToString(), PrintID.ToString(), "", "", ReportInfo.FirstQty.ToString(), "", dt.Rows[0]["Company"].ToString(), ReportInfo.JobSeq.ToString(), "", partinfo.Rows[0]["Description"].ToString());
                jsonStr = "[{" + jsonStr + "}]";


                sql = "select Printer from Userprinter where userid = '" + HttpContext.Current.Session["UserId"].ToString() + "' ";
                string printer = (string)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);
                ServiceReference_Print.WebServiceSoapClient client = new ServiceReference_Print.WebServiceSoapClient();
                if ((res = client.Print(@"C:\D0201.btw", "P052", (int)ReportInfo.PrintQty, jsonStr)) == "1|处理成功")
                {
                    IsPrint = true;
                    client.Close();
                    sql = "UPDATE SerialNumber SET BPMPrintID = BPMPrintID+1  where name = 'BAT'";
                    SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
                }
                else
                {
                    client.Close();
                    return "错误：打印失败  " + res;
                }
            }


            //再回写主表
            sql = @"select enddate from process where userid = '" + HttpContext.Current.Session["UserId"].ToString() + "'";
            DateTime EndDate = (DateTime)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);
            TimeSpan LaborHrs = EndDate - ReportInfo.StartDate;

            sql = @" insert into bpm
                  ([CreateUser]
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
                  ,[LaborHrs]
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
                  ,[CheckCounter]) values({0}) ";
            string valueStr = CommonRepository.ConstructInsertValues(new ArrayList
                {
                    HttpContext.Current.Session["UserId"].ToString(),
                    OpDate,
                    ReportInfo.CheckUserGroup,
                    partinfo.Rows[0]["PartNum"].ToString(),
                    partinfo.Rows[0]["Description"].ToString(),
                    ReportInfo.JobNum,
                    ReportInfo.AssemblySeq,
                    ReportInfo.JobSeq,
                    ReportInfo.OpCode,
                    dt.Rows[0]["OpDesc"].ToString(),
                    ReportInfo.FirstQty,
                    ReportInfo.NextJobSeq,
                    ReportInfo.NextOpCode,
                    ReportInfo.NextOpDesc,
                    ReportInfo.StartDate,
                    EndDate.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    Convert.ToDecimal(LaborHrs.TotalHours.ToString("N2")),
                    0,
                    0,
                    2,
                    1,
                    ReportInfo.Remark,
                    256,
                    dt.Rows[0]["Plant"].ToString(),
                    dt.Rows[0]["Company"].ToString(),
                    ss.Contains("仓库") ? 1 : 0,
                    IsPrint ? PrintID.ToString() : null,
                    0,
                    0,
                    0,
                    0,
                    0
                });
            sql = string.Format(sql, valueStr);
            SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

            sql = sql.Replace("'", "");
            AddOpLog(null, ReportInfo.JobNum, (int)ReportInfo.AssemblySeq, (int)ReportInfo.JobSeq, 102, OpDate, sql);

            //清空当前用户的process
            sql = "update process set StartDate= null, EndDate=null ,Qty=null,JobNum=null,AssemblySeq=null  ,JobSeq=null ,OpCode=null ,OpDesc=null where userid = '" + HttpContext.Current.Session["UserId"].ToString() + "'";
            SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

            sql = sql.Replace("'", "");
            AddOpLog(null, ReportInfo.JobNum, (int)ReportInfo.AssemblySeq, (int)ReportInfo.JobSeq, 102, OpDate, sql);

            return "处理成功";
        }


        public static string CheckerCommit(OpReport CheckInfo)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string sql = @"select * from BPM where Id = " + CheckInfo.ID + "";

            OpReport theReport = CommonRepository.DataTableToList<OpReport>(SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

            if (theReport.IsDelete == true)
                return "错误：该批次的流程已删除";
            
            if (theReport.IsComplete == true)
                return "错误：该批次的流程已结束";

            if (theReport.Status != 2)
                return "错误：流程未在当前节点上";

            string res = CheckJobHeadState(theReport.JobNum);
            if (res != "正常")
                return "0|错误：" + res;

            DataTable dt = GetJobSeqInfo(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq); //erp抓取该工序的最新信息
            if (dt == null)
            {
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, "UPDATE BPm SET isdelete = 1  where ID = " + theReport.ID + "", null);
                return "0|错误：当前工序不存在，该报工流程已被自动删除";
            }

            if (dt.Rows[0]["OpCode"].ToString() != theReport.OpCode)
            {
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, "UPDATE BPm SET isdelete = 1  where ID = " + theReport.ID + "", null);
                return "0|错误：原工序编号" + theReport.OpCode + "， 现工序编号：" + dt.Rows[0]["OpCode"].ToString() + "， 该报工流程已被自动删除";
            }

            if (IsOpSeqComplete(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq))
                return "错误：该工序已完成";

            string ss = GetNextSetpInfo(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, dt.Rows[0]["Company"].ToString());
            if (ss.Substring(0, 1).Trim() == "0")
                return "0|错误：获取下工序去向失败，" + ss;


            CheckInfo.QualifiedQty = Convert.ToDecimal(CheckInfo.QualifiedQty);
            CheckInfo.UnQualifiedQty = Convert.ToDecimal(CheckInfo.UnQualifiedQty);

            if (CheckInfo.QualifiedQty < 0)
                return "错误：合格数量不能为负";

            if (CheckInfo.UnQualifiedQty < 0)
                return "错误：不合格数量不能为负";

            if (CheckInfo.QualifiedQty + CheckInfo.UnQualifiedQty != theReport.FirstQty)
                return "错误：不合格数 + 合格数 不等于报工数";


            string Character05;
            int TranID = -1;
            decimal start = Convert.ToDecimal(theReport.StartDate.TimeOfDay.TotalHours.ToString("N2"));
            decimal end = Convert.ToDecimal(theReport.EndDate.TimeOfDay.TotalHours.ToString("N2"));
            res = ErpAPI.OpReport.D0505("", theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, (decimal)CheckInfo.QualifiedQty, (decimal)CheckInfo.UnQualifiedQty, CheckInfo.UnQualifiedReason, "", theReport.StartDate, theReport.EndDate, theReport.Company, out Character05,out TranID);
            if (res.Substring(0, 1).Trim() != "1")
                return "错误：" + res;


            sql = "update bpm set " +
                   "CheckUser = '" + HttpContext.Current.Session["UserId"].ToString() + "', " +
                   "CheckDate = '" + OpDate + "'," +
                   "TransformUserGroup = '" + CheckInfo.TransformUserGroup + "'," +
                   "QualifiedQty = " + CheckInfo.QualifiedQty + ", " +
                   "UnQualifiedQty = " + CheckInfo.UnQualifiedQty + "," +
                   "UnQualifiedReason = '" + CommonRepository.GetValueAsString(CheckInfo.UnQualifiedReason) + "'," +
                   "Status = " + (theReport.Status + 1) + "," +
                   "PreStatus = " + (theReport.Status) + "," +
                   "Character05 = '" + Character05 + "'," +
                   "tranid = " + (TranID == -1 ? "null" : TranID.ToString()) + "," +
                   "AtRole = 512, " +
                   "CheckCounter = " + (CheckInfo.UnQualifiedQty > 0 ? 1 : 2) + " " +
                   "where id = " + (theReport.ID) + "";


            SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
            sql = sql.Replace("'", "");
            AddOpLog(theReport.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 201, OpDate, sql);

            return "处理成功";
        }


        public static string DMRCommit(OpReport DMRInfo)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string sql = @"select * from BPM where Id = " + DMRInfo.ID + "";

            OpReport theReport = CommonRepository.DataTableToList<OpReport>(SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

            if (theReport.IsDelete == true)
                return "错误：所属的主流程已删除";
            if (theReport.CheckCounter == 2)
                return "错误：该不良品流程已处理";


            DMRInfo.DMRQualifiedQty = Convert.ToDecimal(DMRInfo.DMRQualifiedQty);
            DMRInfo.DMRRepairQty = Convert.ToDecimal(DMRInfo.DMRRepairQty);
            DMRInfo.DMRUnQualifiedQty = Convert.ToDecimal(DMRInfo.DMRUnQualifiedQty);


            if (DMRInfo.DMRQualifiedQty < 0)
                return "错误：让步数量不能为负";

            if (DMRInfo.DMRRepairQty < 0)
                return "错误：返修数量不能为负";

            if(DMRInfo.DMRUnQualifiedQty < 0)
                return "错误：废弃数量不能为负";

            if (DMRInfo.QualifiedQty +DMRInfo.DMRRepairQty + DMRInfo.DMRUnQualifiedQty != theReport.UnQualifiedQty)
                return "错误：让步数 + 返修数 + 废弃数 不等于原待检数";


            sql =  "update bpm set " +
                   "DMRQualifiedQty = " + DMRInfo.DMRQualifiedQty + ", " +
                   "DMRRepairQty = " + DMRInfo.DMRRepairQty + "," +
                   "DMRUnQualifiedQty = " +DMRInfo.DMRUnQualifiedQty + "," +
                   "DMRUnQualifiedReason = '" + DMRInfo.DMRUnQualifiedReason + "', " +
                   "DMRJobNum = '" + DMRInfo.DMRJobNum + "'," +
                   "DMRWarehouseCode = '" + DMRInfo.DMRWarehouseCode + "'," +
                   "DMRBinNum = '" + DMRInfo.DMRBinNum + "' " +
                   "where id = " + DMRInfo.ID + "";
            SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);


            int DMRID; string res;
            if (theReport.ErpCounter < 1)//让步
            {
                res = ErpAPI.OpReport.StartInspProcessing((int)theReport.TranID, (decimal)DMRInfo.DMRQualifiedQty, (decimal)DMRInfo.DMRUnQualifiedQty,theReport.DMRUnQualifiedReason, theReport.DMRWarehouseCode, theReport.DMRBinNum, out DMRID);
                if (res.Substring(0, 1).Trim() != "1")
                    return "错误：" + res;

                if (DMRInfo.DMRQualifiedQty > 0)
                {
                    InsertConcessionRecord((int)DMRInfo.ID, (decimal)DMRInfo.DMRQualifiedQty, DMRInfo.TransformUserGroup);                   
                    AddOpLog(theReport.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 301, OpDate, "让步接收子流程生成");
                }

                string dmrid = DMRInfo.DMRQualifiedQty == theReport.UnQualifiedQty ? "null" : DMRID.ToString();
                sql = " update bpm set ErpCounter = 1, DMRID = "+ dmrid + " where id = " + DMRInfo.ID + "";
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
            }

            if (theReport.ErpCounter < 2)//返修
            {
                sql = @"select dmrid from bpm where id = " + DMRInfo.ID + "";
                object dmrid = SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                if (!Convert.IsDBNull(dmrid) && DMRInfo.DMRRepairQty > 0)
                {
                    res = ErpAPI.OpReport.RepairDMRProcessing((int)dmrid, theReport.Company, theReport.Plant, theReport.PartNum, (decimal)DMRInfo.DMRRepairQty, DMRInfo.DMRJobNum);
                    if (res.Substring(0, 1).Trim() != "1")
                        return "错误：" + res;

                    InsertRepairRecord((int)DMRInfo.ID, (decimal)DMRInfo.DMRRepairQty, DMRInfo.DMRJobNum, (int)dmrid, DMRInfo.TransformUserGroup);
                    AddOpLog(theReport.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 301, OpDate, "返修子流程生成");
                }
                sql = " update bpm set ErpCounter = 2 where id = " + (theReport.ID) + "";
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
            }

            if (theReport.ErpCounter < 3)//报废
            {
                sql = @"select dmrid from bpm where id = " + DMRInfo.ID + "";
                object dmrid = SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                if (!Convert.IsDBNull(dmrid) && DMRInfo.DMRUnQualifiedQty > 0)
                {
                    res = ErpAPI.OpReport.RefuseDMRProcessing(theReport.Company, theReport.Plant, (decimal)DMRInfo.DMRUnQualifiedQty, DMRInfo.DMRUnQualifiedReason, (int)dmrid);
                    if (res.Substring(0, 1).Trim() != "1")
                        return "错误：" + res;

                    InsertDiscardRecord((int)DMRInfo.ID, (decimal)DMRInfo.DMRUnQualifiedQty,DMRInfo.DMRUnQualifiedReason, (int)dmrid, DMRInfo.DMRWarehouseCode, DMRInfo.DMRBinNum, DMRInfo.TransformUserGroup);
                    AddOpLog(theReport.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 301, OpDate, "报废子流程生成");
                }
                sql = " update bpm set ErpCounter = 3 where id = " + (theReport.ID) + "";
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
            }

            return "处理成功";
        }


        private static string TransferCommitOfSub(OpReport TransmitInfo)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string sql = @"select * from bpmsub where Id = " + TransmitInfo.ID + "";

            DataTable dt;

            OpReport theSUbReport = CommonRepository.DataTableToList<OpReport>(SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

            if (theSUbReport.IsComplete == true)
                return "错误：该批次的流程已结束";

            if (theSUbReport.IsDelete == true)
                return "错误：该批次的流程已删除";

            if (theSUbReport.Status != 3)
                return "错误：流程未在当前节点上";


            if (!Convert.IsDBNull(theSUbReport.DMRQualifiedQty))
            {
                string res = CheckJobHeadState(theSUbReport.JobNum);
                if (res != "正常")
                    return "0|错误：" + res;

                dt = GetJobSeqInfo(theSUbReport.JobNum, (int)theSUbReport.AssemblySeq, (int)theSUbReport.JobSeq); //erp抓取该工序的最新信息

                sql = "select isdelete from bpm where ID = " + theSUbReport.RelatedID + "";
                bool mainIsDeleted = (bool)(SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null));
                if (mainIsDeleted)
                {
                    SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, "UPDATE bpmsub SET isdelete = 1  where ID = " + theSUbReport.ID + "", null);
                    return "0|错误：所属的主流程已被删除，该子流程已被自动删除";
                }

                if (dt == null)
                {
                    SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, "UPDATE bpmsub SET isdelete = 1  where ID = " + theSUbReport.ID + "", null);
                    return "0|错误：当前工序不存在，该报工流程已被自动删除";
                }

                if (dt.Rows[0]["OpCode"].ToString() != theSUbReport.OpCode)
                {
                    SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, "UPDATE bpmsub SET isdelete = 1  where ID = " + theSUbReport.ID + "", null);
                    return "0|错误：原工序编号" + theSUbReport.OpCode + "， 现工序编号：" + dt.Rows[0]["OpCode"].ToString() + "， 该报工流程已被自动删除";
                }

                if (IsOpSeqComplete(theSUbReport.JobNum, (int)theSUbReport.AssemblySeq, (int)theSUbReport.JobSeq))
                    return "错误：该工序已完成";


                string ss = GetNextSetpInfo(theSUbReport.JobNum, (int)theSUbReport.AssemblySeq, (int)theSUbReport.JobSeq, dt.Rows[0]["Company"].ToString());
                if (ss.Substring(0, 1).Trim() == "0")
                    return "0|错误：获取下工序去向失败，" + ss;

                string[] arr = ss.Split('~');
                sql = " update bpmsub set " +
                        "TransformUser = '" + HttpContext.Current.Session["UserId"].ToString() + "', " +
                        "NextJobSeq = " + (arr[0] == "仓库" ? theSUbReport.JobSeq : int.Parse(arr[0])) + "," +
                        "NextOpCode = '" + arr[1] + "'," +
                        "NextOpDesc = '" + arr[2] + "'," +
                        "TransformDate = '" + OpDate + "'," +
                        "NextUserGroup = '" + TransmitInfo.NextUserGroup + "'," +
                        "Status = " + (theSUbReport.Status + 1) + "," +
                        "PreStatus = " + (theSUbReport.Status) + "," +
                        "AtRole = " + GetNextRole((int)theSUbReport.RelatedID) + " " +
                        "where id = " + (theSUbReport.ID) + "";
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = sql.Replace("'", "");
                AddOpLog(theSUbReport.ID, theSUbReport.JobNum, (int)theSUbReport.AssemblySeq, (int)theSUbReport.JobSeq, 301, OpDate, "让步提交|" + sql);
            }

            if (!Convert.IsDBNull(theSUbReport.DMRUnQualifiedQty))
            {
                sql = " update bpmsub set " +
                        "TransformUser = '" + HttpContext.Current.Session["UserId"].ToString() + "', " +
                        "TransformDate = '" + OpDate + "'," +
                        "Status = " + 99 + "," +
                        "PreStatus = " + 3 + "," +
                        "AtRole = " + 512 + " " +
                        "where id = " + (theSUbReport.ID) + "";
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = sql.Replace("'", "");
                AddOpLog(theSUbReport.ID, theSUbReport.JobNum, (int)theSUbReport.AssemblySeq, (int)theSUbReport.JobSeq, 301, OpDate, "报废提交|" + sql);
            }

            if (!Convert.IsDBNull(theSUbReport.DMRRepairQty))
            {
                sql = " update bpmsub set " +
                        "TransformUser = '" + HttpContext.Current.Session["UserId"].ToString() + "', " +
                        "TransformDate = '" + OpDate + "'," +
                        "Status = " + 99 + "," +
                        "PreStatus = " + 3 + "," +
                        "AtRole = " + 512 + " " +
                        "where id = " + (theSUbReport.ID) + "";
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = sql.Replace("'", "");
                AddOpLog(theSUbReport.ID, theSUbReport.JobNum, (int)theSUbReport.AssemblySeq, (int)theSUbReport.JobSeq, 301, OpDate, "报废提交|" + sql);
            }
           

            return "处理成功";
        }


        private static string TransferCommitOfMain(OpReport TransmitInfo)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string sql = @"select * from BPM where Id = " + TransmitInfo.ID + "";

            DataTable dt;

            OpReport theReport = CommonRepository.DataTableToList<OpReport>(SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

            if (theReport.IsDelete == true)
                return "错误：该批次的流程已删除";

            if (theReport.IsComplete == true)
                return "错误：该批次的流程已结束";

            if (theReport.Status != 3)
                return "错误：流程未在当前节点上";

            string res = CheckJobHeadState(theReport.JobNum);
            if (res != "正常")
                return "0|错误：" + res;

            dt = GetJobSeqInfo(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq); //erp抓取该工序的最新信息

            if (dt == null)
            {
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, "UPDATE BPm SET isdelete = 1  where ID = " + theReport.ID + "", null);
                return "0|错误：当前工序不存在，该报工流程已被自动删除";
            }

            if (dt.Rows[0]["OpCode"].ToString() != theReport.OpCode)
            {
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, "UPDATE BPm SET isdelete = 1  where ID = " + theReport.ID + "", null);
                return "0|错误：原工序编号" + theReport.OpCode + "， 现工序编号：" + dt.Rows[0]["OpCode"].ToString() + "， 该报工流程已被自动删除";
            }          

            if (IsOpSeqComplete(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq))
                return "错误：该工序已完成";


            string ss = GetNextSetpInfo(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, dt.Rows[0]["Company"].ToString());
            if (ss.Substring(0, 1).Trim() == "0")
                return "0|错误：获取下工序去向失败，" + ss;

            //再回写主表
            string[] arr = ss.Split('~');
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
            SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);


            sql = sql.Replace("'", "");
            AddOpLog(theReport.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 301, OpDate, sql);

            return "处理成功";
        }


        public static string TransferCommit(OpReport TransmitInfo)
        {
            string res;
            if (!(bool)TransmitInfo.IsSubProcess)
                res = TransferCommitOfMain(TransmitInfo);
            else
                res = TransferCommitOfSub(TransmitInfo);

            return res;
        }


        public static string AccepterCommit(OpReport AcceptInfo)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string sql = @"select * from BPM where Id = " + AcceptInfo.ID + "";

            OpReport theReport = CommonRepository.DataTableToList<OpReport>(SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

            if (theReport.IsDelete == true)
                return "错误：该批次的流程已删除";

            if (theReport.IsComplete == true)
                return "错误：该批次的流程已结束";

            if (theReport.Status != 4)
                return "错误：流程未在当前节点上";

            string res = CheckJobHeadState(theReport.JobNum);
            if (res != "正常")
                return "错误：" + res;

            DataTable dt = GetJobSeqInfo(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq); //erp抓取该工序的最新信息

            if (dt == null)
            {
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, "UPDATE BPm SET isdelete = 1  where ID = " + theReport.ID + "", null);
                return "0|错误：当前工序不存在，该报工流程已被自动删除";
            }

            if (dt.Rows[0]["OpCode"].ToString() != theReport.OpCode)
            {
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, "UPDATE BPm SET isdelete = 1  where ID = " + theReport.ID + "", null);
                return "0|错误：原工序编号" + theReport.OpCode + "， 现工序编号：" + dt.Rows[0]["OpCode"].ToString() + "， 该报工流程已被自动删除";
            }

            if ((bool)theReport.IsSubProcess)
            {
                sql = "select IsComplete from bpm where ID = " + theReport.RelatedID + "";
                bool IsComplete = (bool)(SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null));
                if (!IsComplete)
                {
                    return "0|错误：所属的主流程还未完成";
                }

                sql = "select isdelete from bpm where ID = " + theReport.RelatedID + "";
                bool mainIsDeleted = (bool)(SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null));
                if (mainIsDeleted)
                {
                    SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, "UPDATE BPm SET isdelete = 1  where ID = " + theReport.ID + "", null);
                    return "0|错误：所属的主流程已被删除，该子流程已被自动删除";
                }
            }

            //string CreateUser = (string)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, @" Select NextUser  from BPMOpCode  where  OpCode = '" + theReport.OpCode + "' ", null);
            //if (!CreateUser.Contains(HttpContext.Current.Session["UserId"].ToString()))
            //    return "0|错误：该账号没有该工序操作权限";

            if (IsOpSeqComplete(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq))
                return "错误：该工序已完成";

            string ss = GetNextSetpInfo(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, dt.Rows[0]["Company"].ToString());
            if (ss.Substring(0, 1).Trim() == "0")
                return "0|错误：获取下工序去向失败，" + ss;


            decimal CurrQty = theReport.IsSubProcess == false ? (decimal)theReport.QualifiedQty : (decimal)theReport.DMRQualifiedQty;
            if (theReport.IsSubProcess == false) //主流程检测超报
            {
                object PreOpSeq = CommonRepository.GetPreOpSeq(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq);
                if (PreOpSeq == null && CommonRepository.GetReqQtyOfAssemblySeq(theReport.JobNum, (int)theReport.AssemblySeq) < CurrQty + GetTotalQtyOfJobSeq(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, (int)theReport.ID))
                    return "错误： 报工数超出该阶层物料的需求数量";
                if (PreOpSeq != null && CommonRepository.GetOpSeqCompleteQty(theReport.JobNum, (int)theReport.AssemblySeq, (int)PreOpSeq) < CurrQty + GetTotalQtyOfJobSeq(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, (int)theReport.ID))
                    return "错误： 报工数超出上一道工序的完成数量";
            }


            //自动回退检测
            string[] arr = ss.Split('~');
            if ((theReport.NextJobSeq == theReport.JobSeq && arr[0] != "仓库") || (theReport.NextOpCode != arr[1]))
            {
                ReturnStatus((int)theReport.ID, (int)theReport.Status, 11, "工序去向已更改，自动回退", 401);
            }


            #region 主或子的erp交互
            if ((bool)theReport.IsSubProcess)
            {

                if (theReport.ErpCounter < 1)
                {
                    if (theReport.AtRole == 8 && theReport.DMRQualifiedQty > 0)
                    {
                        sql = @"select binnum from bpm where id = " + theReport.RelatedID + "";
                        string binnum = (string)(SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null));

                        res = ErpAPI.Common.D0506_01(null, theReport.JobNum, (int)theReport.AssemblySeq, (decimal)theReport.DMRQualifiedQty, theReport.JobNum, theReport.NextOpCode, binnum, theReport.Company);
                        if (res != "1|处理成功")
                        {
                            //AddOpLog(theReport.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 401, OpDate, "ID：" + theReport.ID + "，入库失败 |" + res);
                            return "错误：" + res;
                        }
                    }

                    sql = " update bpm set ErpCounter = 1 where id = " + (theReport.ID) + "";
                    SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
                }

                decimal qty1 = Convert.ToDecimal(theReport.DMRRepairQty) + Convert.ToDecimal(theReport.DMRUnQualifiedQty);
                if (qty1 > 0)
                {
                    // 不合格品界面
                    if (theReport.ErpCounter < 2)
                    {
                        int tranID = -1;
                        res = ErpAPI.OpReport.Startnonconf(theReport.JobNum, (int)theReport.AssemblySeq, theReport.Company, (int)theReport.JobSeq, qty1, theReport.DMRWarehouseCode, theReport.DMRBinNum, theReport.UnQualifiedReason, theReport.Plant, out tranID);
                        if (res.Substring(0, 1).Trim() == "0")
                            return "错误：" + res;

                        sql = " update bpm set ErpCounter = 2, tranid = " + tranID + " where id = " + (theReport.ID) + "";
                        SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
                    }

                    //检验处理
                    if (theReport.ErpCounter < 3)
                    {
                        sql = @"select tranid from bpm where id = " + theReport.ID + "";
                        int tranID = (int)(SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null));

                        int DMRID = -1;
                        res = ErpAPI.OpReport.StartInspProcessing(tranID, qty1, theReport.DMRUnQualifiedReason, theReport.DMRWarehouseCode, theReport.DMRBinNum, out DMRID);
                        if (res.Substring(0, 1).Trim() == "0")
                            return "错误：" + res;

                        sql = " update bpm set ErpCounter = 3, DMRID = " + DMRID + " where id = " + (theReport.ID) + "";
                        SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                    }
                    //返修
                    if (theReport.ErpCounter < 4)
                    {
                        sql = @"select dmrid from bpm where id = " + theReport.ID + "";
                        int DMRID = (int)(SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null));
                        if (Convert.ToDecimal(theReport.DMRRepairQty) > 0)
                        {
                            res = ErpAPI.OpReport.RepairDMRProcessing(DMRID, theReport.Company, theReport.Plant, theReport.PartNum, (decimal)theReport.DMRRepairQty, theReport.DMRJobNum);
                            if (res.Substring(0, 1).Trim() == "0")
                                return "错误：" + res;
                        }
                        sql = " update bpm set ErpCounter = 4 where id = " + (theReport.ID) + "";
                        SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
                    }
                    //拒收
                    if (theReport.ErpCounter < 5)
                    {
                        sql = @"select DMRID from bpm where id = " + theReport.ID + "";
                        int DMRID = (int)(SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null));

                        if (Convert.ToDecimal(theReport.DMRRepairQty) > 0)
                        {
                            res = ErpAPI.OpReport.RefuseDMRProcessing(theReport.Company, theReport.Plant, (decimal)theReport.DMRUnQualifiedQty, theReport.DMRUnQualifiedReason, DMRID);
                            if (res.Substring(0, 1).Trim() == "0")
                                return "错误：" + res;
                        }

                        sql = " update bpm set ErpCounter = 5 where id = " + (theReport.ID) + "";
                        SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
                    }
                }
            }
            else
            {
                // 写入erp时间费用
                if (theReport.ErpCounter < 1)
                {
                    string Character05;
                    decimal start = Convert.ToDecimal(theReport.StartDate.TimeOfDay.TotalHours.ToString("N2"));
                    decimal end = Convert.ToDecimal(theReport.EndDate.TimeOfDay.TotalHours.ToString("N2"));
                    res = ErpAPI.OpReport.D0505("", theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, (decimal)theReport.FirstQty, 0, "", "", theReport.StartDate, theReport.EndDate, theReport.Company, out Character05);
                    if (res.Substring(0, 1).Trim() != "1")
                        return "错误：" + res;

                    sql = " update bpm set ErpCounter = 1, Character05 = '" + Character05 + "' where id = " + (theReport.ID) + "";
                    SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
                }

                //若去向仓库
                if (theReport.AtRole == 8)
                {
                    if (theReport.ErpCounter < 2)
                    {
                        res = ErpAPI.Common.D0506_01(null, theReport.JobNum, (int)theReport.AssemblySeq, (decimal)theReport.QualifiedQty, theReport.JobNum, theReport.NextOpCode, AcceptInfo.BinNum, theReport.Company);
                        if (res != "1|处理成功")
                        {
                            //AddOpLog(theReport.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 401, OpDate, "ID：" + theReport.ID + "，入库失败 |" + res);
                            return "错误：" + res;
                        }

                        sql = " update bpm set ErpCounter = 2 where id = " + (theReport.ID) + "";
                        SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
                    }
                }
            }
            #endregion


            //再回写主表
            sql = " update bpm set " +
                   "NextUser = '" + HttpContext.Current.Session["UserId"].ToString() + "', " +
                   "NextDate = '" + OpDate + "'," +
                   "Status = 99," +
                   "PreStatus = " + (theReport.Status) + "," +
                   "IsComplete = 1," +
                   "BinNum = '" + CommonRepository.GetValueAsString(AcceptInfo.BinNum) + "' " +
                   "where id = " + (theReport.ID) + "";

            SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);


            sql = sql.Replace("'", "");
            AddOpLog(theReport.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 401, OpDate, sql);

            return "处理成功";
        }


        public static IEnumerable<OpReport> GetRemainsOfUser()
        {
            if ((int)HttpContext.Current.Session["RoleId"] == 1024)
            {
                string sql = @"select * from BPM where CHARINDEX(company, '{0}') > 0   and   CHARINDEX(Plant, '{1}') > 0 and checkcounter = 1 order by CreateDate desc";
                sql = string.Format(sql, HttpContext.Current.Session["Company"].ToString(), HttpContext.Current.Session["Plant"].ToString());

                DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql);
                if (dt == null)
                    return null;

                //for (int i = dt.Rows.Count - 1; i >= 0; i--)
                //{
                //    if (!(dt.Rows[i]["UnQualifiedGroup"].ToString()).Contains(HttpContext.Current.Session["UserId"].ToString()))
                //        dt.Rows[i].Delete();
                //}

                List<OpReport> Remains = CommonRepository.DataTableToList<OpReport>(dt);

                return Remains;
            }
            else
            {
                string sql = @"select * from BPM where AtRole & {0} != 0 and isdelete != 1 and isComplete != 1 and CHARINDEX(company, '{1}') > 0   and   CHARINDEX(Plant, '{2}') > 0 order by CreateDate desc";
                sql = string.Format(sql, (int)HttpContext.Current.Session["RoleId"], HttpContext.Current.Session["Company"].ToString(), HttpContext.Current.Session["Plant"].ToString());

                DataTable dt1 = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql);

                sql = @"select * from BPMsub where AtRole & {0} != 0 and isdelete != 1 and isComplete != 1 and CHARINDEX(company, '{1}') > 0   and   CHARINDEX(Plant, '{2}') > 0 order by CreateDate desc";
                sql = string.Format(sql, (int)HttpContext.Current.Session["RoleId"], HttpContext.Current.Session["Company"].ToString(), HttpContext.Current.Session["Plant"].ToString());

                DataTable dt2 = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql);

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

                return Remains;
            }
        }


        public static string GetProcessOfUser() //retun 工单号~阶层号~工序序号~工序代码~工序描述~NextJobSeq NextOpCode NextOpDesc startdate Qty  CompleteQty
        {
            string sql = @"select * from process where userid = '" + HttpContext.Current.Session["UserId"].ToString() + "'";
            DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql);


            if (dt != null && (!Convert.IsDBNull(dt.Rows[0]["EndDate"]) || !Convert.IsDBNull(dt.Rows[0]["StartDate"])))//未结束
            {
                string ss = GetNextSetpInfo(dt.Rows[0]["JobNum"].ToString(), (int)dt.Rows[0]["AssemblySeq"], (int)dt.Rows[0]["JobSeq"], "001");
                if (ss.Substring(0, 1).Trim() == "0")
                    return "0|错误：无法获取工序最终去向，" + ss;

                return "1|" + (string)dt.Rows[0]["JobNum"] + "~" + dt.Rows[0]["AssemblySeq"].ToString() + "~" + dt.Rows[0]["JobSeq"].ToString() + "~" +
                    (string)dt.Rows[0]["OpCode"] + "~" + (string)dt.Rows[0]["OpDesc"] + "~" + ss + "~" + ((DateTime)dt.Rows[0]["StartDate"]).ToString("yyyy-MM-dd HH:mm:ss.fff") + "~" +
                    (dt.Rows[0]["Qty"].ToString() == "" ? "0" : dt.Rows[0]["Qty"].ToString()) + "~" + GetTotalQtyOfJobSeq(dt.Rows[0]["JobNum"].ToString(), (int)dt.Rows[0]["AssemblySeq"], (int)dt.Rows[0]["JobSeq"], 0).ToString(); ;
            }
            return null;
        }


        public static DataTable GetNextUserGroup(string OpCode, int id)
        {
            DataTable dt = null;
            string sql = null;
            long nextRole = GetNextRole(id);

            if (nextRole == 8)//从拥有权值8的人员表中，选出可以操作指定仓库的人
            {
                sql = "select * from bpm where id = " + id + "";
                dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql);


                sql = @"select WarehouseCode from erp.partplant pp  LEFT JOIN erp.Warehse wh on pp.PrimWhse = wh.WarehouseCode and pp.Company = wh.Company and pp.Plant = wh.Plant   
                      where pp.company = '" + dt.Rows[0]["Company"].ToString() + "'   and pp.plant = '" + dt.Rows[0]["Plant"].ToString() + "'   and   pp.PartNum = '" + dt.Rows[0]["PartNum"].ToString() + "'";
                object Warehouse = SQLRepository.ExecuteScalarToObject(SQLRepository.ERP_strConn, CommandType.Text, sql, null);


                if (Warehouse == null) return null;


                sql = "select UserID,UserName, WhseGroup from userfile where CHARINDEX('" + dt.Rows[0]["Company"].ToString() + "', company) > 0 and CHARINDEX('" + dt.Rows[0]["Plant"].ToString() + "', plant) > 0 and disabled = 0 and RoleID & " + nextRole + " != 0 and RoleID != 2147483647";
                dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表

                for (int i = dt.Rows.Count - 1; i >= 0; i--)
                {
                    if (dt.Rows[i]["WhseGroup"] != null)
                    {
                        string[] ss = dt.Rows[i]["WhseGroup"].ToString().Split(',');
                        if (!ss.Contains(Warehouse.ToString().Trim()))
                            dt.Rows.RemoveAt(i);
                    }
                }
            }
            else if (nextRole == 16)
            {
                dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, "select * from bpm where id = " + id + "");

                sql = "select UserID, UserName from userfile where disabled = 0 and  CHARINDEX('" + dt.Rows[0]["Company"].ToString() + "', company) > 0 and CHARINDEX('" + dt.Rows[0]["Plant"].ToString() + "', plant) > 0 and RoleID & " + nextRole + " != 0 and RoleID != 2147483647";
                dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表
            }
            else if (nextRole == 256)
            {
                sql = "select CheckUser from BPMOpCode where OpCode = '" + OpCode + "'";
                string CheckUser = (string)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = "select UserID, UserName from userfile where disabled = 0 and CHARINDEX(userid, '" + CheckUser + "') > 0 and RoleID & " + nextRole + " != 0 and RoleID != 2147483647";
                dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表
            }
            else if (nextRole == 512)
            {
                sql = "select TransformUser from BPMOpCode where OpCode = '" + OpCode + "'";
                string TransformUser = (string)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = "select UserID, UserName from userfile where disabled = 0 and CHARINDEX(userid, '" + TransformUser + "') > 0 and RoleID & " + nextRole + " != 0 and RoleID != 2147483647";
                dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表
            }
            else if (nextRole == 128)//
            {
                sql = "select NextOpCode from bpm where id = " + id + "";
                string NextOpCode = (string)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = "select NextUser from BPMOpCode where OpCode = '" + NextOpCode + "'";
                string NextUser = (string)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = "select UserID, UserName from userfile where disabled = 0  and CHARINDEX(userid, '" + NextUser + "') > 0 and RoleID & " + nextRole + " != 0 and RoleID != 2147483647";
                dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表
            }


            return dt == null || dt.Rows.Count == 0 ? null : dt;
        }


        public static DataTable GetAssemblySeqByJobNum(string JobNum)
        {
            string sql = @"select AssemblySeq, PartNum, Description  from erp.JobAsmbl where jobnum = '" + JobNum + "' order by AssemblySeq asc";
            DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.ERP_strConn, sql);

            return dt;
        }


        public static DataTable GetJobSeq(string JobNum, int AssemblySeq)
        {
            string sql = @"select OprSeq,OpDesc,OpCode from erp.JobAsmbl ja left join erp.JobOper jo on ja.JobNum = jo.JobNum and ja.AssemblySeq = jo.AssemblySeq where jo.OpComplete = 0 and ja.jobnum = '" + JobNum + "' and ja.AssemblySeq= '" + AssemblySeq + "' order by OprSeq asc";
            DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.ERP_strConn, sql);
            return dt;
        }


        public static DataTable GetRecordByID(int ID)
        {
            string sql = "select * from bpm where ID = " + ID + "";
            DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql);

            return dt;
        }


        public static ScanResult GetRecordByQR(string values) //公司ID%物料编码 % 描述 % 批次号 %《工单号 % 半成品号 % 标签识别码 %》供应商ID % 订单号 % 行号 % 交货数量 % 发货行%工序号%炉批号
        {
            ScanResult sr = new ScanResult();
            sr.batch = null;
            sr.error = null;

            string[] arr = values.Split('~');

            string sql = "select * from bpm where printID = " + int.Parse(arr[7]) + " and IsComplete != 1 and IsDelete != 1";
            DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql);

            if (dt == null)
            {
                sr.error = "错误：不存在该报工记录";
                return sr;
            }

            OpReport theReport = CommonRepository.DataTableToList<OpReport>(dt).First();
            sr.batch = theReport;


            if (!HttpContext.Current.Session["Company"].ToString().Contains(theReport.Company))
                sr.error = "错误：该账号没有相应的公司权限";
            if (!HttpContext.Current.Session["Plant"].ToString().Contains(theReport.Plant))
                sr.error = "错误：该账号没有相应的工厂权限";
            if ((theReport.AtRole & (int)HttpContext.Current.Session["RoleId"]) == 0)
            {
                sr.error = "错误：当前批次的流程未在你的节点 或 你的角色无权操作当前批次";
            }

            return sr;
        }


        public static DataTable GetReason(string type)
        {
            string sql = "select ReasonCode, Description from erp.Reason where ReasonType = '" + type + "' ";
            DataTable Reasons = SQLRepository.ExecuteQueryToDataTable(SQLRepository.ERP_strConn, sql);

            return Reasons;
        }


        public static string ReturnStatus(int ID, int oristatus, int ReasonID, string remark, int apinum)
        {
            string OpDetail = "", OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string sql = "select * from bpm where ID = " + ID + " ";
            OpReport theReport = CommonRepository.DataTableToList<OpReport>(SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql)).First(); //获取该批次记录


            if ((bool)theReport.IsDelete == true)
                return "错误：该批次的流程已删除";

            if ((bool)(theReport.IsComplete) == true)
                return "错误：该批次的流程已结束";

            if ((int)(theReport.Status) != oristatus)
                return "错误：流程未在当前节点上";

            if (oristatus == 4)
            {
                sql = @"update bpm set NextUserGroup=null, PreStatus = " + theReport.Status + ", AtRole = " + 512 + ", status = " + theReport.PreStatus + ", ReturnThree = ReturnThree+1  where id = " + ID + " ";
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                //获取更新后的回退编号
                sql = "select ReturnThree from  bpm  where id = " + ID + "  ";
                int c = (int)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                //把该回退编号的原因插入到ReasonRecord表中
                sql = @"insert into BPMReturn(BPMID, ReturnThree, ReturnReasonId, ReasonRemark, Date) Values(" + ID + ", " + c + ", " + ReasonID + ",'" + remark + "', '" + OpDate + "')";
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
            }


            AddOpLog(ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, apinum, OpDate, "从" + oristatus.ToString() + "回退成功");
            return "处理成功";
        }




        private static void AddOpLog(int? id, string JobNum, int AssemblySeq, int JobSeq, int ApiNum, string OpDate, string OpDetail)
        {
            string sql = @"insert into BPMLog(JobNum, AssemblySeq, UserId, Opdate, ApiNum, JobSeq, OpDetail,bpmid) Values('{0}', {1}, '{2}', '{3}', {4}, {5}, '{6}',{7}) ";
            sql = string.Format(sql, JobNum, AssemblySeq, HttpContext.Current.Session["UserId"].ToString(), OpDate, ApiNum, JobSeq, OpDetail, id == null ? "null" : id.ToString());

            SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
        }//添加操作记录
    }
}