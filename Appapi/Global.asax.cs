using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.SessionState;

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
        public override void Init()
        {
            this.PostAuthenticateRequest += (sender, e) => HttpContext.Current.SetSessionStateBehavior(SessionStateBehavior.Required);
            base.Init();
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            if (HttpContext.Current.Request.HttpMethod == "OPTIONS")
            {
                //These headers are handling the "pre-flight" OPTIONS call sent by the browser
                //HttpContext.Current.Response.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE");
                //HttpContext.Current.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept");
                //HttpContext.Current.Response.AddHeader("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept");
                //HttpContext.Current.Response.AddHeader("Access-Control-Allow-Origin", HttpContext.Current.Request.Headers.GetValues("Origin").First());
                //HttpContext.Current.Response.AddHeader("Access-Control-Allow‌​-Credentials", "true");
                //HttpContext.Current.Response.AddHeader("Access-Control-Max-Age", "1728000");
                //HttpContext.Current.Response.End();
            }
        }

        /*
        void Session_Start(object sender, EventArgs e)
        {
            if (HttpContext.Current.Session != null)
            {
                HttpContext.Current.Session.Add("Company", 1);
                //HttpContext.Current.Session.Add("Plant", Convert.ToString(dt.Rows[0]["Plant"]));
            }
        }
        */
    }
}
