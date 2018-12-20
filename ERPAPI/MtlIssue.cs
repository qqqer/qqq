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
            string res = CheckIssue(partNum, tranQty);

            if (res.Substring(0, 1).Trim() == "1")
            {
                string [] arr = res.Substring(2).Split('~');
                if (IssueReturnSTKMTLbak(jobNum, assemblySeq, oprSeq, mtlSeq, partNum, tranQty, tranDate, arr[2], "WIP", arr[1], "WIP", arr[1], arr[0], "工单发料", companyId))
                     res = "true";
                else
                    res = "发料出错,请检查erp数据";
            }
                       
            return res;
        }


        public static string CheckIssue(string partNum, decimal tranQty)
        {
            string sql = @"select  [PartBin].[LotNum] as [PartBin_LotNum] ,OnhandQty, BinNum,IUM
                    from Erp.PartBin as PartBin
                    inner join Erp.Warehse as Warehse on PartBin.Company = Warehse.Company and PartBin.WarehouseCode = Warehse.WarehouseCode
                    inner join Erp.Part as Part       on  PartBin.Company = Part.Company and PartBin.PartNum = Part.PartNum
                    where Warehse.WarehouseCode = 'wip' and  PartBin.PartNum = '" + partNum + "' and  not (TrackLots = 1 and LotNum = '')";
            DataTable dt = Common.GetDataByERP(sql);

            if (dt == null || dt.Rows.Count == 0) return "0|wip仓中没有该物料 或 追踪的批次号为空";

            for (int i = 0; i < dt.Rows.Count; i++) //遍历wip仓中，该物料的所有批次
            {
                if (tranQty > Convert.ToDecimal(dt.Rows[i]["OnhandQty"]))
                    continue;
                else
                    return "1|" + dt.Rows[i]["PartBin_LotNum"] + "~" + dt.Rows[i]["BinNum"] + "~" + dt.Rows[i]["IUM"]; 
            }

            return "0|库存不足 或 追踪的批次号为空";
        }
            
    }
}
