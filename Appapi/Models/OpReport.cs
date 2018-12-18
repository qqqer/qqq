using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Appapi.Models
{
    public class OpReport
    {
        public string CreateUser { get; set; }
        public string CheckUser { get; set; }
        public string TransformUser { get; set; }
        public string NextUser { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime CheckDate { get; set; }
        public DateTime TransformDate { get; set; }
        public DateTime NextDate { get; set; }
        public string CheckUserGroup { get; set; }
        public string TransformUserGroup { get; set; }
        public string NextUserGroup { get; set; }
        public string PartNum { get; set; }
        public string PartDesc { get; set; }
        public string JobNum { get; set; }
        public int? AssemblySeq { get; set; }
        public int? JobSeq { get; set; }
        public string OpCode { get; set; }
        public string OpDesc { get; set; }
        public decimal? FirstQty { get; set; }
        public int? NextJobSeq { get; set; }
        public string NextOpCode { get; set; }
        public string NextOpDesc { get; set; }
        public decimal? QualifiedQty { get; set; }
        public decimal? UnQualifiedQty { get; set; }
        public string UnQualifiedReason { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal? LaborHrs { get; set; }
        public bool? IsComplete { get; set; }
        public bool? IsDelete { get; set; }
        public int? Status { get; set; }
        public int? PreStatus { get; set; }
        public string Remark { get; set; }
        public long? AtRole { get; set; }
        public string Plant { get; set; }
        public string Company { get; set; }
        public bool? IsPrint { get; set; }
        public string BinNum { get; set; }
        public int? PrintID { get; set; }
        public int? ErpCounter { get; set; }
        public string Character05 { get; set; }
        public int? ID { get; set; }



        public int? PrintQty { get; set; }
    }
}