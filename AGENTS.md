# Enterprise Service Intake Instructions

This project is a Power Platform Senior Developer take-home assignment.

## Always

- Keep implementation artifacts reviewable and interview-ready.
- Do not commit tenant passwords, temporary reviewer passwords, tokens, connection secrets, or screenshots that expose secrets.
- Keep all reusable source in Git:
  - Dataverse solution source under `solution/`
  - C# plugins under `src/plugins/`
  - PCF controls under `src/pcf/`
  - Power Pages source under `src/powerpages/`
  - JavaScript web resources under `src/webresources/`
  - provisioning or utility code under `src/scripts/`
- Keep recruiter/reviewer documents under `docs/`.
- Use the `hx` publisher prefix unless the solution already requires a different prefix.

## Delivery Standards

- Build a complete vertical slice rather than a broad unfinished demo.
- Prefer Dataverse for authoritative state, Power Pages for external intake, Power Automate for approval/integration orchestration, plugins for transactional guardrails, and PCF for internal UX enhancement.
- Document architecture decisions and assumptions as they are made.
- Include test evidence and a demo script before packaging.

## Credentials

- Private environment credentials are stored outside the repository.
- Account names may be documented for reviewers, but passwords must be shared separately by the system administrator.
