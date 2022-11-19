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
			//TODO: restrict the user (by name) to a single Lobby at a time.
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

		/// <summary>
		/// The user is presented with an option to join a game from the lobby, and if they accept, it links here.
		/// <br></br><br></br>
		/// If the user is attempting to navigate to this url directly -- as opposed to being offered the link from
		/// the lobby -- they are redirected to the game's lobby.
		/// </summary>
		/// <param name="id">The RouteId of the game the user is trying to play.</param>
		public IActionResult Play(string? id)
		{
			//TODO: Verify that the user can join this game!
			if (/* user cannot join this game */ false)
			{
				return RedirectToAction(nameof(Lobby), new { id });
			}

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

			return View(game);
		}
	}
}
