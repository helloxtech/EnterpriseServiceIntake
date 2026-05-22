/*
 * ATTENTION: The "eval" devtool has been used (maybe by default in mode: "development").
 * This devtool is neither made for production nor for readable output files.
 * It uses "eval()" calls to create a separate source file in the browser devtools.
 * If you are trying to read the output file, select a different devtool (https://webpack.js.org/configuration/devtool/)
 * or disable the default devtool with "devtool: false".
 * If you are looking for production-ready output files, see mode: "production" (https://webpack.js.org/configuration/mode/).
 */
var pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad;
/******/ (() => { // webpackBootstrap
/******/ 	"use strict";
/******/ 	var __webpack_modules__ = ({

/***/ "./SlaStatusIndicator/index.ts":
/*!*************************************!*\
  !*** ./SlaStatusIndicator/index.ts ***!
  \*************************************/
/***/ ((__unused_webpack_module, exports) => {

eval("\n\nObject.defineProperty(exports, \"__esModule\", ({\n  value: true\n}));\nexports.SlaStatusIndicator = void 0;\nvar SlaStatusIndicator = /** @class */function () {\n  function SlaStatusIndicator() {}\n  SlaStatusIndicator.prototype.init = function (context, notifyOutputChanged, state, container) {\n    this.container = container;\n    this.container.className = \"hx-sla-indicator\";\n    this.container.setAttribute(\"role\", \"status\");\n    this.container.setAttribute(\"aria-live\", \"polite\");\n  };\n  SlaStatusIndicator.prototype.updateView = function (context) {\n    var statusText = context.parameters.statusText.raw || \"Not started\";\n    var severity = context.parameters.severity.raw || \"Unspecified\";\n    var dueOn = context.parameters.slaDueOn.raw || null;\n    var requiresApproval = context.parameters.requiresApproval.raw === true;\n    var slaState = this.getSlaState(dueOn, statusText);\n    var severityClass = severity.toLowerCase().replace(/[^a-z]/g, \"\");\n    this.container.innerHTML = \"\\n            <div class=\\\"hx-sla-card hx-severity-\".concat(severityClass, \" hx-sla-\").concat(slaState.key, \"\\\">\\n                <div class=\\\"hx-sla-main\\\">\\n                    <span class=\\\"hx-sla-dot\\\" aria-hidden=\\\"true\\\"></span>\\n                    <div>\\n                        <div class=\\\"hx-sla-label\\\">\").concat(this.escape(statusText), \"</div>\\n                        <div class=\\\"hx-sla-meta\\\">\").concat(this.escape(severity), \" severity \\u00B7 \").concat(slaState.label, \"</div>\\n                    </div>\\n                </div>\\n                <div class=\\\"hx-sla-side\\\">\\n                    <span class=\\\"hx-sla-pill\\\">\").concat(requiresApproval ? \"Approval Required\" : \"Approval Not Required\", \"</span>\\n                    <span class=\\\"hx-sla-due\\\">\").concat(slaState.dueText, \"</span>\\n                </div>\\n            </div>\");\n  };\n  SlaStatusIndicator.prototype.getOutputs = function () {\n    return {};\n  };\n  SlaStatusIndicator.prototype.destroy = function () {\n    this.container.innerHTML = \"\";\n  };\n  SlaStatusIndicator.prototype.getSlaState = function (dueOn, statusText) {\n    var normalizedStatus = statusText.toLowerCase();\n    if (normalizedStatus.includes(\"closed\") || normalizedStatus.includes(\"resolved\")) {\n      return {\n        key: \"complete\",\n        label: \"Complete\",\n        dueText: \"SLA complete\"\n      };\n    }\n    if (!dueOn) {\n      return {\n        key: \"unknown\",\n        label: \"SLA pending\",\n        dueText: \"No due date\"\n      };\n    }\n    var due = dueOn instanceof Date ? dueOn : new Date(dueOn);\n    var now = new Date();\n    var hoursRemaining = (due.getTime() - now.getTime()) / 36e5;\n    var dueText = new Intl.DateTimeFormat(undefined, {\n      month: \"short\",\n      day: \"numeric\",\n      hour: \"numeric\",\n      minute: \"2-digit\"\n    }).format(due);\n    if (hoursRemaining < 0) {\n      return {\n        key: \"breached\",\n        label: \"SLA breached\",\n        dueText: dueText\n      };\n    }\n    if (hoursRemaining <= 4) {\n      return {\n        key: \"risk\",\n        label: \"At risk\",\n        dueText: dueText\n      };\n    }\n    return {\n      key: \"track\",\n      label: \"On track\",\n      dueText: dueText\n    };\n  };\n  SlaStatusIndicator.prototype.escape = function (value) {\n    var element = document.createElement(\"span\");\n    element.textContent = value;\n    return element.innerHTML;\n  };\n  return SlaStatusIndicator;\n}();\nexports.SlaStatusIndicator = SlaStatusIndicator;\n\n//# sourceURL=webpack://pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad/./SlaStatusIndicator/index.ts?");

/***/ })

/******/ 	});
/************************************************************************/
/******/ 	
/******/ 	// startup
/******/ 	// Load entry module and return exports
/******/ 	// This entry module can't be inlined because the eval devtool is used.
/******/ 	var __webpack_exports__ = {};
/******/ 	__webpack_modules__["./SlaStatusIndicator/index.ts"](0, __webpack_exports__);
/******/ 	pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad = __webpack_exports__;
/******/ 	
/******/ })()
;
if (window.ComponentFramework && window.ComponentFramework.registerControl) {
	ComponentFramework.registerControl('HelloX.ServiceIntake.SlaStatusIndicator', pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad.SlaStatusIndicator);
} else {
	var HelloX = HelloX || {};
	HelloX.ServiceIntake = HelloX.ServiceIntake || {};
	HelloX.ServiceIntake.SlaStatusIndicator = pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad.SlaStatusIndicator;
	pcf_tools_652ac3f36e1e4bca82eb3c1dc44e6fad = undefined;
}