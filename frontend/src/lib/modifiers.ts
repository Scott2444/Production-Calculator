export async function fetchModifiers(
    project: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
) {
    const res = await protectedApi(`/projects/${project}/modifiers`, {
        method: "GET",
    });
    if (!res.ok) throw new Error("Failed to load modifiers");
    return res.json();
}

export interface NewModifierPayload {
    name: string;
    description: string | null;
    flatSpeedBonus: number;
    additivePercentBonus: number;
    multiplicativeModifier: number;
}

export async function postNewModifier(
    project: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
    payload: NewModifierPayload,
) {
    const res = await protectedApi(`/projects/${project}/modifiers`, {
        method: "POST",
        body: JSON.stringify(payload),
        headers: {
            "Content-Type": "application/json",
        },
    });
    if (!res.ok) {
        let message = "Failed to create modifier.";
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

export async function updateModifier(
    project: string,
    modifier: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
    payload: NewModifierPayload,
) {
    const res = await protectedApi(
        `/projects/${project}/modifiers/${modifier}`,
        {
            method: "PUT",
            body: JSON.stringify(payload),
            headers: {
                "Content-Type": "application/json",
            },
        },
    );
    if (!res.ok) {
        let message = "Failed to update modifier.";
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

export async function deleteModifier(
    project: string,
    modifier: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
) {
    const res = await protectedApi(
        `/projects/${project}/modifiers/${modifier}`,
        {
            method: "DELETE",
        },
    );
    if (!res.ok) {
        const message = "Failed to delete modifier.";
        throw new Error(message);
    }
}
