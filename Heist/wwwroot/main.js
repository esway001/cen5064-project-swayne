import { SceneManager } from './core/SceneManager.js';
import { NetworkClient } from './net/NetworkClient.js';
//import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js'; //Don't need yet
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';

import { InputController } from './input/InputController.js';
const input = new InputController();

let last = "";

const playerName = sessionStorage.getItem('playerName') ?? "Anon";
//target canvas element
const myCanvas = document.querySelector('#heist-canvas');

//init scenemanager
const sceneManager = new SceneManager(myCanvas);
const controls = new OrbitControls(sceneManager.camera, sceneManager.renderer.domElement);

//Network Methods
const net = new NetworkClient("/gameHub");
net.onWelcome((me, roster) => {
    sceneManager.myId = me.id;
    roster.forEach(p => sceneManager.addPlayer(p))
});
net.onSnapshot(snap => sceneManager.receiveSnapshot(snap));
net.onPlayerJoined(p => sceneManager.addPlayer(p));
net.onPlayerLeft(id => sceneManager.removePlayer(id));
//debug log
//net.onSnapshot(snap => console.log(snap));

/* snap is players array
*/
net.onSnapshot(snap => {
    //console.log('snapshot', snap);
    sceneManager.applySnapshot(snap);
});

window.addEventListener('resize', () => sceneManager.onWindowResize());

//render loop
function animate() {
    requestAnimationFrame(animate);

    const intent = input.getIntent();
    const sig = `${intent.x},${intent.z}`;
    if (sig !== last) { console.log("intent", intent); last = sig; }
    controls.update();

    //update scenemanager
    sceneManager.update();
}

animate();

await net.join(playerName);

let seq = 0;
setInterval(() => {
    const { x, z } = input.getIntent();
    net.sendInput({ seq: seq++, x, z });
}, 50);