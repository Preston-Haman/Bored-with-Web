using Bored_with_Web.Games;
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
				gameViewModels.Add(new GameInfoViewModel(game, GameService.GetPlayerCount(game), GameInfoViewState.SELECTION));
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

			return View(new GameInfoViewModel(game, GameService.GetPlayerCount(game), GameInfoViewState.DESCRIPTION));
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

			return View(new GameInfoViewModel(game, GameService.GetPlayerCount(game), GameInfoViewState.LOBBY));
		}

		/// <summary>
		/// The user is presented with an option to join a game from the lobby, and if they accept, it links here.
		/// <br></br><br></br>
		/// If the user is attempting to navigate to this url directly -- as opposed to being offered the link from
		/// the lobby -- they are redirected to the game's lobby.
		/// </summary>
		/// <param name="id">The RouteId of the game the user is trying to play.</param>
		/// <param name="game">Part of the query string; the gameId for the game, as assigned by the <see cref="GameService"/>.</param>
		public IActionResult Play(string? id, [FromQuery] string? game)
		{
			//id is RouteId of the game
			string? username = HttpContext.Session.GetUsername();
			if (id is null || game is null || GameService.GetGame(game) is not SimpleGame activeGame || username is null)
			{
				return RedirectToAction(nameof(Index));
			}

			GameInfo? info = CanonicalGames.GetGameInfoByRouteId(id);
			if (info is null)
			{
				return NotFound();
			}

			Player player = new(username);
			//Verify that the user can join this game
			if (!activeGame.Players.Contains(player))
			{
				return RedirectToAction(nameof(Lobby), new { id });
			}

			//Required by MultiplayerGameHub implementations.
			ViewData["gameId"] = game;

			//The current player count doesn't matter here
			return View(new GameInfoViewModel(info, currentPlayerCount: 0, GameInfoViewState.PLAY));
		}
	}
}
