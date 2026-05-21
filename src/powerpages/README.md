# Power Pages Source Notes

The live Power Pages site is created in the Enterprise Service Intake environment:

- Site name: Enterprise Service Intake
- Public URL: `https://enterprise-service-intake-hellox.powerappsportals.com`

The source files in this folder document the intended portal customizations:

- `templates/service-intake-page.liquid`: Authenticated multi-step request page layout with step validation and final review/submit.
- `templates/routing-preview-api.liquid`: Liquid-backed JSON endpoint fallback for SLA/routing preview.
- `web-files/service-intake.js`: Dynamic preview logic using the Power Pages Web API.
- `web-files/service-intake.css`: Portal styling.

Supporting documents use Power Pages SharePoint document management rather than posting binary files through the intake Web API. The intake page creates the Dataverse Service Request first, then opens `/request-documents/?id=<service-request-id>` so the SharePoint document grid can upload files against the existing parent record. Keep the root webpage and localized content page in sync in the enhanced data model; users render the localized page.

The portal feedback is intentionally advisory. Final routing, SLA, approval requirement, and closure guardrails are enforced by Dataverse plugins. Create permissions are scoped to authenticated portal users so submitted requests can be tied back to the Contact record.

Supporting files are handled after the request is created. The custom intake page creates the Service Request through the Power Pages Web API, then directs the user to `/request-documents/?id=<request-id>`, a hidden authenticated page backed by a Basic Form and Dataverse document management. Files uploaded there are stored in the configured SharePoint document library for the specific Service Request record.
