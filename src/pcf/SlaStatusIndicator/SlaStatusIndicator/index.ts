import { IInputs, IOutputs } from "./generated/ManifestTypes";

export class SlaStatusIndicator implements ComponentFramework.StandardControl<IInputs, IOutputs> {
    private container!: HTMLDivElement;

    constructor() {
    }

    public init(
        context: ComponentFramework.Context<IInputs>,
        notifyOutputChanged: () => void,
        state: ComponentFramework.Dictionary,
        container: HTMLDivElement
    ): void {
        this.container = container;
        this.container.className = "hx-sla-indicator";
        this.container.setAttribute("role", "status");
        this.container.setAttribute("aria-live", "polite");
    }

    public updateView(context: ComponentFramework.Context<IInputs>): void {
        const statusText = context.parameters.statusText.raw || "Not started";
        const severity = context.parameters.severity.raw || "Unspecified";
        const dueOn = context.parameters.slaDueOn.raw || null;
        const requiresApproval = context.parameters.requiresApproval.raw === true;
        const slaState = this.getSlaState(dueOn, statusText);
        const severityClass = severity.toLowerCase().replace(/[^a-z]/g, "");

        this.container.innerHTML = `
            <div class="hx-sla-card hx-severity-${severityClass} hx-sla-${slaState.key}">
                <div class="hx-sla-main">
                    <span class="hx-sla-dot" aria-hidden="true"></span>
                    <div>
                        <div class="hx-sla-label">${this.escape(statusText)}</div>
                        <div class="hx-sla-meta">${this.escape(severity)} severity · ${slaState.label}</div>
                    </div>
                </div>
                <div class="hx-sla-side">
                    <span class="hx-sla-pill">${requiresApproval ? "Approval Required" : "Approval Not Required"}</span>
                    <span class="hx-sla-due">${slaState.dueText}</span>
                </div>
            </div>`;
    }

    public getOutputs(): IOutputs {
        return {};
    }

    public destroy(): void {
        this.container.innerHTML = "";
    }

    private getSlaState(dueOn: Date | null, statusText: string): { key: string; label: string; dueText: string } {
        const normalizedStatus = statusText.toLowerCase();
        if (normalizedStatus.includes("closed") || normalizedStatus.includes("resolved")) {
            return { key: "complete", label: "Complete", dueText: "SLA complete" };
        }

        if (!dueOn) {
            return { key: "unknown", label: "SLA pending", dueText: "No due date" };
        }

        const due = dueOn instanceof Date ? dueOn : new Date(dueOn);
        const now = new Date();
        const hoursRemaining = (due.getTime() - now.getTime()) / 36e5;
        const dueText = new Intl.DateTimeFormat(undefined, {
            month: "short",
            day: "numeric",
            hour: "numeric",
            minute: "2-digit"
        }).format(due);

        if (hoursRemaining < 0) {
            return { key: "breached", label: "SLA breached", dueText };
        }

        if (hoursRemaining <= 4) {
            return { key: "risk", label: "At risk", dueText };
        }

        return { key: "track", label: "On track", dueText };
    }

    private escape(value: string): string {
        const element = document.createElement("span");
        element.textContent = value;
        return element.innerHTML;
    }
}
