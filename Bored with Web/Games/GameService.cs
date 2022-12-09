using Bored_with_Web.Data;
using Bored_with_Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Bored_with_Web.Games
{
	/// <summary>
	/// An entry point to the internal state of games, and game lobbies, being tracked by this application.
	/// </summary>
	public static class GameService
	{
		/// <summary>
		/// The service provider for DI for this application.
		/// </summary>
		public static IServiceProvider DIServices { get; set; } = null!;

		/// <summary>
		/// An object to lock on when generating ID's.
		/// </summary>
		private static readonly object ID_LOCK = new();

		/// <summary>
		/// A numeric value that can be used to represent a game.
		/// </summary>
		private static uint nextGameId = 0;

		/// <summary>
		/// A set of <see cref="GameLobby"/> instances for a canonical instance of <see cref="GameInfo"/>.
		/// </summary>
		private static readonly Dictionary<GameInfo, List<GameLobby>> GAME_LOBBIES_BY_GAME = new();

		/// <summary>
		/// A set of ongoing games mapped by the lobby group which they started from.
		/// </summary>
		private static readonly Dictionary<string, HashSet<string>> SIMPLE_GAME_IDS_BY_LOBBY_GROUP = new();

		/// <summary>
		/// A set of ongoing games mapped by their ID's.
		/// </summary>
		private static readonly Dictionary<string, SimpleGame> SIMPLE_GAMES_BY_ID = new();

		/// <summary>
		/// A set of players who are being allotted a length of time to reconnect to their game, mapped by game ID.
		/// </summary>
		private static readonly Dictionary<string, HashSet<Player>> PLAYER_FORFEIT_TIMEOUTS_BY_GAME_ID = new();

		/// <summary>
		/// Retrieves the number of players that are participating in the specified <paramref name="game"/>.
		/// </summary>
		/// <param name="game">The game of interest.</param>
		/// <returns>The number of players participating in the game of interest.</returns>
		public static int GetPlayerCount(GameInfo game)
		{
			int count = 0;
			if (GAME_LOBBIES_BY_GAME.TryGetValue(game, out List<GameLobby>? lobbies))
			{
				foreach (GameLobby lobby in lobbies)
				{
					count += lobby.Players.Count;
				}
			}

			//TODO: It'd probably be better to cache this in another dictionary and pull it from there... maybe Dictionary<GameInfo, int> storing player + spectator count.
			List<SimpleGame> activeGames = (from entry in SIMPLE_GAMES_BY_ID
											where entry.Key.StartsWith(game.RouteId)
											select entry.Value).ToList();
			
			foreach (SimpleGame sGame in activeGames)
			{
				count += sGame.Players.Count;
			}
			return count;
		}

		/// <summary>
		/// Checks if the given <paramref name="player"/> is within a lobby for the game represented by the given <paramref name="gameRouteId"/>,
		/// and returns true if they are.
		/// <br></br><br></br>
		/// <paramref name="lobby"/> is populated with the <see cref="GameLobby"/> the given <paramref name="player"/> is in if this method returns true.
		/// </summary>
		/// <param name="player">The player of interest.</param>
		/// <param name="gameRouteId">The title of the game, as it appears in the url.</param>
		/// <param name="lobby">The lobby the player is in, or null if they are not in a lobby for the specified <paramref name="gameRouteId"/>.</param>
		/// <returns>True if the player is within a lobby representing the given <paramref name="gameRouteId"/>; false otherwise.</returns>
		public static bool IsPlayerInLobby(Player player, string gameRouteId, out GameLobby? lobby)
		{
			GameInfo game = GetGameInfo(gameRouteId);

			if (GAME_LOBBIES_BY_GAME.TryGetValue(game, out List<GameLobby>? lobbies))
			{
				foreach (GameLobby lob in lobbies)
				{
					if (lob.Players.Contains(player))
					{
						lobby = lob;
						return true;
					}
				}
			}
			
			lobby = null;
			return false;
		}

		/// <summary>
		/// Adds a player to a lobby for the given <paramref name="gameRouteId"/>, and returns it.
		/// <br></br><br></br>
		/// If the player is already in a lobby for the given <paramref name="gameRouteId"/>, then an <see cref="InvalidOperationException"/> is thrown.
		/// </summary>
		/// <param name="player">The player to add to the lobby.</param>
		/// <param name="gameRouteId">The title of the game, as it appears in the url.</param>
		/// <returns>The game lobby the <paramref name="player"/> was added to.</returns>
		/// <exception cref="InvalidOperationException">If the player was already in a lobby for the specified <paramref name="gameRouteId"/>.</exception>
		public static GameLobby AddPlayerToLobby(Player player, string gameRouteId)
		{
			if (IsPlayerInLobby(player, gameRouteId, out _))
			{
				throw new InvalidOperationException("The specified player is already in a lobby for the specified game.");
			}

			GameLobby lobby = GetOrCreateGameLobby(gameRouteId);
			lobby.AddPlayer(player);
			return lobby;
		}

		/// <summary>
		/// Creates a <see cref="SimpleGame"/> that implements handling for the specified <paramref name="gameRouteId"/>,
		/// and returns it.
		/// </summary>
		/// <param name="lobbyGroup">The lobby that matched the players together.</param>
		/// <param name="gameRouteId">The title of the game, as it appears in the url.</param>
		/// <param name="players">The players who will be competing in the new game.</param>
		/// <returns>The concrete implementation of <see cref="SimpleGame"/> that implements handling for the specified <paramref name="gameRouteId"/>.</returns>
		public static SimpleGame CreateNextGame(string lobbyGroup, string gameRouteId, Player[] players)
		{
			GameInfo game = GetGameInfo(gameRouteId);

			MethodInfo method = typeof(GameService).GetMethod(nameof(CreateNextGame), BindingFlags.Static | BindingFlags.NonPublic, new Type[] {typeof(string), typeof(Player[]), typeof(string)})!;
			return (SimpleGame) method.MakeGenericMethod(game.ImplementingType).Invoke(null, new object[] { gameRouteId, players, lobbyGroup })!;
		}

		/// <summary>
		/// Does the legwork for <see cref="CreateNextGame(string, string, Player[])"/>.
		/// </summary>
		/// <typeparam name="GameType">The type of the concrete implementation of <see cref="SimpleGame"/> to create.</typeparam>
		/// <param name="gameRouteId">The title of the game, as it appears in the url.</param>
		/// <param name="players">The players who will be competing in the new game.</param>
		/// <param name="lobbyGroup">The lobby that matched the players together.</param>
		/// <returns>The concrete implementation of <see cref="SimpleGame"/> that implements handling for the specified <paramref name="gameRouteId"/>.</returns>
		private static GameType CreateNextGame<GameType>(string gameRouteId, Player[] players, string lobbyGroup) //Parameter order is to avoid ambiguity with Reflection
			where GameType: SimpleGame, new()
		{
			GameInfo info = GetGameInfo(gameRouteId);

			string gameId;
			lock (ID_LOCK)
			{
				gameId = $"{nextGameId++}";
			}

			GameType game = new();
			game.CreateGame(info, gameId, players);
			AddGame(game, lobbyGroup);
			return game;
		}

		/// <summary>
		/// Retrieves the <see cref="SimpleGame"/> represented by the given <paramref name="gameId"/>, and returns it
		/// if it exists.
		/// </summary>
		/// <param name="gameId">The unique ID of the game to retrieve.</param>
		/// <returns>The <see cref="SimpleGame"/> represented by the given <paramref name="gameId"/>, if it exists.</returns>
		public static SimpleGame? GetGame(string gameId)
		{
			SIMPLE_GAMES_BY_ID.TryGetValue(gameId, out SimpleGame? game);
			return game;
		}

		/// <summary>
		/// Retrieves all ongoing games originating from the specified <paramref name="lobbyGroup"/>,
		/// and returns a list of their gameId's.
		/// </summary>
		/// <param name="lobbyGroup">The lobby which the games of interest originated from.</param>
		/// <returns>The gameId's of all ongoing games that originated from the given <paramref name="lobbyGroup"/>.</returns>
		public static string[] GetAllGameIdsFor(string lobbyGroup)
		{
			if (SIMPLE_GAME_IDS_BY_LOBBY_GROUP.TryGetValue(lobbyGroup, out HashSet<string>? gameIds))
			{
				return gameIds.ToArray();
			}

			return Array.Empty<string>();
		}

		/// <summary>
		/// Forfeits the specified <paramref name="player"/> from the game represented by the given <paramref name="gameId"/>
		/// after waiting for the specified <paramref name="timeoutSeconds"/> in seconds.
		/// <br></br><br></br>
		/// The actual time spent waiting may be slightly longer than the requested time.
		/// </summary>
		/// <typeparam name="TGame">The type of <see cref="SimpleGame"/> this timeout is for.</typeparam>
		/// <typeparam name="TClient">The client specification for <paramref name="tHub"/>.</typeparam>
		/// <param name="tHub">The type of the hub that manages player connections for <typeparamref name="TGame"/>.</param>
		/// <param name="gameId">The unique ID of the game this timeout is for.</param>
		/// <param name="player">The player who has lost connection, and is being allotted time to return.</param>
		/// <param name="timeoutSeconds">The number of seconds to wait before forfeiting the specified <paramref name="player"/>.</param>
		/// <exception cref="ArgumentException">If the given <paramref name="tHub"/> is not valid.</exception>
		public static void AddForfeitTimeout<TGame, TClient>(Type tHub, string gameId, Player player, int timeoutSeconds)
			where TGame : SimpleGame
			where TClient : class, IMultiplayerGameClient
		{
			if (!tHub.IsAssignableTo(typeof(MultiplayerGameHub<TGame, TClient>)))
			{
				throw new ArgumentException($"{nameof(tHub)} must be a {nameof(MultiplayerGameHub<TGame, TClient>)}.", nameof(tHub));
			}

			MethodInfo method = typeof(GameService).GetMethod(nameof(AddForfeitTimeout), BindingFlags.Static | BindingFlags.NonPublic, new Type[] { typeof(string), typeof(Player), typeof(int) })!;
			method.MakeGenericMethod(tHub, typeof(TGame), typeof(TClient)).Invoke(null, new object[] { gameId, player, timeoutSeconds });
		}

		/// <summary>
		/// Does the legwork for <see cref="AddForfeitTimeout{TGame, TClient}(Type, string, Player, int)"/>.
		/// </summary>
		/// <typeparam name="THub">The type of the hub that manages player connections for <typeparamref name="TGame"/>.</typeparam>
		/// <typeparam name="TGame">The type of <see cref="SimpleGame"/> this timeout is for.</typeparam>
		/// <typeparam name="TClient">The client specification for <typeparamref name="THub"/>.</typeparam>
		/// <param name="gameId">The unique ID of the game this timeout is for.</param>
		/// <param name="player">The player who has lost connection, and is being allotted time to return.</param>
		/// <param name="timeoutSeconds">The number of seconds to wait before forfeiting the specified <paramref name="player"/>.</param>
		private static async void AddForfeitTimeout<THub, TGame, TClient>(string gameId, Player player, int timeoutSeconds)
			where THub: MultiplayerGameHub<TGame, TClient>
			where TGame: SimpleGame
			where TClient: class, IMultiplayerGameClient
		{
			if (!PLAYER_FORFEIT_TIMEOUTS_BY_GAME_ID.TryGetValue(gameId, out HashSet<Player>? players))
			{
				players = new();
				PLAYER_FORFEIT_TIMEOUTS_BY_GAME_ID.Add(gameId, players);
			}

			players.Add(player);

			if (DIServices.GetHubContext(out IHubContext<THub, TClient>? context))
			{
				await Task.Delay(timeoutSeconds * 1000);
				bool forfeit = false;

				//Use the same lock as the cancel method.
				lock (PLAYER_FORFEIT_TIMEOUTS_BY_GAME_ID)
				{
					//We've potentially had several seconds for player to be removed from players; so we can check it before we forfeit them.
					forfeit = players.Contains(player);
				}

				if (forfeit)
				{
					//default to true because if the GameService isn't tracking it, then it ended.
					bool gameEnded = true;
					if (GetGame(gameId) is SimpleGame game)
					{
						gameEnded = game.PlayerLeft(player, isConnectionTimeout: true);
					}
					
					MultiplayerGameHub<TGame, TClient>.OnForfeitTimeout<THub, TGame, TClient>(context!, gameId, player, gameEnded);
				}
			}
		}

		/// <summary>
		/// Cancels the automatic forfeiture of the specified <paramref name="player"/> within the game
		/// represented by the given <paramref name="gameId"/>.
		/// </summary>
		/// <param name="gameId">The unique ID of the game to cancel the timeout for.</param>
		/// <param name="player">The player who is no longer slated to be forfeited automatically.</param>
		public static void CancelForfeitTimeout(string gameId, Player player)
		{
			lock (PLAYER_FORFEIT_TIMEOUTS_BY_GAME_ID)
			{
				if (PLAYER_FORFEIT_TIMEOUTS_BY_GAME_ID.TryGetValue(gameId, out HashSet<Player>? players))
				{
					players.Remove(player);
				}
			}
		}

		/// <summary>
		/// Adds the given <paramref name="game"/> from the specified <paramref name="lobbyGroup"/> to
		/// the related dictionaries for later access. This method also subscribes to the game's ending
		/// event to later remove it from the related dictionaries.
		/// </summary>
		/// <param name="game">The game to add to the internal tracking of the <see cref="GameService"/>.</param>
		/// <param name="lobbyGroup">The lobby group that this game originated from.</param>
		private static void AddGame(SimpleGame game, string lobbyGroup)
		{
			SIMPLE_GAMES_BY_ID.Add(game.GameId, game);

			if (!SIMPLE_GAME_IDS_BY_LOBBY_GROUP.TryGetValue(lobbyGroup, out HashSet<string>? games))
			{
				games = new();
				SIMPLE_GAME_IDS_BY_LOBBY_GROUP.Add(lobbyGroup, games);
			}
			games.Add(game.GameId);

			game.OnGameEnded += (object? sender, IEnumerable<SimpleGameOutcome> gameOutcomes) => {
				//sender is the SimpleGame that is ending.
				if (sender is not SimpleGame endedGame)
				{
					throw new ArgumentException($"Publishers of game ending events should be the concrete implementation {nameof(SimpleGame)} that handles the game.", nameof(sender));
				}

				SIMPLE_GAMES_BY_ID.Remove(endedGame.GameId);

				if (SIMPLE_GAME_IDS_BY_LOBBY_GROUP.TryGetValue(lobbyGroup, out HashSet<string>? games))
				{
					games.Remove(endedGame.GameId);
				}

				//Update game stats based on the outcome of the game.
				foreach (SimpleGameOutcome outcome in gameOutcomes)
				{
					UseDbContextFor(async dbContext => {
						await outcome.StoreGameStats(dbContext);
					});
				}

				//Send message to lobby clients that the game ended.
				//var context = (IHubContext<GameLobbyHub, IGameLobbyClient>) DIServices.GetRequiredService(typeof(IHubContext<GameLobbyHub, IGameLobbyClient>));
				if (DIServices.GetHubContext(out IHubContext<GameLobbyHub, IGameLobbyClient>? context))
				{
					GameLobbyHub.OnGameEnded(context!, lobbyGroup, endedGame.GameId);
				}
			};
		}

		/// <summary>
		/// Finds a lobby to represent the given <paramref name="gameRouteId"/>, or creates one if the default
		/// lobby contains too many players or doesn't exist.
		/// </summary>
		/// <param name="gameRouteId">The title of the game, as it appears in the url.</param>
		/// <param name="lowerThreshold">The number of players to allow the lobby to drop to before adding more into it.</param>
		/// <param name="upperThreshold">The max number of players to allow in a lobby before creating a new one.</param>
		/// <returns>The next available <see cref="GameLobby"/> with the given restrictions.</returns>
		private static GameLobby GetOrCreateGameLobby(string gameRouteId, int lowerThreshold = 75, int upperThreshold = 100)
		{
			GameInfo game = GetGameInfo(gameRouteId);

			if (!GAME_LOBBIES_BY_GAME.TryGetValue(game, out List<GameLobby>? lobbies))
			{
				lobbies = new();
				lobbies.Add(new(game, $"{gameRouteId}Lobby-1"));
				GAME_LOBBIES_BY_GAME.Add(game, lobbies);
			}

			//TODO: The above documentation does not match this implementation (the implementation should be changed).
			foreach (GameLobby lobby in lobbies)
			{
				if (lobby.Players.Count < lowerThreshold)
				{
					return lobby;
				}
			}

			//If the lobby is too full, create a new one.
			GameLobby newLobby = new(game, $"{gameRouteId}Lobby-{lobbies.Count}");

			//Eventually, these lobbies will empty out. Remove them when they are both empty and at the end of the list.
			newLobby.OnLobbyEmpty += CleanLobbiesDownToOne;

			lobbies.Add(newLobby);
			return lobbies[^1];
		}

		/// <summary>
		/// A simple method to reduce the number of lobbies for a given game down to the default one as players leave
		/// extra ones.
		/// <br></br><br></br>
		/// This method is an event handler subscriber.
		/// </summary>
		/// <param name="sender">The game lobby that published this event.</param>
		/// <param name="emptyLobby">The game lobby that no longer has any players in it.</param>
		private static void CleanLobbiesDownToOne(object? sender, GameLobby emptyLobby)
		{
			//This is a little weird, but eventually it will work; and it will clean them all down to one.
			lock (GAME_LOBBIES_BY_GAME)
			{
				if (GAME_LOBBIES_BY_GAME.TryGetValue(emptyLobby.Game, out List<GameLobby>? lobbies))
				{
					lock (lobbies)
					{
						//Don't remove index 0!
						for (int i = lobbies.Count - 1; i > 0; i--)
						{
							if (lobbies[i].Players.Count != 0) break;

							lobbies.RemoveAt(i);
						}
					}
				}
			}
		}

		/// <summary>
		/// Gets the <see cref="GameInfo"/> associated with the given <paramref name="gameRouteId"/>, or throws an exception
		/// if no such game exists.
		/// </summary>
		/// <param name="gameRouteId">The name of the game as it appears in the website url.</param>
		/// <returns>The <see cref="GameInfo"/> associated with the given <paramref name="gameRouteId"/>.</returns>
		/// <exception cref="ArgumentException">If an invalid <paramref name="gameRouteId"/> is given.</exception>
		private static GameInfo GetGameInfo(string gameRouteId)
		{
			if (CanonicalGames.GetGameInfoByRouteId(gameRouteId) is not GameInfo game)
			{
				throw new ArgumentException("The given RouteId does not match any known Games.", nameof(gameRouteId));
			}

			return game;
		}

		/// <summary>
		/// A helper method to retrieve <paramref name="context"/> in a more readable fashion.
		/// </summary>
		/// <returns>True if <paramref name="context"/> was populated; false otherwise.</returns>
		private static bool GetHubContext<THub, TClient>(this IServiceProvider services, out IHubContext<THub, TClient>? context)
			where THub: Hub<TClient>
			where TClient: class
		{
			context = services.GetRequiredService(typeof(IHubContext<THub, TClient>)) as IHubContext<THub, TClient>;
			return context is not null;
		}

		/// <summary>
		/// A helper method to retrieve a database context from <see cref="DIServices"/>, and run code using it.
		/// <br></br><br></br>
		/// The context is not managed by the DI container; instead, it is disposed after being used by this method.
		/// </summary>
		/// <param name="action">The code to run using the database context.</param>
		private static async void UseDbContextFor(Func<ApplicationDbContext, Task> action)
		{
			if (DIServices.GetRequiredService(typeof(IDbContextFactory<ApplicationDbContext>)) is not IDbContextFactory<ApplicationDbContext> contextFactory)
			{
				throw new InvalidOperationException("Unable to create a database context -- the DI service is unavailable.");
			}

			await using ApplicationDbContext dbContext = await contextFactory.CreateDbContextAsync();
			await action(dbContext);
		}
	}
}
