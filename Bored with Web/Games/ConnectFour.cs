using Connect_X;
using Connect_X.Enums;

namespace Bored_with_Web.Games
{
	/// <summary>
	/// An implementation of <see cref="SimpleGame"/> that handles Connect Four.
	/// <br></br><br></br>
	/// This implementation mostly just wraps the game logic found in <see cref="ConnectionGame"/>.
	/// </summary>
	[Game("Connect-Four")]
	public class ConnectFour : SimpleGame
	{
		/// <summary>
		/// A constant representing the standard number of rows for Connect Four.
		/// <br></br><br></br>
		/// The standard value is 6.
		/// </summary>
		const byte STANDARD_CONNECT_FOUR_ROWS = 6;

		/// <summary>
		/// A constant representing the standard number of columns for Connect Four.
		/// <br></br><br></br>
		/// The standard value is 7.
		/// </summary>
		const byte STANDARD_CONNECT_FOUR_COLUMNS = 7;

		/// <summary>
		/// A constant representing the standard number of tokens that players must connect in order to win for Connect Four.
		/// <br></br><br></br>
		/// The standard value is 4.
		/// </summary>
		const byte STANDARD_CONNECT_FOUR_SEQUENCE_LENGTH = 4;

		/// <summary>
		/// The number representing the player who is currently being allotted a turn.
		/// </summary>
		public int ActivePlayerNumber { get { return (int) board.ActivePlayer; } }

		/// <summary>
		/// The internal state of the game where all game-logic is handled.
		/// </summary>
		private readonly ConnectionGame board;

		/// <summary>
		/// The outcomes of each match, added as they end.
		/// </summary>
		private List<SimpleGameOutcome> matchOutcomes = new();

		/// <summary>
		/// The outcome of the current match.
		/// </summary>
		private SimpleGameOutcome currentMatchOutcome = null!;

		/// <summary>
		/// Creates a standard game of Connect Four with <see cref="STANDARD_CONNECT_FOUR_ROWS"/> rows,
		/// <see cref="STANDARD_CONNECT_FOUR_COLUMNS"/> columns, and requiring the players to connect
		/// <see cref="STANDARD_CONNECT_FOUR_SEQUENCE_LENGTH"/> tokens in a row to win.
		/// <br></br><br></br>
		/// Empty constructors are required of subclasses of <see cref="SimpleGame"/>.
		/// </summary>
		public ConnectFour()
		{
			board = new(STANDARD_CONNECT_FOUR_ROWS, STANDARD_CONNECT_FOUR_COLUMNS, STANDARD_CONNECT_FOUR_SEQUENCE_LENGTH);
			BeginTrackingNewMatch();
		}

		/// <summary>
		/// Creates a standard game of Connect Four with <see cref="STANDARD_CONNECT_FOUR_ROWS"/> rows,
		/// <see cref="STANDARD_CONNECT_FOUR_COLUMNS"/> columns, and requiring the players to connect
		/// <see cref="STANDARD_CONNECT_FOUR_SEQUENCE_LENGTH"/> tokens in a row to win. The game will be between
		/// the given <paramref name="player1"/> and <paramref name="player2"/>; and be tracked by the given
		/// <paramref name="gameId"/>.
		/// </summary>
		/// <param name="gameId">A string identifier which must be unique to this game.</param>
		/// <param name="player1">The first player competing in this game.</param>
		/// <param name="player2">The second player competing in this game.</param>
		public ConnectFour(string gameId, Player player1, Player player2)
		{
			board = new(STANDARD_CONNECT_FOUR_ROWS, STANDARD_CONNECT_FOUR_COLUMNS, STANDARD_CONNECT_FOUR_SEQUENCE_LENGTH);
			base.CreateGame(CanonicalGames.GetGameInfoByRouteId("Connect-Four")!, gameId, player1, player2);
			BeginTrackingNewMatch();
		}

		/// <summary>
		/// Creates a custom game of Connect Four with the specified number of <paramref name="rows"/>, and <paramref name="columns"/>.
		/// The players must connect <paramref name="winningSequenceLength"/> number of tokens in order to win. The game will be between
		/// all given <paramref name="players"/> (up to 255); and be tracked by the given <paramref name="gameId"/>.
		/// <br></br><br></br>
		/// If the number of players exceeds <see cref="byte.MaxValue"/>, an <see cref="ArgumentException"/> is thrown.
		/// </summary>
		/// <param name="gameId">A string identified which must be unique to this game.</param>
		/// <param name="rows">The number of rows to place on the board.</param>
		/// <param name="columns">The number of columns to place on the board.</param>
		/// <param name="winningSequenceLength">The number of tokens that must be placed in a row in order to win.</param>
		/// <param name="players">The players competing in this game.</param>
		/// <exception cref="ArgumentException">If the number of players exceeds <see cref="byte.MaxValue"/></exception>
		public ConnectFour(string gameId, byte rows, byte columns, byte winningSequenceLength, params Player[] players)
		{
			if (players.Length > byte.MaxValue)
			{
				throw new ArgumentException($"This implementation of Connect Four does not support more than {byte.MaxValue} players.", nameof(players));
			}

			board = new(rows, columns, winningSequenceLength, (byte) players.Length);
			base.CreateGame(CanonicalGames.GetGameInfoByRouteId("Connect-Four")!, gameId, players);
			BeginTrackingNewMatch();
		}

		/// <summary>
		/// Attempts to play a token on the board for the given <paramref name="player"/>, in the specified <paramref name="column"/>.
		/// <br></br><br></br>
		/// The given <paramref name="handler"/> will be notified of any changes to the state of the game.
		/// </summary>
		/// <param name="handler">An object that handles updating the caller's representation of this game.</param>
		/// <param name="player">The <see cref="Player"/> attempting this play.</param>
		/// <param name="column">The column to attempt to play a token in.</param>
		public void PlayToken(IConnectionGameEventHandler handler, Player player, byte column)
		{
			Player internalPlayer = GetInternalPlayer(player);

			board.PlayGravityToken(new ConnectionGameEventHandlerWrapper(this, handler), (BoardToken) internalPlayer.PlayerNumber, column);
		}

		/// <summary>
		/// Calls <see cref="IConnectionGameEventHandler.RefreshBoard(BoardToken[])"/> on the given <paramref name="handler"/>.
		/// </summary>
		/// <param name="handler">An object that handles updating the caller's representation of this game.</param>
		public void RefreshBoard(IConnectionGameEventHandler handler)
		{
			board.RefreshBoard(new ConnectionGameEventHandlerWrapper(this, handler));
		}

		/// <summary>
		/// Forfeits the game for the specified <paramref name="player"/>. This action may cause the match to end.
		/// <br></br><br></br>
		/// The given <paramref name="handler"/> will be notified of changes to the state of the game.
		/// </summary>
		/// <param name="handler">An object that handles updating the caller's representation of this game.</param>
		/// <param name="player">The <see cref="Player"/> forfeiting the game.</param>
		public void Forfeit(IConnectionGameEventHandler handler, Player player)
		{
			Player internalPlayer = GetInternalPlayer(player);

			board.Forfeit(new ConnectionGameEventHandlerWrapper(this, handler), (BoardToken) internalPlayer.PlayerNumber, clearBoard: false);

			//TODO: Consider doing the following... note that this is only called when someone intentionally clears the board -- they chose to quit that match.
			//This must come after board.Forfeit
			//currentMatchOutcome.EndState = GameEnding.INCOMPLETE;
			//currentMatchOutcome.ForfeitingPlayers.Add(internalPlayer);
		}

		/// <summary>
		/// Clears all tokens played on the <see cref="board"/>, ending the match. The next match is started,
		/// and the given player, represented by <paramref name="hasNextTurn"/>, is allotted a turn.
		/// <br></br><br></br>
		/// The given <paramref name="handler"/> will be notified of changes to the state of the game.
		/// </summary>
		/// <param name="handler">An object that handles updating the caller's representation of this game.</param>
		/// <param name="hasNextTurn">The <see cref="Player"/> that will be allotted the first turn of the next match.</param>
		public void ClearBoard(IConnectionGameEventHandler handler, Player hasNextTurn)
		{
			Player internalPlayer = GetInternalPlayer(hasNextTurn);

			board.ClearBoard(new ConnectionGameEventHandlerWrapper(this, handler), (BoardToken) internalPlayer.PlayerNumber);
		}

		/// <summary>
		/// Determines if a player can leave the match without it being considered a forfeiture.
		/// If the game is active, then the player will have to forfeit the match if they leave.
		/// <br></br><br></br>
		/// See <see cref="ConnectionGame.IsActive"/> for more information.
		/// </summary>
		/// <returns>True if the player cannot leave without forfeiting the game; false otherwise.</returns>
		public override bool PlayerCannotLeaveWithoutForfeiting()
		{
			return board.IsActive;
		}

		public override bool PlayerLeft(Player player, bool isConnectionTimeout = false)
		{
			if (base.PlayerLeft(player, isConnectionTimeout))
			{
				if (PlayerCannotLeaveWithoutForfeiting())
				{
					currentMatchOutcome.EndState = GameEnding.INCOMPLETE;
					currentMatchOutcome.ForfeitingPlayers.Add(player);
				}
				else
				{
					currentMatchOutcome.EndState = GameEnding.NONE;
				}

				return true;
			}

			return false;
		}

		/// <summary>
		/// Adds the <see cref="currentMatchOutcome"/> to <see cref="matchOutcomes"/>, and then resets
		/// <see cref="currentMatchOutcome"/> to one that is considered to be not started.
		/// </summary>
		private void BeginTrackingNewMatch()
		{
			if (currentMatchOutcome is not null)
			{
				matchOutcomes.Add(currentMatchOutcome);
			}

			currentMatchOutcome = new()
			{
				EndState = GameEnding.NONE,
				Game = this
			};
		}

		protected override IEnumerable<SimpleGameOutcome> GetOutcome()
		{
			//Add the last match to the list before returning it.
			BeginTrackingNewMatch();
			return matchOutcomes;
		}

		/// <summary>
		/// A helper class that allows for intercepting changes to the game state before passing them back to the caller.
		/// </summary>
		private class ConnectionGameEventHandlerWrapper : IConnectionGameEventHandler
		{
			/// <summary>
			/// The instance of <see cref="ConnectFour"/> this nested class was created by.
			/// </summary>
			private ConnectFour parent;

			/// <summary>
			/// The real <see cref="IConnectionGameEventHandler"/> that is being wrapped by this class.
			/// </summary>
			private readonly IConnectionGameEventHandler wrapped;

			public ConnectionGameEventHandlerWrapper(ConnectFour parent, IConnectionGameEventHandler wrapped)
			{
				this.parent = parent;
				this.wrapped = wrapped;
			}

			void IConnectionGameEventHandler.ClearBoard(BoardToken newActivePlayer)
			{
				parent.BeginTrackingNewMatch();
				parent.currentMatchOutcome.EndState = GameEnding.INCOMPLETE;

				wrapped.ClearBoard(newActivePlayer);
			}

			void IConnectionGameEventHandler.GameEnded(BoardToken winningPlayerToken)
			{
				parent.currentMatchOutcome.EndState = winningPlayerToken > 0 ? GameEnding.VICTORY : GameEnding.STALEMATE;
				if (winningPlayerToken > 0)
				{
					parent.currentMatchOutcome.WinningPlayers.Add((from players in parent.Players
																   where players.PlayerNumber == (int) winningPlayerToken
																   select players).Single());

					parent.currentMatchOutcome.LosingPlayers.UnionWith((from players in parent.Players
																		where players.PlayerNumber != (int) winningPlayerToken
																		select players).ToList());
				}
				else
				{
					parent.currentMatchOutcome.LosingPlayers.UnionWith(parent.Players);
				}
				wrapped.GameEnded(winningPlayerToken);
			}

			void IConnectionGameEventHandler.RefreshBoard(BoardToken[] validBoard)
			{
				wrapped.RefreshBoard(validBoard);
			}

			bool IConnectionGameEventHandler.ShouldRefreshBoardOnInvalidPlay(BoardToken attemptedPlayToken, BoardToken existingTokenInSlot, bool isActivePlayer, byte row, byte column)
			{
				return wrapped.ShouldRefreshBoardOnInvalidPlay(attemptedPlayToken, existingTokenInSlot, isActivePlayer, row, column);
			}

			void IConnectionGameEventHandler.TokenPlayed(BoardToken playedToken, BoardToken nextPlayerToken, byte row, byte column)
			{
				if (parent.currentMatchOutcome.EndState == GameEnding.NONE)
				{
					parent.currentMatchOutcome.EndState = GameEnding.INCOMPLETE;
				}

				Player player = (from players in parent.Players
								 where players.PlayerNumber == (int) playedToken
								 select players).Single();

				if (parent.currentMatchOutcome.PlayerTurnCounts.TryGetValue(player, out int turnCount))
				{
					parent.currentMatchOutcome.PlayerTurnCounts[player] = turnCount + 1;
				}
				else
				{
					parent.currentMatchOutcome.PlayerTurnCounts.Add(player, 1);
				}

				wrapped.TokenPlayed(playedToken, nextPlayerToken, row, column);
			}
		}
	}
}
