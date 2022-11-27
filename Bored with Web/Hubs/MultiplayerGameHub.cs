using Bored_with_Web.Games;
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
		/// <param name="playerName">The name of the connecting player.</param>
		/// <param name="playerNumber">The numeric identifier of the player; this is unique to this game only.</param>
		/// <param name="isConnectionTimeout">If the forfeit was caused by a disconnection or not.</param>
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
		/// <param name="players">The names of the players in the game, in the order of their player number (offset by 1; index 0 is player 1, and so on).</param>
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

		protected string SpectatorGroup { get { return $"Spectator-{GameId}"; } }

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

		protected Player CurrentPlayer
		{
			get
			{
				if (GetCallerUsername(out string username) && ActiveGame.GetPlayer(username) is Player player)
					return player;

				throw new InvalidOperationException($"The current connection does not represent an active player in this game: {GameId}");
			}
		}

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
							//Cache the game and player number because it might be removed from GameService if PlayerLeft returns true.
							SimpleGame activeGame = ActiveGame;
							int currentPlayerNumber = CurrentPlayerNumber;
							bool gameEnded = ActiveGame.PlayerLeft(CurrentPlayer);

							await Clients.Group(GameId).PlayerDisconnected(username, currentPlayerNumber, 0);
							if (mustForfeit)
							{
								//TODO: Track that the player lost in their stats.
								await Clients.Group(GameId).PlayerForfeited(username, currentPlayerNumber, false);
							}

							if (gameEnded)
							{
								await Clients.Group(GameId).EndGame();
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

		public static async void OnForfeitTimeout<THub, TGame, TClient>(IHubContext<THub, TClient> context, string gameId, Player player, bool gameEnded)
			where THub : MultiplayerGameHub<TGame, TClient>
			where TGame : GameType
			where TClient : class, IMultiplayerClient
		{
			await context!.Clients.Group(gameId).PlayerForfeited(player.Username, player.PlayerNumber, true);

			if (gameEnded)
			{
				await context.Clients.Group(gameId).EndGame();
			}
		}
	}
}
