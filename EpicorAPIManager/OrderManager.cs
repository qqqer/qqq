using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using Epicor.Mfg.BO;

namespace EpicorAPIManager
{
    public class OrderManager
    {
        PO poOrder;
        SalesOrder salesOrder;
        POApvMsg poApvMsg;
        Epicor.Mfg.Core.BLConnectionPool ConnectionPool;

        public int UpdateQuotes(string state, int quoteNum)
        {
            int result = 0;
            try
            {
                Executesql("update QuoteHed set shortchar01='" + state + "' where QuoteNum=" + quoteNum + "");
                result = 1;
            }
            catch
            {
                result = 0;
            }
            return result;
        }

        public int UpdateOrder(string state, int orderNum,string[] arrs,string[] relnums)
        {
            ConnectionPool = CommonClass.GetSession.Get().ConnectionPool;
            salesOrder = new SalesOrder(ConnectionPool);
            
            SalesOrderDataSet ds = salesOrder.GetByID(orderNum);
            
            int result = 0;
            try
            {
                Executesql("update OrderHed set shortchar01=N'" + state + "',date01=getdate() where ordernum=" + orderNum + "");
                if (state == "拒绝")
                {
                    string str = Executesql("select MAX(Number01)  from OrderRel");
                    string custNum = Executesql("select CustNum from OrderHed where ordernum=" + orderNum + "");
                    Decimal index = Convert.ToDecimal(str);
                    for (int i = 0; i < arrs.Length; i++)
                    {
                        ds.Tables["OrderRel"].Rows[i]["FirmRelease"] = false;
                    }
                    if (!string.IsNullOrEmpty(custNum))
                    {
                        string outstr = "";
                        bool outbool = false;
                        salesOrder.MasterUpdate(true, "OrderRel", Convert.ToInt32(custNum), orderNum, false, out outbool, out outstr, out outstr, out outstr, out outstr, ds);
                    }
                    for (int i = 0; i < arrs.Length; i++)
                    {
                        index = index + 10;
                        string sql = "update Orderrel set Number01=" + index + " where ordernum=" + orderNum + " and orderline=" + arrs[i] + " and orderrelnum=" + relnums[i] + "";
                        Executesqls(sql);
                    }
                    string sqlreturn = Executesql("select sum(FirmRelease) from OrderRel where OrderNum=" + orderNum + "");
                    if (int.Parse(sqlreturn) == 0)
                    {
                        result = 1;
                    }
                    else
                    {
                        result = 0;
                    }
                }
                else
                {
                    string str = Executesql("select MAX(Number01)  from OrderRel");
                    string custNum = Executesql("select CustNum from OrderHed where ordernum=" + orderNum + "");
                    Decimal index = Convert.ToDecimal(str);
                    //result = Convert.ToInt32(index);
                    for (int i = 0; i < arrs.Length; i++)
                    {
                        ds.Tables["OrderRel"].Rows[i]["FirmRelease"] = true;
                    }
                    if (!string.IsNullOrEmpty(custNum))
                    {
                        string outstr = "";
                        bool outbool = false;
                        salesOrder.MasterUpdate(true, "OrderRel", Convert.ToInt32(custNum), orderNum, false, out outbool, out outstr, out outstr, out outstr, out outstr, ds);
                    }
                    for (int i = 0; i < arrs.Length; i++)
                    {
                        index = index + 10;
                        string sql = "update Orderrel set Number01=" + index + " where ordernum=" + orderNum + " and orderline=" + arrs[i] + " and orderrelnum=" + relnums[i] + "";
                        Executesqls(sql);
                    }
                    string sqlreturn = Executesql("select sum(FirmRelease) from OrderRel where OrderNum=" + orderNum + "");
                    if (int.Parse(sqlreturn) > 0)
                    {
                        result = 1;
                    }
                    else
                    {
                        result = 0;
                    }
                }
            }
            catch
            {
                result = 0;
            }
            if (CommonClass.GetSession.epicor9Seesion != null)
            {
                CommonClass.GetSession.epicor9Seesion.Dispose();
            }
            return result;
        }

        public int UpdatePOOrder(string state, int poNum)
        {
            int result = 0;
            try
            {
                Executesql("update POHeader set shortchar01='" + state + "' where PONum=" + poNum + "");
                result = 1;
            }
            catch
            {
                result = 0;
            }
            return result;
        }

        public int UpdateOrderrel(int state, int num, int lineNum, int relnum)
        {
            int result = 0;
            try
            {
                Executesql("update OrderRel set CheckBox01=" + state + " where OrderNum=" + num + " and OrderLine=" + lineNum + " and OrderRelNum=" + relnum + "");
                result = 1;
            }
            catch
            {
                result = 0;
            }
            return result;
        }

        public static string Executesql(string sqlstr)
        {
            SqlConnection conn = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["sqlConnString"]);
            conn.Open();
            SqlCommand cmd = new SqlCommand(sqlstr, conn);
            Object o = cmd.ExecuteScalar();
            conn.Close();
            return o == null ? "" : o.ToString();
        }

        public static string Executesqls(string sqlstr)
        {
            SqlConnection conn = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["sqlConnString"]);
            conn.Open();
            SqlCommand cmd = new SqlCommand(sqlstr, conn);
            Object o = cmd.ExecuteNonQuery();
            conn.Close();
            return o == null ? "" : o.ToString();
        }

        public int UpDatePO(bool state, int poNum,decimal amount)
        {
            int result = 0;
            POApvMsgDataSet ds = new POApvMsgDataSet();
            try
            {
                ConnectionPool = CommonClass.GetSession.Get().ConnectionPool;
                poApvMsg = new POApvMsg(ConnectionPool);
                
                string msg;
                bool boolout;
                //ds = poApvMsg.GetAllRows("", 0, 0, out boolout);
                ds = poApvMsg.GetByID(poNum);
                poApvMsg.CheckApprovalLimit(poNum, amount, "APPROVED", out msg);
                ds.Tables["POApvMsg"].Rows[0]["ApproverResponse"] = "APPROVED";
                ds.Tables["POApvMsg"].Rows[0]["ApvAmt"] = amount;
                ds.Tables["POApvMsg"].Rows[0]["DBRowIdent"] = null;
                poApvMsg.Update(ds);
                result = 1;
            }
            catch (Exception e)
            {
            }
            if (CommonClass.GetSession.epicor9Seesion != null)
            {
                CommonClass.GetSession.epicor9Seesion.Dispose();
            }
            return result;
        }

        public string QueryCustomerData(string companyid,string customerid)
        {
            string result = "";
            try
            {
                DataTable Temp;

                CommonProject.CustCreditInfo _CustCreditInfo = new CommonProject.CustCreditInfo();
                Temp = _CustCreditInfo.GetCustCreditInfor(CommonClass.GetSession.Get(), companyid, customerid);
                for (int i = 0; i < Temp.Columns.Count; i++)
                {
                    //result = result + Temp.Columns[i].ColumnName + ":" + Temp.Rows[0][i].ToString() + ",";
                    result = result + Temp.Rows[0][i].ToString() + ",";
                }
                result = result.Substring(0, result.Length - 1);
            }
            catch(Exception ex)
            {
                result = ex.ToString();
            }
            if (CommonClass.GetSession.epicor9Seesion != null)
            {
                CommonClass.GetSession.epicor9Seesion.Dispose();
            }
            return result;
        }

    }

    
}
