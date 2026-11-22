// Đăng ký và kết nối các thành phần của ResourceModule.
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Resource.Application.Abstractions;
using PetSpa.Modules.Resource.Application.Services;
using PetSpa.Modules.Resource.Endpoints;
using PetSpa.Modules.Resource.Infrastructure.Persistence;
using PetSpa.Modules.Resource.Infrastructure.Persistence.Repositories;
using PetSpa.Modules.Resource.Contracts;
using PetSpa.Modules.Resource.Infrastructure.Services;

namespace PetSpa.Modules.Resource;

public static class ResourceModule
{
    // Đăng ký các dịch vụ của module danh mục dịch vụ.
    public static IServiceCollection AddResourceModule(this IServiceCollection services, string ConnectionString)
    {
        // 1. Đăng ký kết nối cơ sở dữ liệu.
        services.AddDbContext<ResourceDbContext>(options =>
        {
            options.UseNpgsql(ConnectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(ResourceDbContext).Assembly.FullName);
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "resource");
            });
        });
        // 2. Đăng ký các dịch vụ cần sử dụng.
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IServiceOptionRepository, ServiceOptionRepository>();
        services.AddScoped<CategoryService>();
        services.AddScoped<ServiceCatalogService>();
        services.AddScoped<ServiceOptionService>();
        services.AddScoped<IResourceModule, ResourceModuleContract>();
        return services;
    }

    // Đăng ký các endpoint của module danh mục dịch vụ.
    public static IEndpointRouteBuilder MapResourceModuleEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapCategoryEndpoints();
        app.MapServiceEndpoints();
        app.MapServiceOptionEndpoints();
        return app;
    }
}
