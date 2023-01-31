
/**
 * The SignalR connection being used for the game; this is provided by specific game implementations
 * by calling useDefaultMultiplayerGameConnectionEvents.
 */
let defaultMultiplayerGameConnection;

/**
 * A function provided by specific game implementations when calling useDefaultMultiplayerGameConnectionEvents
 * that lets that implementation reset the state of the board without worrying about the source connection event
 * triggering the action.
 */
let gameResetCallbackFunction;

/**
 * Whether or not it's currently the user's turn.
 */
var isUserTurn = false;

/**
 * The number representing the user in this game.
 */
var ourPlayerNumber = 0;

/**
 * If the user has issued a rematch to their opponent or not.
 */
let userIssuedRematch = false;

/**
 * If the opponent has issued a rematch to the user or not.
 */
let opponentIssuedRematch = false;

/**
 * Associates default implementations for IMultiplayerGameClient with the given connection.
 * 
 * Use of the default implementations implies that certain conditions are met for the given webpage.
 * All of these conditions are optional; and will fail silently.
 * 
 * @param {HubConnection} connection - The connection to apply the default implementations to.
 * @param {Function} gameResetCallback - A callback function, accepting no parameters and returning void,
 * that resets the state of the game to the starting conditions.
 * @param {Function} userCanForfeitOrReMatch - A function, accepting no parameters and returning a boolean.
 * If false is returned, inputs made to the button with id "game-forfeit-and-rematch" will be ignored;
 * however, if true is returned, the corresponding Server Event will be triggered by the input.
 */
function useDefaultMultiplayerGameConnectionEvents(connection, gameResetCallback, userCanForfeitOrReMatch) {
	defaultMultiplayerGameConnection = connection;
	gameResetCallbackFunction = gameResetCallback;

	//Client Events -- Things that are triggered by the server, but run on the client.
	connection.on(CE_MULTIPLAYER_GAME_SET_USER_PLAYER_NUMBER, setUserPlayerNumber);
	connection.on(CE_MULTIPLAYER_GAME_PLAYER_CONNECTED, playerConnected);
	connection.on(CE_MULTIPLAYER_GAME_PLAYER_DISCONNECTED, playerDisconnected);
	connection.on(CE_MULTIPLAYER_GAME_PLAYER_FORFEITED, playerForfeited);
	connection.on(CE_MULTIPLAYER_GAME_SPECTATOR_CONNECTED, spectatorConnected);
	connection.on(CE_MULTIPLAYER_GAME_SPECTATOR_DISCONNECTED, spectatorDisconnected);
	connection.on(CE_MULTIPLAYER_GAME_UPDATE_VISIBlE_PLAYERS, updateVisiblePlayers);
	connection.on(CE_MULTIPLAYER_GAME_START_GAME, startGame);
	connection.on(CE_MULTIPLAYER_GAME_SET_PLAYER_TURN, setPlayerTurn);
	connection.on(CE_MULTIPLAYER_GAME_MATCH_ENDED, matchEnded);
	connection.on(CE_MULTIPLAYER_GAME_REMATCH, rematch);
	connection.on(CE_MULTIPLAYER_GAME_RESET_GAME, resetGame);
	connection.on(CE_MULTIPLAYER_GAME_END_GAME, endGame);

	//Server Events -- Things the user triggers to run on the server.
	//SE_MULTIPLAYER_GAME_FORFEIT_AND_REMATCH
	let forfeitAndRematchButton = document.getElementById("game-forfeit-and-rematch");
	if (forfeitAndRematchButton) {
		forfeitAndRematchButton.onclick = function () {
			//There are three things that can happen here...
			//The user can forfeit the match and simultaneously issue a rematch.
			//The user can attempt to rematch their opponent for a match that's already over.
			//The user can accept a rematch from their opponent.
			//Regardless of what's going on, we're only triggering the rematch Server Event.
			if (userCanForfeitOrReMatch()) {
				forfeitAndRematchButton.disabled = true;
				if (!opponentIssuedRematch) {
					userIssuedRematch = true;
				}

				defaultMultiplayerGameConnection.invoke(SE_MULTIPLAYER_GAME_FORFEIT_AND_REMATCH).then(function () {
					if (!opponentIssuedRematch) {
						//If the opponent already issued a rematch, then we are just accepting it and don't need a message.
						//Otherwise, we are issuing a rematch to the opponent; and, if the match is active, forfeiting it.
						createAndSendMessage("You've requested a rematch! Please wait for the other player(s).");
					}
					forfeitAndRematchButton.disabled = false;
				}).catch(function (err) {
					userIssuedRematch = false;
					forfeitAndRematchButton.disabled = false;
					return console.error(err.toString());
				});
			}
		};
	}
}

/**
 * Sets the disabled value of all game-input elements to true.
 * 
 * This is meant to be called under the following conditions:
 * 	The existence of elements containing the class "game-input" supporting the disabled attribute.
 * 
 * @param {Boolean} enabled - Whether or not the input should be enabled; the default value is true.
 */
function defaultEnableGameInput(enabled = true) {
	document.querySelectorAll("game-input").forEach(function (input) {
		input.disabled = !enabled;
	});
}

/**
 * Sets the value of ourPlayerNumber to reflect the number on the server side.
 * 
 * @param {Number} userPlayerNumber - The numeric identifier of the user for the duration of the game.
 */
function setUserPlayerNumber(userPlayerNumber) {
	ourPlayerNumber = userPlayerNumber;
}

/**
 * If an element for the player name exists, the player's name is set as the player name given to the method.
 * If a messages element exists, a dismissible message is displayed within it.
 * 
 * This is meant to handle the following event, under the following conditions:
 * CE_MULTIPLAYER_GAME_PLAYER_CONNECTED:
 * 	The existence of elements for the player names, with an id of "player-x"; where "x" is the player's number for the game.
 * 	The existence of an element with the id of "messages" that can have bootstrap alerts appended to it.
 * 
 * @param {String} playerName - The name of the connecting player.
 * @param {Number} playerNumber - The numeric identifier of the player; this is unique to this game only.
 */
function playerConnected(playerName, playerNumber) {
	let playerId = `player-${playerNumber}`;
	let playerNameElement = document.getElementById(playerId);
	if (playerNameElement) {
		playerNameElement.innerText = playerName;
	}

	createAndSendMessage(`${playerName} connected.`);
}

/**
 * If an element for the player name exists and the timeoutInSeconds is zero, the player's name is set as the player
 * name given to the method, with " {DC'd}" appended. If a messages element exists, a dismissible message is displayed within it.
 * 
 * This is meant to handle the following event, under the following conditions:
 * CE_MULTIPLAYER_GAME_PLAYER_DISCONNECTED:
 * 	The existence of elements for the player names, with an id of "player-x"; where "x" is the player's number for the game.
 * 	The existence of an element with the id of "messages" that can have bootstrap alerts appended to it.
 * 
 * @param {String} playerName - The name of the connecting player.
 * @param {Number} playerNumber - The numeric identifier of the player; this is unique to this game only.
 * @param {Number} timeoutInSeconds - The length of time the game will wait for the disconnected player to rejoin.
 */
function playerDisconnected(playerName, playerNumber, timeoutInSeconds) {
	let playerId = `player-${playerNumber}`;
	let playerNameElement = document.getElementById(playerId);
	if (playerNameElement && timeoutInSeconds > 0) {
		playerNameElement.innerText = `${playerName} {DC'd}`;
	}

	if (timeoutInSeconds > 0) {
		createAndSendMessage(`${playerName} disconnected; they may try to reconnect for ${timeoutInSeconds}s before automatically forfeiting the game.`, "danger");
	} else {
		createAndSendMessage(`${playerName} disconnected.`, "danger");
	}
}

/**
 * If an element for the player name exists, the player's name is set as the player name given to the method, with " {Quit}" appended.
 * If a messages element exists, a dismissible message is displayed within it.
 * 
 * This is meant to handle the following event, under the following conditions:
 * CE_MULTIPLAYER_GAME_PLAYER_FORFEITED:
 * 	The existence of elements for the player names, with an id of "player-x"; where "x" is the player's number for the game.
 * 	The existence of an element with the id of "messages" that can have bootstrap alerts appended to it.
 * 
 * @param {String} playerName - The name of the connecting player.
 * @param {Number} playerNumber - The numeric identifier of the player; this is unique to this game only.
 * @param {Boolean} isConnectionTimeout - If the forfeit was caused by a disconnection or not.
 */
function playerForfeited(playerName, playerNumber, isConnectionTimeout) {
	if (isConnectionTimeout) {
		createAndSendMessage(`${playerName}'s connection has timed out; they have now automatically forfeited the game.`, "danger");
	} else {
		createAndSendMessage(`${playerName} has forfeited the game and left.`, "danger");
	}

	document.getElementById(`player-${playerNumber}`).innerText = `${playerName} {Quit}`;
}

/**
 * Adds the given name to the list of spectators, if it exists.
 * 
 * This is meant to handle the following event, under the following conditions:
 * CE_MULTIPLAYER_GAME_SPECTATOR_CONNECTED:
 * 	The existence of a <ul id="spectator-list">
 * 		This list will be used to display the spectators of the current game;
 * 		each spectator <li> will be given an id of the spectator's name.
 * 
 * @param {String} spectatorName - The name of the spectating user.
 */
function spectatorConnected(spectatorName) {
	let spectatorList = document.getElementById("spectator-list");
	if (spectatorList) {
		let li = document.createElement("li");
		li.innerText = spectatorName;
		li.setAttribute("id", spectatorName);
		spectatorList.appendChild(li);
	}
}

/**
 * Removes the specified spectator from the list of spectators.
 * 
 * This is meant to handle the following event, under the following conditions:
 * CE_MULTIPLAYER_GAME_SPECTATOR_DISCONNECTED:
 * 	The existence of a <ul id="spectator-list"> with an <li id="spectatorName">.
 * 
 * @param {String} spectatorName - The name of the spectating user.
 */
function spectatorDisconnected(spectatorName) {
	let spectatorList = document.getElementById("spectator-list");
	if (spectatorList) {
		spectatorList.getElementById(spectatorName).remove();
	}
}

/**
 * Updates the list of players and spectators, if they exist on the webpage.
 * 
 * This is meant to handle the following event, under the following conditions:
 * CE_MULTIPLAYER_GAME_UPDATE_VISIBlE_PLAYERS:
 * 	The existence of a <ul id="spectator-list">
 * 		This list will be used to display the spectators of the current game;
 * 		each spectator <li> will be given an id of the spectator's name.
 * 
 * 	The existence of elements for the player names, with an id of "player-x"; where "x" is the player's number for the game.
 * 	OR
 * 	The existence of a <ul class="player-list">
 * 		This list will be used to display the players in the current game;
 * 		and each player <li> will be given an id of "player-x"; where "x" is the player's number for the game.
 * 
 * @param {String[]} players - The names of the players in the game, the indices match with playerNumbers.
 * @param {Number[]} playerNumbers - The numeric representation of each player listed in players.
 * @param {String[]} spectators - The names of the users spectating the game.
 */
function updateVisiblePlayers(players, playerNumbers, spectators) {
	//Get player-list; it's a <ul>
	let playerList = document.querySelector(".player-list");
	if (playerList) {
		//Clear the list out first, if it exists.
		playerList.replaceChildren();
	}

	players.forEach(function (player, index) {
		//Elements with "player-x" id are meant for the player's name.
		let playerId = `player-${playerNumbers[index]}`;
		let playerNameElement = document.getElementById(playerId);
		if (playerNameElement) {
			playerNameElement.innerText = player;
		} else if (playerList) {
			li = document.createElement("li");
			li.innerText = player;
			li.setAttribute("id", playerId);
			playerList.appendChild(li);
		}
	});

	//Get spectator list; it's a <ul>
	let spectatorList = document.getElementById("spectator-list");
	if (spectatorList) {
		//Clear the list out first, if it exists.
		spectatorList.replaceChildren();
	}

	if (spectatorList) {
		spectators.forEach(function (spectator) {
			let li = document.createElement("li");
			li.innerText = spectator;
			li.setAttribute("id", spectator);
			spectatorList.appendChild(li);
		});
	}
}

/**
 * Sets the disabled value of all game-input elements to false.
 * 
 * This is meant to handle the following event, under the following conditions:
 * CE_MULTIPLAYER_GAME_START_GAME:
 * 	The existence of elements containing the class "game-input" supporting the disabled attribute.
 */
function startGame() {
	defaultEnableGameInput();
}

/**
 * Updates the turn indicator, if it exists, to display the player with the specified player number as
 * the active player. This is done by iterating the children nodes and adding/removing the classes
 * "active-player", and "text-muted". This method also updates the value for the global variable
 * isUserTurn based on the value of isUs.
 * 
 * This is meant to handle the following event, under the following conditions:
 * CE_MULTIPLAYER_GAME_SET_PLAYER_TURN:
 * 	The existence of an element with an id of "turn-indicator" which is solely for elements
 * 	 representing the players of the game ordered by their player number.
 * 
 * @param {Number} playerNumber - The numeric identifier of the player; this is unique to this game only.
 */
function setPlayerTurn(playerNumber) {
	isUserTurn = ourPlayerNumber == playerNumber;

	let turnIndicator = document.getElementById("turn-indicator");
	if (turnIndicator) {
		for (let i = 0; i < turnIndicator.children.length; i++) {
			let player = turnIndicator.children.item(i);
			let currentPlayerNumber = i + 1;

			if (currentPlayerNumber == playerNumber) {
				player.classList.add("active-player");
				player.classList.remove("text-muted");
			} else {
				player.classList.add("text-muted");
				player.classList.remove("active-player");
			}
		}
	}
}

/**
 * Informs the user that the specified player has forfeited the match.
 * 
 * This is meant to handle the following event, under the following conditions:
 * CE_MULTIPLAYER_GAME_PLAYER_FORFEITED_MATCH:
 * 	The existence of an element with the id of "messages" that can have bootstrap alerts appended to it.
 * 
 * @param {String} playerName - The name of the forfeiting player.
 * @param {Number} playerNumber - The number representing the player that forfeited the match.
 */
function playerForfeitedMatch(playerName, playerNumber) {
	createAndSendMessage(`${playerName} has forfeited the current match.`, "danger");
	document.getElementById(`player-${playerNumber}`).innerText = `${playerName} {Forfeit}`;
}

/**
 * Informs the user that the match has ended and how. This method also sets the disabled value of all game-input elements to true.
 * 
 * This is meant to handle the following event, under the following conditions:
 * CE_MULTIPLAYER_GAME_MATCH_ENDED:
 * 	The existence of elements containing the class "game-input" supporting the disabled attribute.
 * 	The specific game implementation only supporting a single winning player.
 * 
 * @param {Number} winningPlayerNumber - The player number representing the winning player; or zero, if there was no winner.
 */
function matchEnded(winningPlayerNumber) {
	defaultEnableGameInput(false);

	//TODO: Consider changing this text based on how the match ends.
	let title = "The Match is Over!";
	let modal;
	if (winningPlayerNumber) {
		let winningPlayer = document.getElementById(`player-${winningPlayerNumber}`).innerText;
		modal = createModal(title, `${winningPlayer} wins!`);
	} else {
		modal = createModal(title, "The match has ended in a stalemate!");
	}
	const bsModal = new bootstrap.Modal(modal);

	modal.addEventListener("hidden.bs.modal", function () {
		bsModal.dispose();
		modal.remove();
	});

	document.getElementsByTagName("body")[0].appendChild(modal);
	bsModal.show();
}

/**
 * Displays a message to the user that their opponent has challenged them to a rematch.
 * Alternatively, if this user issued a rematch challenge already, then the message
 * is altered to claim the other player accepted the rematch.
 * 
 * This is meant to handle the following event, under the following conditions:
 * CE_MULTIPLAYER_GAME_REMATCH:
 * 	The existence of an element with the id of "messages" that can have bootstrap alerts appended to it.
 * 	The specific game implementation only supporting two players.
 */
function rematch() {
	if (userIssuedRematch) {
		createAndSendMessage("Your opponent accepted your rematch.");
	} else {
		opponentIssuedRematch = true;
		createAndSendMessage("You were challenged to a rematch! Clear the board when you are ready.");
	}
}

/**
 * Sets userIssuedRematch and opponentIssuedRematch to false, then calls gameResetCallbackFunction.
 * 
 * This is meant to handle the following event, under the following conditions:
 * CE_MULTIPLAYER_GAME_RESET_GAME:
 * 	The existence of gameResetCallbackFunction, specified via useDefaultMultiplayerGameConnectionEvents.
 */
function resetGame() {
	userIssuedRematch = false;
	opponentIssuedRematch = false;
	gameResetCallbackFunction();
}

/**
 * Sets the disabled value of all game-input elements to true, and informs the user to leave the page.
 * 
 * This is meant to handle the following event, under the following conditions:
 * CE_MULTIPLAYER_GAME_END_GAME:
 * 	The existence of elements containing the class "game-input" supporting the disabled attribute.
 * 	The existence of an element with the id of "messages" that can have bootstrap alerts appended to it.
 */
function endGame() {
	defaultEnableGameInput(false);
	createAndSendMessage("The game has now ended; please return to the lobby to continue playing.", "primary", false);

	if (defaultMultiplayerGameConnection) {
		defaultMultiplayerGameConnection.stop();
	}
}
