# [Heist]

<!-- CI badge: after Session 4, replace ORG/REPO and the workflow filename, then uncomment:
![CI](https://github.com/ORG/REPO/actions/workflows/ci.yml/badge.svg)
-->

**Student:** [Enzo Swayne] · **Course:** CEN 5064 Software Design, Fall 2026 · **Partner:** [@JosephGabrie]

## Project (approval paragraph — write this by Sun Aug 30)

I propose developing a small 3D multiplayer heist game that runs in a web browser. This application allows a group to connect to a shared game session,
interact with a 3D environment, coordinate, complete objectives, and escape from the place. The technology used will be Three.js/Javascript on the
client, using ASP.NET Core/C# for the server, SignalR for real-time communication, and Supabase/PostgreSQL for persistence and authentication. The 
main focus is software architecture and design patterns, not gameplay or art. The architecture will be layered with a separation between presentation,
application, domain, and data/infrastructure. The design principles will be SOLID, dependency inversion, and the use of design patterns such as SignalR behind a 
transport/session interface. The game will be small enough to finish, with a focus on use cases such as joining sessions, synched movements, interaction, and
objective completion. Testing will be demonstrable with local tests and multiplayer through at least two browser tabs or simulated clients. My personal 
motivations include a desire to strengthen my Three.js and 3D web dev skills, working with a familiar tech stack, and creating a fun multiplayer experience.

## How to run
### PART 1
For now, if you decide you want to run what I have so far, clone or download zip and unzip. Open command prompt:
```
CD unzipped Directory
CD /Heist
dotnet run
```
Open two browser windows:
First Browser window: go to localhost:7159/
You should now be in login page, login with random name
you should see a spawned box.
press F12 to open Dev Tools, click "console" tab
Here you can see object properties and welcome message for success.

Second browser: go to localhost:7159/
Login with another name
now both windows should have two boxes spawned
in first browser you should see that another "player" has joined" and see their object properties
If having trouble, please see me so I can train you on how to use cmd, dev tools, etc.

```
[Exact commands to build and run your system from a clean clone.
Update this every time the steps change — your partner and your
instructor will follow it literally on conference days.]
```

## Architecture

### Tier breakdown (Session 2 studio updated 10/11)

| Tier | Responsibilities in THIS system | Example Classes/Modules |
|------|--------------------------------|-------------------------|
| Presentation | <ul><li>**Browser:** renders the 3D scene and GLB art</li><li>**Browser:** turns WASD into a movement intent</li><li>**Browser:** predicts the local player, interpolates remote players</li><li>**Browser:** shows lobby, waiting room, win/lose banners; fetches level JSON</li><li>**Server:** `GameHub` receives calls/inputs, pushes snapshots to the room's group</li><li>**Server:** `GameLoop` drives the 20 Hz tick; middleware validates the Supabase JWT</li></ul> | <ul><li>`SceneManager.js`</li><li>`InputController.js`</li><li>`NetworkClient.js`</li><li>`LevelLoader.js`</li><li>`LobbyView.js`</li><li>`GameHub.cs`</li><li>`GameLoop.cs`</li><li>`Program.cs` (composition root)</li></ul> |
| Service | <ul><li>Create, join, start and leave rooms; route inputs</li><li>Tick every in-progress room and return snapshots (no transport knowledge)</li><li>Keep the directory of live rooms, with join and teardown serialized</li><li>Load and validate levels; list the playable ones</li><li>Provision a profile on first sign-in</li><li>Record finished matches; read history and best times</li></ul> | <ul><li>`IMatchSession` → `MatchSessionService`</li><li>`RoomRegistry`</li><li>`LevelCatalogService.ListPlayable()`</li><li>`AuthService.EnsureProfile()`</li><li>`MatchResultService.RecordResult()`</li><li>`StatisticsService.GetBestTimes()`</li></ul> |
| Domain | <ul><li>`Room.Step`: freeze → trap clock → movement → hazard check → win check</li><li>Movement: circle-vs-AABB collide-and-slide</li><li>Touching an armed trap → **Lost**; reaching the objective → **Won** (escape later)</li><li>Entities and repository contracts; no framework code</li></ul> | <ul><li>`Room`, `Level`, `Player`</li><li>`MovementRules`</li><li>`TrapResolver`</li><li>`CompletionPolicy`</li><li>`ILevelRepo`</li><li>`IMatchResultRepo`</li><li>`IPlayerProfileRepo`</li></ul> |
| Data | <ul><li>Level files (`levels/*.json` now, Supabase Storage later)</li><li>Match results table (level, players, outcome, time) + history and best-time queries</li><li>Player profiles</li><li>Only tier that references the Supabase SDK</li></ul> | <ul><li>`JsonLevelRepo`</li><li>`MatchResultRepo`</li><li>`PlayerProfileRepo`</li></ul> |

### C4 — Context & Container (Session 3 studio)

```mermaid
flowchart TB
    user([Player, 2-4, web browser]) -->|plays, sends token| system[Heist]
    system-->|read/write|db[(Supabase Auth - External)]
    user-->|logs in via|db[(Supabase Auth - External)]
    user-->|loads libraries from| dbJS[(CDNs/JsDeliver - External)]
```

```mermaid
flowchart LR
    player([Player, 2-4, web browser]) -->|WebSocket/SignalR<br/>inputs up, snapshots down| spa
    player -->|HTTP<br/>static files now, REST later| spa

    subgraph HeistSystem [Heist Game - Your System]
        spa[Browser SPA<br/>Presentation]
        spa -->|WebSocket calls| gameserver
        spa -->|HTTP calls| gameserver
        gameserver[Game Server<br/>Service + Domain<br/>ASP.NET monolith]
    end

    spa -->|logs in via| db[(Supabase Auth - External)]
    gameserver -->|read/write, planned| db
    spa -->|loads libraries from| dbJS[(CDNs/jsDelivr - External)]

    classDef person fill:#d4edda,stroke:#28a745,color:#155724
    classDef presentation fill:#dae8fc,stroke:#6c8ebf,color:#1a3a5c
    classDef appdomain fill:#a9c4e8,stroke:#4a76b8,color:#1a3a5c
    classDef data fill:#b3d9f7,stroke:#3a7ebf,color:#1a3a5c

    class player person
    class spa presentation
    class gameserver appdomain
    class db,dbJS data
```

### UML — Class & Sequence (Session 3 studio)

```mermaid
classDiagram 
    class Player {
        -GUID Id
        -string Name
        -int Number
        -float X
        -float Y
        -float Z
        -int LastSeq
    }

    class Match {
        -GUID Id
        -DateTime StartedAt
        -DateTime? EndedAt
        -List~GUID~ PlayerIds
        +IsComplete() bool
        +Duration() TimeSpan
    }

    class Level {
        <<static>>
        +PlayerRadius : float
        +Walls : List~Wall~
    }

    class Wall {
        <<record>>
        +MinX : float
        +MaxX : float
        +MinZ : float
        +MaxZ : float
    }

    class PlayerProfile {
        -GUID UserId
        -string DisplayName
        -int MatchesPlayed
        -int MatchesWon
        +WinRate() float
    }

    Match "1" --> "2..4" Player : consists of
    Match "1" --> "1" Level : played on
    Level "1" --> "*" Wall : contains
    PlayerProfile "1" --> "0..*" Match : history
```

```mermaid
sequenceDiagram
    actor P as Player
    participant UI
    participant MS as MatchService
    participant R as MatchResultRepo

    P->>UI: trigger success event
    UI->>MS: submitResult(matchId, outcome)
    MS->>MS: check completion rules   
    MS->>R: save(matchResult)
    R-->>MS: matchResult id
    MS-->>UI: response
    UI-->>P: confirmation
```

## Architecture Decision Records

Decisions live in [`docs/adr/`](docs/adr/). Start with ADR-001 in Session 4.

| # | Decision | Status |
|---|----------|--------|
| [001](docs/adr/adr-001.md) | [What I am building and why] | [proposed] |

## Weekly log (optional but recommended)

A one-line note per week keeps your commit story readable:

- Week 1 (Aug 24): repo created, three ideas drafted
- Week 2 (Aug 31): ...


# Working on Test Ping
Will Create Test ping 
