using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using Ice.Core;
using Ice.Proxy.BO;
using Erp.Adapters;
using Erp.Contracts;
using Erp.BO;
using Erp.Proxy.BO;
using Epicor.ServiceModel.Channels;
using System.Data.SqlClient;
using Ice.Adapters;
using Ice.Tablesets;
using Ice.BO;

namespace EpicorAPIManager
{
    public class PurchaseManager
    {
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

        /// <summary>
        /// 获取采购订单数据
        /// </summary>
        /// <param name="poNum">采购订单号</param>
        /// <param name="companyId">公司编码</param>
        /// <returns></returns>
        public DataTable GetPurchaseOrder(int poNum,string companyId)
        {
            if ((poNum.ToString().Length > 0) == false)
            {
                return null;
            }
            try
            {

                Session EpicorSession = ErpLoginbak();
                if (EpicorSession == null)
                {
                    return null;
                }
                EpicorSession.CompanyID = companyId;
                POImpl adapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<POImpl>(EpicorSession, ImplBase<Erp.Contracts.POSvcContract>.UriPath);
                VendorImpl vendoradapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<VendorImpl>(EpicorSession, ImplBase<Erp.Contracts.VendorSvcContract>.UriPath);
                PartClassImpl partadapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<PartClassImpl>(EpicorSession, ImplBase<Erp.Contracts.PartClassSvcContract>.UriPath);

                PODataSet poEntry;
                //Epicor.Mfg.BO.Vendor vendoradapter = new Vendor(ConnectionPool);
                VendorDataSet vendorEntry;
                //Epicor.Mfg.BO.PartClass partadapter = new PartClass(ConnectionPool);
                PartClassDataSet partEntry;
                poEntry = adapter.GetByID(poNum);
                DataTable dt = poEntry.Tables["PODetail"];
                DataTable datadt = new DataTable();
                if (dt.Rows.Count > 0)
                {
                    vendorEntry = vendoradapter.GetByID(Convert.ToInt32(dt.Rows[0]["VendorNum"]));
                    DataTable vendordt = vendorEntry.Tables["Vendor"];
                    DataColumn VendorCode = new DataColumn("VendorCode");
                    DataColumn VendorName = new DataColumn("VendorName");
                    DataColumn PONum = new DataColumn("PONum");
                    DataColumn POLine = new DataColumn("POLine");
                    DataColumn LineQty = new DataColumn("LineQty");
                    DataColumn PartNum = new DataColumn("PartNum");
                    DataColumn PartDescription = new DataColumn("PartDescription");
                    DataColumn IUM = new DataColumn("IUM");
                    DataColumn JobNum = new DataColumn("JobNum");
                    DataColumn DueDate = new DataColumn("DueDate");
                    DataColumn PartType = new DataColumn("PartType");
                    datadt.Columns.Add(VendorCode);
                    datadt.Columns.Add(VendorName);
                    datadt.Columns.Add(PONum);
                    datadt.Columns.Add(POLine);
                    datadt.Columns.Add(LineQty);
                    datadt.Columns.Add(PartNum);
                    datadt.Columns.Add(PartDescription);
                    datadt.Columns.Add(IUM);
                    datadt.Columns.Add(JobNum);
                    datadt.Columns.Add(DueDate);
                    datadt.Columns.Add(PartType);
                    foreach (DataRow dr in dt.Rows)
                    {
                        DataRow row = datadt.NewRow();
                        row["VendorCode"] = vendordt.Rows[0]["VendorID"].ToString();
                        row["VendorName"] = vendordt.Rows[0]["Name"].ToString();
                        row["PONum"] = dr["PONUM"].ToString();
                        row["POLine"] = dr["POLine"].ToString();
                        row["LineQty"] = dr["OrderQty"].ToString();
                        row["PartNum"] = dr["PartNum"].ToString();
                        row["PartDescription"] = dr["LineDesc"].ToString();
                        row["IUM"] = dr["IUM"].ToString();
                        row["JobNum"] = dr["CalcJobNum"].ToString();
                        row["DueDate"] = dr["CalcDueDate"].ToString();
                        partEntry = partadapter.GetByID(dr["ClassID"].ToString());
                        DataTable partdt = partEntry.Tables["PartClass"];
                        row["PartType"] = partdt.Rows[0]["Description"].ToString();
                        datadt.Rows.Add(row);
                    }
                }
                EpicorSession.Dispose();
                return datadt;
            }
            catch
            {
               // EpicorSession.Dispose();
                return null;
            }
        }

    }
}
