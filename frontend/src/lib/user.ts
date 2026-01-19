export async function fetchUser(
    userId: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
) {
    const res = await protectedApi(`/api/users/${userId}`);
    if (!res.ok) throw new Error("Failed to load user");
    return res.json();
}
