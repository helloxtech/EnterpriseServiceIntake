import crypto from "node:crypto";
import fs from "node:fs/promises";
import path from "node:path";

const environmentUrl = required("POWERPLATFORM_ENVIRONMENT_URL").replace(/\/$/, "");
const username = required("POWERPLATFORM_ADMIN_USERNAME");
const password = required("POWERPLATFORM_ADMIN_PASSWORD");
const solutionUniqueName = process.env.POWERPLATFORM_SOLUTION_UNIQUE_NAME || "EnterpriseServiceIntake";
const clientId = process.env.POWERPLATFORM_PUBLIC_CLIENT_ID || "51f81489-12ee-4a9e-aaae-a2591f45987d";

const websiteId = "8c12ac01-467a-4fa8-8034-50b8028de647";
const languageId = "57f25f3a-586a-4082-812e-92da0adc20d9";
const publishedStateId = "58a93b23-e266-40c6-b635-d6b210e00b57";
const defaultTemplateId = "c1990c1e-1485-4542-9cd6-07a724da5ff2";
const homePageId = "00f75bc8-96da-4364-b676-9b7fd00707da";
const activeDocumentLocationsViewId = "820684b1-8d57-df11-a5a2-00155d2a9005";

const portalFormName = "Service Request - SharePoint Documents";
const basicFormName = "ESI - Service Request SharePoint Documents";
const pageName = "Request Documents";
const pagePartialUrl = "request-documents";
const documentPermissionName = "Document Location - Upload - Service Request Parent";

const controlClasses = {
  text: "{4273EDBD-AC1D-40D3-9FB2-095C621B552D}",
  memo: "{E0DECE4B-6FC8-4A8F-A065-082708572369}",
  optionSet: "{3EF39988-22BB-4F0B-BBBE-64B5A3748AEE}"
};

const token = await getToken();

await ensurePortalDocumentSystemForm();
await ensureDocumentLocationPermission();
await ensureBasicForm();
await ensureDocumentUploadPage();
await publishAll();

console.log("SharePoint document upload portal components applied.");

function required(name) {
  const value = process.env[name];
  if (!value) throw new Error(`Missing environment variable: ${name}`);
  return value;
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

  if (!response.ok) throw new Error(`Token request failed: ${response.status} ${await response.text()}`);
  return (await response.json()).access_token;
}

async function ensurePortalDocumentSystemForm() {
  const result = await dataverse(
    "GET",
    `systemforms?$select=formid,name&$filter=type eq 2 and objecttypecode eq 'hx_servicerequest' and name eq '${encodeODataString(portalFormName)}'`
  );
  const payload = {
    name: portalFormName,
    objecttypecode: "hx_servicerequest",
    type: 2,
    formactivationstate: 1,
    description: "Power Pages edit form used to expose SharePoint document management for submitted service requests.",
    formxml: buildPortalDocumentFormXml()
  };

  let id = result.value?.[0]?.formid;
  if (id) {
    await dataverse("PATCH", `systemforms(${id})`, payload);
    console.log(`Updated system form: ${portalFormName}`);
  } else {
    const created = await dataverse("POST", "systemforms", payload, { returnRepresentation: true });
    id = created.formid;
    console.log(`Created system form: ${portalFormName}`);
  }

  await addSolutionComponent(id, 60);
}

async function ensureDocumentLocationPermission() {
  const parent = await getSingle(
    `mspp_entitypermissions?$select=mspp_entitypermissionid&$filter=mspp_entityname eq 'Service Request - Full Control - Contact'`
  );
  if (!parent) throw new Error("Parent Service Request contact table permission was not found.");

  const existing = await getSingle(
    `mspp_entitypermissions?$select=mspp_entitypermissionid&$filter=mspp_entityname eq '${encodeODataString(documentPermissionName)}'`
  );
  const payload = {
    mspp_entityname: documentPermissionName,
    mspp_entitylogicalname: "sharepointdocumentlocation",
    mspp_scope: 756150003,
    mspp_read: true,
    mspp_write: true,
    mspp_create: true,
    mspp_delete: false,
    mspp_append: true,
    mspp_appendto: true,
    mspp_parentrelationship: "hx_servicerequest_SharePointDocumentLocations",
    "mspp_parententitypermission@odata.bind": `/mspp_entitypermissions(${parent.mspp_entitypermissionid})`,
    "mspp_websiteid@odata.bind": `/mspp_websites(${websiteId})`
  };

  if (existing) {
    await patchOrReuse(
      `mspp_entitypermissions(${existing.mspp_entitypermissionid})`,
      payload,
      `Updated table permission: ${documentPermissionName}`,
      `Reused existing table permission: ${documentPermissionName}`
    );
  } else {
    await dataverse("POST", "mspp_entitypermissions", payload);
    console.log(`Created table permission: ${documentPermissionName}`);
  }
}

async function ensureBasicForm() {
  const existing = await getSingle(
    `mspp_entityforms?$select=mspp_entityformid&$filter=mspp_name eq '${encodeODataString(basicFormName)}'`
  );
  const payload = {
    mspp_name: basicFormName,
    mspp_entityname: "hx_servicerequest",
    mspp_formname: portalFormName,
    mspp_mode: 100000001,
    mspp_entitysourcetype: 756150001,
    mspp_recordidquerystringparametername: "id",
    mspp_entitypermissionsenabled: true,
    mspp_showownerfields: false,
    mspp_renderwebresourcesinline: true,
    mspp_tooltipenabled: true,
    mspp_submitbuttontext: "Save request details",
    mspp_submitbuttonbusytext: "Saving...",
    mspp_successmessage: "Your request details were saved.",
    mspp_recordnotfoundmessage: "We could not find a service request for this link, or you do not have permission to view it.",
    mspp_validationsummaryheadertext: "Please fix the highlighted fields before saving.",
    mspp_attachfile: false,
    "mspp_websiteid@odata.bind": `/mspp_websites(${websiteId})`
  };

  if (existing) {
    await patchOrReuse(
      `mspp_entityforms(${existing.mspp_entityformid})`,
      payload,
      `Updated basic form: ${basicFormName}`,
      `Reused existing basic form: ${basicFormName}`
    );
  } else {
    await dataverse("POST", "mspp_entityforms", payload);
    console.log(`Created basic form: ${basicFormName}`);
  }
}

async function ensureDocumentUploadPage() {
  const css = await fs.readFile(path.resolve("src/powerpages/web-files/service-intake.css"), "utf8");
  const copy = buildDocumentUploadPageCopy();
  const root = await upsertWebPage({
    isRoot: true,
    copy,
    customCss: css
  });

  await upsertWebPage({
    isRoot: false,
    copy,
    customCss: css,
    rootPageId: root.mspp_webpageid
  });
}

async function upsertWebPage({ isRoot, copy, customCss, rootPageId }) {
  const filter = [
    `mspp_partialurl eq '${encodeODataString(pagePartialUrl)}'`,
    `mspp_isroot eq ${isRoot ? "true" : "false"}`
  ];
  if (!isRoot) filter.push(`_mspp_rootwebpageid_value eq ${rootPageId}`);

  const existing = await getSingle(`mspp_webpages?$select=mspp_webpageid&$filter=${filter.join(" and ")}`);
  const payload = {
    mspp_name: pageName,
    mspp_title: "Upload supporting files",
    mspp_partialurl: pagePartialUrl,
    mspp_isroot: isRoot,
    mspp_hiddenfromsitemap: true,
    mspp_excludefromsearch: true,
    mspp_enablerating: false,
    mspp_sharedpageconfiguration: false,
    mspp_displayorder: 20,
    mspp_copy: copy,
    mspp_customcss: customCss,
    "mspp_websiteid@odata.bind": `/mspp_websites(${websiteId})`,
    "mspp_pagetemplateid@odata.bind": `/mspp_pagetemplates(${defaultTemplateId})`,
    "mspp_publishingstateid@odata.bind": `/mspp_publishingstates(${publishedStateId})`,
    "mspp_parentpageid@odata.bind": `/mspp_webpages(${homePageId})`
  };

  if (!isRoot) {
    payload["mspp_rootwebpageid@odata.bind"] = `/mspp_webpages(${rootPageId})`;
    payload["mspp_webpagelanguageid@odata.bind"] = `/mspp_websitelanguages(${languageId})`;
  }

  if (existing) {
    await patchOrReuse(
      `mspp_webpages(${existing.mspp_webpageid})`,
      payload,
      `Updated ${isRoot ? "root" : "localized"} document upload page.`,
      `Reused existing ${isRoot ? "root" : "localized"} document upload page.`
    );
    return existing;
  }

  const created = await dataverse("POST", "mspp_webpages", payload, { returnRepresentation: true });
  console.log(`Created ${isRoot ? "root" : "localized"} document upload page.`);
  return created;
}

function buildDocumentUploadPageCopy() {
  return `{% assign homeurl = website.adx_partialurl %}
<section class="esi-hero">
  <div class="esi-hero-inner">
    <div class="esi-hero-copy">
      <h1>Upload supporting files</h1>
      <p>Add screenshots, forms, or other evidence to the SharePoint document library for your submitted service request.</p>
    </div>
    {% if user %}
    <div class="esi-user-card" aria-label="Signed in customer">
      <span class="esi-user-label">Signed in</span>
      <strong>{{ user.fullname | default: user.emailaddress1 | escape }}</strong>
      <span>Customer account</span>
    </div>
    {% endif %}
  </div>
</section>

{% if user and request.params.id %}
<main class="esi-shell esi-document-shell" id="mainContent">
  <div class="esi-workspace-banner">
    <div>
      <strong>Secure SharePoint upload</strong>
      <span>Files added here are stored through Dataverse document management for this service request.</span>
    </div>
    <a class="esi-secondary esi-link-button" href="/">Return to intake</a>
  </div>
  <section class="esi-form esi-document-upload">
    {% entityform name: '${basicFormName}' %}
  </section>
</main>
{% elsif user %}
<main class="esi-shell esi-auth-shell" id="mainContent">
  <section class="esi-auth-gate">
    <div>
      <span class="esi-preview-label">Missing request</span>
      <h2>Open this page from a submitted request</h2>
      <p>The upload page needs a service request ID so files can be saved to the correct SharePoint folder.</p>
      <a class="esi-primary esi-link-button" href="/">Return to intake</a>
    </div>
  </section>
</main>
{% else %}
<main class="esi-shell esi-auth-shell" id="mainContent">
  <section class="esi-auth-gate">
    <div>
      <span class="esi-preview-label">Secure upload</span>
      <h2>Sign in to upload files</h2>
      <p>Please sign in so we can confirm you have access to this service request and its documents.</p>
      <a class="esi-primary esi-link-button" href="{% if homeurl %}/{{ homeurl }}{% endif %}{{ website.sign_in_url_substitution }}">Sign in to continue</a>
    </div>
  </section>
</main>
{% endif %}`;
}

function buildPortalDocumentFormXml() {
  const fields = [
    field("hx_confirmationnumber", "Confirmation Number", "text", true),
    field("hx_title", "Request Title", "text", true),
    field("hx_lifecyclestatus", "Lifecycle Status", "optionSet", true),
    field("hx_customervisibleupdates", "Customer Visible Updates", "memo", false)
  ];
  const lines = [];
  lines.push('<form headerdensity="HighWithControls">');
  lines.push("  <tabs>");
  lines.push(`    <tab verticallayout="true" id="{${deterministicGuid("hx_servicerequest:sharepoint-documents:tab")}}" name="tab_documents" IsUserDefined="1" showlabel="true">`);
  lines.push('      <labels><label description="Supporting Files" languagecode="1033" /></labels>');
  lines.push('      <columns><column width="100%"><sections>');
  lines.push(`        <section id="{${deterministicGuid("hx_servicerequest:sharepoint-documents:summary")}}" name="request_summary" IsUserDefined="1" showlabel="true" showbar="false" layout="varwidth" columns="1" labelwidth="180">`);
  lines.push('          <labels><label description="Request Summary" languagecode="1033" /></labels>');
  lines.push("          <rows>");
  for (const item of fields) appendField(lines, item);
  lines.push("          </rows>");
  lines.push("        </section>");
  lines.push(`        <section id="{${deterministicGuid("hx_servicerequest:sharepoint-documents:documents")}}" name="sharepoint_documents" IsUserDefined="1" showlabel="true" showbar="false" layout="varwidth" columns="1" labelwidth="180">`);
  lines.push('          <labels><label description="SharePoint Documents" languagecode="1033" /></labels>');
  lines.push("          <rows>");
  lines.push("            <row>");
  lines.push(`              <cell id="{${deterministicGuid("hx_servicerequest:sharepoint-documents:subgrid-cell")}}" showlabel="true" rowspan="8" colspan="1" auto="false">`);
  lines.push('                <labels><label description="Supporting Documents" languagecode="1033" /></labels>');
  lines.push('                <control id="SharePointDocuments" classid="{E7A81278-8635-4d9e-8D4D-59480B391C5B}">');
  lines.push("                  <parameters>");
  lines.push("                    <TargetEntityType>sharepointdocumentlocation</TargetEntityType>");
  lines.push("                    <ChartGridMode>Grid</ChartGridMode>");
  lines.push("                    <EnableQuickFind>false</EnableQuickFind>");
  lines.push("                    <EnableViewPicker>false</EnableViewPicker>");
  lines.push("                    <EnableJumpBar>false</EnableJumpBar>");
  lines.push("                    <RecordsPerPage>10</RecordsPerPage>");
  lines.push(`                    <ViewId>{${activeDocumentLocationsViewId}}</ViewId>`);
  lines.push("                    <IsUserView>false</IsUserView>");
  lines.push(`                    <ViewIds>{${activeDocumentLocationsViewId}}</ViewIds>`);
  lines.push("                    <AutoExpand>Fixed</AutoExpand>");
  lines.push("                    <RelationshipName>hx_servicerequest_SharePointDocumentLocations</RelationshipName>");
  lines.push("                  </parameters>");
  lines.push("                </control>");
  lines.push("              </cell>");
  lines.push("            </row>");
  lines.push("          </rows>");
  lines.push("        </section>");
  lines.push("      </sections></column></columns>");
  lines.push("    </tab>");
  lines.push("  </tabs>");
  lines.push(`  <header id="{${deterministicGuid("hx_servicerequest:sharepoint-documents:header")}}" celllabelposition="Top" columns="11" labelwidth="115" celllabelalignment="Left">`);
  lines.push("    <rows><row>");
  appendHeaderField(lines, field("hx_confirmationnumber", "Confirmation", "text", true));
  appendHeaderField(lines, field("hx_lifecyclestatus", "Status", "optionSet", true));
  lines.push("    </row></rows>");
  lines.push("  </header>");
  lines.push(`  <footer id="{${deterministicGuid("hx_servicerequest:sharepoint-documents:footer")}}" celllabelposition="Top" columns="111" labelwidth="115" celllabelalignment="Left"><rows><row /></rows></footer>`);
  lines.push('  <DisplayConditions Order="0" FallbackForm="true"><Everyone /></DisplayConditions>');
  lines.push("</form>");
  return lines.join("\n");
}

function field(logicalName, label, control, disabled = false) {
  return { logicalName, label, control, disabled };
}

function appendField(lines, item) {
  lines.push("            <row>");
  lines.push(`              <cell id="{${deterministicGuid(`hx_servicerequest:${item.logicalName}:portal-doc-cell`)}}" showlabel="true">`);
  lines.push(`                <labels><label description="${xml(item.label)}" languagecode="1033" /></labels>`);
  lines.push(`                <control id="${xml(item.logicalName)}" classid="${controlClasses[item.control]}" datafieldname="${xml(item.logicalName)}"${item.disabled ? ' disabled="true"' : ""} />`);
  lines.push("              </cell>");
  lines.push("            </row>");
}

function appendHeaderField(lines, item) {
  lines.push(`      <cell id="{${deterministicGuid(`hx_servicerequest:${item.logicalName}:portal-doc-header`)}}" showlabel="true">`);
  lines.push(`        <labels><label description="${xml(item.label)}" languagecode="1033" /></labels>`);
  lines.push(`        <control id="${xml(item.logicalName)}" classid="${controlClasses[item.control]}" datafieldname="${xml(item.logicalName)}"${item.disabled ? ' disabled="true"' : ""} />`);
  lines.push("      </cell>");
}

async function getSingle(relativePath) {
  const result = await dataverse("GET", relativePath);
  return result.value?.[0] || null;
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
      console.warn(`Warning: component ${componentId} type ${componentType} was not added to solution: ${error.message}`);
    }
  }
}

async function patchOrReuse(relativePath, payload, updatedMessage, reusedMessage) {
  try {
    await dataverse("PATCH", relativePath, payload);
    console.log(updatedMessage);
  } catch (error) {
    const message = String(error.message);
    if (!message.includes("0x80040333") && !message.includes("Web page with same partial url and parent page already exists")) {
      throw error;
    }
    console.log(reusedMessage);
  }
}

async function publishAll() {
  await dataverse("POST", "PublishAllXml", {});
}

async function dataverse(method, relativePath, body, options = {}) {
  const headers = {
    Authorization: `Bearer ${token}`,
    Accept: "application/json",
    "OData-MaxVersion": "4.0",
    "OData-Version": "4.0",
    "MSCRM.SolutionUniqueName": solutionUniqueName
  };

  if (body !== undefined) headers["Content-Type"] = "application/json; charset=utf-8";
  if (options.returnRepresentation) headers.Prefer = "return=representation";

  const response = await fetch(`${environmentUrl}/api/data/v9.2/${relativePath}`, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body)
  });
  const text = await response.text();
  if (!response.ok) throw new Error(`${method} ${relativePath} failed: ${response.status} ${text}`);
  return text ? JSON.parse(text) : {};
}

function encodeODataString(value) {
  return String(value).replace(/'/g, "''");
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

function xml(value) {
  return String(value)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&apos;");
}
