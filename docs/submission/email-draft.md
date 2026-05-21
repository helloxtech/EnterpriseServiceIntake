# Email Draft

Subject: Senior Power Platform Developer Take-Home Submission - Forrest Zhang

Hi Jason,

Thank you for the opportunity to complete the Senior Dynamics 365 and Power Platform Developer take-home case.

Attached is my submission package: `Enterprise_ServiceIntake_ForrestZhang.zip`.

The package includes:

- Managed solution export.
- Unmanaged solution export.
- Unpacked solution repository generated with PAC solution unpack.
- C# plugin source code.
- PCF control source code.
- Power Pages source files.
- Architecture README with ERD, component decisions, security strategy, demo script, and verification notes.
- Polished architecture design brief in both Word and PDF format.

Live review links:

- Power Platform environment: https://mitacs.crm.dynamics.com/
- Model-driven app: https://mitacs.crm.dynamics.com/main.aspx?appid=3de4f813-b454-f111-bec7-000d3a3aca8f
- Power Pages site: https://enterprise-service-intake-hellox.powerappsportals.com

Reviewer accounts available in the tenant:

- `forrest@hellosmart.ca` - admin/reviewer
- `agent@hellosmart.ca` - internal service coordinator
- `manager@hellosmart.ca` - approval manager

Passwords will be shared separately by the environment administrator and rotated after the interview.

One implementation note: the assignment mentioned `reqres.in` as an example mock endpoint. Its POST endpoint now requires an API key, so I used `https://api.restful-api.dev/objects`, which returns a mock external ID without requiring a third-party secret.

I look forward to walking through the architecture, implementation, and tradeoffs in the live review.

Thanks,

Forrest Zhang
