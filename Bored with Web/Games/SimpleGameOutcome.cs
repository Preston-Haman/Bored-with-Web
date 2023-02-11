namespace Bored_with_Web.Games
{
	/// <summary>
	/// The ending state of the game.
	/// </summary>
	public enum GameEnding : byte
	{
		/// <summary>
		/// The game never started.
		/// </summary>
		NONE,

		/// <summary>
		/// The game was incomplete. This is likely due to the competing players leaving, or forfeiting.
		/// </summary>
		INCOMPLETE,

		/// <summary>
		/// The game ended in a draw.
		/// </summary>
		STALEMATE,

		/// <summary>
		/// The game ended with one or more players being victorious.
		/// </summary>
		VICTORY
	}

	/// <summary>
	/// A class that represents the outcome of a game.
	/// </summary>
	public class SimpleGameOutcome
	{
		/// <summary>
		/// The way the game ended.
		/// <br></br><br></br>
		/// If the game was incomplete, or ended in a stalemate, all players are considered
		/// to be losing players (see <see cref="LosingPlayers"/>).
		/// </summary>
		public GameEnding EndState { get; set; }

		/// <summary>
		/// The game this outcome corresponds to.
		/// </summary>
		public SimpleGame Game { get; set; } = null!;

		/// <summary>
		/// The players that won the game.
		/// </summary>
		public HashSet<Player> WinningPlayers { get; set; } = new();

		/// <summary>
		/// The players that lost the game.
		/// </summary>
		public HashSet<Player> LosingPlayers { get; set; } = new();

		/// <summary>
		/// The players that gave up during this game.
		/// </summary>
		public HashSet<Player> ForfeitingPlayers { get; set; } = new();

		/// <summary>
		/// The number of turns taken by each player.
		/// </summary>
		public Dictionary<Player, int> PlayerTurnCounts { get; set; } = new();

		/// <summary>
		/// Whether or not this game outcome contains valid information stored in <see cref="GameEventsBlob"/>.
		/// </summary>
		public bool HasReplayData { get { return GameEventsBlob.Length > 0; } }

		/// <summary>
		/// A binary serialization of the events that took place during the game.
		/// </summary>
		public byte[] GameEventsBlob { get; set; } = Array.Empty<byte>();
	}
}
