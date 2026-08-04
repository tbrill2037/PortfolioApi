using Portfolio.Api;
using Portfolio.Api.Middleware;
using Portfolio.Application;
using Portfolio.Application.Common.Logging;
using Portfolio.Infra;
using Portfolio.Infra.AWS.Configuration;
using Portfolio.Infra.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    // Load secrets from AWS Secrets Manager into the configuration.
    await SecretInitializer.LoadAsync(
        builder.Configuration,
        builder.Configuration[
            "AWS:Secrets:SecretName"]);

    await SecretInitializer.LoadAsync(
        builder.Configuration,
        builder.Configuration[
            "AWS:Secrets:DatabaseSecretName"]);

    DatabaseSecretInitializer.Load(
        builder.Configuration);
}

// ASP.NET Core
builder.Services.ConfigureForwardedHeaders();
builder.Services.AddApiControllers();
builder.Services.AddHealthChecks();

builder.Services.AddOpenAPIServices(
    builder.Configuration,
    builder.Environment);

builder.Services.AddAWSLambdaHosting(
    LambdaEventSource.HttpApi);

// Cross-cutting
builder.Services.AddApiCors(
    builder.Configuration);

builder.Services.AddApiRateLimiting();

builder.Services.AddStructuredLogging(
    builder.Configuration,
    WorkerNames.API);

// Application
builder.Services.AddApplicationServices(
    builder.Configuration);

builder.Services.AddApiInfrastructure(
    builder.Configuration);

var app = builder.Build();

// Centralized exception handling.
app.UseMiddleware<ApiExceptionMiddleware>();

app.UseForwardedHeaders();
app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("CorsPolicy");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseApiDocumentationRobots();

app.MapHealthChecks("/health");
app.MapControllers();

app.MapOpenApi();

app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Portfolio API")
        .WithTheme(ScalarTheme.DeepSpace)
        .WithDefaultHttpClient(
            ScalarTarget.CSharp,
            ScalarClient.HttpClient);
});

app.Run();