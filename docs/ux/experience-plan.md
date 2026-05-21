# Enterprise Service Intake UX Experience Plan

## Scope

This document plans the user experience for the external Power Pages portal and the internal model-driven app. It does not define implementation details, secrets, environment configuration, or code.

The experience should feel professional, restrained, and enterprise-ready: clear hierarchy, low visual noise, practical feedback, and enough polish to support a live technical demo.

## UX Principles

- Make the next action obvious on every screen.
- Show request status, SLA expectation, and routing preview before submission.
- Avoid exposing internal-only data such as resolution notes, approval comments, error details, or integration payloads to portal users.
- Keep the interface Mitacs-branded but quiet: restrained color usage, strong spacing, readable typography, and limited decorative elements.
- Prefer plain language labels over platform or implementation terms.
- Treat accessibility as part of the core demo, not a later refinement.

## Portal Information Architecture

### Home / Request Dashboard

Purpose: give the authenticated customer a clear starting point.

Recommended content:

- Primary action: `New service request`
- List of the user's submitted requests
- Request number, title, status, submitted date, and expected response window
- Empty state that explains how to submit the first request
- Help link for support, documentation, or contact information

Do not show internal routing notes, approval status details, assigned internal owner, integration status, or error logs.

### New Service Request

Use a multi-step flow with a visible progress indicator:

1. Request details
2. Impact and urgency
3. Supporting documentation
4. Review and submit
5. Confirmation

Each step should support save-and-continue behavior if time permits. If not, keep the steps short enough that the demo does not feel fragile.

### Request Details

Fields:

- Request title
- Service category
- Description
- Organization / account context if available
- Preferred contact method

UX notes:

- Use progressive disclosure for category-specific questions.
- Keep the description helper text practical: what happened, who is affected, and when it started.
- Validate required fields inline before moving to the next step.

### Impact and Urgency

Fields:

- Severity
- Business impact
- Affected users or teams
- Desired completion date
- Operational deadline, if any

Dynamic feedback:

- Show expected SLA as a compact preview panel.
- Show likely destination department or queue.
- Show whether manager approval may be required for critical/high-priority items.
- Update the preview without a full page reload when relevant inputs change.

Tone:

- Use phrases like `Estimated response target` and `Likely routing destination`.
- Avoid implying a guaranteed SLA before the request is accepted.

### Supporting Documentation

UX notes:

- Provide a clear upload area with accepted file types and size limits.
- Explain when documentation is required, especially for high-severity requests.
- Show uploaded file name, size, status, and remove action.
- Use validation messaging that explains the missing requirement, not just `Required`.

### Review and Submit

Content:

- Summary of entered values grouped by step
- Dynamic SLA/routing preview
- Uploaded documents
- Customer acknowledgement checkbox if required
- Final submit button

UX notes:

- Allow editing each section without losing progress.
- Keep the final CTA specific: `Submit service request`.
- Disable submit only when the reason is visible and actionable.

### Confirmation

Content:

- Formatted confirmation number
- Submitted date/time
- Expected response target
- What happens next
- Link back to request dashboard

UX notes:

- Make the confirmation number large enough to capture clearly in screenshots.
- Include a copy action if implementation time allows.

## Dynamic Feedback Behavior

The preview panel should behave consistently across the multi-step portal flow:

- Before enough input exists, show neutral placeholder text such as `Complete category and severity to preview routing`.
- While refreshing, show a small loading state without blocking the whole page.
- When rules match, show department, estimated response target, approval requirement, and documentation requirement.
- When no rule matches, show a fallback message and allow submission if business rules permit.
- On Web API or rules lookup failure, show a graceful message: `Preview is unavailable. You can still submit your request.`

The preview should never expose internal rule identifiers, table names, API errors, stack traces, or integration details.

## Model-Driven App Form Layout

Internal coordinators should be able to triage, review, approve, and close requests from a single predictable form.

Recommended main form structure:

- Header: confirmation number, status, severity, SLA target, owner/team
- Summary tab: request details, customer, category, description, attachments summary
- Triage tab: routing destination, severity, SLA rule applied, priority rationale
- Approval tab: approval requirement, approval status, manager decision, decision date
- Resolution tab: resolution notes, customer-facing summary, closure validation status
- Integration tab: ERP sync status, external system ID, last sync timestamp
- Audit / errors tab: concise operational error summary for authorized internal users

Keep sensitive fields role-secured and visually separated from customer-facing values.

## PCF Experience Concept

Recommended PCF control: visual severity / SLA / status indicator.

Placement:

- Model-driven app header or summary section
- Read-only companion on the triage tab

Expected behavior:

- Display severity with accessible color and text label.
- Show SLA state: on track, at risk, breached, paused, or complete.
- Show approval state when high-priority requests require manager review.
- Provide concise tooltip text explaining why the state is shown.

Visual direction:

- Use color as a supporting signal only; never rely on color alone.
- Use clear icons or labels for status.
- Keep the control compact enough for internal coordinators who review many records.

## Accessibility Notes

- Maintain visible focus states for all links, buttons, uploads, and step navigation.
- Ensure the multi-step progress indicator has text labels, not only numbers or color.
- Use semantic headings in page order.
- Pair all validation messages with the related field.
- Announce dynamic SLA/routing preview changes with an appropriate live region.
- Ensure upload status and errors are available to screen readers.
- Meet WCAG AA contrast for text, buttons, badges, and status indicators.
- Avoid red/green-only status communication; include labels and icons.

## Demo Narrative

1. Customer signs in and sees only their own requests.
2. Customer starts a new service request.
3. Customer changes category, severity, or impact and sees the preview update without a page reload.
4. Customer uploads supporting documentation.
5. Customer reviews the request and submits.
6. Customer receives a formatted confirmation number.
7. Internal coordinator opens the model-driven app record.
8. Coordinator sees severity, SLA, routing, approval state, and documentation status.
9. Manager approval and ERP sync are discussed from internal views without exposing internal fields to the portal user.

