using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using Newtonsoft.Json;
using System.Configuration;
using Common;


using Ice.Core;
using Erp.Proxy.BO;
using Erp.BO;
using Epicor.ServiceModel.Channels;
using Ice.Tablesets;
using System.Web;

namespace ErpAPI
{
    public static class CommonRepository
    {
        public static Session GetEpicorSession()
        {
            try
            {
                string serverUrl = ConfigurationManager.AppSettings["ServerUrl"];
                string userName = ConfigurationManager.AppSettings["EpicorLoginName"];
                string passWord = ConfigurationManager.AppSettings["EpicorLoginPassword"];
                string configFile = ConfigurationManager.AppSettings["ConfigFile"];

                Ice.Core.Session E9Session = new Ice.Core.Session(userName, passWord, serverUrl, Session.LicenseType.Default, configFile);
                return E9Session;
            }
            catch (Exception ex)
            {
                if (ex.Message.ToString().Contains("Maximum users exceeded on license type"))
                {
                    return null;
                }
                else
                {
                    return null;
                }
            }
        }


        public static string getJobNextOprTypes(string jobnum, int asmSeq, int oprseq, out int OutAsm, out int OutOprSeq, out string OutOpcode, out string OutOpDesc, string companyId)
        {
            string stype = "";
            OutAsm = 0;
            OutOprSeq = 0;
            OutOpcode = null;
            OutOpDesc = "";
            if (jobnum.Trim() == "") { return "0|jonum error"; }

            try
            {
                int NextOperSeq = 0;
                if (asmSeq == 0) //0层半层品
                {
                    DataTable drNextdt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn,"select OpCode,OprSeq,SubContract,Opdesc from erp.JobOper where Company='" + companyId + "' and JobNum='" + jobnum + "' and AssemblySeq='" + asmSeq + "' and OprSeq > '" + oprseq + "' order by OprSeq ASC ");
                    if (drNextdt != null && drNextdt.Rows.Count > 0) //当前半成品内有下工序
                    {
                        NextOperSeq = Convert.ToInt32(drNextdt.Rows[0]["OprSeq"]);
                        if (Convert.ToBoolean(drNextdt.Rows[0]["SubContract"]))
                        {
                            stype = "S|下工序外协:" + drNextdt.Rows[0]["Opdesc"].ToString();
                            OutAsm = asmSeq;
                            OutOprSeq = NextOperSeq;
                            OutOpcode = drNextdt.Rows[0]["OpCode"].ToString();
                            OutOpDesc = drNextdt.Rows[0]["Opdesc"].ToString();
                        }
                        else
                        {
                            stype = "M|下工序:" + drNextdt.Rows[0]["Opdesc"].ToString();
                            OutAsm = asmSeq;
                            OutOpcode = drNextdt.Rows[0]["OpCode"].ToString();
                            OutOprSeq = NextOperSeq;
                            OutOpDesc = drNextdt.Rows[0]["Opdesc"].ToString();
                        }
                    }
                    else
                    ////0层半层品内无下工序，代表下面到仓库,暂不考虑工单生产到工单的情况
                    {
                        DataTable jobProddt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, "select WarehouseCode WarehouseCodeDescription from erp.JobProd where Company='" + companyId + "' and JobNum='" + jobnum + "'");
                        stype = "P|工序完成，收货至仓库:" + jobProddt.Rows[0]["WarehouseCodeDescription"].ToString();
                        OutAsm = asmSeq;

                        string sql = "select Description from erp.Warehse where Company = '" + companyId + "' and WarehouseCode  = '" + jobProddt.Rows[0]["WarehouseCodeDescription"].ToString() + "'";
                        object o = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);
                        string WhDescription = o == null ? "" : o.ToString();

                        OutOpcode = jobProddt.Rows[0]["WarehouseCodeDescription"].ToString();
                        OutOprSeq = -1;
                        OutOpDesc = WhDescription;
                    }
                }
                else //上层还有半成品
                {
                    DataTable drNextdt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, "select OpCode,OprSeq,SubContract,Opdesc from erp.JobOper where Company='" + companyId + "' and JobNum='" + jobnum + "' and AssemblySeq='" + asmSeq + "' and OprSeq > '" + oprseq + "' order by OprSeq ASC ");
                    if (drNextdt != null && drNextdt.Rows.Count > 0) //当前半成品内有下工序
                    {
                        NextOperSeq = Convert.ToInt32(drNextdt.Rows[0]["OprSeq"]);
                        if (Convert.ToBoolean(drNextdt.Rows[0]["SubContract"]))
                        {
                            stype = "S|下工序外协" + drNextdt.Rows[0]["Opdesc"].ToString();
                            OutAsm = asmSeq;
                            OutOprSeq = NextOperSeq;
                            OutOpcode = drNextdt.Rows[0]["OpCode"].ToString();
                            OutOpDesc = drNextdt.Rows[0]["Opdesc"].ToString();
                        }
                        else
                        {
                            stype = "M|下工序:" + drNextdt.Rows[0]["Opdesc"].ToString();
                            OutAsm = asmSeq;
                            OutOprSeq = NextOperSeq;
                            OutOpcode = drNextdt.Rows[0]["OpCode"].ToString();
                            OutOpDesc = drNextdt.Rows[0]["Opdesc"].ToString();
                        }
                    }
                    else
                    //非0层半层品内无下工序，还要到上层半层品中找第一个工序.
                    {
                        DataTable relateddrdt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, "select AssemblySeq,Description,RelatedOperation,Parent ParentAssemblySeq from erp.JobAsmbl where Company='" + companyId + "' and JobNum='" + jobnum + "' and AssemblySeq='" + asmSeq + "'");
                        if (relateddrdt != null && relateddrdt.Rows.Count > 0)
                        {
                            NextOperSeq = Convert.ToInt32(relateddrdt.Rows[0]["RelatedOperation"]);
                            int parAsmSeq = Convert.ToInt32(relateddrdt.Rows[0]["ParentAssemblySeq"]);
                            if (NextOperSeq == 0)
                            {
                                return "0|本半成品没有关联到父半成品的工序,取不到下工序类型";
                            }
                            else
                            {
                                //取父半成品相关工序的类型
                                drNextdt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, "select OpCode,OprSeq,SubContract,Opdesc from erp.JobOper where Company='" + companyId + "' and JobNum='" + jobnum + "' and AssemblySeq='" + parAsmSeq + "' and OprSeq = '" + NextOperSeq + "' order by OprSeq ASC ");
                                if (drNextdt != null && drNextdt.Rows.Count > 0)
                                {
                                    NextOperSeq = Convert.ToInt32(drNextdt.Rows[0]["OprSeq"]);
                                    if (Convert.ToBoolean(drNextdt.Rows[0]["SubContract"]))
                                    {
                                        stype = "S|下工序外协" + drNextdt.Rows[0]["Opdesc"].ToString();
                                        OutAsm = asmSeq;
                                        OutOprSeq = NextOperSeq;
                                        OutOpcode = drNextdt.Rows[0]["OpCode"].ToString();
                                        OutOpDesc = drNextdt.Rows[0]["Opdesc"].ToString();

                                    }
                                    else
                                    {
                                        stype = "M|下工序:" + drNextdt.Rows[0]["Opdesc"].ToString();
                                        OutAsm = asmSeq;
                                        OutOprSeq = NextOperSeq;
                                        OutOpcode = drNextdt.Rows[0]["OpCode"].ToString();
                                        OutOpDesc = drNextdt.Rows[0]["Opdesc"].ToString();
                                    }
                                }
                            }
                        }
                    }
                }
                return stype;
            }
            catch (Exception e)
            {
                return "0|" + e.Message.ToString();
            }

        }


        //D0506-01工单收货至库存
        public static string D0506_01(string rqr, string JobNum, int asmSeq, decimal jobQty, string lotnum, string wh, string bin, string companyId, string plantId)
        { //JobNum as string ,jobQty as decimal,partNum as string
            Session EpicorSession = CommonRepository.GetEpicorSession();
            if (EpicorSession == null)
            {
                return "0|GetEpicorSession失败，请稍候再试|工单收货至库存接口";
            }
            try
            {
                EpicorSession.CompanyID = companyId;
                EpicorSession.PlantID = plantId;
                DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, "select [JobOper].[JobNum] as [JobOper_JobNum],[JobOper].[AssemblySeq] as [JobOper_AssemblySeq],[JobOper].[OprSeq] as [JobOper_OprSeq],[JobOper].[RunQty] as [JobOper_RunQty],[JobOper].[QtyCompleted] as [JobOper_QtyCompleted],[JobAsmbl].[PartNum] as [JobAsmbl_PartNum],[JobAsmbl].[RequiredQty] as [JobAsmbl_RequiredQty],[JobPart].[ReceivedQty] as [JobPart_ReceivedQty],[JobHead].[JobComplete] as [JobHead_JobComplete],[JobHead].[JobReleased] as [JobHead_JobReleased] from Erp.JobOper as JobOper left outer join Erp.JobAsmbl as JobAsmbl on JobOper.Company = JobAsmbl.Company and JobOper.JobNum = JobAsmbl.JobNum and JobOper.AssemblySeq = JobAsmbl.AssemblySeq left outer join Erp.JobPart as JobPart on JobAsmbl.Company = JobPart.Company and JobAsmbl.JobNum = JobPart.JobNum and JobAsmbl.PartNum = JobPart.PartNum left outer join Erp.JobHead as JobHead on JobOper.Company = JobHead.Company and JobOper.JobNum = JobHead.JobNum where(JobOper.Company = '" + companyId + "'  and JobOper.JobNum = '" + JobNum + "'  and JobOper.AssemblySeq ='" + asmSeq.ToString() + "') order by JobOper.OprSeq Desc");
                for (int i = 0; dt != null && i < dt.Columns.Count; i++)
                {
                    dt.Columns[i].ColumnName = dt.Columns[i].ColumnName.Replace('_', '.');
                }
                string partNum = "";
                decimal recdQty = 0, compQty = 0, requQty = 0;
                bool jobRes = false, jobCom = true;
                if (dt != null && dt.Rows.Count > 0)
                {
                    partNum = dt.Rows[0]["JobAsmbl.PartNum"].ToString();
                    decimal.TryParse(dt.Rows[0]["JobPart.ReceivedQty"].ToString().Trim(), out recdQty);
                    decimal.TryParse(dt.Rows[0]["JobAsmbl.RequiredQty"].ToString().Trim(), out requQty);
                    decimal.TryParse(dt.Rows[0]["JobOper.QtyCompleted"].ToString().Trim(), out compQty);
                    bool.TryParse(dt.Rows[0]["JobHead.JobComplete"].ToString().Trim(), out jobCom);
                    bool.TryParse(dt.Rows[0]["JobHead.JobReleased"].ToString().Trim(), out jobRes);
                }
                if (jobCom)
                {

                    return "0|工单已关闭，不能收货。";
                }
                if (jobRes == false)
                {

                    return "0|工单未发放，不能收货。";
                }

                string[] w = GetPartWB(partNum, companyId);
                string tlot = w[2].ToString().Trim().ToLower();

                if (wh == "")
                { wh = w[0]; }
                //取物料默认的主仓库}

                //  据库位条码信息校验仓库并取库位
                string bin2 = "";
                string chkbinInfo = checkbin(bin, wh, companyId);
                if (chkbinInfo.Substring(0, 1) == "1")
                { bin2 = chkbinInfo.Substring(2); }
                else
                {
                    return chkbinInfo;
                }

                ReceiptsFromMfgImpl recAD = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<ReceiptsFromMfgImpl>(EpicorSession, ImplBase<Erp.Contracts.ReceiptsFromMfgSvcContract>.UriPath);
                ReceiptsFromMfgDataSet recDs = new ReceiptsFromMfgDataSet();
                string pcTranType = "MFG-STK";

                int piAssemblySeq = 0;
                recAD.GetNewReceiptsFromMfgJobAsm(JobNum, piAssemblySeq, pcTranType, Guid.NewGuid().ToString(), recDs);
                recDs.Tables["PartTran"].Rows[0]["PartNum"] = partNum;
                string opMessage = "";
                recAD.OnChangePartNum(recDs, partNum, out opMessage, false);
                string pcMessage;
                recDs.Tables[0].Rows[0]["ActTranQty"] = jobQty;
                recDs.Tables[0].Rows[0]["WareHouseCode"] = wh;
                recDs.Tables[0].Rows[0]["BinNum"] = bin2;
                if (tlot == "lot")
                {
                    recDs.Tables[0].Rows[0]["lotnum"] = lotnum;
                }

                recAD.OnChangeActTranQty(recDs, out pcMessage);
                bool requiresUserInput = false;
                recAD.PreUpdate(recDs, out requiresUserInput);
                decimal pdSerialNoQty = 0;
                bool plNegQtyAction = true;
                string pks;
                string pcProcessID = "RcptToInvEntry";
                recAD.ReceiveMfgPartToInventory(recDs, pdSerialNoQty, plNegQtyAction, out pcMessage, out pks, pcProcessID);

                return "1|处理成功";
            }
            catch (Exception ex)
            {
                return "0|" + ex.Message.ToString();
            }
            finally
            {
                EpicorSession.Dispose();
            }
        }



        public static string[] GetPartWB(string partnum, string companyId)
        {
            string[] pp = new string[3];
            pp[0] = ""; pp[1] = ""; pp[2] = "";
            try
            {
                bool tlot = false;

                DataTable partDT = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, "select * from erp.Part where Company='" + companyId + "' and PartNum='" + partnum + "'");
                DataTable plantDT = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, "select * from erp.PartPlant where Company='" + companyId + "' and PartNum='" + partnum + "'");
                DataTable pwDT = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, "select * from erp.PartWhse where Company='" + companyId + "' and PartNum='" + partnum + "'");
                string pw = "";
                if (partDT != null && partDT.Rows.Count > 0) { tlot = Convert.ToBoolean(partDT.Rows[0]["TrackLots"]); }
                if (plantDT != null && plantDT.Rows.Count > 0)
                { pw = plantDT.Rows[0]["PrimWhse"].ToString().Trim(); }
                DataTable wbDT = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, "select BinNum from erp.WhseBin where Company='" + companyId + "' and WarehouseCode='" + pw + "'");
                string wb = "";
                int CNT = pwDT != null ? pwDT.Rows.Count : 0;
                for (int i = 0; i < CNT; i++)
                {
                    if (pwDT.Rows[i]["WarehouseCode"].ToString().Trim() == pw)
                    {
                        if (wbDT != null && wbDT.Rows.Count > 0)
                        {
                            wb = wbDT.Rows[0]["BinNum"].ToString().Trim();
                        }
                    }

                }

                pp[0] = pw;
                pp[1] = wb;
                if (tlot) { pp[2] = "lot"; } else { pp[2] = ""; }

                return pp;
            }
            catch (Exception ex)
            {
                return pp;
            }

        }



        public static string[] GetPartWB(string partnum, string companyId, string plant)//获取物料的默认主仓库和库位
        {
            string[] pp = new string[3];
            pp[0] = ""; pp[1] = ""; pp[2] = "";
            try
            {
                bool tlot = false;
                DataTable partDT = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, "select * from erp.Part where Company='" + companyId + "' and PartNum='" + partnum + "'");



                string sql = "select PrimWhse from erp.PartPlant where Company='" + companyId + "' and PartNum='" + partnum + "' and Plant='" + plant + "'";
                object o = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);
                string pw = o == null ? "" : o.ToString();

                sql = "select PrimBin from erp.PlantWhse where Company='" + companyId + "' and PartNum='" + partnum + "' and Plant='" + plant + "' and WarehouseCode='" + pw + "'";
                o = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);
                string wb = o == null ? "" : o.ToString();

            
                if (partDT != null && partDT.Rows.Count > 0) { tlot = Convert.ToBoolean(partDT.Rows[0]["TrackLots"]); }
                pp[0] = pw;
                pp[1] = wb;
                if (tlot) { pp[2] = "lot"; } else { pp[2] = ""; }
                return pp;
            }
            catch (Exception)
            {
                return pp;
            }
        }



        public static string checkbin(string binbar, string wh, string companyId)
        {
            if (binbar.Trim() == "") return "0|库位信息不可为空";
            if (wh.Trim() == "") return "0|仓库id不可为空";
            int sp = 0;
            sp = binbar.IndexOf('-');
            if (sp <= 0) return "0|库位信息不正确";
            string zonid = binbar.Substring(0, sp);
            string binid = binbar.Substring(sp + 1);
            if (zonid == "") return "0|库位信息不正确，包括的区域为空";
            if (binid == "") return "0|库位信息不正确，包括的库位为空";
            try
            {
                DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, "select [WhseBin].[WarehouseCode] as [WhseBin_WarehouseCode],[WhseBin].[ZoneID] as [WhseBin_ZoneID],[WhseBin].[BinNum] as [WhseBin_BinNum] from Erp.WhseBin as WhseBin where (WhseBin.Company = '" + companyId + "'  and WhseBin.ZoneID = '" + zonid + "'  and WhseBin.BinNum = '" + binid + "' and WhseBin.WarehouseCode='" + wh + "')");

                for (int i = 0; dt != null &&  i < dt.Columns.Count; i++)
                {
                    dt.Columns[i].ColumnName = dt.Columns[i].ColumnName.Replace('_', '.');
                }
                string retuWh;

                if (dt != null && dt.Rows.Count > 0)
                {
                    retuWh = dt.Rows[0]["WhseBin.WarehouseCode"].ToString().Trim();
                    if (retuWh.ToLower() == wh.ToLower())
                    {
                        return "1|" + binid;
                    }
                    else
                    {
                        return "0|库位信息对应的仓库" + retuWh + "与默认仓库" + wh + "不一致，请检查修改epicor中的数据";
                    }
                }

                else
                {
                    return "0|找不到默认仓库，请检查epicor数据";
                }
            }

            catch (Exception ex)
            {
                return "0|" + ex.Message.ToString();
            }
        }



        //不合格品发起
        public static string Startnonconf(
                string JobNum,
                int AssemblySeq,
                string Company,
                int MtlSeq,
                decimal Qty,//让步数(DMRQualifiedQty) + 返修数(DMRRepairQty)+拒收数(DMRUnQualifiedQty) + 
                string WarehouseCode,
                string BinNum,
                string ReasonCode,
                string plant,
                string LotNum,
                string type,
                out int tranid)
        {
            tranid = 0;
            Session EpicorSession = CommonRepository.GetEpicorSession();
            if (EpicorSession == null)
            {
                return "0|GetEpicorSession失败，请稍候再试|Startnonconf";
            }
            try
            {
                EpicorSession.PlantID = plant;
                bool plAsmReturned = true;
                string snWarning = "";

                NonConfImpl adapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<NonConfImpl>(EpicorSession, ImplBase<Erp.Contracts.NonConfSvcContract>.UriPath);
                NonConfDataSet ds = new NonConfDataSet();


                if (type == "外协")
                {//新增工序
                     ds = adapter.AddNonConf("Operation");
                }
                else if (type == "物料")
                {
                    //新增物料
                     ds = adapter.AddNonConf("JobMaterial");
                }
                //加入工单
                adapter.OnChangeJobNum(JobNum, out plAsmReturned, out snWarning, ds);
                //选择阶层
                ds.Tables["NonConf"].Rows[0]["JobNum"] = JobNum;
                ds.Tables["NonConf"].Rows[0]["AssemblySeq"] = AssemblySeq;
                adapter.OnChangeJobAsm(AssemblySeq, ds);
                //选择物料
                adapter.OnChangeJobMtl(MtlSeq, ds);
                ds.Tables["NonConf"].Rows[0]["FromOprSeq"] = MtlSeq;
                //填数量
                adapter.OnChangeTranQty(Qty, ds);
                ds.Tables["NonConf"].Rows[0]["Quantity"] = Qty;
                //选仓库
                adapter.OnChangeWarehouseCode(WarehouseCode, false, ds);
                ds.Tables["NonConf"].Rows[0]["ToWarehouseCode"] = WarehouseCode;

                string whc = "WIP";
                if (plant.Substring(0, 2) == "RR") whc = "RRWIP";
                if (plant.Substring(0, 2) == "HD") whc = "HDWIP";

                ds.Tables["NonConf"].Rows[0]["WarehouseCode"] = whc;


                //填库位
                adapter.OnChangeBinNum(BinNum, false, ds);
                ds.Tables["NonConf"].Rows[0]["ToBinNum"] = BinNum;
                ds.Tables["NonConf"].Rows[0]["BinNum"] = "01";
                //原因
                ds.Tables["NonConf"].Rows[0]["ReasonCode"] = ReasonCode;

                if (type == "物料")
                    //批次
                    ds.Tables["NonConf"].Rows[0]["LotNum"] = LotNum;

                //保存
                adapter.Update(ds);
                tranid = int.Parse(ds.Tables["NonConf"].Rows[0]["TranID"].ToString());

                return "1";
            }
            catch (Exception ex)
            {
                return "0|" + ex.Message;
            }
            finally
            {
                EpicorSession.Dispose();
            }
        }


        //检查处理
        public static string StartInspProcessing(
            int TranID,
            decimal DMRQualifiedQty,
            decimal UnQualifiedQty,//返修数(DMRRepairQty)+拒收数(DMRUnQualifiedQty)
            string FailedReasonCode,
            string FailedWarehouseCode,
            string FailedBin,
            string type,
            string plant,
            string packslip,
            int poline,
            out int dmrid)
        {
            dmrid = 0;
            Session EpicorSession = CommonRepository.GetEpicorSession();

            if (EpicorSession == null)
            {
                return "0|GetEpicorSession失败，请稍候再试|StartInspProcessing";
            }

            

            try
            {
                EpicorSession.PlantID = plant;

               
                if (plant.Contains("RR")) FailedWarehouseCode = "RR" + FailedWarehouseCode;
                if (plant.Contains("HD")) FailedWarehouseCode = "HD" + FailedWarehouseCode;


                string infoMsg = "";
                string legalNumberMessage = "";
                int iDMRNum = 0;
                string InspectorID = "Q01";
                int iNonConfID = 0;

                InspProcessingImpl adapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<InspProcessingImpl>(EpicorSession, ImplBase<Erp.Contracts.InspProcessingSvcContract>.UriPath);
                InspProcessingDataSet ds = new InspProcessingDataSet();


                if (type == "外协不良2")
                {
                    string sql = @"select VendorNum, PurPoint, PackLine, PackSlip from erp.RcvDtl where PackSlip = '"+packslip+"' and POLine = "+poline+"";
                    DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);

                    ds = adapter.GetReceiptByID((int)dt.Rows[0]["VendorNum"], dt.Rows[0]["PurPoint"].ToString(), packslip, (int)dt.Rows[0]["PackLine"]);
                }
                else
                {
                     ds = adapter.GetByID(TranID);
                }


                //检验员
                ds.Tables["InspNonConf"].Rows[0]["InspectorID"] = InspectorID;
                adapter.AssignInspectorNonConf(InspectorID, ds, out infoMsg);

                //合格数
                if (type == "报工")
                {
                    ds.Tables["InspNonConf"].Rows[0]["DimPassedQty"] = DMRQualifiedQty;
                    adapter.OnChangePassedQty(ds, "NonConf", DMRQualifiedQty, out infoMsg);
                }

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
                //保存
                if (type == "物料")
                    adapter.InspectMaterial(out legalNumberMessage, out iDMRNum, ds);
                if (type == "报工")
                    adapter.InspectOperation(out legalNumberMessage, out iDMRNum, ds);
                if (type == "外协不良2")
                    adapter.InspectReceipt(out legalNumberMessage, out iDMRNum, out iNonConfID, ds);


                dmrid = iDMRNum;
                return "1";
            }
            catch (Exception ex)
            {
                return "0|" + ex.Message;
            }
            finally
            {
                EpicorSession.Dispose();
            }
        }


        //返修
        public static string RepairDMRProcessing(
        int DMRID,
        string Company,
        string plant,
        string PartNum,
        decimal DMRRepairQty,//返修         
        string DMRJobNum,
        string IUM)
        {
            Session EpicorSession = CommonRepository.GetEpicorSession();
            if (EpicorSession == null)
            {
                return "0|GetEpicorSession失败，请稍候再试|RepairDMRProcessing";
            }
            try
            {
                EpicorSession.PlantID = plant;
                EpicorSession.CompanyID = Company;
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


                //工单是否存在
                string sql = "select count(*) from erp.JobHead jh where jh.JobNum = '" + DMRJobNum + "'";
                object IsExist = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);


                //开返修工单
                if (Convert.ToInt32(IsExist) == 0)
                {
                    adapter1.ValidateJobNum(DMRJobNum);
                    JobEntryDataSet dsJ = adapter1.GetDatasetForTree(DMRJobNum, 0, 0, false, "MFG,PRJ,SRV");
                    adapter1.GetNewJobHead(dsJ);
                    dsJ.Tables["JobHead"].Rows[0]["JobNum"] = DMRJobNum;
                    dsJ.Tables["JobHead"].Rows[0]["PartNum"] = PartNum;
                    dsJ.Tables["JobHead"].Rows[0]["JobType"] = "MFG";
                    adapter1.ChangeJobHeadPartNum(dsJ);
                    dsJ.Tables["JobHead"].Rows[0]["PlantMaintPlant"] = plant;
                    dsJ.Tables["JobHead"].Rows[0]["Plant"] = plant;


                    if (plant.Contains("RR")) dsJ.Tables["JobHead"].Rows[0]["PlantName"] = "引擎零部件工厂";
                    if (plant.Contains("Mfg")) dsJ.Tables["JobHead"].Rows[0]["PlantName"] = "Main Site";
                    if (plant.Contains("HD")) dsJ.Tables["JobHead"].Rows[0]["PlantName"] = "航电工厂";



                    adapter1.Update(dsJ);
                    //物料
                    JobEntryDataSet dsM = adapter1.GetDatasetForTree(DMRJobNum, 0, 0, false, "MFG,PRJ,SRV");
                    adapter1.GetNewJobMtl(dsM, DMRJobNum, 0);
                    dsM.Tables["JobMtl"].Rows[0]["PartNum"] = PartNum;
                    adapter1.ChangeJobMtlPartNum(dsM, true, ref PartNum, ss, "", "", out vMsgText, out vSubAvail, out vMsgType, out multipleMatch, out opPartChgCompleted, out opMtlIssuedAction);

                    adapter1.Update(dsM);
                    //工单是否发放
                    dsJ.Tables["JobHead"].Rows[0]["JobEngineered"] = true;
                    dsJ.Tables["JobHead"].Rows[0]["JobReleased"] = true;
                    dsJ.Tables["JobHead"].Rows[0]["ReqDueDate"] = time;
                    adapter1.Update(dsJ);
                }


                //更新返修工单可生产数
                sql = "update jobhead set UDReqQty_c = " + DMRRepairQty + " where jobnum = '" + DMRJobNum + "'";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);


                //工序接收返修
                DMRProcessingDataSet ds = adapter.GetByID(DMRID);
                int i = ds.Tables["DMRActn"].Rows.Count;
                adapter.GetNewDMRActnAcceptMTL(ds, DMRID); // 物料、工序返修，物料让步
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
                ds.Tables["DMRActn"].Rows[i]["AcceptIUM"] = IUM;
                ds.Tables["DMRActn"].Rows[i]["TranUOM"] =IUM;
                adapter.DefaultIssueComplete(ds);


                if (plant.Contains("RR")) ds.Tables["DMRActn"].Rows[i]["WarehouseCode"] = "RRWIP";
                if (plant.Contains("Mfg")) ds.Tables["DMRActn"].Rows[i]["WarehouseCode"] = "WIP";
                if (plant.Contains("HD")) ds.Tables["DMRActn"].Rows[i]["WarehouseCode"] = "HDWIP";


                adapter.ChangeWarehouse(ds);
                ds.Tables["DMRActn"].Rows[i]["BinNum"] = "01";


                ds.Tables["DMRActn"].Rows[i]["ReasonCode"] = "D03"; //返修D03，  让步接收D01

                //保存
                adapter.CustomUpdate(ds, out opLegalNumberMessage);

                return "1";
            }
            catch (Exception ex)
            {
                return "0|" + ex.Message;
            }
            finally
            {
                EpicorSession.Dispose();
            }
        }




        //DMR让步
        public static string ConcessionDMRProcessing(
        int DMRID,
        string Company,
        string plant,
        string PartNum,
        int AssemblySeq,
        int Seq, //type=报工，代表工序号， type=物料，代表物料序号，
        decimal DMRQualifiedQty,    
        string DMRJobNum,
        string type
        )
        {
            Session EpicorSession = CommonRepository.GetEpicorSession();
            if (EpicorSession == null)
            {
                return "0|GetEpicorSession失败，请稍候再试|RepairDMRProcessing";
            }
            try
            {
                EpicorSession.PlantID = plant;
                
                DateTime time = DateTime.Now;
                string opLegalNumberMessage = "";

                DMRProcessingImpl adapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<DMRProcessingImpl>(EpicorSession, ImplBase<Erp.Contracts.DMRProcessingSvcContract>.UriPath);
                //JobEntryImpl adapter1 = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<JobEntryImpl>(EpicorSession, ImplBase<Erp.Contracts.JobEntrySvcContract>.UriPath);
                

        
                DMRProcessingDataSet ds = adapter.GetByID(DMRID);
                int i = ds.Tables["DMRActn"].Rows.Count;


                if (type == "报工")
                    adapter.GetNewDMRActnAcceptOPR(ds, DMRID); //报工让步
                else if (type == "物料")//
                    adapter.GetNewDMRActnAcceptMTL(ds, DMRID); // 物料、工序返修，物料让步
                //else if (type == "外协不良2")
                //    adapter.GetNewDMRActnAcceptSTK(ds, DMRID);


                ds.Tables["DMRActn"].Rows[i]["DMRNum"] = DMRID;
                ds.Tables["DMRActn"].Rows[i]["Company"] = Company;


                if (type != "报工")
                {
                    adapter.ChangeJobNum(ds, DMRJobNum);
                    ds.Tables["DMRActn"].Rows[i]["JobNum"] = DMRJobNum;

                    adapter.ChangeJobAsmSeq(ds, AssemblySeq);
                    ds.Tables["DMRActn"].Rows[i]["AssemblySeq"] = AssemblySeq;

                    adapter.ChangeJobMtlSeq(ds, Seq); // 物料、工序返修，物料让步
                }
                

                ds.Tables["DMRActn"].Rows[i]["DispQuantity"] = DMRQualifiedQty;
                ds.Tables["DMRActn"].Rows[i]["TranQty"] = DMRQualifiedQty;
                ds.Tables["DMRActn"].Rows[i]["Quantity"] = 0;
                ds.Tables["DMRActn"].Rows[i]["AcceptIUM"] = "PCS";
                ds.Tables["DMRActn"].Rows[i]["TranUOM"] = "PCS";
                adapter.DefaultIssueComplete(ds);

                string whc = "WIP";
                if (plant.Substring(0, 2) == "RR") whc = "RRWIP";
                if (plant.Substring(0, 2) == "HD") whc = "HDWIP";

                ds.Tables["DMRActn"].Rows[i]["WarehouseCode"] = whc;
                
                adapter.ChangeWarehouse(ds);
                ds.Tables["DMRActn"].Rows[i]["BinNum"] = "01";
                ds.Tables["DMRActn"].Rows[i]["ReasonCode"] = "D01"; //返修D03，  让步接收D01

                //保存
                adapter.CustomUpdate(ds, out opLegalNumberMessage);

                return "1";
            }
            catch (Exception ex)
            {
                return "0|" + ex.Message;
            }
            finally
            {
                EpicorSession.Dispose();
            }
        }



        /// <summary>
        /// 拒收
        /// </summary>
        public static string RefuseDMRProcessing(string Company,
        string plant,
        decimal DMRUnQualifiedQty,
        string DMRUnQualifiedReason,
        int DMRID,
        string IUM)
        {

            Session EpicorSession = CommonRepository.GetEpicorSession();

            if (EpicorSession == null)
            {
                return "0|GetEpicorSession失败，请稍候再试|RepairDMRProcessing";
            }
            try
            {
                EpicorSession.CompanyID = Company;
                EpicorSession.PlantID = plant;
                string opLegalNumberMessage = "";

                DMRProcessingImpl adapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<DMRProcessingImpl>(EpicorSession, ImplBase<Erp.Contracts.DMRProcessingSvcContract>.UriPath);


                DMRProcessingDataSet ds = adapter.GetByID(DMRID);
                int i = ds.Tables["DMRActn"].Rows.Count;
                adapter.GetNewDMRActnReject(ds, DMRID);
                ds.Tables["DMRActn"].Rows[i]["DMRNum"] = DMRID;
                ds.Tables["DMRActn"].Rows[i]["Company"] = Company;
                ds.Tables["DMRActn"].Rows[i]["DispQuantity"] = DMRUnQualifiedQty;
                ds.Tables["DMRActn"].Rows[i]["Quantity"] = 0;
                ds.Tables["DMRActn"].Rows[i]["TranQty"] = 0;
                //ds.Tables["DMRActn"].Rows[0]["TotRemainQty"] = tqty;
                ds.Tables["DMRActn"].Rows[i]["AcceptIUM"] = IUM;
                ds.Tables["DMRActn"].Rows[i]["ReasonCode"] = DMRUnQualifiedReason;

                //保存
                adapter.CustomUpdate(ds, out opLegalNumberMessage);
                return "1";
            }
            catch (Exception ex)
            {
                return "0|" + ex.Message;
            }
            finally
            {
                EpicorSession.Dispose();
            }
        }
    }
}
