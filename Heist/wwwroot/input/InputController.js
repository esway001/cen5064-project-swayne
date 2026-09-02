export class InputController {
    constructor() {
        this.keys = new Set();
        window.addEventListener('keydown', (e) => this.keys.add(e.code));
        window.addEventListener('keyup', (e) => this.keys.delete(e.code));
    }

    getIntent() {
        let x = 0, z = 0;
        if (this.keys.has('KeyW')) z -= 1;
        if (this.keys.has('KeyS')) z += 1;
        if (this.keys.has('KeyA')) x -= 1;
        if (this.keys.has('KeyD')) x += 1;
        return { x, z };

    }
}