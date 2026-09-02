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

```
[Exact commands to build and run your system from a clean clone.
Update this every time the steps change — your partner and your
instructor will follow it literally on conference days.]
```

## Architecture

### Tier breakdown (Session 2 studio)

| Tier | Responsibilities in THIS system | Example Classes/Modules |
|------|--------------------------------|------------|
| Presentation | <ul><li>Displays interactable menu (dialog.js)<li> renders the scene<li> loads models and graphics |<ul><li> SceneManager.js<li> ModelLoader.js<li> Dialog.js<li> LevelLoader.js |
| Service | <ul><li>Sign-in and Out with validation tokens signed by Supabase<li> load player profile and player level layouts<li> match making   | <ul><li>AuthService.EnsureProfile<li> AuthService.SignOut<li> PlayerProfileService.GetPlayableLevels<li> PlayerProfileService.GetLevelParams()<li> MatchResultService.RecordResult()   |
| Domain | <ul><li>trigger a trap by doing A<li> complete the objective by interacting with object B and escaping through plane C<li> the collision rules for the capsules of the players to determine touch and interaction<li> interface that contracts the storing and getting match results <li> interface that stores and retrieves player profiles  | <ul><li>MovementRules.cs<li> CompletionPolicy.cs<li> TrapResolver.cs<li> IMatchResultRepo.cs<li> IPlayerProfileRepo.cs |
| Data | <ul><li>match result table (with winner, time, completion; history)<li> statistics readable<li> score per-level the best times<li> playerprofiles | <ul><li>MatchResultRepo.cs<li> StatisticsRepo.cs<li> LeaderboardRepo.cs<li> PlayerProfileRepo.cs  |


### C4 — Context & Container (Session 3 studio)

```mermaid
%% Replace this placeholder with YOUR system's context diagram.
flowchart TB
    user([User]) -->|uses| system[Your System]
    system -->|stores data in| db[(Database)]
```

```mermaid
%% Container view: your containers should match the tier table above.
flowchart TB
    subgraph YourSystem [Your System]
        ui[Web UI / CLI<br/>Presentation] --> api[Application / Service]
        api --> domain[Domain Model]
        domain --> db[(Database<br/>Data tier)]
    end
```

### UML — Class & Sequence (Session 3 studio)

```mermaid
%% Class diagram: your 3–4 core domain classes.
classDiagram
    class ExampleEntity {
        -id: Long
        -name: String
        +doSomething()
    }
```

```mermaid
%% Sequence diagram: ONE core use case, end to end.
sequenceDiagram
    actor U as User
    participant UI
    participant S as Service
    participant D as Data
    U->>UI: action
    UI->>S: request
    S->>D: save/load
    D-->>S: result
    S-->>UI: response
    UI-->>U: confirmation
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
