using MyLoLTracker.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200"];
builder.Services.AddCors(options => options.AddPolicy("frontend", policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders("Retry-After")));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<RiotExceptionHandler>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<IRiotService, RiotService>();
var app = builder.Build();

app.UseCors("frontend");
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/hello", () => "Bonjour depuis MyLoLTracker !");
app.MapGet("/version", () => new { version = "1.0.0", app = "MyLoLTracker" });
app.MapGet("/summoner/{gameName}/{tagLine}", async (string gameName, string tagLine, IRiotService riot) =>
{
    var account = await riot.GetAccountAsync(gameName, tagLine);
    if (account is null)
        return Results.NotFound("Joueur introuvable");

    var summoner = await riot.GetSummonerAsync(account.Puuid);
    if (summoner is null)
        return Results.NotFound("Profil introuvable");

    return Results.Ok(new
    {
        account.GameName,
        account.TagLine,
        summoner.SummonerLevel,
        summoner.ProfileIconId
    });
});

app.MapGet("/summoner/{gameName}/{tagLine}/ranked", async (string gameName, string tagLine, IRiotService riot) =>
{
    var account = await riot.GetAccountAsync(gameName, tagLine);
    if (account is null)
        return Results.NotFound("Joueur introuvable");

    var entries = await riot.GetRankedAsync(account.Puuid);
    return Results.Ok(entries.Select(e => new
    {
        e.QueueType,
        e.Tier,
        e.Rank,
        e.LeaguePoints,
        e.Wins,
        e.Losses,
        WinRate = Math.Round(100.0 * e.Wins / Math.Max(1, e.Wins + e.Losses), 1)
    }));
});

app.MapGet("/summoner/{gameName}/{tagLine}/matches", async (string gameName, string tagLine, IRiotService riot, int count = 5) =>
{
    if (count is < 1 or > 20)
        return Results.BadRequest("count doit être entre 1 et 20");

    var account = await riot.GetAccountAsync(gameName, tagLine);
    if (account is null)
        return Results.NotFound("Joueur introuvable");

    return Results.Ok(await riot.GetRecentMatchesAsync(account.Puuid, count));
});

app.Run();
