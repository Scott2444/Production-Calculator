import type { User } from "@/types/user";
import type { ProtectedApi } from "@/lib/api";

async function getErrorMessage(
    res: Response,
    fallbackMessage: string,
): Promise<string> {
    try {
        const data = await res.json();
        const message = data?.message || data?.error;
        if (typeof message === "string" && message.trim()) {
            return message;
        }
    } catch {
        // ignore JSON parse errors and fall back to default message
    }
    return fallbackMessage;
}

export async function fetchUser(
    userId: string,
    protectedApi: ProtectedApi,
): Promise<User> {
    const res = await protectedApi(`/users/${userId}`);
    if (!res.ok) {
        throw new Error(await getErrorMessage(res, "Failed to load user."));
    }
    return res.json();
}

export interface LoginPayload {
    username: string;
    password: string;
}

export async function deleteUser(
    userId: string,
    loginPayload: LoginPayload,
    protectedApi: ProtectedApi,
) {
    const res = await protectedApi(`/users/${userId}`, {
        skipRefreshOn401: true,
        method: "DELETE",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify(loginPayload),
    });
    if (!res.ok) {
        const fallbackMessage =
            res.status === 401
                ? "Invalid password. Please try again."
                : "Failed to delete user.";
        throw new Error(await getErrorMessage(res, fallbackMessage));
    }
}
