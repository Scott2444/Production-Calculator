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

## API

**Root Demand**
GET: Get a workflow graph
PUT: Update root demands

**Node Config**
PATCH: Override a recipe on a node
PATCH: Override a machine on a node
PATCH: Set node as external
POST: Apply a modifier
DELETE: Delete a modifer

**Node State**
PATCH: Set actual machine count
PATCH: Set external rate

## V1 - Naive Recursion

The structure is based off a Trie. When creating the tree, we can only have one root.

**Algorithm**
On each node:
Choose a recipe where the product is the output. We will just pick the first recipe avaliable.
Calculate how much of the inputs we will need. Recursively call this function to spawn it's subtree based on the required inputs' rate.

We will need to check for cycles by seeing if any inputs are already products. Mark these as cyclical and don't explore these trees.

If we need to change the recipe/machine/modifier, keep the subtrees of those whos inputs are the same. Propagate the new rate requirements. Build the trees of new inputs. Delete the trees of old inputs.

To set as external, we disable the subtree but don't delete in case the user wants to remove the external flag.

State changes will not change the tree structure and will propagate the state changes up the tree.

**Problems:**

1. Limited to one root product
2. Repeated subtrees if multiple products require the same input
3. We can't utilize recipes' secondary outputs

## V2 - Topological Sort

The new structure is based off Direct Acyclical Graphs(DAG). We can have multiple roots. We will use the same root/leaf node terminology. This will also prevent repeated subtrees for repeated input requirements and we can utilize secondary outputs. Some secondary outputs may not be utilized if there is a cycle.

**Algorithm**
We will create a list of inputs, outputs, and secondary outputs. The roots are added to the outputs.
On each node until we have visited each input: <br>
Choose a recipe where the product is the output. We will just pick the first recipe avaliable.
Create the node with all input and outputs.
Add the inputs to the list. If the input already exists in outputs, mark it as cyclical.
If there are additional outputs, mark them as secondary and add them to the secondary outputs
<br>
We now have the nodes and edges that make up our graph. We have also determined it is not cyclical.
We can use topological sort on this graph now to create our graph structure. The sort must only use the primary outputs to prevent cycles. We now have a total order for our nodes.

<br>
Now we must perform our numerical calculations.
Create a map of {product: remaining rate required}. Add the root nodes' product requirements to the map. Reverse iterate over the topological sort (from the root nodes). Calculate how much input is required and add it to the map. Subtract the output from the remaining rate required.

**Problems:**

1. We still cannot support cyclic behavior
2. Secondary outputs aren't optimized and may not be utilized

## V3 - Linear Programming

This version will solve using linear algebra to find the optimal solution. It will now be matrix A where rows are products and recipes are columns. Each entry (A) is the net product produces by the recipe, so inputs are negative and outputs are positive.
Each row will be A\*r = d where r is the production rate and d is the external demand vector (previously called root).
**r >= 0.** Recipe rates can't be negative, so we cannot use a traditional linear algebra.

**Demand Algorithm**
Assemble a matrix of all recipes with rows being net products. Create a demand vector based on the root demands the user has given. There needs to be a cost vector, but for now we can have all values set to 1 to minimize recipe rates.
Feed this info into a LP solver to come up with an optimal solution if possible.

Users will want to select alternative recipes even though they may not be optimal. We will set the cost of selected recipes to near zero (0.0001) to show it is a prefered recipe but still not free. Alternatively, we can set the other recipes to create the product with a (0,0) bounds.

**Supply Algorithm**
Since this design support cycles, the calculation for supply is not trivial. We can essentially do the inverse of the demand algorithm. Create a matrix A where each row is a product and each column is a recipe used in the solution. Each entry (A) is the net product produces by the recipe, so inputs are negative and outputs are positive. This the same as before. There will be two constraints.<br>

1. The net flow of every item must be >= 0 since we can't make resources out of thin-air
2. The rate must be between 0 and the maximum capacity of the user's current machines
   We can set the second constraint by setting the bounds as (0, max_rate)

We want to produce everything but creating our root demand is our ultimate goal. We will set the objective as 1 for everything except the root demand products will be weighted higher (10+).

## V3.5 - LP + Implicit Imports

Products may not always have a raw-resource recipe where a product is generated from nothing. The solver will not be able to create a solution if this is the case. Instead, we will have a hidden "import resource" recipe for every product. To keep the solver from using it unless no recipes exist, we will set the cost of these recipes extremely high (1,000,000). We can also mark imports of products that will be externally provided as nearly-free (0.0001).

However if the solver has to import any product, it will import the final product most likely. To solve this, we will penalize shallow products and encourage importing deeping products in the production tree.<br>
Cost_Import = BasePenalty × (FanOutMultiplier)^Depth<br>
We'll calculate this depth with a DFS search algorithm and memoziation to cache the product depth. We can also store the product depth in the DB in order to further improve performance.

## Future Features

1. Allow multiple recipes per product and distribute workload tools
2. Route/use multi-output recipes
3. Pick recipes based on minimization parameters
4. Belt throughput limits
5. Cyclic graphs
6. Cost-minimization demand algorithm
7. Cross-workflow interaction
