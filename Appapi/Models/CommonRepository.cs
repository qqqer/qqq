using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
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
    }
}