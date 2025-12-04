using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using GameForum.Data;

namespace GameForum.Middleware;

public class BasicAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<BasicAuthMiddleware> _logger;

    public BasicAuthMiddleware(RequestDelegate next, ILogger<BasicAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IServiceProvider services)
    {
        await _next(context);
    }

    private static Task Challenge(HttpContext context)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        context.Response.Headers["WWW-Authenticate"] = "Basic realm=admin";
        return context.Response.WriteAsync("Unauthorized");
    }
}

