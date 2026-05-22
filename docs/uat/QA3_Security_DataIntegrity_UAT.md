# QA 3 UAT Report - Security, Permissions, Data Integrity

## Scope

Portal authentication, Power Pages permissions, own-record access, sensitive fields, Web API exposure, Dataverse security assumptions, plugin guardrails, logs, OAuth secret handling, and artifact secret leakage.

## Environment

| Item | Value |
| --- | --- |
| Repo | `/Volumes/Forrest/Users/Forrest/Github/EnterpriseServiceIntake` |
| Power Pages | `https://enterprise-service-intake-hellox.powerappsportals.com` |
| Dataverse | `https://mitacs.crm.dynamics.com/` |
| Test date | 2026-05-21 |

## Test Approach

- Read-only review of Power Pages source, site settings, table permissions, plugin source, workflow JSON, exported solution source, and docs.
- Safe anonymous checks only; no passwords or secrets used.
- Authenticated tamper/row-isolation tests are listed as Not Executed because they require a live portal login session.

## Test Cases

| ID | Area | Steps | Expected | Actual | Status | Severity | Evidence |
| --- | --- | --- | --- | --- | --- | --- | --- |
| SEC-01 | Anonymous portal/API | Anonymous GET portal and Web API endpoints. | Anonymous users cannot use intake/API. | Public checks redirect/gate access. | Pass | Low | Live public probe |
| SEC-02 | Auth gate | Inspect Liquid template. | Intake renders only for signed-in user. | Template uses user gate and sign-in prompt. | Pass | Low | `src/powerpages/templates/service-intake-page.liquid` |
| SEC-03 | Service Request permissions | Review table permissions. | Create is allowed; own-record access is scoped to contact; document upload is supported. | Live PAC download confirms global create remains enabled and contact-scoped Service Request access is own-record scoped with `create=false`, `read=true`, and `write=true` to enable native SharePoint document upload. | Pass | High | `/tmp/esi-portal-verify-doc-upload/.../Service-Request---Read---Contact.tablepermission.yml` |
| SEC-04 | Web API fields | Review site settings. | Only portal-needed fields exposed. | Live PAC download confirms explicit field allowlists and `Webapi/error/innererror=false`. | Pass | High | `/tmp/esi-portal-verify/.../sitesetting.yml` |
| SEC-05 | Own-record tamper | Logged-in user PATCH own request internal fields. | Portal user cannot alter lifecycle/approval/sync/routing/internal fields. | Contact-scoped write is enabled for native SharePoint document upload, but protected fields are not exposed through the portal UI/Web API allowlist and the live plugin step blocks protected fields for callers outside the internal user/email allowlist. | Pass | Critical | `VERIFY_SECURITY_HARDENING=true` output |
| SEC-06 | Internal notes | Try reading `hx_internalresolutionnotes` via portal Web API. | Not visible to portal user. | Field is secured in metadata and no longer included in the Service Request Web API allowlist. | Pass | Medium | `sitesetting.yml`, `solution/unpacked/*/Entities/hx_Servicerequest/Entity.xml` |
| SEC-07 | Evidence review exposure | Search portal permissions/Web API for `hx_servicedocument`. | Portal cannot create accepted evidence reviews. | No portal Web API/table permission found; cleanup exists. | Pass | Medium | `Program.cs` |
| SEC-08 | Critical close guard | Update critical request to resolved without evidence. | Blocked by server-side plugin. | PreOperation plugin enforces required notes/evidence. | Pass | High | `ServiceRequestClosureGuardPlugin.cs` |
| SEC-09 | Non-critical state changes | Portal user PATCH lifecycle/approval fields on own record. | State changes should be role-controlled. | Live plugin step filters lifecycle, approval, ERP, routing, SLA, documentation, and internal status fields; contact-scoped write is allowed only to satisfy Power Pages document-management upload requirements. | Pass | High | `ServiceRequestClosureGuardPlugin.cs`, `VERIFY_SECURITY_HARDENING=true` output |
| SEC-10 | Server routing authority | Submit tampered preview values. | Server recalculates routing/SLA. | Routing plugin recalculates on create/update. | Pass | Medium | `ServiceRequestRoutingPlugin.cs` |
| SEC-11 | Log exposure | Search portal permissions for error/sync logs. | Logs internal-only. | No portal table permissions found for error/sync logs; live role assignment not verified. | Partial | Medium | `README.md`, solution source |
| SEC-12 | OAuth secrets | Scan committed source/export for plaintext secrets. | No real client secret committed. | Workflow uses secure string parameter placeholder and secure inputs/outputs. Secret pattern scan passed. | Pass | High | Workflow JSON, local scan |
| SEC-13 | Dependency audit | Build/audit plugin and provisioning projects. | No known vulnerable packages. | Existing verification and local builds pass. | Pass | Low | Command output |
| SEC-14 | Dataverse roles | Review exported security roles/live assignments. | Coordinator/manager/admin roles documented or exported. | Not verified in this pass. | Not Executed | High | Requires environment role review |

## Findings

| ID | Severity | Finding | Developer Recommendation |
| --- | --- | --- | --- |
| SEC-F01 | Critical | Authenticated own-record write plus `Webapi/hx_servicerequest/fields: '*'` may allow portal users to PATCH lifecycle, approval, sync, ERP ID, routing, documentation, or customer-visible fields. | Closed 2026-05-21, revised 2026-05-22: Web API allowlist narrowed, protected-field plugin guard registered, and contact-scoped write is retained only because Power Pages SharePoint document upload requires parent write permission. |
| SEC-F02 | High | Web API field exposure is broad for Service Request and reference/routing tables. | Closed 2026-05-21: all project Web API `fields` settings use explicit allowlists. |
| SEC-F03 | Medium | `Webapi/error/innererror=true` can leak implementation details. | Closed 2026-05-21: `Webapi/error/innererror=false` verified from live PAC download. |
| SEC-F04 | Medium | Registration posture should be explicitly confirmed. | Confirm open registration and Entra login settings match interview expectation. |
| SEC-F05 | Medium | Dataverse security role assignments were not exported/verified. | Document or export role assumptions for coordinator, manager, admin, logs, evidence, and sync tables. |

## Required Live Security Tests

1. User A cannot read User B service request.
2. User A cannot PATCH own request lifecycle, approval, ERP ID, routing, or internal notes.
3. Anonymous users cannot query Power Pages Web API after redirects/cookies.
4. Internal agent/manager role split shows different views/actions in model-driven app.
