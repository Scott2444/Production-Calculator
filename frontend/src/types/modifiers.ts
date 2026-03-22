export interface ModifierAttributeBonus {
    puid: string;
    flatBonus: number;
    percentBonus: number;
    multiplicativeBonus: number;
}

export interface Modifier {
    puid: string;
    name: string;
    description: string | null;
    flatBonus: number;
    percentBonus: number;
    multiplicativeBonus: number;
    inputPercent: number;
    outputPercent: number;
    attributes: ModifierAttributeBonus[];
    createdAt: string;
    updatedAt: string;
}

export type ModifierSummary = Pick<Modifier, "puid" | "name">;
