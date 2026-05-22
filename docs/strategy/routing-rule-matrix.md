# Routing/SLA Rule Matrix

The routing matrix is stored as Dataverse `Routing / SLA Rule` rows and is seeded by the provisioning utility. It creates one active exact-match rule for every `Service Category + Impact Level + Urgency` combination.

Total active rules: `5 categories x 4 impact levels x 4 urgency levels = 80`.

## Category Ownership

| Service Category | Department | Documentation Default |
| --- | --- | --- |
| Funding Agreement | Finance | Required |
| Research Partnership | Research Operations | Required |
| Event Support | Client Services | Standard unless risk is high |
| Technical Support | IT Support | Standard unless risk is high |
| General Inquiry | General Intake | Standard unless risk is high |

## Impact/Urgency Matrix

The final SLA/review/documentation outcome is based on the higher risk between Impact and Urgency. Documentation is also required when the service category default requires it.

| Impact Level | Urgency | SLA Policy | Manager Review | Documentation |
| --- | --- | --- | --- | --- |
| Low | Low | Low - 5 business day response | No | Category default |
| Low | Normal | Standard - 3 business day response | No | Category default |
| Low | High | High - 1 business day response | Yes | Required |
| Low | Urgent | Critical - 4 hour response | Yes | Required |
| Medium | Low | Standard - 3 business day response | No | Category default |
| Medium | Normal | Standard - 3 business day response | No | Category default |
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
- If no active exact match exists, the portal shows a rule-missing warning instead of applying hidden frontend thresholds.
- The model-driven app navigation replaces the raw `Routing / SLA Rules` table entry with `Routing Matrix`, an editable web resource page for internal coordinators. The page edits the same Dataverse rule rows through `Xrm.WebApi` and opens the underlying rule record when a rule name is selected.
