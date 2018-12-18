using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Appapi.Models
{
    public static class CommonRepository
    {
        public static List<T> DataTableToList<T>(DataTable dt) where T : class, new()
        {
            if (dt == null || dt.Rows.Count == 0) return null;

            List<T> ts = new List<T>();
            string tempName = string.Empty;
            foreach (DataRow dr in dt.Rows)
            {
                T t = new T();
                PropertyInfo[] propertys = t.GetType().GetProperties();
                foreach (PropertyInfo pi in propertys)
                {
                    tempName = pi.Name;
                    if (dt.Columns.Contains(tempName))
                    {
                        object value = dr[tempName];
                        if (value != DBNull.Value)
                        {
                            pi.SetValue(t, value, null);
                        }
                    }
                }
                ts.Add(t);
            }
            return ts;
        }


        public static DataTable ListToTable<T>(List<T> list)
        {
            Type type = typeof(T);
            PropertyInfo[] proInfo = type.GetProperties();
            DataTable dt = new DataTable();
            foreach (PropertyInfo p in proInfo)
            {
                //类型存在Nullable<Type>时，需要进行以下处理，否则异常
                Type t = p.PropertyType;
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
                    t = t.GetGenericArguments()[0];
                dt.Columns.Add(p.Name, t);
            }
            foreach (T t in list)
            {
                DataRow dr = dt.NewRow();
                foreach (PropertyInfo p in proInfo)
                {
                    object obj = p.GetValue(t);
                    if (obj == null) continue;
                    if (p.PropertyType == typeof(DateTime) && Convert.ToDateTime(obj) < Convert.ToDateTime("1753-01-01"))
                        continue;
                    dr[p.Name] = obj;
                }
                dt.Rows.Add(dr);
            }
            return dt;
        }


        public static bool VerifyAccount(string userid, string password)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] t = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            md5.Dispose();


            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < t.Length; i++)
            {
                sb.Append(t[i].ToString("X2"));
            }

            string sql = "select loginid from [dbo].[HrmResource] where loginid = '" + userid + "' and password = '" + sb.ToString() + "' ";
            object loginid = SQLRepository.ExecuteScalarToObject(SQLRepository.OA_strConn, CommandType.Text, sql, null);


            if (loginid != null)//OA账号表中验证成功
                sql = "select * from userfile where userid = '" + (string)loginid + "' and disabled != 1";
            else //OA里不存在该userid，则检查是否是自定义账号
                sql = "select * from userfile where userid = '" + userid + "' and  password = '" + password + "'  and disabled != 1";


            DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.APP_strConn, sql);
            if (dt != null)
            {
                HttpContext.Current.Session.Add("Company", Convert.ToString(dt.Rows[0]["Company"]));
                HttpContext.Current.Session.Add("Plant", Convert.ToString(dt.Rows[0]["Plant"]));
                HttpContext.Current.Session.Add("UserId", userid.ToUpper());
                HttpContext.Current.Session.Add("RoleId", Convert.ToInt32(dt.Rows[0]["RoleID"]));
                return true;
            }

            return false;
        }


        public static decimal GetOpSeqCompleteQty(string JobNum, int AssemblySeq, int JobSeq)//工序的完成数量
        {
            string sql = @"select top 1 jo.QtyCompleted from erp.JobOper jo left join erp.JobHead jh on jo.Company = jh.Company and jo.JobNum = jh.JobNum
                        where jo.JobNum = '" + JobNum + "' and jo.AssemblySeq = " + AssemblySeq + "  and  jo.OprSeq = " + JobSeq + " order by jo.OprSeq desc";

            decimal QtyCompleted = Convert.ToDecimal(SQLRepository.ExecuteScalarToObject(SQLRepository.ERP_strConn, CommandType.Text, sql, null));

            return QtyCompleted;
        }


        public static decimal GetReqQtyOfAssemblySeq(string JobNum, int AssemblySeq)//获取当前工序所属的半成品的需求数量
        {
            string sql;

            if(AssemblySeq == 0)
                sql= @"select UDReqQty_c from JobHead  jh where jh.JobNum = '" + JobNum + "' ";
            else
                sql = @"select SurplusQty_c from JobAsmbl ja where ja.JobNum = '" + JobNum + "' and ja.AssemblySeq = " + AssemblySeq + "";

            decimal RequiredQty = Convert.ToDecimal(SQLRepository.ExecuteScalarToObject(SQLRepository.ERP_strConn, CommandType.Text, sql, null));

            return RequiredQty;
        }


        public static string ConstructInsertValues(ArrayList array)
        {
            string values = "";
            for (int i = 0; i < array.Count; i++)
            {
                if (array[i] == null)
                    values += "null" + (i == array.Count - 1 ? "" : ",");
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


        public static string GetValueAsString(object o)
        {
            return Convert.IsDBNull(o) || o == null ? "" : o.ToString();
        }


        public static object GetPreOpSeq(string JobNum, int AssemblySeq, int JobSeq)//取出同阶层中JobSeq的上一道工序号，若没有返回null
        {
            string sql = @"select top 1 jo.OprSeq from erp.JobOper jo left join erp.JobHead jh on jo.Company = jh.Company and jo.JobNum = jh.JobNum
                  where jo.JobNum = '" + JobNum + "' and jo.AssemblySeq = " + AssemblySeq + "  and  jo.OprSeq < " + JobSeq + " order by jo.OprSeq desc";

            object PreOpSeq = SQLRepository.ExecuteScalarToObject(SQLRepository.ERP_strConn, CommandType.Text, sql, null);

            return PreOpSeq;
        }

    }
}