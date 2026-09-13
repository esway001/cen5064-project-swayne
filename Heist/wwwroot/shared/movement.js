import { PLAYER_RADIUS } from './level.js';

//simulated client-side clone of server Step, predict + reconcile 
export const MOVE_SPEED = 5;
export const STEP_DT = 1 / 20;

function hitsWall(x, z, w) {
    const cx = Math.max(w.minX, Math.min(x, w.maxX)); //closest point
    const cz = Math.max(w.minZ, Math.min(z, w.maxZ));
    const dx = x - cx, dz = z - cz;
    return dx * dx + dz * dz < PLAYER_RADIUS * PLAYER_RADIUS;
}

function hitsAny(x, z, walls) {
    for (const w of walls) if (hitsWall(x, z, w)) return true;
    return false;
}

//each step will be 5x 0.05 = 0.25, and diagonals is sqrt calc to 0.177 
//IF CHANGE SPEED ON SERVER, CHANGE HERE TOO (CHECK GAMEREGISTRY.CS -> STEP)
export function applyInput(pos, input, walls, dt = STEP_DT) {
    let ix = input.x, iz = input.z;
    const len = Math.hypot(ix, iz);
    if (len > 0) { ix /= len; iz /= len; }

    //collision
    const vx = ix * MOVE_SPEED * dt;
    const vz = iz * MOVE_SPEED * dt;

    let x = pos.x, z = pos.z;
    const nx = x + vx;
    if(!hitsAny(nx, z, walls)) x = nx; //move on x only if no hit
    const nz = z + vz;
    if(!hitsAny(x, nz, walls)) z = nz; //move on z only if no hit

    //return { x: pos.x + ix * MOVE_SPEED * dt, z: pos.z + iz * MOVE_SPEED * dt };
    return { x, z };
}