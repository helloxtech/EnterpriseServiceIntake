# QA 1 UAT Report - Power Pages UI/UX

## Scope

Power Pages external user experience: authentication posture, multi-step intake, routing/SLA preview, validation, document upload handoff, confirmation UX, responsive layout, accessibility, copy clarity, and branding.

## Environment

| Item | Value |
| --- | --- |
| Repo | `/Volumes/Forrest/Users/Forrest/Github/EnterpriseServiceIntake` |
| Power Pages site | `https://enterprise-service-intake-hellox.powerappsportals.com` |
| Hidden HelloX console | `https://hellox.ca/esi/` |
| Test date | 2026-05-21 |
| Access used | Anonymous/public endpoint checks plus repo/live-export source review |

## Test Approach

- Reviewed `README.md`, UX docs, `src/powerpages/`, and `powerpages-live/`.
- Ran safe public `curl` checks for portal, upload route, and HelloX console.
- Ran portal JavaScript syntax checks.
- Marked authenticated browser-only tests as Not Executed where credentials/session were required.

## Test Cases

| ID | Area | Steps | Expected | Actual | Status | Severity | Evidence |
| --- | --- | --- | --- | --- | --- | --- | --- |
| UIUX-01 | Anonymous access | GET portal root anonymously. | Anonymous users cannot submit intake. | Site redirects or gates non-authenticated users. | Pass | Low | `powerpages-live/.../Home.en-US.webpage.copy.html` |
| UIUX-02 | Authenticated shell | Review signed-in source. | User sees intake tied to contact. | Source renders signed-in banner/contact context and `window.esiPortalUser.contactId`. | Pass | Low | `src/powerpages/templates/service-intake-page.liquid` |
| UIUX-03 | Multi-step flow | Inspect intake markup and JS. | Clear 4-step intake path. | Details, Impact, Documents, Review steps are present. | Pass | Low | `src/powerpages/templates/service-intake-page.liquid` |
| UIUX-04 | Step validation | Try next/submit with missing fields or review JS. | Required fields block progression. | `validateStep`, `validateBefore`, invalid styling, and focus handling exist. | Pass | Low | `src/powerpages/web-files/service-intake.js` |
| UIUX-05 | Conditional validation | Select high severity/priority path. | Business impact required for high/urgent. | JS requires impact for high-impact requests. | Pass | Low | `src/powerpages/web-files/service-intake.js` |
| UIUX-06 | Dynamic preview | Change category/severity/priority. | Department, SLA, approval, documentation preview updates without reload. | Source loads categories/rules and renders live preview. Existing evidence confirms Finance/4h case. | Pass | Low | `src/powerpages/web-files/service-intake.js`, `docs/evidence/verification.md` |
| UIUX-07 | Submit behavior | Submit valid form. | Request is created and confirmation fetched. | Web API create/fetch logic is present. Authenticated live submit not rerun. | Partial | Medium | `src/powerpages/web-files/service-intake.js` |
| UIUX-08 | Confirmation UX | Review success modal. | Confirmation number and next steps are visible. | Modal renders confirmation and upload action. | Pass | Low | `src/powerpages/web-files/service-intake.js` |
| UIUX-09 | Document handoff | Open `/request-documents/?id=<id>`. | Requires auth and request id; supports SharePoint upload. | Source gates by user and id; anonymous public probe redirects. | Pass | Low | `powerpages-live/.../Request-Documents.en-US.webpage.copy.html` |
| UIUX-10 | Upload diagnostics | Review upload JS. | User sees ready/warning/error state. | JS detects SharePoint document grid and permission/rendering errors. | Pass | Low | `powerpages-live/.../Request-Documents.en-US.webpage.custom_javascript.js` |
| UIUX-11 | Responsive layout | Inspect CSS. | Layout adapts for desktop and mobile. | Grid/responsive rules exist; visual device pass still recommended. | Partial | Low | `src/powerpages/web-files/service-intake.css` |
| UIUX-12 | Accessibility | Inspect markup/CSS. | Focus states, live regions, semantic labels. | Skip link, focus-visible styles, `aria-live`, and dialog role exist; focus trap not verified. | Partial | Low | `src/powerpages/web-files/service-intake.css` |
| UIUX-13 | Authenticated full flow | Sign in, submit, upload file. | Full customer journey succeeds. | Not executed in this QA pass. | Not Executed | Medium | Requires browser auth session |
| UIUX-14 | Hidden HelloX console | GET console and protected ERP endpoint. | Console loads; ERP protected. | Console returned HTTP 200; ERP requires OAuth. | Pass | Low | Public endpoint probe |

## Findings

| ID | Severity | Finding | Developer Recommendation |
| --- | --- | --- | --- |
| UX-F01 | High | Power Pages Web API site settings expose `fields: '*'` and `Webapi/error/innererror=true`. This is broader than required for the external UX and conflicts with the intent to hide internal fields/errors. | Closed 2026-05-21: explicit allowlists and `Webapi/error/innererror=false` verified from fresh live PAC download. |
| UX-F02 | Medium | Current portal is intake-first. Earlier UX docs describe a customer request dashboard/list, but the implemented home page does not include one. | Either add a small authenticated "My requests" list or update docs/demo script to state the portal scope is intake plus upload handoff. |
| UX-F03 | Medium | Pre-submit document step explains upload happens after submission, but does not show accepted file types or size limits. | Add concise file type/size guidance before upload handoff. |
| UX-F04 | Low | Some default snippets still say `Company Name` / `Company Name logo`. | Replace all remaining default snippets with Mitacs Service Intake wording and logo alt text. |

## Recommended Live Smoke

1. Sign in as a portal reviewer contact.
2. Validate required fields block progression.
3. Select Funding Agreement + Critical + Urgent and confirm Finance / 4-hour preview.
4. Submit request and record confirmation number.
5. Open upload page and upload a harmless `.txt` file.
