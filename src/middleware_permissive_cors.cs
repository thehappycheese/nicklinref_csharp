using Microsoft.AspNetCore.Http;

namespace CustomServicesAndMiddlewares;
/// <summary>
/// <para>
/// This server is set to maximum permissiveness; it will simply echo back Allow to what ever the client requests.
/// Unlike the built in built in CORS function it will send `Access-Control-Allow-Origin: null` if the client origin was null.
/// </para>
/// <example>
/// I already tried the following, and it did not work
/// <code>
///     ...
///     builder.Services.AddCors();
///     var app = builder.Build();
///     app.UseCors(policy => policy
///         .AllowAnyOrigin()
///         .AllowAnyHeader()
///         .AllowAnyMethod()
///         .WithExposedHeaders("*")
///         .SetPreflightMaxAge(TimeSpan.FromMinutes(60))
///     );
/// </code>
/// </example>
/// </summary>
public class PermissiveCORSMiddleware {
    private readonly RequestDelegate next;

    public PermissiveCORSMiddleware(RequestDelegate next) {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context) {
        context.Response.OnStarting(() => {
            
            

            var origin = context.Request.Headers["Origin"].FirstOrDefault();
            if(origin is null || origin==""){
                origin="*";
            }
            var allowed_headers = context.Request.Headers["Access-Control-Request-Headers"].FirstOrDefault();
            if(allowed_headers is null || allowed_headers==""){
                allowed_headers = "*";
            }
            context.Response.Headers.Append("Access-Control-Allow-Origin", origin); // super permissive
            context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            context.Response.Headers.Append("Access-Control-Allow-Headers", allowed_headers);
            context.Response.Headers.Append("Access-Control-Expose-Headers", "*");
            context.Response.Headers.Append("Access-Control-Max-Age", "3600"); // 1 hour

            if (context.Request.Method == HttpMethod.Options.Method) {
                context.Response.StatusCode = StatusCodes.Status204NoContent;
            }

            return Task.CompletedTask;
        });

        await next(context);
    }
}
