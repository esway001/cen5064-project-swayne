namespace Heist.Domain;

public record Wall(float MinX, float MaxX, float MinZ, float MaxZ);

public readonly record struct Hazard(
    float MinX, float MaxX, float MinZ, float MaxZ, //Geometry - must match shared/level.js
    float SafeMs, float ArmedMs                     //Timing - Server only
    );

public static class Level
{
    public const float PlayerRadius = 0.5f;

    public static readonly Wall[] Walls =
    {
        new(-1f, 1f, 3f, 4f),
        new(4f, 5f, -2f, 6f),

    };

    public static readonly Hazard[] Hazards =
    {
        new(3f,4f,-3f,3f, SafeMs: 5000f, ArmedMs: 10000f),
    };

    //Mirror objective of level.js objective
    public static readonly (float X, float Z, float Radius) Objective = (8f, 0, 0.75f);
}
