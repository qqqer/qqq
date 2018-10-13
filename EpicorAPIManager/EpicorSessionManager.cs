using System;
using System.Collections.Generic;
using System.Web;
using CommonClass;
using Ice.Core;

namespace EpicorAPIManager
{
    public class EpicorSessionManager
    {
        private static Session epicorSession;

        public static Session EpicorSession
        {
            get
            {
                if (EpicorSessionManager.epicorSession == null)
                {

                    epicorSession = Authentication.GetEpicorSession();
                    if (epicorSession != null)
                        return epicorSession;
                    else
                        throw new Exception("Session丢失");
                    
                }
                else
                {
                    //int d = 2;
                    return EpicorSessionManager.epicorSession;
                }
            }
            set { EpicorSessionManager.epicorSession = value; }
        }
  
        public static void DisposeSession()
        {
            try
            {
                if (epicorSession != null)
                {
                    epicorSession.Dispose();
                    EpicorSession.Dispose();
                    epicorSession = null;
                }
            }
            catch { }
        }
    }
}