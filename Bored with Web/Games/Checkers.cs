using Bored_with_Web.Hubs;

namespace Bored_with_Web.Games
{
	internal class CheckersBoard
	{
		/// <summary>
		/// Named values that represent the tokens that can be found on a Checkers board.
		/// </summary>
		public enum TokenType
		{
			NONE = 0,
			WHITE_TOKEN = 1,
			WHITE_TOKEN_KING = 2,
			BLACK_TOKEN = 3,
			BLACK_TOKEN_KING = 4
		}

		const byte CHECKERS_BOARD_SIZE = 8;

		/// <summary>
		/// A representation of the Checkers game board.
		/// </summary>
		public TokenType[] Tiles { get; private set; } = null!;

		private readonly HashSet<CheckersToken> whiteTokens = new();

		private readonly HashSet<CheckersToken> blackTokens = new();

		private const int WHITE_FORWARD = 1;

		private const int BLACK_FORWARD = -1;

		private class CheckersToken
		{
			private readonly CheckersBoard parent;

			public TokenType TokenType { get; private set; } = TokenType.NONE;

			public bool Kinged { get { return TokenType == TokenType.WHITE_TOKEN_KING || TokenType == TokenType.BLACK_TOKEN_KING; } }

			public int ForwardDirection { get { return TokenType.IsOpponentTo(TokenType.BLACK_TOKEN) ? WHITE_FORWARD : BLACK_FORWARD; } }

			public int X { get; private set; }

			public int Y { get; private set; }

			public int BoardIndex
			{
				get { return boardIndex; }

				set
				{
					if (boardIndex != -1)
					{
						parent.Tiles[boardIndex] = TokenType.NONE;
					}

					boardIndex = value;
					X = boardIndex % CHECKERS_BOARD_SIZE;
					Y = boardIndex / CHECKERS_BOARD_SIZE;
					parent.Tiles[boardIndex] = TokenType;
				}
			}

			private int boardIndex = -1;

			public CheckersToken(CheckersBoard parent, TokenType type, int boardIndex)
			{
				this.parent = parent;
				TokenType = type;
				BoardIndex = boardIndex;
			}

			public void KingMe()
			{
				if (TokenType == TokenType.WHITE_TOKEN)
				{
					TokenType = TokenType.WHITE_TOKEN_KING;
				}

				if (TokenType == TokenType.BLACK_TOKEN)
				{
					TokenType = TokenType.BLACK_TOKEN_KING;
				}

				parent.Tiles[boardIndex] = TokenType;
			}

			public bool CanJumpAnOpponentToken()
			{
				for (int i = -1; i < 2; i += 2)
				{
					if (parent.HasOpponentToken(X + i, Y + ForwardDirection, TokenType))
					{
						if (parent.IsTileVacant(X + i * 2, Y + ForwardDirection * 2))
						{
							return true;
						}
					}

					if (Kinged && parent.HasOpponentToken(X + i, Y - ForwardDirection, TokenType))
					{
						if (parent.IsTileVacant(X + i * 2, Y - ForwardDirection * 2))
						{
							return true;
						}
					}
				}

				return false;
			}

			public bool CanPerformMoveSet(byte[] moves)
			{
				bool CanPerformMove(byte start, byte end)
				{
					if (!parent.IsTileVacant(end % CHECKERS_BOARD_SIZE, end / CHECKERS_BOARD_SIZE))
					{
						return false;
					}

					int dx = (end % CHECKERS_BOARD_SIZE) - (start % CHECKERS_BOARD_SIZE);
					int dy = (end / CHECKERS_BOARD_SIZE) - (start / CHECKERS_BOARD_SIZE);

					if (Math.Abs(dx) > 2 || Math.Abs(dy) > 2 || dx == 0 || dy == 0)
					{
						return false;
					}

					if (Math.Abs(dx) == 2 || Math.Abs(dy) == 2)
					{
						//If either are one, the move is invalid; the computation here would fail if so.
						if (!parent.HasOpponentToken((start % CHECKERS_BOARD_SIZE) + dx / 2, (start / CHECKERS_BOARD_SIZE) + dy / 2, TokenType))
						{
							return false;
						}
					}

					dy /= Math.Abs(dy);
					if (!Kinged && dy != ForwardDirection)
					{
						return false;
					}

					return true;
				}

				//Basic check that it's us
				if (moves[0] != boardIndex)
				{
					return false;
				}

				for (int i = 1; i < moves.Length; i++)
				{
					if (!CanPerformMove(moves[i - 1], moves[i]))
					{
						return false;
					}
				}

				return true;
			}
		}

		public CheckersBoard()
		{
			Reset();
		}

		/// <summary>
		/// Determines if the given set of <paramref name="moves"/> are valid, and returns the result. The internal
		/// state of the board is not changed. See <see cref="ICheckersClient.TokenPlayed(byte[])"/>
		/// for more information on the format of <paramref name="moves"/>.
		/// </summary>
		/// <param name="moves">The list of board indices that represent the attempted play.</param>
		/// <param name="activePlayerTokenType">A <see cref="TokenType"/> representing the active player.</param>
		/// <returns>True if the move set is valid, false otherwise.</returns>
		public bool IsMoveSetValid(byte[] moves, TokenType activePlayerTokenType)
		{
			//Is it even a move?
			if (moves.Length < 2 || moves.Length > 13)
			{
				return false;
			}

			//Are the tiles we're moving to valid?
			foreach (byte location in moves)
			{
				if (location > CHECKERS_BOARD_SIZE * CHECKERS_BOARD_SIZE - 1)
				{
					return false;
				}
			}

			//Is the token ours to move?
			if (activePlayerTokenType.IsOpponentTo(Tiles[moves[0]]))
			{
				return false;
			}

			HashSet<CheckersToken> tokenSet = activePlayerTokenType.IsOpponentTo(TokenType.BLACK_TOKEN) ? whiteTokens : blackTokens;

			if (!IsJumpingMove(moves[0], moves[1]))
			{
				//Check all available moves... If the player can jump an opponent token, this move is invalid.
				foreach (CheckersToken token in tokenSet)
				{
					if (token.CanJumpAnOpponentToken())
					{
						return false;
					}
				}
			}

			if ((from token in tokenSet where token.BoardIndex == moves[0] select token).FirstOrDefault() is not CheckersToken movingToken)
			{
				return false;
			}

			return movingToken.CanPerformMoveSet(moves);
		}

		/// <summary>
		/// Alters the internal state of the board as if the given <paramref name="moves"/> were just made.
		/// If the token that was moved by this play becomes a kinged token, this method will return true.
		/// <br></br><br></br>
		/// It's assumed that the move set <paramref name="moves"/> represents is valid. If you are unsure of the validity of the
		/// move set, call <see cref="IsMoveSetValid(byte[], TokenType)"/> before calling this method.
		/// <br></br><br></br>
		/// See <see cref="ICheckersClient.TokenPlayed(byte[])"/> for more information on the format of <paramref name="moves"/>.
		/// </summary>
		/// <param name="moves">The list of board indices that represent the attempted play.</param>
		/// <returns>True if the moving token is kinged through this play, false otherwise.</returns>
		public bool PlayMoveSet(byte[] moves)
		{
			//Assume move set is valid
			//Return true if the token was kinged from this.
			HashSet<CheckersToken> ourTokenSet = Tiles[moves[0]].IsOpponentTo(TokenType.BLACK_TOKEN) ? whiteTokens : blackTokens;
			HashSet<CheckersToken> theirTokenSet = Tiles[moves[0]].IsOpponentTo(TokenType.WHITE_TOKEN) ? whiteTokens : blackTokens;
			CheckersToken movingToken = (from token in ourTokenSet where token.BoardIndex == moves[0] select token).Single();
			
			for (int i = 1; i < moves.Length; i++)
			{
				byte start = moves[i - 1];
				byte end = moves[i];

				if (IsJumpingMove(start, end))
				{
					int startX = start % CHECKERS_BOARD_SIZE;
					int startY = start / CHECKERS_BOARD_SIZE;

					int endX = end % CHECKERS_BOARD_SIZE;
					int endY = end / CHECKERS_BOARD_SIZE;

					int jumpedX = startX + ((endX - startX) / 2); //Keep the sign, but change from -2 or 2 to -1 or 1
					int jumpedY = startY + ((endY - startY) / 2);
					int? jumpedLocation = GetBoardLocation(jumpedX, jumpedY);

					CheckersToken jumpedToken = (from token in theirTokenSet where token.BoardIndex == jumpedLocation select token).Single();
					theirTokenSet.Remove(jumpedToken);

					//Not using jumpedLocation here because it's nullable.
					Tiles[jumpedToken.BoardIndex] = TokenType.NONE;
				}
			}

			movingToken.BoardIndex = moves[^1];
			if (!movingToken.Kinged && movingToken.Y == 0 || movingToken.Y == CHECKERS_BOARD_SIZE - 1)
			{
				movingToken.KingMe();
				return true;
			}

			return false;
		}

		/// <summary>
		/// Reverts the internal state of the board to the original state before any moves were made.
		/// </summary>
		public void Reset()
		{
			whiteTokens.Clear();
			blackTokens.Clear();
			Tiles = new TokenType[CHECKERS_BOARD_SIZE * CHECKERS_BOARD_SIZE];

			int[] tokenLocations = { /* White Tokens: */ 00, 02, 04, 06, 09, 11, 13, 15, 16, 18, 20, 22,
									 /* Black Tokens: */ 41, 43, 45, 47, 48, 50, 52, 54, 57, 59, 61, 63 };
			foreach (int location in tokenLocations)
			{
				//White tokens occupy indices up to 22. The rest are black tokens.
				HashSet<CheckersToken> tokenSet = location < 23 ? whiteTokens : blackTokens;
				tokenSet.Add(new CheckersToken(this, location < 23 ? TokenType.WHITE_TOKEN : TokenType.BLACK_TOKEN, location));
			}
		}

		private bool HasOpponentToken(int x, int y, TokenType us)
		{
			if (GetBoardLocation(x, y) is int location)
			{
				return us.IsOpponentTo(Tiles[location]);
			}

			return false;
		}

		private bool IsTileVacant(int x, int y)
		{
			if (GetBoardLocation(x, y) is int location)
			{
				return Tiles[location] == TokenType.NONE;
			}

			return false;
		}

		private static int? GetBoardLocation(int x, int y)
		{
			if (x >= 0 && x < CHECKERS_BOARD_SIZE && y >= 0 && y < CHECKERS_BOARD_SIZE)
			{
				return (y * CHECKERS_BOARD_SIZE) + x;
			}

			return null;
		}

		private static bool IsJumpingMove(int startingLocation, int endingLocation)
		{
			startingLocation %= CHECKERS_BOARD_SIZE;
			endingLocation %= CHECKERS_BOARD_SIZE;
			return Math.Abs(endingLocation - startingLocation) > 1;
		}
	}

	static class CheckersBoardTokenTypeExtension
	{
		/// <summary>
		/// Compares <paramref name="us"/> to <paramref name="them"/> and determines if they are of opposite colour.
		/// If the colours are opposite, the tokens are considered to be opponents of each other, and true is returned.
		/// <br></br><br></br>
		/// In the case of either token being colourless (<see cref="CheckersBoard.TokenType.NONE"/>), false is returned.
		/// </summary>
		/// <param name="us">This <see cref="CheckersBoard.TokenType"/>.</param>
		/// <param name="them">The <see cref="CheckersBoard.TokenType"/> to compare <paramref name="us"/> to.</param>
		/// <returns>True if the comparison found the <paramref name="us"/> and <paramref name="them"/> to be of opposite colours.</returns>
		/// <exception cref="ArgumentException">If <paramref name="us"/> is not a supported <see cref="CheckersBoard.TokenType"/>.</exception>
		public static bool IsOpponentTo(this CheckersBoard.TokenType us, CheckersBoard.TokenType them)
		{
			return us switch
			{
				CheckersBoard.TokenType.NONE => false,
				CheckersBoard.TokenType.WHITE_TOKEN or CheckersBoard.TokenType.WHITE_TOKEN_KING => them == CheckersBoard.TokenType.BLACK_TOKEN || them == CheckersBoard.TokenType.BLACK_TOKEN_KING,
				CheckersBoard.TokenType.BLACK_TOKEN or CheckersBoard.TokenType.BLACK_TOKEN_KING => them == CheckersBoard.TokenType.WHITE_TOKEN || them == CheckersBoard.TokenType.WHITE_TOKEN_KING,
				_ => throw new ArgumentException($"Unsupported {nameof(CheckersBoard.TokenType)} value!", nameof(us)),
			};
		}
	}

	/// <summary>
	/// An implementation of <see cref="SimpleGame"/> that handles Checkers.
	/// </summary>
	[Game("Checkers")]
	public class Checkers : SimpleGame
	{
		public int ActivePlayerNumber { get; private set; } = 1;

		private bool tokensHaveBeenPlayed = false;

		private readonly CheckersBoard board = new();

		public Checkers() //This constructor is Required by the contract of SimpleGame.
		{
			ResetBoard();
		}

		/// <summary>
		/// Returns a copy of the board.
		/// </summary>
		/// <returns>A copy of the board.</returns>
		public byte[] GetBoard()
		{
			return Array.ConvertAll(board.Tiles, boardSlot => (byte) boardSlot);
		}

		/// <summary>
		/// Resets the board to its original state, and assigns the given player the next turn.
		/// </summary>
		/// <param name="nextPlayer">The next player to get a turn.</param>
		public void ResetBoard(Player nextPlayer = null!)
		{
			if (nextPlayer is not null)
			{
				ActivePlayerNumber = GetInternalPlayer(nextPlayer).PlayerNumber;
			}

			board.Reset();
		}

		/// <summary>
		/// Returns true if the given <paramref name="moves"/> are valid. The internal state of the board is updated accordingly.
		/// </summary>
		/// <param name="moves">The list of moves, as described by <see cref="Bored_with_Web.Hubs.ICheckersClient.TokenPlayed(byte[])"/>.</param>
		/// <returns>True if the board has been updated by this move, false otherwise.</returns>
		public async Task PlayToken(CheckersHub hub, byte[] moves)
		{
			if (!board.IsMoveSetValid(moves, ActivePlayerNumber == 1 ? CheckersBoard.TokenType.WHITE_TOKEN : CheckersBoard.TokenType.BLACK_TOKEN))
			{
				await CheckersHub.InvalidPlay(hub);
				return;
			}

			//TODO: Move was valid, but we have to check for a stalemate! If the moves repeat for three turns, it's a stalemate.
			//if (/* Stalemate */)
			//{
			//	await CheckersHub.Stalemate(hub);
			//	return;
			//}

			bool tokenKinged = board.PlayMoveSet(moves);
			
			//mod 2 + 1 keeps it cycling between 1 and 2.
			ActivePlayerNumber %= 2;
			ActivePlayerNumber++;

			tokensHaveBeenPlayed = true;
			await CheckersHub.TokenPlayed(hub, moves);

			if (tokenKinged)
			{
				//Token was kinged by the move.
				await CheckersHub.TokenKinged(hub, moves[^1]);
			}
		}

		public override bool PlayerCannotLeaveWithoutForfeiting()
		{
			return tokensHaveBeenPlayed && MatchIsActive;
		}
	}
}
