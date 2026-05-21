# UAT Consolidated Report

Date: 2026-05-21

## QA Assignments

| QA | Focus | Primary Coverage |
| --- | --- | --- |
| QA 1 | Power Pages UI/UX | External portal, multi-step UX, dynamic preview, upload handoff, responsive/accessibility, branding |
| QA 2 | Functional E2E | Dataverse model, routing/SLA, portal submit, approval, ERP sync, email, app dashboards, sample data |
| QA 3 | Security/Data Integrity | Auth, table permissions, Web API exposure, sensitive fields, guardrails, logs, secrets |
| QA 4 | Performance/Resilience | Response timing, client/API cost, OAuth/ERP contract, Try/Catch, retry posture, dashboard scale |

## Executive Summary

The solution is functionally coherent and interview-demo ready from a source/static review perspective. The strongest implementation areas are Dataverse-first architecture, server-side routing/SLA, plugin-based closure guardrail, multi-step Power Pages intake, OAuth-protected HelloX ERP sync, model-driven dashboards, and seeded demo data.

The primary risks before final submission are security hardening and live evidence capture:

1. Power Pages Web API field allowlists are too broad (`fields: '*'`) and should be narrowed.
2. Service Request contact-scoped write access may allow authenticated portal users to PATCH internal fields unless blocked by Dataverse roles/field security or a plugin.
3. Power Automate HTTP retry/timeout behavior is not explicit in source.
4. Authenticated live UAT still needs final evidence capture for portal submit, SharePoint upload, manager approval, ERP ID writeback, and email/run history.

## Cross-QA Findings

| ID | Severity | Area | Finding | Source QA | Recommended Developer Action | Status |
| --- | --- | --- | --- | --- | --- | --- |
| UAT-F01 | Critical | Portal data integrity | Authenticated own-record write plus `Webapi/hx_servicerequest/fields: '*'` may allow portal users to PATCH internal request state fields. | QA 3 | Narrow Web API fields, reduce write permissions, and add plugin guard for portal/external updates to internal fields. | Open |
| UAT-F02 | High | Portal Web API | Multiple Web API field settings use `'*'`; `Webapi/error/innererror=true` is enabled. | QA 1, QA 3 | Replace with explicit allowlists and disable inner error output. | Open |
| UAT-F03 | High | Integration resilience | Flow has Try/Catch, OAuth, and Bearer POST, but no explicit retry/timeout policy in source. | QA 4 | Add documented retry/timeout policy and handling notes for 401/429/5xx. | Open |
| UAT-F04 | Medium | Portal UX | No customer request dashboard/list despite earlier UX plan. | QA 1 | Add simple "My requests" list or document portal as intake-only. | Open |
| UAT-F05 | Medium | Document UX | File type/size limits are not shown before upload handoff. | QA 1 | Add concise pre-submit upload guidance. | Open |
| UAT-F06 | Medium | Preview scalability | Portal preview loads broad active routing/rule data. | QA 4 | Add `$filter` or server-side preview endpoint before production scale. | Open |
| UAT-F07 | Medium | Dashboard scale | High-volume operational dashboards need recent-date/unresolved variants. | QA 4 | Add recent 30/90-day views and unresolved-only monitoring views. | Open |
| UAT-F08 | Medium | Live evidence | Several authenticated flows were not rerun in this QA pass. | QA 1, QA 2, QA 3, QA 4 | Run final live smoke and attach screenshots/run IDs. | Open |
| UAT-F09 | Low | Branding | Default `Company Name` snippets remain. | QA 1 | Replace snippets and alt text with Mitacs Service Intake wording. | Open |

## Passed Controls

| Area | Evidence |
| --- | --- |
| Anonymous access | Public checks show portal/Power Pages paths are gated or redirect to login. |
| Multi-step portal | Source implements Details, Impact, Documents, Review, validation, confirmation modal, and upload handoff. |
| Dynamic preview | Source loads routing data and updates preview without full page reload. |
| Server routing/SLA | C# plugin recalculates routing/SLA server-side. |
| Closure guardrail | C# plugin blocks critical close without internal notes and accepted evidence. |
| ERP OAuth | Current workflow source includes token action, secure parameters, Bearer header, and protected ERP endpoint. |
| HelloX endpoint | Token probe returned 200, ERP probe returned 201 with `HX-ERP-*`, and no refresh token was issued for default mode. |
| Error logging | Flow Catch scope creates System Error Log and marks sync failed. |
| Secret handling | Source/export scan did not find real OAuth client secret patterns. |

## Final Live Smoke Checklist

Run this once before sending the package:

1. Sign in to Power Pages as external reviewer/contact.
2. Submit a standard request and record confirmation number.
3. Submit a critical/high-priority request and confirm Finance / 4-hour preview.
4. Open post-submit upload page and upload a harmless `.txt` file.
5. Open model-driven app as coordinator and confirm both requests in queues.
6. Approve critical request as manager.
7. Confirm Service Request has external ERP ID and External Sync Log row.
8. Confirm no unresolved System Error Log for the happy path.
9. Trigger or show a safe failure path and confirm Catch writes System Error Log.
10. Show confirmation email or Flow run history if email delivery is restricted.

## Developer Fix Priority

| Priority | Work Item |
| --- | --- |
| P0 | Lock down Power Pages Web API fields and Service Request portal write scope. |
| P0 | Add guardrail for external/portal updates to internal fields if write access remains. |
| P1 | Disable `Webapi/error/innererror`. |
| P1 | Add explicit Power Automate retry/timeout settings or document the deliberate defaults. |
| P1 | Replace default branding snippets. |
| P2 | Add file type/size copy and optional customer request dashboard/list. |
| P2 | Add filtered preview/routing endpoint and recent-date dashboard variants for production scale. |

