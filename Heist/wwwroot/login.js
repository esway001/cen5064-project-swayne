
const submitButton = document.getElementById("submitButton");

submitButton.addEventListener('click', function () {
    const playerName = document.getElementById("username").value.trim();
    const roomCode = document.getElementById("roomCode").value.trim().toUpperCase();
    if (!playerName || !roomCode) return;
    sessionStorage.setItem("playerName", playerName); //keep username
    sessionStorage.setItem("roomCode", roomCode);
    window.location.href = "index.html";
});