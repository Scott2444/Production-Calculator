export async function fetchMachines(
    project: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
) {
    const res = await protectedApi(`/projects/${project}/machines`, {
        method: "GET",
    });
    if (!res.ok) throw new Error("Failed to load machines");
    return res.json();
}

export interface NewMachinePayload {
    name: string;
    description: string | null;
    baseSpeed: number;
    recipePuids: string[];
}

export async function postNewMachine(
    project: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
    payload: NewMachinePayload,
) {
    const res = await protectedApi(`/projects/${project}/machines`, {
        method: "POST",
        body: JSON.stringify(payload),
        headers: {
            "Content-Type": "application/json",
        },
    });
    if (!res.ok) {
        let message = "Failed to create machine.";
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

export async function updateMachine(
    project: string,
    machine: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
    payload: NewMachinePayload,
) {
    const res = await protectedApi(`/projects/${project}/machines/${machine}`, {
        method: "PUT",
        body: JSON.stringify(payload),
        headers: {
            "Content-Type": "application/json",
        },
    });
    if (!res.ok) {
        let message = "Failed to update machine.";
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

export async function deleteMachine(
    project: string,
    machine: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
) {
    const res = await protectedApi(`/projects/${project}/machines/${machine}`, {
        method: "DELETE",
    });
    if (!res.ok) {
        const message = "Failed to delete machine.";
        throw new Error(message);
    }
}
