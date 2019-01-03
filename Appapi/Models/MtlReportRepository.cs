using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace Appapi.Models
{
    public class MtlReportRepository
    {
        public static DataTable GetMtlInfo(string JobNum, int AssemblySeq)
        {
            string  sql = "select MtlSeq,PartNum,Description from erp.JobMtl where JobNum = '" + JobNum+"' and AssemblySeq = "+AssemblySeq+"";

            DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.ERP_strConn, sql);
            return dt;
        }


        public static DataTable GetPartLots(string PartNum)
        {
            string sql = "select LotNum from erp.PartLot where PartNum = '"+PartNum+"' and OnHand = 1";

            DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.ERP_strConn, sql);
            return dt;
        }


        public static string ReportCommit(OpReport ReportInfo)
        {

            return null;
        }
    }
}