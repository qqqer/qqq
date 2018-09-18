using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    public class Users
    {
        public static string UserId { get; set; }
        public static string Username { get; set; }
        public static string Password { get; set; }
        //public static bool AutoLogin { get; set; }
        public static Companies CompanyId { get; set; }
        public static string CurComp { get; set; }
        public static DateTime PwdLastChanged { get; set; }
        public static int PwdExpiresDays { get; set; }
        public static DateTime PwdExpires { get; set; }
        public static string GroupList { get; set; }
        public static string CompList { get; set; }
        public static string  LangNameID { get; set; }
        public static string LangDesc { get; set; }

        //public static string ConfigFilePath { get; set; }
        //public static string ConfigFile { get; set; }
        //public static string ConfigPath { get; set; }
        public static string SessionID { get; set; }

    }
}
