import crypto from "node:crypto";
import fs from "node:fs/promises";
import path from "node:path";

const environmentUrl = required("POWERPLATFORM_ENVIRONMENT_URL").replace(/\/$/, "");
const username = required("POWERPLATFORM_ADMIN_USERNAME");
const password = required("POWERPLATFORM_ADMIN_PASSWORD");
const solutionUniqueName = process.env.POWERPLATFORM_SOLUTION_UNIQUE_NAME || "EnterpriseServiceIntake";
const clientId = process.env.POWERPLATFORM_PUBLIC_CLIENT_ID || "51f81489-12ee-4a9e-aaae-a2591f45987d";
const shouldExport = process.argv.includes("--export");

const controls = {
  text: "{4273EDBD-AC1D-40D3-9FB2-095C621B552D}",
  memo: "{E0DECE4B-6FC8-4A8F-A065-082708572369}",
  lookup: "{270BD3DB-D9AF-4782-9025-509E298DEC0A}",
  optionSet: "{3EF39988-22BB-4F0B-BBBE-64B5A3748AEE}",
  dateTime: "{5B773807-9FB2-42DB-97C3-7A91EFF8ADFF}",
  boolean: "{67FAC785-CD58-4F9F-ABB3-4B7DDC6ED5ED}"
};

const forms = [
  {
    entity: "hx_servicerequest",
    targetName: "Service Request - Coordinator",
    primaryName: "hx_title",
    fallbackNames: ["Information"],
    header: [
      field("hx_confirmationnumber", "Confirmation", "text", true),
      field("hx_lifecyclestatus", "Lifecycle", "optionSet"),
      field("hx_assigneddepartment", "Department", "lookup", true),
      field("hx_duedate", "SLA Due", "dateTime", true)
    ],
    sections: [
      section("Customer and Request", [
        field("hx_title", "Title", "text"),
        field("hx_confirmationnumber", "Confirmation Number", "text", true),
        field("hx_customercontact", "Customer Contact", "lookup"),
        field("hx_customeraccount", "Customer Account", "lookup"),
        field("hx_servicecategory", "Service Category", "lookup"),
        field("hx_description", "Description", "memo")
      ]),
      section("Triage Inputs", [
        field("hx_severity", "Severity", "optionSet"),
        field("hx_priority", "Priority", "optionSet"),
        field("hx_submittedon", "Submitted On", "dateTime", true),
        field("ownerid", "Owner", "lookup")
      ]),
      section("Routing and SLA", [
        field("hx_assigneddepartment", "Assigned Department", "lookup", true),
        field("hx_appliedslapolicy", "Applied SLA Policy", "lookup", true),
        field("hx_duedate", "SLA Due Date", "dateTime", true),
        field("hx_routingpreviewsummary", "Routing / SLA Summary", "text", true)
      ]),
      section("Approval and ERP Sync", [
        field("hx_requiresapproval", "Requires Approval", "boolean", true),
        field("hx_approvalstatus", "Approval Status", "optionSet"),
        field("hx_integrationsyncstatus", "Integration Sync Status", "optionSet"),
        field("hx_externalerpid", "External ERP ID", "text", true)
      ]),
      section("Resolution Guardrail", [
        field("hx_lifecyclestatus", "Lifecycle Status", "optionSet"),
        field("hx_resolutiondocumentationrequired", "Resolution Documentation Required", "boolean", true),
        field("hx_resolutiondocumentationprovided", "Resolution Documentation Provided", "boolean"),
        field("hx_internalresolutionnotes", "Internal Resolution Notes", "memo"),
        field("hx_customervisibleupdates", "Customer Visible Updates", "memo")
      ])
    ]
  },
  {
    entity: "hx_servicedocument",
    targetName: "Service Document - Review",
    primaryName: "hx_name",
    fallbackNames: ["Information"],
    header: [
      field("hx_documenttype", "Type", "optionSet"),
      field("hx_verified", "Verified", "boolean"),
      field("ownerid", "Owner", "lookup")
    ],
    sections: [
      section("Document", [
        field("hx_name", "Name", "text"),
        field("hx_servicerequest", "Service Request", "lookup"),
        field("hx_documenttype", "Document Type", "optionSet"),
        field("hx_filename", "File Name", "text")
      ]),
      section("Review", [
        field("hx_verified", "Verified", "boolean"),
        field("hx_notes", "Notes", "memo"),
        field("ownerid", "Owner", "lookup")
      ])
    ]
  },
  {
    entity: "hx_routingrule",
    targetName: "Routing Rule - Configuration",
    primaryName: "hx_name",
    fallbackNames: ["Information"],
    header: [
      field("hx_sortorder", "Sort", "text"),
      field("hx_active", "Active", "boolean"),
      field("hx_requiresapproval", "Approval", "boolean")
    ],
    sections: [
      section("Rule Identity", [
        field("hx_name", "Name", "text"),
        field("hx_sortorder", "Sort Order", "text"),
        field("hx_active", "Active", "boolean")
      ]),
      section("Match Criteria", [
        field("hx_servicecategory", "Service Category", "lookup"),
        field("hx_matchseverity", "Match Severity", "optionSet"),
        field("hx_matchpriority", "Match Priority", "optionSet")
      ]),
      section("Routing Outcome", [
        field("hx_department", "Department", "lookup"),
        field("hx_slapolicy", "SLA Policy", "lookup"),
        field("hx_requiresapproval", "Requires Manager Approval", "boolean"),
        field("hx_resolutiondocumentationrequired", "Resolution Documentation Required", "boolean")
      ])
    ]
  },
  {
    entity: "hx_department",
    targetName: "Department - Configuration",
    primaryName: "hx_name",
    fallbackNames: ["Information"],
    header: [
      field("hx_code", "Code", "text"),
      field("hx_active", "Active", "boolean"),
      field("ownerid", "Owner", "lookup")
    ],
    sections: [
      section("Department", [
        field("hx_name", "Name", "text"),
        field("hx_code", "Code", "text"),
        field("hx_manageremail", "Manager Email", "text"),
        field("hx_active", "Active", "boolean")
      ]),
      section("Operating Notes", [
        field("hx_description", "Description", "memo"),
        field("ownerid", "Owner", "lookup")
      ])
    ]
  },
  {
    entity: "hx_slapolicy",
    targetName: "SLA Policy - Configuration",
    primaryName: "hx_name",
    fallbackNames: ["Information"],
    header: [
      field("hx_responsehours", "Response Hours", "text"),
      field("hx_resolutionhours", "Resolution Hours", "text"),
      field("hx_active", "Active", "boolean")
    ],
    sections: [
      section("SLA Targets", [
        field("hx_name", "Name", "text"),
        field("hx_responsehours", "Response Hours", "text"),
        field("hx_resolutionhours", "Resolution Hours", "text"),
        field("hx_active", "Active", "boolean")
      ]),
      section("Policy Notes", [
        field("hx_description", "Description", "memo"),
        field("ownerid", "Owner", "lookup")
      ])
    ]
  },
  {
    entity: "hx_servicecategory",
    targetName: "Service Category - Configuration",
    primaryName: "hx_name",
    fallbackNames: ["Information"],
    header: [
      field("hx_code", "Code", "text"),
      field("hx_active", "Active", "boolean"),
      field("hx_defaultdocumentationrequired", "Docs Required", "boolean")
    ],
    sections: [
      section("Category", [
        field("hx_name", "Name", "text"),
        field("hx_code", "Code", "text"),
        field("hx_active", "Active", "boolean"),
        field("hx_defaultdocumentationrequired", "Default Documentation Required", "boolean"),
        field("ownerid", "Owner", "lookup")
      ])
    ]
  },
  {
    entity: "hx_externalsynclog",
    targetName: "External Sync Log - Review",
    primaryName: "hx_name",
    fallbackNames: ["Information"],
    header: [
      field("hx_syncstatus", "Status", "optionSet"),
      field("hx_attemptedon", "Attempted", "dateTime"),
      field("hx_externalid", "External ID", "text")
    ],
    sections: [
      section("Sync Attempt", [
        field("hx_name", "Name", "text"),
        field("hx_servicerequest", "Service Request", "lookup"),
        field("hx_syncstatus", "Sync Status", "optionSet"),
        field("hx_endpointname", "Endpoint Name", "text"),
        field("hx_externalid", "External ID", "text"),
        field("hx_attemptedon", "Attempted On", "dateTime")
      ]),
      section("Payload and Response", [
        field("hx_requestpayload", "Request Payload", "memo"),
        field("hx_responsesummary", "Response Summary", "memo")
      ])
    ]
  },
  {
    entity: "hx_errorlog",
    targetName: "System Error Log - Triage",
    primaryName: "hx_name",
    fallbackNames: ["Information"],
    header: [
      field("hx_sourcecomponent", "Source", "optionSet"),
      field("hx_stage", "Stage", "text"),
      field("hx_resolved", "Resolved", "boolean")
    ],
    sections: [
      section("Triage", [
        field("hx_name", "Name", "text"),
        field("hx_sourcecomponent", "Source Component", "optionSet"),
        field("hx_stage", "Stage", "text"),
        field("hx_servicerequest", "Service Request", "lookup"),
        field("hx_correlationid", "Correlation ID", "text"),
        field("hx_resolved", "Resolved", "boolean")
      ]),
      section("Error Detail", [
        field("hx_message", "Message", "memo"),
        field("hx_technicaldetail", "Technical Detail", "memo"),
        field("hx_payload", "Payload", "memo")
      ])
    ]
  }
];

const views = [
  view("hx_servicerequest", "Coordinator Queue", [
    "hx_confirmationnumber",
    "hx_title",
    "hx_customercontact",
    "hx_servicecategory",
    "hx_severity",
    "hx_priority",
    "hx_assigneddepartment",
    "hx_lifecyclestatus",
    "hx_approvalstatus",
    "hx_integrationsyncstatus",
    "hx_duedate",
    "createdon"
  ], "<condition attribute='statecode' operator='eq' value='0' />", "hx_duedate", false),
  view("hx_servicerequest", "Pending Manager Approval", [
    "hx_confirmationnumber",
    "hx_title",
    "hx_servicecategory",
    "hx_severity",
    "hx_priority",
    "hx_assigneddepartment",
    "hx_approvalstatus",
    "hx_duedate",
    "createdon"
  ], [
    "<condition attribute='statecode' operator='eq' value='0' />",
    "<condition attribute='hx_requiresapproval' operator='eq' value='1' />",
    "<condition attribute='hx_approvalstatus' operator='eq' value='752630001' />"
  ].join("\n      "), "createdon", false),
  view("hx_servicerequest", "Critical Documentation Guardrails", [
    "hx_confirmationnumber",
    "hx_title",
    "hx_severity",
    "hx_priority",
    "hx_lifecyclestatus",
    "hx_resolutiondocumentationrequired",
    "hx_resolutiondocumentationprovided",
    "hx_assigneddepartment",
    "modifiedon"
  ], [
    "<condition attribute='statecode' operator='eq' value='0' />",
    "<condition attribute='hx_resolutiondocumentationrequired' operator='eq' value='1' />",
    "<condition attribute='hx_resolutiondocumentationprovided' operator='eq' value='0' />"
  ].join("\n      "), "modifiedon", true),
  view("hx_servicerequest", "ERP Sync Monitor", [
    "hx_confirmationnumber",
    "hx_title",
    "hx_approvalstatus",
    "hx_integrationsyncstatus",
    "hx_externalerpid",
    "hx_assigneddepartment",
    "createdon",
    "modifiedon"
  ], [
    "<condition attribute='statecode' operator='eq' value='0' />",
    "<filter type='or'>",
    "  <condition attribute='hx_requiresapproval' operator='eq' value='1' />",
    "  <condition attribute='hx_integrationsyncstatus' operator='eq' value='752630003' />",
    "</filter>"
  ].join("\n      "), "modifiedon", true),
  view("hx_servicedocument", "Request Documents - Review", [
    "hx_name",
    "hx_servicerequest",
    "hx_documenttype",
    "hx_filename",
    "hx_verified",
    "createdon",
    "ownerid"
  ], "<condition attribute='statecode' operator='eq' value='0' />", "createdon", true),
  view("hx_routingrule", "Active Routing Rules", [
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
  ], "<condition attribute='hx_active' operator='eq' value='1' />", "hx_sortorder", false),
  view("hx_department", "Active Departments", [
    "hx_name",
    "hx_code",
    "hx_manageremail",
    "hx_active",
    "modifiedon"
  ], "<condition attribute='hx_active' operator='eq' value='1' />", "hx_name", false),
  view("hx_slapolicy", "Active SLA Policies", [
    "hx_name",
    "hx_responsehours",
    "hx_resolutionhours",
    "hx_active",
    "modifiedon"
  ], "<condition attribute='hx_active' operator='eq' value='1' />", "hx_name", false),
  view("hx_servicecategory", "Active Service Categories", [
    "hx_name",
    "hx_code",
    "hx_defaultdocumentationrequired",
    "hx_active",
    "modifiedon"
  ], "<condition attribute='hx_active' operator='eq' value='1' />", "hx_name", false),
  view("hx_externalsynclog", "ERP Sync Attempts", [
    "hx_name",
    "hx_servicerequest",
    "hx_syncstatus",
    "hx_endpointname",
    "hx_externalid",
    "hx_attemptedon",
    "createdon"
  ], null, "hx_attemptedon", true),
  view("hx_errorlog", "Open Integration and Automation Errors", [
    "hx_name",
    "hx_sourcecomponent",
    "hx_stage",
    "hx_servicerequest",
    "hx_correlationid",
    "hx_resolved",
    "createdon"
  ], "<condition attribute='hx_resolved' operator='eq' value='0' />", "createdon", true),
  view("hx_errorlog", "All System Error Logs", [
    "hx_name",
    "hx_sourcecomponent",
    "hx_stage",
    "hx_servicerequest",
    "hx_correlationid",
    "hx_resolved",
    "createdon"
  ], null, "createdon", true)
];

const token = await getToken();

for (const form of forms) {
  await applyForm(form);
}

for (const systemView of views) {
  await applyView(systemView);
}

await publishAll();

if (shouldExport) {
  await exportSolution(false);
  await exportSolution(true);
}

console.log("Model-driven app form and view design applied.");

function required(name) {
  const value = process.env[name];
  if (!value) {
    throw new Error(`Missing environment variable: ${name}`);
  }
  return value;
}

function field(logicalName, label, control, disabled = false) {
  return { logicalName, label, control, disabled };
}

function section(name, fields) {
  return { name, fields };
}

function view(entity, name, columns, filterXml, orderBy, descending) {
  return { entity, name, columns, filterXml, orderBy, descending };
}

async function getToken() {
  const body = new URLSearchParams({
    grant_type: "password",
    client_id: clientId,
    resource: environmentUrl,
    username,
    password
  });

  const response = await fetch("https://login.microsoftonline.com/common/oauth2/token", {
    method: "POST",
    headers: { "content-type": "application/x-www-form-urlencoded" },
    body
  });

  if (!response.ok) {
    throw new Error(`Token request failed: ${response.status} ${await response.text()}`);
  }

  const payload = await response.json();
  return payload.access_token;
}

async function applyForm(form) {
  const response = await dataverse("GET", `systemforms?$select=formid,name&$filter=type eq 2 and objecttypecode eq '${form.entity}'`);
  const candidates = response.value || [];
  const existing =
    candidates.find((item) => item.name === form.targetName) ||
    candidates.find((item) => form.fallbackNames.includes(item.name)) ||
    candidates[0];

  if (!existing) {
    console.log(`Skipped ${form.entity}; no main form was found.`);
    return;
  }

  await dataverse("PATCH", `systemforms(${existing.formid})`, {
    name: form.targetName,
    formxml: buildFormXml(form)
  });

  await addSolutionComponent(existing.formid, 60);
  console.log(`Updated form: ${form.targetName}`);
}

async function applyView(systemView) {
  const escapedName = systemView.name.replace(/'/g, "''");
  const result = await dataverse(
    "GET",
    `savedqueries?$select=savedqueryid,name&$filter=querytype eq 0 and returnedtypecode eq '${systemView.entity}' and name eq '${escapedName}'`
  );
  const existing = result.value?.[0];
  const payload = {
    name: systemView.name,
    returnedtypecode: systemView.entity,
    querytype: 0,
    fetchxml: buildFetchXml(systemView),
    layoutxml: buildLayoutXml(systemView)
  };

  let id;
  if (existing) {
    await dataverse("PATCH", `savedqueries(${existing.savedqueryid})`, payload);
    id = existing.savedqueryid;
    console.log(`Updated view: ${systemView.name}`);
  } else {
    const created = await dataverse("POST", "savedqueries", payload, { returnRepresentation: true });
    id = created.savedqueryid;
    console.log(`Created view: ${systemView.name}`);
  }

  await addSolutionComponent(id, 26);
}

function buildFormXml(form) {
  const lines = [];
  lines.push('<form headerdensity="HighWithControls">');
  lines.push("  <tabs>");
  lines.push(`    <tab verticallayout="true" id="{${deterministicGuid(`${form.entity}:${form.targetName}:summary-tab`)}}" name="tab_summary" IsUserDefined="1" showlabel="true">`);
  lines.push('      <labels><label description="Summary" languagecode="1033" /></labels>');
  lines.push("      <columns><column width=\"100%\"><sections>");

  for (const formSection of form.sections) {
    lines.push(`        <section id="{${deterministicGuid(`${form.entity}:${formSection.name}`)}}" name="${xmlName(formSection.name)}" IsUserDefined="1" showlabel="true" showbar="false" layout="varwidth" columns="1" labelwidth="180">`);
    lines.push(`          <labels><label description="${escapeXml(formSection.name)}" languagecode="1033" /></labels>`);
    lines.push("          <rows>");
    for (const formField of formSection.fields) {
      appendField(lines, form.entity, formField, "            ", true);
    }
    lines.push("          </rows>");
    lines.push("        </section>");
  }

  lines.push("      </sections></column></columns>");
  lines.push("    </tab>");
  lines.push("  </tabs>");
  lines.push(`  <header id="{${deterministicGuid(`${form.entity}:header`)}}" celllabelposition="Top" columns="${"1".repeat(Math.max(1, form.header.length))}" labelwidth="115" celllabelalignment="Left">`);
  lines.push("    <rows><row>");
  for (const formField of form.header) {
    appendField(lines, form.entity, formField, "      ", false);
  }
  lines.push("    </row></rows>");
  lines.push("  </header>");
  lines.push(`  <footer id="{${deterministicGuid(`${form.entity}:footer`)}}" celllabelposition="Top" columns="111" labelwidth="115" celllabelalignment="Left"><rows><row /></rows></footer>`);
  lines.push("  <DisplayConditions Order=\"0\" FallbackForm=\"true\"><Everyone /></DisplayConditions>");
  lines.push("</form>");
  return lines.join("\n");
}

function appendField(lines, entity, formField, indent, includeRow) {
  if (includeRow) {
    lines.push(`${indent}<row>`);
    indent += "  ";
  }

  const disabled = formField.disabled ? ' disabled="true"' : "";
  lines.push(`${indent}<cell id="{${deterministicGuid(`${entity}:${formField.logicalName}:cell`)}}" showlabel="true">`);
  lines.push(`${indent}  <labels><label description="${escapeXml(formField.label)}" languagecode="1033" /></labels>`);
  lines.push(`${indent}  <control id="${escapeXml(formField.logicalName)}" classid="${controls[formField.control]}" datafieldname="${escapeXml(formField.logicalName)}"${disabled} />`);
  lines.push(`${indent}</cell>`);

  if (includeRow) {
    lines.push(`${indent.slice(0, -2)}</row>`);
  }
}

function buildFetchXml(systemView) {
  const attributes = unique([`${systemView.entity}id`, ...systemView.columns, systemView.orderBy]);
  const lines = [];
  lines.push('<fetch version="1.0" mapping="logical" output-format="xml-platform">');
  lines.push(`  <entity name="${escapeXml(systemView.entity)}">`);
  for (const column of attributes) {
    lines.push(`    <attribute name="${escapeXml(column)}" />`);
  }
  lines.push(`    <order attribute="${escapeXml(systemView.orderBy)}" descending="${systemView.descending ? "true" : "false"}" />`);
  if (systemView.filterXml) {
    lines.push("    <filter type=\"and\">");
    lines.push(`      ${systemView.filterXml}`);
    lines.push("    </filter>");
  }
  lines.push("  </entity>");
  lines.push("</fetch>");
  return lines.join("\n");
}

function buildLayoutXml(systemView) {
  const lines = [];
  lines.push(`<grid name="resultset" object="1" jump="${escapeXml(systemView.columns[0])}" select="1" icon="1" preview="1">`);
  lines.push(`  <row name="result" id="${escapeXml(systemView.entity)}id">`);
  for (const column of systemView.columns) {
    lines.push(`    <cell name="${escapeXml(column)}" width="${columnWidth(column)}" />`);
  }
  lines.push("  </row>");
  lines.push("</grid>");
  return lines.join("\n");
}

function columnWidth(column) {
  if (column.includes("title") || column === "hx_name") return 240;
  if (column.includes("confirmation")) return 170;
  if (column.includes("customer") || column.includes("department") || column.includes("policy") || column.includes("request")) return 190;
  if (column.includes("created") || column.includes("modified") || column.includes("attempted") || column.includes("duedate")) return 150;
  return 140;
}

async function addSolutionComponent(componentId, componentType) {
  try {
    await dataverse("POST", "AddSolutionComponent", {
      ComponentId: componentId,
      ComponentType: componentType,
      SolutionUniqueName: solutionUniqueName,
      AddRequiredComponents: false
    });
  } catch (error) {
    if (!String(error.message).toLowerCase().includes("already")) {
      throw error;
    }
  }
}

async function publishAll() {
  await dataverse("POST", "PublishAllXml", {});
  console.log("Published customizations.");
}

async function exportSolution(managed) {
  const response = await dataverse("POST", "ExportSolution", {
    SolutionName: solutionUniqueName,
    Managed: managed
  });
  const file = Buffer.from(response.ExportSolutionFile, "base64");
  const exportDir = path.resolve("solution/export");
  await fs.mkdir(exportDir, { recursive: true });
  const suffix = managed ? "managed" : "unmanaged";
  const outputPath = path.join(exportDir, `Enterprise_ServiceIntake_ForrestZhang_${suffix}.zip`);
  await fs.writeFile(outputPath, file);
  console.log(`Exported ${suffix} solution: ${outputPath}`);
}

async function dataverse(method, relativePath, body, options = {}) {
  const headers = {
    Authorization: `Bearer ${token}`,
    Accept: "application/json",
    "OData-MaxVersion": "4.0",
    "OData-Version": "4.0",
    "MSCRM.SolutionUniqueName": solutionUniqueName
  };

  if (body !== undefined) {
    headers["Content-Type"] = "application/json; charset=utf-8";
  }

  if (options.returnRepresentation) {
    headers.Prefer = "return=representation";
  }

  const response = await fetch(`${environmentUrl}/api/data/v9.2/${relativePath}`, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body)
  });

  const text = await response.text();
  if (!response.ok) {
    throw new Error(`${method} ${relativePath} failed: ${response.status} ${text}`);
  }

  return text ? JSON.parse(text) : {};
}

function deterministicGuid(value) {
  const bytes = crypto.createHash("md5").update(value, "utf8").digest();
  return [
    Buffer.from(bytes.subarray(0, 4)).reverse().toString("hex"),
    Buffer.from(bytes.subarray(4, 6)).reverse().toString("hex"),
    Buffer.from(bytes.subarray(6, 8)).reverse().toString("hex"),
    bytes.subarray(8, 10).toString("hex"),
    bytes.subarray(10, 16).toString("hex")
  ].join("-");
}

function xmlName(value) {
  return value
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "_")
    .replace(/_+/g, "_")
    .replace(/^_|_$/g, "");
}

function escapeXml(value) {
  return String(value)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&apos;");
}

function unique(values) {
  return [...new Set(values.filter(Boolean))];
}
