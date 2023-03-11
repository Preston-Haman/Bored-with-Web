using System.Reflection;

namespace Bored_with_Web.Games
{
	/// <summary>
	/// Information about a game playable through this website.
	/// <br></br><br></br>
	/// <see cref="CanonicalGames"/> stores a list of canonical instances representing available games.
	/// </summary>
	public class GameInfo
	{
		/// <summary>
		/// The type that implements the handling of this game. This must be an implementation of <see cref="SimpleGame"/>.
		/// </summary>
		public Type ImplementingType { get; set; } = typeof(SimpleGame);

		/// <summary>
		/// The name of the game as it would be displayed to the user.
		/// <br></br><br></br>
		/// Special characters are not supported by the implementation of <see cref="RouteId"/>.
		/// </summary>
		public string Title { get; set; } = null!;

		/// <summary>
		/// The title of the game as it appears in the url of the website.
		/// <br></br><br></br>
		/// At the moment, this is the value of the <see cref="Title"/>, but with all spaces converted to dashes.
		/// </summary>
		public string RouteId { get { return Title.Replace(' ', '-'); } }

		/// <summary>
		/// The url of a preview image that represents this game visually.
		/// <br></br><br></br>
		/// The default value is a 512x512 preview image from placeholder.com.
		/// </summary>
		public string ImageURL { get; set; } = "https://via.placeholder.com/512";

		/// <summary>
		/// A very brief description of the game.
		/// <br></br><br></br>
		/// This is only intended to get the point across to those who are familiar with the game already.
		/// </summary>
		public string Summary { get; set; } = null!;

		/// <summary>
		/// A description of the game.
		/// <br></br><br></br>
		/// This is not meant to cover the rules (see <see cref="Rules"/>); but to express what this game is about
		/// in a more detailed way than the <see cref="Summary"/>.
		/// </summary>
		public string Description { get; set; } = null!;

		/// <summary>
		/// The rules of the game in enough detail to come to an understanding of it.
		/// </summary>
		public string Rules { get; set; } = null!;

		/// <summary>
		/// The minimal number of player required to start the game.
		/// <br></br><br></br>
		/// At this time, ranges for this information are not supported.
		/// </summary>
		public int RequiredPlayerCount { get; set; } = 1;
	}

	/// <summary>
	/// Managing class for canonical instances of <see cref="GameInfo"/>. The instances managed by this class
	/// represent all available games that can be displayed (and, hopefully, played) on this website.
	/// </summary>
	internal static class CanonicalGames
	{
		//TODO: Load this from static data somehow...

		/// <summary>
		/// Information about the version of Connect Four that is playable on this site.
		/// </summary>
		public static GameInfo ConnectFour { get; } = new()
		{
			ImplementingType = typeof(ConnectFour),
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

		/// <summary>
		/// Information about the version of Checkers that is playable on this site.
		/// </summary>
		public static GameInfo Checkers { get; } = new()
		{
			ImplementingType = typeof(Checkers),
			Title = "Checkers",
			//ImageURL = "#",
			Summary = "The Classic Diagonal-Jumping Strategy Game.",
			Description = "Standard Checkers. This is a game in which players take turns moving coloured pieces diagonally, " +
						  "while jumping over their opponents pieces. The first player to claim all of their opponent's pieces " +
						  "is the winner.",
			Rules = "Checkers is a game between two players on an 8 x 8 checkered board. Each player has 12 pieces in their own colour. " +
					"Each player is allotted a turn, one after the other; during which, they can move one of their own pieces. Each piece " +
					"can only move diagonally forward; that is, until reaching the other side of the board, where it is 'kinged', and can " +
					"move diagonally forwards or backwards. In order to move a piece, the immediately adjacent tile of the board must be " +
					"vacant; or, in the case of jumping over an opponent's piece, the next tile in the same direction must be vacant. While " +
					"performing a jump, a piece may come to a position where it can jump again; in such a case, a chained jump must be " +
					"performed (the same piece jumps over the next opponent piece, as well). Note that chain jumping does not allow a piece " +
					"to move backwards if it has not yet been 'kinged'. The goal is merely to prevent one's opponent from being able to make a " +
					"move. This can be done by removing all of their pieces from play, or by preventing their movement altogether. There are " +
					"some constraints which make this simple concept more difficult. During their turn, if it's possible to jump over an " +
					"opponent piece, then the player must do so. If the player touches a movable piece, it must be the piece that is moved. " +
					"If a player cannot move a piece during their turn, they lose. If the game enters into a repeating sequence of moves, " +
					"then the match is considered a stalemate.",
			RequiredPlayerCount = 2
		};

		/// <summary>
		/// A list of <see cref="GameInfo"/> instances to display and allow users to play through this site.
		/// </summary>
		public static IEnumerable<GameInfo> AllGames { get; }

		/// <summary>
		/// The set of <see cref="GameInfo"/> instances found in <see cref="AllGames"/>, mapped by their <see cref="GameInfo.RouteId"/>.
		/// </summary>
		private static readonly Dictionary<string, GameInfo> gamesByTitle;

		/// <summary>
		/// Generates the instances for <see cref="AllGames"/>, and populates <see cref="gamesByTitle"/>.
		/// </summary>
		static CanonicalGames()
		{
			gamesByTitle = new();
			AllGames = GetAll();
		}

		/// <summary>
		/// Retrieves the <see cref="GameInfo"/> represented by the given <paramref name="routeId"/>, if it exists.
		/// </summary>
		/// <param name="routeId">The title of the game, as it appears in the url of the website.</param>
		/// <returns>The <see cref="GameInfo"/> represented by the given <paramref name="routeId"/>, or null if it does not exist.</returns>
		public static GameInfo? GetGameInfoByRouteId(string routeId)
		{
			gamesByTitle.TryGetValue(routeId, out GameInfo? game);
			return game;
		}

		/// <summary>
		/// Creates canonical instances of <see cref="GameInfo"/> that are supported by this site, and returns them in a list.
		/// </summary>
		/// <returns>A list of canonical instances of <see cref="GameInfo"/>.</returns>
		private static IEnumerable<GameInfo> GetAll()
		{
			List<GameInfo> ret = new();

			//Just in case this junk never finds its way into static data...
			foreach (PropertyInfo prop in typeof(CanonicalGames).GetProperties(BindingFlags.Public | BindingFlags.Static))
			{
				if (prop.GetValue(null) is GameInfo game)
				{
					ret.Add(game);
					gamesByTitle.Add(game.RouteId, game);
				}
			}
			return ret;
		}
	}
}
