﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Appapi.Models
{
    public class OpReport
    {
        public string CreateUser { get; set; }
        public string StartUser { get; set; }
        public string UserID { get; set; }
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
        public decimal? Qty { get; set; }
        public int? NextJobSeq { get; set; }
        public string NextOpCode { get; set; }
        public string NextOpDesc { get; set; }
        public decimal? QualifiedQty { get; set; }
        public decimal? UnQualifiedQty { get; set; }
        public string UnQualifiedReason { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime AverageStartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime AverageEndDate { get; set; }
        public decimal? LaborHrs { get; set; }
        public decimal? AverageLaborHrs { get; set; }
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
        public int? ReturnThree { get; set; }

        public string DefectNO { get; set; }
        public string CheckRemark { get; set; }

        public int? TranID { get; set; }
        public int? DMRID { get; set; }
        public decimal? DMRQualifiedQty { get; set; }
        public decimal? DMRUnQualifiedQty { get; set; }
        public string DMRUnQualifiedReason { get; set; }
        public string DMRWarehouseCode { get; set; }
        public string DMRBinNum { get; set; }
        public int? RelatedID { get; set; }
        public bool? IsSubProcess { get; set; }
        public decimal? DMRRepairQty { get; set; }       
        public string DMRJobNum { get; set; }
        public decimal? CheckCounter { get; set; }
        public string UnQualifiedGroup { get; set; }
        public int? UnQualifiedType { get; set; } //1 报工不良 2 制程不良
        public int? MtlSeq { get; set; }
        public string LotNum { get; set; }
        public string Responsibility { get; set; }

        public string ShareUserGroup { get; set; }

        public int? ProcessId { get; set; }

        public int? ProcessType { get; set; }

        public int? ID { get; set; }
       
        public int PrintQty { get; set; }
        public string FromUser { get; set; }

        public decimal? OnHandQty { get; set; } //表处临时仓字段
        public decimal? SumOutQty { get; set; } //表处临时仓字段

        public string DMRUnQualifiedReasonRemark { get; set; }
        public string DMRUnQualifiedReasonDesc { get; set; }
        public string ResponsibilityRemark { get; set; }
        public string UnQualifiedReasonRemark { get; set; }
        public string UnQualifiedReasonDesc { get; set; }
    }
}