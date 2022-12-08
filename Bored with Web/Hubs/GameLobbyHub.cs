using Bored_with_Web.Games;
using Microsoft.AspNetCore.SignalR;

namespace Bored_with_Web.Hubs
{
	/// <summary>
	/// Defines methods that are available on the client side of a <see cref="GameLobbyHub"/>.
	/// </summary>
	public interface IGameLobbyClient
	{
		/// <summary>
		/// Called when the lobby has found a match for players. Implementors may use this information to
		/// display the new game to the user if they are not in the listed players; and should redirect
		/// the user to the Games/Play/<paramref name="gameRouteId"/> url if they are in the listed players.
		/// </summary>
		/// <param name="playerNames">The names of the users in the game.</param>
		/// <param name="gameId">A string that represents the ongoing game.</param>
		/// <param name="gameRouteId">The name of the game as it appears in the website url.</param>
		Task GameCreated(string[] playerNames, string gameId, string gameRouteId);

		/// <summary>
		/// Called when a game displayed in the lobby ends. Implementors may use this information to
		/// stop displaying the specified game in the lobby.
		/// </summary>
		/// <param name="gameId">A string that represents the game that just ended.</param>
		Task GameEnded(string gameId);

		/// <summary>
		/// Called when a player connects to the lobby. Implementors may use this information to display
		/// the new player in the lobby's list.
		/// </summary>
		/// <param name="playerName">The name of the player that joined the lobby.</param>
		Task PlayerConnected(string playerName);

		/// <summary>
		/// Called when a player has marked themselves as ready. This is also called for the player
		/// that marked themselves ready. Implementors may use this to update the lobby's list to
		/// indicate visually that the specified player is now ready to play (or not).
		/// </summary>
		/// <param name="playerName">The name of the player whose ready state has changed.</param>
		/// <param name="isReady">If the player is ready (true), or no longer ready.</param>
		Task PlayerIsReady(string playerName, bool isReady);

		/// <summary>
		/// Called when a player disconnects from the lobby. Implementors may use this information to update
		/// the lobby's list of players.
		/// </summary>
		/// <param name="playerName">The name of the player that lost connection.</param>
		Task PlayerDisconnected(string playerName);

		/// <summary>
		/// Called when the player initially connects. Implementors may use this information to update the
		/// display of ongoing games.
		/// </summary>
		/// <param name="gameIds">An array of strings that represent the ongoing games for this lobby.</param>
		Task UpdateGames(string[] gameIds);

		/// <summary>
		/// Called when the player initially connects. Implementors may use this information to update the
		/// lobby's list of players. The provided names will include the name of the player that just connected.
		/// </summary>
		/// <param name="playerNames">The names of all the players in the lobby.</param>
		/// <param name="readyPlayers">The ready status of the specified players.</param>
		Task UpdatePlayers(string[] playerNames, bool[] readyPlayers);

		/// <summary>
		/// Called when the server wants the client to disconnect. Implementors should close the connection,
		/// and remove any information about the lobby being displayed to the user. The user should also
		/// be informed that their connection was terminated; and given advice for further action.
		/// <br></br><br></br>
		/// The client is allowed to reconnect; the main reason this method is called is to allow the client
		/// a chance to sync with the server if it's been determined to be in an invalid state.
		/// </summary>
		Task ServerRequestsDisconnect();
	}

	/// <summary>
	/// A basic implementation for managing the connection of players waiting in the lobby.
	/// </summary>
	public class GameLobbyHub : UsernameAwareHub<IGameLobbyClient>
	{
		/// <summary>
		/// The name of the game this lobby is for, as it appears in the website url. This is provided in the "game" query string by the client.
		/// </summary>
		private string GameRouteId { get { return Context.GetHttpContext()!.Request.Query["game"]; } }

		public async override Task OnConnectedAsync()
		{
			if (GetCallerUsername(out string username))
			{
				if (!GameService.IsPlayerInLobby(new Player(username), GameRouteId, out GameLobby? oldLobby))
				{
					GameLobby lobby = GameService.AddPlayerToLobby(new Player(username), GameRouteId);
					await Groups.AddToGroupAsync(Context.ConnectionId, lobby.LobbyGroup);
					await Clients.Caller.UpdateGames(GameService.GetAllGameIdsFor(lobby.LobbyGroup));

					Player[] players = lobby.Players.ToArray();
					await Clients.Caller.UpdatePlayers(Array.ConvertAll(players, player => player.Username), Array.ConvertAll(players, player => player.Ready));
					await Clients.OthersInGroup(lobby.LobbyGroup).PlayerConnected(username);
				}
				else
				{
					//Joining the lobby again; perhaps on another device.
					await Groups.AddToGroupAsync(Context.ConnectionId, oldLobby!.LobbyGroup);
					await Clients.Caller.UpdateGames(GameService.GetAllGameIdsFor(oldLobby.LobbyGroup));

					Player[] players = oldLobby.Players.ToArray();
					await Clients.Caller.UpdatePlayers(Array.ConvertAll(players, player => player.Username), Array.ConvertAll(players, player => player.Ready));
				}
			}
			
			await base.OnConnectedAsync();
		}

		/// <summary>
		/// Called when the client is ready to join a game.
		/// </summary>
		public async Task OnReady()
		{
			Player player;
			if (GetCallerUsername(out string username))
			{
				if (GameService.IsPlayerInLobby(player = new Player(username), GameRouteId, out GameLobby? lobby))
				{
					GameLobbyResult match = lobby!.PlayerIsReady(player, true);
					if (match.MatchFound)
					{
						SimpleGame game = GameService.CreateNextGame(lobby.LobbyGroup, lobby.Game.RouteId, match.Players!);

						string[] matchedPlayers = (from Player p in match.Players! select p.Username).ToArray();

						await Clients.Group(lobby.LobbyGroup).GameCreated(matchedPlayers, game.GameId, lobby.Game.RouteId);
					}

					//This might pose an interesting race condition on the client side as the matched players disconnect from the lobby.
					await Clients.Group(lobby.LobbyGroup).PlayerIsReady(username, true);
				}
				else
				{
					//Malicious user? Normal user who wound up being removed from the internal lobby?
					await Clients.Caller.ServerRequestsDisconnect();
				}
			}
		}

		/// <summary>
		/// Called when the client is not ready to join a game. The client may call this automatically after being in the
		/// lobby for an extended period of time without a match.
		/// </summary>
		public async Task OnNotReady()
		{
			Player player;
			if (GetCallerUsername(out string username))
			{
				if (GameService.IsPlayerInLobby(player = new Player(username), GameRouteId, out GameLobby? lobby))
				{
					_ = lobby!.PlayerIsReady(player, false);
					await Clients.Group(lobby.LobbyGroup).PlayerIsReady(username, false);
				}
				else
				{
					//Malicious user? Normal user who wound up being removed from the internal lobby?
					await Clients.Caller.ServerRequestsDisconnect();
				}
			}
		}

		/// <summary>
		/// Called over time by the client to determine the state of ongoing games.
		/// </summary>
		public async Task RefreshGameList(string[] gameIds)
		{
			/*TODO
			 * It would be better to call GameEnded when the game actually ends -- there's even an event for that in SimpleGame!
			 * The reason I am not doing so is because I don't know how to access DI to get the necessary context
			 * to properly create a Hub and fire off the messages to everyone.
			 */
			if (GetCallerUsername(out string username) && GameService.IsPlayerInLobby(new Player(username), GameRouteId, out GameLobby? lobby))
			{
				foreach (string gameId in gameIds)
				{
					if (GameService.GetGame(gameId) is null)
					{
						await Clients.Group(lobby!.LobbyGroup).GameEnded(gameId);
					}
				}
			}
		}

		public async override Task OnDisconnectedAsync(Exception? exception)
		{
			if (GetCallerUsername(out string username))
			{
				Player player = new(username);
				if (GameService.IsPlayerInLobby(player, GameRouteId, out GameLobby? lobby))
				{
					/*
					 * This removes the player from the lobby internally; if the player joined the same lobby more than
					 * once, their extra connections will never be matched with anyone.
					 */
					lobby!.RemovePlayer(player);
					await Groups.RemoveFromGroupAsync(Context.ConnectionId, lobby.LobbyGroup);
					await Clients.Group(lobby.LobbyGroup).PlayerDisconnected(username);
				}
			}

			await base.OnDisconnectedAsync(exception);
		}

		/// <summary>
		/// Notifies clients waiting in the specified <paramref name="lobbyGroup"/> that the game represented by
		/// the given <paramref name="gameId"/> has ended.
		/// <br></br><br></br>
		/// This method is presented as a way for calling clients from outside the hub without relying on external
		/// classes to understand how the clients should be notified.
		/// </summary>
		/// <param name="context">The context providing access to the SignalR clients for the lobby group; this should be created through DI.</param>
		/// <param name="lobbyGroup">The lobby to notify of this game's end.</param>
		/// <param name="gameId">The unique, human readable, identifier for the game that ended.</param>
		public static async void OnGameEnded(IHubContext<GameLobbyHub, IGameLobbyClient> context, string lobbyGroup, string gameId)
		{
			await context.Clients.Group(lobbyGroup).GameEnded(gameId);
		}
	}
}
