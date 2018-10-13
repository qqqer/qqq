using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.Data.Odbc;
using System.Threading;
using Ice.Core;
using Ice.Proxy.BO;
using Erp.Adapters;
using Erp.Contracts;
using Erp.BO;
using Erp.Proxy.BO;
using Epicor.ServiceModel.Channels;
using Ice.Adapters;
using Ice.Tablesets;
using Ice.BO;
using System.Collections;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Web.Script.Serialization;

namespace EpicorAPIManager
{
    public class JobManager
    {
        //Labor labor;
        //JobEntry jobEntry;
        //SalesOrder salesOrder;
        //POApvMsg poApvMsg;
        //Resource resource;
        //OpMaster opMaster;
        //Warehse warehse;
        //LaborDtlSearch search;
        //20160225-Jeff- Epicor.Mfg.Core.BLConnectionPool ConnectionPool;
        string bpmSer = System.Configuration.ConfigurationManager.AppSettings["bpmSer"];
        string cgysStra = System.Configuration.ConfigurationManager.AppSettings["cgysStra"];
        string cgysBo = System.Configuration.ConfigurationManager.AppSettings["cgysBo"];
        string cgysUUID = System.Configuration.ConfigurationManager.AppSettings["cgysUUID"];
        string zxUUID = System.Configuration.ConfigurationManager.AppSettings["zxUUID"];
        string zcUUID = System.Configuration.ConfigurationManager.AppSettings["zcUUID"];


        private string gbinid;   //流程id
        public string Gbinid
        { get { return gbinid; } }

        private string gtaskid; //任务id
        public string Gtaskid
        { get { return gtaskid; } }

        private string gtitle; //任务标题
        public string Gtitle
        { get { return gtitle; } }

        private string gnextuser; //下步办理人
        public string Gnextuser
        { get { return gnextuser; } }

        public JobManager()
        {
            //----20160112---Jeff ConnectionPool = CommonClass.GetSession.Get().ConnectionPool;
        }

        public String CreateNewApInvoice(string vendorID, string invoiceNum, string invoiceDate, string invoiceVendorAmt, string termsCode, string json)
        {
            Session EpicorSession = CommonClass.Authentication.GetEpicorSession();
            if (EpicorSession == null)
            {
                return "-1|erp登录异常.";
            }
            APInvGrpImpl aPInvGrp = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<APInvGrpImpl>(EpicorSession, ImplBase<Erp.Contracts.APInvGrpSvcContract>.UriPath);
            APInvoiceImpl aPInvoice = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<APInvoiceImpl>(EpicorSession, ImplBase<Erp.Contracts.APInvoiceSvcContract>.UriPath);
            int VendorNum = 0;
            string cGroupID = "";
            try
            {
                string groupID = "";
                string maxGroupID = QueryERP("select max(groupid) grp from ERP.APInvGrp where GroupID like '" + DateTime.Now.ToString("yyMM") + "%' ");
                if (string.IsNullOrEmpty(maxGroupID))
                {
                    groupID = DateTime.Now.ToString("yyMM") + "0001";
                }
                else
                {
                    groupID = (Convert.ToInt32(maxGroupID) + 1).ToString();
                }
                cGroupID = createAPInvGpl(groupID, DateTime.Now, "", aPInvGrp);
                APInvoiceDataSet ds = new APInvoiceDataSet();
                aPInvoice.GetNewAPInvHedInvoice(ds, cGroupID);
                string cMessageText = "";
                aPInvoice.ChangeVendorID(vendorID, ds);
                ds.Tables["APInvHed"].Rows[0]["InvoiceNum"] = invoiceNum;
                ds.Tables["APInvHed"].Rows[0]["InvoiceDate"] = Convert.ToDateTime(invoiceDate).Date;
                DateTime? ApplyDate = Convert.ToDateTime(invoiceDate);
                aPInvoice.Update(ds);
                //aPInvoice.ChangeInvoiceDateEx(Convert.ToDateTime(invoiceDate), "", out cMessageText, ds);
                ds.Tables["APInvHed"].Rows[0]["InvoiceVendorAmt"] = Convert.ToDecimal(invoiceVendorAmt);
                aPInvoice.ChangeInvoiceVendorAmt(Convert.ToDecimal(invoiceVendorAmt), ds);
                ds.Tables["APInvHed"].Rows[0]["ReadyToCalc"] = false;
                ds.Tables["APInvHed"].Rows[0]["DocInvoiceVendorAmt"] = Convert.ToDecimal(invoiceVendorAmt);
                ds.Tables["APInvHed"].Rows[0]["InvoiceVendorAmt"] = Convert.ToDecimal(invoiceVendorAmt);
                ds.Tables["APInvHed"].Rows[0]["ScrDocInvoiceVendorAmt"] = Convert.ToDecimal(invoiceVendorAmt);
                ds.Tables["APInvHed"].Rows[0]["DocInvoiceVendorAmt"] = Convert.ToDecimal(invoiceVendorAmt);
                ds.Tables["APInvHed"].Rows[0]["InvoiceVendorAmt"] = Convert.ToDecimal(invoiceVendorAmt);
                ds.Tables["APInvHed"].Rows[0]["ScrDocInvoiceVendorAmt"] = Convert.ToDecimal(invoiceVendorAmt);
                if (ds.Tables["APInvHed"].Rows[0]["TermsCode"] == null || string.IsNullOrWhiteSpace(ds.Tables["APInvHed"].Rows[0]["TermsCode"].ToString()))
                {
                    ds.Tables["APInvHed"].Rows[0]["TermsCode"] = termsCode;
                }
                aPInvoice.Update(ds);
                try
                {
                    VendorNum = Convert.ToInt32(ds.Tables["APInvHed"].Rows[0]["VendorNum"]);
                }
                catch
                {
                    VendorNum = 0;
                }
                DataTable detaildt = ToDataTable(json);
                if (detaildt == null || detaildt.Rows.Count == 0)
                {
                    return "false|没有数据传入或传入格式不对.";
                }
                APInvReceiptBillingDataSet abds = new APInvReceiptBillingDataSet();
                aPInvoice.GetAPUninvoicedReceipts(abds, VendorNum, invoiceNum, 0);
                for (int i = 0; i < detaildt.Rows.Count; i++)
                {
                    try
                    {
                        //[{"VENDORID":"AZ0007","PONUM":"860239","PACKSLIP":"AZ0007201801154471","PackLine":"1"}]
                        //[{"VENDORID":"SPR0004","PONUM":"36","PACKSLIP":"16092010515408937"}]
                        foreach (DataRow dr in abds.Tables["APUninvoicedRcptLines"].Rows)
                        {
                            if (dr["PackSlip"].ToString() == detaildt.Rows[i]["PACKSLIP"].ToString() &&
                                dr["PackLine"].ToString() == detaildt.Rows[i]["PackLine"].ToString())
                            {
                                dr["SelectLine"] = true;
                                aPInvoice.SelectUninvoicedRcptLines(abds, VendorNum, "",
                                    Convert.ToInt32(detaildt.Rows[i]["PONUM"].ToString()), detaildt.Rows[i]["PACKSLIP"].ToString(),
                                    false, invoiceNum, false);
                                dr["RowMod"] = "U";
                                //DataRow drSelect = abds.Tables[2].NewRow();
                                //for (int x = 0; x < abds.Tables["APUninvoicedRcptLines"].Columns.Count; x++)
                                //{
                                //    for (int y = 0; y < abds.Tables[2].Columns.Count; y++)
                                //    {
                                //        if (abds.Tables["APUninvoicedRcptLines"].Columns[x].ColumnName ==
                                //            abds.Tables[2].Columns[y].ColumnName)
                                //        {
                                //            drSelect[y] = dr[x];
                                //            break;
                                //        }
                                //    }
                                //}
                                //abds.Tables[2].Rows.Add(drSelect);
                            }
                        }
                        string oplocmsg = "";
                        aPInvoice.InvoiceSelectedLines(abds, out oplocmsg);
                    }
                    catch (Exception ex)
                    {
                        EpicorSession.Dispose();
                        return ex.Message.ToString();
                    }
                }
                EpicorSession.Dispose();
                return "";
            }
            catch (Exception ex)
            {
                EpicorSession.Dispose();
                return ex.Message.ToString();
            }
        }

        private string createAPInvGpl(string groupID, DateTime? ApplyDate, string companyId, APInvGrpImpl aPInvGrp)
        {
            string result = groupID;
            try
            {
                APInvGrpDataSet apgds = aPInvGrp.GetByID(groupID);
                result = apgds.Tables["APInvGrp"].Rows[0]["GroupID"].ToString();


            }
            catch (Exception ex)
            {
                result = CreateGpl(groupID, ApplyDate, companyId, aPInvGrp);
            }
            return result;
        }

        public string CreateGpl(string groupID, DateTime? ApplyDate, string companyId, APInvGrpImpl aPInvGrp)
        {
            string result = "";
            try
            {
                APInvGrpDataSet apgds = new APInvGrpDataSet();
                aPInvGrp.GetNewAPInvGrp(apgds);
                apgds.Tables["APInvGrp"].Rows[0]["GroupID"] = groupID;
                //apgds.Tables["APInvGrp"].Rows[0]["company"] = companyId;
                apgds.Tables["APInvGrp"].Rows[0]["FiscalPeriod"] = Convert.ToDateTime(ApplyDate).Month;
                if (ApplyDate != null)
                {
                    apgds.Tables["APInvGrp"].Rows[0]["ApplyDate"] = ApplyDate;

                }
                aPInvGrp.Update(apgds);
                result = groupID;
                return result;
            }
            catch (Exception ex)
            {
                result = ex.Message.ToString();   //此为现有记录的重复输入.
                return result;
            }

        }


        //登陆erp返回错误
        public string ErpLogin()
        {
            try
            {

                //if (EpicorSessionManager.EpicorSession == null || !EpicorSessionManager.EpicorSession.IsValidSession(EpicorSessionManager.EpicorSession.SessionID, "baogong"))
                //{
                //    EpicorSessionManager.EpicorSession = CommonClass.Authentication.GetEpicorSession();
                //}
                //else
                //{
                //    EpicorSessionManager.EpicorSession.Dispose();
                //    EpicorSessionManager.EpicorSession = CommonClass.Authentication.GetEpicorSession();
                //}
                EpicorSessionManager.EpicorSession = CommonClass.Authentication.GetEpicorSession();
                return "true";
            }
            catch (Exception ex)
            {
                if (ex.Message.ToString().Contains("Maximum users exceeded on license type"))
                {
                    return "erp用户数不够，请稍候再试.";
                }
                else
                {
                    return ex.Message.ToString();
                }
            }
        }

        //登陆erp返回错误
        public Session ErpLoginbak()
        {
            try
            {
                Session EpicorSession = CommonClass.Authentication.GetEpicorSession();
                //EpicorSessionManager.EpicorSession = CommonClass.Authentication.GetEpicorSession();
                return EpicorSession;
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

        //批量发料
        public string MultipleIssueReturnSTKMTL(string json, string companyId)
        {
            UpdateAWS("insert into BO_ERPRECORD(UD01,UD02,UD10) values('" + json + "','" + companyId + "','批量发料')");
            DataTable dt = ToDataTable(json);
            if (dt == null || dt.Rows.Count == 0)
            {
                return "false|没有数据传入.";
            }
            string message = "";
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string tracklots = QueryERP("select tracklots from Erp.Part where Company='" + companyId + "' and PartNum='" + dt.Rows[i]["PartNum"].ToString() + "'");
                if (tracklots.ToLower() == "true" && string.IsNullOrEmpty(dt.Rows[i]["LotNum"].ToString()))
                {
                    message = message + " 物料:" + dt.Rows[i]["PartNum"].ToString() + "使用批次管理,批次不能为空;";
                }
                else
                if (tracklots.ToLower() == "false" && !string.IsNullOrEmpty(dt.Rows[i]["LotNum"].ToString()))
                {
                    message = message + " 物料:" + dt.Rows[i]["PartNum"].ToString() + "未使用批次管理,批次号填写错误;";
                }
                else
                {
                    string onhandQty = QueryERP("select OnhandQty from Erp.PartBin where Company='" + companyId + "' and PartNum='" + dt.Rows[i]["PartNum"].ToString() + "' and LotNum='" + dt.Rows[i]["LotNum"].ToString() + "' and WarehouseCode=(select WarehouseCode from Erp.JobMtl where JobNum='" + dt.Rows[i]["JobNum"].ToString() + "' and AssemblySeq='" + dt.Rows[i]["AssemblySeq"].ToString() + "' and PartNum='" + dt.Rows[i]["PartNum"].ToString() + "') ");
                    if (!string.IsNullOrEmpty(onhandQty) && Convert.ToDecimal(dt.Rows[i]["Qty"]) > Convert.ToDecimal(onhandQty))
                    {
                        message = message + " 物料:" + dt.Rows[i]["PartNum"].ToString() + " 库存不足;";
                    }
                }
            }

            if (message != "")
            {
                return "false|" + message;
            }
            else
            {
                string querysql = "";
                string tracklots = "";
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    int mtlSeq = Convert.ToInt32(QueryERP("select MtlSeq from Erp.JobMtl jm where jm.Company='" + companyId + "' and jm.JobNum='" + dt.Rows[i]["JobNum"].ToString() + "' and jm.AssemblySeq='" + Convert.ToInt32(dt.Rows[i]["AssemblySeq"]) + "' and jm.RelatedOperation='" + Convert.ToInt32(dt.Rows[i]["OprSeq"]) + "' and jm.PartNum='" + dt.Rows[i]["PartNum"].ToString() + "' "));
                    tracklots = QueryERP("select tracklots from Erp.Part where Company='" + companyId + "' and PartNum='" + dt.Rows[i]["PartNum"].ToString() + "'");
                    if (tracklots.ToLower() == "true" && !string.IsNullOrEmpty(dt.Rows[i]["LotNum"].ToString()))
                    {
                        querysql = "select jm.JobNum,jm.AssemblySeq,jm.RelatedOperation,jm.MtlSeq,jm.PartNum,jm.RequiredQty-jm.IssuedQty Qty,jm.IUM,jm.WarehouseCode,(select top(1) BinNum from Erp.PartBin where Company=jm.Company and WarehouseCode=jm.WarehouseCode and PartNum=jm.PartNum and LotNum='" + dt.Rows[i]["LotNum"].ToString() + "') BinNum,rg.InputWhse,rg.InputBinNum,isnull((select top(1) OnhandQty from Erp.PartBin where Company=jm.Company and WarehouseCode=jm.WarehouseCode and PartNum=jm.PartNum and LotNum='" + dt.Rows[i]["LotNum"].ToString() + "'),0) OnhandQty from Erp.JobMtl jm inner join Part p on jm.Company=p.Company and jm.PartNum=p.PartNum and p.TrackLots=1 inner join erp.JobOpDtl jod on jm.Company=jod.Company and jm.JobNum=jod.JobNum and jm.AssemblySeq=jod.AssemblySeq and jm.RelatedOperation=jod.OprSeq inner join erp.ResourceGroup rg on jod.Company=rg.Company and jod.ResourceGrpID=rg.ResourceGrpID where jm.Company='" + companyId + "' and jm.JobNum='" + dt.Rows[i]["JobNum"].ToString() + "' and jm.AssemblySeq='" + dt.Rows[i]["AssemblySeq"].ToString() + "' and jm.RelatedOperation='" + dt.Rows[i]["OprSeq"].ToString() + "' and jm.PartNum='" + dt.Rows[i]["PartNum"].ToString() + "'";
                        DataTable issuedt = GetDataByERP(querysql);
                        bool index = false;
                        if (issuedt != null && issuedt.Rows.Count > 0)
                        {
                            index = IssueReturnSTKMTLbak(dt.Rows[i]["JobNum"].ToString(), Convert.ToInt32(dt.Rows[i]["AssemblySeq"]), Convert.ToInt32(dt.Rows[i]["OprSeq"]), mtlSeq, dt.Rows[i]["PartNum"].ToString(), Convert.ToDecimal(dt.Rows[i]["Qty"]), DateTime.Now, issuedt.Rows[0]["IUM"].ToString(), issuedt.Rows[0]["WarehouseCode"].ToString(), issuedt.Rows[0]["BinNum"].ToString(), issuedt.Rows[0]["InputWhse"].ToString(), issuedt.Rows[0]["InputBinNum"].ToString(), dt.Rows[i]["LotNum"].ToString(), "工单发料", companyId);
                            if (!index)
                            {
                                message = message + " 物料:" + dt.Rows[i]["PartNum"].ToString() + " 批次:" + dt.Rows[i]["LotNum"].ToString() + "发料失败;";
                            }
                        }
                        else
                        {
                            message = message + " 物料:" + dt.Rows[i]["PartNum"].ToString() + "查询仓库或库位信息错误";
                        }
                    }
                    else if (tracklots.ToLower() == "false" && string.IsNullOrEmpty(dt.Rows[i]["LotNum"].ToString()))
                    {
                        querysql = "select jm.JobNum,jm.AssemblySeq,jm.RelatedOperation,jm.MtlSeq,jm.PartNum,jm.RequiredQty-jm.IssuedQty Qty,jm.IUM,jm.WarehouseCode,(select top(1) BinNum from Erp.PartBin where Company=jm.Company and WarehouseCode=jm.WarehouseCode and PartNum=jm.PartNum) BinNum,rg.InputWhse,rg.InputBinNum,isnull((select top(1) OnhandQty from Erp.PartBin where Company=jm.Company and WarehouseCode=jm.WarehouseCode and PartNum=jm.PartNum),0) OnhandQty from Erp.JobMtl jm inner join Part p on jm.Company=p.Company and jm.PartNum=p.PartNum inner join erp.JobOpDtl jod on jm.Company=jod.Company and jm.JobNum=jod.JobNum and jm.AssemblySeq=jod.AssemblySeq and jm.RelatedOperation=jod.OprSeq inner join erp.ResourceGroup rg on jod.Company=rg.Company and jod.ResourceGrpID=rg.ResourceGrpID where jm.Company='" + companyId + "' and jm.JobNum='" + dt.Rows[i]["JobNum"].ToString() + "' and jm.AssemblySeq='" + dt.Rows[i]["AssemblySeq"].ToString() + "' and jm.RelatedOperation='" + dt.Rows[i]["OprSeq"].ToString() + "' and jm.PartNum='" + dt.Rows[i]["PartNum"].ToString() + "'";
                        DataTable issuedt = GetDataByERP(querysql);
                        bool index = false;
                        if (issuedt != null && issuedt.Rows.Count > 0)
                        {
                            index = IssueReturnSTKMTLbak(dt.Rows[i]["JobNum"].ToString(), Convert.ToInt32(dt.Rows[i]["AssemblySeq"]), Convert.ToInt32(dt.Rows[i]["OprSeq"]), mtlSeq, dt.Rows[i]["PartNum"].ToString(), Convert.ToDecimal(dt.Rows[i]["Qty"]), DateTime.Now, issuedt.Rows[0]["IUM"].ToString(), issuedt.Rows[0]["WarehouseCode"].ToString(), issuedt.Rows[0]["BinNum"].ToString(), issuedt.Rows[0]["InputWhse"].ToString(), issuedt.Rows[0]["InputBinNum"].ToString(), dt.Rows[i]["LotNum"].ToString(), "工单发料", companyId);
                            if (!index)
                            {
                                message = message + " 物料:" + dt.Rows[i]["PartNum"].ToString() + " 发料失败;";
                            }
                        }
                        else
                        {
                            message = message + " 物料:" + dt.Rows[i]["PartNum"].ToString() + "查询仓库或库位信息错误";
                        }
                    }
                }
                if (message != "")
                {
                    return "false|" + message;
                }
                else
                {
                    return "true";
                }
            }
        }

        //单个发料 工单发料
        public string OneIssueReturnSTKMTL(string jobNum, int assemblySeq, int oprSeq, int mtlSeq, string partNum, decimal tranQty, DateTime tranDate, string lotNum, string companyId)
        {
            UpdateAWS("insert into BO_ERPRECORD(UD01,UD02,UD03,UD04,UD05,UD06,UD07,UD08,UD09,UD10) values('" + jobNum + "','" + assemblySeq + "','" + oprSeq + "','" + mtlSeq + "','" + partNum + "','" + tranQty + "','" + tranDate + "','" + lotNum + "','" + companyId + "','单个发料')");

            string tranReference = "工单发料";
            string querysql = "";
            string tracklots = QueryERP("select tracklots from Erp.Part where Company='" + companyId + "' and PartNum='" + partNum + "'");
            if (tracklots.ToLower() == "true" && !string.IsNullOrEmpty(lotNum))
            {
                querysql = "select jm.JobNum,jm.AssemblySeq,jm.RelatedOperation,jm.MtlSeq,jm.PartNum,jm.RequiredQty-jm.IssuedQty Qty,jm.IUM,jm.WarehouseCode,(select top(1) BinNum from Erp.PartBin where Company=jm.Company and WarehouseCode=jm.WarehouseCode and PartNum=jm.PartNum and LotNum='" + lotNum + "') BinNum,rg.InputWhse,rg.InputBinNum,isnull((select top(1) OnhandQty from Erp.PartBin where Company=jm.Company and WarehouseCode=jm.WarehouseCode and PartNum=jm.PartNum and LotNum='" + lotNum + "'),0) OnhandQty from Erp.JobMtl jm inner join Part p on jm.Company=p.Company and jm.PartNum=p.PartNum and p.TrackLots=1 inner join erp.JobOpDtl jod on jm.Company=jod.Company and jm.JobNum=jod.JobNum and jm.AssemblySeq=jod.AssemblySeq and jm.RelatedOperation=jod.OprSeq inner join erp.ResourceGroup rg on jod.Company=rg.Company and jod.ResourceGrpID=rg.ResourceGrpID where jm.Company='" + companyId + "' and jm.JobNum='" + jobNum + "' and jm.AssemblySeq='" + assemblySeq + "' and jm.RelatedOperation='" + oprSeq + "' and jm.PartNum='" + partNum + "'";
            }
            else if (tracklots.ToLower() == "true" && string.IsNullOrEmpty(lotNum))
            {
                querysql = "select jm.JobNum,jm.AssemblySeq,jm.RelatedOperation,jm.MtlSeq,jm.PartNum,jm.RequiredQty-jm.IssuedQty Qty,jm.IUM,jm.WarehouseCode,(select top(1) BinNum from Erp.PartBin where Company=jm.Company and WarehouseCode=jm.WarehouseCode and PartNum=jm.PartNum) BinNum,rg.InputWhse,rg.InputBinNum,isnull((select top(1) OnhandQty from Erp.PartBin where Company=jm.Company and WarehouseCode=jm.WarehouseCode and PartNum=jm.PartNum),0) OnhandQty from Erp.JobMtl jm inner join Part p on jm.Company=p.Company and jm.PartNum=p.PartNum inner join erp.JobOpDtl jod on jm.Company=jod.Company and jm.JobNum=jod.JobNum and jm.AssemblySeq=jod.AssemblySeq and jm.RelatedOperation=jod.OprSeq inner join erp.ResourceGroup rg on jod.Company=rg.Company and jod.ResourceGrpID=rg.ResourceGrpID where jm.Company='" + companyId + "' and jm.JobNum='" + jobNum + "' and jm.AssemblySeq='" + assemblySeq + "' and jm.RelatedOperation='" + oprSeq + "' and jm.PartNum='" + partNum + "'";
            }
            else if (tracklots.ToLower() == "false" && !string.IsNullOrEmpty(lotNum))
            {
                return "false|该物料未使用批次追踪.";
            }
            else
            {
                querysql = "select jm.JobNum,jm.AssemblySeq,jm.RelatedOperation,jm.MtlSeq,jm.PartNum,jm.RequiredQty-jm.IssuedQty Qty,jm.IUM,jm.WarehouseCode,(select top(1) BinNum from Erp.PartBin where Company=jm.Company and WarehouseCode=jm.WarehouseCode and PartNum=jm.PartNum) BinNum,rg.InputWhse,rg.InputBinNum,isnull((select top(1) OnhandQty from Erp.PartBin where Company=jm.Company and WarehouseCode=jm.WarehouseCode and PartNum=jm.PartNum),0) OnhandQty from Erp.JobMtl jm inner join Part p on jm.Company=p.Company and jm.PartNum=p.PartNum inner join erp.JobOpDtl jod on jm.Company=jod.Company and jm.JobNum=jod.JobNum and jm.AssemblySeq=jod.AssemblySeq and jm.RelatedOperation=jod.OprSeq inner join erp.ResourceGroup rg on jod.Company=rg.Company and jod.ResourceGrpID=rg.ResourceGrpID where jm.Company='" + companyId + "' and jm.JobNum='" + jobNum + "' and jm.AssemblySeq='" + assemblySeq + "' and jm.RelatedOperation='" + oprSeq + "' and jm.PartNum='" + partNum + "'";
            }
            mtlSeq = Convert.ToInt32(QueryERP("select MtlSeq from Erp.JobMtl jm where jm.Company='" + companyId + "' and jm.JobNum='" + jobNum + "' and jm.AssemblySeq='" + assemblySeq + "' and jm.RelatedOperation='" + oprSeq + "' and jm.PartNum='" + partNum + "' "));
            DataTable dt = GetDataByERP(querysql);
            //DataTable lotdt = GetDataByERP("select LotNum,OnhandQty from Erp.PartBin where Company='" + companyId + "' and PartNum='" + dt.Rows[i]["PartNum"].ToString() + "' order by LotNum");
            //IssueReturnWithLot(dt.Rows[i]["JobNum"].ToString(), Convert.ToInt32(dt.Rows[i]["AssemblySeq"]), Convert.ToInt32(dt.Rows[i]["RelatedOperation"]), Convert.ToInt32(dt.Rows[i]["MtlSeq"]), dt.Rows[i]["PartNum"].ToString(), Convert.ToDecimal(dt.Rows[i]["Qty"]), DateTime.Now, dt.Rows[i]["IUM"].ToString(), dt.Rows[i]["BackflushWhse"].ToString(), dt.Rows[i]["BackflushBinNum"].ToString(), dt.Rows[i]["InputWhse"].ToString(), dt.Rows[i]["InputBinNum"].ToString(), lotdt.Rows[0]["LotNum"].ToString(), Convert.ToDecimal(lotdt.Rows[0]["OnhandQty"]), tranReference, lotdt, 0, companyId);
            //
            if (dt != null && dt.Rows.Count > 0)
            {
                if (tranQty > Convert.ToDecimal(dt.Rows[0]["OnhandQty"]))
                {
                    return "false|发料数量大于库存数量.";
                }
                else
                {
                    if (tracklots.ToLower() == "true" && string.IsNullOrEmpty(lotNum))
                    {
                        DataTable lotdt = GetDataByERP("select LotNum,OnhandQty,BinNum from Erp.PartBin where Company='" + companyId + "' and PartNum='" + partNum + "' order by LotNum");
                        bool index = IssueReturnWithLotbak(jobNum, assemblySeq, oprSeq, mtlSeq, partNum, tranQty, tranDate, dt.Rows[0]["IUM"].ToString(), dt.Rows[0]["WarehouseCode"].ToString(), lotdt.Rows[0]["BinNum"].ToString(), dt.Rows[0]["InputWhse"].ToString(), dt.Rows[0]["InputBinNum"].ToString(), lotdt.Rows[0]["LotNum"].ToString(), Convert.ToDecimal(lotdt.Rows[0]["OnhandQty"]), tranReference, lotdt, 0, companyId);
                        if (index)
                        {
                            return "true";
                        }
                        else
                        {
                            return "false|发料出错,请检查erp数据.";
                        }
                    }
                    else
                    {
                        bool index = IssueReturnSTKMTLbak(jobNum, assemblySeq, oprSeq, mtlSeq, partNum, tranQty, tranDate, dt.Rows[0]["IUM"].ToString(), dt.Rows[0]["WarehouseCode"].ToString(), dt.Rows[0]["BinNum"].ToString(), dt.Rows[0]["InputWhse"].ToString(), dt.Rows[0]["InputBinNum"].ToString(), lotNum, tranReference, companyId);
                        if (index)
                        {
                            return "true";
                        }
                        else
                        {
                            return "false|发料出错,请检查erp数据.";
                        }
                    }
                }
            }
            else
            {
                return "false|查询物料无库存或信息出错,请检查erp数据.";
            }
        }

        public string OneIssueReturnSTKUKN(string partNum, decimal tranQty, DateTime tranDate, string lotNum, string reasonCode, string companyId)
        {
            UpdateAWS("insert into BO_ERPRECORD(UD04,UD05,UD06,UD07,UD08,UD09,UD10) values('" + reasonCode + "','" + partNum + "','" + tranQty + "','" + tranDate + "','" + lotNum + "','" + companyId + "','单个发料')");
            string tracklots = QueryERP("select tracklots from Erp.Part where Company='" + companyId + "' and PartNum='" + partNum + "'");
            if (tracklots.ToLower() == "true" && string.IsNullOrEmpty(lotNum))
            {
                return "false|该物料使用批次追踪,批次号不能为空.";
            }
            else if (tracklots.ToLower() == "false" && !string.IsNullOrEmpty(lotNum))
            {
                return "false|该物料未使用批次追踪.";
            }
            string[] strlist = reasonCode.Split('%');
            if (strlist.Length != 2)
            {
                return "false|杂项出库失败";

            }
            DataTable dt = GetDataByERP("select top(1) WarehouseCode,BinNum,OnhandQty,DimCode from erp.PartBin where Company='" + companyId + "' and PartNum='" + partNum + "' and LotNum='" + lotNum + "' and WarehouseCode='" + strlist[0] + "' and BinNum='" + strlist[1] + "' ");
            if (dt != null && dt.Rows.Count > 0)
            {
                bool index = IssueReturnSTKUKN(partNum, tranQty, tranDate, dt.Rows[0]["DimCode"].ToString(), strlist[0], strlist[1], lotNum, "杂项发料", "A07", companyId);
                if (index)
                {
                    return "1|处理成功";
                }
                else
                {
                    return "false|发料出错,请检查erp数据.";
                }
            }
            else
            {
                return "false|查询物料无库存或信息出错,请检查erp数据.";
            }
        }


        //客户出货
        public string CustShip(string json, string companyId)
        {
            UpdateAWS("insert into BO_ERPRECORD(UD01,UD02,UD10) values('" + json + "','" + companyId + "','客户出货')");
            DataTable dt = ToDataTable(json);
            if (dt == null || dt.Rows.Count == 0)
            {
                return "false|没有数据传入.";
            }
            WriteGetNewLaborInERPTxt("", "", json, "出货", companyId);
            //string result=ErpLogin/();
            //if(result!="true")
            //{
            //    return "false|"+result;
            //}
            Session EpicorSession = ErpLoginbak();
            if (EpicorSession == null)
            {
                return "-1|erp用户数不够，请稍候再试. ERR:CustShip";
            }
            EpicorSession.CompanyID = companyId;
            CustShipImpl adapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<CustShipImpl>(EpicorSession, ImplBase<Erp.Contracts.CustShipSvcContract>.UriPath);
            CustShipDataSet ds = new CustShipDataSet();
            adapter.GetNewShipHead(ds);
            string creditMessage = "";
            adapter.GetHeadOrderInfo(Convert.ToInt32(dt.Rows[0]["OrderNum"]), out creditMessage, ds);
            adapter.Update(ds);
            int packNum = System.Convert.ToInt32(ds.Tables["ShipHead"].Rows[ds.Tables["ShipHead"].Rows.Count - 1]["PackNum"]);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                adapter.GetNewOrdrShipDtl(ds, packNum, 0);
                adapter.GetOrderInfo(Convert.ToInt32(dt.Rows[0]["OrderNum"]), out creditMessage, ds);
                adapter.GetOrderLineInfo(ds, 0, Convert.ToInt32(dt.Rows[i]["OrderLine"]), dt.Rows[i]["PartNum"].ToString());
                adapter.GetOrderRelInfo(ds, 0, Convert.ToInt32(dt.Rows[i]["OrderRelNum"]), true);
                adapter.GetQtyInfo(ds, 0, Convert.ToDecimal(dt.Rows[i]["ShipQty"]), 0);
                int checkint = 0; string checkstr = ""; bool checkbool = false;
                ds.Tables["ShipDtl"].Rows[ds.Tables["ShipDtl"].Rows.Count - 1]["LotNum"] = dt.Rows[i]["LotNum"].ToString();
                ds.Tables["ShipDtl"].Rows[ds.Tables["ShipDtl"].Rows.Count - 1]["XPartNum"] = dt.Rows[i]["XPartNum"].ToString();
                ds.Tables["ShipDtl"].Rows[ds.Tables["ShipDtl"].Rows.Count - 1]["WarehouseCode"] = dt.Rows[i]["WarehouseCode"].ToString();
                ds.Tables["ShipDtl"].Rows[ds.Tables["ShipDtl"].Rows.Count - 1]["BinNum"] = dt.Rows[i]["BinNum"].ToString();
                adapter.CheckPCBinOutLocation(ds, out checkint, out checkbool, out checkstr);
                adapter.Update(ds);
            }
            return "true|出货单" + packNum.ToString();
        }

        //(Description = "明细行的集合方式：行与行之间使用&&分隔，各字段间使用**分隔，字段和值使用@#分隔")
        public string GetNewLabor(string employeenum, string laborHeaderKeyAndValArrayStr, string laborDetailKeyAndValArrayStr, string companyId)
        {
            //外协报工直接返回，不进行报工-------------20160824--begint--green
            if (employeenum.ToLower().Trim() == "db")
            { return "1|ok"; }
            try
            {
                string[] jobdata = GetJobData(laborDetailKeyAndValArrayStr);
                // 此行为原始查询的SQL语句
                // DataTable dt = GetDataByERP("select jm.JobNum,jm.AssemblySeq,jm.RelatedOperation,jm.MtlSeq,jm.PartNum,jm.RequiredQty-jm.IssuedQty Qty,jm.IUM,jm.WarehouseCode BackflushWhse,(select top(1) BinNum from erp.PartBin where Company=jm.Company and WarehouseCode=jm.WarehouseCode and PartNum=jm.PartNum and OnhandQty>=jm.RequiredQty-jm.IssuedQty) BackflushBinNum,rg.InputWhse,rg.InputBinNum,isnull((select OnhandQty from Erp.PartBin where Company=jm.Company and WarehouseCode=rg.BackflushWhse and BinNum=rg.BackflushBinNum and PartNum=jm.PartNum),0) OnhandQty from Erp.JobMtl jm inner join Part p on jm.Company=p.Company and jm.PartNum=p.PartNum and p.CheckBox01=1 and p.TrackLots=1 inner join erp.JobOpDtl jod on jm.Company=jod.Company and jm.JobNum=jod.JobNum and jm.AssemblySeq=jod.AssemblySeq and jm.RelatedOperation=jod.OprSeq inner join erp.ResourceGroup rg on jod.Company=rg.Company and jod.ResourceGrpID=rg.ResourceGrpID where jm.Company='" + companyId + "' and jm.JobNum='" + jobdata[0] + "' and jm.AssemblySeq='" + jobdata[1] + "' and jm.RelatedOperation='" + jobdata[2] + "' ");
                //更改代码 by HJY 20180919 
                DataTable dt = GetDataByERP(@"select jm.JobNum,jm.AssemblySeq,jm.RelatedOperation,jm.MtlSeq,jm.PartNum,jm.RequiredQty-jm.IssuedQty Qty,jm.IUM,jm.WarehouseCode BackflushWhse,
(select top(1) BinNum from erp.PartBin where Company=jm.Company 
and WarehouseCode=jm.WarehouseCode
 and PartNum=jm.PartNum and OnhandQty>=jm.RequiredQty-jm.IssuedQty) BackflushBinNum,
rg.InputWhse,rg.InputBinNum,
isnull( (select top(1) OnHandQty from erp.PartBin where Company=jm.Company 
and WarehouseCode='WIP'
 and PartNum=jm.PartNum and OnhandQty>=jm.RequiredQty-jm.IssuedQty) ,0)OnHandQty
--isnull(
--		(select OnhandQty from Erp.PartBin where Company=jm.Company and WarehouseCode=rg.BackflushWhse and BinNum=rg.BackflushBinNum and PartNum=jm.PartNum),0) OnhandQty 
		
		from Erp.JobMtl jm 
		inner join Part p on 
		jm.Company=p.Company 
		and jm.PartNum=p.PartNum 
		and p.CheckBox01=1
		and p.TrackLots=1 
		inner join erp.JobOpDtl jod on 
		jm.Company=jod.Company 
		and jm.JobNum=jod.JobNum 
		and jm.AssemblySeq=jod.AssemblySeq 
		and jm.RelatedOperation=jod.OprSeq 
		inner join erp.ResourceGroup rg on 
		jod.Company=rg.Company 
		and jod.ResourceGrpID=rg.ResourceGrpID 
 where jm.Company='" + companyId + "' and jm.JobNum='" + jobdata[0] + "' and jm.AssemblySeq='" + jobdata[1] + "' and jm.RelatedOperation='" + jobdata[2] + "' ");


                
                employeenum = QueryERP("select top(1) ResourceGrpID from Erp.JobOpDtl where Company='" + companyId + "' and JobNum='" + jobdata[0] + "' and AssemblySeq='" + jobdata[1] + "' and OprSeq='" + jobdata[2] + "'");
                string checkemp = QueryERP("select  EmpID　from erp.EmpBasic where Company='" + companyId + "' and EmpID='" + employeenum + "'");
                string plantid = QueryERP("select Plant from erp.JobHead where Company='" + companyId + "' and JobNum='" + jobdata[0] + "'");
                if (string.IsNullOrEmpty(checkemp))
                {
                    return "-1:ERP不存在员工号" + employeenum;
                }
                if (string.IsNullOrEmpty(plantid))
                {
                    return "-1:ERP工单不存在" + employeenum;
                }
                string warehousecode = QueryERP("select top(1) WarehouseCode from erp.Warehse where WarehouseCode like '%WIP' and Company='" + companyId + "' and Plant='" + plantid + "'");
                if (string.IsNullOrEmpty(warehousecode))
                {
                    return "-1:ERP的WIP仓库不存在" + employeenum;
                }
                string erormessage = "";
                if (dt != null && dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        //if (Convert.ToDecimal(dt.Rows[i]["OnhandQty"]) - Convert.ToDecimal(dt.Rows[i]["Qty"]) < 0)
                        //{
                        //    erormessage = erormessage + dt.Rows[i]["PartNum"].ToString() + "库位库存不足;";
                        //}
                        //------------20180919 HJY   
                        //if (string.IsNullOrEmpty(dt.Rows[i]["BackflushBinNum"].ToString())) 
                        //{
                        //    erormessage = erormessage + dt.Rows[i]["PartNum"].ToString() + "库位库存不足;";
                        //}
                        if (string.IsNullOrEmpty(dt.Rows[i]["InputBinNum"].ToString())) 
                        {
                            erormessage = erormessage + dt.Rows[i]["PartNum"].ToString() + "库位库存不足;";
                        }
                        //------------20180919 HJY   END

                    }
                }
                if (erormessage.Length > 0)
                {
                    if (erormessage.Length > 500)
                    {
                        return "-1:" + erormessage.Substring(0, 500);
                    }
                    else
                    {
                        return "-1:" + erormessage;
                    }
                }
                string tranReference = "化学物品发料.";
                Session EpicorSession = ErpLoginbak();
                if (EpicorSession == null)
                {
                    return "-1|erp用户数不够，请稍候再试.ERR:GetNewLabor";
                }
                EpicorSession.CompanyID = companyId;
                EpicorSession.PlantID = plantid;
                IssueReturnImpl issueadapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<IssueReturnImpl>(EpicorSession, ImplBase<Erp.Contracts.IssueReturnSvcContract>.UriPath);
                LaborImpl adapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<LaborImpl>(EpicorSession, ImplBase<Erp.Contracts.LaborSvcContract>.UriPath);
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    DataTable lotdt = GetDataByERP("select LotNum,OnhandQty from Erp.PartBin where Company='" + companyId + "' and PartNum='" + dt.Rows[i]["PartNum"].ToString() + "' and WarehouseCode='" + warehousecode + "' and BinNum='01' order by LotNum");
                    if (lotdt != null && lotdt.Rows.Count > 0)
                    {
                        IssueReturnWithLot(dt.Rows[i]["JobNum"].ToString(), Convert.ToInt32(dt.Rows[i]["AssemblySeq"]), Convert.ToInt32(dt.Rows[i]["RelatedOperation"]), Convert.ToInt32(dt.Rows[i]["MtlSeq"]), dt.Rows[i]["PartNum"].ToString(), Convert.ToDecimal(dt.Rows[i]["Qty"]), DateTime.Now, dt.Rows[i]["IUM"].ToString(), warehousecode, "01", dt.Rows[i]["InputWhse"].ToString(), dt.Rows[i]["InputBinNum"].ToString(), lotdt.Rows[0]["LotNum"].ToString(), Convert.ToDecimal(lotdt.Rows[0]["OnhandQty"]), tranReference, lotdt, 0, companyId, issueadapter);
                    }
                    //DataTable lotdt = GetDataByERP("select LotNum,OnhandQty from Erp.PartBin where Company='" + companyId + "' and PartNum='" + dt.Rows[i]["PartNum"].ToString() + "' and WarehouseCode='" + dt.Rows[i]["BackflushWhse"].ToString() + "' and BinNum='" + dt.Rows[i]["BackflushBinNum"].ToString() + "' order by LotNum");

                }
                int laborHedSeq = 0;
                int laborDtlSeq = 0;
                LaborDataSet ds = new LaborDataSet();
                try
                {
                    //LaborDataSet ds = new LaborDataSet();
                    adapter.GetNewLaborHed1(ds, employeenum, false, DateTime.Now.Date);
                    Regex rg = new Regex(Commom.splitArrayLineStr);
                    string[] keyAndValArray = rg.Split(laborHeaderKeyAndValArrayStr);//字段和值的集合
                    string filedName = "";
                    object value;
                    string tableName = "LaborHed";
                    foreach (string str in keyAndValArray)
                    {
                        if (str == "") continue;
                        string[] strArr = Regex.Split(str, Commom.splitArrayKeyAndValStr, RegexOptions.IgnoreCase);
                        if (strArr.Length != 2)
                        {
                            try
                            {
                                EpicorSession.Dispose();
                            }
                            catch { }
                            if (erormessage.Length > 500)
                            {
                                return "-1:" + erormessage.Substring(0, 500);
                            }
                            else
                            {
                                return "-1:" + erormessage;
                            }

                            //throw new Exception("字符串传入不正确");
                        }
                        filedName = strArr[0];
                        value = strArr[1];
                        ds.Tables[tableName].Rows[0][filedName] = value;
                    }
                    adapter.Update(ds);
                    laborHedSeq = System.Convert.ToInt32(ds.Tables["LaborHed"].Rows[ds.Tables["LaborHed"].Rows.Count - 1]["LaborHedSeq"]);
                    tableName = "LaborDtl";

                    Regex rg1 = new Regex(Commom.splitArrayLine);
                    string[] poDetailKeyAndValArray = rg1.Split(laborDetailKeyAndValArrayStr);//字段和值的集合
                    string fieldAndVal = "";
                    int lastRowIndex = 0;
                    for (int i = 0; i < poDetailKeyAndValArray.Length; i++)
                    {
                        if (poDetailKeyAndValArray[i] == "") continue;
                        adapter.GetNewLaborDtlWithHdr(ds, DateTime.Now.Date, 0, DateTime.Now.Date, 0, laborHedSeq);
                        adapter.DefaultLaborType(ds, "P");
                        lastRowIndex = ds.Tables["LaborDtl"].Rows.Count - 1;

                        keyAndValArray = Regex.Split(poDetailKeyAndValArray[i], Commom.splitArrayLineStr, RegexOptions.IgnoreCase);//字段和值的集合

                        for (int j = 0; j < keyAndValArray.Length; j++)
                        {
                            fieldAndVal = keyAndValArray[j];
                            if (fieldAndVal == "") continue;

                            string[] strArr = Regex.Split(fieldAndVal, Commom.splitArrayKeyAndValStr, RegexOptions.IgnoreCase);
                            if (strArr.Length != 2)
                            {
                                EpicorSession.Dispose();
                                return "-1:传参不对.";

                                //throw new Exception("字符串传入不正确");
                            }
                            filedName = strArr[0];
                            value = strArr[1];
                            if (filedName == "JobNum")
                            {
                                adapter.DefaultJobNum(ds, value.ToString());
                            }
                            if (filedName == "AssemblySeq")
                            {
                                adapter.DefaultAssemblySeq(ds, Convert.ToInt32(value));
                            }
                            if (filedName == "OprSeq")
                            {
                                String message = "";
                                adapter.DefaultOprSeq(ds, Convert.ToInt32(value), out message);
                            }
                            if (filedName == "LaborQty")
                            {
                                String message = "";
                                adapter.DefaultLaborQty(ds, Convert.ToDecimal(value), out message);
                            }
                            if (filedName == "DiscrpRsnCode")
                            {
                                if (!string.IsNullOrEmpty(value.ToString()))
                                {
                                    //adapter.DefaultDiscrpRsnCode(ds, value.ToString());
                                }
                            }
                            if (filedName == "DiscrpRsnCodeDesc")
                            {
                                if (!string.IsNullOrEmpty(value.ToString()))
                                {
                                    //adapter.DefaultDiscrpRsnCode(ds, value.ToString());
                                    ds.Tables[tableName].Rows[lastRowIndex]["Shortchar04"] = value;
                                }
                            }
                            if (filedName == "DiscrepQty")
                            {
                                //ds.Tables[tableName].Rows[lastRowIndex][filedName] = value;

                                //----20160112---Jeff 添加不合格数量写回laborDtl表--Begin
                                ds.Tables[tableName].Rows[lastRowIndex]["Number10"] = value;
                                //----20160112---Jeff 添加不合格数量写回laborDtl表--End
                            }
                            if (filedName == "ReportUserName")
                            {
                                ds.Tables[tableName].Rows[lastRowIndex]["Shortchar05"] = value;

                            }
                            if (filedName == "CheckSite")
                            {
                                ds.Tables[tableName].Rows[lastRowIndex]["Shortchar03"] = value;

                            }
                            if (filedName == "SendDate")
                            {
                                ds.Tables[tableName].Rows[lastRowIndex]["Date02"] = value;

                            }
                            if (filedName == "SendTime")
                            {

                                ds.Tables[tableName].Rows[lastRowIndex]["Shortchar02"] = value;

                            }
                            if (filedName == "LaborHrs")
                            {
                                ds.Tables[tableName].Rows[lastRowIndex]["LaborHrs"] = value;

                            }
                            if (filedName == "BurdenHrs")
                            {

                                ds.Tables[tableName].Rows[lastRowIndex]["BurdenHrs"] = value;

                            }
                        }

                        ds.Tables[tableName].Rows[lastRowIndex]["TimeStatus"] = "A";
                        ds.Tables[tableName].Rows[lastRowIndex]["ScrapQty"] = "0";

                        //----20160112---Jeff 添加时间写回laborDtl表--Begin
                        ds.Tables[tableName].Rows[lastRowIndex]["Shortchar01"] = DateTime.Now;
                        ds.Tables[tableName].Rows[lastRowIndex]["Date01"] = DateTime.Now.Date;
                        //----20160112---Jeff 添加时间写回laborDtl表--End

                        string cMessageText = "";
                        try
                        {
                            adapter.CheckWarnings(ds, out cMessageText);
                            adapter.Update(ds);
                        }
                        catch (Exception ex)
                        {
                            EpicorSession.Dispose();
                            return "-1:" + ex.Message.ToString();
                        }
                        //string oumsg = "";
                        try
                        {
                            adapter.SubmitForApproval(ds, false, out cMessageText);
                        }
                        catch (Exception ex)
                        {
                            EpicorSession.Dispose();
                            return "1|ok";
                            //return "0|" + exerror.Message.ToString();
                        }

                    }
                    laborDtlSeq = Convert.ToInt32(ds.Tables["LaborDtl"].Rows[ds.Tables["LaborDtl"].Rows.Count - 1]["LaborDtlSeq"]);

                    try
                    {
                        //EpicorSessionManager.EpicorSession.Dispose();
                        EpicorSession.Dispose();
                    }
                    catch { }
                    if (laborHedSeq > 0)
                    {
                        return "1|ok";
                    }
                    else
                    {
                        if (erormessage.Length > 500)
                        {
                            return "-1:" + erormessage.Substring(0, 500);
                        }
                        else
                        {
                            return "-1:" + erormessage;
                        }
                    }

                }
                catch (Exception ex)
                {
                    try
                    {
                        EpicorSession.Dispose();
                    }
                    catch { }
                    return "-1:" + ex.Message.ToString();
                }
            }
            catch (Exception ex)
            {
                return "-1:" + ex.Message.ToString();
            }
        }

        public string GetNewLaborbak(string employeenum, string laborHeaderKeyAndValArrayStr, string laborDetailKeyAndValArrayStr, string companyId)
        {
            //外协报工直接返回，不进行报工-------------20160824--begint--green
            if (employeenum.ToLower().Trim() == "db")
            { return "1|ok"; }
            try
            {
                string[] jobdata = GetJobData(laborDetailKeyAndValArrayStr);
                DataTable dt = GetDataByERP("select jm.JobNum,jm.AssemblySeq,jm.RelatedOperation,jm.MtlSeq,jm.PartNum,jm.RequiredQty-jm.IssuedQty Qty,jm.IUM,rg.BackflushWhse,rg.BackflushBinNum,rg.InputWhse,rg.InputBinNum,isnull((select OnhandQty from Erp.PartBin where Company=jm.Company and WarehouseCode=rg.BackflushWhse and BinNum=rg.BackflushBinNum and PartNum=jm.PartNum),0) OnhandQty from Erp.JobMtl jm inner join Part p on jm.Company=p.Company and jm.PartNum=p.PartNum and p.CheckBox01=1 and p.ClassID='08' and p.TrackLots=1 inner join erp.JobOpDtl jod on jm.Company=jod.Company and jm.JobNum=jod.JobNum and jm.AssemblySeq=jod.AssemblySeq and jm.RelatedOperation=jod.OprSeq inner join erp.ResourceGroup rg on jod.Company=rg.Company and jod.ResourceGrpID=rg.ResourceGrpID where jm.Company='" + companyId + "' and jm.JobNum='" + jobdata[0] + "' and jm.AssemblySeq='" + jobdata[1] + "' and jm.RelatedOperation='" + jobdata[2] + "' ");
                employeenum = QueryERP("select top(1) ResourceGrpID from Erp.JobOpDtl where Company='" + companyId + "' and JobNum='" + jobdata[0] + "' and AssemblySeq='" + jobdata[1] + "' and OprSeq='" + jobdata[2] + "'");
                string checkemp = QueryERP("select  EmpID　from erp.EmpBasic where Company='" + companyId + "' and EmpID='" + employeenum + "'");
                if (string.IsNullOrEmpty(checkemp))
                {
                    return "-1:ERP不存在员工号" + employeenum;
                }
                string erormessage = "";
                if (dt != null && dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        if (Convert.ToDecimal(dt.Rows[i]["OnhandQty"]) - Convert.ToDecimal(dt.Rows[i]["Qty"]) < 0)
                        {
                            erormessage = erormessage + dt.Rows[i]["PartNum"].ToString() + "反冲库位库存不足;";
                        }
                    }
                }
                if (erormessage.Length > 0)
                {
                    if (erormessage.Length > 500)
                    {
                        return "-1:" + erormessage.Substring(0, 500);
                    }
                    else
                    {
                        return "-1:" + erormessage;
                    }
                }
                //WriteGetNewLaborInERPTxt(employeenum, laborHeaderKeyAndValArrayStr, laborDetailKeyAndValArrayStr, "报工", companyId);
                string tranReference = "化学物品发料.";
                Session EpicorSession = ErpLoginbak();
                //string result = ErpLogin/();
                if (EpicorSession == null)
                {
                    return "-1|erp用户数不够，请稍候再试.ERR:GetNewLaborbak";
                }
                //WriteGetNewLaborInERPTxt("", EpicorSession.SessionID.ToString(), "", "sessionid", "");
                EpicorSession.CompanyID = companyId;
                IssueReturnImpl issueadapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<IssueReturnImpl>(EpicorSession, ImplBase<Erp.Contracts.IssueReturnSvcContract>.UriPath);
                LaborImpl adapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<LaborImpl>(EpicorSession, ImplBase<Erp.Contracts.LaborSvcContract>.UriPath);
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    DataTable lotdt = GetDataByERP("select LotNum,OnhandQty from Erp.PartBin where Company='" + companyId + "' and PartNum='" + dt.Rows[i]["PartNum"].ToString() + "' order by LotNum");
                    IssueReturnWithLot(dt.Rows[i]["JobNum"].ToString(), Convert.ToInt32(dt.Rows[i]["AssemblySeq"]), Convert.ToInt32(dt.Rows[i]["RelatedOperation"]), Convert.ToInt32(dt.Rows[i]["MtlSeq"]), dt.Rows[i]["PartNum"].ToString(), Convert.ToDecimal(dt.Rows[i]["Qty"]), DateTime.Now, dt.Rows[i]["IUM"].ToString(), dt.Rows[i]["BackflushWhse"].ToString(), dt.Rows[i]["BackflushBinNum"].ToString(), dt.Rows[i]["InputWhse"].ToString(), dt.Rows[i]["InputBinNum"].ToString(), lotdt.Rows[0]["LotNum"].ToString(), Convert.ToDecimal(lotdt.Rows[0]["OnhandQty"]), tranReference, lotdt, 0, companyId, issueadapter);

                }
                //WriteGetNewLaborInERPTxt("", EpicorSession.SessionID.ToString(), "0", "sessionid", "");
                //try
                //{
                //    EpicorSession.Dispose();
                //}
                //catch
                //{ }
                int laborHedSeq = 0;
                int laborDtlSeq = 0;
                LaborDataSet ds = new LaborDataSet();
                try
                {
                    //LaborDataSet ds = new LaborDataSet();
                    adapter.GetNewLaborHed1(ds, employeenum, false, DateTime.Now.Date);
                    Regex rg = new Regex(Commom.splitArrayLineStr);
                    string[] keyAndValArray = rg.Split(laborHeaderKeyAndValArrayStr);//字段和值的集合
                    string filedName = "";
                    object value;
                    string tableName = "LaborHed";
                    foreach (string str in keyAndValArray)
                    {
                        if (str == "") continue;
                        string[] strArr = Regex.Split(str, Commom.splitArrayKeyAndValStr, RegexOptions.IgnoreCase);
                        if (strArr.Length != 2)
                        {
                            try
                            {
                                EpicorSession.Dispose();
                            }
                            catch { }
                            if (erormessage.Length > 500)
                            {
                                return "-1:" + erormessage.Substring(0, 500);
                            }
                            else
                            {
                                return "-1:" + erormessage;
                            }

                            //throw new Exception("字符串传入不正确");
                        }
                        filedName = strArr[0];
                        value = strArr[1];
                        ds.Tables[tableName].Rows[0][filedName] = value;
                    }
                    //WriteGetNewLaborInERPTxt("", EpicorSession.SessionID.ToString(), "0.1", "sessionid", "");
                    adapter.Update(ds);
                    //WriteGetNewLaborInERPTxt("", EpicorSession.SessionID.ToString(), "0.2", "sessionid", "");
                    laborHedSeq = System.Convert.ToInt32(ds.Tables["LaborHed"].Rows[ds.Tables["LaborHed"].Rows.Count - 1]["LaborHedSeq"]);
                    //WriteGetNewLaborInERPTxt(laborHedSeq.ToString(), EpicorSession.SessionID.ToString(), "", "sessionid", "");
                    tableName = "LaborDtl";

                    Regex rg1 = new Regex(Commom.splitArrayLine);
                    string[] poDetailKeyAndValArray = rg1.Split(laborDetailKeyAndValArrayStr);//字段和值的集合
                    string fieldAndVal = "";
                    int lastRowIndex = 0;
                    for (int i = 0; i < poDetailKeyAndValArray.Length; i++)
                    {
                        if (poDetailKeyAndValArray[i] == "") continue;
                        adapter.GetNewLaborDtlWithHdr(ds, DateTime.Now.Date, 0, DateTime.Now.Date, 0, laborHedSeq);
                        adapter.DefaultLaborType(ds, "P");
                        lastRowIndex = ds.Tables["LaborDtl"].Rows.Count - 1;

                        keyAndValArray = Regex.Split(poDetailKeyAndValArray[i], Commom.splitArrayLineStr, RegexOptions.IgnoreCase);//字段和值的集合

                        for (int j = 0; j < keyAndValArray.Length; j++)
                        {
                            fieldAndVal = keyAndValArray[j];
                            if (fieldAndVal == "") continue;

                            string[] strArr = Regex.Split(fieldAndVal, Commom.splitArrayKeyAndValStr, RegexOptions.IgnoreCase);
                            if (strArr.Length != 2)
                            {
                                EpicorSession.Dispose();
                                return "-1:传参不对.";

                                //throw new Exception("字符串传入不正确");
                            }
                            filedName = strArr[0];
                            value = strArr[1];
                            if (filedName == "JobNum")
                            {
                                adapter.DefaultJobNum(ds, value.ToString());
                            }
                            if (filedName == "AssemblySeq")
                            {
                                adapter.DefaultAssemblySeq(ds, Convert.ToInt32(value));
                            }
                            if (filedName == "OprSeq")
                            {
                                String message = "";
                                adapter.DefaultOprSeq(ds, Convert.ToInt32(value), out message);
                            }
                            if (filedName == "LaborQty")
                            {
                                String message = "";
                                adapter.DefaultLaborQty(ds, Convert.ToDecimal(value), out message);
                            }
                            if (filedName == "DiscrpRsnCode")
                            {
                                if (!string.IsNullOrEmpty(value.ToString()))
                                {
                                    //adapter.DefaultDiscrpRsnCode(ds, value.ToString());
                                }
                            }
                            if (filedName == "DiscrpRsnCodeDesc")
                            {
                                if (!string.IsNullOrEmpty(value.ToString()))
                                {
                                    //adapter.DefaultDiscrpRsnCode(ds, value.ToString());
                                    ds.Tables[tableName].Rows[lastRowIndex]["Shortchar04"] = value;
                                }
                            }
                            if (filedName == "DiscrepQty")
                            {
                                //ds.Tables[tableName].Rows[lastRowIndex][filedName] = value;

                                //----20160112---Jeff 添加不合格数量写回laborDtl表--Begin
                                ds.Tables[tableName].Rows[lastRowIndex]["Number10"] = value;
                                //----20160112---Jeff 添加不合格数量写回laborDtl表--End
                            }
                            if (filedName == "ReportUserName")
                            {
                                ds.Tables[tableName].Rows[lastRowIndex]["Shortchar05"] = value;

                            }
                            if (filedName == "CheckSite")
                            {
                                ds.Tables[tableName].Rows[lastRowIndex]["Shortchar03"] = value;

                            }
                            if (filedName == "SendDate")
                            {
                                ds.Tables[tableName].Rows[lastRowIndex]["Date02"] = value;

                            }
                            if (filedName == "SendTime")
                            {

                                ds.Tables[tableName].Rows[lastRowIndex]["Shortchar02"] = value;

                            }
                            if (filedName == "LaborHrs")
                            {
                                ds.Tables[tableName].Rows[lastRowIndex]["LaborHrs"] = value;

                            }
                            if (filedName == "BurdenHrs")
                            {

                                ds.Tables[tableName].Rows[lastRowIndex]["BurdenHrs"] = value;

                            }
                        }

                        ds.Tables[tableName].Rows[lastRowIndex]["TimeStatus"] = "A";
                        ds.Tables[tableName].Rows[lastRowIndex]["ScrapQty"] = "0";

                        //----20160112---Jeff 添加时间写回laborDtl表--Begin
                        ds.Tables[tableName].Rows[lastRowIndex]["Shortchar01"] = DateTime.Now;
                        ds.Tables[tableName].Rows[lastRowIndex]["Date01"] = DateTime.Now.Date;
                        //----20160112---Jeff 添加时间写回laborDtl表--End

                        string cMessageText = "";
                        //WriteGetNewLaborInERPTxt(laborHedSeq.ToString(), EpicorSession.SessionID.ToString(), "1", "sessionid", "");
                        try
                        {
                            adapter.CheckWarnings(ds, out cMessageText);
                            adapter.Update(ds);
                        }
                        catch (Exception ex)
                        {
                            EpicorSession.Dispose();
                            return "-1:" + ex.Message.ToString();
                        }
                        //WriteGetNewLaborInERPTxt(laborHedSeq.ToString(), EpicorSession.SessionID.ToString(), "2", "sessionid", "");
                        //string oumsg = "";
                        try
                        {
                            adapter.SubmitForApproval(ds, false, out cMessageText);
                        }
                        catch (Exception ex)
                        {
                            EpicorSession.Dispose();
                            return "1|ok";
                            //return "0|" + exerror.Message.ToString();
                        }

                    }
                    laborDtlSeq = Convert.ToInt32(ds.Tables["LaborDtl"].Rows[ds.Tables["LaborDtl"].Rows.Count - 1]["LaborDtlSeq"]);

                    try
                    {
                        //EpicorSession.Dispose();
                        WriteGetNewLaborInERPTxt(laborHedSeq.ToString(), EpicorSession.SessionID.ToString(), "3", "sessionid", "");
                        EpicorSession.Dispose();
                    }
                    catch { }
                    if (laborHedSeq > 0)
                    {
                        return "1|ok";
                    }
                    else
                    {
                        if (erormessage.Length > 500)
                        {
                            return "-1:" + erormessage.Substring(0, 500);
                        }
                        else
                        {
                            return "-1:" + erormessage;
                        }
                    }

                }
                catch (Exception ex)
                {
                    //result = ErpLogin/();
                    //if (result != "true")
                    //{
                    //    return "-1|" + result;
                    //}
                    //EpicorSession.CompanyID = companyId;
                    //adapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<LaborImpl>(EpicorSession, ImplBase<Erp.Contracts.LaborSvcContract>.UriPath);
                    //string strmessage = "";
                    //adapter.ReviewIsDocumentLock(laborHedSeq.ToString(), laborDtlSeq.ToString(), out strmessage);
                    //adapter.RecallFromApproval(ds, false, out strmessage);
                    //adapter.CheckNonConformance(dt.Rows[0]["JobNum"].ToString(), laborHedSeq, laborDtlSeq, out strmessage);
                    //ds.Tables["LaborDtl"].Rows[ds.Tables["LaborDtl"].Rows.Count - 1]["TimeStatus"] = "E";
                    //adapter.Update(ds);
                    //ds.Tables["LaborDtl"].Rows[ds.Tables["LaborDtl"].Rows.Count - 1].Delete();
                    //ds.Tables["LaborHed"].Rows[ds.Tables["LaborHed"].Rows.Count - 1].Delete();
                    //adapter.Update(ds);
                    try
                    {
                        EpicorSession.Dispose();
                    }
                    catch { }
                    if (ex.Message.ToString().Length > 500)
                    {
                        return "-1:" + ex.Message.ToString().Substring(0, 500);
                    }
                    else
                    {
                        return "-1:" + ex.Message.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.ToString().Length > 500)
                {
                    return "-1:" + ex.Message.ToString().Substring(0, 500);
                }
                else
                {
                    return "-1:" + ex.Message.ToString();
                }
            }
        }


        public bool IssueReturnWithLot(string jobNum, int assemblySeq, int oprSeq, int mtlSeq, string partNum, decimal tranQty, DateTime tranDate, string ium, string fromWarehouseCode, string fromBinNum, string toWarehouseCode, string toBinNum, string lotNum, decimal Qty, string tranReference, DataTable dt, int i, string companyId, IssueReturnImpl adapter)
        {
            bool index = true;
            if (tranQty <= 0)
            {
                return true;
            }
            if (tranQty <= Convert.ToDecimal(dt.Rows[i]["OnhandQty"]))
            {
                index = IssueReturnSTKMTL(jobNum, assemblySeq, oprSeq, mtlSeq, partNum, tranQty, tranDate, ium, fromWarehouseCode, fromBinNum, toWarehouseCode, toBinNum, dt.Rows[i]["LotNum"].ToString(), tranReference, companyId, adapter);
                if (!index)
                {
                    return index;
                }
                return index;
            }
            else
            {
                index = IssueReturnSTKMTL(jobNum, assemblySeq, oprSeq, mtlSeq, partNum, Convert.ToDecimal(dt.Rows[i]["OnhandQty"]), tranDate, ium, fromWarehouseCode, fromBinNum, toWarehouseCode, toBinNum, dt.Rows[i]["LotNum"].ToString(), tranReference, companyId, adapter);
                if (!index)
                {
                    return index;
                }
                if (tranQty - Convert.ToDecimal(dt.Rows[i]["OnhandQty"]) > 0)
                {
                    i++;
                    return IssueReturnWithLot(jobNum, assemblySeq, oprSeq, mtlSeq, partNum, tranQty - Convert.ToDecimal(dt.Rows[i - 1]["OnhandQty"]), tranDate, ium, fromWarehouseCode, fromBinNum, toWarehouseCode, toBinNum, dt.Rows[i]["LotNum"].ToString(), Convert.ToDecimal(dt.Rows[i]["OnhandQty"]), tranReference, dt, i, companyId, adapter);
                }
                else
                {
                    return true;
                }
            }

        }


        public bool IssueReturnWithLotbak(string jobNum, int assemblySeq, int oprSeq, int mtlSeq, string partNum, decimal tranQty, DateTime tranDate, string ium, string fromWarehouseCode, string fromBinNum, string toWarehouseCode, string toBinNum, string lotNum, decimal Qty, string tranReference, DataTable dt, int i, string companyId)
        {
            bool index = true;
            if (tranQty <= 0)
            {
                return true;
            }
            if (tranQty <= Convert.ToDecimal(dt.Rows[i]["OnhandQty"]))
            {
                index = IssueReturnSTKMTLbak(jobNum, assemblySeq, oprSeq, mtlSeq, partNum, tranQty, tranDate, ium, fromWarehouseCode, fromBinNum, toWarehouseCode, toBinNum, dt.Rows[i]["LotNum"].ToString(), tranReference, companyId);
                if (!index)
                {
                    return index;
                }
                return index;
            }
            else
            {
                index = IssueReturnSTKMTLbak(jobNum, assemblySeq, oprSeq, mtlSeq, partNum, Convert.ToDecimal(dt.Rows[i]["OnhandQty"]), tranDate, ium, fromWarehouseCode, fromBinNum, toWarehouseCode, toBinNum, dt.Rows[i]["LotNum"].ToString(), tranReference, companyId);
                if (!index)
                {
                    return index;
                }
                if (tranQty - Convert.ToDecimal(dt.Rows[i]["OnhandQty"]) > 0)
                {
                    i++;
                    return IssueReturnWithLotbak(jobNum, assemblySeq, oprSeq, mtlSeq, partNum, tranQty - Convert.ToDecimal(dt.Rows[i - 1]["OnhandQty"]), tranDate, ium, fromWarehouseCode, fromBinNum, toWarehouseCode, toBinNum, dt.Rows[i]["LotNum"].ToString(), Convert.ToDecimal(dt.Rows[i]["OnhandQty"]), tranReference, dt, i, companyId);
                }
                else
                {
                    return true;
                }
            }

        }



        /// <summary>
        /// 根据参数获取工单号、半成品号、工序序号
        /// </summary>
        /// <param name="laborDetailKeyAndValArrayStr"></param>
        /// <returns></returns>
        private string[] GetJobData(string laborDetailKeyAndValArrayStr)
        {
            string[] jobData = new string[3];
            Regex rg1 = new Regex(Commom.splitArrayLine);
            string[] poDetailKeyAndValArray = rg1.Split(laborDetailKeyAndValArrayStr);//字段和值的集合
            string fieldAndVal = "";
            for (int i = 0; i < poDetailKeyAndValArray.Length; i++)
            {
                if (poDetailKeyAndValArray[i] == "") continue;

                string[] keyAndValArray = Regex.Split(poDetailKeyAndValArray[i], Commom.splitArrayLineStr, RegexOptions.IgnoreCase);//字段和值的集合

                for (int j = 0; j < keyAndValArray.Length; j++)
                {
                    fieldAndVal = keyAndValArray[j];
                    if (fieldAndVal == "") continue;

                    string[] strArr = Regex.Split(fieldAndVal, Commom.splitArrayKeyAndValStr, RegexOptions.IgnoreCase);
                    string filedName = strArr[0];
                    string value = strArr[1];
                    if (filedName == "JobNum")
                    {
                        jobData[0] = value;
                    }
                    if (filedName == "AssemblySeq")
                    {
                        jobData[1] = value;
                    }
                    if (filedName == "OprSeq")
                    {
                        jobData[2] = value;
                    }
                }
            }
            return jobData;
        }

        /// <summary>
        /// 工单发料
        /// </summary>
        private bool IssueReturnSTKMTL(string jobNum, int assemblySeq, int oprSeq, int mtlSeq, string partNum, decimal tranQty, DateTime tranDate, string ium, string fromWarehouseCode, string fromBinNum, string toWarehouseCode, string toBinNum, string lotNum, string tranReference, string companyId, IssueReturnImpl adapter)
        {
            try
            {
                IssueReturnDataSet ds = new IssueReturnDataSet();
                SelectedJobAsmblDataSet jads = new SelectedJobAsmblDataSet();
                string pcTranType;
                string pCallProcess;
                pcTranType = "STK-MTL";
                pCallProcess = "IssueMaterial";
                System.String pcMessage = "";
                System.String parttranpks = "";
                bool result;
                Guid pcMtlQueueRowid = new Guid();
                adapter.GetNewJobAsmblMultiple(pcTranType, pcMtlQueueRowid, pCallProcess, jads, out pcMessage);
                adapter.GetNewIssueReturnToJob(jobNum, assemblySeq, pcTranType, new Guid(), out pcMessage, ds);
                adapter.GetNewJobAsmblMultiple(pcTranType, pcMtlQueueRowid, pCallProcess, jads, out pcMessage);
                adapter.OnChangingToJobSeq(mtlSeq, ds);
                ds.Tables["IssueReturn"].Rows[0]["ToJobSeq"] = mtlSeq;
                adapter.OnChangeToJobSeq(ds, pCallProcess, out pcMessage);
                ds.Tables["IssueReturn"].Rows[0]["TranQty"] = tranQty;
                adapter.OnChangeTranQty(tranQty, ds);
                string pUM = ium;
                ds.Tables["IssueReturn"].Rows[0]["UM"] = ium;
                adapter.OnChangeUM(pUM, ds);
                ds.Tables["IssueReturn"].Rows[0]["TranDate"] = tranDate.ToString("yyyy-MM-dd");
                ds.Tables["IssueReturn"].Rows[0]["FromWarehouseCode"] = fromWarehouseCode;
                ds.Tables["IssueReturn"].Rows[0]["FromBinNum"] = fromBinNum;
                ds.Tables["IssueReturn"].Rows[0]["ToWarehouseCode"] = toWarehouseCode;
                ds.Tables["IssueReturn"].Rows[0]["ToBinNum"] = toBinNum;
                ds.Tables["IssueReturn"].Rows[0]["LotNum"] = lotNum;
                ds.Tables["IssueReturn"].Rows[0]["TranReference"] = tranReference;
                bool plNegQtyAction = true;
                System.String legalNumberMessage = "";
                string partTranPKs = "";
                //Call Adapter method
                adapter.PrePerformMaterialMovement(ds, out plNegQtyAction);
                //来源仓
                //adapterIssueReturn.NegativeInventoryTest(partNum, fromWarehouseCode, fromBinNum, "", ium, 1, 1, out legalNumberMessage, out partTranPKs);
                string pcNeqQtyAction = "";
                string pcNeqQtyMessage = "";
                string pcPCBinAction = "";
                string pcPCBinMessage = "";
                string pcOutBinAction = "";
                string pcOutBinMessage = "";
                adapter.MasterInventoryBinTests(ds, out pcNeqQtyAction, out pcNeqQtyMessage, out pcPCBinAction, out pcPCBinMessage, out pcOutBinAction, out pcOutBinMessage);
                if (!string.IsNullOrEmpty(pcNeqQtyMessage) || (!string.IsNullOrEmpty(pcNeqQtyAction) && pcNeqQtyAction != "None"))
                {
                    string s = (pcNeqQtyMessage);
                    string message = "工单：" + jobNum + "/" + assemblySeq + "/" + oprSeq + ",扣料时系统报错。物料：" + partNum + ",来源仓:" + fromWarehouseCode + "/" + fromBinNum + ",目标仓:" + toWarehouseCode + "/" + toBinNum + ",批次:" + lotNum + ",数量:" + tranQty + ".原因:" + s;
                    //WriteTxt(message);
                    return false;
                }
                adapter.PerformMaterialMovement(true, ds, out legalNumberMessage, out partTranPKs);
                adapter.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                string message = "工单：" + jobNum + "/" + assemblySeq + "/" + oprSeq + ",扣料时系统报错。物料：" + partNum + ",来源仓:" + fromWarehouseCode + "/" + fromBinNum + ",目标仓:" + toWarehouseCode + "/" + toBinNum + ",批次:" + lotNum + ",数量:" + tranQty + ".原因:" + ex.Message;
                //WriteTxt(message);
                return false;
            }
        }

        /// <summary>
        /// 工单发料
        /// </summary>
        private bool IssueReturnSTKMTLbak(string jobNum, int assemblySeq, int oprSeq, int mtlSeq, string partNum, decimal tranQty, DateTime tranDate, string ium, string fromWarehouseCode, string fromBinNum, string toWarehouseCode, string toBinNum, string lotNum, string tranReference, string companyId)
        {
            try
            {
                Session EpicorSession = ErpLoginbak();
                if (EpicorSession == null)
                {
                    return false;
                }
                EpicorSession.CompanyID = companyId;
                IssueReturnImpl adapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<IssueReturnImpl>(EpicorSession, ImplBase<Erp.Contracts.IssueReturnSvcContract>.UriPath);
                IssueReturnDataSet ds = new IssueReturnDataSet();
                SelectedJobAsmblDataSet jads = new SelectedJobAsmblDataSet();
                string pcTranType;
                string pCallProcess;
                pcTranType = "STK-MTL";
                pCallProcess = "IssueMaterial";
                System.String pcMessage = "";
                System.String parttranpks = "";
                bool result;
                Guid pcMtlQueueRowid = new Guid();
                adapter.GetNewJobAsmblMultiple(pcTranType, pcMtlQueueRowid, pCallProcess, jads, out pcMessage);
                adapter.GetNewIssueReturnToJob(jobNum, assemblySeq, pcTranType, new Guid(), out pcMessage, ds);
                adapter.GetNewJobAsmblMultiple(pcTranType, pcMtlQueueRowid, pCallProcess, jads, out pcMessage);
                adapter.OnChangingToJobSeq(mtlSeq, ds);
                ds.Tables["IssueReturn"].Rows[0]["ToJobSeq"] = mtlSeq;
                adapter.OnChangeToJobSeq(ds, pCallProcess, out pcMessage);
                ds.Tables["IssueReturn"].Rows[0]["TranQty"] = tranQty;
                adapter.OnChangeTranQty(tranQty, ds);
                string pUM = ium;
                ds.Tables["IssueReturn"].Rows[0]["UM"] = ium;
                adapter.OnChangeUM(pUM, ds);
                ds.Tables["IssueReturn"].Rows[0]["TranDate"] = tranDate.ToString("yyyy-MM-dd");
                ds.Tables["IssueReturn"].Rows[0]["FromWarehouseCode"] = fromWarehouseCode;
                ds.Tables["IssueReturn"].Rows[0]["FromBinNum"] = fromBinNum;
                ds.Tables["IssueReturn"].Rows[0]["ToWarehouseCode"] = toWarehouseCode;
                ds.Tables["IssueReturn"].Rows[0]["ToBinNum"] = toBinNum;
                ds.Tables["IssueReturn"].Rows[0]["LotNum"] = lotNum;
                ds.Tables["IssueReturn"].Rows[0]["TranReference"] = tranReference;
                bool plNegQtyAction = true;
                System.String legalNumberMessage = "";
                string partTranPKs = "";
                //Call Adapter method
                adapter.PrePerformMaterialMovement(ds, out plNegQtyAction);
                //来源仓
                //adapterIssueReturn.NegativeInventoryTest(partNum, fromWarehouseCode, fromBinNum, "", ium, 1, 1, out legalNumberMessage, out partTranPKs);
                string pcNeqQtyAction = "";
                string pcNeqQtyMessage = "";
                string pcPCBinAction = "";
                string pcPCBinMessage = "";
                string pcOutBinAction = "";
                string pcOutBinMessage = "";
                adapter.MasterInventoryBinTests(ds, out pcNeqQtyAction, out pcNeqQtyMessage, out pcPCBinAction, out pcPCBinMessage, out pcOutBinAction, out pcOutBinMessage);
                if (!string.IsNullOrEmpty(pcNeqQtyMessage) || (!string.IsNullOrEmpty(pcNeqQtyAction) && pcNeqQtyAction != "None"))
                {
                    string s = (pcNeqQtyMessage);
                    string message = "工单：" + jobNum + "/" + assemblySeq + "/" + oprSeq + ",扣料时系统报错。物料：" + partNum + ",来源仓:" + fromWarehouseCode + "/" + fromBinNum + ",目标仓:" + toWarehouseCode + "/" + toBinNum + ",批次:" + lotNum + ",数量:" + tranQty + ".原因:" + s;
                    //WriteTxt(message);
                    return false;
                }
                adapter.PerformMaterialMovement(true, ds, out legalNumberMessage, out partTranPKs);
                adapter.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                string message = "工单：" + jobNum + "/" + assemblySeq + "/" + oprSeq + ",扣料时系统报错。物料：" + partNum + ",来源仓:" + fromWarehouseCode + "/" + fromBinNum + ",目标仓:" + toWarehouseCode + "/" + toBinNum + ",批次:" + lotNum + ",数量:" + tranQty + ".原因:" + ex.Message;
                //WriteTxt(message);
                return false;
            }
        }


        //杂项发料
        private bool IssueReturnSTKUKN(string partNum, decimal tranQty, DateTime tranDate, string ium, string fromWarehouseCode, string fromBinNum, string lotNum, string tranReference, string reasonCode, string companyId)
        {
            try
            {
                Session EpicorSession = ErpLoginbak();
                if (EpicorSession == null)
                {
                    return false;
                }
                EpicorSession.CompanyID = companyId;
                IssueReturnImpl adapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<IssueReturnImpl>(EpicorSession, ImplBase<Erp.Contracts.IssueReturnSvcContract>.UriPath);
                IssueReturnDataSet ds = new IssueReturnDataSet();
                SelectedJobAsmblDataSet jads = new SelectedJobAsmblDataSet();
                Guid guid = new Guid();
                adapter.GetNewPartNum(partNum, "STK-UKN", guid, ds);     //杂项发料
                int rowIndex = ds.Tables["IssueReturn"].Rows.Count - 1;
                ds.Tables["IssueReturn"].Rows[rowIndex]["TranQty"] = tranQty;//数量
                adapter.OnChangeTranQty(tranQty, ds);
                ds.Tables["IssueReturn"].Rows[rowIndex]["ReasonCode"] = reasonCode;
                ds.Tables["IssueReturn"].Rows[rowIndex]["Company"] = companyId;
                ds.Tables["IssueReturn"].Rows[rowIndex]["FromWarehouseCode"] = fromWarehouseCode;
                adapter.OnChangeFromWarehouse(ds, "IssueMiscellaneousMaterial");

                ds.Tables["IssueReturn"].Rows[rowIndex]["FromBinNum"] = fromBinNum;//

                string pcMessage;
                bool plOverrideBinChange = false, requiresUserInput;
                adapter.OnChangingFromBinNum(ds, out pcMessage);
                adapter.OnChangeFromBinNum(plOverrideBinChange, ds);
                ds.Tables["IssueReturn"].Rows[rowIndex]["TranDate"] = tranDate;
                ds.Tables["IssueReturn"].Rows[rowIndex]["LotNum"] = lotNum;
                adapter.PrePerformMaterialMovement(ds, out requiresUserInput);
                bool plNegQtyAction = false;
                string legalNumberMessage = "", partTranPKs = "";
                string pcNeqQtyAction = "", pcmsg = "";
                adapter.NegativeInventoryTest(partNum, fromWarehouseCode, fromBinNum, lotNum, "", ium, 1, 1, out pcNeqQtyAction, out pcmsg);
                adapter.PerformMaterialMovement(plNegQtyAction, ds, out legalNumberMessage, out partTranPKs);
                adapter.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                string message = "发料失败.物料：" + partNum + ",来源仓:" + fromWarehouseCode + "/" + fromBinNum + ",批次:" + lotNum + ",数量:" + tranQty + ".原因:" + ex.Message;
                //WriteTxt(message);
                return false;
            }
        }

        //返回杂项物料
        private bool IssueReturnUKNSTK(string partNum, decimal tranQty, DateTime tranDate, string ium, string toWarehouseCode, string toBinNum, string lotNum, string tranReference, string reasonCode, string companyId)
        {
            try
            {
                Session EpicorSession = ErpLoginbak();
                if (EpicorSession == null)
                {
                    return false;
                }
                EpicorSession.CompanyID = companyId;
                IssueReturnImpl adapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<IssueReturnImpl>(EpicorSession, ImplBase<Erp.Contracts.IssueReturnSvcContract>.UriPath);
                IssueReturnDataSet ds = new IssueReturnDataSet();
                SelectedJobAsmblDataSet jads = new SelectedJobAsmblDataSet();
                Guid guid = new Guid();
                adapter.GetNewPartNum(partNum, "UKN-STK", guid, ds);     //
                int rowIndex = ds.Tables["IssueReturn"].Rows.Count - 1;
                ds.Tables["IssueReturn"].Rows[rowIndex]["TranQty"] = tranQty;//数量
                adapter.OnChangeTranQty(tranQty, ds);
                ds.Tables["IssueReturn"].Rows[rowIndex]["ReasonCode"] = reasonCode;
                ds.Tables["IssueReturn"].Rows[rowIndex]["Company"] = companyId;
                ds.Tables["IssueReturn"].Rows[rowIndex]["OnChangeToWarehouse"] = toWarehouseCode;
                adapter.OnChangeFromWarehouse(ds, "IssueMiscellaneousMaterial");

                ds.Tables["IssueReturn"].Rows[rowIndex]["ToBinNum"] = toBinNum;//

                string pcMessage;
                bool plOverrideBinChange = false, requiresUserInput;
                adapter.OnChangingFromBinNum(ds, out pcMessage);
                adapter.OnChangeFromBinNum(plOverrideBinChange, ds);
                ds.Tables["IssueReturn"].Rows[rowIndex]["TranDate"] = tranDate;
                adapter.PrePerformMaterialMovement(ds, out requiresUserInput);
                bool plNegQtyAction = false;
                string legalNumberMessage, partTranPKs;
                adapter.PerformMaterialMovement(plNegQtyAction, ds, out legalNumberMessage, out partTranPKs);
                adapter.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                string message = "入库失败.物料：" + partNum + ",仓:" + toWarehouseCode + "/" + toBinNum + ",批次:" + lotNum + ",数量:" + tranQty + ".原因:" + ex.Message;
                //WriteTxt(message);
                return false;
            }
        }

        //库存转仓
        public string InvTransfert(string partnum, decimal qty, string fromwarehousecode, string frombinnum, string towarehousecode, string tobinnum, string lotnum, string companyId)
        {
            try
            {
                UpdateAWS("insert into BO_ERPRECORD(UD01,UD02,UD10) values('" + partnum + "|" + qty.ToString() + "|" + fromwarehousecode + "|" + towarehousecode + "','" + companyId + "','库存转仓')");
                //string resultdata = ErpLogin/();
                //if (resultdata != "true")
                //{
                //    return "false|" + resultdata;
                //}
                Session EpicorSession = ErpLoginbak();
                if (EpicorSession == null)
                {
                    return "false|erp用户数不够，请稍候再试.ERR:InvTransfert";
                }
                EpicorSession.CompanyID = companyId;
                InvTransferImpl adapterInvTransfer = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<InvTransferImpl>(EpicorSession, ImplBase<Erp.Contracts.InvTransferSvcContract>.UriPath);

                string uomCode = "";
                // Declare and Initialize Variables
                string serialWarning, questionString;
                bool multipleMatch;
                Guid guid = new Guid();
                adapterInvTransfer.GetPartXRefInfo(ref partnum, ref uomCode, guid, "", out serialWarning, out questionString, out multipleMatch);

                // Call Adapter method
                InvTransferDataSet dsInvTransfer = adapterInvTransfer.GetTransferRecord(partnum, uomCode);
                dsInvTransfer.Tables["InvTrans"].Rows[0]["TransferQty"] = qty;
                adapterInvTransfer.ChangeUOM(dsInvTransfer);

                dsInvTransfer.Tables["InvTrans"].Rows[0]["FromWarehouseCode"] = fromwarehousecode;
                adapterInvTransfer.ChangeFromWhse(dsInvTransfer);
                dsInvTransfer.Tables["InvTrans"].Rows[0]["toWarehouseCode"] = towarehousecode;
                adapterInvTransfer.ChangeToWhse(dsInvTransfer);
                if (dsInvTransfer.Tables["InvTrans"].Rows[0]["toBinNum"].ToString().Trim().Length < 1)
                {
                    dsInvTransfer.Tables["InvTrans"].Rows[0]["toBinNum"] = frombinnum;
                    adapterInvTransfer.ChangeToBin(dsInvTransfer);
                }
                if (dsInvTransfer.Tables["InvTrans"].Rows[0]["fromBinNum"].ToString().Trim().Length < 1)
                {
                    dsInvTransfer.Tables["InvTrans"].Rows[0]["fromBinNum"] = tobinnum;
                    adapterInvTransfer.ChangeFromBin(dsInvTransfer);
                }
                if (dsInvTransfer.Tables["InvTrans"].Rows[0]["FromLotNumber"].ToString().Trim().Length < 1)
                {
                    dsInvTransfer.Tables["InvTrans"].Rows[0]["FromLotNumber"] = lotnum;
                    adapterInvTransfer.ChangeLot(dsInvTransfer);
                }
                if (dsInvTransfer.Tables["InvTrans"].Rows[0]["ToLotNumber"].ToString().Trim().Length < 1)
                {
                    dsInvTransfer.Tables["InvTrans"].Rows[0]["ToLotNumber"] = lotnum;
                    adapterInvTransfer.ChangeLot(dsInvTransfer);
                }
                string pcBinNum = dsInvTransfer.Tables["InvTrans"].Rows[0]["fromBinNum"].ToString();
                string pcLotNum = "", pcDimCode = dsInvTransfer.Tables["InvTrans"].Rows[0]["trackingUOM"].ToString();
                decimal pdDimConvFactor = 1M;
                string pcNeqQtyAction = "", pcMessage;
                //adapterInvTransfer.NegativeInventoryTest(iPartNum, FromWarehouseCode, pcBinNum, pcLotNum, pcDimCode, pdDimConvFactor, qty, out pcNeqQtyAction, out pcMessage);  
                // MessageBox.Show("pcMessage:" + pcMessage + "     pcNeqQtyAction: " + pcNeqQtyAction);
                string legalNumberMessage = "";
                bool requiresUserInput = true;
                string msg = "";
                adapterInvTransfer.MasterInventoryBinTests(dsInvTransfer, out msg, out msg, out msg, out msg, out msg, out msg);
                adapterInvTransfer.PreCommitTransfer(dsInvTransfer, out requiresUserInput);
                string partTranPKs = "";
                //MessageBox.Show(" requiresUserInput: " + requiresUserInput);
                adapterInvTransfer.CommitTransfer(dsInvTransfer, out legalNumberMessage, out partTranPKs);


                //result.Obj = "partTranPKs:" + partTranPKs + "           legalNumberMessage:" + legalNumberMessage;
                //result.Status = 1;
                EpicorSession.Dispose();
                return "true";
            }
            catch (Exception ex)
            {
                return "false|" + ex.Message.ToString();
            }
        }

        //库存类型不合格品
        public string AddWhseNonConf(string json)
        {
            UpdateAWS("insert into BO_ERPRECORD(UD01,UD02,UD10) values('','" + json + "','库存不合格品')");
            DataTable dt = ToDataTable(json);
            if (dt == null || dt.Rows.Count == 0)
            {
                return "false|没有数据传入.";
            }
            string partnum = dt.Rows[0]["partnum"].ToString();
            decimal qty = Convert.ToDecimal(dt.Rows[0]["qty"].ToString());
            string pwhse = dt.Rows[0]["pwhse"].ToString();
            string pbinnum = dt.Rows[0]["pbinnum"].ToString();
            string whse = dt.Rows[0]["whse"].ToString();
            string binnum = dt.Rows[0]["binnum"].ToString();
            string plotnum = dt.Rows[0]["plotnum"].ToString();
            string ium = dt.Rows[0]["ium"].ToString();
            string reasoncode = dt.Rows[0]["reasoncode"].ToString();
            string companyId = dt.Rows[0]["companyId"].ToString();
            //string resultdata = ErpLogin/();
            //if (resultdata != "true")
            //{
            //    return "false|" + resultdata;
            //}
            Session EpicorSession = ErpLoginbak();
            if (EpicorSession == null)
            {
                return "false|erp用户数不够，请稍候再试.ERR:AddWhseNonConf";
            }
            EpicorSession.CompanyID = companyId;
            NonConfImpl adapterNonConf = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<NonConfImpl>(EpicorSession, ImplBase<Erp.Contracts.NonConfSvcContract>.UriPath);
            NonConfDataSet ds = adapterNonConf.AddNonConf("Inventory");
            string msg = "";
            bool boolmsg = false;
            adapterNonConf.GetPartInfo(partnum, out msg, out msg, out msg, out boolmsg, out boolmsg, out msg, out msg);
            //adapterNonConf.GetAvailableQty(ds);
            ds.Tables["NonConf"].Rows[0]["PartNum"] = partnum;
            ds.Tables["NonConf"].Rows[0]["ScrapUM"] = ium;
            ds.Tables["NonConf"].Rows[0]["TranUOM"] = ium;
            adapterNonConf.OnChangeTranQty(qty, ds);
            adapterNonConf.GetAvailableQty(ds);
            bool ifrom = false;
            ds.Tables["NonConf"].Rows[0]["BinNum"] = binnum;
            ds.Tables["NonConf"].Rows[0]["WarehouseCode"] = whse;
            ds.Tables["NonConf"].Rows[0]["ToWarehouseCode"] = pwhse;
            adapterNonConf.OnChangeWarehouseCode(pwhse, ifrom, ds);
            adapterNonConf.OnChangeBinNum(pbinnum, ifrom, ds);
            ds.Tables["NonConf"].Rows[0]["ToBinNum"] = pbinnum;
            string qtyaction = "";
            adapterNonConf.ValidateQtyInventoryTest(partnum, whse, binnum, plotnum, ium, 1, qty, 0, out qtyaction, out msg);
            ds.Tables["NonConf"].Rows[0]["ReasonCode"] = reasoncode;
            ds.Tables["NonConf"].Rows[0]["LotNum"] = plotnum;
            adapterNonConf.PreUpdate(ds, out boolmsg, out msg);
            adapterNonConf.Update(ds);
            return "";
        }

        //工序不合格品
        public string AddOprNonConf(string json)
        {
            UpdateAWS("insert into BO_ERPRECORD(UD01,UD02,UD10) values('','" + json + "','工序不合格品')");
            DataTable dt = ToDataTable(json);
            if (dt == null || dt.Rows.Count == 0)
            {
                return "false|没有数据传入.";
            }
            string jobnum = dt.Rows[0]["jobnum"].ToString();
            int assemblyseq = int.Parse(dt.Rows[0]["assemblyseq"].ToString());
            int oprseq = int.Parse(dt.Rows[0]["oprseq"].ToString());
            decimal qty = Convert.ToDecimal(dt.Rows[0]["qty"].ToString());
            string pwhse = dt.Rows[0]["pwhse"].ToString();
            string pbinnum = dt.Rows[0]["pbinnum"].ToString();
            string whse = dt.Rows[0]["whse"].ToString();
            string binnum = dt.Rows[0]["binnum"].ToString();
            string plotnum = dt.Rows[0]["plotnum"].ToString();
            string ium = dt.Rows[0]["ium"].ToString();
            string reasoncode = dt.Rows[0]["reasoncode"].ToString();
            string companyId = dt.Rows[0]["companyId"].ToString();
            //string resultdata = ErpLogin/();
            //if (resultdata != "true")
            //{
            //    return "false|" + resultdata;
            //}
            Session EpicorSession = ErpLoginbak();
            if (EpicorSession == null)
            {
                return "false|erp用户数不够，请稍候再试.ERR:AddOprNonConf";
            }
            EpicorSession.CompanyID = companyId;
            NonConfImpl adapterNonConf = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<NonConfImpl>(EpicorSession, ImplBase<Erp.Contracts.NonConfSvcContract>.UriPath);
            NonConfDataSet ds = adapterNonConf.AddNonConf("Operation");
            string msg = "";
            bool boolmsg = false;
            adapterNonConf.OnChangeJobNum(jobnum, out boolmsg, out msg, ds);
            ds.Tables["NonConf"].Rows[0]["AssemblySeq"] = assemblyseq;
            bool INCOpr = true;
            adapterNonConf.OnChangeJobOpr(oprseq, INCOpr, ds);
            ds.Tables["NonConf"].Rows[0]["ScrapUM"] = ium;
            ds.Tables["NonConf"].Rows[0]["TranUOM"] = ium;
            adapterNonConf.OnChangeTranQty(qty, ds);
            bool ifrom = false;
            ds.Tables["NonConf"].Rows[0]["BinNum"] = binnum;
            ds.Tables["NonConf"].Rows[0]["WarehouseCode"] = whse;
            ds.Tables["NonConf"].Rows[0]["ToWarehouseCode"] = pwhse;
            adapterNonConf.OnChangeWarehouseCode(pwhse, ifrom, ds);
            adapterNonConf.OnChangeBinNum(pbinnum, ifrom, ds);
            ds.Tables["NonConf"].Rows[0]["ToBinNum"] = pbinnum;
            ds.Tables["NonConf"].Rows[0]["ReasonCode"] = reasoncode;
            ds.Tables["NonConf"].Rows[0]["LotNum"] = plotnum;
            adapterNonConf.PreUpdate(ds, out boolmsg, out msg);
            adapterNonConf.Update(ds);
            return "";
        }

        //库存检验处理
        public string WhseInspRcpt(int pcTranID, string InspectorID, decimal passqty, decimal failedqty, string warehousecode, string binnum, string failedwarehousecode, string failedbinnum, string reasonCode, string companyId)
        {
            UpdateAWS("insert into BO_ERPRECORD(UD01,UD02,UD10) values('','" + companyId + "','库存检验处理')");
            try
            {
                //string resultdata = ErpLogin/();
                //if (resultdata != "true")
                //{
                //    return "false|" + resultdata;
                //}
                Session EpicorSession = ErpLoginbak();
                if (EpicorSession == null)
                {
                    return "false|erp用户数不够，请稍候再试.ERR:WhseInspRcpt";
                }
                EpicorSession.CompanyID = companyId;
                InspProcessingImpl adapterInspProcessing = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<InspProcessingImpl>(EpicorSession, ImplBase<Erp.Contracts.InspProcessingSvcContract>.UriPath);
                InspProcessingDataSet ds = adapterInspProcessing.GetByID(pcTranID);
                string infoMsg = "";
                adapterInspProcessing.AssignInspectorReceipt(InspectorID, ds, out infoMsg);
                ds.Tables["InspNonConf"].Rows[ds.Tables["InspNonConf"].Rows.Count - 1]["InspectorID"] = InspectorID;
                ds.Tables["InspNonConf"].Rows[ds.Tables["InspNonConf"].Rows.Count - 1]["InspectedBy"] = InspectorID;
                ds.Tables["InspNonConf"].Rows[ds.Tables["InspNonConf"].Rows.Count - 1]["PassedQty"] = passqty;
                ds.Tables["InspNonConf"].Rows[ds.Tables["InspNonConf"].Rows.Count - 1]["DimPassedQty"] = passqty;
                ds.Tables["InspProcList"].Rows[ds.Tables["InspProcList"].Rows.Count - 1]["PassedQty"] = passqty;
                //ds.Tables["InspRcpt"].Rows[ds.Tables["InspRcpt"].Rows.Count - 1]["PassedQty"] = passqty;
                //ds.Tables["InspRcpt"].Rows[ds.Tables["InspRcpt"].Rows.Count - 1]["DimPassedQty"] = passqty;
                adapterInspProcessing.OnChangePassedQty(ds, "NonConf", passqty, out infoMsg);
                string binactive = "";
                adapterInspProcessing.CheckPlanningContractBin(ds, "NonConf", out binactive, out infoMsg);
                bool boolmsg = false;
                adapterInspProcessing.GetPassedLegalNumGenOpts(ds, "Inventory", out boolmsg);
                int dmrnum = 0;
                string dmsg = "";
                ds.Tables["InspNonConf"].Rows[ds.Tables["InspNonConf"].Rows.Count - 1]["PassedWarehouseCode"] = warehousecode;
                ds.Tables["InspNonConf"].Rows[ds.Tables["InspNonConf"].Rows.Count - 1]["PassedBin"] = binnum;
                ds.Tables["InspNonConf"].Rows[ds.Tables["InspNonConf"].Rows.Count - 1]["FailedQty"] = failedqty;
                ds.Tables["InspNonConf"].Rows[ds.Tables["InspNonConf"].Rows.Count - 1]["DimFailedQty"] = failedqty;
                ds.Tables["InspNonConf"].Rows[ds.Tables["InspNonConf"].Rows.Count - 1]["FailedWarehouseCode"] = failedwarehousecode;
                ds.Tables["InspNonConf"].Rows[ds.Tables["InspNonConf"].Rows.Count - 1]["FailedBin"] = failedbinnum;
                adapterInspProcessing.InspectInventory(out dmsg, out dmrnum, ds);
            }
            catch (Exception ex)
            {
                return "false|" + ex.Message.ToString();
            }
            return "true";
        }

        public DataTable ToDataTable(string json)
        {
            DataTable dataTable = new DataTable();  //实例化
            DataTable result;
            try
            {
                JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
                javaScriptSerializer.MaxJsonLength = Int32.MaxValue; //取得最大数值
                ArrayList arrayList = javaScriptSerializer.Deserialize<ArrayList>(json);
                if (arrayList.Count > 0)
                {
                    foreach (Dictionary<string, object> dictionary in arrayList)
                    {
                        if (dictionary.Keys.Count == 0)
                        {
                            result = dataTable;
                            return result;
                        }
                        if (dataTable.Columns.Count == 0)
                        {
                            foreach (string current in dictionary.Keys)
                            {
                                if (current == "JobNum")
                                {
                                    dataTable.Columns.Add(current, dictionary[current].GetType());
                                }
                            }
                            foreach (string current in dictionary.Keys)
                            {
                                if (current == "AssemblySeq")
                                {
                                    dataTable.Columns.Add(current, dictionary[current].GetType());
                                }
                            }
                            foreach (string current in dictionary.Keys)
                            {
                                if (current == "OprSeq")
                                {
                                    dataTable.Columns.Add(current, dictionary[current].GetType());
                                }
                            }
                            foreach (string current in dictionary.Keys)
                            {
                                if (current != "JobNum" && current != "AssemblySeq" && current != "OprSeq")
                                {
                                    dataTable.Columns.Add(current, dictionary[current].GetType());
                                }
                            }
                        }
                        DataRow dataRow = dataTable.NewRow();
                        foreach (string current in dictionary.Keys)
                        {
                            dataRow[current] = dictionary[current];
                        }

                        dataTable.Rows.Add(dataRow); //循环添加行到DataTable中
                    }
                }
            }
            catch
            {
            }
            result = dataTable;
            return result;
        }

        //检验工单
        public string CheckJob(string jobnum, string assemblynum, string operSeq, string checkQty, int binid, int id, string companyId)
        {
            if ((jobnum != null && jobnum.Length > 0) == false)
            {
                return "无效工单.";
            }

            if ((assemblynum != null && assemblynum.Length > 0) == false)
            {
                return "无效半成品.";
            }

            if ((operSeq != null && operSeq.Length > 0) == false)
            {
                return "无效工序.";
            }

            if ((checkQty != null && checkQty.Length > 0) == false)
            {
                return "无效报检数.";
            }

            string result = "";

            //string resultdata = ErpLogin/();
            //if (resultdata != "true")
            //{
            //    return "false|" + resultdata;
            //}
            //WriteGetNewLaborInERPTxt("", EpicorSessionManager.EpicorSession.SessionID.ToString(), "", "sessionid", "checkj");
            //EpicorSessionManager.EpicorSession.CompanyID = companyId;
            //JobEntryImpl jobEntry = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<JobEntryImpl>(EpicorSessionManager.EpicorSession, ImplBase<Erp.Contracts.JobEntrySvcContract>.UriPath);
            //LaborDtlSearchImpl search = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<LaborDtlSearchImpl>(EpicorSessionManager.EpicorSession, ImplBase<Erp.Contracts.LaborDtlSearchSvcContract>.UriPath);
            //JobEntryDataSet DsJobEntry;
            //try
            //{
            //    DsJobEntry = jobEntry.GetByID(jobnum);
            //}
            //catch
            //{
            //    result = result + "无效工单.";
            //    EpicorSessionManager.EpicorSession.Dispose();
            //    return result;
            //}
            DataTable dt = GetDataByERP("select JobClosed,JobComplete,JobReleased from erp.JobHead where JobNum='" + jobnum + "' and Company='" + companyId + "'");
            //try
            //{
            //    dt = DsJobEntry.Tables["JobHead"]; //JobClosed,JobComplete
            //}
            //catch(Exception ex) { return ex.Message.ToString(); }
            if (dt != null && dt.Rows.Count > 0)
            {
                if (Convert.ToInt32(dt.Rows[0]["JobClosed"]) == 1)
                {
                    result = result + "工单已关闭.";
                    return result;
                }
                if (Convert.ToInt32(dt.Rows[0]["JobComplete"]) == 1)
                {
                    result = result + "工单已完结.";
                    return result;
                }
                if (Convert.ToInt32(dt.Rows[0]["JobReleased"]) == 0)
                {
                    result = result + "工单未发放,不能报工.";
                    return result;
                }
            }
            else
            {
                result = result + "不存在该工单.";
                return result;
            }

            DataTable jadt = GetDataByERP("select AssemblySeq from erp.JobAsmbl where JobNum='" + jobnum + "' and AssemblySeq=" + assemblynum + " and Company='" + companyId + "'");
            //DataRow[] adr;
            //try
            //{
            //    adr = DsJobEntry.Tables["JobAsmbl"].Select("AssemblySeq=" + assemblynum + "");
            //}
            //catch (Exception ex) { return ex.Message.ToString(); }

            if (jadt != null && jadt.Rows.Count > 0)
            {

            }
            else
            {
                result = result + "不存在该半成品.";
                return result;
            }

            //-----20151106 Jeff Add----Begin--判断工序是否存在,需要考虑半成品号+工序号
            //DataTable jodt = GetDataByERP("select a.SubContract,a.RunQty,a.QtyCompleted,b.RequiredQty+b.OverRunQty UDReqQty_c from erp.JobOper a inner join erp.JobAsmbl b on a.Company=b.Company and a.JobNum=b.JobNum and a.AssemblySeq=b.AssemblySeq where a.JobNum='" + jobnum + "' and a.AssemblySeq=" + assemblynum + " and a.OprSeq=" + operSeq + " and a.Company='" + companyId + "'");
            DataTable jodt = GetDataByERP("select a.SubContract,a.RunQty,a.QtyCompleted,b.SurplusQty_c UDReqQty_c from erp.JobOper a inner join JobAsmbl b on a.Company=b.Company and a.JobNum=b.JobNum and a.AssemblySeq=b.AssemblySeq where a.JobNum='" + jobnum + "' and a.AssemblySeq=" + assemblynum + " and a.OprSeq=" + operSeq + " and a.Company='" + companyId + "'");
            if (assemblynum == "0")
            {
                jodt = GetDataByERP("select a.SubContract,a.RunQty,a.QtyCompleted,b.UDReqQty_c UDReqQty_c from erp.JobOper a inner join JobHead b on a.Company=b.Company and a.JobNum=b.JobNum where a.JobNum='" + jobnum + "' and a.AssemblySeq=" + assemblynum + " and a.OprSeq=" + operSeq + " and a.Company='" + companyId + "'");
            }
            //DataTable jodt = GetDataByERP("select a.SubContract,a.RunQty,a.QtyCompleted,b.UDReqQty_c from erp.JobOper a inner join JobHead b on a.Company=b.Company and a.JobNum=b.JobNum where a.JobNum='" + jobnum + "' and a.AssemblySeq=" + assemblynum + " and a.OprSeq=" + operSeq + " and a.Company='" + companyId + "'");
            //DataTable jodt = GetDataByERP("select SubContract,RunQty,QtyCompleted from erp.JobOper where JobNum='" + jobnum + "' and AssemblySeq=" + assemblynum + " and OprSeq=" + operSeq + " and Company='" + companyId + "'");
            //DataRow[] dr;
            //try
            //{
            //    dr = DsJobEntry.Tables["JobOper"].Select("AssemblySeq=" + assemblynum + " and OprSeq=" + operSeq + "");
            //}
            //catch (Exception ex) { return ex.Message.ToString(); }
            if (jodt != null && jodt.Rows.Count > 0)
            {
                //-----20151106 Jeff暂时不考虑外包工序,外包工序也在BPM中可以做转序
                if (jodt.Rows[0]["SubContract"].ToString() == "True")
                {
                    result = result + "外包工序,不需提交.";
                    return result;
                }
                if (companyId == "001")
                {
                    if (Convert.ToDecimal(jodt.Rows[0]["UDReqQty_c"]) == 0)
                    {
                        result = result + "工单可生产数量不能为0,请设置.";
                        return result;
                    }
                    // 20180831
                    if ((Convert.ToDecimal(checkQty) + Convert.ToDecimal(jodt.Rows[0]["QtyCompleted"])) > Convert.ToDecimal(jodt.Rows[0]["UDReqQty_c"]))
                    {//UDReqQty_c
                        result = result + "当前工序本次报检数+完成数量不能超过可生产数量,当前工序可生产数量为" + jodt.Rows[0]["UDReqQty_c"].ToString() + ",请查询跟踪报表.";
                        //result = result + "当前工序本次报检数+完成数量不能超过生产数量,当前工序生产数量为" + jodt.Rows[0]["RunQty"].ToString() + ",请查询跟踪报表.";
                        return result;
                    }
                }


            }
            else
            {
                result = result + "不存在该工序.";
                return result;
            }
            //-----20151106 Jeff Add----Begin


            //-----20151106 Jeff--取该工序的上一工序号 ---Begin
            Int32 preOperSeq;
            DataTable joPredt = GetDataByERP("select SubContract,RunQty,QtyCompleted,OprSeq from erp.JobOper where JobNum='" + jobnum + "' and AssemblySeq=" + assemblynum + " and OprSeq < " + operSeq + " and Company='" + companyId + "' order by OprSeq desc");
            //DataRow[] drPre = DsJobEntry.Tables["JobOper"].Select("AssemblySeq=" + assemblynum + " and OprSeq < " + operSeq + "", "OprSeq Desc");
            //WriteGetNewLaborInERPTxt("", EpicorSessionManager.EpicorSession.SessionID.ToString(), "2", "sessionid", "checkj");
            if (joPredt != null && joPredt.Rows.Count > 0)
            {
                //-----该工序前面有工序,按工序序号降序排列,取第1条即为当前工序的上一工序
                preOperSeq = Convert.ToInt32(joPredt.Rows[0]["OprSeq"]);
                DataTable josubdt = GetDataByERP("select SubContract,RunQty,QtyCompleted,OpDesc from erp.JobOper where JobNum='" + jobnum + "' and AssemblySeq=" + assemblynum + " and OprSeq=" + preOperSeq + " and Company='" + companyId + "'");
                //DataRow[] drSub = DsJobEntry.Tables["JobOper"].Select("AssemblySeq=" + assemblynum + " and OprSeq=" + preOperSeq + "");
                if (josubdt != null && josubdt.Rows.Count > 0)
                {
                    decimal QtyCompleted;

                    QtyCompleted = Convert.ToDecimal(josubdt.Rows[0]["QtyCompleted"]);

                    decimal CurQtyCompleted;
                    DataTable jocurdt = GetDataByERP("select SubContract,RunQty,QtyCompleted from erp.JobOper where JobNum='" + jobnum + "' and AssemblySeq=" + assemblynum + " and OprSeq=" + operSeq + " and Company='" + companyId + "'");
                    //DataRow[] drCur = DsJobEntry.Tables["JobOper"].Select("AssemblySeq=" + assemblynum + " and OprSeq=" + operSeq + "");
                    if (jocurdt != null && jocurdt.Rows.Count > 0)
                    {
                        CurQtyCompleted = Convert.ToDecimal(jocurdt.Rows[0]["QtyCompleted"]);

                    }
                    else
                    {
                        CurQtyCompleted = 0;
                    }


                    if ((Convert.ToDecimal(checkQty) + CurQtyCompleted) > QtyCompleted)
                    {
                        result = result + "当前工序报检数量+已完成数量不能超过上一道工序的完成数量,上一道工序<" + josubdt.Rows[0]["OpDesc"].ToString() + ">完成数量为" + QtyCompleted + "";
                        return result;
                    }

                    decimal QtyBPMRPT;
                    QtyBPMRPT = Convert.ToDecimal(GetBPMRptQty(jobnum, assemblynum, operSeq.ToString(), binid, id, companyId));

                    if ((Convert.ToDecimal(checkQty) + QtyBPMRPT) > QtyCompleted)
                    {
                        result = result + "当前工序本次报检数量+已提交报检数量不能超过上一道工序的完成数量,上道工序<" + josubdt.Rows[0]["OpDesc"].ToString() + ">完成数量为" + QtyCompleted + ",已报检数量为" + QtyBPMRPT + "";
                        return result;
                    }

                    //if ((Convert.ToDecimal(checkQty) + QtyBPMRPT + CurQtyCompleted) > Convert.ToDecimal(dr[0]["RunQty"]))
                    //{
                    //    result = result + "当前工序本次报检数量+已提交报检数量+已完成数量不能超过工序的生产数量,已提交报检数量为" + QtyBPMRPT + ",当前工序生产数量为" + dr[0]["RunQty"].ToString() + "";

                    //    //20160224--Jeff--Being
                    //    CommonClass.GetSession.DisposeSession();
                    //    ConnectionPool.Dispose();
                    //    //20160224--Jeff--End

                    //    return result;
                    //}


                }


            }
            else
            {
                //-----该工序没有上一工序    
                //DataTable jocurdt = GetDataByERP("select a.SubContract,a.RunQty,a.QtyCompleted,b.RequiredQty+b.OverRunQty UDReqQty_c from erp.JobOper a inner join erp.JobAsmbl b on a.Company=b.Company and a.JobNum=b.JobNum and a.AssemblySeq=b.AssemblySeq where a.JobNum='" + jobnum + "' and a.AssemblySeq=" + assemblynum + " and a.OprSeq=" + operSeq + " and a.Company='" + companyId + "'");
                DataTable jocurdt = GetDataByERP("select a.SubContract,a.RunQty,a.QtyCompleted,b.SurPlusQty_c UDReqQty_c from erp.JobOper a inner join JobAsmbl b on a.Company=b.Company and a.JobNum=b.JobNum and a.AssemblySeq=b.AssemblySeq where a.JobNum='" + jobnum + "' and a.AssemblySeq=" + assemblynum + " and a.OprSeq=" + operSeq + " and a.Company='" + companyId + "'");
                if (assemblynum == "0")
                {
                    jocurdt = GetDataByERP("select a.SubContract,a.RunQty,a.QtyCompleted,b.UDReqQty_c UDReqQty_c from erp.JobOper a inner join JobHead b on a.Company=b.Company and a.JobNum=b.JobNum where a.JobNum='" + jobnum + "' and a.AssemblySeq=" + assemblynum + " and a.OprSeq=" + operSeq + " and a.Company='" + companyId + "'");
                }
                //
                //DataTable jocurdt = GetDataByERP("select SubContract,RunQty,QtyCompleted from erp.JobOper where JobNum='" + jobnum + "' and AssemblySeq=" + assemblynum + " and OprSeq=" + operSeq + " and Company='" + companyId + "'");
                decimal CurRunQty;
                //DataRow[] drCurRun = DsJobEntry.Tables["JobOper"].Select("AssemblySeq=" + assemblynum + " and OprSeq=" + operSeq + "");
                if (jocurdt != null && jocurdt.Rows.Count > 0)
                {
                    CurRunQty = Convert.ToDecimal(jocurdt.Rows[0]["UDReqQty_c"]);
                    //CurRunQty = Convert.ToDecimal(jocurdt.Rows[0]["RunQty"]);
                }
                else
                {
                    CurRunQty = 0;
                }

                decimal QtyBPMRPT;

                QtyBPMRPT = Convert.ToDecimal(GetBPMRptQty(jobnum, assemblynum, operSeq.ToString(), binid, id, companyId));
                if (companyId == "001")
                {
                    if (CurRunQty == 0)
                    {
                        result = result + "工单可生产数量不能为0,请设置";
                        return result;
                    }
                    ///20180831
                    if ((Convert.ToDecimal(checkQty) + QtyBPMRPT) > CurRunQty)
                    {//UDReqQty_c
                        result = result + "当前工序本次报检数量+已提交报检数量不能超过工单可生产数量,当前工序可生产数量为" + CurRunQty + ",已报检数量为" + QtyBPMRPT + "";
                        return result;
                    }
                }
                //if ((Convert.ToDecimal(checkQty) + QtyBPMRPT + Convert.ToDecimal(drCurRun[0]["QtyCompleted"])) > CurRunQty)
                //{
                //    result = result + "当前工序本次报检数量+已提交报检数量+已完成数量不能超过工序的生产数量,已提交报检数量为" + QtyBPMRPT + ",当前工序生产数量为" + CurRunQty.ToString() + "";

                //    //20160224--Jeff--Being
                //    CommonClass.GetSession.DisposeSession();
                //    ConnectionPool.Dispose();
                //    //20160224--Jeff--End

                //    return result;
                //}


            }
            //-----20151106 Jeff--取该工序的上一工序号 ---End
            //EpicorSessionManager.EpicorSession.Dispose();
            return result;
        }

        //public string CheckJobbak(string jobnum, string assemblynum, string operSeq, string checkQty, int binid, int id, string companyId)
        //{
        //    if ((jobnum != null && jobnum.Length > 0) == false)
        //    {
        //        return "无效工单.";
        //    }

        //    if ((assemblynum != null && assemblynum.Length > 0) == false)
        //    {
        //        return "无效半成品.";
        //    }

        //    if ((operSeq != null && operSeq.Length > 0) == false)
        //    {
        //        return "无效工序.";
        //    }

        //    if ((checkQty != null && checkQty.Length > 0) == false)
        //    {
        //        return "无效报检数.";
        //    }

        //    string result = "";

        //    string resultdata = ErpLogin/();
        //    if (resultdata != "true")
        //    {
        //        return "false|" + resultdata;
        //    }
        //    WriteGetNewLaborInERPTxt("", EpicorSessionManager.EpicorSession.SessionID.ToString(), "", "sessionid", "checkj");
        //    EpicorSessionManager.EpicorSession.CompanyID = companyId;
        //    JobEntryImpl jobEntry = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<JobEntryImpl>(EpicorSessionManager.EpicorSession, ImplBase<Erp.Contracts.JobEntrySvcContract>.UriPath);
        //    LaborDtlSearchImpl search = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<LaborDtlSearchImpl>(EpicorSessionManager.EpicorSession, ImplBase<Erp.Contracts.LaborDtlSearchSvcContract>.UriPath);
        //    JobEntryDataSet DsJobEntry;
        //    try
        //    {
        //        DsJobEntry = jobEntry.GetByID(jobnum);
        //    }
        //    catch
        //    {
        //        result = result + "无效工单.";
        //        EpicorSessionManager.EpicorSession.Dispose();
        //        return result;
        //    }
        //    DataTable dt = new DataTable();
        //    try
        //    {
        //        dt = DsJobEntry.Tables["JobHead"]; //JobClosed,JobComplete
        //    }
        //    catch (Exception ex) { return ex.Message.ToString(); }
        //    if (dt != null && dt.Rows.Count > 0)
        //    {
        //        if (Convert.ToInt32(dt.Rows[0]["JobClosed"]) == 1)
        //        {
        //            result = result + "工单已关闭.";
        //            EpicorSessionManager.EpicorSession.Dispose();
        //            return result;
        //        }
        //        if (Convert.ToInt32(dt.Rows[0]["JobComplete"]) == 1)
        //        {
        //            result = result + "工单已完结.";
        //            EpicorSessionManager.EpicorSession.Dispose();
        //            return result;
        //        }
        //        if (Convert.ToInt32(dt.Rows[0]["JobReleased"]) == 0)
        //        {
        //            result = result + "工单未发货,不能报工.";
        //            EpicorSessionManager.EpicorSession.Dispose();
        //            return result;
        //        }
        //    }
        //    else
        //    {
        //        result = result + "不存在该工单.";
        //        EpicorSessionManager.EpicorSession.Dispose();
        //        return result;
        //    }

        //    DataRow[] adr;
        //    try
        //    {
        //        adr = DsJobEntry.Tables["JobAsmbl"].Select("AssemblySeq=" + assemblynum + "");
        //    }
        //    catch (Exception ex) { return ex.Message.ToString(); }

        //    if (adr != null && adr.Length > 0)
        //    {

        //    }
        //    else
        //    {
        //        result = result + "不存在该半成品.";
        //        EpicorSessionManager.EpicorSession.Dispose();
        //        return result;
        //    }

        //    //-----20151106 Jeff Add----Begin--判断工序是否存在,需要考虑半成品号+工序号
        //    DataRow[] dr;
        //    try
        //    {
        //        dr = DsJobEntry.Tables["JobOper"].Select("AssemblySeq=" + assemblynum + " and OprSeq=" + operSeq + "");
        //    }
        //    catch (Exception ex) { return ex.Message.ToString(); }
        //    WriteGetNewLaborInERPTxt("", EpicorSessionManager.EpicorSession.SessionID.ToString(), "1", "sessionid", "checkj");
        //    if (dr != null && dr.Length > 0)
        //    {
        //        //-----20151106 Jeff暂时不考虑外包工序,外包工序也在BPM中可以做转序
        //        if (dr[0]["SubContract"].ToString() == "True")
        //        {
        //            result = result + "外包工序,不需提交.";
        //            EpicorSessionManager.EpicorSession.Dispose();
        //            return result;
        //        }

        //        if ((Convert.ToDecimal(checkQty) + Convert.ToDecimal(dr[0]["QtyCompleted"])) > Convert.ToDecimal(dr[0]["RunQty"]))
        //        {
        //            result = result + "当前工序本次报检数+完成数量不能超过生产数量,当前工序生产数量为" + dr[0]["RunQty"].ToString() + ",请查询跟踪报表.";
        //            EpicorSessionManager.EpicorSession.Dispose();
        //            return result;
        //        }


        //    }
        //    else
        //    {
        //        result = result + "不存在该工序.";
        //        EpicorSessionManager.EpicorSession.Dispose();
        //        return result;
        //    }
        //    //-----20151106 Jeff Add----Begin


        //    //-----20151106 Jeff--取该工序的上一工序号 ---Begin
        //    Int32 preOperSeq;
        //    DataRow[] drPre = DsJobEntry.Tables["JobOper"].Select("AssemblySeq=" + assemblynum + " and OprSeq < " + operSeq + "", "OprSeq Desc");
        //    WriteGetNewLaborInERPTxt("", EpicorSessionManager.EpicorSession.SessionID.ToString(), "2", "sessionid", "checkj");
        //    if (drPre != null && drPre.Length > 0)
        //    {
        //        //-----该工序前面有工序,按工序序号降序排列,取第1条即为当前工序的上一工序
        //        preOperSeq = Convert.ToInt32(drPre[0]["OprSeq"]);

        //        DataRow[] drSub = DsJobEntry.Tables["JobOper"].Select("AssemblySeq=" + assemblynum + " and OprSeq=" + preOperSeq + "");
        //        if (drSub != null && drSub.Length > 0)
        //        {
        //            decimal QtyCompleted;

        //            QtyCompleted = Convert.ToDecimal(drSub[0]["QtyCompleted"]);

        //            decimal CurQtyCompleted;
        //            DataRow[] drCur = DsJobEntry.Tables["JobOper"].Select("AssemblySeq=" + assemblynum + " and OprSeq=" + operSeq + "");
        //            if (drCur != null && drCur.Length > 0)
        //            {
        //                CurQtyCompleted = Convert.ToDecimal(drCur[0]["QtyCompleted"]);

        //            }
        //            else
        //            {
        //                CurQtyCompleted = 0;
        //            }


        //            if ((Convert.ToDecimal(checkQty) + CurQtyCompleted) > QtyCompleted)
        //            {
        //                result = result + "当前工序报检数量+已完成数量不能超过上一道工序的完成数量,上一道工序<" + drSub[0]["OpDesc"].ToString() + ">完成数量为" + QtyCompleted + "";
        //                EpicorSessionManager.EpicorSession.Dispose();
        //                return result;
        //            }

        //            decimal QtyBPMRPT;

        //            QtyBPMRPT = Convert.ToDecimal(GetBPMRptQty(jobnum, assemblynum, operSeq.ToString(), binid, id, companyId));

        //            if ((Convert.ToDecimal(checkQty) + QtyBPMRPT) > QtyCompleted)
        //            {
        //                result = result + "当前工序本次报检数量+已提交报检数量不能超过上一道工序的完成数量,上道工序<" + drSub[0]["OpDesc"].ToString() + ">完成数量为" + QtyCompleted + ",已报检数量为" + QtyBPMRPT + "";
        //                EpicorSessionManager.EpicorSession.Dispose();
        //                return result;
        //            }

        //            //if ((Convert.ToDecimal(checkQty) + QtyBPMRPT + CurQtyCompleted) > Convert.ToDecimal(dr[0]["RunQty"]))
        //            //{
        //            //    result = result + "当前工序本次报检数量+已提交报检数量+已完成数量不能超过工序的生产数量,已提交报检数量为" + QtyBPMRPT + ",当前工序生产数量为" + dr[0]["RunQty"].ToString() + "";

        //            //    //20160224--Jeff--Being
        //            //    CommonClass.GetSession.DisposeSession();
        //            //    ConnectionPool.Dispose();
        //            //    //20160224--Jeff--End

        //            //    return result;
        //            //}


        //        }


        //    }
        //    else
        //    {
        //        //-----该工序没有上一工序    
        //        WriteGetNewLaborInERPTxt("", EpicorSessionManager.EpicorSession.SessionID.ToString(), "2", "sessionid", "checkj");
        //        decimal CurRunQty;
        //        DataRow[] drCurRun = DsJobEntry.Tables["JobOper"].Select("AssemblySeq=" + assemblynum + " and OprSeq=" + operSeq + "");
        //        if (drCurRun != null && drCurRun.Length > 0)
        //        {
        //            CurRunQty = Convert.ToDecimal(drCurRun[0]["RunQty"]);
        //        }
        //        else
        //        {
        //            CurRunQty = 0;
        //        }

        //        decimal QtyBPMRPT;

        //        QtyBPMRPT = Convert.ToDecimal(GetBPMRptQty(jobnum, assemblynum, operSeq.ToString(), binid, id, companyId));

        //        if ((Convert.ToDecimal(checkQty) + QtyBPMRPT) > CurRunQty)
        //        {
        //            result = result + "当前工序本次报检数量+已提交报检数量不能超过工单生产数量,当前工序生产数量为" + CurRunQty + ",已报检数量为" + QtyBPMRPT + "";
        //            EpicorSessionManager.EpicorSession.Dispose();
        //            return result;
        //        }

        //        //if ((Convert.ToDecimal(checkQty) + QtyBPMRPT + Convert.ToDecimal(drCurRun[0]["QtyCompleted"])) > CurRunQty)
        //        //{
        //        //    result = result + "当前工序本次报检数量+已提交报检数量+已完成数量不能超过工序的生产数量,已提交报检数量为" + QtyBPMRPT + ",当前工序生产数量为" + CurRunQty.ToString() + "";

        //        //    //20160224--Jeff--Being
        //        //    CommonClass.GetSession.DisposeSession();
        //        //    ConnectionPool.Dispose();
        //        //    //20160224--Jeff--End

        //        //    return result;
        //        //}


        //    }
        //    WriteGetNewLaborInERPTxt("", EpicorSessionManager.EpicorSession.SessionID.ToString(), "3", "sessionid", "checkj");
        //    //-----20151106 Jeff--取该工序的上一工序号 ---End
        //    EpicorSessionManager.EpicorSession.Dispose();
        //    return result;
        //}

        //获取工序描述,完成数量,下一道工序,下一道工序所在半成品
        //获取工序描述,完成数量,运行数量,下一道工序,下工序描述,下一道工序所在半成品,半成品描述，opmaster.character05
        //bo 
        //public string GetJobOperDescs(string jobNum, string assemblynum, string operSeq, string companyId)
        //{
        //    if ((jobNum != null && jobNum.Length > 0) == false)
        //    {
        //        return "无效工单#.,";
        //    }

        //    if ((assemblynum != null && assemblynum.Length > 0) == false)
        //    {
        //        return "无效半成品#.,";
        //    }

        //    if ((operSeq != null && operSeq.Length > 0) == false)
        //    {
        //        return "无效工序#.,";
        //    }
        //    string resultdata = ErpLogin/();
        //    if (resultdata != "true")
        //    {
        //        return "false|" + resultdata;
        //    }
        //    EpicorSessionManager.EpicorSession.CompanyID = companyId;
        //    JobEntryImpl JEadapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<JobEntryImpl>(EpicorSessionManager.EpicorSession, ImplBase<Erp.Contracts.JobEntrySvcContract>.UriPath);
        //    WarehseImpl whadapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<WarehseImpl>(EpicorSessionManager.EpicorSession, ImplBase<Erp.Contracts.WarehseSvcContract>.UriPath);
        //    OpMasterImpl omadapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<OpMasterImpl>(EpicorSessionManager.EpicorSession, ImplBase<Erp.Contracts.OpMasterSvcContract>.UriPath);
        //    try
        //    {
        //        string result = "";
        //        WarehseDataSet DsWarehse;
        //        JobEntryDataSet DsJobEntry = JEadapter.GetByID(jobNum);
        //        DataRow[] dr = DsJobEntry.Tables["JobOper"].Select("OprSeq=" + operSeq + " and AssemblySeq=" + assemblynum + "");
        //        if (dr.Length > 0)
        //        {
        //            string txtOpDesc;
        //            OpMasterDataSet masterDs = omadapter.GetByID(dr[0]["OpCode"].ToString());
        //            DataRow[] opdr = masterDs.Tables["OpMaster"].Select("OpCode='" + dr[0]["OpCode"].ToString() + "'");
        //            if (opdr != null && opdr.Length > 0)
        //            {
        //                txtOpDesc = opdr[0]["OpDesc"].ToString();
        //            }
        //            else
        //            {
        //                txtOpDesc = "";
        //            }
        //            result = result + txtOpDesc.ToString() + "," + dr[0]["QtyCompleted"].ToString() + "," + dr[0]["RunQty"].ToString() + ",";

        //        }
        //        else
        //        {
        //            result = result + ",,,";
        //        }

        //        //----20151106 Jeff 取下一工序--Add-Begin
        //        Int32 NextOperSeq;
        //        NextOperSeq = 0;
        //        DataRow[] drNext = DsJobEntry.Tables["JobOper"].Select("AssemblySeq=" + assemblynum + " and OprSeq > " + operSeq + "", "OprSeq ASC");
        //        if (drNext != null && drNext.Length > 0)
        //        {
        //            NextOperSeq = Convert.ToInt32(drNext[0]["OprSeq"]);
        //        }
        //        //----20151106 Jeff 取下一工序--Add-End


        //        DataRow[] ndr = DsJobEntry.Tables["JobOper"].Select("OprSeq=" + NextOperSeq + " and AssemblySeq=" + assemblynum + "");
        //        if (ndr.Length > 0)
        //        {

        //            //---20160227--Jeff--Begin
        //            string txtOpDesc;
        //            OpMasterDataSet masterDs = omadapter.GetByID(ndr[0]["OpCode"].ToString());
        //            DataRow[] opdr = masterDs.Tables["OpMaster"].Select("OpCode='" + ndr[0]["OpCode"].ToString() + "'");
        //            if (opdr != null && opdr.Length > 0)
        //            {
        //                txtOpDesc = opdr[0]["OpDesc"].ToString();
        //            }
        //            else
        //            {
        //                txtOpDesc = "";
        //            }
        //            //---20160227--Jeff--End


        //            //result = result + ndr[0]["OprSeq"].ToString() + "," + ndr[0]["OpDesc"].ToString() + ",";
        //            result = result + ndr[0]["OprSeq"].ToString() + "," + txtOpDesc.ToString() + ",";


        //            DataRow[] assemblydr = DsJobEntry.Tables["JobAsmbl"].Select("AssemblySeq=" + assemblynum + "");
        //            if (assemblydr.Length > 0)
        //            {
        //                string assemblydesc;
        //                assemblydesc = assemblydr[0]["Description"].ToString();
        //                assemblydesc = assemblydesc.Replace("\"", "");
        //                result = result + assemblydr[0]["AssemblySeq"].ToString() + "," + assemblydesc.ToString() + ",";

        //                //result = result + assemblydr[0]["AssemblySeq"].ToString() + "," + assemblydr[0]["Description"].ToString() + ",";


        //            }
        //            else
        //            {
        //                result = result + ",,";
        //            }
        //        }
        //        else
        //        {
        //            DataRow[] relateddr = DsJobEntry.Tables["JobAsmbl"].Select("AssemblySeq=" + assemblynum + "");
        //            if (relateddr != null && relateddr.Length > 0 && relateddr[0]["RelatedOperation"].ToString() != "0")
        //            {
        //                if (relateddr[0]["ParentAssemblySeq"].ToString() == "0")
        //                {
        //                    DataRow[] nextdr = DsJobEntry.Tables["JobOper"].Select("OprSeq=" + Convert.ToInt32(relateddr[0]["RelatedOperation"]) + " and AssemblySeq=" + relateddr[0]["ParentAssemblySeq"] + "");
        //                    if (nextdr.Length > 0)
        //                    {

        //                        //---20160227--Jeff--Begin
        //                        string txtOpDesc;
        //                        OpMasterDataSet masterDs = omadapter.GetByID(nextdr[0]["OpCode"].ToString());
        //                        DataRow[] opdr = masterDs.Tables["OpMaster"].Select("OpCode='" + nextdr[0]["OpCode"].ToString() + "'");
        //                        if (opdr != null && opdr.Length > 0)
        //                        {
        //                            txtOpDesc = opdr[0]["OpDesc"].ToString();
        //                        }
        //                        else
        //                        {
        //                            txtOpDesc = "";
        //                        }
        //                        //---20160227--Jeff--End


        //                        //result = result + nextdr[0]["OprSeq"].ToString() + "," + nextdr[0]["OpDesc"].ToString() + ",";
        //                        result = result + nextdr[0]["OprSeq"].ToString() + "," + txtOpDesc.ToString() + ",";


        //                        DataRow[] assemblydr = DsJobEntry.Tables["JobAsmbl"].Select("AssemblySeq=" + nextdr[0]["AssemblySeq"].ToString() + "");
        //                        if (assemblydr.Length > 0)
        //                        {


        //                            string assemblydesc;
        //                            assemblydesc = assemblydr[0]["Description"].ToString();
        //                            assemblydesc = assemblydesc.Replace("\"", "");
        //                            result = result + assemblydr[0]["AssemblySeq"].ToString() + "," + assemblydesc.ToString() + ",";

        //                            //result = result + assemblydr[0]["AssemblySeq"].ToString() + "," + assemblydr[0]["Description"].ToString() + ",";
        //                        }
        //                        else
        //                        {


        //                            result = result + ",,";
        //                        }
        //                    }
        //                    else
        //                    {
        //                        result = result + ",,,,";
        //                    }
        //                }
        //                else
        //                {
        //                    DataRow[] parentasm = DsJobEntry.Tables["JobAsmbl"].Select("AssemblySeq=" + assemblynum + " and ParentAssemblySeq=" + relateddr[0]["ParentAssemblySeq"].ToString() + "");
        //                    if (parentasm != null && parentasm.Length > 0 && parentasm[0]["RelatedOperation"].ToString() != "0")
        //                    {
        //                        DataRow[] nextparentdr = DsJobEntry.Tables["JobOper"].Select("OprSeq=" + Convert.ToInt32(parentasm[0]["RelatedOperation"]) + " and AssemblySeq=" + relateddr[0]["ParentAssemblySeq"].ToString() + "");
        //                        if (nextparentdr.Length > 0)
        //                        {
        //                            string txtOpDesc;
        //                            OpMasterDataSet masterDs = omadapter.GetByID(nextparentdr[0]["OpCode"].ToString());
        //                            DataRow[] opdr = masterDs.Tables["OpMaster"].Select("OpCode='" + nextparentdr[0]["OpCode"].ToString() + "'");
        //                            if (opdr != null && opdr.Length > 0)
        //                            {
        //                                txtOpDesc = opdr[0]["OpDesc"].ToString();
        //                            }
        //                            else
        //                            {
        //                                txtOpDesc = "";
        //                            }
        //                            result = result + nextparentdr[0]["OprSeq"].ToString() + "," + txtOpDesc.ToString() + ",";

        //                            DataRow[] assemblydr = DsJobEntry.Tables["JobAsmbl"].Select("AssemblySeq=" + nextparentdr[0]["AssemblySeq"].ToString() + "");
        //                            if (assemblydr.Length > 0)
        //                            {

        //                                string assemblydesc;
        //                                assemblydesc = assemblydr[0]["Description"].ToString();
        //                                assemblydesc = assemblydesc.Replace("\"", "");
        //                                result = result + assemblydr[0]["AssemblySeq"].ToString() + "," + assemblydesc.ToString() + ",";
        //                            }
        //                            else
        //                            {
        //                                result = result + ",,";
        //                            }
        //                        }
        //                        else
        //                        {
        //                            result = result + ",,,,";
        //                        }
        //                    }
        //                    else
        //                    {
        //                        result = result + ",,,,";
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                if (assemblynum == "0")
        //                {
        //                    if (DsJobEntry.Tables["JobProd"].Rows.Count > 0)
        //                    {

        //                        if (!string.IsNullOrEmpty(DsJobEntry.Tables["JobProd"].Rows[0]["WarehouseCode"].ToString()))
        //                        {
        //                            result = result + DsJobEntry.Tables["JobProd"].Rows[0]["WarehouseCode"].ToString() + ",";
        //                            DsWarehse = whadapter.GetByID(DsJobEntry.Tables["JobProd"].Rows[0]["WarehouseCode"].ToString());

        //                            result = result + DsWarehse.Tables["Warehse"].Rows[0]["Description"].ToString() + ",,,";
        //                        }
        //                        else
        //                        {
        //                            result = result + DsJobEntry.Tables["JobProd"].Rows[0]["OrderNum"].ToString() + ",";
        //                            result = result + "销售发货" + ",,,";
        //                        }
        //                    }
        //                    else
        //                    {
        //                        result = result + ",,,,";
        //                    }
        //                }
        //                else
        //                {
        //                    result = result + ",,,,";
        //                }
        //            }
        //        }

        //        if (dr.Length > 0)
        //        {
        //            OpMasterDataSet masterDs = omadapter.GetByID(dr[0]["OpCode"].ToString());
        //            DataRow[] opdr = masterDs.Tables["OpMaster"].Select("OpCode='" + dr[0]["OpCode"].ToString() + "'");
        //            if (opdr != null && opdr.Length > 0)
        //            {
        //                if (opdr[0]["Character05"].ToString() == "")
        //                {
        //                    result = result + "empty";
        //                }
        //                else
        //                {
        //                    result = result + opdr[0]["Character05"].ToString();
        //                }
        //            }
        //            else
        //            {
        //                result = result + "empty";
        //            }
        //        }
        //        else
        //        {
        //            result = result + "empty";
        //        }
        //        EpicorSessionManager.EpicorSession.Dispose();
        //        return result;
        //    }
        //    catch
        //    {
        //        EpicorSessionManager.EpicorSession.Dispose();
        //        return ",,,,";
        //    }


        //}

        //sql 
        public string GetJobOperDesc(string jobNum, string assemblynum, string operSeq, string companyId)
        {
            if ((jobNum != null && jobNum.Length > 0) == false)
            {
                return "无效工单#.,";
            }

            if ((assemblynum != null && assemblynum.Length > 0) == false)
            {
                return "无效半成品#.,";
            }

            if ((operSeq != null && operSeq.Length > 0) == false)
            {
                return "无效工序#.,";
            }
            //string resultdata = ErpLogin/();
            //if (resultdata != "true")
            //{
            //    return "false|" + resultdata;
            //}
            //EpicorSessionManager.EpicorSession.CompanyID = companyId;
            //JobEntryImpl JEadapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<JobEntryImpl>(EpicorSessionManager.EpicorSession, ImplBase<Erp.Contracts.JobEntrySvcContract>.UriPath);
            //WarehseImpl whadapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<WarehseImpl>(EpicorSessionManager.EpicorSession, ImplBase<Erp.Contracts.WarehseSvcContract>.UriPath);
            //OpMasterImpl omadapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<OpMasterImpl>(EpicorSessionManager.EpicorSession, ImplBase<Erp.Contracts.OpMasterSvcContract>.UriPath);
            try
            {
                string result = "";
                //WarehseDataSet DsWarehse;
                //JobEntryDataSet DsJobEntry = JEadapter.GetByID(jobNum);
                //DataRow[] dr = DsJobEntry.Tables["JobOper"].Select("OprSeq=" + operSeq + " and AssemblySeq=" + assemblynum + "");
                DataTable operdt = GetDataByERP("select jo.OpCode,jo.OprSeq,jo.QtyCompleted,jo.RunQty,om.OpDesc from erp.JobOper jo inner join erp.OpMaster om on jo.Company=om.Company and jo.OpCode=om.OpCode where jo.Company='" + companyId + "' and jo.JobNum='" + jobNum + "' and jo.AssemblySeq='" + assemblynum + "' and jo.OprSeq='" + operSeq + "'");
                if (operdt != null && operdt.Rows.Count > 0)
                {
                    string txtOpDesc;
                    //OpMasterDataSet masterDs = omadapter.GetByID(dr[0]["OpCode"].ToString());
                    //DataRow[] opdr = masterDs.Tables["OpMaster"].Select("OpCode='" + dr[0]["OpCode"].ToString() + "'");
                    if (operdt.Rows[0]["OpDesc"].ToString() != null && operdt.Rows[0]["OpDesc"].ToString().Length > 0)
                    {
                        txtOpDesc = operdt.Rows[0]["OpDesc"].ToString();
                    }
                    else
                    {
                        txtOpDesc = "";
                    }
                    result = result + txtOpDesc.ToString() + "," + operdt.Rows[0]["QtyCompleted"].ToString() + "," + operdt.Rows[0]["RunQty"].ToString() + ",";

                }
                else
                {
                    result = result + ",,,";
                }

                //----20151106 Jeff 取下一工序--Add-Begin
                Int32 NextOperSeq;
                NextOperSeq = 0;
                //DataRow[] drNext = DsJobEntry.Tables["JobOper"].Select("AssemblySeq=" + assemblynum + " and OprSeq > " + operSeq + "", "OprSeq ASC");
                DataTable nextdt = GetDataByERP("select OprSeq from erp.JobOper jo where jo.Company='" + companyId + "' and jo.JobNum='" + jobNum + "' and jo.AssemblySeq='" + assemblynum + "' and jo.OprSeq > '" + operSeq + "' order by jo.OprSeq ASC");
                if (nextdt != null && nextdt.Rows.Count > 0)
                {
                    NextOperSeq = Convert.ToInt32(nextdt.Rows[0]["OprSeq"]);
                }
                //----20151106 Jeff 取下一工序--Add-End


                //DataRow[] ndr = DsJobEntry.Tables["JobOper"].Select("OprSeq=" + NextOperSeq + " and AssemblySeq=" + assemblynum + "");
                DataTable ndt = GetDataByERP("select jo.OpCode,jo.OprSeq,om.OpDesc from erp.JobOper jo inner join erp.OpMaster om on jo.Company=om.Company and jo.OpCode=om.OpCode where jo.Company='" + companyId + "' and jo.JobNum='" + jobNum + "' and jo.AssemblySeq='" + assemblynum + "' and jo.OprSeq='" + NextOperSeq + "'");
                if (ndt != null && ndt.Rows.Count > 0)
                {

                    //---20160227--Jeff--Begin
                    string txtOpDesc;
                    //OpMasterDataSet masterDs = omadapter.GetByID(ndr[0]["OpCode"].ToString());
                    //DataRow[] opdr = masterDs.Tables["OpMaster"].Select("OpCode='" + ndr[0]["OpCode"].ToString() + "'");
                    if (ndt != null && ndt.Rows.Count > 0)
                    {
                        txtOpDesc = ndt.Rows[0]["OpDesc"].ToString();
                    }
                    else
                    {
                        txtOpDesc = "";
                    }
                    //---20160227--Jeff--End


                    //result = result + ndr[0]["OprSeq"].ToString() + "," + ndr[0]["OpDesc"].ToString() + ",";
                    result = result + ndt.Rows[0]["OprSeq"].ToString() + "," + txtOpDesc.ToString() + ",";


                    //DataRow[] assemblydr = DsJobEntry.Tables["JobAsmbl"].Select("AssemblySeq=" + assemblynum + "");
                    DataTable asmdt = GetDataByERP("select AssemblySeq,Description from erp.JobAsmbl where Company='" + companyId + "' and JobNum='" + jobNum + "' and AssemblySeq='" + assemblynum + "'");
                    if (asmdt != null && asmdt.Rows.Count > 0)
                    {
                        string assemblydesc;
                        assemblydesc = asmdt.Rows[0]["Description"].ToString();
                        assemblydesc = assemblydesc.Replace("\"", "");
                        result = result + asmdt.Rows[0]["AssemblySeq"].ToString() + "," + assemblydesc.ToString() + ",";

                        //result = result + assemblydr[0]["AssemblySeq"].ToString() + "," + assemblydr[0]["Description"].ToString() + ",";


                    }
                    else
                    {
                        result = result + ",,";
                    }
                }
                else
                {
                    //DataRow[] relateddr = DsJobEntry.Tables["JobAsmbl"].Select("AssemblySeq=" + assemblynum + "");
                    DataTable relatedt = GetDataByERP("select RelatedOperation,Parent ParentAssemblySeq from erp.JobAsmbl where Company='" + companyId + "' and JobNum='" + jobNum + "' and AssemblySeq='" + assemblynum + "'");
                    if (relatedt != null && relatedt.Rows.Count > 0 && relatedt.Rows[0]["RelatedOperation"].ToString() != "0")
                    {
                        if (relatedt.Rows[0]["ParentAssemblySeq"].ToString() == "0")
                        {
                            //DataRow[] nextdr = DsJobEntry.Tables["JobOper"].Select("OprSeq=" + Convert.ToInt32(relateddr[0]["RelatedOperation"]) + " and AssemblySeq=" + relateddr[0]["ParentAssemblySeq"] + "");
                            DataTable nextdrdt = GetDataByERP("select jo.OpCode,jo.OprSeq,om.OpDesc,jo.AssemblySeq from erp.JobOper jo inner join erp.OpMaster om on jo.Company=om.Company and jo.OpCode=om.OpCode where jo.Company='" + companyId + "' and jo.JobNum='" + jobNum + "' and jo.AssemblySeq='" + relatedt.Rows[0]["ParentAssemblySeq"] + "' and jo.OprSeq='" + Convert.ToInt32(relatedt.Rows[0]["RelatedOperation"]) + "'");
                            if (nextdrdt != null && nextdrdt.Rows.Count > 0)
                            {

                                //---20160227--Jeff--Begin
                                string txtOpDesc;
                                //OpMasterDataSet masterDs = omadapter.GetByID(nextdr[0]["OpCode"].ToString());
                                //DataRow[] opdr = masterDs.Tables["OpMaster"].Select("OpCode='" + nextdr[0]["OpCode"].ToString() + "'");
                                if (nextdrdt != null && nextdrdt.Rows.Count > 0)
                                {
                                    txtOpDesc = nextdrdt.Rows[0]["OpDesc"].ToString();
                                }
                                else
                                {
                                    txtOpDesc = "";
                                }
                                //---20160227--Jeff--End


                                //result = result + nextdr[0]["OprSeq"].ToString() + "," + nextdr[0]["OpDesc"].ToString() + ",";
                                result = result + nextdrdt.Rows[0]["OprSeq"].ToString() + "," + txtOpDesc.ToString() + ",";


                                //DataRow[] assemblydr = DsJobEntry.Tables["JobAsmbl"].Select("AssemblySeq=" + nextdr[0]["AssemblySeq"].ToString() + "");
                                DataTable assemblydrdt = GetDataByERP("select AssemblySeq,Description from erp.JobAsmbl where Company='" + companyId + "' and JobNum='" + jobNum + "' and AssemblySeq='" + nextdrdt.Rows[0]["AssemblySeq"].ToString() + "'");
                                if (assemblydrdt != null && assemblydrdt.Rows.Count > 0)
                                {


                                    string assemblydesc;
                                    assemblydesc = assemblydrdt.Rows[0]["Description"].ToString();
                                    assemblydesc = assemblydesc.Replace("\"", "");
                                    result = result + assemblydrdt.Rows[0]["AssemblySeq"].ToString() + "," + assemblydesc.ToString() + ",";

                                    //result = result + assemblydr[0]["AssemblySeq"].ToString() + "," + assemblydr[0]["Description"].ToString() + ",";
                                }
                                else
                                {


                                    result = result + ",,";
                                }
                            }
                            else
                            {
                                result = result + ",,,,";
                            }
                        }
                        else
                        {
                            //DataRow[] parentasm = DsJobEntry.Tables["JobAsmbl"].Select("AssemblySeq=" + assemblynum + " and ParentAssemblySeq=" + relateddr[0]["ParentAssemblySeq"].ToString() + "");
                            DataTable parentasmdt = GetDataByERP("select AssemblySeq,Description,RelatedOperation from erp.JobAsmbl where Company='" + companyId + "' and JobNum='" + jobNum + "' and AssemblySeq='" + assemblynum + "' and Parent='" + relatedt.Rows[0]["ParentAssemblySeq"].ToString() + "' ");
                            if (parentasmdt != null && parentasmdt.Rows.Count > 0 && parentasmdt.Rows[0]["RelatedOperation"].ToString() != "0")
                            {
                                //DataRow[] nextparentdr = DsJobEntry.Tables["JobOper"].Select("OprSeq=" + Convert.ToInt32(parentasm[0]["RelatedOperation"]) + " and AssemblySeq=" + relateddr[0]["ParentAssemblySeq"].ToString() + "");
                                DataTable nextparentdrdt = GetDataByERP("select jo.OpCode,jo.OprSeq,om.OpDesc,jo.AssemblySeq from erp.JobOper jo inner join erp.OpMaster om on jo.Company=om.Company and jo.OpCode=om.OpCode where jo.Company='" + companyId + "' and jo.JobNum='" + jobNum + "' and jo.AssemblySeq='" + relatedt.Rows[0]["ParentAssemblySeq"].ToString() + "' and jo.OprSeq='" + Convert.ToInt32(parentasmdt.Rows[0]["RelatedOperation"]) + "'");
                                if (nextparentdrdt != null && nextparentdrdt.Rows.Count > 0)
                                {
                                    string txtOpDesc;
                                    //OpMasterDataSet masterDs = omadapter.GetByID(nextparentdr[0]["OpCode"].ToString());
                                    //DataRow[] opdr = masterDs.Tables["OpMaster"].Select("OpCode='" + nextparentdr[0]["OpCode"].ToString() + "'");
                                    if (nextparentdrdt != null && nextparentdrdt.Rows.Count > 0)
                                    {
                                        txtOpDesc = nextparentdrdt.Rows[0]["OpDesc"].ToString();
                                    }
                                    else
                                    {
                                        txtOpDesc = "";
                                    }
                                    result = result + nextparentdrdt.Rows[0]["OprSeq"].ToString() + "," + txtOpDesc.ToString() + ",";

                                    //DataRow[] assemblydr = DsJobEntry.Tables["JobAsmbl"].Select("AssemblySeq=" + nextparentdr[0]["AssemblySeq"].ToString() + "");
                                    DataTable assemblydrdt = GetDataByERP("select AssemblySeq,Description from erp.JobAsmbl where Company='" + companyId + "' and JobNum='" + jobNum + "' and AssemblySeq='" + nextparentdrdt.Rows[0]["AssemblySeq"].ToString() + "'");
                                    if (assemblydrdt != null && assemblydrdt.Rows.Count > 0)
                                    {

                                        string assemblydesc;
                                        assemblydesc = assemblydrdt.Rows[0]["Description"].ToString();
                                        assemblydesc = assemblydesc.Replace("\"", "");
                                        result = result + assemblydrdt.Rows[0]["AssemblySeq"].ToString() + "," + assemblydesc.ToString() + ",";
                                    }
                                    else
                                    {
                                        result = result + ",,";
                                    }
                                }
                                else
                                {
                                    result = result + ",,,,";
                                }
                            }
                            else
                            {
                                result = result + ",,,,";
                            }
                        }
                    }
                    else
                    {
                        if (assemblynum == "0")
                        {
                            DataTable jobProddt = GetDataByERP("select WarehouseCode,OrderNum from erp.JobProd where  Company='" + companyId + "' and JobNum='" + jobNum + "'");
                            if (jobProddt != null && jobProddt.Rows.Count > 0)
                            {

                                if (!string.IsNullOrEmpty(jobProddt.Rows[0]["WarehouseCode"].ToString()))
                                {
                                    result = result + jobProddt.Rows[0]["WarehouseCode"].ToString() + ",";
                                    DataTable warehsedt = GetDataByERP("select Description from erp.Warehse where Company='" + companyId + "' and WarehouseCode='" + jobProddt.Rows[0]["WarehouseCode"].ToString() + "'");
                                    //DsWarehse = whadapter.GetByID(DsJobEntry.Tables["JobProd"].Rows[0]["WarehouseCode"].ToString());

                                    result = result + warehsedt.Rows[0]["Description"].ToString() + ",,,";
                                }
                                else
                                {
                                    result = result + jobProddt.Rows[0]["OrderNum"].ToString() + ",";
                                    result = result + "销售发货" + ",,,";
                                }
                            }
                            else
                            {
                                result = result + ",,,,";
                            }
                        }
                        else
                        {
                            result = result + ",,,,";
                        }
                    }
                }

                if (operdt.Rows.Count > 0)
                {
                    //OpMasterDataSet masterDs = omadapter.GetByID(operdt.Rows[0]["OpCode"].ToString());
                    //DataRow[] opdr = masterDs.Tables["OpMaster"].Select("OpCode='" + dr[0]["OpCode"].ToString() + "'");
                    DataTable opdrdt = GetDataByERP("select Character05 from OpMaster where Company='" + companyId + "' and OpCode='" + operdt.Rows[0]["OpCode"].ToString() + "'");
                    if (opdrdt != null && opdrdt.Rows.Count > 0)
                    {
                        if (opdrdt.Rows[0]["Character05"].ToString() == "")
                        {
                            result = result + "empty";
                        }
                        else
                        {
                            result = result + opdrdt.Rows[0]["Character05"].ToString();
                        }
                    }
                    else
                    {
                        result = result + "empty";
                    }
                }
                else
                {
                    result = result + "empty";
                }

                return result;
            }
            catch
            {

                return ",,,,";
            }


        }


        ////取得porel去向 R:申购物料  W:仓库 T:工单物料对应的工序  ,工单下工序类型:P仓库，S外协,M自制
        public string poDes(int ponum, int poline, int porel, string companyId)
        {
            try
            {
                List<QueryWhereItemRow> whereItems = new List<QueryWhereItemRow>();
                whereItems.Add(new QueryWhereItemRow() { TableID = "porel", FieldName = "ponum", RValue = ponum.ToString() });
                whereItems.Add(new QueryWhereItemRow() { TableID = "porel", FieldName = "poline", RValue = poline.ToString() });
                whereItems.Add(new QueryWhereItemRow() { TableID = "porel", FieldName = "porelnum", RValue = porel.ToString() });
                string sql = "select [PORel].[PONum] as [PORel_PONum],[PORel].[POLine] as [PORel_POLine],[PORel].[PORelNum] as [PORel_PORelNum],[PODetail].[PartNum] as [PODetail_PartNum],[Part].[NonStock] as [Part_NonStock],[PORel].[JobNum] as [PORel_JobNum],[PORel].[AssemblySeq] as [PORel_AssemblySeq],[PORel].[JobSeqType] as [PORel_JobSeqType],[PORel].[JobSeq] as [PORel_JobSeq],[ReqDetail].[ReqNum] as [ReqDetail_ReqNum],[ReqDetail].[ReqLine] as [ReqDetail_ReqLine],[ReqHead].[RequestorID] as [ReqHead_RequestorID],[UserFile].[Name] as [UserFile_Name],[PartPlant].[PrimWhse] as [PartPlant_PrimWhse],[Warehse].[Description] as [Warehse_Description],[JobOper].[OprSeq] as [JobOper_OprSeq],[OpMaster].[OpDesc] as [OpMaster_OpDesc],[PORel].[TranType] as [PORel_TranType],[PODetail].[VendorNum] as [PODetail_VendorNum],[ReqDetail].[Character10] as [ReqDetail_Character10] from Erp.PORel as PORel inner join Erp.PODetail as PODetail on PORel.Company = PODetail.Company and PORel.PONUM = PODetail.PONum and PORel.POLine = PODetail.POLine left outer join Erp.Part as Part on PODetail.Company = Part.Company and PODetail.PartNum = Part.PartNum left outer join Erp.PartPlant as PartPlant on Part.Company = PartPlant.Company and Part.PartNum = PartPlant.PartNum ";
                sql = sql + " left outer join Erp.Warehse as Warehse on PartPlant.Company = Warehse.Company and PartPlant.PrimWhse = Warehse.WarehouseCode left outer join ReqDetail as ReqDetail on PODetail.Company = ReqDetail.Company and PODetail.PONUM = ReqDetail.PONUM and PODetail.POLine = ReqDetail.POLine left outer join Erp.ReqHead as ReqHead on ReqDetail.Company = ReqHead.Company and ReqDetail.ReqNum = ReqHead.ReqNum left outer join Erp.UserFile as UserFile on ReqHead.RequestorID = UserFile.DcdUserID left outer join Erp.JobMtl as JobMtl on PORel.Company = JobMtl.Company and PORel.JobNum = JobMtl.JobNum and PORel.AssemblySeq = JobMtl.AssemblySeq and PORel.JobSeq = JobMtl.MtlSeq left outer join Erp.JobOper as JobOper on JobMtl.Company = JobOper.Company and JobMtl.JobNum = JobOper.JobNum and JobMtl.AssemblySeq = JobOper.AssemblySeq and JobMtl.RelatedOperation = JobOper.OprSeq left outer join Erp.OpMaster as OpMaster on JobOper.Company = OpMaster.Company and JobOper.OpCode = OpMaster.OpCode where (PORel.Company = '" + companyId + "'  and PORel.PONum = '" + ponum + "'  and PORel.POLine ='" + poline + "'  and PORel.PORelNum ='" + porel + "')";
                DataTable dt = GetDataByERP(sql);
                //DataTable dt = BaqResult("001-porelDes", whereItems, 0, companyId);

                //PORel.PONum
                //PORel.POLine
                //PORel.PORelNum
                //PODetail.PartNum
                //Part.NonStock
                //PORel.JobNum
                //PORel.AssemblySeq
                //PORel.JobSeqType
                //PORel.JobSeq
                //ReqDetail.ReqNum
                //ReqDetail.ReqLine
                //ReqHead.RequestorID
                //PartPlant.PrimWhse
                //Warehse.Description
                //JobOper.OprSeq
                //OpMaster.OpDesc

                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    dt.Columns[i].ColumnName = dt.Columns[i].ColumnName.Replace('_', '.');
                }

                string jobnum = "", jobseqType = "", tranType = "";
                bool nonStock = false;
                int reqnum = 0, asmSeq = 0, oprSeq = 0, vendornum = 0;
                if (dt.Rows.Count > 0)
                {
                    jobnum = dt.Rows[0]["PORel.JobNum"].ToString().Trim();
                    jobseqType = dt.Rows[0]["PORel.JobSeqType"].ToString().Trim().ToLower();
                    tranType = dt.Rows[0]["PORel.TranType"].ToString().Trim().ToLower();
                    //nonStock = Convert.ToBoolean(dt.Rows[0]["Part.NonStock"].ToString().Trim());
                    bool.TryParse(dt.Rows[0]["Part.NonStock"].ToString().Trim(), out nonStock);
                    //reqnum = Convert.ToInt32(dt.Rows[0]["ReqDetail.ReqNum"].ToString().Trim());
                    int.TryParse(dt.Rows[0]["ReqDetail.ReqNum"].ToString().Trim(), out reqnum);
                    int.TryParse(dt.Rows[0]["PORel.JobSeq"].ToString().Trim(), out oprSeq);
                    int.TryParse(dt.Rows[0]["PORel.AssemblySeq"].ToString().Trim(), out asmSeq);
                    int.TryParse(dt.Rows[0]["podetail.vendornum"].ToString().Trim(), out vendornum);
                }

                //工单为空时，表示为采购至仓库
                if (jobnum == "")
                {
                    if (tranType == "pur-ukn")  //杂项采购一定是通过需求输入进行
                    {

                        return "R|物料接收人:" + dt.Rows[0]["ReqDetail.Character10"].ToString().Trim();
                    }  //837133-1-1
                    else
                    {

                        return "W|仓库:" + dt.Rows[0]["Warehse.Description"].ToString().Trim();
                    }


                }
                else //工单不为空时，表示为采购至job
                {
                    if (jobseqType == "m") //采购到工单物料
                    {

                        return "T|工单物料:" + dt.Rows[0]["OpMaster.OpDesc"].ToString().Trim();
                    }
                    else
                    {

                        string ss = "";
                        int nextAsm = 0, nextOprSeq = 0;
                        ss = getJobNextOprTypes(jobnum, asmSeq, oprSeq, out nextAsm, out nextOprSeq, companyId);
                        if (ss.Substring(0, 1).Trim().ToLower() == "s")
                        {
                            //取得外协工序对应的po的供应商id
                            //return "s|" + jobnum + "-" + nextAsm + "-" + nextOprSeq;
                            List<QueryWhereItemRow> whereItems2 = new List<QueryWhereItemRow>();
                            whereItems2.Add(new QueryWhereItemRow() { TableID = "porel", FieldName = "jobnum", RValue = jobnum.ToString() });
                            whereItems2.Add(new QueryWhereItemRow() { TableID = "porel", FieldName = "assemblyseq", RValue = nextAsm.ToString() });
                            whereItems2.Add(new QueryWhereItemRow() { TableID = "porel", FieldName = "jobseq", RValue = nextOprSeq.ToString() });
                            //DataTable dt2 = BaqResult("001-porelDes", whereItems2, 0, companyId);
                            string relsql = "select [PORel].[PONum] as [PORel_PONum],[PORel].[POLine] as [PORel_POLine],[PORel].[PORelNum] as [PORel_PORelNum],[PODetail].[PartNum] as [PODetail_PartNum],[Part].[NonStock] as [Part_NonStock],[PORel].[JobNum] as [PORel_JobNum],[PORel].[AssemblySeq] as [PORel_AssemblySeq],[PORel].[JobSeqType] as [PORel_JobSeqType],[PORel].[JobSeq] as [PORel_JobSeq],[ReqDetail].[ReqNum] as [ReqDetail_ReqNum],[ReqDetail].[ReqLine] as [ReqDetail_ReqLine],[ReqHead].[RequestorID] as [ReqHead_RequestorID],[UserFile].[Name] as [UserFile_Name],[PartPlant].[PrimWhse] as [PartPlant_PrimWhse],[Warehse].[Description] as [Warehse_Description],[JobOper].[OprSeq] as [JobOper_OprSeq],[OpMaster].[OpDesc] as [OpMaster_OpDesc],[PORel].[TranType] as [PORel_TranType],[PODetail].[VendorNum] as [PODetail_VendorNum],[ReqDetail].[Character10] as [ReqDetail_Character10] from Erp.PORel as PORel inner join Erp.PODetail as PODetail on PORel.Company = PODetail.Company and PORel.PONUM = PODetail.PONum and PORel.POLine = PODetail.POLine left outer join Erp.Part as Part on PODetail.Company = Part.Company and PODetail.PartNum = Part.PartNum left outer join Erp.PartPlant as PartPlant on Part.Company = PartPlant.Company and Part.PartNum = PartPlant.PartNum left outer join Erp.Warehse as Warehse on PartPlant.Company = Warehse.Company and PartPlant.PrimWhse = Warehse.WarehouseCode left outer join ReqDetail as ReqDetail on PODetail.Company = ReqDetail.Company and PODetail.PONUM = ReqDetail.PONUM and PODetail.POLine = ReqDetail.POLine left outer join Erp.ReqHead as ReqHead on ReqDetail.Company = ReqHead.Company and ReqDetail.ReqNum = ReqHead.ReqNum left outer join Erp.UserFile as UserFile on ReqHead.RequestorID = UserFile.DcdUserID left outer join Erp.JobMtl as JobMtl on PORel.Company = JobMtl.Company and PORel.JobNum = JobMtl.JobNum and PORel.AssemblySeq = JobMtl.AssemblySeq and PORel.JobSeq = JobMtl.MtlSeq left outer join Erp.JobOper as JobOper on JobMtl.Company = JobOper.Company and JobMtl.JobNum = JobOper.JobNum and JobMtl.AssemblySeq = JobOper.AssemblySeq and JobMtl.RelatedOperation = JobOper.OprSeq left outer join Erp.OpMaster as OpMaster on JobOper.Company = OpMaster.Company and JobOper.OpCode = OpMaster.OpCode where (PORel.Company = '" + companyId + "'  and PORel.PONum ='" + ponum + "'  and PORel.POLine ='" + poline + "'  and PORel.PORelNum ='" + porel + "')";
                            DataTable dt2 = GetDataByERP(relsql);
                            for (int i = 0; i < dt2.Columns.Count; i++)
                            {
                                dt2.Columns[i].ColumnName = dt2.Columns[i].ColumnName.Replace('_', '.');
                            }
                            //qdDS = dynamicQuery.GetDashBoardQuery("001-porelVend");

                            //qdDS.QueryWhereItem.DefaultView.RowFilter = "DataTableID='porel' and FieldName='jobnum'";
                            //qdDS.QueryWhereItem.DefaultView[0]["RValue"] = jobnum;

                            //qdDS.QueryWhereItem.DefaultView.RowFilter = "DataTableID='porel' and FieldName='assemblyseq'";
                            //qdDS.QueryWhereItem.DefaultView[0]["RValue"] = nextAsm;

                            //qdDS.QueryWhereItem.DefaultView.RowFilter = "DataTableID='porel' and FieldName='jobseq'";
                            //qdDS.QueryWhereItem.DefaultView[0]["RValue"] = nextOprSeq;

                            //DataTable dt2 = dynamicQuery.ExecuteDashBoardQuery(qdDS).Tables[0];


                            if (dt2.Rows.Count > 0)
                            {
                                int ven = 0;
                                string nponum = "", npoline = "", nporel = "";
                                nponum = dt2.Rows[0]["porel.ponum"].ToString();
                                npoline = dt2.Rows[0]["porel.poline"].ToString();
                                nporel = dt2.Rows[0]["porel.porelnum"].ToString();
                                int.TryParse(dt2.Rows[0]["PODetail.vendornum"].ToString(), out ven);
                                if (ven == vendornum)
                                {
                                    return "S1|下工序外协，供应商相同,当前流程自动结束，请发起下工序的采购验收流程，对应的po为:" + nponum + "-" + npoline + "-" + nporel;
                                }
                                else
                                {
                                    return "S2|下工序外协，供应商不同,转由外协人员处理,po:" + nponum + "-" + npoline + "-" + nporel;
                                }
                            }
                            else
                            {

                                return "S2|下工序外协，但没有找到对应的po,转由外协人员处理";
                            }
                        }
                        else
                        {
                            return ss;
                        }


                    } //采购到工序，返回下工序的去向


                }
            }
            catch (Exception ex)
            {

                return "0|" + ex.Message.ToString();
            }


        }



        // 取得工单下工序类型:P仓库，S外协,M自制
        //public string getJobNextOprType(string jobnum, int asmSeq, int oprseq, out int OutAsm, out int OutOprSeq, string companyId)
        //{
        //    string stype = "";
        //    OutAsm = 0;
        //    OutOprSeq = 0;
        //    if (jobnum.Trim() == "") { return "0|jonum error"; }

        //    string resultdata = ErpLogin/();
        //    if (resultdata != "true")
        //    {
        //        return "0|" + resultdata;
        //    }
        //    EpicorSessionManager.EpicorSession.CompanyID = companyId;
        //    JobEntryImpl jobEntry = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<JobEntryImpl>(EpicorSessionManager.EpicorSession, ImplBase<Erp.Contracts.JobEntrySvcContract>.UriPath);
        //    try
        //    {
        //        JobEntryDataSet DsJobEntry;
        //        DsJobEntry = jobEntry.GetByID(jobnum);
        //        int NextOperSeq = 0;
        //        DataRow[] drNext;
        //        if (asmSeq == 0) //0层半层品
        //        {
        //            drNext = DsJobEntry.Tables["JobOper"].Select("AssemblySeq=" + asmSeq + " and OprSeq > " + oprseq + "", "OprSeq ASC");
        //            //DataTable drNextdt=GetDataByERP
        //            if (drNext != null && drNext.Length > 0) //当前半成品内有下工序
        //            {
        //                NextOperSeq = Convert.ToInt32(drNext[0]["OprSeq"]);
        //                if (Convert.ToBoolean(drNext[0]["SubContract"]))
        //                {
        //                    stype = "S|下工序外协:" + drNext[0]["Opdesc"].ToString();
        //                    OutAsm = asmSeq;
        //                    OutOprSeq = NextOperSeq;

        //                }
        //                else
        //                { stype = "M|下工序:" + drNext[0]["Opdesc"].ToString(); }
        //            }
        //            else
        //            ////0层半层品内无下工序，代表下面到仓库,暂不考虑工单生产到工单的情况
        //            { stype = "P|工序完成，收货至仓库:" + DsJobEntry.Tables["JobProd"].Rows[0]["WarehouseCodeDescription"].ToString(); }
        //        }
        //        else //上层还有半成品
        //        {

        //            drNext = DsJobEntry.Tables["JobOper"].Select("AssemblySeq=" + asmSeq + " and OprSeq > " + oprseq + "", "OprSeq ASC");
        //            if (drNext != null && drNext.Length > 0) //当前半成品内有下工序
        //            {
        //                NextOperSeq = Convert.ToInt32(drNext[0]["OprSeq"]);
        //                if (Convert.ToBoolean(drNext[0]["SubContract"]))
        //                {
        //                    stype = "S|下工序外协" + drNext[0]["Opdesc"].ToString();
        //                    OutAsm = asmSeq;
        //                    OutOprSeq = NextOperSeq;

        //                }
        //                else
        //                { stype = "M|下工序:" + drNext[0]["Opdesc"].ToString(); }
        //            }
        //            else
        //            //非0层半层品内无下工序，还要到上层半层品中找第一个工序.
        //            {
        //                DataRow[] relateddr = DsJobEntry.Tables["JobAsmbl"].Select("AssemblySeq=" + asmSeq + "");
        //                if (relateddr != null && relateddr.Length > 0)
        //                {

        //                    NextOperSeq = Convert.ToInt32(relateddr[0]["RelatedOperation"]);
        //                    int parAsmSeq = Convert.ToInt32(relateddr[0]["ParentAssemblySeq"]);
        //                    if (NextOperSeq == 0)
        //                    {
        //                        EpicorSessionManager.EpicorSession.Dispose();
        //                        return "0|本半成品没有关联到父半成品的工序,取不到下工序类型";

        //                    }
        //                    else
        //                    {
        //                        //取父半成品相关工序的类型
        //                        drNext = DsJobEntry.Tables["JobOper"].Select("AssemblySeq=" + parAsmSeq + " and OprSeq = " + NextOperSeq + "");
        //                        if (drNext != null && drNext.Length > 0)
        //                        {
        //                            NextOperSeq = Convert.ToInt32(drNext[0]["OprSeq"]);
        //                            if (Convert.ToBoolean(drNext[0]["SubContract"]))
        //                            {
        //                                stype = "S|下工序外协" + drNext[0]["Opdesc"].ToString();
        //                                OutAsm = parAsmSeq;
        //                                OutOprSeq = NextOperSeq;

        //                            }
        //                            else
        //                            { stype = "M|下工序:" + drNext[0]["Opdesc"].ToString(); }
        //                        }

        //                    }


        //                }


        //            }
        //        }

        //        EpicorSessionManager.EpicorSession.Dispose();
        //        return stype;
        //    }
        //    catch (Exception e)
        //    {
        //        EpicorSessionManager.EpicorSession.Dispose();
        //        return "0|" + e.Message.ToString();
        //        //return "";
        //    }


        //}

        // 取得工单下工序类型:P仓库，S外协,M自制
        public string getJobNextOprTypes(string jobnum, int asmSeq, int oprseq, out int OutAsm, out int OutOprSeq, string companyId)
        {
            string stype = "";
            OutAsm = 0;
            OutOprSeq = 0;
            if (jobnum.Trim() == "") { return "0|jonum error"; }

            //string resultdata = ErpLogi/n();
            //if (resultdata != "true")
            //{
            //    return "false|" + resultdata;
            //}
            //EpicorSessionManager.EpicorSession.CompanyID = companyId;
            //JobEntryImpl jobEntry = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<JobEntryImpl>(EpicorSessionManager.EpicorSession, ImplBase<Erp.Contracts.JobEntrySvcContract>.UriPath);
            try
            {
                //JobEntryDataSet DsJobEntry;
                //DsJobEntry = jobEntry.GetByID(jobnum);
                int NextOperSeq = 0;
                //DataRow[] drNext;
                if (asmSeq == 0) //0层半层品
                {
                    //drNext = DsJobEntry.Tables["JobOper"].Select("AssemblySeq=" + asmSeq + " and OprSeq > " + oprseq + "", "OprSeq ASC");
                    DataTable drNextdt = GetDataByERP("select OpCode,OprSeq,SubContract,Opdesc from erp.JobOper where Company='" + companyId + "' and JobNum='" + jobnum + "' and AssemblySeq='" + asmSeq + "' and OprSeq > '" + oprseq + "' order by OprSeq ASC ");
                    if (drNextdt != null && drNextdt.Rows.Count > 0) //当前半成品内有下工序
                    {
                        NextOperSeq = Convert.ToInt32(drNextdt.Rows[0]["OprSeq"]);
                        if (Convert.ToBoolean(drNextdt.Rows[0]["SubContract"]))
                        {
                            stype = "S|下工序外协:" + drNextdt.Rows[0]["Opdesc"].ToString();
                            OutAsm = asmSeq;
                            OutOprSeq = NextOperSeq;

                        }
                        else
                        { stype = "M|下工序:" + drNextdt.Rows[0]["Opdesc"].ToString(); }
                    }
                    else
                    ////0层半层品内无下工序，代表下面到仓库,暂不考虑工单生产到工单的情况
                    {
                        DataTable jobProddt = GetDataByERP("select WarehouseCode WarehouseCodeDescription from erp.JobProd where Company='" + companyId + "' and JobNum='" + jobnum + "'");
                        stype = "P|工序完成，收货至仓库:" + jobProddt.Rows[0]["WarehouseCodeDescription"].ToString();
                    }
                }
                else //上层还有半成品
                {

                    //drNext = DsJobEntry.Tables["JobOper"].Select("AssemblySeq=" + asmSeq + " and OprSeq > " + oprseq + "", "OprSeq ASC");
                    DataTable drNextdt = GetDataByERP("select OpCode,OprSeq,SubContract,Opdesc from erp.JobOper where Company='" + companyId + "' and JobNum='" + jobnum + "' and AssemblySeq='" + asmSeq + "' and OprSeq > '" + oprseq + "' order by OprSeq ASC ");
                    if (drNextdt != null && drNextdt.Rows.Count > 0) //当前半成品内有下工序
                    {
                        NextOperSeq = Convert.ToInt32(drNextdt.Rows[0]["OprSeq"]);
                        if (Convert.ToBoolean(drNextdt.Rows[0]["SubContract"]))
                        {
                            stype = "S|下工序外协" + drNextdt.Rows[0]["Opdesc"].ToString();
                            OutAsm = asmSeq;
                            OutOprSeq = NextOperSeq;

                        }
                        else
                        { stype = "M|下工序:" + drNextdt.Rows[0]["Opdesc"].ToString(); }
                    }
                    else
                    //非0层半层品内无下工序，还要到上层半层品中找第一个工序.
                    {
                        //DataRow[] relateddr = DsJobEntry.Tables["JobAsmbl"].Select("AssemblySeq=" + asmSeq + "");
                        DataTable relateddrdt = GetDataByERP("select AssemblySeq,Description,RelatedOperation,Parent ParentAssemblySeq from erp.JobAsmbl where Company='" + companyId + "' and JobNum='" + jobnum + "' and AssemblySeq='" + asmSeq + "'");
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
                                //drNext = DsJobEntry.Tables["JobOper"].Select("AssemblySeq=" + parAsmSeq + " and OprSeq = " + NextOperSeq + "");
                                drNextdt = GetDataByERP("select OpCode,OprSeq,SubContract,Opdesc from erp.JobOper where Company='" + companyId + "' and JobNum='" + jobnum + "' and AssemblySeq='" + parAsmSeq + "' and OprSeq = '" + NextOperSeq + "' order by OprSeq ASC ");
                                if (drNextdt != null && drNextdt.Rows.Count > 0)
                                {
                                    NextOperSeq = Convert.ToInt32(drNextdt.Rows[0]["OprSeq"]);
                                    if (Convert.ToBoolean(drNextdt.Rows[0]["SubContract"]))
                                    {
                                        stype = "S|下工序外协" + drNextdt.Rows[0]["Opdesc"].ToString();
                                        OutAsm = parAsmSeq;
                                        OutOprSeq = NextOperSeq;

                                    }
                                    else
                                    { stype = "M|下工序:" + drNextdt.Rows[0]["Opdesc"].ToString(); }
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
                //return "";
            }


        }


        //获取半成品描述和下一个半成品
        //public string GetJobAsmblDescs(string jobNum, string assemblynum, string companyId)
        //{
        //    if ((jobNum != null && jobNum.Length > 0) == false)
        //    {
        //        return "无效工单.,";
        //    }

        //    if ((assemblynum != null && assemblynum.Length > 0) == false)
        //    {
        //        return "无效半成品.,";
        //    }
        //    try
        //    {
        //        string result = "";
        //        string resultdata = ErpLogin/();
        //        if (resultdata != "true")
        //        {
        //            return "false|" + resultdata;
        //        }
        //        EpicorSessionManager.EpicorSession.CompanyID = companyId;
        //        JobEntryImpl jobEntry = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<JobEntryImpl>(EpicorSessionManager.EpicorSession, ImplBase<Erp.Contracts.JobEntrySvcContract>.UriPath);
        //        try
        //        {
        //            JobEntryDataSet DsJobEntry = jobEntry.GetByID(jobNum);
        //            DataRow[] dr = DsJobEntry.Tables["JobAsmbl"].Select("AssemblySeq=" + assemblynum + "");
        //            if (dr.Length > 0)
        //            {
        //                string assemblydesc;
        //                assemblydesc = dr[0]["Description"].ToString();
        //                assemblydesc = assemblydesc.Replace("\"", "");
        //                result = result + assemblydesc.ToString() + "," + dr[0]["PartNum"].ToString() + ",";

        //                //result = result + dr[0]["Description"].ToString() + "," + dr[0]["PartNum"].ToString() + ",";
        //            }
        //            else
        //            {
        //                result = result + ",,";
        //            }
        //            DataRow[] ndr = DsJobEntry.Tables["JobAsmbl"].Select("AssemblySeq=" + (Convert.ToInt32(assemblynum) + 1) + "");
        //            if (ndr.Length > 0)
        //            {


        //                string assemblydesc;
        //                assemblydesc = ndr[0]["Description"].ToString();
        //                assemblydesc = assemblydesc.Replace("\"", "");
        //                //---20160415 result = result + ndr[0]["AssemblySeq"].ToString() + "," + assemblydesc.ToString();
        //                result = result + ndr[0]["AssemblySeq"].ToString() + "," + "";

        //                //result = result + ndr[0]["AssemblySeq"].ToString() + "," + ndr[0]["Description"].ToString();

        //            }
        //            else
        //            {
        //                result = result + ",";
        //            }

        //            EpicorSessionManager.EpicorSession.Dispose();
        //        }
        //        catch
        //        {
        //            result = result + "无效工单.,";
        //            EpicorSessionManager.EpicorSession.Dispose();
        //            return result;
        //        }
        //        return result;
        //    }
        //    catch
        //    {
        //        EpicorSessionManager.EpicorSession.Dispose();
        //        return ",,,";
        //    }
        //}

        //sql
        public string GetJobAsmblDesc(string jobNum, string assemblynum, string companyId)
        {
            if ((jobNum != null && jobNum.Length > 0) == false)
            {
                return "无效工单.,";
            }

            if ((assemblynum != null && assemblynum.Length > 0) == false)
            {
                return "无效半成品.,";
            }
            try
            {
                string result = "";
                //string resultdata = ErpLogin/();
                //if (resultdata != "true")
                //{
                //    return "false|" + resultdata;
                //}
                try
                {
                    //JobEntryDataSet DsJobEntry = jobEntry.GetByID(jobNum);
                    //DataRow[] dr = DsJobEntry.Tables["JobAsmbl"].Select("AssemblySeq=" + assemblynum + "");
                    DataTable drdt = GetDataByERP("select PartNum,Description from erp.JobAsmbl where Company='" + companyId + "' and JobNum='" + jobNum + "' and AssemblySeq='" + assemblynum + "' ");
                    if (drdt != null && drdt.Rows.Count > 0)
                    {
                        string assemblydesc;
                        assemblydesc = drdt.Rows[0]["Description"].ToString();
                        assemblydesc = assemblydesc.Replace("\"", "");
                        result = result + assemblydesc.ToString() + "," + drdt.Rows[0]["PartNum"].ToString() + ",";

                        //result = result + dr[0]["Description"].ToString() + "," + dr[0]["PartNum"].ToString() + ",";
                    }
                    else
                    {
                        result = result + ",,";
                    }
                    //DataRow[] ndr = DsJobEntry.Tables["JobAsmbl"].Select("AssemblySeq=" + (Convert.ToInt32(assemblynum) + 1) + "");
                    DataTable ndrdt = GetDataByERP("select PartNum,Description,AssemblySeq from erp.JobAsmbl where Company='" + companyId + "' and JobNum='" + jobNum + "' and AssemblySeq='" + (Convert.ToInt32(assemblynum) + 1) + "'");
                    if (ndrdt != null && ndrdt.Rows.Count > 0)
                    {


                        string assemblydesc;
                        assemblydesc = ndrdt.Rows[0]["Description"].ToString();
                        assemblydesc = assemblydesc.Replace("\"", "");
                        //---20160415 result = result + ndr[0]["AssemblySeq"].ToString() + "," + assemblydesc.ToString();
                        result = result + ndrdt.Rows[0]["AssemblySeq"].ToString() + "," + "";

                        //result = result + ndr[0]["AssemblySeq"].ToString() + "," + ndr[0]["Description"].ToString();

                    }
                    else
                    {
                        result = result + ",";
                    }


                }
                catch
                {
                    result = result + "无效工单.,";

                    return result;
                }
                return result;
            }
            catch
            {

                return ",,,";
            }
        }


        public string GetJobResourceDesc(string jobNum, string ResourceID)
        {
            //-----Jeff -- 20151106 -- 速度慢,需要整合成一个函数,暂不取--Begin
            //try
            //{
            //    resource = new Resource(ConnectionPool);
            //    ResourceDataSet rds = resource.GetByID(ResourceID);
            //    DataRow[] dr = rds.Tables["Resource"].Select("ResourceID='" + ResourceID + "'");
            //    if (dr.Length > 0)
            //    {
            //        return dr[0]["Description"].ToString();
            //    }
            //    else
            //    {
            //        return "";
            //    }
            //}
            //catch
            //{
            //    return "";
            //}
            //-----Jeff -- 20151106 -- 速度慢,需要整合成一个函数,暂不取--End


            return "";
        }


        //获取物料描述
        public string GetJobPartIUM(string jobNum)
        {
            //-----Jeff -- 20151106 -- 速度慢,需要整合成一个函数,暂不取--Begin
            //try
            //{
            //Epicor.Mfg.Core.Session se = CommonClass.GetSession.Get();
            //if (se == null)
            //    return CommonClass.GetSession.ex;
            //    ConnectionPool = CommonClass.GetSession.Get().ConnectionPool;
            //    jobEntry = new JobEntry(ConnectionPool);
            //    JobEntryDataSet DsJobEntry;
            //    try
            //    {
            //        DsJobEntry = jobEntry.GetByID(jobNum);
            //    }
            //    catch
            //    {
            //        return "";
            //    }
            //    DataTable dt = DsJobEntry.Tables["JobHead"];
            //    if (dt!=null&& dt.Rows.Count>0)
            //    {
            //        return dt.Rows[0]["IUM"].ToString();
            //    }
            //    else
            //    {
            //        return "";
            //    }
            //}
            //catch
            //{
            //    return "";
            //}
            //-----Jeff -- 20151106 -- 速度慢,需要整合成一个函数,暂不取--End



            return "";
        }

        //获取下一个半成品
        public string GetNextJobAssmbly(string jobNum, string assemblynum)
        {
            //-----Jeff -- 20151106 -- 速度慢,需要整合成一个函数,暂不取--Begin
            //try
            //{
            //    jobEntry = new JobEntry(ConnectionPool);
            //    JobEntryDataSet DsJobEntry = jobEntry.GetByID(jobNum);
            //    DataRow[] dr = DsJobEntry.Tables["JobAsmbl"].Select("AssemblySeq=" + (Convert.ToInt32(assemblynum)+1) + "");
            //    if (dr.Length > 0)
            //    {
            //        return dr[0]["Description"].ToString() + ","+dr[0]["Description"].ToString();
            //    }
            //    else
            //    {
            //        return ",";
            //    }
            //}
            //catch
            //{
            //    return ",";
            //}
            //-----Jeff -- 20151106 -- 速度慢,需要整合成一个函数,暂不取--Begin

            return ",";
        }

        //获取下一道工序
        //public string GetNextJobOper(string jobNum, string operSeq, string companyId)
        //{
        //    //if ((jobNum != null && jobNum.Length > 0) == false)
        //    //{
        //    //    return "无效工单.";
        //    //}

        //    //if ((operSeq != null && operSeq.Length > 0) == false)
        //    //{
        //    //    return "无效工序.";
        //    //}
        //    string resultdata = ErpLogin/();
        //    if (resultdata != "true")
        //    {
        //        return "false|" + resultdata;
        //    }
        //    EpicorSessionManager.EpicorSession.CompanyID = companyId;


        //    JobEntryImpl jobEntry = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<JobEntryImpl>(EpicorSessionManager.EpicorSession, ImplBase<Erp.Contracts.JobEntrySvcContract>.UriPath);
        //    OpMasterImpl opMaster = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<OpMasterImpl>(EpicorSessionManager.EpicorSession, ImplBase<Erp.Contracts.OpMasterSvcContract>.UriPath);
        //    try
        //    {
        //        JobEntryDataSet DsJobEntry = jobEntry.GetByID(jobNum);
        //        DataRow[] dr = DsJobEntry.Tables["JobOper"].Select("OprSeq=" + (Convert.ToInt32(operSeq) + 10) + "");
        //        if (dr.Length > 0)
        //        {

        //            //---20160227--Jeff--Begin
        //            string txtOpDesc;
        //            OpMasterDataSet masterDs = opMaster.GetByID(dr[0]["OpCode"].ToString());
        //            DataRow[] opdr = masterDs.Tables["OpMaster"].Select("OpCode='" + dr[0]["OpCode"].ToString() + "'");
        //            if (opdr != null && opdr.Length > 0)
        //            {
        //                txtOpDesc = opdr[0]["OpDesc"].ToString();
        //            }
        //            else
        //            {
        //                txtOpDesc = "";
        //            }
        //            //---20160227--Jeff--End
        //            EpicorSessionManager.EpicorSession.Dispose();
        //            //return dr[0]["OprSeq"].ToString() + "," + dr[0]["OpDesc"].ToString();
        //            return dr[0]["OprSeq"].ToString() + "," + txtOpDesc.ToString();
        //        }
        //        else
        //        {
        //            EpicorSessionManager.EpicorSession.Dispose();
        //            return ",";
        //        }
        //    }
        //    catch
        //    {
        //        EpicorSessionManager.EpicorSession.Dispose();
        //        return ",";
        //    }
        //}

        //获取工序完成数量
        //public string GetJobOperQtyCompleted(string jobNum, string operSeq, string companyId)
        //{
        //    if ((jobNum != null && jobNum.Length > 0) == false)
        //    {
        //        return "无效工单.";
        //    }



        //    if ((operSeq != null && operSeq.Length > 0) == false)
        //    {
        //        return "无效工序.";
        //    }

        //    string resultdata = ErpLogin/();
        //    if (resultdata != "true")
        //    {
        //        return "false|" + resultdata;
        //    }
        //    EpicorSessionManager.EpicorSession.CompanyID = companyId;
        //    JobEntryImpl jobEntry = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<JobEntryImpl>(EpicorSessionManager.EpicorSession, ImplBase<Erp.Contracts.JobEntrySvcContract>.UriPath);
        //    try
        //    {
        //        JobEntryDataSet DsJobEntry = jobEntry.GetByID(jobNum);
        //        DataRow[] dr = DsJobEntry.Tables["JobOper"].Select("OprSeq=" + operSeq + "");
        //        if (dr.Length > 0)
        //        {
        //            EpicorSessionManager.EpicorSession.Dispose();
        //            return dr[0]["QtyCompleted"].ToString();

        //            //if (Convert.ToBoolean(dr[0]["SubContract"]) == true)
        //            //{
        //            //    return "0";
        //            //}
        //            //else
        //            //{
        //            //    return dr[0]["QtyCompleted"].ToString();
        //            //}

        //        }
        //        else
        //        {
        //            EpicorSessionManager.EpicorSession.Dispose();
        //            return "";
        //        }
        //    }
        //    catch
        //    {
        //        EpicorSessionManager.EpicorSession.Dispose();
        //        return "";
        //    }
        //}

        public string GetSubOprCmpQty(string jobid, string asmid, string oprid, string companyId)
        {
            string result = "0";
            string sql = "select sum(cast(QUALIFIEDAMOUNT as decimal)) from BO_PROCESS where  JOBID = '" + jobid + "' and PARTIALLYID = '" + asmid + "'  and PROCESSID = '" + oprid + "' and ISERP = 1";//company已过滤
            DataTable dt = GetDataByAWS(sql);
            if (dt != null && dt.Rows.Count > 0)
            {
                result = dt.Rows[0][0].ToString();
                if (result == "")
                {
                    result = "0";
                }
            }
            else
            {
                result = "0";
            }



            return result;
        }

        public string GetBPMRptQty(string jobid, string asmid, string oprid, int binid, int id, string companyId)
        {
            string result = "0";
            //string sql = "select SUM(CONVERT(decimal(10,4),CHECKAMOUNT) - CONVERT(decimal(10,4),(case UNQUALIFIEDAMOUNT when null then 0 else UNQUALIFIEDAMOUNT end))) from BO_PROCESS where  JOBID = '" + jobid + "' and PARTIALLYID = '" + asmid + "'  and PROCESSID = '" + oprid + "' and bindid<>" + binid + " and id<>" + id;//company1过滤
            string sql = "select SUM(CONVERT(decimal(10,4),CHECKAMOUNT) - CONVERT(decimal(10,4),(case UNQUALIFIEDAMOUNT when null then 0 else UNQUALIFIEDAMOUNT end))) from BO_PROCESS where  JOBID = '" + jobid + "' and PARTIALLYID = '" + asmid + "'  and PROCESSID = '" + oprid + "' and bindid<>" + binid + " and id<>" + id + " and bindid not in (select id from wf_messagedata where wfs_no=0)";
            DataTable dt = GetDataByAWS(sql);
            if (dt != null && dt.Rows.Count > 0)
            {
                result = dt.Rows[0][0].ToString();
                if (result == "")
                {
                    result = "0";
                }
            }
            else
            {
                result = "0";
            }



            return result;
        }

        public string GetUserByCheckSite(string checksite)
        {
            string result = "";
            string sql = "select USERID from ORGUSER left join ORGROLE on ORGUSER.ROLEID=ORGROLE.ID left join BO_CHECKSITE ON ORGROLE.ROLENAME=BO_CHECKSITE.ROLENAME where BO_CHECKSITE.CHECKSITE='" + checksite + "'";//company过滤
            DataTable dt = GetDataByAWS(sql);
            if (dt != null && dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    result = result + dt.Rows[i][0].ToString() + " ";
                }
            }



            return result;
        }

        public string GetUserPrinter(string userid)
        {
            string result = "";
            string sql = "select extend1 from orguser where userid='" + userid + "'";
            DataTable dt = GetDataByAWS(sql);
            if (dt != null && dt.Rows.Count > 0)
            {
                result = result + dt.Rows[0][0].ToString() + " ";
            }
            return result;
        }

        public DataTable GetDataByAWS(string sqlstr)
        {
            SqlConnection conn = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["awsConnectionstring"]);
            conn.Open();
            SqlDataAdapter ada = new SqlDataAdapter(sqlstr, conn);
            DataTable dt = new DataTable();
            ada.Fill(dt);
            conn.Close();
            return dt;
        }

        public DataTable GetDataByERP(string sqlstr)
        {
            SqlConnection conn = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["erpConnectionstring"]);
            conn.Open();
            SqlDataAdapter ada = new SqlDataAdapter(sqlstr, conn);
            DataTable dt = new DataTable();
            ada.Fill(dt);
            conn.Close();
            return dt;
        }

        public string QueryERP(string sqlstr)
        {
            SqlConnection conn = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["erpConnectionstring"]);
            conn.Open();
            SqlCommand cmd = new SqlCommand(sqlstr, conn);
            Object o = cmd.ExecuteScalar();
            conn.Close();
            return o == null ? "" : o.ToString();
        }

        private void WriteGetNewLaborInERPTxt(string employeenum, string laborHedStr, string laborDetailKeyAndValArrayStr, string type, string company)
        {
            string strSql = "";
            string key1 = System.Guid.NewGuid().ToString();
            strSql = "INSERT INTO ICE.UD30(Company,Key1,Character01,Character02,Character03,Date01,ShortChar01)";
            strSql += "Values('" + company + "','" + key1 + "','" + employeenum + "','" + laborHedStr + "|" + laborDetailKeyAndValArrayStr + "','" + type + "','" + DateTime.Now + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "')";
            int rowsCount = ExecuteSql(strSql);
        }

        public int ExecuteSql(string SQLString)
        {
            using (SqlConnection connection = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["erpConnectionstring"]))
            {
                using (SqlCommand cmd = new SqlCommand(SQLString, connection))
                {
                    try
                    {
                        connection.Open();
                        int rows = cmd.ExecuteNonQuery();
                        return rows;
                    }
                    catch (System.Data.SqlClient.SqlException e)
                    {
                        connection.Close();
                        throw e;
                    }
                }
            }
        }

        public string QueryAWS(string sqlstr)
        {
            SqlConnection conn = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["awsConnectionstring"]);
            conn.Open();
            SqlCommand cmd = new SqlCommand(sqlstr, conn);
            Object o = cmd.ExecuteScalar();
            conn.Close();
            return o == null ? "" : o.ToString();
        }

        public string QueryOrUpdateHS(string sqlstr)
        {
            SqlConnection conn = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["hsConnectionstring"]);
            conn.Open();
            SqlCommand cmd = new SqlCommand(sqlstr, conn);
            Object o = cmd.ExecuteScalar();
            conn.Close();
            return o == null ? "" : o.ToString();
        }

        public string UpdateAWS(string sqlstr)
        {
            SqlConnection conn = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["awsConnectionstring"]);
            conn.Open();
            SqlCommand cmd = new SqlCommand(sqlstr, conn);
            Object o = cmd.ExecuteNonQuery();
            conn.Close();
            return o == null ? "" : o.ToString();
        }

        public string SelectJobHead(string companyId)
        {
            //string resultdata = ErpLogin/();
            //if (resultdata != "true")
            //{
            //    return "false|" + resultdata;
            //}
            Session EpicorSession = ErpLoginbak();
            if (EpicorSession == null)
            {
                return "-1|erp用户数不够，请稍候再试.错误代码：SelectJobHead";
            }
            EpicorSession.CompanyID = companyId;
            //WriteGetNewLaborInERPTxt("", EpicorSession.SessionID.ToString(), "", "sessionidh", "");
            JobEntryImpl jobEntry = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<JobEntryImpl>(EpicorSession, ImplBase<Erp.Contracts.JobEntrySvcContract>.UriPath);
            bool morepages = false;
            JobEntryDataSet DsJobEntry = jobEntry.GetByID("1002576");
            //JobHeadListDataSet DsJobEntry = jobEntry.GetList("", 100, 0, out morepages);
            int count = DsJobEntry.Tables["JobHead"].Rows.Count;
            DataTable dt = DsJobEntry.Tables["JobHead"];
            EpicorSession.Dispose();
            return DataTableToJson(dt);
        }

        //bo 查询半成品
        //public string SelectJobAsmbls(string jobNum, string companyId)
        //{
        //    if (jobNum != null && jobNum.Length > 0)
        //    {
        //        string resultdata = ErpLogin/();
        //        if (resultdata != "true")
        //        {
        //            return "false|" + resultdata;
        //        }
        //        EpicorSessionManager.EpicorSession.CompanyID = companyId;
        //        JobEntryImpl jobEntry = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<JobEntryImpl>(EpicorSessionManager.EpicorSession, ImplBase<Erp.Contracts.JobEntrySvcContract>.UriPath);
        //        try
        //        {
        //            JobEntryDataSet DsJobEntry;
        //            DsJobEntry = jobEntry.GetByID(jobNum);

        //            //------------------------------------------------------20160316---Begin--Jeff
        //            if (DsJobEntry == null)
        //            {
        //                EpicorSessionManager.EpicorSession.Dispose();
        //                return "";
        //            }
        //            //------------------------------------------------------20160316---End--Jeff
        //            EpicorSessionManager.EpicorSession.Dispose();
        //            DataRow[] rows = DsJobEntry.Tables["JobAsmbl"].Select();
        //            //------------------------------------------------------20160315---Begin--Jeff
        //            if (rows != null && rows.Length > 0)
        //            {
        //                DataTable dt = new DataTable();
        //                dt.Columns.Add("AssemblySeq");
        //                dt.Columns.Add("Description");
        //                dt.Columns.Add("PartNum");
        //                foreach (DataRow row in rows)
        //                {
        //                    DataRow dr = dt.NewRow();
        //                    dr[0] = row["AssemblySeq"].ToString();

        //                    string assemblydesc;
        //                    assemblydesc = row["Description"].ToString();
        //                    assemblydesc = assemblydesc.Replace("\"", "");
        //                    dr[1] = assemblydesc.ToString();

        //                    //dr[1] = row["Description"].ToString();

        //                    dr[2] = row["PartNum"].ToString();
        //                    dt.Rows.Add(dr);
        //                }

        //                return DataTableToJson(dt);
        //            }
        //            else
        //            {
        //                return "";
        //            }
        //            //------------------------------------------------------20160315---End--Jeff
        //        }
        //        catch
        //        {
        //            EpicorSessionManager.EpicorSession.Dispose();
        //            return "";
        //            //20160227--Jeff--End

        //        }

        //    }


        //    return "";
        //}

        //sql 查询半成品
        public string SelectJobAsmbl(string jobNum, string companyId)
        {
            if (jobNum != null && jobNum.Length > 0)
            {
                try
                {
                    //DataRow[] rows = DsJobEntry.Tables["JobAsmbl"].Select();
                    DataTable sqldt = GetDataByERP("select AssemblySeq,Description,PartNum from erp.JobAsmbl where Company='" + companyId + "' and JobNum='" + jobNum + "' ");
                    //------------------------------------------------------20160315---Begin--Jeff
                    if (sqldt.Rows != null && sqldt.Rows.Count > 0)
                    {
                        DataTable dt = new DataTable();
                        dt.Columns.Add("AssemblySeq");
                        dt.Columns.Add("Description");
                        dt.Columns.Add("PartNum");
                        foreach (DataRow row in sqldt.Rows)
                        {
                            DataRow dr = dt.NewRow();
                            dr[0] = row["AssemblySeq"].ToString();

                            string assemblydesc;
                            assemblydesc = row["Description"].ToString();
                            assemblydesc = assemblydesc.Replace("\"", "");
                            dr[1] = assemblydesc.ToString();

                            //dr[1] = row["Description"].ToString();

                            dr[2] = row["PartNum"].ToString();
                            dt.Rows.Add(dr);
                        }

                        return DataTableToJson(dt);
                    }
                    else
                    {
                        return "";
                    }
                    //------------------------------------------------------20160315---End--Jeff
                }
                catch
                {

                    return "";
                    //20160227--Jeff--End

                }

            }


            return "";
        }


        public DataTable GetRoleOpr(string cuserid)
        {
            DataTable result;
            string sql = "select distinct BO_GXWH.GXMS from BO_GXWH where BO_GXWH.ROLENAME2 in (Select ORGROLE.ROLENAME FROM ORGUSER LEFT JOIN orgusermap ON ORGUSER.ID = orgusermap.MAPID LEFT JOIN ORGROLE on ORGROLE.ID = orgusermap.ROLEID where ORGROLE.ROLENAME like '现场报工-%' and ORGUSER.USERID =  '" + cuserid + "')";//company过滤
            DataTable dt = GetDataByAWS(sql);
            result = dt;
            return result;
        }

        //bo调用选择工序
        //public string SelectJobOpers(string jobNum, string assemblySeq, string txtUserid, string companyId)
        //{
        //    if ((jobNum != null && jobNum.Length > 0) == false)
        //    {
        //        return "无效工单.";
        //    }

        //    if ((assemblySeq != null && assemblySeq.Length > 0) == false)
        //    {
        //        return "无效半成品.";
        //    }

        //    if ((txtUserid != null && txtUserid.Length > 0) == false)
        //    {
        //        return "无效用户.";
        //    }
        //    string resultdata = ErpLogin/();
        //    if (resultdata != "true")
        //    {
        //        return "false|" + resultdata;
        //    }
        //    EpicorSessionManager.EpicorSession.CompanyID = companyId;
        //    JobEntryImpl jobEntry = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<JobEntryImpl>(EpicorSessionManager.EpicorSession, ImplBase<Erp.Contracts.JobEntrySvcContract>.UriPath);
        //    OpMasterImpl opMaster = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<OpMasterImpl>(EpicorSessionManager.EpicorSession, ImplBase<Erp.Contracts.OpMasterSvcContract>.UriPath);
        //    JobEntryDataSet DsJobEntry;
        //    try
        //    {
        //        DsJobEntry = jobEntry.GetByID(jobNum);

        //        // DataRow[] rows = DsJobEntry.Tables["JobOper"].Select("(AssemblySeq=" + assemblySeq + " and OpComplete = False and SubContract = False) or (AssemblySeq=" + assemblySeq + " and SubContract = True)");
        //        DataRow[] rows = DsJobEntry.Tables["JobOper"].Select("AssemblySeq=" + assemblySeq + " and QtyCompleted < RunQty");

        //        DataTable dt = new DataTable();
        //        dt.Columns.Add("OprSeq");
        //        dt.Columns.Add("Opcode");
        //        dt.Columns.Add("OpDesc");
        //        foreach (DataRow row in rows)
        //        {
        //            DataTable dtopr = new DataTable();
        //            string cuserid;
        //            cuserid = txtUserid;
        //            dtopr = GetRoleOpr(cuserid);

        //            if (dtopr != null && dtopr.Rows.Count > 0)
        //            {
        //                for (int j = 0; j < dtopr.Rows.Count; j++)
        //                {
        //                    //---20160227--Jeff--Begin
        //                    string txtOpDesc;
        //                    OpMasterDataSet masterDs = opMaster.GetByID(row["OpCode"].ToString());
        //                    DataRow[] opdr = masterDs.Tables["OpMaster"].Select("OpCode='" + row["OpCode"].ToString() + "'");
        //                    if (opdr != null && opdr.Length > 0)
        //                    {
        //                        txtOpDesc = opdr[0]["OpDesc"].ToString();
        //                    }
        //                    else
        //                    {
        //                        txtOpDesc = "";
        //                    }
        //                    //---20160227--Jeff--End

        //                    //if (row["OpDesc"].ToString() == dtopr.Rows[j][0].ToString())
        //                    string txto = txtOpDesc.ToString();
        //                    string dtop = dtopr.Rows[j][0].ToString();
        //                    if (txtOpDesc.ToString() == dtopr.Rows[j][0].ToString())
        //                    {
        //                        DataRow dr = dt.NewRow();
        //                        dr[0] = row["OprSeq"].ToString();
        //                        dr[1] = row["Opcode"].ToString();
        //                        //dr[2] = row["OpDesc"].ToString();
        //                        dr[2] = txtOpDesc.ToString();
        //                        dt.Rows.Add(dr);
        //                    }

        //                }
        //            }
        //            else
        //            {
        //                //---20160227--Jeff--Begin
        //                string txtOpDesc;
        //                OpMasterDataSet masterDs = opMaster.GetByID(row["OpCode"].ToString());
        //                DataRow[] opdr = masterDs.Tables["OpMaster"].Select("OpCode='" + row["OpCode"].ToString() + "'");
        //                if (opdr != null && opdr.Length > 0)
        //                {
        //                    txtOpDesc = opdr[0]["OpDesc"].ToString();
        //                }
        //                else
        //                {
        //                    txtOpDesc = "";
        //                }
        //                //---20160227--Jeff--End


        //                DataRow dr = dt.NewRow();
        //                dr[0] = row["OprSeq"].ToString();
        //                dr[1] = row["Opcode"].ToString();
        //                //dr[2] = row["OpDesc"].ToString();
        //                dr[2] = txtOpDesc.ToString();

        //                dt.Rows.Add(dr);
        //            }

        //        }

        //        EpicorSessionManager.EpicorSession.Dispose();
        //        return DataTableToJson(dt);
        //    }
        //    catch
        //    {
        //        EpicorSessionManager.EpicorSession.Dispose();
        //        return "";
        //    }

        //}

        //sql 选择工序
        public string SelectJobOper(string jobNum, string assemblySeq, string txtUserid, string companyId)
        {
            if ((jobNum != null && jobNum.Length > 0) == false)
            {
                return "无效工单.";
            }

            if ((assemblySeq != null && assemblySeq.Length > 0) == false)
            {
                return "无效半成品.";
            }

            if ((txtUserid != null && txtUserid.Length > 0) == false)
            {
                return "无效用户.";
            }
            //string resultdata = ErpLogin/();
            //if (resultdata != "true")
            //{
            //    return "false|" + resultdata;
            //}
            //EpicorSessionManager.EpicorSession.CompanyID = companyId;
            //JobEntryImpl jobEntry = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<JobEntryImpl>(EpicorSessionManager.EpicorSession, ImplBase<Erp.Contracts.JobEntrySvcContract>.UriPath);
            //OpMasterImpl opMaster = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<OpMasterImpl>(EpicorSessionManager.EpicorSession, ImplBase<Erp.Contracts.OpMasterSvcContract>.UriPath);
            //JobEntryDataSet DsJobEntry;
            try
            {
                //DsJobEntry = jobEntry.GetByID(jobNum);

                // DataRow[] rows = DsJobEntry.Tables["JobOper"].Select("(AssemblySeq=" + assemblySeq + " and OpComplete = False and SubContract = False) or (AssemblySeq=" + assemblySeq + " and SubContract = True)");
                //DataRow[] rows = DsJobEntry.Tables["JobOper"].Select("AssemblySeq=" + assemblySeq + " and QtyCompleted < RunQty");
                DataTable sqldt = GetDataByERP("select jo.OpCode,jo.OprSeq,om.OpDesc from erp.JobOper jo inner join erp.OpMaster om on jo.Company=om.Company and jo.OpCode=om.OpCode where jo.Company='" + companyId + "' and jo.JobNum='" + jobNum + "' and jo.AssemblySeq='" + assemblySeq + "' and jo.QtyCompleted < jo.RunQty");
                DataTable dt = new DataTable();
                dt.Columns.Add("OprSeq");
                dt.Columns.Add("Opcode");
                dt.Columns.Add("OpDesc");
                foreach (DataRow row in sqldt.Rows)
                {
                    DataTable dtopr = new DataTable();
                    string cuserid;
                    cuserid = txtUserid;
                    dtopr = GetRoleOpr(cuserid);

                    if (dtopr != null && dtopr.Rows.Count > 0)
                    {
                        for (int j = 0; j < dtopr.Rows.Count; j++)
                        {
                            //---20160227--Jeff--Begin
                            string txtOpDesc;
                            //OpMasterDataSet masterDs = opMaster.GetByID(row["OpCode"].ToString());
                            //DataRow[] opdr = masterDs.Tables["OpMaster"].Select("OpCode='" + row["OpCode"].ToString() + "'");
                            if (row["OpDesc"].ToString() != null && row["OpDesc"].ToString().Length > 0)
                            {
                                txtOpDesc = row["OpDesc"].ToString();
                            }
                            else
                            {
                                txtOpDesc = "";
                            }
                            //---20160227--Jeff--End

                            //if (row["OpDesc"].ToString() == dtopr.Rows[j][0].ToString())
                            string txto = txtOpDesc.ToString();
                            string dtop = dtopr.Rows[j][0].ToString();
                            if (txtOpDesc.ToString() == dtopr.Rows[j][0].ToString())
                            {
                                DataRow dr = dt.NewRow();
                                dr[0] = row["OprSeq"].ToString();
                                dr[1] = row["Opcode"].ToString();
                                //dr[2] = row["OpDesc"].ToString();
                                dr[2] = txtOpDesc.ToString();
                                dt.Rows.Add(dr);
                            }

                        }
                    }
                    else
                    {
                        //---20160227--Jeff--Begin
                        string txtOpDesc;
                        //OpMasterDataSet masterDs = opMaster.GetByID(row["OpCode"].ToString());
                        //DataRow[] opdr = masterDs.Tables["OpMaster"].Select("OpCode='" + row["OpCode"].ToString() + "'");
                        if (row["OpDesc"].ToString() != null && row["OpDesc"].ToString().Length > 0)
                        {
                            txtOpDesc = row["OpDesc"].ToString();
                        }
                        else
                        {
                            txtOpDesc = "";
                        }
                        //---20160227--Jeff--End


                        DataRow dr = dt.NewRow();
                        dr[0] = row["OprSeq"].ToString();
                        dr[1] = row["Opcode"].ToString();
                        //dr[2] = row["OpDesc"].ToString();
                        dr[2] = txtOpDesc.ToString();

                        dt.Rows.Add(dr);
                    }

                }


                return DataTableToJson(dt);
            }
            catch
            {

                return "";
            }

        }


        public string SelectJobOpdtl(string jobNum)
        {

            return "";
        }

        //更新工序
        public bool UpdateJobOper(string jobNum)
        {

            return false;
        }

        //datatable转json
        public string DataTableToJson(DataTable source)
        {
            if (source.Rows.Count == 0)
                return "";
            StringBuilder sb = new StringBuilder("");
            foreach (DataRow row in source.Rows)
            {
                sb.Append("");
                for (int i = 0; i < source.Columns.Count; i++)
                {
                    if (i == source.Columns.Count - 1)
                    {
                        sb.Append("" + row[i].ToString() + "");
                    }
                    else
                    {
                        sb.Append("" + row[i].ToString() + "@&");
                    }
                }
                //sb.Remove(sb.Length - 1, 1);
                sb.Append("|");
            }
            sb.Remove(sb.Length - 1, 1);
            sb.Append("");
            return sb.ToString();
        }

        //返回未关闭工单生产数量
        public List<JobOperRunQty> GetJobOperRunQty(string jobnum, string uuid, string companyId)
        {
            Session EpicorSession = ErpLoginbak();
            if (EpicorSession == null)
            {
                return null;
            }
            EpicorSession.CompanyID = companyId;
            //WriteGetNewLaborInERPTxt("", EpicorSession.SessionID.ToString(), "", "sessionidi", "");
            JobEntryImpl jobEntry = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<JobEntryImpl>(EpicorSession, ImplBase<Erp.Contracts.JobEntrySvcContract>.UriPath);
            OpMasterImpl opMaster = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<OpMasterImpl>(EpicorSession, ImplBase<Erp.Contracts.OpMasterSvcContract>.UriPath);
            try
            {
                JobEntryDataSet DsJobEntry;
                //bool morepages = false;
                DsJobEntry = jobEntry.GetByID(jobnum);

                //DsJobEntry = jobEntry.GetRows("", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "","", 0, 0, out morepages);
                //JobHeadListDataSet DsJobEntrys = jobEntry.GetList("", 100, 0, out morepages);
                DataRow[] drs = DsJobEntry.Tables["JobOper"].Select("");
                List<JobOperRunQty> list = new List<JobOperRunQty>();
                if (drs.Length > 0)
                {
                    foreach (DataRow dr in drs)
                    {
                        OpMasterDataSet masterDs = opMaster.GetByID(dr["OpCode"].ToString());
                        DataRow[] opdr = masterDs.Tables["OpMaster"].Select("OpCode='" + dr["OpCode"].ToString() + "'");
                        string txtOpDesc = "";
                        if (opdr != null && opdr.Length > 0)
                        {
                            txtOpDesc = opdr[0]["OpDesc"].ToString();
                        }
                        else
                        {
                            txtOpDesc = "";
                        }
                        DataRow[] asmdr = DsJobEntry.Tables["JobAsmbl"].Select("AssemblySeq=" + dr["AssemblySeq"].ToString() + "");
                        string partnum = "", assemblydesc = "";
                        if (asmdr.Length > 0)
                        {
                            partnum = asmdr[0]["PartNum"].ToString();
                            assemblydesc = asmdr[0]["Description"].ToString();
                        }
                        UpdateAWS("insert into  BO_JOBOPERDATAIL(UUID, JOBID, PARTIALLYID, PROCESSID, RUNQTY, COMPLETELYAMOUNT,PROCESSDESC,PARTNUM,PARTIALLYDESC) values('" + uuid + "','" + dr["JobNum"].ToString() + "','" + dr["AssemblySeq"].ToString() + "','" + dr["OprSeq"].ToString() + "','" + dr["RunQty"].ToString() + "','" + dr["QtyCompleted"].ToString() + "','" + txtOpDesc + "','" + partnum + "','" + assemblydesc + "')");//company过滤
                        //CommonClass.SqlHelper.UpdateSql("insert into  BO_JOBOPERDATAIL(UUID, JOBNUM, PARTIALLYID, JOBOPERID, RUNQTY, COMPLETEDQTY) values('" + uuid + "','" + dr["JobNum"].ToString() + "','" + dr["AssemblySeq"].ToString() + "','" + dr["OpCode"].ToString() + "','" + dr["RunQty"].ToString() + "','" + dr["QtyCompleted"].ToString() + "')");
                        //JobOperRunQty model = new JobOperRunQty();
                        //model.Jobnum = dr["JobNum"].ToString();
                        //model.Assemblynum = dr["AssemblySeq"].ToString();
                        //model.Opernum = dr["OpCode"].ToString();
                        //model.Runqty = dr["RunQty"].ToString();
                        //model.Completedqty = dr["QtyCompleted"].ToString();
                        //list.Add(model);
                    }
                }
                EpicorSession.Dispose();
                return list;
            }
            catch
            {
                EpicorSession.Dispose();
                return null;
            }
        }

        //返回未关闭工单生产数量
        public List<JobOperRunQty> GetJobOperdetails(string userid, string uuid, string companyId)
        {
            try
            {
                //qdDS.QueryWhereItem.DefaultView.RowFilter = "DataTableID='JobOper' and FieldName='OpCode'";
                string getsql = "select top 1 d.GXMS from ORGUSER a inner join ORGUSERMAP b on a.ID=b.MAPID inner join ORGROLE c on b.ROLEID=c.ID inner join BO_GXWH d on c.ROLENAME=d.ROLENAME  where USERID='" + userid + "'";//company过滤
                DataTable getdt = GetDataByAWS(getsql);
                List<QueryWhereItemRow> whereItems = new List<QueryWhereItemRow>();
                if (getdt != null && getdt.Rows.Count > 0)
                {
                    whereItems.Add(new QueryWhereItemRow() { TableID = "JobOper", FieldName = "OpCode", RValue = getdt.Rows[0][0].ToString() });
                }
                else
                {
                    whereItems.Add(new QueryWhereItemRow() { TableID = "JobOper", FieldName = "OpCode", RValue = "保护" });
                }


                DataTable dt = BaqResult("001-joboper", whereItems, 0, companyId);
                //DataTable dt = dynamicQuery.ExecuteDashBoardQuery(qdDS).Tables[0];
                if (dt != null)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        string sql = "insert into  BO_JOBOPERDATAIL(UUID, JOBID, PARTIALLYID, PROCESSID, RUNQTY, COMPLETELYAMOUNT,PROCESSDESC,PARTNUM,PARTIALLYDESC) values('" + uuid + "','" + dt.Rows[i]["JobOper.JobNum"].ToString() + "','" + dt.Rows[i]["JobOper.AssemblySeq"].ToString() + "','" + dt.Rows[i]["JobOper.OpCode"].ToString() + "','" + dt.Rows[i]["JobOper.RunQty"].ToString() + "','" + dt.Rows[i]["JobOper.QtyCompleted"].ToString() + "','" + dt.Rows[i]["OpMaster.OpDesc"].ToString() + "','" + dt.Rows[i]["JobAsmbl.PartNum"].ToString() + "','" + dt.Rows[i]["JobAsmbl.Description"].ToString() + "')";//company过滤
                        UpdateAWS(sql);
                    }
                }
                // JobEntryDataSet DsJobEntry;
                //DsJobEntry = jobEntry.GetByID(jobnum);
                //opMaster = new OpMaster(ConnectionPool);
                //DataRow[] drs = DsJobEntry.Tables["JobOper"].Select("");
                //List<JobOperRunQty> list = new List<JobOperRunQty>();
                //if (drs.Length > 0)
                //{
                //    foreach (DataRow dr in drs)
                //    {
                //        OpMasterDataSet masterDs = opMaster.GetByID(dr["OpCode"].ToString());
                //        DataRow[] opdr = masterDs.Tables["OpMaster"].Select("OpCode='" + dr["OpCode"].ToString() + "'");
                //        string txtOpDesc = "";
                //        if (opdr != null && opdr.Length > 0)
                //        {
                //            txtOpDesc = opdr[0]["OpDesc"].ToString();
                //        }
                //        else
                //        {
                //            txtOpDesc = "";
                //        }
                //        DataRow[] asmdr = DsJobEntry.Tables["JobAsmbl"].Select("AssemblySeq=" + dr["AssemblySeq"].ToString() + "");
                //        string partnum = "", assemblydesc = "";
                //        if (asmdr.Length > 0)
                //        {
                //            partnum = asmdr[0]["PartNum"].ToString();
                //            assemblydesc = asmdr[0]["Description"].ToString();
                //        }
                //        UpdateAWS("insert into  BO_JOBOPERDATAIL(UUID, JOBID, PARTIALLYID, PROCESSID, RUNQTY, COMPLETELYAMOUNT,PROCESSDESC,PARTNUM,PARTIALLYDESC) values('" + uuid + "','" + dr["JobNum"].ToString() + "','" + dr["AssemblySeq"].ToString() + "','" + dr["OprSeq"].ToString() + "','" + dr["RunQty"].ToString() + "','" + dr["QtyCompleted"].ToString() + "','" + txtOpDesc + "','" + partnum + "','" + assemblydesc + "')");
                //        //CommonClass.SqlHelper.UpdateSql("insert into  BO_JOBOPERDATAIL(UUID, JOBNUM, PARTIALLYID, JOBOPERID, RUNQTY, COMPLETEDQTY) values('" + uuid + "','" + dr["JobNum"].ToString() + "','" + dr["AssemblySeq"].ToString() + "','" + dr["OpCode"].ToString() + "','" + dr["RunQty"].ToString() + "','" + dr["QtyCompleted"].ToString() + "')");
                //        //JobOperRunQty model = new JobOperRunQty();
                //        //model.Jobnum = dr["JobNum"].ToString();
                //        //model.Assemblynum = dr["AssemblySeq"].ToString();
                //        //model.Opernum = dr["OpCode"].ToString();
                //        //model.Runqty = dr["RunQty"].ToString();
                //        //model.Completedqty = dr["QtyCompleted"].ToString();
                //        //list.Add(model);
                //    }
                //}

                return null;
            }
            catch
            {

                return null;
            }
        }

        //校验收货
        public string ChkRcv(string userid, string rcvType, string nextUser, string rcvinfo, string companyId)
        {
            //userid admin
            if (userid.Trim() == "") { return "0|用户ID不可为空."; }
            string awsql = "select userid from orguser   where userid='" + userid.Trim() + "'";
            DataTable dt = GetDataByAWS(awsql);
            if (dt.Rows.Count == 0) { return "0|用户发起收货流程出错，用户ID非法！."; }

            if (nextUser.Trim() == "") { return "0|下步办理人不可为空."; }
            string[] nextUserS = nextUser.Split(' ');
            string nextUserID = "";
            for (int i = 0; i < nextUserS.Length; i++)
            {
                nextUserID = nextUserS[i].ToString().Trim();
                awsql = "select userid from orguser   where userid='" + nextUserID + "'";
                dt = GetDataByAWS(awsql);
                if (dt.Rows.Count == 0) { return "0|下步办理人" + nextUserID + "不存在!"; }

            }



            if ((rcvinfo != null && rcvinfo.Length > 0) == false)
            {
                return "0|无效收货信息.";
            }
            string vendorid = "", vendorName = "", partnum = "", lotnum = "", uom = "", chkRepNum = "";
            DateTime rcvdate;
            int ponum = 0, poline = 0;
            double rcvqty = 0;

            //[  {    "vendorid": "A506",    "vendorName": "上海中元",    "rcvdate": "2016-4-6 15:20:33",    "ponum": "837127",    "poline": "1",    "partnum": "3100211",    "lotnum": "2016041511",    "rcvqty": "1000",    "uom": "pcs",    "ChkRepPath": "$$192.168.9.16$Share$xxx.pdf ",    "ChkRepNum": "BS160406164725",    "poType": "PUR-STK",    "partClass": "板材",    "partDesc": "A3 板材 10mm",    "jobnum": "0002278",    "duedate": "2016-5-12",    "partType": "P",    "asmSeq": "0",    "jobSeq": "10",    "opDesc": "委外拉丝",    "poHeadComm": "紧急订单",    "poLineComm": "按图加工"  }]

            #region changerToVar1
            //1@#A506**2@#上海中元**3@#2016-4-6**4@#837124**5@#1**6@#31010211**7@#20160405011**8@#1000**9@#pcs  //9431
            //1@#A999**2@#苏州甪成**3@#2016-4-21**4@#840266**5@#1**6@#210123918**7@#2016042111**8@#22**9@#pcs  //9401
            //dmr 7973 and 7974
            //baq 001-podmr

            //Regex rg = new Regex(Commom.splitArrayLineStr);
            //string[] keyAndValArray = rg.Split(rcvinfo);//字段和值的集合


            //foreach (string str in keyAndValArray)
            //{
            //    if (str == "") continue;
            //    string[] strArr = Regex.Split(str, Commom.splitArrayKeyAndValStr, RegexOptions.IgnoreCase);
            //    //if (strArr.Length != 2)
            //    //{ return "流程办理出错,无效收货信息"; }
            //    switch (strArr[0].ToString().Trim())
            //    {
            //        case "1":
            //            vendorid = strArr[1].ToString().Trim();
            //            break;
            //        case "2":
            //            vendorName = strArr[1].ToString().Trim();
            //            break;
            //        case "3":
            //            if (DateTime.TryParse(strArr[1], out rcvdate) == false) { return "流程办理出错，收货日期无效"; }                       
            //            break;
            //        case "4":
            //            if (int.TryParse(strArr[1], out ponum) == false) { return "流程办理出错，ponum无效"; } 
            //            break;
            //        case "5":
            //            if (int.TryParse(strArr[1], out poline) == false) { return "流程办理出错，poline无效"; }
            //            break;

            //        case "6":
            //            partnum = strArr[1].ToString().Trim();
            //            break;

            //        case "7":
            //            lotnum = strArr[1].ToString().Trim();
            //            break;

            //        case "8":
            //            if (double.TryParse(strArr[1], out rcvqty) == false) { return "流程办理出错，收货数量无效"; }
            //            break;

            //        case "9":
            //            uom = strArr[1].ToString().Trim();
            //            break;

            //    }

            //}

            #endregion


            #region changerToVar2


            try
            {
                JArray ja = (JArray)JsonConvert.DeserializeObject(rcvinfo);
                vendorid = ja[0]["vendorid"].ToString().Trim();
                vendorName = ja[0]["vendorName"].ToString().Trim();
                if (DateTime.TryParse(ja[0]["rcvdate"].ToString(), out rcvdate) == false) { return "0|流程办理出错，收货日期无效"; }
                if (int.TryParse(ja[0]["ponum"].ToString(), out ponum) == false) { return "0|流程办理出错，ponum无效"; }
                if (int.TryParse(ja[0]["poline"].ToString(), out poline) == false) { return "0|流程办理出错，poline无效"; }
                partnum = ja[0]["partnum"].ToString();
                lotnum = ja[0]["lotnum"].ToString();
                if (double.TryParse(ja[0]["rcvqty"].ToString().Trim(), out rcvqty) == false) { return "0|流程办理出错，收货数量无效"; }
                uom = ja[0]["uom"].ToString();
                chkRepNum = ja[0]["ChkRepNum"].ToString().Trim();
                DateTime duedate;
                if (DateTime.TryParse(ja[0]["duedate"].ToString().Trim(), out duedate) == false) { return "0|流程办理出错，到期日无效"; }
            }
            catch (Exception ex)
            {
                return "0|错误，请检查收货信息参数是否正确。" + ex.Message.ToString();

            }
            #endregion


            #region
            if (vendorid.Trim() == "") { return "0|流程办理出错，供应商无效"; }
            if (vendorName.Trim() == "") { return "0|流程办理出错，供应商无效"; }
            if (ponum == 0) { return "0|流程办理出错，ponum无效"; }
            if (poline == 0) { return "0|流程办理出错，poline无效"; }
            if (partnum.Trim() == "") { return "0|流程办理出错，物料编码无效"; }
            if (lotnum.Trim() == "") { return "0|流程办理出错，批次号无效"; }
            if (rcvqty == 0) { return "0|流程办理出错，收货数量无效"; }
            if (uom.Trim() == "") { return "0|流程办理出错，单位无效"; }
            if (chkRepNum.Trim() == "") { return "0|流程办理出错，验收单号无效"; }
            #endregion


            string sqlstr2 = "select ISNULL(Count(SEQNUM),0) from BO_SC_CGYS where COMPANYID='" + companyId + "' and SEQNUM='" + chkRepNum + "'"; //company1过滤
            DataTable dt3 = GetDataByAWS(sqlstr2);
            if (dt3.Rows.Count > 0)
            {
                if (Convert.ToInt32(dt3.Rows[0][0]) > 0)
                { return "0|流程办理出错，验收单重复"; }

            }
            //string resultdata = ErpLogin/();
            //if (resultdata != "true")
            //{
            //    return "0|" + resultdata;
            //}
            Session EpicorSession = ErpLoginbak();
            if (EpicorSession == null)
            {
                return "-1|erp用户数不够，请稍候再试.ERR:ChkRcv";
            }
            EpicorSession.CompanyID = companyId;
            //WriteGetNewLaborInERPTxt("", EpicorSession.SessionID.ToString(), "", "sessionids", "");
            VendorImpl venAd = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<VendorImpl>(EpicorSession, ImplBase<Erp.Contracts.VendorSvcContract>.UriPath);
            POImpl poAd = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<POImpl>(EpicorSession, ImplBase<Erp.Contracts.POSvcContract>.UriPath);
            try
            {
                VendorDataSet vends = venAd.GetByVendID(vendorid.Trim());
                //DataTable vendt =vends.Tables[];  
                VendorDataSet.VendorDataTable vendt = vends.Vendor;
                if (vendt.Rows.Count == 0)
                {
                    EpicorSession.Dispose();
                    return "0|流程办理出错，供应商无效";
                }
                if (Convert.ToBoolean(vendt.Rows[0]["Inactive"]))
                {
                    EpicorSession.Dispose();
                    return "0|流程办理出错，供应商选中无效勾";
                }
                vends.Dispose();
                PODataSet poDs = poAd.GetByID(ponum);
                PODataSet.POHeaderDataTable poHeadDt = poDs.POHeader;
                if (poHeadDt.Rows.Count == 0)
                {
                    EpicorSession.Dispose();
                    return "0|流程办理出错，po无效";
                }
                if (Convert.ToBoolean(poHeadDt.Rows[0]["OpenOrder"]) == false)
                {
                    EpicorSession.Dispose();
                    return "0|流程办理出错，po已关闭";
                }
                string vendid2 = poHeadDt.Rows[0]["VendorVendorID"].ToString().Trim();
                if (vendid2 != vendorid)
                {
                    EpicorSession.Dispose();
                    return "0|流程办理出错，供应商id与po不匹配";
                }

                PODataSet.PODetailDataTable podtlDt = poDs.PODetail;
                bool exitPoline = false;
                double polineQty = 0;
                for (int i = 0; i < podtlDt.Rows.Count; i++)
                {
                    if (Convert.ToInt32(podtlDt.Rows[i]["poline"]) == poline)
                    {
                        exitPoline = true;
                        if (Convert.ToBoolean(podtlDt.Rows[i]["OpenLine"]) == false)
                        {
                            EpicorSession.Dispose();
                            return "0|流程办理出错，po行已关闭";
                        }
                        polineQty = Convert.ToDouble(podtlDt.Rows[i]["CalcOurQty"]);
                    }
                }
                if (exitPoline == false)
                {
                    EpicorSession.Dispose();
                    return "0|流程办理出错，po行不存在";
                }
                //遍历所有porel
                PODataSet.PORelDataTable porelDt = poDs.PORel;
                double rcvedQty = 0;
                for (int i = 0; i < porelDt.Rows.Count; i++)
                {
                    if (Convert.ToInt32(porelDt.Rows[i]["poline"]) == poline)
                    {
                        rcvedQty = rcvedQty + Convert.ToDouble(porelDt.Rows[i]["receivedQty"]);
                    }
                }
                double unRcvQty = polineQty - rcvedQty;
                if (Math.Round((rcvqty - unRcvQty), 5) > 0)
                {
                    EpicorSession.Dispose();
                    return "0|流程办理出错，收货数量" + rcvqty.ToString() + "超过PO未关闭数量" + unRcvQty.ToString() + "，请确认是否需要修改采购订单！";
                }


                string sqlstr = "select ISNULL(sum(sub.shsl),0) from BO_SC_CGYSSUB sub inner join BO_SC_CGYS main on sub.bindid=main.bindid where main.COMPANYID='" + companyId + "' and sub.ponum=" + ponum + " and sub.poline=" + poline + " and  sub.isend=0";//company1过滤
                double sbQty = 0;
                DataTable dt2 = GetDataByAWS(sqlstr);
                if (dt2.Rows.Count > 0)
                { sbQty = Convert.ToDouble(dt2.Rows[0][0]); }
                if (Math.Round((rcvqty + sbQty - unRcvQty), 5) > 0)
                {
                    EpicorSession.Dispose();
                    return "0|流程办理出错，收货数量:" + rcvqty.ToString() + ",加上正在检验确认的数量:" + sbQty.ToString() + ",超过PO未关闭数量:" + unRcvQty.ToString() + "，请等待检验流程完成。！";
                }


                //发起下步流程
                string pid = stBPM(userid, "采购验收单" + chkRepNum, vendorid, vendorName, ponum, poline, partnum, lotnum, rcvqty, uom, nextUser, rcvinfo);

                if (pid != "ng")
                {
                    //发起成功，查出本流程待办事项并返回

                    awsql = "select  TARGET as userid ,BIND_ID as bind, id as taskid,WF_STYLE as style,TITLE From WF_TASK where BIND_ID='" + pid + "'";
                    DataTable dtR = GetDataByAWS(awsql);

                    int rcnt = dtR.Rows.Count;
                    if (rcnt == 0)
                    {
                        EpicorSession.Dispose();
                        return "0|无待办事项";
                    }
                    else
                    {
                        //增加流程结节点列
                        dtR.Columns.Add("node", typeof(string));
                        int spoint = 0, epoint = 0;
                        for (int i = 0; i < rcnt; i++)
                        {
                            spoint = dtR.Rows[i]["title"].ToString().Trim().IndexOf('(');
                            epoint = dtR.Rows[i]["title"].ToString().Trim().IndexOf(')');
                            if (spoint >= 0 && epoint >= 0)
                            {
                                dtR.Rows[i]["node"] = dtR.Rows[i]["title"].ToString().Trim().Substring(spoint + 1, epoint - spoint - 1);
                                dtR.Rows[i]["title"] = dtR.Rows[i]["title"].ToString().Trim().Substring(epoint + 1);
                            }

                        }

                        string jsonStr = DataTableToJson2(dtR);
                        EpicorSession.Dispose();
                        return "1|" + jsonStr;
                    }


                }
                else
                {
                    EpicorSession.Dispose();
                    return "0|发起采购验收流程失败，请联系管理员！";
                }

            }
            catch (Exception ex)
            {
                EpicorSession.Dispose();
                return "0|流程办理出错，请联系系统管理员 错误代码："+ex.Message;
            }

            finally
            {


            }


        }

        //发起采购验收bpm到检验处理
        public string stBPM(string userid, string title, string vendorid, string vendorName, int ponum, int poline, string partnum, string lotnum, double rcvqty, string uom, string nextUser, string rcvinfo)
        {

            //BPMInterface bpm = new BPMInterface("192.168.9.16", "cf32c01eb82403c27014adc03a0b66b9", "dba7604b33c4fdd4435735e3f94bf27d");
            BPMInterface bpm = new BPMInterface(bpmSer, cgysStra, cgysBo);
            //txtResu.Text = bpm.StartFlow(txtUser.Text.Trim(), txtTitle.Text.Trim(), TxtUUID.Text.Trim(), null, null);
            JArray ja = (JArray)JsonConvert.DeserializeObject(rcvinfo);
            // vendorid = ja[0]["vendorid"].ToString().Trim();


            DataTable dthed = new DataTable("BO_SC_CGYS");
            dthed.Columns.Add("BNO", Type.GetType("System.String"));
            dthed.Columns.Add("GYSDM", Type.GetType("System.String"));
            dthed.Columns.Add("GYSMC", Type.GetType("System.String"));
            dthed.Columns.Add("SHRQ", Type.GetType("System.DateTime"));
            dthed.Columns.Add("DEPID", Type.GetType("System.String"));
            dthed.Columns.Add("SEQNUM", Type.GetType("System.String"));  //SEQNUM  验收单号
            dthed.Columns.Add("FILELOCATION", Type.GetType("System.String")); //验收报告路径           
            dthed.Columns.Add("shdh", Type.GetType("System.String")); //供应商送货单号 

            DataRow dr = dthed.NewRow();
            //dr["BNO"] = "rcv" + ponum.ToString();
            dr["BNO"] = lotnum;
            dr["GYSDM"] = vendorid;
            dr["GYSMC"] = vendorName;
            dr["SHRQ"] = ja[0]["rcvdate"].ToString().Trim();
            dr["DEPID"] = "";
            dr["SEQNUM"] = ja[0]["ChkRepNum"].ToString().Trim();
            dr["FILELOCATION"] = ja[0]["ChkRepPath"].ToString().Trim();
            dr["shdh"] = ja[0]["shdh"].ToString().Trim();
            //dr["FILELOCATION"] = "";
            dthed.Rows.Add(dr);

            DataTable dtMtl = new DataTable("BO_SC_CGYSSUB");
            dtMtl.Columns.Add("PONUM", Type.GetType("System.Int32"));
            dtMtl.Columns.Add("POLINE", Type.GetType("System.Int32"));
            dtMtl.Columns.Add("PARTNUM", Type.GetType("System.String"));
            dtMtl.Columns.Add("PCH", Type.GetType("System.String"));
            dtMtl.Columns.Add("SHSL", Type.GetType("System.Double"));
            dtMtl.Columns.Add("HGSL", Type.GetType("System.Double"));
            dtMtl.Columns.Add("BHGSL", Type.GetType("System.Double"));
            dtMtl.Columns.Add("BHGDM", Type.GetType("System.String"));
            dtMtl.Columns.Add("BHGMS", Type.GetType("System.String"));
            dtMtl.Columns.Add("RKSL", Type.GetType("System.Double"));
            dtMtl.Columns.Add("SLDW", Type.GetType("System.String"));
            dtMtl.Columns.Add("POTYPE", Type.GetType("System.String"));
            dtMtl.Columns.Add("PARTCLASS", Type.GetType("System.String"));
            dtMtl.Columns.Add("PARTDESC", Type.GetType("System.String"));
            dtMtl.Columns.Add("JOBNUM", Type.GetType("System.String"));
            dtMtl.Columns.Add("DUEDATE", Type.GetType("System.DateTime"));
            dtMtl.Columns.Add("PARTTYPE", Type.GetType("System.String"));
            dtMtl.Columns.Add("ASMSEQ", Type.GetType("System.Int32"));
            dtMtl.Columns.Add("JOBSEQ", Type.GetType("System.Int32"));
            dtMtl.Columns.Add("OPDESC", Type.GetType("System.String"));
            dtMtl.Columns.Add("POHEADCOMM", Type.GetType("System.String"));
            dtMtl.Columns.Add("POLINECOMM", Type.GetType("System.String"));


            //  foreach (DataRow dr2 in dt.Rows)
            //{

            DataRow newDr = dtMtl.NewRow();
            newDr["PONUM"] = ponum;
            newDr["POLINE"] = poline;
            newDr["PARTNUM"] = partnum;
            newDr["PCH"] = lotnum;
            newDr["SHSL"] = rcvqty;
            newDr["HGSL"] = 0;
            newDr["BHGSL"] = 0;
            newDr["BHGDM"] = "";
            newDr["BHGMS"] = "";
            newDr["RKSL"] = 0;
            newDr["SLDW"] = uom;
            newDr["POTYPE"] = ja[0]["poType"].ToString().Trim();
            newDr["PARTCLASS"] = ja[0]["partClass"].ToString().Trim();
            newDr["PARTDESC"] = ja[0]["partDesc"].ToString().Trim();
            newDr["JOBNUM"] = ja[0]["jobnum"].ToString().Trim();
            newDr["DUEDATE"] = Convert.ToDateTime(ja[0]["duedate"].ToString().Trim());
            newDr["PARTTYPE"] = ja[0]["partType"].ToString().Trim();
            newDr["ASMSEQ"] = Convert.ToInt32(ja[0]["asmSeq"].ToString().Trim());
            newDr["JOBSEQ"] = Convert.ToInt32(ja[0]["jobSeq"].ToString().Trim());
            newDr["OPDESC"] = ja[0]["opDesc"].ToString().Trim();
            newDr["POHEADCOMM"] = ja[0]["poHeadComm"].ToString().Trim();
            newDr["POLINECOMM"] = ja[0]["poLineComm"].ToString().Trim();
            dtMtl.Rows.Add(newDr);
            //}


            string resu = bpm.StartFlow(userid, title, cgysUUID.Trim(), dthed, dtMtl, nextUser);

            if (resu == "ng")
            { return "ng"; }
            else
            { return resu; }


        }


        //关闭bpm流程
        public bool closeWF(string user, string pid, string sid)
        {
            BPMInterface bpm = new BPMInterface(bpmSer, cgysStra, cgysBo);
            return bpm.closeWF(user, pid, sid);

        }







        //发起转序报工bpm到检验处理
        public string stZXBPM(string jsonStr, string companyId)
        {

            //BPMInterface bpm = new BPMInterface("192.168.9.16", "cf32c01eb82403c27014adc03a0b66b9", "dba7604b33c4fdd4435735e3f94bf27d");
            try
            {
                BPMInterface bpm = new BPMInterface(bpmSer, cgysStra, cgysBo);

                JArray ja = (JArray)JsonConvert.DeserializeObject(jsonStr);
                string userid = ja[0]["USERID"].ToString().Trim();
                string nextUser = ja[0]["NEXTUSER"].ToString().Trim();
                string rcvBpmID = ja[0]["RCVBPMID"].ToString().Trim();
                string jobid = ja[0]["JOBID"].ToString().Trim();
                if (jobid == "") { return "0|工单号不可为空"; }
                string title = "转序申请" + jobid;
                int asmSeq = 0, oprseq = 0;
                int.TryParse(ja[0]["PARTIALLYID"].ToString().Trim(), out asmSeq);
                int.TryParse(ja[0]["PROCESSID"].ToString().Trim(), out oprseq);
                gtitle = title;
                string jobInfo = GetJobOperDesc(jobid, asmSeq.ToString().Trim(), oprseq.ToString().Trim(), companyId);
                string[] ss = jobInfo.Split(',');
                string oprDesc = ss[0];
                string nextOprSeq = ss[3];
                string nextOprDesc = ss[4];
                string nextAsmSeq = ss[5];
                string nextAsmDesc = ss[6];
                string jobInfo2 = GetJobAsmblDesc(jobid, asmSeq.ToString().Trim(), companyId);
                string[] ss2 = jobInfo2.Split(',');
                string asmDesc = ss2[0];

                int nextAsmSeqInt = 0;
                int.TryParse(nextAsmSeq, out nextAsmSeqInt);
                int nextOprSeqInt = 0;
                int.TryParse(nextOprSeq, out nextOprSeqInt);

                DataTable dthed = new DataTable("BO_PROCESS");
                dthed.Columns.Add("USERID", Type.GetType("System.String"));
                dthed.Columns.Add("RQ", Type.GetType("System.String"));
                dthed.Columns.Add("NEXTPROCESSRECVR", Type.GetType("System.String"));
                dthed.Columns.Add("CHECKSITEUSER", Type.GetType("System.String"));
                dthed.Columns.Add("CHECKSITE", Type.GetType("System.String"));
                dthed.Columns.Add("SENDNEXTOPRDATE", Type.GetType("System.String"));
                dthed.Columns.Add("SENDNEXTOPRTIME", Type.GetType("System.String"));
                dthed.Columns.Add("UNQUALIFIEDREASON", Type.GetType("System.String"));
                dthed.Columns.Add("UNQUALIFIEDREASONDESC", Type.GetType("System.String"));
                dthed.Columns.Add("GXWLY", Type.GetType("System.String"));
                dthed.Columns.Add("WLBLR", Type.GetType("System.String"));
                dthed.Columns.Add("PROCESSUSER", Type.GetType("System.String"));
                // dthed.Columns.Add("CHECKSITEUSER", Type.GetType("System.String"));
                dthed.Columns.Add("EMPBASIC", Type.GetType("System.String"));
                dthed.Columns.Add("EMBASIC", Type.GetType("System.String"));
                dthed.Columns.Add("NEXTUSER", Type.GetType("System.String"));
                dthed.Columns.Add("STEPTWO", Type.GetType("System.String"));
                dthed.Columns.Add("STEPTHREE", Type.GetType("System.String"));
                dthed.Columns.Add("STEPFOUR", Type.GetType("System.String"));
                dthed.Columns.Add("JOBID", Type.GetType("System.String"));
                dthed.Columns.Add("PARTIALLYID", Type.GetType("System.Int32"));
                dthed.Columns.Add("PARTIALLYDESC", Type.GetType("System.String"));
                dthed.Columns.Add("PROCESSID", Type.GetType("System.Int32"));
                dthed.Columns.Add("PROCESSDESC", Type.GetType("System.String"));
                dthed.Columns.Add("CHECKAMOUNT", Type.GetType("System.Decimal"));
                dthed.Columns.Add("QUALIFIEDAMOUNT", Type.GetType("System.Decimal"));
                dthed.Columns.Add("UNQUALIFIEDAMOUNT", Type.GetType("System.Decimal"));
                dthed.Columns.Add("UNITPASS", Type.GetType("System.String"));
                dthed.Columns.Add("NEXTPARTIALLYID", Type.GetType("System.Decimal"));
                dthed.Columns.Add("NEXTPARTIALLYDESC", Type.GetType("System.String"));
                dthed.Columns.Add("NEXTPROCESSID", Type.GetType("System.Int32"));
                dthed.Columns.Add("NEXTPROCESSDESC", Type.GetType("System.String"));
                dthed.Columns.Add("PARTNUM", Type.GetType("System.String"));
                dthed.Columns.Add("RUNQTY", Type.GetType("System.Decimal"));
                dthed.Columns.Add("RCVBPMID", Type.GetType("System.String"));

                DataRow dr = dthed.NewRow();
                dr["USERID"] = userid;

                dr["RQ"] = ja[0]["RQ"].ToString().Trim();
                dr["NEXTPROCESSRECVR"] = ja[0]["NEXTPROCESSRECVR"].ToString().Trim();
                string checkuser = ja[0]["CHECKSITEUSER"].ToString().Trim();

                if (checkuser == "")
                { dr["CHECKSITEUSER"] = "外协"; dr["CHECKSITE"] = "外协"; }
                else
                {
                    dr["CHECKSITEUSER"] = ja[0]["CHECKSITEUSER"].ToString().Trim();
                    dr["CHECKSITE"] = checkuser;
                }
                dr["SENDNEXTOPRDATE"] = ja[0]["SENDNEXTOPRDATE"].ToString().Trim();
                dr["SENDNEXTOPRTIME"] = ja[0]["SENDNEXTOPRTIME"].ToString().Trim();


                dr["UNQUALIFIEDREASON"] = ja[0]["UNQUALIFIEDREASON"].ToString().Trim();
                dr["UNQUALIFIEDREASONDESC"] = ja[0]["UNQUALIFIEDREASONDESC"].ToString().Trim();
                dr["GXWLY"] = ja[0]["GXWLY"].ToString().Trim();
                dr["WLBLR"] = ja[0]["WLBLR"].ToString().Trim();
                dr["PROCESSUSER"] = ja[0]["PROCESSUSER"].ToString().Trim();
                //dr["CHECKSITEUSER"] = ja[0]["CHECKSITEUSER"].ToString().Trim();
                dr["EMPBASIC"] = ja[0]["EMPBASIC"].ToString().Trim();
                dr["NEXTUSER"] = ja[0]["NEXTUSER"].ToString().Trim();
                dr["STEPTWO"] = ja[0]["STEPTWO"].ToString().Trim();
                dr["STEPTHREE"] = ja[0]["STEPTHREE"].ToString().Trim();
                dr["STEPFOUR"] = ja[0]["STEPFOUR"].ToString().Trim();
                dr["JOBID"] = jobid;
                dr["PARTIALLYID"] = asmSeq;
                dr["PARTIALLYDESC"] = asmDesc;
                dr["PROCESSID"] = oprseq;
                dr["PROCESSDESC"] = oprDesc;
                dr["CHECKAMOUNT"] = ja[0]["CHECKAMOUNT"].ToString().Trim();
                dr["QUALIFIEDAMOUNT"] = ja[0]["QUALIFIEDAMOUNT"].ToString().Trim();
                dr["UNQUALIFIEDAMOUNT"] = ja[0]["UNQUALIFIEDAMOUNT"].ToString().Trim();
                dr["UNITPASS"] = ja[0]["UNITPASS"].ToString().Trim();
                dr["NEXTPARTIALLYID"] = nextAsmSeqInt;
                dr["NEXTPARTIALLYDESC"] = nextAsmDesc;
                dr["NEXTPROCESSID"] = nextOprSeqInt;
                dr["NEXTPROCESSDESC"] = nextOprDesc;
                dr["PARTNUM"] = ja[0]["PARTNUM"].ToString().Trim();
                dr["RUNQTY"] = ja[0]["RUNQTY"].ToString().Trim();
                dr["RCVBPMID"] = ja[0]["RCVBPMID"].ToString().Trim();


                dthed.Rows.Add(dr);

                DataTable dtMtl = null;


                string resu = bpm.StartFlow(userid, title, zxUUID.Trim(), dthed, dtMtl, userid);
                //转至下个节点
                //bpm.NextStepOne2();



                if (resu == "ng")
                { return "0||处理失败,请联系统管理员"; }
                else
                {
                    gbinid = resu;
                    gtaskid = bpm.Gtaskid;
                    gnextuser = nextUser;
                    return "1|处理成功，流程id:" + resu;
                }

            }
            catch (Exception ex)
            {
                return "0||处理失败" + ex.Message.ToString();


            }


        }



        //查询用户是否存在
        public bool userExit(string userid)
        {
            try
            {
                string awsql = "select userid from orguser   where userid='" + userid.Trim() + "'";
                DataTable dt = GetDataByAWS(awsql);
                if (dt.Rows.Count == 0) { return false; }
                return true;
            }
            catch
            { return false; }

        }

        //D0302_01采购验收流程检验处理节点转下一步
        public string D0302_01(string userid, string bind, string taskid, string style, string node, string nextuser, string title, decimal sQty)
        {
            try
            {

                if (sQty == 0)
                {
                    closeWF(userid, bind.Trim(), taskid.Trim());
                    return "3|产品不合格，拒收，流程" + bind + "关闭";
                }

                string resu = "";
                string sqlstr = "select ws.WFS_NO,wt.ID from WF_TASK wt  " +
                                "inner join wf_messagedata ws on ws.ID=wt.BIND_ID where wt.BIND_ID=" + bind +
                                " and TARGET='" + userid + "'";
                DataTable dt = GetDataByAWS(sqlstr);
                if (dt.Rows.Count == 0) { return "0|流程" + bind + "不存在"; }
                int nodeNum = 0;
                int.TryParse(dt.Rows[0][0].ToString(), out nodeNum);
                if (nodeNum > 2) //流程已经不在第2个节点了
                { return "2|" + dt.Rows[0][1].ToString().Trim(); }
                if (nodeNum < 2)
                { return "0|流程还没有到达第2节点。"; }

                resu = D0502_01(userid, bind, taskid, style, node, nextuser, title);
                return resu;
            }
            catch (Exception ex)
            {
                return "0|" + ex.Message.ToString();
            }
        }



        //D0502-01 结束BPM流程节点,并发起下步单人办理流程
        public string D0502_01(string userid, string bind, string taskid, string style, string node, string nextuser, string title)
        {
            if (userid.Trim() == "") { return "0|用户不可为空"; }
            if (userExit(userid.Trim()) == false) { return "0|用户" + userid + "不存在"; }
            if (nextuser.Trim() == "") { return "0|下步办理人不可为空"; }
            if (userExit(nextuser.Trim()) == false) { return "0|下步办理人" + nextuser + "不存在"; }
            if (bind.Trim() == "") { return "0|流程ID不可为空"; }
            if (taskid.Trim() == "") { return "0|任务不可为空"; }
            if (style.Trim() == "") { return "0|流程类型不可为空"; }
            if (title.Trim() == "") { return "0|任务标题不可为空"; }

            string straid = "", boid = "";
            switch (style.Trim())
            {
                case "采购验收流程组":
                    straid = cgysStra;
                    boid = cgysBo;
                    break;


                case "转序申请流程组":
                    straid = cgysStra;
                    boid = cgysBo;
                    break;

                default:
                    return "0|暂不支持流程:" + style;

            }


            try
            {
                string sqlstr = "select  BIND_ID,ID,OWNER,TARGET,TITLE  from WF_TASK where BIND_ID = " + bind;
                DataTable dt = GetDataByAWS(sqlstr);
                if (dt.Rows.Count == 0) { return "0|当前任务不存在"; }
                BPMInterface bpm = new BPMInterface(bpmSer, straid, boid);
                string str = bpm.NextStepOne2str(userid, bind, taskid, nextuser, title, dt);
                if (!string.IsNullOrEmpty(str))
                {
                    return "0|" + str;
                }
                gbinid = bpm.Gbinid;
                gtaskid = bpm.Gtaskid;
                gtitle = title;
                return "1|" + gtaskid;
            }
            catch (Exception ex)
            { return "0|" + ex.Message.ToString(); }

        }



        //转仓查询基本信息tranA
        public string tranA(string partnum, string companyId)
        {

            try
            {

                decimal tqty;
                List<QueryWhereItemRow> whereItems = new List<QueryWhereItemRow>();
                whereItems.Add(new QueryWhereItemRow() { TableID = "part", FieldName = "partnum", RValue = partnum.ToString() });
                //DataTable dt = BaqResult("001-tranA", whereItems, 0, companyId);
                DataTable dt = GetDataByERP("select [Part].[PartDescription] as [Part_PartDescription],[Part].[IUM] as [Part_IUM],(sum(PartBin.OnhandQty)) as [qty] from Erp.Part as Part left outer join Erp.PartBin as PartBin on Part.Company = PartBin.Company and Part.PartNum = PartBin.PartNum where (Part.Company = '" + companyId + "'  and Part.PartNum = '" + partnum + "') group by [Part].[PartDescription],[Part].[IUM] ");
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    dt.Columns[i].ColumnName = dt.Columns[i].ColumnName.Replace('_', '.');
                }
                if (dt.Rows.Count <= 0)
                {


                    return ("0|物料编码不存在");
                }
                else
                {

                    int rowcnt = 0, colcnt = 0;

                    rowcnt = dt.Rows.Count;
                    colcnt = dt.Columns.Count;
                    for (int i = 0; i < rowcnt; i++)
                    {
                        dt.Rows[i]["Part.PartDescription"] = dt.Rows[i]["Part.PartDescription"].ToString().Replace('"', '#');
                        dt.Rows[i]["Part.PartDescription"] = dt.Rows[i]["Part.PartDescription"].ToString().Replace('"', '#');
                        dt.Rows[i]["Part.PartDescription"] = dt.Rows[i]["Part.PartDescription"].ToString().Replace('\\', '/');
                        dt.Rows[i]["Part.PartDescription"] = dt.Rows[i]["Part.PartDescription"].ToString().Replace('\\', '/');
                    }
                    return "1|" + DataTableToJson2(dt);

                }
            }
            catch (Exception ex)
            {

                return "0|" + ex.Message.ToString();
            }
        }

        //转仓查询物料仓库tranB
        public string tranB(string partnum, string companyId)
        {
            try
            {
                List<QueryWhereItemRow> whereItems = new List<QueryWhereItemRow>();
                whereItems.Add(new QueryWhereItemRow() { TableID = "partwhse", FieldName = "partnum", RValue = partnum });
                //DataTable dt = BaqResult("001-tranB", whereItems, 0, companyId);
                DataTable dt = GetDataByERP("select [PartWhse].[WarehouseCode] as [PartWhse_WarehouseCode],[Warehse].[Description] as [Warehse_Description] from Erp.PartWhse as PartWhse inner join Erp.Warehse as Warehse on PartWhse.Company = Warehse.Company and PartWhse.WarehouseCode =Warehse.WarehouseCode where (PartWhse.Company = '" + companyId + "'  and PartWhse.PartNum = '" + partnum + "')");
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    dt.Columns[i].ColumnName = dt.Columns[i].ColumnName.Replace('_', '.');
                }
                if (dt.Rows.Count <= 0)
                {

                    return ("0|物料编码不存在");
                }
                else
                {

                    return "1|" + DataTableToJson2(dt);

                }
            }
            catch (Exception ex)
            {

                return "0|" + ex.Message.ToString();

            }


        }


        //转仓查询物料库位tranC
        public string tranC(string partnum, string warehouseCode, string companyId)
        {
            try
            {
                decimal tqty;
                List<QueryWhereItemRow> whereItems = new List<QueryWhereItemRow>();
                whereItems.Add(new QueryWhereItemRow() { TableID = "partbin", FieldName = "partnum", RValue = partnum });
                whereItems.Add(new QueryWhereItemRow() { TableID = "partbin", FieldName = "warehouseCode", RValue = warehouseCode });
                //DataTable dt = BaqResult("001-tranC", whereItems, 0, companyId);
                DataTable dt = GetDataByERP("select [PartBin].[BinNum] as [PartBin_BinNum],[WhseBin].[ZoneID] as [WhseBin_ZoneID] from Erp.PartBin as PartBin left outer join Erp.WhseBin as WhseBin on PartBin.Company = WhseBin.Company and PartBin.WarehouseCode = WhseBin.WarehouseCode and PartBin.BinNum = WhseBin.BinNum where (PartBin.Company = '" + companyId + "'  and PartBin.PartNum = '" + partnum + "'  and PartBin.WarehouseCode = '" + warehouseCode + "'  and PartBin.OnhandQty > 0)");
                //dt = dynamicQuery.ExecuteDashBoardQuery(qdDS).Tables[0];
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    dt.Columns[i].ColumnName = dt.Columns[i].ColumnName.Replace('_', '.');
                }
                if (dt.Rows.Count <= 0)
                {

                    return ("0|物料编码不存在");
                }
                else
                {

                    DataView dv = dt.DefaultView;
                    dv.Sort = "PartBin.binnum";
                    DataTable dt2 = dv.ToTable(true); //去掉重复行

                    return "1|" + DataTableToJson2(dt2);

                }
            }
            catch (Exception ex)
            {

                return "0|" + ex.Message.ToString();

            }


        }



        //转仓查询物料批次和数量tranD
        public string tranD(string partnum, string warehouseCode, string binnum, string companyId)
        {
            try
            {
                decimal tqty;
                List<QueryWhereItemRow> whereItems = new List<QueryWhereItemRow>();
                whereItems.Add(new QueryWhereItemRow() { TableID = "partbin", FieldName = "partnum", RValue = partnum });
                whereItems.Add(new QueryWhereItemRow() { TableID = "partbin", FieldName = "warehouseCode", RValue = warehouseCode });
                whereItems.Add(new QueryWhereItemRow() { TableID = "partbin", FieldName = "binnum", RValue = binnum });
                //DataTable dt = BaqResult("001-tranC", whereItems, 0, companyId);
                DataTable dt = GetDataByERP("select [PartBin].[LotNum] as [PartBin_LotNum],[PartBin].[OnhandQty] as [PartBin_OnhandQty] from Erp.PartBin as PartBin where (PartBin.Company = '" + companyId + "'  and PartBin.PartNum = '" + partnum + "'  and PartBin.WarehouseCode = '" + warehouseCode + "'  and PartBin.BinNum = '" + binnum + "')");
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    dt.Columns[i].ColumnName = dt.Columns[i].ColumnName.Replace('_', '.');
                }
                if (dt.Rows.Count <= 0)
                {

                    return ("0|库存不存在");
                }
                else
                {

                    return "1|" + DataTableToJson2(dt);

                }
            }
            catch (Exception ex)
            {

                return "0|" + ex.Message.ToString();

            }


        }

        //按物料批号汇总库存数量
        public string partBinQty(string partnum, string companyId)
        {
            try
            {
                decimal tqty;
                List<QueryWhereItemRow> whereItems = new List<QueryWhereItemRow>();
                whereItems.Add(new QueryWhereItemRow() { TableID = "partbin", FieldName = "partnum", RValue = partnum });
                //DataTable dt = BaqResult("001-partQTY", whereItems, 0, companyId);
                DataTable dt = GetDataByERP("select [PartBin].[PartNum] as [PartBin_PartNum],[PartBin].[WarehouseCode] as [PartBin_WarehouseCode],[PartBin].[BinNum] as      [PartBin_BinNum],(sum( PartBin1.OnhandQty )) as [Calculated_Onhand] from Erp.PartBin as PartBin inner join Erp.PartBin as PartBin1 on PartBin.PartNum = PartBin1.PartNum and PartBin.WarehouseCode = PartBin1.WarehouseCode and PartBin.BinNum =PartBin1.BinNum where (PartBin.Company = '" + companyId + "'  and PartBin.PartNum = '" + partnum + "'  and PartBin.OnhandQty > 0) group by [PartBin].[PartNum],[PartBin].[WarehouseCode],[PartBin].[BinNum]");
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    dt.Columns[i].ColumnName = dt.Columns[i].ColumnName.Replace('_', '.');
                }
                if (dt.Rows.Count <= 0)
                {

                    return ("0|库存不存在");
                }
                else
                {
                    DataView dv = dt.DefaultView;
                    DataTable dt2 = dv.ToTable(true);

                    return "1|" + DataTableToJson2(dt2);

                }
            }
            catch (Exception ex)
            {

                return "0|" + ex.Message.ToString();

            }

        }



        //转仓查询物料数量tranE

        public string tranE(string partnum, string warehouseCode, string binnum, string lotnum, string companyId)
        {
            try
            {
                decimal tqty;
                List<QueryWhereItemRow> whereItems = new List<QueryWhereItemRow>();
                whereItems.Add(new QueryWhereItemRow() { TableID = "partbin", FieldName = "partnum", RValue = partnum });
                whereItems.Add(new QueryWhereItemRow() { TableID = "partbin", FieldName = "warehouseCode", RValue = warehouseCode });
                whereItems.Add(new QueryWhereItemRow() { TableID = "partbin", FieldName = "binnum", RValue = binnum });
                whereItems.Add(new QueryWhereItemRow() { TableID = "partbin", FieldName = "lotnum", RValue = lotnum });
                //DataTable dt = BaqResult("001-tranE", whereItems, 0, companyId);
                DataTable dt = GetDataByERP("select [PartBin].[OnhandQty] as [PartBin_OnhandQty] from Erp.PartBin as PartBin where (PartBin.Company = '" + companyId + "'  and PartBin.PartNum = '" + partnum + "'  and PartBin.WarehouseCode = '" + warehouseCode + "'  and PartBin.BinNum = '" + binnum + "'  and PartBin.LotNum = '" + lotnum + "')");
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    dt.Columns[i].ColumnName = dt.Columns[i].ColumnName.Replace('_', '.');
                }
                if (dt.Rows.Count <= 0)
                {

                    return ("0|库存不存在");
                }
                else
                {

                    return "1|" + DataTableToJson2(dt);

                }
            }
            catch (Exception ex)
            {

                return "0|" + ex.Message.ToString();

            }


        }


        //TranStk库存转仓接口
        public string tranStk(string jsonStr, string companyId)
        {
            int rcnt = 0, i = 0, j = 0;
            string partnum;
            try
            {

                DataTable tranDT = JsonConvert.DeserializeObject<DataTable>(jsonStr);
                rcnt = tranDT.Rows.Count;


                //check data
                string company = companyId;//((Epicor.Mfg.Core.Session)(oTrans.Session)).CompanyID;
                decimal aqty;


                for (i = 0; i < rcnt; i++)
                {
                    //wcode=ugDetail.Rows[i].Cells[0].Value.ToString().Trim();
                    //binnum=ugDetail.Rows[i].Cells[2].Value.ToString().Trim();
                    //lotnum=ugDetail.Rows[i].Cells[11].Value.ToString();

                    if (tranDT.Rows[i]["fromWHcode"].ToString().Trim() == "")
                    { return "0|" + "第" + (i + 1).ToString() + "行[源仓库id]不可为空,请输入有效数据后再执行转仓"; }

                    if (tranDT.Rows[i]["fromWHname"].ToString().Trim() == "")
                    { return "0|第" + (i + 1).ToString() + "行[源仓库名称]列不可为空,请输入有效数据后再执行转仓"; }

                    if (tranDT.Rows[i]["fromBinNum"].ToString().Trim() == "")
                    { return "0|第" + (i + 1).ToString() + "行[源库位id]列不可为空,请输入有效数据后再执行转仓"; }

                    if (tranDT.Rows[i]["fromBinName"].ToString().Trim() == "")
                    { return "0|第" + (i + 1).ToString() + "行[源库位名称]列不可为空,请输入有效数据后再执行转仓"; }

                    if (tranDT.Rows[i]["toWHcode"].ToString().Trim() == "")
                    { return "0|第" + (i + 1).ToString() + "行[目的仓库id]列不可为空,请输入有效数据后再执行转仓"; }

                    if (tranDT.Rows[i]["toWHname"].ToString().Trim() == "")
                    { return "0|第" + (i + 1).ToString() + "行[目的仓库名称]列不可为空,请输入有效数据后再执行转仓"; }

                    if (tranDT.Rows[i]["tobinnum"].ToString().Trim() == "")
                    { return "0|第" + (i + 1).ToString() + "行[目的库位id]列不可为空,请输入有效数据后再执行转仓"; }

                    if (tranDT.Rows[i]["tobinname"].ToString().Trim() == "")
                    { return "0|第" + (i + 1).ToString() + "行[目的库位名称]列不可为空,请输入有效数据后再执行转仓"; }
                    partnum = tranDT.Rows[i]["partnum"].ToString().Trim();
                    if (partnum == "")
                    { return "0|第" + (i + 1).ToString() + "行[物料编码]列不可为空,请输入有效数据后再执行转仓"; }

                    if (tranDT.Rows[i]["partdesc"].ToString().Trim() == "")
                    { return "0|第" + (i + 1).ToString() + "行[物料描述]列不可为空,请输入有效数据后再执行转仓"; }

                    if (tranDT.Rows[i]["tranQty"].ToString().Trim() == "")
                    { return "0|第" + (i + 1).ToString() + "行[转移数量]列不可为空,请输入有效数据后再执行转仓"; }

                    // aqty=Math.Round(Convert.ToDecimal(tranDT.Rows[i]["tranQty"].ToString().Trim()),5);
                    if (decimal.TryParse(tranDT.Rows[i]["tranQty"].ToString(), out aqty) == false)
                    { return "0|第" + (i + 1).ToString() + "行[转移数量]列不可<=0,请输入有效数据后再执行转仓"; }

                    //if (ugDetail.Rows[i].Cells[11].Value.ToString().Trim()=="")
                    //	{MessageBox.Show((i+1).ToString() + "行[批号]列不可为空,请输入有效数据后再执行转仓");return false;}

                    if (tranDT.Rows[i]["uom"].ToString().Trim() == "")
                    { return "0|第" + (i + 1).ToString() + "行[单位]列不可为空,请输入有效数据后再执行转仓"; }

                }

                ////需要执行源库位库存数量的检查,否则有可能产生负库存
                string wcode = "", binnum = "", lotnum = "";
                decimal tqty;

                for (i = 0; i < rcnt; i++)
                {
                    partnum = tranDT.Rows[i]["partnum"].ToString().Trim();
                    wcode = tranDT.Rows[i]["fromWHcode"].ToString().Trim();
                    binnum = tranDT.Rows[i]["fromBinNum"].ToString().Trim();
                    lotnum = tranDT.Rows[i]["lotnum"].ToString().Trim();
                    //List<QueryWhereItemRow> whereItems = new List<QueryWhereItemRow>();
                    //whereItems.Add(new QueryWhereItemRow() { TableID = "partbin", FieldName = "partnum", RValue = partnum });
                    //whereItems.Add(new QueryWhereItemRow() { TableID = "partbin", FieldName = "WarehouseCode", RValue = wcode });
                    //whereItems.Add(new QueryWhereItemRow() { TableID = "partbin", FieldName = "BinNum", RValue = binnum });
                    //whereItems.Add(new QueryWhereItemRow() { TableID = "partbin", FieldName = "LotNum", RValue = lotnum });
                    //DataTable dt = BaqResult("001-binQty", whereItems, 0, companyId);
                    DataTable dt = GetDataByERP("select [PartBin].[OnhandQty] as [PartBin_OnhandQty] from Erp.PartBin as PartBin where (PartBin.Company = '" + company + "'  and PartBin.PartNum = '" + partnum + "'  and PartBin.WarehouseCode = '" + wcode + "'  and PartBin.BinNum = '" + binnum + "'  and PartBin.LotNum = '" + lotnum + "')");
                    if (dt.Rows.Count <= 0)
                    {

                        return ("0|第" + (i + 1).ToString() + "行源库位无库存,请输入有效数据后再执行转仓");
                    }
                    aqty = Math.Round(Convert.ToDecimal(dt.Rows[0][0]), 5);
                    decimal.TryParse(tranDT.Rows[i]["tranqty"].ToString(), out tqty);
                    if (aqty < tqty)
                    {

                        return "0|第" + (i + 1).ToString() + "行源库位库存" + aqty.ToString() + "<转仓数量" + tqty.ToString() + ",请输入有效数据后再执行转仓";

                    }

                }


                //转仓
                //string resultdata = ErpLogin/();
                //if (resultdata != "true")
                //{
                //    return "0|" + resultdata;
                //}
                Session EpicorSession = ErpLoginbak();
                if (EpicorSession == null)
                {
                    return "-1|erp用户数不够，请稍候再试.ERR:tranStk";
                }
                EpicorSession.CompanyID = companyId;
                //WriteGetNewLaborInERPTxt("", EpicorSession.SessionID.ToString(), "", "sessionidc", "");
                InvTransferImpl invAD = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<InvTransferImpl>(EpicorSession, ImplBase<Erp.Contracts.InvTransferSvcContract>.UriPath);
                InvTransferDataSet invDS;

                DataRow invDr;
                string lotNum, tranUOM;
                decimal tranQty;
                bool bstr1;
                string str1, str2;


                for (i = 0; i < rcnt; i++)
                {
                    string uomCode = "";
                    // Declare and Initialize Variables
                    string serialWarning, questionString;
                    bool multipleMatch;
                    Guid guid = new Guid();
                    string xpartnum = tranDT.Rows[i]["partnum"].ToString().Trim();
                    invAD.GetPartXRefInfo(ref xpartnum, ref uomCode, guid, "", out serialWarning, out questionString, out multipleMatch);

                    // Call Adapter method
                    InvTransferDataSet dsInvTransfer = invAD.GetTransferRecord(xpartnum, uomCode);
                    dsInvTransfer.Tables["InvTrans"].Rows[0]["TransferQty"] = Convert.ToDecimal(tranDT.Rows[i]["tranQty"].ToString().Trim());
                    invAD.ChangeUOM(dsInvTransfer);

                    dsInvTransfer.Tables["InvTrans"].Rows[0]["FromWarehouseCode"] = tranDT.Rows[i]["fromWHcode"].ToString().Trim();
                    invAD.ChangeFromWhse(dsInvTransfer);
                    dsInvTransfer.Tables["InvTrans"].Rows[0]["toWarehouseCode"] = tranDT.Rows[i]["toWHcode"].ToString().Trim();
                    invAD.ChangeToWhse(dsInvTransfer);
                    if (dsInvTransfer.Tables["InvTrans"].Rows[0]["toBinNum"].ToString().Trim().Length < 1)
                    {
                        dsInvTransfer.Tables["InvTrans"].Rows[0]["toBinNum"] = tranDT.Rows[i]["toBinNum"].ToString().Trim();
                        invAD.ChangeToBin(dsInvTransfer);
                    }
                    if (dsInvTransfer.Tables["InvTrans"].Rows[0]["fromBinNum"].ToString().Trim().Length < 1)
                    {
                        dsInvTransfer.Tables["InvTrans"].Rows[0]["fromBinNum"] = tranDT.Rows[i]["fromBinNum"].ToString().Trim();
                        invAD.ChangeFromBin(dsInvTransfer);
                    }
                    if (dsInvTransfer.Tables["InvTrans"].Rows[0]["FromLotNumber"].ToString().Trim().Length < 1)
                    {
                        dsInvTransfer.Tables["InvTrans"].Rows[0]["FromLotNumber"] = lotnum;
                        invAD.ChangeLot(dsInvTransfer);
                    }
                    if (dsInvTransfer.Tables["InvTrans"].Rows[0]["ToLotNumber"].ToString().Trim().Length < 1)
                    {
                        dsInvTransfer.Tables["InvTrans"].Rows[0]["ToLotNumber"] = lotnum;
                        invAD.ChangeLot(dsInvTransfer);
                    }
                    string pcBinNum = dsInvTransfer.Tables["InvTrans"].Rows[0]["fromBinNum"].ToString();
                    string pcLotNum = "", pcDimCode = dsInvTransfer.Tables["InvTrans"].Rows[0]["trackingUOM"].ToString();
                    decimal pdDimConvFactor = 1M;
                    string pcNeqQtyAction = "", pcMessage;
                    //adapterInvTransfer.NegativeInventoryTest(iPartNum, FromWarehouseCode, pcBinNum, pcLotNum, pcDimCode, pdDimConvFactor, qty, out pcNeqQtyAction, out pcMessage);  
                    // MessageBox.Show("pcMessage:" + pcMessage + "     pcNeqQtyAction: " + pcNeqQtyAction);
                    string legalNumberMessage = "";
                    bool requiresUserInput = true;
                    string msg = "";
                    invAD.MasterInventoryBinTests(dsInvTransfer, out msg, out msg, out msg, out msg, out msg, out msg);
                    invAD.PreCommitTransfer(dsInvTransfer, out requiresUserInput);
                    string partTranPKs = "";
                    //MessageBox.Show(" requiresUserInput: " + requiresUserInput);
                    invAD.CommitTransfer(dsInvTransfer, out legalNumberMessage, out partTranPKs);



                    //invDS = new InvTransferDataSet();
                    //invDr = invDS.Tables["InvTrans"].NewRow();
                    //invDr["Company"] = companyId; //((Epicor.Mfg.Core.Session)(oTrans.Session)).CompanyID;  //"EPIC06";
                    //invDr["TranDate"] = DateTime.Today;
                    //invDr["FromWarehouseCode"] = tranDT.Rows[i]["fromWHcode"].ToString().Trim();
                    //invDr["FromWarehouseDesc"] = tranDT.Rows[i]["fromWHname"].ToString().Trim();
                    //invDr["ToWarehouseCode"] = tranDT.Rows[i]["toWHcode"].ToString().Trim();    //"OCE"; 
                    //invDr["ToWarehouseDesc"] = tranDT.Rows[i]["toWHname"].ToString().Trim(); ;   // OCE
                    //invDr["FromBinNum"] = tranDT.Rows[i]["fromBinNum"].ToString().Trim(); ;
                    //invDr["FromBinDesc"] = tranDT.Rows[i]["fromBinName"].ToString().Trim();
                    //invDr["ToBinNum"] = tranDT.Rows[i]["toBinNum"].ToString().Trim();    // OCE1
                    //invDr["ToBinDesc"] = tranDT.Rows[i]["toBinName"].ToString().Trim(); // OCE1
                    //lotNum = tranDT.Rows[i]["lotnum"].ToString().Trim();
                    //invDr["FromLotNumber"] = lotNum;
                    //invDr["ToLotNumber"] = lotNum;
                    //tranQty = Convert.ToDecimal(tranDT.Rows[i]["tranQty"].ToString().Trim());
                    //invDr["FromOnHandQty"] = tranQty;
                    //invDr["ToOnHandQty"] = tranQty;
                    //invDr["Plant"] = "mfgsys";//((Epicor.Mfg.Core.Session)(oTrans.Session)).PlantID;    //"MfgSys";
                    //invDr["Plant2"] = "mfgsys";//((Epicor.Mfg.Core.Session)(oTrans.Session)).PlantID;   //"OC";  //mfgsys
                    //invDr["PartNum"] = tranDT.Rows[i]["partnum"].ToString().Trim();// tranDa[8];
                    //invDr["TrackDimension"] = false;
                    //invDr["TrackSerialnumbers"] = false;
                    //if (lotNum == "")
                    //{ invDr["TrackLots"] = false; }
                    //else
                    //{ invDr["TrackLots"] = true; }
                    //invDr["PartDescription"] = tranDT.Rows[i]["partdesc"].ToString().Trim();
                    //invDr["SearchWord"] = "";
                    //invDr["TranReference"] = "";
                    //invDr["FromPlant"] = "mfgsys";// ((Epicor.Mfg.Core.Session)(oTrans.Session)).PlantID;
                    //invDr["FromPlantTracking"] = true;
                    //invDr["ToPlant"] = "mfgsys"; //((Epicor.Mfg.Core.Session)(oTrans.Session)).PlantID;   //"OC"; //"OC";  //MfgSys
                    //invDr["ToPlantTracking"] = true;
                    //tranUOM = tranDT.Rows[i]["uom"].ToString().Trim();// tranDa[12];
                    //invDr["FromOnHandUOM"] = tranUOM;
                    //invDr["TransferQty"] = tranQty;
                    //invDr["TransferQtyUOM"] = tranUOM;
                    //invDr["ToOnHandUOM"] = tranUOM;
                    //invDr["TrackingUOM"] = tranUOM;
                    //invDr["TrackingQty"] = tranQty;
                    //invDr["TranDocTypeID"] = "";
                    ////invDr["PkgNum"] = 0;
                    ////invDr["FromPkgNum"] = 0;
                    //// invDr["ToPkgNum"] = 0;
                    //invDr["ToOrderNum"] = 0;
                    //invDr["ToOrderLine"] = 0;
                    //invDr["ToOrderRelNum"] = 0;
                    ////invDr["ChildPkgNum"] = 0;
                    ////invDr["ChildPkgLine"] = 0;
                    ////invDr["ToPkgLine"] = 0;
                    ////invDr["ToPkgTranDocTypeID"] = "";
                    ////invDr["ToPkgCode"] = "";
                    ////invDr["ToPkgNumExist"] = false;
                    ////invDr["PCID"] = 0;
                    //invDS.Tables["InvTrans"].Rows.Add(invDr);
                    //invAD.PreCommitTransfer(invDS, out bstr1);
                    //invAD.CommitTransfer(invDS, out str1, out str2);
                    //invDS.Dispose();

                    //if (invAD.PreCommitTransfer(invDS, out bstr1) == false)
                    //{ invAD.Dispose(); return false; }

                    //    if (invAD.CommitTransfer(invDS, out str1, out str2))
                    //    { invAD.Dispose(); return true; }
                    //    else
                    //    { invAD.Dispose(); return false; }




                }


                //        if (!chkData(rowCount)) {return;}		

                ////处理转仓
                //for (i=0;i&lt;rowCount;i++)
                //{
                //    string[] tranDa=new string[13];
                //    for (j=0;j&lt;ugDetail.DisplayLayout.Bands[0].Columns.Count;j++)
                //        {tranDa[j]=ugDetail.Rows[i].Cells[j].Value.ToString().Trim();}

                //    if  (CallInvTrans(tranDa))
                //        {
                //            txtImportjd.Text="转仓进度:" + i.ToString() + "/" + rowCount.ToString();
                //            txtImportjd.Refresh();
                //        }
                //        else
                //        {
                //            txtImportjd.Text="第" + (i+1).ToString() + "行转仓失败,请修改后重新再试" ;
                //            return;
                //        }
                //}
                EpicorSession.Dispose();
                return "1|处理成功.";

            }

            catch (Exception ex)
            {
                //EpicorSession.Dispose();
                return "0|" + ex.Message.ToString();

            }


        }







        //D0502-02 结束BPM流程节点,并发起下步多人办理流程,注：流程中下步节点必须是并签
        public string D0502_02(string userid, string bind, string taskid, string style, string node, string nextUser, string title)
        {
            if (userid.Trim() == "") { return "0|用户不可为空"; }
            if (userExit(userid.Trim()) == false) { return "0|用户" + userid + "不存在"; }
            if (nextUser.Trim() == "") { return "0|下步办理人不可为空"; }
            string[] nextUserS = nextUser.Split(' ');
            string nextUserID = "";
            DataTable dt2 = null;
            for (int i = 0; i < nextUserS.Length; i++)
            {
                nextUserID = nextUserS[i].ToString().Trim();
                string awsql = "select userid from orguser   where userid='" + nextUserID + "'";
                dt2 = GetDataByAWS(awsql);
                if (dt2.Rows.Count == 0) { return "0|下步办理人" + nextUserID + "不存在!"; }

            }


            if (bind.Trim() == "") { return "0|流程ID不可为空"; }
            if (taskid.Trim() == "") { return "0|任务不可为空"; }
            if (style.Trim() == "") { return "0|流程类型不可为空"; }
            if (title.Trim() == "") { return "0|任务标题不可为空"; }

            string straid = "", boid = "";
            switch (style.Trim())
            {
                case "采购验收流程组":
                    straid = cgysStra;
                    boid = cgysBo;
                    break;
                default:
                    return "0|暂不支持流程:" + style;

            }


            try
            {
                string sqlstr = "select  BIND_ID,ID,OWNER,TARGET,TITLE  from WF_TASK where BIND_ID = " + bind;
                DataTable dt = GetDataByAWS(sqlstr);
                if (dt.Rows.Count == 0) { return "0|当前任务不存在"; }
                BPMInterface bpm = new BPMInterface(bpmSer, straid, boid);
                //bpm.NextStepOne2(userid, bind, taskid, nextuser, title, dt);
                bpm.NextStepOne3(bind, taskid, nextUser, title, userid);
                gtaskid = bpm.Gtaskid;
                return "1|处理成功";
            }
            catch (Exception ex)
            { return "0|" + ex.Message.ToString(); }

        }



        //D0201-020
        public int D0201_020(string bpmid, string jobnum, int jobasq, string lotnum, string companyId)
        {
            try
            {
                int SBM = 0;
                //string sql = "select max(SeqNum) from BO_PROCESS where JOBID = '" + jobnum + "' and partiallyid=" + jobasq.ToString().Trim();
                string sql = " select isnull(max(aa.sbm),0) from " +
              "(select isnull(BQSBM,0) as sbm from BO_PRINTRECORD where COMPANYID='" + companyId + "' and GDH='" + jobnum + "' and BCPH=" + jobasq.ToString().Trim() +
             " union all " +
             " select isnull(SeqNum,0) from BO_PROCESS where  JOBID = '" + jobnum + "' and partiallyid=" + jobasq.ToString().Trim() + ") aa ";//company1过滤

                DataTable dt = GetDataByAWS(sql);
                if (dt.Rows.Count > 0)
                { SBM = Convert.ToInt32(dt.Rows[0][0]); }
                SBM++;
                UpdateAWS("update  BO_PROCESS set seqnum=" + SBM + "  where  BINDID='" + bpmid + "'");
                return SBM;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }


        //D0201-010
        public string D0201_010(string jobnum, int jobasq, string lotnum, string companyId)
        {
            try
            {

                string sql = "select id from BO_PRINTRECORD where COMPANYID='" + companyId + "' and gdh='" + jobnum + "' and bcph='" + jobasq.ToString().Trim() + "' and pch='" + lotnum + "' and zxzt=0 and bqzt=0";//company1过滤
                DataTable dt = GetDataByAWS(sql);
                if (dt.Rows.Count > 0)
                { return "0|用户当前工单已手工打印标签，不能重复打印！"; }
                else
                { return "1|处理成功"; }
            }
            catch (Exception ex)
            {
                return "0|错误，请联系管理员.";
            }
        }


        //D0201-040 
        public int D0201_040(string jobnum, int jobasq, string lotnum, string companyId)
        {
            int bqsbm = 0;
            try
            {

                string sql = "select BQSBM from BO_PRINTRECORD where COMPANYID='" + companyId + "' and gdh='" + jobnum + "' and bcph='" + jobasq.ToString().Trim() + "' and pch='" + lotnum + "'   and bqzt=0 and zxzt=0 "; //company1过滤
                DataTable dt = GetDataByAWS(sql);
                if (dt.Rows.Count > 0)
                {

                    int.TryParse(dt.Rows[0][0].ToString(), out bqsbm);
                    //UpdateAWS("update BO_PRINTRECORD set zxzt=1   where gdh='" + jobnum + "' and bcph='" + jobasq.ToString().Trim() + "' and pch='" + lotnum + "'  and bqzt=0 and zxzt=0 ");

                    return bqsbm;
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception ex)
            {
                return -1;
            }
        }


        //D0201-050
        public string D0201_050(string bpmid, string jobnum, int jobasq, string lotnum, int bqsbm, string companyId)
        {
            try
            {

                //string sql = "update BO_PROCESS set SeqNum=" + bqsbm + "    where BINDID='" + bpmid + "' and  JOBID = '" + jobnum + "' and partiallyid=" + jobasq.ToString().Trim();
                string sql = "update BO_PROCESS set SeqNum=" + bqsbm + "    where  BINDID=" + bpmid + " and seqnum=0"; //company1过滤

                UpdateAWS(sql);
                UpdateAWS("update BO_PRINTRECORD set zxzt=1   where COMPANYID='" + companyId + "' and gdh='" + jobnum + "' and bcph='" + jobasq.ToString().Trim() + "' and pch='" + lotnum + "'  and bqzt=0 and zxzt=0 ");//company1过滤

                return "1|处理成功";
            }
            catch (Exception ex)
            {
                return "0|错误，请联系管理员.";
            }
        }


        //D01-030
        public int D01_030(string jobnum, int jobasmSeq, string partnum, string partdesc, string lotnum, double qty, int bqsbm, int printQty, int zxzt, int bqzt, string userid, string companyId)
        {
            try
            {
                int SBM = 0, id = 0, sbm2 = 0;
                string sql = "select isnull(max(SeqNum),0) from BO_PROCESS where JOBID = '" + jobnum + "' and partiallyid=" + jobasmSeq.ToString().Trim(); //company1过滤
                DataTable dt = GetDataByAWS(sql);
                if (dt.Rows.Count > 0)
                { int.TryParse(dt.Rows[0][0].ToString(), out SBM); }
                SBM++;

                sql = "select isnull(MAX(BQSBM),0) from BO_PRINTRECORD where COMPANYID='" + companyId + "' and GDH='" + jobnum + "' and BCPH=" + jobasmSeq.ToString().Trim();//company1过滤
                dt = GetDataByAWS(sql);
                if (dt.Rows.Count > 0)
                { int.TryParse(dt.Rows[0][0].ToString(), out sbm2); }
                sbm2++;

                if (SBM < sbm2) { SBM = sbm2; }

                //sql = "update BO_PROCESS set  SeqNum=" + SBM + " where JOBID = '" + jobnum + "' and partiallyid=" + jobasmSeq.ToString().Trim();
                //UpdateAWS(sql);
                sql = "select MAX(id) from BO_PRINTRECORD where COMPANYID='" + companyId + "'";//company1过滤
                dt = GetDataByAWS(sql);
                if (dt.Rows.Count > 0)
                { int.TryParse(dt.Rows[0][0].ToString(), out id); }
                id++;

                sql = "insert into BO_PRINTRECORD (gdh,bcph,wlbh,wlms,pch,sl,bqsbm,dyfs,zxzt,bqzt,id,CREATEUSER,COMPANYID) values " +
                     " ('" + jobnum + "'," + jobasmSeq + ",'" + partnum + "','" + partdesc + "','" + lotnum + "'," + qty + "," + SBM + "," + printQty + "," + zxzt + "," + bqzt + "," + id + ",'" + userid + "','" + companyId + "')";//company1过滤
                UpdateAWS(sql);
                return SBM;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }




        //D01-020
        public string D01_020(string jobnum, int jobasmSeq, string lotnum, string companyId)
        {
            try
            {

                string sql = "select id from BO_PRINTRECORD where COMPANYID='" + companyId + "' and gdh='" + jobnum + "' and bcph=" + jobasmSeq + " and pch='" + lotnum + "' and zxzt=0 and bqzt=0";//company1过滤
                DataTable dt = GetDataByAWS(sql);
                if (dt.Rows.Count > 0)
                { return "0|当前工单已经预打印，但还未发起转序，请先发起转序，本次打印不成功！"; }

                sql = "select id from BO_PROCESS where   JOBID='" + jobnum + "' and partiallyid=" + jobasmSeq + " and  ISEND=0 ";//company1过滤
                dt = GetDataByAWS(sql);
                if (dt.Rows.Count > 0)
                { return "0|当前工单有未完成的转序流程，，本次打印不成功！"; }
                else
                { return "1|处理成功"; }
            }
            catch (Exception ex)
            {
                return "0|错误，请联系管理员！";
            }
        }


        //D01-080
        public string D01_080(string jobnum, int jobasmSeq, string lotnum, int bqsbm, string companyId)
        {
            try
            {

                string sql = "select id from BO_PRINTRECORD where COMPANYID='" + companyId + "' and gdh='" + jobnum + "' and bcph=" + jobasmSeq + " and pch='" + lotnum + "' and zxzt=0 and bqzt=0 and bqsbm=" + bqsbm;//company1过滤
                DataTable dt = GetDataByAWS(sql);
                if (dt.Rows.Count <= 0)
                { return "0|当前标签未打印，不能作废！"; }
                else
                { return "1|处理成功"; }
            }
            catch (Exception ex)
            {
                return "0|错误，请联系管理员！";
            }
        }



        //D01-090
        public string D01_090(string jobnum, int jobasmSeq, string lotnum, int bqsbm, string companyId)
        {
            try
            {


                string sqlstr = "update BO_PRINTRECORD  set bqzt=1 where COMPANYID='" + companyId + "' and gdh='" + jobnum + "' and BCPH=" + jobasmSeq + " and PCH='" + lotnum + "'  and BQSBM=" + bqsbm;//company1过滤
                UpdateAWS(sqlstr);
                return "1|处理成功";
            }
            catch (Exception ex)
            {
                return "0|" + ex.Message.ToString();
            }
        }




        //D01-010
        public string D01_010(string jobnum, string companyId)
        {

            #region
            DataTable dthed = new DataTable("dh1");
            dthed.Columns.Add("jobState", Type.GetType("System.String"));
            dthed.Columns.Add("jobasSeq", Type.GetType("System.Int32"));
            dthed.Columns.Add("partnum", Type.GetType("System.String"));
            dthed.Columns.Add("partdesc", Type.GetType("System.String"));
            dthed.Columns.Add("lotnum", Type.GetType("System.String"));
            dthed.Columns.Add("qty", Type.GetType("System.Double"));
            dthed.Columns.Add("bqsbm", Type.GetType("System.Int32"));
            dthed.Columns.Add("printQty", Type.GetType("System.Int32"));
            dthed.Columns.Add("zxzt", Type.GetType("System.Int32"));
            dthed.Columns.Add("bqzt", Type.GetType("System.Int32"));
            DataRow dr = dthed.NewRow();
            dr["jobState"] = "无效工单";
            dr["jobasSeq"] = 0;
            dr["partnum"] = "";
            dr["partdesc"] = "";
            dr["lotnum"] = "";
            dr["qty"] = 0;
            dr["bqsbm"] = 0;
            dr["printQty"] = 0;
            dr["zxzt"] = 0;
            dr["bqzt"] = 0;
            dthed.Rows.Add(dr);
            #endregion

            if (jobnum.Trim() == "") { dthed.Rows[0]["jobState"] = "无效工单"; return DataTableToJson2(dthed); }
            try
            {
                //string resultdata = ErpLogin/();
                //if (resultdata != "true")
                //{
                //    return "0|" + resultdata;
                //}
                Session EpicorSession = ErpLoginbak();
                if (EpicorSession == null)
                {
                    return "0|erp用户数不够，请稍候再试.ERR:D01_010";
                }
                EpicorSession.CompanyID = companyId;
                //WriteGetNewLaborInERPTxt("", EpicorSession.SessionID.ToString(), "", "sessionide", "");
                JobEntryImpl jobEntry = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<JobEntryImpl>(EpicorSession, ImplBase<Erp.Contracts.JobEntrySvcContract>.UriPath);
                LaborDtlSearchImpl search = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<LaborDtlSearchImpl>(EpicorSession, ImplBase<Erp.Contracts.LaborDtlSearchSvcContract>.UriPath);
                JobEntryDataSet DsJobEntry;
                try
                {
                    DsJobEntry = jobEntry.GetByID(jobnum);
                    dthed.Rows[0]["jobState"] = "有效工单";
                    dthed.Rows[0]["lotnum"] = jobnum;
                    JobEntryDataSet.JobAsmblDataTable jobAsDT = DsJobEntry.JobAsmbl;
                    DataRow[] jobAsDr = jobAsDT.Select("AssemblySeq=0");
                    double jobqty = 0, rcvqty = 0;
                    string partnum = "";
                    if (jobAsDr != null && jobAsDr.Length > 0)
                    {
                        partnum = jobAsDr[0]["partnum"].ToString();
                        dthed.Rows[0]["partnum"] = partnum;
                        dthed.Rows[0]["partdesc"] = jobAsDr[0]["Description"].ToString().Trim();
                        jobqty = Convert.ToDouble(jobAsDr[0]["RequiredQty"]);//RequiredQty
                    }

                    JobEntryDataSet.JobPartDataTable jobpartDT = DsJobEntry.JobPart;
                    DataRow[] jobpartDr = jobpartDT.Select("PartNum='" + partnum + "'");
                    if (jobpartDr != null && jobpartDr.Length > 0)
                    {
                        rcvqty = Convert.ToDouble(jobpartDr[0]["ReceivedQty"]);
                    }

                    dthed.Rows[0]["qty"] = jobqty - rcvqty;


                }
                catch
                {

                    dthed.Rows[0]["jobState"] = "无效工单";
                    EpicorSession.Dispose();


                }
                EpicorSession.Dispose();
                return DataTableToJson2(dthed);

            }
            catch (Exception ex)
            {

                return DataTableToJson2(dthed);
            }
        }



        public string DataTableToJson2(System.Data.DataTable dt)
        {
            StringBuilder Json = new StringBuilder();
            Json.Append("[");
            if (dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    Json.Append("{");
                    for (int j = 0; j < dt.Columns.Count; j++)
                    {
                        Json.Append("\"" + dt.Columns[j].ColumnName.ToString() + "\":\"" + dt.Rows[i][j].ToString() + "\"");
                        if (j < dt.Columns.Count - 1)
                        {
                            Json.Append(",");
                        }
                    }
                    Json.Append("}");
                    if (i < dt.Rows.Count - 1)
                    {
                        Json.Append(",");
                    }
                }
            }
            //  Json.Append("]}");  
            Json.Append("]");
            return Json.ToString();
        }

        //返回查询po信息，速度慢
        public string D04(string str, string companyId)
        {
            //速度慢
            string resuStr = "";

            #region changerToVar2
            string vendorid, vendorName, partnum, partDesc;
            int ponum = 0, poline = 0;
            DateTime orderDate1, orderDate2;
            bool vendorid_f = false, vendorName_f = false, ponum_f = false, poline_f = false, partnum_f = false, partDesc_f = false, orderDate1_f = false, orderDate2_f = false;
            //[{"vendorid=":"A506","vendorNameLike":"上海中元","ponum=":"837124","poline=":"1","partnum=":"3100211","partDescLike":"六价黄色","orderDate>=":"2016-5-1","orderDate<=":"2016-5-16"}]
            //[{"vendorid=":"","vendorNameLike":"","ponum=":"841163","poline=":"2","partnum=":"","partDescLike":"","orderDate>=":"","orderDate<=":""}]

            try
            {
                JArray ja = (JArray)JsonConvert.DeserializeObject(str);
                vendorid = ja[0]["vendorid="].ToString().Trim();
                if (vendorid != "") { vendorid_f = true; }
                vendorName = ja[0]["vendorNameLike"].ToString().Trim();
                if (vendorName != "") { vendorName_f = true; }
                if (int.TryParse(ja[0]["ponum="].ToString(), out ponum)) { ponum_f = true; }
                if (int.TryParse(ja[0]["poline="].ToString(), out poline)) { poline_f = true; }
                if (DateTime.TryParse(ja[0]["orderDate>="].ToString(), out orderDate1)) { orderDate1_f = true; }
                if (DateTime.TryParse(ja[0]["orderDate<="].ToString(), out orderDate2)) { orderDate2_f = true; }
                partnum = ja[0]["partnum="].ToString();
                if (partnum != "") { partnum_f = true; }
                partDesc = ja[0]["partDescLike"].ToString().Trim();
                if (partDesc != "") { partDesc_f = true; }

                if ((vendorid_f || vendorName_f || ponum_f || partnum_f || partDesc_f || orderDate1_f || orderDate2_f) == false)
                { return "0|;最少需要一个有效的查询条件"; }


            }
            catch (Exception ex)
            {
                return "0|错误，请检查传入参数是否正确。" + ex.Message.ToString();

            }
            #endregion





            try
            {
                //qdDS.QueryWhereItem.DefaultView.RowFilter = "DataTableID='JobOper' and FieldName='OpCode'";              
                //qdDS.QueryWhereItem.DefaultView[0]["RValue"] = "保护";
                DataTable dt = BaqResult("001-queryPO", null, 0, companyId);
                if (dt != null)
                {
                    int rowCnt = dt.Rows.Count;
                    //resuStr = rowCnt.ToString();
                    DataView dv = new DataView(dt);
                    string sfiter = "";
                    if (vendorid_f) { sfiter = sfiter + " and  Vendor.VendorID ='" + vendorid + "'"; }
                    if (vendorName_f) { sfiter = sfiter + " and  Vendor.Name like '%" + vendorid + "%'"; }
                    if (ponum_f) { sfiter = sfiter + " and PODetail.PONUM =" + ponum; }
                    if (poline_f) { sfiter = sfiter + " and PODetail.POLine =" + poline; }
                    if (partnum_f) { sfiter = sfiter + " and  PODetail.PartNum ='" + partnum + "'"; }
                    if (partDesc_f) { sfiter = sfiter + " and  PODetail.LineDesc '%" + partDesc + "%'"; }
                    if (orderDate1_f) { sfiter = sfiter + " and  POHeader.OrderDate >='" + orderDate1.ToString("yyyy-MM-dd") + "'"; }
                    if (orderDate2_f) { sfiter = sfiter + " and  POHeader.OrderDate <='" + orderDate2.ToString("yyyy-MM-dd") + "'"; }

                    sfiter = sfiter.Substring(5, sfiter.Length - 5);

                    dv.RowFilter = sfiter;
                    DataTable dt2 = dv.ToTable();


                    //vendorid_f && vendorName_f && ponum_f && partnum_f && partDesc_f && orderDate1_f && orderDate2_f)

                    // return "1|" + dt2.Rows.Count.ToString();
                    return "1|" + DataTableToJson2(dt2);
                }
                return resuStr;
            }
            catch
            {
                return null;
            }




        }

        public DataTable BaqResult(string pcQueryID, List<QueryWhereItemRow> whereItems, int size, string companyId)
        {
            try
            {
                Session EpicorSession = ErpLoginbak();
                if (EpicorSession == null)
                {
                    return null;
                }
                EpicorSession.CompanyID = companyId;
                DynamicQueryImpl adapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<DynamicQueryImpl>(EpicorSession, ImplBase<Ice.Contracts.DynamicQuerySvcContract>.UriPath);
                DataTable dt = new DataTable();
                var querydataset = adapter.GetByID(pcQueryID);
                if (whereItems != null && whereItems.Count > 0)
                {
                    foreach (var item in whereItems)
                    {
                        // item.Table = querydataset.Tables["QueryWhereItem"];
                        querydataset.QueryWhereItem.AddQueryWhereItemRow(
                            "",//string Company
                        pcQueryID,//string QueryID
                       (Guid)querydataset.QuerySubQuery[0]["SubQueryID"],//Guid SubQueryID
                        item.TableID,//string TableID
                        Guid.NewGuid(),//Guid CriteriaID
                        1,//int Seq
                        item.FieldName,//string FieldName
                        "varchar",//string DataType
                        item.CompOp,//string CompOp
                        "And",//string AndOr
                        false,//bool Negative
                        "",//string LeftP (
                        "",//string RightP )
                        true,//bool IsConst
                        2,//int CriteriaType
                        "",//string ToTableID
                        "",//string ToFieldName
                        "",//string ToDataType
                        item.RValue,//string RValue
                        false,//bool ExtSecurity
                        198456,//long SysRevID
                        Guid.NewGuid(),//Guid SysRowID
                        0,//int BitFlag
                        ""//string RowMod
                            );
                    }
                }
                var queryExecutionDataSet = new QueryExecutionDataSet();
                if (size > 0)
                {
                    queryExecutionDataSet.ExecutionSetting.AddExecutionSettingRow("TopN", size.ToString(), Guid.NewGuid(), "");
                }
                DataSet ds = adapter.Execute(querydataset, queryExecutionDataSet);
                EpicorSession.Dispose();
                return ds.Tables[0];
            }
            catch (Exception err)
            {
                throw;
            }
        }


        //返回查询po信息，速度快
        public string D04_1(string str, string companyId)
        {
            //if (EpicorSessionManager.EpicorSession == null || !EpicorSessionManager.EpicorSession.IsValidSession(EpicorSessionManager.EpicorSession.SessionID, "manager"))
            //{
            //    EpicorSessionManager.EpicorSession = CommonClass.Authentication.GetEpicorSession();
            //}
            //DynamicQueryImpl adapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<DynamicQueryImpl>(EpicorSessionManager.EpicorSession, ImplBase<Ice.Contracts.DynamicQuerySvcContract>.UriPath);
            string resuStr = "";
            #region changerToVar2
            string vendorid, vendorName, partnum, partDesc;
            int ponum = 0, poline = 0, poRelNum = 0;
            DateTime orderDate1, orderDate2;
            bool vendorid_f = false, vendorName_f = false, ponum_f = false, poline_f = false, partnum_f = false, partDesc_f = false, orderDate1_f = false, orderDate2_f = false, poRelNum_f = false;
            //[{"vendorid=":"A506","vendorNameLike":"上海中元","ponum=":"837124","poline=":"1","partnum=":"3100211","partDescLike":"六价黄色","orderDate>=":"2016-5-1","orderDate<=":"2016-5-16"}]
            //[{"vendorid=":"","vendorNameLike":"","ponum=":"841163","poline=":"2","partnum=":"","partDescLike":"","orderDate>=":"","orderDate<=":""}]

            try
            {
                JArray ja = (JArray)JsonConvert.DeserializeObject(str);
                vendorid = ja[0]["vendorid="].ToString().Trim();
                if (vendorid != "") { vendorid_f = true; }
                vendorName = ja[0]["vendorNameLike"].ToString().Trim();
                if (vendorName != "") { vendorName_f = true; }
                if (int.TryParse(ja[0]["ponum="].ToString(), out ponum)) { ponum_f = true; }
                if (int.TryParse(ja[0]["poline="].ToString(), out poline)) { poline_f = true; }
                if (DateTime.TryParse(ja[0]["orderDate>="].ToString(), out orderDate1)) { orderDate1_f = true; }
                if (DateTime.TryParse(ja[0]["orderDate<="].ToString(), out orderDate2)) { orderDate2_f = true; }
                partnum = ja[0]["partnum="].ToString();
                if (partnum != "") { partnum_f = true; }
                partDesc = ja[0]["partDescLike"].ToString().Trim();
                if (partDesc != "") { partDesc_f = true; }
                if (int.TryParse(ja[0]["poRelNum="].ToString(), out poRelNum)) { poRelNum_f = true; } //by Danny

                if ((vendorid_f || vendorName_f || ponum_f || partnum_f || partDesc_f || orderDate1_f || orderDate2_f || poRelNum_f) == false)
                {

                    return "0|;最少需要一个有效的查询条件";
                }


            }
            catch (Exception ex)
            {

                return "0|错误，请检查传入参数是否正确。" + ex.Message.ToString();

            }
            #endregion
            try
            {
                string sfiter = "";
                //baq添加条件，指定参数，参数为空时跳过一定要选上
                //QueryDesignDataSet qdDS = adapter.GetDashBoardQuery("001-queryPO2");
                //List<QueryWhereItemRow> whereItems = new List<QueryWhereItemRow>();
                //if (poline_f) { sfiter = sfiter + " and PODetail.POLine =" + poline; }
                //if (ponum_f)
                //{
                //    whereItems.Add(new QueryWhereItemRow() { TableID = "podetail", FieldName = "ponum", RValue = ponum.ToString() });
                //}
                //if (poline_f)
                //{
                //    whereItems.Add(new QueryWhereItemRow() { TableID = "podetail", FieldName = "poline", RValue = poline.ToString() });
                //}
                //if (vendorid_f)
                //{
                //    whereItems.Add(new QueryWhereItemRow() { TableID = "vendor", FieldName = "vendorid", RValue = vendorid.ToString() });
                //}
                //if (vendorName_f)
                //{
                //    whereItems.Add(new QueryWhereItemRow() { TableID = "vendor", FieldName = "name", RValue = "*" + vendorName + "*" });
                //}
                //if (partnum_f)
                //{
                //    whereItems.Add(new QueryWhereItemRow() { TableID = "podetail", FieldName = "partnum", RValue = partnum });
                //}
                //if (partDesc_f)
                //{
                //    whereItems.Add(new QueryWhereItemRow() { TableID = "podetail", FieldName = "linedesc", RValue = "*" + partDesc + "*" });
                //}
                //if (orderDate1_f)
                //{
                //    whereItems.Add(new QueryWhereItemRow() { TableID = "poheader", FieldName = "orderdate", RValue = orderDate1.ToString("MM/dd/yyyy") });
                //}
                //if (orderDate2_f)
                //{
                //    whereItems.Add(new QueryWhereItemRow() { TableID = "poheader", FieldName = "orderdate", RValue = orderDate2.ToString("MM/dd/yyyy") });
                //}
                //if (poRelNum_f)
                //{
                //    whereItems.Add(new QueryWhereItemRow() { TableID = "porel", FieldName = "porelnum", RValue = poRelNum.ToString() });
                //}
                //DataTable dt = BaqResult("001-queryPO2", whereItems, 0, companyId);// adapter.ExecuteDashBoardQuery(qdDS).Tables[0];
                string sql = "select [Vendor].[VendorID] as [Vendor_VendorID],[Vendor].[Name] as [Vendor_Name],[PODetail].[PONUM] as [PODetail_PONUM],[PODetail].[POLine] as [PODetail_POLine],[PORel].[PORelNum] as [PORel_PORelNum],[PODetail].[PartNum] as [PODetail_PartNum],[PODetail].[LineDesc] as [PODetail_LineDesc],[PORel].[XRelQty] as [PORel_XRelQty],[PORel].[DueDate] as [PORel_DueDate],[PORel].[TranType] as [PORel_TranType],[PartClass].[Description] as [PartClass_Description],[PORel].[JobNum] as [PORel_JobNum],[PORel].[AssemblySeq] as [PORel_AssemblySeq],[PORel].[JobSeq] as [PORel_JobSeq],[OpMaster].[OpDesc] as [OpMaster_OpDesc],[POHeader].[CommentText] as [POHeader_CommentText],[PODetail].[CommentText] as [PODetail_CommentText],((case when   Part.TypeCode = 'M'    then  '自制件'  else  (case when            Part.TypeCode = 'P'  then  '采购件'  else (case when  Part.TypeCode = 'K'  then  '销售套件'  else  '' end) end) end)) as [Calculated_partType],[PODetail].[IUM] as [PORel_BaseUOM],[POHeader].[OrderDate] as [POHeader_OrderDate],[POHeader].[ApprovalStatus] as [POHeader_ApprovalStatus],[POHeader].[Confirmed] as [POHeader_Confirmed],[PORel].[JobSeqType] as [PORel_JobSeqType],[PORel].[ReceivedQty] as[PORel_ReceivedQty] from Erp.PODetail as PODetail inner join Erp.Vendor as Vendor on PODetail.Company = Vendor.Company and PODetail.VendorNum = Vendor.VendorNum ";
                if (!string.IsNullOrEmpty(vendorid))
                {
                    sql = sql + " and Vendor.VendorID ='" + vendorid + "' ";
                }
                if (!string.IsNullOrEmpty(vendorName))
                {
                    sql = sql + " and Vendor.Name like '*" + vendorName + "*' ";
                }
                sql = sql + " inner join Erp.PORel as PORel on PODetail.Company = PORel.Company and PODetail.PONum = PORel.PONUM and PODetail.POLine = PORel.POLine ";
                if (!string.IsNullOrEmpty(poRelNum.ToString()) && poRelNum != 0)
                {
                    sql = sql + " and (PORel.PORelNum =" + poRelNum + ") ";
                }
                sql = sql + " left outer join Erp.JobOper as JobOper on PORel.Company = JobOper.Company and PORel.JobNum = JobOper.JobNum and PORel.AssemblySeq = JobOper.AssemblySeq and PORel.JobSeq = JobOper.OprSeq left outer join Erp.OpMaster as OpMaster on JobOper.Company = OpMaster.Company and JobOper.OpCode = OpMaster.OpCode left outer join Erp.Part as Part on PODetail.Company = Part.Company and PODetail.PartNum = Part.PartNum left outer join Erp.PartClass as PartClass on Part.Company = PartClass.Company and Part.ClassID = PartClass.ClassID inner join Erp.POHeader as POHeader on PODetail.Company = POHeader.Company and PODetail.PONum = POHeader.PONUM ";
                if (!string.IsNullOrEmpty(vendorName))
                {
                    sql = sql + " and Vendor.Name like '*" + vendorName + "*' ";
                }
                if (!string.IsNullOrEmpty(orderDate1.ToString()) && orderDate1.ToString("MM/dd/yyyy") != "01/01/0001")
                {
                    sql = sql + " and POHeader.OrderDate >='" + orderDate1.ToString("MM/dd/yyyy") + "' ";
                }
                if (!string.IsNullOrEmpty(orderDate2.ToString()) && orderDate2.ToString("MM/dd/yyyy") != "01/01/0001")
                {
                    sql = sql + " and POHeader.OrderDate <='" + orderDate2.ToString("MM/dd/yyyy") + "' ";
                }
                sql = sql + " where PODetail.Company = '" + companyId + "'  and PODetail.OpenLine = 1 ";
                if (!string.IsNullOrEmpty(ponum.ToString()) && ponum != 0)
                {
                    sql = sql + " and PODetail.PONUM ='" + ponum.ToString() + "' ";
                }
                if (!string.IsNullOrEmpty(poline.ToString()) && poline != 0)
                {
                    sql = sql + " and PODetail.POLine ='" + poline.ToString() + "' ";
                }
                if (!string.IsNullOrEmpty(partnum))
                {
                    sql = sql + " and PODetail.PartNum ='" + partnum + "' ";
                }
                if (!string.IsNullOrEmpty(partDesc))
                {
                    sql = sql + " and PODetail.LineDesc like '*" + partDesc + "*' ";
                }
                DataTable dt = GetDataByERP(sql);

                dt.Columns.Add("insQty");

                //替换引号
                int rowcnt = 0, colcnt = 0;
                DataTable dt2;
                string sqlstr = "";
                rowcnt = dt.Rows.Count;
                colcnt = dt.Columns.Count;
                for (int i = 0; i < rowcnt; i++)
                {
                    dt.Rows[i]["PODetail_LineDesc"] = dt.Rows[i]["PODetail_LineDesc"].ToString().Replace('"', '#');
                    dt.Rows[i]["PODetail_CommentText"] = dt.Rows[i]["PODetail_CommentText"].ToString().Replace('"', '#');
                    dt.Rows[i]["Vendor_Name"] = dt.Rows[i]["Vendor_Name"].ToString().Replace('"', '#');
                    dt.Rows[i]["POHeader_CommentText"] = dt.Rows[i]["POHeader_CommentText"].ToString().Replace('"', '#');
                    dt.Rows[i]["PODetail_LineDesc"] = dt.Rows[i]["PODetail_LineDesc"].ToString().Replace('\\', '/');
                    dt.Rows[i]["PODetail_CommentText"] = dt.Rows[i]["PODetail_CommentText"].ToString().Replace('\\', '/');
                    dt.Rows[i]["Vendor_Name"] = dt.Rows[i]["Vendor_Name"].ToString().Replace('\\', '/');
                    dt.Rows[i]["POHeader_CommentText"] = dt.Rows[i]["POHeader_CommentText"].ToString().Replace('\\', '/');
                    ponum = Convert.ToInt32(dt.Rows[i]["PODetail_PONUM"]);
                    poline = Convert.ToInt32(dt.Rows[i]["PODetail_POLine"]);
                    poRelNum = Convert.ToInt32(dt.Rows[i]["PORel_PORelNum"]);
                    //取得正在转序收货流程中的数量
                    //sqlstr = "select ISNULL(sum(shsl),0) from BO_SC_CGYSSUB where ponum=" + ponum + " and poline=" + poline + " and  isend=0";
                    sqlstr = "select ISNULL(sum(shsl),0) from BO_SC_CGYSSUB sub inner join HS.dbo.Receipt rct on sub.PCH=rct.BatchNo where ponum='" + ponum + "' and poline='" + poline + "' and rct.PORelNum='" + poRelNum + "' and  isend=0 ";
                    dt2 = GetDataByAWS(sqlstr);
                    if (dt2.Rows.Count > 0)
                    { dt.Rows[i]["insQty"] = dt2.Rows[0][0]; }
                    else
                    { dt.Rows[i]["insQty"] = "0"; }
                }

                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    dt.Columns[i].ColumnName = dt.Columns[i].ColumnName.Replace('_', '.');
                }
                return "1|" + DataTableToJson2(dt);
            }
            catch (Exception ex)
            {

                return null;
            }




        }



        //取得需求输入的物料接收人
        public string getReqRole(int ponum, int poline, int porel, string companyId)
        {
            string resuStr = "";




            try
            {



                //baq添加条件，指定参数，参数为空时跳过一定要选上

                List<QueryWhereItemRow> whereItems = new List<QueryWhereItemRow>();
                whereItems.Add(new QueryWhereItemRow() { TableID = "porel", FieldName = "ponum", RValue = ponum.ToString() });
                whereItems.Add(new QueryWhereItemRow() { TableID = "porel", FieldName = "poline", RValue = poline.ToString() });
                whereItems.Add(new QueryWhereItemRow() { TableID = "porel", FieldName = "porelnum", RValue = porel.ToString() });
                //DataTable dt = BaqResult("001-queryReqRole", whereItems, 0, companyId);
                DataTable dt = GetDataByERP("select [ReqDetail].[Character10] as [ReqDetail_Character10],[PORel].[PONum] as [PORel_PONum],[PORel].[POLine] as [PORel_POLine],[PORel].[PORelNum] as [PORel_PORelNum],[ReqDetail].[ReqNum] as [ReqDetail_ReqNum],[ReqDetail].[OpenLine] as [ReqDetail_OpenLine] from Erp.PORel as PORel inner join ReqDetail as ReqDetail on PORel.Company = ReqDetail.Company and PORel.ReqNum = ReqDetail.ReqNum and PORel.ReqLine= ReqDetail.ReqLine where (PORel.Company = '" + companyId + "'  and PORel.PONum ='" + ponum.ToString() + "'  and PORel.POLine ='" + poline.ToString() + "'  and PORel.PORelNum ='" + porel.ToString() + "')");
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    dt.Columns[i].ColumnName = dt.Columns[i].ColumnName.Replace('_', '.');
                }

                if (dt.Rows.Count > 0)
                {
                    return dt.Rows[0][0].ToString().Trim();
                }
                else
                { return ""; }


            }
            catch
            {

                return null;
            }




        }






        public string D0701_01(string jobnum, int PARTIALLYID, int bqsbm, string fType, string fnode, string user, string companyId)
        {
            try
            {
                //string awsql = "select  JOBID,PARTIALLYID,QUALIFIEDAMOUNT,EMPBASIC,PROCESSID,PROCESSDESC,USERNAME,PROCESSID,bindid, " +
                //         " CHECKSITE,PARTIALLYDESC,PROCESSDESC,QUALIFIEDAMOUNT,UNQUALIFIEDAMOUNT,UNQUALIFIEDREASONDESC,GXWLY from BO_PROCESS " +
                //         " where JOBID='" + jobnum + "' and PARTIALLYID=" + PARTIALLYID + " and SEQNUM=" + bqsbm;

                // select   wt.TARGET as userid ,wt.BIND_ID as bind, wt.id as taskid,wt.WF_STYLE as style,wt.TITLE  
                //from WF_TASK wt 
                //inner join wf_messagedata ws on ws.ID=wt.BIND_ID
                //where  wt.TARGET='admin' and wt.STATUS=1 and   wt.WF_STYLE='转序申请流程组' and ws.WFS_NO=4

                string awsql = "select   wt.TARGET as userid ,wt.BIND_ID as bind, wt.id as taskid,wt.WF_STYLE as style,wt.TITLE, " +
 " JOBID,PARTIALLYID,QUALIFIEDAMOUNT,EMPBASIC,PROCESSID,PROCESSDESC,USERNAME,b.bindid, " +
 " CHECKSITE,PARTIALLYDESC,PROCESSDESC,QUALIFIEDAMOUNT,UNQUALIFIEDAMOUNT,UNQUALIFIEDREASONDESC,GXWLY,b.USERNAME  " +
 " from WF_TASK wt " +
 " inner join wf_messagedata ws on ws.ID=wt.BIND_ID " +
 " inner join BO_PROCESS b on b.BINDID=wt.BIND_ID  " +
 " left join BO_PRINTRECORD p on p.GDH=b.JOBID  and p.BQSBM=b.SEQNUM " +
 " where  wt.TARGET='" + user + "' and wt.STATUS=1 and   wt.WF_STYLE='转序申请流程组' and ws.WFS_NO=4 " +
 " and JOBID='" + jobnum + "' and PARTIALLYID=" + PARTIALLYID
 + " and b.SEQNUM=" + bqsbm + "  and isnull(p.BQZT,0)=0";//company1过滤




                //           string awsql = "   select   wt.TARGET as userid ,wt.BIND_ID as bind, wt.id as taskid,wt.WF_STYLE as style,wt.TITLE,  JOBID,PARTIALLYID, " +
                // " QUALIFIEDAMOUNT, " +
                //" EMPBASIC,PROCESSID,PROCESSDESC,USERNAME,b.bindid,CHECKSITE,PARTIALLYDESC,PROCESSDESC,QUALIFIEDAMOUNT,UNQUALIFIEDAMOUNT, " +
                //" UNQUALIFIEDREASONDESC,GXWLY,b.USERNAME,ws.WFS_NO,b.seqnum,p.BQSBM,p.ZXZT  " +
                //"  from WF_TASK wt  " +
                //"  inner join wf_messagedata ws on ws.ID=wt.BIND_ID  " +
                //"  inner join BO_PROCESS b on b.BINDID=wt.BIND_ID " +
                //"  left join BO_PRINTRECORD p on p.GDH=b.JOBID  " +
                //"  where wt.TARGET='" + user + "' and  wt.STATUS=1 and   wt.WF_STYLE='转序申请流程组' " +
                //"   and JOBID='" + jobnum + "' and PARTIALLYID=" + PARTIALLYID + " and b.seqnum=" + bqsbm + " and p.ZXZT=1  and ws.WFS_NO=4  and p.BQSBM=" + bqsbm;


                DataTable dt = GetDataByAWS(awsql);

                if (dt.Rows.Count == 0)
                {
                    return "0|没有符合条件的记录";
                }
                string jsonStr = DataTableToJson2(dt);
                return "1|" + jsonStr;
            }
            catch (Exception ex)
            { return "0|查询出错，靖联系管理员." + ex.Message; }

        }


        public string D0702_01(string jsonstr, string companyId)
        {
            try
            {
                JArray ja = (JArray)JsonConvert.DeserializeObject(jsonstr);
                string rqr = ja[0]["rqr"].ToString().Trim();
                string jobnum = ja[0]["jobnum"].ToString().Trim();
                int asmSeq = 0, oprSeq = 0;
                if (int.TryParse(ja[0]["asmSeq"].ToString(), out asmSeq) == false) { return "0|流程办理出错，半成品号无效"; }
                if (int.TryParse(ja[0]["oprSeq"].ToString(), out oprSeq) == false) { return "0|流程办理出错，工序号无效"; }
                decimal Lqty = 0, disQty = 0;
                if (decimal.TryParse(ja[0]["Lqty"].ToString(), out Lqty) == false) { return "0|流程办理出错，合格数量无效"; }
                if (decimal.TryParse(ja[0]["disQty"].ToString(), out disQty) == false) { return "0|流程办理出错，不合格数量无效"; }
                string disCode = ja[0]["disCode"].ToString().Trim();
                string bjr = ja[0]["bjr"].ToString().Trim();
                // DateTime zxDate = System.DateTime.Today;
                // if (DateTime.TryParse(ja[0]["zxDate"].ToString(), out zxDate) == false) { return "0|流程办理出错，报检日期无效"; }
                string zxDate = ja[0]["zxDate"].ToString().Trim();
                string zxTime = ja[0]["zxTime"].ToString().Trim();
                string lotnum = ja[0]["lotnum"].ToString().Trim();
                string wh = ja[0]["wh"].ToString().Trim();
                string bin = ja[0]["bin"].ToString().Trim();
                string userid = ja[0]["userid"].ToString().Trim();
                int bind = 0, taskid = 0;
                if (int.TryParse(ja[0]["bind"].ToString(), out bind) == false) { return "0|流程办理出错，流程id无效"; }
                if (int.TryParse(ja[0]["taskid"].ToString(), out taskid) == false) { return "0|流程办理出错，任务id无效"; }
                string style = ja[0]["style"].ToString().Trim();
                string node = ja[0]["node"].ToString().Trim();
                string nextuser = ja[0]["nextuser"].ToString().Trim();
                string title = ja[0]["title"].ToString().Trim();
                int psetp = 0, pint = 0;
                int.TryParse(ja[0]["pint"].ToString().Trim(), out psetp);
                string resu = "";
                //if (psetp < 1)
                //{

                //    //最后一道工序报工
                //    resu = D0505(rqr, jobnum, asmSeq, oprSeq, Lqty, disQty, disCode, bjr, zxDate, zxTime, companyId);
                //    if (resu.Substring(0, 1) != "1")
                //    { return "3|" + pint + resu.Substring(1); }

                //}
                //pint = 1;

                //if (psetp < 2)
                //{
                //    //工单收货至库存
                //    resu = D0506_01(rqr, jobnum, asmSeq, Lqty, lotnum, wh, bin, companyId);
                //    if (resu.Substring(0, 1) != "1")
                //    { return "3|" + pint + resu.Substring(1); }

                //}
                resu = LaborMtlStk(rqr, jobnum, asmSeq, oprSeq, Lqty, disQty, disCode, bjr, zxDate, zxTime, lotnum, wh, bin, companyId, taskid.ToString());
                if (resu.Substring(0, 1) != "1")
                { return "3|" + pint + resu.Substring(1); }
                pint = 2;

                //resu = D0502_01(userid, bind.ToString(), taskid.ToString(), style, node, userid, title);
                //if (resu.Substring(0, 1) != "1")
                //{ return resu; }

                if (psetp < 3)
                {
                    //结束整个流程
                    if (closeWF(userid, bind.ToString().Trim(), taskid.ToString().Trim()) == false)
                    {
                        return "3|" + pint;
                    }
                    string sqlstr = "update BO_PROCESS set RCVOPRTIME=CONVERT(varchar, getdate(),120)  where  BINDID=" + bind.ToString().Trim();//company1过滤
                    UpdateAWS(sqlstr);

                }
                pint = 3;
                return "1|处理成功";
            }
            catch (Exception ex)
            { return "0|查询出错，靖联系管理员." + ex.Message; }

        }

        //public string D0601_01(string userid,string binid,string packNum,string recdate,string vendorid,string rcvdtlStr)
        public string D0601_01(string userid, string binid, string companyId)
        {
            try
            {
                string awsql = "select  GXWLY, NEXTPROCESSDESC,rcvbpmid from BO_PROCESS where  BINDID=" + binid;//company1过滤
                DataTable dt = GetDataByAWS(awsql);
                if (dt.Rows.Count == 0)
                {
                    return "0|没有符合条件的记录";
                }
                string gxwly = dt.Rows[0][0].ToString().Trim();
                if (gxwly != "物料员-外协收料")
                {
                    return "1|当前工序不为外协工序";
                }
                //string nextProcessDesc = dt.Rows[0][1].ToString().Trim();
                //if (nextProcessDesc.IndexOf("仓") <=0)
                //{
                //    return "1|当前工序为外协工序,但下工序不为仓库";
                //}
                int rcvbpmid = 0;
                int.TryParse(dt.Rows[0]["rcvbpmid"].ToString(), out rcvbpmid);
                if (rcvbpmid == 0)
                {
                    return "1|查询不到采购验收流程id";
                }


                string awsql2 = "select  m.BNO as packnum,  m.SHRQ as recdate, m.GYSDM as vendorid,PONUM,POLINE, 1  as POREL,PARTNUM, " +
                                " d.SHSL as RECQTY, d.SLDW as  PUM, '' as warehousecode,'' as binnum,d.PCH as LOTNUM,JOBNUM, d.ASMSEQ as ASSEMBLYSEQ,d.JOBSEQ, d.POLINECOMM as COMMENTTEXT, d.POTYPE as ORDERTYPE,m.shdh as shdh   from BO_SC_CGYS m " +
                               " left join BO_SC_CGYSSUB d on d.BINDID=m.BINDID " +
                               " where m.BINDID=" + rcvbpmid;//company1过滤
                DataTable dt2 = GetDataByAWS(awsql2);
                string packNum = dt2.Rows[0]["packnum"].ToString().Trim();
                string recdate = dt2.Rows[0]["recdate"].ToString().Trim();
                string vendorid = dt2.Rows[0]["vendorid"].ToString().Trim();
                string rcvdtlStr = DataTableToJson2(dt2);
                string c10 = dt2.Rows[0]["shdh"].ToString().Trim();
                //外协收货
                string resu = porcv(packNum, recdate, vendorid, rcvdtlStr, c10, companyId);//1porcv未用
                if (resu.Substring(0, 1) != "1")
                {
                    return "0|收货失败：" + resu;
                }

                return "1|收货成功";
            }
            catch (Exception ex)
            { return "0|查询出错，靖联系管理员." + ex.Message; }


        }


        public string D802_01(string userid, int binid, string companyId)
        {

            try
            {
                string awsql = "select  packnum, recdate,vendorid,PONUM,POLINE,POREL,PARTNUM,RECQTY,PUM, " +
                               " '' as warehousecode,'' as binnum,LOTNUM,JOBNUM,ASSEMBLYSEQ,JOBSEQ,COMMENTTEXT,ORDERTYPE,SHDH from BO_PARTRECEIVED where BINDID=" + binid;//company1过滤
                DataTable dt = GetDataByAWS(awsql);
                if (dt.Rows.Count == 0)
                {
                    return "0|没有符合条件的记录";
                }
                string packNum = dt.Rows[0]["packnum"].ToString().Trim();
                string recdate = dt.Rows[0]["recdate"].ToString().Trim();
                string vendorid = dt.Rows[0]["vendorid"].ToString().Trim();
                string rcvdtlStr = DataTableToJson2(dt);
                string c10 = dt.Rows[0]["SHDH"].ToString().Trim();
                string resu = porcv(packNum, recdate, vendorid, rcvdtlStr, c10, companyId);//1porcv未用
                if (resu.Substring(0, 1) != "1")
                {
                    return "0|收货失败：" + resu;
                }
                return "1|ok";
            }
            catch (Exception ex)
            { return "0|查询出错，靖联系管理员." + ex.Message; }


        }




        public string D0302_03(string userid, string fromMD, int binid, decimal hqQty, decimal disQty, string disCode, string disDesc)
        {

            try
            {
                string awsql = "select  * from BO_SC_CGYSSUB where BINDID=" + binid;
                DataTable dt = GetDataByAWS(awsql);
                if (dt.Rows.Count == 0) { return "0|没有符合条件的记录！."; }

                awsql = "update BO_SC_CGYSSUB set HGSL=" + hqQty + ",BHGSL=" + disQty + " ,BHGDM='" + disCode + "',bhgms='" + disDesc + "'" +
                        "where BINDID=" + binid;//company过滤
                UpdateAWS(awsql);

                return "1|成功.";
            }

            catch (Exception ex)
            { return "0|查询出错，靖联系管理员." + ex.Message; }

        }




        //upBoRcvDate更新转序流程接收时间
        public string upBoRecDate(string binid, string companyId)
        {

            try
            {


                string awsql = " update BO_PROCESS set RCVOPRDATE=GetDate(),RCVOPRTIME=CONVERT(varchar, getdate(), 120 ) " +
                        "where  BINDID=" + binid;//company1过滤
                UpdateAWS(awsql);

                return "1|成功.";
            }

            catch (Exception ex)
            { return "0|查询出错，靖联系管理员." + ex.Message; }

        }




        ////根据用户返回待办事项列表
        public string D0501(string userid, string fromMD)
        {
            if (userid.Trim() == "") { return "0|用户ID不可为空."; }
            string awsql = "select userid from orguser   where userid='" + userid.Trim() + "'";
            DataTable dt = GetDataByAWS(awsql);
            if (dt.Rows.Count == 0) { return "0|用户ID非法！."; }

            awsql = "select  TARGET as userid ,BIND_ID as bind, id as taskid,WF_STYLE as style,TITLE From WF_TASK where target='" + userid + "' and STATUS=1  order by begintime desc ";
            //awsql = "select top 50  TARGET as userid ,BIND_ID as bind, id as taskid,WF_STYLE as style,TITLE From WF_TASK where target='" + userid + "' and STATUS=1  order by begintime desc ";
            try
            {
                dt = GetDataByAWS(awsql);
                int rcnt = dt.Rows.Count;
                if (rcnt == 0)
                { return "0|无待办事项"; }
                else
                {
                    //增加流程结节点列
                    dt.Columns.Add("node", typeof(string));
                    int spoint = 0, epoint = 0;
                    for (int i = 0; i < rcnt; i++)
                    {
                        spoint = dt.Rows[i]["title"].ToString().Trim().IndexOf('(');
                        epoint = dt.Rows[i]["title"].ToString().Trim().IndexOf(')');
                        if (spoint >= 0 && epoint >= 0)
                        {
                            dt.Rows[i]["node"] = dt.Rows[i]["title"].ToString().Trim().Substring(spoint + 1, epoint - spoint - 1);
                            dt.Rows[i]["title"] = dt.Rows[i]["title"].ToString().Trim().Substring(epoint + 1);

                            if (dt.Rows[i]["node"].ToString().Trim() == "发起转(序")
                            {
                                dt.Rows[i]["node"] = "发起转(序)库";
                                dt.Rows[i]["title"] = dt.Rows[i]["title"].ToString().Trim().Substring(2);
                            }
                        }

                    }

                    string jsonStr = DataTableToJson2(dt);

                    return "1|" + jsonStr;
                }
            }
            catch (Exception ex)
            { return "0|查询出错，靖联系管理员." + ex.Message; }

        }

        ////根据流程id类型节点查询是否存在待办事项
        public string D0503_01(string userid, string bind, string style, string node)
        {
            if (userid.Trim() == "") { return "0|用户ID不可为空."; }
            if (bind.Trim() == "") { return "0|流程id不可为空."; }
            if (style.Trim() == "") { return "0|流程类型不可为空."; }
            if (node.Trim() == "") { return "0|流程节点不可为空."; }


            //string awsql = "select userid from orguser   where userid='" + userid.Trim() + "'";
            //DataTable dt = GetDataByAWS(awsql);
            //if (dt.Rows.Count == 0) { return "0|用户ID非法！."; }

            string awsql = "select   ISNULL(count(id),0) from WF_TASK  where TARGET='" + userid.Trim() + "' and BIND_ID='" + bind.Trim() +
                         "' and WF_STYLE='" + style.Trim() + "' and TITLE like '" + node.Trim() + "%' and STATUS=1";
            try
            {


                DataTable dt = GetDataByAWS(awsql);
                int rcnt = dt.Rows.Count;
                if (rcnt == 0)
                { return "0|查询出错，靖联系管理员."; }
                else
                {
                    int qty = Convert.ToInt32(dt.Rows[0][0]);
                    if (qty == 0)
                    { return "0|无待办事项."; }
                    else
                    { return "1|待办事项条数:" + qty; }

                }

            }
            catch (Exception ex)
            {
                return "0|查询出错，靖联系管理员，" + ex.Message;

            }

        }



        //采购收货接口
        //自动取物料主要仓库作为收货仓库库位，
        //追踪批次测试成功，如果外协物料追踪批次，且输入信息未提供批次号，则把工单号作为批次号
        //是否检验测试成功,po发货完成标志正确测试成功
        //多行收货测试成功  杂项收货测试成功，外协收货测试成功
        //参数c10为自定义的送货单号
        public string porcv(string packNum, string recdate, string vendorid, string rcvdtlStr, string c10, string companyId) //1porcv
        {
            if (packNum.Trim() == "") { return "0|收货单号不可为空"; }
            DateTime recdateD;
            if (DateTime.TryParse(recdate, out recdateD) == false) { return "0|收货日期无效"; }
            if (vendorid.Trim() == "") { return "0|供应商id不可为空"; }
            if (rcvdtlStr.Trim() == "") { return "0|收货明细信息不可为空"; }
            int poNum = 0;
            int poLine = 0;
            DataTable dtRcvDtl = null;

            #region
            try
            {

                dtRcvDtl = JsonConvert.DeserializeObject<DataTable>(rcvdtlStr);
                if (int.TryParse(dtRcvDtl.Rows[0]["ponum"].ToString(), out poNum) == false) { return "0|ponum无效"; }
                if (int.TryParse(dtRcvDtl.Rows[0]["poline"].ToString(), out poLine) == false) { return "0|poline无效"; }

            }
            catch (Exception ex)
            {
                return "0|错误，请检查收货信息参数是否正确。" + ex.Message.ToString();

            }
            #endregion
            //string resultdata = ErpLogin/();
            //if (resultdata != "true")
            //{
            //    return "0|" + resultdata;
            //}
            Session EpicorSession = ErpLoginbak();
            if (EpicorSession == null)
            {
                return "0|erp用户数不够，请稍候再试.ERR:porcv";
            }
            EpicorSession.CompanyID = companyId;
            string plantid = QueryERP("select Plant from erp.POrel where PONum='" + poNum + "' and POLine='" + poLine + "'");
            EpicorSession.PlantID = plantid;

            //WriteGetNewLaborInERPTxt("", EpicorSession.SessionID.ToString(), "", "sessionida", "");
            POImpl poAD = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<POImpl>(EpicorSession, ImplBase<Erp.Contracts.POSvcContract>.UriPath);
            ReceiptImpl recAD = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<ReceiptImpl>(EpicorSession, ImplBase<Erp.Contracts.ReceiptSvcContract>.UriPath);
            LotSelectUpdateImpl lotadapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<LotSelectUpdateImpl>(EpicorSession, ImplBase<Erp.Contracts.LotSelectUpdateSvcContract>.UriPath);
            LotSelectUpdateDataSet lotds = new LotSelectUpdateDataSet();
            #region 
            int vendornumdel = 0;
            try
            {
                //判定pur-stk仓库库位是否有效
                string[] pb2;
                string wh2 = "", bin2 = "", trantype2 = "";
                for (int j = 0; j < dtRcvDtl.Rows.Count; j++)
                {
                    trantype2 = dtRcvDtl.Rows[j]["ordertype"].ToString().Trim().ToLower();
                    wh2 = dtRcvDtl.Rows[j]["warehousecode"].ToString().Trim();
                    bin2 = dtRcvDtl.Rows[j]["binnum"].ToString().Trim();
                    if (trantype2 == "pur-stk")
                    {
                        if (wh2 == "")
                        {
                            pb2 = GetPartWB(dtRcvDtl.Rows[j]["partnum"].ToString().Trim(), companyId, plantid);
                            wh2 = pb2[0].Trim();
                        }
                        if (bin2 == "")
                        {
                            EpicorSession.Dispose();
                            return "0|pur-stk时库位不可为空.";
                        }
                    }
                }
                string[] pb; // = new string[2];
                int u = 1;
                ReceiptDataSet ds = new ReceiptDataSet();
                int vendornum = 0;
                string purPoint = "";
                recAD.GetNewRcvHead(ds, vendornum, purPoint);
                u = 3;
                recAD.GetPOInfo(ds, poNum, false, out vendornum, out purPoint);
                u = 4;
                recAD.GetNewRcvHead(ds, vendornum, purPoint);
                vendornumdel = vendornum;
                u = 5;
                ds.Tables["RcvHead"].Rows[0]["PackSlip"] = packNum;
                ds.Tables["RcvHead"].Rows[0]["Character10"] = c10;
                u = 7;
                ds.Tables["RcvHead"].Rows[0]["ReceiptDate"] = recdate;//dtRcv.Rows[0]["recdate"].ToString();
                u = 8;
                ds.Tables["RcvHead"].Rows[0]["PONum"] = poNum;
                ds.Tables["RcvHead"].Rows[0]["Plant"] = QueryERP("select Plant from erp.POrel where PONum='" + poNum + "' and POLine='" + poLine + "'");
                u = 9;
                recAD.Update(ds);
                u = 10;
                string lotStr = "";
                string lotStr2 = "";
                int poline = 0, porel = 0;
                string jobnum = "", ordertype = "", whcode = "";
                PODataSet poDS;
                PODataSet.PODetailDataTable poDtlDt;
                string chkbinInfo = "";
                for (int i = 0; i < dtRcvDtl.Rows.Count; i++)
                {
                    poNum = Convert.ToInt32(dtRcvDtl.Rows[i]["ponum"]);
                    poline = Convert.ToInt32(dtRcvDtl.Rows[i]["poline"]);
                    porel = Convert.ToInt32(dtRcvDtl.Rows[i]["porel"]);
                    jobnum = dtRcvDtl.Rows[i]["jobnum"].ToString().Trim();
                    ordertype = dtRcvDtl.Rows[i]["ordertype"].ToString().Trim();
                    whcode = dtRcvDtl.Rows[i]["warehousecode"].ToString().Trim();
                    recAD.GetNewRcvDtl(ds, vendornum, purPoint, packNum);

                    u = 11;
                    string outmsg1x = "", outmsg2x = "", outmsg3x = "";
                    recAD.CheckDtlJobStatus(poNum, poline, porel, jobnum, out outmsg1x, out outmsg2x, out outmsg3x);
                    u = 12;
                    ds.Tables["RcvDtl"].Rows[ds.Tables["RcvDtl"].Rows.Count - 1]["PackSlip"] = packNum;
                    ds.Tables["RcvDtl"].Rows[ds.Tables["RcvDtl"].Rows.Count - 1]["PONum"] = poNum;
                    ds.Tables["RcvDtl"].Rows[ds.Tables["RcvDtl"].Rows.Count - 1]["POLine"] = poline;

                    

                    ds.Tables["RcvDtl"].Rows[ds.Tables["RcvDtl"].Rows.Count - 1]["PORelNum"] = porel;


                    u = 13;
                    
                    ds.Tables["RcvDtl"].Rows[ds.Tables["RcvDtl"].Rows.Count - 1]["POType"] = "STD";
                    ds.Tables["RcvDtl"].Rows[ds.Tables["RcvDtl"].Rows.Count - 1]["ReceiptType"] = "P";
                    ds.Tables["RcvDtl"].Rows[ds.Tables["RcvDtl"].Rows.Count - 1]["ReceivedTo"] = ordertype;
                    u = 14;

                    string warnming = "";
                    recAD.GetDtlPOLineInfo(ds, vendornum, purPoint, packNum, 0, poline, out warnming);
                    u = 155;
                    ds.Tables["RcvDtl"].Rows[ds.Tables["RcvDtl"].Rows.Count - 1]["PORelNum"] = porel;
                    // recAD.GetDtlPORelInfo(ds, vendornum, "", packNum, 0, 1);
                    u = 16;
                    recAD.GetDtlQtyInfo(ds, vendornum, purPoint, packNum, 0, Convert.ToDecimal(dtRcvDtl.Rows[i]["recqty"]), "", "QTY", out warnming);
                    u = 17;
                    recAD.GetDtlQtyInfo(ds, vendornum, purPoint, packNum, 0, 0, dtRcvDtl.Rows[i]["pum"].ToString(), "inputIUM", out warnming);
                    u = 18;
                    //green start edit============取物料默认的主仓库作为收货仓库
                    if (ordertype.ToLower() != "pur-ukn")
                    {

                        pb = GetPartWB(dtRcvDtl.Rows[i]["partnum"].ToString(), companyId, plantid);
                        // pb[1] = logic2.GetPartWB(dtRcvDtl.Rows[i]["warehousecode"].ToString().Trim())[1];
                        if (whcode == "") //取物料主要仓库
                        { whcode = pb[0].Trim(); }


                        //  据库位条码信息校验仓库并取库位
                        if (jobnum == "")
                        {
                            chkbinInfo = checkbin(dtRcvDtl.Rows[i]["binnum"].ToString().Trim(), whcode, companyId);
                            if (chkbinInfo.Substring(0, 1) == "1")
                            { bin2 = chkbinInfo.Substring(2); }
                            else
                            {
                                EpicorSession.Dispose();
                                return chkbinInfo;
                            }
                        }
                        ds.Tables["RcvDtl"].Rows[ds.Tables["RcvDtl"].Rows.Count - 1]["WareHouseCode"] = whcode;
                        ds.Tables["RcvDtl"].Rows[ds.Tables["RcvDtl"].Rows.Count - 1]["BinNum"] = bin2;  //库位
                        lotStr = dtRcvDtl.Rows[i]["lotnum"].ToString().Trim();
                        if (pb[2].Trim() == "lot")
                        {
                            lotStr2 = lotStr;
                            if (lotStr.Trim() == "" && (ordertype.ToLower() == "pur-sub" || ordertype.ToLower() == "pur-mtl"))
                            { lotStr = jobnum; }
                            string lotnum = QueryERP("select pl.LotNum from erp.partlot pl left join erp.PODetail pd on pl.Company=pd.Company and pl.PartNum=pd.PartNum where pl.Company='" + companyId + "' and pd.PONUM=" + poNum + " and pd.POLine=" + poline + " and pl.LotNum='" + lotStr + "'");
                            if (!string.IsNullOrEmpty(lotnum))
                            {
                                ds.Tables["RcvDtl"].Rows[ds.Tables["RcvDtl"].Rows.Count - 1]["LotNum"] = lotStr;
                            }
                            else
                            {
                                if (dtRcvDtl.Columns.Contains("HeatNum") && !string.IsNullOrEmpty(dtRcvDtl.Rows[i]["HeatNum"].ToString()))
                                {
                                    string partnum = QueryERP("select PartNum from erp.PODetail where Company='" + companyId + "' and PONUM=" + poNum + " and POLine=" + poline);
                                    lotadapter.GetNewPartLot(lotds, partnum);
                                    lotds.Tables["PartLot"].Rows[i]["LotNum"] = lotStr;
                                    lotds.Tables["PartLot"].Rows[i]["HeatNum"] = dtRcvDtl.Rows[i]["HeatNum"].ToString();
                                    lotadapter.Update(lotds);
                                }
                                ds.Tables["RcvDtl"].Rows[i]["LotNum"] = lotStr;
                            }
                            //ds.Tables["RcvDtl"].Rows[ds.Tables["RcvDtl"].Rows.Count - 1]["LotNum"] = lotStr;
                        }

                        if (jobnum != "") ////收货到工单的，指定默认仓库
                        {
                            ds.Tables["RcvDtl"].Rows[ds.Tables["RcvDtl"].Rows.Count - 1]["WareHouseCode"] = "ins";  //仓库
                            ds.Tables["RcvDtl"].Rows[ds.Tables["RcvDtl"].Rows.Count - 1]["BinNum"] = "ins";  //库位
                        }
                    }
                    else
                    {
                        ds.Tables["RcvDtl"].Rows[ds.Tables["RcvDtl"].Rows.Count - 1]["WareHouseCode"] = "ins";  //仓库
                        ds.Tables["RcvDtl"].Rows[ds.Tables["RcvDtl"].Rows.Count - 1]["BinNum"] = "ins";  //库位
                        ds.Tables["RcvDtl"].Rows[ds.Tables["RcvDtl"].Rows.Count - 1]["LotNum"] = "";
                        lotStr = "";
                    }


                    ds.Tables["RcvDtl"].Rows[ds.Tables["RcvDtl"].Rows.Count - 1]["OurQty"] = Convert.ToDecimal(dtRcvDtl.Rows[i]["recqty"]);
                    ds.Tables["RcvDtl"].Rows[ds.Tables["RcvDtl"].Rows.Count - 1]["PORelNum"] = porel;
                    ds.Tables["RcvDtl"].Rows[ds.Tables["RcvDtl"].Rows.Count - 1]["IUM"] = dtRcvDtl.Rows[i]["pum"].ToString();
                    ds.Tables["RcvDtl"].Rows[ds.Tables["RcvDtl"].Rows.Count - 1]["Received"] = true;
                    u = 19;
                    //if (dtRcvDtl.Rows[i]["lotnum"].ToString().Trim() != "")

                    string errmsg = "", questionmsg = "";
                    if (lotStr != "")
                    {
                        recAD.CheckDtlLotInfo(ds, vendornum, purPoint, packNum, 0, lotStr,
                            out questionmsg, out errmsg);
                    }


                    //green start edit============判定收货是否完成
                    //if (Convert.ToDecimal(ds.Tables["RcvDtl"].Rows[ds.Tables["RcvDtl"].Rows.Count - 1]["VenRemQty"]) <= 0)
                    //{
                    //    ds.Tables["RcvDtl"].Rows[ds.Tables["RcvDtl"].Rows.Count - 1]["ReceivedComplete"] = true;

                    //}
                    //else
                    //{ ds.Tables["RcvDtl"].Rows[ds.Tables["RcvDtl"].Rows.Count - 1]["ReceivedComplete"] = false; }

                    //green end edit



                    recAD.OnChangeDtlReceived(vendornum, purPoint, packNum, 0, true, ds);
                    u = 20;
                    string outMsg1 = "", outMsg2 = "", outMsg3 = "", outMsg4 = "", outMsg5 = "", outMsg6 = "", outMsg7 = "",
                    outMsg8 = "";
                    bool outBool1 = false, outBool2 = false, outBool3 = false;
                    //recAD.UpdateMaster(true, true, vendornum, purPoint, packNum, 0, out outMsg1,
                    //    out outMsg2, out outMsg3, out outMsg4, true, out outMsg5, out outMsg6, out outMsg7, false, out outMsg8,
                    //    false, out outBool1, true, out outBool2, false, dtRcvDtl.Rows[i]["partnum"].ToString(), lotStr2, false, out outBool3, ds);
                    u = 21;

                    recAD.Update(ds);
                }
                EpicorSession.Dispose();
                return "1|处理成功.";
            }
            catch (Exception ex)
            {
                if (EpicorSession == null || !EpicorSession.IsValidSession(EpicorSession.SessionID, "manager"))
                {
                    EpicorSession = CommonClass.Authentication.GetEpicorSession();
                }
                EpicorSession.CompanyID = companyId;
                recAD = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<ReceiptImpl>(EpicorSession, ImplBase<Erp.Contracts.ReceiptSvcContract>.UriPath);
                ReceiptDataSet ds = recAD.GetByID(vendornumdel, "", packNum);
                ds.Tables["RcvHead"].Rows[0].Delete();
                recAD.Update(ds);
                EpicorSession.Dispose();
                return "0|请再次办理." + ex.Message.ToString();
            }
            #endregion

        }


        //[WebMethod]
        public string PORcv(int poNum, string packslip, string shipViaCode, string podetailjson)
        {
            string vendor = QueryERP("select VendorNum from erp.POHeader where PONum='" + poNum + "' and OpenOrder=1");
            if (string.IsNullOrEmpty(vendor))
            {
                return "false|" + poNum.ToString() + "采购订单已关闭或者不存在.";
            }
            int vendornum = Convert.ToInt32(vendor);
            DataTable dtdtl = new DataTable();
            try
            {
                dtdtl = ToDataTable(podetailjson);
                if (dtdtl == null || dtdtl.Rows.Count == 0)
                {
                    return "false|没有数据传入或参数错误.";
                }
            }
            catch (Exception ex)
            {
                return "false|传入参数错误." + ex.Message.ToString();
            }
            //Session EpicorSession = CommonClass.Authentication.GetEpicorSession();
            Session EpicorSession = ErpLoginbak();
            EpicorSession.CompanyID = "002";
            try
            {
                if (EpicorSession == null)
                {
                    return "-1|erp登录异常.";
                }
                ReceiptImpl adapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<ReceiptImpl>(EpicorSession, ImplBase<Erp.Contracts.ReceiptSvcContract>.UriPath);
                ReceiptDataSet ds = new ReceiptDataSet();
                try
                {
                    int outvendorNum = 0; string purPoint = "";
                    adapter.GetNewRcvHead(ds, vendornum, "");
                    adapter.GetPOInfo(ds, poNum, false, out outvendorNum, out purPoint);
                    ds.Tables["RcvHead"].Rows[0]["PackSlip"] = packslip;
                    ds.Tables["RcvHead"].Rows[0]["ShipViaCode"] = shipViaCode;
                    adapter.Update(ds);
                    adapter.GetByIdChkContainerID(vendornum, "", packslip, poNum);
                    string msg = ""; bool bol = false;
                    adapter.UpdateMaster(true, true, 0, "", packslip, 0, out msg, out msg, out msg, out msg, false, out msg, out msg, out msg, out msg, false, out msg, false, out bol, true, out bol, false, "", "", true, out bol, ds);
                    for (int i = 0; i < dtdtl.Rows.Count; i++)
                    {
                        adapter.GetNewRcvDtl(ds, vendornum, "", packslip);
                        adapter.CheckDtlJobStatus(Convert.ToInt32(dtdtl.Rows[i]["PoNum"]), Convert.ToInt32(dtdtl.Rows[i]["PoLine"]), 0, "", out msg, out msg, out msg);
                        adapter.GetDtlPOLineInfo(ds, vendornum, "", packslip, 0, Convert.ToInt32(dtdtl.Rows[i]["PoLine"]), out msg);
                        adapter.GetDtlQtyInfo(ds, vendornum, "", packslip, 0, Convert.ToDecimal(dtdtl.Rows[i]["OurQty"]), "", "QTY", out msg);
                        //ds.Tables["RcvDtl"].Rows[i]["OurQty"] = dtdtl.Rows[i]["OurQty"].ToString();
                        //ds.Tables["RcvDtl"].Rows[i]["InputOurQty"] = dtdtl.Rows[i]["OurQty"].ToString();
                        if (!string.IsNullOrEmpty(dtdtl.Rows[i]["LotNum"].ToString()))
                        {
                            ds.Tables["RcvDtl"].Rows[i]["LotNum"] = dtdtl.Rows[i]["LotNum"].ToString();
                        }
                        adapter.UpdateMaster(true, true, 0, "", packslip, 0, out msg, out msg, out msg, out msg, false, out msg, out msg, out msg, out msg, false, out msg, false, out bol, true, out bol, false, "", "", true, out bol, ds);
                        adapter.CheckOnLeaveHead(vendornum, "", packslip, out msg);
                        ds.Tables["RcvDtl"].Rows[i]["Received"] = true;
                        //adapter.OnChangeDtlReceived(vendornum, "", packslip, i + 1, true, ds);
                        adapter.UpdateMaster(true, true, 0, "", packslip, 0, out msg, out msg, out msg, out msg, false, out msg, out msg, out msg, out msg, false, out msg, false, out bol, true, out bol, false, "", "", true, out bol, ds);
                    }
                }
                catch (Exception ex)
                {
                    ReceiptDataSet delds = adapter.GetByID(vendornum, "", packslip);
                    if (delds.Tables["RcvHead"].Rows.Count > 0)
                    {
                        delds.Tables["RcvHead"].Rows[0].Delete();
                    }
                    if (delds.Tables["RcvDtl"].Rows.Count > 0)
                    {
                        delds.Tables["RcvDtl"].Rows[0].Delete();
                    }
                    adapter.Update(delds);
                    EpicorSession.Dispose();
                    return "false|" + ex.Message.ToString();
                }
            }
            catch (Exception ex)
            {
                if (EpicorSession != null)
                {
                    EpicorSession.Dispose();
                }
                return "false|" + ex.Message.ToString();
            }
            EpicorSession.Dispose();
            return "true";
        }

        public string PORcvbak(int poNum, string packslip, string shipViaCode, string podetailjson)
        {
            //WritePoExInERP("", podetailjson);
            string vendor = QueryERP("select VendorNum from erp.POHeader where PONum='" + poNum + "' and OpenOrder=1");
            if (string.IsNullOrEmpty(vendor))
            {
                return "false|" + poNum.ToString() + "采购订单已关闭或者不存在.";
            }
            int vendornum = Convert.ToInt32(vendor);
            DataTable dtdtl = new DataTable();
            try
            {
                dtdtl = ToDataTable(podetailjson);
                if (dtdtl == null || dtdtl.Rows.Count == 0)
                {
                    return "false|没有数据传入或参数错误.";
                }
            }
            catch (Exception ex)
            {
                return "false|传入参数错误." + ex.Message.ToString();
            }
            Session EpicorSession = CommonClass.Authentication.GetEpicorSession();
            try
            {
                //if (EpicorSessionManager.EpicorSession == null || !EpicorSessionManager.EpicorSession.IsValidSession(EpicorSessionManager.EpicorSession.SessionID, "manager"))
                //{
                //    EpicorSessionManager.EpicorSession = CommonClass.Authentication.GetEpicorSession();
                //}
                if (EpicorSession == null)
                {
                    return "-1|erp登录异常.";
                }
                ReceiptImpl adapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<ReceiptImpl>(EpicorSession, ImplBase<Erp.Contracts.ReceiptSvcContract>.UriPath);
                LotSelectUpdateImpl lotadapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<LotSelectUpdateImpl>(EpicorSession, ImplBase<Erp.Contracts.LotSelectUpdateSvcContract>.UriPath);
                ReceiptDataSet ds = new ReceiptDataSet();
                LotSelectUpdateDataSet lotds = new LotSelectUpdateDataSet();
                try
                {
                    int outvendorNum = 0; string purPoint = "";
                    adapter.GetNewRcvHead(ds, vendornum, "");
                    adapter.GetPOInfo(ds, poNum, false, out outvendorNum, out purPoint);
                    ds.Tables["RcvHead"].Rows[0]["PackSlip"] = packslip;
                    ds.Tables["RcvHead"].Rows[0]["ShipViaCode"] = shipViaCode;
                    adapter.Update(ds);
                    adapter.GetByIdChkContainerID(vendornum, "", packslip, poNum);
                    string msg = ""; bool bol = false;
                    adapter.UpdateMaster(true, true, 0, "", packslip, 0, out msg, out msg, out msg, out msg, false, out msg, out msg, out msg, out msg, false, out msg, false, out bol, true, out bol, false, "", "", true, out bol, ds);
                    for (int i = 0; i < dtdtl.Rows.Count; i++)
                    {
                        adapter.GetNewRcvDtl(ds, vendornum, "", packslip);
                        adapter.CheckDtlJobStatus(Convert.ToInt32(dtdtl.Rows[i]["PoNum"]), Convert.ToInt32(dtdtl.Rows[i]["PoLine"]), 0, "", out msg, out msg, out msg);
                        adapter.GetDtlPOLineInfo(ds, vendornum, "", packslip, 0, Convert.ToInt32(dtdtl.Rows[i]["PoLine"]), out msg);
                        adapter.GetDtlQtyInfo(ds, vendornum, "", packslip, 0, Convert.ToDecimal(dtdtl.Rows[i]["OurQty"]), "", "QTY", out msg);
                        //ds.Tables["RcvDtl"].Rows[i]["OurQty"] = dtdtl.Rows[i]["OurQty"].ToString();
                        //ds.Tables["RcvDtl"].Rows[i]["InputOurQty"] = dtdtl.Rows[i]["OurQty"].ToString();
                        if (!string.IsNullOrEmpty(dtdtl.Rows[i]["LotNum"].ToString()))
                        {
                            string lotnum = QueryERP("select pl.LotNum from erp.partlot pl left join erp.PODetail pd on pl.Company=pd.Company and pl.PartNum=pd.PartNum where pd.PONUM=" + Convert.ToInt32(dtdtl.Rows[i]["PoNum"]) + " and pd.POLine=" + Convert.ToInt32(dtdtl.Rows[i]["PoLine"]) + " and pl.LotNum='" + dtdtl.Rows[i]["LotNum"].ToString() + "'");
                            if (!string.IsNullOrEmpty(lotnum))
                            {
                                ds.Tables["RcvDtl"].Rows[i]["LotNum"] = dtdtl.Rows[i]["LotNum"].ToString();
                            }
                            else
                            {
                                string partnum = QueryERP("select PartNum from erp.PODetail where PONUM=" + Convert.ToInt32(dtdtl.Rows[i]["PoNum"]) + " and POLine=" + Convert.ToInt32(dtdtl.Rows[i]["PoLine"]));
                                lotadapter.GetNewPartLot(lotds, partnum);
                                lotds.Tables["PartLot"].Rows[i]["LotNum"] = dtdtl.Rows[i]["LotNum"].ToString();
                                lotds.Tables["PartLot"].Rows[i]["HeatNum"] = dtdtl.Rows[i]["HeatNum"].ToString();
                                lotadapter.Update(lotds);
                                ds.Tables["RcvDtl"].Rows[i]["LotNum"] = dtdtl.Rows[i]["LotNum"].ToString();
                            }
                        }
                        adapter.UpdateMaster(true, true, 0, "", packslip, 0, out msg, out msg, out msg, out msg, false, out msg, out msg, out msg, out msg, false, out msg, false, out bol, true, out bol, false, "", "", true, out bol, ds);
                        adapter.CheckOnLeaveHead(vendornum, "", packslip, out msg);
                        ds.Tables["RcvDtl"].Rows[i]["Received"] = true;
                        //adapter.OnChangeDtlReceived(vendornum, "", packslip, i + 1, true, ds);
                        adapter.UpdateMaster(true, true, 0, "", packslip, 0, out msg, out msg, out msg, out msg, false, out msg, out msg, out msg, out msg, false, out msg, false, out bol, true, out bol, false, "", "", true, out bol, ds);
                    }
                }
                catch (Exception ex)
                {
                    ReceiptDataSet delds = adapter.GetByID(vendornum, "", packslip);
                    if (delds.Tables["RcvHead"].Rows.Count > 0)
                    {
                        delds.Tables["RcvHead"].Rows[0].Delete();
                    }
                    if (delds.Tables["RcvDtl"].Rows.Count > 0)
                    {
                        delds.Tables["RcvDtl"].Rows[0].Delete();
                    }
                    adapter.Update(delds);
                    //WriteRuKuTxt(ex.Message.ToString(), "收货输入错误.");
                    EpicorSession.Dispose();
                    return "false|" + ex.Message.ToString();
                }
            }
            catch (Exception ex)
            {
                //WriteRuKuTxt(ex.Message.ToString(), "收货输入错误.");
                if (EpicorSession != null)
                {
                    EpicorSession.Dispose();
                }
                return "false|" + ex.Message.ToString();
            }
            EpicorSession.Dispose();
            return "true";
        }


        public string[] GetPartWB(string partnum, string companyId, string plant)
        {
            string[] pp = new string[3];
            pp[0] = ""; pp[1] = ""; pp[2] = "";
            try
            {
                bool tlot = false;
                DataTable partDT = GetDataByERP("select * from erp.Part where Company='" + companyId + "' and PartNum='" + partnum + "'");
                string pw = QueryERP("select PrimWhse from erp.PartPlant where Company='" + companyId + "' and PartNum='" + partnum + "' and Plant='" + plant + "'");
                string wb = QueryERP("select PrimBin from erp.PlantWhse where Company='" + companyId + "' and PartNum='" + partnum + "' and Plant='" + plant + "' and WarehouseCode='" + pw + "'");
                if (partDT.Rows.Count > 0) { tlot = Convert.ToBoolean(partDT.Rows[0]["TrackLots"]); }
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


        public string[] GetPartWB(string partnum, string companyId)
        {
            string[] pp = new string[3];
            pp[0] = ""; pp[1] = ""; pp[2] = "";
            //if (EpicorSessionManager.EpicorSession == null || !EpicorSessionManager.EpicorSession.IsValidSession(EpicorSessionManager.EpicorSession.SessionID, "manager"))
            //{
            //    EpicorSessionManager.EpicorSession = CommonClass.Authentication.GetEpicorSession();
            //}
            //EpicorSessionManager.EpicorSession.CompanyID = companyId;
            //PartImpl partAD = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<PartImpl>(EpicorSessionManager.EpicorSession, ImplBase<Erp.Contracts.PartSvcContract>.UriPath);
            try
            {
                bool tlot = false;
                //PartDataSet ds = new PartDataSet();
                //ds = partAD.GetByID(partnum);
                //PartDataSet.PartDataTable partDT = ds.Part;
                //PartDataSet.PartPlantDataTable plantDT = ds.PartPlant;
                //PartDataSet.PartWhseDataTable pwDT = ds.PartWhse;
                DataTable partDT = GetDataByERP("select * from erp.Part where Company='" + companyId + "' and PartNum='" + partnum + "'");
                DataTable plantDT = GetDataByERP("select * from erp.PartPlant where Company='" + companyId + "' and PartNum='" + partnum + "'");
                DataTable pwDT = GetDataByERP("select * from erp.PartWhse where Company='" + companyId + "' and PartNum='" + partnum + "'");
                string pw = "";
                if (partDT.Rows.Count > 0) { tlot = Convert.ToBoolean(partDT.Rows[0]["TrackLots"]); }
                if (plantDT.Rows.Count > 0)
                { pw = plantDT.Rows[0]["PrimWhse"].ToString().Trim(); }
                DataTable wbDT = GetDataByERP("select BinNum from erp.WhseBin where Company='" + companyId + "' and WarehouseCode='" + pw + "'");
                string wb = "";
                int CNT = 0;
                CNT = pwDT.Rows.Count;
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


        //根据bpm角色取用户id
        public string getBpmRole_Id(string roleName)
        {
            try
            {
                string userids = "";
                string awsql = "SELECT ID FROM ORGROLE WHERE ROLENAME = '" + roleName.Trim() + "'";

                DataTable dt = GetDataByAWS(awsql);
                int rcnt = dt.Rows.Count;
                if (rcnt == 0)
                { return ""; }

                int roleid = 0;
                int.TryParse(dt.Rows[0][0].ToString(), out roleid);
                if (roleid == 0) { return ""; }

                awsql = "select userid from orguser where id in " +
                        " (select id from orguser where roleid=" + roleid +
                        " union all " +
                        " (select mapid as id from orgusermap where roleid =" + roleid + "))";
                DataTable dt2 = GetDataByAWS(awsql);
                int rcnt2 = dt2.Rows.Count;
                if (rcnt2 == 0)
                { return ""; }

                for (int i = 0; i < rcnt2; i++)
                {
                    userids = userids + dt2.Rows[i][0].ToString().Trim() + " ";
                }


                return userids.Trim();
            }
            catch
            {
                return "";
            }
        }



        //发起转（序）库接口
        public string D0303_01(string userid, string bind, string taskid, string style, string node, string nextuser, string title, string packNum, string recdate, string vendorid, string rcvdtlStr, string zxstr, int psetp, string c10, string companyId)
        {

            DataTable dtRcvDtl = null;
            string jobnum;
            int asmSeq, oprseq, rcnt, i;

            try
            {

                dtRcvDtl = JsonConvert.DeserializeObject<DataTable>(rcvdtlStr);
                rcnt = dtRcvDtl.Rows.Count;

            }
            catch (Exception ex)
            {
                return "0|错误，请检查收货信息参数是否正确。" + ex.Message.ToString();

            }

            int pint = 0;  //任务处理的结果步数
            string ss = "";

            try
            {
                string resu = "";
                string podes = "";
                i = 0;
                // for (i = 0; i < rcnt; i++)  暂只支持单条收货
                // {
                podes = dtRcvDtl.Rows[i]["podes"].ToString().Trim().ToLower();


                switch (podes)
                {
                    case "w": //仓库
                              //结束发起转序节点，发起扫描收料节点
                        pint = 0;
                        resu = D0502_02(userid, bind, taskid, style, node, nextuser, title);
                        if (resu.Substring(0, 1) != "1")
                        { return resu; }
                        else
                        { return "1|" + gtaskid; }
                        break;

                    case "r": //申购物料 
                        pint = 0;
                        if (psetp < 1)
                        {
                            //结束发起转序节点，发起扫描收料节点
                            pint = 0;
                            resu = D0502_02(userid, bind, taskid, style, node, nextuser, title);
                            if (resu.Substring(0, 1) != "1")
                            { return "3|" + pint + resu.Substring(1); }

                        }
                        pint = 1;
                        if (psetp < 2)
                        {

                            //resu = D0502_01(userid, bind, gtaskid, style, node, userid, title);
                            //if (resu.Substring(0, 1) != "1")
                            //{ return resu; }



                            //结束整个流程
                            if (closeWF(userid, bind, gtaskid) == false)
                                return "3|" + pint + resu.Substring(1); ;



                        }
                        pint = 2;
                        Thread.Sleep(2000);

                        if (psetp < 3)
                        {

                            //发起直接采购物料流程
                            ss = "";
                            nextuser = ""; //下步办理人为空，自动取bpm中设定的人员
                            resu = pur_mtl(userid, nextuser, packNum, recdate, vendorid, rcvdtlStr, c10, companyId);
                            if (resu.Substring(0, 1) != "1")
                            { return "3|" + pint + resu.Substring(1); ; }
                            else
                            { ss = resu; }

                            return "2|" + ss;
                        }



                        break;

                    case "t": //本工序是采购到工单物料


                        //调用收货to工序
                        //resu = porcv(packNum, recdate, vendorid, rcvdtlStr);
                        //if (resu.Substring(0, 1) != "1")
                        //{ return "0|" + resu; }


                        if (psetp < 1)
                        {
                            pint = 0;
                            //结束发起转序节点，发起扫描收料节点
                            resu = D0502_02(userid, bind, taskid, style, node, nextuser, title);
                            if (resu.Substring(0, 1) != "1")
                            { return "3|" + pint + resu.Substring(1); }


                        }

                        pint = 1;
                        Thread.Sleep(2000);

                        if (psetp < 2)
                        {

                            //resu = D0502_01(userid, bind, gtaskid, style, node, userid, title);
                            //if (resu.Substring(0, 1) != "1")
                            //{ return resu; }

                            //结束整个流程
                            if (closeWF(userid, bind, gtaskid) == false)
                                return "3|" + pint + resu.Substring(1); ;



                        }
                        pint = 2;
                        Thread.Sleep(2000);

                        if (psetp < 3)
                        {

                            //发起直接采购物料流程
                            ss = "";
                            resu = pur_mtl(userid, nextuser, packNum, recdate, vendorid, rcvdtlStr, c10, companyId);
                            if (resu.Substring(0, 1) != "1")
                            { return "3|" + pint + resu.Substring(1); ; }
                            else
                            { ss = resu; }

                            return "2|" + ss;
                        }
                        break;

                    case "s1": //下工序还是外协工序,供应商相同

                        if (psetp < 1)
                        {
                            //调用收货to工序
                            pint = 0;
                            resu = porcv(packNum, recdate, vendorid, rcvdtlStr, c10, companyId);//1porcv
                            if (resu.Substring(0, 1) != "1")
                            { return resu; }

                        }
                        pint = 1;
                        if (psetp < 2)
                        {
                            //结束发起转序节点，发起扫描收料节点
                            resu = D0502_02(userid, bind, taskid, style, node, nextuser, title);
                            if (resu.Substring(0, 1) != "1")
                            { return "3|" + pint + resu.Substring(1); }

                        }
                        pint = 2;
                        //结束扫描收料节点
                        //resu = D0502_01(userid, bind, gtaskid, style, node, userid, title);
                        //if (resu.Substring(0, 1) != "1")
                        //{ return  resu; }

                        if (psetp < 3)
                        {
                            //结束采购验收流程
                            if (closeWF(userid, bind, gtaskid) == false)
                                return "3|" + pint + resu.Substring(1);


                        }
                        pint = 3;
                        if (psetp < 4)
                        {
                            //发起转序申请流程to检验分析节点
                            resu = stZXBPM(zxstr, companyId);
                            if (resu.Substring(0, 1) != "1")
                            { return "3|" + pint + resu.Substring(1); }


                        }
                        pint = 4;
                        if (psetp < 5)
                        {
                            //结束转序申请流程-检验分析节点，发起转序节点
                            resu = D0502_01(userid, gbinid, gtaskid, "转序申请流程组", node, nextuser, gtitle);
                            if (resu.Substring(0, 1) != "1")
                            { return "3|" + pint + resu.Substring(1); }


                        }
                        pint = 5;
                        if (psetp < 6)
                        {
                            //结束转序申请-发起转序节点,到达转序接收节点
                            resu = D0502_01(userid, gbinid, gtaskid, "转序申请流程组", node, gnextuser, gtitle);
                            if (resu.Substring(0, 1) != "1")
                            { return "3|" + pint + resu.Substring(1); ; }


                        }
                        pint = 6;

                        if (psetp < 7)
                        {
                            //更新接收时间
                            upBoRecDate(gbinid, companyId);
                            //结束转序申请流程
                            if (closeWF(userid, gbinid, gtaskid) == false)
                                return "3|" + pint;


                            pint = 7;
                            return "2|ok";
                        }


                        break;



                    case "s2": //下工序还是外协工序,供应商bu相同

                        if (psetp < 1)
                        {
                            //调用收货to工序
                            pint = 0;
                            //resu = porcv(packNum, recdate, vendorid, rcvdtlStr,c10);
                            //if (resu.Substring(0, 1) != "1")
                            //{ return resu; }

                        }
                        pint = 1;
                        if (psetp < 2)
                        {
                            //结束发起转序节点，发起扫描收料节点
                            resu = D0502_02(userid, bind, taskid, style, node, nextuser, title);
                            if (resu.Substring(0, 1) != "1")
                            { return "3|" + pint + resu.Substring(1); }

                        }
                        pint = 2;
                        //结束扫描收料节点
                        //resu = D0502_01(userid, bind, gtaskid, style, node, userid, title);
                        //if (resu.Substring(0, 1) != "1")
                        //{ return  resu; }

                        if (psetp < 3)
                        {
                            //结束整个流程
                            if (closeWF(userid, bind, gtaskid) == false)
                                return "3|" + pint + resu.Substring(1);


                        }
                        pint = 3;

                        if (psetp < 4)
                        {

                            //发起直接采购物料流程
                            ss = "";
                            resu = pur_mtl(userid, nextuser, packNum, recdate, vendorid, rcvdtlStr, c10, companyId);
                            if (resu.Substring(0, 1) != "1")
                            { return "3|" + pint + resu.Substring(1); ; }
                            else
                            { ss = resu; }


                        }
                        pint = 4;
                        if (psetp < 5)
                        {
                            //发起转序申请流程to检验分析节点
                            resu = stZXBPM(zxstr, companyId);
                            if (resu.Substring(0, 1) != "1")
                            { return "3|" + pint + resu.Substring(1); }


                        }
                        pint = 5;
                        if (psetp < 6)
                        {
                            //结束转序申请流程-检验分析节点，发起转序节点
                            resu = D0502_01(userid, gbinid, gtaskid, "转序申请流程组", node, nextuser, gtitle);
                            if (resu.Substring(0, 1) != "1")
                            { return "3|" + pint + resu.Substring(1); }


                        }
                        pint = 6;
                        if (psetp < 7)
                        {
                            //结束转序申请-发起转序节点,到达转序接收节点
                            resu = D0502_01(userid, gbinid, gtaskid, "转序申请流程组", node, gnextuser, gtitle);
                            if (resu.Substring(0, 1) != "1")
                            { return "3|" + pint + resu.Substring(1); ; }


                        }
                        pint = 7;

                        if (psetp < 8)
                        {
                            //更新接收时间
                            upBoRecDate(gbinid, companyId);
                            //结束转序申请流程
                            if (closeWF(userid, gbinid, gtaskid) == false)
                                return "3|" + pint;


                        }
                        pint = 8;
                        return "2|" + ss;

                        break;



                    case "p": //下工序没有了，需要收货到仓库

                        if (psetp < 1)
                        {

                            //调用收货to工序
                            pint = 0;
                            //resu = porcv(packNum, recdate, vendorid, rcvdtlStr);
                            //if (resu.Substring(0, 1) != "1")
                            //{ return resu; }

                        }
                        pint = 1;
                        if (psetp < 2)
                        {



                        }
                        pint = 2;
                        if (psetp < 3)
                        {
                            //if (closeWF(userid, bind, gtaskid) == false)
                            //    return "3|" + pint;
                            //pint = 3;
                        }

                        if (psetp < 4)
                        {
                            //发起转序申请流程to检验分析节点
                            resu = stZXBPM(zxstr, companyId);
                            if (resu.Substring(0, 1) != "1")
                            { return "3|" + pint + resu.Substring(1); }


                        }
                        pint = 4;
                        if (psetp < 5)
                        {
                            //结束转序申请流程-检验分析节点，发起转序节点
                            resu = D0502_01(userid, gbinid, gtaskid, "转序申请流程组", node, nextuser, gtitle);
                            if (resu.Substring(0, 1) != "1")
                            { return "3|" + pint + resu.Substring(1); }


                        }
                        pint = 5;
                        if (psetp < 6)
                        {
                            //结束转序申请-发起转序节点,到达转序接收节点
                            resu = D0502_01(userid, gbinid, gtaskid, "转序申请流程组", node, gnextuser, gtitle);
                            if (resu.Substring(0, 1) != "1")
                            { return "3|" + pint + resu.Substring(1); ; }


                        }
                        pint = 6;

                        if (psetp < 7)
                        {
                            //更新接收时间
                            upBoRecDate(gbinid, companyId);
                            //结束转序申请流程
                            if (closeWF(userid, gbinid, gtaskid) == false)
                                return "3|" + pint;


                        }
                        pint = 7;
                        if (psetp < 8)
                        {
                            //结束发起转序节点，发起扫描收料节点
                            resu = D0502_02(userid, bind, taskid, style, node, nextuser, title);
                            if (resu.Substring(0, 1) != "1")
                            { return "3|" + pint + resu.Substring(1); }
                            pint = 8;

                            return "1|" + gtaskid;
                        }




                        break;
                    case "m":  //下工序为自制工序

                        if (psetp < 1)
                        {
                            //调用收货to工序
                            pint = 0;

                            //发起转序申请流程to检验分析节点
                            resu = stZXBPM(zxstr, companyId);
                            if (resu.Substring(0, 1) != "1")
                            { return "3|" + pint + resu.Substring(1); }

                        }
                        pint = 1;
                        if (psetp < 1)
                        {
                            //结束转序申请流程-检验分析节点，发起转序节点
                            resu = D0502_01(userid, gbinid, gtaskid, "转序申请流程组", node, nextuser, gtitle);
                            if (resu.Substring(0, 1) != "1")
                            { return "3|" + pint + resu.Substring(1); }

                        }
                        pint = 2;

                        if (psetp < 3)
                        {
                            //结束转序申请-发起转序节点,到达转序接收节点
                            resu = D0502_01(userid, gbinid, gtaskid, "转序申请流程组", node, nextuser, gtitle);
                            if (resu.Substring(0, 1) != "1")
                            { return "3|" + pint + resu.Substring(1); }

                        }
                        pint = 3;
                        if (psetp < 4)
                        {
                            //采购验收流程 结束发起转序节点，发起扫描收料节点
                            resu = D0502_02(userid, bind, taskid, style, node, nextuser, title);
                            if (resu.Substring(0, 1) != "1")
                            { return "3|" + pint + resu.Substring(1); }


                        }
                        pint = 4;
                        if (psetp < 5)
                        {
                            // 采购验收流程 结束整个流程
                            if (closeWF(userid, bind, gtaskid) == false)
                            { return "3|" + pint; }

                            return "2|" + gtaskid;
                        }

                        break;
                    default:
                        return "0|PO收货去向类型出错，无法继续执行";


                }


                //}



                return resu;
            }
            catch (Exception ex)
            {
                if (pint == 0)
                {
                    return "1|" + ex.Message.ToString();
                }
                else
                {
                    return "3|" + pint + "1" + ex.Message.ToString();
                }

            }


        }






        ////发起直接采购物料流程
        public string pur_mtl(string userid, string nextuser, string packNum, string recdate, string vendorid, string rcvdtlStr, string c10, string companyId)
        {
            int poNum = 0;
            try
            {
                DataTable dtRcvDtl = JsonConvert.DeserializeObject<DataTable>(rcvdtlStr);
                if (int.TryParse(dtRcvDtl.Rows[0]["ponum"].ToString(), out poNum) == false)
                {
                    return "0|ponum无效";
                }


                int poline = Convert.ToInt32(dtRcvDtl.Rows[0]["poline"]);
                int porel = Convert.ToInt32(dtRcvDtl.Rows[0]["porel"]);




                string jobnum = dtRcvDtl.Rows[0]["jobnum"].ToString().Trim();
                //if (jobnum == "") 
                //{
                //    c1 = null;
                //    s1.Dispose();
                //    ConnectionPool.Dispose();
                //    return "0|工单号不可为空"; 
                //}
                string ordertype = dtRcvDtl.Rows[0]["ordertype"].ToString().Trim().ToLower();
                if (ordertype == "pur-ukn")
                {

                    //杂项采购取物料接收人，且不可为空
                    string reqRole = getReqRole(poNum, poline, porel, companyId);
                    if (reqRole == "")
                    {
                        return "0|杂项物料采购的接收人不可为空";
                    }
                    nextuser = getBpmRole_Id(reqRole);
                    if (nextuser == "")
                    {
                        return "0|杂项物料采购的接收人对应的bpm角色" + reqRole + "中没有用户。";
                    }




                }
                decimal recqty = 0;
                int assemblyseq = 0, jobseq = 0;
                decimal.TryParse(dtRcvDtl.Rows[0]["recqty"].ToString(), out recqty);
                int.TryParse(dtRcvDtl.Rows[0]["assemblyseq"].ToString(), out assemblyseq);
                int.TryParse(dtRcvDtl.Rows[0]["jobseq"].ToString(), out jobseq);


                //qdDS.QueryWhereItem.DefaultView.RowFilter = "DataTableID='porel' and FieldName='ponum'";
                //qdDS.QueryWhereItem.DefaultView[0]["RValue"] = poNum;

                //qdDS.QueryWhereItem.DefaultView.RowFilter = "DataTableID='porel' and FieldName='poline'";
                //qdDS.QueryWhereItem.DefaultView[0]["RValue"] = poline;

                //qdDS.QueryWhereItem.DefaultView.RowFilter = "DataTableID='porel' and FieldName='porelnum'";
                //qdDS.QueryWhereItem.DefaultView[0]["RValue"] = porel;

                List<QueryWhereItemRow> whereItems = new List<QueryWhereItemRow>();
                whereItems.Add(new QueryWhereItemRow() { TableID = "porel", FieldName = "ponum", RValue = poNum.ToString() });
                whereItems.Add(new QueryWhereItemRow() { TableID = "porel", FieldName = "poline", RValue = poline.ToString() });
                whereItems.Add(new QueryWhereItemRow() { TableID = "porel", FieldName = "porelnum", RValue = porel.ToString() });
                //DataTable dt = BaqResult("001-purMmlInfo", whereItems, 0, companyId);
                string sql = "select [PORel].[PONum] as [PORel_PONum],[PORel].[POLine] as [PORel_POLine],[PORel].[PORelNum] as [PORel_PORelNum],[PODetail].[PartNum] as [PODetail_PartNum],[PODetail].[LineDesc] as [PODetail_LineDesc],[POHeader].[VendorNum] as [POHeader_VendorNum],[Vendor].[Name] as [Vendor_Name],[OpMaster].[OpDesc] as [OpMaster_OpDesc],[JobOper].[OprSeq] as [JobOper_OprSeq] from Erp.PORel as PORel inner join Erp.PODetail as PODetail on PORel.Company = PODetail.Company and PORel.PONUM = PODetail.PONum and PORel.POLine = PODetail.POLine inner join Erp.POHeader as POHeader on PODetail.Company = POHeader.Company and PODetail.PONum = POHeader.PONUM inner join Erp.Vendor as Vendor on POHeader.Company = Vendor.Company and POHeader.VendorNum = Vendor.VendorNum left outer join Erp.JobMtl as JobMtl on PORel.Company = JobMtl.Company and PORel.JobNum = JobMtl.JobNum and PORel.AssemblySeq = JobMtl.AssemblySeq and PORel.JobSeq = JobMtl.MtlSeq left outer join Erp.JobOper as JobOper on JobMtl.Company = JobOper.Company and JobMtl.JobNum = JobOper.JobNum and JobMtl.AssemblySeq = JobOper.AssemblySeq and JobMtl.RelatedOperation = JobOper.OprSeq left outer join Erp.OpMaster as OpMaster on JobOper.Company = OpMaster.Company and JobOper.OpCode = OpMaster.OpCode where (PORel.Company = '" + companyId + "'  and PORel.PONum ='" + poNum.ToString() + "'  and PORel.POLine ='" + poline.ToString() + "'  and PORel.PORelNum ='" + porel.ToString() + "')";
                DataTable dt = GetDataByERP(sql);
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    dt.Columns[i].ColumnName = dt.Columns[i].ColumnName.Replace('_', '.');
                }
                //DataTable dt = dynamicQuery.ExecuteDashBoardQuery(qdDS).Tables[0];
                string partdesc = "", vendorName = "", opdesc = "";
                int oprseq = 0;
                if (dt.Rows.Count > 0)
                {
                    partdesc = dt.Rows[0]["PODetail.LineDesc"].ToString();
                    vendorName = dt.Rows[0]["Vendor.Name"].ToString();
                    opdesc = dt.Rows[0]["OpMaster.OpDesc"].ToString();
                    int.TryParse(dt.Rows[0]["JobOper.OprSeq"].ToString(), out oprseq);
                    //
                }



                BPMInterface bpm = new BPMInterface(bpmSer, cgysStra, cgysBo);
                string title = "采购物料接收" + jobnum + "-" + packNum;

                gtitle = title;

                //DataTable dthed = new DataTable("BO_ZJCGWLJS");
                DataTable dthed = new DataTable("BO_PARTRECEIVED");
                dthed.Columns.Add("PONUM", Type.GetType("System.Int32"));
                dthed.Columns.Add("POLINE", Type.GetType("System.Int32"));
                dthed.Columns.Add("POREL", Type.GetType("System.Int32"));
                dthed.Columns.Add("PARTNUM", Type.GetType("System.String"));
                dthed.Columns.Add("PARTDESC", Type.GetType("System.String"));
                dthed.Columns.Add("RECQTY", Type.GetType("System.Decimal"));
                dthed.Columns.Add("PUM", Type.GetType("System.String"));
                dthed.Columns.Add("LOTNUM", Type.GetType("System.String"));
                dthed.Columns.Add("JOBNUM", Type.GetType("System.String"));
                dthed.Columns.Add("ASSEMBLYSEQ", Type.GetType("System.Int32"));
                dthed.Columns.Add("JOBSEQ", Type.GetType("System.Int32"));
                dthed.Columns.Add("COMMENTTEXT", Type.GetType("System.String"));
                dthed.Columns.Add("ORDERTYPE", Type.GetType("System.String"));
                dthed.Columns.Add("NODE1USER", Type.GetType("System.String"));
                dthed.Columns.Add("NODE2USER", Type.GetType("System.String"));
                dthed.Columns.Add("OPRSEQ", Type.GetType("System.Int32"));
                dthed.Columns.Add("OPRDESC", Type.GetType("System.String"));
                dthed.Columns.Add("VENDORID", Type.GetType("System.String"));
                dthed.Columns.Add("VENDORNAME", Type.GetType("System.String"));
                dthed.Columns.Add("RECDATE", Type.GetType("System.String"));
                dthed.Columns.Add("PACKNUM", Type.GetType("System.String"));
                dthed.Columns.Add("SHDH", Type.GetType("System.String"));

                DataRow dr = dthed.NewRow();
                dr["PONUM"] = poNum;
                dr["POLINE"] = poline;
                dr["POREL"] = porel;
                dr["PARTNUM"] = dtRcvDtl.Rows[0]["partnum"];
                dr["PARTDESC"] = partdesc;
                dr["RECQTY"] = recqty;
                dr["PUM"] = dtRcvDtl.Rows[0]["pum"]; ;
                dr["LOTNUM"] = dtRcvDtl.Rows[0]["lotnum"]; ;
                dr["JOBNUM"] = jobnum;
                dr["ASSEMBLYSEQ"] = assemblyseq;
                dr["JOBSEQ"] = jobseq;
                dr["COMMENTTEXT"] = dtRcvDtl.Rows[0]["commenttext"]; ;
                dr["ORDERTYPE"] = ordertype;
                dr["NODE1USER"] = userid;
                dr["NODE2USER"] = nextuser;
                dr["OPRSEQ"] = oprseq;
                dr["OPRDESC"] = opdesc;
                dr["VENDORID"] = vendorid;
                dr["VENDORNAME"] = vendorName;
                dr["PACKNUM"] = packNum;
                dr["RECDATE"] = recdate;
                dr["SHDH"] = c10;
                dthed.Rows.Add(dr);

                DataTable dtMtl = null;


                string resu = bpm.StartFlow(userid, title, zcUUID.Trim(), dthed, dtMtl, nextuser);




                return "1|" + resu;
            }
            catch (Exception ex)
            {

                return "0|" + ex.Message.ToString();
            }

        }

        //D0304-01采购收货并结束节点接口 //采购入库
        public string D0304_01(string userid, string bind, string taskid, string style, string node, string nextuser, string title, string packNum, string recdate, string vendorid, string rcvdtlStr, int psetp, string c10, string companyId)
        {

            DataTable dtRcvDtl = null;
            string jobnum, podes;
            int asmSeq, oprseq, rcnt, i;
            decimal jobQty = 0;

            try
            {

                dtRcvDtl = JsonConvert.DeserializeObject<DataTable>(rcvdtlStr);
                rcnt = dtRcvDtl.Rows.Count;

            }
            catch (Exception ex)
            {
                return "0|错误，请检查收货信息参数是否正确。" + ex.Message.ToString();

            }


            try
            {
                // for (i = 0; i < rcnt; i++)
                //{
                i = 0;
                int pint = 0;
                string resu = "";
                jobnum = dtRcvDtl.Rows[i]["jobnum"].ToString().Trim();
                podes = dtRcvDtl.Rows[i]["podes"].ToString().Trim().ToLower();
                if (int.TryParse(dtRcvDtl.Rows[i]["assemblyseq"].ToString(), out asmSeq) == false)
                { return "0|半成品号无效"; }
                if (int.TryParse(dtRcvDtl.Rows[i]["jobseq"].ToString(), out oprseq) == false)
                { return "0|工序号无效"; }

                if (psetp < 1)
                {

                    //调用收货入库
                    resu = porcv(packNum, recdate, vendorid, rcvdtlStr, c10, companyId);//1porcv
                    if (resu.Substring(0, 1) != "1")
                    { return "3|" + pint + resu.Substring(1); }
                }
                pint = 1;


                //结束扫描收料节点
                //resu = D0502_01(userid, bind, taskid, style, node, userid, title);
                //if (resu.Substring(0, 1) != "1")
                //{ return  resu; }


                if (psetp < 2)
                {
                    //工单收货至库存
                    if (podes == "p")
                    {
                        decimal.TryParse(dtRcvDtl.Rows[i]["Recqty"].ToString(), out jobQty);
                        if (jobQty == 0) return "0| 收货数量不可为0";
                        string lotnum = dtRcvDtl.Rows[i]["lotnum"].ToString();
                        string wh = GetPartWB(dtRcvDtl.Rows[i]["partnum"].ToString(), companyId)[0]; //取物料默认的主仓库
                        if (wh == "") return "0| 物料默认的主仓库不可为空";
                        string bin = dtRcvDtl.Rows[i]["Binnum"].ToString();
                        if (bin == "") return "0| 库位不可为空";
                        resu = D0506_01(userid, jobnum, asmSeq, jobQty, lotnum, wh, bin, companyId);
                        if (resu.Substring(0, 1) != "1")
                        { return "3|" + pint + resu.Substring(1); }
                    }

                }
                pint = 2;

                if (psetp < 3)
                {
                    //结束整个流程
                    //Thread.Sleep(2000);
                    if (closeWF(userid, bind, taskid) == false)
                        return "3|" + pint;

                }
                pint = 3;
                return "1|处理成功";
            }
            catch (Exception ex)
            {
                return "0|处理失败." + ex.Message.ToString();
            }

        }


        //D0506-01工单收货至库存
        public string D0506_01(string rqr, string JobNum, int asmSeq, decimal jobQty, string lotnum, string wh, string bin, string companyId)
        { //JobNum as string ,jobQty as decimal,partNum as string
            try
            {
                //QueryDesignDataSet qdDS = dynamicQuery.GetDashBoardQuery("001-joboprqty");
                //List<QueryWhereItemRow> whereItems = new List<QueryWhereItemRow>();
                //whereItems.Add(new QueryWhereItemRow() { TableID = "joboper", FieldName = "jobnum", RValue = JobNum });
                //whereItems.Add(new QueryWhereItemRow() { TableID = "joboper", FieldName = "AssemblySeq", RValue = asmSeq.ToString() });
                //DataTable dt = BaqResult("001-joboprqty", whereItems, 0, companyId);
                DataTable dt = GetDataByERP("select [JobOper].[JobNum] as [JobOper_JobNum],[JobOper].[AssemblySeq] as [JobOper_AssemblySeq],[JobOper].[OprSeq] as [JobOper_OprSeq],[JobOper].[RunQty] as [JobOper_RunQty],[JobOper].[QtyCompleted] as [JobOper_QtyCompleted],[JobAsmbl].[PartNum] as [JobAsmbl_PartNum],[JobAsmbl].[RequiredQty] as [JobAsmbl_RequiredQty],[JobPart].[ReceivedQty] as [JobPart_ReceivedQty],[JobHead].[JobComplete] as [JobHead_JobComplete],[JobHead].[JobReleased] as [JobHead_JobReleased] from Erp.JobOper as JobOper left outer join Erp.JobAsmbl as JobAsmbl on JobOper.Company = JobAsmbl.Company and JobOper.JobNum = JobAsmbl.JobNum and JobOper.AssemblySeq = JobAsmbl.AssemblySeq left outer join Erp.JobPart as JobPart on JobAsmbl.Company = JobPart.Company and JobAsmbl.JobNum = JobPart.JobNum and JobAsmbl.PartNum = JobPart.PartNum left outer join Erp.JobHead as JobHead on JobOper.Company = JobHead.Company and JobOper.JobNum = JobHead.JobNum where(JobOper.Company = '" + companyId + "'  and JobOper.JobNum = '" + JobNum + "'  and JobOper.AssemblySeq ='" + asmSeq.ToString() + "') order by JobOper.OprSeq Desc");
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    dt.Columns[i].ColumnName = dt.Columns[i].ColumnName.Replace('_', '.');
                }
                string partNum = "";
                decimal recdQty = 0, compQty = 0, requQty = 0;
                bool jobRes = false, jobCom = true;
                if (dt.Rows.Count > 0)
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
                //if ((recdQty + jobQty) > compQty)
                //{

                //    return "0|以前收货数量" + recdQty + "+ 本次收货数量" + jobQty + ",>完成数量" + compQty + "，不能收货。";
                //}

                //if ((recdQty + jobQty) > requQty)
                //{

                //    return "0|以前收货数量" + recdQty + "+ 本次收货数量" + jobQty + ",>生产数量" + requQty + "，不能收货。";
                //}

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
                try
                {

                }
                catch
                { }
                //string resultdata = ErpLogin/();
                //if (resultdata != "true")
                //{
                //    return "0|" + resultdata;
                //}
                Session EpicorSession = ErpLoginbak();
                if (EpicorSession == null)
                {
                    return "0|erp用户数不够，请稍候再试.错误代码：D0506_01";
                }
                EpicorSession.CompanyID = companyId;
                //WriteGetNewLaborInERPTxt("", EpicorSession.SessionID.ToString(), "", "sessionidb", "");
                ReceiptsFromMfgImpl recAD = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<ReceiptsFromMfgImpl>(EpicorSession, ImplBase<Erp.Contracts.ReceiptsFromMfgSvcContract>.UriPath);
                ReceiptsFromMfgDataSet recDs = new ReceiptsFromMfgDataSet();
                string pcTranType = "MFG-STK";

                int piAssemblySeq = 0;
                recAD.GetNewReceiptsFromMfgJobAsm(JobNum, piAssemblySeq, pcTranType, Guid.NewGuid().ToString(), recDs);
                //recAD.ReceiptsFromMfgData.Tables["PartTran"].Rows[0]["PartNum"] = partNum;
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

        public string MFGSTK(string rqr, string JobNum, int asmSeq, decimal jobQty, string partNum, string lotnum, string wh, string bin, string companyId)
        {
            try
            {
                //DataTable dt = GetDataByERP("select [JobOper].[JobNum] as [JobOper_JobNum],[JobOper].[AssemblySeq] as [JobOper_AssemblySeq],[JobOper].[OprSeq] as [JobOper_OprSeq],[JobOper].[RunQty] as [JobOper_RunQty],[JobOper].[QtyCompleted] as [JobOper_QtyCompleted],[JobAsmbl].[PartNum] as [JobAsmbl_PartNum],[JobAsmbl].[RequiredQty] as [JobAsmbl_RequiredQty],[JobPart].[ReceivedQty] as [JobPart_ReceivedQty],[JobHead].[JobComplete] as [JobHead_JobComplete],[JobHead].[JobReleased] as [JobHead_JobReleased] from Erp.JobOper as JobOper left outer join Erp.JobAsmbl as JobAsmbl on JobOper.Company = JobAsmbl.Company and JobOper.JobNum = JobAsmbl.JobNum and JobOper.AssemblySeq = JobAsmbl.AssemblySeq left outer join Erp.JobPart as JobPart on JobAsmbl.Company = JobPart.Company and JobAsmbl.JobNum = JobPart.JobNum and JobAsmbl.PartNum = JobPart.PartNum left outer join Erp.JobHead as JobHead on JobOper.Company = JobHead.Company and JobOper.JobNum = JobHead.JobNum where(JobOper.Company = '" + companyId + "'  and JobOper.JobNum = '" + JobNum + "'  and JobOper.AssemblySeq ='" + asmSeq.ToString() + "') order by JobOper.OprSeq Desc");
                //for (int i = 0; i < dt.Columns.Count; i++)
                //{
                //    dt.Columns[i].ColumnName = dt.Columns[i].ColumnName.Replace('_', '.');
                //}
                //string partNum = "";
                //decimal recdQty = 0, compQty = 0, requQty = 0;
                //bool jobRes = false, jobCom = true;
                //if (dt.Rows.Count > 0)
                //{
                //    partNum = dt.Rows[0]["JobAsmbl.PartNum"].ToString();
                //    decimal.TryParse(dt.Rows[0]["JobPart.ReceivedQty"].ToString().Trim(), out recdQty);
                //    decimal.TryParse(dt.Rows[0]["JobAsmbl.RequiredQty"].ToString().Trim(), out requQty);
                //    decimal.TryParse(dt.Rows[0]["JobOper.QtyCompleted"].ToString().Trim(), out compQty);
                //    bool.TryParse(dt.Rows[0]["JobHead.JobComplete"].ToString().Trim(), out jobCom);
                //    bool.TryParse(dt.Rows[0]["JobHead.JobReleased"].ToString().Trim(), out jobRes);
                //}
                //if (jobCom)
                //{

                //    return "0|工单已关闭，不能收货。";
                //}
                //if (jobRes == false)
                //{

                //    return "0|工单未发放，不能收货。";
                //}
                //if ((recdQty + jobQty) > compQty)
                //{

                //    return "0|以前收货数量" + recdQty + "+ 本次收货数量" + jobQty + ",>完成数量" + compQty + "，不能收货。";
                //}

                //if ((recdQty + jobQty) > requQty)
                //{

                //    return "0|以前收货数量" + recdQty + "+ 本次收货数量" + jobQty + ",>生产数量" + requQty + "，不能收货。";
                //}

                //string[] w = GetPartWB(partNum, companyId);
                //string tlot = w[2].ToString().Trim().ToLower();

                //if (wh == "")
                //{ wh = w[0]; }
                ////取物料默认的主仓库}

                ////  据库位条码信息校验仓库并取库位
                //string bin2 = "";
                //string chkbinInfo = checkbin(bin, wh, companyId);
                //if (chkbinInfo.Substring(0, 1) == "1")
                //{ bin2 = chkbinInfo.Substring(2); }
                //else
                //{

                //    return chkbinInfo;
                //}
                Session EpicorSession = ErpLoginbak();
                if (EpicorSession == null)
                {
                    return "0|erp用户数不够，请稍候再试. 错误代码：MFGSTK";
                }
                EpicorSession.CompanyID = companyId;
                //(string rqr, string JobNum, int asmSeq, decimal jobQty,string partNum, string lotnum, string wh, string bin, string companyId)
                WriteGetNewLaborInERPTxt("", EpicorSession.SessionID.ToString(), rqr.ToString() + "|" + JobNum.ToString() + "|" + asmSeq.ToString() + "|" + jobQty.ToString() + "|" + partNum.ToString() + "|" + lotnum.ToString() + "|" + wh.ToString() + "|" + bin.ToString(), "MFGSTK", companyId);
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
                recDs.Tables[0].Rows[0]["BinNum"] = bin;
                if (!string.IsNullOrEmpty(lotnum))
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

        //checkbin校验仓库库位 区域与仓库是否匹配,并返回库位
        public string checkbin(string binbar, string wh, string companyId)
        {
            if (binbar.Trim() == "") return "0|库位信息不可为空";
            if (wh.Trim() == "") return "0|仓库id不可为空";
            int sp = 0;
            sp = binbar.IndexOf('-');
            if (sp <= 0) return "0|库存信息不正确";
            string zonid = binbar.Substring(0, sp);
            string binid = binbar.Substring(sp + 1);
            if (zonid == "") return "0|库存信息不正确，包括的区域为空";
            if (binid == "") return "0|库存信息不正确，包括的库位为空";
            try
            {
                //List<QueryWhereItemRow> whereItems = new List<QueryWhereItemRow>();
                //whereItems.Add(new QueryWhereItemRow() { TableID = "WhseBin", FieldName = "ZoneID", RValue = zonid });
                //whereItems.Add(new QueryWhereItemRow() { TableID = "WhseBin", FieldName = "BinNum", RValue = binid.ToString() });
                DataTable dt = GetDataByERP("select [WhseBin].[WarehouseCode] as [WhseBin_WarehouseCode],[WhseBin].[ZoneID] as [WhseBin_ZoneID],[WhseBin].[BinNum] as [WhseBin_BinNum] from Erp.WhseBin as WhseBin where (WhseBin.Company = '" + companyId + "'  and WhseBin.ZoneID = '" + zonid + "'  and WhseBin.BinNum = '" + binid + "' and WhseBin.WarehouseCode='" + wh + "')");
                //DataTable dt = BaqResult("001-wzbin", whereItems, 0, companyId);
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    dt.Columns[i].ColumnName = dt.Columns[i].ColumnName.Replace('_', '.');
                }
                string retuWh;

                if (dt.Rows.Count > 0)
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

        public string LaborMtlStkbak(string empid, string JobNum, int asmSeq, int oprSeq, decimal LQty, decimal disQty, string disCode, string bjr, string zxDate, string zxTime, string lotnum, string wh, string bin, string companyId)
        {



                try
                {
                    DataTable dt = GetDataByERP(@"select [JobOper].[JobNum] as [JobOper_JobNum],
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
                        empid = dt.Rows[0]["OpMaster.Character05"].ToString().Trim();
                    }
                    if (jobCom)
                    {
                        return "0|工单已关闭，不能报工。";
                    }
                    if (jobRes == false)
                    {
                        return "0|工单未发放，不能报工。";
                    }
                    if ((compQty + LQty + disQty) > runQty)
                    {
                        return "0|以前报工数量" + compQty + "+ 本次报工数量" + (LQty + disQty) + ",>工序数量" + runQty + "，不能报工。";
                    }
                    if (empid.Trim() == "") { empid = "DB"; }
                    //try
                    //{
                    //    EpicorSessionManager.DisposeSession();
                    //}
                    //catch
                    //{ }
                    //string resultdata = ErpLogin/();
                    //if (resultdata != "true")
                    //{
                    //    return "0|" + resultdata;
                    //}
                    Session EpicorSession = ErpLoginbak();
                    if (EpicorSession == null)
                    {
                        return "-1|erp用户数不够，请稍候再试.错误代码：LaborMtlStkbak";
                    }
                    EpicorSession.CompanyID = companyId;
                    //WriteGetNewLaborInERPTxt("", EpicorSession.SessionID.ToString(), "", "sessionidf", "");
                    LaborImpl labAd = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<LaborImpl>(EpicorSession, ImplBase<Erp.Contracts.LaborSvcContract>.UriPath);
                    ReceiptsFromMfgImpl recAD = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<ReceiptsFromMfgImpl>(EpicorSession, ImplBase<Erp.Contracts.ReceiptsFromMfgSvcContract>.UriPath);
                    LaborDataSet dsLabHed = new LaborDataSet();
                    LaborDataSet.LaborHedDataTable dtLabHed = new LaborDataSet.LaborHedDataTable();
                    LaborDataSet.LaborDtlDataTable dtLabDtl = new LaborDataSet.LaborDtlDataTable();
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
                    outTime = System.DateTime.Now.Hour + System.DateTime.Now.Minute / 100;
                    labAd.GetNewLaborDtlWithHdr(dsLabHed, System.DateTime.Today, 0, System.DateTime.Today, outTime, labHedSeq);
                    labAd.DefaultLaborType(dsLabHed, "P");
                    labAd.DefaultJobNum(dsLabHed, JobNum);
                    labAd.DefaultAssemblySeq(dsLabHed, asmSeq);
                    string msg;
                    labAd.DefaultOprSeq(dsLabHed, oprSeq, out msg);
                    dtLabDtl = dsLabHed.LaborDtl;
                    labAd.DefaultLaborQty(dsLabHed, LQty, out msg);
                    //dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["LaborQty"] = LQty;
                    disQty = 0;  //先不回写不合格数量
                    disCode = "";
                    dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["DiscrepQty"] = disQty;
                    dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["DiscrpRsnCode"] = disCode;
                    dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["TimeStatus"] = "A";
                    dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["Date01"] = System.DateTime.Today;
                    dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["ShortChar01"] = System.DateTime.Now.ToString("hh:mm:ss");
                    //dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["OpComplete"] = "1";
                    //dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["Complete"] = "1";
                    string cMessageText = "";
                    labAd.CheckWarnings(dsLabHed, out cMessageText);
                    labAd.Update(dsLabHed);
                    //adapter.ValidateChargeRateForTimeType(ds, out oumsg);
                    labAd.SubmitForApproval(dsLabHed, false, out cMessageText);
                    int laborDtlSeq = Convert.ToInt32(dsLabHed.Tables["LaborDtl"].Rows[dsLabHed.Tables["LaborDtl"].Rows.Count - 1]["LaborDtlSeq"]);

                    //入库
                    DataTable mtldt = GetDataByERP("select [JobOper].[JobNum] as [JobOper_JobNum],[JobOper].[AssemblySeq] as [JobOper_AssemblySeq],[JobOper].[OprSeq] as [JobOper_OprSeq],[JobOper].[RunQty] as [JobOper_RunQty],[JobOper].[QtyCompleted] as [JobOper_QtyCompleted],[JobAsmbl].[PartNum] as [JobAsmbl_PartNum],[JobAsmbl].[RequiredQty] as [JobAsmbl_RequiredQty],[JobPart].[ReceivedQty] as [JobPart_ReceivedQty],[JobHead].[JobComplete] as [JobHead_JobComplete],[JobHead].[JobReleased] as [JobHead_JobReleased] from Erp.JobOper as JobOper left outer join Erp.JobAsmbl as JobAsmbl on JobOper.Company = JobAsmbl.Company and JobOper.JobNum = JobAsmbl.JobNum and JobOper.AssemblySeq = JobAsmbl.AssemblySeq left outer join Erp.JobPart as JobPart on JobAsmbl.Company = JobPart.Company and JobAsmbl.JobNum = JobPart.JobNum and JobAsmbl.PartNum = JobPart.PartNum left outer join Erp.JobHead as JobHead on JobOper.Company = JobHead.Company and JobOper.JobNum = JobHead.JobNum where(JobOper.Company = '" + companyId + "'  and JobOper.JobNum = '" + JobNum + "'  and JobOper.AssemblySeq ='" + asmSeq.ToString() + "') order by JobOper.OprSeq Desc");
                    for (int i = 0; i < mtldt.Columns.Count; i++)
                    {
                        mtldt.Columns[i].ColumnName = mtldt.Columns[i].ColumnName.Replace('_', '.');
                    }
                    string partNum = "";
                    decimal recdQty = 0, mtlcompQty = 0, requQty = 0;
                    bool mtljobRes = false, mtljobCom = true;
                    if (mtldt.Rows.Count > 0)
                    {
                        partNum = mtldt.Rows[0]["JobAsmbl.PartNum"].ToString();
                        decimal.TryParse(mtldt.Rows[0]["JobPart.ReceivedQty"].ToString().Trim(), out recdQty);
                        decimal.TryParse(mtldt.Rows[0]["JobAsmbl.RequiredQty"].ToString().Trim(), out requQty);
                        decimal.TryParse(mtldt.Rows[0]["JobOper.QtyCompleted"].ToString().Trim(), out mtlcompQty);
                        bool.TryParse(mtldt.Rows[0]["JobHead.JobComplete"].ToString().Trim(), out mtljobCom);
                        bool.TryParse(mtldt.Rows[0]["JobHead.JobReleased"].ToString().Trim(), out mtljobRes);
                    }
                    if (mtljobCom)
                    {
                        string strmessage = "";
                        labAd.ReviewIsDocumentLock(labHedSeq.ToString(), laborDtlSeq.ToString(), out strmessage);
                        labAd.RecallFromApproval(dsLabHed, false, out strmessage);
                        labAd.CheckNonConformance(dt.Rows[0]["JobNum"].ToString(), labHedSeq, laborDtlSeq, out strmessage);
                        dsLabHed.Tables["LaborDtl"].Rows[dsLabHed.Tables["LaborDtl"].Rows.Count - 1]["TimeStatus"] = "E";
                        labAd.Update(dsLabHed);
                        dsLabHed.Tables["LaborDtl"].Rows[dsLabHed.Tables["LaborDtl"].Rows.Count - 1].Delete();
                        dsLabHed.Tables["LaborHed"].Rows[dsLabHed.Tables["LaborHed"].Rows.Count - 1].Delete();
                        labAd.Update(dsLabHed);
                        try
                        {
                            EpicorSession.Dispose();
                        }
                        catch
                        { }
                        return "0|工单已关闭，不能收货。";
                    }
                    if (mtljobRes == false)
                    {
                        string strmessage = "";
                        labAd.ReviewIsDocumentLock(labHedSeq.ToString(), laborDtlSeq.ToString(), out strmessage);
                        labAd.RecallFromApproval(dsLabHed, false, out strmessage);
                        labAd.CheckNonConformance(dt.Rows[0]["JobNum"].ToString(), labHedSeq, laborDtlSeq, out strmessage);
                        dsLabHed.Tables["LaborDtl"].Rows[dsLabHed.Tables["LaborDtl"].Rows.Count - 1]["TimeStatus"] = "E";
                        labAd.Update(dsLabHed);
                        dsLabHed.Tables["LaborDtl"].Rows[dsLabHed.Tables["LaborDtl"].Rows.Count - 1].Delete();
                        dsLabHed.Tables["LaborHed"].Rows[dsLabHed.Tables["LaborHed"].Rows.Count - 1].Delete();
                        labAd.Update(dsLabHed);
                        try
                        {
                            EpicorSession.Dispose();
                        }
                        catch
                        { }
                        return "0|工单未发放，不能收货。";
                    }
                    if ((recdQty + LQty) > mtlcompQty)
                    {
                        string strmessage = "";
                        labAd.ReviewIsDocumentLock(labHedSeq.ToString(), laborDtlSeq.ToString(), out strmessage);
                        labAd.RecallFromApproval(dsLabHed, false, out strmessage);
                        labAd.CheckNonConformance(dt.Rows[0]["JobNum"].ToString(), labHedSeq, laborDtlSeq, out strmessage);
                        dsLabHed.Tables["LaborDtl"].Rows[dsLabHed.Tables["LaborDtl"].Rows.Count - 1]["TimeStatus"] = "E";
                        labAd.Update(dsLabHed);
                        dsLabHed.Tables["LaborDtl"].Rows[dsLabHed.Tables["LaborDtl"].Rows.Count - 1].Delete();
                        dsLabHed.Tables["LaborHed"].Rows[dsLabHed.Tables["LaborHed"].Rows.Count - 1].Delete();
                        labAd.Update(dsLabHed);
                        try
                        {
                            EpicorSession.Dispose();
                        }
                        catch
                        { }
                        return "0|以前收货数量" + recdQty + "+ 本次收货数量" + LQty + ",>完成数量" + mtlcompQty + "，不能收货。";
                    }
                    if ((recdQty + LQty) > requQty)
                    {
                        string strmessage = "";
                        labAd.ReviewIsDocumentLock(labHedSeq.ToString(), laborDtlSeq.ToString(), out strmessage);
                        labAd.RecallFromApproval(dsLabHed, false, out strmessage);
                        labAd.CheckNonConformance(dt.Rows[0]["JobNum"].ToString(), labHedSeq, laborDtlSeq, out strmessage);
                        dsLabHed.Tables["LaborDtl"].Rows[dsLabHed.Tables["LaborDtl"].Rows.Count - 1]["TimeStatus"] = "E";
                        labAd.Update(dsLabHed);
                        dsLabHed.Tables["LaborDtl"].Rows[dsLabHed.Tables["LaborDtl"].Rows.Count - 1].Delete();
                        dsLabHed.Tables["LaborHed"].Rows[dsLabHed.Tables["LaborHed"].Rows.Count - 1].Delete();
                        labAd.Update(dsLabHed);
                        try
                        {
                            EpicorSession.Dispose();
                        }
                        catch
                        { }
                        //UDReqQty_c
                        return "0|以前收货数量" + recdQty + "+ 本次收货数量" + LQty + ",>生产数量" + requQty + "，不能收货。";
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
                        string strmessage = "";
                        labAd.ReviewIsDocumentLock(labHedSeq.ToString(), laborDtlSeq.ToString(), out strmessage);
                        labAd.RecallFromApproval(dsLabHed, false, out strmessage);
                        labAd.CheckNonConformance(dt.Rows[0]["JobNum"].ToString(), labHedSeq, laborDtlSeq, out strmessage);
                        dsLabHed.Tables["LaborDtl"].Rows[dsLabHed.Tables["LaborDtl"].Rows.Count - 1]["TimeStatus"] = "E";
                        labAd.Update(dsLabHed);
                        dsLabHed.Tables["LaborDtl"].Rows[dsLabHed.Tables["LaborDtl"].Rows.Count - 1].Delete();
                        dsLabHed.Tables["LaborHed"].Rows[dsLabHed.Tables["LaborHed"].Rows.Count - 1].Delete();
                        labAd.Update(dsLabHed);
                        try
                        {
                            EpicorSession.Dispose();
                        }
                        catch
                        { }
                        return chkbinInfo;
                    }
                    try
                    {

                        ReceiptsFromMfgDataSet recDs = new ReceiptsFromMfgDataSet();
                        string pcTranType = "MFG-STK";
                        int piAssemblySeq = 0;
                        recAD.GetNewReceiptsFromMfgJobAsm(JobNum, piAssemblySeq, pcTranType, Guid.NewGuid().ToString(), recDs);
                        //recAD.ReceiptsFromMfgData.Tables["PartTran"].Rows[0]["PartNum"] = partNum;
                        recDs.Tables["PartTran"].Rows[0]["PartNum"] = partNum;
                        string opMessage = "";
                        recAD.OnChangePartNum(recDs, partNum, out opMessage, false);
                        string pcMessage;
                        recDs.Tables[0].Rows[0]["ActTranQty"] = LQty;
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
                    }
                    catch (Exception err)
                    {
                        string strmessage = "";
                        labAd.ReviewIsDocumentLock(labHedSeq.ToString(), laborDtlSeq.ToString(), out strmessage);
                        labAd.RecallFromApproval(dsLabHed, false, out strmessage);
                        labAd.CheckNonConformance(dt.Rows[0]["JobNum"].ToString(), labHedSeq, laborDtlSeq, out strmessage);
                        dsLabHed.Tables["LaborDtl"].Rows[dsLabHed.Tables["LaborDtl"].Rows.Count - 1]["TimeStatus"] = "E";
                        labAd.Update(dsLabHed);
                        dsLabHed.Tables["LaborDtl"].Rows[dsLabHed.Tables["LaborDtl"].Rows.Count - 1].Delete();
                        dsLabHed.Tables["LaborHed"].Rows[dsLabHed.Tables["LaborHed"].Rows.Count - 1].Delete();
                        labAd.Update(dsLabHed);
                        try
                        {
                            EpicorSession.Dispose();
                        }
                        catch
                        { }
                        return "0|" + err.Message.ToString();
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

        public string LaborMtlStk(string empid, string JobNum, int asmSeq, int oprSeq, decimal LQty, decimal disQty, string disCode, string bjr, string zxDate, string zxTime, string lotnum, string wh, string bin, string companyId, string taskID)
        {
            try
            {
                string index = QueryOrUpdateHS("select CurrentSteps from PutStorage where TaskID='" + taskID + "'");
                ReceiptsFromMfgImpl recAD;
                Session EpicorSession;
                if (Convert.ToInt32(index) == 0)
                {
                    //DataTable dt = GetDataByERP("select [JobOper].[JobNum] as [JobOper_JobNum],[JobOper].[AssemblySeq] as [JobOper_AssemblySeq],[JobOper].[OprSeq] as [JobOper_OprSeq],[JobOper].[RunQty] as [JobOper_RunQty],[JobOper].[QtyCompleted] as [JobOper_QtyCompleted],[JobHead].[JobComplete] as [JobHead_JobComplete],[JobHead].[JobReleased] as [JobHead_JobReleased],[OpMaster].[Character05] as [OpMaster_Character05] from Erp.JobOper as JobOper left outer join Erp.JobHead as JobHead on JobOper.Company = JobHead.Company and JobOper.JobNum = JobHead.JobNum inner join OpMaster as OpMaster on JobOper.Company = OpMaster.Company and JobOper.OpCode = OpMaster.OpCode where (JobOper.Company = '" + companyId + "'  and JobOper.JobNum = '" + JobNum + "'  and JobOper.AssemblySeq = '" + asmSeq.ToString() + "'  and JobOper.OprSeq ='" + oprSeq.ToString() + "') order by JobOper.OprSeq Desc");
                    DataTable dt = GetDataByERP(@"select [JobOper].[JobNum] as [JobOper_JobNum],
[JobOper].[AssemblySeq] as [JobOper_AssemblySeq],
[JobOper].[OprSeq] as [JobOper_OprSeq],
[JobOper].[RunQty] as [JobOper_RunQty],
[JobOper].[QtyCompleted] as [JobOper_QtyCompleted],
[JobHead].[JobComplete] as [JobHead_JobComplete],
[JobHead].[JobReleased] as [JobHead_JobReleased],[OpMaster].[Character05] as [OpMaster_Character05] ,
 [JobAsmbl].[AssemblySeq] as [JobAsmbl_AssemblySeq],[JobHead].[UDReqQty_c] as [JobHead_UDReqQty_c],[JobAsmbl].[SurplusQty_c] as [JobAsmbl_SurplusQty_c] 
from Erp.JobOper as JobOper left outer join JobHead as JobHead on JobOper.Company = JobHead.Company and JobOper.JobNum = JobHead.JobNum 
inner join JobAsmbl as JobAsmbl on 
	JobOper.Company = JobAsmbl.Company
	and JobOper.JobNum = JobAsmbl.JobNum
	and JobOper.AssemblySeq = JobAsmbl.AssemblySeq 
inner join OpMaster as OpMaster on JobOper.Company = OpMaster.Company and JobOper.OpCode = OpMaster.OpCode where (JobOper.Company = '" + companyId + "'  and JobOper.JobNum = '" + JobNum + "'  and JobOper.AssemblySeq = '" + asmSeq.ToString() + "'  and JobOper.OprSeq ='" + oprSeq.ToString() + "') order by JobOper.OprSeq Desc");

                    //for (int i = 0; i < dt.Columns.Count; i++)
                    //{
                    //    dt.Columns[i].ColumnName = dt.Columns[i].ColumnName.Replace('_', '.');
                    //}

                    decimal compQty = 0, runQty = 0;
                    bool jobRes = false, jobCom = true;
                    if (dt.Rows.Count > 0)
                    {
                        if (companyId == "001")
                        {
                            if (int.Parse(dt.Rows[0]["JobOper_AssemblySeq"].ToString()) ==0)
                            {
                                decimal.TryParse(dt.Rows[0]["JobHead_UDReqQty_c"].ToString().Trim(), out runQty);
                            }
                            else
                            {
                                decimal.TryParse(dt.Rows[0]["JobAsmbl_SurplusQty_c"].ToString().Trim(), out runQty);
                            }

                        }
                        if(companyId=="002")
                        {
                            decimal.TryParse(dt.Rows[0]["JobOper_RunQty"].ToString().Trim(), out runQty);
                        }
                        
                        //decimal.TryParse(dt.Rows[0]["JobOper.RunQty"].ToString().Trim(), out runQty);
                        decimal.TryParse(dt.Rows[0]["JobOper_QtyCompleted"].ToString().Trim(), out compQty);
                        bool.TryParse(dt.Rows[0]["JobHead_JobComplete"].ToString().Trim(), out jobCom);
                        bool.TryParse(dt.Rows[0]["JobHead_JobReleased"].ToString().Trim(), out jobRes);
                        empid = dt.Rows[0]["OpMaster_Character05"].ToString().Trim();
                    }
                    if (jobCom)
                    {
                        return "0|工单已关闭，不能报工。";
                    }
                    if (jobRes == false)
                    {
                        return "0|工单未发放，不能报工。";
                    }
                    if ((compQty + LQty + disQty) > runQty)
                    {
                        return "0|以前报工数量" + compQty + "+ 本次报工数量" + (LQty + disQty) + ",>工序数量" + runQty + "，不能报工。";
                    }
                    if (empid.Trim() == "") { empid = "DB"; }
                    EpicorSession = ErpLoginbak();
                    if (EpicorSession == null)
                    {
                        return "0|erp用户数不够，请稍候再试. 错误代码：LaborMtlStk 1";
                    }
                    EpicorSession.CompanyID = companyId;
                    //WriteGetNewLaborInERPTxt("", EpicorSession.SessionID.ToString(), "", "sessionidg", "");
                    LaborImpl labAd = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<LaborImpl>(EpicorSession, ImplBase<Erp.Contracts.LaborSvcContract>.UriPath);
                    recAD = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<ReceiptsFromMfgImpl>(EpicorSession, ImplBase<Erp.Contracts.ReceiptsFromMfgSvcContract>.UriPath);
                    LaborDataSet dsLabHed = new LaborDataSet();
                    LaborDataSet.LaborHedDataTable dtLabHed = new LaborDataSet.LaborHedDataTable();
                    LaborDataSet.LaborDtlDataTable dtLabDtl = new LaborDataSet.LaborDtlDataTable();
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
                    outTime = System.DateTime.Now.Hour + System.DateTime.Now.Minute / 100;
                    labAd.GetNewLaborDtlWithHdr(dsLabHed, System.DateTime.Today, 0, System.DateTime.Today, outTime, labHedSeq);
                    labAd.DefaultLaborType(dsLabHed, "P");
                    labAd.DefaultJobNum(dsLabHed, JobNum);
                    labAd.DefaultAssemblySeq(dsLabHed, asmSeq);
                    string msg;
                    labAd.DefaultOprSeq(dsLabHed, oprSeq, out msg);
                    dtLabDtl = dsLabHed.LaborDtl;
                    labAd.DefaultLaborQty(dsLabHed, LQty, out msg);
                    //dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["LaborQty"] = LQty;
                    disQty = 0;  //先不回写不合格数量
                    disCode = "";
                    dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["DiscrepQty"] = disQty;
                    dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["DiscrpRsnCode"] = disCode;
                    dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["TimeStatus"] = "A";
                    dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["Date01"] = System.DateTime.Today;
                    dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["ShortChar01"] = System.DateTime.Now.ToString("hh:mm:ss");
                    //dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["OpComplete"] = "1";
                    //dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["Complete"] = "1";
                    string cMessageText = "";
                    labAd.CheckWarnings(dsLabHed, out cMessageText);
                    labAd.Update(dsLabHed);
                    QueryOrUpdateHS("update PutStorage set  SearchString='3' where TaskID='" + taskID + "'");
                    //adapter.ValidateChargeRateForTimeType(ds, out oumsg);
                    try
                    {
                        labAd.SubmitForApproval(dsLabHed, false, out cMessageText);
                    }
                    catch (Exception exerror)
                    {
                        QueryOrUpdateHS("update PutStorage set  CurrentSteps='1' where TaskID='" + taskID + "'");
                        QueryOrUpdateHS("update PutStorage set  SearchString='6' where TaskID='" + taskID + "'");
                        EpicorSession.Dispose();
                        return "0|" + exerror.Message.ToString();
                    }

                    //int laborDtlSeq = Convert.ToInt32(dsLabHed.Tables["LaborDtl"].Rows[dsLabHed.Tables["LaborDtl"].Rows.Count - 1]["LaborDtlSeq"]);
                    QueryOrUpdateHS("update PutStorage set  CurrentSteps='1' where TaskID='" + taskID + "'");
                }
                else
                {
                    //string resultdata = ErpLogin/();
                    //if (resultdata != "true")
                    //{
                    //    return "0|" + resultdata;
                    //}
                    EpicorSession = ErpLoginbak();
                    if (EpicorSession == null)
                    {
                        return "0|erp用户数不够，请稍候再试. 错误代码：LaborMtlStk 2 ";
                    }
                    EpicorSession.CompanyID = companyId;
                    //WriteGetNewLaborInERPTxt("", EpicorSession.SessionID.ToString(), "", "sessionidj", "");
                    recAD = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<ReceiptsFromMfgImpl>(EpicorSession, ImplBase<Erp.Contracts.ReceiptsFromMfgSvcContract>.UriPath);
                }
                //入库
                DataTable mtldt = GetDataByERP("select [JobOper].[JobNum] as [JobOper_JobNum],[JobOper].[AssemblySeq] as [JobOper_AssemblySeq],[JobOper].[OprSeq] as [JobOper_OprSeq],[JobOper].[RunQty] as [JobOper_RunQty],[JobOper].[QtyCompleted] as [JobOper_QtyCompleted],[JobAsmbl].[PartNum] as [JobAsmbl_PartNum],[JobAsmbl].[RequiredQty] as [JobAsmbl_RequiredQty],[JobPart].[ReceivedQty] as [JobPart_ReceivedQty],[JobHead].[JobComplete] as [JobHead_JobComplete],[JobHead].[JobReleased] as [JobHead_JobReleased] from Erp.JobOper as JobOper left outer join Erp.JobAsmbl as JobAsmbl on JobOper.Company = JobAsmbl.Company and JobOper.JobNum = JobAsmbl.JobNum and JobOper.AssemblySeq = JobAsmbl.AssemblySeq left outer join Erp.JobPart as JobPart on JobAsmbl.Company = JobPart.Company and JobAsmbl.JobNum = JobPart.JobNum and JobAsmbl.PartNum = JobPart.PartNum left outer join Erp.JobHead as JobHead on JobOper.Company = JobHead.Company and JobOper.JobNum = JobHead.JobNum where(JobOper.Company = '" + companyId + "'  and JobOper.JobNum = '" + JobNum + "'  and JobOper.AssemblySeq ='" + asmSeq.ToString() + "') order by JobOper.OprSeq Desc");
                for (int i = 0; i < mtldt.Columns.Count; i++)
                {
                    mtldt.Columns[i].ColumnName = mtldt.Columns[i].ColumnName.Replace('_', '.');
                }
                string partNum = "";
                decimal recdQty = 0, mtlcompQty = 0, requQty = 0;
                bool mtljobRes = false, mtljobCom = true;
                if (mtldt.Rows.Count > 0)
                {
                    partNum = mtldt.Rows[0]["JobAsmbl.PartNum"].ToString();
                    decimal.TryParse(mtldt.Rows[0]["JobPart.ReceivedQty"].ToString().Trim(), out recdQty);
                    decimal.TryParse(mtldt.Rows[0]["JobAsmbl.RequiredQty"].ToString().Trim(), out requQty);
                    decimal.TryParse(mtldt.Rows[0]["JobOper.QtyCompleted"].ToString().Trim(), out mtlcompQty);
                    bool.TryParse(mtldt.Rows[0]["JobHead.JobComplete"].ToString().Trim(), out mtljobCom);
                    bool.TryParse(mtldt.Rows[0]["JobHead.JobReleased"].ToString().Trim(), out mtljobRes);
                }
                if (mtljobCom)
                {
                    //string strmessage = "";
                    //labAd.ReviewIsDocumentLock(labHedSeq.ToString(), laborDtlSeq.ToString(), out strmessage);
                    //labAd.RecallFromApproval(dsLabHed, false, out strmessage);
                    //labAd.CheckNonConformance(dt.Rows[0]["JobNum"].ToString(), labHedSeq, laborDtlSeq, out strmessage);
                    //dsLabHed.Tables["LaborDtl"].Rows[dsLabHed.Tables["LaborDtl"].Rows.Count - 1]["TimeStatus"] = "E";
                    //labAd.Update(dsLabHed);
                    //dsLabHed.Tables["LaborDtl"].Rows[dsLabHed.Tables["LaborDtl"].Rows.Count - 1].Delete();
                    //dsLabHed.Tables["LaborHed"].Rows[dsLabHed.Tables["LaborHed"].Rows.Count - 1].Delete();
                    //labAd.Update(dsLabHed);
                    EpicorSession.Dispose();
                    return "0|工单已关闭，不能收货。";
                }
                if (mtljobRes == false)
                {
                    //string strmessage = "";
                    //labAd.ReviewIsDocumentLock(labHedSeq.ToString(), laborDtlSeq.ToString(), out strmessage);
                    //labAd.RecallFromApproval(dsLabHed, false, out strmessage);
                    //labAd.CheckNonConformance(dt.Rows[0]["JobNum"].ToString(), labHedSeq, laborDtlSeq, out strmessage);
                    //dsLabHed.Tables["LaborDtl"].Rows[dsLabHed.Tables["LaborDtl"].Rows.Count - 1]["TimeStatus"] = "E";
                    //labAd.Update(dsLabHed);
                    //dsLabHed.Tables["LaborDtl"].Rows[dsLabHed.Tables["LaborDtl"].Rows.Count - 1].Delete();
                    //dsLabHed.Tables["LaborHed"].Rows[dsLabHed.Tables["LaborHed"].Rows.Count - 1].Delete();
                    //labAd.Update(dsLabHed);
                    EpicorSession.Dispose();
                    return "0|工单未发放，不能收货。";
                }
                //if ((recdQty + LQty) > mtlcompQty)
                //{
                //    EpicorSession.Dispose();
                //    return "0|以前收货数量" + recdQty + "+ 本次收货数量" + LQty + ",>完成数量" + mtlcompQty + "，不能收货。";
                //}
                //if ((recdQty + LQty) > requQty)
                //{
                //    EpicorSession.Dispose();
                //    return "0|以前收货数量" + recdQty + "+ 本次收货数量" + LQty + ",>生产数量" + requQty + "，不能收货。";
                //}
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
                    //string strmessage = "";
                    //labAd.ReviewIsDocumentLock(labHedSeq.ToString(), laborDtlSeq.ToString(), out strmessage);
                    //labAd.RecallFromApproval(dsLabHed, false, out strmessage);
                    //labAd.CheckNonConformance(dt.Rows[0]["JobNum"].ToString(), labHedSeq, laborDtlSeq, out strmessage);
                    //dsLabHed.Tables["LaborDtl"].Rows[dsLabHed.Tables["LaborDtl"].Rows.Count - 1]["TimeStatus"] = "E";
                    //labAd.Update(dsLabHed);
                    //dsLabHed.Tables["LaborDtl"].Rows[dsLabHed.Tables["LaborDtl"].Rows.Count - 1].Delete();
                    //dsLabHed.Tables["LaborHed"].Rows[dsLabHed.Tables["LaborHed"].Rows.Count - 1].Delete();
                    //labAd.Update(dsLabHed);
                    EpicorSession.Dispose();
                    return chkbinInfo;
                }
                try
                {

                    ReceiptsFromMfgDataSet recDs = new ReceiptsFromMfgDataSet();
                    string pcTranType = "MFG-STK";
                    int piAssemblySeq = 0;
                    recAD.GetNewReceiptsFromMfgJobAsm(JobNum, piAssemblySeq, pcTranType, Guid.NewGuid().ToString(), recDs);
                    //recAD.ReceiptsFromMfgData.Tables["PartTran"].Rows[0]["PartNum"] = partNum;
                    recDs.Tables["PartTran"].Rows[0]["PartNum"] = partNum;
                    string opMessage = "";
                    recAD.OnChangePartNum(recDs, partNum, out opMessage, false);
                    string pcMessage;
                    recDs.Tables[0].Rows[0]["ActTranQty"] = LQty;
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
                }
                catch (Exception err)
                {
                    //string strmessage = "";
                    //labAd.ReviewIsDocumentLock(labHedSeq.ToString(), laborDtlSeq.ToString(), out strmessage);
                    //labAd.RecallFromApproval(dsLabHed, false, out strmessage);
                    //labAd.CheckNonConformance(dt.Rows[0]["JobNum"].ToString(), labHedSeq, laborDtlSeq, out strmessage);
                    //dsLabHed.Tables["LaborDtl"].Rows[dsLabHed.Tables["LaborDtl"].Rows.Count - 1]["TimeStatus"] = "E";
                    //labAd.Update(dsLabHed);
                    //dsLabHed.Tables["LaborDtl"].Rows[dsLabHed.Tables["LaborDtl"].Rows.Count - 1].Delete();
                    //dsLabHed.Tables["LaborHed"].Rows[dsLabHed.Tables["LaborHed"].Rows.Count - 1].Delete();
                    //labAd.Update(dsLabHed);
                    EpicorSession.Dispose();
                    return "0|" + err.Message.ToString();
                }
                EpicorSession.Dispose();
                return "1|处理成功";
            }
            catch (Exception ex)
            {
                //return "0|" + ex.Message.ToString()
                return "0|用户数不够,请尝试再次办理或稍后办理。错误代码：LaborMtlStk \t" + ex.Message;
            }
        }


        //D0505自动报工处理
        public string D0505(string empid, string JobNum, int asmSeq, int oprSeq, decimal LQty, decimal disQty, string disCode, string bjr, string zxDate, string zxTime, string companyId)
        { //JobNum as string ,jobQty as decimal,partNum as string

            try
            {
                //List<QueryWhereItemRow> whereItems = new List<QueryWhereItemRow>();
                //whereItems.Add(new QueryWhereItemRow() { TableID = "joboper", FieldName = "jobnum", RValue = JobNum });
                //whereItems.Add(new QueryWhereItemRow() { TableID = "joboper", FieldName = "AssemblySeq", RValue = asmSeq.ToString() });
                //whereItems.Add(new QueryWhereItemRow() { TableID = "joboper", FieldName = "OprSeq", RValue = oprSeq.ToString() });
                //DataTable dt = BaqResult("001-joboprqty2", whereItems, 0, companyId);
                //DataTable dt = GetDataByERP("select [JobOper].[JobNum] as [JobOper_JobNum],[JobOper].[AssemblySeq] as [JobOper_AssemblySeq],[JobOper].[OprSeq] as [JobOper_OprSeq],[JobOper].[RunQty] as [JobOper_RunQty],[JobOper].[QtyCompleted] as [JobOper_QtyCompleted],[JobHead].[JobComplete] as [JobHead_JobComplete],[JobHead].[JobReleased] as [JobHead_JobReleased],[OpMaster].[Character05] as [OpMaster_Character05] from Erp.JobOper as JobOper left outer join Erp.JobHead as JobHead on JobOper.Company = JobHead.Company and JobOper.JobNum = JobHead.JobNum inner join OpMaster as OpMaster on JobOper.Company = OpMaster.Company and JobOper.OpCode = OpMaster.OpCode where (JobOper.Company = '" + companyId + "'  and JobOper.JobNum = '" + JobNum + "'  and JobOper.AssemblySeq = '" + asmSeq.ToString() + "'  and JobOper.OprSeq ='" + oprSeq.ToString() + "') order by JobOper.OprSeq Desc");
                DataTable dt = GetDataByERP(@"select [JobOper].[JobNum] as [JobOper_JobNum],
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
                    empid = dt.Rows[0]["OpMaster.Character05"].ToString().Trim();
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
                if (empid.Trim() == "") { empid = "DB"; }
                try
                {

                }
                catch
                { }
                //string resultdata = ErpLogin/();
                //if (resultdata != "true")
                //{
                //    return "0|" + resultdata;
                //}
                Session EpicorSession = ErpLoginbak();
                if (EpicorSession == null)
                {
                    return "-1|erp用户数不够，请稍候再试.接口号：D0505";
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
                outTime = System.DateTime.Now.Hour + System.DateTime.Now.Minute / 100;
                labAd.GetNewLaborDtlWithHdr(dsLabHed, System.DateTime.Today, 0, System.DateTime.Today, outTime, labHedSeq);

                labAd.DefaultLaborType(dsLabHed, "P");
                labAd.DefaultJobNum(dsLabHed, JobNum);
                labAd.DefaultAssemblySeq(dsLabHed, asmSeq);
                string msg;
                labAd.DefaultOprSeq(dsLabHed, oprSeq, out msg);
                dtLabDtl = dsLabHed.LaborDtl;
                labAd.DefaultLaborQty(dsLabHed, LQty, out msg);
                //dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["LaborQty"] = LQty;
                disQty = 0;  //先不回写不合格数量
                disCode = "";
                dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["DiscrepQty"] = disQty;
                dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["DiscrpRsnCode"] = disCode;
                dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["TimeStatus"] = "A";
                dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["Date01"] = System.DateTime.Today;
                dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["ShortChar01"] = System.DateTime.Now.ToString("hh:mm:ss");
                //dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["OpComplete"] = "1";
                //dtLabDtl.Rows[dtLabDtl.Rows.Count - 1]["Complete"] = "1";
                string cMessageText = "";
                try
                {
                    labAd.CheckWarnings(dsLabHed, out cMessageText);
                    labAd.Update(dsLabHed);
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
                    return "1|处理成功";
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



        //在此之前插入method
    }
}









[XmlInclude(typeof(JobOperRunQty))]
[Serializable]
public class JobOperRunQty
{
    private string jobnum;
    private string assemblynum;
    private string opernum;
    private string runqty;
    private string completedqty;
    public JobOperRunQty()
    {
    }

    public string Jobnum
    {
        get { return jobnum; }
        set { jobnum = value; }
    }
    public string Assemblynum
    {
        get { return assemblynum; }
        set { assemblynum = value; }
    }
    public string Opernum
    {
        get { return opernum; }
        set { opernum = value; }
    }
    public string Runqty
    {
        get { return runqty; }
        set { runqty = value; }
    }
    public string Completedqty
    {
        get { return completedqty; }
        set { completedqty = value; }
    }
}


