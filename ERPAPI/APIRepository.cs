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
    public static class ErpApi
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


        public static string QueryERP(string sqlstr)
        {
            SqlConnection conn = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["erpConnectionstring"]);
            conn.Open();
            SqlCommand cmd = new SqlCommand(sqlstr, conn);
            Object o = cmd.ExecuteScalar();
            conn.Close();
            return o == null ? "" : o.ToString();
        }



        public static DataTable GetDataByERP(string sqlstr)
        {
            SqlConnection conn = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["erpConnectionstring"]);
            conn.Open();
            SqlDataAdapter ada = new SqlDataAdapter(sqlstr, conn);
            DataTable dt = new DataTable();
            ada.Fill(dt);
            conn.Close();
            return dt;
        }


        public static string getJobNextOprTypes(string jobnum, int asmSeq, int oprseq, out int OutAsm, out int OutOprSeq, string companyId)
        {
            string stype = "";
            OutAsm = 0;
            OutOprSeq = 0;
            if (jobnum.Trim() == "") { return "0|jonum error"; }

            try
            {
                int NextOperSeq = 0;
                if (asmSeq == 0) //0层半层品
                {
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
            }

        }


        public static string poDes(int ponum, int poline, int porel, string companyId)
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
                    bool.TryParse(dt.Rows[0]["Part.NonStock"].ToString().Trim(), out nonStock);
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
                            string relsql = "select [PORel].[PONum] as [PORel_PONum],[PORel].[POLine] as [PORel_POLine],[PORel].[PORelNum] as [PORel_PORelNum],[PODetail].[PartNum] as [PODetail_PartNum],[Part].[NonStock] as [Part_NonStock],[PORel].[JobNum] as [PORel_JobNum],[PORel].[AssemblySeq] as [PORel_AssemblySeq],[PORel].[JobSeqType] as [PORel_JobSeqType],[PORel].[JobSeq] as [PORel_JobSeq],[ReqDetail].[ReqNum] as [ReqDetail_ReqNum],[ReqDetail].[ReqLine] as [ReqDetail_ReqLine],[ReqHead].[RequestorID] as [ReqHead_RequestorID],[UserFile].[Name] as [UserFile_Name],[PartPlant].[PrimWhse] as [PartPlant_PrimWhse],[Warehse].[Description] as [Warehse_Description],[JobOper].[OprSeq] as [JobOper_OprSeq],[OpMaster].[OpDesc] as [OpMaster_OpDesc],[PORel].[TranType] as [PORel_TranType],[PODetail].[VendorNum] as [PODetail_VendorNum],[ReqDetail].[Character10] as [ReqDetail_Character10] from Erp.PORel as PORel inner join Erp.PODetail as PODetail on PORel.Company = PODetail.Company and PORel.PONUM = PODetail.PONum and PORel.POLine = PODetail.POLine left outer join Erp.Part as Part on PODetail.Company = Part.Company and PODetail.PartNum = Part.PartNum left outer join Erp.PartPlant as PartPlant on Part.Company = PartPlant.Company and Part.PartNum = PartPlant.PartNum left outer join Erp.Warehse as Warehse on PartPlant.Company = Warehse.Company and PartPlant.PrimWhse = Warehse.WarehouseCode left outer join ReqDetail as ReqDetail on PODetail.Company = ReqDetail.Company and PODetail.PONUM = ReqDetail.PONUM and PODetail.POLine = ReqDetail.POLine left outer join Erp.ReqHead as ReqHead on ReqDetail.Company = ReqHead.Company and ReqDetail.ReqNum = ReqHead.ReqNum left outer join Erp.UserFile as UserFile on ReqHead.RequestorID = UserFile.DcdUserID left outer join Erp.JobMtl as JobMtl on PORel.Company = JobMtl.Company and PORel.JobNum = JobMtl.JobNum and PORel.AssemblySeq = JobMtl.AssemblySeq and PORel.JobSeq = JobMtl.MtlSeq left outer join Erp.JobOper as JobOper on JobMtl.Company = JobOper.Company and JobMtl.JobNum = JobOper.JobNum and JobMtl.AssemblySeq = JobOper.AssemblySeq and JobMtl.RelatedOperation = JobOper.OprSeq left outer join Erp.OpMaster as OpMaster on JobOper.Company = OpMaster.Company and JobOper.OpCode = OpMaster.OpCode where (PORel.Company = '" + companyId + "'  and PORel.PONum ='" + ponum + "'  and PORel.POLine ='" + poline + "'  and PORel.PORelNum ='" + porel + "')";
                            DataTable dt2 = GetDataByERP(relsql);
                            for (int i = 0; i < dt2.Columns.Count; i++)
                            {
                                dt2.Columns[i].ColumnName = dt2.Columns[i].ColumnName.Replace('_', '.');
                            }

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



        public static string[] GetPartWB(string partnum, string companyId)
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



        public static string[] GetPartWB(string partnum, string companyId, string plant)//获取物料的默认主仓库和库位
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
                DataTable dt = GetDataByERP("select [WhseBin].[WarehouseCode] as [WhseBin_WarehouseCode],[WhseBin].[ZoneID] as [WhseBin_ZoneID],[WhseBin].[BinNum] as [WhseBin_BinNum] from Erp.WhseBin as WhseBin where (WhseBin.Company = '" + companyId + "'  and WhseBin.ZoneID = '" + zonid + "'  and WhseBin.BinNum = '" + binid + "' and WhseBin.WarehouseCode='" + wh + "')");

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


        public static string porcv(string packNum, string recdate, string vendorid, string rcvdtlStr, string c10, string companyId)
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


            Session EpicorSession = GetEpicorSession();
            if (EpicorSession == null)
            {
                return "0|erp用户数不够，请稍候再试.ERR:porcv";
            }
            EpicorSession.CompanyID = companyId;
            string plantid = QueryERP("select Plant from erp.POrel where PONum='" + poNum + "' and POLine='" + poLine + "'");
            EpicorSession.PlantID = plantid;

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
                    EpicorSession = GetEpicorSession();
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


        //D0506-01工单收货至库存
        public static string D0506_01(string rqr, string JobNum, int asmSeq, decimal jobQty, string lotnum, string wh, string bin, string companyId)
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
                Session EpicorSession = GetEpicorSession();
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
    }
}