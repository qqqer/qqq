using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OA_WebService.Model
{
    public class DiscardReview
    {
        public int OARequestID { get; set; }
        public int MtlReportID { get; set; }
        public int BPMID { get; set; }
        public int SubcontractDisMainID { get; set; }
        public string ReviewCreateUserID { get; set; }
        public DateTime? ReviewCreateDate { get; set; }
        public decimal? ReviewQty { get; set; }
        public decimal? TopLimit { get; set; }
        public decimal? Amount { get; set; }
        public string DR_DMRUnQualifiedReason { get; set; }
        public string DR_DMRWarehouseCode { get; set; }
        public string DR_DMRBinNum { get; set; }
        public string DR_TransformUserGroup { get; set; }
        public string DR_Responsibility { get; set; }
        public string DR_DMRUnQualifiedReasonRemark { get; set; }
        public string DR_DMRUnQualifiedReasonDesc { get; set; }
        public string DR_ResponsibilityRemark { get; set; }
        public int StatusCode { get; set; }
        public string OAReviewer { get; set; }
        public DateTime? OAReviewDate { get; set; }
        public string OAComment { get; set; }
        public int BPMSubID { get; set; }
        public int SubcontractDisSubID { get; set; }
    }
}