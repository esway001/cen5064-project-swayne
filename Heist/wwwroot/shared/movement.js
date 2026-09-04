//simulated client-side clone of server Step, predict + reconcile 
export const MOVE_SPEED = 5;
export const STEP_DT = 1 / 20;

//each step will be 5x 0.05 = 0.25, and diagonals is sqrt calc to 0.177 
//IF CHANGE SPEED ON SERVER, CHANGE HERE TOO (CHECK GAMEREGISTRY.CS -> STEP)
export function applyInput(pos, input, dt = STEP_DT) {
    let ix = input.x, iz = input.z;
    const len = Math.hypot(ix, iz);
    if (len > 0) { ix /= len; iz /= len; }
    return { x: pos.x + ix * MOVE_SPEED * dt, z: pos.z + iz * MOVE_SPEED * dt };
}