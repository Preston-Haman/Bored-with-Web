using Bored_with_Web.Hubs;
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
		/// Creates a standard game of Connect Four with <see cref="STANDARD_CONNECT_FOUR_ROWS"/> rows,
		/// <see cref="STANDARD_CONNECT_FOUR_COLUMNS"/> columns, and requiring the players to connect
		/// <see cref="STANDARD_CONNECT_FOUR_SEQUENCE_LENGTH"/> tokens in a row to win.
		/// <br></br><br></br>
		/// Empty constructors are required of subclasses of <see cref="SimpleGame"/>.
		/// </summary>
		public ConnectFour()
		{
			board = new(STANDARD_CONNECT_FOUR_ROWS, STANDARD_CONNECT_FOUR_COLUMNS, STANDARD_CONNECT_FOUR_SEQUENCE_LENGTH);
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

		public async override Task StartNewMatch<GameType, IMultiplayerClient>(MultiplayerGameHub<GameType, IMultiplayerClient> gameHub, Player externalPlayer)
		{
			if (gameHub is not IConnectionGameEventHandler handler)
			{
				throw new InvalidOperationException($"The {nameof(ConnectFour)} implementation of {nameof(ForfeitMatch)} requires the {nameof(gameHub)} to implement {nameof(IConnectionGameEventHandler)}.");
			}

			ClearBoard(handler, externalPlayer);
			await base.StartNewMatch(gameHub, externalPlayer);
		}

		public async override Task ForfeitMatch<GameType, IMultiplayerClient>(MultiplayerGameHub<GameType, IMultiplayerClient> gameHub, Player externalPlayer)
		{
			if (gameHub is not IConnectionGameEventHandler handler)
			{
				throw new InvalidOperationException($"The {nameof(ConnectFour)} implementation of {nameof(ForfeitMatch)} requires the {nameof(gameHub)} to implement {nameof(IConnectionGameEventHandler)}.");
			}

			Forfeit(handler, externalPlayer);
			await IssueRematch(gameHub, externalPlayer);
		}

		/// <summary>
		/// Forfeits the game for the specified <paramref name="player"/>. This action may cause the match to end.
		/// <br></br><br></br>
		/// The given <paramref name="handler"/> will be notified of changes to the state of the game.
		/// </summary>
		/// <param name="handler">An object that handles updating the caller's representation of this game.</param>
		/// <param name="player">The <see cref="Player"/> forfeiting the game.</param>
		private void Forfeit(IConnectionGameEventHandler handler, Player player)
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
		private void ClearBoard(IConnectionGameEventHandler handler, Player hasNextTurn)
		{
			Player internalPlayer = GetInternalPlayer(hasNextTurn);

			board.ClearBoard(new ConnectionGameEventHandlerWrapper(this, handler), (BoardToken) internalPlayer.PlayerNumber);
		}

		/// <summary>
		/// A helper class that allows for intercepting changes to the game state before passing them back to the caller.
		/// </summary>
		private class ConnectionGameEventHandlerWrapper : IConnectionGameEventHandler
		{
			/// <summary>
			/// The instance of <see cref="ConnectFour"/> this nested class was created by.
			/// </summary>
			private readonly ConnectFour parent;

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
				wrapped.ClearBoard(newActivePlayer);
			}

			void IConnectionGameEventHandler.GameEnded(BoardToken winningPlayerToken)
			{
				parent.MatchIsActive = false;
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
				parent.BeginTrackingNewMatch();
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

				parent.currentMatchOutcome.IncrementPlayerTurnCount(parent.GetInternalPlayer((int) playedToken));
				wrapped.TokenPlayed(playedToken, nextPlayerToken, row, column);
			}
		}
	}
}
