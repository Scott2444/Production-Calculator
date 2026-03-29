export interface Project {
    puid: string;
    name: string;
    description: string | null;
    isPublic: boolean;
    aliasProjectPuid: string | null;
    aliasCount: number;
    ownerUsername: string;
    createdAt: string;
    updatedAt: string;
}
