using Bored_with_Web.Models;

namespace Bored_with_Web.Games
{
	public static class GameService
	{
		private static readonly Dictionary<GameInfo, List<GameLobby>> GAME_LOBBIES_BY_GAME = new();

		private static readonly Dictionary<string, SimpleGame> SIMPLE_GAMES_BY_ID = new();

		//TODO: Consider refactoring to use GameId (string) instead of GameInfo; it would allow users to play multiple games at once.
		//It would require that the GamesController is refactored to have a GameId in the url, though.
		private static readonly Dictionary<Player, Dictionary<GameInfo, SimpleGame>> SIMPLE_GAMES_BY_PLAYER = new();

		public static bool IsPlayerInLobby(Player player, out GameLobby? lobby)
		{
			foreach (GameInfo game in CanonicalGames.AllGames)
			{
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
			}
			
			lobby = null;
			return false;
		}

		public static GameLobby AddPlayerToLobby(Player player, string gameRouteId)
		{
			if (IsPlayerInLobby(player, out _))
			{
				throw new InvalidOperationException("The specified player is already in a lobby.");
			}

			GameLobby lobby = GetOrCreateGameLobby(gameRouteId);
			lobby.AddPlayer(player);
			return lobby;
		}

		public static string RemovePlayerFromLobby(Player player)
		{
			if (IsPlayerInLobby(player, out GameLobby? lobby))
			{
				lobby!.RemovePlayer(player);
				return lobby.LobbyGroup;
			}

			throw new InvalidOperationException("The specified player is not in a lobby.");
		}

		public static bool IsPlayerInGame(Player player, string gameRouteId)
		{
			if (CanonicalGames.GetGameInfoByRouteId(gameRouteId) is not GameInfo game)
			{
				throw new ArgumentException("The given RouteId does not match any known Games.", nameof(gameRouteId));
			}

			if (SIMPLE_GAMES_BY_PLAYER.TryGetValue(player, out Dictionary<GameInfo, SimpleGame>? games))
			{
				return games.ContainsKey(game);
			}

			return false;
		}

		public static void AddGame(SimpleGame game)
		//Assume that the player isn't in any other game of the same type.
		{
			SIMPLE_GAMES_BY_ID.Add(game.GameId, game);

			//Get players from the game, and populate the SIMPLE_GAMES_BY_PLAYER dictionary.
			foreach (Player player in game.Players)
			{
				if (!SIMPLE_GAMES_BY_PLAYER.TryGetValue(player, out Dictionary<GameInfo, SimpleGame>? games))
				{
					games = new();
					games.Add(game.Info, game);
					SIMPLE_GAMES_BY_PLAYER.Add(player, games);
				}
				else
				{
					games.Add(game.Info, game);
				}
			}

			game.OnGameEnded += RemoveGameWhenEnded;
		}

		public static SimpleGame? GetGame(string gameId)
		{
			SIMPLE_GAMES_BY_ID.TryGetValue(gameId, out SimpleGame? game);
			return game;
		}

		public static string[] GetAllGameIdsFor(string gameRouteId)
		{
			if (CanonicalGames.GetGameInfoByRouteId(gameRouteId) is not GameInfo info)
			{
				throw new ArgumentException("The given RouteId does not match any known Games.", nameof(gameRouteId));
			}

			//TODO: Refactor to do this faster?
			return (from SimpleGame game in SIMPLE_GAMES_BY_ID
					where game.Info == info
					select game.GameId).ToArray();
		}

		private static GameLobby GetOrCreateGameLobby(string gameRouteId, int lowerThreshold = 75, int upperThreshold = 100)
		{
			if (CanonicalGames.GetGameInfoByRouteId(gameRouteId) is not GameInfo game)
			{
				throw new ArgumentException("The given RouteId does not match any known Games.", nameof(gameRouteId));
			}

			if (!GAME_LOBBIES_BY_GAME.TryGetValue(game, out List<GameLobby>? lobbies))
			{
				lobbies = new();
				lobbies.Add(new(game, $"{gameRouteId}Lobby-1"));
				GAME_LOBBIES_BY_GAME.Add(game, lobbies);
			}

			//TODO: If the lobby is too full, create a new one.
			foreach (GameLobby lobby in lobbies)
			{
				if (lobby.Players.Count < lowerThreshold)
				{
					return lobby;
				}
			}

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

		private static void RemoveGameWhenEnded(object? sender, SimpleGame endedGame)
		{
			SIMPLE_GAMES_BY_ID.Remove(endedGame.GameId);

			//TODO: Remove the entries for SIMPLE_GAMES_BY_PLAYER, as well.
			lock (SIMPLE_GAMES_BY_PLAYER)
			{
				foreach (Player player in endedGame.Players)
				{
					if (SIMPLE_GAMES_BY_PLAYER.TryGetValue(player, out Dictionary<GameInfo, SimpleGame>? games))
					{
						games.Remove(endedGame.Info);
					}
				}
			}
		}
	}
}
