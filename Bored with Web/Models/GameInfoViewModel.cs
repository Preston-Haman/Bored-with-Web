using Bored_with_Web.Games;

namespace Bored_with_Web.Models
{
	public enum GameInfoViewState
	{
		SELECTION,
		DESCRIPTION,
		LOBBY,
		PLAY
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
}
