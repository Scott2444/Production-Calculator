export async function fetchModifiers(
    project: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
) {
    const res = await protectedApi(`/api/projects/${project}/modifiers`, {
        method: "GET",
    });
    if (!res.ok) throw new Error("Failed to load modifiers");
    return res.json();
}
