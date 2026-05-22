# Verification Notes

## Live Environment

| Item | Value |
| --- | --- |
| Environment URL | https://mitacs.crm.dynamics.com/ |
| Environment ID | `99dd50ed-a753-e37f-912c-78a022b12b09` |
| Dataverse org ID | `9970c0d0-9f54-f111-b31f-6045bd003326` |
| Solution unique name | `EnterpriseServiceIntake` |
| Model-driven app ID | `3de4f813-b454-f111-bec7-000d3a3aca8f` |
| Power Pages site ID | `8c12ac01-467a-4fa8-8034-50b8028de647` |
| Power Pages URL | https://enterprise-service-intake-hellox.powerappsportals.com |
| Approval/ERP flow ID | `ab021e8c-bb54-f111-bec7-000d3a3acaff` |
| Confirmation email flow ID | `c97dbafe-d854-f111-bec7-000d3a3aca8f` |

## Verified Build Steps

| Check | Result |
| --- | --- |
| C# plugin build | Passed |
| Provisioning utility build | Passed |
| Provisioning utility NuGet audit | Passed; no vulnerable packages reported by `dotnet list package --vulnerable --include-transitive` |
| HelloX mock ERP function syntax | Passed |
| HelloX mock ERP function behavior | Passed; OAuth token call returned `200`, protected POST returned `201` with `HX-ERP-*`, and refresh token was not returned for the interview scenario |
| Hidden HelloX `/esi/` page smoke test | Passed; local browser check rendered the page title, endpoint, and submit action |
| Public HelloX deployment | Passed; `https://hellox.ca/api/mock/enterprise-service-intake/erp` accepts Bearer-token POSTs and `https://hellox.ca/esi/` returns HTTP 200 |
| PCF build/push | Passed |
| Power Pages upload | Passed |
| Power Pages live download-first update | Passed |
| Power Pages UX validation update | Passed |
| Power Pages live refresh/download | Passed |
| Power Pages SharePoint upload fix | Uploaded and re-downloaded key live files for verification |
| Confirmation email flow creation | Passed |
| Confirmation email smoke test | Passed |
| Managed solution export | Passed |
| Unmanaged solution export | Passed |
| Managed solution unpack | Passed |
| Unmanaged solution unpack | Passed |

## Verified Live Behavior

| Behavior | Evidence |
| --- | --- |
| Portal response estimate | Funding Agreement + Critical + Urgent shows Finance, 4 hour target, Mitacs review required, and follow-up details may be required. |
| Portal submission | `Portal Demo - Critical funding request 2` created with confirmation `SR-20260521-001004`. |
| SharePoint document upload path | Portal now directs users to `/request-documents/?id=<request-id>` after submission; page uses Power Pages Basic Form/document management for SharePoint files. |
| Confirmation email | Smoke test request `SR-20260521-001018` sent through Office 365 Outlook, updated customer-visible notes, and created no System Error Log row. Flow run `08584222604936303691105786332CU11` succeeded. |
| Portal step navigation | Explicit Continue, Back, Review request, and Submit buttons added; required fields block progression before the next step. |
| Portal SharePoint document path | Localized Home page now removes the pre-submit file input, creates the Service Request first, and opens `/request-documents/?id=<service-request-id>` from the success modal. |
| Upload page diagnostics | Request Documents page now shows a visible ready/warning/error status for the SharePoint document grid instead of failing silently. |
| Portal upload UX wording | Fresh live download confirms the upload flow hides the native `New folder` action and relabels portal-facing upload text to `Secure file upload`, `Updates from Mitacs`, `Supporting files`, `Uploaded files`, and `No files have been uploaded yet`. |
| Portal documents wording | Fresh live download confirms the intake Documents step and success modal use customer-facing secure upload wording instead of SharePoint/Power Pages implementation terms. |
| Portal step wording audit | Fresh live download confirms the intake journey uses customer-facing wording across Details, Impact, Documents, Review, response estimate, and upload states; visible labels use `Impact level`, `Urgency`, `Team`, and `Mitacs review` instead of internal routing/department/severity/priority language. |
| SharePoint upload smoke test | `Portal smoke file upload 2026-05-21T07-24-18-212Z` created confirmation `SR-20260521-001025`; `esi-upload-smoke.txt` uploaded through the document grid and appeared on the page. |
| SharePoint document location | Dataverse returned `sharepointdocumentlocationid` `4c2b05a7-e654-f111-89e7-0022488fbd9b` for request `df11b114-e654-f111-bec7-000d3a3aca8f`. |
| Model-driven documents | Live form metadata shows the coordinator `Documents` tab and the Power Pages support form now use `sharepointdocument` through `hx_servicerequest_SharePointDocuments`; the coordinator form also includes an `SR Evidence Reviews` subgrid for `hx_servicedocument`. |
| Model-driven Service Request UX | Live metadata shows `Service Request - Coordinator` as the internal app form and the Power Pages upload support form retained for portal infrastructure; the coordinator form uses two-column sections, includes SharePoint Documents and Evidence Review subgrids, and `Active Service Requests` includes confirmation, customer, category, severity, priority, lifecycle, department, SLA due date, approval, ERP sync, and created-on columns. |
| Evidence review view cleanup | Live saved-query metadata for `Service Request Evidence Review` no longer contains deprecated `Service Request Document` view names; Active, Associated, Lookup, Quick Find, My, Inactive, and Advanced Find views use Evidence Review naming. |
| Routing/SLA matrix | Live Dataverse verification shows 80 active `Routing / SLA Rule` rows, one for each category/impact/urgency combination. Legacy sample-only rules are inactive. `Event Support + High + Normal` resolves to Client Services, High - 1 business day response, manager review required, and documentation required. |
| Routing Matrix editor | Model-driven app navigation now shows `Routing Matrix` instead of raw `Routing / SLA Rules`. The matrix loads 80 rules, saves inline rule edits through `Xrm.WebApi`, and opens the underlying `Routing / SLA Rule` record from the rule name. |
| Portal evidence-review security | Power Pages live source no longer exposes `hx_servicedocument` through Web API settings or a global create table permission; Evidence Review is internal-only. |
| Power Pages Web API hardening | Fresh PAC download from live site confirms explicit field allowlists for Service Request, Service Category, Routing Rule, Department, and SLA Policy; `Webapi/error/innererror=false`. |
| SharePoint upload permission fix | Fresh live download confirms `Service Request - Read - Contact` has `write=true`, `append=true`, and `appendto=true`, while child `Document Location - Upload - Service Request Parent` has `create=true`, `write=true`, and `append=true`; this enables the Power Pages document grid upload actions for owned requests. |
| Portal Service Request create allowlist | Fresh live download confirms `Webapi/hx_servicerequest/fields` includes both lookup columns and Web API bind properties: `hx_servicecategory`, `hx_Servicecategory`, `hx_customercontact`, and `hx_Customercontact`. |
| Power Pages email login | Fresh live download confirms `Authentication/Registration/LocalLoginByEmail=true` and `Authentication/UserManager/UserValidator/RequireUniqueEmail=true`, so local registration/login uses email instead of a separate username. |
| Service Request portal write scope | Fresh PAC download confirms `Service Request - Read - Contact` remains contact-scoped and `create=false`; `write=true` is required for native SharePoint document upload, while protected Service Request fields remain blocked by the plugin and are not exposed through the portal UI. |
| Protected internal field guard | Live Dataverse verification confirms `ESI - Guard critical request closure` filters lifecycle, approval, ERP, routing, SLA, documentation, status, and internal-note fields; plugin config includes internal user IDs plus `agent@hellosmart.ca`, `manager@hellosmart.ca`, and `forrest@hellosmart.ca` email fallback. |
| Plugin routing | Critical funding request routed to Finance with 4 hour SLA. |
| Closure guard | Smoke test blocked undocumented critical closure and allowed closure only after an accepted resolution evidence-review row with a SharePoint file URL was created. |
| Model-driven app | Coordinator queue and Service Request form open in the app. |
| Approval/ERP flow | Active, solution-aware, includes approval, HelloX OAuth token request, Bearer-token HTTP sync to HelloX mock ERP, sync log, reject branch, and catch error-log scope. |
| Confirmation email flow | Active, solution-aware, sends generated confirmation number to applicant and logs missing/failed email cases to System Error Logs. |
| Model-driven dashboards | Exported solution contains `ESI - Coordinator Operations Dashboard`, `ESI - Manager Approval Dashboard`, and `ESI - Integration Monitoring Dashboard`; app module includes all three dashboard components. |
| Hidden HelloX ERP console | Source created under `static-site/esi/`; endpoint function created under `functions/api/esi-service-requests.js`. |

## Commands Used

```bash
dotnet build src/plugins/ServiceIntake.Plugins/ServiceIntake.Plugins.csproj
dotnet build src/scripts/ServiceIntake.Provisioning/ServiceIntake.Provisioning.csproj
dotnet run --project src/tests/ServiceIntake.PluginPolicy.Tests/ServiceIntake.PluginPolicy.Tests.csproj
dotnet list src/scripts/ServiceIntake.Provisioning/ServiceIntake.Provisioning.csproj package --vulnerable --include-transitive
RUN_VALIDATION_TESTS=true dotnet run --project src/scripts/ServiceIntake.Provisioning/ServiceIntake.Provisioning.csproj
REGISTER_PLUGINS_ONLY=true dotnet run --project src/scripts/ServiceIntake.Provisioning/ServiceIntake.Provisioning.csproj
VERIFY_SECURITY_HARDENING=true dotnet run --project src/scripts/ServiceIntake.Provisioning/ServiceIntake.Provisioning.csproj
node --check /Volumes/Forrest/Users/Forrest/Github/HelloXTech-Official-Website/functions/api/esi-service-requests.js
node scripts/check-site.mjs

pac pages download --webSiteId 8c12ac01-467a-4fa8-8034-50b8028de647 --path /tmp/esi-powerpages-live-20260520223111 --modelVersion Enhanced --overwrite
pac pages upload --path /tmp/esi-powerpages-live-20260520223111/enterprise-service-intake---enterprise-service-intake-hellox --modelVersion Enhanced
pac pages upload --path powerpages-live/enterprise-service-intake---enterprise-service-intake-hellox --modelVersion Enhanced --forceUploadAll
pac pages download --path /tmp/esi-portal-verify --webSiteId 8c12ac01-467a-4fa8-8034-50b8028de647 --modelVersion Enhanced --overwrite
pac pages download --path /tmp/esi-pages-verify.<id> --webSiteId 8c12ac01-467a-4fa8-8034-50b8028de647 --overwrite --modelVersion Enhanced
node --check src/powerpages/web-files/service-intake.js
pac pcf push --solution-unique-name EnterpriseServiceIntake --verbosity minimal
ENSURE_CONFIRMATION_EMAIL_FLOW=true dotnet run --project src/scripts/ServiceIntake.Provisioning/ServiceIntake.Provisioning.csproj

pac solution publish
pac solution export --name EnterpriseServiceIntake --path solution/export/Enterprise_ServiceIntake_ForrestZhang_unmanaged.zip --overwrite
pac solution export --name EnterpriseServiceIntake --path solution/export/Enterprise_ServiceIntake_ForrestZhang_managed.zip --managed --overwrite
pac solution unpack --zipfile solution/export/Enterprise_ServiceIntake_ForrestZhang_managed.zip --folder solution/unpacked/managed --packagetype Managed --clobber --allowWrite
pac solution unpack --zipfile solution/export/Enterprise_ServiceIntake_ForrestZhang_unmanaged.zip --folder solution/unpacked/unmanaged --packagetype Unmanaged --clobber --allowWrite
```

## Known Limitations

- Email delivery can be restricted in trial tenants. Use the Office 365 Outlook action output, Approval records, and Flow run history if emails do not arrive externally.
- DOCX visual rendering could not be completed in this local shell because LibreOffice/`soffice` is not installed. The generated PDF is 9 pages and the first-page Quick Look thumbnail was inspected.
- The protected mock ERP endpoint is hosted at `https://hellox.ca/api/mock/enterprise-service-intake/erp`; the OAuth token endpoint is `https://hellox.ca/api/mock/oauth/token`; the hidden demo console is `https://hellox.ca/esi/`.
- The PCF control is included in the solution and bound on the Service Request coordinator form through exported form XML.
