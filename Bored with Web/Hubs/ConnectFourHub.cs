using Bored_with_Web.Games;
using Connect_X;
using Connect_X.Enums;

namespace Bored_with_Web.Hubs
{
	public interface IConnectFourClient : IMultiplayerGameClient
	{
		Task Joined(byte[] board, int ourPlayerNumber);

		Task TokenPlayed(int playerNumber, byte row, byte column);

		Task MatchEnded(int winningPlayerNumber);

		Task Rematch();

		Task BoardCleared();
	}

	public class ConnectFourHub : MultiplayerGameHub<ConnectFour, IConnectFourClient>, IConnectionGameEventHandler
	{
#pragma warning disable CS1998 //These end up being async; but can't await it directly. Hope it works... :x
		protected override async Task OnJoinedGame()
		{
			ActiveGame.RefreshBoard(this);
		}

		public async Task PlayToken(byte column)
		{
			ActiveGame.PlayToken(this, CurrentPlayer, column);
		}
#pragma warning restore CS1998

		public async Task ForfeitMatch()
		{
			ActiveGame.Forfeit(this, CurrentPlayer, false);
			await Clients.OthersInGroup(GameId).Rematch();
		}

		public async Task Rematch()
		{
			await Clients.Caller.BoardCleared();

			//This could be a race condition on the client side...
			await Clients.OthersInGroup(GameId).Rematch();
		}

		async void IConnectionGameEventHandler.ClearBoard(BoardToken newActivePlayer)
		{
			await Clients.Group(GameId).BoardCleared();
		}

		async void IConnectionGameEventHandler.GameEnded(BoardToken winningPlayerToken)
		//Or, more appropriately in this context, "MatchEnded"
		{
			await Clients.Group(GameId).MatchEnded((int) winningPlayerToken);
		}

		async void IConnectionGameEventHandler.RefreshBoard(BoardToken[] validBoard)
		{
			await Clients.Caller.Joined(Array.ConvertAll(validBoard, token => (byte) token), CurrentPlayerNumber);
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
			await Clients.Group(GameId).SetPlayerTurn(nextPlayerNumber, nextPlayerNumber == CurrentPlayerNumber);
		}
	}
}
