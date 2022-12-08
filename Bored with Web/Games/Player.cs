namespace Bored_with_Web.Games
{
	/// <summary>
	/// A simple representation of a player for a game.
	/// <br></br><br></br>
	/// Players are identified by their <see cref="Username"/>.
	/// <br></br><br></br>
	/// The <see cref="Equals(object?)"/> method has been overridden to only compare <see cref="Player"/>
	/// instances by <see cref="Username"/>. The <see cref="GetHashCode"/> method has been similarly overridden
	/// to return the hash value of the <see cref="Username"/> (see <see cref="string.GetHashCode"/>).
	/// </summary>
	public class Player
	{
		/// <summary>
		/// A number representing this player for games that require a way to identify their players numerically.
		/// <br></br><br></br>
		/// These values are meant to start at 1; player1 would have a numeric value of 1, player2 a value of 2, and so on.
		/// <br></br><br></br>
		/// This value is only unique within the game this player is competing in.
		/// </summary>
		public int PlayerNumber { get; set; } = 0; //zero is invalid

		/// <summary>
		/// The name of the player. This value must be unique across players, as it identifies them.
		/// </summary>
		public string Username { get; }

		/// <summary>
		/// Whether this player is ready or not.
		/// <br></br><br></br>
		/// This can be used to dictate if the player is ready within a lobby; or connected to, and ready to play a game.
		/// </summary>
		public bool Ready { get; set; } = false;

		/// <summary>
		/// Creates a Player instance with the given <paramref name="username"/> assigned to <see cref="Username"/>.
		/// </summary>
		/// <param name="username">The unique name of the player.</param>
		public Player(string username)
		{
			Username = username;
		}

		public override bool Equals(object? obj)
		{
			return obj is Player p && Username == p.Username;
		}

		public override int GetHashCode()
		{
			return Username.GetHashCode();
		}
	}
}
