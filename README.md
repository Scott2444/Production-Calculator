# Production-Calculator

## Summary
This full-stack project will enable users to create projects of production pipelines for management style games similar to Factorio, Satisfactory, or Dyson Sphere Program. It will provide a platform to input recipes, create output requirements, and calculate production line designs. These projects will be publically accessible by other users.

## Architecture

### Frontend
This uses React framework with client-side rendering (main calculator interface) and server-side rendering (shareable project pages).
The styling is done in Tailwind CSS.
The state management is done with React Context API + Hooks.
For data visualization, this uses React Flow or D3.js???

### Backend
The framework will use .NET Core Web API and EF Core for database communication.
Authentication will be done with JWT.

### Database
This will use PostgreSQL.
Flyway for DB migration and versioning.
#### Entity Relation Diagram
https://michiganstate-my.sharepoint.com/personal/haakens3_msu_edu/_layouts/15/Doc.aspx?sourcedoc={a9564c30-e26d-4955-9261-0c8381a41fcf}&action=embedview

## Hosting
This is runs on a self-hosted server proxied through Cloudflare Tunnels.

## Calculations
**Recipe**
products_per_recipe -> Products consumed/generated per recipe (products)
base_crafting_time -> Seconds to complete one recipe (sec)
**Modifers**
effective_speed -> Multiplier of all effects on recipe_rate (recipes/sec)
base_speed -> Base speed of machine (recipes/sec)
flat_speed_bonus -> Additive bonuses (recipes/sec)
additive_percent_bonus -> Additive percent modifiers (%)
multiplicative_modifiers -> Multiplicative modifiers (scalar)
**Formulas**
effective_speed =
  (base_speed + flat_speed_bonus)
  × (1 + additive_percent_bonus)
  × multiplicative_modifiers
products_per_second =
  (products_per_recipe / base_crafting_time)
  × effective_speed

### Calculation Example
This example shows several parameters from different games
products_per_recipe = 2 ingots
base_crafting_time = 0.5s
base_speed = 3 recipes/sec from improved machine (DSP)
flat_speed_bonus = 2 recipes/sec from skill level (ONI)
additive_percent_bonus = 20% from speed beacon (Factorio)
multiplicative_modifiers = 3x from overclocking (Satisfactory)

effective_speed =
(3 + 2)
  × (1 + 0.2)
  × 3
= 18 recipes/sec
products_per_second =
(2 / 0.5) * 18
= 72 ingots/sec