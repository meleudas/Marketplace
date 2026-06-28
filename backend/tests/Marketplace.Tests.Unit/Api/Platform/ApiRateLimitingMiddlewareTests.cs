using System.Text;
using System.Text.Json;
using Marketplace.API.Middleware;
using Marketplace.Application.Common.RateLimiting;
using Marketplace.Infrastructure.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

[Trait("Suite", "Platform")]
public sealed class ApiRateLimitingMiddlewareTests
{
    [Fact]
    public async Task Auth_Login_Returns_429_When_Ip_Limit_Exceeded()
    {
        var store = new MemoryRateLimitCounterStore(new MemoryCache(new MemoryCacheOptions()));
        var options = Options.Create(new RateLimitingOptions
        {
            Enabled = true,
            Auth = new RateLimitPolicyOptions { PermitLimit = 2, WindowSeconds = 60 }
        });
        var middleware = new RateLimitingMiddleware(
            _ => Task.CompletedTask,
            store,
            options,
            NullLogger<RateLimitingMiddleware>.Instance);

        for (var i = 0; i < 2; i++)
        {
            var okContext = CreateContext("/auth/login", """{"email":"a@test.com","password":"x"}""");
            await middleware.InvokeAsync(okContext);
            Assert.NotEqual(StatusCodes.Status429TooManyRequests, okContext.Response.StatusCode);
        }

        var blockedContext = CreateContext("/auth/login", """{"email":"a@test.com","password":"x"}""");
        await middleware.InvokeAsync(blockedContext);

        Assert.Equal(StatusCodes.Status429TooManyRequests, blockedContext.Response.StatusCode);
        Assert.True(blockedContext.Response.Headers.ContainsKey("Retry-After"));
    }

    private static DefaultHttpContext CreateContext(string path, string body)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = path;
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Request.ContentType = "application/json";
        context.Response.Body = new MemoryStream();
        return context;
    }
}
