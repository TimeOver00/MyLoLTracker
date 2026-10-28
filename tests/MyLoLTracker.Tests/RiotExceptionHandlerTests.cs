using System.Net;
using Microsoft.AspNetCore.Http;
using MyLoLTracker.Services;

namespace MyLoLTracker.Tests;

public class RiotExceptionHandlerTests
{
    private static async Task<(DefaultHttpContext Context, string Body, bool Handled)> RunAsync(Exception exception)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var handled = await new RiotExceptionHandler().TryHandleAsync(context, exception, CancellationToken.None);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        return (context, body, handled);
    }

    [Fact]
    public async Task Returns429_WithRetryAfterHeader_WhenRateLimited()
    {
        var ex = new RiotApiException(HttpStatusCode.TooManyRequests, "Limite atteinte", TimeSpan.FromSeconds(7));

        var (context, body, handled) = await RunAsync(ex);

        Assert.True(handled);
        Assert.Equal(429, context.Response.StatusCode);
        Assert.Equal("7", context.Response.Headers.RetryAfter.ToString());
        Assert.Contains("Limite atteinte", body);
    }

    [Fact]
    public async Task Returns502_WhenApiKeyIsRejected()
    {
        var ex = new RiotApiException(HttpStatusCode.Forbidden, "Clé API Riot invalide ou expirée.");

        var (context, _, handled) = await RunAsync(ex);

        Assert.True(handled);
        Assert.Equal(502, context.Response.StatusCode);
    }

    [Fact]
    public async Task IgnoresOtherExceptions()
    {
        var (_, _, handled) = await RunAsync(new InvalidOperationException("autre chose"));

        Assert.False(handled);
    }
}
