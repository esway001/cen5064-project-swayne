using System.Text.Json;
using Heist.Domain;
using Xunit;

namespace Heist.Tests;

public class RoomRulesTests
{
    // load the real test level, copied next to the test assembly
    static LevelFile LoadTestLevel()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "test.json");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<LevelFile>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    [Fact]
    public void Player_On_Armed_Trap_Loses()
    {
        var baseLevel = LoadTestLevel();
        var h = baseLevel.Hazards[0];
        float cx = (h.MinX + h.MaxX) / 2f;   // arrange: spawn ON the hazard
        float cz = (h.MinZ + h.MaxZ) / 2f;

        var level = baseLevel with {
            Spawns = new[] { new SpawnData("spawn_1", cx, cz) }
        };
        var room = new Room("TEST", "test", level);
        room.Add("p1", "Trapped");

        // act: one Step with dt just past the safe window -> hazard armed this tick
        room.Step((h.SafeMs + 1f) / 1000f);

        Assert.Equal(GameState.Lost, room.State);   // assert
    }

    [Fact]
    public void Player_On_Objective_Wins()
    {
        var baseLevel = LoadTestLevel();
        var o = baseLevel.Objective;

        var level = baseLevel with {               // arrange: spawn ON the objective
            Spawns = new[] { new SpawnData("spawn_1", o.X, o.Z) }
        };
        var room = new Room("TEST", "test", level);
        room.Add("p1", "Winner");

        room.Step(0.05f);                          // act: one normal tick, no input

        Assert.Equal(GameState.Won, room.State);   // assert
    }
}