# Workflow Design

Workflow calculations are the centeral idea and the most complex part of this project.
Basic Critera:

1. Automatically calculate a production line based on a product rate requirement
2. Keep track of user's implementation progress
3. Allow users to modify the workflow with alternative recipes/machines/modifers and propagate changes

## Conceptual Design

Each node represents a rate of products. The nodes make an acylic graph.
The root node is the finished product(s) and the leaf nodes are the raw resources.
There are two directions of the graph: demand and supply
The demand is the target rate defined by the user when they initialize the workflow.
The supply is the amount the user has implemented in their game.
Changes in demand propagate down towards the leaf nodes.
Changes in supply propagate up towards the root node.
The graph is demand driven. Changes in the workflow through alternative recipes, modifiers, etc. will first calculate the demand first. It is the user's responsiblity to follow the the graph and input their supply.

## Production Nodes

**ProductionNode**:
product_id // Primary product, ignore all other byproducts in v1
recipe_id
machine_id
modifiers[]
parent_id // Parent node
target_recipe_rate8
ideal_machine_count
inputs[] // list of ProductionNodes
is_root // Flag
is_external // Flag
**NodeState**:
actual_machine_count
external_supply_rate // If is_external, expect no inputs

## Versioning

Problem: Changes to the project may invalidate a workflow. If a user changes a recipe's products, a workflow using that recipe may not exist anymore.
Solution: Soft invalidation through warnings

We don't want to force any updates on the user without them knowing nor invalidate their existing workflow.

We can implement this warning on individual nodes. Nodes that have the different effective speed will need recalculation but the tree structure will remain the same. Nodes that are missing definitions will require restructuring through the demand calculation.

## Cycle Detection and Prevention

While calculating the demand for the tree and an input to a recipe already exists as output, mark it with a warning and don't explore this subtree.

## Future Features

1. Allow multiple recipes per product and distribute workload tools
2. Route/use multi-output recipes
3. Pick recipes based on minimization parameters
4. Belt throughput limits
5. Cyclic graphs
6. Cost-minimization demand algorithm
7. Cross-workflow interaction
