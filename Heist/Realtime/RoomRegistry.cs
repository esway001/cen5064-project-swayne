using System.Collections.Concurrent;
using Heist.Domain;
namespace Heist.Realtime;

public class RoomRegistry
{
    private readonly ConcurrentDictionary<string, Room> _rooms = new();         //code for room, ondemand rooms
    private readonly ConcurrentDictionary<string, string> _connRoom = new();    // connectionId for room, for looking up

    //find room
    public Room GetOrCreate(string code) => _rooms.GetOrAdd(code, c => new Domain.Room(c));

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