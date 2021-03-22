
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using Microsoft.Crm.Sdk.Messages;

namespace SDWANLeadassign
{
    public class SDWANLeadassign : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            ITracingService tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                try
                {

                    Entity entity = (Entity)context.InputParameters["Target"];//create the Entity where it call
                    Entity postEntity = (Entity)context.PostEntityImages["PostImage"];

                    string businessSegment = "";
                    string subSegment = "";
                    string leadChannel = "";

                    if (postEntity.Contains("alletech_businesssegmentglb"))
                        businessSegment = ((EntityReference)postEntity.Attributes["alletech_businesssegmentglb"]).Name;
                    tracing.Trace("businessSegment: " + businessSegment);

                    if (postEntity.Contains("alletech_subbusinesssegment"))
                        subSegment = ((EntityReference)postEntity.Attributes["alletech_subbusinesssegment"]).Name;
                    tracing.Trace("subSegment: " + subSegment);

                    if (postEntity.Contains("alletech_leadchannellookup"))
                        leadChannel = ((EntityReference)postEntity.Attributes["alletech_leadchannellookup"]).Name;
                    tracing.Trace("leadChannel: " + leadChannel);

                    if (businessSegment != null && subSegment != null && leadChannel != null)
                    {
                        tracing.Trace("segment : " + businessSegment + subSegment + leadChannel);

                        if (businessSegment == "Business" && subSegment == "SDWAN" && leadChannel != "Self Lead")//lead chanel and sub seg should come from config.
                        {
                            string vertical = ((EntityReference)postEntity.Attributes["onl_vertical"]).Name;
                            tracing.Trace("vertical: " + vertical);
                            QueryExpression objquery = new QueryExpression("onl_vertical");//creating the new Expression to relate the next entity
                            objquery.ColumnSet = new ColumnSet("onl_name");//get all attribute of this entity
                            objquery.Criteria.AddCondition(new ConditionExpression("onl_name", ConditionOperator.Equal, vertical));//check the opp id on saf is same as opp idin customer site
                            EntityCollection objCol = service.RetrieveMultiple(objquery);//
                            tracing.Trace("vertical Retrive Sucessfuly");

                            if (objCol != null)
                            {

                                if (objCol.Entities.Count > 0)
                                {
                                    Guid city = ((EntityReference)postEntity.Attributes["alletech_completecity"]).Id;
                                    tracing.Trace("city ID: " + city);
                                    QueryExpression objquery1 = new QueryExpression("onl_verticalconfiguration");//creating the new Expression to relate the next entity
                                    objquery1.ColumnSet = new ColumnSet("onl_verticalowneruser");//get all attribute of this entity
                                    objquery1.Criteria.AddCondition(new ConditionExpression("onl_city", ConditionOperator.Equal, city));//check the opp id on saf is same as opp idin customer site
                                    EntityCollection objCol1 = service.RetrieveMultiple(objquery1);//
                                    tracing.Trace("verticalconfiguraation retrive secussfuly.");

                                    if (objCol1 != null)
                                    {

                                        if (objCol1.Entities.Count == 1)
                                        {
                                            Guid verticalUser = ((EntityReference)objCol1[0].Attributes["onl_verticalowneruser"]).Id;
                                            tracing.Trace("vertical User: " + verticalUser);
                                            AssignRequest assign = new AssignRequest
                                            {
                                                Assignee = new EntityReference("systemuser", verticalUser),
                                                Target = new EntityReference("lead", entity.Id)

                                            };
                                            service.Execute(assign);
                                            tracing.Trace("Executed Sucessfully. ");
                                            Entity verticalConfig = new Entity("onl_verticalconfiguration");
                                            tracing.Trace("vertical Config: " + verticalConfig);
                                            verticalConfig.Id = objCol1[0].Id;
                                            tracing.Trace("vertical Config id: " + verticalConfig.Id);
                                            verticalConfig["onl_assigneddate"] = DateTime.Now;
                                            service.Update(verticalConfig);
                                            tracing.Trace("vertical Config updated sucessfuly: ");
                                        }

                                        if (objCol1.Entities.Count > 1)
                                        {
                                            QueryExpression objquery2 = new QueryExpression("onl_verticalconfiguration");//creating the new Expression to relate the next entity
                                            objquery2.ColumnSet = new ColumnSet("onl_verticalowneruser");//get all attribute of this entity
                                            objquery2.Criteria.AddCondition(new ConditionExpression("onl_city", ConditionOperator.Equal, city));//check the opp id on saf is same as opp idin customer site
                                            objquery2.Criteria.AddCondition(new ConditionExpression("onl_assigneddate", ConditionOperator.Null));//check the opp id on saf is same as opp idin customer site
                                            objquery2.AddOrder("createdon", OrderType.Ascending);
                                            EntityCollection objCol2 = service.RetrieveMultiple(objquery2);//
                                            tracing.Trace("vertical Config retrive secussfuly: ");

                                            if (objCol2 != null)
                                            {
                                                Guid verticalUser = ((EntityReference)objCol2[0].Attributes["onl_verticalowneruser"]).Id;
                                                tracing.Trace("vertical user: " + verticalUser);
                                                AssignRequest assign = new AssignRequest
                                                {
                                                    Assignee = new EntityReference("systemuser", verticalUser),
                                                    Target = new EntityReference("lead", entity.Id)
                                                };
                                                service.Execute(assign);
                                                tracing.Trace("service Executed Sucessfuly: ");
                                                Entity verticalConfig = new Entity("onl_verticalconfiguration");
                                                tracing.Trace("vertical entity: ");
                                                verticalConfig.Id = objCol2[0].Id;
                                                tracing.Trace("vertical config ID: " + verticalConfig.Id);
                                                verticalConfig["onl_assigneddate"] = DateTime.Now;
                                                service.Update(verticalConfig);

                                                tracing.Trace("vertcal config updated Sucessfuly: ");
                                            }

                                            else
                                            {
                                                QueryExpression objquery3 = new QueryExpression("onl_verticalconfiguration");//creating the new Expression to relate the next entity
                                                objquery3.ColumnSet = new ColumnSet("onl_verticalowneruser");//get all attribute of this entity
                                                objquery3.Criteria.AddCondition(new ConditionExpression("onl_city", ConditionOperator.Equal, city));//check the opp id on saf is same as opp idin customer site
                                                objquery3.AddOrder("onl_assigneddate", OrderType.Ascending);
                                                EntityCollection objCol3 = service.RetrieveMultiple(objquery3);//

                                                if (objCol3 != null)
                                                {
                                                    Guid verticalUser = ((EntityReference)objCol3[0].Attributes["onl_verticalowneruser"]).Id;
                                                    tracing.Trace("Vertical User GUID: " + verticalUser);
                                                    AssignRequest assign = new AssignRequest
                                                    {
                                                        Assignee = new EntityReference("systemuser", verticalUser),
                                                        Target = new EntityReference("lead", entity.Id)
                                                    };
                                                    service.Execute(assign);
                                                    tracing.Trace("service Executed Sucessfuly: ");

                                                    Entity verticalConfig = new Entity("onl_verticalconfiguration");
                                                    verticalConfig.Id = objCol3[0].Id;
                                                    verticalConfig["onl_assigneddate"] = DateTime.Now;
                                                    tracing.Trace("verticalConfig['onl_assigneddate'] " + verticalConfig["onl_assigneddate"]);
                                                    service.Update(verticalConfig);
                                                    tracing.Trace("vertical updated Sucessfuly: ");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidPluginExecutionException("Error: " + ex.Message);
                }
            }
        }
    }
}
