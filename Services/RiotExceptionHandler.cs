using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MyLoLTracker.Services;

public class RiotExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is not RiotApiException riot)
            return false;

        var status = riot.UpstreamStatus == HttpStatusCode.TooManyRequests
            ? StatusCodes.Status429TooManyRequests
            : StatusCodes.Status502BadGateway;

        if (riot.RetryAfter is { } retry)
            context.Response.Headers.RetryAfter = ((int)retry.TotalSeconds).ToString();

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = "Erreur de l'API Riot",
            Detail = riot.Message
        }, ct);

        return true;
    }
}
