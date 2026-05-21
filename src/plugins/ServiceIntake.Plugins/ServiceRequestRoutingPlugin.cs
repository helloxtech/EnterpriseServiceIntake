using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;

namespace ServiceIntake.Plugins
{
    /// <summary>
    /// Applies the configured routing/SLA rule before a service request is saved.
    /// Register synchronously on hx_servicerequest Create/Update PreOperation.
    /// </summary>
    public class ServiceRequestRoutingPlugin : PluginBase
    {
        public ServiceRequestRoutingPlugin(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(ServiceRequestRoutingPlugin))
        {
        }

        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext)
        {
            if (localPluginContext == null)
            {
                throw new ArgumentNullException(nameof(localPluginContext));
            }

            var context = localPluginContext.PluginExecutionContext;
            if (!context.InputParameters.Contains("Target") || !(context.InputParameters["Target"] is Entity target))
            {
                return;
            }

            if (target.LogicalName != Constants.ServiceRequest)
            {
                return;
            }

            var service = localPluginContext.PluginUserService;
            var merged = MergeTargetWithPreImage(context, service, target);
            var category = merged.GetAttributeValue<EntityReference>("hx_servicecategory");
            var severity = merged.GetAttributeValue<OptionSetValue>("hx_severity");
            var priority = merged.GetAttributeValue<OptionSetValue>("hx_priority");

            if (category == null || severity == null || priority == null)
            {
                localPluginContext.Trace("Routing skipped because category, severity, or priority is not set.");
                return;
            }

            var rule = FindRoutingRule(service, category.Id, severity.Value, priority.Value);
            if (rule == null)
            {
                target["hx_routingpreviewsummary"] = "No exact routing rule matched. General Intake review required.";
                localPluginContext.Trace("No routing rule matched.");
                return;
            }

            var department = rule.GetAttributeValue<EntityReference>("hx_department");
            var sla = rule.GetAttributeValue<EntityReference>("hx_slapolicy");
            var requiresApproval = rule.GetAttributeValue<bool?>("hx_requiresapproval") ?? false;
            var requiresDocs = rule.GetAttributeValue<bool?>("hx_resolutiondocumentationrequired") ?? false;
            var responseHours = ResolveResponseHours(service, sla);
            var severityLabel = ResolveSeverityLabel(severity.Value);

            if (department != null)
            {
                target["hx_assigneddepartment"] = department;
            }

            if (sla != null)
            {
                target["hx_appliedslapolicy"] = sla;
            }

            target["hx_requiresapproval"] = requiresApproval;
            target["hx_resolutiondocumentationrequired"] = requiresDocs;
            target["hx_duedate"] = DateTime.UtcNow.AddHours(responseHours);
            target["hx_visualseverity"] = severityLabel;
            target["hx_slaindicatorstatus"] = requiresApproval
                ? "Pending manager approval"
                : "Ready for coordinator triage";
            target["hx_approvalstatus"] = new OptionSetValue(requiresApproval
                ? Constants.ApprovalPending
                : Constants.ApprovalNotRequired);
            target["hx_integrationsyncstatus"] = new OptionSetValue(Constants.SyncNotStarted);

            if (context.MessageName == "Create" && !target.Contains("hx_lifecyclestatus"))
            {
                target["hx_lifecyclestatus"] = new OptionSetValue(Constants.LifecycleSubmitted);
            }

            target["hx_routingpreviewsummary"] =
                $"{department?.Name ?? "General Intake"} | response target {responseHours} hour(s) | approval {(requiresApproval ? "required" : "not required")}";
        }

        private static Entity MergeTargetWithPreImage(IPluginExecutionContext context, IOrganizationService service, Entity target)
        {
            var merged = new Entity(target.LogicalName, target.Id);

            if (context.PreEntityImages.Contains("PreImage"))
            {
                foreach (var attribute in context.PreEntityImages["PreImage"].Attributes)
                {
                    merged[attribute.Key] = attribute.Value;
                }
            }
            else if (target.Id != Guid.Empty && context.MessageName == "Update")
            {
                var current = service.Retrieve(target.LogicalName, target.Id,
                    new ColumnSet("hx_servicecategory", "hx_severity", "hx_priority"));
                foreach (var attribute in current.Attributes)
                {
                    merged[attribute.Key] = attribute.Value;
                }
            }

            foreach (var attribute in target.Attributes)
            {
                merged[attribute.Key] = attribute.Value;
            }

            return merged;
        }

        private static Entity FindRoutingRule(IOrganizationService service, Guid categoryId, int severity, int priority)
        {
            var query = new QueryExpression(Constants.RoutingRule)
            {
                ColumnSet = new ColumnSet("hx_department", "hx_slapolicy", "hx_requiresapproval", "hx_resolutiondocumentationrequired"),
                TopCount = 1
            };
            query.Criteria.AddCondition("hx_active", ConditionOperator.Equal, true);
            query.Criteria.AddCondition("hx_servicecategory", ConditionOperator.Equal, categoryId);
            query.Criteria.AddCondition("hx_matchseverity", ConditionOperator.Equal, severity);
            query.Criteria.AddCondition("hx_matchpriority", ConditionOperator.Equal, priority);
            query.AddOrder("hx_sortorder", OrderType.Ascending);

            return service.RetrieveMultiple(query).Entities.FirstOrDefault();
        }

        private static int ResolveResponseHours(IOrganizationService service, EntityReference sla)
        {
            if (sla == null)
            {
                return 24;
            }

            var policy = service.Retrieve(Constants.SlaPolicy, sla.Id, new ColumnSet("hx_responsehours"));
            return policy.GetAttributeValue<int?>("hx_responsehours") ?? 24;
        }

        private static string ResolveSeverityLabel(int severity)
        {
            switch (severity)
            {
                case Constants.SeverityCritical:
                    return "Critical";
                case Constants.SeverityHigh:
                    return "High";
                case Constants.SeverityMedium:
                    return "Medium";
                case Constants.SeverityLow:
                    return "Low";
                default:
                    return "Unspecified";
            }
        }
    }
}
