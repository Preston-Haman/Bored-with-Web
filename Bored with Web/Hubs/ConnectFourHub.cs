using Bored_with_Web.Games;
using Connect_X;
using Connect_X.Enums;

namespace Bored_with_Web.Hubs
{
	/// <summary>
	/// Defines methods that are available on the client side of a <see cref="ConnectFourHub"/>.
	/// </summary>
	public interface IConnectFourClient : IMultiplayerGameClient
	{
		/// <summary>
		/// Called when a player joins or rejoins this game. Implementations should use the given
		/// information to update their representation of the board.
		/// <br></br><br></br>
		/// The game board is laid out with the 0 index at the bottom left of the board.
		/// As an example, with a 3x3 board, the indices would look like this:<br></br>
		/// [6][7][8]<br></br>
		/// [3][4][5]<br></br>
		/// [0][1][2]<br></br>
		/// </summary>
		/// <param name="board">A representation of the game board.</param>
		Task Joined(byte[] board);

		/// <summary>
		/// Called when a player plays a token on the board. Implementations should update the specified
		/// board slot to display the token representing the specified player.
		/// </summary>
		/// <param name="playerNumber">The number representing who played on the board.</param>
		/// <param name="row">The row in which the token was played.</param>
		/// <param name="column">The column in which the token was played.</param>
		Task TokenPlayed(int playerNumber, byte row, byte column);

		/// <summary>
		/// Called when the match has ended; if the match ended in a draw, then <paramref name="winningPlayerNumber"/>
		/// will be zero.
		/// </summary>
		/// <param name="winningPlayerNumber">The player number representing the winning player; or zero, if there was no winner.</param>
		Task MatchEnded(int winningPlayerNumber);

		/// <summary>
		/// Called when the opponent wants to challenge the player to another match. Implementations should
		/// inform the user that this challenge was issued.
		/// </summary>
		Task Rematch();

		/// <summary>
		/// Called when the board has been cleared. Implementations should reset the state of their board to
		/// an empty state.
		/// </summary>
		Task BoardCleared();
	}

	/// <summary>
	/// A basic implementation for managing the connection of Connect Four players.
	/// </summary>
	public class ConnectFourHub : MultiplayerGameHub<ConnectFour, IConnectFourClient>, IConnectionGameEventHandler
	{
		protected override async Task OnJoinedGame()
		{
			ActiveGame.RefreshBoard(this);
			await Clients.Caller.SetPlayerTurn(ActiveGame.ActivePlayerNumber);
		}

#pragma warning disable CS1998 //This ends up being async; but can't await it directly. Hope it works... :x
		/// <summary>
		/// Called by the client when they attempt to play a token on the board in the given column.
		/// </summary>
		/// <param name="column">The column to attempt to play a token in.</param>
		public async Task PlayToken(byte column)
		{
			ActiveGame.PlayToken(this, CurrentPlayer, column);
		}
#pragma warning restore CS1998

		/// <summary>
		/// Called by the client when they clear the board before the match has ended; this counts as a loss for them.
		/// </summary>
		public async Task ForfeitMatch()
		{
			ActiveGame.Forfeit(this, CurrentPlayer);
			await Clients.Caller.BoardCleared();
			await Clients.OthersInGroup(GameId).Rematch();
		}

		/// <summary>
		/// Called by the client when they clear the board after the match ended.
		/// </summary>
		public async Task Rematch()
		{
			await Clients.Caller.BoardCleared();

			//This could be a race condition on the client side...
			await Clients.OthersInGroup(GameId).Rematch();
		}

		/// <summary>
		/// Called by the client when they clear the board after their opponent challenged them to a rematch.
		/// </summary>
		public async Task AcceptRematch()
		{
			//TODO: Consider randomizing who has the first move instead of giving it to the challenged player.
			ActiveGame.ClearBoard(this, CurrentPlayer);
			await Clients.OthersInGroup(GameId).Rematch();
		}

		async void IConnectionGameEventHandler.ClearBoard(BoardToken newActivePlayer)
		{
			await Clients.Group(GameId).BoardCleared();

			int nextPlayerNumber = (int) newActivePlayer;
			await Clients.Group(GameId).SetPlayerTurn(nextPlayerNumber);
		}

		async void IConnectionGameEventHandler.GameEnded(BoardToken winningPlayerToken)
		//Or, more appropriately in this context, "MatchEnded"
		{
			await Clients.Group(GameId).MatchEnded((int) winningPlayerToken);
		}

		async void IConnectionGameEventHandler.RefreshBoard(BoardToken[] validBoard)
		{
			await Clients.Caller.Joined(Array.ConvertAll(validBoard, token => (byte) token));
		}

		bool IConnectionGameEventHandler.ShouldRefreshBoardOnInvalidPlay(BoardToken attemptedPlayToken, BoardToken existingTokenInSlot, bool isActivePlayer, byte row, byte column)
		{
			//The client shouldn't be allowing this input if they aren't the active player.
			return !isActivePlayer;
		}

		async void IConnectionGameEventHandler.TokenPlayed(BoardToken playedToken, BoardToken nextPlayerToken, byte row, byte column)
		{
			await Clients.Group(GameId).TokenPlayed((int) playedToken, row, column);

			int nextPlayerNumber = (int) nextPlayerToken;
			await Clients.Group(GameId).SetPlayerTurn(nextPlayerNumber);
		}
	}
}
