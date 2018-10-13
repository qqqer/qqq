using System;
using System.Collections.Generic;
using System.Text;

namespace EpicorAPIManager
{
    public class Commom
    {
        /// <summary>
        /// 每一个字段及值的集合。举例：ShortChar01@#ShortChar01 Value**ShortChar02@#ShortChar02 Value
        /// </summary>
        public static string splitArrayLineStr = "\\*\\*";//ConfigurationManager.AppSettings["SplitArrayLineStr"];

        /// <summary>
        /// 字段和字段值.举例：ShortChar01@#ShortChar01 Value**ShortChar02@#ShortChar02 Value
        /// </summary>
        public static string splitArrayKeyAndValStr = "\\@\\#";//ConfigurationManager.AppSettings["SplitArrayKeyAndValStr"];

        /// <summary>
        /// 行与行.举例：ShortChar01@#ShortChar01 Value**ShortChar02@#ShortChar02 Value&&ShortChar01@#ShortChar01 Second line Value**ShortChar02@#ShortChar02 Second line Value1
        /// </summary>
        public static string splitArrayLine = "\\&\\&";
    }
}
