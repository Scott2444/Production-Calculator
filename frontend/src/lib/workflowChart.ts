export interface Node {
    puid: string;
    recipePuid: string;
    machinePuid: string | null;
    actualMachineCount: number | null;
    calculatedMachineCount: number | null;
    calculatedTargetRate: number | null;
    calculatedActualRate: number | null;
    modifierPuids: string[];
}

export interface Edge {
    producerNodePuid: string | null;
    consumerNodePuid: string | null;
    productPuid: string;
    calculatedFlowRate: number;
    actualFlowRate: number;
}

export interface Target {
    productPuid: string;
    targetRate: number;
}

export interface ProductNode {
    productPuid: string;
    calculatedFlowRate: number;
    actualFlowRateIn: number;
    actualFlowRateOut: number;
    isExternal: boolean;
}

export interface WorkflowChart {
    nodes: Node[];
    edges: Edge[];
    targets: Target[];
    productNodes: ProductNode[];
    preferredRecipes: string[];
}

export async function fetchWorkflowChart(
    projectId: string,
    workflowId: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
) {
    const res = await protectedApi(
        `/projects/${projectId}/workflows/${workflowId}/chart`,
        {
            method: "GET",
        },
    );
    if (!res.ok) throw new Error("Failed to load workflow chart.");
    return res.json();
}

export async function updateWorkflowChart(
    projectId: string,
    workflowId: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
) {
    const res = await protectedApi(
        `/projects/${projectId}/workflows/${workflowId}/chart`,
        {
            method: "PATCH",
        },
    );
    if (!res.ok) throw new Error("Failed to update workflow chart.");
    return res.json();
}

export async function updateWorkflowTargets(
    projectId: string,
    workflowId: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
    payload: { targets: Target[] },
) {
    const res = await protectedApi(
        `/projects/${projectId}/workflows/${workflowId}/target`,
        {
            method: "PUT",
            body: JSON.stringify(payload),
            headers: {
                "Content-Type": "application/json",
            },
        },
    );
    if (!res.ok) {
        let message = "Failed to update workflow targets.";
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

export interface UpdateWorkflowChartPayload {
    machinePuid: string;
    actualMachineCount: number;
    modifierPuids: string[];
}

export async function updateWorkflowNode(
    projectId: string,
    workflowId: string,
    nodeId: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
    payload: UpdateWorkflowChartPayload,
) {
    const res = await protectedApi(
        `/projects/${projectId}/workflows/${workflowId}/nodes/${nodeId}`,
        {
            method: "PUT",
            body: JSON.stringify(payload),
            headers: {
                "Content-Type": "application/json",
            },
        },
    );
    if (!res.ok) {
        let message = "Failed to update workflow node.";
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

export async function updateWorkflowPreferredRecipes(
    projectId: string,
    workflowId: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
    payload: { recipePuids: string[] },
) {
    const res = await protectedApi(
        `/projects/${projectId}/workflows/${workflowId}/recipes`,
        {
            method: "PUT",
            body: JSON.stringify(payload),
            headers: {
                "Content-Type": "application/json",
            },
        },
    );
    if (!res.ok) {
        let message = "Failed to update workflow preferred recipes.";
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

export async function setWorkflowProductExternal(
    projectId: string,
    workflowId: string,
    productPuid: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
    payload: { isExternal: boolean; externalRate: number | null },
) {
    const res = await protectedApi(
        `/projects/${projectId}/workflows/${workflowId}/external/${productPuid}/`,
        {
            method: "PUT",
            body: JSON.stringify(payload),
            headers: {
                "Content-Type": "application/json",
            },
        },
    );
    if (!res.ok) {
        let message = "Failed to update workflow product external status.";
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
