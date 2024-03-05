using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using SalesOrderPlugin.Constant;
using SalesOrderPlugin.Enum;
using System;
using SalesOrderPlugin.Extension;

namespace SalesOrderPlugin.Operation
{
    public class SalesOrderDetailPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var trace = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var service = serviceFactory.CreateOrganizationService(context.InitiatingUserId);

            var entity = (Entity)context.InputParameters["Target"];
            trace.Trace("Entity: " + entity.LogicalName);
            if (entity.LogicalName == EntityConstant.SalesOrderDetail)
            {
                var retrievedEntity = service.Retrieve(entity.LogicalName, entity.Id, new ColumnSet("crbf2_productid", "crbf2_qtysales"));
                trace.Trace("Sales Order Detail: " + retrievedEntity.ToJson());

                if (entity.Contains("crbf2_productid"))
                {
                    var productId = retrievedEntity.GetAttributeValue<EntityReference>("crbf2_productid");
                    var entityReference = service.Retrieve("product", productId.Id, new ColumnSet("price"));
                    if (entityReference.Contains("price"))
                    {
                        var price = entityReference.GetAttributeValue<Money>("price");
                        if (price.Value < 1)
                        {
                            throw new InvalidPluginExecutionException("Please select Product that Price not equal zero");
                        }

                        trace.Trace("Price: " + price.Value);
                        var salesOrderDetail = new Entity(EntityConstant.SalesOrderDetail, entity.Id);
                        salesOrderDetail["crbf2_price"] = price;
                        service.Update(salesOrderDetail);
                    }
                }

                if (entity.Contains("crbf2_price") && entity.Contains("crbf2_qtysales"))
                {
                    var price = entity.GetAttributeValue<Money>("crbf2_price");
                    var qtySales = entity.GetAttributeValue<decimal>("crbf2_qtysales");
                    if (price.Value < 1)
                    {
                        throw new InvalidPluginExecutionException("Please select Product that Price not equal zero");
                    }

                    if (qtySales < 0)
                    {
                        throw new InvalidPluginExecutionException("Quantity Sales cannot be minus");
                    }

                    var totalAmountBeforeDiscount = price.Value * qtySales;

                    entity["crbf2_totalamountbeforediscount"] = totalAmountBeforeDiscount;
                    service.Update(entity);
                }

                if (entity.Contains("crbf2_qtydelivered"))
                {
                    var qtyDelivered = entity.GetAttributeValue<decimal>("crbf2_qtydelivered");
                    if (qtyDelivered < 0)
                    {
                        throw new InvalidPluginExecutionException("Qty Delivered cannot be minus");
                    }

                    var qtySales = retrievedEntity.GetAttributeValue<decimal>("crbf2_qtysales");
                    if (qtyDelivered > qtySales)
                    {
                        throw new InvalidPluginExecutionException("Qty Delivered cannot bigger than Qty Sales");
                    }
                }

                if (entity.Contains("crbf2_totalamountbeforediscount") && entity.Contains("crbf2_discountamount"))
                {
                    var totalAmountBeforeDiscount = entity.GetAttributeValue<Money>("crbf2_totalamountbeforediscount");
                    var discount = entity.GetAttributeValue<Money>("crbf2_discountamount");
                    if (discount.Value > 0)
                    {
                        var totalNetAmount = totalAmountBeforeDiscount.Value - discount.Value;
                        entity["crbf2_totalnetamount"] = new Money(totalNetAmount);
                        service.Update(entity);
                    }
                }

                if (entity.Contains("crbf2_totalnetamount"))
                {
                    var salesOrderReference = retrievedEntity.GetAttributeValue<EntityReference>("crbf2_salesorderid");
                    if (salesOrderReference != null)
                    {
                        var salesOrderDetails = service.RetrieveMultiple(new QueryExpression(EntityConstant.SalesOrderDetail)
                        {
                            ColumnSet = new ColumnSet("crbf2_totalnetamount"),
                            Criteria = new FilterExpression
                            {
                                Conditions =
                                {
                                    new ConditionExpression("crbf2_salesorderid", ConditionOperator.Equal, salesOrderReference.Id)
                                }
                            }
                        });

                        if (salesOrderDetails.Entities is { Count: > 0 })
                        {
                            decimal grantTotal = 0;
                            foreach (var item in salesOrderDetails.Entities)
                            {
                                var totalNetAmount = item.GetAttributeValue<Money>("crbf2_totalnetamount");
                                grantTotal += totalNetAmount.Value;
                            }

                            var salesOrder = new Entity(EntityConstant.SalesOrder, salesOrderReference.Id);
                            salesOrder["crbf2_granttotal"] = new Money(grantTotal);

                            service.Update(salesOrder);
                        }
                    }
                }

                //if (entityReference.Contains("price"))
                //{
                //    var price = entityReference.GetAttributeValue<Money>("price");
                //    if (price.Value < 1)
                //    {
                //        throw new InvalidPluginExecutionException("Please select Product that Price not equal zero");
                //    }

                //    trace.Trace("Price: " + price.Value);
                //    var salesOrderDetail = new Entity(EntityConstant.SalesOrderDetail, entity.Id);
                //    salesOrderDetail["crbf2_price"] = price;
                //    var qtySales = retrievedEntity.GetAttributeValue<decimal>("crbf2_qtysales");

                //    if (qtySales < 0)
                //    {
                //        throw new InvalidPluginExecutionException("Quantity Sales cannot be minus");
                //    }

                //    var qtyDelivered = retrievedEntity.GetAttributeValue<decimal>("crbf2_qtydelivered");
                //    if (qtyDelivered < 0)
                //    {
                //        throw new InvalidPluginExecutionException("Qty Delivered cannot be minus");
                //    }

                //    if (qtyDelivered > qtySales)
                //    {
                //        throw new InvalidPluginExecutionException("Qty Delivered cannot bigger than Qty Sales");
                //    }

                //    var totalAmountBeforeDiscount = price.Value * qtySales;
                //    var discount = retrievedEntity.GetAttributeValue<decimal>("crbf2_discountamount");
                //    if (discount > 0)
                //    {
                //        var totalNetAmount = totalAmountBeforeDiscount - discount;
                //        salesOrderDetail["crbf2_totalnetamount"] = new Money(totalNetAmount);
                //    }

                //    salesOrderDetail["crbf2_totalamountbeforediscount"] = totalAmountBeforeDiscount;
                //    service.Update(salesOrderDetail);
                //}
                //else
                //{
                //    throw new InvalidPluginExecutionException("Price not found for the product");
                //}
            }
        }
    }
}