using Appapi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web;
using System.Data;
using System.Web.Services;
using System.IO;

namespace Appapi.Controllers
{
    public class ReceiptController : ApiController
    {
        #region 登录验证接口
        //Post:  /api/Receipt/Login
        [System.Web.Http.HttpPost]
        public bool Login(dynamic Account) //ApiNum: 0
        {
            string OpDetail = "", OpDate = DateTime.Now.ToString();

            string sql = "select * from Userfile where userid = '" + Convert.ToString(Account.userid) + "' and disabled = 0";
            DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql);
            bool isvalid = dt != null ? true : false;   // = 账号认证接口（）

            if (isvalid)
            {
                HttpContext.Current.Session.Add("Company", Convert.ToString(dt.Rows[0]["Company"]));
                HttpContext.Current.Session.Add("Plant", Convert.ToString(dt.Rows[0]["Plant"]));
                HttpContext.Current.Session.Add("UserId", Convert.ToString(Account.userid));
                HttpContext.Current.Session.Add("RoleId", Convert.ToInt32(dt.Rows[0]["RoleId"]));
                //HttpContext.Current.Session.Add("UserPrinter", Convert.ToString(Account.userprinter));

                //ReceiptRepository.AddOpLog(-1, 123, "login", OpDate, OpDetail);

                return true;
            }

            return false;
        }//登录验证
        #endregion



        #region 接收接口
        //Post:  /api/Receipt/GetReceivingBasis
        [HttpPost]
        public IEnumerable<Receipt> GetReceivingBasis(Receipt Condition)//ApiNum: 101
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.GetReceivingBasis(Condition) : throw new HttpResponseException(HttpStatusCode.Forbidden); ;
        }//根据Condition 返回所有匹配的收货依据


        //Post:  /api/Receipt/ReceiveCommitWithNonQRCode
        [HttpPost]
        public string ReceiveCommitWithNonQRCode(Receipt Para) //ApiNum: 102
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.ReceiveCommitWithNonQRCode(Para) : throw new HttpResponseException(HttpStatusCode.Forbidden); ;
        }//根据Para提供的参数，打印收货二维码并新增收货流程记录， 并把流程转到第2节点



        //Post:  /api/Receipt/ReceiveCommitWithQRCode
        [HttpPost]
        public string ReceiveCommitWithQRCode(Receipt Para) //ApiNum: 103
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.ReceiveCommitWithQRCode(Para) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }//根据Para提供的参数，新增收货流程记录或更新指定的收货流程记录，并把收货流程转到第2节点


        #endregion



        #region 进料检验接口
       
        //Post:  /api/Receipt/IQCCommit
        [HttpPost]
        public string IQCCommit(Receipt para) //ApiNum: 201
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.IQCCommit(para) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }//根据Para提供的参数，更新指定的收货流程记录，并把收货流程转到第3节点


        [HttpPost]
        //Post:  /api/Receipt/UpLoadIQCFile
        public bool UpLoadIQCFile() //ApiNum: 202
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.UploadIQCFile() : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }

        #endregion

        

        #region 流转

        //Post:  /api/Receipt/TransferCommit
        [HttpPost]
        public string TransferCommit(Receipt para)//ApiNum: 301
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.TransferCommit(para) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }

        #endregion



        #region 入库接口
        //Post:  /api/Receipt/AcceptCommit
        [HttpPost]
        public string AcceptCommit(Receipt para) //ApiNum: 401
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.AcceptCommit(para) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }//根据Para提供的参数，更新指定的收货流程记录，并把收货流程转到第4节点（结束状态）
        #endregion



        #region 功能
        //Get:  /api/Receipt/GetNextUserGroup
        [HttpGet]
        public string GetNextUserGroup(int nextStatus, string company, string plant)//ApiNum: 1
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.GetNextUserGroup(nextStatus, company, plant) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }//返回下个节点可选人员
        

        //Post:  /api/Receipt/ReturnStatus
        [HttpPost]
        public string ReturnStatus(dynamic para) //ApiNum: 2 
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.ReturnStatus((int)para.ID, (int)para.Status, (int)para.ReasonID) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }//流程回退到上一个节点


        //Get:  /api/Receipt/GetWarehouse
        [HttpGet]
        public DataTable GetWarehouse(string partnum) //ApiNum: 3 
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.GetWarehouse(partnum) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }//返回该物料可存放的所有仓库号



        [HttpGet]
        //Get:  /api/Receipt/ParseQRValues
        public string ParseQRValues(string values)//ApiNum: 4
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.ParseQRValues(values) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }//values末尾追加了工厂值和供应商名， 并且用~替换%作为分隔符


        [HttpGet]
        //Get:  /api/Receipt/GetRemainsOfUser
        public IEnumerable<Receipt> GetRemainsOfUser()//ApiNum: 5
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.GetRemainsOfUser() : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }
        #endregion





        #region test


        #endregion

    }
}
