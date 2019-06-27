using System;
using Ice.Core;
using Erp.Proxy.BO;
using Erp.BO;
using Epicor.ServiceModel.Channels;

namespace ErpAPI
{

    public class ReqRepository
    {
        public static string ReqRepository()
        {
            Session EpicorSession = CommonRepository.GetEpicorSession();
            if (EpicorSession == null)
            {
                return "0|GetEpicorSession失败，请稍候再试|RepairDMRProcessing";
            }
            try
            {
                //EpicorSession.PlantID = plant;
                //EpicorSession.CompanyID = Company;

                ReqImpl reqImpl = Ice.Lib.Framework.WCFServiceSupport.CreateImpl<ReqImpl>(EpicorSession, ImplBase<Erp.Contracts.ReqSvcContract>.UriPath);

                ReqDataSet reqDataSet = new ReqDataSet();

                reqImpl.GetByID()
            }
            catch
            {

            }
            // This constructor is used when an object is loaded from a persistent storage.
            // Do not place any code here.
        }

    }

}