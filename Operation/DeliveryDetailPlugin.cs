using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow.Activities;
using SalesOrderPlugin.Constant;
using SalesOrderPlugin.Enum;
using SalesOrderPlugin.Extension;

namespace SalesOrderPlugin.Operation
{
    public class DeliveryDetailPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var trace = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var service = serviceFactory.CreateOrganizationService(context.InitiatingUserId);

            if (context.InputParameters == null)
            {
                trace.Trace("context null: ");
                return;
            }

            var entity = (Entity)context.InputParameters["Target"];
            if (entity == null)
            {
                trace.Trace("Entity null: ");
                return;
            }
            trace.Trace("Entity: " + entity.LogicalName);

            if (entity.LogicalName == EntityConstant.DeliveryOrder)
            {
                trace.Trace("Delivery: " + entity.ToJson());
                if (entity.Contains("handling"))
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

                if (entity.Contains("salesorderid".ToPrefix()))
                {
                    var salesOrder = entity.GetAttributeValue<EntityReference>("salesorderid".ToPrefix());
                    if (salesOrder != null)
                    {
                        trace.Trace("Sales Order: " + salesOrder.ToJson());
                        var deliveryOrderDetails = service.RetrieveMultiple(new QueryExpression(EntityConstant.DeliveryOrderDetail)
                        {
                            Criteria = new FilterExpression
                            {
                                Conditions =
                                {
                                        new ConditionExpression("deliveryorderid".ToPrefix(), ConditionOperator.Equal, entity.Id)
                                    }
                            }
                        });

                        if (deliveryOrderDetails != null && deliveryOrderDetails.Entities.Count > 0)
                        {
                            foreach (var entityItem in deliveryOrderDetails.Entities)
                            {
                                service.Delete(EntityConstant.DeliveryOrderDetail, entityItem.Id);
                            }
                        }

                        var salesOrderDetails = service.RetrieveMultiple(new QueryExpression(EntityConstant.SalesOrderDetail)
                        {
                            Criteria = new FilterExpression
                            {
                                Conditions =
                                    {
                                        new ConditionExpression("salesorderid".ToPrefix(), ConditionOperator.Equal, salesOrder.Id)
                                    }
                            },
                            ColumnSet = new ColumnSet("salesorderdetailid".ToPrefix(), "productid".ToPrefix(), "qtysale".ToPrefix())
                        });

                        if (salesOrderDetails != null && salesOrderDetails.Entities.Count > 0)
                        {
                            foreach (var entityItem in salesOrderDetails.Entities)
                            {
                                var deliveryOrderDetail = new Entity(EntityConstant.DeliveryOrderDetail);

                                deliveryOrderDetail["deliveryorderdetailno".ToPrefix()] = Guid.NewGuid().ToString();
                                deliveryOrderDetail["deliveryorderid".ToPrefix()] = new EntityReference(EntityConstant.DeliveryOrder, entity.Id);
                                var salesOrderDetail = new EntityReference(EntityConstant.SalesOrderDetail, entityItem.Id);
                                deliveryOrderDetail["salesorderdetail".ToPrefix()] = salesOrderDetail;
                                var product = entityItem.GetAttributeValue<EntityReference>("productid".ToPrefix());
                                if (product != null)
                                    trace.Trace("product: " + product.ToJson());
                                trace.Trace("SalesOrderDetail: " + salesOrderDetail.ToJson());

                                deliveryOrderDetail["qtydelivery".ToPrefix()] = entityItem.GetAttributeValue<double>("qtysale".ToPrefix());
                                deliveryOrderDetail["productid".ToPrefix()] = entityItem.GetAttributeValue<EntityReference>("productid".ToPrefix());
                                trace.Trace("Updated");
                                service.Create(deliveryOrderDetail);
                            }
                        }
                        trace.Trace("Sales Order Detail: " + salesOrderDetails.ToJson());
                    }
                    else trace.Trace("Sales Order null");
                }
            }
        }
    }
}