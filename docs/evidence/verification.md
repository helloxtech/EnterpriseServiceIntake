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
| Cloud flow ID | `ab021e8c-bb54-f111-bec7-000d3a3acaff` |

## Verified Build Steps

| Check | Result |
| --- | --- |
| C# plugin build | Passed |
| Provisioning utility build | Passed with upstream SDK NU1903 warnings |
| PCF build/push | Passed |
| Power Pages upload | Passed |
| Power Pages UX validation update | Passed |
| Power Pages live refresh/download | Passed |
| Power Pages SharePoint upload fix | Uploaded and re-downloaded key live files for verification |
| Managed solution export | Passed |
| Unmanaged solution export | Passed |
| Managed solution unpack | Passed |
| Unmanaged solution unpack | Passed |

## Verified Live Behavior

| Behavior | Evidence |
| --- | --- |
| Portal dynamic preview | Funding Agreement + Critical + Urgent shows Finance, 4 hour target, approval required, documentation required. |
| Portal submission | `Portal Demo - Critical funding request 2` created with confirmation `SR-20260521-001004`. |
| Portal step navigation | Explicit Continue, Back, Review request, and Submit buttons added; required fields block progression before the next step. |
| Portal SharePoint document path | Localized Home page now removes the pre-submit file input, creates the Service Request first, and opens `/request-documents/?id=<service-request-id>` from the success modal. |
| Upload page diagnostics | Request Documents page now shows a visible ready/warning/error status for the SharePoint document grid instead of failing silently. |
| SharePoint upload smoke test | `Portal smoke file upload 2026-05-21T07-24-18-212Z` created confirmation `SR-20260521-001025`; `esi-upload-smoke.txt` uploaded through the document grid and appeared on the page. |
| SharePoint document location | Dataverse returned `sharepointdocumentlocationid` `4c2b05a7-e654-f111-89e7-0022488fbd9b` for request `df11b114-e654-f111-bec7-000d3a3aca8f`. |
| Plugin routing | Critical funding request routed to Finance with 4 hour SLA. |
| Closure guard | Smoke test blocked undocumented critical closure and allowed documented closure. |
| Model-driven app | Coordinator queue and Service Request form open in the app. |
| Cloud flow | Active, solution-aware, includes approval, HTTP sync, sync log, reject branch, and catch error-log scope. |

## Commands Used

```bash
dotnet build src/plugins/ServiceIntake.Plugins/ServiceIntake.Plugins.csproj
dotnet build src/scripts/ServiceIntake.Provisioning/ServiceIntake.Provisioning.csproj
RUN_VALIDATION_TESTS=true dotnet run --project src/scripts/ServiceIntake.Provisioning/ServiceIntake.Provisioning.csproj

pac pages upload --path powerpages-live/enterprise-service-intake---enterprise-service-intake-hellox --modelVersion Enhanced --forceUploadAll
pac pages download --path /tmp/esi-pages-verify.<id> --webSiteId 8c12ac01-467a-4fa8-8034-50b8028de647 --overwrite --modelVersion Enhanced
node --check src/powerpages/web-files/service-intake.js
pac pcf push --solution-unique-name EnterpriseServiceIntake --verbosity minimal

pac solution publish
pac solution export --name EnterpriseServiceIntake --path solution/export/Enterprise_ServiceIntake_ForrestZhang_unmanaged.zip --overwrite
pac solution export --name EnterpriseServiceIntake --path solution/export/Enterprise_ServiceIntake_ForrestZhang_managed.zip --managed --overwrite
pac solution unpack --zipfile solution/export/Enterprise_ServiceIntake_ForrestZhang_managed.zip --folder solution/unpacked/managed --packagetype Managed --clobber --allowWrite
pac solution unpack --zipfile solution/export/Enterprise_ServiceIntake_ForrestZhang_unmanaged.zip --folder solution/unpacked/unmanaged --packagetype Unmanaged --clobber --allowWrite
```

## Known Limitations

- Email delivery can be restricted in trial tenants. Use Approval records and Flow run history if approval emails do not arrive.
- `reqres.in` POST currently requires an API key, so the mock ERP uses `api.restful-api.dev`.
- The PCF control is included in the solution and source. If the imported environment does not automatically bind it on the form, add it to the coordinator field in the model-driven form designer.
