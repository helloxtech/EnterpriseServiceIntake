using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceIntake.Plugins
{
    internal static class ServiceRequestExternalUpdatePolicy
    {
        private static readonly HashSet<string> ProtectedFieldSet =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "hx_lifecyclestatus",
                "hx_approvalstatus",
                "hx_integrationsyncstatus",
                "hx_externalerpid",
                "hx_assigneddepartment",
                "hx_appliedslapolicy",
                "hx_duedate",
                "hx_requiresapproval",
                "hx_resolutiondocumentationrequired",
                "hx_resolutiondocumentationprovided",
                "hx_internalresolutionnotes",
                "hx_customervisibleupdates",
                "hx_routingpreviewsummary",
                "hx_slaindicatorstatus",
                "hx_visualseverity"
            };

        internal static IReadOnlyCollection<string> ProtectedFields => ProtectedFieldSet.ToArray();

        internal static IReadOnlyCollection<Guid> ParseAllowedUserIds(string configuration)
        {
            var values = new HashSet<Guid>();
            if (string.IsNullOrWhiteSpace(configuration))
            {
                return values;
            }

            foreach (var value in NormalizeConfiguration(configuration).Split(new[] { ',', ';', '|', '\n', '\r', '\t', ' ' },
                         StringSplitOptions.RemoveEmptyEntries))
            {
                if (Guid.TryParse(value.Trim(), out var id))
                {
                    values.Add(id);
                }
            }

            return values;
        }

        internal static IReadOnlyCollection<string> ParseAllowedEmails(string configuration)
        {
            var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(configuration))
            {
                return values;
            }

            foreach (var value in NormalizeConfiguration(configuration).Split(new[] { ',', ';', '|', '\n', '\r', '\t', ' ' },
                         StringSplitOptions.RemoveEmptyEntries))
            {
                var email = value.Trim();
                if (email.Contains("@"))
                {
                    values.Add(email);
                }
            }

            return values;
        }

        internal static bool ShouldBlock(
            IEnumerable<string> submittedAttributes,
            Guid userId,
            Guid initiatingUserId,
            IReadOnlyCollection<Guid> allowedUserIds)
        {
            return FindProtectedFields(submittedAttributes).Any()
                   && !IsAllowedCaller(userId, initiatingUserId, allowedUserIds);
        }

        internal static bool ShouldBlock(
            IEnumerable<string> submittedAttributes,
            Guid userId,
            Guid initiatingUserId,
            IReadOnlyCollection<Guid> allowedUserIds,
            IEnumerable<string> callerEmails,
            IReadOnlyCollection<string> allowedEmails)
        {
            return FindProtectedFields(submittedAttributes).Any()
                   && !IsAllowedCaller(userId, initiatingUserId, allowedUserIds)
                   && !IsAllowedCallerEmail(callerEmails, allowedEmails);
        }

        internal static string[] FindProtectedFields(IEnumerable<string> submittedAttributes)
        {
            return submittedAttributes
                .Where(attribute => ProtectedFieldSet.Contains(attribute))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static string NormalizeConfiguration(string configuration)
        {
            return configuration
                .Replace("allowedUserIds=", string.Empty)
                .Replace("allowedEmails=", string.Empty);
        }

        private static bool IsAllowedCaller(
            Guid userId,
            Guid initiatingUserId,
            IReadOnlyCollection<Guid> allowedUserIds)
        {
            return allowedUserIds.Contains(userId) || allowedUserIds.Contains(initiatingUserId);
        }

        private static bool IsAllowedCallerEmail(
            IEnumerable<string> callerEmails,
            IReadOnlyCollection<string> allowedEmails)
        {
            return callerEmails.Any(callerEmail =>
                allowedEmails.Any(allowedEmail =>
                    string.Equals(allowedEmail, callerEmail, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
