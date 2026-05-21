(function () {
  const root = document.querySelector("[data-esi-intake]");
  if (!root) return;

  const preview = root.querySelector("[data-routing-preview]");
  const review = root.querySelector("[data-review-summary]");
  const result = root.querySelector("[data-submit-result]");
  const form = root.querySelector(".esi-form");
  const submitButton = root.querySelector('button[type="submit"]');
  const steps = Array.from(root.querySelectorAll("[data-step]")).map((item) => item.dataset.step);
  const stepButtons = Array.from(root.querySelectorAll("[data-step-target]"));
  let resultModal = null;
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
      ? "Business impact is required for urgent or critical requests."
      : "");

    if (note) {
      note.textContent = required
        ? "Required for this request"
        : "Required for urgent or critical requests";
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
      const isComplete = index < activeIndex;
      const stepIndexBadge = button.querySelector(".esi-step-index");
      button.classList.toggle("is-active", isActive);
      button.classList.toggle("is-complete", isComplete);
      if (stepIndexBadge) {
        stepIndexBadge.textContent = isComplete ? "✓" : String(index + 1);
        stepIndexBadge.setAttribute("aria-hidden", "true");
      }
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
      return Promise.reject(new Error("We could not prepare the secure request. Refresh the page and try again."));
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
      }).fail(() => reject(new Error("We could not prepare the secure request. Refresh the page and try again.")));
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
        heading: "Categories are temporarily unavailable.",
        note: "You can still review the form, but submitting requires an active service category."
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
    if (status === "ready") return "Estimate ready";
    if (status === "warning") return "Needs review";
    if (status === "error") return "Unavailable";
    if (status === "loading") return "Checking options";
    return "Waiting for details";
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
      ${previewRow("Target response", sla)}
      ${previewRow("Review", approval, toneFor(approval, "approval"))}
      ${previewRow("Supporting files", documentation, toneFor(documentation, "documentation"))}
      </div>
      <p class="esi-preview-note">${html(state.note || "The final team and target response time are confirmed after you submit.")}</p>`;
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
        note: "The final team and target response time are confirmed after you submit."
      });
      return;
    }

    setPreview({
      status: "loading",
      heading: "Refreshing live routing preview...",
      note: "Checking the current service options for your request."
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
          approval: isHighImpactRequest() ? "Additional review may be required" : "To be confirmed",
          documentation: isHighImpactRequest() ? "Impact details required" : "As needed",
          note: "We will review this request after submission and assign it to the right team."
        });
        return;
      }

      setPreview({
        status: "ready",
        heading: match.name,
        department: match.department,
        sla: `${match.responseHours} hour response target`,
        approval: match.requiresApproval ? "Additional review required" : "No additional review expected",
        documentation: match.requiresDocumentation ? "Follow-up details may be required" : "Standard supporting files",
        note: "This is an estimate. The final team and target response time are confirmed after you submit."
      });
    } catch (error) {
      setPreview({
        status: "error",
        heading: "Preview temporarily unavailable",
        department: "Assigned after submission",
        sla: "Confirmed after submission",
        approval: isHighImpactRequest() ? "Additional review may be required" : "To be confirmed",
        documentation: isHighImpactRequest() ? "Impact details required" : "Standard",
        note: "We could not show an estimate right now. You can still submit the request."
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
    review.innerHTML = [
      reviewRow("Title", summaryValue(field("title")?.value, "Not entered")),
      reviewRow("Category", summaryValue(selectedCategoryName(), "Not selected")),
      reviewRow("Severity", summaryValue(selectedText("severity"), "Not selected")),
      reviewRow("Priority", summaryValue(selectedText("priority"), "Not selected")),
      reviewRow("Business impact", summaryValue(field("impact")?.value, "Not provided")),
      reviewRow("Supporting documents", "SharePoint upload available after submission")
    ].join("");
  }

  function ensureResultModal() {
    if (resultModal) return resultModal;

    resultModal = document.createElement("div");
    resultModal.className = "esi-submit-modal";
    resultModal.setAttribute("aria-hidden", "true");
    document.body.appendChild(resultModal);

    resultModal.addEventListener("click", (event) => {
      const action = event.target.closest("[data-result-action]")?.dataset.resultAction;
      if (!action || resultModal.dataset.locked === "true") return;

      if (action === "new") {
        closeResult();
        setActiveStep("details");
        window.scrollTo({ top: root.offsetTop - 20, behavior: "smooth" });
        field("title")?.focus({ preventScroll: true });
      }

      if (action === "documents") {
        const url = event.target.closest("[data-result-url]")?.dataset.resultUrl;
        if (url) window.location.href = url;
      }

      if (action === "review") {
        closeResult();
        setActiveStep("review");
        submitButton?.focus({ preventScroll: false });
      }

      if (action === "close") {
        const wasSuccess = resultModal.classList.contains("is-success");
        closeResult();
        if (wasSuccess) {
          field("title")?.focus({ preventScroll: false });
        } else {
          submitButton?.focus({ preventScroll: false });
        }
      }
    });

    document.addEventListener("keydown", (event) => {
      if (event.key === "Escape" && resultModal?.classList.contains("is-visible") && resultModal.dataset.locked !== "true") {
        const wasSuccess = resultModal.classList.contains("is-success");
        closeResult();
        if (wasSuccess) {
          field("title")?.focus({ preventScroll: false });
        } else {
          submitButton?.focus({ preventScroll: false });
        }
      }
    });

    return resultModal;
  }

  function resultIcon(tone) {
    if (tone === "success") return "OK";
    if (tone === "error") return "!";
    return "";
  }

  function resultActions(tone, options = {}) {
    if (tone === "success") {
      return `
        ${options.documentsUrl ? `<button type="button" class="esi-primary" data-result-action="documents" data-result-url="${html(options.documentsUrl)}">Upload supporting files</button>` : ""}
        <button type="button" class="esi-secondary" data-result-action="new">Submit another request</button>
        <button type="button" class="esi-secondary" data-result-action="close">Close</button>`;
    }

    if (tone === "error") {
      return `
        <button type="button" class="esi-primary" data-result-action="review">Review and try again</button>
        <button type="button" class="esi-secondary" data-result-action="close">Close</button>`;
    }

    return "";
  }

  function showResult(tone, title, body, options = {}) {
    const modal = ensureResultModal();
    const toneClass = tone === "success" ? "is-success" : tone === "error" ? "is-error" : "is-info";
    const confirmation = options.confirmation || "";

    result.className = `esi-result is-visible ${tone === "success" ? "is-success" : tone === "error" ? "is-error" : ""}`;
    result.textContent = `${title}. ${body}`;

    modal.dataset.locked = tone === "info" ? "true" : "false";
    modal.className = `esi-submit-modal is-visible ${toneClass}`;
    modal.setAttribute("aria-hidden", "false");
    modal.innerHTML = `
      <div class="esi-submit-modal-backdrop" data-result-action="${tone === "info" ? "" : "close"}"></div>
      <section class="esi-submit-dialog" role="dialog" aria-modal="true" aria-labelledby="esi-submit-title" aria-describedby="esi-submit-message" tabindex="-1">
        <div class="esi-submit-icon" aria-hidden="true">${html(resultIcon(tone))}</div>
        <div class="esi-submit-content">
          <span class="esi-submit-kicker">${tone === "success" ? "Submitted" : tone === "error" ? "Action needed" : "Sending request"}</span>
          <h2 id="esi-submit-title">${html(title)}</h2>
          <p id="esi-submit-message">${html(body)}</p>
          ${confirmation ? `
            <div class="esi-confirmation-number">
              <span>Confirmation number</span>
              <strong>${html(confirmation)}</strong>
            </div>` : ""}
          ${tone === "success" ? `
            <ul class="esi-submit-next">
              <li>Your request is now linked to your portal account.</li>
              <li>Supporting files are uploaded to the secure SharePoint document library after submission.</li>
              <li>Mitacs will confirm routing, review needs, and response timing after submission.</li>
            </ul>` : ""}
        </div>
        ${resultActions(tone, options) ? `<div class="esi-submit-actions">${resultActions(tone, options)}</div>` : ""}
      </section>`;

    modal.querySelector(".esi-submit-dialog")?.focus({ preventScroll: true });
  }

  function closeResult() {
    if (!resultModal) return;
    resultModal.className = "esi-submit-modal";
    resultModal.setAttribute("aria-hidden", "true");
    resultModal.dataset.locked = "false";
    resultModal.innerHTML = "";
  }

  function clearResult() {
    result.className = "esi-result";
    result.innerHTML = "";
    closeResult();
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
      showResult("error", "Service category unavailable", "We could not load the current service categories. Refresh the page and try again before submitting.");
      return;
    }

    submitButton.disabled = true;
    showResult("info", "Submitting request...", "Creating your request. Supporting files can be uploaded after the confirmation number is assigned.");

    const descriptionParts = [
      field("description").value,
      field("impact").value ? `Business impact: ${field("impact").value}` : ""
    ].filter(Boolean);

    const payload = {
      hx_title: field("title").value,
      hx_description: descriptionParts.join("\n\n"),
      hx_severity: Number(field("severity").value),
      hx_priority: Number(field("priority").value),
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
          ? "Your request has been received. Save this confirmation number if you need to follow up with Mitacs."
          : "Your request has been received. A confirmation number will be available after processing.",
        {
          confirmation,
          documentsUrl: requestId ? `/request-documents/?id=${encodeURIComponent(requestId)}` : ""
        }
      );

      form.reset();
      setActiveStep("details");
      updateReview();
      await populateCategories();
      await refreshPreview();
    } catch (error) {
      console.error(error);
      showResult("error", "Submission failed", "We could not submit your request. Your information is still on the form. Please review the details and try again.");
    } finally {
      submitButton.disabled = false;
    }
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

  form?.addEventListener("submit", submitRequest);

  setActiveStep("details");
  populateCategories().then(refreshPreview);
})();
