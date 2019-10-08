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
    public class SUB
    {
        internal static string DMRDiscardHandler(Hashtable ht)
        {
            try
            {
                string OAReviewDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");


                int StatusCode = ((string)ht["StatusCode"]).Contains("不同意") ? 2 : 3;
                int OARequestID = Convert.ToInt32(ht["OARequestID"]);
                string OAComment = (string)ht["OAComment"];


                string sql = @"select top 1 lastname from  workflow_currentoperator wc left join HrmResource hr on wc.userid = hr.id  where requestid = " + OARequestID + " and groupid < 5 order by groupid desc";
                string OAReviewer = (string)Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.OA_strConn, CommandType.Text, sql, null);


                sql = @"select  *  from DiscardReview where OARequestID = " + OARequestID + "";
                DiscardReview discardReview = CommonRepository.DataTableToList<DiscardReview>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First();

                sql = @"select * from SubcontractDisMain where m_Id = " + discardReview.SubcontractDisMainID + "";
                SubcontractDis theSubcontractDis = CommonRepository.DataTableToList<SubcontractDis>(Common.SQLRepository.ExecuteQueryToDataTable(Common.SQLRepository.APP_strConn, sql)).First(); //获取该批次记录


                if (StatusCode == 2) //OA拒绝报废
                {
                    sql = " update SubcontractDisMain set checkcounter = checkcounter + " + discardReview.ReviewQty + "  where m_id = " + discardReview.SubcontractDisMainID + "";
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                    SubcontractDisRepository.AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, (int)theSubcontractDis.JobSeq, 201, "checkcounter += " + discardReview.ReviewQty + " 更新成功", theSubcontractDis.M_ID, 0, (int)theSubcontractDis.PoNum, (int)theSubcontractDis.Type);



                    sql = " update DiscardReview set StatusCode = " + StatusCode + ", OAReviewDate = '" + OAReviewDate + "', OAReviewer = '" + OAReviewer + "',OAComment = '" + OAComment + "'  where OARequestID = " + OARequestID + "";
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                    SubcontractDisRepository.AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, (int)theSubcontractDis.JobSeq, 201, "StatusCode = " + StatusCode + " 更新成功", theSubcontractDis.M_ID, 0, (int)theSubcontractDis.PoNum, (int)theSubcontractDis.Type);

                }
                else if (StatusCode == 3) //OA同意报废
                {
                    string res = ErpAPI.CommonRepository.RefuseDMRProcessing(theSubcontractDis.Company, theSubcontractDis.Plant, (decimal)discardReview.ReviewQty, discardReview.DR_DMRUnQualifiedReason, (int)theSubcontractDis.DMRID, theSubcontractDis.IUM);
                    if (res.Substring(0, 1).Trim() != "1")
                    {
                        SubcontractDisRepository.AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, (int)theSubcontractDis.JobSeq, 201, res + ". 请重新提交报废数量", theSubcontractDis.M_ID, 0, (int)theSubcontractDis.PoNum, (int)theSubcontractDis.Type);
                        return "错误：" + res + ". 请重新提交报废数量";
                    }


                    SubcontractDisRepository.InsertDiscardRecord((int)discardReview.SubcontractDisMainID, (decimal)discardReview.ReviewQty, discardReview.DR_DMRUnQualifiedReason,
                       (int)theSubcontractDis.DMRID, discardReview.DR_DMRWarehouseCode, discardReview.DR_DMRBinNum, discardReview.DR_TransformUserGroup,
                       discardReview.DR_DMRUnQualifiedReasonRemark, discardReview.DR_ResponsibilityRemark,  discardReview.DR_Responsibility,discardReview.ReviewCreateUserID);
                    SubcontractDisRepository.AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, (int)theSubcontractDis.JobSeq, 201, "报废子流程生成", theSubcontractDis.M_ID, 0, (int)theSubcontractDis.PoNum, (int)theSubcontractDis.Type);


                    sql = " update SubcontractDisMain set  ExistSubProcess = 1, TotalDMRUnQualifiedQty = ISNULL(totalDMRUnQualifiedQty,0) + " + discardReview.ReviewQty + "  where m_id = " + discardReview.SubcontractDisMainID + "";
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                    SubcontractDisRepository.AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, (int)theSubcontractDis.JobSeq, 201, "TotalDMRUnQualifiedQty += " + discardReview.ReviewQty + "  更新成功", theSubcontractDis.M_ID, 0, (int)theSubcontractDis.PoNum, (int)theSubcontractDis.Type);



                    sql = @"select s_id from SubcontractDisSub where RelatedID  = " + discardReview.SubcontractDisMainID + " order by DMRDate desc";
                    object SubcontractDisSubID = Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);


                    sql = " update DiscardReview set SubcontractDisSubID = " + SubcontractDisSubID + ",  StatusCode = " + StatusCode + ", OAReviewDate = '" + OAReviewDate + "', OAReviewer = '" + OAReviewer + "',OAComment = '" + OAComment + "'  where OARequestID = " + OARequestID + "";
                    Common.SQLRepository.ExecuteNonQuery(Common.SQLRepository.APP_strConn, CommandType.Text, sql, null);
                    SubcontractDisRepository.AddOpLog(theSubcontractDis.JobNum, (int)theSubcontractDis.AssemblySeq, (int)theSubcontractDis.JobSeq, 201, "StatusCode = " + StatusCode + " 更新成功", theSubcontractDis.M_ID, 0, (int)theSubcontractDis.PoNum, (int)theSubcontractDis.Type);
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