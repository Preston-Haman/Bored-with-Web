﻿using Bored_with_Web.Models;
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
			//id is title of the game
			if (id is null)
			{
				return RedirectToAction(nameof(Index));
			}

			GameInfo? game = CanonicalGames.GetGameInfoByTitle(id);
			if (game is null)
			{
				return NotFound();
			}

			//TODO: Get current player count from whatever system can track that later on...
			return View(new GameInfoViewModel(game, currentPlayerCount: 0, GameInfoViewState.DESCRIPTION));
		}
	}
}
