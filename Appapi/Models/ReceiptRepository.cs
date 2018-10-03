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


        private static string GetOpDetail(string keys, string values)
        {
            string[] arrKey = keys.Split('|');
            string[] arrValue = keys.Split('|');

            //string OpDetail

            for (int i = 0; i < arrKey.Count(); i++)
            {

            }

            return null;
        }


        private static void AddOpLog(string BatchNo, int ApiNum, string OpType, string OpDate, string OpDetail)
        {
            string sql = @"insert into OpLog(ReceiptId, UserId, Company, plant, Opdate, ApiNum, OpType, OpDetail) Values({0}, '{1}', '{2}', '{3}', '{4}', {5}, '{6}', {7})";
            string.Format(sql, BatchNo, HttpContext.Current.Session["UserId"].ToString(), HttpContext.Current.Session["Company"].ToString(), HttpContext.Current.Session["Plant"].ToString(), OpDate, ApiNum, OpType, OpDetail);

            SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
        }//添加操作记录


        private static decimal GetNotReceiptQty(Receipt batInfo)
        {
            string sql = "select sum(case when  ActCount is null then receivecount else ActCount end) from Receipt " +
                        "where isdelete != 1 and ponum = " + batInfo.PoNum + " and poline = " + batInfo.PoLine + " and  PORelNum = " + batInfo.PORelNum + " and company = " + batInfo.Company + " and plant = " + batInfo.Plant + "";

            object sum = SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

            return (decimal)batInfo.NeedReceiptQty - (sum != null ? (decimal)sum : 0);
        }

        /*
        private static int GetReceiptID(Receipt para)
        {
            string sql = "select ID from Receipt where PoNum = {0} and  PoLine = {1} and PORelNum = {2} and Plant = '{3}'and Company = '{4}' and  BatchNo = '{5}' ";
            string.Format(sql, para.PoNum, para.PoLine, para.PORelNum, para.Plant, para.Company, para.BatchNo);

            return (int)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);
        }*/


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


        public static IEnumerable<Receipt> ts()
        {
            List<Receipt> RBs = null;

            return RBs;
        }


        #region 接收
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
                pc.description,
                (pr.XRelQty-pr.PassedQty) NeedReceiptQty, 
                pp.PrimWhse as Warehouse
                 from erp.PORel pr
                left join erp.PODetail pd   on pr.PONum = pd.PONUM   and   pr.Company = pd.Company   and   pr.POLine = pd.POLine 
                left join erp.POHeader ph   on ph.Company = pd.Company   and   ph.PONum = pd.PONUM 
                left join erp.JobOper jo    on pr.JobNum = jo.JobNum   and   pr.Company = jo.Company
                left join erp.Vendor vd     on ph.VendorNum = vd.VendorNum   and   ph.company = vd.company             
                left join erp.part pa       on pd.PartNum = pa.PartNum   and   pa.company = pd.company
                left join erp.partclass pc  on pc.classid = pd.ClassID   and   pc.company = pd.company
                left join erp.partplant pp  on pp.company = pr.Company   and   pp.plant = pr.plant   and   pp.PartNum = pd.PartNum
                where pr.Company = '" + HttpContext.Current.Session["Company"].ToString() + "'   and    pr.Plant = '" + HttpContext.Current.Session["Plant"].ToString() + "' ";

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

            List<Receipt> RBs = CommonRepository.DataTableToList<Receipt>(SQLRepository.ExecuteQueryToDataTable(SQLRepository.ERP_strConn, sql));

            if (RBs != null)
            {
                for (int i = 0; i < RBs.Count; i++)
                {
                    RBs[i].NotReceiptQty = GetNotReceiptQty(RBs[i]);
                }//计算每个收货依据中的还可接收数量NotReceiptQty
            }

            return RBs;
        }


        public static string ReceiveCommitWithNonQRCode(Receipt batInfo)
        {
            string OpDate = (batInfo.ReceiptDate = DateTime.Now).ToString();


            IEnumerable<Receipt> RB = GetReceivingBasis(batInfo); //获取该批次所属的收货依据

            if (RB == null)
                return "错误：该批次所属的收货依据已失效";

            if (batInfo.ReceiveCount > RB.First().NotReceiptQty)//若超收
                return string.Format("超收数量：{0}， 可收数量：{1}", batInfo.ReceiveCount - RB.First().NotReceiptQty, RB.First().NotReceiptQty);


            #region 计算批次号
            string sql = "select * from SerialNumber where name = 'BAT'";
            DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql);

            string OriDay = dt.Rows[0]["time"].ToString().Substring(0, 10);//截取年月日部分
            string today = DateTime.Now.ToString().Substring(0, 10);

            if (OriDay == today)
            {
                batInfo.BatchNo = "P" + DateTime.Now.ToString("yyyyMMdd") + ((int)dt.Rows[0]["Current"]).ToString("d4");
                dt.Rows[0]["Current"] = (int)dt.Rows[0]["Current"] + 1;
            }
            else
            {
                batInfo.BatchNo = "P" + DateTime.Now.ToString("yyyyMMdd") + "0001";
                dt.Rows[0]["Current"] = 1;
            }

            sql = "UPDATE SerialNumber SET time = getdate(), current = " + Convert.ToInt32(dt.Rows[0]["Current"]) + " where name = 'BAT'";
            SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
            #endregion

            #region 调用现有接口打印

            string jsonStr = @"{ 'text1': '{0}', 'text2': '', 'text3': '{1}', 'text4': '{2}', 'text5': {3}, 'text6': '', 'text7': '{4}', 'text8': {5}, 'text9': {6}, 
'text10': {7}, 'text11': {8}, 'text12': '{9}', 'text13': '', 'text14': {10}, 'text15': '{11}', 'text16': '', 'text17': '', 'text18': '', 'text19': '', 
'text20': '', 'text21': '', 'text22': '', 'text23': '', 'text24': '', 'text25': '', 'text26': '', 'text27': '', 'text28': '', 'text29': '', 'text30': '' }";
            string.Format(jsonStr, batInfo.PartNum, batInfo.BatchNo, batInfo.JobNum, batInfo.AssemblySeq, batInfo.SupplierNo, batInfo.PoNum, batInfo.PoLine, batInfo.ReceiveCount, batInfo.PORelNum, batInfo.Company, batInfo.JobSeq, batInfo.HeatNum);

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
                ReceiveCount,
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
                Description,
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
                    batInfo.ReceiveCount,
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
                    batInfo.Description,
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
                    batInfo.ReceiptDate
                });
                string.Format(sql, values);
                #endregion

                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
            }
            #endregion

            //string OpDetail  =  GetOpDetail("")
            // AddOpLog(batInfo.BatchNo, 101, "insert", OpDate);

            return "处理成功";
        }


        public static string GetSupplierName(string SupplierNo)
        {
            string sql = "select name from erp.vendor where vendorid = '" + SupplierNo + "' and company = '" + HttpContext.Current.Session["Company"].ToString() + "'";

            return (string)SQLRepository.ExecuteScalarToObject(SQLRepository.ERP_strConn, CommandType.Text, sql, null); ;
        }


        public static string ReceiveCommitWithQRCode(Receipt batInfo)
        {
            string OpDate = (batInfo.ReceiptDate = DateTime.Now).ToString();

            IEnumerable<Receipt> RB = GetReceivingBasis(batInfo); //获取该批次所属的收货依据

            if (RB == null)
                return "错误：该批次所属的收货依据已失效";

            if (batInfo.ReceiveCount > RB.First().NotReceiptQty)//若超收
                return string.Format("超收数量：{0}， 可收数量：{1}", batInfo.ReceiveCount - RB.First().NotReceiptQty, RB.First().NotReceiptQty);


            string sql = "select status，isdelete from Receipt where BatchNo = '"+ batInfo.BatchNo + "'";
            DataTable theBatch = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql); //查询batInfo所指定的批次信息

            if (theBatch == null) //batInfo所指定的批次不存在，为该批次生成一条新的receipt记录
            {
                #region 构造sql语句
                sql = @"insert into Receipt(
                        SupplierNo, 
                        SupplierName,                
                        ReceiveCount,
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
                        Description,
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
                    batInfo.ReceiveCount,
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
                    RB.First().Description,
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
                    batInfo.ReceiptDate
                });
                string.Format(sql, values);
                #endregion
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);


                //string OpDetail  =  GetOpDetail("")
                // AddOpLog(batInfo.BatchNo, 101, "insert", OpDate);

                return "处理成功";
            }

            else if ((int)theBatch.Rows[0]["isdelete"] == 1)
                return "错误：该批次的流程已删除";

            else if ((int)theBatch.Rows[0]["status"] != 1)
                return "错误：流程未在当前节点上";

            else //status == 1  在第二届点被退回， 更新批次信息。
            {
                #region 构造sql语句:
                sql = @"update Receipt set
                        SupplierNo = {0}, 
                        SupplierName = {1},
                        ReceiveCount = {3},
                        PartNum = {4},
                        PartDesc = {5},
                        JobNum = {6}, 
                        Remark = {7},
                        SecondUserGroup = {8},
                        FirstUserID = {9},
                        PoNum = {10},
                        PoLine = 11},
                        PORelNum = {12},
                        BatchNo = {13},
                        Company = {14},
                        Plant = {15},
                        HeatNum = {16},
                        Warehouse = {17},
                        ReceiptDate = {18}
                        where ID = {19}";
                string.Format(sql,
                    batInfo.SupplierNo,
                    batInfo.SupplierName,
                    batInfo.ReceiveCount,
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
                    batInfo.ReceiptDate,
                    batInfo.ID);
                #endregion
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                //string OpDetail  =  GetOpDetail("")
                //AddOpLog(batInfo.BatchNo, 102, "update", OpDate);

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
                        ReceiveCount,
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

            //获取与该用户有关的有效待办批次
            return GetValidBatchs(CommonRepository.DataTableToList<Receipt>(SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql))); 
        }
        #endregion


        #region 进料检验
        public static IEnumerable<Receipt> GetRemainsOfIQCUser()
        {
            #region 构造sql语句
            string sql = @"select 
                        ID,
                        SupplierNo, 
                        SupplierName,                
                        ReceiveCount,
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
                        Description,
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
                        SpotCheckCount, 
                        QualifiedCount, 
                        UnqualifiedCount, 
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


        public static string IQCCommit(Receipt batInfo)
        {
            string OpDate = DateTime.Now.ToString();


            IEnumerable<Receipt> RB = GetReceivingBasis(batInfo); //获取该批次所属的收货依据

            if (RB == null)
                return "错误：该批次所属的收货依据已失效";

            if (batInfo.QualifiedCount > RB.First().NotReceiptQty)//若超收
                return string.Format("超收数量：{0}， 可收数量：{1}", batInfo.QualifiedCount - RB.First().NotReceiptQty, RB.First().NotReceiptQty);


            string sql = "select status，isdelete from Receipt where BatchNo = '" + batInfo.BatchNo + "'";
            DataTable theBatch = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql); //查询batInfo所指定的批次


            if ((int)theBatch.Rows[0]["isdelete"] == 1)
                return "错误：该批次的流程已删除";

            else if ((int)theBatch.Rows[0]["status"] != 2)
                return "错误：流程未在当前节点上";

            else //status == 2  更新批次信息。
            {         
                sql = @"update Receipt set IQCDate = getdate(), IsAllCheck = {0}, SpotCheckCount = {1}, QualifiedCount = {2}, UnqualifiedCount = {3}, Result = '{4}'，Remark = '{5}'，Status=" + batInfo.Status + "，ThirdUserGroup = '{6}', SecondUserID = '{7}', ReceiptNo = '{8}' where BatchNo = '{9}'";
                string.Format(sql, batInfo.IsAllCheck, batInfo.SpotCheckCount, batInfo.QualifiedCount, batInfo.UnqualifiedCount, batInfo.Result, batInfo.Remark, batInfo.ThirdUserGroup, HttpContext.Current.Session["UserId"].ToString(), batInfo.ReceiptNo, batInfo.BatchNo);
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                //AddOpLog(batInfo.BatchNo, 201, para.Status == 3 ? "update" : "save", OpDate);

                return "处理成功";
            }
        }
        #endregion


        #region 入库
        public static IEnumerable<Receipt> GetRemainsOfAcceptUser()
        {
            #region 构造sql语句
            string sql = @"select 
                        ID,
                        SupplierNo, 
                        SupplierName,                
                        ReceiveCount,
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
                        Description,
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
                        SpotCheckCount,
                        QualifiedCount,
                        UnqualifiedCount,
                        Result,
                        Warehouse,
                        StockDate,
                        StockCount, 
                        BinNum, 
                        ThirdUserID
                        from Receipt where 
                        ThirdUserGroup like '%" + HttpContext.Current.Session["UserId"].ToString() + "%' " +
                        "and status = "+ (int)HttpContext.Current.Session["RoleId"] + " and isdelete != 1 " +
                        "and Company = '" + HttpContext.Current.Session["Company"].ToString() + "'   and   Plant = '" + HttpContext.Current.Session["Plant"].ToString() + "' ";
            #endregion

            //获取与该用户有关的有效待办批次
            return GetValidBatchs(CommonRepository.DataTableToList<Receipt>(SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql)));
        }


        public static string AcceptCommit(Receipt batInfo)
        {
            string OpDate = DateTime.Now.ToString();


            IEnumerable<Receipt> RB = GetReceivingBasis(batInfo); //获取该批次所属的收货依据

            if (RB == null)
                return "错误：该批次所属的收货依据已失效";

            if (batInfo.StockCount > RB.First().NotReceiptQty)//若超收
                return string.Format("超收数量：{0}， 可收数量：{1}", batInfo.StockCount - RB.First().NotReceiptQty, RB.First().NotReceiptQty);


            string sql = "select status，isdelete from Receipt where BatchNo = '" + batInfo.BatchNo + "'";
            DataTable theBatch = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql); //查询batInfo所指定的批次

            if ((int)theBatch.Rows[0]["isdelete"] == 1)
                return "错误：该批次的流程已删除";

            else if ((int)theBatch.Rows[0]["status"] != 3)
                return "错误：流程未在当前节点上";

            else //status == 3  更新批次信息。
            { 
                sql = @"update Receipt set StockDate = getdate(), StockCount = {0}, Warehouse = '{1}', BinNum = '{2}', ThirdUserID = '{3}' where BatchNo = '{4}'";
                string.Format(sql, batInfo.StockCount, batInfo.Warehouse, batInfo.BinNum, HttpContext.Current.Session["UserId"].ToString(), batInfo.BatchNo);
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                //调用反写接口。     若反写erp失败，返回"处理失败-2" //反写数据进erp时失败

                //AddOpLog(batInfo.BatchNo, 301, "update", OpDate);
                return "处理成功";
            }
        }
        #endregion

        public static string GetNextUserGroup()
        {
            DataTable dt;
            string sql, NextUserGroup = null;


            sql = "select userid from userfile where company = '{0}' and plant = '{1}' and disabled = 0 and RoleID = {2}";
            string.Format(sql, HttpContext.Current.Session["Company"].ToString(), HttpContext.Current.Session["Plant"].ToString(), (int)HttpContext.Current.Session["RoleId"]);
            dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.ERP_strConn, sql);


            for (int i = 0; i < dt.Rows.Count; i++)
            {
                NextUserGroup += dt.Rows[i][0].ToString() + ",";
            }

            return NextUserGroup;
        }

        public static string ReturnStatus(string BatchNo, int oristatus, int ReasonID)
        {
            string OpDate = DateTime.Now.ToString();

            string sql = "select status，isdelete from Receipt where BatchNo = '" + BatchNo + "' ";
            DataTable theBatch = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql); //查询batInfo所指定的批次


            if ((int)theBatch.Rows[0]["isdelete"] == 1)
                return "错误：该批次的流程已删除";

            else if ((int)theBatch.Rows[0]["status"] != oristatus)
                return "错误：流程未在当前节点上";


            int ApiNum;
            if (oristatus == 3)
            {
                ApiNum = 300;
                sql = @"update Receipt set status = 2, ReturnTwo = ReturnTwo+1 where BatchNo = '" + BatchNo + "' ";
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = "select ReturnTwo from Receipt where BatchNo = '" + BatchNo + "' ";
                int ReturnTwo = (int)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = @"insert into ReasonRecord(BatchNo, ReturnTwo, ReturnReasonId, Date) Values('" + BatchNo + "', " + ReturnTwo + ", " + ReasonID + ", '" + OpDate + "')";
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

            }
            else //if (oristatus == 2)
            {
                ApiNum = 200;
                sql = @"update Receipt set status = 1, ReturnOne = ReturnOne+1 where BatchNo = '" + BatchNo + "'";
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = "select ReturnOne from Receipt where BatchNo = '" + BatchNo + "' ";
                int ReturnOne = (int)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = @"insert into ReasonRecord(BatchNo, ReturnOne, ReturnReasonId, Date) Values('" + BatchNo + "', " + ReturnOne + ", " + ReasonID + ", '" + OpDate + "')";
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
            }


            // AddOpLog(BatchNo, ApiNum, "return", OpDate);
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
    }
}
