using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Web;

namespace Appapi.Models
{
    public class MtlReportRepository
    {
        private static readonly object lock_dmr = new object();
        private static List<int> dmr_IDs = new List<int>();


        private static string CheckBinNum(string company, string binnum, string WarehouseCode)
        {
            string sql = "select count(*) from erp.WhseBin where Company = '{0}' and  WarehouseCode = '{1}' and BinNum = '{2}'";
            sql = string.Format(sql, company, WarehouseCode, binnum);
            int exist = (int)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);

            return exist == 0 ? "错误：库位与仓库不匹配" : "ok";
        }



        private static void InsertConcessionRecord(int Id, decimal DMRQualifiedQty, string TransformUserGroup, int DMRID, string DMRWarehouseCode, string DMRBinNum, string DMRUnQualifiedReason, string Responsibility, string DMRUnQualifiedReasonRemark, string DMRUnQualifiedReasonDesc, string ResponsibilityRemark)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string sql = @"select * from MtlReport where Id = " + Id + "";
            OpReport theReport = CommonRepository.DataTableToList<OpReport>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

            sql = @"insert into BPMSub(
                     [CreateUser]
                    ,[CreateDate]
                    ,[CheckUser]
                    ,[TransformUserGroup]
                    ,[CheckDate]
                    ,[PartNum]
                    ,[PartDesc]
                    ,[JobNum]
                    ,[AssemblySeq]
                    ,[MtlSeq]
                    ,[UnQualifiedReason]
                    ,[Remark]
                    ,[AtRole]
                    ,[Plant]
                    ,[Company]
                    ,[TranID]
                    ,[DMRID]
                    ,[RelatedID]
                    ,[DMRQualifiedQty]
                    ,[UnQualifiedType]
                    ,[Status]
                    ,[IsComplete]
                    ,[IsDelete]
                    ,[IsSubProcess]
                    ,[LotNum]
                    ,[Responsibility]
                    ,[DMRUnQualifiedReason]
                    ,[DMRWarehouseCode]
                    ,[DMRBinNum]
                    ,DMRUnQualifiedReasonRemark
                    ,DMRUnQualifiedReasonDesc
                    ,ResponsibilityRemark
                    ,UnQualifiedReasonRemark
                    ,UnQualifiedReasonDesc) values({0})";
            string valueStr = CommonRepository.ConstructInsertValues(new ArrayList
            {
                theReport.CreateUser,
                theReport.CreateDate,
                HttpContext.Current.Session["UserId"].ToString(),
                TransformUserGroup,
                OpDate,
                theReport.PartNum,
                theReport.PartDesc,
                theReport.JobNum,
                theReport.AssemblySeq,
                theReport.MtlSeq,
                theReport.UnQualifiedReason,
                theReport.Remark,
                512,
                theReport.Plant,
                theReport.Company,
                theReport.TranID,
                DMRID,
                Id,
                DMRQualifiedQty,
                2,
                3,
                0,
                0,
                1,
                theReport.LotNum,
                Responsibility,
                DMRUnQualifiedReason,
                DMRWarehouseCode,
                DMRBinNum,
                DMRUnQualifiedReasonRemark,
                DMRUnQualifiedReasonDesc,
                ResponsibilityRemark,
                theReport.UnQualifiedReasonRemark,
                theReport.UnQualifiedReasonDesc
            });
            sql = string.Format(sql, valueStr);
            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
        }



        private static void InsertRepairRecord(int Id, decimal DMRRepairQty, string DMRJobNum, int DMRID, string TransformUserGroup, string DMRWarehouseCode, string DMRBinNum, string DMRUnQualifiedReason, string Responsibility, string DMRUnQualifiedReasonRemark, string DMRUnQualifiedReasonDesc, string ResponsibilityRemark)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string sql = @"select * from MtlReport where Id = " + Id + "";
            OpReport theReport = CommonRepository.DataTableToList<OpReport>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

            sql = @"insert into BPMSub(
                     [CreateUser]
                    ,[CreateDate]
                    ,[CheckUser]
                    ,[TransformUserGroup]
                    ,[CheckDate]
                    ,[PartNum]
                    ,[PartDesc]
                    ,[JobNum]
                    ,[AssemblySeq]
                    ,[MtlSeq]
                    ,[UnQualifiedReason]
                    ,[Remark]
                    ,[AtRole]
                    ,[Plant]
                    ,[Company]
                    ,[TranID]
                    ,[DMRID]
                    ,[RelatedID]
                    ,[DMRRepairQty]
                    ,[DMRJobNum]
                    ,[UnQualifiedType]
                    ,[Status]
                    ,[IsComplete]
                    ,[IsDelete]
                    ,[IsSubProcess]
                    ,[LotNum]
                    ,[Responsibility]
                    ,[DMRUnQualifiedReason]
                    ,[DMRWarehouseCode]
                    ,[DMRBinNum]
                    ,DMRUnQualifiedReasonRemark
                    ,DMRUnQualifiedReasonDesc
                    ,ResponsibilityRemark
                    ,UnQualifiedReasonRemark
                    ,UnQualifiedReasonDesc) values({0})";
            string valueStr = CommonRepository.ConstructInsertValues(new ArrayList
            {
                theReport.CreateUser,
                theReport.CreateDate,
                HttpContext.Current.Session["UserId"].ToString(),
                TransformUserGroup,
                OpDate,
                theReport.PartNum,
                theReport.PartDesc,
                theReport.JobNum,
                theReport.AssemblySeq,
                theReport.MtlSeq,
                theReport.UnQualifiedReason,
                theReport.Remark,
                512,
                theReport.Plant,
                theReport.Company,
                theReport.TranID,
                DMRID,
                Id,
                DMRRepairQty,
                DMRJobNum,
                2,
                3,
                0,
                0,
                1,
                theReport.LotNum,
                Responsibility,
                DMRUnQualifiedReason,
                DMRWarehouseCode,
                DMRBinNum       ,
                DMRUnQualifiedReasonRemark,
                DMRUnQualifiedReasonDesc,
                ResponsibilityRemark,
                theReport.UnQualifiedReasonRemark,
                theReport.UnQualifiedReasonDesc
            });
            sql = string.Format(sql, valueStr);
            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
        }



        public static void InsertDiscardRecord(int Id, decimal DMRUnQualifiedQty, string DMRUnQualifiedReason, int DMRID, string DMRWarehouseCode, string DMRBinNum, string TransformUserGroup, string Responsibility, string DMRUnQualifiedReasonRemark, string DMRUnQualifiedReasonDesc, string ResponsibilityRemark, string OpDate)
        {
            string sql = @"select * from MtlReport where Id = " + Id + "";
            OpReport theReport = CommonRepository.DataTableToList<OpReport>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

            sql = @"insert into BPMSub(
                     [CreateUser]
                    ,[CreateDate]
                    ,[CheckUser]
                    ,[TransformUserGroup]
                    ,[CheckDate]
                    ,[PartNum]
                    ,[PartDesc]
                    ,[JobNum]
                    ,[AssemblySeq]
                    ,[MtlSeq]
                    ,[UnQualifiedReason]
                    ,[Remark]
                    ,[AtRole]
                    ,[Plant]
                    ,[Company]
                    ,[TranID]
                    ,[DMRID]
                    ,[RelatedID]
                    ,[DMRWarehouseCode]
                    ,[DMRBinNum]
                    ,[DMRUnQualifiedQty]
                    ,[DMRUnQualifiedReason]
                    ,[UnQualifiedType]
                    ,[Status]
                    ,[IsComplete]
                    ,[IsDelete]
                    ,[IsSubProcess]
                    ,[LotNum]
                    ,[Responsibility]
                    ,DMRUnQualifiedReasonRemark
                    ,DMRUnQualifiedReasonDesc
                    ,ResponsibilityRemark
                    ,UnQualifiedReasonRemark
                    ,UnQualifiedReasonDesc) values({0})";
            string valueStr = CommonRepository.ConstructInsertValues(new ArrayList
            {
                theReport.CreateUser,
                theReport.CreateDate,
                HttpContext.Current.Session["UserId"].ToString(),
                TransformUserGroup,
                OpDate,
                theReport.PartNum,
                theReport.PartDesc,
                theReport.JobNum,
                theReport.AssemblySeq,
                theReport.MtlSeq,
                theReport.UnQualifiedReason,
                theReport.Remark,
                512,
                theReport.Plant,
                theReport.Company,
                theReport.TranID,
                DMRID,
                Id,
                DMRWarehouseCode,
                DMRBinNum,
                DMRUnQualifiedQty,
                DMRUnQualifiedReason,
                2,
                3,
                0,
                0,
                1,
                theReport.LotNum,
                Responsibility,
                DMRUnQualifiedReasonRemark,
                DMRUnQualifiedReasonDesc,
                ResponsibilityRemark,
                theReport.UnQualifiedReasonRemark,
                theReport.UnQualifiedReasonDesc
            });
            sql = string.Format(sql, valueStr);
            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
        }



        private static bool IsExistMtl(string JobNum, int AssemblySeq, int MtlSeq, string PartNum)
        {
            string sql = "select count(*) from erp.JobMtl where JobNum = '{0}' and AssemblySeq = {1} and MtlSeq = {2} and PartNum = '{3}' ";
            sql = string.Format(sql, JobNum, AssemblySeq, MtlSeq, PartNum);

            bool isexist = Convert.ToBoolean(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null));

            return isexist;
        }



        private static decimal GetMtlIssuedQty(string JobNum, int AssemblySeq, int MtlSeq)
        {
            string sql = "select IssuedQty from erp.JobMtl where JobNum = '" + JobNum + "' and AssemblySeq = " + AssemblySeq + " and MtlSeq = " + MtlSeq + "";

            decimal IssuedQty = (decimal)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);

            return IssuedQty;
        }



        public static DataTable GetMtlInfo(string JobNum, int AssemblySeq)
        {
            string sql = "select MtlSeq,PartNum,Description,JobNum,AssemblySeq from erp.JobMtl where JobNum = '" + JobNum + "' and AssemblySeq = " + AssemblySeq + "";

            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);
            return dt;
        }



        public static DataTable GetPartLots(string PartNum, int MtlSeq, string JobNum, int AssemblySeq)
        {
            string sql = " select LotNum from erp.PartTran where TranType='stk-mtl' and JobNum='" + JobNum + "' and JobSeq = " + MtlSeq + " and PartNum = '" + PartNum + "' and AssemblySeq = " + AssemblySeq + "";

            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);
            return dt;
        }


        private static decimal GetSumOfReportQty(string JobNum, int AssemblySeq, int MtlSeq, string PartNum)
        {
            string sql = " select sum(UnQualifiedQty) from  MtlReport where isdelete = 0 and JobNum='" + JobNum + "' and MtlSeq = " + MtlSeq + " and PartNum = '" + PartNum + "' and AssemblySeq = " + AssemblySeq + "";

            object o = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

            decimal SumOfReportQty = o is DBNull ? 0 : Convert.ToDecimal(o);
            return SumOfReportQty;
        }



        public static string ReportCommit(OpReport ReportInfo)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string res = CommonRepository.GetJobHeadState(ReportInfo.JobNum);
            if (res != "正常")
                return "错误：" + res;

            if (!IsExistMtl(ReportInfo.JobNum, (int)ReportInfo.AssemblySeq, (int)ReportInfo.MtlSeq, ReportInfo.PartNum))
                return "错误：物料不存在";

            if (0 >= Convert.ToDecimal(ReportInfo.UnQualifiedQty))
                return "错误：数量需大于0";

            if (ReportInfo.UnQualifiedReasonRemark.Trim() == "")
                return "错误：必须填写不良原因备注";

            if (ReportInfo.PartNum.Substring(0, 1).Trim().ToLower() == "c")
                return "错误：化学品暂不能上报";


            decimal MtlIssuedQty = GetMtlIssuedQty(ReportInfo.JobNum, (int)ReportInfo.AssemblySeq, (int)ReportInfo.MtlSeq);
            decimal SumOfReportQty = GetSumOfReportQty(ReportInfo.JobNum, (int)ReportInfo.AssemblySeq, (int)ReportInfo.MtlSeq, ReportInfo.PartNum);
            if (MtlIssuedQty < ReportInfo.UnQualifiedQty)
                return "错误：累计上报数量：" + SumOfReportQty.ToString("N2") + "(+" + ((decimal)ReportInfo.UnQualifiedQty).ToString("N2") + ")，将大于物料的已发料数量：" + MtlIssuedQty.ToString("N2") + "，或该物料未发料";


            string sql = @"select jh.Company, Plant from erp.JobHead jh  where jh.JobNum = '" + ReportInfo.JobNum + "' ";
            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);
            ReportInfo.Company = dt.Rows[0]["Company"].ToString();
            ReportInfo.Plant = dt.Rows[0]["Plant"].ToString();

            if (!HttpContext.Current.Session["Company"].ToString().Contains(ReportInfo.Company))
                return "错误：该账号没有相应的公司权限";

            if (!HttpContext.Current.Session["Plant"].ToString().Contains(ReportInfo.Plant))
                return "错误：该账号没有相应的工厂权限";

            sql = @" insert into MtlReport
                  ([CreateUser]
                  ,[CreateDate]
                  ,[PartNum]
                  ,[PartDesc]
                  ,[JobNum]
                  ,[AssemblySeq]
                  ,[MtlSeq]
                  ,[LotNum]
                  ,[UnQualifiedQty]
                  ,[UnQualifiedReason]
                  ,[Remark]
                  ,[Plant]
                  ,[Company]
                  ,[ErpCounter]
                  ,[CheckCounter]
                  ,[IsDelete]
                  ,[IsSubProcess]
                  ,[DMRQualifiedQty]
                  ,[DMRRepairQty]
                  ,[DMRUnQualifiedQty]
                    ,UnQualifiedReasonRemark
                    ,UnQualifiedReasonDesc) values({0}) ";
            string valueStr = CommonRepository.ConstructInsertValues(new ArrayList
                {
                    HttpContext.Current.Session["UserId"].ToString(),
                    OpDate,
                    ReportInfo.PartNum,
                    ReportInfo.PartDesc,
                    ReportInfo.JobNum,
                    ReportInfo.AssemblySeq,
                    ReportInfo.MtlSeq,
                    ReportInfo.LotNum,
                    ReportInfo.UnQualifiedQty,
                    ReportInfo.UnQualifiedReason,
                    ReportInfo.Remark,
                    dt.Rows[0]["Plant"].ToString(),
                    dt.Rows[0]["Company"].ToString(),
                    0,
                    ReportInfo.UnQualifiedQty,
                    0,
                    0,
                    0,
                    0,
                    0,
                    ReportInfo.UnQualifiedReasonRemark,
                    CommonRepository.GetReasonDesc(ReportInfo.UnQualifiedReason)
                });

            sql = string.Format(sql, valueStr);
            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

            sql = sql.Replace("'", "");
            AddOpLog(null, 101, OpDate, sql);


            return "处理成功";
        }



        public static string DMRCommit(OpReport DMRInfo)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string sql = @"select * from MtlReport where Id = " + DMRInfo.ID + "";

            OpReport theReport = CommonRepository.DataTableToList<OpReport>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

            if (theReport.CheckCounter == 0)
                return "错误：该不良品流程已处理";

            string res;
            if ((res = CommonRepository.GetJobHeadState(theReport.JobNum)) != "正常" && res != "该工单已完成,请联系计划部")
                return "错误：" + res;

            DMRInfo.DMRQualifiedQty = Convert.ToDecimal(DMRInfo.DMRQualifiedQty);
            DMRInfo.DMRRepairQty = Convert.ToDecimal(DMRInfo.DMRRepairQty);
            DMRInfo.DMRUnQualifiedQty = Convert.ToDecimal(DMRInfo.DMRUnQualifiedQty);
            DMRInfo.DMRJobNum = DMRInfo.DMRJobNum.Trim();

            decimal determinedQty = Convert.ToDecimal(theReport.DMRQualifiedQty) + Convert.ToDecimal(theReport.DMRRepairQty) + Convert.ToDecimal(theReport.DMRUnQualifiedQty);


            if (DMRInfo.TransformUserGroup == null || DMRInfo.TransformUserGroup == "")
                return "错误：下步接收人为空";

            if (DMRInfo.DMRQualifiedQty < 0)
                return "错误：让步数量不能为负";

            if (DMRInfo.DMRRepairQty < 0)
                return "错误：返修数量不能为负";

            if (DMRInfo.DMRUnQualifiedQty < 0)
                return "错误：废弃数量不能为负";

            if (DMRInfo.DMRQualifiedQty + DMRInfo.DMRUnQualifiedQty + DMRInfo.DMRRepairQty == 0)
                return "错误：数量不能都为0";

            if (DMRInfo.DMRQualifiedQty + DMRInfo.DMRRepairQty + DMRInfo.DMRUnQualifiedQty > theReport.CheckCounter)
                return "错误：让步数 + 返修数 + 废弃数 超过剩余待检数：" + theReport.CheckCounter;

            if (DMRInfo.DMRRepairQty > 0 && DMRInfo.DMRJobNum.Trim() == "")
                return "错误：返修工单号不能为空";

            if (DMRInfo.DMRRepairQty > 0 && CommonRepository.GetJobHeadState(DMRInfo.DMRJobNum) != "工单不存在,请联系计划部")
                return "错误：返修工单号已存在";

            if ((DMRInfo.DMRUnQualifiedQty > 0 && DMRInfo.DMRUnQualifiedReason.Trim() == ""))
                return "错误：报废原因不能为空";

            if ((DMRInfo.DMRUnQualifiedQty > 0 && DMRInfo.DMRWarehouseCode.Trim() == ""))
                return "错误：仓库不能为空";

            if (DMRInfo.DMRUnQualifiedQty > 0 && (DMRInfo.DMRBinNum.Trim() == ""))
                return "错误：库位不能为空";

            if (DMRInfo.DMRUnQualifiedQty > 0 && CheckBinNum(theReport.Company, DMRInfo.DMRBinNum, DMRInfo.DMRWarehouseCode) != "ok")
                return "错误：库位与仓库不匹配";

            lock (lock_dmr)
            {
                if (dmr_IDs.Contains((int)DMRInfo.ID))
                    return "错误：其他账号正在提交中，请勿重复提交";
                dmr_IDs.Add((int)DMRInfo.ID);
            }

            try
            {
                if (theReport.ErpCounter < 1)//不合格品界面
                {
                    int TranID = -1;
                    res = ErpAPI.CommonRepository.Startnonconf(theReport.JobNum, (int)theReport.AssemblySeq, theReport.Company, (int)theReport.MtlSeq, (decimal)theReport.UnQualifiedQty, DMRInfo.DMRWarehouseCode, DMRInfo.DMRBinNum, theReport.UnQualifiedReason, theReport.Plant, theReport.LotNum, "物料", out TranID);
                    if (res.Substring(0, 1).Trim() != "1")
                        return "错误：" + res;

                    theReport.TranID = TranID; //及时更新该值


                    sql = "update MtlReport set ErpCounter = 1 ," +
                        "tranid = " + (TranID == -1 ? "null" : TranID.ToString()) + "," +
                        "CheckCounter = " + theReport.UnQualifiedQty + " " +
                        "where id = " + DMRInfo.ID + "";

                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                }

                if (theReport.ErpCounter < 2) //检验处理界面
                {
                    int DMRID = 0;

                    res = ErpAPI.CommonRepository.StartInspProcessing((int)theReport.TranID, 0, (decimal)theReport.UnQualifiedQty, "D22", "BLPC", "01", "物料", theReport.Plant, "", 0, out DMRID); //产品其它不良 D22  D
                    if (res.Substring(0, 1).Trim() != "1")
                        return "错误：" + res;

                    theReport.DMRID = DMRID;

                    sql = " update MtlReport set ErpCounter = 2, DMRID = " + (DMRID == -1 ? "null" : DMRID.ToString()) + " where id = " + DMRInfo.ID + "";
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                }
            }
            catch (Exception ex)
            {
                return "错误：" + ex.Message;
            }
            finally
            {
                dmr_IDs.Remove((int)DMRInfo.ID);
            }


            if (DMRInfo.DMRQualifiedQty > 0)//让步
            {
                res = ErpAPI.CommonRepository.ConcessionDMRProcessing((int)theReport.DMRID, theReport.Company, theReport.Plant, theReport.PartNum, (int)theReport.AssemblySeq, (int)theReport.MtlSeq, (decimal)DMRInfo.DMRQualifiedQty, theReport.JobNum, "物料");
                if (res.Substring(0, 1).Trim() != "1")
                    return "错误：" + res + ". 请重新提交让步数量、返修数量、报废数量";

                InsertConcessionRecord((int)DMRInfo.ID, (decimal)DMRInfo.DMRQualifiedQty, DMRInfo.TransformUserGroup, (int)theReport.DMRID, DMRInfo.DMRWarehouseCode, DMRInfo.DMRBinNum, DMRInfo.DMRUnQualifiedReason, DMRInfo.Responsibility, DMRInfo.DMRUnQualifiedReasonRemark, CommonRepository.GetReasonDesc(DMRInfo.DMRUnQualifiedReason), DMRInfo.ResponsibilityRemark);

                sql = " update MtlReport set checkcounter = checkcounter - " + DMRInfo.DMRQualifiedQty + ",DMRQualifiedQty = ISNULL(DMRQualifiedQty,0) + " + DMRInfo.DMRQualifiedQty + "  where id = " + (DMRInfo.ID) + "";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                AddOpLog(DMRInfo.ID, 201, OpDate, "让步接收子流程生成");
            }

            if (DMRInfo.DMRRepairQty > 0)//返修
            {
                sql = @"select IUM from erp.JobMtl where JobNum ='" + theReport.JobNum + "'  and   AssemblySeq = " + theReport.AssemblySeq + " and  MtlSeq= " + theReport.MtlSeq + "";
                object IUM = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);

                res = ErpAPI.CommonRepository.RepairDMRProcessing((int)theReport.DMRID, theReport.Company, theReport.Plant, theReport.PartNum, (decimal)DMRInfo.DMRRepairQty, DMRInfo.DMRJobNum, IUM.ToString());
                if (res.Substring(0, 1).Trim() != "1")
                    return "错误：" + res + ". 请重新提交返修数量、报废数量";

                InsertRepairRecord((int)DMRInfo.ID, (decimal)DMRInfo.DMRRepairQty, DMRInfo.DMRJobNum, (int)theReport.DMRID, DMRInfo.TransformUserGroup, DMRInfo.DMRWarehouseCode, DMRInfo.DMRBinNum, DMRInfo.DMRUnQualifiedReason, DMRInfo.Responsibility, DMRInfo.DMRUnQualifiedReasonRemark, CommonRepository.GetReasonDesc(DMRInfo.DMRUnQualifiedReason), DMRInfo.ResponsibilityRemark);

                sql = " update MtlReport set checkcounter = checkcounter - " + DMRInfo.DMRRepairQty + ",DMRRepairQty = ISNULL(DMRRepairQty,0) + " + DMRInfo.DMRRepairQty + "  where id = " + (DMRInfo.ID) + "";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                AddOpLog(DMRInfo.ID, 201, OpDate, "返修子流程生成");

                sql = @"select PartNum , Description from erp.JobAsmbl where JobNum ='" + theReport.JobNum + "'  and   AssemblySeq = " + theReport.AssemblySeq + " ";
                DataTable dt3 = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);


                sql = @"select RelatedOperation from erp.JobMtl where JobNum = '" + theReport.JobNum + "' and  AssemblySeq = " + theReport.AssemblySeq + " and MtlSeq = " + theReport.MtlSeq + "";
                int RelatedOperation = (int)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);

                sql = @"select  OpCode , OpDesc from erp.JobOper where JobNum = '" + theReport.JobNum + "' and  AssemblySeq = " + theReport.AssemblySeq + " and OprSeq = " + RelatedOperation + " ";
                DataTable dt4 = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);


                string XML = OA_XML_Template.Create2188XML(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.MtlSeq, theReport.PartNum, theReport.PartDesc, (decimal)DMRInfo.DMRRepairQty,
                    theReport.Plant, DMRInfo.DMRJobNum, HttpContext.Current.Session["UserId"].ToString(), OpDate, "物料不良返工", DMRInfo.Responsibility,
                    "", DMRInfo.DMRUnQualifiedReasonRemark, CommonRepository.GetReasonDesc(DMRInfo.DMRUnQualifiedReason), DMRInfo.ResponsibilityRemark, dt3.Rows[0]["PartNum"].ToString(), dt3.Rows[0]["Description"].ToString(),
                    RelatedOperation + "," + dt4.Rows[0]["OpCode"].ToString() + "," + dt4.Rows[0]["OpDesc"].ToString(), "");

                OAServiceReference.WorkflowServiceXmlPortTypeClient client = new OAServiceReference.WorkflowServiceXmlPortTypeClient();
                res = client.doCreateWorkflowRequest(XML, 1012);

                if (Convert.ToInt32(res) <= 0)
                {
                    AddOpLog(DMRInfo.ID, 201, OpDate, "返修转发OA失败:" + res);
                    return "错误：返修转发OA失败:" + res;
                }

                AddOpLog(DMRInfo.ID, 201, OpDate, "返修转发OA成功，OA流程id：" + res);

            }

            if (DMRInfo.DMRUnQualifiedQty > 0)//报废
            {
                sql = @"select IUM from erp.JobMtl where JobNum ='" + theReport.JobNum + "'  and   AssemblySeq = " + theReport.AssemblySeq + " and MtlSeq= " + theReport.MtlSeq + "";
                object IUM = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);

                sql = @"select QtyPer from erp.JobMtl where JobNum ='" + theReport.JobNum + "'  and   AssemblySeq = " + theReport.AssemblySeq + " and MtlSeq= " + theReport.MtlSeq + "";
                object QtyPer = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);


                #region OA部分
                decimal amount = GetPartUnitCost(theReport.PartNum, theReport.Plant) * (decimal)DMRInfo.DMRUnQualifiedQty * (decimal)QtyPer;
                int OARequestID = 0;
                int StatusCode = 4; //自动确认报废


                if (amount >= Decimal.Parse(ConfigurationManager.AppSettings["MTLTopLimit"]))
                {
                    sql = @"select PartNum , Description from erp.JobAsmbl where JobNum ='" + theReport.JobNum + "'  and   AssemblySeq = " + theReport.AssemblySeq + " ";
                    DataTable dt3 = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);


                    sql = @"select RelatedOperation from erp.JobMtl where JobNum = '" + theReport.JobNum + "' and  AssemblySeq = " + theReport.AssemblySeq + " and MtlSeq = " + theReport.MtlSeq + "";
                    int RelatedOperation = (int)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);

                    sql = @"select  OpCode , OpDesc from erp.JobOper where JobNum = '" + theReport.JobNum + "' and  AssemblySeq = " + theReport.AssemblySeq + " and OprSeq = " + RelatedOperation + " ";
                    DataTable dt4 = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);


                    string XML = OA_XML_Template.Create2199XML(theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.MtlSeq, theReport.PartNum, theReport.PartDesc, (decimal)DMRInfo.DMRUnQualifiedQty,
                     theReport.Plant, amount, Decimal.Parse(ConfigurationManager.AppSettings["MTLTopLimit"]), HttpContext.Current.Session["UserId"].ToString(), OpDate, "物料不良返工", DMRInfo.Responsibility,
                     "", DMRInfo.DMRUnQualifiedReasonRemark, CommonRepository.GetReasonDesc(DMRInfo.DMRUnQualifiedReason), DMRInfo.ResponsibilityRemark, dt3.Rows[0]["PartNum"].ToString(), dt3.Rows[0]["Description"].ToString(),
                     RelatedOperation + "," + dt4.Rows[0]["OpCode"].ToString() + "," + dt4.Rows[0]["OpDesc"].ToString(),"");

                    OAServiceReference.WorkflowServiceXmlPortTypeClient client = new OAServiceReference.WorkflowServiceXmlPortTypeClient();
                    res = client.doCreateWorkflowRequest(XML, 1012);

                    if (Convert.ToInt32(res) <= 0)
                    {
                        AddOpLog(DMRInfo.ID, 201, OpDate, "报废转发OA失败:" + res);
                        return "错误：报废转发OA失败:" + res;
                    }

                    AddOpLog(DMRInfo.ID, 201, OpDate, "报废转发OA成功，OA流程id：" + res);


                    sql = " update MtlReport set checkcounter = checkcounter - " + DMRInfo.DMRUnQualifiedQty + "  where id = " + (DMRInfo.ID) + "";
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                    AddOpLog(DMRInfo.ID, 201, OpDate, "checkcounter -= " + DMRInfo.DMRUnQualifiedQty+ " 更新成功");
                    StatusCode = 1; //OA处理中
                }
                #endregion


                sql = @"INSERT INTO [dbo].[DiscardReview]
                       ([MtlReportID]
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
                        ,DR_ResponsibilityRemark)
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
                    ,'{14}')";
                sql = string.Format(sql, theReport.ID, HttpContext.Current.Session["UserId"].ToString(), DMRInfo.DMRUnQualifiedQty, Decimal.Parse(ConfigurationManager.AppSettings["MTLTopLimit"]),
                    amount, StatusCode, OARequestID, DMRInfo.DMRUnQualifiedReason, DMRInfo.DMRWarehouseCode, DMRInfo.DMRBinNum,
                    DMRInfo.TransformUserGroup, DMRInfo.Responsibility, DMRInfo.DMRUnQualifiedReasonRemark,
                    CommonRepository.GetReasonDesc(DMRInfo.DMRUnQualifiedReason), DMRInfo.ResponsibilityRemark);

                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                AddOpLog(DMRInfo.ID, 201, OpDate, "报废缓存记录生成成功");


                if (amount < Decimal.Parse(ConfigurationManager.AppSettings["MTLTopLimit"]))
                {
                    res = ErpAPI.CommonRepository.RefuseDMRProcessing(theReport.Company, theReport.Plant, (decimal)DMRInfo.DMRUnQualifiedQty, DMRInfo.DMRUnQualifiedReason, (int)theReport.DMRID, IUM.ToString());
                    if (res.Substring(0, 1).Trim() != "1")
                        return "错误：" + res + ". 请重新提交报废数量";

                    InsertDiscardRecord((int)DMRInfo.ID, (decimal)DMRInfo.DMRUnQualifiedQty, DMRInfo.DMRUnQualifiedReason, (int)theReport.DMRID, 
                        DMRInfo.DMRWarehouseCode, DMRInfo.DMRBinNum, DMRInfo.TransformUserGroup, DMRInfo.Responsibility, 
                        DMRInfo.DMRUnQualifiedReasonRemark, CommonRepository.GetReasonDesc(DMRInfo.DMRUnQualifiedReason), DMRInfo.ResponsibilityRemark, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));

                    sql = " update MtlReport set checkcounter = checkcounter - " + DMRInfo.DMRUnQualifiedQty + ",DMRUnQualifiedQty = ISNULL(DMRUnQualifiedQty,0) + " + DMRInfo.DMRUnQualifiedQty + "  where id = " + (DMRInfo.ID) + "";
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                    AddOpLog(DMRInfo.ID, 201, OpDate, "报废子流程生成");
                }
            }

            return "处理成功";
        }



        public static string TransferCommit(OpReport TransferInfo)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string sql = @"select * from bpmsub where Id = " + TransferInfo.ID + "";
            OpReport theSubReport = CommonRepository.DataTableToList<OpReport>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

            if (theSubReport.IsComplete == true)
                return "错误：该批次的流程已结束";

            if (theSubReport.IsDelete == true)
                return "错误：该批次的流程已删除";

            if (theSubReport.Status != 3)
                return "错误：流程未在当前节点上，在 " + theSubReport.Status + "节点";


            //以下只会执行一个if
            if (theSubReport.DMRQualifiedQty != null)
            {
                sql = " update bpmsub set " +
                        "TransformUser = '" + HttpContext.Current.Session["UserId"].ToString() + "', " +
                        "TransformDate = '" + OpDate + "'," +
                        "NextUserGroup = '" + TransferInfo.NextUserGroup + "'," +
                        "Status = " + 4 + "," +
                        "PreStatus = " + 3 + "," +
                        "AtRole = " + (256 + 128) + " " +
                        "where id = " + (theSubReport.ID) + "";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = sql.Replace("'", "");
                AddOpLog(theSubReport.ID, 301, OpDate, "让步提交|" + sql);
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

                sql = sql.Replace("'", "");
                AddOpLog(theSubReport.ID, 301, OpDate, "报废提交|" + sql);
            }
            if ((theSubReport.DMRRepairQty) != null)
            {
                sql = @"select count(*) from erp.JobOper  where JobNum = '" + theSubReport.DMRJobNum + "'";
                bool IsExistOprSeq = Convert.ToBoolean(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null));

                if (!IsExistOprSeq) return "错误：返修工单工序为空";

                sql = " update bpmsub set " +
                        "TransformUser = '" + HttpContext.Current.Session["UserId"].ToString() + "', " +
                        "TransformDate = '" + OpDate + "'," +
                        "Status = " + 4 + "," +
                        "NextUserGroup = '" + TransferInfo.NextUserGroup + "'," +
                        "PreStatus = " + 3 + "," +
                        "AtRole = " + 128 + " " +
                        "where id = " + (theSubReport.ID) + "";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = sql.Replace("'", "");
                AddOpLog(theSubReport.ID, 301, OpDate, "返修提交|" + sql);
            }

            return "处理成功";
        }



        public static string AcceptCommit(OpReport AcceptInfo)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string sql = @"select * from bpmsub where Id = " + AcceptInfo.ID + "";
            OpReport theSubReport = CommonRepository.DataTableToList<OpReport>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录


            if (theSubReport.IsComplete == true)
                return "错误：该批次的流程已结束";

            if (theSubReport.IsDelete == true)
                return "错误：该批次的流程已删除";

            if (theSubReport.Status != 4)
                return "错误：流程未在当前节点上，在 " + theSubReport.Status + "节点";



            string append = "", type = "返修提交|";
            if ((theSubReport.DMRQualifiedQty) != null)
            {
                type = "让步提交|";
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
                    OpReportRepository.InputToBC_Warehouse(theSubReport.DMRJobNum, 0, (int)nextinfo.Rows[0]["OprSeq"], AcceptInfo.BinNum,
                    (string)nextinfo.Rows[0]["OpCode"], (string)nextinfo.Rows[0]["OpDesc"], theSubReport.PartNum,
                    (string)nextinfo.Rows[0]["Description"], theSubReport.Plant, theSubReport.Company, (decimal)theSubReport.DMRRepairQty, "物料DMR返修接收");

                    AddOpLog(theSubReport.ID, 401, OpDate, "下工序表处物料入库成功");
                }
                if (theSubReport.Plant != "RRSite")
                {
                    string sql2 = @"SELECT schedule FROM BC_Plan where Company = '{0}' and Plant = '{1}' and JobNum= '{2}' and AssemblySeq={3} and JobSeq = {4}";
                    sql = string.Format(sql2, "001", theSubReport.Plant, theSubReport.DMRJobNum, 0, (int)nextinfo.Rows[0]["OprSeq"]);


                    object schedule = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                    if (schedule != null)
                        append = "。下工序在计划中，请尽快出货," + schedule;
                }
                type = "返修提交|";
            }

            sql = "update bpmsub set " +
                "NextUser = '" + HttpContext.Current.Session["UserId"].ToString() + "', " +
                "NextDate = '" + OpDate + "'," +
                "Status = 99," +
                "PreStatus = " + (theSubReport.Status) + "," +
                "IsComplete = 1 " +
                "where id = " + (theSubReport.ID) + "";
            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

            sql = sql.Replace("'", "");
            AddOpLog(theSubReport.ID, 401, OpDate, type + sql);



            return "处理成功" + append;
        }



        public static IEnumerable<OpReport> GetDMRRemainsOfUser()
        {
            if (((int)HttpContext.Current.Session["RoleId"] & 1024) != 0)
            {
                string sql = @"select * from MtlReport where CHARINDEX(company, '{0}') > 0   and   CHARINDEX(Plant, '{1}') > 0 and checkcounter > 0 and isdelete != 1 order by CreateDate desc";
                sql = string.Format(sql, HttpContext.Current.Session["Company"].ToString(), HttpContext.Current.Session["Plant"].ToString());

                DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql);
                if (dt == null)
                    return null;

                List<OpReport> Remains = CommonRepository.DataTableToList<OpReport>(dt);


                if (Remains != null)
                {
                    for (int i = 0; i < Remains.Count; i++)
                    {
                        string userid = Remains[i].CreateUser;
                        Remains[i].FromUser = CommonRepository.GetUserName(userid);
                    }
                }

                return Remains;
            }
            else return null;
        }



        public static IEnumerable<OpReport> GetRemainsOfUser()
        {
            string sql = @"select * from BPMsub where AtRole & {0} != 0 and UnQualifiedType = 2 and isdelete != 1 and isComplete != 1 and CHARINDEX(company, '{1}') > 0   and   CHARINDEX(Plant, '{2}') > 0 order by CreateDate desc";
            sql = string.Format(sql, (int)HttpContext.Current.Session["RoleId"], HttpContext.Current.Session["Company"].ToString(), HttpContext.Current.Session["Plant"].ToString());

            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql);


            if (dt == null) return null;

            for (int i = dt.Rows.Count - 1; i >= 0; i--)
            {
                if ((int)dt.Rows[i]["Status"] == 3 && ((string)dt.Rows[i]["TransformUserGroup"]).Contains((string)HttpContext.Current.Session["UserId"])) continue;

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
                    if (Remains[i].Status == 3)
                        userid = Remains[i].CheckUser;
                    if (Remains[i].Status == 4)
                        userid = Remains[i].TransformUser;
                    Remains[i].FromUser = CommonRepository.GetUserName(userid);
                }
            }

            return Remains;

        }



        public static DataTable GetNextUserGroup(int id, bool IsSubProcess)
        {
            string sql = "";
            DataTable dt = null;
            string jobnum = "", OpCode = "";
            if (!IsSubProcess)//2选3
            {
                sql = @"select * from MtlReport where Id = " + id + "";
                OpReport theReport = CommonRepository.DataTableToList<OpReport>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

                jobnum = theReport.JobNum;

                sql = @"select RelatedOperation from erp.JobMtl where JobNum = '" + theReport.JobNum + "' and  AssemblySeq = " + theReport.AssemblySeq + " and MtlSeq = " + theReport.MtlSeq + "";
                int RelatedOperation = (int)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);

                sql = @"select OpCode from erp.JobOper where JobNum = '" + theReport.JobNum + "' and  AssemblySeq = " + theReport.AssemblySeq + " and OprSeq = " + RelatedOperation + " ";
                OpCode = (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);

                sql = "select TransformUser from BPMOpCode where OpCode = '" + OpCode + "'";
                string TransformUser = (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = "select * from userfile where disabled = 0 and CHARINDEX(userid, '" + TransformUser + "') > 0 and CHARINDEX('" + theReport.Plant + "', plant) > 0 and RoleID & " + 512 + " != 0 and RoleID != 2147483647";
                dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表
            }
            else//3选4
            {
                sql = @"select * from bpmsub where Id = " + id + "";
                OpReport theSubReport = CommonRepository.DataTableToList<OpReport>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录
                jobnum = theSubReport.JobNum;
                if (theSubReport.DMRQualifiedQty != null)
                {
                    sql = "select * from userfile where userid = '" + theSubReport.CreateUser + "'";
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
                        sql = "select * from userfile where disabled = 0  and CHARINDEX('" + theSubReport.Plant + "', plant) > 0  and  RoleID & " + 16 + " != 0 and RoleID != 2147483647";
                    else
                    {
                        sql = @"select top 1  OpCode  from erp.JobOper  where JobNum = '" + theSubReport.DMRJobNum + "' order by OprSeq asc";
                        string nextOpCode = (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);

                        sql = "select NextUser from BPMOpCode where OpCode = '" + nextOpCode + "'";
                        string NextUser = (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                        sql = "select * from userfile where disabled = 0  and CHARINDEX('" + theSubReport.Plant + "', plant) > 0 and  CHARINDEX(userid, '" + NextUser + "') > 0 and  RoleID & " + 128 + " != 0 and RoleID != 2147483647";
                    }
                }
                dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表
            }

            //if (!OpCode.Contains("WL0101"))
            {
                dt = CommonRepository.NPI_Handler(jobnum.ToUpper(), dt);
                dt = CommonRepository.WD_Handler(jobnum.ToUpper(), dt);
            }

            return dt == null || dt.Rows.Count == 0 ? null : dt;
        }



        public static DataTable GetRecordByID(int ID)
        {
            string sql;
            if (ID > 0)
            {
                sql = "select * from MtlReport where ID = " + ID + "";
            }
            else
                sql = "select * from bpmsub where ID = " + (-ID) + "";

            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql);

            return dt;
        }


        public static decimal GetPartUnitCost(string partnum, string plant)
        {
            int costid = 0;

            if (plant == "MfgSys") costid = 1;
            if (plant == "RRSite") costid = 2;
            if (plant == "HDSite") costid = 3;

            decimal  cost = 0;

            string ss = @"select  case when TypeCode = 'M' then (StdLaborCost + StdBurdenCost + StdMaterialCost + StdMtlBurCost + StdSubContCost) else AvgMaterialCost end from erp.PartCost pc left join erp.part pa on pc.PartNum = pa.PartNum  where pa. PartNum = '" + partnum + "' and costid = " + costid + "";

            cost = (decimal)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, ss, null);

            return cost;
        }



        public static void AddOpLog(int? MtlReportID, int ApiNum, string OpDate, string OpDetail)
        {
            string sql = @"insert into MtlReportLog(UserId, Opdate, ApiNum,  OpDetail, MtlReportID) Values('{0}', {1}, {2}, '{3}', {4}) ";
            sql = string.Format(sql, HttpContext.Current.Session["UserId"].ToString(), "getdate()", ApiNum, OpDetail, MtlReportID == null ? "null" : MtlReportID.ToString());

            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
        }//添加操作记录

    }
}