# Live Demo Script

## 1. Architecture Walkthrough

Open `README.md` and show:

- ERD and core tables.
- Why routing and close validation are plugins.
- Why confirmation email, approvals, and ERP sync are in Power Automate.
- Where PCF, Power Pages, and solution source live in the repo.

## 2. External Portal Submission

Open: https://enterprise-service-intake-hellox.powerappsportals.com

Steps:

1. Sign in as a reviewer/customer account.
2. Show `My requests` in the top navigation and, from the profile page, in the left navigation.
3. Open `My requests` and show that the page lists the signed-in user's own drafts/submitted requests.
4. Start or resume a service request.
5. Show `Save for later`; the request is saved as a draft and can be resumed from `My requests`.
6. Show that the first step cannot continue until required request details are complete.
7. Use the explicit step actions: `Continue to impact`, `Continue to documents`, and `Review request`.
8. Select `Funding Agreement`, `Critical`, and `Urgent`.
9. Show the dynamic response estimate updating without a page reload:
   - Team: Finance.
   - SLA: 4 hour response target.
   - Mitacs review required.
   - Required files before final submission.
10. Complete the review step and submit.
11. When files are required, show that Step 3 creates/saves the request and opens the secure upload area in the same step.
12. Show that `Review request` stays disabled until at least one file is uploaded.
13. Upload a file, wait for the uploaded-file count, then continue to review and submit.
14. For a request where files are optional, show that Step 3 allows `Review request` immediately and optional files can be added after submission.

Expected result:

- A draft Service Request row is created before Step 3 when required files must be uploaded before submission.
- After required files are uploaded and the user submits from Review, the request is moved to Submitted.
- Confirmation number follows `SR-yyyyMMdd-######`.
- Response estimate says Finance with a 4 hour response target.
- Required files cannot be bypassed from the portal when the matched routing rule requires documentation.
- Optional supporting files can still be uploaded after submission through the secure file upload page for that request.

## 3. Internal Coordinator App

Open: https://mitacs.crm.dynamics.com/main.aspx?appid=3de4f813-b454-f111-bec7-000d3a3aca8f

Steps:

1. Point out the three app navigation groups: `Intake Work`, `Routing Configuration`, and `Monitoring`.
2. Open `Intake Work` > `Service Requests`.
3. Find the submitted request or use the seeded critical request.
4. Show:
   - Confirmation number.
   - Assigned Department.
   - Applied SLA.
   - Approval Status.
   - Integration Sync Status.
   - Internal Resolution Notes.
   - PCF SLA/status control package in the solution source.
5. Open the `Documents` tab and show:
   - The SharePoint Documents grid for the actual uploaded files.
   - The `SR Evidence Reviews` subgrid for internal evidence review metadata and file links.
6. Open `Dashboards` and show:
   - `ESI - Coordinator Operations Dashboard` for queue, severity, lifecycle, and documentation-risk views.
   - `ESI - Manager Approval Dashboard` for manager approval and documentation guardrails.
   - `ESI - Integration Monitoring Dashboard` for sync status, sync attempts, and open automation errors.

Talking point:

- Portal users never see internal notes, integration payloads, error logs, or coordinator-only fields.

## 4. Routing Matrix Administration

Open `Routing Configuration` > `Routing Matrix`.

Show:

- Category tabs keep the 80-rule matrix readable by showing one service category at a time.
- Summary cards show department, SLA distribution, manager-review count, and documentation count.
- Each row is an Impact level and each column is an Urgency level.
- Manager review and required-document values use toggle controls.
- Department and SLA can be edited inline.
- Selecting a rule name opens the underlying `Routing / SLA Rule` record for audit/detail edits.

Safe demo path:

1. Change one low-risk toggle.
2. Wait for `Change saved`.
3. Change it back to the original value.
4. Explain that the portal preview and plugin use these same Dataverse rule rows.

## 5. Confirmation Email Flow

Open Power Automate flow `ESI - Send Confirmation Email`.

Show:

- Dataverse submitted-status trigger on Service Requests.
- `Try - send confirmation email` scope.
- Contact/email validation.
- Office 365 Outlook `Send an email (V2)` action with formatted confirmation number.
- Dataverse update confirming the notification was sent.
- Catch/skip paths writing to System Error Logs.

Smoke-test evidence:

- Request `SR-20260521-001018` updated customer-visible notes to confirm the email was sent through Office 365 Outlook.
- The flow run history shows `Send_confirmation_email` succeeded with subject `Mitacs service request received - SR-20260521-001018`.

## 6. Approval And ERP Sync Flow

Open Power Automate flow `ESI - Approval and ERP Sync`.

Show:

- Dataverse trigger filtered to approval-required, pending, unsynced requests.
- `Try - approval and ERP sync` scope.
- Approval assigned to `manager@hellosmart.ca`.
- `HTTP - get HelloX OAuth token` using the client-credentials grant.
- HTTP POST to `https://hellox.ca/api/mock/enterprise-service-intake/erp` with the Bearer token.
- Dataverse update that stores external ERP ID.
- External Sync Log creation.
- Reject branch.
- `Catch - log automation error` scope.

If email is restricted:

- Use Office 365 Outlook action output, Flow run history, and Approval records as evidence.
- The assignment explicitly allows run history evidence when tenant email delivery is limited.

## 7. Failure Handling Demo

Preferred safe demo:

1. Open the flow definition and point to the Catch scope.
2. Open Dataverse `System Error Logs`.
3. Explain that HTTP/approval failures are captured with run correlation ID and failed scope details.

Optional live failure demo:

1. Temporarily append `?fail=true` to the HTTP URI or use the hidden HelloX `/esi/` console failure mode.
2. Trigger a pending critical request.
3. Show failed run.
4. Show System Error Log row and request marked failed.
5. Restore the HTTP URI.

## 8. Plugin Guardrail Demo

Use the seeded critical request or run the provisioning smoke test.

Expected behavior:

- Setting a critical request to Resolved/Closed without internal resolution notes and accepted resolution evidence is blocked.
- Adding internal resolution notes plus an accepted `Service Request Evidence Review` row with a SharePoint file URL allows closure.

Command evidence:

```bash
RUN_VALIDATION_TESTS=true dotnet run --project src/scripts/ServiceIntake.Provisioning/ServiceIntake.Provisioning.csproj
```

## 9. ALM Evidence

Show:

- Managed solution zip in `solution/export/`.
- Unpacked source in `solution/unpacked/`.
- Plugin source in `src/plugins/`.
- PCF source in `src/pcf/`.
- Power Pages source in `src/powerpages/`.
- Git commit and GitHub repository.

## Prepared Q&A

| Question | Answer |
| --- | --- |
| Why a plugin for routing instead of Flow? | Routing affects transactional Service Request creation and must apply equally from portal, model app, import, API, or automation. A PreOperation plugin keeps it centralized and non-bypassable. |
| Why a plugin for closure guardrail? | The requirement explicitly says agents cannot bypass documentation requirements. A server-side PreOperation plugin is the strongest Dataverse enforcement point, and it checks accepted evidence rows instead of trusting a user-editable checkbox. |
| Why Flow for approvals and ERP sync? | It has native approval records, connector run history, retries, connection references, and a clear Try/Catch pattern for integration work. |
| Why Flow for confirmation email? | Email delivery is asynchronous and should not block Service Request creation; Flow gives run history, Outlook delivery evidence, and centralized error logging. |
| Why upload after submit instead of before submit? | SharePoint document management needs a saved Dataverse record to associate files with the correct folder, so the portal creates the request first and then opens the document upload page with the request ID. |
| How are required documents enforced in the portal? | The portal creates a Draft before the Files step, embeds the secure request-specific upload page, counts uploaded files, and enables Review only after at least one file exists. The server-side plugin must continue to allow only that Draft to Submitted portal transition while blocking protected internal fields. |
| Why not use `reqres.in`? | Its POST endpoint currently requires an API key. I used a HelloX-hosted mock endpoint so the demo can run without storing a third-party key and can intentionally trigger the Catch path. |
| What is the hidden HelloX `/esi/` page for? | It is a noindex demo console at `https://hellox.ca/esi/` for showing the mock ERP endpoint, deterministic external IDs, and the failure mode outside Power Automate. It is not linked from the public site navigation. |
| How do external users only see their data? | Contact-scoped Power Pages table permissions restrict request access; internal-only columns and tables are not exposed on the portal. |
| How is the solution deployable? | Components are solution-aware, exported as managed/unmanaged zips, and unpacked with PAC for source control. |
