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
        public string Login(dynamic Account)
        {
            bool isvalid = false; // = 账号认证接口（）
            
            if (isvalid)
            {
                HttpContext.Current.Session.Add("Company", Convert.ToString(Account.company));
                HttpContext.Current.Session.Add("Plant", Convert.ToString(Account.plant));
                HttpContext.Current.Session.Add("UserId", Convert.ToString(Account.userid));
                HttpContext.Current.Session.Add("UserPrinter", Convert.ToString(Account.userprinter));
            }

            return Convert.ToString(Account.company);
        }//登录验证
        #endregion
        
        #region 接收接口
        //Post:  /api/Receipt/GetPOByCondition
        [HttpPost]
        public IEnumerable<Receipt> GetPOByCondition(Receipt Condition)
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.GetPO(Condition) : throw new HttpResponseException(HttpStatusCode.Forbidden); ;
        }//根据Condition 返回匹配的采购单信息

        //Post:  /api/Receipt/PrintQRCodeByPO
        [HttpPost]
        public string FirstCommitWithNonQRCode(Receipt Para)
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.ReceiveCommitWithNonQRCode(Para) : throw new HttpResponseException(HttpStatusCode.Forbidden); ;
        }//根据Para提供的参数，打印收货二维码

        //Get:  /api/Receipt/GetSupplierNameBySupplierNo
        public string GetSupplierNameBySupplierNo(string SupplierNo)
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.GetSupplierName(SupplierNo) : throw new HttpResponseException(HttpStatusCode.Forbidden); ;
        }//根据供应商ID，返回供应商名称

        //Post:  /api/Receipt/IsOverReceived
        [HttpPost]
        public string FirstCommitWithQRCode(Receipt Para)
        {
            //throw new HttpResponseException(HttpStatusCode.Forbidden);
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.ReceiveCommitWithQRCode(Para) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }//根据Para提供的参数,检测是否超收
        #endregion

        #region 进料检验接口
        //Get:  /api/Receipt/GetIQCmessage
        public IEnumerable<Receipt> GetRemainsOfSecondUser()
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.GetRemainsOfIQCUser() : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }//获取IQC信息

        //Post:  /api/Receipt/UpdateIQCAmount
        [HttpPost]
        public string SecondCommit(Receipt para)
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.IQCCommit(para) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }//更新IQC数量
        #endregion

        #region 入库接口
        //Get:  /api/Receipt/GetACTMessage
        public IEnumerable<Receipt> GetRemainsOfThirdUser()
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.GetRemainsOfAcceptUser() : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }//获取入库信息

        //Post:  /api/Receipt/UpdateACTInfo
        [HttpPost]
        public string ThirdCommit(Receipt para)
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.AcceptCommit(para) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }//更新入库信息
        #endregion

        //Get:  /api/Receipt/GetNextUserGroup
        public static string GetNextUserGroup()
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.GetNextUserGroup() : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }//返回下个节点可选人员
    }
}
