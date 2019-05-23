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
            string u = @"
                <WorkflowRequestInfo>
                    <creatorId>1012</creatorId>
                    <requestName>收料测试</requestName>     
            
                    <workflowBaseInfo>
                        <workflowId>2162</workflowId>
                    </workflowBaseInfo>

                    <workflowMainTableInfo>
                        <requestRecords>
                            <weaver.workflow.webservices.WorkflowRequestTableRecord>   
                                <workflowRequestTableFields>
                                <weaver.workflow.webservices.WorkflowRequestTableField>     
                                    <fieldName>ReceivePart</fieldName>
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
                                </workflowRequestTableFields>
                            </weaver.workflow.webservices.WorkflowRequestTableRecord>
                        </requestRecords>
                    </workflowMainTableInfo>
                </WorkflowRequestInfo>";

            u = string.Format(receipt.DepartmentUKN, receipt.Company, receipt.SupplierNo, receipt.SupplierName, receipt.PoNum,
                receipt.PoLine, receipt.PORelNum, receipt.PartNum, receipt.PartDesc, receipt.BatchNo, receipt.ReceiveQty2, receipt.Plant,receipt.ID);

            return u;
        }
    }
}