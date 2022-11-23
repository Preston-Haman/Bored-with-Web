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
	/// reflection; once the class instance has been created, a call to <see cref="CreateGame(GameInfo, string, Player[])"/> should be made.
	/// </summary>
	public abstract class SimpleGame : ISimpleGameCreation
	{
		public GameInfo Info { get; protected set; } = null!;

		public string GameId { get; protected set; } = null!;

		public HashSet<Player> Players { get; } = new();

		public event EventHandler<SimpleGame>? OnGameEnded;

		public bool Started { get; private set; } = false;

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
				Player player = new(players[i].Username);
				player.PlayerNumber = i + 1;
				Players.Add(player);
			}
		}

		/// <summary>
		/// Declares the given <paramref name="player"/> as ready to play the game.
		/// <br></br><br></br>
		/// If all competing players are ready, and the game is now starting, true is returned; false otherwise.
		/// </summary>
		/// <param name="player">The player who is ready to play.</param>
		/// <returns>True if the game starts from this call; false otherwise.</returns>
		public bool PlayerIsReady(Player player, bool ready = true)
		{
			lock (Players)
			{
				Player internalPlayer = GetInternalPlayer(player);
				internalPlayer.Ready = ready;

				if (!Started && !(from Player p in Players where !p.Ready select p).Any())
				{
					//Assignment is intentional.
					return Started = true;
				}

				return false;
			}
		}

		/// <summary>
		/// Determines if this game is still available. If the game has <see cref="Started"/>,
		/// and then all players left, this game should be ended. If there are any players
		/// remaining in the game, then the game will remain available.
		/// <br></br><br></br>
		/// If this game has not <see cref="Started"/>, then this method will return true.
		/// </summary>
		/// <returns>True if the game should remain available; false otherwise.</returns>
		public bool HasRemainingReadyPlayers()
		{
			if (!Started) return true;

			return (from Player p in Players where p.Ready select p).Any();
		}

		/// <summary>
		/// Retrieves a list of usernames representing the competing players who are ready to play. The array's
		/// indices are the corresponding player numbers minus 1 (i.e.: index 0 contains the username of player 1).
		/// </summary>
		/// <returns>A list of usernames representing the competing players who are ready to play.</returns>
		public string[] GetPlayerNames()
		{
			return (from Player p in Players
					orderby p.PlayerNumber
					select p.Username).ToArray();
		}

		/// <summary>
		/// Gets the player associated with the given <paramref name="username"/>. If no such player is competing
		/// in this game, then null is returned.
		/// </summary>
		/// <param name="username">The name of a player competing in this game to get the player number of.</param>
		/// <returns>The competing player with the given username, or null.</returns>
		public Player? GetPlayer(string username)
		{
			if (Players.TryGetValue(new Player(username), out Player? internalPlayer))
			{
				return internalPlayer;
			}

			return null;
		}

		/// <summary>
		/// Determines if a player can leave the game without forfeiting, and returns the result.
		/// <br></br><br></br>
		/// The default implementation found in <see cref="SimpleGame"/> just directly returns true.
		/// </summary>
		/// <returns>True if a player can leave the game without being counted as a loss; false otherwise.</returns>
		public virtual bool PlayerCannotLeaveWithoutForfeiting()
		{
			return true;
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

		/// <summary>
		/// Retrieves the internal player from <see cref="Players"/> by using <paramref name="externalPlayer"/> as a key.
		/// <br></br><br></br>
		/// If no such internal player exists, then an <see cref="InvalidOperationException"/> is thrown.
		/// </summary>
		/// <param name="externalPlayer">A player given from an external source.</param>
		/// <returns>The internal player from <see cref="Players"/> that matches the given <paramref name="externalPlayer"/>.</returns>
		/// <exception cref="InvalidOperationException">If no such internal player matching <paramref name="externalPlayer"/> exists.</exception>
		protected Player GetInternalPlayer(Player externalPlayer)
		{
			if (!Players.TryGetValue(externalPlayer, out Player? internalPlayer))
			{
				throw new InvalidOperationException("The given player is not a part of this game!");
			}

			return internalPlayer;
		}
	}
}
