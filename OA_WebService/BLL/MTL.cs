﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using Appapi.Models;
using OA_WebService.Model;

namespace OA_WebService
{
    public static class MTL
    {
        public static string DMRDiscardHandler(Hashtable ht)
        {
            try
            {
                string OAReviewDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                
                int StatusCode = ((string)ht["StatusCode"]).Contains("不同意") ? 2 : 3;
                int OARequestID = Convert.ToInt32(ht["OARequestID"]);
                string OAComment = (string)ht["OAComment"];


                string sql = @"select top 1 lastname from  workflow_currentoperator wc left join HrmResource hr on wc.userid = hr.id  where requestid = "+OARequestID+" and groupid < 5 order by groupid desc";
                string OAReviewer = (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.OA_strConn, CommandType.Text, sql, null);



                sql = @"select  *  from DiscardReview where OARequestID = " + OARequestID + "";
                DiscardReview discardReview = CommonRepository.DataTableToList<DiscardReview>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First();


                sql = @"select * from MtlReport where Id = " + discardReview.MtlReportID + "";
                OpReport theReport = CommonRepository.DataTableToList<OpReport>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录

                
                if (StatusCode == 2) //OA拒绝报废
                {
                    sql = " update MtlReport set checkcounter = checkcounter + " + discardReview.ReviewQty + "  where id = " + discardReview.MtlReportID + "";
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                    MtlReportRepository.AddOpLog(discardReview.MtlReportID, 201, "", "checkcounter += " + discardReview.ReviewQty + " 更新成功");

                    sql = " update DiscardReview set StatusCode = " + StatusCode + ", OAReviewDate = '" + OAReviewDate + "', OAReviewer = '" + OAReviewer + "',OAComment = '" + OAComment + "'  where OARequestID = " + OARequestID + "";
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                    MtlReportRepository.AddOpLog(discardReview.MtlReportID, 201, "", "StatusCode = " + StatusCode + " 更新成功");
                }
                else if (StatusCode == 3) //OA同意报废
                {
                    sql = @"select IUM from erp.JobMtl where JobNum ='" + theReport.JobNum + "'  and   AssemblySeq = " + theReport.AssemblySeq + " and MtlSeq= " + theReport.MtlSeq + "";
                    object IUM = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);

                    string res = ErpAPI.CommonRepository.RefuseDMRProcessing(theReport.Company, theReport.Plant, (decimal)discardReview.ReviewQty, discardReview.DR_DMRUnQualifiedReason, (int)theReport.DMRID, IUM.ToString());
                    if (res.Substring(0, 1).Trim() != "1")
                    {
                        MtlReportRepository.AddOpLog(discardReview.MtlReportID, 201, "", res + ". 请重新提交报废数量");
                        return "错误：" + res + ". 请重新提交报废数量";
                    }

                    MtlReportRepository.InsertDiscardRecord((int)discardReview.MtlReportID, (decimal)discardReview.ReviewQty, discardReview.DR_DMRUnQualifiedReason,
                        (int)theReport.DMRID, discardReview.DR_DMRWarehouseCode, discardReview.DR_DMRBinNum, discardReview.DR_TransformUserGroup,
                        discardReview.DR_Responsibility, discardReview.DR_DMRUnQualifiedReasonRemark,
                        CommonRepository.GetReasonDesc(discardReview.DR_DMRUnQualifiedReason), discardReview.DR_ResponsibilityRemark,
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),discardReview.ReviewCreateUserID);
                    MtlReportRepository.AddOpLog(discardReview.MtlReportID, 201, "", "报废子流程生成");

                    sql = " update MtlReport set DMRUnQualifiedQty = ISNULL(DMRUnQualifiedQty,0) + " + discardReview.ReviewQty + "  where id = " + (discardReview.MtlReportID) + "";
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                    MtlReportRepository.AddOpLog(discardReview.MtlReportID, 201, "", " DMRUnQualifiedQty += " + discardReview.ReviewQty + "  更新成功");


                    sql = @"select id from BPMSub where UnQualifiedType = 2 and RelatedID  = " + discardReview.MtlReportID + " order by CheckDate desc";
                    object bpmsubid = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);


                    sql = " update DiscardReview set bpmsubid = " + bpmsubid + ",  StatusCode = " + StatusCode + ", OAReviewDate = '" + OAReviewDate + "', OAReviewer = '" + OAReviewer + "',OAComment = '" + OAComment + "'  where OARequestID = " + OARequestID + "";
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                    MtlReportRepository.AddOpLog(discardReview.MtlReportID, 201, "", "StatusCode = " + StatusCode + " 更新成功");
                }

                return "true";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}