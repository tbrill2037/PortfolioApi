namespace Portfolio.Api.Middleware;

public static class RobotsMiddlewareExtensions
{
    public static IApplicationBuilder UseApiDocumentationRobots(
        this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/scalar") ||
                context.Request.Path.StartsWithSegments("/openapi"))
            {
                context.Response.Headers.Append(
                    "X-Robots-Tag",
                    "noindex, nofollow, noarchive");
            }

            await next();
        });
    }
}