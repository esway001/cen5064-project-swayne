namespace Heist.Domain;

//id, username, playernumber, pos xyz, last input
public record Player(string Id, string Name, int Number, float X, float Y, float Z, uint LastSeq); 

