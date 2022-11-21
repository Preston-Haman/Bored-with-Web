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

	public interface ISimpleGameCreation
	{
		/// <summary>
		/// Initializes a game by applying the given <paramref name="gameId"/> and <paramref name="players"/>
		/// to the internal state.
		/// </summary>
		/// <param name="info">The <see cref="GameInfo"/> associated with the game being created.</param>
		/// <param name="gameId">A string representing this instance of a game.</param>
		/// <param name="players">The players that will be competing in this game.</param>
		public void CreateGame(GameInfo info, string gameId, params Player[] players);
	}

	/// <summary>
	/// Represents a game where the internal state is simple in nature. Examples might include Connect Four, Checkers, and Chess.
	/// <br></br><br></br>
	/// Subclasses must offer a parameterless constructor. This class hierarchy is generally meant to be instantiated through
	/// reflection; once the class instance has been created, a call to <see cref="CreateGame(string, Player[])"/> should be made.
	/// </summary>
	public abstract class SimpleGame : ISimpleGameCreation
	{
		public GameInfo Info { get; protected set; } = null!;

		public string GameId { get; protected set; } = null!;

		public HashSet<Player> Players { get; } = new();

		public event EventHandler<SimpleGame>? OnGameEnded;

		public void CreateGame(GameInfo info, string gameId, params Player[] players)
		{
			if (Attribute.GetCustomAttribute(this.GetType(), typeof(GameAttribute)) is not GameAttribute game)
			{
				throw new NotImplementedException($"SimpleGame subclasses must be marked with a {nameof(GameAttribute)}.");
			}

			if (info != game.Info)
			{
				//TODO: Add name of subclass through reflection.
				throw new ArgumentException($"The given {nameof(GameInfo)} does not match this class.", nameof(info));
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
