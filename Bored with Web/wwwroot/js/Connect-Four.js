
//This is a temporary var; it will be removed when the server-side is implemented.
var isPlayer1Turn = true;

/**
 * Add onclick listener to all column buttons that allows the user to play a token.
 * Add onclick listener to the clear button so the user can clear the board.
 */
window.onload = function () {
	document.querySelectorAll(".c4-slot-btn").forEach(function (btn) {
		/**
		 * Searches the column the button is for, and plays a token on the highest open slot.
		 * If there aren't any open slots left after this, the button will be disabled.
		 */
		btn.onclick = function () {
			//This code will be replaced later by some real-time communication API.
			let column = parseInt(this.getAttribute("data-col"));
			let row = 0
			for (; getSlot(column, row); row++) {
				let slot = getSlot(column, row);
				if (!slot.classList.contains("c4-played")) {
					playToken(column, row);
					break;
				}
			}

			if (!getSlot(column, row + 1)) {
				btn.disabled = true;
			}
		}
	});

	//This function will be replaced later by some real-time communication API.
	document.getElementById("c4-clear-btn").onclick = clearBoard;
};

/**
 * Adds the names of the competing players to the turn indicator.
 * 
 * @param {String} player1 - The name of the first player.
 * @param {String} player2 - The name of the second player.
 */
function setCompetingPlayers(player1, player2) {
	document.getElementById("player-1").innerText = player1;
	document.getElementById("player-2").innerText = player2;
}

/**
 * Toggles the "active-player" and "text-muted" classes on the elements within the turn indicator based
 * on which player is currently being allowed a turn.
 * 
 * @param {Number} playerNumber - The integer value representing the active player.
 */
function setActivePlayer(playerNumber) {
	let activePlayer = playerNumber == 1 ? document.getElementById("player-1") : document.getElementById("player-2");
	let inactivePlayer = playerNumber == 1 ? document.getElementById("player-2") : document.getElementById("player-1");

	activePlayer.classList.add("active-player");
	activePlayer.classList.remove("text-muted");

	inactivePlayer.classList.add("text-muted");
	inactivePlayer.classList.remove("active-player");
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
 * Plays a token at the specified row and column for the specified player. If a token has been played at this
 * location previously, then the slot will be reset.
 * 
 * @param {Number} column - an integer value representing the column of the c4-board the slot will be found in.
 * @param {Number} row - an integer value representing the row of the c4-board the slot will be found in.
 * @param {Number} playerNumber - The integer value representing the player that played the token (1 for player1, 2 for player2).
 */
function playToken(column, row, playerNumber = isPlayer1Turn ? 1 : 2) {
	let slot = getSlot(column, row);
	if (slotHasToken(slot)) {
		clearSlot(slot);
	}
	slot.classList.add(`c4-slot-player-${playerNumber}`, "c4-played");
	isPlayer1Turn = !isPlayer1Turn;
	setActivePlayer(isPlayer1Turn ? 1 : 2);
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
function clearBoard() {
	document.querySelectorAll(".c4-played").forEach(clearSlot);
	document.querySelectorAll(".c4-slot-btn").forEach(function (btn) {
		btn.disabled = false;
	});
}

/**
 * Displays a message to the user in a modal dialog, and disables all column buttons.
 * 
 * @param {Number} endingType - An integer value representing how this game ended. 0 = there was a winner, 1 = a draw, 2 = forfeit, 3 = opponent left.
 * @param {String} message - The message to display to the user relating to this event.
 */
function onGameEnd(endingType, message) {
	document.querySelectorAll(".c4-slot-btn").forEach(function (btn) {
		btn.disabled = true;
	});

	//TODO: Consider changing this text based on the endingType.
	let title = "The Match is Over!";
	const modal = createModal(title, message);
	const bsModal = new bootstrap.Modal(modal);

	modal.addEventListener("hidden.bs.modal", function () {
		bsModal.dispose();
		modal.remove();
	});

	document.getElementsByTagName("body")[0].appendChild(modal);
	bsModal.show();
}

/**
 * Creates a modal dialog and returns it with the following structure:
 * 
 * <div class="modal fade" tabindex="-1" aria-labelledby="exampleModalLabel" aria-hidden="true">
 * 		<div class="modal-dialog">
 * 			<div class="modal-content">
 * 				<div class="modal-header">
 * 					<h1 class="modal-title fs-5" id="modal-label">title</h1>
 * 					<button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
 * 				</div>
 * 				<div class="modal-body">
 * 					<p>message</p>
 * 				</div>
 * 			</div>
 * 		</div>
 * 	</div>
 * 
 * @param {String} title - The title of the created modal.
 * @param {String} message - The message to display in the body of the modal.
 * @returns The created modal element.
 */
function createModal(title, message) {
	let modal = document.createElement("div");
	modal.classList.add("modal", "fade");
	modal.setAttribute("tabindex", "-1");
	modal.setAttribute("aria-labelledby", "modal-label");
	modal.setAttribute("aria-hidden", "true");
	/* div class="modal fade" */{
		let modalDialog = document.createElement("div");
		modalDialog.classList.add("modal-dialog");
		/* div class="modal-dialog" */{
			let modalContent = document.createElement("div");
			modalContent.classList.add("modal-content");
			/* div class="modal-content" */{
				let modalHeader = document.createElement("div");
				modalHeader.classList.add("modal-header");
				/* div class="modal-header" */{
					let modalTitle = document.createElement("h1");
					modalTitle.classList.add("modal-title", "fs-5");
					modalTitle.setAttribute("id", "modal-label");
					modalTitle.innerText = title;
					modalHeader.appendChild(modalTitle);

					let modalHeaderButton = document.createElement("button");
					modalHeaderButton.classList.add("btn-close");
					modalHeaderButton.setAttribute("type", "button");
					modalHeaderButton.setAttribute("data-bs-dismiss", "modal");
					modalHeaderButton.setAttribute("aria-label", "Close");
					modalHeader.appendChild(modalHeaderButton);
				}
				modalContent.appendChild(modalHeader);

				let modalBody = document.createElement("div");
				modalBody.classList.add("modal-body");
				/* div class="modal-body" */ {
					let messageParagraph = document.createElement("p");
					messageParagraph.innerText = message;
					modalBody.appendChild(messageParagraph);
				}
				modalContent.appendChild(modalBody);
			}
			modalDialog.appendChild(modalContent);
		}
		modal.appendChild(modalDialog);
	}

	return modal;
}
