import { SceneManager } from './core/SceneManager.js';
import { NetworkClient } from './net/NetworkClient.js';
//import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js'; //Don't need yet
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';

import { InputController } from './input/InputController.js';
import { applyInput } from './shared/movement.js';
import { WALLS, OBJECTIVE } from './shared/level.js';


const input = new InputController();
let predicted = { x: 0, z: 0 };
let pending = [];
let last = "";
let gameOver = false;

const playerName = sessionStorage.getItem('playerName') ?? "Anon";
const roomCode = sessionStorage.getItem('roomCode') ?? "LOBBY";
//target canvas element
const myCanvas = document.querySelector('#heist-canvas');

//init scenemanager
const sceneManager = new SceneManager(myCanvas);
sceneManager.buildLevel(WALLS);
sceneManager.buildObjective(OBJECTIVE);
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

/* snap is players array, update 9/27: snapshot must read through players and states, so added .players to the snapshot object, and added a lastSeq to each player for prediction
*/
net.onSnapshot(snap => {
    const me = snap.players.find(p => p.id === sceneManager.myId);
    if (me) {
        predicted = { x: me.x, z: me.z };                   //1. snap to recieved server pos
        pending = pending.filter(c => c.seq > me.lastSeq);  //2. drop our acknowledged seq
        for (const c of pending) predicted = applyInput(predicted, c, WALLS); //3. replay
        sceneManager.setSelf(predicted);
    }
    sceneManager.receiveSnapshot(snap.players); //remotes
    //console.log("state:", snap.state);          // lets test before making UI

    //simple win banner UI, if the server sends a "Won" state, we show the win banner
    if (snap.state === "Won" && !gameOver) {
        gameOver = true;
        document.getElementById("winBanner").hidden = false;
    }
});


await net.join(roomCode, playerName);

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
    if (gameOver) return; //stop sending input if game is over)
    const intent = input.getIntent();
    const cmd = { seq: seq++, x: intent.x, z: intent.z };
    pending.push(cmd);
    predicted = applyInput(predicted, cmd, WALLS); //prediction
    sceneManager.setSelf(predicted);        //move box instantly
    net.sendInput(cmd);
}, 50);



