﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Appapi.Models
{
    /// <summary>
    /// PoNum、PoLine、PORelNum、Company确定一个唯一的收货依据ReceivingBasis（erp数据库查询得到）
    /// PoNum、PoLine、PORelNum、Plant、Company、 batchNo 确定某个收货依据的某次收货记录（查询Receipt表得到）
    /// 
    /// 收货记录中包含它所属的收货依据的所有信息
    /// </summary>
    public class Receipt 
    {
        public int? PoNum { get; set; }
        public int? PoLine { get; set; }
        public int? JobSeq { get; set; }
        public int? AssemblySeq { get; set; }
        public decimal? ReceiveQty1 { get; set; }
        public decimal? ReceiveQty2 { get; set; }
        public int? PORelNum { get; set; }
        public int? Status { get; set; }
        public bool? IsPrint { get; set; }
        public decimal? NeedReceiptQty { get; set; }
        public decimal? NotReceiptQty { get; set; }
        public string SupplierNo { get; set; }
        public string TranType { get; set; }
        public string SupplierName { get; set; }
        public string PartType { get; set; }
        public string PartNum { get; set; }
        public string PartDesc { get; set; }
        public string OpDesc { get; set; }
        public string OpCode { get; set; }
        public string IUM { get; set; }
        public string JobNum { get; set; }
        public string CommentText { get; set; }
        public string PartClassDesc { get; set; }
        public string Plant { get; set; }
        public string Company { get; set; }
        public string Remark { get; set; }
        public string FirstUserID { get; set; }
        public string SecondUserID { get; set; }
        public string BatchNo { get; set; }
        public string ReceiptNo { get; set; }
        public string HeatNum { get; set; }
        public DateTime ReceiptDate { get; set; }
        public DateTime IQCDate { get; set; }
        public int ID { get; set; }
        public bool? IsAllCheck { get; set; }
        public decimal? InspectionQty { get; set; }
        public decimal? PassedQty { get; set; }
        public decimal? FailedQty { get; set; }
        public string Result { get; set; }
        public string ThirdUserID { get; set; }
        public string FourthUserID { get; set; }
        public DateTime StockDate { get; set; }
        public DateTime ChooseDate { get; set; }
        public DateTime ReceiptCommitDate { get; set; }
      
        public decimal? ArrivedQty { get; set; }
        public string Warehouse { get; set; }
        public string BinNum { get; set; }
        public string SecondUserGroup { get; set; }
        public string ThirdUserGroup { get; set; }
        public string FourthUserGroup { get; set; }
        public int? ReturnOne { get; set; }
        public int? ReturnTwo { get; set; }
        public int? ReturnThree { get; set; }
        public bool IsAuto { get; set; }
        public bool IsComplete { get; set; }
        public bool IsDelete { get; set; }
        public bool IsPrintRcv { get; set; }
        public long AtRole { get; set; }      
        public string NBBatchNo { get; set; }
        public string IQCRemark { get; set; }    
        public int PreStatus { get; set; }
        public string PackSlip { get; set; }
        public decimal? OurFailedQty { get; set; }
        public string POType { get; set; }
        public string DepartmentUKN { get; set; }
        public string RequestIDD { get; set; }
        public bool OAConfirmUKN { get; set; }

        public string WarehouseName { get; set; } 
        public bool IsRestrictRcv { get; set; }
        public int Colour { get; set; }
        public string ReturnReason { get; set; }
        public string ReturnReasonRemark { get; set; }
        public bool IsForPrintQR { get; set; }      
        public int? PrintQty { get; set; }
    }
}