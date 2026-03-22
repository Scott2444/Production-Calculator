export interface RecipeExchange {
    puid: string;
    quantity: number;
}

export interface RecipeAttributeRate {
    puid: string;
    rate: number;
}

export interface Recipe {
    puid: string;
    name: string;
    description: string | null;
    baseCraftingTime: number;
    inputs: RecipeExchange[];
    outputs: RecipeExchange[];
    attributes: RecipeAttributeRate[];
    createdAt: string;
    updatedAt: string;
}

export type RecipeSummary = Pick<Recipe, "puid" | "name" | "description">;
