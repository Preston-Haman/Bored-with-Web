using Microsoft.AspNetCore.SignalR;

namespace Bored_with_Web.Hubs
{
	/// <summary>
	/// Defines methods that are available on the client side of a <see cref="MultiplayerGameHub{MultiplayerClient}"/>.
	/// </summary>
	public interface IMultiplayerGameClient
	{
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
		/// Called when the user first connects to a game, and there are other players already connected.
		/// Implementations may use the provided information to update their representation of the game's
		/// players and spectators.
		/// </summary>
		/// <param name="players">The names of the players in the game, in the order of their player number (offset by 1; index 0 is player 1, and so on).</param>
		/// <param name="spectators">The names of the users spectating the game.</param>
		Task UpdateVisiblePlayers(string[] players, string[] spectators);

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
		/// <param name="isUs">Indicates that the active player is the user receiving the method call.</param>
		Task SetPlayerTurn(int playerNumber, bool isUs);

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
	/// <typeparam name="IMultiplayerClient">An interface, implementing <see cref="IMultiplayerGameClient"/>, that defines methods available on the client
	/// for the game represented by the concrete subclass' implementation.</typeparam>
	public abstract class MultiplayerGameHub<IMultiplayerClient> : Hub<IMultiplayerClient>
		where IMultiplayerClient: class, IMultiplayerGameClient
	{
		public async override Task OnConnectedAsync()
		{
			/*TODO
			 * Place the user into a group for their game; maybe pull it from their session?
			 * Notify other users in the group that the user connected.
			 * Tell the new user who else is already there.
			 * Start the game, if appropriate.
			 * 
			 * If the user is rejoining after a lost connection, subclass will send them information about the game;
			 *	but we need to remove their forfeit timeout.
			 */
			await base.OnConnectedAsync();
		}

		/// <summary>
		/// Called when a user rejoins an ongoing game after losing their connection.
		/// </summary>
		protected abstract Task OnRejoinedGame();

		public async override Task OnDisconnectedAsync(Exception? exception)
		{
			/*TODO
			 * Remove the user from their group.
			 * Notify the other users in the group that the user disconnected.
			 * If there is no one left in the group, shut it down; notify game service to end the game.
			 * 
			 * If the game is ongoing, set a timeout for the missing player before forfeiting them; -- HOW???
			 *	If the game can no longer continue, prevent reconnection after the timeout ends, otherwise allow their return as a spectator.
			 */
			await base.OnDisconnectedAsync(exception);
		}
	}
}
