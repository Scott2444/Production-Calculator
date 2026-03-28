using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Business.Services
{
    public class MachineCalculator : IMachineCalculator
    {
        
        public MachineCalculator()
        {
        }

        /// <summary>
        /// Calculates the required machine count to achieve the target recipe rate, given the recipe, machine, and applicable modifiers.
        /// </summary>
        /// <param name="rate">Recipe rate (recipes/s)</param>
        /// <param name="recipe"></param>
        /// <param name="machine"></param>
        /// <param name="modifiers"></param>
        /// <returns>Number of machines to achieve the recipe rate</returns>
        /// <exception>ArgumentException if effective speed is zero or negative</exception>
        public double CalculateMachineCount(double rate, Recipe recipe, Machine machine, List<Modifier> modifiers)
        {
            // recipes_per_second_per_machine = effective_speed / base_crafting_time<br>
            // machine_count = recipe_rate / recipes_per_second_per_machine

            var effective_speed = CalculateEffectiveSpeed(machine, modifiers);
            if (effective_speed <= 0)
            {
                throw new ArgumentException("Effective speed must be greater than zero.");
            }

            var recipes_per_second_per_machine = effective_speed / recipe.Base_Crafting_Time;
            var machine_count = rate / recipes_per_second_per_machine;
            return machine_count;
        }


        public double CalculateRecipeRate(double numMachines, Recipe recipe, Machine machine, List<Modifier> modifiers)
        {
            // recipes_per_second_per_machine = effective_speed / base_crafting_time<br>
            // recipe_rate = machine_count × recipes_per_second_per_machine
            var effective_speed = CalculateEffectiveSpeed(machine, modifiers);
            if (effective_speed <= 0)
            {
                throw new ArgumentException("Effective speed must be greater than zero.");
            }
            var recipes_per_second_per_machine = effective_speed / recipe.Base_Crafting_Time;
            return numMachines * recipes_per_second_per_machine;
        }

        private double CalculateEffectiveSpeed(Machine machine, List<Modifier> modifiers)
        {
            // effective_speed =
            // (base_speed + flat_speed_bonus)
            // × (1 + additive_percent_bonus)
            // × multiplicative_modifiers
            var baseSpeed = machine.Base_Speed;

            // Apply modifiers
            double flatSpeedBonus = 0.0;
            double additivePercentBonus = 0.0;
            double multiplicativeModifier = 1.0;
            foreach (var modifier in modifiers)
            {
                flatSpeedBonus += modifier.Flat_Bonus;
                additivePercentBonus += modifier.Percent_Bonus;
                multiplicativeModifier *= modifier.Multiplicative_Bonus;
            }

            // Calculate effective speed
            var effective_speed = (baseSpeed + flatSpeedBonus) * (1.0 + additivePercentBonus) * multiplicativeModifier;
            return effective_speed;
        }
    }
}
