export async function fetchUser(
    userId: string,
    protectedApiFetch: (
        input: RequestInfo,
        init?: RequestInit,
    ) => Promise<Response>,
) {
    const res = await protectedApiFetch(`/api/users/${userId}`);
    if (!res.ok) throw new Error("Failed to load user");
    return res.json();
}
