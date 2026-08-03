using System.Reflection;
using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Portfolio.Application.Common.Behaviors;

namespace Portfolio.Application;

/// <summary>
/// Provides dependency injection configuration for application-layer services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers application-layer services and dependencies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(serviceProvider =>
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.LicenseKey = configuration.GetSection("AutoMapper:LicenseKey").Value;
                cfg.AddMaps(Assembly.GetExecutingAssembly());
            }, loggerFactory);

            return mapperConfig.CreateMapper();
        });

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(ValidationBehavior<,>));

        return services;
    }
}