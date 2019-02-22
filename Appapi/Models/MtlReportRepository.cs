using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace Appapi.Models
{
    public class MtlReportRepository
    {
        private static string CheckBinNum(string company, string binnum, string WarehouseCode)
        {
            string sql = "select count(*) from erp.WhseBin where Company = '{0}' and  WarehouseCode = '{1}' and BinNum = '{2}'";
            sql = string.Format(sql, company, WarehouseCode, binnum);
            int exist = (int)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);

            return exist == 0 ? "错误：库位与仓库不匹配" : "ok";
        }



        private static void InsertConcessionRecord(int Id, decimal DMRQualifiedQty, string TransformUserGroup, int DMRID)
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
                    ,[Responsibility]) values({0})";
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
                theReport.Responsibility
            });
            sql = string.Format(sql, valueStr);
            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
        }



        private static void InsertRepairRecord(int Id, decimal DMRRepairQty, string DMRJobNum, int DMRID, string TransformUserGroup)
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
                    ,[Responsibility]) values({0})";
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
                theReport.Responsibility
            });
            sql = string.Format(sql, valueStr);
            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
        }



        private static void InsertDiscardRecord(int Id, decimal DMRUnQualifiedQty, string DMRUnQualifiedReason, int DMRID, string DMRWarehouseCode, string DMRBinNum, string TransformUserGroup)
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
                    ,[Responsibility]) values({0})";
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
                theReport.Responsibility
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



        public static string ReportCommit(OpReport ReportInfo)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string res = CommonRepository.CheckJobHeadState(ReportInfo.JobNum);
            if (res != "正常")
                return "错误：" + res;

            if (!IsExistMtl(ReportInfo.JobNum, (int)ReportInfo.AssemblySeq, (int)ReportInfo.MtlSeq, ReportInfo.PartNum))
                return "错误：物料不存在";

            if (0 >= Convert.ToDecimal(ReportInfo.UnQualifiedQty))
                return "错误：数量需大于0";

            if (ReportInfo.PartNum.Substring(0, 1).Trim().ToLower() == "c")
                return "错误：化学品暂不能上报";


            if (GetMtlIssuedQty(ReportInfo.JobNum, (int)ReportInfo.AssemblySeq, (int)ReportInfo.MtlSeq) < ReportInfo.UnQualifiedQty)
                return "错误：上报数量大于物料的已发料数量，或该物料未发料";


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
                  ,[IsSubProcess]) values({0}) ";
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
                    0
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

            DMRInfo.DMRQualifiedQty = Convert.ToDecimal(DMRInfo.DMRQualifiedQty);
            DMRInfo.DMRRepairQty = Convert.ToDecimal(DMRInfo.DMRRepairQty);
            DMRInfo.DMRUnQualifiedQty = Convert.ToDecimal(DMRInfo.DMRUnQualifiedQty);

            decimal determinedQty = Convert.ToDecimal(theReport.DMRQualifiedQty) + Convert.ToDecimal(theReport.DMRRepairQty) + Convert.ToDecimal(theReport.DMRUnQualifiedQty);


            if (DMRInfo.DMRQualifiedQty < 0)
                return "错误：让步数量不能为负";

            if (DMRInfo.DMRRepairQty < 0)
                return "错误：返修数量不能为负";

            if (DMRInfo.DMRUnQualifiedQty < 0)
                return "错误：废弃数量不能为负";

            if (DMRInfo.DMRQualifiedQty + DMRInfo.DMRQualifiedQty + DMRInfo.DMRQualifiedQty == 0)
                return "错误：数量不能都为0";

            if (DMRInfo.DMRQualifiedQty + DMRInfo.DMRRepairQty + DMRInfo.DMRUnQualifiedQty > theReport.CheckCounter)
                return "错误：让步数 + 返修数 + 废弃数 超过剩余待检数：" + theReport.CheckCounter;

            if (DMRInfo.DMRRepairQty > 0 && DMRInfo.DMRJobNum == "")
                return "错误：返修工单号不能为空";

            //if (DMRInfo.DMRRepairQty > 0 && CommonRepository.CheckJobHeadState(DMRInfo.DMRJobNum) != "工单不存在")
            //    return "错误：返修工单号已存在";

            if ((DMRInfo.DMRUnQualifiedQty > 0 && DMRInfo.DMRUnQualifiedReason == ""))
                return "错误：报废原因不能为空";

            if ((DMRInfo.DMRUnQualifiedQty > 0 && DMRInfo.DMRWarehouseCode == ""))
                return "错误：仓库不能为空";

            if (DMRInfo.DMRUnQualifiedQty > 0 && (DMRInfo.DMRBinNum == ""))
                return "错误：库位不能为空";

            if (DMRInfo.DMRUnQualifiedQty > 0 && CheckBinNum(theReport.Company, DMRInfo.DMRBinNum, DMRInfo.DMRWarehouseCode) != "ok")
                return "错误：库位与仓库不匹配";

            string res;
            if (theReport.ErpCounter < 1)//不合格品界面
            {
                int TranID = -1;
                res = ErpAPI.CommonRepository.Startnonconf(theReport.JobNum, (int)theReport.AssemblySeq, theReport.Company, (int)theReport.MtlSeq, (decimal)theReport.UnQualifiedQty, DMRInfo.DMRWarehouseCode, DMRInfo.DMRBinNum, theReport.UnQualifiedReason, theReport.Plant, theReport.LotNum, out TranID);
                if (res.Substring(0, 1).Trim() != "1")
                    return "错误：" + res;

                theReport.TranID = TranID; //及时更新该值
                theReport.UnQualifiedQty = DMRInfo.UnQualifiedQty; //及时更新该值


                sql = "update MtlReport set ErpCounter = 1 ," +
                    "tranid = " + (TranID == -1 ? "null" : TranID.ToString()) + "," +
                    "UnQualifiedReason = '" + CommonRepository.GetValueAsString(DMRInfo.UnQualifiedReason) + "'," +
                    "CheckCounter = " +  DMRInfo.UnQualifiedQty + ", " +
                    "UnQualifiedQty = " + DMRInfo.UnQualifiedQty + " " +
                    "where id = " + DMRInfo.ID + ""; Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = sql.Replace("'", "");
                AddOpLog(DMRInfo.ID, 201, OpDate, "erp不合格品|" + sql);

            }

            if (theReport.ErpCounter < 2) //检验处理界面
            {
                int DMRID = 0;

                res = ErpAPI.CommonRepository.StartInspProcessing((int)theReport.TranID, 0, (decimal)theReport.UnQualifiedQty, "D22", "BLPC", "01", "物料", theReport.Plant, out DMRID); //产品其它不良 D22  D
                if (res.Substring(0, 1).Trim() != "1")
                    return "错误：" + res;

                sql = " update MtlReport set ErpCounter = 2, DMRID = " + (DMRID == -1 ? "null" : DMRID.ToString()) + " where id = " + DMRInfo.ID + "";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
            }






            if (theReport.ErpCounter < 1)//让步
            {
                res = ErpAPI.CommonRepository.StartInspProcessing((int)theReport.TranID, 0, (decimal)(DMRInfo.DMRRepairQty + DMRInfo.DMRUnQualifiedQty), DMRInfo.DMRUnQualifiedReason, DMRInfo.DMRWarehouseCode, DMRInfo.DMRBinNum, "物料", theReport.Plant, out DMRID);
                if (res.Substring(0, 1).Trim() != "1")
                    return "错误：" + res;


                if (DMRInfo.DMRQualifiedQty > 0)
                {
                    InsertConcessionRecord((int)DMRInfo.ID, (decimal)DMRInfo.DMRQualifiedQty, DMRInfo.TransformUserGroup, DMRID);
                    AddOpLog(DMRInfo.ID, 201, OpDate, "让步接收子流程生成");
                }

                sql = " update MtlReport set ErpCounter = 1, DMRID = " + DMRID + " where id = " + DMRInfo.ID + "";
                Common.SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
            }

            if (theReport.ErpCounter < 2)//返修
            {
                sql = @"select dmrid from MtlReport where id = " + DMRInfo.ID + "";
                object dmrid = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = @"select IUM from erp.JobMtl where JobNum ='" + theReport.JobNum + "'  and   AssemblySeq = " + theReport.AssemblySeq + " and  MtlSeq= " + theReport.MtlSeq + "";
                object IUM = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);

                if ((int)dmrid != 0 && DMRInfo.DMRRepairQty > 0)
                {
                    res = ErpAPI.CommonRepository.RepairDMRProcessing((int)dmrid, theReport.Company, theReport.Plant, theReport.PartNum, (decimal)DMRInfo.DMRRepairQty, DMRInfo.DMRJobNum, IUM.ToString());
                    if (res.Substring(0, 1).Trim() != "1")
                        return "错误：" + res;

                    InsertRepairRecord((int)DMRInfo.ID, (decimal)DMRInfo.DMRRepairQty, DMRInfo.DMRJobNum, (int)dmrid, DMRInfo.TransformUserGroup);
                    AddOpLog(DMRInfo.ID, 201, OpDate, "返修子流程生成");
                }
                sql = " update MtlReport set ErpCounter = 2 where id = " + (DMRInfo.ID) + "";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
            }

            if (theReport.ErpCounter < 3)//报废
            {
                sql = @"select dmrid from MtlReport where id = " + DMRInfo.ID + "";
                object dmrid = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = @"select IUM from erp.JobMtl where JobNum ='" + theReport.JobNum + "'  and   AssemblySeq = " + theReport.AssemblySeq + " and MtlSeq= " + theReport.MtlSeq + "";
                object IUM = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);


                if ((int)dmrid != 0 && DMRInfo.DMRUnQualifiedQty > 0)
                {
                    res = ErpAPI.CommonRepository.RefuseDMRProcessing(theReport.Company, theReport.Plant, (decimal)DMRInfo.DMRUnQualifiedQty, DMRInfo.DMRUnQualifiedReason, (int)dmrid, IUM.ToString());
                    if (res.Substring(0, 1).Trim() != "1")
                        return "错误：" + res;

                    InsertDiscardRecord((int)DMRInfo.ID, (decimal)DMRInfo.DMRUnQualifiedQty, DMRInfo.DMRUnQualifiedReason, (int)dmrid, DMRInfo.DMRWarehouseCode, DMRInfo.DMRBinNum, DMRInfo.TransformUserGroup);
                    AddOpLog(DMRInfo.ID, 201, OpDate, "报废子流程生成");
                }
                sql = " update MtlReport set ErpCounter = 3, checkcounter = 2 where id = " + (DMRInfo.ID) + ""; //checkcounter = 2， 完成不良品处理
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
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
                return "错误：流程未在当前节点上";


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
                return "错误：流程未在当前节点上";



            string type = "返修提交|";
            if ((theSubReport.DMRQualifiedQty) != null)
            {
                type = "让步提交|";
            }
            if ((theSubReport.DMRRepairQty) != null)
            {
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

            return "处理成功";
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
            if (!IsSubProcess)//2选3
            {
                sql = @"select * from MtlReport where Id = " + id + "";
                OpReport theReport = CommonRepository.DataTableToList<OpReport>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

                sql = @"select RelatedOperation from erp.JobMtl where JobNum = '" + theReport.JobNum + "' and  AssemblySeq = " + theReport.AssemblySeq + " and MtlSeq = " + theReport.MtlSeq + "";
                int RelatedOperation = (int)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);

                sql = @"select OpCode from erp.JobOper where JobNum = '" + theReport.JobNum + "' and  AssemblySeq = " + theReport.AssemblySeq + " and OprSeq = " + RelatedOperation + " ";
                string OpCode = (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);

                sql = "select TransformUser from BPMOpCode where OpCode = '" + OpCode + "'";
                string TransformUser = (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = "select UserID, UserName from userfile where disabled = 0 and CHARINDEX(userid, '" + TransformUser + "') > 0 and CHARINDEX('" + theReport.Plant + "', plant) > 0 and RoleID & " + 512 + " != 0 and RoleID != 2147483647";
                dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表
            }
            else//3选4
            {
                sql = @"select * from bpmsub where Id = " + id + "";
                OpReport theSubReport = CommonRepository.DataTableToList<OpReport>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

                if (theSubReport.DMRQualifiedQty != null)
                {
                    sql = "select UserID, UserName from userfile where userid = '" + theSubReport.CreateUser + "'";
                }

                if (theSubReport.DMRUnQualifiedQty != null)
                    return null;

                if (theSubReport.DMRRepairQty != null)
                {
                    sql = @"select  SubContract  from erp.JobOper where jobnum = '" + theSubReport.DMRJobNum + "' order by OprSeq asc ";
                    bool IsSubContract = Convert.ToBoolean(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null));

                    if (IsSubContract)
                        sql = "select UserID, UserName from userfile where disabled = 0  and CHARINDEX('" + theSubReport.Plant + "', plant) > 0  and  RoleID & " + 16 + " != 0 and RoleID != 2147483647";
                    else
                    {
                        sql = @"select top 1  OpCode  from erp.JobOper  where JobNum = '" + theSubReport.DMRJobNum + "' order by OprSeq asc";
                        string nextOpCode = (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);

                        sql = "select NextUser from BPMOpCode where OpCode = '" + nextOpCode + "'";
                        string NextUser = (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                        sql = "select UserID, UserName from userfile where disabled = 0  and CHARINDEX('" + theSubReport.Plant + "', plant) > 0 and  CHARINDEX(userid, '" + NextUser + "') > 0 and  RoleID & " + 128 + " != 0 and RoleID != 2147483647";
                    }
                }
                dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表
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



        private static void AddOpLog(int? MtlReportID, int ApiNum, string OpDate, string OpDetail)
        {
            string sql = @"insert into MtlReportLog(UserId, Opdate, ApiNum,  OpDetail, MtlReportID) Values('{0}', '{1}', {2}, '{3}', {4}) ";
            sql = string.Format(sql, HttpContext.Current.Session["UserId"].ToString(), OpDate, ApiNum, OpDetail, MtlReportID == null ? "null" : MtlReportID.ToString());

            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
        }//添加操作记录

    }
}