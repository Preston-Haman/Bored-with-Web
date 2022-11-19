using Connect_X;
using Connect_X.Enums;

namespace Bored_with_Web.Games
{
	[Game("Connect-Four")]
	public class ConnectFour : SimpleGame
	{
		private readonly ConnectionGame board;

		public ConnectFour(string gameId, Player player1, Player player2) : base(gameId, player1, player2)
		{
			const byte STANDARD_CONNECT_FOUR_ROWS = 6;
			const byte STANDARD_CONNECT_FOUR_COLUMNS = 7;
			const byte STANDARD_CONNECT_FOUR_SEQUENCE_LENGTH = 4;
			board = new(STANDARD_CONNECT_FOUR_ROWS, STANDARD_CONNECT_FOUR_COLUMNS, STANDARD_CONNECT_FOUR_SEQUENCE_LENGTH);
		}

		public ConnectFour(string gameId, byte rows, byte columns, byte winningSequenceLength, params Player[] players) : base(gameId, players)
		{
			if (players.Length > byte.MaxValue)
			{
				throw new ArgumentException($"This implementation of Connect Four does not support more than {byte.MaxValue} players.", nameof(players));
			}

			board = new(rows, columns, winningSequenceLength, (byte) players.Length);
		}

		public void PlayToken(IConnectionGameEventHandler handler, Player player, byte column)
		{
			Player internalPlayer = GetInternalPlayer(player);

			board.PlayGravityToken(handler, (BoardToken) internalPlayer.PlayerNumber, column);
		}

		public void Forfeit(IConnectionGameEventHandler handler, Player player, bool isDisconnected)
		{
			Player internalPlayer = GetInternalPlayer(player);

			board.Forfeit(handler, (BoardToken) internalPlayer.PlayerNumber, !isDisconnected);
		}

		public void ClearBoard(IConnectionGameEventHandler handler, Player hasNextTurn)
		{
			Player internalPlayer = GetInternalPlayer(hasNextTurn);

			board.ClearBoard(handler, (BoardToken) internalPlayer.PlayerNumber);
		}

		private Player GetInternalPlayer(Player externalPlayer)
		{
			if (!Players.TryGetValue(externalPlayer, out Player? internalPlayer))
			{
				throw new InvalidOperationException("The given player is not a part of this game!");
			}

			return internalPlayer;
		}
	}
}
