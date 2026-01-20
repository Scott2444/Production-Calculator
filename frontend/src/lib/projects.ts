export async function fetchProjects(
    userId: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
) {
    const res = await protectedApi(`/api/users/${userId}/projects`, {
        method: "GET",
    });
    if (!res.ok) throw new Error("Failed to load projects");
    return res.json();
}

export interface UpsertProjectPayload {
    name: string;
    description: string | null;
    isPublic: boolean;
    aliasProjectPuid: string | null;
}

export async function postNewProject(
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
    payload: UpsertProjectPayload,
) {
    const res = await protectedApi(`/api/projects`, {
        method: "POST",
        body: JSON.stringify(payload),
        headers: {
            "Content-Type": "application/json",
        },
    });
    if (!res.ok) {
        let message = "Failed to create project.";
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

export async function updateProject(
    project: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
    payload: UpsertProjectPayload,
) {
    const res = await protectedApi(`/api/projects/${project}`, {
        method: "PUT",
        body: JSON.stringify(payload),
        headers: {
            "Content-Type": "application/json",
        },
    });
    if (!res.ok) {
        let message = "Failed to update project.";
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

export async function deleteProject(
    project: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
) {
    const res = await protectedApi(`/api/projects/${project}`, {
        method: "DELETE",
    });
    if (!res.ok) {
        let message = "Failed to delete project.";
        throw new Error(message);
    }
}
