import { Node } from "./workflowChart";
import { Machine } from "@/types/machines";
import { Modifier } from "@/types/modifiers";
import { Recipe } from "@/types/recipes";

export interface NodeAttributeValues {
    demand: Map<string, number>;
    supply: Map<string, number>;
}

function asFiniteNumber(value: number | null | undefined): number {
    if (typeof value !== "number" || !Number.isFinite(value)) {
        return 0;
    }
    return value;
}

/**
 * Performs the attribute calculations per node, for all nodes in the workflow
 * @returns Returns a map of Map<NodeId, { demand: Map<AttributeId, AttributeValue>, supply: Map<AttributeId, AttributeValue> }>
 */
export function calculateWorkflowAttributes(
    nodes: Node[],
    recipes: Recipe[],
    machines: Machine[],
    modifiers: Modifier[],
): Map<string, NodeAttributeValues> {
    const workflowAttributes = new Map<string, NodeAttributeValues>();
    const recipeByPuid = new Map(
        recipes.map((recipe) => [recipe.puid, recipe]),
    );
    const machineByPuid = new Map(
        machines.map((machine) => [machine.puid, machine]),
    );
    const modifierByPuid = new Map(
        modifiers.map((modifier) => [modifier.puid, modifier]),
    );

    function calculateNodeAttributes(
        node: Node,
        machineCount: number,
        recipeCount: number,
    ): Map<string, number> {
        const baseByAttribute = new Map<string, number>();
        const percentByAttribute = new Map<string, number>();
        const multiplicativeByAttribute = new Map<string, number>();

        const recipe = recipeByPuid.get(node.recipePuid);
        for (const item of recipe?.attributes ?? []) {
            if (!item.puid) continue;
            const nextBase =
                (baseByAttribute.get(item.puid) ?? 0) +
                asFiniteNumber(item.rate) * recipeCount;
            baseByAttribute.set(item.puid, nextBase);
        }

        const machine = node.machinePuid
            ? machineByPuid.get(node.machinePuid)
            : undefined;
        for (const item of machine?.attributes ?? []) {
            if (!item.puid) continue;
            const nextBase =
                (baseByAttribute.get(item.puid) ?? 0) +
                asFiniteNumber(item.rate) * machineCount;
            baseByAttribute.set(item.puid, nextBase);
        }

        for (const modifierPuid of node.modifierPuids) {
            const modifier = modifierByPuid.get(modifierPuid);
            for (const attribute of modifier?.attributes ?? []) {
                if (!attribute.puid) continue;

                const attributeId = attribute.puid;
                baseByAttribute.set(
                    attributeId,
                    (baseByAttribute.get(attributeId) ?? 0) +
                        asFiniteNumber(attribute.flatBonus),
                );
                percentByAttribute.set(
                    attributeId,
                    (percentByAttribute.get(attributeId) ?? 0) +
                        asFiniteNumber(attribute.percentBonus),
                );
                multiplicativeByAttribute.set(
                    attributeId,
                    (multiplicativeByAttribute.get(attributeId) ?? 1) *
                        asFiniteNumber(attribute.multiplicativeBonus ?? 1),
                );
            }
        }

        const nodeAttributeValues = new Map<string, number>();
        const attributeIds = new Set<string>([
            ...baseByAttribute.keys(),
            ...percentByAttribute.keys(),
            ...multiplicativeByAttribute.keys(),
        ]);

        for (const attributeId of attributeIds) {
            const base = baseByAttribute.get(attributeId) ?? 0;
            const percent = percentByAttribute.get(attributeId) ?? 0;
            const multiplicative =
                multiplicativeByAttribute.get(attributeId) ?? 1;
            const amount = base * (1 + percent) * multiplicative;

            nodeAttributeValues.set(
                attributeId,
                Number.isFinite(amount) ? amount : 0,
            );
        }

        return nodeAttributeValues;
    }

    for (const node of nodes) {
        const demandMachineCount = Math.max(
            0,
            asFiniteNumber(node.calculatedMachineCount),
        );
        const supplyMachineCount = Math.max(
            0,
            asFiniteNumber(node.actualMachineCount),
        );
        const demandRecipeRate = Math.max(
            0,
            asFiniteNumber(node.calculatedTargetRate),
        );
        const supplyRecipeRate = Math.max(
            0,
            asFiniteNumber(node.calculatedActualRate),
        );

        workflowAttributes.set(node.puid, {
            demand: calculateNodeAttributes(
                node,
                demandMachineCount,
                demandRecipeRate,
            ),
            supply: calculateNodeAttributes(
                node,
                supplyMachineCount,
                supplyRecipeRate,
            ),
        });
    }

    return workflowAttributes;
}
