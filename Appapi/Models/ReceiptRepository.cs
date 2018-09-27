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
                    values += "getdate()" + (i == array.Count - 1 ? "" : ",");
                }
            }
            return values;
        }//根据待拆入的字段值来生成sql语句中的values部分。

        private static void AddOpLog(int ReceiptID, int ApiNum, string OpType, string OpDate)
        {
            string sql = @"insert into OpLog(ReceiptId, UserId, Company, plant, Opdate, ApiNum, OpType) Values({0}, '{1}', '{2}', '{3}', '{4}', {5}, '{6}')";
            string.Format(sql, ReceiptID, HttpContext.Current.Session["UserId"].ToString(), HttpContext.Current.Session["Company"].ToString(), HttpContext.Current.Session["Plant"].ToString(), OpDate, ApiNum, OpType);

            SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
        }//添加操作记录

        #region 接收
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
                (pr.XRelQty-pr.PassedQty) NotReceiptQty,
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
            if (Condition.PartNum != null)
                sql += "and pr.partnum like '%" + Condition.PartNum + "%' ";
            if (Condition.PartDesc != null)
                sql += "and pd.LineDesc like '%" + Condition.PartDesc + "%' ";
            #endregion

            List<Receipt> POs = CommonRepository.DataTableToList<Receipt>(SQLRepository.ExecuteQueryToDataTable(SQLRepository.ERP_strConn, sql));

            if (POs != null)
            {
                for (int i = 0; i < POs.Count; i++)
                {
                    sql = "select (case ActCount when null then ReceiveCount else ActCount end) from Receipt " +
                        "where ponum = " + POs[i].PoNum + " and poline = " + POs[i].PoLine + " and  PORelNum = " + POs[i].PORelNum + " and company = " + POs[i].Company + " and plant = " + POs[i].Plant + "";

                    object sum = SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);
                    POs[i].NotReceiptQty -= sum != null ? (int)sum : 0;
                }//计算POs中每个PO的NotReceiptQty
            }

            return POs;
        }

        public static string ReceiveCommitWithNonQRCode(Receipt para)
        {
            string OpDate = DateTime.Now.ToString();
            string sql = "select status from Receipt where PoNum = {0}, PoLine = {1}, PORelNum = {2}, Plant = '{3}', Company = '{4}'";
            string.Format(sql, para.PoNum, para.PoLine, para.PORelNum, para.Plant, para.Company);

            object status = SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);
            if (status != null && (int)status > 1)//该收货该流程已有一条receipt记录， 且已在该节点已被处理
            {
                return "处理失败-1"; //在该节点已被处理
            }

            #region 计算批次号
            sql = "select * from SerialNumber where name = 'BAT'";
            DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql);
            
            string OriDay = dt.Rows[0]["time"].ToString().Substring(0, 10);//截取年月日部分
            string today = DateTime.Now.ToString().Substring(0, 10);

            if (OriDay == today)
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

            if (PrintRepository.PrintQR("C:\\btw\\D0201.btw", HttpContext.Current.Session["UserPrinter"].ToString(), 1, jsonStr) == "1|处理成功")
            {
                para.IsPrint = true;
                para.Status = 2;
            }
            else
                return "处理失败-2"; //打印失败
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
                    para.SecondUserGroup,
                    HttpContext.Current.Session["UserId"].ToString(),//
                    para.Status,
                    para.PoNum,
                    para.PoLine,
                    para.PORelNum,
                    para.BatchNo,
                    HttpContext.Current.Session["Company"].ToString() ,//
                    HttpContext.Current.Session["Plant"].ToString() ,//
                    para.IsPrint,
                    0,
                    0,
                    para.HeatNum,
                    para.Warehouse,
                    para.ReceiptDate
                });
                string.Format(sql, values);
                #endregion

                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
            }
            #endregion

            sql = "select ID from Receipt where PoNum = {0}, PoLine = {1}, PORelNum = {2}, Plant = '{3}', Company = '{4}'";
            string.Format(sql, para.PoNum, para.PoLine, para.PORelNum, para.Plant, para.Company);

            int ID = (int)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

            AddOpLog(ID, 101, "insert", OpDate);
            return "处理成功";
        }

        public static string GetSupplierName(string SupplierNo)
        {
            string sql = "select name from erp.vendor where vendorid = '" + SupplierNo + "' ";

            return (string)SQLRepository.ExecuteScalarToObject(SQLRepository.ERP_strConn, CommandType.Text, sql, null); ;
        }

        public static string ReceiveCommitWithQRCode(Receipt para)
        {
            string OpDate = DateTime.Now.ToString();

            string sql = "select status from Receipt where PoNum = {0}, PoLine = {1}, PORelNum = {2}, Plant = '{3}', Company = '{4}'";
            string.Format(sql, para.PoNum, para.PoLine, para.PORelNum, para.Plant, para.Company);

            object status = SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

            if (status != null && (int)status > 1) //该收货该流程已有一条receipt记录，且当前位置在第一个节点之后
                return "处理失败-1"; //在该节点已被处理
            else if (status != null && (int)status == 1) //该收货该流程已有一条receipt记录，且该流程在第二节点被退回。
            {
                sql = "select NotReceiptQty from Receipt where id = "+para.ID+"";
                int NotReceiptQty = (int)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                if (para.ReceiveCount <= NotReceiptQty)
                {
                    #region 构造sql语句
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
                        para.SupplierNo,
                        para.SupplierName,
                        para.ReceiveCount,
                        para.PartNum,
                        para.PartDesc,
                        para.JobNum,
                        para.Remark,
                        para.SecondUserGroup,
                        para.FirstUserID,
                        para.PoNum,
                        para.PoLine,
                        para.PORelNum,
                        para.BatchNo,
                        para.Company,
                        para.Plant,
                        para.HeatNum,
                        para.Warehouse,
                        para.ReceiptDate,
                        para.ID);
                    #endregion

                    SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                    AddOpLog((int)para.ID, 102, "update", OpDate);
                    return "处理成功"; //更新提交成功
                }

                return "处理失败-3"; //接收节点超收
            }
            else //该流程是新的，因此为其生成一条receipt记录
            {
                Receipt temp = GetPO(para).ElementAt(0);

                if (temp == null) return "处理失败-2"; // 未在erp中找到其余相关数据

                if (para.ReceiveCount <= temp.NotReceiptQty)
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
                    para.Warehouse = temp.Warehouse;

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
                    para.SecondUserGroup,
                    HttpContext.Current.Session["UserId"].ToString(),
                    para.Status,
                    para.PoNum,
                    para.PoLine,
                    para.PORelNum,
                    para.BatchNo,
                    HttpContext.Current.Session["Company"].ToString(),
                    HttpContext.Current.Session["Plant"].ToString(),
                    para.IsPrint,
                    para.HeatNum,
                    0,
                    0,
                    para.Warehouse,
                    para.ReceiptDate
                });
                    string.Format(sql, values);
                    #endregion

                    SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                    sql = "select ID from Receipt where PoNum = {0}, PoLine = {1}, PORelNum = {2}, Plant = '{3}', Company = '{4}'";
                    string.Format(sql, para.PoNum, para.PoLine, para.PORelNum, para.Plant, para.Company);

                    int ID = (int)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                    AddOpLog((int)para.ID, 102, "insert", OpDate);
                    return "处理成功";
                }
                return "处理失败-3"; //接收节点超收
            }
        }

        public static IEnumerable<Receipt> GetRemainsOfReceiveUser()
        {
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
                        where FirstUserID = '" + HttpContext.Current.Session["UserId"].ToString() + "' and status = 1";

            List<Receipt> Remains = CommonRepository.DataTableToList<Receipt>(SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql));

            return Remains;
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
                        ReceiptNo,
                        HeatNum,
                        ReceiptDate,
                        Warehouse
                        from Receipt where SecondUserGroup like '%" + HttpContext.Current.Session["UserId"].ToString() + "%' and status = 2 ";
            #endregion

            List<Receipt> Remains = CommonRepository.DataTableToList<Receipt>(SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql));

            return Remains;
        }

        public static string IQCCommit(Receipt para)
        {
            string OpDate = DateTime.Now.ToString();

            string sql = "select status from Receipt where ID = " + para.ID + " ";
            object status = SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

            if ((int)status < 2) return "处理失败-1"; //已被退回至某个节点。
            if ((int)status > 2) return "处理失败-2"; //该节点已被处理完毕


            sql = "select NotReceiptQty from Receipt where ID = " + para.ID + " ";
            object NotReceiptQty = SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

            if (para.Status == 2 || para.QualifiedCount <= (decimal)NotReceiptQty)
            {
                sql = @"update Receipt set IQCDate = getdate(), IsAllCheck = {0}, SpotCheckCount = {1}, QualifiedCount = {2}, UnqualifiedCount = {3}, Result = '{4}'，Remark = '{5}'，Status="+para.Status+"，ThirdUserGroup = '{6}', SecondUserID = '{7}', ReceiptNo = '{8}' where ID = {9}";
                string.Format(sql, para.IsAllCheck, para.SpotCheckCount, para.QualifiedCount, para.UnqualifiedCount, para.Result, para.Remark, para.ThirdUserGroup, HttpContext.Current.Session["UserId"].ToString(), para.ReceiptNo, para.ID);
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                
                AddOpLog((int)para.ID, 201, para.Status == 3 ? "update" : "save", OpDate);

                return "处理成功";
            }

            return "处理失败-3"; //(para.QualifiedCount <= NotReceiptQty IQC超收)
        }
        #endregion

        #region 入库
        public static IEnumerable<Receipt> GetRemainsOfAcceptUser()
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
                        Result,
                        Warehouse
                        from Receipt where ThirdUserGroup like '%" + HttpContext.Current.Session["UserId"].ToString() + "%' and status = 3 ";
            #endregion

            List<Receipt> Remains = CommonRepository.DataTableToList<Receipt>(SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql));

            return Remains;
        }

        public static string AcceptCommit(Receipt para)
        {
            string OpDate = DateTime.Now.ToString();
            string sql = "select status from Receipt where ID = " + para.ID + " ";
            object status = SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

            if ((int)status < 3) return "处理失败-1"; //已被退回至某个节点。
            if ((int)status > 3) return "处理失败-2"; //该节点已被处理完毕

            sql = "select NotReceiptQty from Receipt where ID = " + para.ID + " ";
            object NotReceiptQty = SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

            if (para.ActCount <= (decimal)NotReceiptQty)
            {
                sql = @"update Receipt set StockDate = getdate(), ActCount = {0}, Warehouse = '{1}', BinNum = '{2}', ThirdUserID = '{3}' where ID = {4}";
                string.Format(sql, para.ActCount, para.Warehouse, para.BinNum, HttpContext.Current.Session["UserId"].ToString(), para.ID);
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                //调用反写接口。     若反写erp失败，返回"处理失败-2" //反写数据进erp时失败

                //AddOpLog((int)para.ID, 301, "update", OpDate);
                //return "处理成功";
            }

            return "处理失败-3"; //para.ActCount <= NotReceiptQty 入库超收
        }
        #endregion

        public static string GetNextUserGroup()
        {
            DataTable dt;
            string sql, NextUserGroup = null;

            sql = "select * from userfile where userid = '{0}', company = '{1}', plant = '{2}'";
            string.Format(sql, HttpContext.Current.Session["UserId"].ToString(), HttpContext.Current.Session["Company"].ToString(), HttpContext.Current.Session["Plant"].ToString());
            dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.ERP_strConn, sql);

            sql = "select userid from userfile where company = '{0}' and plant = '{1}' and disabled = 0 and RoleID = {2}";
            string.Format(sql, dt.Rows[0][3].ToString(), dt.Rows[0][4].ToString(), (int)dt.Rows[0][1] + 1);
            dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.ERP_strConn, sql);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                NextUserGroup += dt.Rows[i][0].ToString() + ",";
            }

            return NextUserGroup;
        }

        public static string ReturnStatus(int ReceiptID, int oristatus, int ReasonID)
        {
            string OpDate = DateTime.Now.ToString();

            string sql = "select status from Receipt where ID = " + ReceiptID + " ";
            object currstatus = SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

            if ((int)currstatus < oristatus) return "处理失败-1"; //已被退回至某个节点。
            if ((int)currstatus > oristatus) return "处理失败-2"; //该节点已被处理完毕。

            int ApiNum;
            if (oristatus == 3)
            {
                ApiNum = 300;
                sql = @"update Receipt set status = 2, ReturnTwo = ReturnTwo+1 where ID = " + ReceiptID + " ";
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = "select ReturnTwo from Receipt where ID = " + ReceiptID + " ";
                int ReturnTwo = (int)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = @"insert into ReasonRecord(ReceiptId, ReturnTwo, ReturnReasonId, Date) Values("+ ReceiptID + ", "+ ReturnTwo + ", "+ ReasonID + ", '"+ OpDate + "')";
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

            }
            else //if (oristatus == 2)
            {
                ApiNum = 200;
                sql = @"update Receipt set status = 1, ReturnOne = ReturnOne+1 where ID = " + ReceiptID + "";
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = "select ReturnOne from Receipt where ID = " + ReceiptID + " ";
                int ReturnOne = (int)SQLRepository.ExecuteScalarToObject(SQLRepository.APP_strConn, CommandType.Text, sql, null);

                sql = @"insert into ReasonRecord(ReceiptId, ReturnOne, ReturnReasonId, Date) Values(" + ReceiptID + ", " + ReturnOne + ", " + ReasonID + ", '" + OpDate + "')";
                SQLRepository.ExecuteNonQuery(SQLRepository.APP_strConn, CommandType.Text, sql, null);
            }
   

            AddOpLog(ReceiptID, ApiNum, "return", OpDate);
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
