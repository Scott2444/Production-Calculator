export async function fetchProjects(userId: string, protectedApiFetch: (input: RequestInfo, init?: RequestInit) => Promise<Response>) {
    const res = await protectedApiFetch(`/api/users/${userId}/projects`);
    if (!res.ok) throw new Error('Failed to load projects');
    return res.json();
}