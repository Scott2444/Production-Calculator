# Production-Calculator

## Summary

This full-stack project will enable users to create projects of production pipelines for management style games similar to Factorio, Satisfactory, or Dyson Sphere Program. It will provide a platform to input recipes, create output requirements, and calculate production line designs. These projects will be publically accessible by other users.

## Architecture

### Frontend

This uses static Next.js with client side dynamic routing.
The styling is done in Tailwind CSS.
The state management is done with React.
Data fetching, persistent local storage, and routing done with TanStack.
Workflows use React Flow.

### Backend

The framework is use .NET Core Web API and EF Core for database communication.
Authentication with JWT.

### Database

PostgreSQL.
Flyway for DB migration and versioning.

#### Entity Relation Diagram

https://michiganstate-my.sharepoint.com/personal/haakens3_msu_edu/_layouts/15/Doc.aspx?sourcedoc={a9564c30-e26d-4955-9261-0c8381a41fcf}&action=embedview

## Hosting

This is runs on a self-hosted server proxied through Cloudflare Tunnels.

## Calculations

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
(base_speed + flat_speed_bonus)<br>
× (1 + additive_percent_bonus)<br>
× multiplicative_modifiers<br>
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
