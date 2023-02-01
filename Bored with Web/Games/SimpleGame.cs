using Bored_with_Web.Hubs;

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
		/// Whether this game has an ongoing match or not. The match is considered active if the players
		/// are still capable of performing inputs that alter the game's state.
		/// </summary>
		public bool MatchIsActive { get; protected set; } = true;

		/// <summary>
		/// Whether or not a rematch notification went out to the players in this game.
		/// </summary>
		public bool RematchWasIssued { get; set; } = false;

		/// <summary>
		/// The players who accepted a rematch and are waiting for the match to begin.
		/// </summary>
		private HashSet<Player> RematchPlayers { get; } = new();

		/// <summary>
		/// The outcomes of each match, added as they end.
		/// </summary>
		private readonly List<SimpleGameOutcome> matchOutcomes = new();

		/// <summary>
		/// The outcome of the current match. Subclasses are responsible for updating this member as
		/// their game state changes.
		/// </summary>
		protected SimpleGameOutcome currentMatchOutcome = null!;

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
			BeginTrackingNewMatch();
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

			return (from Player p in Players where p.Ready && !p.Left select p).Any();
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
		/// The default implementation found in <see cref="SimpleGame"/> just directly returns <see cref="MatchIsActive"/>.
		/// </summary>
		/// <returns>True if a player can leave the game without being counted as a loss; false otherwise.</returns>
		public virtual bool PlayerCannotLeaveWithoutForfeiting()
		{
			return MatchIsActive;
		}

		/// <summary>
		/// Removes the specified player from the game. Returns true if this action should cause the game to end.
		/// <br></br><br></br>
		/// The default implementation found in <see cref="SimpleGame"/> will mark the <paramref name="player"/> from
		/// <see cref="Players"/> as having left, and end the game if the required number of players are no longer available.
		/// </summary>
		/// <param name="player">The player that is leaving the game.</param>
		/// <param name="isConnectionTimeout">If the player's connection timed out or not.</param>
		/// <returns>True if the game should end because this player left; false otherwise.</returns>
		public virtual bool PlayerLeft(Player player, bool isConnectionTimeout = false)
		{
			Player internalPlayer = GetInternalPlayer(player);
			internalPlayer.Left = true;

			if (PlayerCannotLeaveWithoutForfeiting())
			{
				currentMatchOutcome.ForfeitingPlayers.Add(internalPlayer);
			}

			if (Info.RequiredPlayerCount > (from players in Players
											where !players.Left
											select players).Count())
			{
				//The game is ending because they left; if they have to forfeit, the match is incomplete.
				currentMatchOutcome.EndState = PlayerCannotLeaveWithoutForfeiting() ? GameEnding.INCOMPLETE : GameEnding.NONE;
				return true;
			}

			return false;
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously; it's async for subclasses to use gameHub methods.
		/// <summary>
		/// Changes the game's internal state to that of the starting state.
		/// <br></br><br></br>
		/// The default implementation found in <see cref="SimpleGame"/> only cleans some cached data relating to rematch functionality,
		/// and makes a call to <see cref="BeginTrackingNewMatch"/>.
		/// <br></br><br></br>
		/// Subclasses of <see cref="SimpleGame"/> should override this method and call the base implementation. The subclass
		/// implementation should set the values for <see cref="currentMatchOutcome"/> to reflect how the previous match ended during this method
		/// before calling the base implementation.
		/// </summary>
		public async virtual Task StartNewMatch<GameType, IMultiplayerClient>(MultiplayerGameHub<GameType, IMultiplayerClient> gameHub, Player externalPlayer)
			where GameType : SimpleGame
			where IMultiplayerClient : class, IMultiplayerGameClient
		{
			MatchIsActive = true; //This change has to happen after the rematch notification
			RematchWasIssued = false;
			RematchPlayers.Clear();
			BeginTrackingNewMatch();
		}
#pragma warning restore CS1998

		/// <summary>
		/// Forfeits the given <paramref name="externalPlayer"/> from the current match. The <paramref name="gameHub"/>
		/// will be tasked with sending this information to the clients. If the match can no longer continue, then
		/// a call to <see cref="MultiplayerGameHub{,}.IssueRematchNotification()"/> is made.
		/// <br></br><br></br>
		/// The default implementation found in <see cref="SimpleGame"/> adds the internal player that represents the given
		/// <paramref name="externalPlayer"/> to <see cref="currentMatchOutcome"/>'s <see cref="SimpleGameOutcome.ForfeitingPlayers"/>
		/// list; then, asks the <paramref name="gameHub"/> to notify the other players. If the number of competing players
		/// has dropped to 1, a rematch is also issued.
		/// </summary>
		/// <param name="gameHub">The hub handling the network connections for this game.</param>
		/// <param name="externalPlayer">The player that is forfeiting the match.</param>
		public async virtual Task ForfeitMatch<GameType, IMultiplayerClient>(MultiplayerGameHub<GameType, IMultiplayerClient> gameHub, Player externalPlayer)
			where GameType : SimpleGame
			where IMultiplayerClient : class, IMultiplayerGameClient
		{
			Player internalPlayer = GetInternalPlayer(externalPlayer);
			if (!currentMatchOutcome.ForfeitingPlayers.Contains(internalPlayer))
			{
				currentMatchOutcome.ForfeitingPlayers.Add(internalPlayer);

				//Add the player to the rematch list; if they don't want to rematch, they can leave when the game ends.
				//However, if their forfeiture is ending the game, then they are implying they want to rematch by
				//forfeiting the match instead of leaving the game.
				RematchPlayers.Add(internalPlayer);

				await gameHub.NotifyOthersOfCallerMatchForfeiture();

				//If the match is ending because of this forfeiture
				if (currentMatchOutcome.ForfeitingPlayers.Count > ((from player in Players
																	where !player.Left
																	select player).Count() - 2))
				{
					MatchIsActive = false;
					currentMatchOutcome.EndState = GameEnding.INCOMPLETE;

					RematchWasIssued = true;
					await gameHub.IssueRematchNotification();
				}
			}
		}

		/// <summary>
		/// Issues a rematch from <paramref name="externalPlayer"/> to all other players.
		/// </summary>
		/// <param name="gameHub">The hub handling the network connections for this game.</param>
		/// <param name="externalPlayer">The player that is issuing the rematch.</param>
		public async Task IssueRematch<GameType, IMultiplayerClient>(MultiplayerGameHub<GameType, IMultiplayerClient> gameHub, Player externalPlayer)
			where GameType : SimpleGame
			where IMultiplayerClient : class, IMultiplayerGameClient
		{
			Player internalPlayer = GetInternalPlayer(externalPlayer);
			RematchWasIssued = true;
			RematchPlayers.Add(internalPlayer);

			await gameHub.IssueRematchNotification();
		}

		/// <summary>
		/// This method should be called when a player accepts a rematch challenge, or leaves the game after being issued one.
		/// <br></br><br></br>
		/// As players accept the rematch, their game will be reset via the <paramref name="gameHub"/>. When all players
		/// have either accepted the rematch, or left, a new match will begin if possible. Whether a new match begins,
		/// or the overall game session ends, the players will be notified via the <paramref name="gameHub"/>.
		/// <br></br><br></br>
		/// If this is being called from a player leaving, the call to <see cref="PlayerLeft"/> should occur first.
		/// 
		/// </summary>
		/// <param name="gameHub">The hub handling the network connections for this game.</param>
		/// <param name="externalPlayer">The player accepting the rematch or leaving.</param>
		public async virtual Task AcceptRematchOrLeave<GameType, IMultiplayerClient>(MultiplayerGameHub<GameType, IMultiplayerClient> gameHub, Player externalPlayer)
			where GameType : SimpleGame
			where IMultiplayerClient : class, IMultiplayerGameClient
		{
			if (!RematchWasIssued)
			{
				throw new InvalidOperationException("There are no active rematch requests to accept.");
			}

			Player internalPlayer = GetInternalPlayer(externalPlayer);
			if (!internalPlayer.Left)
			{
				if (RematchPlayers.Add(internalPlayer))
				{
					await gameHub.AcceptRematch();
				}
			}
			else
			{
				//If they left, then we should remove them from RematchPlayers; 'though, it's not strictly necessary.
				RematchPlayers.Remove(internalPlayer);
			}

			if (RematchPlayers.Count >= (from player in Players where !player.Left select player).Count())
			{
				//If all remaining players are waiting for the rematch to begin...
				if (RematchPlayers.Count >= Info.RequiredPlayerCount)
				{
					//... and enough of them remain to continue: Start a new match.
					await StartNewMatch(gameHub, internalPlayer);
				}
				else
				{
					//... but there aren't enough of them to continue: End the game session.
					await gameHub.EndGameSession();
				}
			}
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
		/// Adds the <see cref="currentMatchOutcome"/> to <see cref="matchOutcomes"/>, and then resets
		/// <see cref="currentMatchOutcome"/> to one that is considered to be not started.
		/// </summary>
		protected void BeginTrackingNewMatch()
		{
			if (currentMatchOutcome is not null)
			{
				matchOutcomes.Add(currentMatchOutcome);
			}

			currentMatchOutcome = new()
			{
				EndState = GameEnding.NONE,
				Game = this
			};
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
		protected virtual IEnumerable<SimpleGameOutcome> GetOutcome()
		{
			//Add the last match to the list before returning it.
			BeginTrackingNewMatch();
			return matchOutcomes;
		}
	}
}
