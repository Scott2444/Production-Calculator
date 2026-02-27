export async function fetchRecipes(
    project: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
) {
    const res = await protectedApi(`/projects/${project}/recipes`, {
        method: "GET",
    });
    if (!res.ok) throw new Error("Failed to load recipes");
    return res.json();
}

export interface NewRecipePayload {
    name: string;
    description: string | null;
    baseCraftingTime: number;
    inputs: { puid: string; quantity: number }[];
    outputs: { puid: string; quantity: number }[];
}

export async function postNewRecipe(
    project: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
    payload: NewRecipePayload,
) {
    const res = await protectedApi(`/projects/${project}/recipes`, {
        method: "POST",
        body: JSON.stringify(payload),
        headers: {
            "Content-Type": "application/json",
        },
    });
    if (!res.ok) {
        let message = "Failed to create recipe.";
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

export async function updateRecipe(
    project: string,
    recipe: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
    payload: NewRecipePayload,
) {
    const res = await protectedApi(`/projects/${project}/recipes/${recipe}`, {
        method: "PUT",
        body: JSON.stringify(payload),
        headers: {
            "Content-Type": "application/json",
        },
    });
    if (!res.ok) {
        let message = "Failed to update recipe.";
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

export async function deleteRecipe(
    project: string,
    recipe: string,
    protectedApi: (input: RequestInfo, init?: RequestInit) => Promise<Response>,
) {
    const res = await protectedApi(`/projects/${project}/recipes/${recipe}`, {
        method: "DELETE",
    });
    if (!res.ok) {
        const message = "Failed to delete recipe.";
        throw new Error(message);
    }
}
