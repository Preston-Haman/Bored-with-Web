namespace Bored_with_Web.Games
{
	[AttributeUsage(AttributeTargets.Class)]
	public class GameAttribute : Attribute
	{
		public GameInfo Info { get; }

		public GameAttribute(string gameRouteId)
		{
			if (CanonicalGames.GetGameInfoByRouteId(gameRouteId) is not GameInfo game)
			{
				throw new ArgumentException("The given RouteId does not match any known Games.", nameof(gameRouteId));
			}

			Info = game;
		}
	}

	public abstract class SimpleGame
	{
		public GameInfo Info { get; }

		public string GameId { get; }

		public HashSet<Player> Players { get; } = new();

		public event EventHandler<SimpleGame>? OnGameEnded;

		public SimpleGame(string gameId, params Player[] players)
		{
			if (Attribute.GetCustomAttribute(this.GetType(), typeof(GameAttribute)) is not GameAttribute game)
			{
				throw new NotImplementedException($"SimpleGame subclasses must be marked with a {nameof(GameAttribute)}.");
			}

			Info = game.Info;
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
