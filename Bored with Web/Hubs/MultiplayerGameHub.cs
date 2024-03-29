﻿using Bored_with_Web.Games;
using Microsoft.AspNetCore.SignalR;

namespace Bored_with_Web.Hubs
{
	/// <summary>
	/// Defines methods that are available on the client side of a <see cref="MultiplayerGameHub{,}"/>.
	/// </summary>
	public interface IMultiplayerGameClient
	{
		/// <summary>
		/// Called when the user connects. Implementations should store <paramref name="userPlayerNumber"/>
		/// for later; as it is how players are distinguished from each other.
		/// </summary>
		/// <param name="userPlayerNumber">The numeric identifier of the user for the duration of the game.</param>
		Task SetUserPlayerNumber(int userPlayerNumber);

		/// <summary>
		/// Called when a player has connected. Implementations may use the provided information
		/// to update their representation of the game.
		/// <br></br><br></br>
		/// This method is also called when a player rejoins after having been disconnected.
		/// </summary>
		/// <param name="playerName">The name of the connecting player.</param>
		/// <param name="playerNumber">The numeric identifier of the player; this is unique to this game only.</param>
		Task PlayerConnected(string playerName, int playerNumber);

		/// <summary>
		/// Called when a player has lost connection to an ongoing game. Implementations should let the user know
		/// that the specified player has disconnected; and that the game will wait <paramref name="timeoutInSeconds"/>
		/// seconds for that player to rejoin before they automatically forfeit.
		/// </summary>
		/// <param name="playerName">The name of the connecting player.</param>
		/// <param name="playerNumber">The numeric identifier of the player; this is unique to this game only.</param>
		/// <param name="timeoutInSeconds">The length of time the game will wait for the disconnected player to rejoin.</param>
		Task PlayerDisconnected(string playerName, int playerNumber, int timeoutInSeconds);

		/// <summary>
		/// Called when the specified player has forfeited the game. This may be an automatic consequence of them
		/// having lost connection; in such a case, <paramref name="isConnectionTimeout"/> will be true.
		/// Implementations may let the user know that the specified player is no longer competing.
		/// </summary>
		/// <param name="playerName">The name of the forfeiting player.</param>
		/// <param name="playerNumber">The numeric identifier of the player; this is unique to this game only.</param>
		/// <param name="isConnectionTimeout">Whether or not the player automatically forfeited from their connection being timed out.</param>
		Task PlayerForfeited(string playerName, int playerNumber, bool isConnectionTimeout);

		/// <summary>
		/// Called when the specified user has joined as a spectator. Implementations may use this information
		/// to update a displayed list of spectators to the user.
		/// </summary>
		/// <param name="spectatorName">The name of the spectating user.</param>
		Task SpectatorConnected(string spectatorName);

		/// <summary>
		/// Called when the specified user has stopped spectating the game. Implementations may use this information
		/// to update a displayed list of spectators to the user.
		/// </summary>
		/// <param name="spectatorName">The name of the spectating user.</param>
		Task SpectatorDisconnected(string spectatorName);

		/// <summary>
		/// Called when the user first connects to a game. Implementations may use the provided information to update their
		/// representation of the game's players and spectators.
		/// <br></br><br></br>
		/// The client's own name will appear in the list of players.
		/// </summary>
		/// <param name="players">The names of the players in the game, the indices match with <paramref name="playerNumbers"/>.</param>
		/// <param name="playerNumbers">The numeric representation of each player listed in <paramref name="players"/>.</param>
		/// <param name="spectators">The names of the users spectating the game.</param>
		Task UpdateVisiblePlayers(string[] players, int[] playerNumbers, string[] spectators);

		/// <summary>
		/// Called when all the players have connected, and the game is ready to begin. Implementors may use this
		/// as a way to enable all game-related inputs.
		/// </summary>
		Task StartGame();

		/// <summary>
		/// Called when a turn-based game is allotting a turn to the specified player. Implementors may use the provided
		/// information to update their representation of which player is currently allowed to perform actions; and, if
		/// necessary, they may also disable, or enable game-related inputs.
		/// </summary>
		/// <param name="playerNumber">The numeric identifier of the player; this is unique to this game only.</param>
		Task SetPlayerTurn(int playerNumber);

		/// <summary>
		/// Called when a game supporting matches had one of its players forfeit the match. Implementors may use the
		/// provided information to inform the other players of this event.
		/// </summary>
		/// <param name="playerName">The name of the forfeiting player.</param>
		/// <param name="playerNumber">The numeric identifier of the player; this is unique to this game only.</param>
		Task PlayerForfeitedMatch(string playerName, int playerNumber);

		/// <summary>
		/// Called when the match has ended; if the match ended in a draw, then <paramref name="winningPlayerNumber"/>
		/// will be zero.
		/// </summary>
		/// <param name="winningPlayerNumber">The player number representing the winning player; or zero, if there was no winner.</param>
		Task MatchEnded(int winningPlayerNumber);

		/// <summary>
		/// Called when the opponent wants to challenge the player to another match; or when an existing rematch challenge was accepted.
		/// Implementations should inform the user that this challenge was issued; or, that the challenge was accepted.
		/// </summary>
		/// <param name="playerName">The name of the player issuing the rematch.</param>
		/// <param name="playerNumber">The numeric identifier of the player; this is unique to this game only.</param>
		Task Rematch(string playerName, int playerNumber);

		/// <summary>
		/// Called when a player accepts a rematch request. Implementations should inform the user that the specified
		/// player accepted a rematch.
		/// </summary>
		/// <param name="playerName">The name of the player accepting the rematch.</param>
		/// <param name="playerNumber">The numeric identifier of the player; this is unique to this game only.</param>
		Task RematchAccepted(string playerName, int playerNumber);

		/// <summary>
		/// Called when a match has concluded. Implementations should reset the state of the game to the starting conditions.
		/// </summary>
		Task ResetGame();

		/// <summary>
		/// Called when the game has fully ended. The connection should be closed. Implementors may allow the user
		/// to ponder the last state of the game; but should inform the user that no more gameplay will be available
		/// until they create a new game session.
		/// </summary>
		Task EndGame();
	}

	/// <summary>
	/// A basic implementation for managing the connections of multiplayer games. Concrete subclasses are responsible
	/// for providing game-state information to rejoining players.
	/// </summary>
	/// <typeparam name="GameType">The <see cref="SimpleGame"/> subclass that this hub manages.</typeparam>
	/// <typeparam name="IMultiplayerClient">An interface, implementing <see cref="IMultiplayerGameClient"/>, that defines methods available on the client
	/// for the game represented by the concrete subclass' implementation.</typeparam>
	public abstract class MultiplayerGameHub<GameType, IMultiplayerClient> : UsernameAwareHub<IMultiplayerClient>
		where GameType: SimpleGame
		where IMultiplayerClient: class, IMultiplayerGameClient
	{
		/// <summary>
		/// The gameId this connection has joined. This is provided in the "game" query string by the client.
		/// </summary>
		protected string GameId { get { return Context.GetHttpContext()!.Request.Query["game"]; } }

		/// <summary>
		/// The group name for connections that joined this game as a spectator.
		/// </summary>
		protected string SpectatorGroup { get { return $"Spectator-{GameId}"; } }

		/// <summary>
		/// The <see cref="SimpleGame"/> implementation that is storing all the internal state for the game this hub is managing connections for.
		/// </summary>
		protected GameType ActiveGame
		{
			get
			{
				SimpleGame? game = GameService.GetGame(GameId);

				if (game is null)
				{
					throw new NullReferenceException("The specified game is either no longer active, or never existed.");
				}

				if (game is not GameType value)
				{
					//TODO: I'm not sure if this error message is correct.
					throw new InvalidOperationException($"{GetType().Name} is only capable of handling active {typeof(GameType)} games.");
				}

				return value;
			}
		}

		/// <summary>
		/// The player that is being represented by the current caller connection.
		/// </summary>
		protected Player CurrentPlayer
		{
			get
			{
				if (GetCallerUsername(out string username) && ActiveGame.GetPlayer(username) is Player player)
					return player;

				throw new InvalidOperationException($"The current connection does not represent an active player in this game: {GameId}");
			}
		}

		/// <summary>
		/// The number being stored in the <see cref="Player.PlayerNumber"/> property of <see cref="CurrentPlayer"/>.
		/// </summary>
		protected int CurrentPlayerNumber { get { return CurrentPlayer.PlayerNumber; } }

		public async override Task OnConnectedAsync()
		{
			try
			{
				if (GetCallerUsername(out string username))
				{
					await Groups.AddToGroupAsync(Context.ConnectionId, GameId);

					bool startGame = ActiveGame.PlayerIsReady(CurrentPlayer);

					await Clients.Caller.SetUserPlayerNumber(CurrentPlayerNumber);

					string[] playerNames = (from p in ActiveGame.Players orderby p.PlayerNumber select p.Username).ToArray();
					int[] playerNumbers = (from p in ActiveGame.Players orderby p.PlayerNumber select p.PlayerNumber).ToArray();
					//TODO: Spectating is not implemented, yet; but when it is, replace the empty array with the spectators.
					await Clients.Caller.UpdateVisiblePlayers(playerNames, playerNumbers, Array.Empty<string>());
					await OnJoinedGame();

					await Clients.OthersInGroup(GameId).PlayerConnected(username, CurrentPlayerNumber);

					if (startGame)
					{
						await Clients.Group(GameId).StartGame();
					}
					else if (ActiveGame.Started)
					{
						//Game already started; so this player must be rejoining.
						GameService.CancelForfeitTimeout(GameId, CurrentPlayer);
					}
				}
			}
			catch (NullReferenceException)
			{
				await Clients.Caller.EndGame();
			}
			
			await base.OnConnectedAsync();
		}

		/// <summary>
		/// Called when a user joins/rejoins an ongoing game.
		/// <br></br><br></br>
		/// Information about the current game can be retrieved from the <see cref="ActiveGame"/> property.
		/// </summary>
		protected abstract Task OnJoinedGame();

		/// <summary>
		/// Called when a user attempts to issue or accept a rematch, or declare their own forfeiture of the current match.
		/// <br></br><br></br>
		/// The default implementation found in <see cref="MultiplayerGameHub{,}"/> only fully handles the trivial
		/// case of two player games. Subclasses are responsible for overriding this method when their game
		/// implementations support multiple matches with more than two players.
		/// </summary>
		public virtual async Task ForfeitAndRematch()
		{
			//If the match is active, forfeit the caller.
			if (ActiveGame.MatchIsActive)
			{
				//If the match becomes inactive from this call, a rematch notification will go out.
				await ActiveGame.ForfeitMatch(this, CurrentPlayer);
			}
			else
			{
				if (ActiveGame.RematchWasIssued)
				{
					//The match isn't active, and a rematch notification went out already.
					await ActiveGame.AcceptRematchOrLeave(this, CurrentPlayer);
				}
				else
				{
					//The match ended (probably naturally), and the caller is the first player trying to issue a rematch.
					await ActiveGame.IssueRematch(this, CurrentPlayer);
				}
			}
		}

		/// <summary>
		/// Sends out a notification to all other players in the game that the caller has accepted a rematch;
		/// and, resets the caller's game to the starting state.
		/// </summary>
		public virtual async Task AcceptRematch()
		{
			await ResetCallerGame();
			await Clients.OthersInGroup(GameId).RematchAccepted(CurrentPlayer.Username, CurrentPlayerNumber);
		}

		/// <summary>
		/// Sends out a forfeit notification to all other players in the game.
		/// </summary>
		public virtual async Task NotifyOthersOfCallerMatchForfeiture()
		{
			await Clients.OthersInGroup(GameId).PlayerForfeitedMatch(CurrentPlayer.Username, CurrentPlayerNumber);
		}

		/// <summary>
		/// Sends out a rematch notification to all other players in the game; and,
		/// resets the caller's game to the starting state.
		/// </summary>
		public virtual async Task IssueRematchNotification()
		{
			if (!ActiveGame.MatchIsActive)
			{
				await ResetCallerGame();
				await Clients.OthersInGroup(GameId).Rematch(CurrentPlayer.Username, CurrentPlayerNumber);
			}
		}

		/// <summary>
		/// Sends a notification to the caller that their game's state should be reset to the initial one.
		/// </summary>
		public virtual async Task ResetCallerGame()
		{
			await Clients.Caller.ResetGame();
		}

		/// <summary>
		/// Sends a notification to the clients participating in the game that the match has ended with the
		/// specified <paramref name="winner"/>. In the case that the game ended without a victor, i.e.:
		/// <paramref name="winner"/> is null, the clients will be notified of a stalemate.
		/// </summary>
		/// <param name="winner">The player who has emerged victorious, or null if there was no victor.</param>
		public virtual async Task EndMatch(Player? winner = null)
		{
			await Clients.Group(GameId).SetPlayerTurn(0);
			await Clients.Group(GameId).MatchEnded(winner?.PlayerNumber ?? 0);
		}

		/// <summary>
		/// Sends a notification to the clients that the given player, <paramref name="hasNextTurn"/>, is
		/// currently being allotted a turn.
		/// <br></br><br></br>
		/// This method makes a direct call to <see cref="IMultiplayerGameClient.SetPlayerTurn(int)"/>, and is meant
		/// to be used externally.
		/// </summary>
		/// <param name="hasFirstTurn">The player with the first turn of the match.</param>
		public async Task SetPlayerTurn(Player hasNextTurn)
		{
			await Clients.Group(GameId).SetPlayerTurn(hasNextTurn.PlayerNumber);
		}

		/// <summary>
		/// Informs connected clients that the game session has ended.
		/// </summary>
		public virtual async Task EndGameSession()
		{
			await Clients.Group(GameId).EndGame();
			ActiveGame.EndGame();
		}

		public async override Task OnDisconnectedAsync(Exception? exception)
		{
			const int TimeoutSeconds = 60;
			try
			{
				if (GetCallerUsername(out string username))
				{
					await Groups.RemoveFromGroupAsync(Context.ConnectionId, GameId);

					bool mustForfeit = ActiveGame.PlayerCannotLeaveWithoutForfeiting();
					ActiveGame.PlayerIsReady(CurrentPlayer, false);

					bool gameShouldEnd = !ActiveGame.HasRemainingReadyPlayers();

					if (gameShouldEnd)
					{
						//Means all competing players are gone.
						await Clients.Group(SpectatorGroup).EndGame();
						ActiveGame.EndGame();
					}
					else
					{
						if (exception is null)
						{
							//Player left on purpose?
							gameShouldEnd = ActiveGame.PlayerLeft(CurrentPlayer);

							if (mustForfeit)
							{
								//The message on the client side for this says the player left.
								await Clients.Group(GameId).PlayerForfeited(username, CurrentPlayerNumber, isConnectionTimeout: false);
							} else
							{
								//The message on the client side here just says they disconnected.
								await Clients.Group(GameId).PlayerDisconnected(username, CurrentPlayerNumber, 0);
							}

							if (ActiveGame.RematchWasIssued && !gameShouldEnd)
							{
								await ActiveGame.AcceptRematchOrLeave(this, CurrentPlayer);
							}

							if (gameShouldEnd)
							{
								await Clients.Group(GameId).EndGame();
								ActiveGame.EndGame();
							}
						}
						else
						{
							await Clients.Group(GameId).PlayerDisconnected(username, CurrentPlayerNumber, TimeoutSeconds);

							if (mustForfeit)
							{
								GameService.AddForfeitTimeout<GameType, IMultiplayerClient>(GetType(), GameId, CurrentPlayer, TimeoutSeconds);
							}
						}
					}
				}
			}
			catch (NullReferenceException)
			{
				//Likely an invalid user or a game that no longer exists.
				//Do nothing; they just disconnected.
			}

			await base.OnDisconnectedAsync(exception);
		}

		/// <summary>
		/// Notifies clients playing the game represented by the given <paramref name="gameId"/> that the specified <paramref name="player"/>
		/// has lost connection and forfeited the game.
		/// <br></br><br></br>
		/// This method is presented as a way for calling clients from outside the hub without relying on external
		/// classes to understand how the clients should be notified.
		/// </summary>
		/// <param name="context">The context providing access to the SignalR clients for the lobby group; this should be created through DI.</param>
		/// <param name="gameId">The unique, human readable, identifier for the game that ended.</param>
		/// <param name="player">The player who has lost connection and timed out.</param>
		/// <param name="gameEnded">Whether or not the game has ended due to the <paramref name="player"/> being timed out.</param>
		public static async void OnForfeitTimeout<THub, TGame, TClient>(IHubContext<THub, TClient> context, string gameId, Player player, bool gameEnded)
			where THub : MultiplayerGameHub<TGame, TClient>
			where TGame : GameType
			where TClient : class, IMultiplayerClient
		{
			await context!.Clients.Group(gameId).PlayerForfeited(player.Username, player.PlayerNumber, isConnectionTimeout: true);

			if (gameEnded)
			{
				await context.Clients.Group(gameId).EndGame();
			}
		}
	}
}
