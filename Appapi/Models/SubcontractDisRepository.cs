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
        private static DataTable GetAllOpSeqOfSeriesSUB(SubcontractDis sd) //取出该订单中的连续委外工序（包括当前处理的批次工序）的工序号、poline、porelnum、工序描述、工序代码
        {
            string sql = @" Select jobseq, jo.PartNum, jo.IUM, jo.Description,  pr.poline, porelnum ,OpDesc,OpCode,pd.CommentText 
                            from erp.porel pr 
                            left join erp.PODetail pd   on pr.PONum = pd.PONUM   and   pr.Company = pd.Company   and   pr.POLine = pd.POLine 
                            left join erp.JobOper jo on pr.jobnum = jo.JobNum and pr.AssemblySeq = jo.AssemblySeq and pr.Company = jo.Company and jobseq = jo.OprSeq 
                            where pr.ponum={0} and pr.jobnum = '{1}'  and pr.assemblyseq={2} and trantype='PUR-SUB' and pr.company = '{3}' order by jobseq  asc";
            sql = string.Format(sql, sd.PoNum, sd.JobNum, sd.AssemblySeq, "001");
            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);
            return dt;
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
                vd.Name as SupplierName,
               
                from erp.PORel pr
                left join erp.PODetail pd   on pr.PONum = pd.PONUM   and   pr.Company = pd.Company   and   pr.POLine = pd.POLine 
                left join erp.POHeader ph   on ph.Company = pd.Company   and   ph.PONum = pd.PONUM                 
                left join erp.Vendor vd     on ph.VendorNum = vd.VendorNum   and   ph.company = vd.company                     
                where pr.ponum = {0} and pr.poline ={1} and pr.PORelNum = {2} and pr.Company = '001'";

            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);
            SubcontractDis commoninfo = CommonRepository.DataTableToList<SubcontractDis>
                (Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

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
            if (((long)HttpContext.Current.Session["RoleID"] & 2048) == 0)
                return "0|错误：该账号没有权限发起外协不良";

            DataTable AllOpSeqOfSeriesSUB = GetAllOpSeqOfSeriesSUB(sd);
            SubcontractDis CommonInfo = GetCommonInfo((int)sd.PoNum,
                (int)AllOpSeqOfSeriesSUB.Rows[0]["poline"], (int)AllOpSeqOfSeriesSUB.Rows[0]["porelnum"]);

            string PackSlip = CommonInfo.SupplierNo + "D" + sd.PoNum + new Random().Next() % 100000000;
            string recdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string sql = "";

            for (int i = 0; i < AllOpSeqOfSeriesSUB.Rows.Count; i++)
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
                               ,[Type]) values{0}";
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
                        2
                    });
                sql = string.Format(sql, values);
            }


            string rcvdtlStr = "[";
            for (int i = 0; i < AllOpSeqOfSeriesSUB.Rows.Count; i++)
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
                                CommonRepository.GetValueAsString("")});
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

            string sql = @"select * from SubcontractDisMain where  = " + sd.M_ID + "";

            SubcontractDis theSubcontractDis = CommonRepository.DataTableToList<SubcontractDis>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

            if (theSubcontractDis.M_IsDelete == true)
                return "错误：所属的主流程已删除";
            if (theSubcontractDis.CheckCounter == 0)
                return "错误：该报工流程下的所有不良品已处理完毕";
            if (theSubcontractDis.ThirdUserGroup == "")
                return "错误：下步接收人不能为空";


            sd.DMRQualifiedQty = Convert.ToDecimal(sd.DMRQualifiedQty);
            sd.DMRRepairQty = Convert.ToDecimal(sd.DMRRepairQty);
            sd.DMRUnQualifiedQty = Convert.ToDecimal(sd.DMRUnQualifiedQty);


            decimal determinedQty = Convert.ToDecimal(theSubcontractDis.DMRQualifiedQty) + Convert.ToDecimal(theSubcontractDis.DMRRepairQty) + Convert.ToDecimal(theSubcontractDis.DMRUnQualifiedQty);

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

           

            string res, type = (int)theSubcontractDis.TranID == 0 ? "外协不良1" : "外协不良2";
            if (theSubcontractDis.DMRID == 0)
            {
                int DMRID = 0;

                if (theSubcontractDis.Type == 1)
                {
                    int TranID = 0;
                    res = ErpAPI.CommonRepository.Startnonconf(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, theSubcontractDis.Company, (int)theSubcontractDis.JobSeq, (decimal)theSubcontractDis.DisQty, sd.DMRWarehouseCode, sd.DMRBinNum, sd.UnQualifiedReason, theSubcontractDis.Plant, theSubcontractDis.LotNum, out TranID);
                    if (res.Substring(0, 1).Trim() != "1")
                    {
                        AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, 201, "不合格品发起失败：" + res, theSubcontractDis.M_ID, 0, (int)theSubcontractDis.PoNum);
                        return "错误：" + res;
                    }

                    theSubcontractDis.TranID = TranID;
                    AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, 201, "不合格品发起成功，TranID = " + TranID, theSubcontractDis.M_ID, 0, (int)theSubcontractDis.PoNum);
                }

                res = ErpAPI.CommonRepository.StartInspProcessing((int)theSubcontractDis.TranID, 0, (decimal)theSubcontractDis.DisQty, "D22", "BLPC", "01", type, theSubcontractDis.Plant, out DMRID); //产品其它不良 D22  D
                if (res.Substring(0, 1).Trim() != "1")
                {
                    AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, 201, "检验处理失败：" + res, theSubcontractDis.M_ID, 0, (int)theSubcontractDis.PoNum);
                    return "错误：" + res;
                }

                theSubcontractDis.DMRID = DMRID;
                AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, 201, "检验处理成功，DMRID = " + DMRID, theSubcontractDis.M_ID, 0, (int)theSubcontractDis.PoNum);            
            }


            if (sd.DMRQualifiedQty > 0)
            {
                res = ErpAPI.CommonRepository.ConcessionDMRProcessing((int)theSubcontractDis.DMRID, theSubcontractDis.Company, theSubcontractDis.Plant, theSubcontractDis.PartNum, (int)theSubcontractDis.AssemblySeq, (int)theSubcontractDis.JobSeq, (decimal)sd.DMRQualifiedQty, theSubcontractDis.JobNum, type);
                if (res.Substring(0, 1).Trim() != "1")
                    return "错误：" + res + ". 请重新提交让步数量、返修数量、报废数量";

                InsertConcessionRecord((int)theSubcontractDis.M_ID, (decimal)sd.DMRQualifiedQty, sd.ThirdUserGroup, (int)theSubcontractDis.DMRID, sd.DMRWarehouseCode, sd.DMRBinNum, sd.DMRUnQualifiedReason, sd.Responsibility);

                sql = " update bpm set checkcounter = checkcounter - " + sd.DMRQualifiedQty + ",DMRQualifiedQty = ISNULL(DMRQualifiedQty,0) + " + sd.DMRQualifiedQty + "  where id = " + (sd.ID) + "";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, 201, "让步子流程生成", theSubcontractDis.M_ID, 0, (int)theSubcontractDis.PoNum);
            }

            if (sd.DMRRepairQty > 0)
            {
                res = ErpAPI.CommonRepository.RepairDMRProcessing((int)theSubcontractDis.DMRID, theSubcontractDis.Company, theSubcontractDis.Plant, theSubcontractDis.PartNum, (decimal)sd.DMRRepairQty, sd.DMRJobNum, IUM.ToString());
                if (res.Substring(0, 1).Trim() != "1")
                    return "错误：" + res + ". 请重新提交返修数量、报废数量";

                InsertRepairRecord((int)sd.ID, (decimal)sd.DMRRepairQty, sd.DMRJobNum, (int)theSubcontractDis.DMRID, sd.TransformUserGroup, sd.DMRWarehouseCode, sd.DMRBinNum, sd.DMRUnQualifiedReason, sd.Responsibility);


                sql = " update bpm set checkcounter = checkcounter - " + sd.DMRRepairQty + ",DMRRepairQty = ISNULL(DMRRepairQty,0) + " + sd.DMRRepairQty + "  where id = " + (sd.ID) + "";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, 201, "返修子流程生成", theSubcontractDis.M_ID, 0, (int)theSubcontractDis.PoNum);
            }

            if (sd.DMRUnQualifiedQty > 0)
            {
               
                res = ErpAPI.CommonRepository.RefuseDMRProcessing(theSubcontractDis.Company, theSubcontractDis.Plant, (decimal)sd.DMRUnQualifiedQty, sd.DMRUnQualifiedReason, (int)theSubcontractDis.DMRID, IUM.ToString());
                if (res.Substring(0, 1).Trim() != "1")
                    return "错误：" + res + ". 请重新提交报废数量";

                InsertDiscardRecord((int)sd.ID, (decimal)sd.DMRUnQualifiedQty, sd.DMRUnQualifiedReason, (int)theSubcontractDis.DMRID, sd.DMRWarehouseCode, sd.DMRBinNum, sd.TransformUserGroup, sd.Responsibility);

                sql = " update bpm set checkcounter = checkcounter - " + sd.DMRUnQualifiedQty + ",DMRUnQualifiedQty = ISNULL(DMRUnQualifiedQty,0) + " + sd.DMRUnQualifiedQty + "  where id = " + (sd.ID) + "";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, 201, "报废子流程生成", theSubcontractDis.M_ID, 0, (int)theSubcontractDis.PoNum);
            }

            return "处理成功";
        }

        private static void InsertConcessionRecord(int m_ID, decimal dMRQualifiedQty, string thirdUserGroup, int dMRID, string dMRWarehouseCode, string dMRBinNum, string dMRUnQualifiedReason, string responsibility)
        {
            throw new NotImplementedException();
        }

        public static string AccepterCommitOfSub(SubcontractDis sd)//apinum 301
        {
            try
            {
                string sql = @"select * from SubcontractDisMain sdm left join SubcontractDisSub sds on sdm.M_ID = sds.S_ID where sds.S_ID = " + sd.S_ID + "";
                SubcontractDis theSubcontractDis = CommonRepository.DataTableToList<SubcontractDis>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

                if (theSubcontractDis.S_IsDelete == true)
                    return "错误：该流程已删除";

                if (theSubcontractDis.IsComplete == true)
                    return "错误：该流程已结束";

                sql = @"update SubcontractDisSub set IsComplete = 1, CompleteDate = getdate(), ThirdUserID = '" + HttpContext.Current.Session["UserId"].ToString() + "' where s_id = "+ theSubcontractDis.S_ID +"";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, 301, "流程成功完结", 0, (int)theSubcontractDis.S_ID, (int)theSubcontractDis.PoNum);

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
                string sql = @"select * from SubcontractDisMain where CHARINDEX(company, '{0}') > 0   and   CHARINDEX(Plant, '{1}') > 0 and checkcounter > 0  and isdelete != 1 order by CreateDate desc";
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


            else return null;
        }


        public static DataTable GetNextUserGroup(int s_id)
        {
            string sql = @"select * from SubcontractDisSub where  = " + s_id + "";
            SubcontractDis theSubcontractDis = CommonRepository.DataTableToList<SubcontractDis>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

            sql = "select * from userfile where CHARINDEX('" + theSubcontractDis.Company + "', company) > 0 and CHARINDEX('" + theSubcontractDis.Plant + "', plant) > 0 and disabled = 0 and RoleID & 2048 != 0 and RoleID != 2147483647";
            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表

            return dt;
        }


        private static void AddOpLog(string JobNum, int AssemblySeq, int ApiNum, string OpDetail, int M_ID, int S_ID, int PONum)
        {
            string sql = @"insert into SubcontractDisLog(JobNum, AssemblySeq, UserId, Opdate, ApiNum, OpDetail,M_ID,S_ID, PONum) Values('{0}', {1}, '{2}', {3}, {4},  @OpDetail,{5},{6},{7}) ";
            sql = string.Format(sql, JobNum, AssemblySeq, HttpContext.Current.Session["UserId"].ToString(), "getdate()", ApiNum, OpDetail, M_ID, S_ID, PONum);

            SqlParameter[] ps = new SqlParameter[] { new SqlParameter("@OpDetail", OpDetail) };
            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, ps);

        }//添加操作记录
    }
}