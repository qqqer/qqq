using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Services;
using OA_WebService.Model;

namespace OA_WebService
{
    /// <summary>
    /// WebService1 的摘要说明
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // 若要允许使用 ASP.NET AJAX 从脚本中调用此 Web 服务，请取消注释以下行。 
    // [System.Web.Script.Services.ScriptService]
    public class WebService1 : System.Web.Services.WebService
    {
        [WebMethod]
        public string HelloWorld(string sd)
        {
            //Hashtable ht = XmlHandler.GetParametersFromXML(sd);
            //string result = "Hello world! Time is: " + DateTime.Now;
            //var resp = new HttpResponseMessage(HttpStatusCode.OK);
            //resp.Content = new StringContent(sd, System.Text.Encoding.UTF8, "text/plain");
            
            return sd;
        }


        [WebMethod]
        public string MTL2199(string paraXML)
        {
            string ret =  MTL.DMRDiscardHandler(paraXML);
            return ret;
        }

        [WebMethod]
        public string PRO2199(string paraXML)
        {
            string ret = PRO.DMRDiscardHandler(paraXML);
            return ret;
        }

        [WebMethod]
        public string SUB2199(string paraXML)
        {
            string ret = SUB.DMRDiscardHandler(paraXML);
            return ret;
        }
    }
}
