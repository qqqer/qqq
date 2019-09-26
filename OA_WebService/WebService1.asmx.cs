using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Services;
using System.Web.Services.Protocols;
using OA_WebService.Model;

namespace OA_WebService
{
    /// <summary>
    /// WebService1 的摘要说明
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    //[SoapDocumentService(RoutingStyle = SoapServiceRoutingStyle.RequestElement)]
    // 若要允许使用 ASP.NET AJAX 从脚本中调用此 Web 服务，请取消注释以下行。 
    // [System.Web.Script.Services.ScriptService]
    public class WebService1 : System.Web.Services.WebService
    {
        [WebMethod]
        public string Commit2199(string paraXML)
        {
            paraXML = HttpUtility.HtmlDecode(paraXML);
            Hashtable ht = XmlHandler.GetParametersFromXML(paraXML);
            string ret = "";
            if (ht["type"].ToString() == "制程不良报废")
            {
                ret = PRO.DMRDiscardHandler(ht);
            }
            else if (ht["type"].ToString() == "物料不良报废")
            {
                ret = MTL.DMRDiscardHandler(ht);
            }
            else if (ht["type"].ToString().Contains("外协不良报废"))
            {
                ret = SUB.DMRDiscardHandler(ht);
            }

            return ret;
        }
    }
}
