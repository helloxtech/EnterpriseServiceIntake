# Enterprise Service Intake User Manual

Version: V2  
Date: 2026-05-22  
Audience: Mitacs reviewers, portal customers, internal coordinators, managers, and administrators.

## Purpose

This guide explains how to use the Enterprise Service Intake solution during the live review. It is intentionally practical: where to go, what to click, what result to expect, and where to verify the system behavior.

## Quick Access

| Area | URL / Location | Purpose |
| --- | --- | --- |
| Power Pages site | https://enterprise-service-intake-hellox.powerappsportals.com | External customer intake, drafts, required-file upload, and final submission |
| Model-driven app | https://mitacs.crm.dynamics.com/main.aspx?appid=3de4f813-b454-f111-bec7-000d3a3aca8f | Internal coordinator, manager, configuration, and monitoring experience |
| Maker solution | https://make.powerapps.com/environments/99dd50ed-a753-e37f-912c-78a022b12b09/solutions | Solution components, flows, tables, forms, views, and web resources |
| Hidden ERP console | https://hellox.ca/esi/ | View mock ERP sync attempts, returned external IDs, and failure-path evidence |

Reviewer passwords are not stored in Git. The administrator should share passwords out of band and rotate them after the interview.

## External Customer Portal

### Sign In

1. Open the Power Pages site.
2. Select `Sign in`.
3. Sign in with a reviewer/customer account.
4. Confirm that customer pages do not expose internal-only notes, error logs, approval details, or integration payloads.

Sign-in is required before creating, saving, resuming, or submitting a service request. This keeps each request and its supporting files linked to the correct portal account.

### Create Or Resume A Request

1. Select `New service request` to start a new intake.
2. Use `My requests` to resume an existing draft or review submitted requests.
3. Use `View details` on a request card to inspect the confirmation number, status, routing estimate, request text, and supporting-file requirement.
4. Use `Save for later` if you want to leave the intake before final submission.
5. Return through `My requests` and select the saved draft.

Expected result: draft requests remain in Draft and do not trigger the applicant confirmation email until the request is submitted.

### Complete Request Details

1. Enter a clear request title.
2. Select a service category.
3. Enter a description.
4. Select `Continue to impact`.

Expected result: missing required values are shown inline and the user cannot continue until they are corrected.

### Complete Impact And Urgency

1. Select an impact level.
2. Select an urgency.
3. Enter the business impact.
4. Watch the response estimate update without a full page reload.

Expected result: the estimate shows the likely team, response target, Mitacs review status, and required-file status based on the active routing matrix.

### Upload Supporting Files

For requests where documentation is required:

1. Continue to the Files step.
2. The portal creates or updates a Draft Service Request so SharePoint can associate files to the correct record.
3. Upload at least one file in the embedded secure upload area.
4. Wait until the uploaded-file count appears.
5. Select `Review request`.

Expected result: `Review request` remains disabled until at least one required file exists.

For requests where documentation is optional:

1. Continue through the Files step without uploading a file.
2. Submit from the Review step.
3. Add optional supporting files after submission if needed.

### Submit And Confirm

1. Review the request summary.
2. Select `Submit service request`.
3. Capture the confirmation number in the format `SR-yyyyMMdd-######`.
4. Return to `My requests` to view the submitted request.
5. Use `View details` if reviewers want to inspect the submitted request from the portal user's perspective.

Expected result: the request moves to Submitted, the confirmation email flow becomes eligible, and internal users can see the routed request in the model-driven app.

## Internal Coordinator App

### Navigation Groups

The model-driven app is organized into three groups:

| Group | Use |
| --- | --- |
| Intake Work | Service Requests and Service Request Evidence Reviews |
| Routing Configuration | Routing Matrix, Departments, SLA Policies, and Service Categories |
| Monitoring | System Error Logs and External Sync Logs |

### Role-Based Dashboards

| User | Required roles | Expected dashboards |
| --- | --- | --- |
| `agent@hellosmart.ca` | Basic User; ESI Service Coordinator | Operations Dashboard and Monitoring Dashboard |
| `manager@hellosmart.ca` | Basic User; ESI Approval Manager; Approvals User | Approval Dashboard and Monitoring Dashboard |
| `forrest@hellosmart.ca` | System Administrator or System Customizer | All dashboards for administration and review |

Notes:

- `Approvals User` supports Power Automate approval records; it does not control ESI dashboard visibility.
- If a recently changed dashboard list looks stale, sign out and back in or open a fresh browser session.

### Review A Service Request

1. Open `Intake Work` > `Service Requests`.
2. Use `Active Service Requests` to scan confirmation number, customer, category, severity, priority, lifecycle status, assigned department, SLA due date, approval status, ERP sync status, and created date.
3. Open a request.
4. Review customer/request details, triage inputs, routing/SLA, approval/ERP sync, and resolution fields.
5. Use the PCF status indicator as a compact visual summary of severity, SLA, approval, and sync state.

Expected result: internal users can triage the request without switching to raw table views.

### Resolve Or Complete A Request

1. Open a Service Request from the model-driven app.
2. Confirm `Lifecycle Status` is read-only on the form.
3. Use `Resolve Request` when the work has been resolved but still needs final completion.
4. Use `Complete Request` when the request is fully complete.

Expected result: coordinators use intentional command buttons for the two manual status changes. Other lifecycle statuses are set by portal submission, routing, approval, ERP sync, or automation.

### Review Documents

1. Open a Service Request.
2. Open the `Documents` tab.
3. Use the SharePoint Documents grid to view files uploaded through Power Pages document management.
4. Use the `SR Evidence Reviews` subgrid to record internal review status, file URL, document type, verification status, and notes.

Expected result: SharePoint Documents remain the file storage surface, while Service Request Evidence Review stores Dataverse-owned review metadata.

### Complete A Critical Request

1. Open a critical request.
2. Select `Complete Request` without internal resolution notes and accepted evidence.
3. Confirm that the plugin blocks completion.
4. Add internal resolution notes.
5. Add or update an accepted Service Request Evidence Review row with a SharePoint file URL.
6. Select `Complete Request` again.

Expected result: completion is allowed only after the required internal documentation exists.

## Routing Matrix Administration

### Edit Routing Rules From The Matrix

1. Open `Routing Configuration` > `Routing Matrix`.
2. Select a service category tab.
3. Review summary cards for department, SLA distribution, manager review, and required documents.
4. Find the Impact row and Urgency column you want to adjust.
5. Change Department or SLA from the dropdown.
6. Use toggles for `Review` and `Docs`.
7. Wait for the saved status.

Expected result: the web resource updates the same Dataverse `Routing / SLA Rule` rows used by the portal preview and the routing plugin.

Record count note: `Routing Matrix` presents the 80 active exact-match cells for `5 categories x 4 impact levels x 4 urgency levels`. The raw Dataverse `Routing / SLA Rule` table also includes one active `Generic fallback - unmatched request`, so the clean final rule set is 81 active rules total. Runtime logic filters to active rules and uses the fallback only when no exact active match exists.

### Open The Source Rule

1. Select the rule name inside a matrix cell.
2. Review the underlying `Routing / SLA Rule` record.
3. Use the source record for audit fields, owner/state checks, or advanced troubleshooting.

Recommended demo practice: if you change a rule during the live demo, change it back immediately after showing the saved status.

## Manager Approval And ERP Sync

1. Open the approval-required request.
2. Confirm approval status is pending.
3. Open Power Automate flow `ESI - Approval and ERP Sync`.
4. Review the Try scope, approval action, OAuth token request, protected HTTP POST, Service Request update, External Sync Log creation, and Catch scope.
5. Use Approval records and flow run history as evidence if email delivery is restricted.

Expected result: approved requests receive an external ERP ID and sync log; failed requests write a System Error Log.

## Monitoring And Troubleshooting

### System Error Logs

Use `Monitoring` > `System Error Logs` to review plugin, portal, flow, approval, and integration failures. Important columns include source component, stage, message, correlation ID, related request, resolved status, and created date.

### External Sync Logs

Use `Monitoring` > `External Sync Logs` to review ERP endpoint calls, HTTP status, external ID, payload snapshot, and timestamps.

### HelloX ERP Dashboard

Open https://hellox.ca/esi/ to view the mock ERP side of the integration. This hidden dashboard shows the requests received from Power Automate, returned external ERP IDs, accepted/failed counts, and breakdowns by status, priority, and department.

### Flow Run Histories

Use Power Automate run history to prove the confirmation email, approval, ERP sync, and Catch paths. This is especially useful when tenant email delivery is restricted.

## Demo Accounts

| Account | Purpose | Sign-in location | Credential note |
| --- | --- | --- | --- |
| `agent@hellosmart.ca` | Internal service coordinator | Model-driven app / Power Platform environment | Uses the temporary demo password shared separately by the administrator |
| `manager@hellosmart.ca` | Approval manager | Model-driven app / Power Platform environment | Uses the temporary demo password shared separately by the administrator |
| `forrest@hellosmart.ca` | Admin/customizer reviewer | Maker portal, model-driven app, and Power Pages administration | Admin password is shared separately by the administrator |

Do not commit passwords or OAuth secrets. Share them separately for review and rotate them after the interview.

Reviewer URLs:

- Power Platform environment: https://mitacs.crm.dynamics.com/
- Maker solution: https://make.powerapps.com/environments/99dd50ed-a753-e37f-912c-78a022b12b09/solutions
- Model-driven app: https://mitacs.crm.dynamics.com/main.aspx?appid=3de4f813-b454-f111-bec7-000d3a3aca8f
- Power Pages site: https://enterprise-service-intake-hellox.powerappsportals.com
- Hidden ERP console: https://hellox.ca/esi/

## Notes For Reviewers

- The portal is private for the interview tenant.
- The Power Pages UI intentionally uses customer-facing labels such as `Impact level`, `Urgency`, `Team`, and `Mitacs review`.
- The model-driven app intentionally uses internal terminology such as Service Request Evidence Review, System Error Log, and External Sync Log.
- The raw `Routing / SLA Rule` table still exists for Dataverse administration, but normal rule maintenance should use `Routing Matrix`.
- The clean final routing table should contain 81 active rules: 80 exact-match matrix rows plus one active generic fallback.
- No final solution export should be produced until the administrator requests it.
