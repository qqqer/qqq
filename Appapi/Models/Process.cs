using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Appapi.Models
{
    public class Process
    {
        public string JobNum { get; set; }
        public int AssemblySeq { get; set; }
        public int JobSeq { get; set; }
        public int FirstQty { get; set; }
        public string CheckUserGroup { get; set; }
        public int PrintQty { get; set; }
        public string OpCode { get; set; }
        public string OpDesc { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string StartUser { get; set; }
        public int ID { get; set; }
        public bool IsParallel { get; set; }
        public string ShareUserGroup { get; set; }
        public string Plant { get; set; }
    }
}