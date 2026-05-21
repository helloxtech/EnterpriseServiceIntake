using ServiceIntake.Plugins;

var agentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
var portalUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
var workflowUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");

var allowed = ServiceRequestExternalUpdatePolicy.ParseAllowedUserIds(
    $"allowedUserIds={agentId}; {workflowUserId}");
var allowedEmails = ServiceRequestExternalUpdatePolicy.ParseAllowedEmails(
    "allowedEmails=agent@hellosmart.ca, manager@hellosmart.ca");

Assert(allowed.Contains(agentId), "Expected semicolon-delimited allowed user id to parse.");
Assert(allowed.Contains(workflowUserId), "Expected whitespace around allowed user id to be ignored.");

Assert(
    ServiceRequestExternalUpdatePolicy.ShouldBlock(
        new[] { "hx_internalresolutionnotes" },
        portalUserId,
        portalUserId,
        allowed),
    "Portal callers must not update internal resolution notes.");

Assert(
    !ServiceRequestExternalUpdatePolicy.ShouldBlock(
        new[] { "hx_internalresolutionnotes" },
        portalUserId,
        agentId,
        allowed),
    "Allowed initiating users must be able to update internal resolution notes.");

Assert(
    !ServiceRequestExternalUpdatePolicy.ShouldBlock(
        new[] { "hx_internalresolutionnotes" },
        portalUserId,
        portalUserId,
        Array.Empty<Guid>(),
        new[] { "AGENT@HELLOSMART.CA" },
        allowedEmails),
    "Allowed caller emails must be accepted when the Dataverse user id is not known during registration.");

Assert(
    !ServiceRequestExternalUpdatePolicy.ShouldBlock(
        new[] { "hx_title", "hx_description", "hx_priority" },
        portalUserId,
        portalUserId,
        allowed),
    "Portal-facing fields should not be treated as protected internal fields.");

Assert(
    ServiceRequestExternalUpdatePolicy.FindProtectedFields(
        new[] { "HX_EXTERNALERPID", "hx_title" }).SequenceEqual(new[] { "HX_EXTERNALERPID" }),
    "Protected field matching should be case-insensitive and preserve the submitted attribute name.");

Console.WriteLine("Service request external update policy tests passed.");

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
