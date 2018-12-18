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
    public static class Receipt
    {

        public static string poDes(int ponum, int poline, int porel, string companyId)
        {
            try
            {
                List<QueryWhereItemRow> whereItems = new List<QueryWhereItemRow>();
                whereItems.Add(new QueryWhereItemRow() { TableID = "porel", FieldName = "ponum", RValue = ponum.ToString() });
                whereItems.Add(new QueryWhereItemRow() { TableID = "porel", FieldName = "poline", RValue = poline.ToString() });
                whereItems.Add(new QueryWhereItemRow() { TableID = "porel", FieldName = "porelnum", RValue = porel.ToString() });
                string sql = "select [PORel].[PONum] as [PORel_PONum],[PORel].[POLine] as [PORel_POLine],[PORel].[PORelNum] as [PORel_PORelNum],[PODetail].[PartNum] as [PODetail_PartNum],[Part].[NonStock] as [Part_NonStock],[PORel].[JobNum] as [PORel_JobNum],[PORel].[AssemblySeq] as [PORel_AssemblySeq],[PORel].[JobSeqType] as [PORel_JobSeqType],[PORel].[JobSeq] as [PORel_JobSeq],[ReqDetail].[ReqNum] as [ReqDetail_ReqNum],[ReqDetail].[ReqLine] as [ReqDetail_ReqLine],[ReqHead].[RequestorID] as [ReqHead_RequestorID],[UserFile].[Name] as [UserFile_Name],[PartPlant].[PrimWhse] as [PartPlant_PrimWhse],[Warehse].[Description] as [Warehse_Description],[JobOper].[OprSeq] as [JobOper_OprSeq],[OpMaster].[OpDesc] as [OpMaster_OpDesc],[PORel].[TranType] as [PORel_TranType],[PODetail].[VendorNum] as [PODetail_VendorNum],[ReqDetail].[RcvPerson_c] as [ReqDetail_RcvPerson_c] from Erp.PORel as PORel inner join Erp.PODetail as PODetail on PORel.Company = PODetail.Company and PORel.PONUM = PODetail.PONum and PORel.POLine = PODetail.POLine left outer join Erp.Part as Part on PODetail.Company = Part.Company and PODetail.PartNum = Part.PartNum left outer join Erp.PartPlant as PartPlant on Part.Company = PartPlant.Company and Part.PartNum = PartPlant.PartNum ";
                sql = sql + " left outer join Erp.Warehse as Warehse on PartPlant.Company = Warehse.Company and PartPlant.PrimWhse = Warehse.WarehouseCode left outer join ReqDetail as ReqDetail on PODetail.Company = ReqDetail.Company and PODetail.PONUM = ReqDetail.PONUM and PODetail.POLine = ReqDetail.POLine left outer join Erp.ReqHead as ReqHead on ReqDetail.Company = ReqHead.Company and ReqDetail.ReqNum = ReqHead.ReqNum left outer join Erp.UserFile as UserFile on ReqHead.RequestorID = UserFile.DcdUserID left outer join Erp.JobMtl as JobMtl on PORel.Company = JobMtl.Company and PORel.JobNum = JobMtl.JobNum and PORel.AssemblySeq = JobMtl.AssemblySeq and PORel.JobSeq = JobMtl.MtlSeq left outer join Erp.JobOper as JobOper on JobMtl.Company = JobOper.Company and JobMtl.JobNum = JobOper.JobNum and JobMtl.AssemblySeq = JobOper.AssemblySeq and JobMtl.RelatedOperation = JobOper.OprSeq left outer join Erp.OpMaster as OpMaster on JobOper.Company = OpMaster.Company and JobOper.OpCode = OpMaster.OpCode where (PORel.Company = '" + companyId + "'  and PORel.PONum = '" + ponum + "'  and PORel.POLine ='" + poline + "'  and PORel.PORelNum ='" + porel + "')";
                DataTable dt = Common.GetDataByERP(sql);


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
                        return "R|物料接收人:" + dt.Rows[0]["ReqDetail.RcvPerson.c"].ToString().Trim();
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

                        string ss = "",code,c;
                        int nextAsm = 0, nextOprSeq = 0;
                        ss = Common.getJobNextOprTypes(jobnum, asmSeq, oprSeq, out nextAsm, out nextOprSeq, out code, out c,companyId);
                        if (ss.Substring(0, 1).Trim().ToLower() == "s")
                        {
                            DataTable dt2;

                            sql = @"select PONum, POLine, PORelNum from erp.PORel pr INNER JOIN  Erp.JobOper jo  on  pr.JobNum = jo.JobNum and pr.AssemblySeq  = jo.AssemblySeq and pr.JobSeq = jo.OprSeq
	                                where pr.JobNum='"+ jobnum + "' and pr.AssemblySeq = " + nextAsm + " and pr.JobSeq = " + nextOprSeq +"";
                            dt2 = Common.GetDataByERP(sql);

                            string relsql = "select [PORel].[PONum] as [PORel_PONum],[PORel].[POLine] as [PORel_POLine],[PORel].[PORelNum] as [PORel_PORelNum],[PODetail].[PartNum] as [PODetail_PartNum],[Part].[NonStock] as [Part_NonStock],[PORel].[JobNum] as [PORel_JobNum],[PORel].[AssemblySeq] as [PORel_AssemblySeq],[PORel].[JobSeqType] as [PORel_JobSeqType],[PORel].[JobSeq] as [PORel_JobSeq],[ReqDetail].[ReqNum] as [ReqDetail_ReqNum],[ReqDetail].[ReqLine] as [ReqDetail_ReqLine],[ReqHead].[RequestorID] as [ReqHead_RequestorID],[UserFile].[Name] as [UserFile_Name],[PartPlant].[PrimWhse] as [PartPlant_PrimWhse],[Warehse].[Description] as [Warehse_Description],[JobOper].[OprSeq] as [JobOper_OprSeq],[OpMaster].[OpDesc] as [OpMaster_OpDesc],[PORel].[TranType] as [PORel_TranType],[PODetail].[VendorNum] as [PODetail_VendorNum],[ReqDetail].[RcvPerson_c] as [ReqDetail_RcvPerson_c] from Erp.PORel as PORel inner join Erp.PODetail as PODetail on PORel.Company = PODetail.Company and PORel.PONUM = PODetail.PONum and PORel.POLine = PODetail.POLine left outer join Erp.Part as Part on PODetail.Company = Part.Company and PODetail.PartNum = Part.PartNum left outer join Erp.PartPlant as PartPlant on Part.Company = PartPlant.Company and Part.PartNum = PartPlant.PartNum left outer join Erp.Warehse as Warehse on PartPlant.Company = Warehse.Company and PartPlant.PrimWhse = Warehse.WarehouseCode left outer join ReqDetail as ReqDetail on PODetail.Company = ReqDetail.Company and PODetail.PONUM = ReqDetail.PONUM and PODetail.POLine = ReqDetail.POLine left outer join Erp.ReqHead as ReqHead on ReqDetail.Company = ReqHead.Company and ReqDetail.ReqNum = ReqHead.ReqNum left outer join Erp.UserFile as UserFile on ReqHead.RequestorID = UserFile.DcdUserID left outer join Erp.JobMtl as JobMtl on PORel.Company = JobMtl.Company and PORel.JobNum = JobMtl.JobNum and PORel.AssemblySeq = JobMtl.AssemblySeq and PORel.JobSeq = JobMtl.MtlSeq left outer join Erp.JobOper as JobOper on JobMtl.Company = JobOper.Company and JobMtl.JobNum = JobOper.JobNum and JobMtl.AssemblySeq = JobOper.AssemblySeq and JobMtl.RelatedOperation = JobOper.OprSeq left outer join Erp.OpMaster as OpMaster on JobOper.Company = OpMaster.Company and JobOper.OpCode = OpMaster.OpCode where (PORel.Company = '" + companyId + "'  and PORel.PONum = "+ dt2.Rows[0]["PONum"].ToString() + "  and PORel.POLine ="+ dt2.Rows[0]["POLine"].ToString() + "  and PORel.PORelNum ="+ dt2.Rows[0]["PORelNum"].ToString() + ")";
                            dt2 = Common.GetDataByERP(relsql);
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


            Session EpicorSession =  Common.GetEpicorSession();
            if (EpicorSession == null)
            {
                return "0|erp用户数不够，请稍候再试.ERR:porcv";
            }
            EpicorSession.CompanyID = companyId;
            string plantid = Common.QueryERP("select Plant from erp.POrel where PONum='" + poNum + "' and POLine='" + poLine + "'");
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
                            pb2 = Common.GetPartWB(dtRcvDtl.Rows[j]["partnum"].ToString().Trim(), companyId, plantid);
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
                ds.Tables["RcvHead"].Rows[0]["Plant"] = Common.QueryERP("select Plant from erp.POrel where PONum='" + poNum + "' and POLine='" + poLine + "'");
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
                    }
                    //green start edit============取物料默认的主仓库作为收货仓库
                    if (ordertype.ToLower() != "pur-ukn")
                    {

                        pb = Common.GetPartWB(dtRcvDtl.Rows[i]["partnum"].ToString(), companyId, plantid);
                        // pb[1] = logic2.GetPartWB(dtRcvDtl.Rows[i]["warehousecode"].ToString().Trim())[1];
                        if (whcode == "") //取物料主要仓库
                        { whcode = pb[0].Trim(); }


                        //  据库位条码信息校验仓库并取库位
                        if (jobnum == "")
                        {
                            chkbinInfo = Common.checkbin(dtRcvDtl.Rows[i]["binnum"].ToString().Trim(), whcode, companyId);
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

                        if (pb[2].Trim() == "lot") //使用批次追踪
                        {
                            lotStr2 = lotStr;
                            if (lotStr.Trim() == "" && (ordertype.ToLower() == "pur-sub" || ordertype.ToLower() == "pur-mtl"))
                            { lotStr = jobnum; }
                            string lotnum = Common.QueryERP("select pl.LotNum from erp.partlot pl left join erp.PODetail pd on pl.Company=pd.Company and pl.PartNum=pd.PartNum where pl.Company='" + companyId + "' and pd.PONUM=" + poNum + " and pd.POLine=" + poline + " and pl.LotNum='" + lotStr + "'");
                            if (!string.IsNullOrEmpty(lotnum))
                            {
                                ds.Tables["RcvDtl"].Rows[ds.Tables["RcvDtl"].Rows.Count - 1]["LotNum"] = lotStr;
                            }
                            else
                            {
                                if (dtRcvDtl.Columns.Contains("HeatNum") && !string.IsNullOrEmpty(dtRcvDtl.Rows[i]["HeatNum"].ToString()))
                                {
                                    string partnum = Common.QueryERP("select PartNum from erp.PODetail where Company='" + companyId + "' and PONUM=" + poNum + " and POLine=" + poline);
                                    lotadapter.GetNewPartLot(lotds, partnum);
                                    lotds.Tables["PartLot"].Rows[i]["LotNum"] = lotStr;
                                    lotds.Tables["PartLot"].Rows[i]["HeatNum"] = dtRcvDtl.Rows[i]["HeatNum"].ToString();
                                    lotadapter.Update(lotds);
                                }
                                ds.Tables["RcvDtl"].Rows[i]["LotNum"] = lotStr;
                            }
                        }

                        if (jobnum != "") ////收货到工单的，指定默认仓库
                        {
                            ds.Tables["RcvDtl"].Rows[ds.Tables["RcvDtl"].Rows.Count - 1]["WareHouseCode"] = "ins";  //仓库
                            ds.Tables["RcvDtl"].Rows[ds.Tables["RcvDtl"].Rows.Count - 1]["BinNum"] = "ins";  //库位
                        }
                    }
                    else // == pur-ukn 时
                    {
                        ds.Tables["RcvDtl"].Rows[ds.Tables["RcvDtl"].Rows.Count - 1]["WareHouseCode"] = "ins";  //仓库
                        ds.Tables["RcvDtl"].Rows[ds.Tables["RcvDtl"].Rows.Count - 1]["BinNum"] = "ins";  //库位
                        ds.Tables["RcvDtl"].Rows[ds.Tables["RcvDtl"].Rows.Count - 1]["LotNum"] = "";
                        lotStr = "";
                    }


                    {
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

                        recAD.OnChangeDtlReceived(vendornum, purPoint, packNum, 0, true, ds);
                        u = 20;
                        string outMsg1 = "", outMsg2 = "", outMsg3 = "", outMsg4 = "", outMsg5 = "", outMsg6 = "", outMsg7 = "",
                        outMsg8 = "";
                        bool outBool1 = false, outBool2 = false, outBool3 = false;

                        u = 21;

                        recAD.Update(ds);
                    }
                }
                EpicorSession.Dispose();
                //EpicorSessionManager.DisposeSession();
                return "1|处理成功.";
            }
            catch (Exception ex)
            {
                if (EpicorSession == null || !EpicorSession.IsValidSession(EpicorSession.SessionID, "manager"))
                {
                    EpicorSession = Common.GetEpicorSession();
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



        //TranStk库存转仓接口
        public static string tranStk(string jsonStr, string companyId)
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
                    
                    DataTable dt = Common.GetDataByERP("select [PartBin].[OnhandQty] as [PartBin_OnhandQty] from Erp.PartBin as PartBin where (PartBin.Company = '" + company + "'  and PartBin.PartNum = '" + partnum + "'  and PartBin.WarehouseCode = '" + wcode + "'  and PartBin.BinNum = '" + binnum + "'  and PartBin.LotNum = '" + lotnum + "')");
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


                Session EpicorSession = Common.GetEpicorSession();
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
                }


                EpicorSession.Dispose();
                return "1|处理成功.";

            }

            catch (Exception ex)
            {
                return "0|" + ex.Message.ToString();
            }
        }
      
    }
}