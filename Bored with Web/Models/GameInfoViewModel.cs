using Bored_with_Web.Games;

namespace Bored_with_Web.Models
{
	/// <summary>
	/// A description of the context in which a related <see cref="GameInfoViewModel"/> is being used.
	/// </summary>
	public enum GameInfoViewState
	{
		/// <summary>
		/// Indicates that the related <see cref="GameInfoViewModel"/> is being used to express information
		/// related to the Games/Index view.
		/// </summary>
		SELECTION,

		/// <summary>
		/// Indicates that the related <see cref="GameInfoViewModel"/> is being used to express information
		/// related to the Games/Detail view.
		/// </summary>
		DESCRIPTION,

		/// <summary>
		/// Indicates that the related <see cref="GameInfoViewModel"/> is being used to express information
		/// related to the Games/Lobby view.
		/// </summary>
		LOBBY,

		/// <summary>
		/// Indicates that the related <see cref="GameInfoViewModel"/> is being used to express information
		/// related to the Games/Play view.
		/// </summary>
		PLAY
	}

	/// <summary>
	/// A view model for displaying a <see cref="GameInfo"/> instance to the user, along with
	/// some other related information.
	/// </summary>
	public class GameInfoViewModel
	{
		/// <summary>
		/// The basic information about the game this model is for.
		/// </summary>
		public GameInfo Info { get; }

		/// <summary>
		/// The current number of players playing the game/waiting in the lobby.
		/// <br></br><br></br>
		/// This value is only populated in a context in which it needs to be displayed (see <see cref="ViewState"/>).
		/// </summary>
		public int CurrentPlayerCount { get; set; }

		/// <summary>
		/// The context in which this information is being used (see <see cref="GameInfoViewState"/>).
		/// </summary>
		public GameInfoViewState ViewState { get; set; }

		/// <summary>
		/// Creates this model with the given information.
		/// </summary>
		/// <param name="info">The game this model is for.</param>
		/// <param name="currentPlayerCount">The current number of players playing this game/waiting in the lobby.</param>
		/// <param name="viewState">The context in which this model is being used.</param>
		public GameInfoViewModel(GameInfo info, int currentPlayerCount, GameInfoViewState viewState)
		{
			Info = info;
			CurrentPlayerCount = currentPlayerCount;
			ViewState = viewState;
		}
	}
}
