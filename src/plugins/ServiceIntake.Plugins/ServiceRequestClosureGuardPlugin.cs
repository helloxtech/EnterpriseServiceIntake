using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace ServiceIntake.Plugins
{
    /// <summary>
    /// Blocks closure of critical requests unless resolution notes and accepted resolution evidence are present.
    /// Register synchronously on hx_servicerequest Update PreOperation with a PreImage.
    /// </summary>
    public class ServiceRequestClosureGuardPlugin : PluginBase
    {
        public ServiceRequestClosureGuardPlugin(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(ServiceRequestClosureGuardPlugin))
        {
        }

        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext)
        {
            if (localPluginContext == null)
            {
                throw new ArgumentNullException(nameof(localPluginContext));
            }

            var context = localPluginContext.PluginExecutionContext;
            if (context.MessageName != "Update" ||
                !context.InputParameters.Contains("Target") ||
                !(context.InputParameters["Target"] is Entity target) ||
                target.LogicalName != Constants.ServiceRequest)
            {
                return;
            }

            var requestedStatus = target.GetAttributeValue<OptionSetValue>("hx_lifecyclestatus");
            if (requestedStatus == null ||
                (requestedStatus.Value != Constants.LifecycleClosed && requestedStatus.Value != Constants.LifecycleResolved))
            {
                return;
            }

            var service = localPluginContext.PluginUserService;
            var current = MergeTargetWithCurrentValues(context, service, target);
            var severity = current.GetAttributeValue<OptionSetValue>("hx_severity");
            var docsRequired = current.GetAttributeValue<bool?>("hx_resolutiondocumentationrequired") ?? false;

            if (severity == null || severity.Value != Constants.SeverityCritical || !docsRequired)
            {
                return;
            }

            var resolutionNotes = current.GetAttributeValue<string>("hx_internalresolutionnotes");
            var hasResolutionDocument = HasAcceptedResolutionEvidence(service, target.Id);

            if (string.IsNullOrWhiteSpace(resolutionNotes) || !hasResolutionDocument)
            {
                throw new InvalidPluginExecutionException(
                    "Critical requests cannot be resolved or closed until internal resolution notes and resolution documentation are provided.");
            }
        }

        private static Entity MergeTargetWithCurrentValues(IPluginExecutionContext context, IOrganizationService service, Entity target)
        {
            var merged = context.PreEntityImages.Contains("PreImage")
                ? context.PreEntityImages["PreImage"]
                : service.Retrieve(target.LogicalName, target.Id,
                    new ColumnSet("hx_severity", "hx_resolutiondocumentationrequired",
                        "hx_internalresolutionnotes"));

            var copy = new Entity(target.LogicalName, target.Id);
            foreach (var attribute in merged.Attributes)
            {
                copy[attribute.Key] = attribute.Value;
            }

            foreach (var attribute in target.Attributes)
            {
                copy[attribute.Key] = attribute.Value;
            }

            return copy;
        }

        private static bool HasAcceptedResolutionEvidence(IOrganizationService service, Guid serviceRequestId)
        {
            var query = new QueryExpression(Constants.ServiceDocument)
            {
                ColumnSet = new ColumnSet("hx_servicedocumentid"),
                TopCount = 1
            };
            query.Criteria.AddCondition("hx_servicerequest", ConditionOperator.Equal, serviceRequestId);
            query.Criteria.AddCondition("hx_documenttype", ConditionOperator.Equal, Constants.DocumentTypeResolution);
            query.Criteria.AddCondition("hx_reviewstatus", ConditionOperator.Equal, Constants.EvidenceReviewAccepted);
            query.Criteria.AddCondition("hx_verified", ConditionOperator.Equal, true);
            query.Criteria.AddCondition("hx_sharepointfileurl", ConditionOperator.NotNull);
            return service.RetrieveMultiple(query).Entities.Count > 0;
        }
    }
}
