using System;
using System.Collections.Generic;
using System.Text;
using Ice.Core;

namespace CommonClass
{
    public class GetSession
    {
        public static string ex = "";

        public static Session epicor9Seesion;

        public static Session Get()
        {
            
            return EpicorSession;
        }


        public Session GetPoolSession()
        {
            try
            {
                Session  epicor9Seesion;
           
                string serverUrl = System.Configuration.ConfigurationManager.AppSettings["ServerUrl"];
                string userName = System.Configuration.ConfigurationManager.AppSettings["EpicorLoginName"];
                string passWord = System.Configuration.ConfigurationManager.AppSettings["EpicorLoginPassword"];

                //20160224--jeff epicor9Seesion = new Session(userName, passWord, serverUrl, Session.LicenseType.Default);
                epicor9Seesion = new Session(userName, passWord, serverUrl, Session.LicenseType.DataCollection)
                {
                    CompanyID = System.Configuration.ConfigurationManager.AppSettings["CompanyCode"]
                };
                return epicor9Seesion;      
             }
             catch (Exception e)
             {
                    ex = e.ToString();
                    if (epicor9Seesion != null)
                    {
                        epicor9Seesion.Dispose();
                    }
                    return null;
              }
        }


        public static Session EpicorSession
        {
            get
            {
                if (epicor9Seesion == null)
                {
                    string serverUrl = System.Configuration.ConfigurationManager.AppSettings["ServerUrl"];
                    string userName = System.Configuration.ConfigurationManager.AppSettings["EpicorLoginName"];
                    string passWord = System.Configuration.ConfigurationManager.AppSettings["EpicorLoginPassword"];

                    //passWord = DESEncrypt.Decrypt(passWord);
                    //passWord = "Manager";
                    try
                    {
                        //20160224--jeff epicor9Seesion = new Session(userName, passWord, serverUrl, Session.LicenseType.Default);
                        epicor9Seesion = new Session(userName, passWord, serverUrl, Session.LicenseType.DataCollection);
                        epicor9Seesion.CompanyID = System.Configuration.ConfigurationManager.AppSettings["CompanyCode"];
                    }
                    catch(Exception e)
                    {
                        ex = e.ToString();
                        if (epicor9Seesion != null)
                        {
                            epicor9Seesion.Dispose();
                        }
                        
                    }
                    return epicor9Seesion;
                    // throw new Exception("Session丢失");
                }
                else
                {
                    //if (epicor9Seesion != null)
                    //{
                    //    epicor9Seesion.Dispose();
                    //}
                    string serverUrl = System.Configuration.ConfigurationManager.AppSettings["ServerUrl"];
                    string userName = System.Configuration.ConfigurationManager.AppSettings["EpicorLoginName"];
                    string passWord = System.Configuration.ConfigurationManager.AppSettings["EpicorLoginPassword"];
                    try
                    {
                        try
                        {
                            epicor9Seesion.Dispose();
                        }
                        catch
                        {

                        }
                        epicor9Seesion = new Session(userName, passWord, serverUrl, Session.LicenseType.DataCollection);
                        epicor9Seesion.CompanyID = System.Configuration.ConfigurationManager.AppSettings["CompanyCode"];
                    }
                    catch (Exception e)
                    {
                        //if (epicor9Seesion != null)
                        //{
                        //    epicor9Seesion.Dispose();
                        //}
                    }
                    return epicor9Seesion;
                }
            }
            set { epicor9Seesion = value; }
        }

        public static void DisposeSession()
        {
            try
            {
                if (epicor9Seesion != null)
                {
                    epicor9Seesion.Dispose();
                }
            }
            catch { }
        }
    }
}
