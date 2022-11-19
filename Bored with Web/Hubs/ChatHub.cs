using Microsoft.AspNetCore.SignalR;

namespace Bored_with_Web.Hubs
{
	/// <summary>
	/// Defines methods that are available on the client side of <see cref="ChatHub"/>.
	/// </summary>
	public interface IChatClient
    {
		/// <summary>
		/// Retrieves a message from the server. The message may have been sent by this client.
		/// </summary>
		/// <param name="username">The username of the sender of the message.</param>
		/// <param name="message">The message being sent.</param>
		/// <param name="isActiveUser">True if the sender of this message is this client.</param>
		Task ReceiveMessage(string username, string message, bool isActiveUser);
    }

	/// <summary>
	/// Handles basic real-time chat features.
	/// </summary>
	public class ChatHub : UsernameAwareHub<IChatClient>
	{
        public async override Task OnConnectedAsync()
        {
			//TODO: Add the client to a group to distinguish which chat they joined.
			if (GetCallerUsername(out string username))
            {
				await Clients.Others.ReceiveMessage(string.Empty, $"{username} has connected.", false);
			}

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Sends a message to all connected clients in the same group as the caller.
        /// <br></br><br></br>
        /// The caller also receives their own message via
        /// <see cref="IChatClient.ReceiveMessage(string, string, bool)"/>.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public async Task SendMessage(string message)
		{
			if (GetCallerUsername(out string username))
            {
				await Clients.Others.ReceiveMessage(username, message, false);
				await Clients.Caller.ReceiveMessage(username, message, true);
			}
		}

        public async override Task OnDisconnectedAsync(Exception? exception)
        {
			//TODO: Clean up groups if necessary (see OnConnectedAsync).
			if (GetCallerUsername(out string username))
            {
				await Clients.Others.ReceiveMessage(string.Empty, $"{username} has been disconnected.", false);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
