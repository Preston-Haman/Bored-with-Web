using Bored_with_Web.Games;
using Bored_with_Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Bored_with_Web.Data
{
	/// <summary>
	/// Stores data relating to guests based on their username. Over time, this data is cleared.
	/// </summary>
	public static class GuestCache
	{
		private static readonly Dictionary<string, Dictionary<GameInfo, GameStatistic>> GAME_STATS_BY_GUEST_USERNAME = new();

		private static readonly Dictionary<string, DateTime> LAST_CACHE_UPDATE_BY_GUEST_USERNAME = new();

		/// <summary>
		/// Gets, or creates, an instance of <see cref="GameStatistic"/> representing the specified <paramref name="game"/>
		/// for the guest with the given <paramref name="username"/>.
		/// </summary>
		/// <param name="game">The game of interest.</param>
		/// <param name="username">The name of the guest of interest.</param>
		/// <returns>The guest <see cref="GameStatistic"/> for the specified <paramref name="game"/>.</returns>
		public static GameStatistic GetGameStats(GameInfo game, string username)
		{
			CleanOldCacheValues();

			LAST_CACHE_UPDATE_BY_GUEST_USERNAME[username] = DateTime.Now;

			if (!GAME_STATS_BY_GUEST_USERNAME.TryGetValue(username, out Dictionary<GameInfo, GameStatistic>? gameStats))
			{
				gameStats = new();
				GAME_STATS_BY_GUEST_USERNAME.Add(username, gameStats);

				gameStats.Add(game, new GameStatistic()
				{
					Username = username,
					GameRouteId = game.RouteId
				});
			}

			return gameStats[game];
		}

		/// <summary>
		/// Gets a list of game stats for the guest with the given <paramref name="username"/>, if any exist.
		/// </summary>
		/// <param name="username">The name of the guest to retrieve game stats for.</param>
		/// <returns>The list of game stats for the guest with the given <paramref name="username"/>, if any exist.</returns>
		public static IEnumerable<GameStatistic>? GetGameStats(string username)
		{
			CleanOldCacheValues();

			if (GAME_STATS_BY_GUEST_USERNAME.TryGetValue(username, out Dictionary<GameInfo, GameStatistic>? gameStats))
			{
				LAST_CACHE_UPDATE_BY_GUEST_USERNAME[username] = DateTime.Now;
				return (from entry in gameStats
						select entry.Value).ToList();
			}

			return null;
		}

		/// <summary>
		/// Transfers any stats stored for the guest with the given <paramref name="guestName"/> into the database
		/// entries for the registered account represented by the specified <paramref name="registeredUsername"/>.
		/// </summary>
		/// <param name="guestName">The name of the guest with stats to transfer.</param>
		/// <param name="registeredUsername">The registered username for the account to transfer the guest's stats into.</param>
		/// <param name="dbContext">The database context for this site.</param>
		public static async Task OnGuestLogin(string guestName, string registeredUsername, ApplicationDbContext dbContext)
		{
			if (GAME_STATS_BY_GUEST_USERNAME.TryGetValue(guestName, out Dictionary<GameInfo, GameStatistic>? gameStats))
			{
				Dictionary<GameInfo, GameStatistic> dbGameStats = await (from dbStats in dbContext.GameStatistics
																		 where dbStats.Username == registeredUsername
																		 select dbStats).ToDictionaryAsync(gameStat => CanonicalGames.GetGameInfoByRouteId(gameStat.GameRouteId)!);

				foreach (KeyValuePair<GameInfo, GameStatistic> keyValuePair in gameStats)
				{
					GameInfo game = keyValuePair.Key;
					GameStatistic stat = keyValuePair.Value;
					stat.Username = registeredUsername;

					if (dbGameStats.TryGetValue(game, out GameStatistic? dbStat))
					{
						dbStat.MergeStats(stat);
						dbContext.GameStatistics.Update(dbStat);
					}
					else
					{
						//Add stats to the database as new entities.
						dbContext.GameStatistics.Add(stat);
					}
				}

				await dbContext.SaveChangesAsync();
			}
		}

		private static void CleanOldCacheValues()
		{
			DateTime twentyMinutesAgo = DateTime.Now.AddMinutes(-20F);

			List<string> oldGuests = (from entry in LAST_CACHE_UPDATE_BY_GUEST_USERNAME
									  where entry.Value < twentyMinutesAgo
									  select entry.Key).ToList();

			foreach (string guestName in oldGuests)
			{
				GAME_STATS_BY_GUEST_USERNAME.Remove(guestName);
				LAST_CACHE_UPDATE_BY_GUEST_USERNAME.Remove(guestName);
			}
		}
	}
}
