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

### C4 — Context & Container (Session 3 studio updated 10/11)

```mermaid
flowchart TB
    player([Player<br/>2–4 per match, desktop browser])
    dev([Developer<br/>builds levels in Blender])
 
    heist[Heist Game<br/>co-op 3D heist, server-authoritative]
 
    supabase[Supabase - external<br/>Auth + Postgres/Storage]
    cdn[CDNs: jsDelivr / cdnjs - external<br/>three.js, SignalR client, Bootstrap]
 
    player -->|plays: inputs up, snapshots down| heist
    player -.->|signs in, receives JWT| supabase
    player -->|browser loads JS libraries| cdn
    heist -.->|verifies JWT, reads/writes profiles and results| supabase
    dev -->|exports level GLB, generates level JSON| heist
 
    classDef person fill:#d4edda,stroke:#28a745,color:#155724
    classDef system fill:#a9c4e8,stroke:#4a76b8,color:#1a3a5c
    classDef external fill:#e6e6e6,stroke:#888888,color:#333333
 
    class player,dev person
    class heist system
    class supabase,cdn external
```

```mermaid
flowchart LR
    player([Player<br/>desktop browser])
    dev([Developer])
 
    subgraph heist [Heist Game]
        spa[Browser SPA<br/>JavaScript, three.js, Bootstrap<br/>render, input, prediction, interpolation]
        server[Game Server<br/>ASP.NET Core + SignalR<br/>hub + 20 Hz loop, services,<br/>domain rules, repositories]
        files[(Level and asset files<br/>levels/*.json, *.glb<br/>served from wwwroot)]
        tool[LevelTool<br/>C# console, dev-time only<br/>GLB to validated level JSON]
    end
 
    supaAuth[Supabase Auth<br/>external]
    supaDb[(Supabase Postgres + Storage<br/>external)]
    cdn[CDNs: jsDelivr / cdnjs<br/>external]
 
    player -->|uses: keyboard, screen| spa
    spa <-->|WebSocket / SignalR<br/>inputs up, snapshots down| server
    spa -->|HTTP: SPA, GLB, level JSON<br/>REST later| server
    server -->|loads and validates at startup| files
    tool -->|writes| files
    dev -->|runs| tool
    spa -->|loads libraries| cdn
    spa -.->|signs in, receives JWT| supaAuth
    server -.->|verifies JWT| supaAuth
    server -.->|reads/writes via repositories| supaDb
 
    classDef person fill:#d4edda,stroke:#28a745,color:#155724
    classDef container fill:#dae8fc,stroke:#6c8ebf,color:#1a3a5c
    classDef store fill:#b3d9f7,stroke:#3a7ebf,color:#1a3a5c
    classDef external fill:#e6e6e6,stroke:#888888,color:#333333
 
    class player,dev person
    class spa,server,tool container
    class files store
    class supaAuth,supaDb,cdn external
```

### UML — Class & Sequence (Session 3 studio)
#### Class Diagram Phase II GameLoop Relationship

```mermaid
classDiagram
    direction LR
 
    class Room {
        +string Code
        +GameState State
        +Level Level
        -Dictionary~string, Player~ _players
        -Dictionary~string, InputCommand~ _inputs
        -double _elapsedMs
        -object _gate
        +const int MaxPlayers = 4
        +Add(string id, string name) Player?
        +Remove(string id) void
        +SetInput(string id, InputCommand cmd) void
        +Step(float dt) void
        +TrapStates() bool[]
        +IsEmpty() bool
        -HitsWall(float x, float z) bool
    }
 
    class Player {
        <<record>>
        +string Id
        +string Name
        +int Number
        +float X
        +float Y
        +float Z
        +int LastSeq
    }
 
    class Level {
        +string Id
        +int FormatVersion
        +IReadOnlyList~Box~ Walls
        +IReadOnlyList~Hazard~ Hazards
        +IReadOnlyList~Spawn~ Spawns
        +Objective Objective
        +SpawnFor(int number) Spawn
    }
 
    class Hazard {
        <<record>>
        +string Name
        +float MinX
        +float MaxX
        +float MinY
        +float MaxY
        +float MinZ
        +float MaxZ
        +int SafeMs
        +int ArmedMs
        +IsArmed(double elapsedMs) bool
    }
 
    class MatchResult {
        +Guid Id
        +string RoomCode
        +string LevelId
        +GameState Outcome
        +DateTime StartedAt
        +DateTime EndedAt
        +List~Guid~ PlayerIds
        +Duration() TimeSpan
    }
 
    class GameState {
        <<enumeration>>
        Playing
        Won
        Lost
    }
 
    Room "1" *-- "0..4" Player : holds
    Room "*" --> "1" Level : played on
    Level "1" *-- "0..*" Hazard : contains
    Room --> GameState : state
    Room ..> MatchResult : creates on Won or Lost
    MatchResult --> GameState : outcome
```
#### Sequence Diagram Phase I Sign into Game

```mermaid
sequenceDiagram
    autonumber
    actor P as Player
    box Presentation - browser (client)
        participant LG as login.html
        participant M as main.js
        participant NC as NetworkClient
    end
    box Presentation - server
        participant GH as GameHub
    end
    box Service
        participant RR as RoomRegistry
    end
    box Domain
        participant R as Room
    end
 
    P->>LG: type name and room code, submit
    LG->>LG: save name and code in sessionStorage
    LG->>M: navigate to index.html
    M->>NC: register onWelcome, onPlayerJoined, onPlayerLeft, onSnapshot
    Note over M,NC: Handlers are wired before join, or one-time events are lost
    M->>NC: join(code, name)
    NC->>GH: JoinRoom(code, name) over SignalR
    GH->>RR: GetOrCreate(code)
    alt code is new, including a typo
        RR->>R: new Room(code)
        R-->>RR: room
    end
    RR-->>GH: room
    GH->>R: AddPlayer(connectionId, name)
    R->>R: next free number under the lock, spawn at X = (number-1)*2, Z = 0
    R-->>GH: Player
    GH->>RR: Track(connectionId, code)
    GH->>GH: add connection to SignalR group code
    GH-->>NC: Welcome(player, players)
    GH-->>NC: PlayerJoined(player) to others in the group
    NC-->>M: onWelcome builds the scene, starts sending input
    Note over GH,R: The room ticks from creation, so snapshots start at once
    GH-->>NC: Snapshot(state, players, traps) every 50 ms
    M-->>P: in the game
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
