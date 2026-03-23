import { Node } from "./workflowChart";

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
): Map<string, NodeAttributeValues> {
    const workflowAttributes = new Map<string, NodeAttributeValues>();

    function calculateNodeAttributes(
        node: Node,
        machineCount: number,
        recipeCount: number,
    ): Map<string, number> {
        const baseByAttribute = new Map<string, number>();
        const percentByAttribute = new Map<string, number>();
        const multiplicativeByAttribute = new Map<string, number>();

        for (const item of node.machineAttributes) {
            if (!item.puid) continue;
            const nextBase =
                (baseByAttribute.get(item.puid) ?? 0) +
                asFiniteNumber(item.rate) * machineCount;
            baseByAttribute.set(item.puid, nextBase);
        }

        for (const item of node.recipeAttributes) {
            if (!item.puid) continue;
            const nextBase =
                (baseByAttribute.get(item.puid) ?? 0) +
                asFiniteNumber(item.rate) * recipeCount;
            baseByAttribute.set(item.puid, nextBase);
        }

        for (const modifier of node.modifiers) {
            for (const attribute of modifier.attributes) {
                if (!attribute.attributePuid) continue;

                const attributeId = attribute.attributePuid;
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
