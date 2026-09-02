import * as THREE from 'three';
//import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js'; //Don't need yet
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';

const scene = new THREE.Scene();
const camera = new THREE.PerspectiveCamera(75, window.innerWidth / window.innerHeight, 0.1, 100);

const renderer = new THREE.WebGLRenderer();
renderer.setSize(window.innerWidth, window.innerHeight);
renderer.setAnimationLoop(animate);
document.body.appendChild(renderer.domElement);

const geometry = new THREE.BoxGeometry(1,1,1);
const material = new THREE.MeshBasicMaterial({ color: 0x550033 });
const cube = new THREE.Mesh(geometry, material);
//scene.add(cube);

camera.position.z = 5;
const controls = new OrbitControls(camera, renderer.domElement);

//Here we are gonna create players
const players = new Map(); //map of players, key is player id, value is object with player data

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/gameHub")
    .withAutomaticReconnect()
    .build();

connection.on("Welcome", (me, roster) => roster.forEach(spawnBox));
connection.on("PlayerJoined", (payload) => { console.log("Joined Player, read payload: ", payload); spawnBox(payload); });
connection.on("PlayerLeft", (id) => {
    const mesh = players.get(id);
    if (mesh) { scene.remove(mesh); players.delete(id); }
});

await connection.start();
//await connection.invoke("Join", playerName);
await connection.invoke("Join", "testName");

//render loop
function animate() {
    renderer.render(scene, camera);
}

function spawnBox(p) {
    const newMat = new THREE.MeshBasicMaterial({ color: 0xff0000 });
    const mesh = new THREE.Mesh(geometry, newMat);
    mesh.position.set(p.x, p.y, p.z);
    
    scene.add(mesh);
    players.set(p.id, mesh);
}