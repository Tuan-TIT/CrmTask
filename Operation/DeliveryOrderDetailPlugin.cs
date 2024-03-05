using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using SalesOrderPlugin.Constant;
using SalesOrderPlugin.Enum;
using SalesOrderPlugin.Extension;

namespace SalesOrderPlugin.Operation
{
    public class DeliveryOrderDetailPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var serviceFactory =
                (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var trace = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var service = serviceFactory.CreateOrganizationService(context.InitiatingUserId);

            var entity = (Entity)context.InputParameters["Target"];
            trace.Trace("Entity: " + entity.LogicalName);
            if (entity.LogicalName == EntityConstant.DeliveryOrderDetail)
            {
                var handling = entity.GetAttributeValue<OptionSetValue>("handling".ToPrefix()).Value;
                var qtyDelivered = entity.GetAttributeValue<decimal>("qtydelivered".ToPrefix());
                var retrievedEntity = service.Retrieve(entity.LogicalName, entity.Id, new ColumnSet("state".ToPrefix(), "handling".ToPrefix()));
                var salesOrderDetailId = retrievedEntity.GetAttributeValue<EntityReference>("salesorderdetailid".ToPrefix()).Id;

                var salesOrderDetail = service.Retrieve(EntityConstant.SalesOrderDetail, salesOrderDetailId, new ColumnSet("qtydelivered".ToPrefix()));
                if (handling == (int)HandlingEnum.Release)
                {
                    retrievedEntity.SetOptionValue("state".ToPrefix(), (int)StateEnum.Released);
                    retrievedEntity.SetOptionValue("handling".ToPrefix(), (int)HandlingEnum.NoAction);
                    
                    if (salesOrderDetail.Contains("qtydelivered".ToPrefix()))
                    {
                        var qtyDeliveredSalesOrderDetail = salesOrderDetail.GetAttributeValue<decimal>("qtydelivered".ToPrefix());
                        salesOrderDetail["qtydelivered".ToPrefix()] = qtyDeliveredSalesOrderDetail + qtyDelivered;
                        service.Update(salesOrderDetail);
                    }

                    service.Update(retrievedEntity);
                }

                if (handling == (int)HandlingEnum.Cancel)
                {
                    var state = retrievedEntity.GetAttributeValue<OptionSetValue>("state".ToPrefix()).Value;
                    if (state == (int)StateEnum.Released || state == (int)StateEnum.Open)
                    {
                        throw new InvalidPluginExecutionException("Cannot Cancel SO because already used in DO");
                    }

                    if (salesOrderDetail.Contains("qtydelivered".ToPrefix()))
                    {
                        var qtyDeliveredSalesOrderDetail = salesOrderDetail.GetAttributeValue<decimal>("qtydelivered".ToPrefix());
                        salesOrderDetail["qtydelivered".ToPrefix()] = qtyDeliveredSalesOrderDetail - qtyDelivered;
                        service.Update(salesOrderDetail);
                    }

                    retrievedEntity.SetOptionValue("state".ToPrefix(), (int)StateEnum.Canceled);
                    retrievedEntity.SetOptionValue("handling".ToPrefix(), (int)HandlingEnum.NoAction);

                    service.Update(retrievedEntity);
                }
            }
        }
    }
}