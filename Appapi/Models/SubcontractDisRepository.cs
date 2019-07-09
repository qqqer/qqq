using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace Appapi.Models
{
    public static class SubcontractDisRepository
    {
        private static void InsertDiscardRecord(int M_ID, decimal DMRUnQualifiedQty, string DMRUnQualifiedReason, int DMRID, string DMRWarehouseCode, string DMRBinNum, string ThirdUserGroup, string DMRUnQualifiedReasonRemark, string ResponsibilityRemark)
        {
            string sql = @"insert into SubcontractDisSub values({0}, getdate(),  null, null, '{1}','{2}','','','',0,0,2048,'',0,0,{3},'{4}','','{5}','{6}',3,'','{7}','{8}','{9}')";
            sql = string.Format(sql, M_ID, HttpContext.Current.Session["UserId"].ToString(), ThirdUserGroup, DMRUnQualifiedQty, DMRUnQualifiedReason, DMRWarehouseCode, DMRBinNum, DMRUnQualifiedReasonRemark, CommonRepository.GetReasonDesc(DMRUnQualifiedReason), ResponsibilityRemark);

            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
        }

        private static void InsertRepairRecord(int M_ID, decimal DMRRepairQty, string DMRJobNum, int DMRID, string ThirdUserGroup, string DMRWarehouseCode, string DMRBinNum, string DMRUnQualifiedReason, string DMRUnQualifiedReasonRemark, string ResponsibilityRemark)
        {
            string sql = @"insert into SubcontractDisSub values({0}, getdate(),  null, null,'{1}','{2}','','','',0,0,2048,'',0,{3},0,'{4}','{5}','{6}','{7}',3,'','{8}','{9}','{10}')";
            sql = string.Format(sql, M_ID, HttpContext.Current.Session["UserId"].ToString(), ThirdUserGroup, DMRRepairQty, DMRUnQualifiedReason, DMRJobNum, DMRWarehouseCode, DMRBinNum, DMRUnQualifiedReasonRemark, CommonRepository.GetReasonDesc(DMRUnQualifiedReason), ResponsibilityRemark);

            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
        }



        public static SubcontractDis GetPOInfo(int ponum, int poline, int porelnum)
        {
            string sql = @"select        
                pr.Plant,
                pr.Company,
                pr.PoNum,
                vd.VendorID  as SupplierNo,
                vd.Name as SupplierName,
                pr.XRelQty as ReqQty
               
                from erp.PORel pr
                left join erp.PODetail pd   on pr.PONum = pd.PONUM   and   pr.Company = pd.Company   and   pr.POLine = pd.POLine 
                left join erp.POHeader ph   on ph.Company = pd.Company   and   ph.PONum = pd.PONUM                 
                left join erp.Vendor vd     on ph.VendorNum = vd.VendorNum   and   ph.company = vd.company                     
                where pr.ponum = {0} and pr.poline ={1} and pr.PORelNum = {2} and pr.Company = '001'";

            sql = string.Format(sql, ponum, poline, porelnum);

            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);
            SubcontractDis commoninfo = new SubcontractDis();

            commoninfo.Plant = dt.Rows[0]["Plant"].ToString();
            commoninfo.Company = dt.Rows[0]["Company"].ToString();
            commoninfo.SupplierNo = dt.Rows[0]["SupplierNo"].ToString();
            commoninfo.SupplierName = dt.Rows[0]["SupplierName"].ToString();
            commoninfo.PoNum = (int)dt.Rows[0]["PoNum"];
            commoninfo.ReqQty = (decimal)dt.Rows[0]["ReqQty"];

            return commoninfo;
        }


        private static string CheckPO(int PoNum, int PoLine, int PORelNum)
        {
            string sql = @"select 
                ph.OpenOrder,
                ph.orderHeld,
                ph.Approve,
                ph.Confirmed,
                pd.openLine,
                pr.openRelease,
                jh.jobClosed,
                jh.jobComplete,   
                jh.JobEngineered,
                jh.JobReleased
                from erp.PORel pr

                left join erp.PODetail pd   on pr.PONum = pd.PONUM   and   pr.Company = pd.Company   and   pr.POLine = pd.POLine 
                left join erp.POHeader ph   on ph.Company = pd.Company   and   ph.PONum = pd.PONUM 
                left join erp.JobHead jh  on pr.JobNum = jh.JobNum   and   pr.Company = jh.Company 

                where pr.Company = '001' " +
                "and  pr.PONum = " + PoNum + "   and    pr.POLine = " + PoLine + "    and    pr.PORelNum = " + PORelNum + " ";

            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);


            if (dt == null)
                return "该订单项目不存在";
            if ((bool)dt.Rows[0]["OpenOrder"] == false)
                return "订单已关闭,请联系采购部";
            else if ((bool)dt.Rows[0]["orderHeld"] == true)
                return "订单已冻结,请联系采购部";
            else if ((bool)dt.Rows[0]["Approve"] == false)
                return "订单未同意,请联系采购部";
            else if ((bool)dt.Rows[0]["Confirmed"] == false)
                return "订单未确认,请联系采购部";
            else if ((bool)dt.Rows[0]["openLine"] == false)
                return "订单行已关闭,请联系采购部";
            else if ((bool)dt.Rows[0]["openRelease"] == false)
                return "发货行已关闭,请联系采购部";

            return "OK";
        }



        public static string ApplySubcontractDisQty(SubcontractDis sd)  //sd.jobseq为厂内工序
        {
            sd.JobNum = sd.JobNum.TrimEnd();
            if ((Convert.ToInt64(HttpContext.Current.Session["RoleID"]) & 2048) == 0)
                return "错误：该账号没有权限发起外协不良";

            string res = CommonRepository.GetJobHeadState(sd.JobNum);
            if (res != "正常")
                return "0|错误：" + res;

            string sql = @"select  SubContract  from erp.JobOper where jobnum = '" + sd.JobNum + "' and AssemblySeq = " + sd.AssemblySeq + " and OprSeq = " + sd.JobSeq + "  and company = '001'";
            bool IsSubContract = Convert.ToBoolean(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null));
            if (IsSubContract)
                return "错误：该工序号不是厂内工序";


            object NextOpSeq = CommonRepository.GetNextOpSeq(sd.JobNum, (int)sd.AssemblySeq, (int)sd.JobSeq); //JobSeq是打包工序

            sql = @"select  SubContract  from erp.JobOper where jobnum = '" + sd.JobNum + "' and AssemblySeq = " + sd.AssemblySeq + " and OprSeq = " + NextOpSeq + "  and company = '001'";
            IsSubContract = Convert.ToBoolean(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null));
            if (!IsSubContract)
                return "错误：下工序不是委外工序";


            sql = @"INSERT INTO [dbo].[SubcontractDisMain]
                               ([SupplierNo]
                               ,[SupplierName]
                               ,[CreateDate]
                               ,[PartNum]
                               ,[PartDesc]
                               ,[DisQty]
                               ,[IUM]
                               ,[PoNum]
                               ,[PoLine]
                               ,[PORelNum]
                               ,[JobNum]
                               ,[AssemblySeq]
                               ,[JobSeq]
                               ,[OpCode]
                               ,[OpDesc]
                               ,[CommentText]
                               ,[Plant]
                               ,[Company]
                               ,[FirstUserID]
                               ,[M_IsDelete]
                               ,[UnQualifiedReason]
                               ,[PackSlip] 
                               ,[TotalDMRQualifiedQty]
                               ,[TotalDMRRepairQty]
                               ,[TotalDMRUnQualifiedQty]
                               ,[ExistSubProcess]
                               ,[CheckCounter]
                               ,[Responsibility]
                               ,[Type]
                               ,M_Remark
                               ,ReqQty
                               ,StockPosition) values({0})";
            string values = "";
            if (sd.PoNum != 0)
            {
                DataTable FirstSubcontractedOprInfo = CommonRepository.GetSpecifiedSubcontractedOprInfo((int)sd.PoNum, sd.JobNum, (int)sd.AssemblySeq, (int)NextOpSeq, "001");
                SubcontractDis POInfo = GetPOInfo((int)sd.PoNum, (int)FirstSubcontractedOprInfo.Rows[0]["poline"], (int)FirstSubcontractedOprInfo.Rows[0]["porelnum"]);

                values = CommonRepository.ConstructInsertValues(new ArrayList
                    {
                        POInfo.SupplierNo,
                        POInfo.SupplierName,
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        FirstSubcontractedOprInfo.Rows[0]["PartNum"].ToString(),
                        FirstSubcontractedOprInfo.Rows[0]["Description"].ToString(),
                        sd.DisQty,
                        FirstSubcontractedOprInfo.Rows[0]["IUM"].ToString(),
                        sd.PoNum,
                        (int)FirstSubcontractedOprInfo.Rows[0]["poline"],
                        (int)FirstSubcontractedOprInfo.Rows[0]["porelnum"],
                        sd.JobNum.ToUpper(),
                        sd.AssemblySeq,
                        (int)FirstSubcontractedOprInfo.Rows[0]["jobseq"],
                        FirstSubcontractedOprInfo.Rows[0]["OpCode"].ToString(),
                        FirstSubcontractedOprInfo.Rows[0]["OpDesc"].ToString(),
                        FirstSubcontractedOprInfo.Rows[0]["CommentText"].ToString(),
                        POInfo.Plant,
                        POInfo.Company,
                        HttpContext.Current.Session["UserId"].ToString(),
                        0,
                        sd.UnQualifiedReason,
                        "",
                        0,
                        0,
                        0,
                        0,
                        sd.DisQty,
                        "",
                        1,
                        CommonRepository.GetValueAsString(sd.M_Remark),
                        POInfo.ReqQty,
                        sd.StockPosition
                    });
            }
            else
            {
                string sql2 = @"select  jo.IUM, jo.PartNum, Plant, jo.OpCode, jo.OpDesc, jo.Description  
                        from  erp.JobAsmbl  jd left join  erp.JobOper jo on jd.JobNum  = jo.JobNum and jd.AssemblySeq = jo.AssemblySeq
                        where jd.Company = '001' and jo.jobnum = '" + sd.JobNum + "' and jo.AssemblySeq = " + sd.AssemblySeq + " and jo.OprSeq = " + sd.JobSeq + "";
                DataTable ApplyOprInfo = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql2);

                values = CommonRepository.ConstructInsertValues(new ArrayList
                    {
                        "",
                        "",
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        ApplyOprInfo.Rows[0]["PartNum"].ToString(),
                        ApplyOprInfo.Rows[0]["Description"].ToString(),
                        sd.DisQty,
                        ApplyOprInfo.Rows[0]["IUM"].ToString(),
                        sd.PoNum,
                        0,
                        0,
                        sd.JobNum.ToUpper(),
                        sd.AssemblySeq,
                        (int)sd.JobSeq,
                        ApplyOprInfo.Rows[0]["OpCode"].ToString(),
                        ApplyOprInfo.Rows[0]["OpDesc"].ToString(),
                        "",
                        ApplyOprInfo.Rows[0]["Plant"].ToString(),
                        "001",
                        HttpContext.Current.Session["UserId"].ToString(),
                        0,
                        sd.UnQualifiedReason,
                        "",
                        0,
                        0,
                        0,
                        0,
                        sd.DisQty,
                        "",
                        1,
                        CommonRepository.GetValueAsString(sd.M_Remark),
                        0,
                        sd.StockPosition
                    });
            }
            sql = string.Format(sql, values);
            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

            AddOpLog(sd.JobNum, (int)sd.AssemblySeq, (int)sd.JobSeq, 100, "外协不良品流程发起成功", 0, 0, (int)sd.PoNum, 1);
            return "处理成功";
        }


        public static string ReceiveSubcontractDisQty(SubcontractDis sd)
        {
            sd.JobNum = sd.JobNum.TrimEnd();
            if ((Convert.ToInt64(HttpContext.Current.Session["RoleID"]) & 2048) == 0)
                return "错误：该账号没有权限发起外协不良";

            string res = CommonRepository.GetJobHeadState(sd.JobNum);
            if (res != "正常")
                return "错误：" + res;

            string sql = @"select  SubContract  from erp.JobOper where jobnum = '" + sd.JobNum + "' and AssemblySeq = " + sd.AssemblySeq + " and OprSeq = " + sd.JobSeq + "  and company = '001'";
            bool IsSubContract = Convert.ToBoolean(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null));
            if (!IsSubContract)
                return "错误：该工序不是委外工序";

            DataTable ReceiveOpSeqOfSeriesSUB = CommonRepository.GetSpecifiedSubcontractedOprInfo((int)sd.PoNum, sd.JobNum, (int)sd.AssemblySeq, (int)sd.JobSeq, "001");

            string ret = ErpAPI.ReceiptRepository.poDes((int)sd.PoNum, (int)ReceiveOpSeqOfSeriesSUB.Rows[0]["poline"], (int)ReceiveOpSeqOfSeriesSUB.Rows[0]["porelnum"], "001");
            if (ret.Substring(0, 1) == "0")
                return "错误：" + ret;
            if (ret.Substring(0, 2) != "S2" && ret.Substring(0, 1) != "P" && ret.Substring(0, 1) != "M")
                return "错误：该工序不是最后一道委外工序";


            SubcontractDis POInfo = GetPOInfo((int)sd.PoNum,
                (int)ReceiveOpSeqOfSeriesSUB.Rows[0]["poline"], (int)ReceiveOpSeqOfSeriesSUB.Rows[0]["porelnum"]);


            ret = CheckPO((int)sd.PoNum, (int)ReceiveOpSeqOfSeriesSUB.Rows[0]["poline"], (int)ReceiveOpSeqOfSeriesSUB.Rows[0]["porelnum"]);
            if (ret != "OK")
                return "错误：" + ret;


            string PackSlip = POInfo.SupplierNo + "D" + sd.PoNum + ((new Random().Next() % 100000) + 100000);
            string recdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string rcvdtlStr = "[";
            for (int i = ReceiveOpSeqOfSeriesSUB.Rows.Count - 1; i < ReceiveOpSeqOfSeriesSUB.Rows.Count; i++)
            {
                rcvdtlStr += ReceiptRepository.ConstructRcvdtlStr(
                    new String[] {
                                CommonRepository.GetValueAsString(sd.PoNum),
                                CommonRepository.GetValueAsString((int)ReceiveOpSeqOfSeriesSUB.Rows[i]["poline"]),
                                CommonRepository.GetValueAsString((int)ReceiveOpSeqOfSeriesSUB.Rows[i]["porelnum"]),
                                CommonRepository.GetValueAsString(ReceiveOpSeqOfSeriesSUB.Rows[i]["PartNum"].ToString()),
                                CommonRepository.GetValueAsString(sd.DisQty),
                                CommonRepository.GetValueAsString(ReceiveOpSeqOfSeriesSUB.Rows[i]["IUM"].ToString()),
                                CommonRepository.GetValueAsString("待检区"),
                                CommonRepository.GetValueAsString("ins"),
                                CommonRepository.GetValueAsString(sd.JobNum),
                                CommonRepository.GetValueAsString(sd.JobNum),
                                CommonRepository.GetValueAsString(sd.AssemblySeq),
                                CommonRepository.GetValueAsString((int)ReceiveOpSeqOfSeriesSUB.Rows[i]["jobseq"]),
                                CommonRepository.GetValueAsString(ReceiveOpSeqOfSeriesSUB.Rows[i]["CommentText"].ToString().Replace('\'','"')),
                                CommonRepository.GetValueAsString("PUR-SUB"),
                                CommonRepository.GetValueAsString("")}) + (i == ReceiveOpSeqOfSeriesSUB.Rows.Count - 1 ? "]" : ",");
            }

            res = "";
            if ((res = ErpAPI.ReceiptRepository.porcv(PackSlip, recdate, POInfo.SupplierNo, rcvdtlStr, "", POInfo.Company, true)) != "1|处理成功.")//若回写erp成功， 则更新对应的Receipt记录
            {
                AddOpLog(sd.JobNum, (int)sd.AssemblySeq, (int)sd.JobSeq, 101, res, 0, 0, (int)sd.PoNum, 2);
                return res;
            }
            AddOpLog(sd.JobNum, (int)sd.AssemblySeq, (int)sd.JobSeq, 101, "porcv收货执行成功", 0, 0, (int)sd.PoNum, 2);


            for (int i = ReceiveOpSeqOfSeriesSUB.Rows.Count - 1; i < ReceiveOpSeqOfSeriesSUB.Rows.Count; i++)
            {
                sql = @"INSERT INTO [dbo].[SubcontractDisMain]
                               ([SupplierNo]
                               ,[SupplierName]
                               ,[CreateDate]
                               ,[PartNum]
                               ,[PartDesc]
                               ,[DisQty]
                               ,[IUM]
                               ,[PoNum]
                               ,[PoLine]
                               ,[PORelNum]
                               ,[JobNum]
                               ,[AssemblySeq]
                               ,[JobSeq]
                               ,[OpCode]
                               ,[OpDesc]
                               ,[CommentText]
                               ,[Plant]
                               ,[Company]
                               ,[FirstUserID]
                               ,[M_IsDelete]
                               ,[UnQualifiedReason]
                               ,[PackSlip] 
                               ,[TotalDMRQualifiedQty]
                               ,[TotalDMRRepairQty]
                               ,[TotalDMRUnQualifiedQty]
                               ,[ExistSubProcess]
                               ,[CheckCounter]
                               ,[Responsibility]
                               ,[Type]
                               ,ReqQty
                               ,StockPosition) values({0})";
                string values = CommonRepository.ConstructInsertValues(new ArrayList
                    {
                        POInfo.SupplierNo,
                        POInfo.SupplierName,
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        ReceiveOpSeqOfSeriesSUB.Rows[i]["PartNum"].ToString(),
                        ReceiveOpSeqOfSeriesSUB.Rows[i]["Description"].ToString(),
                        sd.DisQty,
                        ReceiveOpSeqOfSeriesSUB.Rows[i]["IUM"].ToString(),
                        sd.PoNum,
                        (int)ReceiveOpSeqOfSeriesSUB.Rows[i]["poline"],
                        (int)ReceiveOpSeqOfSeriesSUB.Rows[i]["porelnum"],
                        sd.JobNum.ToUpper(),
                        sd.AssemblySeq,
                        (int)ReceiveOpSeqOfSeriesSUB.Rows[i]["jobseq"],
                        ReceiveOpSeqOfSeriesSUB.Rows[i]["OpCode"].ToString(),
                        ReceiveOpSeqOfSeriesSUB.Rows[i]["OpDesc"].ToString(),
                        ReceiveOpSeqOfSeriesSUB.Rows[i]["CommentText"].ToString(),
                        POInfo.Plant,
                        POInfo.Company,
                        HttpContext.Current.Session["UserId"].ToString(),
                        0,
                        sd.UnQualifiedReason,
                        PackSlip,
                        0,
                        0,
                        0,
                        0,
                        sd.DisQty,
                        "",
                        2,
                        POInfo.ReqQty,
                        sd.StockPosition
                    });
                sql = string.Format(sql, values);
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
            }

            AddOpLog(sd.JobNum, (int)sd.AssemblySeq, (int)sd.JobSeq, 101, "外协不良品流程发起成功", 0, 0, (int)sd.PoNum, 2);
            return "处理成功";
        }


        public static string DMRCommit(SubcontractDis sd) //apinum 201
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string sql = @"select * from SubcontractDisMain where m_id = " + sd.M_ID + "";

            SubcontractDis theSubcontractDis = CommonRepository.DataTableToList<SubcontractDis>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

            if (theSubcontractDis.M_IsDelete == true)
                return "错误：所属的主流程已删除";
            if (theSubcontractDis.CheckCounter == 0)
                return "错误：该报工流程下的所有不良品已处理完毕";
            if (sd.TransferUserGroup == "")
                return "错误：下步接收人不能为空";


            sd.DMRQualifiedQty = 0;
            sd.DMRRepairQty = Convert.ToDecimal(sd.DMRRepairQty);
            sd.DMRUnQualifiedQty = Convert.ToDecimal(sd.DMRUnQualifiedQty);


            decimal determinedQty = Convert.ToDecimal(theSubcontractDis.TotalDMRQualifiedQty) + Convert.ToDecimal(theSubcontractDis.TotalDMRRepairQty) + Convert.ToDecimal(theSubcontractDis.TotalDMRUnQualifiedQty);

            if (sd.DMRQualifiedQty < 0)
                return "错误：让步数量不能为负";

            if (sd.DMRRepairQty < 0)
                return "错误：返修数量不能为负";

            if (sd.DMRUnQualifiedQty < 0)
                return "错误：废弃数量不能为负";

            if (sd.DMRQualifiedQty + sd.DMRUnQualifiedQty + sd.DMRRepairQty == 0)
                return "错误：数量不能都为0";

            if (sd.DMRQualifiedQty + sd.DMRRepairQty + sd.DMRUnQualifiedQty > theSubcontractDis.DisQty - determinedQty)
                return "错误：让步数 + 返修数 + 废弃数 超过剩余待检数：" + (theSubcontractDis.DisQty - determinedQty);

            if (sd.DMRRepairQty > 0 && sd.DMRJobNum == "")
                return "错误：返修工单号不能为空";

            if (sd.DMRRepairQty > 0 && CommonRepository.GetJobHeadState(sd.DMRJobNum) != "工单不存在,请联系计划部")
                return "错误：返修工单号已存在";

            if ((sd.DMRUnQualifiedQty > 0 && sd.DMRUnQualifiedReason == ""))
                return "错误：报废原因不能为空";

            if ((sd.DMRUnQualifiedQty > 0 && sd.DMRWarehouseCode == ""))
                return "错误：仓库不能为空";

            if (sd.DMRUnQualifiedQty > 0 && (sd.DMRBinNum == ""))
                return "错误：库位不能为空";

            if (sd.DMRUnQualifiedQty > 0 && OpReportRepository.CheckBinNum(theSubcontractDis.Company, sd.DMRBinNum, sd.DMRWarehouseCode) != "ok")
                return "错误：库位与仓库不匹配";



            string res, type = (int)theSubcontractDis.Type == 1 ? "外协不良1" : "外协不良2";
            if (theSubcontractDis.DMRID == 0)
            {
                int DMRID = 0, TranID = 0;
                if (theSubcontractDis.Type == 1 && theSubcontractDis.TranID == 0) //我方不良，以不合格品发起
                {
                    object PreOpSeq = CommonRepository.GetPreOpSeq(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, (int)theSubcontractDis.JobSeq); //获取上一道厂内工序号PreOpSeq， JobSeq是厂内工序的下道委外工序

                    res = ErpAPI.CommonRepository.Startnonconf(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, theSubcontractDis.Company, (int)PreOpSeq, (decimal)theSubcontractDis.DisQty, sd.DMRWarehouseCode, sd.DMRBinNum, "B19", theSubcontractDis.Plant, "", type, out TranID); //B19 其他不良
                    if (res.Substring(0, 1).Trim() != "1")
                    {
                        AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, (int)theSubcontractDis.JobSeq, 201, PreOpSeq + "厂内工序不合格品发起失败：" + res, theSubcontractDis.M_ID, 0, (int)theSubcontractDis.PoNum, (int)theSubcontractDis.Type);
                        return "错误：" + res;
                    }

                    sql = @"update SubcontractDisMain set tranid = " + TranID + " where m_id = " + sd.M_ID + "";
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);


                    theSubcontractDis.TranID = TranID;
                    AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, (int)theSubcontractDis.JobSeq, 201, PreOpSeq + "厂内工序不合格品发起成功，TranID = " + TranID, theSubcontractDis.M_ID, 0, (int)theSubcontractDis.PoNum, (int)theSubcontractDis.Type);
                }


                res = ErpAPI.CommonRepository.StartInspProcessing((int)theSubcontractDis.TranID, 0, (decimal)theSubcontractDis.DisQty, "D22", "BLPC", "01", type, theSubcontractDis.Plant, theSubcontractDis.PackSlip, (int)theSubcontractDis.PoLine, out DMRID); //产品其它不良 D22  D
                if (res.Substring(0, 1).Trim() != "1")
                {
                    AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, (int)theSubcontractDis.JobSeq, 201, "检验处理失败：" + res, theSubcontractDis.M_ID, 0, (int)theSubcontractDis.PoNum, (int)theSubcontractDis.Type);
                    return "错误：" + res;
                }

                theSubcontractDis.DMRID = DMRID;
                AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, (int)theSubcontractDis.JobSeq, 201, "检验处理成功，DMRID = " + DMRID, theSubcontractDis.M_ID, 0, (int)theSubcontractDis.PoNum, (int)theSubcontractDis.Type);


                sql = @"update SubcontractDisMain set Dmrid = " + DMRID + "  where m_id = " + sd.M_ID + "";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
            }

            if (sd.DMRRepairQty > 0)
            {
                res = ErpAPI.CommonRepository.RepairDMRProcessing((int)theSubcontractDis.DMRID, theSubcontractDis.Company, theSubcontractDis.Plant, theSubcontractDis.PartNum, (decimal)sd.DMRRepairQty, sd.DMRJobNum, theSubcontractDis.IUM);
                if (res.Substring(0, 1).Trim() != "1")
                {
                    AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, (int)theSubcontractDis.JobSeq, 201, "DMR返修失败：" + res, theSubcontractDis.M_ID, 0, (int)theSubcontractDis.PoNum, (int)theSubcontractDis.Type);
                    return "错误：" + res + ". 请重新提交返修数量、报废数量";
                }

                InsertRepairRecord(sd.M_ID, (decimal)sd.DMRRepairQty, sd.DMRJobNum, (int)theSubcontractDis.DMRID, sd.TransferUserGroup, sd.DMRWarehouseCode, sd.DMRBinNum, sd.DMRUnQualifiedReason,sd.DMRUnQualifiedReasonRemark, sd.ResponsibilityRemark);


                sql = " update SubcontractDisMain set Responsibility = '" + sd.Responsibility + "', ExistSubProcess = 1, checkcounter = checkcounter - " + sd.DMRRepairQty + ",TotalDMRRepairQty = ISNULL(TotalDMRRepairQty,0) + " + sd.DMRRepairQty + "  where m_id = " + (sd.M_ID) + "";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, (int)theSubcontractDis.JobSeq, 201, "返修子流程生成", theSubcontractDis.M_ID, 0, (int)theSubcontractDis.PoNum, (int)theSubcontractDis.Type);
            }

            if (sd.DMRUnQualifiedQty > 0)
            {

                res = ErpAPI.CommonRepository.RefuseDMRProcessing(theSubcontractDis.Company, theSubcontractDis.Plant, (decimal)sd.DMRUnQualifiedQty, sd.DMRUnQualifiedReason, (int)theSubcontractDis.DMRID, theSubcontractDis.IUM);
                if (res.Substring(0, 1).Trim() != "1")
                {
                    AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, (int)theSubcontractDis.JobSeq, 201, "DMR拒收失败：" + res, theSubcontractDis.M_ID, 0, (int)theSubcontractDis.PoNum, (int)theSubcontractDis.Type);
                    return "错误：" + res + ". 请重新提交报废数量";
                }

                InsertDiscardRecord(theSubcontractDis.M_ID, (decimal)sd.DMRUnQualifiedQty, sd.DMRUnQualifiedReason, (int)theSubcontractDis.DMRID, sd.DMRWarehouseCode, sd.DMRBinNum, sd.TransferUserGroup, sd.DMRUnQualifiedReasonRemark, sd.ResponsibilityRemark);

                sql = " update SubcontractDisMain set ExistSubProcess = 1,checkcounter = checkcounter - " + sd.DMRUnQualifiedQty + ", TotalDMRUnQualifiedQty = ISNULL(totalDMRUnQualifiedQty,0) + " + sd.DMRUnQualifiedQty + "  where m_id = " + (sd.M_ID) + "";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, (int)theSubcontractDis.JobSeq, 201, "报废子流程生成", theSubcontractDis.M_ID, 0, (int)theSubcontractDis.PoNum, (int)theSubcontractDis.Type);
            }

            return "处理成功";
        }


        public static string TransferCommitOfSub(SubcontractDis sd)//apinum 301
        {
            try
            {
                string sql = @"select * from SubcontractDisMain sdm left join SubcontractDisSub sds on sdm.M_ID = sds.RelatedID where sds.S_ID = " + sd.S_ID + "";
                SubcontractDis theSubcontractDis = CommonRepository.DataTableToList<SubcontractDis>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

                if (theSubcontractDis.S_IsDelete == true)
                    return "错误：该流程已删除";

                if (theSubcontractDis.IsComplete == true)
                    return "错误：该流程已结束";

                if (theSubcontractDis.NodeNum != 3)
                    return "错误：流程未在当前节点上，在 " + theSubcontractDis.NodeNum + "节点";



                if (theSubcontractDis.DMRRepairQty > 0)
                {
                    if (sd.AccepterUserGroup == "") return "错误：下步接收人不能为空";

                    sql = @"select count(*) from erp.JobOper  where JobNum = '" + theSubcontractDis.DMRJobNum + "'";
                    bool IsExistOprSeq = Convert.ToBoolean(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null));

                    if (!IsExistOprSeq) return "错误：返修工单工序为空，请联系计划部";

                    sql = @"select  SubContract  from erp.JobOper where jobnum = '" + theSubcontractDis.DMRJobNum + "' order by OprSeq asc ";
                    bool IsSubContract = Convert.ToBoolean(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null));


                    theSubcontractDis.AtRole = IsSubContract ? 16 : 128;

                    sql = @"update SubcontractDisSub set atrole = " + theSubcontractDis.AtRole + ", NodeNum = 4, TransferDate = getdate(), TransferUserID = '" + HttpContext.Current.Session["UserId"].ToString() + "', AccepterUserGroup = '" + sd.AccepterUserGroup + "'  where s_id = " + theSubcontractDis.S_ID + "";
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                    AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, (int)theSubcontractDis.JobSeq, 301, "返修流程提交成功|" + sql, 0, (int)theSubcontractDis.S_ID, (int)theSubcontractDis.PoNum, (int)theSubcontractDis.Type);
                }

                else if (theSubcontractDis.DMRUnQualifiedQty > 0)
                {
                    sql = @"update SubcontractDisSub set IsComplete = 1,NodeNum = 99, TransferDate = getdate(), TransferUserID = '" + HttpContext.Current.Session["UserId"].ToString() + "' where s_id = " + theSubcontractDis.S_ID + "";
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                    AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, (int)theSubcontractDis.JobSeq, 301, "报废流程完结", 0, (int)theSubcontractDis.S_ID, (int)theSubcontractDis.PoNum, (int)theSubcontractDis.Type);
                }

                return "处理成功";
            }
            catch (Exception ex)
            {
                return "错误：" + ex.Message;
            }
        }


        public static string AccepterCommitOfSub(SubcontractDis sd)//apinum 401
        {
            try
            {
                string sql = @"select * from SubcontractDisMain sdm left join SubcontractDisSub sds on sdm.M_ID = sds.RelatedID where sds.S_ID = " + sd.S_ID + "";
                SubcontractDis theSubcontractDis = CommonRepository.DataTableToList<SubcontractDis>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

                if (theSubcontractDis.S_IsDelete == true)
                    return "错误：该流程已删除";

                if (theSubcontractDis.IsComplete == true)
                    return "错误：该流程已结束";



                sql = @"select OpCode,  OprSeq, jo.PartNum, OpDesc, ja.Description from erp.JobOper jo left join erp.JobAsmbl ja on ja.JobNum = jo.JobNum 
                                    where ja.Company = '001'  and jo.JobNum = '" + theSubcontractDis.DMRJobNum + "'  order by OprSeq asc";
                DataTable nextJobInfo = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);

                if (((string)nextJobInfo.Rows[0]["OpCode"]).Substring(0, 2) == "BC" && theSubcontractDis.Plant != "RRSite")
                {
                    if (sd.BinNum == "")
                    {
                        return "错误：下工序表处，请填写表处现场仓库位";
                    }
                    OpReportRepository.InputToBC_Warehouse(theSubcontractDis.DMRJobNum, 0, (int)nextJobInfo.Rows[0]["OprSeq"], sd.BinNum,
                    (string)nextJobInfo.Rows[0]["OpCode"], (string)nextJobInfo.Rows[0]["OpDesc"], theSubcontractDis.PartNum,
                    (string)nextJobInfo.Rows[0]["Description"], theSubcontractDis.Plant, theSubcontractDis.Company, (decimal)theSubcontractDis.DMRRepairQty, "外协不良返修接收");

                    AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, (int)theSubcontractDis.JobSeq, 401, "下工序表处物料入库成功", 0, (int)theSubcontractDis.S_ID, (int)theSubcontractDis.PoNum, (int)theSubcontractDis.Type);
                }



                sql = @"update SubcontractDisSub set binnum = '" + sd.BinNum + "', IsComplete = 1,NodeNum = 99, AccepterDate = getdate(), AccepterUserID = '" + HttpContext.Current.Session["UserId"].ToString() + "' where s_id = " + theSubcontractDis.S_ID + "";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, (int)theSubcontractDis.JobSeq, 401, "返修流程完结", 0, (int)theSubcontractDis.S_ID, (int)theSubcontractDis.PoNum, (int)theSubcontractDis.Type);

                return "处理成功";
            }
            catch (Exception ex)
            {
                return "错误：" + ex.Message;
            }
        }


        public static IEnumerable<SubcontractDis> GetDMRRemainsOfUser()
        {
            if (((int)HttpContext.Current.Session["RoleId"] & 1024) != 0)
            {
                string sql = @"select * from SubcontractDisMain where CHARINDEX(company, '{0}') > 0   and   CHARINDEX(Plant, '{1}') > 0 and checkcounter > 0  and m_isdelete != 1 order by CreateDate desc";
                sql = string.Format(sql, HttpContext.Current.Session["Company"].ToString(), HttpContext.Current.Session["Plant"].ToString());
                DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql);


                if (dt == null) return null;


                List<SubcontractDis> Remains = CommonRepository.DataTableToList<SubcontractDis>(dt);
                if (Remains != null)
                {
                    for (int i = 0; i < Remains.Count; i++)
                    {
                        Remains[i].FromUser = CommonRepository.GetUserName(Remains[i].FirstUserID);
                    }
                }
                return Remains;
            }

            return null;
        }


        public static IEnumerable<SubcontractDis> GetRemainsOfUser()
        {
            string sql = @"select * from SubcontractDisMain sdm left join SubcontractDisSub sds on sdm.M_ID = sds.RelatedID
            where AtRole & {0} != 0 and s_isdelete != 1 and isComplete != 1 and CHARINDEX(company, '{1}') > 0   and   CHARINDEX(Plant, '{2}') > 0 order by CreateDate desc";
            sql = string.Format(sql, (int)HttpContext.Current.Session["RoleId"], HttpContext.Current.Session["Company"].ToString(), HttpContext.Current.Session["Plant"].ToString());
            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql);


            if (dt == null) return null;

            for (int i = dt.Rows.Count - 1; i >= 0; i--)
            {
                if ((int)dt.Rows[i]["NodeNum"] == 3 && ((string)dt.Rows[i]["TransferUserGroup"]).Contains((string)HttpContext.Current.Session["UserId"])) continue;

                else if ((int)dt.Rows[i]["NodeNum"] == 4 && ((string)dt.Rows[i]["AccepterUserGroup"]).Contains((string)HttpContext.Current.Session["UserId"])) continue;

                else
                    dt.Rows[i].Delete();//当前节点群组未包含改用户
            }


            List<SubcontractDis> Remains = CommonRepository.DataTableToList<SubcontractDis>(dt);
            if (Remains != null)
            {
                for (int i = 0; i < Remains.Count; i++)
                {
                    string userid = "";
                    if (Remains[i].NodeNum == 3)
                        userid = Remains[i].DMRUserID;
                    if (Remains[i].NodeNum == 4)
                        userid = Remains[i].TransferUserID;
                    Remains[i].FromUser = CommonRepository.GetUserName(userid);
                }
            }

            return Remains;
        }


        public static DataTable GetTransferUserGroup(int m_id)
        {
            string sql = @"select * from SubcontractDisMain where m_id = " + m_id + "";
            SubcontractDis theSubcontractDis = CommonRepository.DataTableToList<SubcontractDis>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

            sql = "select * from userfile where CHARINDEX('" + theSubcontractDis.Company + "', company) > 0 and CHARINDEX('" + theSubcontractDis.Plant + "', plant) > 0 and disabled = 0 and RoleID & 2048 != 0 and RoleID != 2147483647";
            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表

            return dt;
        }


        public static DataTable GetAccepterUserGroup(int s_id)
        {
            string sql = @"select * from SubcontractDisMain sdm left join SubcontractDisSub sds on sdm.M_ID = sds.RelatedID where sds.S_ID = " + s_id + "";
            SubcontractDis theSubcontractDis = CommonRepository.DataTableToList<SubcontractDis>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

            long nextRole = 0;
            string nextOpCode = "";
            DataTable dt = null;

            if (theSubcontractDis.DMRUnQualifiedQty > 0) return null;

            if (theSubcontractDis.DMRRepairQty > 0)
            {
                sql = @"select count(*) from erp.JobOper  where JobNum = '" + theSubcontractDis.DMRJobNum + "'";
                bool IsExistOprSeq = Convert.ToBoolean(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null));

                if (!IsExistOprSeq) return null;

                sql = @"select  SubContract  from erp.JobOper where jobnum = '" + theSubcontractDis.DMRJobNum + "' order by OprSeq asc ";
                bool IsSubContract = Convert.ToBoolean(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null));

                if (IsSubContract)
                    nextRole = 16;
                else
                {
                    nextRole = 128;
                    sql = @"select top 1  OpCode  from erp.JobOper  where JobNum = '" + theSubcontractDis.DMRJobNum + "' order by OprSeq asc";
                    nextOpCode = (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);
                }
            }


            if (nextRole == 16)
            {
                sql = "select * from userfile where disabled = 0 and  CHARINDEX('" + theSubcontractDis.Company + "', company) > 0 and CHARINDEX('" + theSubcontractDis.Plant + "', plant) > 0 and RoleID & " + nextRole + " != 0 and RoleID != 2147483647";
                dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表
            }

            else if (nextRole == 128)
            {
                sql = "select NextUser from BPMOpCode where OpCode = '" + nextOpCode + "'";
                string NextUser = (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = "select * from userfile where disabled = 0  and CHARINDEX('" + theSubcontractDis.Plant + "', plant) > 0 and CHARINDEX(userid, '" + NextUser + "') > 0 and RoleID & " + nextRole + " != 0 and RoleID != 2147483647";
                dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表
            }

            if (nextRole == 128 && !nextOpCode.Contains("JJ"))
            {
                dt = CommonRepository.NPI_Handler(theSubcontractDis.DMRJobNum.ToUpper(), dt);
                dt = CommonRepository.WD_Handler(theSubcontractDis.DMRJobNum.ToUpper(), dt);
            }

            return dt == null || dt.Rows.Count == 0 ? null : dt;
        }


        public static DataTable GetRecordByID(int ID)
        {
            string sql;
            if (ID > 0)
            {
                sql = @"select * from SubcontractDisMain sdm left join SubcontractDisSub sds on sdm.M_ID = sds.RelatedID where sdm.M_ID = " + ID + "";
            }
            else
                sql = @"select * from SubcontractDisMain sdm left join SubcontractDisSub sds on sdm.M_ID = sds.RelatedID where sds.S_ID = " + (-ID) + "";

            DataTable dt = (Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)); //获取该批次记录

            return dt;
        }



        private static void AddOpLog(string JobNum, int AssemblySeq, int jobseq, int ApiNum, string OpDetail, int M_ID, int S_ID, int PONum, int type)
        {
            string sql = @"insert into SubcontractDisLog(JobNum, AssemblySeq, jobseq , UserId, Opdate, ApiNum, OpDetail,M_ID,S_ID, PONum, type) Values('{0}', {1}, " + jobseq + ", '{2}', {3}, {4},  @OpDetail,{5},{6},{7},{8}) ";
            sql = string.Format(sql, JobNum, AssemblySeq, HttpContext.Current.Session["UserId"].ToString(), "getdate()", ApiNum, M_ID, S_ID, PONum, type);

            SqlParameter[] ps = new SqlParameter[] { new SqlParameter("@OpDetail", OpDetail) };
            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, ps);

        }//添加操作记录
    }
}