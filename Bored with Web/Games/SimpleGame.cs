using Bored_with_Web.Models;

namespace Bored_with_Web.Games
{
	public abstract class SimpleGame
	{
		public GameInfo Info { get; }

		public string GameId { get; }

		public HashSet<Player> Players { get; } = new();

		public event EventHandler<SimpleGame>? OnGameEnded;

		public SimpleGame(GameInfo gameInfo, string gameId, params Player[] players)
		{
			Info = gameInfo;
			GameId = gameId;

			for (int i = 0; i < players.Length; i++)
			{
				Player p = players[i];
				p.PlayerNumber = i + 1;
				Players.Add(p);
			}
		}

		/// <summary>
		/// The default implementation found in <see cref="SimpleGame"/> just directly raises events through <see cref="OnGameEnded"/>.
		/// </summary>
		public virtual void EndGame()
		{
			if (OnGameEnded is EventHandler<SimpleGame> handler)
			{
				handler(this, this);
			}
		}
	}
}
