﻿using Microsoft.AspNetCore.SignalR;

namespace Bored_with_Web.Hubs
{
	/// <summary>
	/// A simple Hub subclass that is aware of the username within the user's session data.
	/// </summary>
	/// <typeparam name="IClient">The interface representing methods available on the client side.</typeparam>
	public abstract class UsernameAwareHub<IClient> : Hub<IClient>
		where IClient: class
	{
		/// <summary>
		/// Pulls the caller's username from the HttpContext.Session object, and places it into <paramref name="username"/>.
		/// The method returns true if the username has been populated; false otherwise.
		/// </summary>
		/// <param name="username">The username associated with the caller's session.</param>
		/// <returns>True if <paramref name="username"/> was populated with a username; false otherwise.</returns>
		protected bool GetCallerUsername(out string username)
		{
			username = Context.GetHttpContext()?.Session.GetUsername()!;
			return username is not null;
		}
	}
}
