using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MyLoLTracker.Services;

namespace MyLoLTracker.Tests;

public class RiotServiceTests
{
    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(request.RequestUri!.AbsoluteUri);
            return Task.FromResult(respond(request));
        }
    }

    private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static RiotService CreateService(FakeHandler handler)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Riot:ApiKey"] = "RGAPI-test" })
            .Build();
        return new RiotService(new HttpClient(handler), config, new MemoryCache(new MemoryCacheOptions()));
    }

    [Fact]
    public async Task GetAccountAsync_ReturnsAccount_WhenRiotAnswers200()
    {
        var handler = new FakeHandler(_ => Json("""{"puuid":"abc","gameName":"Ahmed","tagLine":"EUW"}"""));
        var service = CreateService(handler);

        var account = await service.GetAccountAsync("Ahmed", "EUW");

        Assert.NotNull(account);
        Assert.Equal("abc", account.Puuid);
    }

    [Fact]
    public async Task GetAccountAsync_ReturnsNull_WhenRiotAnswers404()
    {
        var service = CreateService(new FakeHandler(_ => Json("{}", HttpStatusCode.NotFound)));

        Assert.Null(await service.GetAccountAsync("Inconnu", "EUW"));
    }

    [Fact]
    public async Task GetAccountAsync_EscapesSpecialCharactersInUrl()
    {
        var handler = new FakeHandler(_ => Json("""{"puuid":"abc","gameName":"Le Joueur","tagLine":"EUW"}"""));
        var service = CreateService(handler);

        await service.GetAccountAsync("Le Joueur", "EUW");

        Assert.Contains("Le%20Joueur", handler.Requests.Single());
    }

    [Fact]
    public async Task GetAccountAsync_UsesCache_OnSecondCall()
    {
        var handler = new FakeHandler(_ => Json("""{"puuid":"abc","gameName":"Ahmed","tagLine":"EUW"}"""));
        var service = CreateService(handler);

        await service.GetAccountAsync("Ahmed", "EUW");
        await service.GetAccountAsync("ahmed", "euw");

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task GetRankedAsync_ReturnsEmptyList_WhenRiotAnswers404()
    {
        var service = CreateService(new FakeHandler(_ => Json("{}", HttpStatusCode.NotFound)));

        Assert.Empty(await service.GetRankedAsync("abc"));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task GetAccountAsync_Throws_WhenApiKeyIsRejected(HttpStatusCode status)
    {
        var service = CreateService(new FakeHandler(_ => Json("{}", status)));

        var ex = await Assert.ThrowsAsync<RiotApiException>(() => service.GetAccountAsync("Ahmed", "EUW"));

        Assert.Contains("expirée", ex.Message);
    }

    [Fact]
    public async Task GetAccountAsync_Throws_WithRetryAfter_WhenRateLimited()
    {
        var service = CreateService(new FakeHandler(_ =>
        {
            var response = Json("{}", HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(7));
            return response;
        }));

        var ex = await Assert.ThrowsAsync<RiotApiException>(() => service.GetAccountAsync("Ahmed", "EUW"));

        Assert.Equal(HttpStatusCode.TooManyRequests, ex.UpstreamStatus);
        Assert.Equal(TimeSpan.FromSeconds(7), ex.RetryAfter);
    }

    [Fact]
    public async Task GetRecentMatchesAsync_BuildsSummaryForTheRequestedPlayer()
    {
        var handler = new FakeHandler(request =>
        {
            var url = request.RequestUri!.ToString();
            if (url.Contains("/ids"))
                return Json("""["EUW1_1"]""");

            return Json("""
                {"info":{"gameDuration":1800,"gameMode":"CLASSIC","participants":[
                  {"puuid":"other","championName":"Ahri","kills":1,"deaths":1,"assists":1,"win":false},
                  {"puuid":"abc","championName":"Jinx","kills":10,"deaths":2,"assists":7,"win":true}
                ]}}
                """);
        });
        var service = CreateService(handler);

        var matches = await service.GetRecentMatchesAsync("abc", 1);

        var match = Assert.Single(matches);
        Assert.Equal("Jinx", match.Champion);
        Assert.Equal("10/2/7", match.Kda);
        Assert.True(match.Win);
    }
}
