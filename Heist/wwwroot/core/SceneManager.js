import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
import { DRACOLoader } from 'three/addons/loaders/DRACOLoader.js';
import { clone as skeletonClone } from 'three/addons/utils/SkeletonUtils.js'; 
console.log("MAIN BUILD 3");

const PLAYER_COLORS = { 1: 0xff0000, 2: 0x1656AD, 3: 0x008000, 4: 0xF0C807 };
const MARKER_PREFIXES = ['collider_', 'hazard_', 'spawn_', 'objective'];
const INTERP_DELAY = 100; //100ms delay
const CHAR_SCALE = 1;
const MODEL_YAW_OFFSET = +Math.PI / 2; //model is facing right, rotate to face forward

//helper function to find a clip by keywords, returns null if not found
function findClip(clips, keywords) {
    return clips?.find(c => keywords.some(k => c.name.toLowerCase().includes(k))) ?? null;
}
export class SceneManager {
    constructor(canvas) {
        this.canvas = canvas;

        // scene, camera, renderer
        this.scene = new THREE.Scene();
        this.world = new THREE.Group();
        this.scene.add(this.world);
        this.camera = this.createCamera();
        this.renderer = this.createRenderer();
        this.players = new Map();
        this.buffers = new Map();   //player list of timestamp samples
        this.myId = null;
        this.clock = new THREE.Clock();
        this.selfTarget = null;
        this.gltfLoader = new GLTFLoader();
        this.draco = new DRACOLoader();
        this.addLights();
        this.doorDefs = [];
        this.doorNodes = [];
        this.doorBaseY = [];
        this.doorTargetY = [];
        this.doorLift = [];
    }

    /**
     * 
     * THREE JS METHODS
     */
    createCamera() {
        const camera = new THREE.PerspectiveCamera(75, window.innerWidth / window.innerHeight, 0.1, 100);
        camera.position.z = 5;
        camera.position.y = 5;
        camera.lookAt(0, 0, 0);
        return camera;
    }
    createRenderer() {
        const renderer = new THREE.WebGLRenderer({ canvas: this.canvas, antialias: true });
        //renderer.setSize(this.canvas.clientWidth, this.canvas.clientHeight);
        renderer.setSize(window.innerWidth, window.innerHeight);
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

        //self smoothing
        const selfMesh = this.players.get(this.myId);
        if (selfMesh && this.selfTarget) {
            const k = 0.3;  //snappy smoothing, 0 no smooth, 1 instant snap, lets test out different numbers
            // formula better for later k = 1 - Math.exp(-12 * dt)
            selfMesh.position.x += (this.selfTarget.x - selfMesh.position.x) * k;
            selfMesh.position.z += (this.selfTarget.z - selfMesh.position.z) * k;
        }

        //face movement since model is turned 90 degrees.
        const dt = this.clock.getDelta();
        for (const mesh of this.players.values()) {
            mesh.userData.mixer?.update(dt);
            this.faceMovement(mesh);
        }

        for (let i = 0; i < this.doorNodes.length; i++) {
            const node = this.doorNodes[i];
            if (node) node.position.y += (this.doorTargetY[i] - node.position.y) * 0.15; //smooth door movement
        }
        this.updateCamera();
        //Render Scene
        this.renderer.render(this.scene, this.camera);
    }

    onWindowResize() {
        // const width = this.canvas.clientWidth;
        // const height = this.canvas.clientHeight;
        const width = window.innerWidth;
        const height = window.innerHeight;

        this.camera.aspect = width / height;
        this.camera.updateProjectionMatrix();

        this.renderer.setSize(width, height);
    }

    /**
     * Network Methods
     * */

    addPlayer(p) {
        const container = new THREE.Group();

        if (this.characterTemplate) {
            const model = skeletonClone(this.characterTemplate);
            model.scale.setScalar(CHAR_SCALE);
            model.rotation.y = MODEL_YAW_OFFSET;
            container.add(model);

            if (this.characterClips?.length) {
                const mixer = new THREE.AnimationMixer(model);
                const idleClip = THREE.AnimationClip.findByName(this.characterClips, 'Crouch_Idle');
                const walkClip = THREE.AnimationClip.findByName(this.characterClips, 'Crouch_Walk');
                container.userData.mixer = mixer;
                container.userData.actions = {
                    idle: idleClip ? mixer.clipAction(idleClip) : null,
                    walk: walkClip ? mixer.clipAction(walkClip) : null
                };
                container.userData.anim = 'idle';
                container.userData.actions.idle?.play();
            }
        } else {
            const geo = new THREE.BoxGeometry(1, 1, 1);
            geo.translate(0, 0.5, 0);
            container.add(new THREE.Mesh(geo, new THREE.MeshBasicMaterial({ color: 0xffffff })));
        }

        const ring = new THREE.Mesh(
            new THREE.RingGeometry(0.4, 0.6, 24),
            new THREE.MeshBasicMaterial({ color: PLAYER_COLORS[p.number] ?? 0xffffff, side: THREE.DoubleSide })
        );
        ring.rotation.x = -Math.PI / 2;
        ring.position.y = 0.02;
        container.add(ring);

        container.position.set(p.x, p.y, p.z);
        this.world.add(container);
        this.players.set(p.id, container);
    }
        

    buildLevel(walls) {
        for (const w of walls) {
            const width = w.maxX - w.minX;
            const height = w.maxY - w.minY;
            const depth = w.maxZ - w.minZ;
            const mesh = new THREE.Mesh(
                new THREE.BoxGeometry(width, 2, depth),
                new THREE.MeshBasicMaterial({ color: 0x888888 })
            );
            mesh.position.set((w.minX + w.maxX) / 2,
                (w.minY + w.maxY) / 2,
                (w.minZ + w.maxZ) / 2);
            
            this.world.add(mesh);
        }
    }

    removePlayer(id) {
        const mesh = this.players.get(id);
        if (mesh) { this.world.remove(mesh); this.players.delete(id); }
    }

    setSelf(pos) {
        //store target instead
        this.selfTarget = { x: pos.x, z: pos.z };
    }

    //This method will take the intent from the snapshot positions and apply to the mesh [UPDATE, NO LONGER CALLED]
    // applySnapshot(players) {
    //     for (const p of players) {
    //         const mesh = this.players.get(p.id);
    //         console.log(p.id.slice(0, 4), "x:", p.x, "z:", p.z, "mesh?", !!mesh);
    //         if (mesh) mesh.position.set(p.x, p.y, p.z);
    //     }
    // }

    receiveSnapshot(players) {
        const t = performance.now();
        for (const p of players) {
            if (p.id === this.myId) continue;
            /*{                       //snapshot of self
                console.log("self lastSeq:", p.lastSeq, "pos:", p.x.toFixed(2), p.z.toFixed(2)); //see last seq rounded to two digits
                const mesh = this.players.get(p.id);
                if (mesh) mesh.position.set(p.x, p.y, p.z);
                continue;
            }*/
            let buf = this.buffers.get(p.id);               //store timestamp
            if (!buf) { buf = []; this.buffers.set(p.id, buf); }
            buf.push({ t, x: p.x, y: p.y, z: p.z });
            while (buf.length > 60) buf.shift();        //history about 3s
        }
    }


    //Render the objective
    buildObjective(obj) {
        const mesh = new THREE.Mesh(
            new THREE.CylinderGeometry(obj.radius, obj.radius, 1.5, 16),
            new THREE.MeshBasicMaterial({ color: 0xffd700 }) //golden color
        );
        mesh.position.set(obj.x, 0.75, obj.z);
        this.world.add(mesh);
    }

    buildHazards(hazards) {
        this.hazardMeshes = [];
        for (const h of hazards) {
            const w = h.maxX - h.minX;
            const d = h.maxZ - h.minZ;
            const geo = new THREE.PlaneGeometry(w, d);
            const mat = new THREE.MeshBasicMaterial({
                color: 0x8b94a3,                            //grey for safe, flip to red for danger
                transparent: true,
                opacity: 0.3,
                side: THREE.DoubleSide,
            });
            const mesh = new THREE.Mesh(geo, mat);
            mesh.rotation.x = -Math.PI / 2;                 //rotate to make it live horizontally
            mesh.position.set((h.minX + h.maxX) / 2, 0.02, (h.minZ + h.maxZ) / 2);  //lift to prevent z-fighting
            this.world.add(mesh);
            this.hazardMeshes.push(mesh);
        }
    }

    buildDoors(doors) {
        this.doorDefs = doors;
        this.doorNodes = new Array(doors.length).fill(null);
        this.doorBaseY = new Array(doors.length).fill(0);
        this.doorTargetY = new Array(doors.length).fill(0);
        this.doorLift = doors.map(d => (d.maxY - d.minY) + 0.1); // small offset to prevent z-fighting
    }

    setHazardStates(armedFlags) {
        this.hazardMeshes.forEach((m, i) => {
            const armed = armedFlags[i];
            m.material.color.setHex(armed ? 0xc23b22 : 0x8b94a3);
            m.material.opacity = armed ? 0.5 : 0.3;
        });
    }

    async loadModel(url, pos = {x:0,y:0,z:0}) {
        let gltf;
        try {
            //gltf also holds .animations, .cameras, and .asset
            gltf = await this.gltfLoader.loadAsync(url);      // load file to gltf container, gltf.scene is the model
        } catch (err) {
            console.error(`[GLB] failed to load ${url} `, err);       //url says which file failed, err gives why
            return null;
        }
        const model = gltf.scene;                               // the model is the gltf.scene
        model.position.set(pos.x, pos.y, pos.z);                // change vector3, give position to model
        this.world.add(model);                                  // add model to scene

        model.traverse(obj => {                                             //hide the markers
            if (MARKER_PREFIXES.some(p => obj.name.startsWith(p))) obj.visible = false;
        });
        // grab door art nodes so we can raise them when open (collider_doorA -> wall_doorA)
        this.doorDefs?.forEach((d, i) => {
            const artName = 'wall_' + d.name.replace('collider_', '');
            const node = model.getObjectByName(artName);
            if (node) {
                this.doorNodes[i] = node;
                this.doorBaseY[i] = node.position.y;
                this.doorTargetY[i] = node.position.y;   // start closed
            } else {
                console.warn('[door] art node not found:', artName);
            }
        });
        const box3 = new THREE.Box3().setFromObject(model);
        console.log('[GLB] min: ', box3.min, ' max: ', box3.max);
        return model;                                           // give caller loaded object
    }

    async loadCharacterTemplate(url) {
        try {
            const gltf = await this.gltfLoader.loadAsync(url);
            this.characterTemplate = gltf.scene;
            this.characterClips = gltf.animations;
            console.log('[character] clips:', gltf.animations.map(a => a.name));    //temporary
        } catch (err) {
            console.error('[character] failed to load ', url, err);
        }
    }

    addLights() {
        const skyColor = 0xffffff;
        const dirColor = 0xffffff;
        const groundColor = 0x444444;
        const sunIntensity = 1.5;
        const hemiIntensity = 2;
        const light = new THREE.HemisphereLight(skyColor, groundColor, sunIntensity);
        this.scene.add(light);

        const dirLight = new THREE.DirectionalLight(dirColor, hemiIntensity);
        dirLight.position.set(5, 10, 7);
        this.scene.add(dirLight);
    }

    reset() {
        this.scene.remove(this.world);
        this.world = new THREE.Group();
        this.scene.add(this.world);
        this.players.clear();
        this.buffers.clear();
        this.hazardMeshes = [];
        this.myId = null;
        this.selfTarget = null;
        this.doorDefs = [];
        this.doorNodes = [];
        this.doorBaseY = [];
        this.doorTargetY = [];
        this.doorLift = [];
    }

    setDoors(openFlags) {
        if (!this.doorDefs) return;
        for (let i = 0; i < this.doorDefs.length; i++) {
            const open = !!openFlags?.[i];
            this.doorTargetY[i] = this.doorBaseY[i] + (open ? this.doorLift[i] : 0);
        }
    }

    //helper function to interpolate angles correctly
    lerpAngle(a, b, t) {
        let diff = b - a;
        while (diff > Math.PI) diff -= 2 * Math.PI;
        while (diff < -Math.PI) diff += 2 * Math.PI;
        return a + diff * t;
    }

    faceMovement(container) {
        const dx = container.position.x - (container.userData.lastX ?? container.position.x);
        const dz = container.position.z - (container.userData.lastZ ?? container.position.z);
        container.userData.lastX = container.position.x;
        container.userData.lastZ = container.position.z;

        const moving = dx * dx + dz * dz > 1e-6; //check if moving, avoid jitter
        if (moving) {             //during movement, avoid jitter
            const target = Math.atan2(dx, dz) - MODEL_YAW_OFFSET; //atan2(y,x) gives angle in radians
            container.rotation.y = this.lerpAngle(container.rotation.y, target, 0.25); // 0.25 is turn speed
        }

        const actions = container.userData.actions;
        if (actions) {
            const want = moving ? 'walk' : 'idle';
            if (container.userData.anim !== want && actions[want]) {
                actions[want].reset().fadeIn(0.2).play();
                const prev = actions[container.userData.anim];
                if (prev && prev !== actions[want]) prev.fadeOut(0.2);
                container.userData.anim = want;
            }
        }
    }

    enableMouseLook() {
        this.camYaw = 0;
        this.canvas.addEventListener('click', () => this.canvas.requestPointerLock());
        document.addEventListener('mousemove', (e) => {
            if (document.pointerLockElement !== this.canvas) return;
            this.camYaw -= (e.movementX || 0) * 0.0025;        //sensitivity, also added || 0 to guard against null movementX
            console.log('camYaw', this.camYaw, 'movementX', e.movementX);
        });
    }

    getCameraYaw() {
        return Number.isFinite(this.camYaw) ? this.camYaw : 0;      //guard against NaN, return 0 if not finite
    }

    updateCamera() {
        const self = this.players.get(this.myId);
        if (!self) return;
        const dist = 6, height = 3;
        const fx = Math.sin(this.camYaw), fz = Math.cos(this.camYaw);
        this.camera.position.set(
            self.position.x - fx * dist,        //sit behind player
            self.position.y + height,
            self.position.z - fz * dist
        );
        this.camera.lookAt(self.position.x, self.position.y + 1, self.position.z);
    }


};
