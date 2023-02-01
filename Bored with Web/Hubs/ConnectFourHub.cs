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

		async void IConnectionGameEventHandler.ClearBoard(BoardToken newActivePlayer)
		{
			await Clients.Group(GameId).ResetGame();

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
			await Clients.Caller.SetPlayerTurn(ActiveGame.ActivePlayerNumber);
			await Clients.Caller.Joined(Array.ConvertAll(validBoard, token => (byte) token));
		}

		bool IConnectionGameEventHandler.ShouldRefreshBoardOnInvalidPlay(BoardToken attemptedPlayToken, BoardToken existingTokenInSlot, bool isActivePlayer, byte row, byte column)
		{
			//For now, we'll always return true; in the future, we could decide based on the method parameters.
			return true;
		}

		async void IConnectionGameEventHandler.TokenPlayed(BoardToken playedToken, BoardToken nextPlayerToken, byte row, byte column)
		{
			await Clients.Group(GameId).TokenPlayed((int) playedToken, row, column);

			int nextPlayerNumber = (int) nextPlayerToken;
			await Clients.Group(GameId).SetPlayerTurn(nextPlayerNumber);
		}
	}
}
