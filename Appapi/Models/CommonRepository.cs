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
        public static DataTable UnionDataTable(DataTable dt1, DataTable dt2)
        {
            if (dt1 == null && dt2 == null) return null;

            if (dt1 != null && dt2 == null) return dt1;

            if (dt1 == null && dt2 != null) return dt2;


            object[] obj = new object[dt2.Columns.Count];

            for (int i = 0; i < dt2.Rows.Count; i++)
            {
                dt2.Rows[i].ItemArray.CopyTo(obj, 0);
                dt1.Rows.Add(obj);
            }

            return dt1;
        }


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


        public static string GetUserName(string userid)
        {
            string sql = "select username from userfile where userid = '" + userid + "'";

            string UserName = (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

            return UserName;
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
            object loginid = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.OA_strConn, CommandType.Text, sql, null);


            if (loginid != null)//OA账号表中验证成功
                sql = "select * from userfile where userid = '" + (string)loginid + "' and disabled != 1";
            else //OA里不存在该userid，则检查是否是自定义账号
                sql = "select * from userfile where userid = '" + userid + "' and  password = '" + password + "'  and disabled != 1";


            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql);
            if (dt != null)
            {
                HttpContext.Current.Session.Add("Company", Convert.ToString(dt.Rows[0]["Company"]));
                HttpContext.Current.Session.Add("UserName", Convert.ToString(dt.Rows[0]["UserName"]));
                HttpContext.Current.Session.Add("Plant", Convert.ToString(dt.Rows[0]["Plant"]));
                HttpContext.Current.Session.Add("UserId", userid.Split('\'')[0].ToUpper());//去掉免密登录方式账号的后缀字符
                HttpContext.Current.Session.Add("RoleId", Convert.ToInt32(dt.Rows[0]["RoleID"]));
                return true;
            }

            return false;
        }



        public static void SignOut()
        {
            HttpContext.Current.Session.Abandon();
        }



        public static bool CheckVersion(string version)//ApiNum: 19   检测版本号
        {
            string sql = "select Version from SerialNumber";
            object Version = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);

            if (Version.ToString().Trim() == version.Trim())
                return true;

            return false;
        }



        public static DataTable GetMtlsOfOpSeq(string jobnum, int AssemblySeq, int jobseq, string company) //获取当前工序下所有的未发物料
        {
            string sql = @"select partnum, mtlseq, qtyper,RequiredQty  from erp.JobMtl where jobnum ='{0}' and AssemblySeq = {1} and RelatedOperation = {2} and company = '{3}' and IssuedComplete = 0";
            sql = string.Format(sql, jobnum, AssemblySeq, jobseq, company);

            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);

            return dt;
        }



        public static string GetJobHeadState(string jobnum)
        {
            string sql = @"select jh.jobClosed,jh.jobComplete, jh.JobEngineered, jh.JobReleased from erp.JobHead jh where company = '001' and jh.JobNum = '" + jobnum + "'";
            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);

            if (dt == null)
                return "工单不存在,请联系计划部";
            else if ((bool)dt.Rows[0]["jobClosed"] == true)
                return "该工单已关闭,请联系计划部";
            else if ((bool)dt.Rows[0]["jobComplete"] == true)
                return "该工单已完成,请联系计划部";
            else if ((bool)dt.Rows[0]["JobEngineered"] == false)
                return "该工单未设计,请联系计划部";
            else if ((bool)dt.Rows[0]["JobReleased"] == false)
                return "该工单未发放,请联系计划部";

            return "正常";
        }


        public static decimal GetOpSeqCompleteQty(string JobNum, int AssemblySeq, int JobSeq)//工序的完成数量
        {
            string sql = @"select QtyCompleted from erp.JobOper where JobNum = '" + JobNum + "' and AssemblySeq = " + AssemblySeq + "  and OprSeq = " + JobSeq + " ";

            decimal QtyCompleted = Convert.ToDecimal(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null));

            return QtyCompleted;
        }


        public static decimal GetReqQtyOfAssemblySeq(string JobNum, int AssemblySeq)//获取当前工序所属的半成品的需求数量
        {
            string sql;

            if (AssemblySeq == 0)
                sql = @"select UDReqQty_c from JobHead  jh where jh.JobNum = '" + JobNum + "' ";
            else
                sql = @"select SurplusQty_c from JobAsmbl ja where ja.JobNum = '" + JobNum + "' and ja.AssemblySeq = " + AssemblySeq + "";

            decimal RequiredQty = Convert.ToDecimal(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null));

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
                else if (array[i].GetType() == typeof(string) && ((string)array[i] == "" || ((string)array[i])[0] != '\u1234'))//非参数化
                {
                    values += "'" + array[i] + "'" + (i == array.Count - 1 ? "" : ",");
                }
                else if (array[i].GetType() == typeof(string) && ((string)array[i])[0] == '\u1234')//参数化 此处不会出现空串，因为空串在上一个if已经被处理掉了
                {
                    values += ((string)array[i]).Replace('\u1234', '@') + (i == array.Count - 1 ? "" : ",");
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


        public static DataTable NPI_Handler(string jobnum, DataTable UserGroup)
        {
            string sql = @"select Plant from erp.JobHead where company = '001' and JobNum = '" + jobnum + "'";

            string Plant = (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);
            if (Plant != "HDSite")
            {
                if (UserGroup != null)
                {
                    if (jobnum.ToUpper().Contains("NPI") && UserGroup != null)
                    {
                        for (int i = UserGroup.Rows.Count - 1; i >= 0; i--)
                        {
                            if (!Convert.ToBoolean(UserGroup.Rows[i]["OnlyNPI"]) && ((Convert.ToInt32(UserGroup.Rows[i]["RoleID"]) & 16) == 0)) //排除不是专门处理npi的人
                            {
                                UserGroup.Rows.RemoveAt(i);
                            }
                        }
                    }
                    else if (!jobnum.ToUpper().Contains("NPI") && UserGroup != null)
                    {
                        for (int i = UserGroup.Rows.Count - 1; i >= 0; i--)
                        {
                            if (Convert.ToBoolean(UserGroup.Rows[i]["OnlyNPI"])) //排除专门处理npi的人
                            {
                                UserGroup.Rows.RemoveAt(i);
                            }
                        }
                    }
                }
            }
            return UserGroup;
        }


        public static DataTable WD_Handler(string jobnum, DataTable UserGroup)
        {
            if (UserGroup != null)
            {
                if (jobnum.ToUpper().Contains("WD") && UserGroup != null)
                {
                    for (int i = UserGroup.Rows.Count - 1; i >= 0; i--)
                    {
                        if (!Convert.ToBoolean(UserGroup.Rows[i]["OnlyWD"]) && ((Convert.ToInt32(UserGroup.Rows[i]["RoleID"]) & 16) == 0)) //排除不是专门处理npi的人
                        {
                            UserGroup.Rows.RemoveAt(i);
                        }
                    }
                }
                else if (!jobnum.ToUpper().Contains("WD") && UserGroup != null)
                {
                    for (int i = UserGroup.Rows.Count - 1; i >= 0; i--)
                    {
                        if (Convert.ToBoolean(UserGroup.Rows[i]["OnlyWD"])) //排除专门处理npi的人
                        {
                            UserGroup.Rows.RemoveAt(i);
                        }
                    }
                }
            }

            return UserGroup;
        }


        public static DataTable GetSpecifiedSubcontractedOprInfo(int PoNum, string JobNum, int AssemblySeq, int JobSeq, string Company) //取出指定的外工序相关信息
        {
            string sql = @" Select jobseq, jo.PartNum, jo.IUM, jo.Description,  pr.poline, porelnum ,OpDesc,OpCode,pd.CommentText 
                            from erp.porel pr 
                            left join erp.PODetail pd   on pr.PONum = pd.PONUM   and   pr.Company = pd.Company   and   pr.POLine = pd.POLine 
                            left join erp.JobOper jo on pr.jobnum = jo.JobNum and pr.AssemblySeq = jo.AssemblySeq and pr.Company = jo.Company and jobseq = jo.OprSeq 
                            where pr.ponum={0} and pr.jobnum = '{1}'  and pr.assemblyseq={2} and trantype='PUR-SUB' and pr.company = '{3}'  and jobseq = {4}";
            sql = string.Format(sql, PoNum, JobNum, AssemblySeq, "001", JobSeq);
            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);
            return dt;
        }


        public static DataTable GetOprsOfAssemblySeq(string JobNum, int AssemblySeq) //取出阶层下的所有工序
        {
            string sql = @" Select jobseq ,OpDesc,OpCode
                            from  erp.JobOper jo
                            where  jo.jobnum = '{0}'  and assemblyseq={1} and company = '001' order by jobseq asc";
            sql = string.Format(sql, JobNum, AssemblySeq);
            DataTable dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);
            return dt;
        }

        public static object GetPreOpSeq(string JobNum, int AssemblySeq, int JobSeq)//取出同阶层中JobSeq的上一道工序号，若没有返回null
        {
            string sql = @"select top 1 jo.OprSeq from erp.JobOper jo left join erp.JobHead jh on jo.Company = jh.Company and jo.JobNum = jh.JobNum
                  where jo.JobNum = '" + JobNum + "' and jo.AssemblySeq = " + AssemblySeq + "  and  jo.OprSeq < " + JobSeq + " and jh.Company = '001' order by jo.OprSeq desc";

            object PreOpSeq = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);

            return PreOpSeq;
        }

        public static object GetValidPreOpSeq(string JobNum, int AssemblySeq, int JobSeq)//取出同阶层中JobSeq的上一道工序号，若没有返回null
        {
            string sql = @"select top 1 jo.OprSeq from erp.JobOper jo left join erp.JobHead jh on jo.Company = jh.Company and jo.JobNum = jh.JobNum
                  where jo.JobNum = '" + JobNum + "' and jo.AssemblySeq = " + AssemblySeq + "  and  jo.OprSeq < " + JobSeq + " and  jo.Opcode != 'BC0205' and  jo.Opcode != 'ZP0501' and jh.Company = '001' order by jo.OprSeq desc";

            object ValidPreOpSeq = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);

            return ValidPreOpSeq;
        }

        public static DataTable GetOpInfo(string JobNum, int AssemblySeq, int JobSeq)//
        {
            string sql = @"select * from erp.JobOper 
                  where JobNum = '" + JobNum + "' and AssemblySeq = " + AssemblySeq + "  and  OprSeq = " + JobSeq + "";

            DataTable  dt = Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.ERP_strConn, sql);

            return dt;
        }


        public static object GetNextOpSeq(string JobNum, int AssemblySeq, int JobSeq)//取出同阶层中JobSeq的上一道工序号，若没有返回null
        {
            string sql = @"select top 1 jo.OprSeq from erp.JobOper jo left join erp.JobHead jh on jo.Company = jh.Company and jo.JobNum = jh.JobNum
                  where jo.JobNum = '" + JobNum + "' and jo.AssemblySeq = " + AssemblySeq + "  and  jo.OprSeq > " + JobSeq + " and jh.Company = '001' order by jo.OprSeq asc";

            object NextOpSeq = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);

            return NextOpSeq;
        }


        public static bool IsOpSeqComplete(string JobNum, int AssemblySeq, int JobSeq)
        {
            string sql = @"select  OpComplete  from erp.JobOper where jobnum = '" + JobNum + "' and AssemblySeq = " + AssemblySeq + " and  OprSeq = " + JobSeq + "";

            return Convert.ToBoolean(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null));
        }

        public static string GetReasonDesc(string ReasonCode)
        {
            string sql = "select Description from erp.Reason where Company = '001' and ReasonCode = '" + ReasonCode + "' ";
            string Reasonsdesc = (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);

            return Reasonsdesc;
        }

    }
}