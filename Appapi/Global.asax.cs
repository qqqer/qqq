using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.SessionState;
using System.Windows.Forms;

namespace Appapi
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }


        public override void Init() //启用session
        {
            this.PostAuthenticateRequest += (sender, e) => HttpContext.Current.SetSessionStateBehavior(SessionStateBehavior.Required);
            base.Init();
        }


        protected void Application_EndRequest(object sender, EventArgs e)
        {
            if (HttpContext.Current.Request.HttpMethod == "OPTIONS")
            {
                HttpContext.Current.Response.StatusCode = 200;
            }

            var v = HttpContext.Current.Request.Headers.GetValues("Origin");          
            if(v != null)
                HttpContext.Current.Response.AddHeader("Access-Control-Allow-Origin", v.First());
        }



        //protected void Application_BeginRequest(object sender, EventArgs e)
        //{
        //    if(HttpContext.Current.Request.ContentType == "application/json")
        //         HttpContext.Current.Request.Params;
        //}






        //void Session_Start(object sender, EventArgs e)
        //{

        //}
        //void Session_End(object sender, EventArgs e)
        //{

        //}

    }
}
