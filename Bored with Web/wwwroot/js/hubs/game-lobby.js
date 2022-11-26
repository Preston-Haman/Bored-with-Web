
var lobbyConnection;

window.addEventListener("load", function () {
	enableLobbyInput(false);

	initLobbyConnection();

	//Hook up SE sources
	let readyButton = document.getElementById("ready-button");
	readyButton.onclick = function () {
		readyButton.disabled = true;

		lobbyConnection.invoke(SE_READY).catch(function (err) {
			readyButton.disabled = false;
			return console.error(err.toString());
		});

		setTimeout(function () {
			lobbyConnection.invoke(SE_NOT_READY).catch(function (err) {
				return console.error(err.toString());
			});

			readyButton.disabled = false;
		}, 5 * 60 * 1000); //5 minute delay
	}
});

function initLobbyConnection() {
	lobbyConnection = LOBBY_CONNECTION_BUILDER.build();

	lobbyConnection.on(CE_LOBBY_GAME_CREATED, gameCreated);
	lobbyConnection.on(CE_LOBBY_GAME_ENDED, gameEnded);
	lobbyConnection.on(CE_LOBBY_PLAYER_CONNECTED, playerConnected);
	lobbyConnection.on(CE_LOBBY_PLAYER_IS_READY, playerIsReady);
	lobbyConnection.on(CE_LOBBY_PLAYER_DISCONNECTED, playerDisconnected);
	lobbyConnection.on(CE_LOBBY_UPDATE_GAMES, updateGames);
	lobbyConnection.on(CE_LOBBY_UPDATE_PLAYERS, updatePlayers);
	lobbyConnection.on(CE_LOBBY_SERVER_REQUESTS_DISCONNECT, serverRequestsDisconnect);

	lobbyConnection.start().then(function () {
		enableLobbyInput(true);
		setConnectionStatus(true);
	}).catch(function (err) {
		enableLobbyInput(false);
		createAndSendMessage("There was an error; please refresh the page.", "danger", false);
		setConnectionStatus(false);
		return console.error(err.toString());
	});
}

/**
 * Enables or disables the lobby-related inputs.
 * 
 * @param {Boolean} enabled - If the lobby inputs should be enabled or not.
 */
function enableLobbyInput(enabled) {
	let readyButton = document.getElementById("ready-button");
	readyButton.disabled = !enabled;

	let disconnectButton = document.getElementById("lobby-disconnect");
	disconnectButton.disabled = !enabled;
}

/**
 * Adjusts the connection indicator element's inner text to reflect the current status of the connection,
 * and applies CSS classes to change the background colour.
 * 
 * @param {Boolean} connected - If the connection is connected or not.
 * @param {Boolean} ready - If the user is ready or not.
 */
function setConnectionStatus(connected, ready = false) {
	let connectionIndicator = document.getElementById("connection-indicator");

	//Clear classes
	connectionIndicator.classList.remove("lobby-connection-connected");
	connectionIndicator.classList.remove("lobby-connection-ready");
	connectionIndicator.classList.remove("lobby-connection-disconnected");

	if (connected && ready) {
		connectionIndicator.classList.add("lobby-connection-ready");
		connectionIndicator.innerText = "Connected -- Ready";
	} else if (connected) {
		connectionIndicator.classList.add("lobby-connection-connected");
		connectionIndicator.innerText = "Connected -- Not Ready";
	} else {
		connectionIndicator.classList.add("lobby-connection-disconnected");
		connectionIndicator.innerText = "Not connected -- Error";
	}
}

/**
 * Adds a game li to the given ul.
 * 
 * @param {HTMLUListElement} gameList - The list to add to.
 * @param {String} gameId - The gameId to add to the list.
 */
function addGameToList(gameList, gameId) {
	let gameRouteId = window.location.pathname.substring(window.location.pathname.lastIndexOf("/") + 1);

	let gameLi = document.createElement("li");
	gameLi.innerText = `${gameRouteId}#${gameId}`;
	gameLi.setAttribute("id", gameId);
	gameLi.setAttribute("role", "button");
	gameLi.onclick = function () {
		//TODO: Allow the player to spectate these games.
	}

	gameList.appendChild(gameLi);
}

/**
 * Adds a player to the given ul.
 * 
 * @param {HTMLUListElement} playerList - The list to add to.
 * @param {String} playerName - The name of the player to add to the list.
 */
function addPlayerToList(playerList, playerName, isReady = false) {
	let playerLi = document.createElement("li");
	playerLi.innerText = playerName;
	playerLi.setAttribute("id", playerName);
	if (playerName == USERNAME) {
		playerLi.classList.add("lobby-active-user");
	}

	if (!isReady) {
		playerLi.classList.add("lobby-not-ready");
	}

	playerList.appendChild(playerLi);
}

/**
 * Adds a game to the lobby's list of games in progress.
 * 
 * @param {String[]} playerNames - The names of the users in the game.
 * @param {String} gameId - A string that represents the ongoing game.
 * @param {String} gameRouteId - The name of the game as it appears in the website url.
 */
function gameCreated(playerNames, gameId, gameRouteId) {
	for (let i = 0; i < playerNames.length; i++) {
		if (playerNames[i] == USERNAME) {
			//Redirect user to `/Games/Play/${gameRouteId}?game=${gameId}`
			window.location.assign(`${window.location.origin}/Games/Play/${gameRouteId}?game=${gameId}`);
			break; //Probably not needed...
		}
	}

	let gameList = document.querySelector("#in-progress > .progress-list");
	addGameToList(gameList, gameId);
}

/**
 * Removes a game from the lobby's list of games in progress,
 * 
 * @param {String} gameId - A string that represents the game that just ended.
 */
function gameEnded(gameId) {
	document.getElementById(gameId).remove();
}

/**
 * Adds a player to the lobby's list of connected players.
 * 
 * @param {String} playerName - The name of the player that joined the lobby.
 */
function playerConnected(playerName) {
	let playerList = document.querySelector("#lobby-players > .player-list");
	addPlayerToList(playerList, playerName);
}

/**
 * Finds the given player in the lobby's list of connected players, and marks them
 * with a CSS class of "lobby-not-ready" if isReady is false.
 * 
 * @param {String} playerName - The name of the player whose ready state has changed.
 * @param {Boolean} isReady - If the player is ready (true), or no longer ready.
 */
function playerIsReady(playerName, isReady) {
	if (playerName == USERNAME) {
		setConnectionStatus(true, isReady);
	}

	if (isReady) {
		document.getElementById(playerName).classList.remove("lobby-not-ready");
	} else {
		document.getElementById(playerName).classList.add("lobby-not-ready");
	}
}

/**
 * Removes a player from the lobby's list of connected players.
 * 
 * @param {String} playerName - The name of the player that lost connection.
 */
function playerDisconnected(playerName) {
	document.getElementById(playerName).remove();
}

/**
 * Adds a set of games to the lobby's list of games in progress.
 * 
 * @param {String[]} gameIds - An array of strings that represent the ongoing games for this lobby.
 */
function updateGames(gameIds) {
	let gameList = document.querySelector("#in-progress > .progress-list");

	gameIds.forEach(function (gameId) {
		addGameToList(gameList, gameId);
	});
}

/**
 * Adds a set of players to the lobby's list of players.
 * 
 * @param {String[]} playerNames - The names of all the players in the lobby.
 * @param {Boolean[]} readyplayers - The ready status of the specified players.
 */
function updatePlayers(playerNames, readyplayers) {
	let playerList = document.querySelector("#lobby-players > .player-list");

	for (let i = 0; i < playerNames.length; i++) {
		let playerName = playerNames[i];
		addPlayerToList(playerList, playerName, readyplayers[i]);
	}
}

/**
 * Closes the current connection and informs the user to refresh the page.
 */
function serverRequestsDisconnect() {
	createAndSendMessage("The server has requested that you disconnect; please refresh the page when you are ready to continue.", "danger", false);
	setConnectionStatus(false, false);
	lobbyConnection.stop();
}
