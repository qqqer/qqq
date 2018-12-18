using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using Newtonsoft.Json;
using System.Configuration;


using Ice.Core;
using Erp.Proxy.BO;
using Erp.BO;
using Epicor.ServiceModel.Channels;
using Ice.Tablesets;
using System.Web;

namespace ErpAPI
{
    public static class MtlIssue
    {
        //工单发料
        private static string OneIssueReturnSTKMTL(string jobNum, int assemblySeq, int oprSeq, int mtlSeq, string partNum, decimal tranQty, DateTime tranDate, string lotNum, string companyId)
        {
            string tranReference = "工单发料";
            string querysql = "";
            string tracklots = Common.QueryERP("select tracklots from Erp.Part where Company='" + companyId + "' and PartNum='" + partNum + "'");
            if (tracklots.ToLower() == "true" && !string.IsNullOrEmpty(lotNum)) // 有批次追踪 且 有批次号
            {
                querysql = "select jm.JobNum,jm.AssemblySeq,jm.RelatedOperation,jm.MtlSeq,jm.PartNum,jm.RequiredQty-jm.IssuedQty Qty,jm.IUM,jm.WarehouseCode,(select top(1) BinNum from Erp.PartBin where Company=jm.Company and WarehouseCode=jm.WarehouseCode and PartNum=jm.PartNum and LotNum='" + lotNum + "') BinNum,rg.InputWhse,rg.InputBinNum,isnull((select top(1) OnhandQty from Erp.PartBin where Company=jm.Company and WarehouseCode=jm.WarehouseCode and PartNum=jm.PartNum and LotNum='" + lotNum + "'),0) OnhandQty from Erp.JobMtl jm inner join Part p on jm.Company=p.Company and jm.PartNum=p.PartNum and p.TrackLots=1 inner join erp.JobOpDtl jod on jm.Company=jod.Company and jm.JobNum=jod.JobNum and jm.AssemblySeq=jod.AssemblySeq and jm.RelatedOperation=jod.OprSeq inner join erp.ResourceGroup rg on jod.Company=rg.Company and jod.ResourceGrpID=rg.ResourceGrpID where jm.Company='" + companyId + "' and jm.JobNum='" + jobNum + "' and jm.AssemblySeq='" + assemblySeq + "' and jm.RelatedOperation='" + oprSeq + "' and jm.PartNum='" + partNum + "' and WarehouseCode='wip'";
            }
            else if (tracklots.ToLower() == "true" && string.IsNullOrEmpty(lotNum))// 有批次追踪 且 没批次号
            {
                return "false|该物料有批次追踪,但没有批次号.";

            }
            else if (tracklots.ToLower() == "false" && !string.IsNullOrEmpty(lotNum))// 无批次追踪 且 有批次号
            {
                querysql = "select jm.JobNum,jm.AssemblySeq,jm.RelatedOperation,jm.MtlSeq,jm.PartNum,jm.RequiredQty-jm.IssuedQty Qty,jm.IUM,jm.WarehouseCode,(select top(1) BinNum from Erp.PartBin where Company=jm.Company and WarehouseCode=jm.WarehouseCode and PartNum=jm.PartNum and LotNum='" + lotNum + "') BinNum,rg.InputWhse,rg.InputBinNum,isnull((select top(1) OnhandQty from Erp.PartBin where Company=jm.Company and WarehouseCode=jm.WarehouseCode and PartNum=jm.PartNum and LotNum='" + lotNum + "'),0) OnhandQty from Erp.JobMtl jm inner join Part p on jm.Company=p.Company and jm.PartNum=p.PartNum inner join erp.JobOpDtl jod on jm.Company=jod.Company and jm.JobNum=jod.JobNum and jm.AssemblySeq=jod.AssemblySeq and jm.RelatedOperation=jod.OprSeq inner join erp.ResourceGroup rg on jod.Company=rg.Company and jod.ResourceGrpID=rg.ResourceGrpID where jm.Company='" + companyId + "' and jm.JobNum='" + jobNum + "' and jm.AssemblySeq='" + assemblySeq + "' and jm.RelatedOperation='" + oprSeq + "' and jm.PartNum='" + partNum + "' and WarehouseCode='wip'";
            }
            else//都没
            {
                querysql = "select jm.JobNum,jm.AssemblySeq,jm.RelatedOperation,jm.MtlSeq,jm.PartNum,jm.RequiredQty-jm.IssuedQty Qty,jm.IUM,jm.WarehouseCode,(select top(1) BinNum from Erp.PartBin where Company=jm.Company and WarehouseCode=jm.WarehouseCode and PartNum=jm.PartNum) BinNum,rg.InputWhse,rg.InputBinNum,isnull((select top(1) OnhandQty from Erp.PartBin where Company=jm.Company and WarehouseCode=jm.WarehouseCode and PartNum=jm.PartNum),0) OnhandQty from Erp.JobMtl jm inner join Part p on jm.Company=p.Company and jm.PartNum=p.PartNum inner join erp.JobOpDtl jod on jm.Company=jod.Company and jm.JobNum=jod.JobNum and jm.AssemblySeq=jod.AssemblySeq and jm.RelatedOperation=jod.OprSeq inner join erp.ResourceGroup rg on jod.Company=rg.Company and jod.ResourceGrpID=rg.ResourceGrpID where jm.Company='" + companyId + "' and jm.JobNum='" + jobNum + "' and jm.AssemblySeq='" + assemblySeq + "' and jm.RelatedOperation='" + oprSeq + "' and jm.PartNum='" + partNum + "'";
            }
            mtlSeq = Convert.ToInt32(Common.QueryERP("select MtlSeq from Erp.JobMtl jm where jm.Company='" + companyId + "' and jm.JobNum='" + jobNum + "' and jm.AssemblySeq='" + assemblySeq + "' and jm.RelatedOperation='" + oprSeq + "' and jm.PartNum='" + partNum + "' "));
            DataTable dt = Common.GetDataByERP(querysql);

            if (dt != null && dt.Rows.Count > 0)
            {
                if (tranQty > Convert.ToDecimal(dt.Rows[0]["OnhandQty"]))
                {
                    return "false|发料数量大于库存数量.";
                }
                else
                {
                    bool index = IssueReturnSTKMTLbak(jobNum, assemblySeq, oprSeq, mtlSeq, partNum, tranQty, tranDate, dt.Rows[0]["IUM"].ToString(), dt.Rows[0]["WarehouseCode"].ToString(), dt.Rows[0]["BinNum"].ToString(), dt.Rows[0]["InputWhse"].ToString(), dt.Rows[0]["InputBinNum"].ToString(), lotNum, tranReference, companyId);
                    if (index)
                    {
                        return "true";
                    }
                    else
                    {
                        return "false|发料出错,请检查erp数据.";
                    }
                }
            }
            else
            {
                return "false|查询物料无库存或信息出错,请检查erp数据.";
            }
        }


        private static bool IssueReturnSTKMTLbak(string jobNum, int assemblySeq, int oprSeq, int mtlSeq, string partNum, decimal tranQty, DateTime tranDate, string ium, string fromWarehouseCode, string fromBinNum, string toWarehouseCode, string toBinNum, string lotNum, string tranReference, string companyId)
        {
            try
            {
                Session EpicorSession = Common.GetEpicorSession();
                if (EpicorSession == null)
                {
                    return false;
                }
                EpicorSession.CompanyID = companyId;
                IssueReturnImpl adapter = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<IssueReturnImpl>(EpicorSession, ImplBase<Erp.Contracts.IssueReturnSvcContract>.UriPath);
                IssueReturnDataSet ds = new IssueReturnDataSet();
                SelectedJobAsmblDataSet jads = new SelectedJobAsmblDataSet();
                string pcTranType;
                string pCallProcess;
                pcTranType = "STK-MTL";
                pCallProcess = "IssueMaterial";
                System.String pcMessage = "";
                System.String parttranpks = "";
                bool result;
                Guid pcMtlQueueRowid = new Guid();
                adapter.GetNewJobAsmblMultiple(pcTranType, pcMtlQueueRowid, pCallProcess, jads, out pcMessage);
                adapter.GetNewIssueReturnToJob(jobNum, assemblySeq, pcTranType, new Guid(), out pcMessage, ds);
                adapter.GetNewJobAsmblMultiple(pcTranType, pcMtlQueueRowid, pCallProcess, jads, out pcMessage);
                adapter.OnChangingToJobSeq(mtlSeq, ds);
                ds.Tables["IssueReturn"].Rows[0]["ToJobSeq"] = mtlSeq;
                adapter.OnChangeToJobSeq(ds, pCallProcess, out pcMessage);
                ds.Tables["IssueReturn"].Rows[0]["TranQty"] = tranQty;
                adapter.OnChangeTranQty(tranQty, ds);
                string pUM = ium;
                ds.Tables["IssueReturn"].Rows[0]["UM"] = ium;
                adapter.OnChangeUM(pUM, ds);
                ds.Tables["IssueReturn"].Rows[0]["TranDate"] = tranDate.ToString("yyyy-MM-dd");
                ds.Tables["IssueReturn"].Rows[0]["FromWarehouseCode"] = fromWarehouseCode;
                ds.Tables["IssueReturn"].Rows[0]["FromBinNum"] = fromBinNum;
                ds.Tables["IssueReturn"].Rows[0]["ToWarehouseCode"] = toWarehouseCode;
                ds.Tables["IssueReturn"].Rows[0]["ToBinNum"] = toBinNum;
                ds.Tables["IssueReturn"].Rows[0]["LotNum"] = lotNum;
                ds.Tables["IssueReturn"].Rows[0]["TranReference"] = tranReference;
                bool plNegQtyAction = true;
                System.String legalNumberMessage = "";
                string partTranPKs = "";
                //Call Adapter method
                adapter.PrePerformMaterialMovement(ds, out plNegQtyAction);
                //来源仓
                //adapterIssueReturn.NegativeInventoryTest(partNum, fromWarehouseCode, fromBinNum, "", ium, 1, 1, out legalNumberMessage, out partTranPKs);
                string pcNeqQtyAction = "";
                string pcNeqQtyMessage = "";
                string pcPCBinAction = "";
                string pcPCBinMessage = "";
                string pcOutBinAction = "";
                string pcOutBinMessage = "";
                adapter.MasterInventoryBinTests(ds, out pcNeqQtyAction, out pcNeqQtyMessage, out pcPCBinAction, out pcPCBinMessage, out pcOutBinAction, out pcOutBinMessage);
                if (!string.IsNullOrEmpty(pcNeqQtyMessage) || (!string.IsNullOrEmpty(pcNeqQtyAction) && pcNeqQtyAction != "None"))
                {
                    string s = (pcNeqQtyMessage);
                    string message = "工单：" + jobNum + "/" + assemblySeq + "/" + oprSeq + ",扣料时系统报错。物料：" + partNum + ",来源仓:" + fromWarehouseCode + "/" + fromBinNum + ",目标仓:" + toWarehouseCode + "/" + toBinNum + ",批次:" + lotNum + ",数量:" + tranQty + ".原因:" + s;
                    //WriteTxt(message);
                    return false;
                }
                adapter.PerformMaterialMovement(true, ds, out legalNumberMessage, out partTranPKs);
                adapter.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                string message = "工单：" + jobNum + "/" + assemblySeq + "/" + oprSeq + ",扣料时系统报错。物料：" + partNum + ",来源仓:" + fromWarehouseCode + "/" + fromBinNum + ",目标仓:" + toWarehouseCode + "/" + toBinNum + ",批次:" + lotNum + ",数量:" + tranQty + ".原因:" + ex.Message;
                //WriteTxt(message);
                return false;
            }
        }


        public static string Issue(string jobNum, int assemblySeq, int oprSeq, int mtlSeq, string partNum, decimal tranQty, DateTime tranDate, string companyId)
        {
            string sql = @"select  [PartBin].[LotNum] as [PartBin_LotNum] 
                    from Erp.PartBin as PartBin
                    inner join Erp.Warehse as Warehse on PartBin.Company = Warehse.Company and PartBin.WarehouseCode = Warehse.WarehouseCode
                    inner join Erp.Part as Part       on  PartBin.Company = Part.Company and PartBin.PartNum = Part.PartNum
                    where Warehse.WarehouseCode = 'wip' ";
            DataTable dt = Common.GetDataByERP(sql);

            string res= "false|wip仓中没有该物料";

            for (int i = 0; i < dt.Rows.Count; i++) //遍历wip仓中，该物料的所有批次
            {
                res = OneIssueReturnSTKMTL(jobNum, assemblySeq, oprSeq, mtlSeq, partNum, tranQty, tranDate, dt.Rows[i]["PartBin_LotNum"].ToString(), companyId);
                if (res == "true")
                    break;
            }
            return res;
        }


        public static string CheckIssue(string jobNum, int assemblySeq, int oprSeq, int mtlSeq, string partNum, decimal tranQty, DateTime tranDate, string companyId)
        {
            string sql = @"select  [PartBin].[LotNum] as [PartBin_LotNum] 
                    from Erp.PartBin as PartBin
                    inner join Erp.Warehse as Warehse on PartBin.Company = Warehse.Company and PartBin.WarehouseCode = Warehse.WarehouseCode
                    inner join Erp.Part as Part       on  PartBin.Company = Part.Company and PartBin.PartNum = Part.PartNum
                    where Warehse.WarehouseCode = 'wip' and [PartBin].[LotNum] != '' and tracklots != 'false' ";
            DataTable dt = Common.GetDataByERP(sql);

            if (dt != null || dt.Rows.Count == 0)return  "false|wip仓中没有该物料";

            for (int i = 0; i < dt.Rows.Count; i++) //遍历wip仓中，该物料的所有批次
            {
                string querysql;
                string tracklots = Common.QueryERP("select tracklots from Erp.Part where Company='" + companyId + "' and PartNum='" + partNum + "'");
                if (tracklots.ToLower() == "true" && !string.IsNullOrEmpty(dt.Rows[i]["PartBin_LotNum"].ToString())) // 有批次追踪 且 有批次号
                {
                    querysql = "select jm.JobNum,jm.AssemblySeq,jm.RelatedOperation,jm.MtlSeq,jm.PartNum,jm.RequiredQty-jm.IssuedQty Qty,jm.IUM,jm.WarehouseCode,(select top(1) BinNum from Erp.PartBin where Company=jm.Company and WarehouseCode=jm.WarehouseCode and PartNum=jm.PartNum and LotNum='" + dt.Rows[i]["PartBin_LotNum"].ToString() + "') BinNum,rg.InputWhse,rg.InputBinNum,isnull((select top(1) OnhandQty from Erp.PartBin where Company=jm.Company and WarehouseCode=jm.WarehouseCode and PartNum=jm.PartNum and LotNum='" + dt.Rows[i]["PartBin_LotNum"].ToString() + "'),0) OnhandQty from Erp.JobMtl jm inner join Part p on jm.Company=p.Company and jm.PartNum=p.PartNum and p.TrackLots=1 inner join erp.JobOpDtl jod on jm.Company=jod.Company and jm.JobNum=jod.JobNum and jm.AssemblySeq=jod.AssemblySeq and jm.RelatedOperation=jod.OprSeq inner join erp.ResourceGroup rg on jod.Company=rg.Company and jod.ResourceGrpID=rg.ResourceGrpID where jm.Company='" + companyId + "' and jm.JobNum='" + jobNum + "' and jm.AssemblySeq='" + assemblySeq + "' and jm.RelatedOperation='" + oprSeq + "' and jm.PartNum='" + partNum + "' and WarehouseCode='wip'";
                }
                else if (tracklots.ToLower() == "true" && string.IsNullOrEmpty(dt.Rows[i]["PartBin_LotNum"].ToString()))// 有批次追踪 且 没批次号
                {
                    return "false|该物料有批次追踪,但没有批次号.";

                }
                else if (tracklots.ToLower() == "false" && !string.IsNullOrEmpty(dt.Rows[i]["PartBin_LotNum"].ToString()))// 无批次追踪 且 有批次号
                {
                    querysql = "select jm.JobNum,jm.AssemblySeq,jm.RelatedOperation,jm.MtlSeq,jm.PartNum,jm.RequiredQty-jm.IssuedQty Qty,jm.IUM,jm.WarehouseCode,(select top(1) BinNum from Erp.PartBin where Company=jm.Company and WarehouseCode=jm.WarehouseCode and PartNum=jm.PartNum and LotNum='" + dt.Rows[i]["PartBin_LotNum"].ToString() + "') BinNum,rg.InputWhse,rg.InputBinNum,isnull((select top(1) OnhandQty from Erp.PartBin where Company=jm.Company and WarehouseCode=jm.WarehouseCode and PartNum=jm.PartNum and LotNum='" + dt.Rows[i]["PartBin_LotNum"].ToString() + "'),0) OnhandQty from Erp.JobMtl jm inner join Part p on jm.Company=p.Company and jm.PartNum=p.PartNum inner join erp.JobOpDtl jod on jm.Company=jod.Company and jm.JobNum=jod.JobNum and jm.AssemblySeq=jod.AssemblySeq and jm.RelatedOperation=jod.OprSeq inner join erp.ResourceGroup rg on jod.Company=rg.Company and jod.ResourceGrpID=rg.ResourceGrpID where jm.Company='" + companyId + "' and jm.JobNum='" + jobNum + "' and jm.AssemblySeq='" + assemblySeq + "' and jm.RelatedOperation='" + oprSeq + "' and jm.PartNum='" + partNum + "' and WarehouseCode='wip'";
                }
                else//都没
                {
                    querysql = "select jm.JobNum,jm.AssemblySeq,jm.RelatedOperation,jm.MtlSeq,jm.PartNum,jm.RequiredQty-jm.IssuedQty Qty,jm.IUM,jm.WarehouseCode,(select top(1) BinNum from Erp.PartBin where Company=jm.Company and WarehouseCode=jm.WarehouseCode and PartNum=jm.PartNum) BinNum,rg.InputWhse,rg.InputBinNum,isnull((select top(1) OnhandQty from Erp.PartBin where Company=jm.Company and WarehouseCode=jm.WarehouseCode and PartNum=jm.PartNum),0) OnhandQty from Erp.JobMtl jm inner join Part p on jm.Company=p.Company and jm.PartNum=p.PartNum inner join erp.JobOpDtl jod on jm.Company=jod.Company and jm.JobNum=jod.JobNum and jm.AssemblySeq=jod.AssemblySeq and jm.RelatedOperation=jod.OprSeq inner join erp.ResourceGroup rg on jod.Company=rg.Company and jod.ResourceGrpID=rg.ResourceGrpID where jm.Company='" + companyId + "' and jm.JobNum='" + jobNum + "' and jm.AssemblySeq='" + assemblySeq + "' and jm.RelatedOperation='" + oprSeq + "' and jm.PartNum='" + partNum + "'";
                }
                DataTable dt2 = Common.GetDataByERP(querysql);

                if (dt2 != null && dt2.Rows.Count > 0)
                {
                    if (tranQty > Convert.ToDecimal(dt2.Rows[0]["OnhandQty"]))
                    {
                        return "false|发料数量大于库存数量.";
                    }
                }
                else
                {
                    return "false|查询物料无库存或信息出错,请检查erp数据.";
                }
            }
            return "true";
        }

    }
}
