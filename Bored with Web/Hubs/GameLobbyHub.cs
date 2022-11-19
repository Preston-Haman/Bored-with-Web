using Bored_with_Web.Games;
using Microsoft.AspNetCore.SignalR;

namespace Bored_with_Web.Hubs
{
	/// <summary>
	/// Defines methods that are available on the client side of a <see cref="GameLobbyHub"/>.
	/// </summary>
	public interface IGameLobbyClient
	{
		Task OnJoinedLobby(string lobbyGroup);

		Task GameCreated(string[] playerNames, int gameId);

		Task GameEnded(int gameId);

		Task PlayerConnected(string playerName);

		Task PlayerIsReady(string playerName, bool isReady);

		Task PlayerDisconnected(string playerName);

		Task UpdateGames(string[] gameId);

		Task UpdatePlayers(string[] playerNames);
	}

	public class GameLobbyHub : UsernameAwareHub<IGameLobbyClient>
	{
		public async override Task OnConnectedAsync()
		{
			/*
			 * Add user to group (based on username); SignalR has a way to associate users to connections, but it's overkill and requires authentication (I think).
			 * I would be worried about the number of groups; but the implementation for group management is so far abstracted from me that I can't even tell
			 * if it will pose a problem or not. The documentation is either hidden, or useless.
			 */
			if (GetCallerUsername(out string username))
			{
				await Groups.AddToGroupAsync(Context.ConnectionId, username);
			}
			
			await base.OnConnectedAsync();
		}

		public async Task OnJoinLobby(string gameRouteId)
		{
			if (GetCallerUsername(out string username))
			{
				GameLobby lobby = GameService.AddPlayerToLobby(new Player(username), gameRouteId);
				await Clients.Caller.OnJoinedLobby(lobby.LobbyGroup);
				await Clients.Caller.UpdateGames(GameService.GetAllGameIdsFor(gameRouteId));
				await Clients.Caller.UpdatePlayers((from Player p in lobby.Players select p.Username).ToArray());
				await Clients.OthersInGroup(lobby.LobbyGroup).PlayerConnected(username);
			}
		}

		public async Task OnReady(string lobbyGroup)
		{
			if (GetCallerUsername(out string username))
			{
				await Clients.Groups(lobbyGroup).PlayerIsReady(username, true);
			}
		}

		public async Task OnNotReady(string lobbyGroup)
		{
			if (GetCallerUsername(out string username))
			{
				await Clients.Groups(lobbyGroup).PlayerIsReady(username, false);
			}
		}

		public async override Task OnDisconnectedAsync(Exception? exception)
		{
			if (GetCallerUsername(out string username))
			{
				await Groups.RemoveFromGroupAsync(Context.ConnectionId, username);
			}

			//unless we make multiple lobby Hubs and urls, or find some other solution,
			//we'll be forced to restrict the user to one lobby at a time because of this.
			Player player = new(username);
			if (GameService.IsPlayerInLobby(player, out GameLobby? _))
			{
				GameService.RemovePlayerFromLobby(player);
			}
			
			await base.OnDisconnectedAsync(exception);
		}
	}
}
