# Routing/SLA Rule Matrix

The routing matrix is stored as Dataverse `Routing / SLA Rule` rows and is seeded by the provisioning utility. It creates one active exact-match rule for every `Service Category + Impact Level + Urgency` combination.

Total active exact-match rules: `5 categories x 4 impact levels x 4 urgency levels = 80`.

One additional active rule, `Generic fallback - unmatched request`, is kept as a documented safety net for misconfiguration. It routes unmatched requests to General Intake with the Low response target, no manager review, and optional supporting files.

## Category Ownership

| Service Category | Department |
| --- | --- |
| Funding Agreement | Finance |
| Research Partnership | Research Operations |
| Event Support | Client Services |
| Technical Support | IT Support |
| General Inquiry | General Intake |

## Impact/Urgency Matrix

The final SLA/review/documentation outcome is based on the exact Dataverse rule row. The seeded defaults below use the higher risk between Impact and Urgency; service category does not add a separate documentation default.

| Impact Level | Urgency | SLA Policy | Manager Review | Documentation |
| --- | --- | --- | --- | --- |
| Low | Low | Low - 5 business day response | No | Optional |
| Low | Normal | Standard - 3 business day response | No | Optional |
| Low | High | High - 1 business day response | Yes | Required |
| Low | Urgent | Critical - 4 hour response | Yes | Required |
| Medium | Low | Standard - 3 business day response | No | Optional |
| Medium | Normal | Standard - 3 business day response | No | Optional |
| Medium | High | High - 1 business day response | Yes | Required |
| Medium | Urgent | Critical - 4 hour response | Yes | Required |
| High | Low | High - 1 business day response | Yes | Required |
| High | Normal | High - 1 business day response | Yes | Required |
| High | High | High - 1 business day response | Yes | Required |
| High | Urgent | Critical - 4 hour response | Yes | Required |
| Critical | Low | Critical - 4 hour response | Yes | Required |
| Critical | Normal | Critical - 4 hour response | Yes | Required |
| Critical | High | Critical - 4 hour response | Yes | Required |
| Critical | Urgent | Critical - 4 hour response | Yes | Required |

## Runtime Behavior

- Power Pages loads the active matrix rows through the Web API and previews only exact matches.
- The routing plugin uses the same Dataverse rule rows on create/update, so submitted records match the portal preview.
- If no active exact match exists, the portal and plugin apply the documented `Generic fallback - unmatched request` rule rather than category defaults or hidden frontend thresholds.
- The model-driven app navigation replaces the raw `Routing / SLA Rules` table entry with `Routing Matrix`, an editable web resource page for internal coordinators. The page edits the same Dataverse rule rows through `Xrm.WebApi` and opens the underlying rule record when a rule name is selected.

## Editable Matrix UX

The matrix is intentionally not exposed as a flat 80-row Dataverse view for normal rule maintenance. The model-driven app includes a `Routing Matrix` web resource under `Routing Configuration` because business administrators need to understand the full rule pattern without reading every row individually.

User-facing behavior:

- Category tabs show one service category at a time.
- Summary cards show the destination department, SLA distribution, manager-review count, and documentation-required count for the selected category.
- Rows are Impact levels and columns are Urgency levels, matching the portal language.
- Each cell shows the exact rule row for that category/impact/urgency combination.
- Department and SLA are edited inline with dropdowns.
- Manager review and required documentation use switch controls.
- Selecting the rule name opens the underlying `Routing / SLA Rule` Dataverse row for audit fields or detailed edits.

This keeps the full exact-match design while giving admins a readable maintenance surface. The underlying table remains in Dataverse for solution packaging, auditing, plugin execution, and advanced troubleshooting.
