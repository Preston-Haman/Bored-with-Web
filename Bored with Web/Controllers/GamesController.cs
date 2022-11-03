using Bored_with_Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Bored_with_Web.Controllers
{
	public class GamesController : Controller
	{
		public IActionResult Index()
		{
			List<GameInfoViewModel> gameViewModels = new();

			foreach (GameInfo game in CanonicalGames.AllGames)
			{
				//TODO: Get current player count from whatever system can track that later on...
				gameViewModels.Add(new GameInfoViewModel(game, currentPlayerCount: 0, GameInfoViewState.SELECTION));
			}

			return View(gameViewModels);
		}

		public IActionResult Detail(string? id)
		{
			//id is RouteId of the game
			if (id is null)
			{
				return RedirectToAction(nameof(Index));
			}

			GameInfo? game = CanonicalGames.GetGameInfoByRouteId(id);
			if (game is null)
			{
				return NotFound();
			}

			//TODO: Get current player count from whatever system can track that later on...
			return View(new GameInfoViewModel(game, currentPlayerCount: 0, GameInfoViewState.DESCRIPTION));
		}

		public IActionResult Lobby(string? id)
		{
			//id is RouteId of the game
			if (id is null)
			{
				return RedirectToAction(nameof(Index));
			}

			GameInfo? game = CanonicalGames.GetGameInfoByRouteId(id);
			if (game is null)
			{
				return NotFound();
			}

			//TODO: Get current player count from whatever system can track that later on...
			//Might have to replace this model with something else later... depends on how I feel about SignalR hubs
			return View(new GameInfoViewModel(game, currentPlayerCount: 0, GameInfoViewState.LOBBY));
		}
	}
}
