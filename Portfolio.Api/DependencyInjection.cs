using Microsoft.AspNetCore.HttpOverrides;
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