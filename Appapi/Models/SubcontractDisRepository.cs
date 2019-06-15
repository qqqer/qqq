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
                AddOpLog(sd.JobNum, (int)sd.AssemblySeq, 101, res, 0, 0 , (int)sd.PoNum);
                return res;
            }

            AddOpLog(sd.JobNum, (int)sd.AssemblySeq, 101, "外协不良品流程发起成功", 0, 0, (int)sd.PoNum);
            return "处理成功";
        }

        public static IEnumerable<OpReport> GetDMRRemainsOfUser()
        {
            if (((int)HttpContext.Current.Session["RoleId"] & 1024) != 0)
            {
                string sql = @"select * from SubcontractDisMain where CHARINDEX(company, '{0}') > 0   and   CHARINDEX(Plant, '{1}') > 0 and checkcounter > 0  and isdelete != 1 order by CreateDate desc";
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

        private static void AddOpLog(string JobNum, int AssemblySeq, int ApiNum,  string OpDetail, int M_ID, int S_ID , int PONum)
        {
            string sql = @"insert into SubcontractDisLog(JobNum, AssemblySeq, UserId, Opdate, ApiNum, OpDetail,M_ID,S_ID, PONum) Values('{0}', {1}, '{2}', {3}, {4},  @OpDetail,{5},{6},{7}) ";
            sql = string.Format(sql, JobNum, AssemblySeq, HttpContext.Current.Session["UserId"].ToString(), "getdate()", ApiNum, OpDetail, M_ID, S_ID, PONum);

            SqlParameter[] ps = new SqlParameter[] { new SqlParameter("@OpDetail", OpDetail) };
            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, ps);

        }//添加操作记录
    }
}