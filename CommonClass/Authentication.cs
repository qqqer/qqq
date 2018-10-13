using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using Ice.Core;

namespace CommonClass
{
    public class Authentication
    {
        /// <summary>
        /// 登录时写入cookies
        /// </summary>
        /// <param name="companyCode"></param>
        /// <param name="userName"></param>
        /// <param name="pwd"></param>
        public static void LoginWriteCookies(string serverUrl, string userName, string pwd)
        {
            CookiesOperate.SaveCookie("serverUrl", serverUrl, 1);
            CookiesOperate.SaveCookie("userName", userName, 1);
            string strPassWord = DESEncrypt.Encrypt(pwd);
            CookiesOperate.SaveCookie("passWord", strPassWord, 1);
        }
        public static Session E9Session1;
        /// <summary>
        /// 取得EpicorSession
        /// </summary>
        /// <returns></returns>
        public static Session GetEpicorSession()
        {
            try
            {
                string serverUrl = ConfigurationManager.AppSettings["ServerUrl"];
                string userName = ConfigurationManager.AppSettings["EpicorLoginName"];
                string passWord = ConfigurationManager.AppSettings["EpicorLoginPassword"];

                //passWord = DESEncrypt.Decrypt(passWord);
                Ice.Core.Session E9Session = new Ice.Core.Session(userName, passWord, serverUrl, Session.LicenseType.Default);
                return E9Session;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }



    }
}