export async function fetchRecipes(
    project: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
) {
    const res = await protectedApi(`/api/projects/${project}/recipes`, {
        method: "GET",
    });
    if (!res.ok) throw new Error("Failed to load recipes");
    return res.json();
}
