using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Infrastructure.DI;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("Set ConnectionStrings:DefaultConnection in Development config or user secrets.");
        services.AddDbContextFactory<AppDbContext>(options => options.UseNpgsql(connection));
        services.AddScoped<CatalogService>();
        services.AddScoped<OrderService>();
        return services;
    }
}
