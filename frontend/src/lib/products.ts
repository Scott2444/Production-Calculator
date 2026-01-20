export async function fetchProducts(
    project: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
) {
    const res = await protectedApi(`/api/projects/${project}/products`, {
        method: "GET",
    });
    if (!res.ok) throw new Error("Failed to load products");
    return res.json();
}

export interface NewProductPayload {
    name: string;
    description: string | null;
}

export async function postNewProduct(
    project: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
    payload: NewProductPayload,
) {
    const res = await protectedApi(`/api/projects/${project}/products`, {
        method: "POST",
        body: JSON.stringify(payload),
        headers: {
            "Content-Type": "application/json",
        },
    });
    if (!res.ok) {
        let message = "Failed to create product.";
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

export async function updateProduct(
    project: string,
    product: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
    payload: NewProductPayload,
) {
    const res = await protectedApi(
        `/api/projects/${project}/products/${product}`,
        {
            method: "PUT",
            body: JSON.stringify(payload),
            headers: {
                "Content-Type": "application/json",
            },
        },
    );
    if (!res.ok) {
        let message = "Failed to update product.";
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

export async function deleteProduct(
    project: string,
    product: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
) {
    const res = await protectedApi(
        `/api/projects/${project}/products/${product}`,
        {
            method: "DELETE",
        },
    );
    if (!res.ok) {
        const message = "Failed to delete product.";
        throw new Error(message);
    }
}
