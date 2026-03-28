export async function fetchAttributes(
    project: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
) {
    const res = await protectedApi(`/projects/${project}/attributes`, {
        method: "GET",
    });
    if (!res.ok) throw new Error("Failed to load attributes");
    return res.json();
}

export interface NewAttributePayload {
    name: string;
    description: string | null;
    unit: string | null;
}

export async function postNewAttribute(
    project: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
    payload: NewAttributePayload,
) {
    const res = await protectedApi(`/projects/${project}/attributes`, {
        method: "POST",
        body: JSON.stringify(payload),
        headers: {
            "Content-Type": "application/json",
        },
    });
    if (!res.ok) {
        let message = "Failed to create attribute.";
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

export async function updateAttribute(
    project: string,
    attribute: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
    payload: NewAttributePayload,
) {
    const res = await protectedApi(
        `/projects/${project}/attributes/${attribute}`,
        {
            method: "PUT",
            body: JSON.stringify(payload),
            headers: {
                "Content-Type": "application/json",
            },
        },
    );
    if (!res.ok) {
        let message = "Failed to update attribute.";
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

export async function deleteAttribute(
    project: string,
    attribute: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
) {
    const res = await protectedApi(
        `/projects/${project}/attributes/${attribute}`,
        {
            method: "DELETE",
        },
    );
    if (!res.ok) {
        const message = "Failed to delete attribute.";
        throw new Error(message);
    }
}
