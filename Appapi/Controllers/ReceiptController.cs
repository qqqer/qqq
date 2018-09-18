using Appapi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web;

namespace Appapi.Controllers
{
    public class ReceiptController : ApiController
    {
        #region 登录验证接口
        //Post:  /api/Receipt/Login
        [HttpPost]
        public bool Login(dynamic Account)
        {
            bool isvalid = true; // = 账号认证接口（）
            
            if (isvalid)
            {
                HttpContext.Current.Session.Add("Company", Convert.ToString(Account.company));
                HttpContext.Current.Session.Add("Plant", Convert.ToString(Account.plant));
                HttpContext.Current.Session.Add("UserId", Convert.ToString(Account.userid));
                HttpContext.Current.Session.Add("UserPrinter", Convert.ToString(Account.userprinter));
            }

            return isvalid;
        }//登录验证
        #endregion

        #region 收货接口
        //Post:  /api/Receipt/GetPOByCondition
        [HttpPost]
        public IEnumerable<Receipt> GetPOByCondition(Receipt Condition)
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.GetPO(Condition) : throw new HttpResponseException(HttpStatusCode.Forbidden); ;
        }//根据Condition 返回匹配的采购单信息

        //Post:  /api/Receipt/PrintQRCodeByPO
        [HttpPost]
        public bool PrintQRCodeByPO(Receipt Para)
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.PrintQRCode(Para) : throw new HttpResponseException(HttpStatusCode.Forbidden); ;
        }//根据Para提供的参数，打印收货二维码

        //Get:  /api/Receipt/GetSupplierNameBySupplierNo
        public string GetSupplierNameBySupplierNo(string SupplierNo)
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.GetSupplierName(SupplierNo) : throw new HttpResponseException(HttpStatusCode.Forbidden); ;
        }//根据供应商ID，返回供应商名称

        //Post:  /api/Receipt/IsOverReceived
        [HttpPost]
        public bool CheckOverReceived(Receipt Para)
        {
            //throw new HttpResponseException(HttpStatusCode.Forbidden);
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.IsOverReceived(Para) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }//根据Para提供的参数,检测是否超收
        #endregion

        #region IQC接口
        //Get:  /api/Receipt/GetIQCmessage
        public IEnumerable<Receipt> GetIQCMessage()
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.GetIQCMessage() : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }//获取IQC信息

        //Post:  /api/Receipt/UpdateIQCAmount
        [HttpPost]
        public bool UpdateIQCAmount(Receipt para)
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.UpdateIQCAmount(para) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }//更新IQC数量
        #endregion

        #region 入库接口
        //Get:  /api/Receipt/GetACTMessage
        public IEnumerable<Receipt> GetACTMessage()
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.GetACTMessage() : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }//获取入库信息

        //Post:  /api/Receipt/UpdateACTInfo
        [HttpPost]
        public bool UpdateACTInfo(Receipt para)
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.UpdateACTInfo(para) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }//更新入库信息
        #endregion
    }
}
