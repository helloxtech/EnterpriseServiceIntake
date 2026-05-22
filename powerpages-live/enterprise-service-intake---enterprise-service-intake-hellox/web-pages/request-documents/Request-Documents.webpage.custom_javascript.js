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

  function textOf(element) {
    return (element.getAttribute("aria-label") || element.getAttribute("title") || element.textContent || "")
      .replace(/\s+/g, " ")
      .trim();
  }

  function replaceExactText(from, to) {
    const walker = document.createTreeWalker(uploadArea, NodeFilter.SHOW_TEXT);
    const matches = [];
    while (walker.nextNode()) {
      if (walker.currentNode.nodeValue.trim() === from) {
        matches.push(walker.currentNode);
      }
    }
    matches.forEach((node) => {
      node.nodeValue = node.nodeValue.replace(from, to);
    });
  }

  function applyPortalLanguage() {
    [
      ["Request Summary", "Request details"],
      ["Confirmation Number", "Confirmation number"],
      ["Request Title", "Request title"],
      ["Lifecycle Status", "Status"],
      ["Customer Visible Updates", "Updates from Mitacs"],
      ["SharePoint Documents", "Supporting files"],
      ["Supporting Documents", "Uploaded files"],
      ["There are no folders or files to display.", "No files have been uploaded yet."]
    ].forEach(([from, to]) => replaceExactText(from, to));
  }

  function hideNewFolderActions() {
    uploadArea.querySelectorAll("button, a, input, span, li").forEach((element) => {
      if (!/^new folder$/i.test(textOf(element))) return;
      const action = element.closest("button, a, li, .dropdown-item, .toolbar-item") || element;
      action.hidden = true;
      action.setAttribute("aria-hidden", "true");
      action.classList.add("esi-hidden-new-folder");
    });
  }

  function applyUploadUx() {
    applyPortalLanguage();
    hideNewFolderActions();
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
    applyUploadUx();

    if (hasSharePointDocumentControl()) {
      setStatus("ready", "File upload is ready. Use the file list below to add supporting files for this request.");
      return true;
    }

    if (hasPermissionOrRenderingError()) {
      setStatus("error", "The file upload area could not load for this request. Confirm this request belongs to your signed-in account and try again.");
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
      setStatus("warning", "The file upload area is still loading. If the file list does not appear, refresh this page.");
    }
    observer.disconnect();
  }, 8000);
})();
