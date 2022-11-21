using System.Reflection;

namespace Bored_with_Web.Games
{
	public static class GameService
	{
		private static readonly object ID_LOCK = new();

		private static uint nextGameId = 0;

		private static readonly Dictionary<GameInfo, List<GameLobby>> GAME_LOBBIES_BY_GAME = new();

		private static readonly Dictionary<string, HashSet<string>> SIMPLE_GAME_IDS_BY_LOBBY_GROUP = new();

		private static readonly Dictionary<string, SimpleGame> SIMPLE_GAMES_BY_ID = new();

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

		public static SimpleGame CreateNextGame(string lobbyGroup, string gameRouteId, Player[] players)
		{
			GameInfo game = GetGameInfo(gameRouteId);

			MethodInfo method = typeof(GameService).GetMethod(nameof(CreateNextGame), BindingFlags.Static, new Type[] {typeof(string), typeof(Player[]), typeof(string)})!;
			return (SimpleGame) method.MakeGenericMethod(game.ImplementingType).Invoke(null, new object[] { gameRouteId, players, lobbyGroup })!;
		}
		
		private static GameType CreateNextGame<GameType>(string gameRouteId, Player[] players, string lobbyGroup) //Parameter order is to avoid ambiguity with Reflection
			where GameType: SimpleGame, new()
		{
			GameInfo info = GetGameInfo(gameRouteId);

			string gameId;
			lock (ID_LOCK)
			{
				gameId = $"{gameRouteId}#{nextGameId++}";
			}

			GameType game = new();
			game.CreateGame(info, gameId, players);
			AddGame(game, lobbyGroup);
			return game;
		}

		public static SimpleGame? GetGame(string gameId)
		{
			SIMPLE_GAMES_BY_ID.TryGetValue(gameId, out SimpleGame? game);
			return game;
		}

		public static string[] GetAllGameIdsFor(string lobbyGroup)
		{
			if (SIMPLE_GAME_IDS_BY_LOBBY_GROUP.TryGetValue(lobbyGroup, out HashSet<string>? gameIds))
			{
				return gameIds.ToArray();
			}

			return Array.Empty<string>();
		}

		private static void AddGame(SimpleGame game, string lobbyGroup)
		{
			SIMPLE_GAMES_BY_ID.Add(game.GameId, game);

			if (!SIMPLE_GAME_IDS_BY_LOBBY_GROUP.TryGetValue(lobbyGroup, out HashSet<string>? games))
			{
				games = new();
				games.Add(game.GameId);
				SIMPLE_GAME_IDS_BY_LOBBY_GROUP.Add(lobbyGroup, games);
			}
			else
			{
				games.Add(game.GameId);
			}

			game.OnGameEnded += (object? sender, SimpleGame endedGame) => {
				SIMPLE_GAMES_BY_ID.Remove(endedGame.GameId);

				if (SIMPLE_GAME_IDS_BY_LOBBY_GROUP.TryGetValue(lobbyGroup, out HashSet<string>? games))
				{
					games.Remove(endedGame.GameId);
				}
			};
		}

		private static GameLobby GetOrCreateGameLobby(string gameRouteId, int lowerThreshold = 75, int upperThreshold = 100)
		{
			GameInfo game = GetGameInfo(gameRouteId);

			if (!GAME_LOBBIES_BY_GAME.TryGetValue(game, out List<GameLobby>? lobbies))
			{
				lobbies = new();
				lobbies.Add(new(game, $"{gameRouteId}Lobby-1"));
				GAME_LOBBIES_BY_GAME.Add(game, lobbies);
			}

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
	}
}
