
const submitButton = document.getElementById("submitButton");

submitButton.addEventListener('click', function () {
    const playerName = document.getElementById("username").value.trim();
    if (!playerName) return;
    sessionStorage.setItem("playerName", playerName); //keep username
    window.location.href = "index.html";
});