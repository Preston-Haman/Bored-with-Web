using Bored_with_Web.Games;
using Bored_with_Web.Models;

namespace Bored_with_Web.Data
{
	/// <summary>
	/// An extension class for <see cref="SimpleGameOutcome"/> that handles database related updates for <see cref="GameStatistic"/>.
	/// </summary>
	public static class SimpleGameOutcomeExtension
	{
		public static async Task StoreGameStats(this SimpleGameOutcome outcome, ApplicationDbContext dbContext)
		{
			//Assume if the player username contains a pound symbol (#) that they are a guest.
			static bool IsGuest(Player player)
			{
				return player.Username.Contains('#');
			}

			void UpdateGameStats(GameStatistic stats, Player player)
			{
				if (outcome.EndState == GameEnding.NONE)
					return;

				stats.PlayCount++;

				if (outcome.EndState == GameEnding.VICTORY && outcome.WinningPlayers.Contains(player))
				{
					stats.Wins++;
				}

				if (outcome.EndState == GameEnding.VICTORY && outcome.LosingPlayers.Contains(player))
				{
					stats.Losses++;
				}

				if (outcome.EndState == GameEnding.STALEMATE)
				{
					stats.Stalemates++;
				}

				if (outcome.EndState == GameEnding.INCOMPLETE)
				{
					if (outcome.ForfeitingPlayers.Contains(player))
						stats.Forfeitures++;
					else
						stats.IncompleteCount++;
				}

				if (outcome.PlayerTurnCounts.TryGetValue(player, out int turnCount) && turnCount > 0)
				{
					stats.MovesPlayed = stats.MovesPlayed < 0 ? turnCount : stats.MovesPlayed + turnCount;
				}
			}

			foreach (Player player in outcome.Game.Players)
			{
				bool newDbRecord = false;

				if (IsGuest(player))
				{
					GameStatistic stats = GuestCache.GetGameStats(outcome.Game.Info, player.Username);
					UpdateGameStats(stats, player);
				}
				else
				{
					GameStatistic? dbStats = (from gameStats in dbContext.GameStatistics
											  where gameStats.Username == player.Username && gameStats.GameRouteId == outcome.Game.Info.RouteId
											  select gameStats).SingleOrDefault();
					if (dbStats is null)
					{
						newDbRecord = true;
						dbStats = new GameStatistic()
						{
							Username = player.Username,
							GameRouteId = outcome.Game.Info.RouteId
						};
					}

					UpdateGameStats(dbStats, player);

					if (newDbRecord)
					{
						dbContext.GameStatistics.Add(dbStats);
					}
					else
					{
						dbContext.GameStatistics.Update(dbStats);
					}

					await dbContext.SaveChangesAsync();
				}
			}
		}
	}
}
