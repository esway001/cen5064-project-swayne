import * as THREE from 'three';

const PLAYER_COLORS = { 1: 0xff0000, 2: 0x1656AD, 3: 0x008000, 4: 0xF0C807 };
export class SceneManager {
    constructor(canvas) {
        this.canvas = canvas;

        // scene, camera, renderer
        this.scene = new THREE.Scene();
        this.camera = this.createCamera();
        this.renderer = this.createRenderer();
        this.players = new Map();
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
};

