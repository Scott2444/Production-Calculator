export interface Product {
    puid: string;
    name: string;
    description: string | null;
    createdAt: string;
    updatedAt: string;
}

export type ProductSummary = Pick<Product, "puid" | "name">;
