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
            services.AddScoped<IItemRepository, ItemRepository>();
            return services;
        }
    }
}
