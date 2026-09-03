import * as THREE from 'three';

const PLAYER_COLORS = { 1: 0xff0000, 2: 0x1656AD, 3: 0x008000, 4: 0xF0C807 };
const INTERP_DELAY = 100; //100ms delay
export class SceneManager {
    constructor(canvas) {
        this.canvas = canvas;

        // scene, camera, renderer
        this.scene = new THREE.Scene();
        this.camera = this.createCamera();
        this.renderer = this.createRenderer();
        this.players = new Map();
        this.buffers = new Map();   //player list of timestamp samples
        this.myId = null;
    }

    /**
     * 
     * THREE JS METHODS
     */
    createCamera() {
        const camera = new THREE.PerspectiveCamera(75, window.innerWidth / window.innerHeight, 0.1, 100);
        camera.position.z = 5;
        return camera;
    }
    createRenderer() {
        const renderer = new THREE.WebGLRenderer({ canvas: this.canvas, antialias: true });
        renderer.setSize(this.canvas.clientWidth, this.canvas.clientHeight);
        return renderer;
    }
    createCube() {
        const geo = new THREE.BoxGeometry(1, 1, 1);
        const material = new THREE.MeshBasicMaterial({ color: 0x550033 });
        return new THREE.Mesh(geo, material);
    }



    update() {
        //if animations needed they go here

        //Interpolation on each render frame; performance.now built in browser function returns high precision timestamp
        //so here rendertime is 100ms before precise timestamp at receive time, not syncing server clock
        const renderTime = performance.now() - INTERP_DELAY;

        //buffers is timestamp samples map, we want to step through pairs of neighbors, since we need i and i+1 and don't
        //want to go out of bounds. The test is: are we at or before rendertime and are we at or after rendertime? did we find bracket
        // of s0->rendertime->s1
        for (const [id, buf] of this.buffers) { //once per player
            const mesh = this.players.get(id);
            if (!mesh || buf.length < 2) continue; //skip if not spawned yet or skip if only one sample available and wait for 2+

            //between rendertime and the timestamp in buffer right after, we are looking for the bracket
            let s0, s1;
            for (let i = 0; i < buf.length - 1; i++) { //searches for bracket pair
                if (buf[i].t <= renderTime && buf[i + 1].t >= renderTime) {
                    s0 = buf[i]; s1 = buf[i + 1]; break;
                }
            }

            if (s0 && s1) { //blend between s0 and s1 by alpha and move mesh
                const span = s1.t - s0.t;
                const a = span > 0 ? (renderTime - s0.t) / span : 0; //halfway between 0..1
                mesh.position.set(
                    s0.x + (s1.x - s0.x) * a,
                    s0.y + (s1.y - s0.y) * a,
                    s0.z + (s1.z - s0.z) * a
                );
            } else {
                const last = buf[buf.length - 1];
                mesh.position.set(last.x, last.y, last.z);
            }

        }

        //Render Scene
        this.renderer.render(this.scene, this.camera);
    }

    onWindowResize() {
        const width = this.canvas.clientWidth;
        const height = this.canvas.clientHeight;

        this.camera.aspect = width / height;
        this.camera.updateProjectionMatrix();

        this.renderer.setSize(width, height);
    }

    /**
     * Network Methods
     * */

    addPlayer(p) {
        //we can later replace this mesh with a model from blender
        const color = PLAYER_COLORS[p.number] ?? 0xffffff;
        const mesh = new THREE.Mesh(
            new THREE.BoxGeometry(1, 1, 1),
            new THREE.MeshBasicMaterial({ color })
        );
        //set posiiton
        mesh.position.set(p.x, p.y, p.z);
        this.scene.add(mesh);
        this.players.set(p.id, mesh);
    }

    removePlayer(id) {
        const mesh = this.players.get(id);
        if (mesh) { this.scene.remove(mesh); this.players.delete(id); }
    }

    //This method will take the intent from the snapshot positions and apply to the mesh
    applySnapshot(players) {
        for (const p of players) {
            const mesh = this.players.get(p.id);
            console.log(p.id.slice(0, 4), "x:", p.x, "z:", p.z, "mesh?", !!mesh);
            if (mesh) mesh.position.set(p.x, p.y, p.z);
        }
    }

    receiveSnapshot(players) {
        const t = performance.now();
        for (const p of players) {
            if (p.id === this.myId) {                       //snapshot of self
                const mesh = this.players.get(p.id);
                if (mesh) mesh.position.set(p.x, p.y, p.z);
                continue;
            }
            let buf = this.buffers.get(p.id);               //store timestamp
            if (!buf) { buf = []; this.buffers.set(p.id, buf); }
            buf.push({ t, x: p.x, y: p.y, z: p.z });
            while (buf.length > 60) buf.shift();        //history about 3s
        }
    }
};

