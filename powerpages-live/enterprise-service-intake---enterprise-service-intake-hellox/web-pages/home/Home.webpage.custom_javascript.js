(function () {
  const root = document.querySelector("[data-esi-intake]");
  if (!root) return;

  const preview = root.querySelector("[data-routing-preview]");
  const review = root.querySelector("[data-review-summary]");
  const result = root.querySelector("[data-submit-result]");
  const submitButton = root.querySelector(".esi-primary");
  let cachedRules = null;
  let cachedCategories = null;

  function field(name) {
    return root.querySelector(`[name="${name}"]`);
  }

  function selectedOption(name) {
    return field(name)?.selectedOptions?.[0] || null;
  }

  function selectedCategoryName() {
    return selectedOption("category")?.textContent?.trim() || "";
  }

  function html(value) {
    const element = document.createElement("span");
    element.textContent = value == null ? "" : String(value);
    return element.innerHTML;
  }

  function safeAjax(options) {
    const tokenProvider = window.shell && window.shell.getTokenDeferred;
    if (!window.jQuery || !tokenProvider) {
      return Promise.reject(new Error("Power Pages request token is unavailable."));
    }

    return new Promise((resolve, reject) => {
      tokenProvider().done((token) => {
        const headers = Object.assign({
          Accept: "application/json",
          "Content-Type": "application/json",
          "__RequestVerificationToken": token
        }, options.headers || {});

        window.jQuery.ajax(Object.assign({}, options, { headers }))
          .done((data, textStatus, xhr) => resolve({ data, xhr }))
          .fail((xhr) => {
            const detail = xhr.responseJSON?.error?.message || xhr.responseText || xhr.statusText;
            reject(new Error(detail || `Request failed with status ${xhr.status}`));
          });
      }).fail(() => reject(new Error("Could not get Power Pages request token.")));
    });
  }

  async function loadCategories() {
    if (cachedCategories) return cachedCategories;
    const response = await safeAjax({
      method: "GET",
      url: "/_api/hx_servicecategories?$select=hx_servicecategoryid,hx_name&$filter=hx_active eq true&$orderby=hx_name asc"
    });
    cachedCategories = response.data.value || [];
    return cachedCategories;
  }

  async function populateCategories() {
    const category = field("category");
    try {
      const categories = await loadCategories();
      category.innerHTML = '<option value="">Select a category</option>' + categories
        .map((item) => `<option value="${html(item.hx_servicecategoryid)}">${html(item.hx_name)}</option>`)
        .join("");
    } catch (error) {
      category.innerHTML = [
        '<option value="">Select a category</option>',
        '<option value="static-funding">Funding Agreement</option>',
        '<option value="static-research">Research Partnership</option>',
        '<option value="static-event">Event Support</option>',
        '<option value="static-tech">Technical Support</option>',
        '<option value="static-general">General Inquiry</option>'
      ].join("");
      setPreview({
        heading: "Live category lookup is unavailable.",
        note: "The page can still preview common scenarios; submission requires portal table permissions."
      });
    }
  }

  async function loadRules() {
    if (cachedRules) return cachedRules;

    const query = [
      "/_api/hx_routingrules",
      "?$select=hx_name,hx_matchseverity,hx_matchpriority,hx_requiresapproval,hx_resolutiondocumentationrequired,hx_sortorder",
      "&$expand=hx_Servicecategory($select=hx_name),hx_Department($select=hx_name),hx_Slapolicy($select=hx_responsehours)",
      "&$filter=hx_active eq true",
      "&$orderby=hx_sortorder asc"
    ].join("");

    const response = await safeAjax({ method: "GET", url: query });
    cachedRules = (response.data.value || []).map((rule) => ({
      name: rule.hx_name,
      category: rule.hx_Servicecategory?.hx_name,
      severity: Number(rule.hx_matchseverity),
      priority: Number(rule.hx_matchpriority),
      department: rule.hx_Department?.hx_name,
      responseHours: rule.hx_Slapolicy?.hx_responsehours,
      requiresApproval: Boolean(rule.hx_requiresapproval),
      requiresDocumentation: Boolean(rule.hx_resolutiondocumentationrequired)
    }));
    return cachedRules;
  }

  function setPreview(state) {
    preview.innerHTML = `
      <span class="esi-preview-label">Routing preview</span>
      <h2>${html(state.heading)}</h2>
      <dl>
        <div><dt>Department</dt><dd>${html(state.department || "Pending")}</dd></div>
        <div><dt>Response target</dt><dd>${html(state.sla || "Pending")}</dd></div>
        <div><dt>Approval</dt><dd>${html(state.approval || "Pending")}</dd></div>
        <div><dt>Documentation</dt><dd>${html(state.documentation || "Standard")}</dd></div>
      </dl>
      <p>${html(state.note)}</p>`;
  }

  async function refreshPreview() {
    const category = selectedCategoryName();
    const severity = Number(field("severity")?.value || 0);
    const priority = Number(field("priority")?.value || 0);

    if (!category || !severity || !priority) {
      setPreview({
        heading: "Complete category, severity, and priority to preview routing.",
        note: "The final department and SLA are assigned server-side after submission."
      });
      return;
    }

    setPreview({ heading: "Refreshing preview...", note: "Checking active Dataverse routing rules." });

    try {
      const rules = await loadRules();
      const match = rules.find((rule) =>
        rule.category === category &&
        rule.severity === severity &&
        rule.priority === priority);

      if (!match) {
        setPreview({
          heading: "General Intake review expected",
          department: "General Intake",
          sla: "Reviewed after submission",
          approval: "To be confirmed",
          documentation: "As needed",
          note: "No exact rule matched this combination. The request can still be submitted."
        });
        return;
      }

      setPreview({
        heading: match.name,
        department: match.department,
        sla: `${match.responseHours} hour response target`,
        approval: match.requiresApproval ? "Manager approval required" : "No manager approval",
        documentation: match.requiresDocumentation ? "Resolution evidence required" : "Standard supporting files",
        note: "This is a live preview. Dataverse applies the authoritative rule when the request is saved."
      });
    } catch (error) {
      setPreview({
        heading: "Preview temporarily unavailable",
        note: error.message || "The server-side plugin still applies routing on save."
      });
    }
  }

  function updateReview() {
    if (!review) return;
    review.innerHTML = `
      <p><strong>Title:</strong> ${html(field("title")?.value || "Not entered")}</p>
      <p><strong>Category:</strong> ${html(selectedCategoryName() || "Not selected")}</p>
      <p><strong>Severity:</strong> ${html(selectedOption("severity")?.textContent || "Not selected")}</p>
      <p><strong>Priority:</strong> ${html(selectedOption("priority")?.textContent || "Not selected")}</p>`;
  }

  function extractCreatedId(xhr) {
    const entityId = xhr.getResponseHeader("entityid") || xhr.getResponseHeader("OData-EntityId") || "";
    const match = entityId.match(/\(([^)]+)\)/);
    return match ? match[1] : entityId;
  }

  async function submitRequest(event) {
    event.preventDefault();
    const form = event.currentTarget;
    if (!form.reportValidity()) return;

    submitButton.disabled = true;
    result.className = "esi-result";
    result.textContent = "Submitting request...";

    const categoryId = field("category").value;
    const descriptionParts = [
      field("description").value,
      field("impact").value ? `Business impact: ${field("impact").value}` : ""
    ].filter(Boolean);

    const payload = {
      hx_title: field("title").value,
      hx_description: descriptionParts.join("\n\n"),
      hx_severity: Number(field("severity").value),
      hx_priority: Number(field("priority").value),
      hx_lifecyclestatus: 752630001,
      hx_submittedon: new Date().toISOString(),
      "hx_Servicecategory@odata.bind": `/hx_servicecategories(${categoryId})`
    };

    if (window.esiPortalUser?.contactId) {
      payload["hx_Customercontact@odata.bind"] = `/contacts(${window.esiPortalUser.contactId})`;
    }

    try {
      const createResponse = await safeAjax({
        method: "POST",
        url: "/_api/hx_servicerequests",
        data: JSON.stringify(payload)
      });
      const requestId = extractCreatedId(createResponse.xhr);
      await createDocumentRows(requestId);

      let confirmation = "";
      if (requestId) {
        try {
          const created = await safeAjax({
            method: "GET",
            url: `/_api/hx_servicerequests(${requestId})?$select=hx_confirmationnumber,hx_routingpreviewsummary`
          });
          confirmation = created.data.hx_confirmationnumber || "";
        } catch {
          confirmation = "";
        }
      }

      result.className = "esi-result is-success";
      result.textContent = confirmation
        ? `Request submitted. Confirmation number: ${confirmation}.`
        : "Request submitted. The confirmation number will be available in the internal app.";
      form.reset();
      updateReview();
      await populateCategories();
      await refreshPreview();
    } catch (error) {
      result.className = "esi-result is-error";
      result.textContent = error.message || "The request could not be submitted.";
    } finally {
      submitButton.disabled = false;
    }
  }

  async function createDocumentRows(requestId) {
    const files = Array.from(field("documents")?.files || []);
    if (!requestId || files.length === 0) return;

    await Promise.all(files.map((file) => safeAjax({
      method: "POST",
      url: "/_api/hx_servicedocuments",
      data: JSON.stringify({
        hx_name: file.name,
        hx_filename: file.name,
        hx_documenttype: 752630000,
        hx_notes: "Uploaded through the Power Pages intake form. File metadata captured for demo traceability.",
        "hx_Servicerequest@odata.bind": `/hx_servicerequests(${requestId})`
      })
    })));
  }

  root.querySelectorAll("[data-step-target]").forEach((button) => {
    button.addEventListener("click", () => {
      root.querySelectorAll("[data-step-target]").forEach((item) => item.classList.remove("is-active"));
      root.querySelectorAll("[data-step]").forEach((item) => item.classList.remove("is-active"));
      button.classList.add("is-active");
      root.querySelector(`[data-step="${button.dataset.stepTarget}"]`)?.classList.add("is-active");
      updateReview();
    });
  });

  root.querySelectorAll("[data-routing-input]").forEach((input) => input.addEventListener("change", refreshPreview));
  root.querySelector(".esi-form")?.addEventListener("input", updateReview);
  root.querySelector(".esi-form")?.addEventListener("submit", submitRequest);

  populateCategories().then(refreshPreview);
})();
