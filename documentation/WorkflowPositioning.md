# Workflow Chart Node Positioning

## Summary

For the workflow chart we don't store the positions of nodes in our database. We need to determine the position of each node deterministically. Ideally, the position would be near-optimal so the user doesn't feel forced to move the nodes, especially since the user might feel frustrated if they have to move on every load.<br>

_Note that the definition of deterministic will be used loosely. In this case, deterministic algorithms will have similar looking workflows if they have similar structure. It is in addition to reproducing the same results with the same inputs._

## Outline

The layout of the graph must be intuitive to a factory flow starting from raw resources to the target products. We want to minimize the number of crossing edges and loop backs. This algorithm needs to handle cyclic behavior.

## Designs

### BFS Tiers

Explore the graph and tier each node using the distance from a target node. This handles cycles and a basic workflow, but struggles on grouping by alike sub-workflows.

### Force-Directed

Each edge would act as a spring and nodes that are closely tied would naturally attract each other. This is easy to implement with a library, but is non-deterministic and messy.

### Dagre

Dagre is essentially a topological sort for DAGs, although this can be adapted to DCGs by removing back edges and re-inserting them as the end. Factory worflows generally use this library since it produces very readable graphs.

### ELK (Eclipse Layout Kernel)

This is a more advanced option to Dagre as it uses multiple algorithms. In addition to Dagre, it uses force-directed, orthogonal routing, and more. It handles cycles and dense graphs well, but it may require more set up than the previous options.
