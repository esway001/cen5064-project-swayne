//simple physics, levels are boxes players are "circles"
// these walls are just test data, should be server side only for now..
export const WALLS = [
    { minX: -1, maxX: 1, minZ: 3, maxZ: 4 },
    { minX: 4, maxX: 5, minZ: -2, maxZ: 6 }
];

// Define the prize, server is gonna check if player is in this area, if yes player wins
export const OBJECTIVE = { x: 8, z: 0, radius: 0.75 };

export const PLAYER_RADIUS = 0.5;

export const HAZARDS = [
    //armed and safe state comes from the server each snapshot, just drawing the box here
    {minX: 3, maxX: 4, minZ: -3, maxZ: 3},

]