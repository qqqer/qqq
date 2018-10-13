using System;
using System.Collections.Generic;
using System.Text;
using System.Web;


namespace CommonClass
{
    public class CookiesOperate
    {
        /// <summary>  
        /// 保存一个Cookie 若不输入过期时间表示关闭页面时消失
        /// </summary>  
        /// <param name="CookieName">Cookie名称</param>  
        /// <param name="CookieValue">Cookie值</param>  
        /// <param name="CookieTime">Cookie过期时间(天),0为关闭页面失效</param>  
        static public void SaveCookie(string CookieName, string CookieValue, double CookieTime)
        {
            HttpCookie myCookie = new HttpCookie(CookieName);
            DateTime now = DateTime.Now;
            myCookie.Value = HttpUtility.UrlEncode(CookieValue, Encoding.UTF8);//IIS里运行可能会造成乱码

            if (CookieTime != 0)
            {
                myCookie.Expires = now.AddDays(CookieTime);
                if (HttpContext.Current.Response.Cookies[CookieName] != null)
                    HttpContext.Current.Response.Cookies.Remove(CookieName);

                HttpContext.Current.Response.Cookies.Add(myCookie);
            }
            else
            {
                if (HttpContext.Current.Response.Cookies[CookieName] != null)
                    HttpContext.Current.Response.Cookies.Remove(CookieName);

                HttpContext.Current.Response.Cookies.Add(myCookie);
            }
        }

        /// <summary>  
        /// 保存一个Cookie  若不输入过期时间表示关闭页面时消失
        /// </summary>  
        /// <param name="CookieName">Cookie名称</param>  
        /// <param name="CookieValue">Cookie值</param>  
        static public void SaveCookie(string CookieName, string CookieValue)
        {
            HttpCookie myCookie = new HttpCookie(CookieName);
            DateTime now = DateTime.Now;
            myCookie.Value = HttpUtility.UrlEncode(CookieValue, Encoding.UTF8);//IIS里运行可能会造成乱码
            if (HttpContext.Current.Response.Cookies[CookieName] != null)
                HttpContext.Current.Response.Cookies.Remove(CookieName);
            HttpContext.Current.Response.Cookies.Add(myCookie);
        }


        /// <summary>  
        /// 取得CookieValue  
        /// </summary>  
        /// <param name="CookieName">Cookie名称</param>  
        /// <returns>Cookie的值</returns>  
        static public string GetCookie(string CookieName)
        {
            HttpCookie myCookie = new HttpCookie(CookieName);
            myCookie = HttpContext.Current.Request.Cookies[CookieName];

            if (myCookie != null)

                return HttpUtility.UrlDecode(myCookie.Value, Encoding.UTF8);
            else
                return null;
        }


        /// <summary>  
        /// 清除CookieValue  
        /// </summary>  
        /// <param name="CookieName">Cookie名称</param>  
        public static void ClearCookie(string CookieName)
        {
            HttpCookie myCookie = new HttpCookie(CookieName);
            DateTime now = DateTime.Now;

            myCookie.Expires = now.AddYears(-2);

            HttpContext.Current.Response.Cookies.Add(myCookie);
        }
    }
}
