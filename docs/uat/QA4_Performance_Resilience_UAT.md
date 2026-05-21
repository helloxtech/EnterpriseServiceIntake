# QA 4 UAT Report - Performance, Resilience, Integration

## Scope

Portal perceived performance, JavaScript/API cost, HelloX OAuth and ERP contract, Power Automate resilience, error logging, dashboard scalability, and ALM/export risks.

## Environment

| Item | Value |
| --- | --- |
| Repo | `/Volumes/Forrest/Users/Forrest/Github/EnterpriseServiceIntake` |
| Power Pages | `https://enterprise-service-intake-hellox.powerappsportals.com` |
| HelloX token endpoint | `https://hellox.ca/api/mock/oauth/token` |
| HelloX ERP endpoint | `https://hellox.ca/api/mock/enterprise-service-intake/erp` |
| Test date | 2026-05-21 |

## Test Approach

- Reviewed portal JS/CSS, Power Automate workflow JSON, provisioning source, dashboards/views, and HelloX endpoint behavior.
- Ran safe public endpoint checks and one private-secret OAuth probe without printing credentials.
- Did not perform load tests or live authenticated portal submits without explicit approval.

## Test Cases

| ID | Area | Steps | Expected | Actual | Status | Severity | Evidence |
| --- | --- | --- | --- | --- | --- | --- | --- |
| PERF-01 | Portal response | Public GET Power Pages home. | Site responds/gates promptly. | Home returned HTTP 200 in about 1.11s from local probe. | Pass | Low | Curl timing |
| PERF-02 | Hidden console response | Public GET HelloX `/esi/`. | Console responds quickly. | Returned HTTP 200 in about 0.24s. | Pass | Low | Curl timing |
| PERF-03 | Portal JS syntax | Run `node --check` on portal scripts. | No syntax errors. | Syntax checks passed. | Pass | Low | Command output |
| PERF-04 | JS payload/cost | Inspect custom JS/CSS. | Lightweight validation and preview. | Validation is local; preview caches categories/rules after first load. | Pass | Low | `src/powerpages/web-files/service-intake.js` |
| PERF-05 | Routing preview scalability | Inspect Web API rule reads. | Avoid unbounded reads as rules grow. | Client loads active categories/rules broadly; acceptable for demo, weak for scale. | Concern | Medium | `src/powerpages/web-files/service-intake.js` |
| PERF-06 | Submit resilience | Inspect submit error handling. | Fail clearly and preserve user data. | Failure modal exists and form data remains; no explicit client timeout/idempotency key. | Concern | Medium | `src/powerpages/web-files/service-intake.js` |
| PERF-07 | HelloX OAuth contract | Request token with client credentials. | Returns Bearer token, no refresh token for default mode. | Token `200` in about 269ms; no refresh token returned. | Pass | Low | Private-safe probe |
| PERF-08 | HelloX ERP contract | POST with Bearer token. | Returns `201` and `HX-ERP-*` external ID. | ERP POST `201` in about 325ms and returned valid ID pattern. | Pass | Low | Private-safe probe |
| PERF-09 | ERP auth rejection | Call ERP without token. | Rejects unauthenticated requests. | Protected endpoint requires OAuth. | Pass | Low | Public endpoint/source check |
| PERF-10 | Flow integration | Inspect current workflow JSON. | Token action runs before ERP POST; ERP POST uses Bearer token. | Current repo has `HTTP_-_get_HelloX_OAuth_token`, secure parameters, and Authorization header. | Pass | High | `solution/unpacked/*/Workflows/ESI-ApprovalandERPSync-*.json` |
| PERF-11 | Retry/timeout posture | Inspect HTTP action config. | Explicit retry/timeout policy for 429/5xx. | No explicit retry policy/timeout is visible in workflow JSON. | Concern | High | Workflow JSON |
| PERF-12 | Catch/error logging | Inspect flow Try/Catch. | Failed approval/ERP path logs and marks request failed. | Catch scope creates System Error Log and updates request failed state. | Pass | Low | Workflow JSON |
| PERF-13 | Dashboard scalability | Inspect views/charts. | Monitoring views remain useful with large row counts. | Dashboards use focused views/charts, but high-volume logs lack recent-date variants. | Concern | Medium | `Program.cs`, solution source |
| PERF-14 | ALM/export | Inspect exports and source placeholders. | Packages/source contain placeholders, not secrets. | Export/source have `HelloXMockOAuthClientSecret` placeholder; secret scan passed. | Pass | High | Local scan |
| PERF-15 | Authenticated live UAT | Submit, approve, and confirm ERP sync. | Flow completes after approval under target threshold. | Not executed. | Not Executed | N/A | Requires auth/approval actor |
| PERF-16 | Load test | Run concurrent portal/API scenario. | p95 within thresholds and no throttling. | Not executed. | Not Executed | N/A | Requires explicit load approval |

## Reconciliation Note

An initial QA 4 check against the older sibling repo `Enterprise_ServiceIntake` reported the ERP flow as not OAuth-aligned. That finding is superseded for the current repo `EnterpriseServiceIntake`: the current workflow source contains the OAuth token action and Bearer ERP POST. The active resilience concern is explicit retry/timeout handling, not OAuth alignment.

## Findings

| ID | Severity | Finding | Developer Recommendation |
| --- | --- | --- | --- |
| PERF-F01 | High | ERP HTTP and token actions do not show explicit retry/timeout policy in workflow JSON. | Add explicit retry policy and timeout, and document expected behavior for 401/429/5xx. |
| PERF-F02 | Medium | Portal preview loads all active routing rules/categories once. | Add `$filter` or a narrow server-side preview endpoint as rules grow. |
| PERF-F03 | Medium | Submit path has no explicit idempotency key. | Consider client-generated submission correlation ID to detect accidental double submits. |
| PERF-F04 | Medium | Dashboards/log views may become heavy with high row counts. | Add recent-date and unresolved-only views/charts for operational dashboards. |

## Not Executed Thresholds

Authenticated live UAT:

- Portal submit perceived completion: under 5 seconds.
- Flow completion after approval: under 2 minutes.
- No duplicate ERP IDs.
- Failed ERP calls always create a System Error Log with correlation ID.

Load/resilience test, only with explicit approval:

- 25 virtual users for 5 minutes in a test tenant.
- p95 unauthenticated static/redirect page under 2.5 seconds.
- p95 submit under 5 seconds.
- Error rate under 1%.
- No Dataverse/Power Pages throttling without logged evidence.

