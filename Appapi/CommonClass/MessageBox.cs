using System;
using System.Text;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

namespace CommonClass
{
    public class MessageBox
    {
        public MessageBox()
        {
            //
            // TODO: 在此处添加构造函数逻辑
            //
        }
        static public void Show(string msg)
        {
            msg = GetString(msg);
            HttpContext.Current.Response.Write("<script type='text/javascript' language='javascript' >alert('" + @msg.ToString() + "');</script>");
            //page.ClientScript.RegisterStartupScript(page.GetType(), "message", "<script type='text/javascript' language='javascript' defer='defer'>alert('" + @msg.ToString() + "');</script>");
        }

        /// <summary>
        /// 显示消息提示对话框
        /// </summary>
        /// <param name="page">当前页面指针，一般为this</param>
        /// <param name="msg">提示信息</param>
        static public void Show(System.Web.UI.Page page, string msg)
        {
            msg = GetString(msg); page.ClientScript.RegisterStartupScript(page.GetType(), "message", "<script type='text/javascript' language='javascript' defer='defer'>alert('" + @msg.ToString() + "');</script>");
        }

        /// <summary>
        /// 给服务器端控件添加ONCLICK确认脚本
        /// </summary>
        /// <param name="Control">控件对象</param>
        /// <param name="msg">提示信息</param>
        static public void ShowConfirm(WebControl Control, string msg)
        {
            msg = GetString(msg);
            Control.Attributes.Add("onclick", "return confirm('" + msg + "');");
        }

        /// <summary>
        /// 给客户端控件添加ONCLICK确认脚本
        /// 重载
        /// </summary>
        /// <param name="Control">控件对象</param>
        /// <param name="msg">提示信息</param>
        static public void ShowConfirm(HtmlControl Control, string msg)
        {
            msg = GetString(msg);
            Control.Attributes.Add("onclick", "return confirm('" + msg + "');");
        }

        /// <summary>
        /// 给服务器端控件添加自定义脚本
        /// </summary>
        /// <param name="Control">控件对象</param>
        /// <param name="strKey">属性</param>
        /// <param name="msg">事件</param>
        static public void AddMessage(WebControl Control, string strKey, string msg)
        {
            msg = GetString(msg);
            Control.Attributes.Add(strKey, msg);
        }

        /// <summary>
        /// 给客户端控件添加自定义脚本函数
        /// </summary>
        /// <param name="Control">控件对象</param>
        /// <param name="strKey">属性</param>
        /// <param name="msg">事件</param>
        static public void AddMessage(HtmlControl Control, string strKey, string msg)
        {
            msg = GetString(msg);
            Control.Attributes.Add(strKey, msg);
        }

        /// <summary>
        /// 显示消息提示对话框，并进行页面跳转
        /// </summary>
        /// <param name="page">当前页面指针，一般为this</param>
        /// <param name="msg">提示信息</param>
        /// <param name="url">跳转的目标URL，如果为空，则回返上一页</param>
        static public void ShowAndRedirect(System.Web.UI.Page page, string msg, string url)
        {
            StringBuilder Builder = new StringBuilder();
            Builder.Append("<script  type='text/javascript' language='javascript' defer='defer'>");
            Builder.AppendFormat("alert('{0}');", GetString(msg));
            if (url != "") Builder.AppendFormat("window.location.href='{0}'", url);
            else Builder.Append("window.history.go(-1);");
            Builder.Append("</script>");
            page.ClientScript.RegisterStartupScript(page.GetType(), "message", Builder.ToString());
        }
        /// <summary>
        /// 显示消息提示对话框，并进行页面跳转
        /// </summary>
        /// <param name="page">当前页面指针，一般为this</param>
        /// <param name="msg">提示信息</param>
        /// <param name="returnPageCount">后退几页</param>
        static public void ShowAndRedirect(System.Web.UI.Page page, string msg, int returnPageCount)
        {
            StringBuilder Builder = new StringBuilder();
            Builder.Append("<script  type='text/javascript' language='javascript' defer='defer'>");
            Builder.AppendFormat("alert('{0}');", GetString(msg));
            Builder.AppendFormat("window.history.go(-{0});", returnPageCount);
            Builder.Append("</script>");
            page.ClientScript.RegisterStartupScript(page.GetType(), "message", Builder.ToString());
        }


        /// <summary>
        /// 屏蔽脚本字符串中出现的单引号问题
        /// </summary>
        /// <param name="strmsg">脚本字符串</param>
        /// <returns>屏蔽单引号的字符串</returns>
        static private string GetString(string strmsg)
        {
            strmsg = @strmsg.Replace("'", "\\'");
            strmsg = @strmsg.Replace("\n", "\\n");
            strmsg = @strmsg.Replace("\r", "\\r");
            return strmsg;
        }
    }
}
