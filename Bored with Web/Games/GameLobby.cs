using Bored_with_Web.Models;

namespace Bored_with_Web.Games
{
	/// <summary>
	/// Returned by <see cref="GameLobby.PlayerIsReady(Player, bool)"/>. If <see cref="MatchFound"/> is true,
	/// <see cref="Players"/> will be populated.
	/// <br></br><br></br>
	/// This class exists for the sole purpose of skirting around SignalR code outside of the lobby hub.
	/// It may be removed in the future.
	/// </summary>
	//TODO: Refactor and remove this class. Players can be an out parameter instead.
	public class GameLobbyResult
	{
		/// <summary>
		/// Whether or not a match was found for the player who became ready.
		/// </summary>
		public bool MatchFound { get; }

		/// <summary>
		/// The list of players matched for a game.
		/// <br></br><br></br>
		/// This will be null if <see cref="MatchFound"/> is false.
		/// </summary>
		public Player[]? Players { get; }

		/// <summary>
		/// Populates <see cref="MatchFound"/> with the value of <paramref name="matchFound"/>,
		/// and <see cref="Players"/> with the value of <paramref name="players"/>.
		/// </summary>
		/// <param name="matchFound"></param>
		/// <param name="players"></param>
		public GameLobbyResult(bool matchFound, Player[]? players = null)
		{
			MatchFound = matchFound;
			Players = players;
		}
	}

	/// <summary>
	/// Manages the state of a lobby for the specified <see cref="Game"/>.
	/// <br></br><br></br>
	/// The game lobby is a waiting room for players who are interested in playing a game.
	/// As players join, they are tracked internally. Players who mark themselves as ready
	/// are matched with other ready players so they may be placed in a <see cref="SimpleGame"/>.
	/// </summary>
	public class GameLobby
	{
		/// <summary>
		/// The game this lobby is for.
		/// </summary>
		public GameInfo Game { get; }

		/// <summary>
		/// A unique identifier for this instance of the lobby.
		/// <br></br><br></br>
		/// This is intended as a human readable identifier for SignalR groups consisting of
		/// connections for the players in this lobby.
		/// </summary>
		public string LobbyGroup { get; }

		/// <summary>
		/// A set of players in this lobby.
		/// </summary>
		public HashSet<Player> Players { get; } = new();

		/// <summary>
		/// A set of players in this lobby who have been marked as ready.
		/// </summary>
		/// <seealso cref="Player.Ready"/>
		public HashSet<Player> ReadyPlayers { get; } = new();

		/// <summary>
		/// Publisher of the event of this lobby being empty.
		/// <br></br><br></br>
		/// Subscribers may register directly, and will be notified
		/// when this lobby no longer has any players waiting within it.
		/// </summary>
		public event EventHandler<GameLobby>? OnLobbyEmpty;

		/// <summary>
		/// Creates a new game lobby for the specified <paramref name="game"/>, represented by the given
		/// <paramref name="lobbyGroup"/>.
		/// </summary>
		/// <param name="game">The game this lobby represents.</param>
		/// <param name="lobbyGroup">A unique, human readable, identifier of this lobby (see <see cref="LobbyGroup"/>).</param>
		public GameLobby(GameInfo game, string lobbyGroup)
		{
			Game = game;
			LobbyGroup = lobbyGroup;
		}

		/// <summary>
		/// Adds a player to this lobby. The player is assumed to be not ready.
		/// </summary>
		/// <param name="player">The player to add.</param>
		/// <seealso cref="Players"/>
		public void AddPlayer(Player player)
		{
			//TODO: Consider doing something if the player is already in the set...
			Players.Add(player);
		}

		/// <summary>
		/// Removes a player from this lobby.
		/// </summary>
		/// <param name="player">The player to remove.</param>
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

		/// <summary>
		/// Marks the given <paramref name="player"/> as being ready or not ready based on the value of
		/// <paramref name="isReady"/>. A <see cref="GameLobbyResult"/> is returned to indicate if enough
		/// players are ready for a match or not.
		/// </summary>
		/// <param name="player">The <see cref="Player"/> who has become ready, or not ready.</param>
		/// <param name="isReady">Whether or not the <paramref name="player"/> is ready.</param>
		/// <returns>A <see cref="GameLobbyResult"/> representing the result of this action.</returns>
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
