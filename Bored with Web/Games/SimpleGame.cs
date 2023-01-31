namespace Bored_with_Web.Games
{
	/// <summary>
	/// An attribute that must be applied to concrete implementations of <see cref="SimpleGame"/>.
	/// <br></br><br></br>
	/// This attribute defines what game the implementation is for, based on the game's <see cref="GameInfo.RouteId"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class GameAttribute : Attribute
	{
		/// <summary>
		/// The game being represented by the class this attribute is applied to.
		/// </summary>
		public GameInfo Info { get; }

		/// <summary>
		/// Specifies the <see cref="GameInfo.RouteId"/> of the game being represented by the class this attribute is
		/// applied to. If the <see cref="GameInfo.RouteId"/> is not recognized by <see cref="CanonicalGames"/>,
		/// then an <see cref="ArgumentException"/> is thrown.
		/// </summary>
		/// <param name="gameRouteId">The title of the game, as it appears in the website url.</param>
		/// <exception cref="ArgumentException">If the <paramref name="gameRouteId"/> is invalid.</exception>
		public GameAttribute(string gameRouteId)
		{
			if (CanonicalGames.GetGameInfoByRouteId(gameRouteId) is not GameInfo game)
			{
				throw new ArgumentException("The given RouteId does not match any known Games.", nameof(gameRouteId));
			}

			Info = game;
		}
	}

	/// <summary>
	/// Represents a game where the internal state is simple in nature. Examples might include Connect Four, Checkers, and Chess.
	/// <br></br><br></br>
	/// Subclasses must offer a parameterless constructor. This class hierarchy is generally meant to be instantiated through
	/// reflection; once the class instance has been created, a call to <see cref="CreateGame(GameInfo, string, Player[])"/> should be made.
	/// </summary>
	public abstract class SimpleGame
	{
		/// <summary>
		/// Information about the game represented.
		/// </summary>
		public GameInfo Info { get; protected set; } = null!;

		/// <summary>
		/// The unique, human readable, identifier of this game instance.
		/// </summary>
		public string GameId { get; protected set; } = null!;

		/// <summary>
		/// The set of players competing in this game.
		/// </summary>
		public HashSet<Player> Players { get; } = new();

		/// <summary>
		/// A publisher of the event of this game ending.
		/// <br></br><br></br>
		/// Subscribers may register directly, and will be notified of this game ending.
		/// </summary>
		public event EventHandler<IEnumerable<SimpleGameOutcome>>? OnGameEnded;

		/// <summary>
		/// Whether this game has started or not. This is defined by all the players
		/// who are participating in this game having been marked as ready (see <see cref="PlayerIsReady"/>).
		/// </summary>
		public bool Started { get; private set; } = false;

		/// <summary>
		/// Initializes a game by applying the given <paramref name="gameId"/> and <paramref name="players"/>
		/// to the internal state.
		/// </summary>
		/// <param name="info">The <see cref="GameInfo"/> associated with the game being created.</param>
		/// <param name="gameId">A string representing this instance of a game.</param>
		/// <param name="players">The players that will be competing in this game.</param>
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
		/// Removes the specified player from the game. Returns true if this action has caused the game to end.
		/// <br></br><br></br>
		/// The default implementation found in <see cref="SimpleGame"/> will removed the <paramref name="player"/> from
		/// <see cref="Players"/>, and end the game if the required number of players are no longer available.
		/// </summary>
		/// <param name="player">The player that is leaving the game.</param>
		/// <param name="isConnectionTimeout">If the player's connection timed out or not.</param>
		/// <returns>True if the game ends because this player left; false otherwise.</returns>
		public virtual bool PlayerLeft(Player player, bool isConnectionTimeout = false)
		{
			Player internalPlayer = GetInternalPlayer(player);
			internalPlayer.Left = true;

			if (Info.RequiredPlayerCount > (from players in Players
											where !players.Left
											select players).Count())
			{
				EndGame();
				return true;
			}

			return false;
		}

		/// <summary>
		/// The default implementation found in <see cref="SimpleGame"/> just directly raises events through <see cref="OnGameEnded"/>.
		/// </summary>
		public virtual void EndGame()
		{
			if (OnGameEnded is EventHandler<IEnumerable<SimpleGameOutcome>> handler)
			{
				handler(this, GetOutcome());
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

		/// <summary>
		/// Returns the outcome of this game's competition. Games that allow matches may return multiple
		/// outcomes, one for each match. This method is only called when the game ends, with the returned value
		/// being published out to subscribers of <see cref="OnGameEnded"/>.
		/// <br></br><br></br>
		/// The outcome of a game includes the ending state of the game, the number of turns taken (if applicable),
		/// and the list of players that won, lost, or forfeited. See <see cref="SimpleGameOutcome"/> for more information.
		/// </summary>
		/// <returns>The outcome, or list of outcomes, of this game.</returns>
		protected abstract IEnumerable<SimpleGameOutcome> GetOutcome();
	}
}
