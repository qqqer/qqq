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

                Ice.Core.Session E9Session = new Ice.Core.Session(userName, passWord, serverUrl, Session.LicenseType.Default);
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
    }
}
