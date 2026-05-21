# QA 2 UAT Report - Functional End-To-End

## Scope

End-to-end business functionality: Dataverse model, routing/SLA, confirmation number, Power Pages submission, document upload, approval, HelloX OAuth ERP sync, confirmation email, model-driven app views/dashboards, and seeded sample data.

## Environment

| Item | Value |
| --- | --- |
| Dataverse | `https://mitacs.crm.dynamics.com/` |
| Model-driven app | `https://mitacs.crm.dynamics.com/main.aspx?appid=3de4f813-b454-f111-bec7-000d3a3aca8f` |
| Power Pages | `https://enterprise-service-intake-hellox.powerappsportals.com` |
| Test date | 2026-05-21 |

## Test Approach

- Reviewed documentation, plugin source, provisioning source, Power Pages source, workflow JSON, table permissions, and exported solution metadata.
- Ran safe local/static checks for JavaScript and .NET projects.
- Did not use credentials or execute live authenticated approval flows in this pass.

## Test Cases

| ID | Area | Steps | Expected | Actual | Status | Severity | Evidence |
| --- | --- | --- | --- | --- | --- | --- | --- |
| FUN-01 | Dataverse model | Review custom tables/fields. | Request, routing, SLA, document review, sync log, and error log tables exist. | Provisioning and solution source define required model. | Pass | Low | `src/scripts/ServiceIntake.Provisioning/Program.cs`, `README.md` |
| FUN-02 | Confirmation number | Inspect metadata; live submit recommended. | `SR-{yyyyMMdd}-{SEQNUM}` generated server-side. | Static evidence confirms autonumber pattern; live submit not rerun. | Partial | Medium | `Program.cs`, `docs/evidence/verification.md` |
| FUN-03 | Routing/SLA | Submit Funding Agreement + Critical + Urgent. | Finance, 4-hour SLA, approval/docs required. | Plugin/rules implement this path. Live evidence already recorded. | Pass | Medium | `ServiceRequestRoutingPlugin.cs`, `docs/evidence/verification.md` |
| FUN-04 | Portal intake create | Complete valid 4-step portal form. | Creates `hx_servicerequest` and binds contact/category. | Web API payload and create path present. Auth live test not repeated. | Partial | Medium | `src/powerpages/web-files/service-intake.js` |
| FUN-05 | Dynamic preview | Change routing inputs before submit. | Preview updates without page reload. | JS loads active rules and updates preview. | Pass | Low | `src/powerpages/web-files/service-intake.js` |
| FUN-06 | SharePoint upload | After submit, click upload supporting files. | Redirects to document upload page and stores files through document management. | Source and prior evidence confirm flow; live upload not repeated in this pass. | Partial | Medium | `docs/evidence/verification.md` |
| FUN-07 | Closure guard | Close critical request without notes/evidence, then with accepted evidence. | First blocked, second allowed. | Plugin enforces notes + accepted verified evidence. | Pass | High | `ServiceRequestClosureGuardPlugin.cs` |
| FUN-08 | Approval + ERP sync | Trigger pending approval request and approve as manager. | Approval starts; OAuth token requested; ERP POST returns external ID; sync log created. | Workflow source contains approval, OAuth token action, Bearer ERP POST, Dataverse writeback, sync/error logs. Live run not repeated. | Partial | High | `solution/unpacked/*/Workflows/ESI-ApprovalandERPSync-*.json` |
| FUN-09 | Confirmation email | Submit request with applicant email. | Email sends confirmation or logs failure. | Confirmation email flow source handles send, skip, and failure logging. Live email not repeated. | Partial | Medium | `Program.cs` |
| FUN-10 | Queues/dashboards | Open model-driven app views/dashboards. | Coordinator, manager, and integration dashboards available. | Source/provisioning/export evidence indicates dashboards exist. Live visual not repeated. | Partial | Low | `README.md`, solution source |
| FUN-11 | Seed data | Review seeded demo scenarios. | Tables have rows for pending, approved/synced, rejected, failed, in-progress. | Provisioning seeds representative data. | Pass | Low | `Program.cs` |
| FUN-12 | Static quality | Run build/syntax checks. | Build/syntax pass. | JS syntax checks and provisioning/plugin builds pass in local verification. | Pass | Low | Command output |

## Findings

No functional blockers were found from static/source UAT. The architecture is coherent: Dataverse owns state, plugins own transactional rules, Power Pages owns intake/upload handoff, Power Automate owns approval/email/integration, and the model-driven app owns internal operations.

Residual risk remains for live authenticated execution: portal sign-in, actual file upload, manager approval, email delivery, and dashboard rendering should be captured immediately before final submission.

## Developer Recommendations

1. Run one fresh happy path and capture evidence: portal submit, confirmation number, upload, model-driven row, approval run, ERP external ID, confirmation email or run history.
2. Capture one negative closure-guard evidence item.
3. Add known seeded record names and one known confirmation number to the live demo script.
4. Keep secrets out of docs and exported source.

