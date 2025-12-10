using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using MovieDatabase.Api.Core.Jwt;
using MovieDatabase.Api.Core.Services;

namespace MovieDatabase.Api.Core;

public static class CoreExtensions
{
    public static IServiceCollection AddCoreDefaults(this IServiceCollection services, IConfigurationManager configuration)
        => services.RegisterServiceDefaults(configuration);

    private static IServiceCollection RegisterServiceDefaults(this IServiceCollection services, IConfigurationManager configuration)
    {
        services.Configure<JwtSettings>(options => configuration.GetSection("Jwt").Bind(options));
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IBlobService, BlobService>();

        return services;
    }
}