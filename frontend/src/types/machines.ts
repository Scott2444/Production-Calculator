export interface MachineAttributeRate {
    puid: string;
    rate: number;
}

export interface Machine {
    puid: string;
    name: string;
    description: string | null;
    baseSpeed: number;
    recipePuids: string[];
    attributes: MachineAttributeRate[];
    createdAt: string;
    updatedAt: string;
}

export type MachineSummary = Pick<Machine, "puid" | "name" | "recipePuids">;
