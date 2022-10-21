﻿using System.Reflection;

namespace Bored_with_Web.Models
{
	public class GameInfo
	{
		public string Title { get; set; } = null!;

		public string ImageURL { get; set; } = "https://via.placeholder.com/512";

		public string Summary { get; set; } = null!;

		public string Description { get; set; } = null!;

		public string Rules { get; set; } = null!;

		public int RequiredPlayerCount { get; set; } = 1;
	}

	public enum GameInfoViewState
	{
		SELECTION,
		DESCRIPTION,
		LOBBY
	}

	public class GameInfoViewModel
	{
		public GameInfo Info { get; }

		public int CurrentPlayerCount { get; set; }

		public GameInfoViewState ViewState { get; set; }

		public GameInfoViewModel(GameInfo info, int currentPlayerCount, GameInfoViewState viewState)
		{
			Info = info;
			CurrentPlayerCount = currentPlayerCount;
			ViewState = viewState;
		}
	}

	//TODO: Load this from static data somehow...
	internal static class CanonicalGames
	{
		public static GameInfo ConnectFour { get; } = new()
		{
			Title = "Connect Four",
			//ImageURL = "#",
			Summary = "The Classic Four-in-a-row Matching Game.",
			Description = "Standard Connect Four. This is a game where players take turns placing " +
						  "coloured tokens into the board — which fall to the bottom — and attempt " +
						  "to get four of their tokens in a connected sequence.",
			Rules = "Connect Four is a game between two players. Each player is given tokens in a single " +
					"colour; these tokens are placed into a vertical board that has been slotted to accept " +
					"them. As tokens are placed into the board, they fall to the bottom; tokens placed in the " +
					"same slot will stack up — eventually filling that slot of the board. Players take turns " +
					"placing one token into the board at a time. The goal of players is to place their own tokens " +
					"in such a way that four or more are adjacent to each other in a sequence. The first player to " +
					"do so wins.",
			RequiredPlayerCount = 2
		};

		//Just in case this junk never finds its way into static data...
		public static IEnumerable<GameInfo> GetAll()
		{
			List<GameInfo> ret = new();
			foreach (PropertyInfo prop in typeof(CanonicalGames).GetProperties(BindingFlags.Public | BindingFlags.Static))
			{
				if (prop.GetValue(null) is GameInfo game)
				{
					ret.Add(game);
				}
			}
			return ret;
		}
	}
}