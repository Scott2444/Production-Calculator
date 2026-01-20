export async function fetchMachines(
    project: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
) {
    const res = await protectedApi(`/api/projects/${project}/machines`, {
        method: "GET",
    });
    if (!res.ok) throw new Error("Failed to load machines");
    return res.json();
}
