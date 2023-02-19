
/**
 * The SignalR connection for this game.
 */
var checkersConnection;

/**
 * Whether or not any moves have been made during this match.
 */
let tokensHaveBeenPlayed = false;

/**
 * The current move set that the player is building. The first element is the boardIndex
 * of the token being moved; and, subsequent elements are the boardIndices that are being
 * moved to.
 * 
 * @type {Number[]}
 */
let userCurrentMoves = [];

/**
 * 1. Disable input until the opponent joins.
 * 2. Initialize the connection.
 * 3. Hook up the button for forfeit/rematch as an SE source.
 */
window.addEventListener("load", function () {
	boardReset();
	enableCheckersInput(false);

	initCheckersConnection();
});

/**
 * Creates, associates client events with, and starts the SignalR connection for this game.
 */
function initCheckersConnection() {
	checkersConnection = CHECKERS_CONNECTION_BUILDER.build();

	//Can use all default MultiplayerGame implementations except for CE_MULTIPLAYER_GAME_SET_PLAYER_TURN
	useDefaultMultiplayerGameConnectionEvents(checkersConnection, boardReset, function /*userCanForfeitOrReMatch*/() { return tokensHaveBeenPlayed; });
	checkersConnection.off(CE_MULTIPLAYER_GAME_SET_PLAYER_TURN);
	checkersConnection.on(CE_MULTIPLAYER_GAME_SET_PLAYER_TURN, checkersSetPlayerTurn);

	checkersConnection.on(CE_CHECKERS_JOINED, joined);
	checkersConnection.on(CE_CHECKERS_TOKEN_PLAYED, tokenPlayed);
	checkersConnection.on(CE_CHECKERS_TOKEN_KINGED, tokenKinged);

	checkersConnection.start().catch(function (err) {
		enableCheckersInput(false);
		createAndSendMessage("There was an error; please refresh the page.", "danger", false);
		return console.error(err.toString());
	});
}

/**
 * Retrieves the element at the specified board coordinates and returns it.
 * If it does not exist, undefined is returned.
 * 
 * @param {Number} x - The x-coordinate of the slot to retrieve.
 * @param {Number} y - The y-coordinate of the slot to retrieve.
 * 
 * @returns {HTMLElement} - The element, likely a div, that represents the board slot at the specified
 * coordinates, if it exists.
 */
function getSlotElement(x, y) {
	let highestCoordinate = CHECKERS_BOARD_SIZE - 1;
	if (x < 0 || x > highestCoordinate) return undefined;
	if (y < 0 || y > highestCoordinate) return undefined;

	return document.getElementById(`slot-x${x}-y${y}`);
}

/**
 * Looks for a child element within the slot at the specified board coordinates with the
 * CSS class "ch-token-white" or "ch-token-black" and returns it. Only the first element
 * is considered, as multiple elements within the slot would be an invalid state of the
 * board.
 * 
 * If no such token element exists, undefined is returned.
 * 
 * @param {Number} x - The x-coordinate of the slot to retrieve a token from.
 * @param {Number} y - The y-coordinate of the slot to retrieve a token from.
 * 
 * @returns {HTMLElement} - The element, likely a div, that represents the token within the slot at the specified
 * coordinates, if it exists.
 */
function getSlotTokenElement(x, y) {
	let slotElement = getSlotElement(x, y);
	if (slotElement) {
		let token = slotElement.firstChild;
		if (token && (token.classList.contains("ch-token-white") || token.classList.contains("ch-token-black"))) {
			return token;
		}
	}

	return undefined;
}

/**
 * Examines a slot on the board at the given coordinate and determines if it is vacant.
 * If the coordinates are invalid, the slot will not exist, and thus cannot be vacant.
 * 
 * @param {Number} x - The x-coordinate of the slot to examine.
 * @param {Number} y - The y-coordinate of the slot to examine.
 * 
 * @returns {Boolean} - Whether or not the slot on the board at the provided coordinates is vacant.
 */
function isSlotVacant(x, y) {
	let highestCoordinate = CHECKERS_BOARD_SIZE - 1;
	if (x < 0 || x > highestCoordinate) return false;
	if (y < 0 || y > highestCoordinate) return false;

	return !getSlotTokenElement(x, y);
}

/**
 * Alters the state of the board to reflect the given state.
 * 
 * @param {UInt8Array} newBoardState - The new state of the board.
 * 0 represents an empty slot,
 * 1 a white token,
 * 2 a kinged white token,
 * 3 a black token, and
 * 4 a kinged black token.
 */
function setBoardState(newBoardState) {
	//Make a new board, since the server won't populate it from the viewpoint of the user.
	let boardSlots = [];

	let createBoardSlot = function (x, y) {
		let boardIndex = (y * CHECKERS_BOARD_SIZE) + x;
		let slotElement = document.createElement("div");
		slotElement.setAttribute("id", `slot-x${x}-y${y}`);
		slotElement.setAttribute("data-board-index", `${boardIndex}`);

		let slotColour;
		if (y % 2) {
			//If y is odd, the row starts with white.
			//If x is even, the slot will be black.
			slotColour = x % 2 ? "ch-slot-black" : "ch-slot-white";
		} else {
			slotColour = x % 2 ? "ch-slot-white" : "ch-slot-black";
		}
		slotElement.classList.add(slotColour);

		return slotElement;
	}

	if (ourPlayerNumber == 1) {
		//The player is using the white tokens
		for (let y = CHECKERS_BOARD_SIZE - 1; y >= 0; y--) {
			for (let x = 0; x < CHECKERS_BOARD_SIZE; x++) {
				boardSlots.push(createBoardSlot(x, y));
			}
		}
	} else {
		//The player is using the black tokens
		for (let y = 0; y < CHECKERS_BOARD_SIZE; y++) {
			for (let x = CHECKERS_BOARD_SIZE - 1; x >= 0; x--) {
				boardSlots.push(createBoardSlot(x, y));
			}
		}
	}

	document.querySelector(".ch-board").replaceChildren(...boardSlots);

	newBoardState.forEach(function (token, index) {
		if (token == 0) return;

		let x = index % CHECKERS_BOARD_SIZE;
		let y = parseInt(index / CHECKERS_BOARD_SIZE);

		let slotElement = getSlotElement(x, y);

		let tokenDiv = document.createElement("div");

		//The fallthrough on case 2 and 4 is intentional.
		switch (token) {
			case 2:
				tokenDiv.classList.add("ch-token-king");
			case 1:
				tokenDiv.classList.add("ch-token-white");
				break;
			case 4:
				tokenDiv.classList.add("ch-token-king");
			case 3:
				tokenDiv.classList.add("ch-token-black");
				break;
			default:
				break;
		}

		slotElement.appendChild(tokenDiv);
	});
}

/**
 * Scans the board for possible player moves, and applies CSS class "ch-token-highlight" to
 * the token elements if the move is possible; further, if the move is possible, the token
 * element has a tabindex of -1 applied, a role of "button" applied, and the onclick
 * event listener is overwritten.
 * 
 * If the inputs are being disabled, any elements marked with the CSS class of
 * "ch-token-highlight" will have the things listed above removed.
 * 
 * The token elements are expected to be marked with a CSS class of "ch-token-white", or
 * "ch-token-black", depending on the user's player number.
 * 
 * @param {Boolean} enabled - If the input should be enabled or not.
 */
function enableCheckersInput(enabled = true) {
	/**
	 * Removes the highlighting and onclick functions of slot and token elements,
	 * with the exception of the given token element.
	 * 
	 * @param {HTMLElement} highlightedTokenElement - The element representing a token that should remain highlighted.
	 */
	let clearHighlightExceptOn = function (highlightedTokenElement) {
		document.querySelectorAll(".ch-slot-highlight").forEach(function (slotElement) {
			slotElement.classList.remove("ch-slot-highlight");
			slotElement.onclick = function () { }; //Setting this as an empty function just in case.
		});

		document.querySelectorAll(".ch-token-highlight").forEach(function (otherTokenElement) {
			if (otherTokenElement != highlightedTokenElement) {
				otherTokenElement.classList.remove("ch-token-highlight");
				otherTokenElement.removeAttribute("tabindex");
				otherTokenElement.removeAttribute("role");
				otherTokenElement.onclick = function () { }; //Setting this as an empty function just in case.
			}
		});
	}

	if (!enabled) {
		//Clean up any old states
		clearHighlightExceptOn(undefined);
	} else {
		let playerHasForcedMoves = false;

		/** @type {CheckersToken[]} */
		let tokensWithValidPlays = [];

		document.querySelectorAll((ourPlayerNumber == 1 ? ".ch-token-white" : ".ch-token-black")).forEach(function (tokenElement) {
			let boardIndex = parseInt(tokenElement.parentElement.getAttribute("data-board-index"));
			let token = new CheckersToken(boardIndex);

			//If a move that jumps an opponent token is possible, it must be made (see official rules); so, moves that do not should be filtered.
			if (playerHasForcedMoves) {
				if (token.canJumpOpponent) {
					tokensWithValidPlays.push(token);
				}
			} else if (token.canMove) {
				if (token.canJumpOpponent) {
					tokensWithValidPlays.length = 0;
					playerHasForcedMoves = true;
				}
				tokensWithValidPlays.push(token);
			}
		});

		/**
		 * Displays possible moves of the given token to the user.
		 * 
		 * @param {CheckersToken} token - The token to show possible moves for.
		 */
		let highlightPossibleMovesFor = function (token) {
			//Grab our token, and highlight it; also, assume the user clicked our token already (focus it).
			//If we're in a recursive call, we still want to act like the user clicked our token.
			let tokenElement = getSlotTokenElement(token.x, token.y);

			tokenElement.classList.add("ch-token-highlight");
			tokenElement.setAttribute("tabindex", "-1");
			tokenElement.setAttribute("role", "button");
			tokenElement.focus();

			//Clear out other highlights.
			clearHighlightExceptOn(tokenElement);

			token.possibleMoves.forEach(function (boardIndex) {
				let possibleMoveX = boardIndex % CHECKERS_BOARD_SIZE;
				let possibleMoveY = Math.floor(boardIndex / CHECKERS_BOARD_SIZE);

				let possibleMoveSlotElement = getSlotElement(possibleMoveX, possibleMoveY);
				possibleMoveSlotElement.classList.add("ch-slot-highlight");

				possibleMoveSlotElement.onclick = function () {
					let possibleMoveBoardIndex = parseInt(possibleMoveSlotElement.getAttribute("data-board-index"));
					userCurrentMoves.push(possibleMoveBoardIndex);

					let tokenElementCopy = tokenElement.cloneNode();
					tokenElementCopy.classList.add("ch-token-ghost");
					possibleMoveSlotElement.replaceChildren(tokenElementCopy);

					let ghostToken = new CheckersToken(possibleMoveBoardIndex);
					if (token.canJumpOpponent && ghostToken.canJumpOpponent) {
						//This is a recursive call
						highlightPossibleMovesFor(ghostToken);
					} else {
						enableCheckersInput(false);
						document.querySelectorAll(".ch-token-ghost").forEach(function (ghostTokenElement) {
							ghostTokenElement.remove();
						});
						checkersConnection.invoke(SE_CHECKERS_PLAY_TOKEN, userCurrentMoves).catch(function (err) {
							return console.error(err.toString());
						});
					}
				};
			});
		};

		tokensWithValidPlays.forEach(function (token) {
			let tokenElement = getSlotTokenElement(token.x, token.y);

			tokenElement.classList.add("ch-token-highlight");
			tokenElement.setAttribute("tabindex", "-1");
			tokenElement.setAttribute("role", "button");

			tokenElement.onclick = function () {
				tokenElement.onclick = function () { }; //Setting this as an empty function just in case.

				//The official rules state when you touch a token, you are commited to moving it!
				//Clear userCurrentMoves; and then, add the selected token as the first element.
				userCurrentMoves.length = 0;
				userCurrentMoves.push(parseInt(getSlotElement(token.x, token.y).getAttribute("data-board-index")));

				highlightPossibleMovesFor(token);
			};
		});
	}
}



function checkersSetPlayerTurn(playerNumber) {
	setPlayerTurn(playerNumber);
	if (isUserTurn) {
		enableCheckersInput();
	}
}

/**
 * Sets the board to match the given board state.
 * 
 * @param {String} board - A Base64 representation of a byte array that represents the current state of the board.
 */
function joined(board) {
	//Convert board to a UInt8Array
	board = Uint8Array.from(window.atob(board), b => b.charCodeAt(0));
	setBoardState(board);
}

/**
 * Simulates the play of a token on the current board. The given moves list will be used to
 * determine which token to move, and where to move it to. As the token is moved, if it jumps
 * over an enemy token, the enemy token will be removed from the board.
 * 
 * @param {String} moves - A Base64 string for an array of bytes that represents the move being made.
 * The first number is the board index for the token being moved; and, subsequent numbers are
 * the board slots that token is being moved to.
 */
function tokenPlayed(moves) {
	tokensHaveBeenPlayed = true;

	//Convert moves to a UInt8Array
	moves = Uint8Array.from(window.atob(moves), b => b.charCodeAt(0));

	let startingX = moves[0] % CHECKERS_BOARD_SIZE;
	let startingY = Math.floor(moves[0] / CHECKERS_BOARD_SIZE);
	let startingToken = getSlotTokenElement(startingX, startingY);

	//We assume the move is valid because the server is in charge of validation.
	for (let i = 1; i < moves.length; i++) {
		let targetX = moves[i] % CHECKERS_BOARD_SIZE;
		let targetY = Math.floor(moves[i] / CHECKERS_BOARD_SIZE);
		let targetSlot = getSlotElement(targetX, targetY);

		//This moves the token element into the target slot (no need to call remove).
		targetSlot.replaceChildren(startingToken);

		if (Math.abs(targetX - startingX) > 1) {
			//Moves are guaranteed to move at least 1 in x or y; if they move 2, they made a jump.
			let jumpedX = startingX + ((targetX - startingX) / 2); //Keep the sign, but change from -2 or 2 to -1 or 1
			let jumpedY = startingY + ((targetY - startingY) / 2);
			getSlotTokenElement(jumpedX, jumpedY).remove();

			//Since we might move again, let's reposition the starting coordinates.
			//There's no need to get the token again, since startingToken still holds the correct element.
			startingX = targetX;
			startingY = targetY;
		} else {
			//The token can only move once, unless they jumped an opponent token.
			break;
		}
	}
}

/**
 * Marks a token at the specified board index as a king.
 * 
 * @param {Number} boardIndex - The board index denoting the location of a token that has been kinged.
 */
function tokenKinged(boardIndex) {
	let tokenElement = getSlotTokenElement(boardIndex % CHECKERS_BOARD_SIZE, Math.floor(boardIndex / CHECKERS_BOARD_SIZE));
	if (tokenElement) {
		tokenElement.classList.add("ch-token-kinged");
	}
}

/**
 * Resets the board, and sets tokensHaveBeenPlayed to false.
 */
function boardReset() {
	tokensHaveBeenPlayed = false;

	document.querySelectorAll(".ch-slot-black").forEach(function (slotElement) {
		//This will remove any tokens on the board. In Checkers, only the black tiles are used.
		slotElement.replaceChildren();
	});

	setBoardState(CHECKERS_DEFAULT_BOARD_STATE);
}

/**
 * A helper class for determining possible moves that can be made on the board.
 */
class CheckersToken {

	/**
	 * The x-coordinate of the board the token this represents is located at.
	 * 
	 * @type {Number}
	 */
	x = -1;

	/**
	 * The y-coordinate of the board the token this represents is located at.
	 * 
	 * @type {Number}
	 */
	y = -1;

	/**
	 * Whether or not the token this represents has been kinged.
	 * 
	 * @type {Boolean}
	 */
	isKing = false;

	/**
	 * Whether or not the token this represents has any moves available.
	 * 
	 * @type {Boolean}
	 */
	canMove = false;

	/**
	 * Whether or not the token this represents has any valid moves which jump over an opponent
	 * available.
	 * 
	 * @type {Boolean}
	 */
	canJumpOpponent = false;

	/**
	 * A list of valid board indices the token this represents can move to. According to the
	 * official rules, moves that can jump an opponent token are forced; as such, the possible
	 * moves will be filtered when applicable.
	 * 
	 * A board index is a single value that represents x and y coordinates on the game board.
	 * To retrieve the x-coordinate of a board index, mod the index by CHECKERS_BOARD_SIZE.
	 * To retrieve the y-coordinate of a board index, divide the index by CHECKERS_BOARD_SIZE,
	 * followed by a call to Math.floor() on the result.
	 * 
	 * The board is laid out in such a way that the bottom left corner, from the perspective of
	 * the player using white tokens, is the zero index. An example layout is as follows:
	 * 	[56][57][58][59][60][61][62][63]
	 * 	[48][49][50][51][52][53][54][55]
	 * 	[40][41][42][43][44][45][46][47]
	 * 	[32][33][34][35][36][37][38][39]
	 * 	[24][25][26][27][28][29][30][31]
	 * 	[16][17][18][19][20][21][22][23]
	 * 	[ 8][ 9][10][11][12][13][14][15]
	 * 	[ 0][ 1][ 2][ 3][ 4][ 5][ 6][ 7]
	 * 
	 * @type {Number[]}
	 */
	possibleMoves = [];

	/**
	 * Constructs a representation of the token located at the given coordinates on the board.
	 * This token's position will be analyzed, and any moves that can potentially be made will
	 * be placed within the possibleMoves member. According to the official rules, moves that
	 * can jump an opponent token are forced; as such, the possible moves will be filtered when
	 * applicable.
	 * 
	 * @param {Number} boardIndex - The index of the board in which the token this will represent is located.
	 */
	constructor(boardIndex) {
		this.x = boardIndex % CHECKERS_BOARD_SIZE;
		this.y = Math.floor(boardIndex / CHECKERS_BOARD_SIZE);

		let token = getSlotTokenElement(this.x, this.y);

		if (!token) {
			return;
		}

		this.isKing = token.classList.contains("ch-token-kinged");
		let opponentTokenClass = token.classList.contains("ch-token-white") ? "ch-token-black" : "ch-token-white";
		let forwardDirection = token.classList.contains("ch-token-white") ? 1 : -1;

		/**
		 * Returns the board index for the given coordinate on the board. If the specified coordinate
		 * is invalid, the resulting board index will also be invalid.
		 * 
		 * @param {Number} xCoord - The x-coordinate of a slot on the board.
		 * @param {Number} yCoord - The y-coordinate of a slot on the board.
		 * 
		 * @returns {Number} - The number representing the board index of the given coordinate, if it's
		 * within the valid range of the board.
		 */
		let getSlotBoardIndex = function (xCoord, yCoord) {
			return (yCoord * CHECKERS_BOARD_SIZE) + xCoord;
		};

		/**
		 * Determines if a valid move is available in the given direction.
		 * 
		 * @param {CheckersToken} token - The token to analyze moves for.
		 * @param {Number} xDir - The normalized x direction to look towards.
		 * @param {Number} yDir - The normalized y direction to look towards.
		 */
		let analyzeSlotForMoves = function (token, xDir, yDir) {
			if (isSlotVacant(token.x + xDir, token.y + yDir)) {
				//Slot is vacant; unless we are forced to jump an opponent, add it to possible moves.
				if (!token.canJumpOpponent) {
					token.possibleMoves.push(getSlotBoardIndex(token.x + xDir, token.y + yDir));
				}
			} else {
				//Slot is not vacant; check if it exists, and if it's an opponent; if it is, see if the next slot is vacant.
				let nonVacantSlotElement = getSlotTokenElement(token.x + xDir, token.y + yDir);
				if (nonVacantSlotElement && nonVacantSlotElement.classList.contains(opponentTokenClass)
					&& isSlotVacant(token.x + (xDir * 2), token.y + (yDir * 2))) {
					if (!token.canJumpOpponent) {
						token.possibleMoves.length = 0;
						token.canJumpOpponent = true;
					}

					//The next slot is vacant, so it's a possible move to jump an opponent.
					token.possibleMoves.push(getSlotBoardIndex(token.x + (xDir * 2), token.y + (yDir * 2)))
				}
			}
		};

		//Check ahead for vacant slots
		for (let i = -1; i < 2; i += 2) {
			analyzeSlotForMoves(this, i, forwardDirection);

			//If kinged, check behind, too.
			if (this.isKing) {
				analyzeSlotForMoves(this, i, -forwardDirection);
			}
		}

		if (this.possibleMoves.length) {
			this.canMove = true;
		}
	}

}
