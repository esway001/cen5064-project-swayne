import { SceneManager } from './core/SceneManager.js';
import { NetworkClient } from './net/NetworkClient.js';
//import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js'; //Don't need yet
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';

const playerName = sessionStorage.getItem('playerName') ?? "Anon";
//target canvas element
const myCanvas = document.querySelector('#heist-canvas');

//init scenemanager
const sceneManager = new SceneManager(myCanvas);
const controls = new OrbitControls(sceneManager.camera, sceneManager.renderer.domElement);

//Network Methods
const net = new NetworkClient("/gameHub");
net.onWelcome((me, roster) => roster.forEach(p => sceneManager.addPlayer(p)));
net.onPlayerJoined(p => sceneManager.addPlayer(p));
net.onPlayerLeft(id => sceneManager.removePlayer(id));


window.addEventListener('resize', () => sceneManager.onWindowResize());


// connection.on("Welcome", (me, roster) => { console.log('Welcome to the Game', me, roster); roster.forEach(spawnBox) });
// connection.on("PlayerJoined", (payload) => { console.log("Joined Player, read payload: ", payload); spawnBox(payload); });
// connection.on("PlayerLeft", (id) => {
//     console.log('player left', id);
//     const mesh = players.get(id);
//     if (mesh) { scene.remove(mesh); players.delete(id); }
// });

//render loop
function animate() {
    requestAnimationFrame(animate);
    controls.update();

    //update scenemanager
    sceneManager.update();
}

animate();

await net.join(playerName);