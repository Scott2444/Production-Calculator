export interface Workflow {
    puid: string;
    name: string | null;
    description: string | null;
    createdAt: string;
    updatedAt: string;
}

export async function fetchWorkflows(
    projectId: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
) {
    const res = await protectedApi(`/projects/${projectId}/workflows`, {
        method: "GET",
    });
    if (!res.ok) throw new Error("Failed to load workflows");
    return res.json();
}

export interface NewWorkflowPayload {
    name: string;
    description: string | null;
}

export async function postNewWorkflow(
    project: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
    payload: NewWorkflowPayload,
) {
    const res = await protectedApi(`/projects/${project}/workflows`, {
        method: "POST",
        body: JSON.stringify(payload),
        headers: {
            "Content-Type": "application/json",
        },
    });
    if (!res.ok) {
        let message = "Failed to create workflow.";
        try {
            const data = (await res.json()) as { error?: string };
            if (data?.error) message = data.error;
        } catch {
            // ignore json parse errors
        }
        throw new Error(message);
    }
    return res.json();
}

export async function updateWorkflow(
    project: string,
    workflow: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
    payload: NewWorkflowPayload,
) {
    const res = await protectedApi(
        `/projects/${project}/workflows/${workflow}`,
        {
            method: "PUT",
            body: JSON.stringify(payload),
            headers: {
                "Content-Type": "application/json",
            },
        },
    );
    if (!res.ok) {
        let message = "Failed to update workflow.";
        try {
            const data = (await res.json()) as { error?: string };
            if (data?.error) message = data.error;
        } catch {
            // ignore json parse errors
        }
        throw new Error(message);
    }
    return res.json();
}

export async function deleteWorkflow(
    project: string,
    workflow: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
) {
    const res = await protectedApi(
        `/projects/${project}/workflows/${workflow}`,
        {
            method: "DELETE",
        },
    );
    if (!res.ok) {
        const message = "Failed to delete workflow.";
        throw new Error(message);
    }
}
