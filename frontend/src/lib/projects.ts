import { Project } from "@/types/projects";

export async function fetchProjects(
    userId: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
) {
    const res = await protectedApi(`/users/${userId}/projects`, {
        method: "GET",
    });
    if (!res.ok) throw new Error("Failed to load projects");
    return res.json();
}

export async function fetchProject(
    projectPuid: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
) {
    const res = await protectedApi(`/projects/${projectPuid}`, {
        method: "GET",
    });
    if (!res.ok) throw new Error("Failed to load project");
    return res.json();
}

export async function resolveProject(
    username: string,
    projectName?: string,
    protectedApi: (
        input: RequestInfo,
        init?: RequestInit,
    ) => Promise<Response> = fetch,
) {
    let url = `/resolve/projects?username=${encodeURIComponent(username)}`;
    if (projectName) {
        url += `&project=${encodeURIComponent(projectName)}`;
    }
    const res = await protectedApi(url, {
        method: "GET",
    });
    if (!res.ok) throw new Error("Failed to load project");
    return res.json() as Promise<
        { projectName: string; projectPuid: string }[]
    >;
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
    const res = await protectedApi(`/projects`, {
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
    const res = await protectedApi(`/projects/${project}`, {
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
    const res = await protectedApi(`/projects/${project}`, {
        method: "DELETE",
    });
    if (!res.ok) {
        const message = "Failed to delete project.";
        throw new Error(message);
    }
}

export interface PublicProjectSearchResult {
    projects: Project[];
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
}

export async function searchPublicProjects(
    query: string,
    page: number,
    pageSize: number,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
) {
    const params = new URLSearchParams({
        query,
        page: String(page),
        pageSize: String(pageSize),
    });

    const res = await protectedApi(`/projects/search/public?${params}`, {
        method: "GET",
    });

    if (!res.ok) {
        let message = "Failed to load projects.";
        try {
            const data = (await res.json()) as {
                error?: string;
                message?: string;
            };
            if (data?.error) message = data.error;
            else if (data?.message) message = data.message;
        } catch {
            // ignore json parse errors
        }
        throw new Error(message);
    }

    return (await res.json()) as PublicProjectSearchResult;
}
