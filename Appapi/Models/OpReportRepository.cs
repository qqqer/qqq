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
        private static readonly object BPMPrintIDLock = new object();

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


        private static DataTable GetJobSeqInfo(string jobnum, int asmSeq, int oprseq)
        {
            string sql = @"select jh.Company, Plant,OpDesc, jo.OpCode from erp.JobHead jh left join erp.JobOper jo on jh.Company = jo.Company and jh.JobNum =jo.JobNum where jo.JobNum = '" + jobnum + "' and jo.AssemblySeq = " + asmSeq + " and  jo.OprSeq = " + oprseq + "";
            DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.ERP_strConn, sql);

            return dt;
        }


        private static string CheckBinNum(string company, string binnum, string WarehouseCode)
        {
            string sql = "select count(*) from erp.WhseBin where Company = '{0}' and  WarehouseCode = '{1}' and BinNum = '{2}'";
            sql = string.Format(sql, company, WarehouseCode, binnum);
            int exist = (int)SQLRepository.ExecuteScalarToObject(SQLRepository.ERP_strConn, CommandType.Text, sql, null);

            return exist == 0 ? "错误：库位与仓库不匹配" : "ok";
        }


        private static void InsertConcessionRecord(int Id, decimal DMRQualifiedQty, string TransformUserGroup, string dmrid)
        {
            string sql = @"
                   insert into BPMSub   select [CreateUser]
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
                  ,null
                  ,[Character05]
                  ,[TranID]
                  ,{5}
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
                  ,null
                  ,1
                  ,null
                  ,null
                  ,[Responsibility]
             from bpm where id = " + Id + "";

            sql = string.Format(sql, DMRQualifiedQty, Id, 1, TransformUserGroup, HttpContext.Current.Session["UserId"].ToString(), dmrid);

            SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
        }


        private static void InsertRepairRecord(int Id, decimal DMRRepairQty, string DMRJobNum, int DMRID, string TransformUserGroup)
        {
            string sql = @"
                   insert into BPMSub   select [CreateUser]
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
                  ,null
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
                  ,null
                  ,1
                  ,null
                  ,null
                  ,[Responsibility]
             from bpm where id = " + Id + "";

            sql = string.Format(sql, DMRRepairQty, Id, 1, DMRJobNum, DMRID, TransformUserGroup, HttpContext.Current.Session["UserId"].ToString());

            SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
        }


        private static void InsertDiscardRecord(int Id, decimal DMRUnQualifiedQty, string DMRUnQualifiedReason, int DMRID, string DMRWarehouseCode, string DMRBinNum, string TransformUserGroup)
        {
            string sql = @"
               insert into BPMSub   select [CreateUser]
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
              ,[Responsibility]
         from bpm where id = " + Id + "";

            sql = string.Format(sql, DMRUnQualifiedQty, Id, 1, DMRUnQualifiedReason, DMRWarehouseCode, DMRBinNum, DMRID, TransformUserGroup, HttpContext.Current.Session["UserId"].ToString());

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
            string sql = @"select * from bpm where id != " + id + " and isdelete != 1  and  jobnum = '" + jobnum + "' and AssemblySeq = " + asmSeq + " and  JobSeq = " + oprseq + "";

            DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql);

            decimal bpm_qty =0;

            if (dt != null)
            {
                for(int i =0; i < dt.Rows.Count; i++)
                {
                    if ((int)dt.Rows[i]["Status"] < 3) //未写时间费用
                        bpm_qty += (decimal)dt.Rows[i]["FirstQty"];

                    else if ((int)dt.Rows[i]["Status"] > 2 && (int)dt.Rows[i]["CheckCounter"] < 2)//已写时间费用，不良品还未介入
                    {
                        bpm_qty += (decimal)dt.Rows[i]["UnQualifiedQty"];
                    }
                    else //不良品处理已结束
                    {
                        decimal DMRRepairQty =  dt.Rows[i]["DMRRepairQty"] is DBNull || dt.Rows[i]["DMRRepairQty"] == null ? 0 : (decimal)dt.Rows[i]["DMRRepairQty"] ;
                        bpm_qty += DMRRepairQty; //返修数算作原工单的报工数
                    }
                }
            }

            decimal erp_qty = CommonRepository.GetOpSeqCompleteQty(jobnum, asmSeq, oprseq);

            return bpm_qty + erp_qty;
        }



        private static decimal AcceptQtyOfUser(string jobnum, int asmSeq, int oprseq, string userid) //该工序的 在跑+erp 数量, 不包括本次报工数量
        {
            string sql = @"select sum(QualifiedQty) from bpm where NextUser = '" + userid + "' and isdelete != 1  and  jobnum = '" + jobnum + "' and AssemblySeq = " + asmSeq + " and  JobSeq = " + oprseq + "";

            object BPMAcceptQty = SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

            sql = @"select sum(DMRQualifiedQty) from bpmsub where NextUser = '" + userid + "' and isdelete != 1 and UnQualifiedType = 1 and DMRQualifiedQty != null  and  jobnum = '" + jobnum + "' and AssemblySeq = " + asmSeq + " and  JobSeq = " + oprseq + "";

            object BPMSubAcceptQty = SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

            BPMAcceptQty = Convert.IsDBNull(BPMAcceptQty) ? 0 : BPMAcceptQty;
            BPMSubAcceptQty = Convert.IsDBNull(BPMSubAcceptQty) ? 0 : BPMSubAcceptQty;


            return (decimal)BPMAcceptQty + (decimal)BPMSubAcceptQty;
        }


        public static string Start(string values) //retun 工单号~阶层号~工序序号~工序代码~工序描述~NextJobSeq NextOpCode NextOpDesc startdate CompleteQty
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string[] arr = values.Split(' '); //工单号~阶层号~工序序号~工序代码


            string res = CommonRepository.CheckJobHeadState(arr[0]);
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


            int IsOpCodeExist = (int)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, @" Select count(*)  from BPMOpCode where  OpCode = '" + arr[3] + "' ", null);
            if (!Convert.ToBoolean(IsOpCodeExist))
                return "0|错误：当前工序 " + arr[3] + " 未添加，请联系管理员";


            string CreateUser = (string)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, @" Select CreateUser  from BPMOpCode where  OpCode = '" + arr[3] + "' ", null);
            if (!CreateUser.Contains(HttpContext.Current.Session["UserId"].ToString()))
                return "0|错误：该账号没有该工序操作权限";


            object PreOpSeq = CommonRepository.GetPreOpSeq(arr[0], int.Parse(arr[1]), int.Parse(arr[2]));
            if (PreOpSeq != null && AcceptQtyOfUser(arr[0], int.Parse(arr[1]), int.Parse(arr[2]), HttpContext.Current.Session["UserId"].ToString()) == 0)
                return "错误：上工序接收数量为0，无法开始当前工序";


            //if (CommonRepository.IsOpSeqComplete(arr[0], int.Parse(arr[1]), int.Parse(arr[2])))
            //    return "错误：该工序已完成";


            string NextSetpInfo = GetNextSetpInfo(arr[0], int.Parse(arr[1]), int.Parse(arr[2]), dt.Rows[0]["Company"].ToString());
            if (NextSetpInfo.Substring(0, 1).Trim() == "0")
                return "0|错误：无法获取工序最终去向，" + NextSetpInfo;


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


            string TotalQtyOfJobSeq = GetTotalQtyOfJobSeq(arr[0], int.Parse(arr[1]), int.Parse(arr[2]), 0).ToString();

            arr[1] += "|" + (string)SQLRepository.ExecuteScalarToObject(SQLRepository.ERP_strConn, CommandType.Text, @" select PartNum from erp.JobAsmbl where JobNum = '" + arr[0] + "' and AssemblySeq = " + int.Parse(arr[1]) + "", null);
            return "1|" + arr[0] + "~" + arr[1] + "~" + arr[2] + "~" + arr[3] + "~" + OpDesc + "~" + NextSetpInfo + "~" + OpDate + "~" + TotalQtyOfJobSeq;
        }


        public static string ReporterCommit(OpReport ReportInfo)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            DataTable dt, dt2;

            string res = CommonRepository.CheckJobHeadState(ReportInfo.JobNum);
            if (res != "正常")
                return "0|错误：" + res;

            if ((dt = GetJobSeqInfo(ReportInfo.JobNum, (int)ReportInfo.AssemblySeq, (int)ReportInfo.JobSeq)) == null)
                return "0|错误：当前工序不存在";

            dt2 = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, @"select * from process where userid = '" + HttpContext.Current.Session["UserId"].ToString() + "'");
            if (dt.Rows[0]["OpCode"].ToString() != dt2.Rows[0]["OpCode"].ToString())
                return "0|错误：原工序编号" + dt2.Rows[0]["OpCode"].ToString() + "， 现工序编号：" + dt.Rows[0]["OpCode"].ToString();

            //if (CommonRepository.IsOpSeqComplete(ReportInfo.JobNum, (int)ReportInfo.AssemblySeq, (int)ReportInfo.JobSeq))
            //{
            //    ClearProcess();
            //    return "错误：该工序已完成";
            //}

            if (ReportInfo.FirstQty <= 0)
                return "错误：报工数量需大于0";

            if (ReportInfo.CheckUserGroup == "")
                return "错误：下步接收人不能为空";

            object PreOpSeq = CommonRepository.GetPreOpSeq(ReportInfo.JobNum, (int)ReportInfo.AssemblySeq, (int)ReportInfo.JobSeq);
            if (PreOpSeq == null && CommonRepository.GetReqQtyOfAssemblySeq(ReportInfo.JobNum, (int)ReportInfo.AssemblySeq) < ReportInfo.FirstQty + GetTotalQtyOfJobSeq(ReportInfo.JobNum, (int)ReportInfo.AssemblySeq, (int)ReportInfo.JobSeq, 0))
                return "错误： 报工数超出该阶层的可生产数量";

            if (PreOpSeq != null && CommonRepository.GetOpSeqCompleteQty(ReportInfo.JobNum, (int)ReportInfo.AssemblySeq, (int)PreOpSeq) < ReportInfo.FirstQty + GetTotalQtyOfJobSeq(ReportInfo.JobNum, (int)ReportInfo.AssemblySeq, (int)ReportInfo.JobSeq, 0))
                return "错误： 当前报工数超出上一道工序的报工数";

            string NextSetpInfo = GetNextSetpInfo(ReportInfo.JobNum, (int)ReportInfo.AssemblySeq, (int)ReportInfo.JobSeq, dt.Rows[0]["Company"].ToString());
            if (NextSetpInfo.Substring(0, 1).Trim() == "0")
                return "0|错误：获取下工序去向失败，" + NextSetpInfo;



            //为当前工序下的化学品发料
            string issue_res = "";
            DataTable mtls = CommonRepository.GetMtlsOfOpSeq(ReportInfo.JobNum, (int)ReportInfo.AssemblySeq, (int)ReportInfo.JobSeq, "001");
            if (mtls != null)
            {
                for (int j = 0; j < mtls.Rows.Count; j++)
                {
                    if (mtls.Rows[j]["partnum"].ToString().Substring(0, 1).Trim().ToLower() == "c")
                    {
                        res = ErpAPI.MtlIssue.Issue(ReportInfo.JobNum, (int)ReportInfo.AssemblySeq, (int)ReportInfo.JobSeq, (int)mtls.Rows[j]["mtlseq"], mtls.Rows[j]["partnum"].ToString(), (decimal)mtls.Rows[j]["qtyper"] * (decimal)ReportInfo.FirstQty, DateTime.Parse(OpDate), "001", dt.Rows[0]["Plant"].ToString());
                        issue_res += mtls.Rows[j]["partnum"].ToString() + " ";
                        issue_res += (res == "true") ? (decimal)mtls.Rows[j]["qtyper"] * (decimal)ReportInfo.FirstQty + ", " : res.Substring(2);

                        AddOpLog(null, ReportInfo.JobNum, (int)ReportInfo.AssemblySeq, (int)ReportInfo.JobSeq, 102, OpDate, issue_res);
                        if (res != "true")
                            return "错误：" + issue_res;
                    }
                }
            }



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
            if (NextSetpInfo.Contains("仓库"))
            {
                lock (BPMPrintIDLock)//获取并更新BPMPrintID
                {
                    sql = "select BPMPrintID from SerialNumber where name = 'BAT'";
                    PrintID = (int)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                    sql = "UPDATE SerialNumber SET BPMPrintID = BPMPrintID+1  where name = 'BAT'";
                    SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
                }

                string jsonStr = " text1: '{0}', text2: '{12}', text3: '{1}', text4: '{2}', text5: '{3}', text6: '', text7: '{4}', text8: '{5}', text9: '{6}', text10: '{7}', text11: '{8}', text12: '{9}', text13: '', text14: '{10}', text15: '{11}', text16: '', text17: '', text18: '', text19: '', text20: '', text21: '', text22: '', text23: '', text24: '', text25: '', text26: '', text27: '', text28: '', text29: '', text30: '' ";
                jsonStr = string.Format(jsonStr, partinfo.Rows[0]["PartNum"].ToString(), ReportInfo.JobNum, ReportInfo.JobNum, ReportInfo.AssemblySeq.ToString(), PrintID.ToString(), "", "", ReportInfo.FirstQty.ToString(), "", dt.Rows[0]["Company"].ToString(), ReportInfo.JobSeq.ToString(), "", partinfo.Rows[0]["Description"].ToString());
                jsonStr = "[{" + jsonStr + "}]";


                sql = "select Printer from Userprinter where userid = '" + HttpContext.Current.Session["UserId"].ToString() + "' ";
                string printer = (string)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);
                ServiceReference_Print.WebServiceSoapClient client = new ServiceReference_Print.WebServiceSoapClient();
                if ((res = client.Print(@"C:\D0201.btw", printer, (int)ReportInfo.PrintQty, jsonStr)) == "1|处理成功")
                {
                    IsPrint = true;
                    client.Close();                 
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
                    NextSetpInfo.Contains("仓库") ? 1 : 0,
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

            string res = CommonRepository.CheckJobHeadState(theReport.JobNum);
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

            //if (CommonRepository.IsOpSeqComplete(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq))
            //    return "错误：该工序已完成";


            //object PreOpSeq = CommonRepository.GetPreOpSeq(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq);
            //if (PreOpSeq == null && CommonRepository.GetReqQtyOfAssemblySeq(theReport.JobNum, (int)theReport.AssemblySeq) < theReport.FirstQty + GetTotalQtyOfJobSeq(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, (int)theReport.ID))
            //    return "错误： 报工数超出该阶层的可生产数量";
            //if (PreOpSeq != null && CommonRepository.GetOpSeqCompleteQty(theReport.JobNum, (int)theReport.AssemblySeq, (int)PreOpSeq) < theReport.FirstQty + GetTotalQtyOfJobSeq(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, (int)theReport.ID))
            //    return "错误： 当前报工数超出上一道工序的报工数";


            string NextSetpInfo = GetNextSetpInfo(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, dt.Rows[0]["Company"].ToString());
            if (NextSetpInfo.Substring(0, 1).Trim() == "0")
                return "0|错误：获取下工序去向失败，" + NextSetpInfo;


            CheckInfo.QualifiedQty = Convert.ToDecimal(CheckInfo.QualifiedQty);
            CheckInfo.UnQualifiedQty = Convert.ToDecimal(CheckInfo.UnQualifiedQty);

            if (CheckInfo.QualifiedQty < 0)
                return "错误：合格数量不能为负";

            if (CheckInfo.UnQualifiedQty < 0)
                return "错误：不合格数量不能为负";

            if (CheckInfo.UnQualifiedQty > 0 && CheckInfo.UnQualifiedReason == "")
                return "错误：不合格原因不能为空";

            if (CheckInfo.QualifiedQty + CheckInfo.UnQualifiedQty != theReport.FirstQty)
                return "错误：不合格数 + 合格数 不等于报工数";


            string Character05;
            int TranID = -1;
            //decimal start = Convert.ToDecimal(theReport.StartDate.TimeOfDay.TotalHours.ToString("N2"));
            //decimal end = Convert.ToDecimal(theReport.EndDate.TimeOfDay.TotalHours.ToString("N2"));
            res = ErpAPI.OpReport.TimeAndCost("", theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, (decimal)CheckInfo.QualifiedQty, (decimal)CheckInfo.UnQualifiedQty, CheckInfo.UnQualifiedReason, "", theReport.StartDate, theReport.EndDate, theReport.Company, theReport.Plant, (decimal)theReport.LaborHrs, out Character05, out TranID);
            if (res.Substring(0, 1).Trim() != "1")
                return "错误：" + res;

            AddOpLog(theReport.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 201, OpDate, "时间费用写入成功");


            sql = "update bpm set " +
                   "CheckUser = '" + HttpContext.Current.Session["UserId"].ToString() + "', " +
                   "CheckDate = '" + OpDate + "'," +
                   "TransformUserGroup = '" + CheckInfo.TransformUserGroup + "'," +
                   "QualifiedQty = " + CheckInfo.QualifiedQty + ", " +
                   "UnQualifiedQty = " + CheckInfo.UnQualifiedQty + "," +
                   "UnQualifiedReason = '" + (CheckInfo.UnQualifiedQty > 0 ? CommonRepository.GetValueAsString(CheckInfo.UnQualifiedReason) : "") + "'," +
                   "Status = " + (CheckInfo.QualifiedQty > 0 ? theReport.Status + 1 : 99) + "," +
                   "PreStatus = " + theReport.Status + "," +
                   "Character05 = '" + Character05 + "'," +
                   "tranid = " + (TranID == -1 ? "null" : TranID.ToString()) + "," +
                   "AtRole = 512, " +
                   "CheckCounter = " + (CheckInfo.UnQualifiedQty > 0 ? 1 : 2) + ", " +
                   "IsComplete = " + (CheckInfo.QualifiedQty > 0 ? 0 : 1) + " " +
                   "where id = " + (theReport.ID) + "";


            SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
            sql = sql.Replace("'", "");
            AddOpLog(theReport.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 201, OpDate, sql);

            return "处理成功";
        }


        public static string DMRCommit(OpReport DMRInfo) //apinum 601
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

            if (DMRInfo.DMRUnQualifiedQty < 0)
                return "错误：废弃数量不能为负";

            if (DMRInfo.DMRQualifiedQty + DMRInfo.DMRRepairQty + DMRInfo.DMRUnQualifiedQty != theReport.UnQualifiedQty)
                return "错误：让步数 + 返修数 + 废弃数 不等于原待检数";

            if (DMRInfo.DMRRepairQty > 0 && DMRInfo.DMRJobNum == "")
                return "错误：返修工单号不能为空";

            if (DMRInfo.DMRRepairQty > 0 && CommonRepository.CheckJobHeadState(DMRInfo.DMRJobNum) != "工单不存在")
                return "错误：返修工单号已存在";

            if ((DMRInfo.DMRUnQualifiedQty > 0 && DMRInfo.DMRUnQualifiedReason == ""))
                return "错误：报废原因不能为空";

            if ((DMRInfo.DMRUnQualifiedQty > 0 && DMRInfo.DMRWarehouseCode == ""))
                return "错误：仓库不能为空";

            if (DMRInfo.DMRUnQualifiedQty > 0 && (DMRInfo.DMRBinNum == ""))
                return "错误：库位不能为空";

            if (DMRInfo.DMRUnQualifiedQty > 0 && CheckBinNum(theReport.Company, DMRInfo.DMRBinNum, DMRInfo.DMRWarehouseCode) != "ok")
                return "错误：库位与仓库不匹配";


            if (theReport.ErpCounter < 1) //没交互过，或第一次交互失败,允许更改数量
            {
                sql = " update bpm set " +
                       "DMRQualifiedQty = " + DMRInfo.DMRQualifiedQty + ", " +
                       "DMRRepairQty = " + DMRInfo.DMRRepairQty + "," +
                       "DMRUnQualifiedQty = " + DMRInfo.DMRUnQualifiedQty + "," +
                       "DMRUnQualifiedReason = '" + DMRInfo.DMRUnQualifiedReason + "', " +
                       "DMRJobNum = '" + DMRInfo.DMRJobNum + "'," +
                       "DMRWarehouseCode = '" + DMRInfo.DMRWarehouseCode + "'," +
                       "DMRBinNum = '" + DMRInfo.DMRBinNum + "', " +
                       "Responsibility = '" + DMRInfo.Responsibility + "' " +
                       "where id = " + DMRInfo.ID + "";
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
            }
            else //交互成功过，只能使用原始数量值
            {
                DMRInfo.DMRQualifiedQty = theReport.DMRQualifiedQty;
                DMRInfo.DMRRepairQty = theReport.DMRRepairQty;
                DMRInfo.DMRUnQualifiedQty = theReport.DMRUnQualifiedQty;
                DMRInfo.DMRUnQualifiedReason = theReport.DMRUnQualifiedReason;
                DMRInfo.DMRJobNum = theReport.DMRJobNum;
                DMRInfo.DMRWarehouseCode = theReport.DMRWarehouseCode;
                DMRInfo.DMRBinNum = theReport.DMRBinNum;
            }


            int DMRID; string res;
            if (theReport.ErpCounter < 1)//让步
            {
                res = ErpAPI.Common.StartInspProcessing((int)theReport.TranID, (decimal)DMRInfo.DMRQualifiedQty, (decimal)(DMRInfo.DMRRepairQty + DMRInfo.DMRUnQualifiedQty), DMRInfo.DMRUnQualifiedReason, DMRInfo.DMRWarehouseCode, DMRInfo.DMRBinNum, "报工", theReport.Plant, out DMRID);
                if (res.Substring(0, 1).Trim() != "1")
                    return "错误：" + res;

                string dmrid = DMRInfo.DMRQualifiedQty == theReport.UnQualifiedQty ? "null" : DMRID.ToString();

                if (DMRInfo.DMRQualifiedQty > 0)
                {
                    InsertConcessionRecord((int)DMRInfo.ID, (decimal)DMRInfo.DMRQualifiedQty, DMRInfo.TransformUserGroup, dmrid);
                    AddOpLog(DMRInfo.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 601, OpDate, "让步接收子流程生成");
                }

                sql = " update bpm set ErpCounter = 1, DMRID = " + dmrid + " where id = " + DMRInfo.ID + "";
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
            }

            if (theReport.ErpCounter < 2)//返修
            {
                sql = @"select dmrid from bpm where id = " + DMRInfo.ID + "";
                object dmrid = SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);


                sql = @"select IUM  from erp.JobAsmbl where JobNum = '"+theReport.JobNum+"' and AssemblySeq = "+theReport.AssemblySeq+"";
                object IUM = SQLRepository.ExecuteScalarToObject(SQLRepository.ERP_strConn, CommandType.Text, sql, null);


                if (!Convert.IsDBNull(dmrid) && DMRInfo.DMRRepairQty > 0)
                {
                    res = ErpAPI.Common.RepairDMRProcessing((int)dmrid, theReport.Company, theReport.Plant, theReport.PartNum, (decimal)DMRInfo.DMRRepairQty, DMRInfo.DMRJobNum,IUM.ToString());
                    if (res.Substring(0, 1).Trim() != "1")
                        return "错误：" + res;

                    InsertRepairRecord((int)DMRInfo.ID, (decimal)DMRInfo.DMRRepairQty, DMRInfo.DMRJobNum, (int)dmrid, DMRInfo.TransformUserGroup);
                    AddOpLog(DMRInfo.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 601, OpDate, "返修子流程生成");
                }
                sql = " update bpm set ErpCounter = 2 where id = " + (DMRInfo.ID) + "";
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
            }

            if (theReport.ErpCounter < 3)//报废
            {
                sql = @"select dmrid from bpm where id = " + DMRInfo.ID + "";
                object dmrid = SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = @"select IUM  from erp.JobAsmbl where JobNum = '" + theReport.JobNum + "' and AssemblySeq = " + theReport.AssemblySeq + "";
                object IUM = SQLRepository.ExecuteScalarToObject(SQLRepository.ERP_strConn, CommandType.Text, sql, null);

                if (!Convert.IsDBNull(dmrid) && DMRInfo.DMRUnQualifiedQty > 0)
                {
                    res = ErpAPI.Common.RefuseDMRProcessing(theReport.Company, theReport.Plant, (decimal)DMRInfo.DMRUnQualifiedQty, DMRInfo.DMRUnQualifiedReason, (int)dmrid, IUM.ToString());
                    if (res.Substring(0, 1).Trim() != "1")
                        return "错误：" + res;

                    InsertDiscardRecord((int)DMRInfo.ID, (decimal)DMRInfo.DMRUnQualifiedQty, DMRInfo.DMRUnQualifiedReason, (int)dmrid, DMRInfo.DMRWarehouseCode, DMRInfo.DMRBinNum, DMRInfo.TransformUserGroup);
                    AddOpLog(DMRInfo.ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 601, OpDate, "报废子流程生成");
                }
                sql = " update bpm set ErpCounter = 3, checkcounter = 2 where id = " + (DMRInfo.ID) + ""; //令checkcounter = 2， 完成不良品处理
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
            }

            return "处理成功";
        }


        private static string TransferCommitOfSub(OpReport TransmitInfo)//apinum 302
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string sql = @"select * from bpmsub where Id = " + TransmitInfo.ID + "";
            OpReport theSubReport = CommonRepository.DataTableToList<OpReport>(SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

            if (theSubReport.IsComplete == true)
                return "错误：该批次的流程已结束";

            if (theSubReport.IsDelete == true)
                return "错误：该批次的流程已删除";

            if (theSubReport.Status != 3)
                return "错误：流程未在当前节点上";

            //以下只会执行一个if
            if (theSubReport.DMRQualifiedQty != null)
            {
                string res = CommonRepository.CheckJobHeadState(theSubReport.JobNum);
                if (res != "正常")
                    return "0|错误：" + res;

                sql = "select isdelete from bpm where ID = " + theSubReport.RelatedID + "";
                bool mainIsDeleted = (bool)(SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null));
                if (mainIsDeleted)
                {
                    SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, "UPDATE bpmsub SET isdelete = 1  where ID = " + theSubReport.ID + "", null);
                    return "0|错误：所属的主流程已被删除，该子流程已被自动删除";
                }

                DataTable dt = GetJobSeqInfo(theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq); //erp抓取该工序的最新信息
                if (dt == null)
                {
                    SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, "UPDATE bpmsub SET isdelete = 1  where ID = " + theSubReport.ID + "", null);
                    return "0|错误：当前工序不存在，该报工流程已被自动删除";
                }

                if (dt.Rows[0]["OpCode"].ToString() != theSubReport.OpCode)
                {
                    SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, "UPDATE bpmsub SET isdelete = 1  where ID = " + theSubReport.ID + "", null);
                    return "0|错误：原工序编号" + theSubReport.OpCode + "， 现工序编号：" + dt.Rows[0]["OpCode"].ToString() + "， 该报工流程已被自动删除";
                }

                //if (CommonRepository.IsOpSeqComplete(theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq))
                //    return "错误：该工序已完成";


                string NextSetpInfo = GetNextSetpInfo(theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq, dt.Rows[0]["Company"].ToString());
                if (NextSetpInfo.Substring(0, 1).Trim() == "0")
                    return "0|错误：获取下工序去向失败，" + NextSetpInfo;


                int a, b;//凑个数，无意义
                string c, OpCode;//凑个数，无意义                
                res = ErpAPI.Common.getJobNextOprTypes(theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq, out a, out b, out OpCode, out c, theSubReport.Company);
                if (res.Substring(0, 1).Trim() == "0")
                    return res;


                int nextRole = -1;
                if (res.Substring(0, 1).Trim().ToLower() == "p") //由仓库接收人员处理 设置8
                    nextRole = 8;
                else if (res.Substring(0, 1).Trim().ToLower() == "s")//外协
                    nextRole = 16;
                else if (res.Substring(0, 1).Trim().ToLower() == "m")//场内
                    nextRole = 128;


                string[] arr = NextSetpInfo.Split('~');
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
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = sql.Replace("'", "");
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
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = sql.Replace("'", "");
                AddOpLog(theSubReport.ID, theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq, 301, OpDate, "报废提交|" + sql);
            }

            if ((theSubReport.DMRRepairQty) != null)
            {
                sql = @"select count(*) from erp.JobOper  where JobNum = '" + theSubReport.DMRJobNum + "'";
                bool IsExistOprSeq = Convert.ToBoolean(SQLRepository.ExecuteScalarToObject(SQLRepository.ERP_strConn, CommandType.Text, sql, null));

                if (!IsExistOprSeq) return "错误：返修工单工序为空";

                //sql = @"select top 1  OpCode, OpDesc, OprSeq  from erp.JobOper  where JobNum = '" + theSubReport.DMRJobNum + "' order by OprSeq asc";
                //DataTable dt3 = SQLRepository.ExecuteQueryToDataTable(SQLRepository.ERP_strConn, sql);


                sql = " update bpmsub set " +
                        "TransformUser = '" + HttpContext.Current.Session["UserId"].ToString() + "', " +
                        "TransformDate = '" + OpDate + "'," +
                        "Status = " + 4 + "," +
                        "NextUserGroup = '" + TransmitInfo.NextUserGroup + "'," +
                        "PreStatus = " + 3 + "," +
                        "AtRole = " + 128 + " " +
                        //"NextJobSeq = " + (int)dt3.Rows[0]["OprSeq"] + ", " +
                        //"NextOpCode = " + dt3.Rows[0]["OpCode"].ToString() + ", " +
                        //"NextOpDesc = " + dt3.Rows[0]["OpDesc"].ToString() + " " +
                        "where id = " + (theSubReport.ID) + "";
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = sql.Replace("'", "");
                AddOpLog(theSubReport.ID, theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq, 302, OpDate, "返修提交|" + sql);
            }


            return "处理成功";
        }


        private static string TransferCommitOfMain(OpReport TransmitInfo)//apinum 301
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

            string res = CommonRepository.CheckJobHeadState(theReport.JobNum);
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

            //if (CommonRepository.IsOpSeqComplete(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq))
            //    return "错误：该工序已完成";


            string NextSetpInfo = GetNextSetpInfo(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, dt.Rows[0]["Company"].ToString());
            if (NextSetpInfo.Substring(0, 1).Trim() == "0")
                return "0|错误：获取下工序去向失败，" + NextSetpInfo;

            //再回写主表
            string[] arr = NextSetpInfo.Split('~');
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
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string sql = @"select * from bpmsub where Id = " + AcceptInfo.ID + "";
            OpReport theSubReport = CommonRepository.DataTableToList<OpReport>(SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

            if (theSubReport.IsComplete == true)
                return "错误：该批次的流程已结束";

            if (theSubReport.IsDelete == true)
                return "错误：该批次的流程已删除";

            if (theSubReport.Status != 4)
                return "错误：流程未在当前节点上";


            if ((theSubReport.DMRQualifiedQty) != null)
            {
                string res = CommonRepository.CheckJobHeadState(theSubReport.JobNum);
                if (res != "正常")
                    return "错误：" + res;

                sql = "select isdelete from bpm where ID = " + theSubReport.RelatedID + "";
                bool mainIsDeleted = (bool)(SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null));
                if (mainIsDeleted)
                {
                    SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, "UPDATE bpmsub SET isdelete = 1  where ID = " + theSubReport.ID + "", null);
                    return "0|错误：所属的主流程已被删除，该子流程已被自动删除";
                }

                DataTable dt = GetJobSeqInfo(theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq); //erp抓取该工序的最新信息
                if (dt == null)
                {
                    SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, "UPDATE bpmsub SET isdelete = 1  where ID = " + theSubReport.ID + "", null);
                    return "0|错误：当前工序不存在，该报工流程已被自动删除";
                }

                if (dt.Rows[0]["OpCode"].ToString() != theSubReport.OpCode)
                {
                    SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, "UPDATE bpmsub SET isdelete = 1  where ID = " + theSubReport.ID + "", null);
                    return "0|错误：原工序编号" + theSubReport.OpCode + "， 现工序编号：" + dt.Rows[0]["OpCode"].ToString() + "， 该报工流程已被自动删除";
                }

                //if (CommonRepository.IsOpSeqComplete(theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq))
                //    return "错误：该工序已完成";

                string NextSetpInfo = GetNextSetpInfo(theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq, dt.Rows[0]["Company"].ToString());
                if (NextSetpInfo.Substring(0, 1).Trim() == "0")
                    return "0|错误：获取下工序去向失败，" + NextSetpInfo;


                object PreOpSeq = CommonRepository.GetPreOpSeq(theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq);
                if (PreOpSeq == null && CommonRepository.GetReqQtyOfAssemblySeq(theSubReport.JobNum, (int)theSubReport.AssemblySeq) < GetTotalQtyOfJobSeq(theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq, (int)theSubReport.RelatedID))
                    return "错误： 报工数超出该阶层的可生产数量";
                if (PreOpSeq != null && CommonRepository.GetOpSeqCompleteQty(theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)PreOpSeq) < GetTotalQtyOfJobSeq(theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq, (int)theSubReport.RelatedID))
                    return "错误： 当前报工数超出上一道工序的报工数";


                //自动回退检测
                string[] arr = NextSetpInfo.Split('~');
                if (theSubReport.NextOpCode != arr[1])
                {
                    ReturnStatus((bool)theSubReport.IsSubProcess, (int)theSubReport.ID, (int)theSubReport.Status, 11, "工序去向已更改，子流程自动回退", 402);
                    return "错误： 工序去向已更改，流程已自动回退至上一节点";
                }


                //若去向仓库
                if (theSubReport.AtRole == 8)
                {
                    res = ErpAPI.Common.D0506_01(null, theSubReport.JobNum, (int)theSubReport.AssemblySeq, (decimal)theSubReport.DMRQualifiedQty, theSubReport.JobNum, theSubReport.NextOpCode, AcceptInfo.BinNum, theSubReport.Company, theSubReport.Plant);
                    if (res != "1|处理成功")
                        return "错误：" + res;
                }

                //再回写主表
                sql = " update bpmsub set " +
                       "NextUser = '" + HttpContext.Current.Session["UserId"].ToString() + "|" + HttpContext.Current.Session["UserName"].ToString() + "', " +
                       "NextDate = '" + OpDate + "'," +
                       "Status = 99," +
                       "PreStatus = " + (theSubReport.Status) + "," +
                       "IsComplete = 1," +
                       "BinNum = '" + CommonRepository.GetValueAsString(AcceptInfo.BinNum) + "' " +
                       "where id = " + (theSubReport.ID) + "";
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);


                sql = sql.Replace("'", "");
                AddOpLog(theSubReport.ID, theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq, 402, OpDate, "让步提交|" + sql);
            }

            if ((theSubReport.DMRRepairQty) != null)
            {
                string res = CommonRepository.CheckJobHeadState(theSubReport.JobNum);
                if (res != "正常")
                    return "错误：" + res;

                sql = " update bpmsub set " +
                       "NextUser = '" + HttpContext.Current.Session["UserId"].ToString() + "|" + HttpContext.Current.Session["UserName"].ToString() + "', " +
                       "NextDate = '" + OpDate + "'," +
                       "Status = 99," +
                       "PreStatus = " + (theSubReport.Status) + "," +
                       "IsComplete = 1," +
                       "BinNum = '" + CommonRepository.GetValueAsString(AcceptInfo.BinNum) + "' " +
                       "where id = " + (theSubReport.ID) + "";
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);


                sql = sql.Replace("'", "");
                AddOpLog(theSubReport.ID, theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq, 402, OpDate, "返修提交|" + sql);
            }

            return "处理成功";
        }


        private static string AccepterCommitOfMain(OpReport AcceptInfo)//apinum 401
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

            string res = CommonRepository.CheckJobHeadState(theReport.JobNum);
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

            //if (CommonRepository.IsOpSeqComplete(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq))
            //    return "错误：该工序已完成";

            string NextSetpInfo = GetNextSetpInfo(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, dt.Rows[0]["Company"].ToString());
            if (NextSetpInfo.Substring(0, 1).Trim() == "0")
                return "0|错误：获取下工序去向失败，" + NextSetpInfo;



            object PreOpSeq = CommonRepository.GetPreOpSeq(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq);
            if (PreOpSeq == null && CommonRepository.GetReqQtyOfAssemblySeq(theReport.JobNum, (int)theReport.AssemblySeq) < GetTotalQtyOfJobSeq(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, (int)theReport.ID))
                return "错误： 报工数超出该阶层的可生产数量";
            if (PreOpSeq != null && CommonRepository.GetOpSeqCompleteQty(theReport.JobNum, (int)theReport.AssemblySeq, (int)PreOpSeq) < GetTotalQtyOfJobSeq(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, (int)theReport.ID))
                return "错误： 当前报工数超出上一道工序的报工数";


            //自动回退检测
            string[] arr = NextSetpInfo.Split('~');
            if (theReport.NextOpCode != arr[1])
            {
                ReturnStatus((bool)theReport.IsSubProcess, (int)theReport.ID, (int)theReport.Status, 11, "工序去向已更改，主流程自动回退", 401);
                return "错误： 工序去向已更改，流程已自动回退至上一节点";
            }


            //若去向仓库
            if (theReport.AtRole == 8)
            {
                res = ErpAPI.Common.D0506_01(null, theReport.JobNum, (int)theReport.AssemblySeq, (decimal)theReport.QualifiedQty, theReport.JobNum, theReport.NextOpCode, AcceptInfo.BinNum, theReport.Company, theReport.Plant);
                if (res != "1|处理成功")
                    return "错误：" + res;
            }

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
                string sql = @"select * from BPM where CHARINDEX(company, '{0}') > 0   and   CHARINDEX(Plant, '{1}') > 0 and checkcounter = 1 and isdelete != 1 order by CreateDate desc";
                sql = string.Format(sql, HttpContext.Current.Session["Company"].ToString(), HttpContext.Current.Session["Plant"].ToString());

                DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql);
                if (dt == null)
                    return null;

                //UnQualifiedGroup默认包含整个不良品小组人，所以注释掉以下代码
                //for (int i = dt.Rows.Count - 1; i >= 0; i--)
                //{
                //    if (!(dt.Rows[i]["UnQualifiedGroup"].ToString()).Contains(HttpContext.Current.Session["UserId"].ToString()))
                //        dt.Rows[i].Delete();
                //}

                List<OpReport> Remains = CommonRepository.DataTableToList<OpReport>(dt);

                if (Remains != null)
                {
                    for (int i = 0; i < Remains.Count; i++)
                    {
                        string userid = "";
                        if (Remains[i].Status == 2)
                            userid = Remains[i].CreateUser;
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

            DataTable dt1 = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql);

            sql = @"select * from BPMsub where AtRole & {0} != 0 and UnQualifiedType = 1 and isdelete != 1 and isComplete != 1 and CHARINDEX(company, '{1}') > 0   and   CHARINDEX(Plant, '{2}') > 0 order by CreateDate desc";
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


        public static string GetProcessOfUser() //retun 工单号~阶层号~工序序号~工序代码~工序描述~NextJobSeq NextOpCode NextOpDesc startdate Qty  CompleteQty
        {
            string sql = @"select * from process where userid = '" + HttpContext.Current.Session["UserId"].ToString() + "'";
            DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql);


            if (dt != null && (!Convert.IsDBNull(dt.Rows[0]["EndDate"]) || !Convert.IsDBNull(dt.Rows[0]["StartDate"])))//未结束
            {
                string NextSetpInfo = GetNextSetpInfo(dt.Rows[0]["JobNum"].ToString(), (int)dt.Rows[0]["AssemblySeq"], (int)dt.Rows[0]["JobSeq"], "001");
                if (NextSetpInfo.Substring(0, 1).Trim() == "0")
                    return "0|错误：无法获取工序最终去向，" + NextSetpInfo;

                string partnum = "|" + (string)SQLRepository.ExecuteScalarToObject(SQLRepository.ERP_strConn, CommandType.Text, @"select PartNum from erp.JobMtl where JobNum = '" + dt.Rows[0]["JobNum"].ToString() + "' and AssemblySeq = " + dt.Rows[0]["AssemblySeq"].ToString() + "", null);


                return "1|" + (string)dt.Rows[0]["JobNum"] + "~" + dt.Rows[0]["AssemblySeq"].ToString() + partnum + "~" + dt.Rows[0]["JobSeq"].ToString() + "~" +
                    (string)dt.Rows[0]["OpCode"] + "~" + (string)dt.Rows[0]["OpDesc"] + "~" + NextSetpInfo + "~" + ((DateTime)dt.Rows[0]["StartDate"]).ToString("yyyy-MM-dd HH:mm:ss.fff") + "~" +
                    (dt.Rows[0]["Qty"].ToString() == "" ? "0" : dt.Rows[0]["Qty"].ToString()) + "~" + GetTotalQtyOfJobSeq(dt.Rows[0]["JobNum"].ToString(), (int)dt.Rows[0]["AssemblySeq"], (int)dt.Rows[0]["JobSeq"], 0).ToString(); ;
            }
            return null;
        }


        public static DataTable GetNextUserGroup(string OpCode, int id, string jobnum)
        {
            DataTable dt = null;
            string sql = null;
            long nextRole = GetNextRole(id);

            if (nextRole == 8)//从拥有权值8的人员表中，选出可以操作指定仓库的人
            {
                sql = "select * from bpm where id = " + id + "";
                dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql);


                string NextSetpInfo = GetNextSetpInfo(dt.Rows[0]["JobNum"].ToString(), (int)dt.Rows[0]["AssemblySeq"], (int)dt.Rows[0]["JobSeq"], dt.Rows[0]["Company"].ToString());
                if (NextSetpInfo.Substring(0, 1).Trim() == "0" || NextSetpInfo.Split('~')[0] != "仓库")//获取仓库失败
                    return null;

                object Warehouse = NextSetpInfo.Split('~')[1].Trim();

                //sql = @"select WarehouseCode from erp.partplant pp  LEFT JOIN erp.Warehse wh on pp.PrimWhse = wh.WarehouseCode and pp.Company = wh.Company and pp.Plant = wh.Plant   
                //      where pp.company = '" + dt.Rows[0]["Company"].ToString() + "'   and pp.plant = '" + dt.Rows[0]["Plant"].ToString() + "'   and   pp.PartNum = '" + dt.Rows[0]["PartNum"].ToString() + "'";
                //object Warehouse =  SQLRepository.ExecuteScalarToObject(SQLRepository.ERP_strConn, CommandType.Text, sql, null);

                sql = "select UserID,UserName, WhseGroup from userfile where CHARINDEX('" + dt.Rows[0]["Company"].ToString() + "', company) > 0 and CHARINDEX('" + dt.Rows[0]["Plant"].ToString() + "', plant) > 0 and disabled = 0 and RoleID & " + nextRole + " != 0 and RoleID != 2147483647";
                dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表

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
                dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, "select * from bpm where id = " + id + "");

                sql = "select UserID, UserName from userfile where disabled = 0  and  CHARINDEX('" + dt.Rows[0]["Company"].ToString() + "', company) > 0 and CHARINDEX('" + dt.Rows[0]["Plant"].ToString() + "', plant) > 0 and RoleID & " + nextRole + " != 0 and RoleID != 2147483647";
                dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表
            }
            else if (nextRole == 256)
            {
                sql = @"select Plant from erp.JobHead where jobnum = '" + jobnum + "'";
                string Plant = (string)SQLRepository.ExecuteScalarToObject(SQLRepository.ERP_strConn, CommandType.Text, sql, null);

                sql = "select CheckUser from BPMOpCode where OpCode = '" + OpCode + "'";
                string CheckUser = (string)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = "select UserID, UserName from userfile where disabled = 0 and CHARINDEX(userid, '" + CheckUser + "') > 0 and CHARINDEX('" + Plant + "', plant) > 0 and RoleID & " + nextRole + " != 0 and RoleID != 2147483647";
                dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表
            }
            else if (nextRole == 512)
            {
                dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, "select * from bpm where id = " + id + "");

                sql = "select TransformUser from BPMOpCode where OpCode = '" + OpCode + "'";
                string TransformUser = (string)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = "select UserID, UserName from userfile where disabled = 0 and CHARINDEX(userid, '" + TransformUser + "') > 0 and CHARINDEX('" + dt.Rows[0]["Plant"].ToString() + "', plant) > 0 and RoleID & " + nextRole + " != 0 and RoleID != 2147483647";
                dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表
            }
            else if (nextRole == 128)//
            {
                dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, "select * from bpm where id = " + id + "");

                sql = "select NextOpCode from bpm where id = " + id + "";
                string NextOpCode = (string)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = "select NextUser from BPMOpCode where OpCode = '" + NextOpCode + "'";
                string NextUser = (string)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = "select UserID, UserName from userfile where disabled = 0  and CHARINDEX('" + dt.Rows[0]["Plant"].ToString() + "', plant) > 0 and CHARINDEX(userid, '" + NextUser + "') > 0 and RoleID & " + nextRole + " != 0 and RoleID != 2147483647";
                dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表
            }


            return dt == null || dt.Rows.Count == 0 ? null : dt;
        }


        public static DataTable GetNextUserGroupOfSub(int id)//三选四
        {
            string sql = "select * from bpmsub where id = " + id + "";
            OpReport theSubReport = CommonRepository.DataTableToList<OpReport>(SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

            long nextRole = -1;
            string nextOpCode = "";
            if (theSubReport.DMRQualifiedQty != null)
            {
                int a, b;//凑个数，无意义
                string res, c;

                res = ErpAPI.Common.getJobNextOprTypes(theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq, out a, out b, out nextOpCode, out c, theSubReport.Company);

                if (res.Substring(0, 1).Trim().ToLower() == "p") //由仓库接收人员处理 设置8
                    nextRole = 8;
                else if (res.Substring(0, 1).Trim().ToLower() == "s")//外协
                    nextRole = 16;
                else if (res.Substring(0, 1).Trim().ToLower() == "m")//场内
                    nextRole = 128;
            }

            if (theSubReport.DMRUnQualifiedQty != null)
                return null;

            if (theSubReport.DMRRepairQty != null)
            {
                sql = @"select  SubContract  from erp.JobOper where jobnum = '" + theSubReport.DMRJobNum + "' order by OprSeq asc ";
                bool IsSubContract = Convert.ToBoolean(SQLRepository.ExecuteScalarToObject(SQLRepository.ERP_strConn, CommandType.Text, sql, null));

                if (IsSubContract)
                    nextRole = 16;
                else
                {
                    nextRole = 128;
                    sql = @"select top 1  OpCode  from erp.JobOper  where JobNum = '" + theSubReport.DMRJobNum + "' order by OprSeq asc";
                    nextOpCode = (string)SQLRepository.ExecuteScalarToObject(SQLRepository.ERP_strConn, CommandType.Text, sql, null);
                }
            }


            DataTable dt = null;
            if (nextRole == 8)//从拥有权值8的人员表中，选出可以操作指定仓库的人
            {
                sql = @"select WarehouseCode from erp.partplant pp  LEFT JOIN erp.Warehse wh on pp.PrimWhse = wh.WarehouseCode and pp.Company = wh.Company and pp.Plant = wh.Plant   
                      where pp.company = '" + theSubReport.Company + "'   and pp.plant = '" + theSubReport.Plant + "'   and   pp.PartNum = '" + theSubReport.PartNum + "'";
                object Warehouse = SQLRepository.ExecuteScalarToObject(SQLRepository.ERP_strConn, CommandType.Text, sql, null);

                if (Warehouse == null) return null;

                sql = "select UserID,UserName, WhseGroup from userfile where CHARINDEX('" + theSubReport.Company + "', company) > 0 and CHARINDEX('" + theSubReport.Plant + "', plant) > 0 and disabled = 0 and RoleID & " + nextRole + " != 0 and RoleID != 2147483647";
                dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表

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
                sql = "select UserID, UserName from userfile where disabled = 0 and  CHARINDEX('" + theSubReport.Company + "', company) > 0 and CHARINDEX('" + theSubReport.Plant + "', plant) > 0 and RoleID & " + nextRole + " != 0 and RoleID != 2147483647";
                dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表
            }

            else if (nextRole == 128)
            {
                sql = "select NextUser from BPMOpCode where OpCode = '" + nextOpCode + "'";
                string NextUser = (string)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = "select UserID, UserName from userfile where disabled = 0  and CHARINDEX('" + theSubReport.Plant + "', plant) > 0 and CHARINDEX(userid, '" + NextUser + "') > 0 and RoleID & " + nextRole + " != 0 and RoleID != 2147483647";
                dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表
            }

            return dt == null || dt.Rows.Count == 0 ? null : dt;
        }


        public static DataTable GetDMRNextUserGroup(string OpCode, int id)
        {
            string sql = "select TransformUser from BPMOpCode where OpCode = '" + OpCode + "'";
            string TransformUsers = (string)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

            DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, "select * from bpm where id = " + id + "");

            sql = "select UserID, UserName from userfile where disabled = 0 and CHARINDEX('" + dt.Rows[0]["Plant"].ToString() + "', plant) > 0 and CHARINDEX(userid, '" + TransformUsers + "') > 0 and RoleID & " + 512 + " != 0 and RoleID != 2147483647";
            dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表

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
            string sql = @"select OprSeq,OpDesc,OpCode, jo.QtyCompleted from erp.JobAsmbl ja left join erp.JobOper jo on ja.JobNum = jo.JobNum and ja.AssemblySeq = jo.AssemblySeq where  ja.jobnum = '" + JobNum + "' and ja.AssemblySeq= '" + AssemblySeq + "' order by OprSeq asc";
            DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.ERP_strConn, sql);

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

            DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql);

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
            DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql);
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
            DataTable Reasons = SQLRepository.ExecuteQueryToDataTable(SQLRepository.ERP_strConn, sql);

            return Reasons;
        }


        public static string ReturnStatus(bool IsSubProcess, int ID, int oristatus, int ReasonID, string remark, int apinum)
        {
            string OpDetail = "", OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string table = IsSubProcess ? "bpmsub" : "bpm", processtype = IsSubProcess ? "主流程" : "子流程";
            string sql = "select * from " + table + " where ID = " + ID + " ";
            OpReport theReport = CommonRepository.DataTableToList<OpReport>(SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql)).First(); //获取该批次记录


            if ((bool)theReport.IsDelete == true)
                return "错误：该批次的流程已删除";

            if ((bool)(theReport.IsComplete) == true)
                return "错误：该批次的流程已结束";

            if ((int)(theReport.Status) != oristatus)
                return "错误：流程未在当前节点上";

            if (oristatus == 4)
            {
                sql = @"update " + table + " set NextUserGroup=null, PreStatus = " + theReport.Status + ", AtRole = " + 512 + ", status = " + theReport.PreStatus + ", ReturnThree = ReturnThree+1  where id = " + ID + " ";
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                //获取更新后的回退编号
                sql = "select ReturnThree from  " + table + "  where id = " + ID + "  ";
                int c = (int)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                //把该回退编号的原因插入到ReasonRecord表中
                sql = @"insert into BPMReturn(BPMID, ReturnThree, ReturnReasonId, ReasonRemark, Date,IsSubProcess) Values(" + ID + ", " + c + ", " + ReasonID + ",'" + remark + "', '" + OpDate + "', " + Convert.ToInt32(IsSubProcess) + ")";
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
            }


            AddOpLog(ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, apinum, OpDate, processtype + "从" + oristatus.ToString() + "回退成功");
            return "处理成功";
        }


        public static IEnumerable<OpReport> GetRecordsForPrint(string JobNum, int? AssemblySeq, int? JobSeq)
        {
            string sql = @"select * from BPM where isdelete != 1 and printid is not null and JobNum = '" + JobNum + "' and iscomplete = 0 ";

            if (AssemblySeq != null)
                sql += "and AssemblySeq = " + AssemblySeq + " ";
            if (JobSeq != null)
                sql += "and JobSeq = " + JobSeq + " ";

            sql += " order by CreateDate desc";

            DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql);

            List<OpReport> Remains = CommonRepository.DataTableToList<OpReport>(dt);

            return Remains;

        } //取消已开始的工序


        public static void ClearProcess()
        {
            string sql = "update process set StartDate= null, EndDate=null ,Qty=null,JobNum=null,AssemblySeq=null  ,JobSeq=null ,OpCode=null ,OpDesc=null where userid = '" + HttpContext.Current.Session["UserId"].ToString() + "'";
            SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);//情况
        } //取消已开始的工序



        public static void DeleteProcess(int ID)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string sql = "select * from bpm where ID = " + ID + " ";
            OpReport theReport = CommonRepository.DataTableToList<OpReport>(SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

            sql = "update bpm set isdelete = 1 where id = " + ID + "";
            SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

            AddOpLog(ID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 16, OpDate, "二节点删除流程");

        }


        public static string PrintQR(int id, int printqty)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string sql = @"select * from bpm where Id = " + id + "";
            OpReport theSubReport = CommonRepository.DataTableToList<OpReport>(SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql)).First(); //获取该批次记录


            string jsonStr = " text1: '{0}', text2: '{12}', text3: '{1}', text4: '{2}', text5: '{3}', text6: '', text7: '{4}', text8: '{5}', text9: '{6}', text10: '{7}', text11: '{8}', text12: '{9}', text13: '', text14: '{10}', text15: '{11}', text16: '', text17: '', text18: '', text19: '', text20: '', text21: '', text22: '', text23: '', text24: '', text25: '', text26: '', text27: '', text28: '', text29: '', text30: '' ";
            jsonStr = string.Format(jsonStr, theSubReport.PartNum, theSubReport.JobNum, theSubReport.JobNum, theSubReport.AssemblySeq.ToString(), theSubReport.PrintID.ToString(), "", "", theSubReport.FirstQty.ToString(), "", theSubReport.Company, theSubReport.JobSeq.ToString(), "", theSubReport.PartDesc);
            jsonStr = "[{" + jsonStr + "}]";


            sql = "select Printer from Userprinter where userid = '" + HttpContext.Current.Session["UserId"].ToString() + "' ";
            string printer = (string)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);
            string res = "";
            ServiceReference_Print.WebServiceSoapClient client = new ServiceReference_Print.WebServiceSoapClient();
            if ((res = client.Print(@"C:\D0201.btw", printer, printqty, jsonStr)) == "1|处理成功")
            {
                client.Close();
                AddOpLog(id, theSubReport.JobNum, (int)theSubReport.AssemblySeq, (int)theSubReport.JobSeq, 14, OpDate, "复制二维码");
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

            DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.ERP_strConn, sql);

            return dt;
        }




        private static void AddOpLog(int? id, string JobNum, int AssemblySeq, int JobSeq, int ApiNum, string OpDate, string OpDetail)
        {
            string sql = @"insert into BPMLog(JobNum, AssemblySeq, UserId, Opdate, ApiNum, JobSeq, OpDetail,bpmid) Values('{0}', {1}, '{2}', '{3}', {4}, {5}, '{6}',{7}) ";
            sql = string.Format(sql, JobNum, AssemblySeq, HttpContext.Current.Session["UserId"].ToString(), OpDate, ApiNum, JobSeq, OpDetail, id == null ? "null" : id.ToString());

            SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
        }//添加操作记录
    }
}