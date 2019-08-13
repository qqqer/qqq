using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace Appapi.Models
{
    public static class OA_XML_Template
    {
        public static string Create2162XML(Receipt receipt)
        {
            string users = receipt.FourthUserGroup;
            users = users.Substring(0, users.Length-1);

            string u = @"
                <WorkflowRequestInfo>
                    <creatorId>1012</creatorId>
                    <requestName>临时物料</requestName>     
            
                    <workflowBaseInfo>
                        <workflowId>2162</workflowId>
                    </workflowBaseInfo>

                    <workflowMainTableInfo>
                        <requestRecords>
                            <weaver.workflow.webservices.WorkflowRequestTableRecord>   
                                <workflowRequestTableFields>
                                <weaver.workflow.webservices.WorkflowRequestTableField>     
                                    <fieldName>Receivers</fieldName>
                                    <fieldValue>{0}</fieldValue>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                </weaver.workflow.webservices.WorkflowRequestTableField>

                                <weaver.workflow.webservices.WorkflowRequestTableField>     
                                    <fieldName>Company</fieldName>
                                    <fieldValue>{1}</fieldValue>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                </weaver.workflow.webservices.WorkflowRequestTableField>

                                <weaver.workflow.webservices.WorkflowRequestTableField>     
                                    <fieldName>SupplierNo</fieldName>
                                    <fieldValue>{2}</fieldValue>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                </weaver.workflow.webservices.WorkflowRequestTableField>

                                <weaver.workflow.webservices.WorkflowRequestTableField>     
                                    <fieldName>SupplierName</fieldName>
                                    <fieldValue>{3}</fieldValue>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                </weaver.workflow.webservices.WorkflowRequestTableField>

                                <weaver.workflow.webservices.WorkflowRequestTableField>     
                                    <fieldName>PoNum</fieldName>
                                    <fieldValue>{4}</fieldValue>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                </weaver.workflow.webservices.WorkflowRequestTableField>

                                <weaver.workflow.webservices.WorkflowRequestTableField>     
                                    <fieldName>PoLine</fieldName>
                                    <fieldValue>{5}</fieldValue>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                </weaver.workflow.webservices.WorkflowRequestTableField>

                                <weaver.workflow.webservices.WorkflowRequestTableField>     
                                    <fieldName>PORelNum</fieldName>
                                    <fieldValue>{6}</fieldValue>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                </weaver.workflow.webservices.WorkflowRequestTableField>

                                <weaver.workflow.webservices.WorkflowRequestTableField>     
                                    <fieldName>PartNum</fieldName>
                                    <fieldValue>{7}</fieldValue>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                </weaver.workflow.webservices.WorkflowRequestTableField>

                                <weaver.workflow.webservices.WorkflowRequestTableField>     
                                    <fieldName>PartDesc</fieldName>
                                    <fieldValue>{8}</fieldValue>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                </weaver.workflow.webservices.WorkflowRequestTableField>

                                    <weaver.workflow.webservices.WorkflowRequestTableField>     
                                    <fieldName>BatchNo</fieldName>
                                    <fieldValue>{9}</fieldValue>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                </weaver.workflow.webservices.WorkflowRequestTableField>

                                <weaver.workflow.webservices.WorkflowRequestTableField>     
                                    <fieldName>ReceiveQty</fieldName>
                                    <fieldValue>{10}</fieldValue>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                </weaver.workflow.webservices.WorkflowRequestTableField>

                                <weaver.workflow.webservices.WorkflowRequestTableField>     
                                    <fieldName>Plant</fieldName>
                                    <fieldValue>{11}</fieldValue>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                </weaver.workflow.webservices.WorkflowRequestTableField>

					            <weaver.workflow.webservices.WorkflowRequestTableField>
                                    <fieldName>idd</fieldName>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                    <fieldValue>{12}</fieldValue>
                                </weaver.workflow.webservices.WorkflowRequestTableField>

                                <weaver.workflow.webservices.WorkflowRequestTableField>
                                    <fieldName>zjr</fieldName>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                    <fieldValue>{13}</fieldValue>
                                </weaver.workflow.webservices.WorkflowRequestTableField>  

                                <weaver.workflow.webservices.WorkflowRequestTableField>
                                    <fieldName>remark</fieldName>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                    <fieldValue>{14}</fieldValue>
                                </weaver.workflow.webservices.WorkflowRequestTableField>  

                                
                                </workflowRequestTableFields>
                            </weaver.workflow.webservices.WorkflowRequestTableRecord>
                        </requestRecords>
                    </workflowMainTableInfo>
                </WorkflowRequestInfo>";

            u = string.Format(u,users, receipt.Company, receipt.SupplierNo, receipt.SupplierName, receipt.PoNum,
                receipt.PoLine, receipt.PORelNum, receipt.PartNum, receipt.PartDesc, receipt.BatchNo, 
                ((decimal)(receipt.ReceiveQty2)).ToString("N2"), receipt.Plant,receipt.ID, users, System.Security.SecurityElement.Escape(receipt.CommentText));

            return u;
        }


        public static string Create2188XML(string jobnum, int AssemblySeq, int JobSeq, string OpCode, string OpDesc, decimal DMRRepairQty,
        string plant, string DMRJobNum, string CheckUserid, string CheckDate, string UnQualifiedType, string Responsibility, string DefectNO,
        string DMRUnQualifiedReasonRemark, string DMRUnQualifiedReasonDesc, string ResponsibilityRemark, string PartNum, string PartDesc, string RelatedOprInfo)
        {
            string sql = "select  plantid  from uf_cust_planter where custid = '" + PartDesc.Substring(0, 4) + "'";
            string planner = CommonRepository.GetValueAsString(Common.SQLRepository.ExecuteScalarToObject(Common.SQLRepository.OA_strConn, CommandType.Text, sql, null));


            string u = @"
                <WorkflowRequestInfo>
                    <creatorId>1012</creatorId>
                    <requestName>不良品返工</requestName>     
            
                    <workflowBaseInfo>
                        <workflowId>2188</workflowId>
                    </workflowBaseInfo>

                    <workflowMainTableInfo>
                        <requestRecords>
                            <weaver.workflow.webservices.WorkflowRequestTableRecord>   
                                <workflowRequestTableFields>
                                <weaver.workflow.webservices.WorkflowRequestTableField>     
                                    <fieldName>jobnum</fieldName>
                                    <fieldValue>{0}</fieldValue>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                </weaver.workflow.webservices.WorkflowRequestTableField>

                                <weaver.workflow.webservices.WorkflowRequestTableField>     
                                    <fieldName>AssemblySeq</fieldName>
                                    <fieldValue>{1}</fieldValue>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                </weaver.workflow.webservices.WorkflowRequestTableField>

                                <weaver.workflow.webservices.WorkflowRequestTableField>     
                                    <fieldName>JobSeq</fieldName>
                                    <fieldValue>{2}</fieldValue>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                </weaver.workflow.webservices.WorkflowRequestTableField>

                                <weaver.workflow.webservices.WorkflowRequestTableField>     
                                    <fieldName>OpCode</fieldName>
                                    <fieldValue>{3}</fieldValue>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                </weaver.workflow.webservices.WorkflowRequestTableField>

                                <weaver.workflow.webservices.WorkflowRequestTableField>     
                                    <fieldName>OpDesc</fieldName>
                                    <fieldValue>{4}</fieldValue>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                </weaver.workflow.webservices.WorkflowRequestTableField>

                                <weaver.workflow.webservices.WorkflowRequestTableField>     
                                    <fieldName>DMRRepairQty</fieldName>
                                    <fieldValue>{5}</fieldValue>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                </weaver.workflow.webservices.WorkflowRequestTableField>

                                <weaver.workflow.webservices.WorkflowRequestTableField>     
                                    <fieldName>plant</fieldName>
                                    <fieldValue>{6}</fieldValue>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                </weaver.workflow.webservices.WorkflowRequestTableField>

                                <weaver.workflow.webservices.WorkflowRequestTableField>     
                                    <fieldName>DMRJobNum</fieldName>
                                    <fieldValue>{7}</fieldValue>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                </weaver.workflow.webservices.WorkflowRequestTableField>

                                <weaver.workflow.webservices.WorkflowRequestTableField>     
                                    <fieldName>CheckUserid</fieldName>
                                    <fieldValue>{8}</fieldValue>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                </weaver.workflow.webservices.WorkflowRequestTableField>

                                    <weaver.workflow.webservices.WorkflowRequestTableField>     
                                    <fieldName>CheckDate</fieldName>
                                    <fieldValue>{9}</fieldValue>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                </weaver.workflow.webservices.WorkflowRequestTableField>

                                <weaver.workflow.webservices.WorkflowRequestTableField>     
                                    <fieldName>UnQualifiedType</fieldName>
                                    <fieldValue>{10}</fieldValue>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                </weaver.workflow.webservices.WorkflowRequestTableField>

                                <weaver.workflow.webservices.WorkflowRequestTableField>     
                                    <fieldName>Responsibility</fieldName>
                                    <fieldValue>{11}</fieldValue>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                </weaver.workflow.webservices.WorkflowRequestTableField>

					            <weaver.workflow.webservices.WorkflowRequestTableField>
                                    <fieldName>DefectNO</fieldName>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                    <fieldValue>{12}</fieldValue>
                                </weaver.workflow.webservices.WorkflowRequestTableField>

                                <weaver.workflow.webservices.WorkflowRequestTableField>
                                    <fieldName>DMRUnQualifiedReasonRemark</fieldName>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                    <fieldValue>{13}</fieldValue>
                                </weaver.workflow.webservices.WorkflowRequestTableField>  

                                <weaver.workflow.webservices.WorkflowRequestTableField>
                                    <fieldName>DMRUnQualifiedReasonDesc</fieldName>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                    <fieldValue>{14}</fieldValue>
                                </weaver.workflow.webservices.WorkflowRequestTableField>  

                                <weaver.workflow.webservices.WorkflowRequestTableField>
                                    <fieldName>ResponsibilityRemark</fieldName>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                    <fieldValue>{15}</fieldValue>
                                </weaver.workflow.webservices.WorkflowRequestTableField>  

                                <weaver.workflow.webservices.WorkflowRequestTableField>
                                    <fieldName>CheckUser</fieldName>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                    <fieldValue>{16}</fieldValue>
                                </weaver.workflow.webservices.WorkflowRequestTableField>  

                                <weaver.workflow.webservices.WorkflowRequestTableField>
                                    <fieldName>partcode</fieldName>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                    <fieldValue>{17}</fieldValue>
                                </weaver.workflow.webservices.WorkflowRequestTableField>  

                                <weaver.workflow.webservices.WorkflowRequestTableField>
                                    <fieldName>partdesc</fieldName>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                    <fieldValue>{18}</fieldValue>
                                </weaver.workflow.webservices.WorkflowRequestTableField>  

                                <weaver.workflow.webservices.WorkflowRequestTableField>
                                    <fieldName>custid</fieldName>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                    <fieldValue>{19}</fieldValue>
                                </weaver.workflow.webservices.WorkflowRequestTableField>  

                                <weaver.workflow.webservices.WorkflowRequestTableField>
                                    <fieldName>plantid</fieldName>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                    <fieldValue>{20}</fieldValue>
                                </weaver.workflow.webservices.WorkflowRequestTableField>  

                                <weaver.workflow.webservices.WorkflowRequestTableField>
                                    <fieldName>RelatedOprInfo</fieldName>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                    <fieldValue>{21}</fieldValue>
                                </weaver.workflow.webservices.WorkflowRequestTableField>  

                                </workflowRequestTableFields>
                            </weaver.workflow.webservices.WorkflowRequestTableRecord>
                        </requestRecords>
                    </workflowMainTableInfo>
                </WorkflowRequestInfo>";

            u = string.Format(u, jobnum, AssemblySeq, JobSeq,  OpCode, System.Security.SecurityElement.Escape(OpDesc), DMRRepairQty,
         plant,  DMRJobNum,  CheckUserid,  CheckDate,  UnQualifiedType,  Responsibility,  DefectNO,
         System.Security.SecurityElement.Escape(DMRUnQualifiedReasonRemark),  DMRUnQualifiedReasonDesc,  ResponsibilityRemark, CommonRepository.GetUserName(CheckUserid),PartNum, System.Security.SecurityElement.Escape(PartDesc),PartDesc.Substring(0,4),planner,RelatedOprInfo);

            return u;
        }

    }
}