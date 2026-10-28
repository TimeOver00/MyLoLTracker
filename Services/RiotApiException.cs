using System.Net;

namespace MyLoLTracker.Services;

public class RiotApiException(HttpStatusCode upstreamStatus, string message, TimeSpan? retryAfter = null)
    : Exception(message)
{
    public HttpStatusCode UpstreamStatus { get; } = upstreamStatus;
    public TimeSpan? RetryAfter { get; } = retryAfter;

    public static RiotApiException From(HttpResponseMessage response)
    {
        var message = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                "Clé API Riot invalide ou expirée. Régénère-la sur developer.riotgames.com.",
            HttpStatusCode.TooManyRequests =>
                "Limite de requêtes Riot atteinte. Réessaie dans quelques secondes.",
            _ => $"Erreur inattendue de l'API Riot ({(int)response.StatusCode})."
        };

        return new RiotApiException(response.StatusCode, message, response.Headers.RetryAfter?.Delta);
    }
}
