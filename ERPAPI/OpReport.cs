using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using Newtonsoft.Json;
using System.Configuration;


using Ice.Core;
using Erp.Proxy.BO;
using Erp.BO;
using Epicor.ServiceModel.Channels;
using Ice.Tablesets;
using System.Web;

namespace ErpAPI
{
    public static class OpReport
    {
        public static string D0505(string empid, string JobNum, int asmSeq, int oprSeq, decimal LQty, decimal disQty, string disCode, string bjr, DateTime StartDate, DateTime EndDate, string companyId,string plantId, decimal labh, out string Character05, out int tranid)
        { //JobNum as string ,jobQty as decimal,partNum as string
            Character05 = "";
            tranid = -1;
            try
            {
                DataTable dt = Common.GetDataByERP(@"select [JobOper].[JobNum] as [JobOper_JobNum],
                [JobOper].[AssemblySeq] as [JobOper_AssemblySeq],
                [JobOper].[OprSeq] as [JobOper_OprSeq],
                [JobOper].[RunQty] as [JobOper_RunQty],
                [JobOper].[QtyCompleted] as [JobOper_QtyCompleted],
                [JobHead].[JobComplete] as [JobHead_JobComplete],
                [JobHead].[JobReleased] as [JobHead_JobReleased],[OpMaster].[Character05] as [OpMaster_Character05] ,
                (case when JobAsmbl.AssemblySeq=0 then JobHead.UDReqQty_c else JobAsmbl.SurplusQty_c end) as [Calculated_prodqty] 
                from Erp.JobOper as JobOper left outer join JobHead as JobHead on JobOper.Company = JobHead.Company and JobOper.JobNum = JobHead.JobNum 
                inner join JobAsmbl as JobAsmbl on 
	                JobOper.Company = JobAsmbl.Company
	                and JobOper.JobNum = JobAsmbl.JobNum
	                and JobOper.AssemblySeq = JobAsmbl.AssemblySeq 
                inner join OpMaster as OpMaster on JobOper.Company = OpMaster.Company and JobOper.OpCode = OpMaster.OpCode where (JobOper.Company = '" + companyId + "'  and JobOper.JobNum = '" + JobNum + "'  and JobOper.AssemblySeq = '" + asmSeq.ToString() + "'  and JobOper.OprSeq ='" + oprSeq.ToString() + "') order by JobOper.OprSeq Desc");


                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    dt.Columns[i].ColumnName = dt.Columns[i].ColumnName.Replace('_', '.');
                }
                decimal compQty = 0, runQty = 0;
                bool jobRes = false, jobCom = true;
                if (dt.Rows.Count > 0)
                {
                    decimal.TryParse(dt.Rows[0]["Calculated.prodqty"].ToString().Trim(), out runQty);
                    //decimal.TryParse(dt.Rows[0]["JobOper.RunQty"].ToString().Trim(), out runQty);
                    decimal.TryParse(dt.Rows[0]["JobOper.QtyCompleted"].ToString().Trim(), out compQty);
                    bool.TryParse(dt.Rows[0]["JobHead.JobComplete"].ToString().Trim(), out jobCom);
                    bool.TryParse(dt.Rows[0]["JobHead.JobReleased"].ToString().Trim(), out jobRes);
                    Character05 = empid = dt.Rows[0]["OpMaster.Character05"].ToString().Trim();
                }
                if (jobCom)
                {

                    return "0|工单已关闭，不能报工。";
                }
                if (jobRes == false)
                {
                    // EpicorSessionManager.DisposeSession();
                    return "0|工单未发放，不能报工。";
                }
                if ((compQty + LQty + disQty) > runQty)
                {
                    // EpicorSessionManager.DisposeSession();
                    return "0|以前报工数量" + compQty + "+ 本次报工数量" + (LQty + disQty) + "  >  工序数量" + runQty + "，不能报工。";
                }
                if (empid.Trim() == "") { Character05=empid = "DB"; }
      
                
                Session EpicorSession = Common.GetEpicorSession();
                if (EpicorSession == null)
                {
                    return "0|erp用户数不够，请稍候再试.接口号：D0505";
                }
                EpicorSession.CompanyID = companyId;
                EpicorSession.PlantID = plantId;
                WriteGetNewLaborInERPTxt("", EpicorSession.SessionID.ToString(), "", "sessionidd", "");
                LaborImpl labAd = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<LaborImpl>(EpicorSession, ImplBase<Erp.Contracts.LaborSvcContract>.UriPath);
                LaborDataSet dsLabHed = new LaborDataSet();
                LaborDataSet.LaborHedDataTable dtLabHed = new LaborDataSet.LaborHedDataTable();
                LaborDataSet.LaborDtlDataTable dtLabDtl = new LaborDataSet.LaborDtlDataTable();
                //Dim fbg, runqty, outTime As Decimal
                decimal outTime;
                int labHedSeq;

                labHedSeq = 0;
                if (labHedSeq == 0)
                {
                    labAd.GetNewLaborHed1(dsLabHed, empid, false, System.DateTime.Today);
                    dtLabHed = dsLabHed.LaborHed;
                    labAd.Update(dsLabHed);
                    labHedSeq = Convert.ToInt32(dtLabHed.Rows[0]["LaborHedSeq"]);
                }
                {
                    dsLabHed = labAd.GetByID(labHedSeq);
                }
                outTime  = EndDate.Hour;// + System.DateTime.Now.Minute / 100;
                labAd.GetNewLaborDtlWithHdr(dsLabHed, System.DateTime.Today, 0, System.DateTime.Today, outTime, labHedSeq);
                 
                labAd.DefaultLaborType(dsLabHed, "P");
                labAd.DefaultJobNum(dsLabHed, JobNum);
               /// labAd.defaultjo
                labAd.DefaultAssemblySeq(dsLabHed, asmSeq);
                string msg;
                labAd.DefaultOprSeq(dsLabHed, oprSeq, out msg);

                dtLabDtl = dsLabHed.LaborDtl;

                if(LQty > 0 )
                    labAd.DefaultLaborQty(dsLabHed, LQty, out msg);

                //dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["LaborQty"] = LQty;
                //disQty = disQty;  //先不回写不合格数量
                //disCode = disCode;

                TimeSpan timeSpan = EndDate - StartDate;

                dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["DiscrepQty"] = disQty;
                dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["DiscrpRsnCode"] = disQty > 0 ? disCode : "";
                dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["TimeStatus"] = "A"; 
                dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["ClockinTime"] = Convert.ToDecimal(StartDate.TimeOfDay.TotalHours.ToString("N2"));
                dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["ClockOutTime"] = timeSpan.TotalMinutes < 1 ? Convert.ToDecimal(EndDate.AddMinutes(1).TimeOfDay.TotalHours.ToString("N2")) : Convert.ToDecimal(EndDate.TimeOfDay.TotalHours.ToString("N2"));
                //dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["LaborHrs"] = labh;
                dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["Date01"] = System.DateTime.Today;
                dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["ShortChar01"] = System.DateTime.Now.ToString("hh:mm:ss");

               
                //dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["OpComplete"] = "1";
                //dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["Complete"] = "1";
                labAd.DefaultDtlTime(dsLabHed);
                string cMessageText = "";
                try
                {
                    labAd.CheckWarnings(dsLabHed, out cMessageText);
                    labAd.Update(dsLabHed);
                    string LaborDtlSeq = dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["LaborDtlSeq"].ToString();
             
                    if (disQty > 0)
                        tranid = int.Parse(Common.QueryERP("select tranid from erp.NonConf where LaborDtlSeq = " + LaborDtlSeq + " "));
                }
                catch (Exception ex)
                {
                    EpicorSession.Dispose();
                    return "0|" + ex.Message.ToString();
                }
                //adapter.ValidateChargeRateForTimeType(ds, out oumsg);
                try
                {
                    labAd.SubmitForApproval(dsLabHed, false, out cMessageText);
                }
                catch (Exception ex)
                {
                    EpicorSession.Dispose();
                    return "0|" + ex.Message.ToString();
                }
                try
                {
                    EpicorSession.Dispose();
                }
                catch
                { }
                return "1|处理成功";
            }
            catch (Exception ex)
            {
                return "0|" + ex.Message.ToString();
            }
        }


        private static void WriteGetNewLaborInERPTxt(string employeenum, string laborHedStr, string laborDetailKeyAndValArrayStr, string type, string company)
        {
            string strSql = "";
            string key1 = System.Guid.NewGuid().ToString();
            strSql = "INSERT INTO ICE.UD30(Company,Key1,Character01,Character02,Character03,Date01,ShortChar01)";
            strSql += "Values('" + company + "','" + key1 + "','" + employeenum + "','" + laborHedStr + "|" + laborDetailKeyAndValArrayStr + "','" + type + "','" + DateTime.Now + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "')";
            int rowsCount = Common.ExecuteSql(strSql);
        }

    }
}
