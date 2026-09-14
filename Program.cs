using MyLoLTracker.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient<RiotService>();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/hello", () => "Bonjour depuis MyLoLTracker !");
app.MapGet("/version", () => new { version = "1.0.0", app = "MyLoLTracker" });
app.MapGet("/summoner/{gameName}/{tagLine}", async (string gameName, string tagLine, RiotService riot) =>
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
app.Run();
