using Microsoft.AspNetCore.Http;

namespace CustomServices;
/* This server is set to maximum permissiveness; it will simply echo back Allow to what ever the client requests.
** Unlike the built in built in CORS function it will send `Access-Control-Allow-Origin: null` if the client origin was null.
**
** I already tried the following, and it did not work
**
** ```C#
** ...
** builder.Services.AddCors();
** var app = builder.Build();
** app.UseCors(policy => policy
**     .AllowAnyOrigin()
**     .AllowAnyHeader()
**     .AllowAnyMethod()
**     .WithExposedHeaders("*")
**     .SetPreflightMaxAge(TimeSpan.FromMinutes(60))
** );
** ```
**
** Furthermore; adding `.A
*/
public class ServicePermissiveCORS {
    private readonly RequestDelegate _next;

    public ServicePermissiveCORS(RequestDelegate next) {
        _next = next;
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

        await _next(context);
    }

    private bool IsAllowedOrigin(string origin) {
        var allowedOrigins = new[] { "http://example.com", "https://another.example.com", "null" };
        return allowedOrigins.Contains(origin);
    }
}
