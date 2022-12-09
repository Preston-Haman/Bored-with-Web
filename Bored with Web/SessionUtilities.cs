using Bored_with_Web.Data;

namespace Bored_with_Web
{
	/// <summary>
	/// An extension class that helps with session storage.
	/// </summary>
	internal static class SessionUtilities
	{
		/// <summary>
		/// The key to use in HttpContext.Session to access the user's username.
		/// </summary>
		private static readonly string USERNAME = "Username";

		/// <summary>
		/// Gets the username associated with this session.
		/// </summary>
		/// <param name="session">This session.</param>
		/// <returns>The username associated with this session.</returns>
		public static string? GetUsername(this ISession session)
        {
			return session.GetString(USERNAME);
        }

		/// <summary>
		/// Sets the username associated with this session.
		/// </summary>
		/// <param name="session">This session.</param>
		/// <param name="username">The username to associate with this session.</param>
		public static void SetUsername(this ISession session, string username)
        {
			session.SetString(USERNAME, username);
        }

		/// <summary>
		/// Guest users have their stats, if any, transferred to their registered accounts.
		/// The session username for the user is also set to match their registered username.
		/// </summary>
		/// <param name="session">This session.</param>
		/// <param name="registeredUsername">The username associated with the account of the user.</param>
		/// <param name="dbContext">The database context for the site.</param>
		public static async Task LoginAs(this ISession session, string registeredUsername, ApplicationDbContext dbContext)
		{
			await GuestCache.OnGuestLogin(session.GetUsername()!, registeredUsername, dbContext);
			session.SetUsername(registeredUsername);
		}
	}

	/// <summary>
	/// A simple class that creates a guest username for users who are not signed in.
	/// <br></br><br></br>
	/// This is achieved by randomly selecting an adjective, and noun, from a short list
	/// and pairing it with an incrementing integer value.
	/// </summary>
	internal static class GuestNameGenerator
	{
		/// <summary>
		/// RNG for selecting adjectives and nouns.
		/// </summary>
		private static readonly Random RND = new();
		
		/// <summary>
		/// A numeric value that will be concatenated to a random adjective-noun pair to create
		/// a guest username.
		/// </summary>
		private static uint guestCounter = 1;

		/// <summary>
		/// A small list of adjectives that will be used to generate guest usernames.
		/// </summary>
		private static readonly string[] ADJECTIVES = { "Angry", "Giant", "Salty", "Silly", "Zealous" };

		/// <summary>
		/// A small list of nouns that will be used to generate guest usernames.
		/// </summary>
		private static readonly string[] NOUNS = { "Anon", "Guest", "Noob", "Player", "Steve" };

		/// <summary>
		/// Creates a new random name that may be used by a guest. The generated name uses a numeric value
		/// to ensure uniqueness. It is possible, albeit unlikely, that a guest can exist for a long enough
		/// time that their name is assigned again. This should not be a concern, however.
		/// </summary>
		/// <returns>The newly generated name.</returns>
		public static string GenerateGuestName()
		{
			lock (RND)
			{
				return $"{ADJECTIVES[RND.Next(ADJECTIVES.Length)]}{NOUNS[RND.Next(NOUNS.Length)]}#{guestCounter++}";
            }
		}
	}
}
