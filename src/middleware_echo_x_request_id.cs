using Microsoft.AspNetCore.Http;

namespace CustomServicesAndMiddlewares;

/// <summary>
/// <para>
/// If the client receives the servers responses out of order,
/// it can be very difficult on the client side to sort out
/// which is the most recent data to display to the user.
/// </para>
/// <para>
/// It is basically unsolvable on the client side due to some quirky aspects
/// of the javascript event loop and how it interacts with async code and the
/// browser fetch() api. The abort signal api of the fetch function is also junk
/// for this problem, it catches most issues, but there are many requests where
/// it simply cannot abort at the right time.
/// </para>
/// <para>
/// This is a particularly bad problem in PowerBI where the user will frequently
/// and rapidly adjust slicers, causing many requests to the server.
/// </para>
/// <para>
/// However on the server it is very easy to echo the x-request-id header
/// this allows the user to very easily and reliably detect out of order
/// responses and ignore old data.
/// </para>
/// <para>
/// This middleware simply echos whatever value is sent by the user
/// as long as it is a valid u64
/// </para>
/// </summary>
public class EchoXRequestIdMiddleware {
    private readonly RequestDelegate next;

    public EchoXRequestIdMiddleware(RequestDelegate next) {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context) {
       context.Response.OnStarting(() => {
            var request_id = context.Request.Headers["x-request-id"].FirstOrDefault();
            if(request_id is null || request_id==""){
                return Task.CompletedTask;
            }
            // Only echo if the value is a valid u64
            if (ulong.TryParse(request_id, out _)) {
                context.Response.Headers.Append("x-request-id", request_id);
            }
            return Task.CompletedTask;
        });
       await next(context);
    }
}