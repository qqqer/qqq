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
using Newtonsoft.Json;
using System.Windows.Forms;

namespace Appapi.Controllers
{
    public class ReceiptController : ApiController
    {
        #region 登录验证接口
        /// <summary>
        /// web或手机登录
        /// </summary>
        /// <param name="Account">账号与密码键值对json</param>
        /// <returns>bool值</returns>
        //Post:  /api/Receipt/Login
        [System.Web.Http.HttpPost]
        public bool Login(dynamic Account)
        {
            if (CommonRepository.VerifyAccount(Convert.ToString(Account.userid), Convert.ToString(Account.password)))
                return true;

            return false;
        }



        /// <summary>
        /// winform登录
        /// </summary>
        /// <returns>bool值</returns>
        //Post:  /api/Receipt/Login2
        [System.Web.Http.HttpPost]
        public bool Login2() //ApiNum: 10001   winform登录
        {
            StreamReader reader = new StreamReader(HttpContext.Current.Request.InputStream, System.Text.Encoding.Unicode);         
            string[] arr = reader.ReadToEnd().Split(',');

            string userid = arr[0], password = arr[1];

            if (CommonRepository.VerifyAccount(userid, password))
                return true;

            return false;
        }//登录验证



        /// <summary>
        /// 退出登录
        /// </summary>
        //Get:  /api/Receipt/SignOut
        [System.Web.Http.HttpGet]
        public void SignOut() //ApiNum: 10002   退出登录
        {
            CommonRepository.SignOut();
        }
        #endregion



        #region 接收接口
        /// <summary>
        /// 手动收货搜索
        /// </summary>
        /// <param name="Condition">json</param>
        /// <returns>根据Condition 返回所有匹配的待收事项</returns>
        //Post:  /api/Receipt/GetReceivingBasis
        [HttpPost]
        public IEnumerable<Receipt> GetReceivingBasis(Receipt Condition)//ApiNum: 101
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.GetReceivingBasis(Condition) : throw new HttpResponseException(HttpStatusCode.Forbidden); 
        }//根据Condition 返回所有匹配的收货依据



        /// <summary>
        /// 无码收货提交
        /// </summary>
        /// <param name="Para">json</param>
        /// <returns>处理成功或错误提示</returns>
        //Post:  /api/Receipt/ReceiveCommitWithNonQRCode
        [HttpPost]
        public string ReceiveCommitWithNonQRCode(Receipt Para) //ApiNum: 102
        {
            if(HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            string res = ReceiptRepository.ReceiveCommitWithNonQRCode(Para);

            return res == "处理成功" ? res : res + "|102";
        }//根据Para提供的参数，打印收货二维码并新增收货流程记录， 并把流程转到第2节点



        /// <summary>
        /// 有码收货提交
        /// </summary>
        /// <param name="Para">json</param>
        /// <returns>处理成功或错误提示</returns>
        //Post:  /api/Receipt/ReceiveCommitWithQRCode
        [HttpPost]
        public string ReceiveCommitWithQRCode(Receipt Para) //ApiNum: 103
        {
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            string res = ReceiptRepository.ReceiveCommitWithQRCode(Para);

            return res == "处理成功" ? res : res + "|103";
        }//根据Para提供的参数，新增收货流程记录或更新指定的收货流程记录，并把收货流程转到第2节点


        #endregion



        #region 进料检验接口

        /// <summary>
        /// IQC提交，winform接口
        /// </summary>
        /// <returns>处理成功或错误提示</returns>
        //Post:  /api/Receipt/IQCCommit
        [HttpPost]
        public string IQCCommit() //ApiNum: 201     winform
        {
            //MessageBox.Show(HttpContext.Current.Session.SessionID);
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);


            StreamReader reader = new StreamReader(HttpContext.Current.Request.InputStream, System.Text.Encoding.Unicode);
            string json = reader.ReadToEnd();
            Receipt batch = JsonConvert.DeserializeObject<Receipt>(json);
         

            string res = ReceiptRepository.IQCCommit(batch);

            return res == "处理成功" ? res : res + "|201";
        }//根据Para提供的参数，更新指定的收货流程记录，并把收货流程转到第3节点


        /// <summary>
        /// FTP提交IQC报告，上传指定批次的单个IQC文件，winform接口
        /// </summary>
        /// <returns>bool值</returns>
        [HttpPost]
        //Post:  /api/Receipt/UpLoadIQCFile
        public bool UpLoadIQCFile() //ApiNum: 202    winform   上传指定批次的单个IQC文件
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.UploadIQCFile() : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }

        #endregion



        #region 流转

        /// <summary>
        /// 收料转序提交
        /// </summary>
        /// <param name="para">json</param>
        /// <returns>处理成功或错误提示</returns>
        //Post:  /api/Receipt/TransferCommit
        [HttpPost]
        public string TransferCommit(Receipt para)//ApiNum: 301
        {
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            string res = ReceiptRepository.TransferCommit(para);

            return res == "处理成功" ? res : res + "|301";
        }

        #endregion



        #region 入库接口
        /// <summary>
        /// 第四节点入库提交
        /// </summary>
        /// <param name="para">json</param>
        /// <returns>处理成功或错误提示</returns>
        //Post:  /api/Receipt/AcceptCommit
        [HttpPost]
        public string AcceptCommit(Receipt para) //ApiNum: 401
        {
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            string res = ReceiptRepository.AcceptCommit(para);

            return res == "处理成功" ? res : res + "|401";
        }//根据Para提供的参数，更新指定的收货流程记录，并把收货流程转到第4节点（结束状态）
        #endregion



        #region 功能
        /// <summary>
        /// 一选二，二选三，三选四
        /// </summary>
        /// <param name="nextStatus">下个节点号</param>
        /// <param name="company"></param>
        /// <param name="plant"></param>
        /// <param name="id">若是一选二则id传0，其他二种情况传真实的receipt id</param>
        /// <returns></returns>
        //Get:  /api/Receipt/GetNextUserGroup
        [HttpGet]
        public DataTable GetNextUserGroup(long nextStatus, string company, string plant, int id)//ApiNum: 1
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.GetNextUserGroup(0, company, plant,id) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }//返回下个节点可选人员



        /// <summary>
        /// 收料节点回退 web或手机接口
        /// </summary>
        /// <param name="para">json串，其中 para.Status当前节点号, para.ReasonID原因id, para.ReasonRemark备注</param>
        /// <returns>处理成功或错误提示</returns>
        //Post:  /api/Receipt/ReturnStatus
        [HttpPost]
        public string ReturnStatus(dynamic para) //ApiNum: 2 
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.ReturnStatus((int)para.ID, (int)para.Status, (int)para.ReasonID, (string)para.ReasonRemark, 2) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }//流程回退到上一个节点



        /// <summary>
        /// 收料节点回退 winform接口
        /// </summary>
        /// <param name="para">包含当前节点号, 原因id, 备注</param>
        /// <returns>处理成功或错误提示</returns>
        //Post:  /api/Receipt/ReturnStatus2
        [HttpPost]
        public string ReturnStatus2() //ApiNum: 9 winform
        {
            StreamReader reader = new StreamReader(HttpContext.Current.Request.InputStream, System.Text.Encoding.Unicode);
            string ss = reader.ReadToEnd();
            string[] arr = ss.Split(',');

            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.ReturnStatus(int.Parse(arr[0]), int.Parse(arr[1]), int.Parse(arr[2]), arr[3] ,9) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }//流程回退到上一个节点



        /// <summary>
        /// 根据指定的物料编码返回所在的仓库信息
        /// </summary>
        /// <param name="partnum">物料编码</param>
        /// <returns>返回该物料可存放的所有仓库编号和描述</returns>
        //Get:  /api/Receipt/GetWarehouse
        [HttpGet]
        public DataTable GetWarehouse(string partnum) //ApiNum: 3 
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.GetWarehouse(partnum) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }//返回该物料可存放的所有仓库号



        /// <summary>
        /// 替换原始二维码里的内容，用~替换%作为分隔符
        /// </summary>
        /// <param name="values">原始二维码内容</param>
        /// <returns>末尾追加了工厂值和供应商名和单位 并且用~替换%作为分隔符</returns>
        [HttpGet]
        //Get:  /api/Receipt/ParseQRValues
        public string ParseQRValues(string values)//ApiNum: 4
        {        
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.ParseQRValues(values) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }//values末尾追加了工厂值和供应商名， 并且用~替换%作为分隔符




        /// <summary>
        /// 获取当前用户的待办事项
        /// </summary>
        /// <returns>返回当前用户的所有待办事项</returns>
        [HttpGet]
        //Get:  /api/Receipt/GetRemainsOfUser
        public IEnumerable<Receipt> GetRemainsOfUser()//ApiNum: 5   获取当前用户的待办事项
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.GetRemainsOfUser() : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }



        /// <summary>
        /// 从receipt表中获取ID指定的记录行
        /// </summary>
        /// <param name="ReceiptID">Receipt ID</param>
        /// <returns>返回该记录的所有字段值</returns>
        [HttpGet]
        //Get:  /api/Receipt/GetRecordByID
        public DataTable GetRecordByID(int ReceiptID)//ApiNum: 6   从receipt表中获取ID指定的记录行
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.GetRecordByID(ReceiptID) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }



        /// <summary>
        /// web或手机端接口   按条件从receipt表中获取的记录集合 可选条件：ponum ，poline partnum  PartDesc  BatchNo  SupplierNo  IsRestrictRcv（true表示非打印批次）
        /// </summary>
        /// <param name="con">Receipt类</param>
        /// <returns>返回所有符合指定条件的记录，且按第一节点提交时间倒序</returns>
        [HttpPost]
        //Post:  /api/Receipt/GetRecordByCondition
        public IEnumerable<Receipt> GetRecordByCondition(Receipt con)//ApiNum: 10   按条件从receipt表中获取的记录集合
        {
            return ReceiptRepository.GetRecordByCondition(con);
        }


        /// <summary>
        ///  winform接口 功能和 /api/Receipt/GetRecordByCondition 相同
        /// </summary>
        /// <returns>返回所有符合指定条件的记录，且按第一节点提交时间倒序</returns>
        [HttpPost]
        //Post:  /api/Receipt/GetRecordByCondition2
        public IEnumerable<Receipt> GetRecordByCondition2()//ApiNum: 13   winform     按条件从receipt表中获取的记录集合
        {
            StreamReader reader = new StreamReader(HttpContext.Current.Request.InputStream, System.Text.Encoding.Unicode);
            string json = reader.ReadToEnd();
            Receipt con = JsonConvert.DeserializeObject<Receipt>(json);

            return ReceiptRepository.GetRecordByCondition(con);
        }


        /// <summary>
        /// 获取指定批次的IQC文件
        /// </summary>
        /// <param name="batchno">批次号</param>
        /// <returns>返回指定批次的所有IQC文件</returns>
        [HttpGet]
        //Get:  /api/Receipt/GetFileList
        public DataTable GetFileList(string batchno) //ApiNum: 14    winform    获取指定批次的IQC文件列表
        {
            return ReceiptRepository.GetFileList(batchno);
        }


        /// <summary>
        /// 删除指定批次的单个IQC文件
        /// </summary>
        /// <param name="id">IQCFile id</param>
        /// <param name="filepath">IQCFile FilePath</param>
        /// <param name="filename">IQCFile FileName</param>
        /// <returns>bool值</returns>
        [HttpGet]
        //Post:  /api/Receipt/DeleteIQCFile
        public bool DeleteIQCFile(int id, string filepath, string filename) //ApiNum: 15   winform    删除指定批次的单个IQC文件
        {
            return ReceiptRepository.DeleteIQCFile(id,filepath,filename);
        }


        /// <summary>
        /// 库存转库取数接口
        /// </summary>
        /// <param name="oristr">company~partnum</param>
        /// <returns>返回partnum~partdesc~onhandqty~company~dimcode</returns>
        [HttpGet]
        //Get:  /api/Receipt/GetValueForTranStk_1
        public string GetValueForTranStk_1(string oristr) //ApiNum: 16  获取 partnum~partdesc~onhandqty~company~dimcode
        {
            return ReceiptRepository.GetValueForTranStk_1(oristr);
        }


        /// <summary>
        /// 库存转库取数接口
        /// </summary>
        /// <param name="para">必填条件para.Company， para.WarehouseCode，可选条件 para.BinNum， para.LotNum</param>
        /// <returns>返回所有符合条件的记录的BinNum, LotNum, OnhandQty字段</returns>
        [HttpPost]
        //Post:  /api/Receipt/GetValueForTranStk_2
        public DataTable GetValueForTranStk_2(dynamic para) //ApiNum: 17  获取 BinNum列表 或LotNum列表 或 指定行的Onhand数量
        {
            return ReceiptRepository.GetValueForTranStk_2(para);
        }



        /// <summary>
        /// 库存转库提交接口
        /// </summary>
        /// <param name="para">para.PartNum，para.PartDesc，para.Warehouse（原来仓库），para.BinNum（原来库位），para.ToBinNum，para.LotNum，para.ToQty（转的数量），para.uom（单位）</param>
        /// <returns>处理成功或错误提示</returns>
        [HttpPost]
        //Post:  /api/Receipt/TranStk
        public string TranStk(dynamic para) //ApiNum: 18  转仓
        {
            string res = ReceiptRepository.TranStk(para);

            return res == "处理成功" ? res : res + "|18";
        }



        /// <summary>
        /// 检测版本号，若参数的值和数据库里的版本号字段的值相等则返回true否则false
        /// </summary>
        /// <param name="version">版本号</param>
        /// <returns>bool值</returns>
        [HttpGet]
        //Get:  /api/Receipt/CheckVersion
        public bool CheckVersion(string version)//ApiNum: 19   检测版本号
        {
            return CommonRepository.CheckVersion(version);
        }



        /// <summary>
        /// winform 接口 IQC强制全退 强制结束该在跑批次 成功返回true失败则false
        /// </summary>
        /// <returns>bool值</returns>
        [HttpPost]
        //Post:  /api/Receipt/ForceComplete
        public bool ForceComplete()//ApiNum: 20   强制全退，结束该在跑批次
        {
            if (HttpContext.Current.Session.Count == 0)
                throw new HttpResponseException(HttpStatusCode.Forbidden);


            StreamReader reader = new StreamReader(HttpContext.Current.Request.InputStream, System.Text.Encoding.Unicode);
            string json = reader.ReadToEnd();
            Receipt IQCInfo = JsonConvert.DeserializeObject<Receipt>(json);

                                                                                                                                                                                                                                
            return ReceiptRepository.ForceComplete(IQCInfo);
        }



        /// <summary>
        /// 获取物料的仓库记录
        /// </summary>
        /// <param name="partnum">物料编码</param>
        /// <returns>返该物料有关的erp.partbin记录</returns>
        [HttpGet]
        //Get:  /api/Receipt/GetPartRecords
        public  DataTable GetPartRecords(string partnum)//ApiNum: 21   获取物料在erp.partbin表中的信息
        {
            return ReceiptRepository.GetPartRecords(partnum);
        }



        /// <summary>
        /// 获取所有连续委外工序的描述
        /// </summary>
        /// <param name="PoNum"></param>
        /// <param name="JobNum"></param>
        /// <param name="AssemblySeq"></param>
        /// <param name="Company"></param>
        /// <returns>返回符合参数条件的连续委外工序的描述</returns>
        [HttpGet]
        //Get:  /api/Receipt/GetAllCommentTextOfSeriesSUB
        public DataTable GetAllCommentTextOfSeriesSUB(int PoNum, string JobNum, int AssemblySeq, string Company)//ApiNum: 22   winform   获取所有连续委外工序的描述
        {
            return ReceiptRepository.GetAllCommentTextOfSeriesSUB(PoNum, JobNum, AssemblySeq, Company);
        }



        /// <summary>
        /// 打印二维码
        /// </summary>
        /// <param name="info">要成为二维码的内容的键值对</param>
        /// <returns>处理成功或错误提示</returns>
        [HttpPost]
        //Post:  /api/Receipt/PrintQR
        public string PrintQR(Receipt info)//ApiNum: 11   打印二维码
        {
            string res = ReceiptRepository.PrintQR(info);
            return res == "处理成功" ? res : res + "|11"; 
        }



        /// <summary>
        /// 设置暂收单为打印状态
        /// </summary>
        /// <param name="ID">Receipt id</param>
        /// <returns>bool值</returns>
        [HttpGet]
        //Get:  /api/Receipt/SetIsPrintRcv
        public bool SetIsPrintRcv(int ID)//ApiNum: 12   设置暂收单是否已打印
        {
            return ReceiptRepository.SetIsPrintRcv(ID);
        }



        /// <summary>
        /// 扫码获取收料待办事项
        /// </summary>
        /// <param name="values">二维码字符串</param>
        /// <param name="IsForPrintQR">指示本次扫码是为了重新打印该二维码（true）还是获取待办事项（false）</param>
        /// <returns>返回ScanResult对象， 若成功则ScanResult.error为null，ScanResult.batch不为null， 否则ScanResult.error != null，ScanResult.batch为null</returns>
        [HttpGet]
        //Get:  /api/Receipt/GetRecordByQR
        public ScanResult GetRecordByQR(string values, bool IsForPrintQR) //ApiNum: 7
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.GetRecordByQR(values, IsForPrintQR) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }


        /// <summary>
        /// 获取APP.Reason表里的所有记录
        /// </summary>
        /// <returns>返回APP.Reason表里的所有记录</returns>
        //Get:  /api/Receipt/GetReason
        [HttpGet]
        public IEnumerable<Reason> GetReason()//ApiNum: 8
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.GetReason() : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }

        //Get:  /api/Receipt/GetShipDetailListByPreparationNum
        [HttpGet]
        public DataTable GetShipDetailListByPreparationNum(string PreparationNum)
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.GetShipDetailListByPreparationNum(PreparationNum) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }

        //Get:  /api/Receipt/IsPOPartNumAndQRPartNumEqual
        [HttpGet]
        public bool IsPOPartNumAndQRPartNumEqual(int ID, string QR)
        {
            return HttpContext.Current.Session.Count != 0 ? ReceiptRepository.IsPOPartNumAndQRPartNumEqual(ID,QR) : throw new HttpResponseException(HttpStatusCode.Forbidden);
        }
        #endregion





        #region test
        [HttpPost]
        //Post:  /api/Receipt/uuu
        public Receipt uuu(Receipt info)//ApiNum: 11   打印二维码
        {
            //string res = ReceiptRepository.PrintQR(info);
            return info;
        }
        #endregion

    }
}
