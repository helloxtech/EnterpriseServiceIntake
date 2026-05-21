using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceIntake.Plugins
{
    /// <summary>
    /// Blocks closure of critical requests unless resolution notes and accepted resolution evidence are present.
    /// Register synchronously on hx_servicerequest Update PreOperation with a PreImage.
    /// </summary>
    public class ServiceRequestClosureGuardPlugin : PluginBase
    {
        private readonly IReadOnlyCollection<Guid> allowedInternalUserIds;
        private readonly IReadOnlyCollection<string> allowedInternalEmails;

        public ServiceRequestClosureGuardPlugin(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(ServiceRequestClosureGuardPlugin))
        {
            allowedInternalUserIds = ServiceRequestExternalUpdatePolicy.ParseAllowedUserIds(unsecureConfiguration);
            allowedInternalEmails = ServiceRequestExternalUpdatePolicy.ParseAllowedEmails(unsecureConfiguration);
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

            GuardProtectedInternalFields(context, localPluginContext.PluginUserService, target);

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

        private void GuardProtectedInternalFields(IPluginExecutionContext context, IOrganizationService service, Entity target)
        {
            var protectedFields = ServiceRequestExternalUpdatePolicy.FindProtectedFields(target.Attributes.Keys);
            if (protectedFields.Length == 0)
            {
                return;
            }

            var callerEmails = ResolveCallerEmails(service, context.UserId, context.InitiatingUserId);
            if (!ServiceRequestExternalUpdatePolicy.ShouldBlock(
                    target.Attributes.Keys,
                    context.UserId,
                    context.InitiatingUserId,
                    allowedInternalUserIds,
                    callerEmails,
                    allowedInternalEmails))
            {
                return;
            }

            throw new InvalidPluginExecutionException(
                $"Only internal service users can update protected service request fields: {string.Join(", ", protectedFields)}.");
        }

        private static IEnumerable<string> ResolveCallerEmails(IOrganizationService service, params Guid[] userIds)
        {
            foreach (var userId in userIds.Where(id => id != Guid.Empty).Distinct())
            {
                Entity user;
                try
                {
                    user = service.Retrieve("systemuser", userId,
                        new ColumnSet("internalemailaddress", "domainname"));
                }
                catch
                {
                    continue;
                }

                var internalEmail = user.GetAttributeValue<string>("internalemailaddress");
                if (!string.IsNullOrWhiteSpace(internalEmail))
                {
                    yield return internalEmail;
                }

                var domainName = user.GetAttributeValue<string>("domainname");
                if (!string.IsNullOrWhiteSpace(domainName))
                {
                    yield return domainName;
                }
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
