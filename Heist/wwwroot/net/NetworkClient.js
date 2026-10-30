export class NetworkClient {
    constructor(url) {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(url)
            .withAutomaticReconnect()
            .build();
    }
    //wrappers for hub methods
    onWelcome(cb)               { this.connection.on("Welcome", cb); console.log('Welcome to the Game', cb); }
    onPlayerJoined(cb)          { this.connection.on("PlayerJoined", cb); console.log("Joined Player, read payload: ", cb); }
    onPlayerLeft(cb)            { this.connection.on("PlayerLeft", cb); }
    listLevels()                { return this.connection.invoke("ListLevels"); }
    listOpenRooms()             { return this.connection.invoke("ListOpenRooms"); }
    createRoom(name, levelId)   { return this.connection.invoke("CreateRoom", name, levelId); }
    onLobbyChanged(cb)          { this.connection.on("LobbyChanged", cb); }
    startGame()                 { return this.connection.invoke("StartGame"); }
    onRosterChanged(cb)         { this.connection.on("RosterChanged", cb); }
    onGameStarted(cb)           { this.connection.on("GameStarted", cb); }

    async connect() { await this.connection.start(); }
    async join(code, name) {
        await this.connection.invoke("JoinRoom", code, name);
    }

    sendInput(cmd) {
        if (this.connection.state === signalR.HubConnectionState.Connected) {
            this.connection.invoke("SendInput", cmd);
        }
    }

    onSnapshot(cb) { this.connection.on("Snapshot", cb); }
}