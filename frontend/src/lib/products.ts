export async function fetchProducts(
    userId: string,
    project: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
) {
    const res = await protectedApi(`/api/users/${userId}/projects/${project}`);
    if (!res.ok) throw new Error("Failed to load projects");
    return res.json();
}
