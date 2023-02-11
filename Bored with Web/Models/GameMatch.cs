using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bored_with_Web.Models
{
	/// <summary>
	/// A database model that stores information about a game's match.
	/// <br></br><br></br>
	/// This is just a small BLOB that stores all information about the match
	/// that took place.
	/// </summary>
	public class GameMatch
	{
		/// <summary>
		/// The Id of the match, as given by the database. This value is part of an identity column.
		/// </summary>
		[Key]
		public long GameMatchId { get; private set; } = 0;

		/// <summary>
		/// The title of the game this match happened in, as it would appear in the url of the website.
		/// </summary>
		[Display(Name = "Game")]
		public string GameRouteId { get; set; } = null!; //TODO: Consider finding a better way to identify games in the database.

		/// <summary>
		/// Raw bytes serialized to the database that represent the events of the game's match.
		/// <br></br><br></br>
		/// Each game handles how it's been serialized independently.
		/// </summary>
		public byte[] MatchBlob { get; set; } = null!;
	}

	/// <summary>
	/// A database model that stores information about the participants of a <see cref="GameMatch"/>.
	/// </summary>
	public class GameMatchParticipant
	{
		/// <summary>
		/// The match that a player with the specified <see cref="ParticipantUsername"/> took part in.
		/// </summary>
		[Key]
		[ForeignKey(nameof(GameMatch.GameMatchId))]
		public GameMatch Match { get; set; } = null!;

		/// <summary>
		/// The username of the player that participated in this match. It's possible that this will be a guest name.
		/// </summary>
		[Key]
		//[ForeignKey(nameof(Microsoft.AspNetCore.Identity.IdentityUser.UserName))] //Can't do this because we might have to store guest names.
		public string ParticipantUsername { get; set; } = null!;
	}
}
