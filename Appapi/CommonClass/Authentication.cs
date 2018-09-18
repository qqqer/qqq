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
            CookiesOperate.SaveCookie("Epicor10ServerUrl", serverUrl, 1);
            CookiesOperate.SaveCookie("userName", userName, 1);
            string strPassWord = DESEncrypt.Encrypt(pwd);
            CookiesOperate.SaveCookie("passWord", strPassWord, 1);
        }
        public static Session E9Session1;
        /// <summary>
        /// 取得EpicorSession
        /// </summary>
        /// <returns></returns>
        public static Session GetEpicorSession(string userId,string passWord)
        {
            try
            {
                string serverUrl = ConfigurationManager.AppSettings["Epicor10ServerUrl"];
                //string userName = ConfigurationManager.AppSettings["EpicorLoginName"];
                //string passWord = ConfigurationManager.AppSettings["EpicorLoginPassword"];
                string configFilePath = ConfigurationManager.AppSettings["Epicor10ConfigFilePath"];

                //passWord = DESEncrypt.Decrypt(passWord);
                Ice.Core.Session ESession = new Ice.Core.Session(userId, passWord, serverUrl, Session.LicenseType.Default, configFilePath);
                return ESession;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }



    }
}