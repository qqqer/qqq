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
        private static void InsertDiscardRecord(int M_ID, decimal DMRUnQualifiedQty, string DMRUnQualifiedReason, int DMRID, string DMRWarehouseCode, string DMRBinNum, string ThirdUserGroup)
        {
            string sql = @"insert into SubcontractDisSub values({0}, getdate(),  null, null, '{1}','{2}','','','',0,0,2048,'',0,0,{3},'{4}','','{5}','{6}',3)";
            sql = string.Format(sql, M_ID, HttpContext.Current.Session["UserId"].ToString(), ThirdUserGroup, DMRUnQualifiedQty, DMRUnQualifiedReason, DMRWarehouseCode, DMRBinNum);

            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
        }

        private static void InsertRepairRecord(int M_ID, decimal DMRRepairQty, string DMRJobNum, int DMRID, string ThirdUserGroup, string DMRWarehouseCode, string DMRBinNum, string DMRUnQualifiedReason)
        {
            string sql = @"insert into SubcontractDisSub values({0}, getdate(),  null, null,'{1}','{2}','','','',0,0,2048,'',0,{3},0,'{4}','{5}','{6}','{7}',3)";
            sql = string.Format(sql, M_ID, HttpContext.Current.Session["UserId"].ToString(), ThirdUserGroup, DMRRepairQty, DMRUnQualifiedReason, DMRJobNum, DMRWarehouseCode, DMRBinNum);

            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
        }



        

        public static SubcontractDis GetCommonInfo(int ponum, int poline, int porelnum)
        {
            string sql = @"select        
                pr.Plant,
                pr.Company,
                pr.JobNum, 
                pr.AssemblySeq,  
                pr.PoNum,
                vd.VendorID  as SupplierNo,
                vd.Name as SupplierName
               
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
            commoninfo.JobNum = dt.Rows[0]["JobNum"].ToString();
            commoninfo.AssemblySeq = (int)dt.Rows[0]["AssemblySeq"];
            commoninfo.PoNum = (int)dt.Rows[0]["PoNum"];

            return commoninfo;
        }


        public static string ReceiveSubcontractDisQty(SubcontractDis sd)
        {
            if ((Convert.ToInt64(HttpContext.Current.Session["RoleID"]) & 2048) == 0)
                return "0|错误：该账号没有权限发起外协不良";

            DataTable AllOpSeqOfSeriesSUB = CommonRepository.GetSpecifiedSubcontractedOprInfo((int)sd.PoNum, sd.JobNum, (int)sd.AssemblySeq, (int)sd.JobSeq, "001");
            SubcontractDis CommonInfo = GetCommonInfo((int)sd.PoNum,
                (int)AllOpSeqOfSeriesSUB.Rows[0]["poline"], (int)AllOpSeqOfSeriesSUB.Rows[0]["porelnum"]);

            string PackSlip = CommonInfo.SupplierNo + "D" + sd.PoNum + ((new Random().Next() % 100000) + 100000);
            string recdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string sql = "";

            for (int i = AllOpSeqOfSeriesSUB.Rows.Count - 1; i < AllOpSeqOfSeriesSUB.Rows.Count; i++)
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
                               ,[Type]) values({0})";
                string values = CommonRepository.ConstructInsertValues(new ArrayList
                    {
                        CommonInfo.SupplierNo,
                        CommonInfo.SupplierName,
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        AllOpSeqOfSeriesSUB.Rows[i]["PartNum"].ToString(),
                        AllOpSeqOfSeriesSUB.Rows[i]["Description"].ToString(),
                        sd.DisQty,
                        AllOpSeqOfSeriesSUB.Rows[i]["IUM"].ToString(),
                        sd.PoNum,
                        (int)AllOpSeqOfSeriesSUB.Rows[i]["poline"],
                        (int)AllOpSeqOfSeriesSUB.Rows[i]["porelnum"],
                        sd.JobNum.ToUpper(),
                        sd.AssemblySeq,
                        (int)AllOpSeqOfSeriesSUB.Rows[i]["jobseq"],
                        AllOpSeqOfSeriesSUB.Rows[i]["OpCode"].ToString(),
                        AllOpSeqOfSeriesSUB.Rows[i]["OpDesc"].ToString(),
                        AllOpSeqOfSeriesSUB.Rows[i]["CommentText"].ToString(),
                        CommonInfo.Plant,
                        CommonInfo.Company,
                        HttpContext.Current.Session["UserId"].ToString(),
                        0,
                        sd.UnQualifiedReason,
                        PackSlip,
                        0,
                        0,
                        0,
                        0,
                        sd.DisQty,
                        sd.Responsibility,
                        sd.Type
                    });
                sql = string.Format(sql, values);
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
            }


            string rcvdtlStr = "[";
            for (int i = AllOpSeqOfSeriesSUB.Rows.Count - 1; i < AllOpSeqOfSeriesSUB.Rows.Count; i++)
            {
                rcvdtlStr += ReceiptRepository.ConstructRcvdtlStr(
                    new String[] {
                                CommonRepository.GetValueAsString(sd.PoNum),
                                CommonRepository.GetValueAsString((int)AllOpSeqOfSeriesSUB.Rows[i]["poline"]),
                                CommonRepository.GetValueAsString((int)AllOpSeqOfSeriesSUB.Rows[i]["porelnum"]),
                                CommonRepository.GetValueAsString(AllOpSeqOfSeriesSUB.Rows[i]["PartNum"].ToString()),
                                CommonRepository.GetValueAsString(sd.DisQty),
                                CommonRepository.GetValueAsString(AllOpSeqOfSeriesSUB.Rows[i]["IUM"].ToString()),
                                CommonRepository.GetValueAsString("待检区"),
                                CommonRepository.GetValueAsString("ins"),
                                CommonRepository.GetValueAsString(sd.JobNum),
                                CommonRepository.GetValueAsString(sd.JobNum),
                                CommonRepository.GetValueAsString(sd.AssemblySeq),
                                CommonRepository.GetValueAsString((int)AllOpSeqOfSeriesSUB.Rows[i]["jobseq"]),
                                CommonRepository.GetValueAsString(AllOpSeqOfSeriesSUB.Rows[i]["CommentText"].ToString().Replace('\'','"')),
                                CommonRepository.GetValueAsString("PUR-SUB"),
                                CommonRepository.GetValueAsString("")}) + (i == AllOpSeqOfSeriesSUB.Rows.Count - 1 ? "]" : ",");
            }

            string res = "";
            if ((res = ErpAPI.ReceiptRepository.porcv(PackSlip, recdate, CommonInfo.SupplierNo, rcvdtlStr, "", CommonInfo.Company, true)) != "1|处理成功.")//若回写erp成功， 则更新对应的Receipt记录
            {
                AddOpLog(sd.JobNum, (int)sd.AssemblySeq, 101, res, 0, 0, (int)sd.PoNum);
                return res;
            }

            AddOpLog(sd.JobNum, (int)sd.AssemblySeq, 101, "外协不良品流程发起成功", 0, 0, (int)sd.PoNum);
            return "处理成功";
        }


        public static string ReceiveSubcontractDisQty(SubcontractDis sd)
        {
            if ((Convert.ToInt64(HttpContext.Current.Session["RoleID"]) & 2048) == 0)
                return "0|错误：该账号没有权限发起外协不良";

            DataTable AllOpSeqOfSeriesSUB = CommonRepository.GetSpecifiedSubcontractedOprInfo((int)sd.PoNum, sd.JobNum, (int)sd.AssemblySeq, (int)sd.JobSeq, "001");
            SubcontractDis CommonInfo = GetCommonInfo((int)sd.PoNum,
                (int)AllOpSeqOfSeriesSUB.Rows[0]["poline"], (int)AllOpSeqOfSeriesSUB.Rows[0]["porelnum"]);

            string PackSlip = CommonInfo.SupplierNo + "D" + sd.PoNum + ((new Random().Next() % 100000) + 100000);
            string recdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string sql = "";

            for (int i = AllOpSeqOfSeriesSUB.Rows.Count - 1; i < AllOpSeqOfSeriesSUB.Rows.Count; i++)
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
                               ,[Type]) values({0})";
                string values = CommonRepository.ConstructInsertValues(new ArrayList
                    {
                        CommonInfo.SupplierNo,
                        CommonInfo.SupplierName,
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        AllOpSeqOfSeriesSUB.Rows[i]["PartNum"].ToString(),
                        AllOpSeqOfSeriesSUB.Rows[i]["Description"].ToString(),
                        sd.DisQty,
                        AllOpSeqOfSeriesSUB.Rows[i]["IUM"].ToString(),
                        sd.PoNum,
                        (int)AllOpSeqOfSeriesSUB.Rows[i]["poline"],
                        (int)AllOpSeqOfSeriesSUB.Rows[i]["porelnum"],
                        sd.JobNum.ToUpper(),
                        sd.AssemblySeq,
                        (int)AllOpSeqOfSeriesSUB.Rows[i]["jobseq"],
                        AllOpSeqOfSeriesSUB.Rows[i]["OpCode"].ToString(),
                        AllOpSeqOfSeriesSUB.Rows[i]["OpDesc"].ToString(),
                        AllOpSeqOfSeriesSUB.Rows[i]["CommentText"].ToString(),
                        CommonInfo.Plant,
                        CommonInfo.Company,
                        HttpContext.Current.Session["UserId"].ToString(),
                        0,
                        sd.UnQualifiedReason,
                        PackSlip,
                        0,
                        0,
                        0,
                        0,
                        sd.DisQty,
                        sd.Responsibility,
                        sd.Type
                    });
                sql = string.Format(sql, values);
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
            }


            string rcvdtlStr = "[";
            for (int i = AllOpSeqOfSeriesSUB.Rows.Count - 1; i < AllOpSeqOfSeriesSUB.Rows.Count; i++)
            {
                rcvdtlStr += ReceiptRepository.ConstructRcvdtlStr(
                    new String[] {
                                CommonRepository.GetValueAsString(sd.PoNum),
                                CommonRepository.GetValueAsString((int)AllOpSeqOfSeriesSUB.Rows[i]["poline"]),
                                CommonRepository.GetValueAsString((int)AllOpSeqOfSeriesSUB.Rows[i]["porelnum"]),
                                CommonRepository.GetValueAsString(AllOpSeqOfSeriesSUB.Rows[i]["PartNum"].ToString()),
                                CommonRepository.GetValueAsString(sd.DisQty),
                                CommonRepository.GetValueAsString(AllOpSeqOfSeriesSUB.Rows[i]["IUM"].ToString()),
                                CommonRepository.GetValueAsString("待检区"),
                                CommonRepository.GetValueAsString("ins"),
                                CommonRepository.GetValueAsString(sd.JobNum),
                                CommonRepository.GetValueAsString(sd.JobNum),
                                CommonRepository.GetValueAsString(sd.AssemblySeq),
                                CommonRepository.GetValueAsString((int)AllOpSeqOfSeriesSUB.Rows[i]["jobseq"]),
                                CommonRepository.GetValueAsString(AllOpSeqOfSeriesSUB.Rows[i]["CommentText"].ToString().Replace('\'','"')),
                                CommonRepository.GetValueAsString("PUR-SUB"),
                                CommonRepository.GetValueAsString("")}) + (i == AllOpSeqOfSeriesSUB.Rows.Count - 1 ? "]" : ",");
            }

            string res = "";
            if ((res = ErpAPI.ReceiptRepository.porcv(PackSlip, recdate, CommonInfo.SupplierNo, rcvdtlStr, "", CommonInfo.Company, true)) != "1|处理成功.")//若回写erp成功， 则更新对应的Receipt记录
            {
                AddOpLog(sd.JobNum, (int)sd.AssemblySeq, 101, res, 0, 0, (int)sd.PoNum);
                return res;
            }

            AddOpLog(sd.JobNum, (int)sd.AssemblySeq, 101, "外协不良品流程发起成功", 0, 0, (int)sd.PoNum);
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



            string res, type = (int)theSubcontractDis.TranID != 0 ? "外协不良1" : "外协不良2";
            if (theSubcontractDis.DMRID == 0)
            {
                int DMRID = 0, TranID = 0;
                if (theSubcontractDis.Type == 1) //我方不良，以不合格品发起
                {

                    res = ErpAPI.CommonRepository.Startnonconf(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, theSubcontractDis.Company, (int)theSubcontractDis.JobSeq, (decimal)theSubcontractDis.DisQty, sd.DMRWarehouseCode, sd.DMRBinNum, theSubcontractDis.UnQualifiedReason, theSubcontractDis.Plant, "", type, out TranID);
                    if (res.Substring(0, 1).Trim() != "1")
                    {
                        AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, 201, "不合格品发起失败：" + res, theSubcontractDis.M_ID, 0, (int)theSubcontractDis.PoNum);
                        return "错误：" + res;
                    }

                    theSubcontractDis.TranID = TranID;
                    AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, 201, "不合格品发起成功，TranID = " + TranID, theSubcontractDis.M_ID, 0, (int)theSubcontractDis.PoNum);
                }


                res = ErpAPI.CommonRepository.StartInspProcessing((int)theSubcontractDis.TranID, 0, (decimal)theSubcontractDis.DisQty, "D22", "BLPC", "01", type, theSubcontractDis.Plant, theSubcontractDis.PackSlip,(int)theSubcontractDis.PoLine, out DMRID); //产品其它不良 D22  D
                if (res.Substring(0, 1).Trim() != "1")
                {
                    AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, 201, "检验处理失败：" + res, theSubcontractDis.M_ID, 0, (int)theSubcontractDis.PoNum);
                    return "错误：" + res;
                }

                theSubcontractDis.DMRID = DMRID;
                AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, 201, "检验处理成功，DMRID = " + DMRID, theSubcontractDis.M_ID, 0, (int)theSubcontractDis.PoNum);


                sql = @"update SubcontractDisMain set tranid = " + TranID + ", Dmrid = " + DMRID + "  where m_id = " + sd.M_ID + "";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
            }

            if (sd.DMRRepairQty > 0)
            {
                res = ErpAPI.CommonRepository.RepairDMRProcessing((int)theSubcontractDis.DMRID, theSubcontractDis.Company, theSubcontractDis.Plant, theSubcontractDis.PartNum, (decimal)sd.DMRRepairQty, sd.DMRJobNum, theSubcontractDis.IUM);
                if (res.Substring(0, 1).Trim() != "1")
                {
                    AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, 201, "DMR返修失败：" + res, theSubcontractDis.M_ID, 0, (int)theSubcontractDis.PoNum);
                    return "错误：" + res + ". 请重新提交返修数量、报废数量";
                }

                InsertRepairRecord(sd.M_ID, (decimal)sd.DMRRepairQty, sd.DMRJobNum, (int)theSubcontractDis.DMRID, sd.TransferUserGroup, sd.DMRWarehouseCode, sd.DMRBinNum, sd.DMRUnQualifiedReason);


                sql = " update SubcontractDisMain set Responsibility = '"+sd.Responsibility+"', ExistSubProcess = 1, checkcounter = checkcounter - " + sd.DMRRepairQty + ",TotalDMRRepairQty = ISNULL(TotalDMRRepairQty,0) + " + sd.DMRRepairQty + "  where m_id = " + (sd.M_ID) + "";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, 201, "返修子流程生成", theSubcontractDis.M_ID, 0, (int)theSubcontractDis.PoNum);
            }

            if (sd.DMRUnQualifiedQty > 0)
            {

                res = ErpAPI.CommonRepository.RefuseDMRProcessing(theSubcontractDis.Company, theSubcontractDis.Plant, (decimal)sd.DMRUnQualifiedQty, sd.DMRUnQualifiedReason, (int)theSubcontractDis.DMRID, theSubcontractDis.IUM);
                if (res.Substring(0, 1).Trim() != "1")
                {
                    AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, 201, "DMR拒收失败：" + res, theSubcontractDis.M_ID, 0, (int)theSubcontractDis.PoNum);
                    return "错误：" + res + ". 请重新提交报废数量";
                }

                InsertDiscardRecord(theSubcontractDis.M_ID, (decimal)sd.DMRUnQualifiedQty, sd.DMRUnQualifiedReason, (int)theSubcontractDis.DMRID, sd.DMRWarehouseCode, sd.DMRBinNum, sd.TransferUserGroup);

                sql = " update SubcontractDisMain set ExistSubProcess = 1,checkcounter = checkcounter - " + sd.DMRUnQualifiedQty + ", TotalDMRUnQualifiedQty = ISNULL(totalDMRUnQualifiedQty,0) + " + sd.DMRUnQualifiedQty + "  where m_id = " + (sd.M_ID) + "";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, 201, "报废子流程生成", theSubcontractDis.M_ID, 0, (int)theSubcontractDis.PoNum);
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

                    sql = @"update SubcontractDisSub set atrole = " + theSubcontractDis.AtRole + ", NodeNum = 4, TransferDate = getdate(), TransferUserID = '" + HttpContext.Current.Session["UserId"].ToString() + "', AccepterUserGroup = '"+sd.AccepterUserGroup+"'  where s_id = " + theSubcontractDis.S_ID + "";
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                    AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, 301, "返修流程提交成功|" + sql, 0, (int)theSubcontractDis.S_ID, (int)theSubcontractDis.PoNum);
                }

                else if (theSubcontractDis.DMRUnQualifiedQty > 0)
                {
                    sql = @"update SubcontractDisSub set IsComplete = 1,NodeNum = 99, TransferDate = getdate(), TransferUserID = '" + HttpContext.Current.Session["UserId"].ToString() + "' where s_id = " + theSubcontractDis.S_ID + "";
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                    AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, 301, "报废流程完结", 0, (int)theSubcontractDis.S_ID, (int)theSubcontractDis.PoNum);
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


                sql = @"update SubcontractDisSub set IsComplete = 1,NodeNum = 99, AccepterDate = getdate(), AccepterUserID = '" + HttpContext.Current.Session["UserId"].ToString() + "' where s_id = " + theSubcontractDis.S_ID + "";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, 401, "返修流程完结", 0, (int)theSubcontractDis.S_ID, (int)theSubcontractDis.PoNum);

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


        private static void AddOpLog(string JobNum, int AssemblySeq, int ApiNum, string OpDetail, int M_ID, int S_ID, int PONum)
        {
            string sql = @"insert into SubcontractDisLog(JobNum, AssemblySeq, UserId, Opdate, ApiNum, OpDetail,M_ID,S_ID, PONum) Values('{0}', {1}, '{2}', {3}, {4},  @OpDetail,{5},{6},{7}) ";
            sql = string.Format(sql, JobNum, AssemblySeq, HttpContext.Current.Session["UserId"].ToString(), "getdate()", ApiNum, M_ID, S_ID, PONum);

            SqlParameter[] ps = new SqlParameter[] { new SqlParameter("@OpDetail", OpDetail) };
            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, ps);

        }//添加操作记录
    }
}