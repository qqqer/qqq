using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Collections;
using System.Timers;
using System.Net; //ftp
using System.Threading;
using System.Security.Cryptography;
using System.Text;
using System.Drawing.Imaging;
using System.Drawing;
using System.Data.SqlClient;

namespace Appapi.Models
{
    public static class ReceiptRepository
    {
        private static readonly object PrintSeriesNumLock = new object();

        #region  重用函数（非接口）

        private static string ConstructRcvdtlStr(string[] array)//生成RcvdtlStr中的单个 json串 {...}
        {
            string rcvdtlStr = "'ponum':'{0}', " +
                             "'poline':'{1}', " +
                             "'porel':'{2}',  " +
                             "'partnum':'{3}'," +
                             "'recqty':'{4}'," +
                             "'pum':'{5}'," +
                             "'warehousecode':'{6}'," +
                             "'binnum':'{7}'," +
                             "'lotnum':'{8}'," +
                             "'jobnum':'{9}'," +
                             "'assemblyseq':'{10}'," +
                             "'jobseq':'{11}'," +
                             "'commenttext':'{12}'," +
                             "'ordertype':'{13}', " +
                             "'HeatNum':'{14}'";
            rcvdtlStr = string.Format(rcvdtlStr,array[0],array[1],array[2],array[3], array[4],array[5],array[6],array[7],array[8],array[9],array[10],array[11],array[12],array[13], array[14]);
            rcvdtlStr = "{" + rcvdtlStr + "}";

            return rcvdtlStr;
        }
   

        private static int GetLastOpSeqOfSeriesSUB(Receipt theBatch)//取出该订单中的连续委外工序中（包括当前处理的批次工序）最后一道的工序号
        {
            DataTable dt = GetAllOpSeqOfSeriesSUB(theBatch);
            return (int)dt.Rows[dt.Rows.Count - 1]["jobseq"];
        }



        private static DataTable GetAllOpSeqOfSeriesSUB(Receipt theBatch) //取出该订单中的连续委外工序（包括当前处理的批次工序）的工序号、poline、porelnum、工序描述、工序代码
        {
            string sql = @"  Select jobseq,PartNum, Description, poline,porelnum ,OpDesc,OpCode, pr.company,pr.ponum from erp.porel pr 
                          left join erp.JobOper jo on pr.jobnum = jo.JobNum and pr.AssemblySeq = jo.AssemblySeq and pr.Company = jo.Company and jobseq = jo.OprSeq 
                          where pr.ponum={0} and pr.jobnum = '{1}'  and pr.assemblyseq={2} and trantype='PUR-SUB' and pr.company = '{3}' order by jobseq  asc";
            sql = string.Format(sql, theBatch.PoNum, theBatch.JobNum, theBatch.AssemblySeq, theBatch.Company);
            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);

            if (dt != null)
            {
                for (int i = dt.Rows.Count - 1; i >= 0; i--)//排除连续委外表中重复的工序号
                {
                    if ((int)dt.Rows[i]["jobseq"] == theBatch.JobSeq && ((int)dt.Rows[i]["poline"] != theBatch.PoLine || (int)dt.Rows[i]["porelnum"] != theBatch.PORelNum))
                        dt.Rows.RemoveAt(i);
                }
            }

            return dt;
        }



        private static long GetNextRole(int id)
        {
            long nextRole = 1152921504606846976;//2^60

            string sql = "select * from receipt where id = " + id + "";
            var t = CommonRepository.DataTableToList<Receipt>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql));
            Receipt theBatch = t?.First(); //获取该批次记录

            int nextStatsu = (theBatch != null ? (int)theBatch.Status : 1) + 1; 


            if (nextStatsu == 2)
            {
                nextRole = 2;
            }

            else if (nextStatsu == 3)
            {
                nextRole = 4;
            }

            else if (nextStatsu == 4)
            {
                if (theBatch.TranType == "PUR-STK")
                    nextRole = 8;
                else if (theBatch.TranType == "PUR-SUB")
                {
                    int a, b;//凑个数，无意义
                    string c;//凑个数，无意义
                    string OpCode, res;
                    int jobseq = GetLastOpSeqOfSeriesSUB(theBatch);

                    res = ErpAPI.CommonRepository.getJobNextOprTypes(theBatch.JobNum, (int)theBatch.AssemblySeq, jobseq, out a, out b, out OpCode, out c,theBatch.Company);

                    if (res.Substring(0, 1).Trim().ToLower() == "p") //由仓库接收人员处理 设置8
                        nextRole = 8;
                    else if (res.Substring(0, 1).Trim().ToLower() == "s")//外协
                        nextRole = 16;
                    else if (res.Substring(0, 1).Trim().ToLower() == "m")//场内
                        nextRole = 64;
                }
                else if (theBatch.TranType == "PUR-UKN")
                    nextRole = 32;
            }

            return nextRole;
        }


        /// <summary>
        /// 获取该批次号在Receipt表中的ID
        /// </summary>
        /// <param name="batInfo"></param>
        /// <returns></returns>
        private static int GetReceiptID(Receipt batInfo)
        {
            string sql = "select ID from Receipt where PoNum = {0} and  PoLine = {1} and PORelNum = {2} and Company = '{3}' and  BatchNo = '{4}' ";
            sql = string.Format(sql, batInfo.PoNum, batInfo.PoLine, batInfo.PORelNum, batInfo.Company, batInfo.BatchNo);

            object o = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
            return o == null ? -1 : (int)o;
        }


        /// <summary>
        /// 从给定的待办批次列表中筛选出有效的待办批次
        /// </summary>
        /// <param name="batchs"></param>
        /// <returns></returns>
        private static List<Receipt> GetValidBatchs(List<Receipt> batchs)
        {
            if (batchs == null)
                return null;

            string[] errs = new string[500];
            for (int i = batchs.Count - 1, j = 0; i >= 0; i--)
            {
                if (GetReceivingBasis(batchs[i]) == null) //若当前待办批次所属的收货依据无效则去掉该待办批次
                {
                    errs[j++] = GetErrorInfo(batchs[i]) + batchs[i].ID;
                    batchs.RemoveAt(i);
                }
            }//筛选待办批次。

            return batchs.Count > 0 ? batchs : null;
        }



        private static void SetReturnReason(Receipt theBatch)
        {
            string sql = "";

            if (theBatch.Status == 1)
            {
                //只可能是红色
                sql = @"select ReasonDesc, ReasonRemark from ReasonRecord rr left join  Reason on rr.ReturnReasonId = Reason.id  
                        where BatchNo = '" + theBatch.BatchNo + "' and ReturnOne = " + theBatch.ReturnOne + "";
            }
            else if (theBatch.Status == 2)
            {
                if (theBatch.Colour == 1)//红色
                    sql = @"select ReasonDesc, ReasonRemark from ReasonRecord rr left join  Reason on rr.ReturnReasonId = Reason.id  
                            where BatchNo = '" + theBatch.BatchNo + "' and ReturnTwo = " + theBatch.ReturnTwo + "";
                else if (theBatch.Colour == 2)//黄色
                    sql = @"select ReasonDesc, ReasonRemark from ReasonRecord rr left join  Reason on rr.ReturnReasonId = Reason.id  
                            where BatchNo = '" + theBatch.BatchNo + "' and ReturnOne = " + theBatch.ReturnOne + "";
            }
            else if (theBatch.Status == 3)
            {
                if (theBatch.Colour == 1)//红色
                    sql = @"select ReasonDesc, ReasonRemark from ReasonRecord rr left join  Reason on rr.ReturnReasonId = Reason.id  
                            where BatchNo = '" + theBatch.BatchNo + "' and ReturnThree = " + theBatch.ReturnThree + "";
                else if (theBatch.Colour == 2)//黄色
                    sql = @"select ReasonDesc, ReasonRemark from ReasonRecord rr left join  Reason on rr.ReturnReasonId = Reason.id  
                            where BatchNo = '" + theBatch.BatchNo + "' and ReturnTwo = " + theBatch.ReturnTwo + "";
            }
            else if (theBatch.Status == 4)
            {
                //只可能是黄色
                sql = @"select ReasonDesc, ReasonRemark from ReasonRecord rr left join  Reason on rr.ReturnReasonId = Reason.id  
                        where BatchNo = '" + theBatch.BatchNo + "' and ReturnThree = " + theBatch.ReturnThree + "";
            }

            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql);

            theBatch.ReturnReason = dt.Rows[0]["ReasonDesc"].ToString();
            theBatch.ReturnReasonRemark = dt.Rows[0]["ReasonRemark"].ToString();
        }



        private static string CheckBinNum(Receipt AcceptInfo)
        {
            //统计库位中的'-'字符数
            int c = 0;
            foreach (var i in AcceptInfo.BinNum)
            {
                if (i == '-') c++;
            }
            if (c < 2)
                return "错误：库位格式不正确";


            string zoneid = AcceptInfo.BinNum.Substring(0, AcceptInfo.BinNum.IndexOf('-'));
            string binnum = AcceptInfo.BinNum.Substring(AcceptInfo.BinNum.IndexOf('-') + 1);

            string sql = "select count(*) from erp.WhseBin where Company = '" + AcceptInfo.Company + "' and  WarehouseCode = '{0}' and BinNum = '{1}' and zoneid = '{2}'";
            sql = string.Format(sql, AcceptInfo.Warehouse, binnum, zoneid);
            int exist = (int)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);

            return exist == 0 ? "错误：库位与仓库不匹配" : "ok";
        }



        private static void Return(int previousStatus, string returnNum, string batchno, string OpDate, int ReasonID, string remark, int AtRole)//回退设置原因和状态值
        {
            //更新该批次的status为上一个节点值，指定的回退编号次数+1
            string sql = @"update Receipt set PreStatus = " + (previousStatus + 1) + ", AtRole = " + AtRole + ", status = " + previousStatus + ", " + returnNum + " = " + returnNum + "+1  where batchno = '" + batchno + "' ";
            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

            //获取更新后的回退编号
            sql = "select " + returnNum + " from  Receipt  where batchno = '" + batchno + "' ";
            int c = (int)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

            //把该回退编号的原因插入到ReasonRecord表中
            sql = @"insert into ReasonRecord(batchno, " + returnNum + ", ReturnReasonId, ReasonRemark, Date) Values('" + batchno + "', " + c + ", " + ReasonID + ",'" + remark + "', '" + OpDate + "')";
            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
        }



        private static int GetColour(Receipt theBatch)
        {
            if (theBatch.Status == 1 && (int)theBatch.PreStatus == 2)
            {
                return 1;//红色      
            }

            else if (theBatch.Status == 2)
            {
                if (theBatch.PreStatus == 3)
                    return 1;
                else if (Convert.ToInt32(theBatch.ReturnOne) > 0 && (int)theBatch.PreStatus == 1)
                    return 2; //黄色    
                else
                    return 0;//白色
            }

            else if (theBatch.Status == 3)
            {
                if (theBatch.PreStatus == 4)
                    return 1;
                else if (Convert.ToInt32(theBatch.ReturnTwo) > 0 && theBatch.PreStatus == 2)
                    return 2; //黄色
                else
                    return 0;//白色
            }

            else if (theBatch.Status == 4 && Convert.ToInt32(theBatch.ReturnThree) > 0 && theBatch.PreStatus == 3)
            {
                return 2; //黄色
            }

            else
                return 0;//白色
        }



        private static decimal GetRunningQtyOfJobSeq(string jobnum, int asmSeq, int oprseq, int id) //获取本次收货工序的在跑数量, 不包括本次收货数量
        {
            //不能锁定ponum poline porel
            string sql = "select sum(case when ArrivedQty is null then(case when  ReceiveQty2 is null then ReceiveQty1 else ReceiveQty2 end) else ArrivedQty end) from Receipt " +
            "where isdelete != 1 and isComplete != 1 and  jobnum = '" + jobnum + "' and AssemblySeq = " + asmSeq + " and  JobSeq = " + oprseq + " and id != " + id + "";
            object sum = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

            return sum is DBNull || sum == null ? 0 : (decimal)sum;
        }


        private static decimal GetTotalQtyOfJobSeq(string jobnum, int asmSeq, int oprseq, int id) //该工序的 在跑+erp 数量, 不包括本次收货数量
        {
            decimal bpm_qty = GetRunningQtyOfJobSeq(jobnum, asmSeq, oprseq, id);
            decimal erp_qty = CommonRepository.GetOpSeqCompleteQty(jobnum, asmSeq, oprseq);

            return bpm_qty + erp_qty;
        }


        private static string GetErrorInfo(Receipt batInfo)
        {
            if (HttpContext.Current.Session["Company"].ToString().Contains(batInfo.Company) == false)
                return "无权处理" + batInfo.Company + "公司的批次";
            if (HttpContext.Current.Session["Plant"].ToString().Contains(batInfo.Plant) == false)
                return "无权处理" + batInfo.Plant + "工厂的批次";


            string sql = @"select 
                ph.OpenOrder,
                ph.orderHeld,
                ph.Approve,
                ph.Confirmed,
                pd.openLine,
                pd.PartNum,
                pr.openRelease,
                pr.TranType,
                jh.jobClosed,
                jh.jobComplete,
                jh.JobHeld,
               (pr.XRelQty-pr.ArrivedQty) NeedReceiptQty,
                jh.JobEngineered,
                jh.JobReleased
                from erp.PORel pr

                left join erp.PODetail pd   on pr.PONum = pd.PONUM   and   pr.Company = pd.Company   and   pr.POLine = pd.POLine 
                left join erp.POHeader ph   on ph.Company = pd.Company   and   ph.PONum = pd.PONUM 
                left join erp.JobHead jh  on pr.JobNum = jh.JobNum   and   pr.Company = jh.Company 

                where pr.Company = '" + batInfo.Company + "'   and    pr.Plant = '" + batInfo.Plant + "' " +
                "and  pr.PONum = " + batInfo.PoNum + "   and    pr.POLine = " + batInfo.PoLine + "    and    pr.PORelNum = " + batInfo.PORelNum + " ";

            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);


            if (dt == null)
                return "该订单项目不存在";


            string append = batInfo.BatchNo != null ? "and  batchno != '" + batInfo.BatchNo + "'" : ""; //若batInfo.BatchNo 不为空，则排除该批次的在跑数量      
            sql = "select sum(case when ArrivedQty is null then(case when  ReceiveQty2 is null then ReceiveQty1 else ReceiveQty2 end) else ArrivedQty end) from Receipt " +
                "where isdelete != 1 and isComplete != 1 and  ponum = " + (int)batInfo.PoNum + " and poline = " + (int)batInfo.PoLine + " and  PORelNum = " + (int)batInfo.PORelNum + " and company = '" + batInfo.Company + "' and plant = '" + batInfo.Plant + "'  " + append;
            object sum = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
            decimal NotReceiptQty = (decimal)dt.Rows[0]["NeedReceiptQty"] - (sum is DBNull || sum == null ? 0 : (decimal)sum);

            if (NotReceiptQty <= 0)
                return "剩余可收数量：" + NotReceiptQty.ToString("N2") + " ";
            else if (batInfo.PartNum != dt.Rows[0]["PartNum"].ToString())
                return "物料编码不正确";
            else if ((bool)dt.Rows[0]["OpenOrder"] == false)
                return "订单已关闭";
            else if ((bool)dt.Rows[0]["orderHeld"] == true)
                return "订单已冻结";
            else if ((bool)dt.Rows[0]["Approve"] == false)
                return "订单未同意";
            else if ((bool)dt.Rows[0]["Confirmed"] == false)
                return "订单未确认";
            else if ((bool)dt.Rows[0]["openLine"] == false)
                return "订单行已关闭";
            else if ((bool)dt.Rows[0]["openRelease"] == false)
                return "发货行已关闭";
            else if ((string)dt.Rows[0]["TranType"] != "PUR-UKN" && (string)dt.Rows[0]["TranType"] != "PUR-STK") //是外协或工单物料， 需要判断与之关联的工单状态
            {
                if ((bool)dt.Rows[0]["jobClosed"] == true)
                    return "该工单已关闭";
                else if ((bool)dt.Rows[0]["jobComplete"] == true)
                    return "该工单已完成";
                else if ((bool)dt.Rows[0]["JobEngineered"] == false)
                    return "该工单未设计";
                else if ((bool)dt.Rows[0]["JobReleased"] == false)
                    return "该工单未发放";
            }

            return "其他错误，请联系管理员";
        }



        private static string GetCommentText(int ponum, int poline, string company)
        {
            string sql = @"select  pd.CommentText  from erp.PODetail pd  where pd.ponum = {0} and pd.poline = {1} and pd.company = '{2}'";
            sql = string.Format(sql, ponum, poline, company);

            return CommonRepository.GetValueAsString(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null));
        }



       
        #endregion



        #region 接收
        /// <summary>
        /// 根据条件，返回有效收货依据列表
        /// </summary>
        /// <param name="Condition"></param>
        /// <returns>若返回null， 说明按照条件没有找到有效的收货依据</returns>
        public static IEnumerable<Receipt> GetReceivingBasis(Receipt Condition)
        {
            #region 构造sql语句，按条件筛选收货依据
            string sql = @"select 
                pr.JobNum, 
                pr.PORelNum,
                pr.PoNum,
                pr.TranType,
                pr.JobSeq,
                pr.PoLine ,
                pr.Plant,
                pr.Company,
                pd.CommentText,
                pd.IUM,
                pd.PartNum,
                vd.VendorID  as SupplierNo,
                vd.Name as SupplierName,
                pa.TypeCode  as  PartType,
                pd.LineDesc  as  PartDesc,
                pr.AssemblySeq,               
                jh.jobClosed,
                jh.jobComplete,
                jh.JobEngineered,
                jh.JobReleased,
                jh.JobHeld,
                ph.POType,
                pc.Description   as  PartClassDesc,
                (pr.XRelQty-pr.ArrivedQty) NeedReceiptQty, 
                pp.PrimWhse as Warehouse
                 from erp.PORel pr
                left join erp.PODetail pd   on pr.PONum = pd.PONUM   and   pr.Company = pd.Company   and   pr.POLine = pd.POLine 
                left join erp.POHeader ph   on ph.Company = pd.Company   and   ph.PONum = pd.PONUM                 
                left join erp.JobHead jh    on pr.JobNum = jh.JobNum   and   pr.Company = jh.Company
                left join erp.Vendor vd     on ph.VendorNum = vd.VendorNum   and   ph.company = vd.company             
                left join erp.part pa       on pd.PartNum = pa.PartNum   and   pa.company = pd.company
                left join erp.partclass pc  on pc.classid = pd.ClassID   and   pc.company = pd.company
                left join erp.partplant pp  on pp.company = pr.Company   and   pp.plant = pr.plant   and   pp.PartNum = pd.PartNum
                where CHARINDEX(pr.Company, '" + HttpContext.Current.Session["Company"].ToString() + "') > 0   and    CHARINDEX(pr.Plant, '" + HttpContext.Current.Session["Plant"].ToString() + "') > 0 " +
                "and  ph.OpenOrder = 1   and    ph.orderHeld != 1    and    pd.openLine = 1   and   pr.openRelease = 1   and  ph.Approve = 1 and ph.Confirmed =1";

            if (Condition.PoNum != null)
                sql += "and pr.ponum = " + Condition.PoNum + " ";
            if (Condition.PoLine != null)
                sql += "and pr.poline = " + Condition.PoLine + " ";
            if (Condition.PORelNum != null)
                sql += "and pr.PORelNum = " + Condition.PORelNum + " ";
            if (Condition.PartNum != null && Condition.PartNum != "")
                sql += "and pd.partnum like '%" + Condition.PartNum + "%' ";
            if (Condition.BatchNo == null && Condition.PartDesc != null)
                sql += "and pd.LineDesc like '%" + Condition.PartDesc + "%' ";
            if (Condition.Company != null)
                sql += "and pr.Company = '" + Condition.Company + "' ";
            #endregion

            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql); //获取可能有效的收货依据

            if (dt == null) //没有找到可能有效的收货依据
                return null;


            //筛选可能有效的收货依据，以得到最终有效的收货依据表。     
            for (int i = dt.Rows.Count - 1; i >= 0; i--)
            {
                //如果该收货依据是外协或工单物料 ，且关联的工单已完成或关闭或冻结 则排除该收货依据
                if (((string)dt.Rows[i]["TranType"] != "PUR-STK" && (string)dt.Rows[i]["TranType"] != "PUR-UKN") && ((bool)dt.Rows[i]["jobClosed"] == true || (bool)dt.Rows[i]["jobComplete"] == true || (bool)dt.Rows[i]["JobEngineered"] == false || (bool)dt.Rows[i]["JobReleased"] == false))// || (bool)dt.Rows[i]["JobHeld"] == true))
                    dt.Rows.RemoveAt(i);
            }
            List<Receipt> RBs = CommonRepository.DataTableToList<Receipt>(dt);




            if (RBs != null)//若经过筛选后收货依据列表不为空
            {
                string append = Condition.BatchNo != null ? "and  batchno != '" + Condition.BatchNo + "'" : ""; //若Condition.BatchNo 不为空，则RBs中只有一条记录
                for (int i = RBs.Count - 1; i >= 0; i--)
                {
                    sql = "select sum(case when ArrivedQty is null then(case when  ReceiveQty2 is null then ReceiveQty1 else ReceiveQty2 end) else ArrivedQty end) from Receipt " +
                        "where isdelete != 1 and isComplete != 1 and  ponum = " + (int)RBs[i].PoNum + " and poline = " + (int)RBs[i].PoLine + " and  PORelNum = " + (int)RBs[i].PORelNum + " and company = '" + RBs[i].Company + "' and plant = '" + RBs[i].Plant + "'  " + append;

                    object sum = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                    RBs[i].NotReceiptQty = (decimal)RBs[i].NeedReceiptQty - (sum is DBNull || sum == null ? 0 : (decimal)sum);

                    if (RBs[i].NotReceiptQty <= 0)
                        RBs.RemoveAt(i);
                }//计算每个有效收货依据中的还可接收数量NotReceiptQty


                for (int i = 0; i < RBs.Count; i++)
                {
                    if (RBs[i].JobNum != "")
                    {
                        sql = @" Select OpDesc, opcode from erp.porel pr 
                          left join erp.JobOper jo on pr.jobnum = jo.JobNum and pr.AssemblySeq = jo.AssemblySeq and pr.Company = jo.Company and jobseq = jo.OprSeq 
                          where pr.ponum={0} and pr.jobnum = '{1}'  and pr.assemblyseq={2} and trantype='PUR-SUB' and pr.company = '{3}'  and pr.poline = {4} and pr.porelnum = {5}";
                        sql = string.Format(sql, RBs[i].PoNum, RBs[i].JobNum, RBs[i].AssemblySeq, RBs[i].Company, RBs[i].PoLine, RBs[i].PORelNum);

                        DataTable opinfo = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);

                        RBs[i].OpDesc = opinfo.Rows[0]["OpDesc"].ToString();
                        RBs[i].OpCode = opinfo.Rows[0]["opcode"].ToString();
                    }
                }//获取与工单有关的有效收货依据的工序描述与工序代码
            }

            return RBs != null && RBs.Count > 0 ? RBs : null;
        }


        /// <summary>
        /// 无二维码收货
        /// </summary>
        /// <param name="batInfo"></param>
        /// <returns></returns>
        public static string ReceiveCommitWithNonQRCode(Receipt batInfo)
        {
            try
            {
                if ((1 & (int)HttpContext.Current.Session["RoleID"]) == 0)
                    return "错误：您的账号没有收货操作权限";


                string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"); // 获取当前操作时间

                var t = GetReceivingBasis(batInfo);
                Receipt RB = t?.First();//根据批次信息 获取该批次所属的收货依据

                if (RB == null) //该批次所属的收货依据错误
                {
                    return GetErrorInfo(batInfo);
                }

                if (batInfo.ReceiveQty1 == null || batInfo.ReceiveQty1 <= 0)
                    return "错误：数量需大于0";

                if (batInfo.ReceiveQty1 > RB.NotReceiptQty)//若超收
                    return string.Format("超收数量：{0}， 可收数量：{1}", Math.Round((double)(batInfo.ReceiveQty1 - RB.NotReceiptQty), 2), Math.Round((double)RB.NotReceiptQty));

                //临时取消完成数判断，财务上线 +
                if (RB.TranType == "PUR-SUB")
                {
                    DataTable temp = GetAllOpSeqOfSeriesSUB(new Receipt { PoNum = batInfo.PoNum, PoLine = batInfo.PoLine, Company = batInfo.Company, PORelNum = batInfo.PORelNum, JobNum = RB.JobNum, AssemblySeq = RB.AssemblySeq, JobSeq = RB.JobSeq });
                    object PreOpSeq = CommonRepository.GetPreOpSeq(RB.JobNum, (int)RB.AssemblySeq, (int)temp.Rows[0]["jobseq"]);

                    if (PreOpSeq == null && CommonRepository.GetReqQtyOfAssemblySeq(RB.JobNum, (int)RB.AssemblySeq) < batInfo.ReceiveQty1 + GetTotalQtyOfJobSeq(RB.JobNum, (int)RB.AssemblySeq, (int)RB.JobSeq, 0))
                        return "错误： 收货数超出该阶层的可生产数量";

                    if (temp.Rows[0]["PartNum"].ToString() != RB.PartNum || temp.Rows[0]["Description"].ToString() != RB.PartDesc)
                        return "错误：物料信息与工单不一致";
                    //if (CommonRepository.IsOpSeqComplete(RB.JobNum, (int)RB.AssemblySeq, (int)RB.JobSeq))
                    //    return "错误：该工序已收满";

                    if (PreOpSeq != null && CommonRepository.GetOpSeqCompleteQty(RB.JobNum, (int)RB.AssemblySeq, (int)PreOpSeq) < batInfo.ReceiveQty1 + GetTotalQtyOfJobSeq(RB.JobNum, (int)RB.AssemblySeq, (int)RB.JobSeq, 0))
                        return "错误： 收货数超出上一道非该供应商工序的完成数量";
                }

                string sql = "select * from SerialNumber where name = 'BAT'";

                #region 计算批次号
                lock (PrintSeriesNumLock)
                {
                    DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql);


                    string OriDay = ((DateTime)dt.Rows[0]["time"]).ToString("yyyy-MM-dd");//截取从数据库获得的时间的年月日部分
                    string today = DateTime.Now.ToString("yyyy-MM-dd");//截取当前时间的年月日部分


                    if (OriDay == today) // 如果从数据库获得的日期 是今天 
                    {
                        batInfo.BatchNo = "P" + DateTime.Now.ToString("yyyyMMdd") + ((int)dt.Rows[0]["Curr"]).ToString("d4");
                        dt.Rows[0]["Curr"] = (int)dt.Rows[0]["Curr"] + 1; //计数器递增1
                    }
                    else // 不是今天 
                    {
                        batInfo.BatchNo = "P" + DateTime.Now.ToString("yyyyMMdd") + "0001";
                        dt.Rows[0]["Curr"] = 2; //计数器重置为1
                    }

                    sql = "UPDATE SerialNumber SET time = getdate(), curr = " + Convert.ToInt32(dt.Rows[0]["Curr"]) + " where name = 'BAT'";
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                }
                #endregion


                #region 调用现有接口打印

                string jsonStr = " text1: '{0}', text2: '{12}', text3: '{1}', text4: '{2}', text5: '{3}', text6: '', text7: '{4}', text8: '{5}', text9: '{6}', text10: '{7}', text11: '{8}', text12: '{9}', text13: '', text14: '{10}', text15: '{11}', text16: '', text17: '', text18: '', text19: '', text20: '', text21: '', text22: '', text23: '', text24: '', text25: '', text26: '', text27: '', text28: '', text29: '', text30: '' ";
                jsonStr = string.Format(jsonStr, batInfo.PartNum, batInfo.BatchNo, CommonRepository.GetValueAsString(batInfo.JobNum), CommonRepository.GetValueAsString(batInfo.AssemblySeq), batInfo.SupplierNo, batInfo.PoNum, batInfo.PoLine, batInfo.ReceiveQty1, batInfo.PORelNum, batInfo.Company, CommonRepository.GetValueAsString(batInfo.JobSeq), batInfo.HeatNum, batInfo.PartDesc);
                jsonStr = "[{" + jsonStr + "}]";



                string res = "";
                batInfo.IsPrint = false;
                sql = "select Printer from Userprinter where userid = '" + HttpContext.Current.Session["UserId"].ToString() + "' ";
                string printer = (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                ServiceReference_Print.WebServiceSoapClient client = new ServiceReference_Print.WebServiceSoapClient();
                if ((res = client.Print(@"C:\D0201.btw", printer, (int)batInfo.PrintQty, jsonStr)) == "1|处理成功")
                {
                    client.Close();
                    batInfo.IsPrint = true;
                }
                else
                {
                    client.Close();
                    return "错误：打印失败  " + res;
                }
                #endregion

                #region 回写数据到APP.Receipt表中
                if (batInfo.IsPrint == true)
                {
                    #region 构造sql语句
                    sql = @"insert into Receipt(
                            SupplierNo, 
                            SupplierName,                
                            ReceiveQty1,
                            AssemblySeq, 
                            JobSeq,
                            OpCode,
                            PartNum,
                            PartDesc,
                            IUM, 
                            JobNum, 
                            Remark,
                            TranType,
                            PartType,           
                            OpDesc,
                            CommentText,
                            PartClassDesc,
                            NeedReceiptQty,
                            NotReceiptQty,
                            SecondUserGroup,
                            FirstUserID,
                            Status,
                            PreStatus,
                            AtRole,
                            PoNum,
                            PoLine,
                            PORelNum,
                            BatchNo,
                            Company,
                            Plant,
                            IsPrint,                      
                            HeatNum,
                            ReturnOne,
                            ReturnTwo,  
                            ReturnThree,
                            Warehouse,
                            isdelete,
                            isAuto,
                            isComplete,
                            PackSlip,
                            ReceiptDate,
                            POType
                            ) values({0}) ";
                    string values = CommonRepository.ConstructInsertValues(new ArrayList
                    {
                        batInfo.SupplierNo,
                        batInfo.SupplierName,
                        batInfo.ReceiveQty1,
                        RB.AssemblySeq,
                        RB.JobSeq,
                        RB.OpCode,
                        batInfo.PartNum,
                        "\u1234PartDesc",
                        RB.IUM,
                        batInfo.JobNum.ToUpper(),
                        batInfo.Remark,
                        RB.TranType,
                        RB.PartType,
                        RB.OpDesc,
                        "\u1234CommentText",   //   \u1234  指示参数化
                        RB.PartClassDesc,
                        RB.NeedReceiptQty,
                        RB.NotReceiptQty,
                        batInfo.SecondUserGroup,
                        HttpContext.Current.Session["UserId"].ToString(),
                        2,
                        1,
                        2,
                        batInfo.PoNum,
                        batInfo.PoLine,
                        batInfo.PORelNum,
                        batInfo.BatchNo,
                        batInfo.Company,
                        batInfo.Plant,
                        1,
                        batInfo.HeatNum,
                        0,
                        0,
                        0,
                        RB.Warehouse,
                        0,
                        0,
                        0,
                        RB.SupplierNo + batInfo.BatchNo,
                        OpDate,
                        RB.POType
                    });
                    sql = string.Format(sql, values);
                    #endregion
                   

                    SqlParameter[] ps = new SqlParameter[] { new SqlParameter("@CommentText", RB.CommentText), new SqlParameter("@PartDesc", RB.PartDesc) };
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, ps);
                }
                #endregion


                sql = sql.Replace("'", "");
                AddOpLog(GetReceiptID(batInfo), batInfo.BatchNo, 102, "insert", OpDate, sql);

                return "处理成功";
            }
            catch
            {
                throw;
            }

        }


        /// <summary>
        /// 有二维码收货
        /// </summary>
        /// <param name="batInfo"></param>
        /// <returns></returns>
        public static string ReceiveCommitWithQRCode(Receipt batInfo)
        {
            if ((1 & (int)HttpContext.Current.Session["RoleID"]) == 0)
                return "错误：您的账号没有收货操作权限";


            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"); //获取当前操作时间点

            var t = GetReceivingBasis(batInfo);
            Receipt RB = t?.First();//根据批次信息 获取该批次所属的收货依据

            if (RB == null)//该批次的收货依据错误
                return GetErrorInfo(batInfo);

            if (batInfo.ReceiveQty1 == null || batInfo.ReceiveQty1 <= 0)
                return "错误：数量需大于0";

            if (batInfo.ReceiveQty1 > RB.NotReceiptQty)//若超收
                return string.Format("超收数量：{0}， 可收数量：{1}", Math.Round((double)(batInfo.ReceiveQty1 - RB.NotReceiptQty), 2), Math.Round((double)RB.NotReceiptQty));


            //临时取消完成数判断，财务上线 +
            if (RB.TranType == "PUR-SUB")
            {
                DataTable temp = GetAllOpSeqOfSeriesSUB(new Receipt { PoNum = batInfo.PoNum, PoLine = batInfo.PoLine, Company = batInfo.Company, PORelNum = batInfo.PORelNum, JobNum = RB.JobNum, AssemblySeq = RB.AssemblySeq, JobSeq = RB.JobSeq });

                object PreOpSeq = CommonRepository.GetPreOpSeq(RB.JobNum, (int)RB.AssemblySeq, (int)temp.Rows[0]["jobseq"]);

                string sql2 = "select id from Receipt where BatchNo = '" + batInfo.BatchNo + "'";
                object o = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql2, null);
                int id = Convert.IsDBNull(o) || o == null ? 0 : (int)o;

                if (temp.Rows[0]["PartNum"].ToString() != RB.PartNum || temp.Rows[0]["Description"].ToString() != RB.PartDesc)
                    return "错误：物料信息与工单不一致";

                if (PreOpSeq == null && CommonRepository.GetReqQtyOfAssemblySeq(RB.JobNum, (int)RB.AssemblySeq) < batInfo.ReceiveQty1 + GetTotalQtyOfJobSeq(RB.JobNum, (int)RB.AssemblySeq, (int)RB.JobSeq, id))
                    return "错误： 收货数超出该阶层的可生产数量";
                //if (CommonRepository.IsOpSeqComplete(RB.JobNum, (int)RB.AssemblySeq, (int)RB.JobSeq))
                //    return "错误：该工序已收满";
                if (PreOpSeq != null && CommonRepository.GetOpSeqCompleteQty(RB.JobNum, (int)RB.AssemblySeq, (int)PreOpSeq) < batInfo.ReceiveQty1 + GetTotalQtyOfJobSeq(RB.JobNum, (int)RB.AssemblySeq, (int)RB.JobSeq, id))
                    return "错误： 收货数超出上一道非该供应商工序的完成数量";
            }


            string sql = "select status, isdelete, iscomplete from Receipt where BatchNo = '" + batInfo.BatchNo + "' ";
            DataTable theBatch = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql);//获取batInfo所指定的批次的status，isdelete字段值

            if (theBatch == null) //batInfo所指定的批次不存在，为该批次生成一条新的receipt记录
            {
                #region 构造sql语句
                sql = @"insert into Receipt(
                        SupplierNo, 
                        SupplierName,                
                        ReceiveQty1,
                        AssemblySeq, 
                        JobSeq,
                        OpCode,
                        PartNum,
                        PartDesc,
                        IUM, 
                        JobNum, 
                        Remark,
                        TranType,
                        PartType,           
                        OpDesc,
                        CommentText,
                        PartClassDesc,
                        NeedReceiptQty,
                        NotReceiptQty,
                        SecondUserGroup,
                        FirstUserID,
                        Status,
                        PreStatus,
                        AtRole,
                        PoNum,
                        PoLine,
                        PORelNum,
                        BatchNo,
                        Company,
                        Plant,
                        IsPrint,
                        HeatNum,
                        ReturnOne,
                        ReturnTwo,
                        ReturnThree,
                        Warehouse,
                        isdelete,
                        isAuto,
                        isComplete,
                        PackSlip,
                        ReceiptDate,
                        POType
                        ) values({0}) ";
                string values = CommonRepository.ConstructInsertValues(new ArrayList
                {
                    batInfo.SupplierNo,
                    batInfo.SupplierName,
                    batInfo.ReceiveQty1,
                    RB.AssemblySeq,
                    RB.JobSeq,
                    RB.OpCode,
                    batInfo.PartNum,
                    "\u1234PartDesc",
                    RB.IUM,
                    batInfo.JobNum.ToUpper(),
                    batInfo.Remark,
                    RB.TranType,
                    RB.PartType,
                    RB.OpDesc,
                    "\u1234CommentText",
                    RB.PartClassDesc,
                    RB.NeedReceiptQty,
                    RB.NotReceiptQty,
                    batInfo.SecondUserGroup,
                    HttpContext.Current.Session["UserId"].ToString(),
                    2,
                    1,
                    2,
                    batInfo.PoNum,                                                                                                                       
                    batInfo.PoLine,
                    batInfo.PORelNum,
                    batInfo.BatchNo,
                    batInfo.Company,
                    batInfo.Plant,
                    1,
                    batInfo.HeatNum,
                    0,
                    0,
                    0,
                    RB.Warehouse,
                    0,
                    0,
                    0,
                    RB.SupplierNo + batInfo.BatchNo,
                    OpDate,
                    RB.POType
                });
                sql = string.Format(sql, values);
                #endregion

                SqlParameter[] ps = new SqlParameter[] { new SqlParameter("@CommentText", RB.CommentText), new SqlParameter("@PartDesc", RB.PartDesc) };
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, ps);


                sql = sql.Replace("'", "");
                AddOpLog(GetReceiptID(batInfo), batInfo.BatchNo, 103, "insert", OpDate, sql);

                return "处理成功";
            }

            else if ((bool)theBatch.Rows[0]["isdelete"] == true)
                return "错误：该批次的流程已删除";

            else if ((bool)theBatch.Rows[0]["isComplete"] == true)
                return "错误：该批次的流程已结束";

            else if ((int)theBatch.Rows[0]["status"] != 1)
                return "错误：流程未在当前节点上，在 " + theBatch.Rows[0]["status"].ToString() + "节点";

            else //status == 1  表明在第二届点被退回， 更新批次信息。
            {
                #region 构造sql语句:
                sql = @"update Receipt set                      
                        ReceiveQty1 = {0},                      
                        Remark = '{1}',
                        SecondUserGroup = '{2}',    
                        Status = {3},
                        AtRole = {4},
                        PreStatus = {5},
                        HeatNum = '{6}'
                        where BatchNo = '" + batInfo.BatchNo + "'";
                sql = string.Format(sql,
                    batInfo.ReceiveQty1,
                    batInfo.Remark,
                    batInfo.SecondUserGroup,
                    2,
                    2, 
                    (int)theBatch.Rows[0]["status"],
                    batInfo.HeatNum);
                #endregion
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = sql.Replace("'", "");
                AddOpLog(batInfo.ID, batInfo.BatchNo, 103, "update", OpDate, sql);

                return "处理成功"; //更新提交成功                
            }

        }

        #endregion



        #region 进料检验

        /// <summary>
        /// 
        /// </summary>
        /// <param name="IQCInfo"></param>
        /// <returns></returns>
        public static string IQCCommit(Receipt IQCInfo)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");


            string sql = "select * from Receipt where ID = " + IQCInfo.ID + "";
            Receipt theBatch = CommonRepository.DataTableToList<Receipt>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

            var t = GetReceivingBasis(theBatch);
            Receipt RB = t?.First();//根据批次信息 获取该批次所属的收货依据


            if (RB == null)
                return GetErrorInfo(theBatch);

            else if (IQCInfo.ReceiveQty2 == null || IQCInfo.ReceiveQty2 <= 0)
                return "错误：数量需大于0";

            else if (Convert.ToDecimal(IQCInfo.FailedQty) > 0  &&  Convert.ToDecimal(IQCInfo.OurFailedQty) > Convert.ToDecimal(IQCInfo.FailedQty))
                return "错误：我方不合格数需小于等于不合格数";

            else if (IQCInfo.ReceiveQty2 > RB.NotReceiptQty)//若超收
                return string.Format("超收数量：{0}， 可收数量：{1}", Math.Round((double)(IQCInfo.ReceiveQty2 - RB.NotReceiptQty), 2), Math.Round((double)RB.NotReceiptQty));

            else if (theBatch.IsDelete == true)
                return "错误：该批次的流程已删除";

            else if (theBatch.PartNum != RB.PartNum || (!theBatch.PartDesc.Contains("?") && theBatch.PartDesc != RB.PartDesc))
                return "错误：物料信息已被更改，请联系采购部";

            else if (theBatch.IsComplete == true)
                return "错误：该批次的流程已结束";

            else if (theBatch.Status != 2)
                return "错误：流程未在当前节点上，在 " + theBatch.Status.ToString() + "节点";

            else //status == 2  更新批次信息。
            {
                string PassedQty = IQCInfo.PassedQty != null ? IQCInfo.PassedQty.ToString() : "null";
                string FailedQty = IQCInfo.FailedQty != null ? IQCInfo.FailedQty.ToString() : "null";
                string OurFailedQty = IQCInfo.OurFailedQty != null ? IQCInfo.OurFailedQty.ToString() : "null";
                string InspectionQty = IQCInfo.InspectionQty != -1 ? IQCInfo.InspectionQty.ToString() : "null";
                string ThirdUserGroup = IQCInfo.ThirdUserGroup ?? "";
                string ReceiptNo = IQCInfo.ReceiptNo ?? "";


                sql = @"update Receipt set OurFailedQty = " + OurFailedQty + ", PreStatus = " + theBatch.Status + " , IQCRemark = '" + IQCInfo.IQCRemark + "' ,  NBBatchNo = '" + IQCInfo.NBBatchNo + "', IQCDate = '" + OpDate + "', IsAllCheck = {0},  InspectionQty = {1}, PassedQty = {2}, FailedQty = {3}, Result = '{4}', Status= " + IQCInfo.Status + " ,ThirdUserGroup = '{5}', SecondUserID = '{6}', ReceiptNo = '{7}', ReceiveQty2 = {8}, AtRole = {10} where ID = {9}";
                sql = string.Format(sql, Convert.ToInt32(IQCInfo.IsAllCheck), InspectionQty, PassedQty, FailedQty, IQCInfo.Result, ThirdUserGroup, HttpContext.Current.Session["UserId"].ToString(), ReceiptNo, IQCInfo.ReceiveQty2, IQCInfo.ID, IQCInfo.Status == 3 ? 4 : 2);
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);


                sql = sql.Replace("'", "");
                AddOpLog(IQCInfo.ID, theBatch.BatchNo, 201, "update", OpDate, sql);

                return "处理成功";
            }
        }



        public static bool UploadIQCFile()
        {
            Thread.Sleep(10);
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string ss = OpDate.Replace(":", "-").Replace(".", "").Replace(" ", "_");

            byte[] fileContents = new byte[HttpContext.Current.Request.InputStream.Length];
            HttpContext.Current.Request.InputStream.Read(fileContents, 0, fileContents.Length);


            string fn = HttpContext.Current.Request.Headers.Get("FileName");
            string fileType = fn.Substring(fn.LastIndexOf('.'));
            int ReceiptID = int.Parse(HttpContext.Current.Request.Headers.Get("ReceiptID"));



            string sql = "select batchNo, SupplierNo, ponum,poline  from Receipt where ID = " + ReceiptID + " ";
            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql);



            if (!FTPRepository.IsFolderExist("/", (string)dt.Rows[0]["SupplierNo"])) //供应商层
            {
                FTPRepository.MakeFolder("/", (string)dt.Rows[0]["SupplierNo"]);
            }

            if (!FTPRepository.IsFolderExist("/" + (string)dt.Rows[0]["SupplierNo"] + "/", dt.Rows[0]["ponum"].ToString()))//订单号层
            {
                FTPRepository.MakeFolder("/" + (string)dt.Rows[0]["SupplierNo"] + "/", dt.Rows[0]["ponum"].ToString());
            }

            if (!FTPRepository.IsFolderExist("/" + (string)dt.Rows[0]["SupplierNo"] + "/" + dt.Rows[0]["ponum"].ToString() + "/", dt.Rows[0]["poline"].ToString()))//订单行号层
            {
                FTPRepository.MakeFolder("/" + (string)dt.Rows[0]["SupplierNo"] + "/" + dt.Rows[0]["ponum"].ToString() + "/", dt.Rows[0]["poline"].ToString());
            }

            if (!FTPRepository.IsFolderExist("/" + (string)dt.Rows[0]["SupplierNo"] + "/" + dt.Rows[0]["ponum"].ToString() + "/" + dt.Rows[0]["poline"].ToString() + "/", dt.Rows[0]["batchno"].ToString()))//批次号号层
            {
                FTPRepository.MakeFolder("/" + (string)dt.Rows[0]["SupplierNo"] + "/" + dt.Rows[0]["ponum"].ToString() + "/" + dt.Rows[0]["poline"].ToString() + "/", dt.Rows[0]["batchno"].ToString());
            }


            //设置文件名
            string newFileName = (string)dt.Rows[0]["batchNo"] + "_" + ss + "_" + HttpContext.Current.Session["UserId"].ToString() + fileType;


            //上传，成功则更新数据库
            if (FTPRepository.UploadFile(fileContents, "/" + (string)dt.Rows[0]["SupplierNo"] + "/" + dt.Rows[0]["ponum"].ToString() + "/" + dt.Rows[0]["poline"].ToString() + "/" + dt.Rows[0]["batchno"].ToString() + "/", newFileName) == true)
            {
                string FilePath = FTPRepository.ftpServer.Substring(6) + "/" + (string)dt.Rows[0]["SupplierNo"] + "/" + dt.Rows[0]["ponum"].ToString() + "/" + dt.Rows[0]["poline"].ToString() + "/" + dt.Rows[0]["batchno"].ToString() + "/";

                sql = @"insert into IQCFile Values('{0}', '{1}', '{2}', '{3}')";
                sql = string.Format(sql, (string)dt.Rows[0]["batchNo"], FilePath.Replace('/', '\\'), newFileName, OpDate);

                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = sql.Replace("'", "");
                AddOpLog(ReceiptID, (string)dt.Rows[0]["batchNo"], 202, "upload", OpDate, "ftp://" + FilePath + newFileName);

                return true;
            }

            return false;
        }
        #endregion



        #region 流转

        public static string TransferCommit(Receipt TransferInfo)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");


            string sql = "select * from Receipt where ID = " + TransferInfo.ID + "";
            Receipt theBatch = CommonRepository.DataTableToList<Receipt>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录


            var t = GetReceivingBasis(theBatch);
            Receipt RB = t?.First();//根据批次信息 获取该批次所属的收货依据


            if (RB == null)
                return GetErrorInfo(theBatch);

            else if (theBatch.PartNum != RB.PartNum || (!theBatch.PartDesc.Contains("?") && theBatch.PartDesc != RB.PartDesc))
                return "错误：物料信息已被更改，请联系采购部";

            else if (TransferInfo.ReceiveQty2 > RB.NotReceiptQty)//若超收
                return string.Format("超收数量：{0}， 可收数量：{1}", Math.Round((double)(TransferInfo.ReceiveQty2 - RB.NotReceiptQty), 2), Math.Round((double)RB.NotReceiptQty));

            else if (theBatch.IsDelete == true)
                return "错误：该批次的流程已删除";

            else if (theBatch.IsComplete == true)
                return "错误：该批次的流程已结束";

            else if (theBatch.Status != 3)
                return "错误：流程未在当前节点上，在 " + theBatch.Status + "节点";

            else //status == 3  选人。
            {
                TransferInfo.AtRole = GetNextRole(theBatch.ID);
                if (TransferInfo.AtRole == 1152921504606846976) //2^60
                    return "错误：无法确定去向";
                
                sql = @"update Receipt set PreStatus = " + theBatch.Status + " , ChooseDate = '" + OpDate + "', Status = 4, FourthUserGroup = '{0}', ThirdUserID = '{1}', AtRole = {2} where ID = " + TransferInfo.ID + "";
                sql = string.Format(sql, TransferInfo.FourthUserGroup, HttpContext.Current.Session["UserId"].ToString(), TransferInfo.AtRole);
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);


                sql = sql.Replace("'", "");
                AddOpLog(TransferInfo.ID, theBatch.BatchNo, 301, "update", OpDate, sql);

                return "处理成功";
            }
        }

        #endregion



        #region 入库


        public static string AcceptCommit(Receipt AcceptInfo)
        {
            string sql2 = "", sql = "";
            int ponum = -1, poline = -1, porel = -1;
            try
            {
                string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

                sql = "select * from Receipt where ID = " + AcceptInfo.ID + "";
                Receipt theBatch = CommonRepository.DataTableToList<Receipt>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录


                var t = GetReceivingBasis(theBatch);
                Receipt RB = t?.First();//根据批次信息 获取该批次所属的收货依据

                AcceptInfo.Company = theBatch.Company; //CheckBinNum 设置公司值

                string res = "";//保存调用结果


                if (RB == null)
                    return GetErrorInfo(theBatch);

                else if (theBatch.PartNum != RB.PartNum || (!theBatch.PartDesc.Contains("?") && theBatch.PartDesc != RB.PartDesc))
                    return "错误：物料信息已被更改";

                else if (theBatch.AtRole == 8 && (res = CheckBinNum(AcceptInfo)) != "ok") //去向仓库，则需检查库位
                    return res;

                else if (AcceptInfo.ArrivedQty == null || AcceptInfo.ArrivedQty <= 0)
                    return "错误：数量需大于0";

                else if (AcceptInfo.ArrivedQty > RB.NotReceiptQty)//若超收
                    return string.Format("超收数量：{0}， 可收数量：{1}", Math.Round((double)(AcceptInfo.ArrivedQty - RB.NotReceiptQty), 2), Math.Round((double)RB.NotReceiptQty));

                else if (theBatch.IsDelete == true)
                    return "错误：该批次的流程已删除";

                else if (theBatch.IsComplete == true)
                    return "错误：该批次的流程已结束";

                else if (theBatch.Status != 4)
                    return "错误：流程未在当前节点上，在 " + theBatch.Status + "节点";

                else
                {
                    string packnum, recdate = OpDate, vendorid = theBatch.SupplierNo, rcvdtlStr = "", companyId = theBatch.Company;

                    if (theBatch.TranType == "PUR-STK" || theBatch.TranType == "PUR-UKN")
                    {
                        packnum = vendorid + theBatch.BatchNo;
                        if (packnum.Length > 20)
                            return "错误：装箱单号过长";


                        rcvdtlStr = ConstructRcvdtlStr(
                                new String[] {
                                CommonRepository.GetValueAsString(theBatch.PoNum),
                                CommonRepository.GetValueAsString(theBatch.PoLine),
                                CommonRepository.GetValueAsString(theBatch.PORelNum),
                                CommonRepository.GetValueAsString(theBatch.PartNum),
                                CommonRepository.GetValueAsString(AcceptInfo.ArrivedQty),
                                CommonRepository.GetValueAsString(theBatch.IUM),
                                CommonRepository.GetValueAsString(AcceptInfo.Warehouse),
                                CommonRepository.GetValueAsString(AcceptInfo.BinNum),
                                CommonRepository.GetValueAsString(theBatch.BatchNo),
                                CommonRepository.GetValueAsString(theBatch.JobNum),
                                CommonRepository.GetValueAsString(theBatch.AssemblySeq),
                                CommonRepository.GetValueAsString(theBatch.JobSeq),
                                CommonRepository.GetValueAsString(theBatch.CommentText.Replace('\'','"')),
                                CommonRepository.GetValueAsString(theBatch.TranType),
                                CommonRepository.GetValueAsString(theBatch.HeatNum)});
                        rcvdtlStr = "[" + rcvdtlStr + "]";


                        if ((res = ErpAPI.ReceiptRepository.porcv(packnum, recdate.Split(' ')[0], vendorid, rcvdtlStr, "", companyId)) == "1|处理成功.")//erp回写成功，更新对应的Receipt记录
                        {
                            string Location = ErpAPI.ReceiptRepository.poDes((int)theBatch.PoNum, (int)theBatch.PoLine, (int)theBatch.PORelNum, theBatch.Company);
                            Location = Location == "R|物料接收人:" ? Location + (string)HttpContext.Current.Session["UserId"] : Location;

                            sql = @"update Receipt set PreStatus = " + theBatch.Status + " , StockDate = '" + OpDate + "', ArrivedQty = {0}, Warehouse = {1}, BinNum = {2}, FourthUserID = '{3}', isComplete = 1, Location = '{4}', status = 99  where ID = " + AcceptInfo.ID + "";
                            sql = string.Format(sql, AcceptInfo.ArrivedQty, AcceptInfo.Warehouse != null ? "'" + AcceptInfo.Warehouse + "'" : "null", AcceptInfo.BinNum != null ? "'" + AcceptInfo.BinNum + "'" : "null", HttpContext.Current.Session["UserId"].ToString(), Location);
                            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                            sql = sql.Replace("'", "");
                            AddOpLog(AcceptInfo.ID, theBatch.BatchNo, 401, "update", OpDate, sql);
                            return "处理成功";
                        }
                        return "错误：" + res;
                    }

                    else if (theBatch.TranType == "PUR-SUB")
                    {
                        DataTable dt = GetAllOpSeqOfSeriesSUB(theBatch);

                        object PreOpSeq = CommonRepository.GetPreOpSeq(theBatch.JobNum, (int)theBatch.AssemblySeq, (int)dt.Rows[0]["jobseq"]);
                        if (PreOpSeq == null && CommonRepository.GetReqQtyOfAssemblySeq(RB.JobNum, (int)RB.AssemblySeq) < AcceptInfo.ArrivedQty + GetTotalQtyOfJobSeq(RB.JobNum, (int)RB.AssemblySeq, (int)RB.JobSeq, AcceptInfo.ID))
                            return "错误： 收货数超出该阶层的可生产数量";
                        if (PreOpSeq != null && CommonRepository.GetOpSeqCompleteQty(RB.JobNum, (int)RB.AssemblySeq, (int)PreOpSeq) < AcceptInfo.ArrivedQty + GetTotalQtyOfJobSeq(RB.JobNum, (int)RB.AssemblySeq, (int)RB.JobSeq, AcceptInfo.ID))
                            return "错误： 收货数超出上一道非该供应商工序的完成数量";


                        {
                            packnum = vendorid + theBatch.BatchNo;
                            if (packnum.Length > 20)
                                return "错误：装箱单号过长";


                            if (theBatch.AtRole != 8) //连续委外回来不直接入库
                            {
                                AcceptInfo.Warehouse = "待检区";
                                AcceptInfo.BinNum = "ins";
                            }


                            //发料验证， 若该组委外工序中有多个相同化学品，每个单独验证时 数量满足， 但实际发料时 会有多次发料，则可能不够发， 
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                DataTable mtls = CommonRepository.GetMtlsOfOpSeq(theBatch.JobNum, (int)theBatch.AssemblySeq, (int)dt.Rows[i]["jobseq"], theBatch.Company);
                                if (mtls != null)
                                {
                                    for (int j = 0; j < mtls.Rows.Count; j++)
                                    {
                                        if (mtls.Rows[j]["partnum"].ToString().Substring(0, 1).Trim().ToLower() == "c")
                                        {
                                            res = ErpAPI.MtlIssueRepository.CheckIssue(mtls.Rows[j]["partnum"].ToString(), (decimal)mtls.Rows[j]["RequiredQty"]);
                                            if (res.Substring(0, 1).Trim() == "0")
                                                return "工单：" + theBatch.JobNum + "，阶层：" + theBatch.AssemblySeq.ToString() + "，工序：" + dt.Rows[i]["jobseq"].ToString() + "， 物料编码：" + mtls.Rows[j]["partnum"].ToString() + "  " + res.Substring(2);
                                        }
                                    }
                                }
                            }


                                rcvdtlStr = "[";
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                rcvdtlStr += ConstructRcvdtlStr(
                                    new String[] {
                                CommonRepository.GetValueAsString(theBatch.PoNum),
                                CommonRepository.GetValueAsString(dt.Rows[i]["poline"]),
                                CommonRepository.GetValueAsString(dt.Rows[i]["porelnum"]),
                                CommonRepository.GetValueAsString(theBatch.PartNum),
                                CommonRepository.GetValueAsString(AcceptInfo.ArrivedQty),
                                CommonRepository.GetValueAsString(theBatch.IUM),
                                CommonRepository.GetValueAsString(AcceptInfo.Warehouse),
                                CommonRepository.GetValueAsString(AcceptInfo.BinNum),
                                CommonRepository.GetValueAsString(theBatch.BatchNo),
                                CommonRepository.GetValueAsString(theBatch.JobNum),
                                CommonRepository.GetValueAsString(theBatch.AssemblySeq),
                                CommonRepository.GetValueAsString(dt.Rows[i]["jobseq"]),
                                CommonRepository.GetValueAsString(theBatch.CommentText.Replace('\'','"')),
                                CommonRepository.GetValueAsString(theBatch.TranType),
                                CommonRepository.GetValueAsString(theBatch.HeatNum)}) + (i == dt.Rows.Count - 1 ? "]" : ",");
                            }

                            //(res = ErpApi.porcv(packnum, recdate, vendorid, rcvdtlStr, "", companyId)) == "1|处理成功."
                            if ((res = ErpAPI.ReceiptRepository.porcv(packnum, recdate, vendorid, rcvdtlStr, "", companyId)) == "1|处理成功.")//若回写erp成功， 则更新对应的Receipt记录
                            {
                                for (int i = 0; i < dt.Rows.Count; i++)
                                {
                                    #region sql
                                    if ((int)dt.Rows[i]["jobseq"] != theBatch.JobSeq)
                                    {
                                        sql = "insert into Receipt Values(" +
                                        "'" + theBatch.ReceiptNo + "'," +
                                        "'" + theBatch.SupplierNo + "'," +
                                        "'" + theBatch.SupplierName + "'," +
                                        "'" + ErpAPI.ReceiptRepository.poDes((int)theBatch.PoNum, (int)dt.Rows[i]["PoLine"], (int)dt.Rows[i]["PORelNum"], theBatch.Company) + "'," +
                                        "'" + theBatch.ReceiptDate.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'," +
                                        "'" + theBatch.IQCDate.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'," +
                                        "'" + theBatch.ChooseDate.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'," +
                                        "'" + OpDate + "'," +
                                        "" + theBatch.PoNum + "," +
                                        "" + (int)dt.Rows[i]["poline"] + "," +
                                        "" + (int)dt.Rows[i]["porelnum"] + "," +
                                        "'" + theBatch.PartNum + "'," +
                                        "@PartDesc," +
                                        "'" + theBatch.IUM + "'," +
                                        "'" + theBatch.BatchNo + "'," +
                                        "'" + theBatch.HeatNum + "'," +
                                        "'" + theBatch.JobNum + "'," +
                                        "" + (theBatch.IsAllCheck == true ? 1 : 0) + "," +
                                        "" + theBatch.ReceiveQty1 + "," +
                                        "" + theBatch.ReceiveQty2 + "," +
                                        "" + (Convert.IsDBNull(theBatch.InspectionQty) || theBatch.InspectionQty == null ? "null" : theBatch.InspectionQty.ToString()) + "," +
                                        "" + theBatch.PassedQty + "," +
                                        "" + theBatch.FailedQty + "," +
                                        "" + theBatch.OurFailedQty + "," +
                                        "" + AcceptInfo.ArrivedQty + "," +
                                        "'" + theBatch.Result + "'," +
                                        "'" + theBatch.Remark + "'," +
                                        "'" + AcceptInfo.Warehouse + "'," +
                                        "'" + AcceptInfo.BinNum + "'," +
                                        "'" + theBatch.TranType + "'," +
                                        "'" + theBatch.PartType + "'," +
                                        "" + theBatch.AssemblySeq + "," +
                                        "" + (int)dt.Rows[i]["jobseq"] + "," +
                                        "'" + (string)dt.Rows[i]["OpCode"] + "'," +
                                        "'" + (string)dt.Rows[i]["OpDesc"] + "'," +
                                        "@CommentText," +
                                        "" + 99 + "," +
                                        "'" + theBatch.PartClassDesc + "'," +
                                        "" + RB.NeedReceiptQty + "," +
                                        "" + RB.NotReceiptQty + "," +
                                        "'" + theBatch.Plant + "'," +
                                        "'" + theBatch.Company + "'," +
                                        "" + (theBatch.IsPrint == true ? 1 : 0) + "," +
                                        "'" + theBatch.FirstUserID + "'," +
                                        "'" + theBatch.SecondUserID + "'," +
                                        "'" + theBatch.ThirdUserID + "'," +
                                        "'" + HttpContext.Current.Session["UserId"].ToString() + "'," +
                                        "'" + theBatch.SecondUserGroup + "'," +
                                        "'" + theBatch.ThirdUserGroup + "'," +
                                        "'" + theBatch.FourthUserGroup + "'," +
                                        "" + theBatch.ReturnOne + "," +
                                        "" + theBatch.ReturnTwo + "," +
                                        "" + theBatch.ReturnThree + "," +
                                        "" + (theBatch.IsDelete == true ? 1 : 0) + "," +
                                        "" + 1 + "," +
                                        "" + 1 + "," +
                                        "" + theBatch.AtRole + "," +
                                        "'" + theBatch.NBBatchNo + "'," +
                                        "" + theBatch.Status + "," +
                                        "'" + theBatch.IQCRemark + "'," +
                                        "" + (Convert.ToBoolean(theBatch.IsPrintRcv) == true ? 1 : 0) + "," +
                                        " '" + theBatch.PackSlip + "'," +
                                        " '" + theBatch.POType + "')";
                                        ponum = (int)theBatch.PoNum; poline = (int)dt.Rows[i]["PoLine"]; porel = (int)dt.Rows[i]["PORelNum"];
                                    }
                                    else
                                    {
                                        ponum = (int)theBatch.PoNum; poline = (int)theBatch.PoLine; porel = (int)theBatch.PORelNum;
                                        string Location = ErpAPI.ReceiptRepository.poDes((int)theBatch.PoNum, (int)theBatch.PoLine, (int)theBatch.PORelNum, theBatch.Company);
                                        sql = @"update Receipt set PreStatus = " + theBatch.Status + ", StockDate = '" + OpDate + "', Status = 99, FourthUserID = '{0}', Warehouse = '{1}', BinNum = '{2}', ArrivedQty = {3}, Location = '{4}', IsComplete = 1, opdesc = '{5}',OpCode = '{6}' where ID = " + theBatch.ID + "";
                                        sql = string.Format(sql, HttpContext.Current.Session["UserId"].ToString(), AcceptInfo.Warehouse, AcceptInfo.BinNum, AcceptInfo.ArrivedQty, Location, (string)dt.Rows[i]["OpDesc"], (string)dt.Rows[i]["OpCode"]);
                                    }
                                    #endregion


                                    //为当前工序下的化学品发料
                                    string issue_res = "";  //若issue_res不为空， 则表明虽通过了之前的发料验证，但实际发料时不够发导致失败。    存在解决该问题的算法O(mlgm + m*n)   m：需要发几次物料A    n：物料A所有库存批次
                                    DataTable mtls = CommonRepository.GetMtlsOfOpSeq(theBatch.JobNum, (int)theBatch.AssemblySeq, (int)dt.Rows[i]["jobseq"], theBatch.Company);
                                    if (mtls != null)
                                    {
                                        for (int j = 0; j < mtls.Rows.Count; j++)
                                        {
                                            if (mtls.Rows[j]["partnum"].ToString().Substring(0, 1).Trim().ToLower() == "c")
                                            {
                                                res = ErpAPI.MtlIssueRepository.Issue(theBatch.JobNum, (int)theBatch.AssemblySeq, (int)dt.Rows[i]["jobseq"], (int)mtls.Rows[j]["mtlseq"], mtls.Rows[j]["partnum"].ToString(), (decimal)mtls.Rows[j]["RequiredQty"], DateTime.Parse(OpDate),theBatch.Company,theBatch.Plant);
                                                issue_res += mtls.Rows[j]["partnum"].ToString() + "：";
                                                issue_res += (res == "true") ? (decimal)mtls.Rows[j]["qtyper"] * (decimal)AcceptInfo.ArrivedQty + ", " : res + ", ";
                                            }
                                        }
                                    }

                                    //执行sql
                                    //Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                                    SqlParameter[] ps = new SqlParameter[] { new SqlParameter("@CommentText", theBatch.CommentText), new SqlParameter("@PartDesc", theBatch.PartDesc) };
                                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, ps);

                                    sql = sql.Replace("'", "");
                                    AddOpLog(AcceptInfo.ID, theBatch.BatchNo, 401, sql.Contains("update") ? "update" : "insert", OpDate, sql + "    " + issue_res);
                                }
                            }
                            else
                                return "错误：" + res;


                            int a, b;//凑个数，无意义
                            string c ,d;//凑个数，无意义
                            res = ErpAPI.CommonRepository.getJobNextOprTypes(theBatch.JobNum, (int)theBatch.AssemblySeq, (int)dt.Rows[dt.Rows.Count - 1]["jobseq"], out a, out b, out c, out d, theBatch.Company);

                            if (res.Substring(0, 1).Trim().ToLower() == "p") //工序完成，收货至仓库
                            {
                                res = ErpAPI.CommonRepository.D0506_01(null, theBatch.JobNum, (int)theBatch.AssemblySeq, (decimal)AcceptInfo.ArrivedQty, theBatch.BatchNo, AcceptInfo.Warehouse, AcceptInfo.BinNum, theBatch.Company,theBatch.Plant);
                                if (res != "1|处理成功")
                                    return "错误：" + res;
                            }

                            return "处理成功";
                        }
                    }

                    else
                        return "错误：交易类型错误";
                }
            }
            catch (Exception ex)
            {
                //sql2 = "insert into sqllog(ponum,poline,porel,sql,catch) values(" + ponum + ", " + poline + ", " + porel + ", @sql, @ex) ";
                //SqlParameter[] ps = new SqlParameter[] { new SqlParameter("@sql", sql), new SqlParameter("@ex", ex.Message) };
                //Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql2, ps);

                return "错误：" + ex.Message.ToString();
            }
        }

        #endregion



        #region 功能
        /// <summary>
        /// 返回下个节点的可选人员
        /// </summary>
        /// <returns></returns>
        public static DataTable GetNextUserGroup(long nextRole, string company, string plant, int id)
        {
            DataTable dt = null;
            string sql = null;
            nextRole = GetNextRole(id);

            if (nextRole == 2)
            {
                sql = "select * from userfile where CHARINDEX('" + company + "', company) > 0 and CHARINDEX('" + plant + "', plant) > 0 and disabled = 0 and RoleID & " + nextRole + " != 0 and RoleID != 2147483647";
                dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表
            }
            else if (nextRole == 4)
            {
                sql = "select FirstUserID from receipt where id = " + id + "";
                var FirstUserID = (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = "select * from userfile where CHARINDEX('" + company + "', company) > 0 and CHARINDEX('" + plant + "', plant) > 0 and disabled = 0 and RoleID & " + nextRole + " != 0 and RoleID != 2147483647 and UserID = '" + FirstUserID + "'";
                dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表
            }
            else if (nextRole == 8)//从拥有权值8的人员表中，选出可以操作指定仓库的人
            {
                sql = "select PartNum from receipt where id = " + id + "";
                string PartNum = (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = @"select WarehouseCode from erp.partplant pp  LEFT JOIN erp.Warehse wh on pp.PrimWhse = wh.WarehouseCode and pp.Company = wh.Company and pp.Plant = wh.Plant   
                  where pp.company = '" + company + "'   and pp.plant = '" + plant + "'   and   pp.PartNum = '" + PartNum + "'";

                object Warehouse = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);

                if (Warehouse == null) return null;

                sql = "select UserID,UserName, WhseGroup from userfile where CHARINDEX('" + company + "', company) > 0 and CHARINDEX('" + plant + "', plant) > 0 and disabled = 0 and RoleID & " + nextRole + " != 0 and RoleID != 2147483647";
                dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表

                for (int i = dt.Rows.Count - 1; i >= 0; i--)
                {
                    if (dt.Rows[i]["WhseGroup"] != null)
                    {
                        string[] WhseGroups = dt.Rows[i]["WhseGroup"].ToString().Split(',');
                        if (!WhseGroups.Contains(Warehouse.ToString().Trim()))
                            dt.Rows.RemoveAt(i);
                    }
                }
            }
            else if (nextRole == 16)//
            {
                sql = "select UserID, UserName from userfile where disabled = 0 and  CHARINDEX('" + company + "', company) > 0 and CHARINDEX('" + plant + "', plant) > 0 and RoleID & 16 != 0 and RoleID != 2147483647";
                dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表
            }
            else if (nextRole == 32)//UKN接收人
            {
                sql = "select UserID,UserName, Department from userfile where CHARINDEX('" + company + "', company) > 0 and CHARINDEX('" + plant + "', plant) > 0 and disabled = 0 and RoleID & " + nextRole + " != 0 and RoleID != 2147483647";
                dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表


                sql = "select * from receipt where id = " + id + "";
                DataTable dt2 = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql);

                sql = "select RcvPerson_c from PODetail where company = '{0}' and ponum = {1} and poline = {2}";
                sql = string.Format(sql, dt2.Rows[0]["Company"].ToString(), dt2.Rows[0]["PoNum"], dt2.Rows[0]["PoLine"]);

                var RcvPerson_c = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);


                if (RcvPerson_c == null)
                    return null;


                for (int i = dt.Rows.Count - 1; i >= 0; i--)
                {
                    if (dt.Rows[i]["Department"] != null)
                    {
                        string[] Departments = dt.Rows[i]["Department"].ToString().Split(',');

                        int j;
                        for (j = 0; j < Departments.Length; j++)
                        {
                            if (RcvPerson_c.ToString().Contains(Departments[j].Trim()))
                                break;
                        }

                        if (j == Departments.Length)
                            dt.Rows.RemoveAt(i);
                    }
                    else
                        dt.Rows.RemoveAt(i);
                }
            }
            else if (nextRole == 64)//
            {
                sql = "select * from Receipt where ID = " + id + "";
                dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql);

                Receipt theBatch = CommonRepository.DataTableToList<Receipt>(dt).First();

                int jobseq = GetLastOpSeqOfSeriesSUB(theBatch);
                int a, b;//凑个数，无意义
                string c;//凑个数，无意义
                string OpCode, res;
                res = ErpAPI.CommonRepository.getJobNextOprTypes(theBatch.JobNum, (int)theBatch.AssemblySeq, jobseq, out a, out b, out OpCode, out c,theBatch.Company);

                sql = "select UserID,UserName from OpCodeUser where opcode = '" + OpCode + "' and  CHARINDEX('" + company + "', company) > 0 and CHARINDEX('" + plant + "', plant) > 0  and disabled = 0";
                dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表
            }

            return dt == null || dt.Rows.Count == 0 ? null : dt;
        }



        /// <summary>
        /// 流程回退到上一个节点
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="oristatus"></param>
        /// <param name="ReasonID"></param>
        /// <returns></returns>
        public static string ReturnStatus(int ID, int oristatus, int ReasonID, string remark, int apinum)
        {
            string OpDetail = "", OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string sql = "select status, isdelete, batchno, iscomplete from Receipt where ID = " + ID + " ";
            DataTable theBatch = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql); //获取ID所指定的批次的 status，isdelete字段值


            if ((bool)theBatch.Rows[0]["isdelete"] == true)
                return "错误：该批次的流程已删除";

            else if ((bool)(theBatch.Rows[0]["isComplete"]) == true)
                return "错误：该批次的流程已结束";

            else if ((int)(theBatch.Rows[0]["status"]) != oristatus)
                return "错误：流程未在当前节点上，在 " + theBatch.Rows[0]["status"].ToString() + "节点";


            if (oristatus == 4)
            {
                sql = "update receipt set fourthusergroup=null where id = " + ID + "";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                Return(3, "ReturnThree", (string)theBatch.Rows[0]["batchno"], OpDate, ReasonID, remark, 4);
            }
            else if (oristatus == 3)
            {
                sql = "update receipt set  thirdusergroup=null, thirduserid=null, choosedate=null where id = " + ID + "";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                Return(2, "ReturnTwo", (string)theBatch.Rows[0]["batchno"], OpDate, ReasonID, remark, 2);
            }
            else if (oristatus == 2)
            {
                sql = "update receipt set ReceiveQty2 =null, NBBatchNo = null,  InspectionQty = null, passedqty=null, failedqty=null, isallcheck=null, result=null, secondusergroup=null, seconduserid=null, iqcdate=null where id = " + ID + "";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                Return(1, "ReturnOne", (string)theBatch.Rows[0]["batchno"], OpDate, ReasonID, remark, 1);
            }
            else //oristatus == 1  
            {
                sql = "select * from IQCFile where batchno = '" + (string)theBatch.Rows[0]["batchno"] + "' ";
                DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql);

                if (dt != null)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        if (FTPRepository.DeleteFile("ftp://" + (((string)dt.Rows[i]["FilePath"]).Replace('\\', '/')), (string)dt.Rows[i]["FileName"]) == true)
                        {
                            AddOpLog(ID, (string)theBatch.Rows[0]["batchno"], apinum, "delete", OpDate, "回退自动删除|" + (string)dt.Rows[i]["FilePath"] + (string)dt.Rows[i]["FileName"]);
                            continue;
                        }
                        else
                            return "错误：回退失败，删除现有报告时出错，请重试";
                    }
                    sql = "delete from IQCFile where batchno = '" + (string)theBatch.Rows[0]["batchno"] + "'";
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                }

                sql = @"update Receipt set isdelete = 1  where ID = " + ID + " ";   // 把该批次的流程标记为已删除
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
            }


            AddOpLog(ID, (string)theBatch.Rows[0]["batchno"], apinum, "return", OpDate, "从" + oristatus.ToString() + "回退成功");
            return "处理成功";
        }


        public static IEnumerable<Reason> GetReason()
        {
            string sql = "select * from Reason";
            List<Reason> Reasons = CommonRepository.DataTableToList<Reason>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql));

            return Reasons;
        }



        public static DataTable GetWarehouse(string partnum)
        {
            string sql = @"select erp.Warehse.WarehouseCode,  Description  from erp.PartBin left join erp.Warehse on erp.PartBin.WarehouseCode = erp.Warehse.WarehouseCode  where PartNum = '" + partnum+"'";
            return Common.SQLRepository.ExecuteQueryToDataTable(SQLRepository.ERP_strConn, sql);
        }



        /// <summary>
        /// 补全扫描原始数据
        /// </summary>
        /// <param name="values"></param>
        /// <returns>返回16个值，用~隔开</returns>
        public static string ParseQRValues(string values)
        {
            string[] arr = values.Split('~');

            if (arr.Length == 14) //刚好13个波浪线， 扫码收货时解析
            {
                string sql = @"select 
                pr.plant,
                vd.Name,
                pr.JobNum,
                pd.IUM,
                pd.LineDesc as  PartDesc
                from erp.PORel pr    
                left join erp.PODetail pd   on pr.PONum = pd.PONUM   and   pr.Company = pd.Company   and   pr.POLine = pd.POLine 
                left join erp.POHeader ph   on ph.Company = pr.Company   and   ph.PONum = pr.PONUM                 
                left join erp.Vendor vd     on ph.VendorNum = vd.VendorNum   and   ph.company = vd.company             
                where pr.Company = '" + arr[0] + "'   and   pr.ponum = " + int.Parse(arr[8]) + "   and   pr.poline = " + int.Parse(arr[9]) + "  and pr.porelnum = " + int.Parse(arr[11]) + " ";

                DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);
                arr[2] = Convert.ToString(dt.Rows[0]["PartDesc"]);
                arr[4] = Convert.ToString(dt.Rows[0]["JobNum"]);


                values = "";
                for (int i = 0; i < arr.Length; i++)
                    values += arr[i] + "~";


                values += (string)dt.Rows[0]["plant"] + "~" + (string)dt.Rows[0]["Name"] + "~" + Convert.ToString(dt.Rows[0]["IUM"]);

                return values;
            }
            else if (arr.Length == 5) //4个波浪线， 无二维码获取详情页面数据
            {
                string sql = @"select 
                pr.JobNum, 
                pr.PORelNum,
                pr.PoNum,
                pr.TranType,
                pr.JobSeq,
                pr.PoLine ,
                pr.Plant,
                pr.Company,
                pd.CommentText,
                pd.PartNum,
                pd.IUM,
                vd.VendorID  as SupplierNo,
                vd.Name as SupplierName,
                pa.TypeCode  as  PartType,
                pd.LineDesc  as  PartDesc,
                pr.AssemblySeq                                       
                from erp.PORel pr
                left join erp.PODetail pd   on pr.PONum = pd.PONUM   and   pr.Company = pd.Company   and   pr.POLine = pd.POLine 
                left join erp.POHeader ph   on ph.Company = pd.Company   and   ph.PONum = pd.PONUM                 
                left join erp.Vendor vd     on ph.VendorNum = vd.VendorNum   and   ph.company = vd.company             
                left join erp.part pa       on pd.PartNum = pa.PartNum   and   pa.company = pd.company
                where pr.ponum = {0} and pr.poline = {1} and pr.porelnum = {2} and pr.company = '{3}'";
                sql = string.Format(sql, int.Parse(arr[0]), int.Parse(arr[1]), int.Parse(arr[2]), arr[3]);

                DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);

                values = "";

                values += (string)dt.Rows[0]["Company"] + "~";
                values += (string)dt.Rows[0]["PartNum"] + "~";
                values += dt.Rows[0]["PartDesc"] + "~";
                values += "~";  //batchno

                values += dt.Rows[0]["JobNum"] + "~";
                values += dt.Rows[0]["AssemblySeq"] != null ? dt.Rows[0]["AssemblySeq"].ToString() + "~" : "~";
                values += "~"; //textid
                values += (string)dt.Rows[0]["SupplierNo"] + "~";

                values += dt.Rows[0]["PoNum"] != null ? dt.Rows[0]["PoNum"].ToString() + "~" : "~";
                values += dt.Rows[0]["PoLine"] != null ? dt.Rows[0]["PoLine"].ToString() + "~" : "~";
                values += arr[4] + "~"; //receivQty
                values += dt.Rows[0]["PORelNum"] != null ? dt.Rows[0]["PORelNum"].ToString() + "~" : "~";

                values += dt.Rows[0]["JobSeq"] != null ? dt.Rows[0]["JobSeq"].ToString() + "~" : "~";
                values += "~"; //heatnum
                values += (string)dt.Rows[0]["Plant"] + "~";
                values += (string)dt.Rows[0]["SupplierName"] + "~";
                values += Convert.ToString(dt.Rows[0]["IUM"]);

                return values;
            }

            return null; //不是13个百分号
        }



        /// <summary>
        /// 获取当前用户的待办事项
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Receipt> GetRemainsOfUser()
        {
            string sql = @"select * from Receipt where AtRole & {0} != 0 and isdelete != 1 and isComplete != 1 and CHARINDEX(Company, '{1}') > 0   and   CHARINDEX(Plant, '{2}') > 0 order by ReceiptDate desc";
            sql = string.Format(sql, (int)HttpContext.Current.Session["RoleId"], HttpContext.Current.Session["Company"].ToString(), HttpContext.Current.Session["Plant"].ToString());

            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql);
            if (dt == null)
                return null;


            for (int i = dt.Rows.Count - 1; i >= 0; i--)
            {
                if ((int)(dt.Rows[i]["Status"]) == 1 && (string)dt.Rows[i]["FirstUserID"] == (string)HttpContext.Current.Session["UserId"]) continue;

                else if ((int)dt.Rows[i]["Status"] == 2 && (dt.Rows[i]["SecondUserGroup"].ToString()).Contains(HttpContext.Current.Session["UserId"].ToString()))
                {
                    if (!Convert.IsDBNull(dt.Rows[i]["SecondUserID"]) && (string)HttpContext.Current.Session["UserId"] != (string)dt.Rows[i]["SecondUserID"])
                        dt.Rows[i].Delete();
                }

                else if ((int)dt.Rows[i]["Status"] == 3 && ((string)dt.Rows[i]["ThirdUserGroup"]).Contains((string)HttpContext.Current.Session["UserId"]))
                {
                    if (!Convert.IsDBNull(dt.Rows[i]["ThirdUserID"]) && (string)HttpContext.Current.Session["UserId"] != (string)dt.Rows[i]["ThirdUserID"])
                        dt.Rows[i].Delete();
                }
                else if ((int)dt.Rows[i]["Status"] == 4 && ((string)dt.Rows[i]["FourthUserGroup"]).Contains((string)HttpContext.Current.Session["UserId"])) continue;

                else
                    dt.Rows[i].Delete();//当前节点群组未包含改用户
            }
            List<Receipt> RBs = CommonRepository.DataTableToList<Receipt>(dt);

            if (RBs == null)
                return null;

            foreach (var i in RBs)
            {
                //int sd;
                //if (i.BatchNo == "P320181206134" && i.PoLine == 3)
                //     sd= i.ID;
                i.Colour = GetColour(i);
                if (i.Colour != 0) //不等于白色，则需设置回退原因
                    SetReturnReason(i);
                else
                    i.ReturnReason = i.ReturnReasonRemark = "";
            }

            return RBs;
            //return GetValidBatchs(RBs);
        }



        public static IEnumerable<Receipt> GetRecordByCondition(Receipt Condition)
        {
            string sql = "";
            if (Condition.IsForPrintQR)
                sql = @"SELECT * FROM [APP].[dbo].[Receipt] where IsDelete!=1";
            else
                sql = @"SELECT * FROM [APP].[dbo].[Receipt] where IsDelete!=1 and IsComplete != 1";


            if (Condition.PoNum != null)
                sql += "and ponum = " + Condition.PoNum + " ";
            if (Condition.PoLine != null)
                sql += "and poline = " + Condition.PoLine + " ";
            if (Condition.PartNum != null && Condition.PartNum != "")
                sql += "and partnum like '%" + Condition.PartNum + "%' ";
            if (Condition.PartDesc != null && Condition.PartDesc != "")
                sql += "and PartDesc like '%" + Condition.PartDesc + "%' ";
            if (Condition.BatchNo != null && Condition.BatchNo != "")
                sql += "and BatchNo like '%" + Condition.BatchNo + "%' ";
            if (Condition.SupplierNo != null && Condition.SupplierNo != "")
                sql += "and SupplierNo like '%" + Condition.SupplierNo + "%' ";
            if (Condition.IsRestrictRcv == true)
                sql += "and (IsPrintRcv is null or IsPrintRcv = 0)  ";

            sql += " order by ReceiptDate desc";

            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql);

            return CommonRepository.DataTableToList<Receipt>(dt);
        }



        public static bool SetIsPrintRcv(int ReceiptID)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string sql = @"update Receipt set IsPrintRcv = 1 where ID = " + ReceiptID + "";

            AddOpLog(ReceiptID, null, 12, "update", OpDate, "打印暂收单");

            return Convert.ToBoolean(Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null));
        }


        public static bool ForceComplete(Receipt IQCInfo)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string sql = @"update Receipt set SecondUserID = '"+ HttpContext.Current.Session["UserId"].ToString() + "', IQCDate = '" + OpDate + "', IsAllCheck = {0},  InspectionQty = {1}, PassedQty = {2}, FailedQty = {3}, isComplete = 1, IQCRemark = '" + IQCInfo.IQCRemark + "' where ID = {4}";
            sql = string.Format(sql, Convert.ToInt32(IQCInfo.IsAllCheck), (IQCInfo.InspectionQty) == -1 ? "null" : IQCInfo.InspectionQty.ToString(), IQCInfo.PassedQty, IQCInfo.FailedQty, IQCInfo.ID);


            AddOpLog(IQCInfo.ID, null, 20, "update", OpDate, "全退");

            return Convert.ToBoolean(Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null));
        }



        public static DataTable GetRecordByID(int ReceiptID)
        {
            if (ReceiptID == -1) return null;

            string sql = "select * from Receipt where ID = " + ReceiptID + "";
            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql);


            sql = @"select WarehouseCode, wh.Description from erp.partplant pp  LEFT JOIN erp.Warehse wh on pp.PrimWhse = wh.WarehouseCode and pp.Company = wh.Company and pp.Plant = wh.Plant   
                  where pp.company = '" + dt.Rows[0]["Company"].ToString() + "'   and pp.plant = '" + dt.Rows[0]["Plant"].ToString() + "'   and   pp.PartNum = '" + dt.Rows[0]["PartNum"].ToString() + "'";
            DataTable d2 = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);

            Receipt theBatch = CommonRepository.DataTableToList<Receipt>(dt).First();


            if (d2 != null)
            {
                theBatch.Warehouse = d2.Rows[0]["WarehouseCode"].ToString();
                theBatch.WarehouseName = d2.Rows[0]["Description"].ToString();
            }

            theBatch.Colour = GetColour(theBatch);
            if (theBatch.Colour != 0) //不等于白色，则需设置回退原因
                SetReturnReason(theBatch);
            else
                theBatch.ReturnReason = theBatch.ReturnReasonRemark = "";


            List<Receipt> receipts = new List<Receipt>();
            receipts.Add(theBatch);

            return CommonRepository.ListToTable(receipts);
        }



        public static string PrintQR(Receipt info)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string jsonStr = " text1: '{0}', text2: '{12}', text3: '{1}', text4: '{2}', text5: '{3}', text6: '', text7: '{4}', text8: '{5}', text9: '{6}', text10: '{7}', text11: '{8}', text12: '{9}', text13: '', text14: '{10}', text15: '{11}', text16: '', text17: '', text18: '', text19: '', text20: '', text21: '', text22: '', text23: '', text24: '', text25: '', text26: '', text27: '', text28: '', text29: '', text30: '' ";
            jsonStr = string.Format(jsonStr, info.PartNum, info.BatchNo, CommonRepository.GetValueAsString(info.JobNum), CommonRepository.GetValueAsString(info.AssemblySeq), info.SupplierNo, info.PoNum, info.PoLine, info.ReceiveQty1, info.PORelNum, info.Company, CommonRepository.GetValueAsString(info.JobSeq), info.HeatNum, info.PartDesc);
            jsonStr = "[{" + jsonStr + "}]";


            string sql = "select Printer from Userprinter where userid = '" + HttpContext.Current.Session["UserId"].ToString() + "' ";
            string printer = (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
            string res = "";
            ServiceReference_Print.WebServiceSoapClient client = new ServiceReference_Print.WebServiceSoapClient();
            if ((res = client.Print(@"C:\D0201.btw", printer, (int)info.PrintQty, jsonStr)) == "1|处理成功")
            {
                client.Close();
                AddOpLog(null, info.BatchNo, 11, "update", OpDate, "复制二维码");
                return "处理成功";
            }
            else
            {
                client.Close();
                return "错误：打印失败  " + res;
            }
        }


        public static DataTable GetPartRecords(string partnum)
        {
            string sql = @"select * from erp.Partbin pb  where pb.partnum = '" + partnum + "' ";
            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);

            return dt;
        }


        public static ScanResult GetRecordByQR(string values, bool IsForPrintQR) //只根据APP数据库中的数据检查是否有操作权限
        {
            ScanResult sr = new ScanResult();
            sr.batch = null;
            sr.error = null;


            string[] arr = values.Split('~');
            DataTable dt = GetRecordByID(GetReceiptID(new Receipt { PoNum = int.Parse(arr[8]), PoLine = int.Parse(arr[9]), PORelNum = int.Parse(arr[11]), Company = arr[0], BatchNo = arr[3] }));

            if (dt == null)
            {
                sr.error = "错误：不存在该批次记录";
                return sr;
            }


            Receipt theBatch = CommonRepository.DataTableToList<Receipt>(dt).First();
            theBatch.Colour = GetColour(theBatch);

            if (!IsForPrintQR)
            {
                if(!HttpContext.Current.Session["Company"].ToString().Contains(theBatch.Company))
                    sr.error = "错误：该账号没有相应的公司权限";
                if (!HttpContext.Current.Session["Plant"].ToString().Contains(theBatch.Plant))
                    sr.error = "错误：该账号没有相应的工厂权限";
                if ((theBatch.AtRole & (int)HttpContext.Current.Session["RoleId"]) == 0)
                {
                    sr.error = "错误：当前批次的流程未在你的节点 或 你的角色无权操作当前批次";
                }
                else
                {
                    if ((int)dt.Rows[0]["Status"] == 1 && (string)dt.Rows[0]["FirstUserID"] == (string)HttpContext.Current.Session["UserId"])
                        sr.batch = theBatch;

                    else if ((int)dt.Rows[0]["Status"] == 2 && ((string)dt.Rows[0]["SecondUserGroup"]).Contains((string)HttpContext.Current.Session["UserId"]))
                    {
                        if (!Convert.IsDBNull(dt.Rows[0]["SecondUserID"]) && (string)HttpContext.Current.Session["UserId"] != (string)dt.Rows[0]["SecondUserID"])
                            sr.error = "错误：需指定的办理人处理";
                        else
                            sr.batch = theBatch;
                    }

                    else if ((int)dt.Rows[0]["Status"] == 3 && ((string)dt.Rows[0]["ThirdUserGroup"]).Contains((string)HttpContext.Current.Session["UserId"]))
                    {
                        if (!Convert.IsDBNull(dt.Rows[0]["ThirdUserID"]) && (string)HttpContext.Current.Session["UserId"] != (string)dt.Rows[0]["ThirdUserID"])
                            sr.error = "错误：需指定的办理人处理";
                        else
                            sr.batch = theBatch;
                    }
                    else if ((int)dt.Rows[0]["Status"] == 4 && ((string)dt.Rows[0]["FourthUserGroup"]).Contains((string)HttpContext.Current.Session["UserId"]))
                        sr.batch = theBatch;

                    else
                        sr.error = "错误：无权处理，账号不在节点的处理人列表中";
                }
            }
            else
                sr.batch = theBatch;

            return sr;
        }



        public static DataTable GetFileList(string batchno)
        {
            string sql = "select * from IQCfile where batchno = '" + batchno + "' ";
            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql);

            return dt;
        }



        public static bool DeleteIQCFile(int id, string filePath, string filename) //ApiNum: 15   winform    删除指定批次的单个IQC文件
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");


            if (FTPRepository.DeleteFile("ftp://" + filePath, filename) == true)
            {
                string sql = "delete from IQCFile where id = " + id + "";
                Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = "select  batchno from IQCFile where id = " + id + "";
                string batchno = (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

                AddOpLog(null, batchno, 15, "delete", OpDate, "手动删除|" + filePath + filename);

                return true;
            }
            else
                return false;
        }




        //public static void GenerateQRCode(int width, int height, string value)
        //{
        //    BarcodeWriter barCodeWriter = new BarcodeWriter();
        //    barCodeWriter.Format = BarcodeFormat.QR_CODE; // 生成码的方式(这里设置的是二维码),有条形码\二维码\还有中间嵌入图片的二维码等
        //    barCodeWriter.Options.Hints.Add(EncodeHintType.CHARACTER_SET, "UTF-8");
        //    barCodeWriter.Options.Hints.Add(EncodeHintType.ERROR_CORRECTION, ZXing.QrCode.Internal.ErrorCorrectionLevel.H);
        //    barCodeWriter.Options.Height = height;
        //    barCodeWriter.Options.Width = width;
        //    barCodeWriter.Options.Margin = 2; //设置的白边大小
        //    ZXing.Common.BitMatrix bm = barCodeWriter.Encode("asdkjhasjdh342k3jhkj54345");  //要生成的二维码字符串
        //    Bitmap result = barCodeWriter.Write(bm);
        //    Bitmap Qcbmp = result.Clone(new Rectangle(Point.Empty, result.Size), PixelFormat.Format1bppIndexed);//位深度

        //    //pictureBox1.Image = Qcbmp;
        //    //pictureBox1.Refresh();
        //}




        public static string GetValueForTranStk_1(string oristr)
        {
            string[] arr = oristr.Split('~');

            string sql = @"select 
                sum(onhandqty)
                from erp.Partbin pb           
                where pb.company = '" + arr[0] + "' and pb.partnum = '" + arr[1] + "' ";

            object sum = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);

            if (sum == null || (decimal)sum < 1)
                return null; //库存不存在   

            sql = @"select dimcode from erp.Partbin pb where pb.company = '" + arr[0] + "' and pb.partnum = '" + arr[1] + "' ";

            object dimcode = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);

            sql = @"select PartDescription from erp.Part where company = '" + arr[0] + "' and  partnum = '" + arr[1] + "' ";

            object partdesc = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);

            return arr[1] + "~" + Convert.ToString(partdesc) + "~" + sum.ToString() + "~" + arr[0] + "~" + dimcode.ToString();   //partnum~partdesc~onhandqty~company~dimcode
        }



        public static DataTable GetValueForTranStk_2(dynamic para)
        {
            string sql = @"select 
                BinNum, LotNum, OnhandQty
                from erp.Partbin pb           
                where pb.company = '" + Convert.ToString(para.Company) + "' and pb.WarehouseCode = '" + Convert.ToString(para.WarehouseCode) + "' and pb.PartNum = '" + Convert.ToString(para.PartNum) + "' ";

            if (Convert.ToString(para.BinNum) != "")
                sql += "and BinNum = '" + Convert.ToString(para.BinNum) + "' ";
            if (Convert.ToString(para.LotNum) != "")
                sql += "and LotNum = '" + Convert.ToString(para.LotNum) + "' ";

            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);

            return dt;
        }



        public static DataTable GetAllCommentTextOfSeriesSUB(int PoNum, string JobNum, int AssemblySeq, string Company) //获取所有连续委外工序的描述
        {
            DataTable dt = GetAllOpSeqOfSeriesSUB(new Receipt { PoNum = PoNum, JobNum = JobNum, AssemblySeq = AssemblySeq, Company = Company });

            if (dt == null) return null;

            dt.Columns.Add("CommentText", typeof(string));
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                dt.Rows[i]["CommentText"] = GetCommentText((int)dt.Rows[i]["ponum"], (int)dt.Rows[i]["poline"], (string)dt.Rows[i]["company"]);
            }

            return dt;
        }



        public static string TranStk(dynamic para)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");


            string tranJson = "'partnum':'{0}', " +
                            "'partdesc':'{1}', " +
                            "'fromWHcode':'{2}', " +
                            "'fromWHname':'{3}'," +
                            "'fromBinNum':'{4}'," +
                            "'fromBinName':'{5}'," +
                            "'toWHcode':'{6}'," +
                            "'toWHname':'{7}'," +
                            "'toBinNum':'{8}'," +
                            "'toBinName':'{9}'," +
                            "'lotnum':'{10}'," +
                            "'tranQty':'{11}'," +
                            "'uom':'{12}'";
            tranJson = string.Format(tranJson,
                Convert.ToString(para.PartNum),
                Convert.ToString(para.PartDesc),
                Convert.ToString(para.Warehouse),
                Convert.ToString(para.Warehouse),
                Convert.ToString(para.BinNum),
                Convert.ToString(para.BinNum),
                Convert.ToString(para.Warehouse),
                Convert.ToString(para.Warehouse),
                Convert.ToString(para.ToBinNum),
                Convert.ToString(para.ToBinNum),
                Convert.ToString(para.LotNum),
                Convert.ToString(para.ToQty),
                Convert.ToString(para.uom));


            tranJson = "[{" + tranJson + "}]";



            string Warehouse = Convert.ToString(para.Warehouse);


            string res = ErpAPI.ReceiptRepository.tranStk(tranJson, System.Convert.ToString(para.Company), Warehouse.Contains("RR") ? "RRSite" : "MfgSys");

            if (res.Substring(0, 1) == "1")
            {
                tranJson = tranJson.Replace("'", "");
                AddOpLog(null, null, 18, "update", OpDate, "转仓|" + tranJson);

                return "处理成功";
            }

            return "错误：" + res;
        }
        #endregion



        public static void AddOpLog(int? ReceiptId, string batchno, int ApiNum, string OpType, string OpDate, string OpDetail)
        {
            string sql = @"insert into OpLog(ReceiptId,  UserId, Opdate, ApiNum, OpType, OpDetail,batchno) Values({0}, '{1}', '{2}', {3}, '{4}', '{5}', {6}) ";
            sql = string.Format(sql, ReceiptId == null ? "null" : ReceiptId.ToString(), ApiNum != 12 ? Convert.ToString(HttpContext.Current.Session["UserId"]) : "102543", OpDate, ApiNum, OpType, OpDetail, batchno != null ? "'" + batchno + "'" : "null");

            Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
        }//添加操作记录


    }
}