using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using Newtonsoft.Json;
using System.Configuration;


using Ice.Core;
using Erp.Proxy.BO;
using Erp.BO;
using Epicor.ServiceModel.Channels;
using Ice.Tablesets;
using System.Web;

namespace ErpAPI
{
    public static class Common
    {
        public static Session GetEpicorSession()
        {
            try
            {
                string serverUrl = ConfigurationManager.AppSettings["ServerUrl"];
                string userName = ConfigurationManager.AppSettings["EpicorLoginName"];
                string passWord = ConfigurationManager.AppSettings["EpicorLoginPassword"];
                string configFile = ConfigurationManager.AppSettings["ConfigFile"];

                Ice.Core.Session E9Session = new Ice.Core.Session(userName, passWord, serverUrl, Session.LicenseType.Default, configFile);
                return E9Session;
            }
            catch (Exception ex)
            {
                if (ex.Message.ToString().Contains("Maximum users exceeded on license type"))
                {
                    return null;
                }
                else
                {
                    return null;
                }
            }
        }

        public static DataTable GetDataByERP(string sqlstr)
        {
            SqlConnection conn = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["erpConnectionstring"]);
            conn.Open();
            SqlDataAdapter ada = new SqlDataAdapter(sqlstr, conn);
            DataTable dt = new DataTable();
            ada.Fill(dt);
            conn.Close();
            return dt;
        }

        public static int ExecuteSql(string SQLString)
        {
            using (SqlConnection connection = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["erpConnectionstring"]))
            {
                using (SqlCommand cmd = new SqlCommand(SQLString, connection))
                {
                    try
                    {
                        connection.Open();
                        int rows = cmd.ExecuteNonQuery();
                        return rows;
                    }
                    catch (System.Data.SqlClient.SqlException e)
                    {
                        connection.Close();
                        throw e;
                    }
                }
            }
        }

        public static string QueryERP(string sqlstr)
        {
            SqlConnection conn = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["erpConnectionstring"]);
            conn.Open();
            SqlCommand cmd = new SqlCommand(sqlstr, conn);
            Object o = cmd.ExecuteScalar();
            conn.Close();
            return o == null ? "" : o.ToString();
        }
    }
}
