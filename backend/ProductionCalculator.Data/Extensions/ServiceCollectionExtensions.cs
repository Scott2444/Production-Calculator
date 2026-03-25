using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Data.Repositories;

namespace ProductionCalculator.Data.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddProductionCalculatorData(this IServiceCollection services, IConfiguration configuration)
        {
            var conn = configuration.GetConnectionString("DefaultConnection") ?? configuration["ConnectionStrings:DefaultConnection"];
            services.AddDbContext<ProductionCalculatorDbContext>(opts => opts.UseNpgsql(conn));
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IAttributeRepository, AttributeRepository>();
            services.AddScoped<IRecipeRepository, RecipeRepository>();
            services.AddScoped<IRecipeProductRepository, RecipeProductRepository>();
            services.AddScoped<IRecipeAttributeRepository, RecipeAttributeRepository>();
            services.AddScoped<IMachineRepository, MachineRepository>();
            services.AddScoped<IMachineRecipeRepository, MachineRecipeRepository>();
            services.AddScoped<IMachineAttributeRepository, MachineAttributeRepository>();
            services.AddScoped<IModifierRepository, ModifierRepository>();
            services.AddScoped<IModifierAttributeRepository, ModifierAttributeRepository>();
            services.AddScoped<IWorkflowRepository, WorkflowRepository>();
            services.AddScoped<IWorkflowNodeRepository, WorkflowNodeRepository>();
            services.AddScoped<IWorkflowTargetRepository, WorkflowTargetRepository>();
            services.AddScoped<IWorkflowNodeModifierRepository, WorkflowNodeModifierRepository>();
            services.AddScoped<IWorkflowEdgeRepository, WorkflowEdgeRepository>();
            services.AddScoped<IWorkflowProductNodeRepository, WorkflowProductNodeRepository>();
            services.AddScoped<IWorkflowRecipeRepository, WorkflowRecipeRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IVerificationCodeRepository, VerificationCodeRepository>();
            return services;
        }
    }
}
