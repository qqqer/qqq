using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Collections;
using System.Net; //ftp

namespace Appapi.Models
{
    public static class ReceiptRepository
    {
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
        /// 计算该批次的收货依据中的还可接收数量。  计算方法参考收货文档
        /// </summary>
        /// <param name="batInfo"></param>
        /// <returns></returns>
        private static decimal GetNotReceiptQty(Receipt batInfo)
        {
            string sql = "select sum(case when ArrivedQty is null then(case when  ReceiveQty2 is null then ReceiveQty1 else ReceiveQty2 end) else ArrivedQty end) from Receipt " +
                        "where isdelete != 1 and ponum = " + batInfo.PoNum + " and poline = " + batInfo.PoLine + " and  PORelNum = " + batInfo.PORelNum + " and company = " + batInfo.Company + " and plant = " + batInfo.Plant + "";

            object sum = SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

            return (decimal)batInfo.NeedReceiptQty - (sum != null ? (decimal)sum : 0);
        }

        
        /// <summary>
        /// 获取该批次号在Receipt表中的ID
        /// </summary>
        /// <param name="batInfo"></param>
        /// <returns></returns>
        private static int GetReceiptID(Receipt batInfo)
        {
            string sql = "select ID from Receipt where PoNum = {0} and  PoLine = {1} and PORelNum = {2} and Plant = '{3}'and Company = '{4}' and  BatchNo = '{5}' ";
            string.Format(sql, batInfo.PoNum, batInfo.PoLine, batInfo.PORelNum, batInfo.Plant, batInfo.Company, batInfo.BatchNo);

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
                pd.PartNum ,
                vd.VendorID  as SupplierNo,
                vd.Name as SupplierName,
                pa.TypeCode  as  PartType,
                pa.PartDescription  as  Partdesc,
                jo.AssemblySeq,
                jo.OpDesc,
                jh.jobClosed,
                jh.jobComplete,
                pc.PartClassDesc,
                (pr.XRelQty-pr.ArrivedQty) NeedReceiptQty, 
                pp.PrimWhse as Warehouse
                 from erp.PORel pr
                left join erp.PODetail pd   on pr.PONum = pd.PONUM   and   pr.Company = pd.Company   and   pr.POLine = pd.POLine 
                left join erp.POHeader ph   on ph.Company = pd.Company   and   ph.PONum = pd.PONUM 
                left join erp.JobOper jo    on pr.JobNum = jo.JobNum   and   pr.Company = jo.Company
                left join erp.JobHead jh  on pr.JobNum = jh.JobNum   and   pr.Company = jh.Company
                left join erp.Vendor vd     on ph.VendorNum = vd.VendorNum   and   ph.company = vd.company             
                left join erp.part pa       on pd.PartNum = pa.PartNum   and   pa.company = pd.company
                left join erp.partclass pc  on pc.classid = pd.ClassID   and   pc.company = pd.company
                left join erp.partplant pp  on pp.company = pr.Company   and   pp.plant = pr.plant   and   pp.PartNum = pd.PartNum
                where pr.Company = '" + HttpContext.Current.Session["Company"].ToString() + "'   and    pr.Plant = '" + HttpContext.Current.Session["Plant"].ToString() + "' " +
                "and  ph.OpenOrder = 1   and    ph.orderHeld != 1    and    pd.openLine = 1     and      pr.openRelease = 1   ";

            if (Condition.PoNum != null)
                sql += "and pr.ponum = " + Condition.PoNum + " ";
            if (Condition.PoLine != null)
                sql += "and pr.poline = " + Condition.PoLine + " ";
            if (Condition.PORelNum != null)
                sql += "and pr.PORelNum = " + Condition.PORelNum + " ";
            if (Condition.PartNum != null)
                sql += "and pr.partnum like '%" + Condition.PartNum + "%' ";
            if (Condition.PartDesc != null)
                sql += "and pd.LineDesc like '%" + Condition.PartDesc + "%' ";
            #endregion

            DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.ERP_strConn, sql); //获取可能有效的收货依据

            if (dt == null) //没有找到可能有效的收货依据
                return null;


            //筛选可能有效的收货依据，以得到最终有效的收货依据表。     
            for (int i = 0;i < dt.Rows.Count; i++)
            {
                //如果该收货依据是外协 且关联的工单已完成或关闭 则排除该收货依据
                if (dt.Rows[i]["TranType"].ToString() == "PUR-SUB" && ((int)dt.Rows[i]["jobClosed"] == 1 || (int)dt.Rows[i]["jobComplete"] == 1))
                    dt.Rows[i].Delete();
            }
            List<Receipt> RBs = CommonRepository.DataTableToList<Receipt>(dt); 



            if (RBs != null)//若经过筛选后收货依据列表不为空
            {
                for (int i = 0; i < RBs.Count; i++)
                {
                    RBs[i].NotReceiptQty = GetNotReceiptQty(RBs[i]);
                }//计算每个收货依据中的还可接收数量NotReceiptQty
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

            if (RB == null)
                return "错误：该批次所属的收货依据已失效";

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
            string.Format(jsonStr, batInfo.PartNum, batInfo.BatchNo, batInfo.JobNum, batInfo.AssemblySeq, batInfo.SupplierNo, batInfo.PoNum, batInfo.PoLine, batInfo.ReceiveQty1, batInfo.PORelNum, batInfo.Company, batInfo.JobSeq, batInfo.HeatNum);

            if (PrintRepository.PrintQR("C:\\btw\\D0201.btw", HttpContext.Current.Session["UserPrinter"].ToString(), 1, jsonStr) == "1|处理成功")
            {
                batInfo.IsPrint = true;
                batInfo.Status = 2;
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
                    batInfo.Status,
                    batInfo.PoNum,
                    batInfo.PoLine,
                    batInfo.PORelNum,
                    batInfo.BatchNo,
                    HttpContext.Current.Session["Company"].ToString() ,//
                    HttpContext.Current.Session["Plant"].ToString() ,//
                    batInfo.IsPrint,
                    0,
                    0,
                    batInfo.HeatNum,
                    batInfo.Warehouse,
                    0,
                    OpDate
                });
                string.Format(sql, values);
                #endregion

                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
            }
            #endregion


            //string OpDetail  =  GetOpDetail("")
            // AddOpLog(GetReceiptID(batInfo), 101, "insert", OpDate, OpDetail);

            return "处理成功";
        }


        /// <summary>
        /// 根据供应商编号获取供应商名称
        /// </summary>
        /// <param name="SupplierNo"></param>
        /// <returns></returns>
        public static string GetSupplierName(string SupplierNo)
        {
            string sql = "select name from erp.vendor where vendorid = '" + SupplierNo + "' and company = '" + HttpContext.Current.Session["Company"].ToString() + "'";

            return (string)SQLRepository.ExecuteScalarToObject(SQLRepository.ERP_strConn, CommandType.Text, sql, null); ;
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

            if (RB == null)
                return "错误：该批次所属的收货依据已失效";

            if (batInfo.ReceiveQty1 > RB.First().NotReceiptQty)//若超收
                return string.Format("超收数量：{0}， 可收数量：{1}", batInfo.ReceiveQty1 - RB.First().NotReceiptQty, RB.First().NotReceiptQty);


            string sql = "select status，isdelete from Receipt where ID = "+ batInfo.ID + " ";
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
                    HttpContext.Current.Session["Company"].ToString(),
                    HttpContext.Current.Session["Plant"].ToString(),
                    1,
                    batInfo.HeatNum,
                    0,
                    0,
                    RB.First().Warehouse,
                    0,
                    OpDate
                });
                string.Format(sql, values);
                #endregion
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);


                //string OpDetail  =  GetOpDetail("")
                // AddOpLog(GetReceiptID(batInfo), 101, "insert", OpDate, OpDetail);

                return "处理成功";
            }

            else if ((int)theBatch.Rows[0]["isdelete"] == 1)
                return "错误：该批次的流程已删除";

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
                        where ID = {19}";
                string.Format(sql,
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
                    batInfo.ID);
                #endregion
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                //string OpDetail  =  GetOpDetail("")
                //AddOpLog(batInfo.ID, 102, "update", OpDate, OpDetail);

                return "处理成功"; //更新提交成功                
            }

        }


        public static IEnumerable<Receipt> GetRemainsOfReceiveUser()
        {
            #region 构造sql语句:
            string sql = @"select
                        ID,
                        SupplierNo, 
                        SupplierName,                
                        ReceiveQty1,
                        PartNum,
                        PartDesc,
                        JobNum, 
                        Remark,
                        SecondUserGroup,
                        FirstUserID,
                        PoNum,
                        PoLine,
                        PORelNum,
                        BatchNo,
                        Company,
                        Plant,
                        HeatNum,
                        ReceiptDate,
                        Warehouse
                        from receipt
                        where FirstUserID = '" + HttpContext.Current.Session["UserId"].ToString() + "' " +
                        "and status = " + (int)HttpContext.Current.Session["RoleId"] + " and isdelete != 1 " +
                        "and Company = '" + HttpContext.Current.Session["Company"].ToString() + "'   and   Plant = '" + HttpContext.Current.Session["Plant"].ToString() + "' "; ;
            #endregion

            //获取与该用户有关的有效待办批次。    有效待办批次：其所属的收货依据有效
            return GetValidBatchs(CommonRepository.DataTableToList<Receipt>(SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql))); 
        }
        #endregion



        #region 进料检验
        /// <summary>
        /// 返回节点2的待办批次
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Receipt> GetRemainsOfIQCUser()
        {
            #region 构造sql语句
            string sql = @"select 
                        ID,
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
                        ReceiptDate,
                        Warehouse,

                        IQCDate,
                        IsAllCheck, 
                        InspectionQty, 
                        PassedQty, 
                        FailedQty, 
                        Result，
                        ThirdUserGroup, 
                        SecondUserID, 
                        ReceiptNo 
                        from Receipt where SecondUserGroup like '%" + HttpContext.Current.Session["UserId"].ToString() + "%' " +
                        "and status = " + (int)HttpContext.Current.Session["RoleId"] + " and isdelete != 1 " +
                        "and Company = '" + HttpContext.Current.Session["Company"].ToString() + "'   and   Plant = '" + HttpContext.Current.Session["Plant"].ToString() + "' ";
            #endregion

            //获取与该用户有关的有效待办批次
            return GetValidBatchs(CommonRepository.DataTableToList<Receipt>(SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql)));
        }


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
                return "错误：该批次所属的收货依据已失效";

            if (batInfo.ReceiveQty2 > RB.First().NotReceiptQty)//若超收
                return string.Format("超收数量：{0}， 可收数量：{1}", batInfo.ReceiveQty2 - RB.First().NotReceiptQty, RB.First().NotReceiptQty);


            string sql = "select status，isdelete from Receipt where ID = " + batInfo.ID + "";
            DataTable theBatch = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql); //获取batInfo所指定的批次的status，isdelete字段值


            if ((int)theBatch.Rows[0]["isdelete"] == 1)
                return "错误：该批次的流程已删除";

            else if ((int)theBatch.Rows[0]["status"] != 2)
                return "错误：流程未在当前节点上";

            else //status == 2  更新批次信息。
            {         
                sql = @"update Receipt set IQCDate = '"+ OpDate +"', IsAllCheck = {0},  InspectionQty = {1}, PassedQty = {2}, FailedQty = {3}, Result = '{4}'，Remark = '{5}'，Status=" + batInfo.Status + "，ThirdUserGroup = '{6}', SecondUserID = '{7}', ReceiptNo = '{8}', ReceiveQty2 = {9} where ID = {10}";
                string.Format(sql, batInfo.IsAllCheck, batInfo.InspectionQty, batInfo.PassedQty, batInfo.FailedQty, batInfo.Result, batInfo.Remark, batInfo.ThirdUserGroup, HttpContext.Current.Session["UserId"].ToString(), batInfo.ReceiptNo, batInfo.ReceiveQty2, batInfo.ID);
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);


                //string OpDetail  =  GetOpDetail("")
                //AddOpLog(batInfo.ID, 201, "update", OpDate, OpDetail);

                return "处理成功";
            }
        }

        #endregion



        #region 确认
        public static IEnumerable<Receipt> GetRemainsOfConfirmUser()
        {
            #region 构造sql语句
            string sql = @"select 
                        ID,
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
                        SecondUserID,
                        FourthUserGroup
                        from Receipt where 
                        ThirdUserGroup like '%" + HttpContext.Current.Session["UserId"].ToString() + "%' " +
                        "and status = " + (int)HttpContext.Current.Session["RoleId"] + " and isdelete != 1 " +
                        "and Company = '" + HttpContext.Current.Session["Company"].ToString() + "'   and   Plant = '" + HttpContext.Current.Session["Plant"].ToString() + "' ";
            #endregion

            //获取与该用户有关的有效待办批次
            return GetValidBatchs(CommonRepository.DataTableToList<Receipt>(SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql)));
        }


        public static string ConfirmCommit(Receipt batInfo)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");


            IEnumerable<Receipt> RB = GetReceivingBasis(batInfo); //获取该批次所属的收货依据

            if (RB == null)
                return "错误：该批次所属的收货依据已失效";


            string sql = "select status，isdelete from Receipt where ID = " + batInfo.ID + "";
            DataTable theBatch = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql); //获取batInfo所指定的批次的status，isdelete字段值


            if ((int)theBatch.Rows[0]["isdelete"] == 1)
                return "错误：该批次的流程已删除";

            else if ((int)theBatch.Rows[0]["status"] != 3)
                return "错误：流程未在当前节点上";

            else //status == 3  选人。
            {
                sql = @"update Receipt set ChooseDate = '"+ OpDate +"'，Status=" + batInfo.Status + "，FourthUserGroup = '{0}', ThirdUserID = '{1}' where ID = " + batInfo.ID + "";
                string.Format(sql,  batInfo.FourthUserGroup, HttpContext.Current.Session["UserId"].ToString());
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);


                //string OpDetail  =  GetOpDetail("")
                //AddOpLog(batInfo.ID, 201, "update", OpDate, OpDetail);

                return "处理成功";
            }
        }
        #endregion



        #region 入库

        /// <summary>
        /// 返回节点4的待办批次
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Receipt> GetRemainsOfAcceptUser()
        {
            #region 构造sql语句
            string sql = @"select 
                        ID,
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
                        ThirdUserID
                        from Receipt where 
                        FourthUserGroup like '%" + HttpContext.Current.Session["UserId"].ToString() + "%' " +
                        "and status = "+ (int)HttpContext.Current.Session["RoleId"] + " and isdelete != 1 " +
                        "and Company = '" + HttpContext.Current.Session["Company"].ToString() + "'   and   Plant = '" + HttpContext.Current.Session["Plant"].ToString() + "' ";
            #endregion

            //获取与该用户有关的有效待办批次
            return GetValidBatchs(CommonRepository.DataTableToList<Receipt>(SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql)));
        }


        public static string AcceptCommit(Receipt batInfo)
        {
            string OpDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");


            IEnumerable<Receipt> RB = GetReceivingBasis(batInfo); //获取该批次所属的收货依据

            if (RB == null)
                return "错误：该批次所属的收货依据已失效";

            if (batInfo.ArrivedQty > RB.First().NotReceiptQty)//若超收
                return string.Format("超收数量：{0}， 可收数量：{1}", batInfo.ArrivedQty - RB.First().NotReceiptQty, RB.First().NotReceiptQty);


            string sql = "select status，isdelete from Receipt where ID = " + batInfo.ID + "";
            DataTable theBatch = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql); //获取batInfo所指定的批次的status，isdelete字段值

            if ((int)theBatch.Rows[0]["isdelete"] == 1)
                return "错误：该批次的流程已删除";

            else if ((int)theBatch.Rows[0]["status"] != 3)
                return "错误：流程未在当前节点上";

            else //status == 3  更新批次信息。
            { 
                sql = @"update Receipt set StockDate = '"+ OpDate +"', ArrivedQty = {0}, Warehouse = '{1}', BinNum = '{2}', ThirdUserID = '{3}' where ID = " + batInfo.ID + "";
                string.Format(sql, batInfo.ArrivedQty, batInfo.Warehouse, batInfo.BinNum, HttpContext.Current.Session["UserId"].ToString());
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                //调用反写接口。     若反写erp失败，返回"处理失败-2" //反写数据进erp时失败


                //string OpDetail  =  GetOpDetail("")
                //AddOpLog(batInfo.ID, 301, "update", OpDate, OpDetail);
                return "处理成功";
            }
        }
        #endregion




        /// <summary>
        /// 返回下个节点的可选人员
        /// </summary>
        /// <returns></returns>
        public static string GetNextUserGroup()
        {
            DataTable dt;
            string sql, NextUserGroup = null;

            
            sql = "select userid from userfile where company = '{0}' and plant = '{1}' and disabled = 0 and RoleID = {2}";
            string.Format(sql, HttpContext.Current.Session["Company"].ToString(), HttpContext.Current.Session["Plant"].ToString(), (int)HttpContext.Current.Session["RoleId"]);
            dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.ERP_strConn, sql);


            for (int i = 0; i < dt.Rows.Count; i++)
            {
                NextUserGroup += dt.Rows[i][0].ToString() + ","; //用逗号把可选的userid拼接起来
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
            
            string sql = "select status，isdelete from Receipt where ID = " + ID + " ";
            DataTable theBatch = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql); //获取ID所指定的批次的 status，isdelete字段值


            if ((int)theBatch.Rows[0]["isdelete"] == 1)
                return "错误：该批次的流程已删除";

            else if ((int)theBatch.Rows[0]["status"] != oristatus)
                return "错误：流程未在当前节点上";


            int ApiNum;
            if (oristatus == 3)
            {
                ApiNum = 300;
                sql = @"update Receipt set status = 2, ReturnTwo = ReturnTwo+1 where where ID = " + ID + " ";
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = "select ReturnTwo from Receipt where where ID = " + ID + " ";
                int ReturnTwo = (int)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = @"insert into ReasonRecord(ReceiptId, ReturnTwo, ReturnReasonId, Date) Values(" + ID + ", " + ReturnTwo + ", " + ReasonID + ", '" + OpDate + "')";
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                //OpDetail = GetOpDetail("")

            }
            else //if (oristatus == 2)
            {
                ApiNum = 200;
                sql = @"update Receipt set status = 1, ReturnOne = ReturnOne+1 where where ID = " + ID + " ";
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = "select ReturnOne from Receipt where where ID = " + ID + " ";
                int ReturnOne = (int)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = @"insert into ReasonRecord(ReceiptId, ReturnOne, ReturnReasonId, Date) Values(" + ID + ", " + ReturnOne + ", " + ReasonID + ", '" + OpDate + "')";
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                //OpDetail = GetOpDetail("")
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
            string.Format(sql, HttpContext.Current.Session["Plant"].ToString(), HttpContext.Current.Session["Company"].ToString(), partnum);

            return SQLRepository.ExecuteQueryToDataTable(SQLRepository.ERP_strConn, sql);
        }


        public static void AddOpLog(int ReceiptId, int ApiNum, string OpType, string OpDate, string OpDetail)
        {
            string sql = @"insert into OpLog(ReceiptId, UserId, Company, plant, Opdate, ApiNum, OpType, OpDetail) Values({0}, '{1}', '{2}', '{3}', '{4}', {5}, '{6}', '{7}') ";
            string.Format(sql, ReceiptId, HttpContext.Current.Session["UserId"].ToString(), HttpContext.Current.Session["Company"].ToString(), HttpContext.Current.Session["Plant"].ToString(), OpDate, ApiNum, OpType, OpDetail);

            SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
        }//添加操作记录
    }
}
