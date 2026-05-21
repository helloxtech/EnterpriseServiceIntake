(function () {
  const root = document.querySelector("[data-esi-document-page]");
  if (!root) return;

  const status = root.querySelector("[data-document-status]");
  const uploadArea = root.querySelector(".esi-document-upload");
  if (!status || !uploadArea) return;

  function setStatus(tone, message) {
    status.className = `esi-document-status ${tone ? `is-${tone}` : ""}`.trim();
    status.textContent = message;
  }

  function hasSharePointDocumentControl() {
    return Boolean(uploadArea.querySelector([
      "#SharePointDocuments",
      "[data-name='SharePointDocuments']",
      "[id*='SharePointDocuments']",
      "[id*='sharepoint']",
      "[class*='sharepoint']",
      ".entity-grid",
      ".subgrid"
    ].join(",")));
  }

  function hasPermissionOrRenderingError() {
    const text = uploadArea.textContent || "";
    return /permission|access denied|not authorized|could not find|record not found|error/i.test(text);
  }

  function evaluateUploadArea() {
    if (hasSharePointDocumentControl()) {
      setStatus("ready", "SharePoint upload area is ready. Use the document grid below to add supporting files for this request.");
      return true;
    }

    if (hasPermissionOrRenderingError()) {
      setStatus("error", "The SharePoint upload area could not load for this request. Confirm the request belongs to the signed-in contact and that document-location table permissions are active.");
      return true;
    }

    return false;
  }

  if (evaluateUploadArea()) return;

  const observer = new MutationObserver(() => {
    if (evaluateUploadArea()) observer.disconnect();
  });
  observer.observe(uploadArea, { childList: true, subtree: true });

  window.setTimeout(() => {
    if (!evaluateUploadArea()) {
      setStatus("warning", "The SharePoint upload area is still loading. If the document grid does not appear, refresh this page or verify SharePoint document management for Service Request.");
    }
    observer.disconnect();
  }, 8000);
})();
