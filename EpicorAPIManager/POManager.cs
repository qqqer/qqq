using System;
using System.Collections.Generic;
using System.Text;
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
   public class POManager
    {
        

        public  POManager()
        {
            
        
        
        }



        public void dispo()
        {
            
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

        //更新po自定义字段  848728  
        //经过测试，发现po关闭了可以写入自定义字段，但核准了却不行
        public string upPON05(int ponum, int poline, decimal n05, string companyId)
        {
            bool isClose = false;
            bool isApprove = false;
            string s1="";

            Session EpicorSession = ErpLoginbak();
            if (EpicorSession == null)
            {
                return "-1|erp用户数不够，请稍候再试. 错误代码：upPON05";
            }
            EpicorSession.CompanyID = companyId;
            POImpl poAd = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<POImpl>(EpicorSession, ImplBase<Erp.Contracts.POSvcContract>.UriPath);
            PODataSet poDs = poAd.GetByID(ponum);
                PODataSet.POHeaderDataTable poHeadDt = poDs.POHeader;
               
                if (Convert.ToBoolean(poHeadDt.Rows[0]["OpenOrder"]) == false)
                {
                    isClose = true;
                    poHeadDt.Rows[0]["OpenOrder"] = true;
                    poAd.ReopenOrder(ponum);
                    poAd.Update(poDs);
                    
                }

              



               

                if (Convert.ToBoolean(poHeadDt.Rows[0]["Approve"]) == true)
                {
                    isApprove = true;
                    poHeadDt.Rows[0]["Approve"] = false;  //要先设值，再ChangeApproveSwitch,否则会提示【尚未调整】
                    poHeadDt.Rows[0]["ApprovalStatus"] = "U";
                    poAd.ChangeApproveSwitch(false, out s1, poDs);
                    poAd.Update(poDs);

                }

                try
                {
                    PODataSet.PODetailDataTable podtlDt = poDs.PODetail;
                    System.Data.DataRow[] r = podtlDt.Select("poline=" + poline);
                    if (r == null || r.Length == 0)
                    { return "0|找不到po行" + poline; }
                    r[0]["Number05"] = n05;
                    poAd.Update(poDs);

               
                




                //bool exitPoline = false;
                //double polineQty = 0;
                //for (int i = 0; i < podtlDt.Rows.Count; i++)
                //{
                //    if (Convert.ToInt32(podtlDt.Rows[i]["poline"]) == poline)
                //    {
                //        exitPoline = true;
                //        if (Convert.ToBoolean(podtlDt.Rows[i]["OpenLine"]) == false)
                //        {
                //            c1 = null;
                //            s1.Dispose();
                //            ConnectionPool.Dispose();
                //            return "0|流程办理出错，po行已关闭";
                //        }
                //        polineQty = Convert.ToDouble(podtlDt.Rows[i]["CalcOurQty"]);
                //    }
                //}
                //if (exitPoline == false)
                //{
                //    c1 = null;
                //    s1.Dispose();
                //    ConnectionPool.Dispose();
                //    return "0|流程办理出错，po行不存在";
                //}
                //遍历所有porel
                //PODataSet.PORelDataTable porelDt = poDs.PORel;
                //int rcvedQty = 0;
                //for (int i = 0; i < porelDt.Rows.Count; i++)
                //{
                //    if (Convert.ToInt32(porelDt.Rows[i]["poline"]) == poline)
                //    {
                //        rcvedQty = rcvedQty + Convert.ToInt32(porelDt.Rows[i]["receivedQty"]);
                //    }
                //}
                //double unRcvQty = polineQty - rcvedQty;
                //if (Math.Round((rcvqty - unRcvQty), 5) > 0)
                //{
                //    c1 = null;
                //    s1.Dispose();
                //    ConnectionPool.Dispose();
                //    return "0|流程办理出错，收货数量" + rcvqty.ToString() + "超过PO未关闭数量" + unRcvQty.ToString() + "，请确认是否需要修改采购订单！";
                //}


                //string sqlstr = "select ISNULL(sum(shsl),0) from BO_SC_CGYSSUB where ponum=" + ponum + " and poline=" + poline + " and  isend=0";
                //double sbQty = 0;
                //DataTable dt2 = GetDataByAWS(sqlstr);
                //if (dt2.Rows.Count > 0)
                //{ sbQty = Convert.ToDouble(dt2.Rows[0][0]); }





               
            }
            catch (Exception ex)
            {
                EpicorSession.Dispose();
                return "0|" + ex.Message.ToString();
            }


                if (isApprove)
                {

                    poHeadDt.Rows[0]["Approve"] = true;  //要先设值，再ChangeApproveSwitch,否则会提示【尚未调整】
                    poHeadDt.Rows[0]["ApprovalStatus"] = "A";
                    poAd.ChangeApproveSwitch(true, out s1, poDs);
                    poAd.Update(poDs);
                }

                if (isClose)
                {

                    poAd.CloseOrder(ponum);

                }
            EpicorSession.Dispose();
            return "1|ok";



        }
    }
}
