# Power Pages Source Notes

The live Power Pages site is created in the Enterprise Service Intake environment:

- Site name: Enterprise Service Intake
- Public URL: `https://enterprise-service-intake-hellox.powerappsportals.com`

The source files in this folder document the intended portal customizations:

- `templates/service-intake-page.liquid`: Authenticated multi-step request page layout with step validation and final review/submit.
- `templates/routing-preview-api.liquid`: Liquid-backed JSON endpoint fallback for SLA/routing preview.
- `web-files/service-intake.js`: Dynamic preview logic using the Power Pages Web API.
- `web-files/service-intake.css`: Portal styling.

The portal feedback is intentionally advisory. Final routing, SLA, approval requirement, and closure guardrails are enforced by Dataverse plugins. Create permissions are scoped to authenticated portal users so submitted requests can be tied back to the Contact record.

Portal table permissions must include relationship privileges for the document upload step:

- `hx_servicerequest`: `Create`, `Append`, and `Append To`; contact-scoped `Read`/`Write` for owned rows.
- `hx_servicedocument`: `Create`, `Append`, and `Append To`.

The `Append`/`Append To` pair is required because the Web API creates `hx_servicedocument` rows with an `hx_servicerequest` lookup. Without both sides, Power Pages blocks the relationship operation with an associate/disassociate permission error.
