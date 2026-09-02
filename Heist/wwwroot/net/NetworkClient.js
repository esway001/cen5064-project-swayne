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
}