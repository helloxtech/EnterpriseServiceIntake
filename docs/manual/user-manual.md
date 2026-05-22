# Enterprise Service Intake User Manual

Version: V1  
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
| Hidden ERP console | https://hellox.ca/esi/ | Mock ERP endpoint demonstration and intentional failure mode |

Reviewer passwords are not stored in Git. The administrator should share passwords out of band and rotate them after the interview.

## External Customer Portal

### Sign In

1. Open the Power Pages site.
2. Select `Sign in`.
3. Sign in with a reviewer/customer account.
4. Confirm that customer pages do not expose internal-only notes, error logs, approval details, or integration payloads.

### Create Or Resume A Request

1. Select `New service request` to start a new intake.
2. Use `My requests` to resume an existing draft or review submitted requests.
3. Use `Save for later` if you want to leave the intake before final submission.
4. Return through `My requests` and select the saved draft.

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

Expected result: the request moves to Submitted, the confirmation email flow becomes eligible, and internal users can see the routed request in the model-driven app.

## Internal Coordinator App

### Navigation Groups

The model-driven app is organized into three groups:

| Group | Use |
| --- | --- |
| Intake Work | Service Requests and Service Request Evidence Reviews |
| Routing Configuration | Routing Matrix, Departments, SLA Policies, and Service Categories |
| Monitoring | System Error Logs and External Sync Logs |

### Review A Service Request

1. Open `Intake Work` > `Service Requests`.
2. Use `Active Service Requests` to scan confirmation number, customer, category, severity, priority, lifecycle status, assigned department, SLA due date, approval status, ERP sync status, and created date.
3. Open a request.
4. Review customer/request details, triage inputs, routing/SLA, approval/ERP sync, and resolution fields.
5. Use the PCF status indicator as a compact visual summary of severity, SLA, approval, and sync state.

Expected result: internal users can triage the request without switching to raw table views.

### Review Documents

1. Open a Service Request.
2. Open the `Documents` tab.
3. Use the SharePoint Documents grid to view files uploaded through Power Pages document management.
4. Use the `SR Evidence Reviews` subgrid to record internal review status, file URL, document type, verification status, and notes.

Expected result: SharePoint Documents remain the file storage surface, while Service Request Evidence Review stores Dataverse-owned review metadata.

### Close A Critical Request

1. Open a critical request.
2. Try to close it without internal resolution notes and accepted evidence.
3. Confirm that the plugin blocks the close.
4. Add internal resolution notes.
5. Add or update an accepted Service Request Evidence Review row with a SharePoint file URL.
6. Close the request again.

Expected result: closure is allowed only after the required internal documentation exists.

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

### Flow Run Histories

Use Power Automate run history to prove the confirmation email, approval, ERP sync, and Catch paths. This is especially useful when tenant email delivery is restricted.

## Demo Accounts

| Account | Purpose |
| --- | --- |
| `forrest@hellosmart.ca` | Admin/customizer reviewer |
| `agent@hellosmart.ca` | Internal service coordinator |
| `manager@hellosmart.ca` | Approval manager |

Do not commit passwords or OAuth secrets. Share them separately for review and rotate them after the interview.

## Notes For Reviewers

- The portal is private for the interview tenant.
- The Power Pages UI intentionally uses customer-facing labels such as `Impact level`, `Urgency`, `Team`, and `Mitacs review`.
- The model-driven app intentionally uses internal terminology such as Service Request Evidence Review, System Error Log, and External Sync Log.
- The raw `Routing / SLA Rule` table still exists for Dataverse administration, but normal rule maintenance should use `Routing Matrix`.
- No final solution export should be produced until the administrator requests it.
