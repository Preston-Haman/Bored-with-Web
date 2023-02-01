
/**
 * The SignalR connection for this game.
 */
var connectFourConnection;

/**
 * The number of tokens that have been played during this match.
 */
let tokensPlayed = 0;

/**
 * Add onclick listener to all column buttons that allows the user to play a token.
 * Add onclick listener to the clear button so the user can clear the board.
 */
window.addEventListener("load", function () {
	defaultEnableGameInput(false);
	initConnectFourConnection();

	//Hook up SE sources.
	document.querySelectorAll(".game-input").forEach(function (btn) {
		btn.onclick = function () {
			if (isUserTurn) {
				let column = parseInt(this.getAttribute("data-col"));
				connectFourConnection.invoke(SE_CONNECT_FOUR_PLAY_TOKEN, column).catch(function (err) {
					return console.error(err.toString());
				});
			}
		}
	});
});

/**
 * Creates, associates client events with, and starts the SignalR connection for this game.
 */
function initConnectFourConnection() {
	connectFourConnection = CONNECT_FOUR_CONNECTION_BUILDER.build();

	useDefaultMultiplayerGameConnectionEvents(connectFourConnection, boardCleared, function /*userCanForfeitOrReMatch*/() { return tokensPlayed > 0; });

	connectFourConnection.on(CE_CONNECT_FOUR_JOINED, joined);
	connectFourConnection.on(CE_CONNECT_FOUR_TOKEN_PLAYED, tokenPlayed);

	connectFourConnection.start().catch(function (err) {
		defaultEnableGameInput(false);
		createAndSendMessage("There was an error; please refresh the page.", "danger", false);
		return console.error(err.toString());
	});
}

/**
 * Finds the slot on the c4-board that is represented by the given row and column values.
 * 
 * @param {Number} column - an integer value representing the column of the c4-board this slot will be found in.
 * @param {Number} row - an integer value representing the row of the c4-board this slot will be found in.
 * @returns Returns the Element with the id of `slot-x${column}-y${row}`
 */
function getSlot(column, row) {
	return document.getElementById(`slot-x${column}-y${row}`);
}

/**
 * Checks the classList of the given slot element and returns true if it contains "c4-played"; false otherwise.
 * 
 * @param {Element} slot - The element representing a c4-board slot to check.
 */
function slotHasToken(slot) {
	return slot.classList.contains("c4-played");
}

/**
 * Removes the classes "c4-slot-player-1", "c4-slot-player-2", "c4-played", from the classList of the given slot element.
 * 
 * @param {Element} slot - The element representing a c4-board slot to clear.
 */
function clearSlot(slot) {
	slot.classList.remove("c4-slot-player-1", "c4-slot-player-2", "c4-played");
}

/**
 * Retrieves all elements marked with the class "c4-played", and clears them. Then,
 * if any of the column buttons on the board have been disabled, this will re-enable them.
 */
function clearAllSlots() {
	document.querySelectorAll(".c4-played").forEach(clearSlot);
	defaultEnableGameInput();
}

/**
 * Updates the state of the game so the representation displayed to the user matches what
 * has been given by the server.
 * 
 * The game board is laid out with the 0 index at the bottom left of the board.
 * As an example, with a 3x3 board, the indices would look like this:
 * 
 * 		[6][7][8]
 * 		[3][4][5]
 * 		[0][1][2]
 * 
 * @param {String} board - A binary representation of the game board that's been converted into a Base64 String.
 */
function joined(board) {
	clearAllSlots();

	board = Uint8Array.from(window.atob(board), b => b.charCodeAt(0));

	for (let y = 0; y < CONNECT_FOUR_ROWS; y++) {
		let offset = CONNECT_FOUR_COLUMNS * y;
		for (let x = 0; x < CONNECT_FOUR_COLUMNS; x++) {
			let tokenNumber = board[offset + x];
			if (tokenNumber) {
				getSlot(x, y).classList.add(`c4-slot-player-${tokenNumber}`, "c4-played");
			}
		}
	}
}

/**
 * Plays a token at the specified row and column for the specified player. If a token has been played at this
 * location previously, then the slot will be reset. If the column has been filled, the button to play a token
 * in that column is disabled.
 * 
 * @param {Number} playerNumber - The integer value representing the player that played the token (1 for player1, 2 for player2).
 * @param {Number} row - an integer value representing the row of the c4-board the slot will be found in.
 * @param {Number} column - an integer value representing the column of the c4-board the slot will be found in.
 */
function tokenPlayed(playerNumber, row, column) {
	tokensPlayed++;
	let slot = getSlot(column, row);
	if (slotHasToken(slot)) {
		clearSlot(slot);
	}

	slot.classList.add(`c4-slot-player-${playerNumber}`, "c4-played");
	if (!getSlot(column, row + 1)) {
		document.getElementById(`btn-col-${column}`).disabled = true;
	}
}

/**
 * Resets the board, the number of tokens played, and sets matchIsActive to true.
 */
function boardCleared() {
	tokensPlayed = 0;
	clearAllSlots();
}
