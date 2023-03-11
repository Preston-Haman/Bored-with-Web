using Bored_with_Web.Games;

namespace Bored_with_Web.Hubs
{
	/// <summary>
	/// Defines methods that are available on the client side of a <see cref="CheckersHub"/>.
	/// </summary>
	public interface ICheckersClient : IMultiplayerGameClient
	{
		/// <summary>
		/// Called when a player joins or rejoins the game. Implementations should use the given information
		/// to update their representation of the board. This method may also be called in the situation that
		/// the server thinks the client's board has fallen out of sync with the server's board.
		/// <br></br><br></br>
		/// The board is laid out in such a way that the bottom left corner, from the perspective of
		/// the player using white tokens, is the zero index. An example layout is as follows:<br></br>
		/// [56][57][58][59][60][61][62][63]<br></br>
		/// [48][49][50][51][52][53][54][55]<br></br>
		/// [40][41][42][43][44][45][46][47]<br></br>
		/// [32][33][34][35][36][37][38][39]<br></br>
		/// [24][25][26][27][28][29][30][31]<br></br>
		/// [16][17][18][19][20][21][22][23]<br></br>
		/// [ 8][ 9][10][11][12][13][14][15]<br></br>
		/// [ 0][ 1][ 2][ 3][ 4][ 5][ 6][ 7]<br></br>
		/// <br></br>
		/// Each index of the board holds a value representing what can be found at that location of the board.<br></br>
		/// The following values are known:<br></br>
		/// 0 -- Empty<br></br>
		/// 1 -- White Token<br></br>
		/// 2 -- Kinged White Token<br></br>
		/// 3 -- Black Token<br></br>
		/// 4 -- Kinged Black Token<br></br>
		/// <br></br>
		/// See <see cref="Checkers.Token"/> for more information.
		/// </summary>
		/// <param name="boardState">A binary representation of the game board.</param>
		Task Joined(byte[] boardState);

		/// <summary>
		/// Called when a token has been played. Implementations should use the provided information to update their state
		/// of the board.
		/// <br></br><br></br>
		/// <paramref name="moves"/> is represented in the following way:<br></br>
		/// The first index holds the board index of the token that is being moved. Each subsequent index holds the board index
		/// the token is being moved to.
		/// </summary>
		/// <param name="moves">A binary representation of the move being made.</param>
		Task TokenPlayed(byte[] moves);

		/// <summary>
		/// Called when a token has reached the far side of the board. Implementations should update their state of the board
		/// to reflect that the token located at the specified <paramref name="boardIndex"/> has been 'kinged'.
		/// </summary>
		/// <param name="boardIndex">The location on the game board where a token should be 'kinged'.</param>
		Task TokenKinged(int boardIndex);
	}

	/// <summary>
	/// A basic implementation for managing the connection of Checkers players.
	/// </summary>
	public class CheckersHub : MultiplayerGameHub<Checkers, ICheckersClient>
	{
		protected override async Task OnJoinedGame()
		{
			await Clients.Caller.Joined(ActiveGame.GetBoard());
			await Clients.Caller.SetPlayerTurn(ActiveGame.ActivePlayerNumber);
		}

		/// <summary>
		/// Called by the client when they attempt to play a token on the board. See <see cref="ICheckersClient.TokenPlayed(byte[])"/>
		/// for more information on the format of <paramref name="moves"/>.
		/// </summary>
		/// <param name="moves">The list of board indices that represent the attempted play.</param>
		public async Task PlayToken(int[] moves)
		{
			//The client-side code expects a byte[] to be sent back... so we can just convert it now.
			byte[] clientMoves = Array.ConvertAll(moves, move => (byte) move);

			await ActiveGame.PlayToken(this, clientMoves);
		}

		public static async Task TokenPlayed(CheckersHub hub, byte[] moves)
		{
			await hub.Clients.Group(hub.GameId).TokenPlayed(moves);
			await hub.Clients.Group(hub.GameId).SetPlayerTurn(hub.ActiveGame.ActivePlayerNumber);
		}

		public static async Task TokenKinged(CheckersHub hub, int boardIndex)
		{
			await hub.Clients.Group(hub.GameId).TokenKinged(boardIndex);
		}

		public static async Task InvalidPlay(CheckersHub hub)
		{
			await hub.Clients.Caller.Joined(hub.ActiveGame.GetBoard());
			await hub.Clients.Caller.SetPlayerTurn(hub.ActiveGame.ActivePlayerNumber);
		}
	}
}
