using Bored_with_Web.Models;

namespace Bored_with_Web.Games
{
	public class GameLobbyResult
	{
		public bool MatchFound { get; }

		public Player[]? Players { get; }

		public GameLobbyResult(bool matchFound, Player[]? players = null)
		{
			MatchFound = matchFound;
			Players = players;
		}
	}

	public class GameLobby
	{
		public GameInfo Game { get; }

		public string LobbyGroup { get; }

		public HashSet<Player> Players { get; } = new();

		public HashSet<Player> ReadyPlayers { get; } = new();

		public event EventHandler<GameLobby>? OnLobbyEmpty;

		public GameLobby(GameInfo game, string lobbyGroup)
		{
			Game = game;
			LobbyGroup = lobbyGroup;
		}

		public void AddPlayer(Player player)
		{
			//TODO: Consider doing something if the player is already in the set...
			Players.Add(player);
		}

		public void RemovePlayer(Player player)
		{
			lock (ReadyPlayers)
			{
				Players.Remove(player);
				ReadyPlayers.Remove(player);

				if (Players.Count == 0 && OnLobbyEmpty is EventHandler<GameLobby> handler)
				{
					handler(this, this);
				}
			}
		}

		public GameLobbyResult PlayerIsReady(Player player, bool isReady)
		{
			lock (ReadyPlayers)
			{
				if (Players.TryGetValue(player, out Player? internalPlayer))
				{
					internalPlayer.Ready = isReady;
					if (isReady)
					{
						ReadyPlayers.Add(player);
					}
					else
					{
						ReadyPlayers.Remove(player);
					}
				}

				if (ReadyPlayers.Count >= Game.RequiredPlayerCount)
				{
					Player[] players = ReadyPlayers.Take(Game.RequiredPlayerCount).ToArray();

					foreach (Player p in players)
					{
						//These players are going to be redirected out of the lobby.
						RemovePlayer(p);
					}

					return new(true, players);
				}

				return new(false);
			}
		}
	}
}
