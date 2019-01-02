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
        public static string D0505(string empid, string JobNum, int asmSeq, int oprSeq, decimal LQty, decimal disQty, string disCode, string bjr, DateTime StartDate, DateTime EndDate, string companyId, out string Character05, out int tranid)
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
                    return "0|以前报工数量" + compQty + "+ 本次报工数量" + (LQty + disQty) + ",>工序数量" + runQty + "，不能报工。";
                }
                if (empid.Trim() == "") { Character05=empid = "DB"; }
      
                
                Session EpicorSession = Common.GetEpicorSession();
                if (EpicorSession == null)
                {
                    return "0|erp用户数不够，请稍候再试.接口号：D0505";
                }
                EpicorSession.CompanyID = companyId;
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
                labAd.GetNewLaborDtlWithHdr(dsLabHed, StartDate.Date, 0, EndDate.Date, outTime, labHedSeq);

                labAd.DefaultLaborType(dsLabHed, "P");
                labAd.DefaultJobNum(dsLabHed, JobNum);
                labAd.DefaultAssemblySeq(dsLabHed, asmSeq);
                string msg;
                labAd.DefaultOprSeq(dsLabHed, oprSeq, out msg);
                dtLabDtl = dsLabHed.LaborDtl;
                labAd.DefaultLaborQty(dsLabHed, LQty, out msg);
                //dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["LaborQty"] = LQty;
                //disQty = disQty;  //先不回写不合格数量
                //disCode = disCode;
                dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["DiscrepQty"] = disQty;
                dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["DiscrpRsnCode"] = disCode;
                dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["TimeStatus"] = "A";
                dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["ClockinTime"] = Convert.ToDecimal(StartDate.TimeOfDay.TotalHours.ToString("N2"));
                dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["ClockOutTime"] = Convert.ToDecimal(EndDate.TimeOfDay.TotalHours.ToString("N2"));
                //dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["LaborHrs"] = EndDate - StartDate;
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


        //不合格品发起
        public static string Startnonconf(
                string JobNum ,
                int AssemblySeq,
                string Company,
                int OperSeq,
                decimal Qty,//返修数(DMRRepairQty)+拒收数(DMRUnQualifiedQty)
                string WarehouseCode,
                string BinNum ,
                string ReasonCode,
                string plant,
                out int tranid)
        {

            tranid = -1;
            Session EpicorSession = Common.GetEpicorSession();
            if (EpicorSession == null)
            {
                return "0|GetEpicorSession失败，请稍候再试|Startnonconf";
            }

            try
            {
                bool plAsmReturned = true;
                string snWarning = "";

                NonConfImpl adapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<NonConfImpl>(EpicorSession, ImplBase<Erp.Contracts.NonConfSvcContract>.UriPath);
                //EpicorSessionManager.EpicorSession.CompanyID = Company;
                //EpicorSessionManager.EpicorSession.PlantID = plant;

                //新增工序
                NonConfDataSet ds = adapter.AddNonConf("Operation");
                //加入工单
                adapter.OnChangeJobNum(JobNum, out plAsmReturned, out snWarning, ds);
                //选择阶层
                ds.Tables["NonConf"].Rows[0]["JobNum"] = JobNum;
                ds.Tables["NonConf"].Rows[0]["AssemblySeq"] = AssemblySeq;
                adapter.OnChangeJobAsm(AssemblySeq, ds);
                //选择工序
                adapter.OnChangeJobOpr(OperSeq, true, ds);
                ds.Tables["NonConf"].Rows[0]["FromOprSeq"] = OperSeq;
                //填数量
                adapter.OnChangeTranQty(Qty, ds);
                ds.Tables["NonConf"].Rows[0]["Quantity"] = Qty;
                //选仓库
                adapter.OnChangeWarehouseCode(WarehouseCode, false, ds);
                ds.Tables["NonConf"].Rows[0]["ToWarehouseCode"] = WarehouseCode;
                ds.Tables["NonConf"].Rows[0]["WarehouseCode"] = "WIP";
                //填库位
                adapter.OnChangeBinNum(BinNum, false, ds);
                ds.Tables["NonConf"].Rows[0]["ToBinNum"] = BinNum;
                ds.Tables["NonConf"].Rows[0]["BinNum"] = "01";
                //原因
                ds.Tables["NonConf"].Rows[0]["ReasonCode"] = ReasonCode;
                //保存
                adapter.Update(ds);
                tranid = int.Parse( ds.Tables["NonConf"].Rows[0]["TranID"].ToString());

                return "1";
            }
            catch(Exception  ex)
            {
                return "0|" + ex.Message;
            }
        }


        //检查处理
        public static string StartInspProcessing(
            int TranID,
            decimal DMRQualifiedQty,
            decimal UnQualifiedQty ,//返修数(DMRRepairQty)+拒收数(DMRUnQualifiedQty)
            string FailedReasonCode,
            string FailedWarehouseCode ,
            string FailedBin ,
            out int mdiid)
        {
            mdiid = -1;
            Session EpicorSession = Common.GetEpicorSession();
            if (EpicorSession == null)
            {
                return "0|GetEpicorSession失败，请稍候再试|StartInspProcessing";
            }

            try
            {
                string infoMsg = "";
                string legalNumberMessage = "";
                int iDMRNum = 0;
                string InspectorID = "Q01";

                InspProcessingImpl adapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<InspProcessingImpl>(EpicorSession, ImplBase<Erp.Contracts.InspProcessingSvcContract>.UriPath);
                InspProcessingDataSet ds = adapter.GetByID(TranID);
                //检验员
                adapter.AssignInspectorNonConf(InspectorID, ds, out infoMsg);
                //合格数
                ds.Tables["InspNonConf"].Rows[0]["DimPassedQty"] = DMRQualifiedQty;
                adapter.OnChangePassedQty(ds, "NonConf", DMRQualifiedQty, out infoMsg);
                if (UnQualifiedQty > 0)
                {
                    //不合格数
                    ds.Tables["InspNonConf"].Rows[0]["DimFailedQty"] = UnQualifiedQty;
                    adapter.OnChangeFailedQty(ds, "NonConf", UnQualifiedQty, out infoMsg);
                    //不合格原因
                    ds.Tables["InspNonConf"].Rows[0]["FailedReasonCode"] = FailedReasonCode;
                    //不合格仓库
                    adapter.OnChangePassedWhse(ds, "NonConf", "Failed", FailedWarehouseCode);
                    //不合格库位
                    ds.Tables["InspNonConf"].Rows[0]["FailedBin"] = FailedBin;
                    ds.Tables["InspNonConf"].Rows[0]["RowMod"] = "U";
                }

                //保存
                adapter.InspectOperation(out legalNumberMessage, out iDMRNum, ds);

                mdiid = iDMRNum;

                return "1";
            }
            catch(Exception ex)
            {
                return "0|" + ex.Message;
            }
        }


        //返修
        public static string RepairDMRProcessing(
            int DMRID,
        string Company ,
        string plant ,
        string PartNum,
        decimal DMRRepairQty ,//返修         
        string DMRJobNum)
        {
            Session EpicorSession = Common.GetEpicorSession();
            if (EpicorSession == null)
            {
                return "0|GetEpicorSession失败，请稍候再试|RepairDMRProcessing";
            }

            try
            {
                int AssemblySeq = 0;
                bool multipleMatch = false;
                bool vSubAvail = false;
                string vMsgText = "";
                string vMsgType = "";
                bool opPartChgCompleted = false;
                string opMtlIssuedAction = "";
                Guid ss = new Guid();
                DateTime time = DateTime.Now;
                string opLegalNumberMessage = "";

                DMRProcessingImpl adapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<DMRProcessingImpl>(EpicorSession, ImplBase<Erp.Contracts.DMRProcessingSvcContract>.UriPath);
                JobEntryImpl adapter1 = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<JobEntryImpl>(EpicorSession, ImplBase<Erp.Contracts.JobEntrySvcContract>.UriPath);
                //EpicorSessionManager.EpicorSession.CompanyID = Company;
               // EpicorSessionManager.EpicorSession.PlantID = plant;
                //开返修工单
                adapter1.ValidateJobNum(DMRJobNum);
                JobEntryDataSet dsJ = adapter1.GetDatasetForTree(DMRJobNum, 0, 0, false, "MFG,PRJ,SRV");
                adapter1.GetNewJobHead(dsJ);
                dsJ.Tables["JobHead"].Rows[0]["JobNum"] = DMRJobNum;
                dsJ.Tables["JobHead"].Rows[0]["PartNum"] = PartNum;
                dsJ.Tables["JobHead"].Rows[0]["JobType"] = "MFG";
                adapter1.ChangeJobHeadPartNum(dsJ);
                dsJ.Tables["JobHead"].Rows[0]["PlantMaintPlant"] = plant;
                dsJ.Tables["JobHead"].Rows[0]["Plant"] = plant;
                dsJ.Tables["JobHead"].Rows[0]["PlantName"] = "Main Site";

                adapter1.Update(dsJ);
                //物料
                JobEntryDataSet dsM = adapter1.GetDatasetForTree(DMRJobNum, 0, 0, false, "MFG,PRJ,SRV");
                adapter1.GetNewJobMtl(dsM, DMRJobNum, 0);
                dsM.Tables["JobMtl"].Rows[0]["PartNum"] = PartNum;
                adapter1.ChangeJobMtlPartNum(dsM, true, ref PartNum, ss, "", "", out vMsgText, out vSubAvail, out vMsgType, out multipleMatch, out opPartChgCompleted, out opMtlIssuedAction);

                adapter1.Update(dsM);
                dsJ.Tables["JobHead"].Rows[0]["JobEngineered"] = true;
                dsJ.Tables["JobHead"].Rows[0]["JobReleased"] = true;
                dsJ.Tables["JobHead"].Rows[0]["ReqDueDate"] = time;
                adapter1.Update(dsJ);
                //工序接收返修
                DMRProcessingDataSet ds = adapter.GetByID(DMRID);
                int i = ds.Tables["DMRActn"].Rows.Count;
                adapter.GetNewDMRActnAcceptMTL(ds, DMRID);
                ds.Tables["DMRActn"].Rows[i]["DMRNum"] = DMRID;
                ds.Tables["DMRActn"].Rows[i]["Company"] = Company;

                adapter.ChangeJobNum(ds, DMRJobNum);
                ds.Tables["DMRActn"].Rows[i]["JobNum"] = DMRJobNum;

                adapter.ChangeJobAsmSeq(ds, AssemblySeq);
                ds.Tables["DMRActn"].Rows[i]["AssemblySeq"] = AssemblySeq;

                adapter.ChangeJobMtlSeq(ds, 10);//10是序号，默认写死

                ds.Tables["DMRActn"].Rows[i]["DispQuantity"] = DMRRepairQty;
                ds.Tables["DMRActn"].Rows[i]["TranQty"] = DMRRepairQty;
                ds.Tables["DMRActn"].Rows[i]["Quantity"] = 0;
                ds.Tables["DMRActn"].Rows[i]["AcceptIUM"] = "PCS";
                ds.Tables["DMRActn"].Rows[i]["TranUOM"] = "PCS";
                adapter.DefaultIssueComplete(ds);

                ds.Tables["DMRActn"].Rows[i]["WarehouseCode"] = "WIP";
                ds.Tables["DMRActn"].Rows[i]["BinNum"] = "01";
                adapter.ChangeWarehouse(ds);
                ds.Tables["DMRActn"].Rows[i]["ReasonCode"] = "D03";//返工
                                                                   //保存
                adapter.CustomUpdate(ds, out opLegalNumberMessage);
                return "1";
            }
            catch(Exception ex)
            {
                return "0|" + ex.Message;
            }
        }



        /// <summary>
        /// 拒收
        /// </summary>
        public static string RefuseDMRProcessing(string Company,
        string plant,
        decimal DMRUnQualifiedQty,
        string DMRUnQualifiedReason ,
        int DMRID )
        {

            Session EpicorSession = Common.GetEpicorSession();
            if (EpicorSession == null)
            {
                return "0|GetEpicorSession失败，请稍候再试|RepairDMRProcessing";
            }

            try
            {
                string opLegalNumberMessage = "";

                DMRProcessingImpl adapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<DMRProcessingImpl>(EpicorSession, ImplBase<Erp.Contracts.DMRProcessingSvcContract>.UriPath);
                //EpicorSessionManager.EpicorSession.CompanyID = Company;
                //EpicorSessionManager.EpicorSession.PlantID = plant;


                DMRProcessingDataSet ds = adapter.GetByID(DMRID);
                int i = ds.Tables["DMRActn"].Rows.Count;
                adapter.GetNewDMRActnReject(ds, DMRID);
                ds.Tables["DMRActn"].Rows[i]["DMRNum"] = DMRID;
                ds.Tables["DMRActn"].Rows[i]["Company"] = Company;
                ds.Tables["DMRActn"].Rows[i]["DispQuantity"] = DMRUnQualifiedQty;
                ds.Tables["DMRActn"].Rows[i]["Quantity"] = 0;
                ds.Tables["DMRActn"].Rows[i]["TranQty"] = 0;
                //ds.Tables["DMRActn"].Rows[0]["TotRemainQty"] = tqty;
                ds.Tables["DMRActn"].Rows[i]["AcceptIUM"] = "PCS";
                ds.Tables["DMRActn"].Rows[i]["ReasonCode"] = DMRUnQualifiedReason;

                //保存
                adapter.CustomUpdate(ds, out opLegalNumberMessage);
                return "1";
            }
            catch (Exception ex)
            {
                return "0|" + ex.Message;
            }
        }


    }
}
