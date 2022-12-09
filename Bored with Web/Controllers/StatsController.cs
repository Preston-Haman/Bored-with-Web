using Bored_with_Web.Data;
using Bored_with_Web.Games;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bored_with_Web.Controllers
{
	/// <summary>
	/// A simple controller that allows users to delete their game statistics, or guests to view them.
	/// </summary>
	[Authorize]
	public class StatsController : Controller
	{
		private readonly ApplicationDbContext dbContext;

		public StatsController(ApplicationDbContext dbContext)
		{
			this.dbContext = dbContext;
		}

		[AllowAnonymous]
		public IActionResult Guest()
		{
			return View(GuestCache.GetGameStats(HttpContext.Session.GetUsername()!));
		}

		public async Task<IActionResult> Delete(string id, string returnUrl)
		{
			//id is the gameRouteId
			GameInfo? game = CanonicalGames.GetGameInfoByRouteId(id);
			if (game is null)
				return NotFound();

			dbContext.GameStatistics.Remove((from gameStats in dbContext.GameStatistics
											 where gameStats.Username == User.Identity!.Name && gameStats.GameRouteId == id
											 select gameStats).Single());

			await dbContext.SaveChangesAsync();

			return LocalRedirect(returnUrl);
		}

		public async Task<IActionResult> DeleteAll(string returnUrl)
		{


			dbContext.GameStatistics.Remove((from gameStats in dbContext.GameStatistics
											 where gameStats.Username == User.Identity!.Name
											 select gameStats).Single());

			await dbContext.SaveChangesAsync();

			return LocalRedirect(returnUrl);
		}
	}
}
