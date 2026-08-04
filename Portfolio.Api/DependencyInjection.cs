using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi;
using System.Text.Json;
using System.Threading.RateLimiting;

namespace Portfolio.Api;

public static class DependencyInjection
{
    public static IServiceCollection ConfigureForwardedHeaders(this IServiceCollection services)
    {
        // Configures support for reverse proxies and load balancers to preserve original request metadata.
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor |
            ForwardedHeaders.XForwardedProto;
        });

        return services;
    }

    public static IServiceCollection AddApiControllers(this IServiceCollection services)
    {
        // Registers API controllers with consistent JSON serialization behavior across endpoints.
        services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                });

        return services;
    }

    public static IServiceCollection AddOpenAPIServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer(
                (document, context, cancellationToken) =>
                {
                    document.Info.Title = configuration["AWS:SES:FromName"] + " Portfolio API";
                    document.Info.Version = "v1";
                    document.Info.Description = "Public API supporting the " + configuration["AWS:SES:FromName"] + " developer portfolio.";

                    var serverUrl = configuration["OpenApi:ServerUrl"];

                    if (!string.IsNullOrWhiteSpace(serverUrl))
                    {
                        document.Servers =
                        [
                            new OpenApiServer
                            {
                                Url = serverUrl,
                                Description = environment.IsDevelopment()
                                    ? "Local development server"
                                    : "Production server",
                            },
                        ];
                    }

                    return Task.CompletedTask;
                });
        });

        return services;
    }

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode =
                StatusCodes.Status429TooManyRequests;


            options.AddPolicy(
                "EmailRequestPolicy",
                httpContext =>
                {
                    var key =
                        httpContext.Connection.RemoteIpAddress?.ToString()
                        ?? "anonymous";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        key,
                        _ =>
                            new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 3,
                                Window = TimeSpan.FromMinutes(5),
                                QueueLimit = 0,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                            });
                });


            options.AddPolicy(
                "AuthPolicy",
                httpContext =>
                {
                    var key =
                        httpContext.Connection.RemoteIpAddress?.ToString()
                        ?? "anonymous";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        key,
                        _ =>
                            new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 5,
                                Window = TimeSpan.FromMinutes(1),
                                QueueLimit = 0,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                            });
                });


            options.AddPolicy(
                "ManagementPolicy",
                httpContext =>
                {
                    var key =
                        httpContext.Connection.RemoteIpAddress?.ToString()
                        ?? "anonymous";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        key,
                        _ =>
                            new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 30,
                                Window = TimeSpan.FromMinutes(1),
                                QueueLimit = 5,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                            });
                });
        });

        return services;
    }
}