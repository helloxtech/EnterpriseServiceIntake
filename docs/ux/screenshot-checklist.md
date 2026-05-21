# UX Screenshot Checklist

Use this checklist to collect evidence for the README and live demo. Capture only non-sensitive sample data.

## Power Pages Portal

- Authenticated portal landing page with customer request list.
- Empty or populated dashboard showing only the signed-in user's requests.
- New request step 1: request details.
- New request step 2: impact and urgency.
- Dynamic SLA/routing preview before required inputs are complete.
- Dynamic SLA/routing preview after category, severity, and impact are selected.
- Documentation upload step with file status visible.
- Validation state for a missing required field or required document.
- Review and submit page with editable summary sections.
- Confirmation page with formatted confirmation number.
- Existing request detail page showing customer-safe status and expected response target.

## Model-Driven App

- Main request view with confirmation number, status, severity, and SLA columns.
- Request main form header with status, severity, SLA target, and owner/team.
- Summary tab showing customer request details.
- Triage tab showing routing destination and SLA rule result.
- Approval tab showing approval requirement and manager decision state.
- Resolution tab showing required internal resolution documentation.
- Integration tab showing ERP sync status and external system ID.
- Authorized internal error/audit tab showing graceful failure logging.
- PCF visual indicator in normal/on-track state.
- PCF visual indicator in at-risk or breached state.

## Accessibility And Quality

- Keyboard focus visible on portal step navigation, upload, and submit controls.
- Dynamic preview update visible without a full page reload.
- Validation messages appear next to the affected fields.
- Status badges include text labels, not color alone.
- Mobile or narrow viewport screenshot of the portal request flow.
- Screenshot proving customer users cannot see internal-only fields.

## Demo Evidence

- Before-submission preview screenshot.
- Submitted confirmation screenshot.
- Internal model-driven record opened from the submitted request.
- Approval evidence screenshot if email delivery is unavailable.
- ERP sync success screenshot with external ID.
- Error handling screenshot or run history showing failure captured in Dataverse error log.

