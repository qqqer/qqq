using System;
using System.Collections.Generic;
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

                                <weaver.workflow.webservices.WorkflowRequestTableField>
                                    <fieldName>remark</fieldName>
                                    <isView>true</isView>
                                    <isEdit>true</isEdit>
                                    <fieldValue>{15}</fieldValue>
                                </weaver.workflow.webservices.WorkflowRequestTableField>  
                                </workflowRequestTableFields>
                            </weaver.workflow.webservices.WorkflowRequestTableRecord>
                        </requestRecords>
                    </workflowMainTableInfo>
                </WorkflowRequestInfo>";

            u = string.Format(u,users, receipt.Company, receipt.SupplierNo, receipt.SupplierName, receipt.PoNum,
                receipt.PoLine, receipt.PORelNum, receipt.PartNum, receipt.PartDesc, receipt.BatchNo, 
                ((decimal)(receipt.ReceiveQty2)).ToString("N2"), receipt.Plant,receipt.ID, users,receipt.CommentText,receipt.RequestIDD);

            return u;
        }


        public static string Create2188XML(string jobnum, int AssemblySeq, int JobSeq, string OpCode, string OpDesc, decimal DMRRepairQty,
        string plant, string DMRJobNum, string CheckUserid, string CheckDate, string UnQualifiedType, string Responsibility, string DefectNO,
        string DMRUnQualifiedReasonRemark, string DMRUnQualifiedReasonDesc, string ResponsibilityRemark)
        {
            string u = @"
                <WorkflowRequestInfo>
                    <creatorId>1012</creatorId>
                    <requestName>不良品返修</requestName>     
            
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
                                </workflowRequestTableFields>
                            </weaver.workflow.webservices.WorkflowRequestTableRecord>
                        </requestRecords>
                    </workflowMainTableInfo>
                </WorkflowRequestInfo>";

            u = string.Format(u, jobnum, AssemblySeq, JobSeq,  OpCode,  OpDesc, DMRRepairQty,
         plant,  DMRJobNum,  CheckUserid,  CheckDate,  UnQualifiedType,  Responsibility,  DefectNO,
         DMRUnQualifiedReasonRemark,  DMRUnQualifiedReasonDesc,  ResponsibilityRemark, CommonRepository.GetUserName(CheckUserid));

            return u;
        }

    }
}