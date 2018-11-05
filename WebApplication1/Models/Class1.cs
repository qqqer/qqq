using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using Appapi.Models;

namespace WebApplication1.Models
{
    public static class Class1
    {
        public static DataTable GetPO()
        {
            string sql = @"select vd.VendorID,  vd.name,  pr.*  from  erp.POHeader ph 

                        left join erp.Vendor vd on ph.VendorNum = vd.VendorNum and ph.company = vd.company

                        left join erp.PORel pr   on  ph.PONum = pr.PONum  and  pr.Company = ph.Company 

                        where ph.Approve = 1 and ph.Confirmed =1 and  ph.OpenOrder = 1   and    ph.orderHeld != 1  

                        and vd.VendorID = 'AA0002' ";
            

            DataTable dt = SQLRepository.ExecuteQueryToDataTable(SQLRepository.ERP_strConn, sql);

            return dt;
        }
    }
}