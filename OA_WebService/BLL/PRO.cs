using Appapi.Models;
using OA_WebService.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace OA_WebService
{
    public class PRO
    {
        internal static string DMRDiscardHandler(string paraXML)
        {
            try
            {
                string OAReviewDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

                Hashtable ht = XmlHandler.GetParametersFromXML(paraXML);

                int StatusCode = (string)ht["StatusCode"] == "同意" ? 3 : 2;
                int OARequestID = (int)ht["OARequestID"];
                string OAReviewer = (string)ht["OAReviewer"];
                string OAComment = (string)ht["OAComment"];


                string sql = @"select  *  from DiscardReview where OARequestID = " + OARequestID + "";
                DiscardReview discardReview = CommonRepository.DataTableToList<DiscardReview>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First();

                sql = @"select * from bpm where Id = " + discardReview.BPMID + "";
                OpReport theReport = CommonRepository.DataTableToList<OpReport>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录


                if (StatusCode == 2) //OA拒绝报废
                {
                    sql = " update bpm set checkcounter = checkcounter + " + discardReview.ReviewQty + "  where id = " + discardReview.BPMID + "";
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                    OpReportRepository.AddOpLog(discardReview.BPMID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 601, "", "checkcounter += " + discardReview.ReviewQty + " 更新成功");



                    sql = " update DiscardReview set StatusCode = " + StatusCode + ", OAReviewDate = '" + OAReviewDate + "', OAReviewer = '" + OAReviewer + "',OAComment = '" + OAComment + "'  where OARequestID = " + OARequestID + "";
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                    OpReportRepository.AddOpLog(discardReview.BPMID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 601, "", "StatusCode = " + StatusCode + " 更新成功");

                }
                else if (StatusCode == 3) //OA同意报废
                {
                    sql = @"select IUM  from erp.JobAsmbl where JobNum = '" + theReport.JobNum + "' and AssemblySeq = " + theReport.AssemblySeq + "";
                    object IUM = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);


                    string res = ErpAPI.CommonRepository.RefuseDMRProcessing(theReport.Company, theReport.Plant, (decimal)discardReview.ReviewQty, discardReview.DR_DMRUnQualifiedReason, (int)theReport.DMRID, IUM.ToString());
                    if (res.Substring(0, 1).Trim() != "1")
                    {
                        OpReportRepository.AddOpLog(discardReview.BPMID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 601, "", res + ". 请重新提交报废数量");
                        return "错误：" + res + ". 请重新提交报废数量";
                    }


                    OpReportRepository.InsertDiscardRecord((int)discardReview.BPMID, (decimal)discardReview.ReviewQty, discardReview.DR_DMRUnQualifiedReason,
                        (int)theReport.DMRID, discardReview.DR_DMRWarehouseCode, discardReview.DR_DMRBinNum, discardReview.DR_TransformUserGroup,
                        discardReview.DR_Responsibility, discardReview.DR_DMRUnQualifiedReasonRemark,
                        discardReview.DR_ResponsibilityRemark);
                    OpReportRepository.AddOpLog(discardReview.BPMID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 601, "", "报废子流程生成");


                    sql = " update bpm set DMRUnQualifiedQty = ISNULL(DMRUnQualifiedQty,0) + " + discardReview.ReviewQty + "  where id = " + (discardReview.BPMID) + "";
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                    OpReportRepository.AddOpLog(discardReview.BPMID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 601, "", " DMRUnQualifiedQty += " + discardReview.ReviewQty + "  更新成功");



                    sql = @"select id from BPMSub where UnQualifiedType = 1 and RelatedID  = " + discardReview.BPMID + " order by CheckDate desc";
                    object bpmsubid = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.ERP_strConn, CommandType.Text, sql, null);


                    sql = " update DiscardReview set bpmsubid = " + bpmsubid + ",  StatusCode = " + StatusCode + ", OAReviewDate = '" + OAReviewDate + "', OAReviewer = '" + OAReviewer + "',OAComment = '" + OAComment + "'  where OARequestID = " + OARequestID + "";
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                    OpReportRepository.AddOpLog(discardReview.BPMID, theReport.JobNum, (int)theReport.AssemblySeq, (int)theReport.JobSeq, 601, "", "StatusCode = " + StatusCode + " 更新成功");

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