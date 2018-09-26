using Appapi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web;
using System.Data;

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


        //Post:  /api/Receipt/ReceiveCommitWithNonQRCode
        [HttpPost]
        public string ReceiveCommitWithNonQRCode(Receipt Para) //ApiNum: 101
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.ReceiveCommitWithNonQRCode(Para) : throw new HttpResponseException(HttpStatusCode.Forbidden); ;
        }//根据Para提供的参数，打印收货二维码并新增收货流程记录， 并把流程转到第2节点


        //Get:  /api/Receipt/GetSupplierNameBySupplierNo
        public string GetSupplierNameBySupplierNo(string SupplierNo)
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.GetSupplierName(SupplierNo) : throw new HttpResponseException(HttpStatusCode.Forbidden); ;
        }//根据供应商ID，返回供应商名称


        //Post:  /api/Receipt/ReceiveCommitWithQRCode
        [HttpPost]
        public string ReceiveCommitWithQRCode(Receipt Para) //ApiNum: 102
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.ReceiveCommitWithQRCode(Para) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }//根据Para提供的参数，新增收货流程记录或更新指定的收货流程记录，并把收货流程转到第2节点


        //Get:  /api/Receipt/GetRemainsOfReceiveUser
        public static IEnumerable<Receipt> GetRemainsOfReceiveUser()
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.GetRemainsOfReceiveUser() : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }//获取节点1的代办事项
        #endregion


        #region 进料检验接口
        //Get:  /api/Receipt/GetRemainsOfIQCUser
        public IEnumerable<Receipt> GetRemainsOfIQCUser()
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.GetRemainsOfIQCUser() : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }//获取节点2的代办事项


        //Post:  /api/Receipt/IQCCommit
        [HttpPost]
        public string IQCCommit(Receipt para) //ApiNum: 201
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.IQCCommit(para) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }//根据Para提供的参数，更新指定的收货流程记录，并把收货流程转到第3节点
        #endregion


        #region 入库接口
        //Get:  /api/Receipt/GetRemainsOfAcceptUser
        public IEnumerable<Receipt> GetRemainsOfAcceptUser()
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.GetRemainsOfAcceptUser() : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }//获取节点3的代办事项


        //Post:  /api/Receipt/AcceptCommit
        [HttpPost]
        public string AcceptCommit(Receipt para) //ApiNum: 301
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.AcceptCommit(para) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }//根据Para提供的参数，更新指定的收货流程记录，并把收货流程转到第4节点（结束状态）
        #endregion


        //Get:  /api/Receipt/GetNextUserGroup
        public static string GetNextUserGroup()
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.GetNextUserGroup() : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }//返回下个节点可选人员


        //Post:  /api/Receipt/ReturnStatus
        public static string ReturnStatus(dynamic para) //ApiNum: 300(节点3的回退接口) or  ApiNum: 200（节点2的回退接口)
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.ReturnStatus((int)para.ReceiptID, (int)para.Status, (int)para.ReasonID) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }//流程回退到上一个节点


        //Get:  /api/Receipt/GetWarehouse
        public static DataTable GetWarehouse(string partnum) //ApiNum: 300(节点3的回退接口) or  ApiNum: 200（节点2的回退接口)
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.GetWarehouse(partnum) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }//返回该物料可存放的所有仓库号
    }
}
