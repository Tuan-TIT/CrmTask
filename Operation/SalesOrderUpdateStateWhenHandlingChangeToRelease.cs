using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using SalesOrderPlugin.Constant;
using SalesOrderPlugin.Enum;
using System;
using SalesOrderPlugin.Extension;

namespace SalesOrderPlugin.Operation
{
    public class SalesOrderUpdateStateWhenHandlingChangeToRelease : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var trace = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var service = serviceFactory.CreateOrganizationService(context.InitiatingUserId);

            var entity = (Entity)context.InputParameters["Target"];
            trace.Trace("Entity: " + entity.LogicalName);
            if (entity.LogicalName == EntityConstant.SalesOrder)
            {
                if (!entity.Contains("crbf2_handling"))
                {
                    return;
                }

                var handling = entity.GetAttributeValue<OptionSetValue>("crbf2_handling").Value;
                var retrievedEntity = service.Retrieve(entity.LogicalName, entity.Id, new ColumnSet("crbf2_state", "crbf2_handling"));

                if (handling == (int)HandlingEnum.Release)
                {
                    retrievedEntity.SetOptionValue("crbf2_state", (int)StateEnum.Released);
                    retrievedEntity.SetOptionValue("crbf2_handling", (int)HandlingEnum.NoAction);

                    service.Update(retrievedEntity);
                }

                if (handling == (int)HandlingEnum.Cancel)
                {
                    var deliveryOrder = service.Retrieve(EntityConstant.DeliveryOrder, entity.Id, new ColumnSet("crbf2_state", "crbf2_handling"));
                    if (deliveryOrder != null && deliveryOrder.Contains("crbf2_state"))
                    {
                        var state = deliveryOrder.GetAttributeValue<OptionSetValue>("crbf2_state").Value;

                        if (state == (int)StateEnum.Released || state == (int)StateEnum.Open)
                        {
                            throw new InvalidPluginExecutionException("Cannot Cancel SO because already used in DO");
                        }
                    }

                    retrievedEntity.SetOptionValue("crbf2_state", (int)StateEnum.Canceled);
                    retrievedEntity.SetOptionValue("crbf2_handling", (int)HandlingEnum.NoAction);

                    service.Update(retrievedEntity);
                }
            }
        }
    }
}