using Bored_with_Web.Games;
using System.ComponentModel.DataAnnotations;

namespace Bored_with_Web.Models
{
	/// <summary>
	/// Statistics about a game, represented by <see cref="GameRouteId"/>, for a specified user, represented by <see cref="Username"/>.
	/// </summary>
	public class GameStatistic
	{
		/// <summary>
		/// The name of the user this statistic is for.
		/// </summary>
		[Key]
		public string Username { get; set; } = null!;

		/// <summary>
		/// The title of the game this statistic is for, as it would appear in the url of the website.
		/// </summary>
		[Key]
		[Display(Name = "Game")]
		public string GameRouteId { get; set; } = null!; //TODO: Consider finding a better way to identify games in the database.

		/// <summary>
		/// The number of matches played for this game.
		/// </summary>
		[Display(Name = "Games Played")]
		public int PlayCount { get; set; }

		/// <summary>
		/// The number of wins achieved in this game.
		/// </summary>
		public int Wins { get; set; }

		/// <summary>
		/// The number of losses for this game.
		/// </summary>
		public int Losses { get; set; }

		/// <summary>
		/// The number of stalemates for this game.
		/// </summary>
		public int Stalemates { get; set; }

		/// <summary>
		/// The number of times the user forfeited for this game.
		/// <br></br><br></br>
		/// This also includes the number of times the user was automatically forfeited due to a lost connection.
		/// </summary>
		public int Forfeitures { get; set; }

		/// <summary>
		/// The number of games that were left incomplete due to a forfeiture by the other competing players.
		/// <br></br><br></br>
		/// The games that are forfeited by the user whose stats are being represented are not counted here.
		/// </summary>
		[Display(Name = "Incomplete")]
		public int IncompleteCount { get; set; }

		/// <summary>
		/// The number of turns taken for this game.
		/// <br></br><br></br>
		/// Games that do not track turns will store -1 in this value.
		/// </summary>
		[Display(Name = "Turns Used")]
		public int MovesPlayed { get; set; } = -1;

		/// <summary>
		/// Combines the given <paramref name="stat"/> with this one.
		/// <br></br><br></br>
		/// The given <paramref name="stat"/> is left unaltered.
		/// </summary>
		/// <param name="stat">The stats to merge into this one.</param>
		/// <exception cref="ArgumentException">If the given <paramref name="stat"/> is for a different game or user.</exception>
		public void MergeStats(GameStatistic stat)
		{
			if (Username != stat.Username || GameRouteId != stat.GameRouteId)
			{
				throw new ArgumentException("Cannot merge stats representing different games or users!");
			}

			PlayCount += stat.PlayCount;
			Wins += stat.Wins;
			Losses += stat.Losses;
			Stalemates += stat.Stalemates;
			Forfeitures += stat.Forfeitures;
			IncompleteCount += stat.IncompleteCount;

			if (stat.MovesPlayed > 0)
				MovesPlayed += stat.MovesPlayed;
		}
	}
}
