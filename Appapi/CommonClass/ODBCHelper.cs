using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Odbc;

namespace CommonClass
{
    public class ODBCHelper
    {
        private string sConString = "";
        private OdbcConnection con;
        private OdbcCommand cmd;

        public ODBCHelper()
        {
            if (System.Configuration.ConfigurationManager.AppSettings["EpicorConnString"] == null)
            {
                sConString = "";
            }
            else
            {
                sConString = System.Configuration.ConfigurationManager.AppSettings["EpicorConnString"];
            }


            con = new OdbcConnection(sConString);
            cmd = new OdbcCommand();
            cmd.Connection = con;

        }

        /// <summary>
        /// 执行一条Sql语句
        /// </summary>
        /// <param name="aSql"></param>
        public virtual void ExecuteSql(string aSql)
        {
            SqlSetup(aSql);
            TransBegin();
            try
            {
                //OpenConnection();
                cmd.ExecuteNonQuery();
                TransCommit();
            }
            catch (Exception e)
            {
                TransRollBack();
                throw e;
            }
            finally
            {
                
                con.Close();
            }
        }

        /// <summary>
        /// 根据Sql语句获取对应的DataTable
        /// </summary>
        /// <param name="aSql"></param>
        /// <returns></returns>
        public virtual DataTable GetTableBySql(string aSql)
        {
            try
            {
                SqlSetup(aSql);
                return SqlFillTable("");
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// 根据Sql语句获取第一行第一列
        /// </summary>
        /// <param name="aSql"></param>
        /// <returns></returns>
        public virtual object GetOneDataBySql(string aSql)
        {
            try
            {
                SqlSetup(aSql);
                OpenConnection();
                return cmd.ExecuteScalar();
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                con.Close();
            }
        }


        /// <summary>
        /// 根据Sql语句获取对应的DataSet
        /// </summary>
        /// <param name="aSql"></param>
        /// <returns></returns>
        public virtual DataSet GetDataSetBySql(string aSql)
        {
            return this.GetDataSetBySql("", aSql);
        }

        public virtual DataSet GetDataSetBySql(string tableName, string aSql)
        {
            try
            {
                SqlSetup(aSql);
                return SqlFillDataSet(tableName);
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        private DataSet SqlFillDataSet(string tableName)
        {
            DataSet ds = new DataSet();
            ds.Tables.Add(this.SqlFillTable(tableName));
            return ds;
        }

        private DataTable SqlFillTable(string tableName)
        {
            DataTable dt = (string.IsNullOrEmpty(tableName)) ? new DataTable("Default") : new DataTable(tableName);
            OdbcDataAdapter adp = new OdbcDataAdapter(cmd);
            try
            {
                OpenConnection();
                adp.Fill(dt);
                return dt;
            }
            catch (Exception e)
            {

                throw e;

            }
            finally { con.Close(); }
        }

        private void SqlSetup(string aSql)
        {
            cmd.Parameters.Clear();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = aSql;
        }
        private void OpenConnection()
        {
            try
            {
                if (con.State == ConnectionState.Closed)
                {
                    con.Open();
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void TestConn()
        {
            if (con.State == ConnectionState.Closed)
            {
                con.Open();
            }

            con.Close();
        }

        ///<summary>
        /// 开始事务
        /// </summary>
        public virtual void TransBegin()
        {
            if (cmd.Transaction != null) { throw new Exception("存在尚未提交的事务，无法开始新事务！"); }
            try
            {
                OpenConnection();
                cmd.Transaction = con.BeginTransaction();
            }
            catch (Exception e)
            {

                throw e;
            }
            //finally { con.Close(); }
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        public virtual void TransCommit()
        {
            if (cmd.Transaction != null)
            {
                try
                {
                    //OpenConnection();
                    cmd.Transaction.Commit();
                }
                catch (Exception e)
                {

                    throw e;
                }
                finally
                {
                    con.Close();
                }
            }
        }

        /// <summary>
        /// 回滚事务
        /// </summary>
        public virtual void TransRollBack()
        {
            if (cmd.Transaction != null)
            {
                try
                {
                    //OpenConnection();
                    cmd.Transaction.Rollback();
                }
                catch (Exception e)
                {

                    throw e;
                }
                finally { con.Close(); }
            }
        }

    }
}
