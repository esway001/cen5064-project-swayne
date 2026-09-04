import { SceneManager } from './core/SceneManager.js';
import { NetworkClient } from './net/NetworkClient.js';
//import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js'; //Don't need yet
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';

import { InputController } from './input/InputController.js';
import { applyInput } from './shared/movement.js';
const input = new InputController();
let predicted = { x: 0, z: 0 };
let pending = [];
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
    predicted = { x: me.x, z: me.z }; //seed on spawn the predictions
    roster.forEach(p => sceneManager.addPlayer(p))
});

net.onPlayerJoined(p => sceneManager.addPlayer(p));
net.onPlayerLeft(id => sceneManager.removePlayer(id));
//debug log
//net.onSnapshot(snap => console.log(snap));

/* snap is players array
*/
net.onSnapshot(snap => {
    const me = snap.find(p => p.id === sceneManager.myId);
    if (me) {
        predicted = { x: me.x, z: me.z };                   //1. snap to recieved server pos
        pending = pending.filter(c => c.seq > me.lastSeq);  //2. drop our acknowledged seq
        for (const c of pending) predicted = applyInput(predicted, c); //3. replay
        sceneManager.setSelf(predicted);
    }
    sceneManager.receiveSnapshot(snap); //remotes
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
let seq = 0;
// setInterval(() => {
//     const { x, z } = input.getIntent();
//     net.sendInput({ seq: seq++, x, z });
// }, 50);

/**Update intent intervals function to prediction on input function*/
setInterval(() => {
    const intent = input.getIntent();
    const cmd = { seq: seq++, x: intent.x, z: intent.z };
    pending.push(cmd);
    predicted = applyInput(predicted, cmd); //prediction
    sceneManager.setSelf(predicted);        //move box instantly
    net.sendInput(cmd);
}, 50);
await net.join(playerName);


