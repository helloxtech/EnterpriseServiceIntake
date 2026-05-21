using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

const string Prefix = "hx";
const string SolutionUniqueName = "EnterpriseServiceIntake";
const string PublisherUniqueName = "HelloXTech";
const string Office365ConnectionReferenceLogicalName = "new_sharedoffice365_confirmation";
const string Office365ConnectorId = "/providers/Microsoft.PowerApps/apis/shared_office365";
const string HelloXMockErpEndpoint = "https://hellox.ca/api/esi-service-requests";

var url = Required("POWERPLATFORM_ENVIRONMENT_URL");
var username = Required("POWERPLATFORM_ADMIN_USERNAME");
var password = Required("POWERPLATFORM_ADMIN_PASSWORD");

var connectionString =
    $"AuthType=OAuth;Url={url};Username={username};Password={password};" +
    "AppId=51f81489-12ee-4a9e-aaae-a2591f45987d;" +
    "RedirectUri=app://58145B91-0C36-4500-8554-080854F2AC97;LoginPrompt=Never";

using var service = new ServiceClient(connectionString);
if (!service.IsReady)
{
    throw new InvalidOperationException($"Dataverse connection failed: {service.LastError}");
}

var who = (WhoAmIResponse)service.Execute(new WhoAmIRequest());
Console.WriteLine($"Connected. UserId={who.UserId}");

if (string.Equals(Environment.GetEnvironmentVariable("DUMP_PORTAL_METADATA"), "true", StringComparison.OrdinalIgnoreCase))
{
    DumpPortalMetadata(service);
    return;
}

if (string.Equals(Environment.GetEnvironmentVariable("PATCH_FLOW_DEFINITION"), "true", StringComparison.OrdinalIgnoreCase))
{
    PatchApprovalFlowDefinition(service);
    return;
}

if (string.Equals(Environment.GetEnvironmentVariable("ENSURE_CONFIRMATION_EMAIL_FLOW"), "true", StringComparison.OrdinalIgnoreCase))
{
    EnsureConfirmationEmailFlow(service);
    return;
}

EnsurePublisher(service);
EnsureSolution(service);
EnsureMetadata(service);
EnsureModelDrivenExperience(service);
RegisterPlugins(service);
Publish(service);
SeedSampleData(service);
if (string.Equals(Environment.GetEnvironmentVariable("RUN_VALIDATION_TESTS"), "true", StringComparison.OrdinalIgnoreCase))
{
    RunValidationSmokeTests(service);
}

Console.WriteLine("Provisioning complete.");

static string Required(string name)
{
    var value = Environment.GetEnvironmentVariable(name);
    return string.IsNullOrWhiteSpace(value)
        ? throw new InvalidOperationException($"Missing environment variable: {name}")
        : value;
}

static Label Label(string text) => new(text, 1033);

static void EnsurePublisher(IOrganizationService service)
{
    if (FindByAttribute(service, "publisher", "uniquename", PublisherUniqueName) != null)
    {
        return;
    }

    var publisher = new Entity("publisher")
    {
        ["uniquename"] = PublisherUniqueName,
        ["friendlyname"] = "HelloX Tech",
        ["customizationprefix"] = Prefix,
        ["customizationoptionvalueprefix"] = 75263
    };
    service.Create(publisher);
    Console.WriteLine("Created publisher.");
}

static void EnsureSolution(IOrganizationService service)
{
    if (FindByAttribute(service, "solution", "uniquename", SolutionUniqueName) != null)
    {
        return;
    }

    var publisher = FindByAttribute(service, "publisher", "uniquename", PublisherUniqueName)
        ?? throw new InvalidOperationException("Publisher was not created.");

    var solution = new Entity("solution")
    {
        ["uniquename"] = SolutionUniqueName,
        ["friendlyname"] = "Enterprise Service Intake",
        ["description"] = "Senior Power Platform Developer take-home assignment solution.",
        ["version"] = "1.0.0.0",
        ["publisherid"] = publisher.ToEntityReference()
    };
    service.Create(solution);
    Console.WriteLine("Created solution.");
}

static Entity? FindByAttribute(IOrganizationService service, string entityName, string attributeName, object value)
{
    var query = new QueryExpression(entityName)
    {
        ColumnSet = new ColumnSet(true),
        TopCount = 1
    };
    query.Criteria.AddCondition(attributeName, ConditionOperator.Equal, value);
    return service.RetrieveMultiple(query).Entities.FirstOrDefault();
}

static bool EntityExists(IOrganizationService service, string logicalName)
{
    try
    {
        service.Execute(new RetrieveEntityRequest
        {
            LogicalName = logicalName,
            EntityFilters = EntityFilters.Entity
        });
        return true;
    }
    catch
    {
        return false;
    }
}

static bool AttributeExists(IOrganizationService service, string entityName, string logicalName)
{
    try
    {
        service.Execute(new RetrieveAttributeRequest
        {
            EntityLogicalName = entityName,
            LogicalName = logicalName
        });
        return true;
    }
    catch
    {
        return false;
    }
}

static void EnsureMetadata(IOrganizationService service)
{
    EnsureEntity(service, "hx_department", "Department", "Departments", "hx_name", "Name");
    EnsureString(service, "hx_department", "hx_code", "Code", 40);
    EnsureString(service, "hx_department", "hx_manageremail", "Manager Email", 200);
    EnsureBoolean(service, "hx_department", "hx_active", "Active", true);
    EnsureMemo(service, "hx_department", "hx_description", "Description");

    EnsureEntity(service, "hx_servicecategory", "Service Category", "Service Categories", "hx_name", "Name");
    EnsureString(service, "hx_servicecategory", "hx_code", "Code", 40);
    EnsureBoolean(service, "hx_servicecategory", "hx_active", "Active", true);
    EnsureBoolean(service, "hx_servicecategory", "hx_defaultdocumentationrequired", "Default Documentation Required", false);

    EnsureEntity(service, "hx_slapolicy", "SLA Policy", "SLA Policies", "hx_name", "Name");
    EnsureInteger(service, "hx_slapolicy", "hx_responsehours", "Response Hours", 1, 720);
    EnsureInteger(service, "hx_slapolicy", "hx_resolutionhours", "Resolution Hours", 1, 1440);
    EnsureBoolean(service, "hx_slapolicy", "hx_active", "Active", true);
    EnsureMemo(service, "hx_slapolicy", "hx_description", "Description");

    EnsureEntity(service, "hx_routingrule", "Routing / SLA Rule", "Routing / SLA Rules", "hx_name", "Name");
    EnsureInteger(service, "hx_routingrule", "hx_sortorder", "Sort Order", 1, 1000);
    EnsureBoolean(service, "hx_routingrule", "hx_active", "Active", true);
    EnsureChoice(service, "hx_routingrule", "hx_matchseverity", "Match Severity", SeverityOptions());
    EnsureChoice(service, "hx_routingrule", "hx_matchpriority", "Match Priority", PriorityOptions());
    EnsureBoolean(service, "hx_routingrule", "hx_requiresapproval", "Requires Manager Approval", false);
    EnsureBoolean(service, "hx_routingrule", "hx_resolutiondocumentationrequired", "Resolution Documentation Required", false);
    EnsureLookup(service, "hx_routingrule", "hx_servicecategory", "hx_servicecategory", "Service Category");
    EnsureLookup(service, "hx_routingrule", "hx_department", "hx_department", "Department");
    EnsureLookup(service, "hx_routingrule", "hx_slapolicy", "hx_slapolicy", "SLA Policy");

    EnsureEntity(service, "hx_servicerequest", "Service Request", "Service Requests", "hx_title", "Title");
    EnsureAutoNumber(service, "hx_servicerequest", "hx_confirmationnumber", "Confirmation Number", 100,
        "SR-{DATETIMEUTC:yyyyMMdd}-{SEQNUM:6}");
    EnsureMemo(service, "hx_servicerequest", "hx_description", "Description");
    EnsureChoice(service, "hx_servicerequest", "hx_severity", "Severity", SeverityOptions());
    EnsureChoice(service, "hx_servicerequest", "hx_priority", "Priority", PriorityOptions());
    EnsureChoice(service, "hx_servicerequest", "hx_lifecyclestatus", "Lifecycle Status", LifecycleStatusOptions());
    EnsureDateTime(service, "hx_servicerequest", "hx_submittedon", "Submitted On");
    EnsureDateTime(service, "hx_servicerequest", "hx_duedate", "SLA Due Date");
    EnsureBoolean(service, "hx_servicerequest", "hx_requiresapproval", "Requires Manager Approval", false);
    EnsureChoice(service, "hx_servicerequest", "hx_approvalstatus", "Approval Status", ApprovalStatusOptions());
    EnsureString(service, "hx_servicerequest", "hx_externalerpid", "External ERP ID", 100);
    EnsureChoice(service, "hx_servicerequest", "hx_integrationsyncstatus", "Integration Sync Status", SyncStatusOptions());
    EnsureMemo(service, "hx_servicerequest", "hx_internalresolutionnotes", "Internal Resolution Notes", isSecured: true);
    EnsureMemo(service, "hx_servicerequest", "hx_customervisibleupdates", "Customer Visible Updates");
    EnsureBoolean(service, "hx_servicerequest", "hx_resolutiondocumentationrequired", "Resolution Documentation Required", false);
    EnsureBoolean(service, "hx_servicerequest", "hx_resolutiondocumentationprovided", "Resolution Documentation Provided", false);
    EnsureString(service, "hx_servicerequest", "hx_routingpreviewsummary", "Routing Preview Summary", 500);
    EnsureString(service, "hx_servicerequest", "hx_slaindicatorstatus", "SLA Indicator Status", 200);
    EnsureString(service, "hx_servicerequest", "hx_visualseverity", "Visual Severity", 40);
    EnsureLookup(service, "hx_servicerequest", "contact", "hx_customercontact", "Customer Contact");
    EnsureLookup(service, "hx_servicerequest", "account", "hx_customeraccount", "Customer Account");
    EnsureLookup(service, "hx_servicerequest", "hx_servicecategory", "hx_servicecategory", "Service Category");
    EnsureLookup(service, "hx_servicerequest", "hx_department", "hx_assigneddepartment", "Assigned Department");
    EnsureLookup(service, "hx_servicerequest", "hx_slapolicy", "hx_appliedslapolicy", "Applied SLA Policy");

    EnsureEntity(service, "hx_servicedocument", "Service Request Document", "Service Request Documents", "hx_name", "Name");
    EnsureChoice(service, "hx_servicedocument", "hx_documenttype", "Document Type", DocumentTypeOptions());
    EnsureString(service, "hx_servicedocument", "hx_filename", "File Name", 255);
    EnsureBoolean(service, "hx_servicedocument", "hx_verified", "Verified", false);
    EnsureMemo(service, "hx_servicedocument", "hx_notes", "Notes");
    EnsureLookup(service, "hx_servicedocument", "hx_servicerequest", "hx_servicerequest", "Service Request");

    EnsureEntity(service, "hx_externalsynclog", "External Sync Log", "External Sync Logs", "hx_name", "Name");
    EnsureChoice(service, "hx_externalsynclog", "hx_syncstatus", "Sync Status", SyncStatusOptions());
    EnsureString(service, "hx_externalsynclog", "hx_endpointname", "Endpoint Name", 100);
    EnsureString(service, "hx_externalsynclog", "hx_externalid", "External ID", 100);
    EnsureDateTime(service, "hx_externalsynclog", "hx_attemptedon", "Attempted On");
    EnsureMemo(service, "hx_externalsynclog", "hx_requestpayload", "Request Payload");
    EnsureMemo(service, "hx_externalsynclog", "hx_responsesummary", "Response Summary");
    EnsureLookup(service, "hx_externalsynclog", "hx_servicerequest", "hx_servicerequest", "Service Request");

    EnsureEntity(service, "hx_errorlog", "System Error Log", "System Error Logs", "hx_name", "Name");
    EnsureChoice(service, "hx_errorlog", "hx_sourcecomponent", "Source Component", ErrorSourceOptions());
    EnsureString(service, "hx_errorlog", "hx_stage", "Stage", 100);
    EnsureString(service, "hx_errorlog", "hx_correlationid", "Correlation ID", 100);
    EnsureMemo(service, "hx_errorlog", "hx_message", "Message");
    EnsureMemo(service, "hx_errorlog", "hx_technicaldetail", "Technical Detail");
    EnsureMemo(service, "hx_errorlog", "hx_payload", "Payload");
    EnsureBoolean(service, "hx_errorlog", "hx_resolved", "Resolved", false);
    EnsureLookup(service, "hx_errorlog", "hx_servicerequest", "hx_servicerequest", "Service Request");
}

static void DumpPortalMetadata(IOrganizationService service)
{
    foreach (var table in new[] { "hx_servicerequest", "hx_servicedocument", "hx_servicecategory", "hx_routingrule", "hx_department", "hx_slapolicy" })
    {
        var response = (RetrieveEntityResponse)service.Execute(new RetrieveEntityRequest
        {
            LogicalName = table,
            EntityFilters = EntityFilters.Entity | EntityFilters.Relationships
        });

        Console.WriteLine($"{table} EntitySetName={response.EntityMetadata.EntitySetName}");
        foreach (var relationship in response.EntityMetadata.ManyToOneRelationships
                     .Where(item => item.ReferencingEntity == table && item.SchemaName.StartsWith("hx_", StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine(
                $"  Lookup {relationship.ReferencingAttribute} -> {relationship.ReferencedEntity}; Schema={relationship.SchemaName}; ReferencedNav={relationship.ReferencedEntityNavigationPropertyName}; ReferencingNav={relationship.ReferencingEntityNavigationPropertyName}");
        }
    }
}

static void EnsureEntity(
    IOrganizationService service,
    string logicalName,
    string displayName,
    string pluralName,
    string primaryNameLogicalName,
    string primaryNameDisplayName)
{
    if (EntityExists(service, logicalName))
    {
        return;
    }

    var request = new CreateEntityRequest
    {
        SolutionUniqueName = SolutionUniqueName,
        Entity = new EntityMetadata
        {
            SchemaName = ToSchema(logicalName),
            DisplayName = Label(displayName),
            DisplayCollectionName = Label(pluralName),
            Description = Label($"{displayName} table for the Enterprise Service Intake solution."),
            OwnershipType = OwnershipTypes.UserOwned,
            IsActivity = false
        },
        PrimaryAttribute = new StringAttributeMetadata
        {
            SchemaName = ToSchema(primaryNameLogicalName),
            RequiredLevel = new AttributeRequiredLevelManagedProperty(AttributeRequiredLevel.ApplicationRequired),
            MaxLength = 200,
            FormatName = StringFormatName.Text,
            DisplayName = Label(primaryNameDisplayName),
            Description = Label($"Primary name for {displayName}.")
        }
    };

    service.Execute(request);
    Console.WriteLine($"Created table {logicalName}.");
}

static string ToSchema(string logicalName)
{
    var normalized = logicalName.StartsWith($"{Prefix}_", StringComparison.Ordinal)
        ? logicalName[$"{Prefix}_".Length..]
        : logicalName;
    var parts = normalized.Split('_', StringSplitOptions.RemoveEmptyEntries);
    return $"{Prefix}_{string.Concat(parts.Select(part => char.ToUpperInvariant(part[0]) + part[1..]))}";
}

static void EnsureString(IOrganizationService service, string entity, string logicalName, string displayName, int maxLength)
{
    if (AttributeExists(service, entity, logicalName)) return;
    service.Execute(new CreateAttributeRequest
    {
        SolutionUniqueName = SolutionUniqueName,
        EntityName = entity,
        Attribute = new StringAttributeMetadata
        {
            SchemaName = ToSchema(logicalName),
            DisplayName = Label(displayName),
            MaxLength = maxLength,
            FormatName = StringFormatName.Text
        }
    });
    Console.WriteLine($"Created {entity}.{logicalName}.");
}

static void EnsureAutoNumber(
    IOrganizationService service,
    string entity,
    string logicalName,
    string displayName,
    int maxLength,
    string format)
{
    if (AttributeExists(service, entity, logicalName)) return;
    service.Execute(new CreateAttributeRequest
    {
        SolutionUniqueName = SolutionUniqueName,
        EntityName = entity,
        Attribute = new StringAttributeMetadata
        {
            SchemaName = ToSchema(logicalName),
            DisplayName = Label(displayName),
            MaxLength = maxLength,
            FormatName = StringFormatName.Text,
            AutoNumberFormat = format
        }
    });
    Console.WriteLine($"Created autonumber {entity}.{logicalName}.");
}

static void EnsureMemo(
    IOrganizationService service,
    string entity,
    string logicalName,
    string displayName,
    int maxLength = 4000,
    bool isSecured = false)
{
    if (AttributeExists(service, entity, logicalName)) return;
    service.Execute(new CreateAttributeRequest
    {
        SolutionUniqueName = SolutionUniqueName,
        EntityName = entity,
        Attribute = new MemoAttributeMetadata
        {
            SchemaName = ToSchema(logicalName),
            DisplayName = Label(displayName),
            MaxLength = maxLength,
            FormatName = MemoFormatName.TextArea,
            IsSecured = isSecured
        }
    });
    Console.WriteLine($"Created {entity}.{logicalName}.");
}

static void EnsureInteger(IOrganizationService service, string entity, string logicalName, string displayName, int min, int max)
{
    if (AttributeExists(service, entity, logicalName)) return;
    service.Execute(new CreateAttributeRequest
    {
        SolutionUniqueName = SolutionUniqueName,
        EntityName = entity,
        Attribute = new IntegerAttributeMetadata
        {
            SchemaName = ToSchema(logicalName),
            DisplayName = Label(displayName),
            MinValue = min,
            MaxValue = max,
            Format = IntegerFormat.None
        }
    });
    Console.WriteLine($"Created {entity}.{logicalName}.");
}

static void EnsureBoolean(IOrganizationService service, string entity, string logicalName, string displayName, bool defaultValue)
{
    if (AttributeExists(service, entity, logicalName)) return;
    service.Execute(new CreateAttributeRequest
    {
        SolutionUniqueName = SolutionUniqueName,
        EntityName = entity,
        Attribute = new BooleanAttributeMetadata
        {
            SchemaName = ToSchema(logicalName),
            DisplayName = Label(displayName),
            DefaultValue = defaultValue,
            OptionSet = new BooleanOptionSetMetadata(
                new OptionMetadata(Label("Yes"), 1),
                new OptionMetadata(Label("No"), 0))
        }
    });
    Console.WriteLine($"Created {entity}.{logicalName}.");
}

static void EnsureDateTime(IOrganizationService service, string entity, string logicalName, string displayName)
{
    if (AttributeExists(service, entity, logicalName)) return;
    service.Execute(new CreateAttributeRequest
    {
        SolutionUniqueName = SolutionUniqueName,
        EntityName = entity,
        Attribute = new DateTimeAttributeMetadata
        {
            SchemaName = ToSchema(logicalName),
            DisplayName = Label(displayName),
            Format = DateTimeFormat.DateAndTime,
            DateTimeBehavior = DateTimeBehavior.UserLocal
        }
    });
    Console.WriteLine($"Created {entity}.{logicalName}.");
}

static void EnsureChoice(
    IOrganizationService service,
    string entity,
    string logicalName,
    string displayName,
    IReadOnlyList<(int Value, string Label)> options)
{
    if (AttributeExists(service, entity, logicalName)) return;
    var optionSet = new OptionSetMetadata
    {
        IsGlobal = false,
        OptionSetType = OptionSetType.Picklist
    };
    foreach (var option in options)
    {
        optionSet.Options.Add(new OptionMetadata(Label(option.Label), option.Value));
    }

    service.Execute(new CreateAttributeRequest
    {
        SolutionUniqueName = SolutionUniqueName,
        EntityName = entity,
        Attribute = new PicklistAttributeMetadata
        {
            SchemaName = ToSchema(logicalName),
            DisplayName = Label(displayName),
            OptionSet = optionSet
        }
    });
    Console.WriteLine($"Created choice {entity}.{logicalName}.");
}

static void EnsureLookup(
    IOrganizationService service,
    string referencingEntity,
    string referencedEntity,
    string lookupLogicalName,
    string displayName)
{
    if (AttributeExists(service, referencingEntity, lookupLogicalName)) return;

    var relationshipSchema = ToSchema($"{referencedEntity}_{referencingEntity}_{lookupLogicalName}");
    service.Execute(new CreateOneToManyRequest
    {
        SolutionUniqueName = SolutionUniqueName,
        OneToManyRelationship = new OneToManyRelationshipMetadata
        {
            SchemaName = relationshipSchema,
            ReferencedEntity = referencedEntity,
            ReferencingEntity = referencingEntity,
            CascadeConfiguration = new CascadeConfiguration
            {
                Assign = CascadeType.NoCascade,
                Delete = CascadeType.RemoveLink,
                Merge = CascadeType.NoCascade,
                Reparent = CascadeType.NoCascade,
                Share = CascadeType.NoCascade,
                Unshare = CascadeType.NoCascade
            }
        },
        Lookup = new LookupAttributeMetadata
        {
            SchemaName = ToSchema(lookupLogicalName),
            DisplayName = Label(displayName),
            RequiredLevel = new AttributeRequiredLevelManagedProperty(AttributeRequiredLevel.None)
        }
    });
    Console.WriteLine($"Created lookup {referencingEntity}.{lookupLogicalName} -> {referencedEntity}.");
}

static IReadOnlyList<(int Value, string Label)> SeverityOptions() => new[]
{
    (752630000, "Low"),
    (752630001, "Medium"),
    (752630002, "High"),
    (752630003, "Critical")
};

static IReadOnlyList<(int Value, string Label)> PriorityOptions() => new[]
{
    (752630000, "Low"),
    (752630001, "Normal"),
    (752630002, "High"),
    (752630003, "Urgent")
};

static IReadOnlyList<(int Value, string Label)> LifecycleStatusOptions() => new[]
{
    (752630000, "Draft"),
    (752630001, "Submitted"),
    (752630002, "Triage"),
    (752630003, "Pending Approval"),
    (752630004, "Approved"),
    (752630005, "ERP Synced"),
    (752630006, "In Progress"),
    (752630007, "Resolved"),
    (752630008, "Closed"),
    (752630009, "Rejected")
};

static IReadOnlyList<(int Value, string Label)> ApprovalStatusOptions() => new[]
{
    (752630000, "Not Required"),
    (752630001, "Pending"),
    (752630002, "Approved"),
    (752630003, "Rejected"),
    (752630004, "Failed")
};

static IReadOnlyList<(int Value, string Label)> SyncStatusOptions() => new[]
{
    (752630000, "Not Started"),
    (752630001, "Pending"),
    (752630002, "Succeeded"),
    (752630003, "Failed")
};

static IReadOnlyList<(int Value, string Label)> DocumentTypeOptions() => new[]
{
    (752630000, "Supporting"),
    (752630001, "Resolution"),
    (752630002, "Manager Approval"),
    (752630003, "Other")
};

static IReadOnlyList<(int Value, string Label)> ErrorSourceOptions() => new[]
{
    (752630000, "Plugin"),
    (752630001, "Power Automate"),
    (752630002, "Power Pages"),
    (752630003, "External API"),
    (752630004, "Unknown")
};

static void Publish(IOrganizationService service)
{
    service.Execute(new PublishAllXmlRequest());
    Console.WriteLine("Published customizations.");
}

static void EnsureModelDrivenExperience(IOrganizationService service)
{
    EnsureMainForm(
        service,
        "hx_servicerequest",
        "Information",
        "Service Request - Coordinator",
        new[]
        {
            new FormSection("Coordinator Status", new[]
            {
                SlaPcfField("hx_slaindicatorstatus", "SLA Status", FormControlClass.Text),
                Field("hx_visualseverity", "Visual Severity", FormControlClass.Text, disabled: true)
            }),
            new FormSection("Customer and Request", new[]
            {
                Field("hx_title", "Title", FormControlClass.Text),
                Field("hx_confirmationnumber", "Confirmation Number", FormControlClass.Text, disabled: true),
                Field("hx_customercontact", "Customer Contact", FormControlClass.Lookup),
                Field("hx_customeraccount", "Customer Account", FormControlClass.Lookup),
                Field("hx_servicecategory", "Service Category", FormControlClass.Lookup),
                Field("hx_description", "Description", FormControlClass.Memo)
            }),
            new FormSection("Triage Inputs", new[]
            {
                Field("hx_severity", "Severity", FormControlClass.OptionSet),
                Field("hx_priority", "Priority", FormControlClass.OptionSet),
                Field("hx_submittedon", "Submitted On", FormControlClass.DateTime, disabled: true),
                Field("ownerid", "Owner", FormControlClass.Lookup)
            }),
            new FormSection("Routing and SLA", new[]
            {
                Field("hx_assigneddepartment", "Assigned Department", FormControlClass.Lookup, disabled: true),
                Field("hx_appliedslapolicy", "Applied SLA Policy", FormControlClass.Lookup, disabled: true),
                Field("hx_duedate", "SLA Due Date", FormControlClass.DateTime, disabled: true),
                Field("hx_routingpreviewsummary", "Routing / SLA Summary", FormControlClass.Text, disabled: true)
            }),
            new FormSection("Approval and ERP Sync", new[]
            {
                Field("hx_requiresapproval", "Requires Approval", FormControlClass.Boolean, disabled: true),
                Field("hx_approvalstatus", "Approval Status", FormControlClass.OptionSet),
                Field("hx_integrationsyncstatus", "Integration Sync Status", FormControlClass.OptionSet),
                Field("hx_externalerpid", "External ERP ID", FormControlClass.Text, disabled: true)
            }),
            new FormSection("Resolution Guardrail", new[]
            {
                Field("hx_lifecyclestatus", "Lifecycle Status", FormControlClass.OptionSet),
                Field("hx_resolutiondocumentationrequired", "Resolution Documentation Required", FormControlClass.Boolean, disabled: true),
                Field("hx_resolutiondocumentationprovided", "Resolution Documentation Provided", FormControlClass.Boolean),
                Field("hx_internalresolutionnotes", "Internal Resolution Notes", FormControlClass.Memo),
                Field("hx_customervisibleupdates", "Customer Visible Updates", FormControlClass.Memo)
            })
        },
        new[]
        {
            Field("hx_confirmationnumber", "Confirmation", FormControlClass.Text, disabled: true),
            Field("hx_lifecyclestatus", "Lifecycle", FormControlClass.OptionSet),
            Field("hx_assigneddepartment", "Department", FormControlClass.Lookup, disabled: true),
            Field("hx_duedate", "SLA Due", FormControlClass.DateTime, disabled: true)
        });

    EnsureMainForm(
        service,
        "hx_servicedocument",
        "Information",
        "Service Document - Review",
        new[]
        {
            new FormSection("Document", new[]
            {
                Field("hx_name", "Name", FormControlClass.Text),
                Field("hx_servicerequest", "Service Request", FormControlClass.Lookup),
                Field("hx_documenttype", "Document Type", FormControlClass.OptionSet),
                Field("hx_filename", "File Name", FormControlClass.Text)
            }),
            new FormSection("Review", new[]
            {
                Field("hx_verified", "Verified", FormControlClass.Boolean),
                Field("hx_notes", "Notes", FormControlClass.Memo),
                Field("ownerid", "Owner", FormControlClass.Lookup)
            })
        },
        new[]
        {
            Field("hx_documenttype", "Type", FormControlClass.OptionSet),
            Field("hx_verified", "Verified", FormControlClass.Boolean),
            Field("ownerid", "Owner", FormControlClass.Lookup)
        });

    EnsureMainForm(
        service,
        "hx_routingrule",
        "Information",
        "Routing Rule - Configuration",
        new[]
        {
            new FormSection("Rule Identity", new[]
            {
                Field("hx_name", "Name", FormControlClass.Text),
                Field("hx_sortorder", "Sort Order", FormControlClass.Text),
                Field("hx_active", "Active", FormControlClass.Boolean)
            }),
            new FormSection("Match Criteria", new[]
            {
                Field("hx_servicecategory", "Service Category", FormControlClass.Lookup),
                Field("hx_matchseverity", "Match Severity", FormControlClass.OptionSet),
                Field("hx_matchpriority", "Match Priority", FormControlClass.OptionSet)
            }),
            new FormSection("Routing Outcome", new[]
            {
                Field("hx_department", "Department", FormControlClass.Lookup),
                Field("hx_slapolicy", "SLA Policy", FormControlClass.Lookup),
                Field("hx_requiresapproval", "Requires Manager Approval", FormControlClass.Boolean),
                Field("hx_resolutiondocumentationrequired", "Resolution Documentation Required", FormControlClass.Boolean)
            })
        },
        new[]
        {
            Field("hx_sortorder", "Sort", FormControlClass.Text),
            Field("hx_active", "Active", FormControlClass.Boolean),
            Field("hx_requiresapproval", "Approval", FormControlClass.Boolean)
        });

    EnsureMainForm(
        service,
        "hx_department",
        "Information",
        "Department - Configuration",
        new[]
        {
            new FormSection("Department", new[]
            {
                Field("hx_name", "Name", FormControlClass.Text),
                Field("hx_code", "Code", FormControlClass.Text),
                Field("hx_manageremail", "Manager Email", FormControlClass.Text),
                Field("hx_active", "Active", FormControlClass.Boolean)
            }),
            new FormSection("Operating Notes", new[]
            {
                Field("hx_description", "Description", FormControlClass.Memo),
                Field("ownerid", "Owner", FormControlClass.Lookup)
            })
        },
        new[]
        {
            Field("hx_code", "Code", FormControlClass.Text),
            Field("hx_active", "Active", FormControlClass.Boolean),
            Field("ownerid", "Owner", FormControlClass.Lookup)
        });

    EnsureMainForm(
        service,
        "hx_slapolicy",
        "Information",
        "SLA Policy - Configuration",
        new[]
        {
            new FormSection("SLA Targets", new[]
            {
                Field("hx_name", "Name", FormControlClass.Text),
                Field("hx_responsehours", "Response Hours", FormControlClass.Text),
                Field("hx_resolutionhours", "Resolution Hours", FormControlClass.Text),
                Field("hx_active", "Active", FormControlClass.Boolean)
            }),
            new FormSection("Policy Notes", new[]
            {
                Field("hx_description", "Description", FormControlClass.Memo),
                Field("ownerid", "Owner", FormControlClass.Lookup)
            })
        },
        new[]
        {
            Field("hx_responsehours", "Response Hours", FormControlClass.Text),
            Field("hx_resolutionhours", "Resolution Hours", FormControlClass.Text),
            Field("hx_active", "Active", FormControlClass.Boolean)
        });

    EnsureMainForm(
        service,
        "hx_servicecategory",
        "Information",
        "Service Category - Configuration",
        new[]
        {
            new FormSection("Category", new[]
            {
                Field("hx_name", "Name", FormControlClass.Text),
                Field("hx_code", "Code", FormControlClass.Text),
                Field("hx_active", "Active", FormControlClass.Boolean),
                Field("hx_defaultdocumentationrequired", "Default Documentation Required", FormControlClass.Boolean),
                Field("ownerid", "Owner", FormControlClass.Lookup)
            })
        },
        new[]
        {
            Field("hx_code", "Code", FormControlClass.Text),
            Field("hx_active", "Active", FormControlClass.Boolean),
            Field("hx_defaultdocumentationrequired", "Docs Required", FormControlClass.Boolean)
        });

    EnsureMainForm(
        service,
        "hx_externalsynclog",
        "Information",
        "External Sync Log - Review",
        new[]
        {
            new FormSection("Sync Attempt", new[]
            {
                Field("hx_name", "Name", FormControlClass.Text),
                Field("hx_servicerequest", "Service Request", FormControlClass.Lookup),
                Field("hx_syncstatus", "Sync Status", FormControlClass.OptionSet),
                Field("hx_endpointname", "Endpoint Name", FormControlClass.Text),
                Field("hx_externalid", "External ID", FormControlClass.Text),
                Field("hx_attemptedon", "Attempted On", FormControlClass.DateTime)
            }),
            new FormSection("Payload and Response", new[]
            {
                Field("hx_requestpayload", "Request Payload", FormControlClass.Memo),
                Field("hx_responsesummary", "Response Summary", FormControlClass.Memo)
            })
        },
        new[]
        {
            Field("hx_syncstatus", "Status", FormControlClass.OptionSet),
            Field("hx_attemptedon", "Attempted", FormControlClass.DateTime),
            Field("hx_externalid", "External ID", FormControlClass.Text)
        });

    EnsureMainForm(
        service,
        "hx_errorlog",
        "Information",
        "System Error Log - Triage",
        new[]
        {
            new FormSection("Triage", new[]
            {
                Field("hx_name", "Name", FormControlClass.Text),
                Field("hx_sourcecomponent", "Source Component", FormControlClass.OptionSet),
                Field("hx_stage", "Stage", FormControlClass.Text),
                Field("hx_servicerequest", "Service Request", FormControlClass.Lookup),
                Field("hx_correlationid", "Correlation ID", FormControlClass.Text),
                Field("hx_resolved", "Resolved", FormControlClass.Boolean)
            }),
            new FormSection("Error Detail", new[]
            {
                Field("hx_message", "Message", FormControlClass.Memo),
                Field("hx_technicaldetail", "Technical Detail", FormControlClass.Memo),
                Field("hx_payload", "Payload", FormControlClass.Memo)
            })
        },
        new[]
        {
            Field("hx_sourcecomponent", "Source", FormControlClass.OptionSet),
            Field("hx_stage", "Stage", FormControlClass.Text),
            Field("hx_resolved", "Resolved", FormControlClass.Boolean)
        });

    DeleteObsoleteMainViewDuplicates(service);

    EnsureSystemView(
        service,
        "hx_servicerequest",
        "Coordinator Queue",
        new[]
        {
            "hx_confirmationnumber",
            "hx_title",
            "hx_servicecategory",
            "hx_severity",
            "hx_priority",
            "hx_assigneddepartment",
            "hx_lifecyclestatus",
            "hx_approvalstatus",
            "hx_integrationsyncstatus",
            "hx_duedate",
            "createdon"
        },
        "<condition attribute='statecode' operator='eq' value='0' />",
        "hx_duedate",
        descending: false);

    EnsureSystemView(
        service,
        "hx_servicerequest",
        "Pending Manager Approval",
        new[]
        {
            "hx_confirmationnumber",
            "hx_title",
            "hx_servicecategory",
            "hx_severity",
            "hx_priority",
            "hx_assigneddepartment",
            "hx_approvalstatus",
            "hx_duedate",
            "createdon"
        },
        string.Join(Environment.NewLine + "      ", new[]
        {
            "<condition attribute='statecode' operator='eq' value='0' />",
            "<condition attribute='hx_requiresapproval' operator='eq' value='1' />",
            "<condition attribute='hx_approvalstatus' operator='eq' value='752630001' />"
        }),
        "createdon",
        descending: false);

    EnsureSystemView(
        service,
        "hx_servicerequest",
        "Critical Documentation Guardrails",
        new[]
        {
            "hx_confirmationnumber",
            "hx_title",
            "hx_severity",
            "hx_priority",
            "hx_lifecyclestatus",
            "hx_resolutiondocumentationrequired",
            "hx_resolutiondocumentationprovided",
            "hx_assigneddepartment",
            "modifiedon"
        },
        string.Join(Environment.NewLine + "      ", new[]
        {
            "<condition attribute='statecode' operator='eq' value='0' />",
            "<condition attribute='hx_resolutiondocumentationrequired' operator='eq' value='1' />",
            "<condition attribute='hx_resolutiondocumentationprovided' operator='eq' value='0' />"
        }),
        "modifiedon",
        descending: true);

    EnsureSystemView(
        service,
        "hx_servicerequest",
        "ERP Sync Monitor",
        new[]
        {
            "hx_confirmationnumber",
            "hx_title",
            "hx_approvalstatus",
            "hx_integrationsyncstatus",
            "hx_externalerpid",
            "hx_assigneddepartment",
            "createdon",
            "modifiedon"
        },
        string.Join(Environment.NewLine + "      ", new[]
        {
            "<condition attribute='statecode' operator='eq' value='0' />",
            "<filter type='or'>",
            "  <condition attribute='hx_requiresapproval' operator='eq' value='1' />",
            "  <condition attribute='hx_integrationsyncstatus' operator='eq' value='752630003' />",
            "</filter>"
        }),
        "modifiedon",
        descending: true);

    EnsureSystemView(
        service,
        "hx_servicedocument",
        "Request Documents - Review",
        new[]
        {
            "hx_name",
            "hx_servicerequest",
            "hx_documenttype",
            "hx_filename",
            "hx_verified",
            "createdon",
            "ownerid"
        },
        "<condition attribute='statecode' operator='eq' value='0' />",
        "createdon",
        descending: true);

    EnsureSystemView(
        service,
        "hx_servicedocument",
        "Active Service Request Documents",
        new[]
        {
            "hx_name",
            "hx_servicerequest",
            "hx_documenttype",
            "hx_filename",
            "hx_verified",
            "createdon",
            "ownerid"
        },
        "<condition attribute='statecode' operator='eq' value='0' />",
        "createdon",
        descending: true);

    EnsureSystemView(
        service,
        "hx_servicedocument",
        "Service Request Document Associated View",
        new[]
        {
            "hx_name",
            "hx_servicerequest",
            "hx_documenttype",
            "hx_filename",
            "hx_verified",
            "createdon",
            "ownerid"
        },
        "<condition attribute='statecode' operator='eq' value='0' />",
        "createdon",
        descending: true,
        queryType: 2);

    EnsureSystemView(
        service,
        "hx_servicedocument",
        "Service Request Document Lookup View",
        new[]
        {
            "hx_name",
            "hx_servicerequest",
            "hx_documenttype",
            "hx_filename",
            "createdon"
        },
        "<condition attribute='statecode' operator='eq' value='0' />",
        "hx_name",
        descending: false,
        queryType: 64);

    EnsureSystemView(
        service,
        "hx_routingrule",
        "Active Routing Rules",
        new[]
        {
            "hx_sortorder",
            "hx_name",
            "hx_servicecategory",
            "hx_matchseverity",
            "hx_matchpriority",
            "hx_department",
            "hx_slapolicy",
            "hx_requiresapproval",
            "hx_resolutiondocumentationrequired",
            "hx_active"
        },
        "<condition attribute='hx_active' operator='eq' value='1' />",
        "hx_sortorder",
        descending: false);

    EnsureSystemView(
        service,
        "hx_routingrule",
        "Active Routing / SLA Rules",
        new[]
        {
            "hx_sortorder",
            "hx_name",
            "hx_servicecategory",
            "hx_matchseverity",
            "hx_matchpriority",
            "hx_department",
            "hx_slapolicy",
            "hx_requiresapproval",
            "hx_resolutiondocumentationrequired",
            "hx_active"
        },
        "<condition attribute='hx_active' operator='eq' value='1' />",
        "hx_sortorder",
        descending: false);

    EnsureSystemView(
        service,
        "hx_routingrule",
        "Routing / SLA Rule Associated View",
        new[]
        {
            "hx_sortorder",
            "hx_name",
            "hx_servicecategory",
            "hx_matchseverity",
            "hx_matchpriority",
            "hx_department",
            "hx_slapolicy",
            "hx_requiresapproval",
            "hx_resolutiondocumentationrequired",
            "hx_active"
        },
        "<condition attribute='hx_active' operator='eq' value='1' />",
        "hx_sortorder",
        descending: false,
        queryType: 2);

    EnsureSystemView(
        service,
        "hx_routingrule",
        "Routing / SLA Rule Lookup View",
        new[]
        {
            "hx_name",
            "hx_servicecategory",
            "hx_matchseverity",
            "hx_matchpriority",
            "hx_department",
            "hx_slapolicy",
            "hx_active"
        },
        "<condition attribute='hx_active' operator='eq' value='1' />",
        "hx_name",
        descending: false,
        queryType: 64);

    EnsureSystemView(
        service,
        "hx_department",
        "Active Departments",
        new[]
        {
            "hx_name",
            "hx_code",
            "hx_manageremail",
            "hx_active",
            "modifiedon"
        },
        "<condition attribute='hx_active' operator='eq' value='1' />",
        "hx_name",
        descending: false);

    EnsureSystemView(
        service,
        "hx_slapolicy",
        "Active SLA Policies",
        new[]
        {
            "hx_name",
            "hx_responsehours",
            "hx_resolutionhours",
            "hx_active",
            "modifiedon"
        },
        "<condition attribute='hx_active' operator='eq' value='1' />",
        "hx_name",
        descending: false);

    EnsureSystemView(
        service,
        "hx_servicecategory",
        "Active Service Categories",
        new[]
        {
            "hx_name",
            "hx_code",
            "hx_defaultdocumentationrequired",
            "hx_active",
            "modifiedon"
        },
        "<condition attribute='hx_active' operator='eq' value='1' />",
        "hx_name",
        descending: false);

    EnsureSystemView(
        service,
        "hx_errorlog",
        "Open Integration and Automation Errors",
        new[]
        {
            "hx_name",
            "hx_sourcecomponent",
            "hx_stage",
            "hx_servicerequest",
            "hx_correlationid",
            "hx_resolved",
            "createdon"
        },
        "<condition attribute='hx_resolved' operator='eq' value='0' />",
        "createdon",
        descending: true);

    EnsureSystemView(
        service,
        "hx_errorlog",
        "All System Error Logs",
        new[]
        {
            "hx_name",
            "hx_sourcecomponent",
            "hx_stage",
            "hx_servicerequest",
            "hx_correlationid",
            "hx_resolved",
            "createdon"
        },
        null,
        "createdon",
        descending: true);

    EnsureSystemView(
        service,
        "hx_errorlog",
        "Active System Error Logs",
        new[]
        {
            "hx_name",
            "hx_sourcecomponent",
            "hx_stage",
            "hx_servicerequest",
            "hx_correlationid",
            "hx_resolved",
            "createdon"
        },
        "<condition attribute='statecode' operator='eq' value='0' />",
        "createdon",
        descending: true);

    EnsureSystemView(
        service,
        "hx_errorlog",
        "System Error Log Associated View",
        new[]
        {
            "hx_name",
            "hx_sourcecomponent",
            "hx_stage",
            "hx_servicerequest",
            "hx_correlationid",
            "hx_resolved",
            "createdon"
        },
        "<condition attribute='statecode' operator='eq' value='0' />",
        "createdon",
        descending: true,
        queryType: 2);

    EnsureSystemView(
        service,
        "hx_errorlog",
        "System Error Log Lookup View",
        new[]
        {
            "hx_name",
            "hx_sourcecomponent",
            "hx_stage",
            "hx_servicerequest",
            "hx_resolved",
            "createdon"
        },
        "<condition attribute='statecode' operator='eq' value='0' />",
        "hx_name",
        descending: false,
        queryType: 64);

    EnsureSystemView(
        service,
        "hx_externalsynclog",
        "ERP Sync Attempts",
        new[]
        {
            "hx_name",
            "hx_servicerequest",
            "hx_syncstatus",
            "hx_endpointname",
            "hx_externalid",
            "hx_attemptedon",
            "createdon"
        },
        null,
        "hx_attemptedon",
        descending: true);

    EnsureSystemView(
        service,
        "hx_externalsynclog",
        "Active External Sync Logs",
        new[]
        {
            "hx_name",
            "hx_servicerequest",
            "hx_syncstatus",
            "hx_endpointname",
            "hx_externalid",
            "hx_attemptedon",
            "createdon"
        },
        "<condition attribute='statecode' operator='eq' value='0' />",
        "hx_attemptedon",
        descending: true);

    EnsureSystemView(
        service,
        "hx_externalsynclog",
        "External Sync Log Associated View",
        new[]
        {
            "hx_name",
            "hx_servicerequest",
            "hx_syncstatus",
            "hx_endpointname",
            "hx_externalid",
            "hx_attemptedon",
            "createdon"
        },
        "<condition attribute='statecode' operator='eq' value='0' />",
        "hx_attemptedon",
        descending: true,
        queryType: 2);

    EnsureSystemView(
        service,
        "hx_externalsynclog",
        "External Sync Log Lookup View",
        new[]
        {
            "hx_name",
            "hx_servicerequest",
            "hx_syncstatus",
            "hx_externalid",
            "createdon"
        },
        "<condition attribute='statecode' operator='eq' value='0' />",
        "hx_name",
        descending: false,
        queryType: 64);

    EnsureServiceIntakeDashboards(service);

    Console.WriteLine("Configured model-driven forms and views.");
}

static FormField Field(
    string logicalName,
    string label,
    FormControlClass controlClass,
    bool disabled = false) => new(logicalName, label, controlClass, disabled, false);

static FormField SlaPcfField(
    string logicalName,
    string label,
    FormControlClass controlClass,
    bool disabled = true) => new(logicalName, label, controlClass, disabled, true);

static void EnsureMainForm(
    IOrganizationService service,
    string entityLogicalName,
    string existingFormName,
    string targetFormName,
    IReadOnlyList<FormSection> sections,
    IReadOnlyList<FormField> headerFields)
{
    var objectTypeCode = GetObjectTypeCode(service, entityLogicalName);
    var form = FindMainForm(service, objectTypeCode, existingFormName) ?? FindMainForm(service, objectTypeCode, targetFormName);
    if (form == null)
    {
        Console.WriteLine($"Skipped form configuration for {entityLogicalName}; no main form found.");
        return;
    }

    var update = new Entity("systemform", form.Id)
    {
        ["name"] = targetFormName,
        ["formxml"] = BuildMainFormXml(entityLogicalName, targetFormName, sections, headerFields)
    };
    service.Update(update);
    AddToSolution(service, form.Id, 60);
}

static int GetObjectTypeCode(IOrganizationService service, string entityLogicalName)
{
    var response = (RetrieveEntityResponse)service.Execute(new RetrieveEntityRequest
    {
        LogicalName = entityLogicalName,
        EntityFilters = EntityFilters.Entity
    });

    return response.EntityMetadata.ObjectTypeCode
        ?? throw new InvalidOperationException($"Object type code not found for {entityLogicalName}.");
}

static Entity? FindMainForm(IOrganizationService service, int objectTypeCode, string name)
{
    var query = new QueryExpression("systemform")
    {
        ColumnSet = new ColumnSet("formid", "name", "formxml"),
        TopCount = 1
    };
    query.Criteria.AddCondition("objecttypecode", ConditionOperator.Equal, objectTypeCode);
    query.Criteria.AddCondition("type", ConditionOperator.Equal, 2);
    query.Criteria.AddCondition("name", ConditionOperator.Equal, name);
    return service.RetrieveMultiple(query).Entities.FirstOrDefault();
}

static string BuildMainFormXml(
    string entityLogicalName,
    string formName,
    IReadOnlyList<FormSection> sections,
    IReadOnlyList<FormField> headerFields)
{
    var formId = DeterministicGuid($"{entityLogicalName}:{formName}:form");
    var builder = new StringBuilder();
    builder.AppendLine("<form headerdensity=\"HighWithControls\">");
    builder.AppendLine("  <tabs>");
    builder.AppendLine($"    <tab verticallayout=\"true\" id=\"{{{DeterministicGuid($"{entityLogicalName}:summary-tab")}}}\" name=\"tab_summary\" IsUserDefined=\"1\" showlabel=\"true\">");
    builder.AppendLine("      <labels><label description=\"Summary\" languagecode=\"1033\" /></labels>");
    builder.AppendLine("      <columns><column width=\"100%\"><sections>");

    foreach (var section in sections)
    {
        builder.AppendLine($"        <section id=\"{{{DeterministicGuid($"{entityLogicalName}:{section.Name}")}}}\" name=\"{XmlName(section.Name)}\" IsUserDefined=\"1\" showlabel=\"true\" showbar=\"false\" layout=\"varwidth\" columns=\"1\" labelwidth=\"160\">");
        builder.AppendLine($"          <labels><label description=\"{Xml(section.Name)}\" languagecode=\"1033\" /></labels>");
        builder.AppendLine("          <rows>");
        foreach (var field in section.Fields)
        {
            AppendCell(builder, entityLogicalName, field, indent: "            ");
        }
        builder.AppendLine("          </rows>");
        builder.AppendLine("        </section>");
    }

    builder.AppendLine("      </sections></column></columns>");
    builder.AppendLine("    </tab>");
    builder.AppendLine("  </tabs>");
    var headerColumns = new string('1', Math.Max(1, headerFields.Count));
    builder.AppendLine($"  <header id=\"{{{DeterministicGuid($"{entityLogicalName}:header")}}}\" celllabelposition=\"Top\" columns=\"{headerColumns}\" labelwidth=\"115\" celllabelalignment=\"Left\">");
    builder.AppendLine("    <rows><row>");
    foreach (var field in headerFields)
    {
        AppendCell(builder, entityLogicalName, field, indent: "      ", includeRow: false);
    }
    builder.AppendLine("    </row></rows>");
    builder.AppendLine("  </header>");
    builder.AppendLine($"  <footer id=\"{{{DeterministicGuid($"{entityLogicalName}:footer")}}}\" celllabelposition=\"Top\" columns=\"111\" labelwidth=\"115\" celllabelalignment=\"Left\"><rows><row /></rows></footer>");
    AppendPcfControlDescriptions(builder, entityLogicalName, sections.SelectMany(section => section.Fields));
    builder.AppendLine("  <DisplayConditions Order=\"0\" FallbackForm=\"true\"><Everyone /></DisplayConditions>");
    builder.AppendLine("</form>");
    _ = formId;
    return builder.ToString();
}

static void AppendCell(
    StringBuilder builder,
    string entityLogicalName,
    FormField field,
    string indent,
    bool includeRow = true)
{
    if (includeRow)
    {
        builder.AppendLine($"{indent}<row>");
        indent += "  ";
    }

    var disabled = field.Disabled ? " disabled=\"true\"" : string.Empty;
    var uniqueId = field.UseSlaPcf
        ? $" uniqueid=\"{{{DeterministicGuid($"{entityLogicalName}:{field.LogicalName}:sla-pcf")}}}\""
        : string.Empty;
    builder.AppendLine($"{indent}<cell id=\"{{{DeterministicGuid($"{entityLogicalName}:{field.LogicalName}:cell")}}}\" showlabel=\"true\">");
    builder.AppendLine($"{indent}  <labels><label description=\"{Xml(field.Label)}\" languagecode=\"1033\" /></labels>");
    builder.AppendLine($"{indent}  <control id=\"{Xml(field.LogicalName)}\" classid=\"{field.ControlClass.Id}\" datafieldname=\"{Xml(field.LogicalName)}\"{disabled}{uniqueId} />");
    builder.AppendLine($"{indent}</cell>");

    if (includeRow)
    {
        builder.AppendLine($"{indent[..^2]}</row>");
    }
}

static void AppendPcfControlDescriptions(StringBuilder builder, string entityLogicalName, IEnumerable<FormField> fields)
{
    var pcfFields = fields.Where(field => field.UseSlaPcf).ToList();
    if (pcfFields.Count == 0)
    {
        return;
    }

    builder.AppendLine("  <controlDescriptions>");
    foreach (var field in pcfFields)
    {
        var controlId = DeterministicGuid($"{entityLogicalName}:{field.LogicalName}:sla-pcf");
        builder.AppendLine($"    <controlDescription forControl=\"{{{controlId}}}\">");
        foreach (var formFactor in new[] { 0, 1, 2 })
        {
            builder.AppendLine($"      <customControl formFactor=\"{formFactor}\" name=\"hx_HelloX.ServiceIntake.SlaStatusIndicator\">");
            builder.AppendLine("        <parameters>");
            builder.AppendLine($"          <statusText>{Xml(field.LogicalName)}</statusText>");
            builder.AppendLine("          <severity>hx_visualseverity</severity>");
            builder.AppendLine("          <slaDueOn>hx_duedate</slaDueOn>");
            builder.AppendLine("          <requiresApproval>hx_requiresapproval</requiresApproval>");
            builder.AppendLine("        </parameters>");
            builder.AppendLine("      </customControl>");
        }
        builder.AppendLine("    </controlDescription>");
    }
    builder.AppendLine("  </controlDescriptions>");
}

static Guid EnsureSystemView(
    IOrganizationService service,
    string entityLogicalName,
    string viewName,
    IReadOnlyList<string> columns,
    string? filterConditionXml,
    string orderBy,
    bool descending,
    int queryType = 0)
{
    var objectTypeCode = GetObjectTypeCode(service, entityLogicalName);
    var existing = FindSystemView(service, objectTypeCode, viewName, queryType);
    var savedQuery = existing ?? new Entity("savedquery");
    savedQuery["name"] = viewName;
    savedQuery["fetchxml"] = BuildViewFetchXml(entityLogicalName, columns, filterConditionXml, orderBy, descending);
    savedQuery["layoutxml"] = BuildViewLayoutXml(entityLogicalName, columns);

    if (existing == null)
    {
        savedQuery["returnedtypecode"] = objectTypeCode;
        savedQuery["querytype"] = queryType;
        var id = service.Create(savedQuery);
        AddToSolution(service, id, 26);
        return id;
    }
    else
    {
        service.Update(savedQuery);
        AddToSolution(service, savedQuery.Id, 26);
        return savedQuery.Id;
    }
}

static void DeleteObsoleteMainViewDuplicates(IOrganizationService service)
{
    var duplicateNames = new (string EntityLogicalName, string ViewName)[]
    {
        ("hx_servicedocument", "Service Request Document Associated View"),
        ("hx_servicedocument", "Service Request Document Lookup View"),
        ("hx_routingrule", "Routing / SLA Rule Associated View"),
        ("hx_routingrule", "Routing / SLA Rule Lookup View"),
        ("hx_externalsynclog", "External Sync Log Associated View"),
        ("hx_externalsynclog", "External Sync Log Lookup View"),
        ("hx_errorlog", "System Error Log Associated View"),
        ("hx_errorlog", "System Error Log Lookup View")
    };

    foreach (var duplicate in duplicateNames)
    {
        var objectTypeCode = GetObjectTypeCode(service, duplicate.EntityLogicalName);
        var query = new QueryExpression("savedquery")
        {
            ColumnSet = new ColumnSet("savedqueryid")
        };
        query.Criteria.AddCondition("returnedtypecode", ConditionOperator.Equal, objectTypeCode);
        query.Criteria.AddCondition("querytype", ConditionOperator.Equal, 0);
        query.Criteria.AddCondition("name", ConditionOperator.Equal, duplicate.ViewName);

        foreach (var view in service.RetrieveMultiple(query).Entities)
        {
            service.Delete("savedquery", view.Id);
            Console.WriteLine($"Deleted duplicate main view: {duplicate.ViewName}");
        }
    }
}

static Entity? FindSystemView(IOrganizationService service, int objectTypeCode, string viewName, int queryType)
{
    var query = new QueryExpression("savedquery")
    {
        ColumnSet = new ColumnSet("savedqueryid"),
        TopCount = 1
    };
    query.Criteria.AddCondition("returnedtypecode", ConditionOperator.Equal, objectTypeCode);
    query.Criteria.AddCondition("querytype", ConditionOperator.Equal, queryType);
    query.Criteria.AddCondition("name", ConditionOperator.Equal, viewName);
    return service.RetrieveMultiple(query).Entities.FirstOrDefault();
}

static Guid FindRequiredSystemViewId(IOrganizationService service, string entityLogicalName, string viewName)
{
    var objectTypeCode = GetObjectTypeCode(service, entityLogicalName);
    var view = FindSystemView(service, objectTypeCode, viewName, queryType: 0)
        ?? throw new InvalidOperationException($"Required view was not found: {entityLogicalName}/{viewName}");
    return view.Id;
}

static void EnsureServiceIntakeDashboards(IOrganizationService service)
{
    var coordinatorQueueView = FindRequiredSystemViewId(service, "hx_servicerequest", "Coordinator Queue");
    var pendingApprovalView = FindRequiredSystemViewId(service, "hx_servicerequest", "Pending Manager Approval");
    var criticalDocsView = FindRequiredSystemViewId(service, "hx_servicerequest", "Critical Documentation Guardrails");
    var erpMonitorView = FindRequiredSystemViewId(service, "hx_servicerequest", "ERP Sync Monitor");
    var syncAttemptsView = FindRequiredSystemViewId(service, "hx_externalsynclog", "ERP Sync Attempts");
    var errorLogView = FindRequiredSystemViewId(service, "hx_errorlog", "Open Integration and Automation Errors");

    var requestsByDepartment = EnsureSystemChart(
        service,
        "ESI - Requests by Department",
        "hx_servicerequest",
        BuildCountByAttributeChartData("hx_servicerequest", "hx_assigneddepartment", "hx_servicerequestid"),
        BuildSingleSeriesPresentation("Bar", includeLegend: false));
    var requestsBySeverity = EnsureSystemChart(
        service,
        "ESI - Requests by Severity",
        "hx_servicerequest",
        BuildCountByAttributeChartData("hx_servicerequest", "hx_severity", "hx_servicerequestid"),
        BuildSingleSeriesPresentation("Pie", includeLegend: true));
    var requestsByLifecycle = EnsureSystemChart(
        service,
        "ESI - Lifecycle Mix",
        "hx_servicerequest",
        BuildCountByAttributeChartData("hx_servicerequest", "hx_lifecyclestatus", "hx_servicerequestid"),
        BuildSingleSeriesPresentation("Column", includeLegend: false));
    var approvalOutcomes = EnsureSystemChart(
        service,
        "ESI - Approval Outcomes",
        "hx_servicerequest",
        BuildCountByAttributeChartData("hx_servicerequest", "hx_approvalstatus", "hx_servicerequestid"),
        BuildSingleSeriesPresentation("Column", includeLegend: false));
    var documentationQueue = EnsureSystemChart(
        service,
        "ESI - Documentation Guardrails",
        "hx_servicerequest",
        BuildCountByAttributeChartData("hx_servicerequest", "hx_resolutiondocumentationprovided", "hx_servicerequestid"),
        BuildSingleSeriesPresentation("Doughnut", includeLegend: true));
    var syncStatus = EnsureSystemChart(
        service,
        "ESI - ERP Sync Status",
        "hx_servicerequest",
        BuildCountByAttributeChartData("hx_servicerequest", "hx_integrationsyncstatus", "hx_servicerequestid"),
        BuildSingleSeriesPresentation("StackedColumn", includeLegend: true));
    var syncTrend = EnsureSystemChart(
        service,
        "ESI - Sync Attempts by Day",
        "hx_externalsynclog",
        BuildCountByDateChartData("hx_externalsynclog", "hx_attemptedon", "hx_externalsynclogid"),
        BuildSingleSeriesPresentation("Line", includeLegend: false));
    var errorsBySource = EnsureSystemChart(
        service,
        "ESI - Errors by Source",
        "hx_errorlog",
        BuildCountByAttributeChartData("hx_errorlog", "hx_sourcecomponent", "hx_errorlogid"),
        BuildSingleSeriesPresentation("Pie", includeLegend: true));

    var coordinatorDashboard = EnsureSystemDashboard(
        service,
        "ESI - Coordinator Operations Dashboard",
        "Coordinator queue, routing, severity, and SLA workbench.",
        BuildDashboardFormXml(
            "ESI - Coordinator Operations Dashboard",
            new[]
            {
                DashboardComponent.Chart("RequestsByDepartment", "Requests by department", "hx_servicerequest", coordinatorQueueView, requestsByDepartment),
                DashboardComponent.Chart("RequestsBySeverity", "Severity mix", "hx_servicerequest", coordinatorQueueView, requestsBySeverity),
                DashboardComponent.Chart("LifecycleMix", "Lifecycle mix", "hx_servicerequest", coordinatorQueueView, requestsByLifecycle)
            },
            new[]
            {
                DashboardComponent.Grid("CoordinatorQueue", "Coordinator queue", "hx_servicerequest", coordinatorQueueView),
                DashboardComponent.Grid("CriticalDocs", "Critical documentation queue", "hx_servicerequest", criticalDocsView)
            }));

    var managerDashboard = EnsureSystemDashboard(
        service,
        "ESI - Manager Approval Dashboard",
        "Manager view of pending approvals, approval outcomes, and critical documentation risk.",
        BuildDashboardFormXml(
            "ESI - Manager Approval Dashboard",
            new[]
            {
                DashboardComponent.Chart("ApprovalOutcomes", "Approval outcomes", "hx_servicerequest", pendingApprovalView, approvalOutcomes),
                DashboardComponent.Chart("PendingSeverity", "Pending approval severity", "hx_servicerequest", pendingApprovalView, requestsBySeverity),
                DashboardComponent.Chart("DocumentationGuardrails", "Documentation guardrails", "hx_servicerequest", criticalDocsView, documentationQueue)
            },
            new[]
            {
                DashboardComponent.Grid("PendingApprovals", "Pending manager approval", "hx_servicerequest", pendingApprovalView),
                DashboardComponent.Grid("CriticalDocsManager", "Critical documentation gaps", "hx_servicerequest", criticalDocsView)
            }));

    var integrationDashboard = EnsureSystemDashboard(
        service,
        "ESI - Integration Monitoring Dashboard",
        "ERP sync and automation support dashboard.",
        BuildDashboardFormXml(
            "ESI - Integration Monitoring Dashboard",
            new[]
            {
                DashboardComponent.Chart("SyncStatus", "ERP sync status", "hx_servicerequest", erpMonitorView, syncStatus),
                DashboardComponent.Chart("SyncTrend", "Sync attempts by day", "hx_externalsynclog", syncAttemptsView, syncTrend),
                DashboardComponent.Chart("ErrorsBySource", "Errors by source", "hx_errorlog", errorLogView, errorsBySource)
            },
            new[]
            {
                DashboardComponent.Grid("SyncAttempts", "Recent sync attempts", "hx_externalsynclog", syncAttemptsView),
                DashboardComponent.Grid("OpenErrors", "Open automation errors", "hx_errorlog", errorLogView)
            }));

    AddDashboardToAppModule(service, coordinatorDashboard);
    AddDashboardToAppModule(service, managerDashboard);
    AddDashboardToAppModule(service, integrationDashboard);
}

static Guid EnsureSystemChart(
    IOrganizationService service,
    string name,
    string entityLogicalName,
    string dataDescription,
    string presentationDescription)
{
    var existing = FindByAttribute(service, "savedqueryvisualization", "name", name);
    var chart = existing ?? new Entity("savedqueryvisualization");
    chart["name"] = name;
    chart["description"] = "Enterprise Service Intake demo dashboard chart.";
    chart["primaryentitytypecode"] = entityLogicalName;
    chart["datadescription"] = dataDescription;
    chart["presentationdescription"] = presentationDescription;
    chart["charttype"] = new OptionSetValue(0);
    chart["type"] = new OptionSetValue(0);
    chart["isdefault"] = false;

    if (existing == null)
    {
        var id = service.Create(chart);
        AddToSolution(service, id, 59);
        return id;
    }

    service.Update(chart);
    AddToSolution(service, chart.Id, 59);
    return chart.Id;
}

static string BuildCountByAttributeChartData(string entityLogicalName, string groupByAttribute, string countAttribute)
{
    return
        "<datadefinition>" +
        "<fetchcollection>" +
        "<fetch mapping=\"logical\" aggregate=\"true\">" +
        $"<entity name=\"{Xml(entityLogicalName)}\">" +
        $"<attribute name=\"{Xml(countAttribute)}\" aggregate=\"count\" alias=\"aggregate_column\" />" +
        $"<attribute name=\"{Xml(groupByAttribute)}\" groupby=\"true\" alias=\"groupby_column\" />" +
        "</entity>" +
        "</fetch>" +
        "</fetchcollection>" +
        "<categorycollection><category><measurecollection><measure alias=\"aggregate_column\" /></measurecollection></category></categorycollection>" +
        "</datadefinition>";
}

static string BuildCountByDateChartData(string entityLogicalName, string groupByAttribute, string countAttribute)
{
    return
        "<datadefinition>" +
        "<fetchcollection>" +
        "<fetch mapping=\"logical\" aggregate=\"true\">" +
        $"<entity name=\"{Xml(entityLogicalName)}\">" +
        $"<attribute name=\"{Xml(countAttribute)}\" aggregate=\"count\" alias=\"aggregate_column\" />" +
        $"<attribute name=\"{Xml(groupByAttribute)}\" groupby=\"true\" dategrouping=\"day\" alias=\"groupby_column\" />" +
        "<order alias=\"groupby_column\" descending=\"false\" />" +
        "</entity>" +
        "</fetch>" +
        "</fetchcollection>" +
        "<categorycollection><category><measurecollection><measure alias=\"aggregate_column\" /></measurecollection></category></categorycollection>" +
        "</datadefinition>";
}

static string BuildSingleSeriesPresentation(string chartType, bool includeLegend)
{
    var legend = includeLegend
        ? "<Legends><Legend Alignment=\"Center\" LegendStyle=\"Table\" Docking=\"Right\" IsEquallySpacedItems=\"True\" Font=\"{0}, 11px\" ForeColor=\"59,59,59\" /></Legends>"
        : string.Empty;

    return
        "<Chart Palette=\"None\" PaletteCustomColors=\"31,78,121; 14,118,110; 181,71,8; 180,35,24; 37,99,235; 92,110,128; 22,163,74\">" +
        "<Series>" +
        $"<Series ChartType=\"{Xml(chartType)}\" IsValueShownAsLabel=\"True\" Font=\"{{0}}, 9.5px\" LabelForeColor=\"59,59,59\" CustomProperties=\"PointWidth=0.75, MaxPixelPointWidth=40\"><SmartLabelStyle Enabled=\"True\" /></Series>" +
        "</Series>" +
        "<ChartAreas><ChartArea BorderColor=\"White\" BorderDashStyle=\"Solid\"><AxisY LineColor=\"165,172,181\"><MajorGrid LineColor=\"239,242,246\" /><LabelStyle Font=\"{0}, 10.5px\" ForeColor=\"59,59,59\" /></AxisY><AxisX LineColor=\"165,172,181\"><MajorGrid Enabled=\"False\" /><LabelStyle Font=\"{0}, 10.5px\" ForeColor=\"59,59,59\" /></AxisX><Area3DStyle Enable3D=\"False\" /></ChartArea></ChartAreas>" +
        legend +
        "<Titles><Title Alignment=\"TopLeft\" DockingOffset=\"-3\" Font=\"{0}, 13px\" ForeColor=\"59,59,59\" /></Titles>" +
        "</Chart>";
}

static Guid EnsureSystemDashboard(IOrganizationService service, string name, string description, string formXml)
{
    var existing = FindByAttribute(service, "systemform", "name", name);
    var dashboard = existing ?? new Entity("systemform");
    dashboard["name"] = name;
    dashboard["description"] = description;
    dashboard["formxml"] = formXml;
    dashboard["type"] = new OptionSetValue(0);
    dashboard["isdefault"] = false;
    dashboard["isdesktopenabled"] = true;
    dashboard["istabletenabled"] = true;

    if (existing == null)
    {
        var id = service.Create(dashboard);
        AddToSolution(service, id, 60);
        return id;
    }

    service.Update(dashboard);
    AddToSolution(service, dashboard.Id, 60);
    return dashboard.Id;
}

static string BuildDashboardFormXml(
    string dashboardName,
    IReadOnlyList<DashboardComponent> chartComponents,
    IReadOnlyList<DashboardComponent> gridComponents)
{
    var builder = new StringBuilder();
    builder.AppendLine("<form>");
    builder.AppendLine("  <tabs>");
    builder.AppendLine($"    <tab showlabel=\"true\" verticallayout=\"true\" id=\"{{{DeterministicGuid($"{dashboardName}:tab")}}}\" name=\"{XmlName(dashboardName)}\" locklevel=\"0\" expanded=\"true\">");
    builder.AppendLine($"      <labels><label description=\"{Xml(dashboardName)}\" languagecode=\"1033\" /></labels>");
    builder.AppendLine("      <columns><column width=\"100%\"><sections>");
    AppendDashboardSection(builder, dashboardName, "Visual filters", chartComponents);
    AppendDashboardSection(builder, dashboardName, "Action queues", gridComponents);
    builder.AppendLine("      </sections></column></columns>");
    builder.AppendLine("    </tab>");
    builder.AppendLine("  </tabs>");
    builder.AppendLine("</form>");
    return builder.ToString();
}

static void AppendDashboardSection(StringBuilder builder, string dashboardName, string sectionName, IReadOnlyList<DashboardComponent> components)
{
    var columns = new string('1', Math.Max(1, components.Count));
    builder.AppendLine($"        <section showlabel=\"true\" showbar=\"false\" columns=\"{columns}\" id=\"{{{DeterministicGuid($"{dashboardName}:{sectionName}:section")}}}\" name=\"{XmlName(sectionName)}\">");
    builder.AppendLine($"          <labels><label description=\"{Xml(sectionName)}\" languagecode=\"1033\" /></labels>");
    builder.AppendLine("          <rows>");
    builder.AppendLine("            <row>");
    foreach (var component in components)
    {
        AppendDashboardCell(builder, dashboardName, component);
    }
    builder.AppendLine("            </row>");
    for (var i = 0; i < 9; i++)
    {
        builder.AppendLine("            <row />");
    }
    builder.AppendLine("          </rows>");
    builder.AppendLine("        </section>");
}

static void AppendDashboardCell(StringBuilder builder, string dashboardName, DashboardComponent component)
{
    builder.AppendLine($"              <cell colspan=\"1\" rowspan=\"10\" showlabel=\"false\" id=\"{{{DeterministicGuid($"{dashboardName}:{component.Id}:cell")}}}\" auto=\"false\">");
    builder.AppendLine($"                <labels><label description=\"{Xml(component.Label)}\" languagecode=\"1033\" /></labels>");
    builder.AppendLine($"                <control id=\"{Xml(component.Id)}\" classid=\"{{E7A81278-8635-4d9e-8D4D-59480B391C5B}}\">");
    builder.AppendLine("                  <parameters>");
    builder.AppendLine($"                    <TargetEntityType>{Xml(component.TargetEntityLogicalName)}</TargetEntityType>");
    builder.AppendLine($"                    <ChartGridMode>{Xml(component.Mode)}</ChartGridMode>");
    builder.AppendLine("                    <EnableQuickFind>false</EnableQuickFind>");
    builder.AppendLine("                    <EnableViewPicker>true</EnableViewPicker>");
    builder.AppendLine("                    <EnableJumpBar>true</EnableJumpBar>");
    builder.AppendLine("                    <RecordsPerPage>8</RecordsPerPage>");
    builder.AppendLine($"                    <ViewId>{{{component.ViewId}}}</ViewId>");
    builder.AppendLine("                    <IsUserView>false</IsUserView>");
    builder.AppendLine("                    <ViewIds></ViewIds>");
    builder.AppendLine("                    <AutoExpand>Fixed</AutoExpand>");
    if (component.VisualizationId.HasValue)
    {
        builder.AppendLine($"                    <VisualizationId>{{{component.VisualizationId.Value}}}</VisualizationId>");
        builder.AppendLine("                    <IsUserChart>false</IsUserChart>");
        builder.AppendLine("                    <EnableChartPicker>false</EnableChartPicker>");
    }
    builder.AppendLine("                    <RelationshipName></RelationshipName>");
    builder.AppendLine("                  </parameters>");
    builder.AppendLine("                </control>");
    builder.AppendLine("              </cell>");
}

static void AddDashboardToAppModule(IOrganizationService service, Guid dashboardId)
{
    try
    {
        var app = FindByAttribute(service, "appmodule", "uniquename", "hx_EnterpriseServiceIntake");
        if (app == null)
        {
            return;
        }

        service.Execute(new AddAppComponentsRequest
        {
            AppId = app.Id,
            Components = new EntityReferenceCollection
            {
                new("systemform", dashboardId)
            }
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: dashboard {dashboardId} was not added to app module: {ex.Message}");
    }
}

static string BuildViewFetchXml(
    string entityLogicalName,
    IReadOnlyList<string> columns,
    string? filterConditionXml,
    string orderBy,
    bool descending)
{
    var builder = new StringBuilder();
    builder.AppendLine("<fetch version=\"1.0\" mapping=\"logical\">");
    builder.AppendLine($"  <entity name=\"{Xml(entityLogicalName)}\">");
    foreach (var column in columns)
    {
        builder.AppendLine($"    <attribute name=\"{Xml(column)}\" />");
    }
    builder.AppendLine($"    <order attribute=\"{Xml(orderBy)}\" descending=\"{descending.ToString().ToLowerInvariant()}\" />");
    if (!string.IsNullOrWhiteSpace(filterConditionXml))
    {
        builder.AppendLine("    <filter type=\"and\">");
        builder.AppendLine($"      {filterConditionXml}");
        builder.AppendLine("    </filter>");
    }
    builder.AppendLine("  </entity>");
    builder.AppendLine("</fetch>");
    return builder.ToString();
}

static string BuildViewLayoutXml(string entityLogicalName, IReadOnlyList<string> columns)
{
    var objectIdAttribute = $"{entityLogicalName}id";
    var builder = new StringBuilder();
    builder.AppendLine($"<grid name=\"resultset\" object=\"1\" jump=\"{Xml(columns.First())}\" select=\"1\" icon=\"1\" preview=\"1\">");
    builder.AppendLine($"  <row name=\"result\" id=\"{Xml(objectIdAttribute)}\">");
    foreach (var column in columns)
    {
        builder.AppendLine($"    <cell name=\"{Xml(column)}\" width=\"160\" />");
    }
    builder.AppendLine("  </row>");
    builder.AppendLine("</grid>");
    return builder.ToString();
}

static string DeterministicGuid(string input)
{
    var hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
    return new Guid(hash).ToString();
}

static string XmlName(string value)
{
    var safe = new string(value.Select(ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '_').ToArray());
    while (safe.Contains("__", StringComparison.Ordinal))
    {
        safe = safe.Replace("__", "_", StringComparison.Ordinal);
    }
    return safe.Trim('_');
}

static string Xml(string value) => System.Security.SecurityElement.Escape(value) ?? string.Empty;

static void RegisterPlugins(IOrganizationService service)
{
    var assemblyPath = Environment.GetEnvironmentVariable("PLUGIN_ASSEMBLY_PATH")
        ?? Path.Combine(Environment.CurrentDirectory, "src/plugins/ServiceIntake.Plugins/bin/Debug/net462/ServiceIntake.Plugins.dll");

    if (!File.Exists(assemblyPath))
    {
        Console.WriteLine($"Plugin registration skipped. Assembly not found: {assemblyPath}");
        return;
    }

    var assemblyId = UpsertPluginAssembly(service, assemblyPath);
    var routingTypeId = EnsurePluginType(service, assemblyId,
        "ServiceIntake.Plugins.ServiceRequestRoutingPlugin", "Service Request Routing Plugin");
    var closureTypeId = EnsurePluginType(service, assemblyId,
        "ServiceIntake.Plugins.ServiceRequestClosureGuardPlugin", "Service Request Closure Guard Plugin");

    EnsurePluginStep(service,
        name: "ESI - Route service request on create",
        messageName: "Create",
        primaryEntity: "hx_servicerequest",
        pluginTypeId: routingTypeId,
        filteringAttributes: null,
        imageAttributes: null);

    EnsurePluginStep(service,
        name: "ESI - Route service request on category/severity/priority update",
        messageName: "Update",
        primaryEntity: "hx_servicerequest",
        pluginTypeId: routingTypeId,
        filteringAttributes: "hx_servicecategory,hx_severity,hx_priority",
        imageAttributes: "hx_servicecategory,hx_severity,hx_priority");

    EnsurePluginStep(service,
        name: "ESI - Guard critical request closure",
        messageName: "Update",
        primaryEntity: "hx_servicerequest",
        pluginTypeId: closureTypeId,
        filteringAttributes: "hx_lifecyclestatus,hx_internalresolutionnotes,hx_resolutiondocumentationprovided",
        imageAttributes: "hx_severity,hx_resolutiondocumentationrequired,hx_resolutiondocumentationprovided,hx_internalresolutionnotes");

    Console.WriteLine("Registered plugins and steps.");
}

static Guid UpsertPluginAssembly(IOrganizationService service, string assemblyPath)
{
    var content = Convert.ToBase64String(File.ReadAllBytes(assemblyPath));
    var assemblyName = "ServiceIntake.Plugins";
    var existing = FindByAttribute(service, "pluginassembly", "name", assemblyName);

    var assembly = existing ?? new Entity("pluginassembly");
    assembly["name"] = assemblyName;
    assembly["content"] = content;
    assembly["sourcetype"] = new OptionSetValue(0);
    assembly["isolationmode"] = new OptionSetValue(2);
    assembly["version"] = "1.0.0.0";
    assembly["culture"] = "neutral";

    if (existing == null)
    {
        var id = service.Create(assembly);
        AddToSolution(service, id, 91);
        return id;
    }

    service.Update(assembly);
    AddToSolution(service, assembly.Id, 91);
    return assembly.Id;
}

static Guid EnsurePluginType(IOrganizationService service, Guid assemblyId, string typeName, string friendlyName)
{
    var query = new QueryExpression("plugintype")
    {
        ColumnSet = new ColumnSet("plugintypeid"),
        TopCount = 1
    };
    query.Criteria.AddCondition("typename", ConditionOperator.Equal, typeName);
    var existing = service.RetrieveMultiple(query).Entities.FirstOrDefault();
    if (existing != null)
    {
        return existing.Id;
    }

    var pluginType = new Entity("plugintype")
    {
        ["name"] = friendlyName,
        ["friendlyname"] = friendlyName,
        ["typename"] = typeName,
        ["pluginassemblyid"] = new EntityReference("pluginassembly", assemblyId)
    };
    var id = service.Create(pluginType);
    return id;
}

static void EnsurePluginStep(
    IOrganizationService service,
    string name,
    string messageName,
    string primaryEntity,
    Guid pluginTypeId,
    string? filteringAttributes,
    string? imageAttributes)
{
    var existing = FindByAttribute(service, "sdkmessageprocessingstep", "name", name);
    if (existing != null)
    {
        AddToSolution(service, existing.Id, 92);
        EnsurePreImage(service, existing.Id, imageAttributes);
        return;
    }

    var message = FindByAttribute(service, "sdkmessage", "name", messageName)
        ?? throw new InvalidOperationException($"SDK message not found: {messageName}");
    var filter = FindSdkMessageFilter(service, message.Id, primaryEntity)
        ?? throw new InvalidOperationException($"SDK message filter not found: {messageName}/{primaryEntity}");

    var step = new Entity("sdkmessageprocessingstep")
    {
        ["name"] = name,
        ["sdkmessageid"] = message.ToEntityReference(),
        ["sdkmessagefilterid"] = filter.ToEntityReference(),
        ["plugintypeid"] = new EntityReference("plugintype", pluginTypeId),
        ["stage"] = new OptionSetValue(20),
        ["mode"] = new OptionSetValue(0),
        ["rank"] = 1,
        ["supporteddeployment"] = new OptionSetValue(0),
        ["asyncautodelete"] = false
    };

    if (!string.IsNullOrWhiteSpace(filteringAttributes))
    {
        step["filteringattributes"] = filteringAttributes;
    }

    var stepId = service.Create(step);
    AddToSolution(service, stepId, 92);
    EnsurePreImage(service, stepId, imageAttributes);
}

static Entity? FindSdkMessageFilter(IOrganizationService service, Guid sdkMessageId, string primaryEntity)
{
    var query = new QueryExpression("sdkmessagefilter")
    {
        ColumnSet = new ColumnSet("sdkmessagefilterid"),
        TopCount = 1
    };
    query.Criteria.AddCondition("sdkmessageid", ConditionOperator.Equal, sdkMessageId);
    query.Criteria.AddCondition("primaryobjecttypecode", ConditionOperator.Equal, primaryEntity);
    return service.RetrieveMultiple(query).Entities.FirstOrDefault();
}

static void EnsurePreImage(IOrganizationService service, Guid stepId, string? attributes)
{
    if (string.IsNullOrWhiteSpace(attributes))
    {
        return;
    }

    var query = new QueryExpression("sdkmessageprocessingstepimage")
    {
        ColumnSet = new ColumnSet("sdkmessageprocessingstepimageid"),
        TopCount = 1
    };
    query.Criteria.AddCondition("sdkmessageprocessingstepid", ConditionOperator.Equal, stepId);
    query.Criteria.AddCondition("entityalias", ConditionOperator.Equal, "PreImage");
    var existing = service.RetrieveMultiple(query).Entities.FirstOrDefault();
    if (existing != null)
    {
        service.Delete("sdkmessageprocessingstepimage", existing.Id);
    }

    var image = new Entity("sdkmessageprocessingstepimage")
    {
        ["name"] = "PreImage",
        ["entityalias"] = "PreImage",
        ["imagetype"] = new OptionSetValue(0),
        ["messagepropertyname"] = "Target",
        ["attributes"] = attributes,
        ["sdkmessageprocessingstepid"] = new EntityReference("sdkmessageprocessingstep", stepId)
    };
    var imageId = service.Create(image);
    _ = imageId;
}

static void AddToSolution(IOrganizationService service, Guid componentId, int componentType)
{
    try
    {
        service.Execute(new AddSolutionComponentRequest
        {
            SolutionUniqueName = SolutionUniqueName,
            ComponentId = componentId,
            ComponentType = componentType,
            AddRequiredComponents = false
        });
    }
    catch (Exception ex)
    {
        if (!ex.Message.Contains("already", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"Warning: component {componentId} type {componentType} was not added to solution: {ex.Message}");
        }
    }
}

static void SeedSampleData(IOrganizationService service)
{
    var departments = new Dictionary<string, Guid>
    {
        ["Client Services"] = UpsertNamed(service, "hx_department", "Client Services", entity =>
        {
            entity["hx_code"] = "CLIENT";
            entity["hx_manageremail"] = "manager@hellosmart.ca";
            entity["hx_active"] = true;
        }),
        ["Research Operations"] = UpsertNamed(service, "hx_department", "Research Operations", entity =>
        {
            entity["hx_code"] = "RESEARCH";
            entity["hx_manageremail"] = "manager@hellosmart.ca";
            entity["hx_active"] = true;
        }),
        ["Finance"] = UpsertNamed(service, "hx_department", "Finance", entity =>
        {
            entity["hx_code"] = "FIN";
            entity["hx_manageremail"] = "manager@hellosmart.ca";
            entity["hx_active"] = true;
        }),
        ["IT Support"] = UpsertNamed(service, "hx_department", "IT Support", entity =>
        {
            entity["hx_code"] = "IT";
            entity["hx_manageremail"] = "manager@hellosmart.ca";
            entity["hx_active"] = true;
        }),
        ["General Intake"] = UpsertNamed(service, "hx_department", "General Intake", entity =>
        {
            entity["hx_code"] = "GENERAL";
            entity["hx_manageremail"] = "manager@hellosmart.ca";
            entity["hx_active"] = true;
        })
    };

    var categories = new Dictionary<string, Guid>
    {
        ["Funding Agreement"] = UpsertNamed(service, "hx_servicecategory", "Funding Agreement", entity =>
        {
            entity["hx_code"] = "FUNDING";
            entity["hx_active"] = true;
            entity["hx_defaultdocumentationrequired"] = true;
        }),
        ["Research Partnership"] = UpsertNamed(service, "hx_servicecategory", "Research Partnership", entity =>
        {
            entity["hx_code"] = "RESEARCH";
            entity["hx_active"] = true;
            entity["hx_defaultdocumentationrequired"] = true;
        }),
        ["Event Support"] = UpsertNamed(service, "hx_servicecategory", "Event Support", entity =>
        {
            entity["hx_code"] = "EVENT";
            entity["hx_active"] = true;
        }),
        ["Technical Support"] = UpsertNamed(service, "hx_servicecategory", "Technical Support", entity =>
        {
            entity["hx_code"] = "TECH";
            entity["hx_active"] = true;
        }),
        ["General Inquiry"] = UpsertNamed(service, "hx_servicecategory", "General Inquiry", entity =>
        {
            entity["hx_code"] = "GENERAL";
            entity["hx_active"] = true;
        })
    };

    var slaPolicies = new Dictionary<string, Guid>
    {
        ["Critical - 4 hour response"] = UpsertNamed(service, "hx_slapolicy", "Critical - 4 hour response", entity =>
        {
            entity["hx_responsehours"] = 4;
            entity["hx_resolutionhours"] = 24;
            entity["hx_active"] = true;
        }),
        ["High - 1 business day response"] = UpsertNamed(service, "hx_slapolicy", "High - 1 business day response", entity =>
        {
            entity["hx_responsehours"] = 8;
            entity["hx_resolutionhours"] = 48;
            entity["hx_active"] = true;
        }),
        ["Standard - 3 business day response"] = UpsertNamed(service, "hx_slapolicy", "Standard - 3 business day response", entity =>
        {
            entity["hx_responsehours"] = 24;
            entity["hx_resolutionhours"] = 120;
            entity["hx_active"] = true;
        }),
        ["Low - 5 business day response"] = UpsertNamed(service, "hx_slapolicy", "Low - 5 business day response", entity =>
        {
            entity["hx_responsehours"] = 40;
            entity["hx_resolutionhours"] = 240;
            entity["hx_active"] = true;
        })
    };

    UpsertRule(service, "Critical funding requests", 10, categories["Funding Agreement"], departments["Finance"],
        slaPolicies["Critical - 4 hour response"], 752630003, 752630003, requiresApproval: true, requiresDocs: true);
    UpsertRule(service, "High research partnership requests", 20, categories["Research Partnership"], departments["Research Operations"],
        slaPolicies["High - 1 business day response"], 752630002, 752630002, requiresApproval: true, requiresDocs: true);
    UpsertRule(service, "Technical support standard requests", 30, categories["Technical Support"], departments["IT Support"],
        slaPolicies["Standard - 3 business day response"], 752630001, 752630001, requiresApproval: false, requiresDocs: false);
    UpsertRule(service, "Event support standard requests", 40, categories["Event Support"], departments["Client Services"],
        slaPolicies["Standard - 3 business day response"], 752630001, 752630001, requiresApproval: false, requiresDocs: false);
    UpsertRule(service, "General low priority fallback", 90, categories["General Inquiry"], departments["General Intake"],
        slaPolicies["Low - 5 business day response"], 752630000, 752630000, requiresApproval: false, requiresDocs: false);

    var customerOneId = UpsertContact(service, "Customer One", "customer.one@example.com");
    var customerTwoId = UpsertContact(service, "Customer Two", "customer.two@example.com");
    var customerThreeId = UpsertContact(service, "Customer Three", "customer.three@example.com");
    var customerFourId = UpsertContact(service, "Customer Four", "customer.four@example.com");
    var customerFiveId = UpsertContact(service, "Customer Five", "customer.five@example.com");

    var criticalPendingId = UpsertDemoRequest(service,
        title: "Demo - Critical funding agreement support",
        contactId: customerOneId,
        categoryId: categories["Funding Agreement"],
        severity: 752630003,
        priority: 752630003,
        description: "Customer reports a funding agreement issue that blocks an active project milestone.",
        assignState: entity =>
        {
            entity["hx_lifecyclestatus"] = new OptionSetValue(752630003);
            entity["hx_approvalstatus"] = new OptionSetValue(752630001);
            entity["hx_integrationsyncstatus"] = new OptionSetValue(752630000);
            entity["hx_duedate"] = DateTime.UtcNow.AddHours(4);
            entity["hx_slaindicatorstatus"] = "Pending manager approval";
        });

    var technicalSupportId = UpsertDemoRequest(service,
        title: "Demo - Standard technical support",
        contactId: customerTwoId,
        categoryId: categories["Technical Support"],
        severity: 752630001,
        priority: 752630001,
        description: "Customer needs help accessing a portal resource.",
        assignState: entity =>
        {
            entity["hx_lifecyclestatus"] = new OptionSetValue(752630002);
            entity["hx_approvalstatus"] = new OptionSetValue(752630000);
            entity["hx_integrationsyncstatus"] = new OptionSetValue(752630000);
            entity["hx_duedate"] = DateTime.UtcNow.AddHours(20);
            entity["hx_slaindicatorstatus"] = "Ready for coordinator triage";
        });

    var syncedResearchId = UpsertDemoRequest(service,
        title: "Demo - Approved research partnership synced",
        contactId: customerThreeId,
        categoryId: categories["Research Partnership"],
        severity: 752630002,
        priority: 752630002,
        description: "Research partner request approved by management and synchronized to the external ERP.",
        assignState: entity =>
        {
            entity["hx_lifecyclestatus"] = new OptionSetValue(752630005);
            entity["hx_approvalstatus"] = new OptionSetValue(752630002);
            entity["hx_integrationsyncstatus"] = new OptionSetValue(752630002);
            entity["hx_externalerpid"] = "HX-ERP-DEMO-1001";
            entity["hx_resolutiondocumentationprovided"] = true;
            entity["hx_internalresolutionnotes"] = "Manager approval completed and ERP accepted the record.";
            entity["hx_duedate"] = DateTime.UtcNow.AddHours(-6);
            entity["hx_slaindicatorstatus"] = "ERP sync complete";
        });

    var rejectedResearchId = UpsertDemoRequest(service,
        title: "Demo - Rejected research exception",
        contactId: customerFourId,
        categoryId: categories["Research Partnership"],
        severity: 752630002,
        priority: 752630002,
        description: "Request was rejected because the required eligibility evidence was not provided.",
        assignState: entity =>
        {
            entity["hx_lifecyclestatus"] = new OptionSetValue(752630009);
            entity["hx_approvalstatus"] = new OptionSetValue(752630003);
            entity["hx_integrationsyncstatus"] = new OptionSetValue(752630000);
            entity["hx_customervisibleupdates"] = "Manager review completed. Additional eligibility evidence is required before resubmission.";
            entity["hx_slaindicatorstatus"] = "Rejected after manager review";
        });

    var failedSyncId = UpsertDemoRequest(service,
        title: "Demo - Approved funding ERP sync failure",
        contactId: customerFiveId,
        categoryId: categories["Funding Agreement"],
        severity: 752630003,
        priority: 752630003,
        description: "Approved funding request that demonstrates the Power Automate catch branch and error logging.",
        assignState: entity =>
        {
            entity["hx_lifecyclestatus"] = new OptionSetValue(752630004);
            entity["hx_approvalstatus"] = new OptionSetValue(752630002);
            entity["hx_integrationsyncstatus"] = new OptionSetValue(752630003);
            entity["hx_resolutiondocumentationprovided"] = true;
            entity["hx_internalresolutionnotes"] = "Approved for ERP sync; mock endpoint failure is retained for demo evidence.";
            entity["hx_duedate"] = DateTime.UtcNow.AddHours(-2);
            entity["hx_slaindicatorstatus"] = "ERP sync failed";
        });

    var eventSupportId = UpsertDemoRequest(service,
        title: "Demo - Event support in progress",
        contactId: customerFourId,
        categoryId: categories["Event Support"],
        severity: 752630001,
        priority: 752630001,
        description: "Customer needs event logistics support and does not require manager approval.",
        assignState: entity =>
        {
            entity["hx_lifecyclestatus"] = new OptionSetValue(752630006);
            entity["hx_approvalstatus"] = new OptionSetValue(752630000);
            entity["hx_integrationsyncstatus"] = new OptionSetValue(752630000);
            entity["hx_duedate"] = DateTime.UtcNow.AddHours(32);
            entity["hx_slaindicatorstatus"] = "Coordinator working request";
        });

    UpsertDemoDocument(service, "Demo Doc - Critical funding support evidence", criticalPendingId, 752630000,
        "funding-agreement-blocker.pdf", verified: true, notes: "Customer uploaded the supporting contract excerpt through the portal.");
    UpsertDemoDocument(service, "Demo Doc - Technical support screenshot", technicalSupportId, 752630000,
        "portal-access-error.png", verified: false, notes: "Screenshot supplied by the customer.");
    UpsertDemoDocument(service, "Demo Doc - Research manager approval", syncedResearchId, 752630002,
        "manager-approval-note.txt", verified: true, notes: "Approval captured by Power Automate.");
    UpsertDemoDocument(service, "Demo Doc - Research resolution package", syncedResearchId, 752630001,
        "erp-sync-resolution.pdf", verified: true, notes: "Resolution documentation required for critical closure guard evidence.");
    UpsertDemoDocument(service, "Demo Doc - Rejection evidence", rejectedResearchId, 752630002,
        "eligibility-review.txt", verified: true, notes: "Manager rejection note retained for audit.");
    UpsertDemoDocument(service, "Demo Doc - Failed ERP resolution evidence", failedSyncId, 752630001,
        "erp-sync-failure-resolution.txt", verified: true, notes: "Resolution documentation exists; integration error remains open.");
    UpsertDemoDocument(service, "Demo Doc - Event support brief", eventSupportId, 752630003,
        "event-support-brief.docx", verified: false, notes: "Coordinator will validate after triage.");

    UpsertDemoSyncLog(service, "Demo Sync - Research accepted by HelloX ERP", syncedResearchId, 752630002,
        "HelloX mock ERP", "HX-ERP-DEMO-1001", DateTime.UtcNow.AddHours(-5),
        "{\"confirmationNumber\":\"SR-20260520-00031\",\"title\":\"Approved research partnership synced\"}",
        "{\"ok\":true,\"externalId\":\"HX-ERP-DEMO-1001\",\"status\":\"accepted\"}");
    UpsertDemoSyncLog(service, "Demo Sync - Funding rejected by HelloX ERP", failedSyncId, 752630003,
        "HelloX mock ERP", string.Empty, DateTime.UtcNow.AddHours(-1),
        "{\"confirmationNumber\":\"SR-20260520-00044\",\"simulateFailure\":true}",
        "{\"ok\":false,\"error\":{\"code\":\"ERP_DEMO_REJECTION\"},\"retryable\":true}");
    UpsertDemoSyncLog(service, "Demo Sync - Pending approval not sent", criticalPendingId, 752630000,
        "HelloX mock ERP", string.Empty, DateTime.UtcNow.AddMinutes(-45),
        "{\"status\":\"held until approval\"}",
        "No API call made because manager approval is still pending.");

    UpsertDemoErrorLog(service, "Demo Error - ERP sync failure captured", failedSyncId, 752630003,
        "ERP Sync", "FLOW-DEMO-ERP-FAIL", "HelloX mock ERP returned the demo failure response.",
        "HTTP 503 from /api/esi-service-requests?fail=true", "{\"simulateFailure\":true}", resolved: false);
    UpsertDemoErrorLog(service, "Demo Error - Approval notification retry resolved", rejectedResearchId, 752630001,
        "Approval", "FLOW-DEMO-APPROVAL-RETRY", "Approval notification was delayed and then retried successfully.",
        "Approval connector transient timeout on first attempt.", "{\"approvalStatus\":\"Rejected\"}", resolved: true);

    Console.WriteLine("Seeded sample configuration and demo transaction data.");
}

static Guid UpsertNamed(IOrganizationService service, string logicalName, string name, Action<Entity> assign)
{
    var existing = FindByAttribute(service, logicalName, "hx_name", name);
    var entity = existing ?? new Entity(logicalName);
    entity["hx_name"] = name;
    assign(entity);

    if (existing == null)
    {
        return service.Create(entity);
    }

    service.Update(entity);
    return entity.Id;
}

static void UpsertRule(
    IOrganizationService service,
    string name,
    int sortOrder,
    Guid categoryId,
    Guid departmentId,
    Guid slaPolicyId,
    int severity,
    int priority,
    bool requiresApproval,
    bool requiresDocs)
{
    UpsertNamed(service, "hx_routingrule", name, entity =>
    {
        entity["hx_sortorder"] = sortOrder;
        entity["hx_active"] = true;
        entity["hx_servicecategory"] = new EntityReference("hx_servicecategory", categoryId);
        entity["hx_department"] = new EntityReference("hx_department", departmentId);
        entity["hx_slapolicy"] = new EntityReference("hx_slapolicy", slaPolicyId);
        entity["hx_matchseverity"] = new OptionSetValue(severity);
        entity["hx_matchpriority"] = new OptionSetValue(priority);
        entity["hx_requiresapproval"] = requiresApproval;
        entity["hx_resolutiondocumentationrequired"] = requiresDocs;
    });
}

static Guid UpsertContact(IOrganizationService service, string fullName, string email)
{
    var existing = FindByAttribute(service, "contact", "emailaddress1", email);
    var entity = existing ?? new Entity("contact");
    entity["firstname"] = fullName.Split(' ')[0];
    entity["lastname"] = string.Join(' ', fullName.Split(' ').Skip(1));
    entity["emailaddress1"] = email;

    if (existing == null)
    {
        return service.Create(entity);
    }
    else
    {
        service.Update(entity);
        return entity.Id;
    }
}

static Guid UpsertDemoRequest(
    IOrganizationService service,
    string title,
    Guid contactId,
    Guid categoryId,
    int severity,
    int priority,
    string description,
    Action<Entity>? assignState = null)
{
    var existing = FindByAttribute(service, "hx_servicerequest", "hx_title", title);
    var entity = existing ?? new Entity("hx_servicerequest");
    entity["hx_title"] = title;
    entity["hx_customercontact"] = new EntityReference("contact", contactId);
    entity["hx_servicecategory"] = new EntityReference("hx_servicecategory", categoryId);
    entity["hx_severity"] = new OptionSetValue(severity);
    entity["hx_priority"] = new OptionSetValue(priority);
    entity["hx_description"] = description;
    entity["hx_submittedon"] = DateTime.UtcNow;
    entity["hx_lifecyclestatus"] = new OptionSetValue(752630001);

    Guid id;
    if (existing == null)
    {
        id = service.Create(entity);
    }
    else
    {
        service.Update(entity);
        id = entity.Id;
    }

    if (assignState != null)
    {
        var stateUpdate = new Entity("hx_servicerequest", id);
        assignState(stateUpdate);
        service.Update(stateUpdate);
    }

    return id;
}

static void UpsertDemoDocument(
    IOrganizationService service,
    string name,
    Guid serviceRequestId,
    int documentType,
    string filename,
    bool verified,
    string notes)
{
    UpsertNamed(service, "hx_servicedocument", name, entity =>
    {
        entity["hx_servicerequest"] = new EntityReference("hx_servicerequest", serviceRequestId);
        entity["hx_documenttype"] = new OptionSetValue(documentType);
        entity["hx_filename"] = filename;
        entity["hx_verified"] = verified;
        entity["hx_notes"] = notes;
    });
}

static void UpsertDemoSyncLog(
    IOrganizationService service,
    string name,
    Guid serviceRequestId,
    int syncStatus,
    string endpointName,
    string externalId,
    DateTime attemptedOn,
    string requestPayload,
    string responseSummary)
{
    UpsertNamed(service, "hx_externalsynclog", name, entity =>
    {
        entity["hx_servicerequest"] = new EntityReference("hx_servicerequest", serviceRequestId);
        entity["hx_syncstatus"] = new OptionSetValue(syncStatus);
        entity["hx_endpointname"] = endpointName;
        entity["hx_externalid"] = externalId;
        entity["hx_attemptedon"] = attemptedOn;
        entity["hx_requestpayload"] = requestPayload;
        entity["hx_responsesummary"] = responseSummary;
    });
}

static void UpsertDemoErrorLog(
    IOrganizationService service,
    string name,
    Guid serviceRequestId,
    int sourceComponent,
    string stage,
    string correlationId,
    string message,
    string technicalDetail,
    string payload,
    bool resolved)
{
    UpsertNamed(service, "hx_errorlog", name, entity =>
    {
        entity["hx_servicerequest"] = new EntityReference("hx_servicerequest", serviceRequestId);
        entity["hx_sourcecomponent"] = new OptionSetValue(sourceComponent);
        entity["hx_stage"] = stage;
        entity["hx_correlationid"] = correlationId;
        entity["hx_message"] = message;
        entity["hx_technicaldetail"] = technicalDetail;
        entity["hx_payload"] = payload;
        entity["hx_resolved"] = resolved;
    });
}
static void RunValidationSmokeTests(IOrganizationService service)
{
    var fundingCategory = FindByAttribute(service, "hx_servicecategory", "hx_name", "Funding Agreement")
        ?? throw new InvalidOperationException("Funding Agreement category not found.");
    var customer = FindByAttribute(service, "contact", "emailaddress1", "customer.one@example.com")
        ?? throw new InvalidOperationException("Demo customer not found.");

    var test = new Entity("hx_servicerequest")
    {
        ["hx_title"] = $"Smoke Test - Critical closure guard {DateTime.UtcNow:yyyyMMddHHmmss}",
        ["hx_customercontact"] = customer.ToEntityReference(),
        ["hx_servicecategory"] = fundingCategory.ToEntityReference(),
        ["hx_severity"] = new OptionSetValue(752630003),
        ["hx_priority"] = new OptionSetValue(752630003),
        ["hx_description"] = "Temporary smoke test record for plugin validation.",
        ["hx_submittedon"] = DateTime.UtcNow,
        ["hx_lifecyclestatus"] = new OptionSetValue(752630001)
    };
    var id = service.Create(test);

    try
    {
        service.Update(new Entity("hx_servicerequest", id)
        {
            ["hx_lifecyclestatus"] = new OptionSetValue(752630008)
        });
        throw new InvalidOperationException("Closure guard smoke test failed: critical request closed without documentation.");
    }
    catch (Exception ex) when (ex.Message.Contains("Critical requests cannot be resolved or closed", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Closure guard smoke test passed: undocumented critical close was blocked.");
    }

    service.Update(new Entity("hx_servicerequest", id)
    {
        ["hx_internalresolutionnotes"] = "Validated the blocker and attached resolution evidence.",
        ["hx_resolutiondocumentationprovided"] = true,
        ["hx_lifecyclestatus"] = new OptionSetValue(752630008)
    });

    Console.WriteLine("Closure guard smoke test passed: documented critical close succeeded.");
}

static void EnsureConfirmationEmailFlow(IOrganizationService service)
{
    const string flowName = "ESI - Send Confirmation Email";
    EnsureConnectorConnectionReference(
        service,
        Office365ConnectionReferenceLogicalName,
        "Office 365 Outlook",
        Office365ConnectorId,
        Environment.GetEnvironmentVariable("POWERPLATFORM_OFFICE365_CONNECTION_ID"));

    var query = new QueryExpression("workflow")
    {
        ColumnSet = new ColumnSet("workflowid", "name", "clientdata", "statecode", "statuscode"),
        TopCount = 1
    };
    query.Criteria.AddCondition("name", ConditionOperator.Equal, flowName);

    var flow = service.RetrieveMultiple(query).Entities.FirstOrDefault();
    var clientData = BuildConfirmationEmailFlowClientData(service)
        .ToJsonString(new JsonSerializerOptions { WriteIndented = false });

    if (flow == null)
    {
        var created = new Entity("workflow")
        {
            ["name"] = flowName,
            ["category"] = new OptionSetValue(5),
            ["scope"] = new OptionSetValue(4),
            ["type"] = new OptionSetValue(1),
            ["mode"] = new OptionSetValue(0),
            ["ondemand"] = false,
            ["subprocess"] = false,
            ["primaryentity"] = "none",
            ["runas"] = new OptionSetValue(1),
            ["asyncautodelete"] = false,
            ["iscrmuiworkflow"] = false,
            ["istransacted"] = true,
            ["businessprocesstype"] = new OptionSetValue(0),
            ["clientdata"] = clientData
        };

        var id = service.Create(created);
        AddToSolution(service, id, 29);
        var saved = service.Retrieve("workflow", id, new ColumnSet("workflowid", "statecode", "statuscode"));
        ActivateFlow(service, saved);
        Console.WriteLine($"Created and activated cloud flow: {flowName}");
        return;
    }

    TryUpdateFlowDefinition(service, flow, new Entity("workflow", flow.Id)
    {
        ["clientdata"] = clientData
    });
    AddToSolution(service, flow.Id, 29);
    ActivateFlow(service, flow);
    Console.WriteLine($"Updated cloud flow: {flowName}");
}

static JsonObject BuildConfirmationEmailFlowClientData(IOrganizationService service)
{
    return new JsonObject
    {
        ["properties"] = new JsonObject
        {
            ["connectionReferences"] = BuildConfirmationEmailConnectionReferences(service),
            ["definition"] = new JsonObject
            {
                ["$schema"] = "https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#",
                ["contentVersion"] = "1.0.0.0",
                ["parameters"] = new JsonObject
                {
                    ["$authentication"] = new JsonObject
                    {
                        ["defaultValue"] = new JsonObject(),
                        ["type"] = "SecureObject"
                    },
                    ["$connections"] = new JsonObject
                    {
                        ["defaultValue"] = new JsonObject(),
                        ["type"] = "Object"
                    }
                },
                ["triggers"] = new JsonObject
                {
                    ["When_a_service_request_is_created"] = new JsonObject
                    {
                        ["type"] = "OpenApiConnectionWebhook",
                        ["inputs"] = DataverseInputs("SubscribeWebhookTrigger", new JsonObject
                        {
                            ["subscriptionRequest/message"] = 1,
                            ["subscriptionRequest/entityname"] = "hx_servicerequest",
                            ["subscriptionRequest/scope"] = 4
                        }, "shared_commondataserviceforapps")
                    }
                },
                ["actions"] = BuildConfirmationEmailActions(),
                ["outputs"] = new JsonObject()
            }
        },
        ["schemaVersion"] = "1.0.0.0"
    };
}

static JsonObject BuildConfirmationEmailActions()
{
    return new JsonObject
    {
        ["Try_-_send_confirmation_email"] = new JsonObject
        {
            ["runAfter"] = new JsonObject(),
            ["type"] = "Scope",
            ["actions"] = new JsonObject
            {
                ["Condition_-_applicant_contact_exists"] = new JsonObject
                {
                    ["type"] = "If",
                    ["expression"] = new JsonObject
                    {
                        ["and"] = new JsonArray(new JsonObject
                        {
                            ["equals"] = new JsonArray("@empty(triggerOutputs()?['body/_hx_customercontact_value'])", false)
                        })
                    },
                    ["actions"] = new JsonObject
                    {
                        ["Get_applicant_contact"] = DataverseGetAction(new JsonObject
                        {
                            ["entityName"] = "contacts",
                            ["recordId"] = "@triggerOutputs()?['body/_hx_customercontact_value']"
                        }),
                        ["Condition_-_applicant_email_exists"] = new JsonObject
                        {
                            ["runAfter"] = RunAfterSucceeded("Get_applicant_contact"),
                            ["type"] = "If",
                            ["expression"] = new JsonObject
                            {
                                ["and"] = new JsonArray(new JsonObject
                                {
                                    ["equals"] = new JsonArray("@empty(outputs('Get_applicant_contact')?['body/emailaddress1'])", false)
                                })
                            },
                            ["actions"] = new JsonObject
                            {
                                ["Send_confirmation_email"] = OutlookSendEmailV2Action(new JsonObject
                                {
                                    ["emailMessage/To"] = "@outputs('Get_applicant_contact')?['body/emailaddress1']",
                                    ["emailMessage/Subject"] = "Mitacs service request received - @{triggerOutputs()?['body/hx_confirmationnumber']}",
                                    ["emailMessage/Body"] = BuildConfirmationEmailBody(),
                                    ["emailMessage/Importance"] = "Normal"
                                }),
                                ["Mark_request_confirmation_sent"] = DataverseUpdateAction(new JsonObject
                                {
                                    ["entityName"] = "hx_servicerequests",
                                    ["recordId"] = "@triggerOutputs()?['body/hx_servicerequestid']",
                                    ["item/hx_customervisibleupdates"] = "Confirmation email sent to the applicant through Office 365 Outlook. Supporting files can be uploaded through the secure SharePoint upload page."
                                }, RunAfterSucceeded("Send_confirmation_email"))
                            },
                            ["else"] = new JsonObject
                            {
                                ["actions"] = new JsonObject
                                {
                                    ["Log_missing_applicant_email"] = BuildConfirmationEmailLogAction(
                                        "Confirmation email skipped - @{triggerOutputs()?['body/hx_confirmationnumber']}",
                                        "Applicant contact does not have an email address.",
                                        "@string(outputs('Get_applicant_contact')?['body'])")
                                }
                            }
                        }
                    },
                    ["else"] = new JsonObject
                    {
                        ["actions"] = new JsonObject
                        {
                            ["Log_missing_applicant_contact"] = BuildConfirmationEmailLogAction(
                                "Confirmation email skipped - @{triggerOutputs()?['body/hx_confirmationnumber']}",
                                "Service request has no applicant contact.",
                                "@string(triggerOutputs()?['body'])")
                        }
                    }
                }
            }
        },
        ["Catch_-_log_confirmation_email_error"] = new JsonObject
        {
            ["runAfter"] = new JsonObject
            {
                ["Try_-_send_confirmation_email"] = new JsonArray("Failed", "Skipped", "TimedOut")
            },
            ["type"] = "Scope",
            ["actions"] = new JsonObject
            {
                ["Create_confirmation_email_error_log"] = BuildConfirmationEmailLogAction(
                    "Confirmation email failure - @{triggerOutputs()?['body/hx_confirmationnumber']}",
                    "Power Automate could not send the confirmation email. See technical detail for the failed scope result.",
                    "@string(result('Try_-_send_confirmation_email'))")
            }
        }
    };
}

static JsonObject BuildConfirmationEmailLogAction(string name, string message, string technicalDetail)
{
    return DataverseCreateAction(new JsonObject
    {
        ["entityName"] = "hx_errorlogs",
        ["item/hx_name"] = name,
        ["item/hx_sourcecomponent"] = 752630001,
        ["item/hx_stage"] = "Confirmation email",
        ["item/hx_correlationid"] = "@workflow()?['run']?['name']",
        ["item/hx_message"] = message,
        ["item/hx_technicaldetail"] = technicalDetail,
        ["item/hx_payload"] = "@string(triggerOutputs()?['body'])",
        ["item/hx_resolved"] = false,
        ["item/hx_Servicerequest@odata.bind"] = "@triggerOutputs()?['body/hx_servicerequestid']"
    });
}

static string BuildConfirmationEmailBody()
{
    return """
<div style="font-family:Segoe UI,Arial,sans-serif;background:#f6f8fb;padding:24px;color:#1f2933;">
  <div style="max-width:640px;margin:0 auto;background:#ffffff;border:1px solid #d9e2ec;border-radius:8px;overflow:hidden;">
    <div style="background:#0067b1;padding:20px 24px;color:#ffffff;">
      <h1 style="font-size:22px;line-height:1.3;margin:0;">Service request received</h1>
    </div>
    <div style="padding:24px;">
      <p style="margin:0 0 16px 0;">Hello @{outputs('Get_applicant_contact')?['body/fullname']},</p>
      <p style="margin:0 0 18px 0;">Thank you for submitting your request. We have received it and assigned the confirmation number below.</p>
      <div style="border:1px solid #c8d3df;border-radius:6px;padding:16px;margin:18px 0;background:#f9fbfd;">
        <div style="font-size:12px;font-weight:600;letter-spacing:.04em;text-transform:uppercase;color:#52616f;">Confirmation number</div>
        <div style="font-size:24px;font-weight:700;color:#111827;margin-top:4px;">@{triggerOutputs()?['body/hx_confirmationnumber']}</div>
      </div>
      <table role="presentation" style="border-collapse:collapse;width:100%;margin:18px 0;">
        <tr>
          <td style="padding:8px 0;color:#52616f;width:150px;">Request title</td>
          <td style="padding:8px 0;color:#111827;font-weight:600;">@{triggerOutputs()?['body/hx_title']}</td>
        </tr>
        <tr>
          <td style="padding:8px 0;color:#52616f;">Submitted</td>
          <td style="padding:8px 0;color:#111827;">@{utcNow()}</td>
        </tr>
      </table>
      <p style="margin:18px 0 0 0;">If you need to add screenshots, forms, or other evidence, use the portal after submission to upload supporting files to the secure SharePoint folder for this request.</p>
      <p style="margin:20px 0 0 0;">Regards,<br/>Mitacs Service Intake</p>
    </div>
  </div>
</div>
""";
}

static JsonObject BuildConfirmationEmailConnectionReferences(IOrganizationService service)
{
    var references = BuildDataverseConnectionReferences(service);
    var currentFlowReferences = GetConnectionReferencesFromFlow(service, "ESI - Send Confirmation Email");
    references["shared_office365"] =
        CloneJson(currentFlowReferences?["shared_office365"]) ??
        BuildConnectorConnectionReference(Office365ConnectionReferenceLogicalName, "shared_office365");

    return references;
}

static void EnsureConnectorConnectionReference(
    IOrganizationService service,
    string logicalName,
    string displayName,
    string connectorId,
    string? connectionId)
{
    var existing = FindByAttribute(service, "connectionreference", "connectionreferencelogicalname", logicalName);
    if (existing == null)
    {
        var created = new Entity("connectionreference")
        {
            ["connectionreferencedisplayname"] = displayName,
            ["connectionreferencelogicalname"] = logicalName,
            ["connectorid"] = connectorId,
            ["iscustomizable"] = new BooleanManagedProperty(true),
            ["promptingbehavior"] = new OptionSetValue(0),
            ["statuscode"] = new OptionSetValue(1)
        };
        if (!string.IsNullOrWhiteSpace(connectionId))
        {
            created["connectionid"] = connectionId;
        }

        var id = service.Create(created);
        AddToSolution(service, id, 10210);
        Console.WriteLine($"Created connection reference: {displayName}");
        return;
    }

    var updated = new Entity("connectionreference", existing.Id);
    var hasUpdates = false;
    if (existing.GetAttributeValue<string>("connectionreferencedisplayname") != displayName)
    {
        updated["connectionreferencedisplayname"] = displayName;
        hasUpdates = true;
    }
    if (existing.GetAttributeValue<string>("connectorid") != connectorId)
    {
        updated["connectorid"] = connectorId;
        hasUpdates = true;
    }
    if (!string.IsNullOrWhiteSpace(connectionId) &&
        existing.GetAttributeValue<string>("connectionid") != connectionId)
    {
        updated["connectionid"] = connectionId;
        hasUpdates = true;
    }

    if (hasUpdates)
    {
        service.Update(updated);
    }
    AddToSolution(service, existing.Id, 10210);
}

static JsonObject BuildDataverseConnectionReferences(IOrganizationService service)
{
    var source = GetConnectionReferencesFromFlow(service, "ESI - Approval and ERP Sync");
    return new JsonObject
    {
        ["shared_commondataserviceforapps"] =
            CloneJson(source?["shared_commondataserviceforapps"]) ??
            BuildDataverseConnectionReference("new_sharedcommondataserviceforapps_30187"),
        ["shared_commondataserviceforapps-1"] =
            CloneJson(source?["shared_commondataserviceforapps-1"]) ??
            CloneJson(source?["shared_commondataserviceforapps"]) ??
            BuildDataverseConnectionReference("new_sharedcommondataserviceforapps_80dda")
    };
}

static JsonObject? GetConnectionReferencesFromFlow(IOrganizationService service, string flowName)
{
    var query = new QueryExpression("workflow")
    {
        ColumnSet = new ColumnSet("clientdata"),
        TopCount = 1
    };
    query.Criteria.AddCondition("name", ConditionOperator.Equal, flowName);

    var flow = service.RetrieveMultiple(query).Entities.FirstOrDefault();
    var clientData = flow?.GetAttributeValue<string>("clientdata");
    if (string.IsNullOrWhiteSpace(clientData))
    {
        return null;
    }

    return JsonNode.Parse(clientData)?["properties"]?["connectionReferences"]?.AsObject();
}

static JsonObject BuildDataverseConnectionReference(string logicalName)
    => BuildConnectorConnectionReference(logicalName, "shared_commondataserviceforapps");

static JsonObject BuildConnectorConnectionReference(string logicalName, string apiName)
{
    return new JsonObject
    {
        ["runtimeSource"] = "embedded",
        ["connection"] = new JsonObject
        {
            ["connectionReferenceLogicalName"] = logicalName
        },
        ["api"] = new JsonObject
        {
            ["name"] = apiName
        }
    };
}

static JsonObject? CloneJson(JsonNode? node)
{
    return node == null
        ? null
        : JsonNode.Parse(node.ToJsonString())?.AsObject();
}

static void PatchApprovalFlowDefinition(IOrganizationService service)
{
    var query = new QueryExpression("workflow")
    {
        ColumnSet = new ColumnSet("workflowid", "name", "clientdata", "statecode", "statuscode"),
        TopCount = 1
    };
    query.Criteria.AddCondition("name", ConditionOperator.Equal, "ESI - Approval and ERP Sync");

    var flow = service.RetrieveMultiple(query).Entities.FirstOrDefault()
        ?? throw new InvalidOperationException("Cloud flow 'ESI - Approval and ERP Sync' was not found.");
    var clientData = flow.GetAttributeValue<string>("clientdata");
    if (string.IsNullOrWhiteSpace(clientData))
    {
        throw new InvalidOperationException("Cloud flow clientdata is empty.");
    }

    var root = JsonNode.Parse(clientData)?.AsObject()
        ?? throw new InvalidOperationException("Cloud flow clientdata is not valid JSON.");
    var actions = root["properties"]?["definition"]?["actions"]?.AsObject()
        ?? throw new InvalidOperationException("Cloud flow action definition was not found.");
    var tryScope = actions["Try_-_approval_and_ERP_sync"]?.AsObject()
        ?? throw new InvalidOperationException("Try scope was not found in the cloud flow definition.");
    var condition = tryScope["actions"]?["Condition"]?.AsObject()
        ?? throw new InvalidOperationException("Approval condition was not found in the cloud flow definition.");
    var elseActions = condition["else"]?["actions"]?.AsObject()
        ?? throw new InvalidOperationException("Approval condition false branch was not found.");
    var approvedActions = condition["actions"]?.AsObject()
        ?? throw new InvalidOperationException("Approval condition true branch was not found.");

    if (approvedActions["HTTP"] is JsonObject httpAction)
    {
        var inputs = httpAction["inputs"]?.AsObject()
            ?? throw new InvalidOperationException("HTTP action inputs were not found.");
        inputs["uri"] = HelloXMockErpEndpoint;
    }

    if (approvedActions["Add_a_new_row"] is JsonObject syncLogAction)
    {
        var parameters = syncLogAction["inputs"]?["parameters"]?.AsObject()
            ?? throw new InvalidOperationException("External Sync Log action parameters were not found.");
        parameters["item/hx_endpointname"] = "HelloX mock ERP";
    }

    if (!elseActions.ContainsKey("Update_request_as_rejected"))
    {
        elseActions["Update_request_as_rejected"] = DataverseUpdateAction(new JsonObject
        {
            ["entityName"] = "hx_servicerequests",
            ["recordId"] = "@triggerOutputs()?['body/hx_servicerequestid']",
            ["item/hx_approvalstatus"] = 752630003,
            ["item/hx_lifecyclestatus"] = 752630009,
            ["item/hx_customervisibleupdates"] = "Manager rejected the request. ERP synchronization was not attempted."
        });
    }

    if (!actions.ContainsKey("Catch_-_log_automation_error"))
    {
        actions["Catch_-_log_automation_error"] = new JsonObject
        {
            ["runAfter"] = new JsonObject
            {
                ["Try_-_approval_and_ERP_sync"] = new JsonArray("Failed", "Skipped", "TimedOut")
            },
            ["type"] = "Scope",
            ["actions"] = new JsonObject
            {
                ["Create_system_error_log"] = DataverseCreateAction(new JsonObject
                {
                    ["entityName"] = "hx_errorlogs",
                    ["item/hx_name"] = "Automation failure - @{triggerOutputs()?['body/hx_confirmationnumber']}",
                    ["item/hx_sourcecomponent"] = 752630001,
                    ["item/hx_stage"] = "Approval / ERP sync",
                    ["item/hx_correlationid"] = "@workflow()?['run']?['name']",
                    ["item/hx_message"] = "Power Automate approval or ERP sync failed. See technical detail for failed scope results.",
                    ["item/hx_technicaldetail"] = "@string(result('Try_-_approval_and_ERP_sync'))",
                    ["item/hx_payload"] = "@string(triggerOutputs()?['body'])",
                    ["item/hx_resolved"] = false,
                    ["item/hx_Servicerequest@odata.bind"] = "@triggerOutputs()?['body/hx_servicerequestid']"
                }),
                ["Mark_request_sync_failed"] = DataverseUpdateAction(new JsonObject
                {
                    ["entityName"] = "hx_servicerequests",
                    ["recordId"] = "@triggerOutputs()?['body/hx_servicerequestid']",
                    ["item/hx_approvalstatus"] = 752630004,
                    ["item/hx_integrationsyncstatus"] = 752630003,
                    ["item/hx_customervisibleupdates"] = "Automation failed during approval or ERP synchronization. Internal teams have been notified through the error log."
                }, new JsonObject
                {
                    ["Create_system_error_log"] = new JsonArray("Succeeded")
                })
            }
        };
    }

    var updated = new Entity("workflow", flow.Id)
    {
        ["clientdata"] = root.ToJsonString(new JsonSerializerOptions { WriteIndented = false })
    };

    TryUpdateFlowDefinition(service, flow, updated);
    Console.WriteLine("Patched cloud flow endpoint, false branch, and Catch error-log scope.");
}

static JsonObject DataverseCreateAction(JsonObject parameters, JsonObject? runAfter = null)
{
    var action = new JsonObject
    {
        ["type"] = "OpenApiConnection",
        ["inputs"] = DataverseInputs("CreateRecord", parameters)
    };
    if (runAfter != null)
    {
        action["runAfter"] = runAfter;
    }
    return action;
}

static JsonObject DataverseGetAction(JsonObject parameters, JsonObject? runAfter = null)
{
    var action = new JsonObject
    {
        ["type"] = "OpenApiConnection",
        ["inputs"] = DataverseInputs("GetItem", parameters)
    };
    if (runAfter != null)
    {
        action["runAfter"] = runAfter;
    }
    return action;
}

static JsonObject DataverseUpdateAction(JsonObject parameters, JsonObject? runAfter = null)
{
    var action = new JsonObject
    {
        ["type"] = "OpenApiConnection",
        ["inputs"] = DataverseInputs("UpdateOnlyRecord", parameters)
    };
    if (runAfter != null)
    {
        action["runAfter"] = runAfter;
    }
    return action;
}

static JsonObject OutlookSendEmailV2Action(JsonObject parameters, JsonObject? runAfter = null)
{
    var action = new JsonObject
    {
        ["type"] = "OpenApiConnection",
        ["inputs"] = new JsonObject
        {
            ["parameters"] = parameters,
            ["host"] = new JsonObject
            {
                ["apiId"] = "/providers/Microsoft.PowerApps/apis/shared_office365",
                ["operationId"] = "SendEmailV2",
                ["connectionName"] = "shared_office365"
            }
        }
    };
    if (runAfter != null)
    {
        action["runAfter"] = runAfter;
    }
    return action;
}

static JsonObject RunAfterSucceeded(string actionName) => new()
{
    [actionName] = new JsonArray("Succeeded")
};

static JsonObject DataverseInputs(string operationId, JsonObject parameters, string connectionName = "shared_commondataserviceforapps-1") => new()
{
    ["parameters"] = parameters,
    ["host"] = new JsonObject
    {
        ["apiId"] = "/providers/Microsoft.PowerApps/apis/shared_commondataserviceforapps",
        ["operationId"] = operationId,
        ["connectionName"] = connectionName
    }
};

static void ActivateFlow(IOrganizationService service, Entity flow)
{
    var state = flow.GetAttributeValue<OptionSetValue>("statecode")?.Value;
    var status = flow.GetAttributeValue<OptionSetValue>("statuscode")?.Value;
    if (state == 1 && status == 2)
    {
        return;
    }

    service.Execute(new SetStateRequest
    {
        EntityMoniker = flow.ToEntityReference(),
        State = new OptionSetValue(1),
        Status = new OptionSetValue(2)
    });
}

static void TryUpdateFlowDefinition(IOrganizationService service, Entity flow, Entity updated)
{
    try
    {
        service.Update(updated);
        return;
    }
    catch (Exception activeUpdateException)
    {
        Console.WriteLine($"Direct flow update failed, retrying via deactivate/update/activate: {activeUpdateException.Message}");
    }

    var reference = flow.ToEntityReference();
    service.Execute(new SetStateRequest
    {
        EntityMoniker = reference,
        State = new OptionSetValue(0),
        Status = new OptionSetValue(1)
    });
    service.Update(updated);
    service.Execute(new SetStateRequest
    {
        EntityMoniker = reference,
        State = new OptionSetValue(1),
        Status = new OptionSetValue(2)
    });
}

internal sealed record FormSection(string Name, IReadOnlyList<FormField> Fields);

internal sealed record FormField(string LogicalName, string Label, FormControlClass ControlClass, bool Disabled, bool UseSlaPcf);

internal sealed record DashboardComponent(
    string Id,
    string Label,
    string TargetEntityLogicalName,
    string Mode,
    Guid ViewId,
    Guid? VisualizationId)
{
    public static DashboardComponent Chart(string id, string label, string targetEntityLogicalName, Guid viewId, Guid visualizationId)
        => new(id, label, targetEntityLogicalName, "Chart", viewId, visualizationId);

    public static DashboardComponent Grid(string id, string label, string targetEntityLogicalName, Guid viewId)
        => new(id, label, targetEntityLogicalName, "Grid", viewId, null);
}

internal sealed record FormControlClass(string Id)
{
    public static readonly FormControlClass Text = new("{4273EDBD-AC1D-40D3-9FB2-095C621B552D}");
    public static readonly FormControlClass Memo = new("{E0DECE4B-6FC8-4A8F-A065-082708572369}");
    public static readonly FormControlClass Lookup = new("{270BD3DB-D9AF-4782-9025-509E298DEC0A}");
    public static readonly FormControlClass OptionSet = new("{3EF39988-22BB-4F0B-BBBE-64B5A3748AEE}");
    public static readonly FormControlClass DateTime = new("{5B773807-9FB2-42DB-97C3-7A91EFF8ADFF}");
    public static readonly FormControlClass Boolean = new("{67FAC785-CD58-4F9F-ABB3-4B7DDC6ED5ED}");
}
