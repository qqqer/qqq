using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Appapi.Models
{
    public class SubcontractDis
    {
        public string SupplierNo { get; set; }
        public string SupplierName { get; set; }
        public DateTime CreateDate { get; set; }
        public string PartNum { get; set; }
        public string PartDesc { get; set; }
        public decimal? DisQty { get; set; }
        public string IUM { get; set; }
        public int? PoNum { get; set; }
        public int? PoLine { get; set; }
        public int? PORelNum { get; set; }
        public string JobNum { get; set; }
        public int? AssemblySeq { get; set; }
        public int? JobSeq { get; set; }
        public string OpCode { get; set; }
        public string OpDesc { get; set; }
        public string M_Remark { get; set; }
        public string CommentText { get; set; }
        public string Plant { get; set; }
        public string Company { get; set; }
        public string FirstUserID { get; set; }
        public bool M_IsDelete { get; set; }
        public string UnQualifiedReason { get; set; }
        public string PackSlip { get; set; }
        public int TranID { get; set; }
        public int DMRID { get; set; }
        public decimal? TotalDMRQualifiedQty { get; set; }
        public decimal? TotalDMRRepairQty { get; set; }
        public decimal? TotalDMRUnQualifiedQty { get; set; }
        public bool ExistSubProcess { get; set; }
        public decimal? CheckCounter { get; set; }
        public int? Type { get; set; }
        public decimal? ReqQty { get; set; }
        public string StockPosition { get; set; }
        public string ResponsibilityRemark { get; set; }
        public bool POReceived { get; set; }

        public int M_ID { get; set; }

        public int RelatedID { get; set; }
        public DateTime DMRDate { get; set; }
        public DateTime TransferDate { get; set; }
        public DateTime AccepterDate { get; set; }
        public string DMRUserID { get; set; }
        public string TransferUserGroup { get; set; }
        public string TransferUserID { get; set; }
        public string AccepterUserGroup { get; set; }
        public string AccepterUserID { get; set; }
        public bool S_IsDelete { get; set; }
        public bool IsComplete { get; set; }
        public long AtRole { get; set; }
        public string S_Remark { get; set; }
        public decimal? DMRQualifiedQty { get; set; }
        public decimal? DMRRepairQty { get; set; }
        public decimal? DMRUnQualifiedQty { get; set; }
        public string DMRUnQualifiedReason { get; set; }
        public string DMRJobNum { get; set; }
        public string DMRWarehouseCode { get; set; }
        public string DMRBinNum { get; set; }
        public int? NodeNum { get; set; }
        public string BinNum { get; set; }
        public string DMRUnQualifiedReasonRemark { get; set; }
        public string DMRUnQualifiedReasonDesc { get; set; }
        public string S_ResponsibilityRemark { get; set; }
        public string Responsibility { get; set; }
        public int? S_ID { get; set; }
       
        public string FromUser { get; set; }
    }
}