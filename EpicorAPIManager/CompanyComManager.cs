using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using CommonClass;
namespace EpicorAPIManager
{
    public class CompanyComManager
    {
        /// <summary>
        /// 通过bpm公司id获得epicor的公司id
        /// </summary>
        /// <param name="bpmCompany"></param>
        /// <returns>如果没有获取成功返回为空</returns>
        //public static string GetCompanyByBpmCompany(string bpmCompany)
        //{
        //    string CompanyId = "";
        //    try
        //    {

        //        string sql = "select EpicorCompanyNo from CompanyCom where bpmCompanyNo='" + bpmCompany + "'";
        //        var obj = SqlHelper.ExecuteScalar(SqlHelper.strConn, CommandType.Text, sql);
        //        if (obj != null)
        //            CompanyId = obj.ToString();
        //        return CompanyId;
        //    }
        //    catch (Exception ex)
        //    {
        //        return "";
        //    }
        //}

        /// <summary>
        /// 根据bpm公司成对应的epicor公司
        /// </summary>
        /// <param name="bpmCompany"></param>
        public void SetCompany(string bpmCompany)
        {
           //string companyId = CompanyComManager.GetCompanyByBpmCompany(bpmCompany);
            string companyId = bpmCompany;
            if (!string.IsNullOrEmpty(companyId))
                //Authentication.SetCompany(companyId, EpicorSessionManager.EpicorSession.ConnectionPool);
            if (CommonClass.GetSession.Get() != null)
            {
                EpicorSessionManager.EpicorSession.Dispose();
            }
        }
    }


}
