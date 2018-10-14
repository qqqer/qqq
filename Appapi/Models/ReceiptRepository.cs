﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Collections;
using System.Timers;
using System.Net; //ftp
using System.Threading;
using EpicorAPIManager;

namespace Appapi.Models
{
    //[System.Web.Script.Services.ScriptService]
    public static class ReceiptRepository
    {
        
        #region  重用函数（非接口）
        private static string ConstructValues(ArrayList array)
        {
            string values = "";
            for (int i = 0; i < array.Count; i++)
            {
                if (array[i] == null)
                    values += "null,";
                else if (array[i].GetType() == typeof(int) || array[i].GetType() == typeof(decimal))
                {
                    values += array[i].ToString() + (i == array.Count - 1 ? "" : ",");
                }
                else if (array[i].GetType() == typeof(string))
                {
                    values += "'" + array[i] + "'" + (i == array.Count - 1 ? "" : ",");
                }
                else if (array[i].GetType() == typeof(bool))
                {
                    values += Convert.ToInt32(array[i]).ToString() + (i == array.Count - 1 ? "" : ",");
                }
                else if (array[i].GetType() == typeof(DateTime))
                {
                    values += "'" + array[i].ToString() + "'" + (i == array.Count - 1 ? "" : ",");
                }
            }
            return values;
        }//生成inser into语句中的values部分。为了方便处理string类型参数的两种情况：string不为null时需加'', 而string为null时则不必加'' 


        /// <summary>
        /// 为改变了值的字段生成操作日志详情
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        private static string GetOpDetail(string keys, string values)
        {
            string[] arrKey = keys.Split('|');
            string[] arrValue = keys.Split('|');

            string OpDetail = string.Empty;

            for (int i = 0; i < arrKey.Count(); i++)
            {
                OpDetail += arrKey[i] + "：" + arrValue[i] + (i == arrKey.Count() - 1 ? "" : "，");
            }//每个字段名对应它的值

            return OpDetail;
        }



        /// <summary>
        /// 获取该批次号在Receipt表中的ID
        /// </summary>
        /// <param name="batInfo"></param>
        /// <returns></returns>
        private static int GetReceiptID(Receipt batInfo)
        {
            string sql = "select ID from Receipt where PoNum = {0} and  PoLine = {1} and PORelNum = {2} and Company = '{3}' and  BatchNo = '{4}' ";
            sql = string.Format(sql, batInfo.PoNum, batInfo.PoLine, batInfo.PORelNum,  batInfo.Company, batInfo.BatchNo);

            return (int)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);
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

            for (int i = 0; i < batchs.Count; i++)
            {
                if (GetReceivingBasis(batchs[i]) == null) //若当前待办批次所属的收货依据无效则去掉该待办批次
                {
                    batchs.RemoveAt(i);
                }
            }//筛选待办批次。

            return batchs.Count > 0 ? batchs : null;
        }


        private static void Return(int previousStatus, string returnNum, string batchno, string OpDate, int ReasonID)
        {
            //更新该批次的status为上一个节点值，指定的回退编号次数+1
            string sql = @"update Receipt set status = " + previousStatus + ", " + returnNum + " = " + returnNum + "+1 where where batchno = '" + batchno + "' ";
            SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

            //获取更新后的回退编号
            sql = "select " + returnNum + " from Receipt where where batchno = '" + batchno + "' ";
            int c = (int)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

            //把该回退编号的原因插入到ReasonRecord表中
            sql = @"insert into ReasonRecord(batchno, " + returnNum + ", ReturnReasonId, Date) Values('" + batchno + "', " + c + ", " + ReasonID + ", '" + OpDate + "')";
            SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
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
                pd.openLine,
                pr.openRelease,
                pr.TranType,
                jh.jobClosed,
                jh.jobComplete,
                jh.JobHeld
               
                from erp.PORel pr

                left join erp.PODetail pd   on pr.PONum = pd.PONUM   and   pr.Company = pd.Company   and   pr.POLine = pd.POLine 
                left join erp.POHeader ph   on ph.Company = pd.Company   and   ph.PONum = pd.PONUM 
                left join erp.JobHead jh  on pr.JobNum = jh.JobNum   and   pr.Company = jh.Company 

                where pr.Company = '" + batInfo.Company + "'   and    pr.Plant = '" + batInfo.Plant + "' " +
                "and  pr.PONum = " + batInfo.PoNum + "   and    pr.POLine = " + batInfo.PoLine + "    and    pr.PORelNum = " + batInfo.PORelNum + " ";

            DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.ERP_strConn, sql);


            if (dt == null)
                return "该订单项目不存在";

            if ((bool)dt.Rows[0]["OpenOrder"] == false)
                return "订单已关闭";
            else if ((bool)dt.Rows[0]["orderHeld"] == true)
                return "订单已冻结";
            else if ((bool)dt.Rows[0]["openLine"] == false)
                return "订单行已关闭";
            else if ((bool)dt.Rows[0]["openRelease"] == false)
                return "发货行已关闭";
            else if ((string)dt.Rows[0]["TranType"] != "PUR-UKN" && (string)dt.Rows[0]["TranType"] != "PUR-STK") //是外协或工单物料， 需要判断与之关联的工单状态
            {
                if ((bool)dt.Rows[0]["jobClosed"] == true)
                    return "关联的工单已关闭";
                else if ((bool)dt.Rows[0]["jobComplete"] == true)
                    return "关联的工单已完成";
                else if ((bool)dt.Rows[0]["JobHeld"] == true)
                    return "关联的工单已冻结";
            }

            return "其他错误";
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
                pa.PartDescription  as  PartDesc,
                pr.AssemblySeq,               
                jh.jobClosed,
                jh.jobComplete,
                jh.JobHeld,
                pc.Description   as  PartClassDesc,
                (pr.XRelQty-pr.ArrivedQty) NeedReceiptQty, 
                pp.PrimWhse as Warehouse
                 from erp.PORel pr
                left join erp.PODetail pd   on pr.PONum = pd.PONUM   and   pr.Company = pd.Company   and   pr.POLine = pd.POLine 
                left join erp.POHeader ph   on ph.Company = pd.Company   and   ph.PONum = pd.PONUM                 
                left join erp.JobHead jh  on pr.JobNum = jh.JobNum   and   pr.Company = jh.Company
                left join erp.Vendor vd     on ph.VendorNum = vd.VendorNum   and   ph.company = vd.company             
                left join erp.part pa       on pd.PartNum = pa.PartNum   and   pa.company = pd.company
                left join erp.partclass pc  on pc.classid = pd.ClassID   and   pc.company = pd.company
                left join erp.partplant pp  on pp.company = pr.Company   and   pp.plant = pr.plant   and   pp.PartNum = pd.PartNum
                where CHARINDEX(pr.Company, '" + HttpContext.Current.Session["Company"].ToString() + "') > 0   and    CHARINDEX(pr.Plant, '" + HttpContext.Current.Session["Plant"].ToString() + "') > 0" +
                "and  ph.OpenOrder = 1   and    ph.orderHeld != 1    and    pd.openLine = 1     and      pr.openRelease = 1   ";

            if (Condition.PoNum != null)
                sql += "and pr.ponum = " + Condition.PoNum + " ";
            if (Condition.PoLine != null)
                sql += "and pr.poline = " + Condition.PoLine + " ";
            if (Condition.PORelNum != null)
                sql += "and pr.PORelNum = " + Condition.PORelNum + " ";
            if (Condition.PartNum != null)
                sql += "and pd.partnum like '%" + Condition.PartNum + "%' ";
            if (Condition.PartDesc != null && Condition.PartDesc != "")
                sql += "and pd.LineDesc like '%" + Condition.PartDesc + "%' ";
            if (Condition.Company != null)
                sql += "and pr.Company = '" + Condition.Company + "' ";
            if (Condition.Plant != null)
                sql += "and pr.Plant = '" + Condition.Plant + "' ";
            #endregion

            DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.ERP_strConn, sql); //获取可能有效的收货依据

            if (dt == null) //没有找到可能有效的收货依据
                return null;


            //筛选可能有效的收货依据，以得到最终有效的收货依据表。     
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                //如果该收货依据是外协或工单物料 ，且关联的工单已完成或关闭或冻结 则排除该收货依据
                if (((string)dt.Rows[i]["TranType"] != "PUR-STK" && (string)dt.Rows[i]["TranType"] != "PUR-UKN") && ((bool)dt.Rows[i]["jobClosed"] == true || (bool)dt.Rows[i]["jobComplete"] == true || (bool)dt.Rows[i]["JobHeld"] == true))
                    dt.Rows.RemoveAt(i);
            }
            List<Receipt> RBs = CommonRepository.DataTableToList<Receipt>(dt);



            if (RBs != null)//若经过筛选后收货依据列表不为空， 则计算每个有效收货依据中的还可接收数量NotReceiptQty
            {
                for (int i = 0; i < RBs.Count; i++)
                {
                    sql = "select sum(case when ArrivedQty is null then(case when  ReceiveQty2 is null then ReceiveQty1 else ReceiveQty2 end) else ArrivedQty end) from Receipt " +
                        "where isdelete != 1 and ponum = " + (int)RBs[i].PoNum + " and poline = " + (int)RBs[i].PoLine + " and  PORelNum = " + (int)RBs[i].PORelNum + " and company = '" + RBs[i].Company + "' and plant = '" + RBs[i].Plant + "'";

                    object sum = SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                    RBs[i].NotReceiptQty = (decimal)RBs[i].NeedReceiptQty - (sum is DBNull ? 0 : (decimal)sum);

                }
            }

            return RBs;
        }


        /// <summary>
        /// 无二维码收货
        /// </summary>
        /// <param name="batInfo"></param>
        /// <returns></returns>
        public static string ReceiveCommitWithNonQRCode(Receipt batInfo)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"); // 获取当前操作时间


            IEnumerable<Receipt> RB = GetReceivingBasis(batInfo); //根据批次信息 获取该批次所属的收货依据

            if (RB == null) //该批次所属的收货依据错误
            {
                return GetErrorInfo(batInfo);
            }

            if (batInfo.ReceiveQty1 > RB.First().NotReceiptQty)//若超收
                return string.Format("超收数量：{0}， 可收数量：{1}", batInfo.ReceiveQty1 - RB.First().NotReceiptQty, RB.First().NotReceiptQty);


            #region 计算批次号
            string sql = "select * from SerialNumber where name = 'BAT'";
            DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql);


            string OriDay = dt.Rows[0]["time"].ToString().Substring(0, 10);//截取从数据库获得的时间的年月日部分
            string today = DateTime.Now.ToString().Substring(0, 10);//截取当前时间的年月日部分


            if (OriDay == today) // 如果从数据库获得的日期 是今天 
            {
                batInfo.BatchNo = "P" + DateTime.Now.ToString("yyyyMMdd") + ((int)dt.Rows[0]["Current"]).ToString("d4");
                dt.Rows[0]["Current"] = (int)dt.Rows[0]["Current"] + 1; //计数器递增1
            }
            else // 不是今天 
            {
                batInfo.BatchNo = "P" + DateTime.Now.ToString("yyyyMMdd") + "0001";
                dt.Rows[0]["Current"] = 1; //计数器重置为1
            }


            sql = "UPDATE SerialNumber SET time = getdate(), current = " + Convert.ToInt32(dt.Rows[0]["Current"]) + " where name = 'BAT'";
            SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
            #endregion

            #region 调用现有接口打印

            string jsonStr = @"{ 'text1': '{0}', 'text2': '', 'text3': '{1}', 'text4': '{2}', 'text5': {3}, 'text6': '', 'text7': '{4}', 'text8': {5}, 'text9': {6}, 
'text10': {7}, 'text11': {8}, 'text12': '{9}', 'text13': '', 'text14': {10}, 'text15': '{11}', 'text16': '', 'text17': '', 'text18': '', 'text19': '', 
'text20': '', 'text21': '', 'text22': '', 'text23': '', 'text24': '', 'text25': '', 'text26': '', 'text27': '', 'text28': '', 'text29': '', 'text30': '' }";
            jsonStr = string.Format(jsonStr, batInfo.PartNum, batInfo.BatchNo, batInfo.JobNum, batInfo.AssemblySeq, batInfo.SupplierNo, batInfo.PoNum, batInfo.PoLine, batInfo.ReceiveQty1, batInfo.PORelNum, batInfo.Company, batInfo.JobSeq, batInfo.HeatNum);



            batInfo.IsPrint = false;
            ServiceReference_Print.WebServiceSoapClient client = new ServiceReference_Print.WebServiceSoapClient();
            if (client.Print(@"c:\test.btw", HttpContext.Current.Session["UserPrinter"].ToString(), 1, jsonStr) == "1|处理成功")
            {
                batInfo.IsPrint = true;
            }
            else
                return "错误：打印失败"; //打印失败
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
                PoNum,
                PoLine,
                PORelNum,
                BatchNo,
                Company,
                Plant,
                IsPrint,
                ReturnOne,
                ReturnTwo,              
                HeatNum,
                Warehouse,
                isdelete,
                isAuto,
                isComplete,
                ReceiptDate
                ) values({0}) ";
                string values = ConstructValues(new ArrayList
                {
                    batInfo.SupplierNo,
                    batInfo.SupplierName,
                    batInfo.ReceiveQty1,
                    batInfo.AssemblySeq,
                    batInfo.JobSeq,
                    batInfo.PartNum,
                    batInfo.PartDesc,
                    batInfo.IUM,
                    batInfo.JobNum,
                    batInfo.Remark,
                    batInfo.TranType,
                    batInfo.PartType,
                    batInfo.OpDesc,
                    batInfo.CommentText,
                    batInfo.PartClassDesc,
                    batInfo.NeedReceiptQty,
                    batInfo.NotReceiptQty,
                    batInfo.SecondUserGroup,
                    HttpContext.Current.Session["UserId"].ToString(),//
                    2,
                    batInfo.PoNum,
                    batInfo.PoLine,
                    batInfo.PORelNum,
                    batInfo.BatchNo,
                    batInfo.Company ,
                    batInfo.Plant ,
                    batInfo.IsPrint,
                    0,
                    0,
                    batInfo.HeatNum,
                    batInfo.Warehouse,
                    0,
                    0,
                    0,
                    OpDate
                });
                sql = string.Format(sql, values);
                #endregion

                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
            }
            #endregion


            //string OpDetail  =  GetOpDetail("")
            // AddOpLog(GetReceiptID(batInfo), 101, "insert", OpDate, OpDetail);

            return "处理成功";
        }


        /// <summary>
        /// 有二维码收货
        /// </summary>
        /// <param name="batInfo"></param>
        /// <returns></returns>
        public static string ReceiveCommitWithQRCode(Receipt batInfo)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"); //获取当前操作时间点

            IEnumerable<Receipt> RB = GetReceivingBasis(batInfo); //获取该批次所属的收货依据

            if (RB == null)//该批次的收货依据错误
                return GetErrorInfo(batInfo);

            if (batInfo.ReceiveQty1 > RB.First().NotReceiptQty)//若超收
                return string.Format("超收数量：{0}， 可收数量：{1}", batInfo.ReceiveQty1 - RB.First().NotReceiptQty, RB.First().NotReceiptQty);


            string sql = "select status, isdelete, iscomplete from Receipt where BatchNo = '" + batInfo.BatchNo + "' ";
            DataTable theBatch = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql);//获取batInfo所指定的批次的status，isdelete字段值

            if (theBatch == null) //batInfo所指定的批次不存在，为该批次生成一条新的receipt记录
            {
                #region 构造sql语句
                sql = @"insert into Receipt(
                        SupplierNo, 
                        SupplierName,                
                        ReceiveQty1,
                        AssemblySeq, 
                        JobSeq,
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
                        Warehouse,
                        isdelete,
                        isAuto,
                        isComplete,
                        ReceiptDate
                        ) values({0}) ";
                string values = ConstructValues(new ArrayList
                {
                    batInfo.SupplierNo,
                    batInfo.SupplierName,
                    batInfo.ReceiveQty1,
                    RB.First().AssemblySeq,
                    RB.First().JobSeq,
                    batInfo.PartNum,
                    batInfo.PartDesc,
                    RB.First().IUM,
                    batInfo.JobNum,
                    batInfo.Remark,
                    RB.First().TranType,
                    RB.First().PartType,
                    RB.First().OpDesc,
                    RB.First().CommentText,
                    RB.First().PartClassDesc,
                    RB.First().NeedReceiptQty,
                    RB.First().NotReceiptQty,
                    batInfo.SecondUserGroup,
                    HttpContext.Current.Session["UserId"].ToString(),
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
                    RB.First().Warehouse,
                    0,
                    0,
                    0,
                    OpDate
                });
                sql = string.Format(sql, values);
                #endregion
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);


                //string OpDetail  =  GetOpDetail("")
                // AddOpLog(GetReceiptID(batInfo), 101, "insert", OpDate, OpDetail);

                return "处理成功";
            }

            else if ((bool)theBatch.Rows[0]["isdelete"] == true)
                return "错误：该批次的流程已删除";

            else if ((bool)theBatch.Rows[0]["isComplete"] == true)
                return "错误：该批次的流程已结束";

            else if ((int)theBatch.Rows[0]["status"] != 1)
                return "错误：流程未在当前节点上";

            else //status == 1  表明在第二届点被退回， 更新批次信息。
            {
                #region 构造sql语句:
                sql = @"update Receipt set
                        SupplierNo = '{0}', 
                        SupplierName = '{1}',
                        ReceiveQty1 = {3},
                        PartNum = '{4}',
                        PartDesc = '{5}',
                        JobNum = '{6}', 
                        Remark = '{7}',
                        SecondUserGroup = '{8}',
                        FirstUserID = '{9}',
                        PoNum = {10},
                        PoLine = 11},
                        PORelNum = {12},
                        BatchNo = '{13}',
                        Company = '{14}',
                        Plant = '{15}',
                        HeatNum = '{16}',
                        Warehouse = '{17}',                       
                        ReceiptDate = '{18}'
                        Status = {19}
                        where ID = {20}";
                sql = string.Format(sql,
                    batInfo.SupplierNo,
                    batInfo.SupplierName,
                    batInfo.ReceiveQty1,
                    batInfo.PartNum,
                    batInfo.PartDesc,
                    batInfo.JobNum,
                    batInfo.Remark,
                    batInfo.SecondUserGroup,
                    batInfo.FirstUserID,
                    batInfo.PoNum,
                    batInfo.PoLine,
                    batInfo.PORelNum,
                    batInfo.BatchNo,
                    batInfo.Company,
                    batInfo.Plant,
                    batInfo.HeatNum,
                    batInfo.Warehouse,
                    OpDate,
                    2,
                    batInfo.ID);
                #endregion
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                //string OpDetail  =  GetOpDetail("")
                //AddOpLog(batInfo.ID, 102, "update", OpDate, OpDetail);

                return "处理成功"; //更新提交成功                
            }

        }

        #endregion



        #region 进料检验

        /// <summary>
        /// 
        /// </summary>
        /// <param name="batInfo"></param>
        /// <returns></returns>
        public static string IQCCommit(Receipt batInfo)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");


            IEnumerable<Receipt> RB = GetReceivingBasis(batInfo); //获取该批次所属的收货依据

            if (RB == null)
                return GetErrorInfo(batInfo);

            if (batInfo.ReceiveQty2 > RB.First().NotReceiptQty)//若超收
                return string.Format("超收数量：{0}， 可收数量：{1}", batInfo.ReceiveQty2 - RB.First().NotReceiptQty, RB.First().NotReceiptQty);


            string sql = "select status，isdelete, iscomplete from Receipt where ID = " + batInfo.ID + "";
            DataTable theBatch = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql); //获取batInfo所指定的批次的status，isdelete字段值


            if ((bool)theBatch.Rows[0]["isdelete"] == true)
                return "错误：该批次的流程已删除";

            else if ((bool)theBatch.Rows[0]["isComplete"] == true)
                return "错误：该批次的流程已结束";

            else if ((int)theBatch.Rows[0]["status"] != 2)
                return "错误：流程未在当前节点上";

            else //status == 2  更新批次信息。
            {
                sql = @"update Receipt set IQCDate = '" + OpDate + "', IsAllCheck = {0},  InspectionQty = {1}, PassedQty = {2}, FailedQty = {3}, Result = '{4}'，Remark = '{5}'，Status=" + batInfo.Status + "，ThirdUserGroup = '{6}', SecondUserID = '{7}', ReceiptNo = '{8}', ReceiveQty2 = {9} where ID = {10}";
                sql = string.Format(sql, batInfo.IsAllCheck, batInfo.InspectionQty, batInfo.PassedQty, batInfo.FailedQty, batInfo.Result, batInfo.Remark, batInfo.ThirdUserGroup, HttpContext.Current.Session["UserId"].ToString(), batInfo.ReceiptNo, batInfo.ReceiveQty2, batInfo.ID);
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);


                //string OpDetail  =  GetOpDetail("")
                //AddOpLog(batInfo.ID, 201, "update", OpDate, OpDetail);

                return "处理成功";
            }
        }



        public static bool UploadIQCFile()
        {
            Thread.Sleep(5);
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            byte[] fileContents = new byte[HttpContext.Current.Request.InputStream.Length];
            HttpContext.Current.Request.InputStream.Read(fileContents, 0, fileContents.Length);


            string fn = HttpContext.Current.Request.Headers.Get("FileName");
            string fileType = fn.Substring(fn.LastIndexOf('.'));
            int ReceiptID = int.Parse(HttpContext.Current.Request.Headers.Get("ReceiptID"));



            string sql = "select batchNo, SupplierName, ponum  from Receipt where ID = " + ReceiptID + " ";
            DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql);


            if (!FtpRepository.IsFolderExist("/", (string)dt.Rows[0]["SupplierName"]))
            {
                FtpRepository.MakeFolder("/", (string)dt.Rows[0]["SupplierName"]);
            }

            if (!FtpRepository.IsFolderExist("/" + (string)dt.Rows[0]["SupplierName"] + "/", dt.Rows[0]["ponum"].ToString()))
            {
                FtpRepository.MakeFolder("/" + (string)dt.Rows[0]["SupplierName"] + "/", dt.Rows[0]["ponum"].ToString());
            }

            string newFileName = (string)dt.Rows[0]["batchNo"] + "_" + OpDate + "_" + HttpContext.Current.Session["UserId"].ToString() + fileType;


            if (FtpRepository.UploadFile(fileContents, "/" + (string)dt.Rows[0]["SupplierName"] + "/" + dt.Rows[0]["ponum"].ToString() + "/", newFileName) == true)
            {
                string FilePath = FtpRepository.ftpServer + "/" + (string)dt.Rows[0]["SupplierName"] + "/" + dt.Rows[0]["ponum"].ToString() + "/";

                sql = @"insert into IQCFile Values('{0}', '{1}', '{2}', '{3}')";
                sql = string.Format(sql, (string)dt.Rows[0]["batchNo"], FilePath, newFileName, OpDate);

                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                return true;
            }

            return false;
        }
        #endregion



        #region 流转
        
        public static string TransferCommit(Receipt batInfo)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");


            IEnumerable<Receipt> RB = GetReceivingBasis(batInfo); //获取该批次所属的收货依据

            if (RB == null)
                return GetErrorInfo(batInfo);


            string sql = "select status，isdelete, iscomplete from Receipt where ID = " + batInfo.ID + "";
            DataTable theBatch = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql); //获取batInfo所指定的批次的status，isdelete字段值


            if ((bool)theBatch.Rows[0]["isdelete"] == true)
                return "错误：该批次的流程已删除";

            else if ((bool)theBatch.Rows[0]["isComplete"] == true)
                return "错误：该批次的流程已结束";

            else if ((int)theBatch.Rows[0]["status"] != 4)
                return "错误：流程未在当前节点上";

            else //status == 4  选人。
            {
                sql = @"update Receipt set ChooseDate = '" + OpDate + "'，Status=" + batInfo.Status + "，FourthUserGroup = '{0}', ThirdUserID = '{1}' where ID = " + batInfo.ID + "";
                sql = string.Format(sql, batInfo.FourthUserGroup, HttpContext.Current.Session["UserId"].ToString());
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);


                //string OpDetail  =  GetOpDetail("")
                //AddOpLog(batInfo.ID, 201, "update", OpDate, OpDetail);

                return "处理成功";
            }
        }

        #endregion



        #region 入库


        public static string AcceptCommit(Receipt batInfo)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");


            IEnumerable<Receipt> RB = GetReceivingBasis(batInfo); //获取该批次所属的收货依据

            if (RB == null)
                return GetErrorInfo(batInfo);

            if (batInfo.ArrivedQty > RB.First().NotReceiptQty)//若超收
                return string.Format("超收数量：{0}， 可收数量：{1}", batInfo.ArrivedQty - RB.First().NotReceiptQty, RB.First().NotReceiptQty);


            string sql = "select status，isdelete, isComplete from Receipt where ID = " + batInfo.ID + "";
            DataTable theBatch = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql); //获取batInfo所指定的批次的status，isdelete字段值


            if ((bool)theBatch.Rows[0]["isdelete"] == true)
                return "错误：该批次的流程已删除";

            else if ((bool)theBatch.Rows[0]["isComplete"] == true)
                return "错误：该批次的流程已结束";

            else if (((int)theBatch.Rows[0]["status"] & 8+16+32)  == 0)
                return "错误：流程未在当前节点上";


            if(batInfo.TranType == "PUR-SUB")
            {
                JobManager jobManager = new JobManager();
                if (jobManager.porcv(packNum, recdate, vendorid, rcvdtlStr, c10, companyId) == "true")//若回写erp成功， 则更新Receipt记录
                {                   
                    string Location = jobManager.poDes((int)batInfo.PoNum, (int)batInfo.PoLine, (int)batInfo.PORelNum, batInfo.Company);
                    sql = @"update Receipt set StockDate = '" + OpDate + "', ArrivedQty = {0}, Warehouse = '{1}', BinNum = '{2}', FourthUserID = '{3}', isComplete = 1, Location = '{4}'  where ID = " + batInfo.ID + "";
                    sql = string.Format(sql, batInfo.ArrivedQty, batInfo.Warehouse, batInfo.BinNum, HttpContext.Current.Session["UserId"].ToString(), Location);
                    SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
                }



                sql = @" Select *  from Receipt where ID = "+batInfo.ID+"";
                theBatch = SQLRepository.ExecuteQueryToDataTable(SQLRepository.ERP_strConn, sql);


                sql = @"  Select jobseq, poline,porelnum ,OpDesc from erp.porel pr 
                          left join erp.JobOper jo on pr.jobnum = jo.JobNum and pr.AssemblySeq = jo.AssemblySeq and pr.Company = jo.Company and jobseq = jo.OprSeq 
                          where pr.ponum={0} and pr.jobnum = '{1}'  and pr.assemblyseq={2} and trantype='PUR-SUB' and pr.company = '{3}' and jobseq!= {4} ";
                sql = string.Format(sql, batInfo.PoNum, batInfo.JobNum, batInfo.AssemblySeq, batInfo.Company, batInfo.JobSeq);
                DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.ERP_strConn, sql);



                for(int i =0;i<dt.Rows.Count;i++)
                {
                    theBatch.Rows[0]["PoLine"] = (int)dt.Rows[i]["poline"];
                    theBatch.Rows[0]["PORelNum"] = (int)dt.Rows[i]["porelnum"];
                    theBatch.Rows[0]["JobSeq"] = (int)dt.Rows[i]["jobseq"];
                    theBatch.Rows[0]["IsAuto"] = true;
                    theBatch.Rows[0]["OpDesc"] = (int)dt.Rows[i]["OpDesc"];

                }
            }

            return null;
        }

        #endregion



        #region 功能
        /// <summary>
        /// 返回下个节点的可选人员
        /// </summary>
        /// <returns></returns>
        public static string GetNextUserGroup(int nextStatus, string company, string plant)
        {
            DataTable dt = null;
            string sql = null, NextUserGroup = null;

            sql = "select userid from userfile where CHARINDEX('" + company + "', company) > 0 and CHARINDEX('" + plant + "', plant) > 0 and disabled = 0 and RoleID & " + nextStatus + " != 0 ";


            dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql); //根据sql，获取指定人员表


            for (int i = 0; i < dt.Rows.Count; i++)
            {
                NextUserGroup += dt.Rows[i][0].ToString() + ","; //把每行的userid拼接起来，以逗号分隔
            }

            return NextUserGroup;
        }


        /// <summary>
        /// 流程回退到上一个节点
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="oristatus"></param>
        /// <param name="ReasonID"></param>
        /// <returns></returns>
        public static string ReturnStatus(int ID, int oristatus, int ReasonID)
        {
            string OpDetail = "", OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string sql = "select status，isdelete, batchno iscomplete from Receipt where ID = " + ID + " ";
            DataTable theBatch = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql); //获取ID所指定的批次的 status，isdelete字段值


            if ((bool)theBatch.Rows[0]["isdelete"] == true)
                return "错误：该批次的流程已删除";

            else if ((bool)theBatch.Rows[0]["isComplete"] == true)
                return "错误：该批次的流程已结束";

            else if ((int)theBatch.Rows[0]["status"] != oristatus)
                return "错误：流程未在当前节点上";



            if ((oristatus & (8+16+32)) != 0)
            {
                Return(3, "ReturnThree", (string)theBatch.Rows[0]["batchno"], OpDate, ReasonID);

                //OpDetail = GetOpDetail("")

            }
            else if (oristatus == 4)
            {
                Return(2, "ReturnTwo", (string)theBatch.Rows[0]["batchno"], OpDate, ReasonID);

                //OpDetail = GetOpDetail("")
            }
            else if (oristatus == 2)
            {
                Return(1, "ReturnOne", (string)theBatch.Rows[0]["batchno"], OpDate, ReasonID);

                //清空报告

                //OpDetail = GetOpDetail("")
            }
            else //oristatus == 1  
            {
                sql = @"update Receipt set isdelete = 1 where where ID = " + ID + " ";   // 把该批次的流程标记为已删除
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
            }


            // AddOpLog(ID, ApiNum, "return", OpDate, OpDetail);
            return "处理成功";
        }


        public static IEnumerable<Reason> GetReason()
        {
            string sql = "select * from Reason";
            List<Reason> Reasons = CommonRepository.DataTableToList<Reason>(SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql));

            return Reasons;
        }



        public static DataTable GetWarehouse(string partnum)
        {
            string sql = @"select pw.WarehouseCode from erp.PartWhse pw
                           left join erp.Warehse wh 
                           on  pw.Company=wh.Company  and  pw.WarehouseCode = wh.WarehouseCode
                           where Plant = '{0}' and pw.Company = '{1}' and pw.PartNum = '{2}'";
            sql = string.Format(sql, HttpContext.Current.Session["Plant"].ToString(), HttpContext.Current.Session["Company"].ToString(), partnum);

            return SQLRepository.ExecuteQueryToDataTable(SQLRepository.ERP_strConn, sql);
        }



        public static string ParseQRValues(string values)
        {
            string[] arr = values.Split('~');

            if (arr.Length == 14) //刚好13个波浪线， 扫码收货时解析
            {
                string sql = @"select 
                pr.plant,
                vd.Name
                from erp.PORel pr    
                left join erp.POHeader ph   on ph.Company = pr.Company   and   ph.PONum = pr.PONUM                 
                left join erp.Vendor vd     on ph.VendorNum = vd.VendorNum   and   ph.company = vd.company             
                where pr.Company = '" + arr[0] + "'   and   pr.ponum = " + int.Parse(arr[8]) + "   and   pr.poline = " + int.Parse(arr[9]) + "  and pr.porelnum = " + int.Parse(arr[11]) + " ";

                DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.ERP_strConn, sql);


                //values = values.Replace('%', '~');
                values += "~" + (string)dt.Rows[0]["plant"] + "~" + (string)dt.Rows[0]["Name"];

                return values;
            }
            else if(arr.Length == 4) //3个波浪线， 无二维码获取详情页面数据
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
                vd.VendorID  as SupplierNo,
                vd.Name as SupplierName,
                pa.TypeCode  as  PartType,
                pa.PartDescription  as  PartDesc,
                pr.AssemblySeq,                                       
                from erp.PORel pr
                left join erp.PODetail pd   on pr.PONum = pd.PONUM   and   pr.Company = pd.Company   and   pr.POLine = pd.POLine 
                left join erp.POHeader ph   on ph.Company = pd.Company   and   ph.PONum = pd.PONUM                 
                left join erp.Vendor vd     on ph.VendorNum = vd.VendorNum   and   ph.company = vd.company             
                left join erp.part pa       on pd.PartNum = pa.PartNum   and   pa.company = pd.company
                where pr.ponum = {0} and pr.poline = {1} and pr.porelnum = {2} and pr.company = '{3}'";
                sql = string.Format(sql, arr[0], arr[1], arr[2], arr[3]);

                DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.ERP_strConn, sql);

                values = "";

                values += (string)dt.Rows[0]["Company"] + "~";
                values += (string)dt.Rows[0]["PartNum"] + "~";
                values += (string)dt.Rows[0]["PartDesc"] + "~";
                values += "~";  //batchno

                values += (string)dt.Rows[0]["JobNum"] + "~"; 
                values += (string)dt.Rows[0]["AssemblySeq"] + "~";
                values += "~"; //textid
                values += (string)dt.Rows[0]["SupplierNo"] + "~";

                values += (string)dt.Rows[0]["PoNum"] + "~"; 
                values += (string)dt.Rows[0]["PoLine"] + "~";
                values += "~"; //receivQty
                values += (string)dt.Rows[0]["PORelNum"] + "~";

                values += (string)dt.Rows[0]["JobSeq"] + "~"; 
                values += "~"; //heatnum
                values += (string)dt.Rows[0]["Plant"] + "~";
                values += (string)dt.Rows[0]["SupplierName"];

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
            #region 构造sql语句
            string sql = @"select 
                        ID,
                        SupplierNo, 
                        SupplierName,                
                        ReceiveQty1,
                        ReceiveQty2,
                        AssemblySeq, 
                        JobSeq,
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
                        SecondUserID,
                        FirstUserID,
                        Status,
                        PoNum,
                        PoLine,
                        PORelNum,
                        BatchNo,
                        Company,
                        Plant,
                        IsPrint,
                        ReceiptNo,
                        HeatNum,
                        ReceiptDate,
                        IQCDate,
                        IsAllCheck,
                        InspectionQty,
                        PassedQty,
                        FailedQty,
                        Result,
                        Warehouse,
                        StockDate,
                        ArrivedQty, 
                        BinNum, 
                        ThirdUserID，
                        ReturnOne，
                        ReturnTwo，
                        ReturnThree,
                        FourthUserGroup,
                        SecondUserGroup,
                        ThirdUserGroup
                        from Receipt where 
                        status & {0} != 0 and isdelete != 1 and isComplete != 1
                        and CHARINDEX(Company, '{1}') > 0   and   CHARINDEX(Plant, '{2}') > 0 ";
            sql = string.Format(sql, (int)HttpContext.Current.Session["RoleId"], HttpContext.Current.Session["Company"].ToString(), HttpContext.Current.Session["Plant"].ToString());
            #endregion

            DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql);

            if (dt == null)
                return null;

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if ((int)dt.Rows[i]["Status"] == 1 && (string)dt.Rows[i]["FirstUserID"] == (string)HttpContext.Current.Session["UserId"])
                    continue;
                else if ((int)dt.Rows[i]["Status"] == 2 && ((string)dt.Rows[i]["SecondUserGroup"]).Contains((string)HttpContext.Current.Session["UserId"]))
                    continue;
                else if ((int)dt.Rows[i]["Status"] == 4 && ((string)dt.Rows[i]["ThirdUserGroup"]).Contains((string)HttpContext.Current.Session["UserId"]))
                    continue;
                else if (((int)dt.Rows[i]["Status"] == 8 || (int)dt.Rows[i]["Status"] == 16 || (int)dt.Rows[i]["Status"] == 32) && ((string)dt.Rows[i]["FourthUserGroup"]).Contains((string)HttpContext.Current.Session["UserId"]))
                    continue;
                else
                    dt.Rows.RemoveAt(i);
            }
            List<Receipt> RBs = CommonRepository.DataTableToList<Receipt>(dt);


            return GetValidBatchs(RBs);
        }



        public static DataTable GetRecordByID(int ReceiptID)
        {
            string sql = "select * from Receipt where ID = " + ReceiptID + "";
            DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql);

            return dt;
        }

        #endregion



        public static void AddOpLog(int ReceiptId, int ApiNum, string OpType, string OpDate, string OpDetail)
        {
            string sql = @"insert into OpLog(ReceiptId, UserId, Company, plant, Opdate, ApiNum, OpType, OpDetail) Values({0}, '{1}', '{2}', '{3}', '{4}', {5}, '{6}', '{7}') ";
            sql = string.Format(sql, ReceiptId, HttpContext.Current.Session["UserId"].ToString(), HttpContext.Current.Session["Company"].ToString(), HttpContext.Current.Session["Plant"].ToString(), OpDate, ApiNum, OpType, OpDetail);

            SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
        }//添加操作记录

    }
}