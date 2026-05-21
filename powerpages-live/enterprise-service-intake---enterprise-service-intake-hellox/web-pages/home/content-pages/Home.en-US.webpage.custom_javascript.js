(function () {
  const root = document.querySelector("[data-esi-intake]");
  if (!root) return;

  const preview = root.querySelector("[data-routing-preview]");
  const review = root.querySelector("[data-review-summary]");
  const result = root.querySelector("[data-submit-result]");
  const fileList = root.querySelector("[data-file-list]");
  const form = root.querySelector(".esi-form");
  const submitButton = root.querySelector('button[type="submit"]');
  const steps = Array.from(root.querySelectorAll("[data-step]")).map((item) => item.dataset.step);
  const stepButtons = Array.from(root.querySelectorAll("[data-step-target]"));
  let cachedRules = null;
  let cachedCategories = null;
  let currentStep = "details";

  function field(name) {
    return root.querySelector(`[name="${name}"]`);
  }

  function selectedOption(name) {
    return field(name)?.selectedOptions?.[0] || null;
  }

  function selectedText(name) {
    const option = selectedOption(name);
    return option && option.value ? option.textContent.trim() : "";
  }

  function selectedCategoryName() {
    return selectedText("category");
  }

  function html(value) {
    const element = document.createElement("span");
    element.textContent = value == null ? "" : String(value);
    return element.innerHTML;
  }

  function stepIndex(stepName) {
    return steps.indexOf(stepName);
  }

  function stepPanel(stepName) {
    return root.querySelector(`[data-step="${stepName}"]`);
  }

  function stepError(stepName) {
    return stepPanel(stepName)?.querySelector("[data-step-error]");
  }

  function setStepError(stepName, message) {
    const error = stepError(stepName);
    if (!error) return;
    error.textContent = message || "";
    error.classList.toggle("is-visible", Boolean(message));
  }

  function controlsForStep(stepName) {
    return Array.from(stepPanel(stepName)?.querySelectorAll("input, select, textarea") || [])
      .filter((control) => !control.disabled && control.type !== "button" && control.type !== "submit");
  }

  function isHighImpactRequest() {
    const severity = Number(field("severity")?.value || 0);
    const priority = Number(field("priority")?.value || 0);
    return severity >= 752630002 || priority >= 752630002;
  }

  function applyConditionalRequirements() {
    const impact = field("impact");
    const note = root.querySelector("[data-critical-required]");
    const required = isHighImpactRequest();
    if (!impact) return;

    impact.required = required;
    impact.setCustomValidity(required && !impact.value.trim()
      ? "Business impact is required for high priority or critical requests."
      : "");

    if (note) {
      note.textContent = required
        ? "Required for this request"
        : "Required for high priority or critical requests";
    }
  }

  function validateStep(stepName) {
    applyConditionalRequirements();
    const controls = controlsForStep(stepName);
    const invalid = controls.find((control) => !control.checkValidity());

    controls.forEach((control) => {
      control.classList.toggle("is-invalid", !control.checkValidity());
    });

    if (invalid) {
      const message = invalid.validationMessage || "Complete the required fields on this step before continuing.";
      setStepError(stepName, message);
      invalid.reportValidity();
      invalid.focus({ preventScroll: false });
      return false;
    }

    setStepError(stepName, "");
    return true;
  }

  function validateBefore(targetStep) {
    const targetIndex = stepIndex(targetStep);
    const currentIndex = stepIndex(currentStep);
    if (targetIndex <= currentIndex) return true;

    for (let index = currentIndex; index < targetIndex; index += 1) {
      const stepName = steps[index];
      if (stepName !== currentStep) setActiveStep(stepName);
      if (!validateStep(stepName)) return false;
    }
    return true;
  }

  function setActiveStep(stepName) {
    if (!steps.includes(stepName)) return;

    currentStep = stepName;
    const activeIndex = stepIndex(stepName);

    root.querySelectorAll("[data-step]").forEach((item) => {
      item.classList.toggle("is-active", item.dataset.step === stepName);
    });

    stepButtons.forEach((button, index) => {
      const isActive = button.dataset.stepTarget === stepName;
      button.classList.toggle("is-active", isActive);
      button.classList.toggle("is-complete", index < activeIndex);
      if (isActive) {
        button.setAttribute("aria-current", "step");
      } else {
        button.removeAttribute("aria-current");
      }
    });

    applyConditionalRequirements();
    updateReview();
  }

  function goToStep(stepName) {
    if (!validateBefore(stepName)) return;
    setActiveStep(stepName);
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
        status: "warning",
        heading: "Live category lookup is unavailable.",
        note: "The page can still preview common scenarios. Submission requires live Dataverse category access."
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

  function previewStateLabel(status) {
    if (status === "ready") return "Live estimate";
    if (status === "warning") return "Needs review";
    if (status === "error") return "Unavailable";
    if (status === "loading") return "Checking rules";
    return "Waiting for inputs";
  }

  function toneFor(value, kind) {
    if (kind === "approval" && /required/i.test(value || "")) return "is-amber";
    if (kind === "documentation" && /required|evidence/i.test(value || "")) return "is-red";
    if (/pending|unavailable/i.test(value || "")) return "";
    return "is-green";
  }

  function previewRow(label, value, toneClass) {
    return `
      <div>
        <span>${html(label)}</span>
        ${toneClass ? `<strong class="esi-badge ${toneClass}">${html(value)}</strong>` : `<strong>${html(value)}</strong>`}
      </div>`;
  }

  function setPreview(state) {
    const department = state.department || "Pending";
    const sla = state.sla || "Pending";
    const approval = state.approval || "Pending";
    const documentation = state.documentation || "Standard";
    const status = state.status || "waiting";
    const stateClass = status === "ready" ? "is-ready" : status === "warning" ? "is-warning" : status === "error" ? "is-error" : "";

    preview.innerHTML = `
      <div class="esi-preview-header">
        <span class="esi-preview-label">Routing preview</span>
        <span class="esi-preview-state ${stateClass}">${html(previewStateLabel(status))}</span>
      </div>
      <h2>${html(state.heading)}</h2>
      <div class="esi-preview-grid">
        ${previewRow("Department", department)}
        ${previewRow("Response target / SLA", sla)}
        ${previewRow("Approval", approval, toneFor(approval, "approval"))}
        ${previewRow("Documentation", documentation, toneFor(documentation, "documentation"))}
      </div>
      <p class="esi-preview-note">${html(state.note || "The final department and SLA are assigned server-side after submission.")}</p>`;
  }

  async function refreshPreview() {
    applyConditionalRequirements();
    const category = selectedCategoryName();
    const severity = Number(field("severity")?.value || 0);
    const priority = Number(field("priority")?.value || 0);

    if (!category || !severity || !priority) {
      setPreview({
        status: "waiting",
        heading: "Complete category, severity, and priority to preview routing.",
        note: "The final department and SLA are assigned server-side after submission."
      });
      return;
    }

    setPreview({
      status: "loading",
      heading: "Refreshing live routing preview...",
      note: "Checking active Dataverse routing rules through the Power Pages Web API."
    });

    try {
      const rules = await loadRules();
      const match = rules.find((rule) =>
        rule.category === category &&
        rule.severity === severity &&
        rule.priority === priority);

      if (!match) {
        setPreview({
          status: "warning",
          heading: "General Intake review expected",
          department: "General Intake",
          sla: "Reviewed after submission",
          approval: isHighImpactRequest() ? "Manager approval likely" : "To be confirmed",
          documentation: isHighImpactRequest() ? "Impact notes required" : "As needed",
          note: "No exact rule matched this combination. The request can still be submitted and will be routed server-side."
        });
        return;
      }

      setPreview({
        status: "ready",
        heading: match.name,
        department: match.department,
        sla: `${match.responseHours} hour response target`,
        approval: match.requiresApproval ? "Manager approval required" : "No manager approval",
        documentation: match.requiresDocumentation ? "Resolution evidence required" : "Standard supporting files",
        note: "This is a live preview. Dataverse applies the authoritative routing rule when the request is saved."
      });
    } catch (error) {
      setPreview({
        status: "error",
        heading: "Preview temporarily unavailable",
        department: "Server-side routing",
        sla: "Applied after save",
        approval: isHighImpactRequest() ? "Manager approval likely" : "To be confirmed",
        documentation: isHighImpactRequest() ? "Impact notes required" : "Standard",
        note: error.message || "The server-side plugin still applies routing on save."
      });
    }
  }

  function summaryValue(value, fallback) {
    return value && String(value).trim() ? String(value).trim() : fallback;
  }

  function reviewRow(label, value) {
    return `
      <div class="esi-review-row">
        <span>${html(label)}</span>
        <strong>${html(value)}</strong>
      </div>`;
  }

  function updateReview() {
    if (!review) return;
    const documents = Array.from(field("documents")?.files || []);
    review.innerHTML = [
      reviewRow("Title", summaryValue(field("title")?.value, "Not entered")),
      reviewRow("Category", summaryValue(selectedCategoryName(), "Not selected")),
      reviewRow("Severity", summaryValue(selectedText("severity"), "Not selected")),
      reviewRow("Priority", summaryValue(selectedText("priority"), "Not selected")),
      reviewRow("Business impact", summaryValue(field("impact")?.value, "Not provided")),
      reviewRow("Supporting documents", documents.length ? `${documents.length} file${documents.length === 1 ? "" : "s"} selected` : "No files selected")
    ].join("");
  }

  function formatBytes(bytes) {
    if (!bytes) return "0 KB";
    const kb = bytes / 1024;
    if (kb < 1024) return `${Math.round(kb)} KB`;
    return `${(kb / 1024).toFixed(1)} MB`;
  }

  function updateFileList() {
    if (!fileList) return;
    const files = Array.from(field("documents")?.files || []);
    if (!files.length) {
      fileList.innerHTML = "";
      return;
    }
    fileList.innerHTML = files
      .map((file) => `<div><strong>${html(file.name)}</strong><span>${html(formatBytes(file.size))}</span></div>`)
      .join("");
  }

  function showResult(tone, title, body) {
    result.className = `esi-result is-visible ${tone === "success" ? "is-success" : tone === "error" ? "is-error" : ""}`;
    result.innerHTML = `
      <div class="esi-result-card">
        <strong>${html(title)}</strong>
        <span>${html(body)}</span>
      </div>`;
  }

  function clearResult() {
    result.className = "esi-result";
    result.innerHTML = "";
  }

  function extractCreatedId(xhr) {
    const entityId = xhr.getResponseHeader("entityid") || xhr.getResponseHeader("OData-EntityId") || "";
    const match = entityId.match(/\(([^)]+)\)/);
    return match ? match[1] : entityId;
  }

  async function submitRequest(event) {
    event.preventDefault();
    if (!validateBefore("review") || !validateStep("review") || !form.reportValidity()) return;

    const categoryId = field("category").value;
    if (/^static-/.test(categoryId)) {
      showResult("error", "Live category lookup required", "The category list could not be loaded from Dataverse, so the request was not submitted.");
      return;
    }

    submitButton.disabled = true;
    showResult("info", "Submitting request...", "Creating the Dataverse service request and capturing document metadata.");

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

      showResult(
        "success",
        "Request submitted",
        confirmation
          ? `Confirmation number ${confirmation} has been created. Approval and ERP sync continue through Power Automate when required.`
          : "The request was submitted. The confirmation number will be available in the internal app."
      );

      form.reset();
      updateFileList();
      setActiveStep("details");
      updateReview();
      await populateCategories();
      await refreshPreview();
    } catch (error) {
      showResult("error", "Submission failed", error.message || "The request could not be submitted.");
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

  stepButtons.forEach((button) => {
    button.addEventListener("click", () => goToStep(button.dataset.stepTarget));
  });

  root.querySelectorAll("[data-next-step]").forEach((button) => {
    button.addEventListener("click", () => goToStep(button.dataset.nextStep));
  });

  root.querySelectorAll("[data-prev-step]").forEach((button) => {
    button.addEventListener("click", () => setActiveStep(button.dataset.prevStep));
  });

  root.querySelectorAll("input, select, textarea").forEach((control) => {
    control.addEventListener("input", () => {
      applyConditionalRequirements();
      if (control.checkValidity()) control.classList.remove("is-invalid");
      setStepError(control.closest("[data-step]")?.dataset.step, "");
      updateReview();
      if (result.classList.contains("is-error")) clearResult();
    });
  });

  root.querySelectorAll("[data-routing-input]").forEach((input) => {
    input.addEventListener("change", () => {
      refreshPreview();
      updateReview();
    });
  });

  field("documents")?.addEventListener("change", () => {
    updateFileList();
    updateReview();
  });

  form?.addEventListener("submit", submitRequest);

  setActiveStep("details");
  updateFileList();
  populateCategories().then(refreshPreview);
})();
