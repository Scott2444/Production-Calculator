export interface Attribute {
    puid: string;
    name: string;
    description: string | null;
    unit: string | null;
    createdAt: string;
    updatedAt: string;
}

export type AttributeSummary = Pick<Attribute, "puid" | "name" | "unit">;
