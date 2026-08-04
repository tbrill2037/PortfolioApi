using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Interfaces.Messaging;
using Portfolio.Domain.Repositories.Command.Base;
using Portfolio.Domain.Repositories.Query.Base;
using Portfolio.Infra.AWS;
using Portfolio.Infra.AWS.Configuration;
using Portfolio.Infra.Data;
using Portfolio.Infra.Data.Configuration;
using Portfolio.Infra.Email;
using Portfolio.Infra.Identity;
using Portfolio.Infra.Repository.Command.Base;
using Portfolio.Infra.Repository.Email;
using Portfolio.Infra.Repository.Query.Base;
using Portfolio.Infra.Services;
using Portfolio.Infra.Services.Messaging;
using System.Text;

namespace Portfolio.Infra;

/// <summary>
/// Provides dependency injection registrations for infrastructure services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));

        services.AddSingleton<SqlConnectionStringFactory>();

        services.AddDbContext<ApplicationDbContext>(
            (serviceProvider, options) =>
            {
                var connectionFactory = serviceProvider
                    .GetRequiredService<SqlConnectionStringFactory>();

                options.UseSqlServer(
                    connectionFactory.Create(),
                    sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(
                            typeof(ApplicationDbContext).Assembly.FullName);

                        sqlOptions.EnableRetryOnFailure();
                    });
            });

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IQueryRepository<>), typeof(QueryRepository<>));
        services.AddScoped(typeof(ICommandRepository<>), typeof(CommandRepository<>));
        services.AddScoped<IEmailRepository, EmailRepository>();
        services.AddScoped<IEmailCooldownRepository, EmailCooldownRepository>();
        services.AddScoped<IEmailEventRepository, EmailEventRepository>();
        services.AddScoped<IEmailOutboxRepository, EmailOutboxRepository>();
        services.AddScoped<IEmailDeadLetterRepository, EmailDeadLetterRepository>();

        return services;
    }

    public static IServiceCollection AddApiCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins =
            configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>()
            ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", builder =>
            {
                builder
                    .WithOrigins(allowedOrigins)
                    .WithMethods("POST")
                    .WithHeaders("Content-Type");
            });
        });

        return services;
    }

    public static IServiceCollection AddIdentity(this IServiceCollection services)
    {
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        services.Configure<IdentityOptions>(options =>
        {
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 6;
            options.Password.RequiredUniqueChars = 1;

            options.SignIn.RequireConfirmedEmail = false;
            options.SignIn.RequireConfirmedPhoneNumber = false;

            options.User.RequireUniqueEmail = true;
        });

        services.AddScoped<IIdentityService, IdentityService>();

        return services;
    }

    public static IServiceCollection AddJwt(this IServiceCollection services, IConfiguration configuration)
    {
        var key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt key is missing.");
        var issuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt issuer is missing.");
        var audience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt audience is missing.");
        var expiryMinutes = int.Parse(configuration["Jwt:ExpiryMinutes"] ?? "60");

        services.AddSingleton<ITokenGenerator>(new TokenGenerator(key, issuer, audience, expiryMinutes));

        services.AddAuthentication(x =>
        {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(x =>
        {
            x.RequireHttpsMetadata = false;
            x.SaveToken = true;
            x.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidAudience = audience,
                ValidIssuer = issuer,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                ClockSkew = TimeSpan.FromMinutes(expiryMinutes)
            };
        });

        return services;
    }

    public static IServiceCollection AddApiInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDatabase(configuration);
        services.AddRepositories();
        services.AddIdentity();
        services.AddJwt(configuration);
        services.AddAws(configuration);
        services.AddEmail(configuration);
        services.AddHttpContextAccessor();
        services.AddCommonInfrastructure(configuration);

        services.AddScoped<IClientIpService, ClientIpService>();

        return services;
    }

    public static IServiceCollection AddEmailWorkerInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDatabase(configuration);
        services.AddRepositories();
        services.AddAws(configuration);
        services.AddEmail(configuration);
        services.AddCommonInfrastructure(configuration);

        return services;
    }

    public static IServiceCollection AddCommonInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EnvironmentOptions>(configuration.GetSection(EnvironmentOptions.SectionName));
        services.AddSingleton<ICorrelationIdGenerator, CorrelationIdGenerator>();

        return services;
    }
}