using Connect_X.Enums;

namespace Connect_X
{
	/// <summary>
	/// A collection of callback methods to be used by <see cref="ConnectionGame"/> when certain events occur. These methods
	/// are intended to allow implementors the ability to update their own representation of the game to match the underlying
	/// one used by the <see cref="ConnectionGame"/> class.
	/// </summary>
	public interface IConnectionGameEventHandler
	{
		/// <summary>
		/// Called when a token was played on the board. Implementors of <see cref="IConnectionGameEventHandler"/> may use this method
		/// to update their representation of the game.
		/// <br></br><br></br>
		/// The coordinates of the <paramref name="row"/> and <paramref name="column"/> are from the bottom left of the board.
		/// </summary>
		/// <param name="playedToken">The token that was played on the board.</param>
		/// <param name="nextPlayerToken">The token representing the next active player, or <see cref="BoardToken.None"/> if the game is over.</param>
		/// <param name="row">The row where the token was played.</param>
		/// <param name="column">The column where the token was played.</param>
		public void TokenPlayed(BoardToken playedToken, BoardToken nextPlayerToken, byte row, byte column);

		/// <summary>
		/// Called when an invalid move was attempted by a player. Implementors of <see cref="IConnectionGameEventHandler"/> may use this method
		/// to decide if their board representation has fallen out of sync with the underlying one. If they believe that to be the case,
		/// this method should return true; false otherwise. If this method returns true, a subsequent call to
		/// <see cref="RefreshBoard(BoardToken[])"/> will be made.
		/// <br></br><br></br>
		/// The coordinates of the <paramref name="row"/> and <paramref name="column"/> are from the bottom left of the board.
		/// </summary>
		/// <param name="attemptedPlayToken">The type of token that a player attempted to play on the board.</param>
		/// <param name="existingTokenInSlot">The token at the specified <paramref name="row"/> and <paramref name="column"/>.</param>
		/// <param name="isActivePlayer">If it's currently the turn of the player represented by <paramref name="attemptedPlayToken"/>.</param>
		/// <param name="row">The row where the token was attempted to be played.</param>
		/// <param name="column">The column where the token was attempted to be played.</param>
		/// <returns>True if <see cref="RefreshBoard(BoardToken[])"/> should be called; false otherwise.</returns>
		public bool ShouldRefreshBoardOnInvalidPlay(BoardToken attemptedPlayToken, BoardToken existingTokenInSlot, bool isActivePlayer, byte row, byte column);

		/// <summary>
		/// Allows implementors to sync representations of the game board with the underlying one.
		/// <br></br><br></br>
		/// This method is only called under the condition that an invalid move was attempted, and
		/// <see cref="ShouldRefreshBoardOnInvalidPlay(BoardToken, BoardToken, bool, byte, byte)"/>
		/// returned true.
		/// </summary>
		/// <param name="validBoard">The underlying representation of the board. This is not a copy; and should not be modified.</param>
		public void RefreshBoard(BoardToken[] validBoard);

		/// <summary>
		/// Called when the board has been cleared by the <see cref="ConnectionGame"/>; a new game is started
		/// with the player represented by the given <paramref name="newActivePlayer"/> being allotted a turn.
		/// </summary>
		/// <param name="newActivePlayer">The token representing the next active player.</param>
		public void ClearBoard(BoardToken newActivePlayer);

		/// <summary>
		/// Called when the <see cref="ConnectionGame"/> has ended. The game may end due to a player winning,
		/// a stalemate having been reached, or all other players forfeiting.
		/// </summary>
		/// <param name="winningPlayerToken">The token representing the winning player; or <see cref="BoardToken.None"/> if the game ended without a winner.</param>
		public void GameEnded(BoardToken winningPlayerToken);
	}

	/// <summary>
	/// A game in which players attempt to connect their tokens in a sequence while preventing their opponents from doing the same.
	/// <br></br><br></br>
	/// Examples of this type of game include tic-tac-toe, and Connect Four.
	/// </summary>
	public class ConnectionGame
	{
		/// <summary>
		/// An underlying representation of the game board to be used for this game.
		/// </summary>
		private readonly ConnectionBoard board;

		/// <summary>
		/// The number of players participating in this game.
		/// </summary>
		private readonly byte playerCount;

		/// <summary>
		/// The token representing the active player.
		/// </summary>
		private BoardToken activePlayer = BoardToken.Player1;

		/// <summary>
		/// Whether or not this game is ongoing. If the game has ended, but has not been cleared, then this will be false.
		/// </summary>
		public bool IsActive { get; private set; } = true;

		/// <summary>
		/// Creates a connection game with the specified board size, connection sequence length, and players.
		/// </summary>
		/// <param name="boardRows">The number of rows on the board.</param>
		/// <param name="boardColumns">The number of columns on the board.</param>
		/// <param name="winningSequenceLength">The minimum connection sequence length to win the game.</param>
		/// <param name="players">The number of players playing the game; the minimum value is two.</param>
		/// <exception cref="ArgumentOutOfRangeException">If the number of players is less than two.</exception>
		public ConnectionGame(byte boardRows, byte boardColumns, byte winningSequenceLength, byte players = 2)
		{
			if (players < 2)
			{
				throw new ArgumentOutOfRangeException(nameof(players), $"A {nameof(ConnectionGame)} requires at least two players.");
			}

			board = new(boardRows, boardColumns, winningSequenceLength);
			playerCount = players;
		}

		/// <summary>
		/// Attempts to play a token at the specified location of the board. The <paramref name="handler"/>
		/// is notified of any changes made to the state of the game.
		/// <br></br><br></br>
		/// The coordinates of the <paramref name="row"/> and <paramref name="column"/> are from the bottom left of the board.
		/// </summary>
		/// <param name="handler">An implementation of callback methods that the caller can update their representation of the board with.</param>
		/// <param name="token">The token to play at the specified location.</param>
		/// <param name="row">The row in which to play the <paramref name="token"/>.</param>
		/// <param name="column">The column in which to play the <paramref name="token"/>.</param>
		public void PlayToken(IConnectionGameEventHandler handler, BoardToken token, byte row, byte column)
		{
			if (token != activePlayer || !board.IsTokenSlotAvailable(row, column))
			{
				BoardToken existingToken = board.GetTokenAtLocation(row, column);
				if (handler.ShouldRefreshBoardOnInvalidPlay(token, existingToken, token == activePlayer, row, column))
				{
					handler.RefreshBoard(board.Slots);
				}

				//Return early; the play wasn't possible.
				return;
			}

			board.SetTokenAtLocation(token, row, column);
			
			if (board.HasTokenSequenceAtLocation(row, column) || board.IsFull)
			{
				handler.TokenPlayed(token, BoardToken.None, row, column);
				handler.GameEnded(board.IsFull ? BoardToken.None : activePlayer);
			}
			else
			{
				CyclePlayerTurn();
				handler.TokenPlayed(token, activePlayer, row, column);
			}
		}

		/// <summary>
		/// Attempts to play a token in the specified column of the board; the token will be played at the bottom-most available
		/// slot of the board, as if it were affected by gravity.
		/// <br></br><br></br>
		/// The leftmost <paramref name="column"/> value is zero.
		/// </summary>
		/// <param name="handler">An implementation of callback methods that the caller can update their representation of the board with.</param>
		/// <param name="token">The token to play in the specified <paramref name="column"/>.</param>
		/// <param name="column">The column in which to play the <paramref name="token"/>.</param>
		/// <seealso cref="PlayToken(IConnectionGameEventHandler, BoardToken, byte, byte)"/>
		public void PlayGravityToken(IConnectionGameEventHandler handler, BoardToken token, byte column)
		{
			bool attempted = false;
			for (byte row = 0; row < board.Rows; row++)
			{
				if (board.IsTokenSlotAvailable(row, column))
				{
					PlayToken(handler, token, row, column);
					attempted = true;
					break;
				}
			}

			if (!attempted)
			{
				//Player attempted to play in a column that was full; the attempted row is out of bounds.
				if (handler.ShouldRefreshBoardOnInvalidPlay(token, BoardToken.None, token == activePlayer, board.Rows, column))
				{
					handler.RefreshBoard(board.Slots);
				}
			}
		}

		/// <summary>
		/// Considers the player represented by the given <paramref name="forfeitingPlayerToken"/> to have lost the game.
		/// This action can result in the game ending.
		/// <br></br><br></br>
		/// If <paramref name="clearBoard"/> is true, and the game has ended due to this action,
		/// <see cref="ClearBoard(IConnectionGameEventHandler, BoardToken)"/> is also called. In such a case, the next active player is the
		/// one who forfeited.
		/// </summary>
		/// <param name="handler">An implementation of callback methods that the caller can update their representation of the board with.</param>
		/// <param name="forfeitingPlayerToken">The token representing the player who is forfeiting.</param>
		/// <param name="clearBoard">True if a call to <see cref="ClearBoard(IConnectionGameEventHandler, BoardToken)"/> should be made.</param>
		/// <exception cref="NotImplementedException">If the number of players exceeds two.</exception>
		public void Forfeit(IConnectionGameEventHandler handler, BoardToken forfeitingPlayerToken, bool clearBoard)
		{
			if (playerCount > 2)
			{
				throw new NotImplementedException($"{nameof(ConnectionGame)} does not currently support forfeiting when more than two players are competing.");
			}

			IsActive = false;
			handler.GameEnded(forfeitingPlayerToken == BoardToken.Player1 ? BoardToken.Player2 : BoardToken.Player1);
			if (clearBoard)
			{
				//Losing player gets the first turn; TODO: Consider if that's acceptable.
				ClearBoard(handler, forfeitingPlayerToken);
			}
		}

		/// <summary>
		/// Clears the board. The game is assumed to be inactive (see <see cref="IsActive"/>).
		/// After the board has been cleared, the game is restarted with the player represented by the given <paramref name="newActivePlayerToken"/>
		/// being allotted a turn.
		/// </summary>
		/// <param name="handler">An implementation of callback methods that the caller can update their representation of the board with.</param>
		/// <param name="newActivePlayerToken">The token representing the next player to be allotted a turn.</param>
		public void ClearBoard(IConnectionGameEventHandler handler, BoardToken newActivePlayerToken)
		{
			//assert !IsActive;
			board.Clear();
			activePlayer = newActivePlayerToken;
			IsActive = true;
			handler.ClearBoard(newActivePlayerToken);
		}

		/// <summary>
		/// Changes <see cref="activePlayer"/> to the next player in line.
		/// </summary>
		private void CyclePlayerTurn()
		{
			//TODO: Consider a new name for this method.
			if ((byte) activePlayer == playerCount)
			{
				activePlayer = BoardToken.Player1;
			}
			else
			{
				activePlayer += 1;
			}
		}
	}
}
