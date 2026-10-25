using System.Text;
using System.Text.Json;

if (args.Length != 2)
    return Fail("usage: Heist.LevelTool <input.glb> <output.json>");

using var reader = new BinaryReader(File.OpenRead(args[0]));

//GLB header: three four byte fields:
uint magic      = reader.ReadUInt32();
uint version    = reader.ReadUInt32();
uint length     = reader.ReadUInt32();

if (magic != 0x46546C67)
    return Fail("not a GLB file");
if (version != 2)
    return Fail($"unsupported glTF version {version}");
if(length != reader.BaseStream.Length)
    return Fail($"file length mismatch(truncated?): header {length} vs actual {reader.BaseStream.Length}");

// First chunk is the JSON, else not a valid GLB file
uint chunkLength = reader.ReadUInt32();
uint chunkType   = reader.ReadUInt32();
if(chunkType != 0x4E4F534A)
    return Fail("first chunk is not JSON");

string json = Encoding.UTF8.GetString(reader.ReadBytes((int)chunkLength));
//Console.WriteLine(json);

using var doc = JsonDocument.Parse(json);
JsonElement root        = doc.RootElement;
JsonElement nodes       = root.GetProperty("nodes");
JsonElement meshes      = root.GetProperty("meshes");
JsonElement accessors   = root.GetProperty("accessors");
var walls = new List<BoxData>();
var hazards = new List<HazardData>();
var errors = new List<string>();

Console.WriteLine($"[LevelTool] nodes in file: {nodes.GetArrayLength()}");

// every node index that is somebody's child
var childIndices = new HashSet<int>();
foreach (JsonElement other in nodes.EnumerateArray())
    if (other.TryGetProperty("children", out var kids))
        foreach (JsonElement k in kids.EnumerateArray())
            childIndices.Add(k.GetInt32());

var spawns = new Dictionary<int, PointData>();
ObjectiveData? objective = null;
int index = -1;

//main loop for reading nodes, colliders, and hazards
foreach (JsonElement node in nodes.EnumerateArray())
{
    string name = node.TryGetProperty("name", out var n) ? n.GetString()! : "(unnamed)";

    //adding code to spot children and parents
    index++;
    bool isMarker = name.StartsWith("collider_") || name.StartsWith("hazard_")
                 || name.StartsWith("spawn_") || name.StartsWith("objective");
    if (isMarker && childIndices.Contains(index))
    {
        errors.Add($"{name}: is parented to another object (markers must be top-level)");
        continue;
    }

    //main logic for reading colliders and hazards
    if (name.StartsWith("collider_"))
    {
        BoxData? box = ReadBox(node, name, meshes, accessors, errors);
        if (box is not null) walls.Add(box);
    }
    else if (name.StartsWith("hazard_"))
    {
        BoxData? box = ReadBox(node, name, meshes, accessors, errors);
        if (box is null) continue;

        // timing comes from Blender custom properties -> glTF "extras"
        if (!node.TryGetProperty("extras", out var extras)
            || !extras.TryGetProperty("safeMs", out var safe)
            || !extras.TryGetProperty("armedMs", out var armed)
            || safe.ValueKind != JsonValueKind.Number
            || armed.ValueKind != JsonValueKind.Number)
        {
            errors.Add($"{name}: needs number custom properties safeMs and armedMs");
            continue;
        }

        float safeMs = safe.GetSingle(), armedMs = armed.GetSingle();
        const float MinMs = 100f;   // make sure hazard has atleast 0.1s of safe armed time
        if (safeMs < MinMs || armedMs < MinMs)
        {
            errors.Add($"{name}: safeMs/armedMs are {safeMs}/{armedMs} — must be at least {MinMs} (forgot to set the values?)");
            continue;
        }
        hazards.Add(new HazardData(box.Name, box.MinX, box.MaxX, box.MinY, box.MaxY, box.MinZ, box.MaxZ, safeMs, armedMs));
    }
    else if (name.StartsWith("spawn_"))
    {
        // "spawn_3" -> 3; "spawn_1.001" fails to parse -> error
        if (!int.TryParse(name["spawn_".Length..], out int slot) || slot < 1 || slot > 4)
        { errors.Add($"{name}: must be named spawn_1 to spawn_4"); continue; }
        if (spawns.ContainsKey(slot))
        { errors.Add($"{name}: duplicate spawn slot {slot}"); continue; }

        float[] t = ReadVec3(node, "translation", 0f);
        spawns[slot] = new PointData(name, t[0], t[2]);
    }
    else if (name.StartsWith("objective"))
    {
        if (objective is not null) { errors.Add($"{name}: more than one objective"); continue; }

        float[] t = ReadVec3(node, "translation", 0f);
        float radius = 0.75f;   // default if no custom property
        if (node.TryGetProperty("extras", out var ex) && ex.TryGetProperty("radius", out var r)
            && r.ValueKind == JsonValueKind.Number)
            radius = r.GetSingle();
        if (radius <= 0) { errors.Add($"{name}: radius must be > 0"); continue; }

        objective = new ObjectiveData(name, t[0], t[2], radius);
    }
    else
    {
        Console.WriteLine($"[LevelTool] skipping {name}");
    }
}

for (int slot = 1; slot <= 4; slot++)
    if (!spawns.ContainsKey(slot)) errors.Add($"missing spawn_{slot}");
if (objective is null) errors.Add("missing objective");

//guard against spawn in walls 
const float PlayerRadius = 0.5f;   // must match the game's PlayerRadius
foreach (PointData p in spawns.Values)
{
    foreach (BoxData w in walls)
        if (CircleHitsBox(p.X, p.Z, PlayerRadius, w.MinX, w.MaxX, w.MinZ, w.MaxZ))
            errors.Add($"{p.Name}: overlaps wall {w.Name}");
    foreach (HazardData h in hazards)
        if (CircleHitsBox(p.X, p.Z, PlayerRadius, h.MinX, h.MaxX, h.MinZ, h.MaxZ))
            errors.Add($"{p.Name}: overlaps hazard {h.Name}");
}

if (errors.Count > 0)
{
    foreach (string e in errors) Console.Error.WriteLine($"[LevelTool] {e}");
    return 1;
}
if (walls.Count == 0) return Fail("no collider_* objects found");

foreach (BoxData w in walls)
    Console.WriteLine($"wall {w.Name}: x {w.MinX}..{w.MaxX}  z {w.MinZ}..{w.MaxZ}");

foreach (HazardData h in hazards)
    Console.WriteLine($"hazard {h.Name}: x {h.MinX}..{h.MaxX}  z {h.MinZ}..{h.MaxZ}  safe {h.SafeMs}  armed {h.ArmedMs}");

foreach (var kv in spawns.OrderBy(kv => kv.Key))
    Console.WriteLine($"spawn {kv.Key}: x {kv.Value.X}  z {kv.Value.Z}");
Console.WriteLine($"objective: x {objective!.X}  z {objective.Z}  radius {objective.Radius}");

var level = new LevelFile(
    FormatVersion: 1,
    Walls: walls,
    Hazards: hazards,
    Spawns: spawns.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToList(),  // index 0 = spawn_1
    Objective: objective!);

var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,   // MinX -> minX, matches JS style
    WriteIndented = true                                  // readable, diff-friendly in Git
};

string outPath = Path.GetFullPath(args[1]);
Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
File.WriteAllText(outPath, JsonSerializer.Serialize(level, options));
Console.WriteLine($"[LevelTool] wrote {outPath}");

return 0;

static int Fail(string message)
{
    Console.Error.WriteLine($"[LevelTool] {message}");
    return 1;
}

static float[] ReadVec3(JsonElement obj, string prop, float fallback)
{
    if(!obj.TryGetProperty(prop, out var arr))
        return new[] {fallback, fallback, fallback };
    return new[] { arr[0].GetSingle(), arr[1].GetSingle(), arr[2].GetSingle() };
}

static bool HasRotation(JsonElement node)
{
    if (!node.TryGetProperty("rotation", out var r)) return false;
    // glTF rotation is a quaternion [x, y, z, w]; "no rotation" = [0, 0, 0, 1]
    const float eps = 0.0001f;
    return MathF.Abs(r[0].GetSingle()) > eps
        || MathF.Abs(r[1].GetSingle()) > eps
        || MathF.Abs(r[2].GetSingle()) > eps;
}

static BoxData? ReadBox(JsonElement node, string name, JsonElement meshes, JsonElement accessors, List<string> errors)
{
    float[] t = ReadVec3(node, "translation", 0f);
    float[] s = ReadVec3(node, "scale", 1f);

    if (HasRotation(node)) { errors.Add($"{name}: is rotated (must be axis-aligned)"); return null; }
    if (s[0] <= 0 || s[2] <= 0) { errors.Add($"{name}: has negative scale (mirrored)"); return null; }
    if (node.TryGetProperty("children", out _)) { errors.Add($"{name}: has child objects"); return null; }
    if (!node.TryGetProperty("mesh", out var meshIndex)) { errors.Add($"{name}: has no mesh"); return null; }

    int posIndex = meshes[meshIndex.GetInt32()]
        .GetProperty("primitives")[0]
        .GetProperty("attributes")
        .GetProperty("POSITION").GetInt32();

    float[] min = ReadVec3(accessors[posIndex], "min", 0f);
    float[] max = ReadVec3(accessors[posIndex], "max", 0f);

    return new BoxData(name,
    min[0] * s[0] + t[0], max[0] * s[0] + t[0],
    min[1] * s[1] + t[1], max[1] * s[1] + t[1],   // Y: index 1
    min[2] * s[2] + t[2], max[2] * s[2] + t[2]);
}

// same closest-point test as Room.HitsWall
static bool CircleHitsBox(float x, float z, float r, float minX, float maxX, float minZ, float maxZ)
{
    float cx = MathF.Max(minX, MathF.Min(x, maxX));
    float cz = MathF.Max(minZ, MathF.Min(z, maxZ));
    float dx = x - cx, dz = z - cz;
    return dx * dx + dz * dz < r * r;
}

record BoxData(string Name, float MinX, float MaxX, float MinY, float MaxY, float MinZ, float MaxZ);
record HazardData(string Name, float MinX, float MaxX, float MinY, float MaxY, float MinZ, float MaxZ, float SafeMs, float ArmedMs);
record PointData(string Name, float X, float Z);
record ObjectiveData(string Name, float X, float Z, float Radius);

record LevelFile(int FormatVersion, List<BoxData> Walls, List<HazardData> Hazards,
                 List<PointData> Spawns, ObjectiveData Objective);