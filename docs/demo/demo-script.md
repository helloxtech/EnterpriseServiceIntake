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
2. Start a new service request.
3. Show that the first step cannot continue until required request details are complete.
4. Use the explicit step actions: `Continue to impact`, `Continue to documents`, and `Review request`.
5. Select `Funding Agreement`, `Critical`, and `Urgent`.
6. Show the dynamic preview updating without a page reload:
   - Department: Finance.
   - SLA: 4 hour response target.
   - Manager approval required.
   - Resolution documentation required.
7. Complete the review step and submit.
8. Show confirmation message.
9. Select `Upload supporting files` to open the secure SharePoint document upload page for the saved request.

Expected result:

- A new Service Request row is created in Dataverse.
- Confirmation number follows `SR-yyyyMMdd-######`.
- Routing preview says Finance with a 4 hour response target.
- Supporting files are uploaded after submission through Power Pages document management into SharePoint for that request.

## 3. Internal Coordinator App

Open: https://mitacs.crm.dynamics.com/main.aspx?appid=3de4f813-b454-f111-bec7-000d3a3aca8f

Steps:

1. Open `Service Requests`.
2. Find the submitted request or use the seeded critical request.
3. Show:
   - Confirmation number.
   - Assigned Department.
   - Applied SLA.
   - Approval Status.
   - Integration Sync Status.
   - Internal Resolution Notes.
   - PCF SLA/status control package in the solution source.
4. Open `Dashboards` and show:
   - `ESI - Coordinator Operations Dashboard` for queue, severity, lifecycle, and documentation-risk views.
   - `ESI - Manager Approval Dashboard` for manager approval and documentation guardrails.
   - `ESI - Integration Monitoring Dashboard` for sync status, sync attempts, and open automation errors.

Talking point:

- Portal users never see internal notes, integration payloads, error logs, or coordinator-only fields.

## 4. Confirmation Email Flow

Open Power Automate flow `ESI - Send Confirmation Email`.

Show:

- Dataverse create trigger on Service Requests.
- `Try - send confirmation email` scope.
- Contact/email validation.
- Office 365 Outlook `Send an email (V2)` action with formatted confirmation number.
- Dataverse update confirming the notification was sent.
- Catch/skip paths writing to System Error Logs.

Smoke-test evidence:

- Request `SR-20260521-001018` updated customer-visible notes to confirm the email was sent through Office 365 Outlook.
- The flow run history shows `Send_confirmation_email` succeeded with subject `Mitacs service request received - SR-20260521-001018`.

## 5. Approval And ERP Sync Flow

Open Power Automate flow `ESI - Approval and ERP Sync`.

Show:

- Dataverse trigger filtered to approval-required, pending, unsynced requests.
- `Try - approval and ERP sync` scope.
- Approval assigned to `manager@hellosmart.ca`.
- HTTP POST to `https://hellox.ca/api/esi-service-requests`.
- Dataverse update that stores external ERP ID.
- External Sync Log creation.
- Reject branch.
- `Catch - log automation error` scope.

If email is restricted:

- Use Office 365 Outlook action output, Flow run history, and Approval records as evidence.
- The assignment explicitly allows run history evidence when tenant email delivery is limited.

## 6. Failure Handling Demo

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

## 7. Plugin Guardrail Demo

Use the seeded critical request or run the provisioning smoke test.

Expected behavior:

- Setting a critical request to Resolved/Closed without internal resolution notes and accepted resolution evidence is blocked.
- Adding internal resolution notes plus an accepted `Service Request Evidence Review` row with a SharePoint file URL allows closure.

Command evidence:

```bash
RUN_VALIDATION_TESTS=true dotnet run --project src/scripts/ServiceIntake.Provisioning/ServiceIntake.Provisioning.csproj
```

## 8. ALM Evidence

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
| Why not use `reqres.in`? | Its POST endpoint currently requires an API key. I used a HelloX-hosted mock endpoint so the demo can run without storing a third-party key and can intentionally trigger the Catch path. |
| What is the hidden HelloX `/esi/` page for? | It is a noindex demo console at `https://hellox.ca/esi/` for showing the mock ERP endpoint, deterministic external IDs, and the failure mode outside Power Automate. It is not linked from the public site navigation. |
| How do external users only see their data? | Contact-scoped Power Pages table permissions restrict request access; internal-only columns and tables are not exposed on the portal. |
| How is the solution deployable? | Components are solution-aware, exported as managed/unmanaged zips, and unpacked with PAC for source control. |
