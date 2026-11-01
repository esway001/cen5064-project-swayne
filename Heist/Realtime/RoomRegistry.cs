using System.Collections.Concurrent;
using Heist.Domain;
namespace Heist.Realtime;

public class RoomRegistry
{
    private readonly ConcurrentDictionary<string, Room> _rooms = new();         //code for room, ondemand rooms
    private readonly ConcurrentDictionary<string, string> _connRoom = new();    // connectionId for room, for looking up
    private readonly LevelCatalog _catalog;
    private const string CodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private static string NewCode() =>
        new string(Enumerable.Range(0, 4).Select(_ => CodeChars[Random.Shared.Next(CodeChars.Length)]).ToArray());

    public Room Create(string levelId)
    {
        while(true)
        {
            var room = new Room(NewCode(), levelId, _catalog.Get(levelId));
            if (_rooms.TryAdd(room.Code, room)) return room;                //code collision protection
        }
    }

    public Room? Get(string code) => _rooms.TryGetValue(code, out var r) ? r : null;

    public RoomRegistry(LevelCatalog catalog) => _catalog = catalog;


    //find room
    //public Room GetOrCreate(string code) => _rooms.GetOrAdd(code, c => new Room(c, "test", _catalog.Get("test")));


    //Routemap
    public Room? RoomOf(string connId)
        => _connRoom.TryGetValue(connId, out var code) && _rooms.TryGetValue(code, out var room)
        ? room : null;

    //record connection to Room
    public void Track(string connId, string code) => _connRoom[connId] = code;

    //remove conn from room, delete room if empty, return code instead of null for broadcasting playerleft
    public string? RemoveConnection(string connId)
    {
        if (!_connRoom.TryRemove(connId, out var code)) return null;
        if(_rooms.TryGetValue(code, out var room))
        {
            room.Remove(connId);
            if (room.IsEmpty) _rooms.TryRemove(code, out _);
        }
        return code;
    }

    public IReadOnlyCollection<Room> ActiveRooms => _rooms.Values.ToArray();
}