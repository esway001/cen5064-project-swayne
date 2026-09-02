export class NetworkClient {
    constructor(url) {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(url)
            .withAutomaticReconnect()
            .build();
    }
    onWelcome(cb) { this.connection.on("Welcome", cb); console.log('Welcome to the Game', cb); }
    onPlayerJoined(cb) { this.connection.on("PlayerJoined", cb); console.log("Joined Player, read payload: ", cb); }
    onPlayerLeft(cb) { this.connection.on("PlayerLeft", cb); }
    async join(name) {
        await this.connection.start();
        await this.connection.invoke("Join", name);
    }

    sendInput(cmd) {
        if (this.connection.state === signalR.HubConnectionState.Connected) {
            this.connection.invoke("SendInput", cmd);
        }
    }

    onSnapshot(cb) { this.connection.on("Snapshot", cb); }
}