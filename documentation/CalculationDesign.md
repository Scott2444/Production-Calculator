# Calculation Design

Workflow Calculations and storage define the capabilities of the calculator. The design of this calculator is supposed to be as generic as possible while supporting all major features of major factory games.

## V1 - Speed and Rate Focus

The project data will consist of:

1. Products
2. Recipes
3. Machines
4. Modifiers

The recipes will hold the ratio of conversion between products as well as the crafting speed (in seconds).

The machines will be speed bonus inherent to that recipe (scalar). An example of machines with speed bonuses are in Dyson Sphere Program where the machine has a production speed bonus.

Modifiers have three fields for values:

1. Flat Speed Bonus (Ex: Oxygen Not Included - duplicant's skill bonus)
2. Additive Percent Bonus (Ex: Factorio - Speed modules)
3. Multiplicative Bonus (Ex: Satisfactory - Overclocking)

### Calculations

**Recipe**
products_per_recipe -> Products consumed/generated per recipe (products)<br>
base_crafting_time -> Seconds to complete one recipe (sec)<br>
**Modifers**<br>
effective_speed -> Multiplier of all effects on recipe_rate (scalar)<br>
base_speed -> Base speed of machine (scalar)<br>
flat_speed_bonus -> Additive bonuses (scalar)<br>
additive_percent_bonus -> Additive percent modifiers (%)<br>
multiplicative_modifiers -> Multiplicative modifiers (scalar)<br>
**Formulas**<br>
effective_speed =<br>
(base_speed + ∑flat_speed_bonus)<br>
× (1 + ∑additive_percent_bonus)<br>
× ∏ multiplicative_modifiers<br>
products_per_second =<br>
(products_per_recipe / base_crafting_time)<br>
× effective_speed<br>
recipes_per_second = effective_speed / base_crafting_time<br>

### Calculation Example

This example shows several parameters from different games<br>
products_per_recipe = 2 ingots<br>
base_crafting_time = 0.5s<br>
base_speed = 3 recipes/sec from improved machine (DSP)<br>
flat_speed_bonus = 2 recipes/sec from skill level (ONI)<br>
additive_percent_bonus = 20% from speed beacon (Factorio)<br>
multiplicative_modifiers = 3x from overclocking (Satisfactory)<br>

effective_speed =<br>
(3 + 2)<br>
× (1 + 0.2)<br>
× 3<br>
= 18 recipes/sec<br>
products_per_second =<br>
(2 / 0.5) \* 18<br>
= 72 ingots/sec<br>

### Problems

1. V1 works well if speed is your only concern but usually you would want more information from your workflow than just total output. What if you want power consumption, total value created, or pollution?
2. Factorio has a productivity module where the recipe inputs/outputs are multiplied. If you add a module you could get an 1.2x products for every recipe. This changes the solver's calculations and therefore we cannot have only post-solver modifiers.

## V2 - User Defined Attributes

There will be another project entity type (Product, Recipe, Machine, Modifier) called Attributes. These attributes are user defined. We can link these attributes to the other project entities except products. If it is linked to a recipe or machine, it will have a base value only. If it is linked to a modifier, the link will have the following fields:

1. Flat_Rate
2. Percent_Bonus
3. Multiplicative_Bonus

There are now two program derived attributes: Speed and Yield. These PDAs are inherent to modifiers. Only modifiers can hold these stats and they are automatically included in the modifier's data.<br>
Speed:

1. Flat_Bonus (renamed from flat_speed_bonus)
2. Percent_Bonus (renamed from additive_percent_bonus)
3. Multiplicative_Bonus (renamed from multiplicative_modifiers)

Yield has these fields:

1. Output Multiplier
2. Input Multiplier

Attributes can attach to Recipes, Machines, and Modifiers.<br>
Attribute links to Recipes and Machines can only store the base value.<br>
Modifiers can attach to machines.<br>
Attaching attributes can be defined in the project data or workflow specific, so a machine could have a power attribute of 10MW in the project data, but also get a 3x modifier to the power attribute due to overclocking in the workflow.<br>

### Calculations

We have divided the calculations into 5 sequential layers.

#### 1. Pre-Demand Configuration

- Yield modifiers must be added to recipes in order to change their recipe ratio.
- Externally provided products must be marked to avoid exploration of their subtree during the demand calculation.
- Preferred recipes must be marked.
- The target demand for products is specified so we know what to produce.

#### 2. Demand Solve

- Construct the graph using LP solver.
- Nodes will persist if that recipe is used again, but otherwise the graph will mutate.
- More info in SolverDesign.md.

#### 3. Pre-Supply Configuration

- Speed of recipes + modifiers must be considered to find out how many machines are necessary to meet the target demand rate.
- User provides how many machines they actually have

#### 4. Supply Solve

- Find the maximum utilization of existing machines using LP solver.
- More info in SolverDesign.md

#### 5. Attribute Calculation (On frontend)

- This is the main difference of this version of calculations. User defined attributes are calculated here at the end. They are supplementary to the rest of the calculations since they don't change the graph structure.
- Attributes are calculated per node. Attribute values will not affect any other node. Within the node, they will follow the following formula:<br>
  amount_node =<br>
  ((Machine_Flat_Rate \* machine_count) + (Recipe_Flat_Rate \* recipe_count) + ∑Modifier_Flat_Rate)<br>
  × (1 + ∑Percent_Bonus)<br>
  × ∏ Multiplicative_Bonus<br>
- These attributes can be aggregated (Sum, Avg, Median, Min, Max)

### UDA Examples

A common example is Power. In Satisfactory, this is based on the machine. The user can define Power as an attribute. They can link the power attribute to a machine and specify that this type of machine uses 10 MW of power. If you overclock a machine, the power scales with it. Overclocking a machine by 250% scales the power usage by 3.35x which is a multiplicative bonus. In Factorio, speed modules are additive percentages to the power increase.

### Yield Examples

Factorio has a productivity module where the recipe inputs/outputs are multiplied. If you add a module you could get an 1.2x products for every recipe.

### Notes

Attributes cannot link to products since this brings a lot more complexity that what I intend the calculator for. A common use of a product attribute is monetary value. For example, if a recipe/machine/modifier increases the value of it by 25%, does that carry onto the next node? Are all downstream recipes worth 25% more? Is the aggregation the sum or net of products in and out of nodes? If multiple input products have modifiers, do they added or the weighted average? What is the weight of a product? This complexity does not seem like it is worth adding to my calculator nor well utilizied by its users.

Users can have a flat_rate on both recipes and machines. The flat_rate will add together on a single node. There may be rare cases where this is useful.
