# MyLoLTracker 🎮

A full-stack app to look up a League of Legends player's profile, ranked stats, and recent match history, built on top of the Riot Games API.

## Features

- Look up a player by Riot ID (game name + tag)
- View summoner level and profile icon
- View ranked Solo/Duo and Flex stats (tier, rank, LP, win rate)
- View recent match history

## Tech stack

- **Backend**: ASP.NET Core 8 (minimal API), consuming the Riot Games API
- **Frontend**: Angular
- **Tests**: xUnit
- **CI/CD**: GitHub Actions (build, test, Docker image)
- **Containerization**: Docker

## Project structure

```
MyLoLTracker/
├── Models/       # Riot API response models
├── Services/     # Riot API client and error handling
├── frontend/     # Angular app
├── tests/        # xUnit test project
└── Program.cs    # API endpoints
```

## Getting started

### Backend

Requires a [Riot Games API key](https://developer.riotgames.com/).

```bash
dotnet user-secrets set "Riot:ApiKey" "your_api_key_here"
dotnet run
```

The API is now available at `https://localhost:5001` (or similar; check the console output), with Swagger UI at `/swagger` in development.

### Frontend

```bash
cd frontend
npm install
npm start
```

The app is now available at `http://localhost:4200`.

### Tests

```bash
dotnet test tests/MyLoLTracker.Tests
```

### Docker

```bash
docker build -t mylol-tracker .
docker run -p 8080:8080 -e Riot__ApiKey=your_api_key_here mylol-tracker
```

## API

- `GET /summoner/{gameName}/{tagLine}` — profile (level, icon)
- `GET /summoner/{gameName}/{tagLine}/ranked` — ranked stats per queue
- `GET /summoner/{gameName}/{tagLine}/matches?count={count}` — recent matches (1-20, default 5)
