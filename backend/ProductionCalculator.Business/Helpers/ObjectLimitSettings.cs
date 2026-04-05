using Microsoft.Extensions.Configuration;

namespace ProductionCalculator.Business.Helpers
{
    public sealed class ObjectLimitSettings
    {
        private const int DefaultMaxProjectsPerUser = 5;
        private const int DefaultMaxProductsPerProject = 1000;
        private const int DefaultMaxRecipesPerProject = 1000;
        private const int DefaultMaxMachinesPerProject = 1000;
        private const int DefaultMaxModifiersPerProject = 1000;
        private const int DefaultMaxAttributesPerProject = 1000;
        private const int DefaultMaxWorkflowsPerProject = 20;

        public int MaxProjectsPerUser { get; private set; } = DefaultMaxProjectsPerUser;
        public int MaxProductsPerProject { get; private set; } = DefaultMaxProductsPerProject;
        public int MaxRecipesPerProject { get; private set; } = DefaultMaxRecipesPerProject;
        public int MaxMachinesPerProject { get; private set; } = DefaultMaxMachinesPerProject;
        public int MaxModifiersPerProject { get; private set; } = DefaultMaxModifiersPerProject;
        public int MaxAttributesPerProject { get; private set; } = DefaultMaxAttributesPerProject;
        public int MaxWorkflowsPerProject { get; private set; } = DefaultMaxWorkflowsPerProject;

        public static ObjectLimitSettings FromConfiguration(IConfiguration? configuration)
        {
            var settings = new ObjectLimitSettings();
            if (configuration == null)
            {
                return settings;
            }

            settings.MaxProjectsPerUser = ReadPositiveInt(configuration["ObjectLimits:MaxProjectsPerUser"], DefaultMaxProjectsPerUser);
            settings.MaxProductsPerProject = ReadPositiveInt(configuration["ObjectLimits:MaxProductsPerProject"], DefaultMaxProductsPerProject);
            settings.MaxRecipesPerProject = ReadPositiveInt(configuration["ObjectLimits:MaxRecipesPerProject"], DefaultMaxRecipesPerProject);
            settings.MaxMachinesPerProject = ReadPositiveInt(configuration["ObjectLimits:MaxMachinesPerProject"], DefaultMaxMachinesPerProject);
            settings.MaxModifiersPerProject = ReadPositiveInt(configuration["ObjectLimits:MaxModifiersPerProject"], DefaultMaxModifiersPerProject);
            settings.MaxAttributesPerProject = ReadPositiveInt(configuration["ObjectLimits:MaxAttributesPerProject"], DefaultMaxAttributesPerProject);
            settings.MaxWorkflowsPerProject = ReadPositiveInt(configuration["ObjectLimits:MaxWorkflowsPerProject"], DefaultMaxWorkflowsPerProject);

            return settings;
        }

        private static int ReadPositiveInt(string? rawValue, int defaultValue)
        {
            if (int.TryParse(rawValue, out var parsed) && parsed > 0)
            {
                return parsed;
            }

            return defaultValue;
        }
    }
}
