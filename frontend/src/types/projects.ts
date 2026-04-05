export interface Project {
    puid: string;
    name: string;
    description: string | null;
    isPublic: boolean;
    aliasProjectPuid: string | null;
    aliasCount: number;
    productCount: number;
    recipeCount: number;
    machineCount: number;
    modifierCount: number;
    attributeCount: number;
    workflowCount: number;
    ownerUsername: string;
    createdAt: string;
    updatedAt: string;
}
