using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Collections;

namespace Appapi.Models
{
    public static class ReceiptRepository
    {       
        private static string ConstructValues(ArrayList array)
        {
            string values = "";
            for(int i = 0; i < array.Count; i++)
            {
                if (array[i] == null)
                    values += "null,";
                else if(array[i].GetType() == typeof(int) || array[i].GetType() == typeof(decimal))
                {
                    values += array[i].ToString() + (i == array.Count - 1 ? "" : ",");
                }
                else if (array[i].GetType() == typeof(string))
                {
                    values += "'" + array[i] + "'" + (i == array.Count - 1 ? "" : ",");
                }
                else if(array[i].GetType() == typeof(bool))
                {
                    values += Convert.ToInt32(array[i]).ToString() + (i == array.Count - 1 ? "" : ",");
                }
                else if (array[i].GetType() == typeof(DateTime))
                {
                    values += "getdate()" + (i == array.Count - 1 ? "" : ",");
                }
            }         
            return values;
        }//根据待拆入的字段值来生成sql语句中的values部分。

        public static IEnumerable<Receipt> GetPO(Receipt Condition)
        {
            #region 构造sql语句
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
                (pr.XRelQty-pr.PassedQty) NotReceiptQty
                 from erp.PORel pr 
                left join erp.PODetail pd   on pr.PONum = pd.PONUM and pr.Company = pd.Company and pr.POLine = pd.POLine 
                left join erp.POHeader ph   on ph.Company = pd.Company and ph.PONum = pd.PONUM 
                left join erp.JobOper jo    on pr.JobNum = jo.JobNum and pr.Company = jo.Company
                left join erp.Vendor vd     on ph.VendorNum = vd.VendorNum                 
                left join erp.part pa       on pd.PartNum = pa.PartNum
                left join erp.partclass pc  on pc.classid = pd.ClassID
                where pr.Company = '" + HttpContext.Current.Session["Company"].ToString() + "' and pr.Plant = '"+ HttpContext.Current.Session["Plant"].ToString() + "' ";


            if (Condition.PoNum != null)
                sql += "and pr.ponum = "+ Condition.PoNum +" "; 
            if (Condition.PoLine != null)
                sql += "and pr.poline = " + Condition.PoLine + " ";
            if (Condition.PartNum != null)
                sql += "and pr.partnum like '%"+ Condition.PartNum + "%' ";
            if (Condition.PartDesc != null)
                sql += "and pd.LineDesc like '%" + Condition.PartDesc + "%' ";
            #endregion

            List<Receipt> POs = CommonRepository.DataTableToList<Receipt>(SQLRepository.ExecuteQueryToDataTable(SQLRepository.ERP_strConn, sql));

            for (int i = 0; i < POs.Count; i++)
            {
                sql = "select (case ActCount when null then ReceiveCount else ActCount end) from Receipt " +
                    "where ponum = " + POs[i].PoNum + " and poline = " + POs[i].PoLine + " and  PORelNum = " + POs[i].PORelNum + " and company = " + POs[i].Company + " and plant = " + POs[i].Plant + "";

                POs[i].NotReceiptQty -= (decimal)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);
            }//计算POs中每个PO的NotReceiptQty

            return POs;
        }

        public static bool PrintQRCode(Receipt para)
        {
            #region 计算批次号
            string sql = "select * from SerialNumber where name = 'BAT'";
            DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql);

            string time = dt.Rows[0]["time"].ToString().Substring(0, 10);//截取年月日部分
            string today = DateTime.Now.ToString().Substring(0, 10);

            if (time == today)
            {
                para.BatchNo = "P" + DateTime.Now.ToString("yyyyMMdd") + ((int)dt.Rows[0]["Current"]).ToString("d4");
                dt.Rows[0]["Current"] = (int)dt.Rows[0]["Current"] + 1;
            }
            else
            {
                para.BatchNo = "P" + DateTime.Now.ToString("yyyyMMdd") + "0001";
                dt.Rows[0]["Current"] = 1;
            }

            sql = "UPDATE SerialNumber SET time = getdate(), current = " + Convert.ToInt32(dt.Rows[0]["Current"]) + " where name = 'BAT'";
            SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
            #endregion

            #region 调用现有接口打印

            string jsonStr = @"{ 'text1': '{0}', 'text2': '', 'text3': '{1}', 'text4': '{2}', 'text5': {3}, 'text6': '', 'text7': '{4}', 'text8': {5}, 'text9': {6}, 
'text10': {7}, 'text11': {8}, 'text12': '{9}', 'text13': '', 'text14': {10}, 'text15': '{11}', 'text16': '', 'text17': '', 'text18': '', 'text19': '', 
'text20': '', 'text21': '', 'text22': '', 'text23': '', 'text24': '', 'text25': '', 'text26': '', 'text27': '', 'text28': '', 'text29': '', 'text30': '' }";
            string.Format(jsonStr, para.PartNum, para.BatchNo, para.JobNum, para.AssemblySeq, para.SupplierNo, para.PoNum, para.PoLine, para.ReceiveCount, para.PORelNum, para.Company, para.JobSeq, para.HeatNum);

            if (PrintRepository.PrintQR("C:\\D0201.btw", HttpContext.Current.Session["UserPrinter"].ToString(), 1,jsonStr) == "1|处理成功")
            {
                para.IsPrint = true;
                para.Status = 2;
            }
            #endregion
            
            #region 回写数据到APP.Receipt表中
            if (para.IsPrint == true)
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
                ReceiptDate
                ) values({0}) ";
                string values = ConstructValues(new ArrayList
                {
                    para.SupplierNo,
                    para.SupplierName,
                    para.ReceiveCount,
                    para.AssemblySeq,
                    para.JobSeq,
                    para.PartNum,
                    para.PartDesc,
                    para.IUM,
                    para.JobNum,
                    para.Remark,
                    para.TranType,
                    para.PartType,
                    para.OpDesc,
                    para.CommentText,
                    para.Description,
                    para.NeedReceiptQty,
                    para.NotReceiptQty,
                    para.SecondUserID,
                    HttpContext.Current.Session["UserId"].ToString(),//
                    para.Status,
                    para.PoNum,
                    para.PoLine,
                    para.PORelNum,
                    para.BatchNo,
                    HttpContext.Current.Session["Company"].ToString() ,//
                    HttpContext.Current.Session["Plant"].ToString() ,//
                    para.IsPrint,
                    para.ReceiptDate
                });
                string.Format(sql, values);
                #endregion

                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
            }
            #endregion

            return (bool)para.IsPrint;
        }

        public static string GetSupplierName(string SupplierNo)
        {
            string sql = "select name from erp.vendor where vendorid = '"+ SupplierNo +"' ";

            return (string)SQLRepository.ExecuteScalarToObject(SQLRepository.ERP_strConn, CommandType.Text, sql, null); ;
        }

        public static bool IsOverReceived(Receipt para)
        {
            Receipt temp = GetPO(para).ElementAt(0);

            if(para.ReceiveCount <= temp.NotReceiptQty)
            {
                para.AssemblySeq = temp.AssemblySeq;
                para.JobSeq = temp.JobSeq;
                para.IUM = temp.IUM;
                para.TranType = temp.TranType;
                para.PartType = temp.PartType;
                para.OpDesc = temp.OpDesc;
                para.CommentText = temp.CommentText;
                para.Description = temp.Description;
                para.NeedReceiptQty = temp.NeedReceiptQty;
                para.NotReceiptQty = temp.NotReceiptQty;
                para.Status = 2;
                para.IsPrint = true;

                #region 构造sql语句
                string sql = @"insert into Receipt(
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
                        ReceiptDate
                        ) values({0}) ";
                string values = ConstructValues(new ArrayList
                {
                    para.SupplierNo,
                    para.SupplierName,
                    para.ReceiveCount,
                    para.AssemblySeq,
                    para.JobSeq,
                    para.PartNum,
                    para.PartDesc,
                    para.IUM,
                    para.JobNum,
                    para.Remark,
                    para.TranType,
                    para.PartType,
                    para.OpDesc,
                    para.CommentText,
                    para.Description,
                    para.NeedReceiptQty,
                    para.NotReceiptQty,
                    para.SecondUserID,
                    HttpContext.Current.Session["UserId"].ToString(),
                    para.Status,
                    para.PoNum,
                    para.PoLine,
                    para.PORelNum,
                    para.BatchNo,
                    HttpContext.Current.Session["Company"].ToString(),
                    HttpContext.Current.Session["Plant"].ToString(),
                    para.IsPrint,
                    para.ReceiptNo,
                    para.HeatNum,
                    para.ReceiptDate
                });
                string.Format(sql, values);
                #endregion

                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
            }
            
            return true;
        }

        public static IEnumerable<Receipt> GetIQCMessage()
        {
            #region 构造sql语句
            string sql = @"select 
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
                        ReceiptDate
                        from Receipt where SecondUserID = '"+ HttpContext.Current.Session["UserId"].ToString() + "' and status = 2 ";
            #endregion

            List<Receipt> POs = CommonRepository.DataTableToList<Receipt>(SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql));

            return POs;
        }

        public static bool UpdateIQCAmount(Receipt para)
        {
            string sql = "select NotReceiptQty from Receipt where ID = " + para.ID + " ";

            decimal NotReceiptQty = (decimal)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

            if(para.QualifiedCount <= NotReceiptQty)
            {
                sql = @"update Receipt set IQCDate = getdate(), IsAllCheck = {0}, SpotCheckCount = {1}, QualifiedCount = {2}, UnqualifiedCount = {3}, Result = '{4}'，Remark = '{5}'，Status=3，ThirdUserID = '{6}' where ID = {7}";
                string.Format(sql, para.IsAllCheck, para.SpotCheckCount, para.QualifiedCount, para.UnqualifiedCount, para.Result, para.Remark, para.ThirdUserID, para.ID);
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                return true;
            }

            return false;
        }

        public static IEnumerable<Receipt> GetACTMessage()
        {
            #region 构造sql语句
            string sql = @"select 
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
                        Result
                        from Receipt where ThirdUserID = '" + HttpContext.Current.Session["UserId"].ToString() + "' and status = 3 ";
            #endregion

            List<Receipt> POs = CommonRepository.DataTableToList<Receipt>(SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql));

            return POs;
        }

        public static bool UpdateACTInfo(Receipt para)
        {
            string sql = "select NotReceiptQty from Receipt where ID = " + para.ID + " ";

            decimal NotReceiptQty = (decimal)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

            if (para.ActCount <= NotReceiptQty)
            {
                sql = @"update Receipt set ActReceiptDate = getdate(), ActCount = {0}, Warehouse = '{1}', BinNum = '{2}' where ID = {3}";
                string.Format(sql, para.ActCount, para.Warehouse, para.BinNum, para.ID);
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                //待续
            }
        
            return false;
        }

    }
}
