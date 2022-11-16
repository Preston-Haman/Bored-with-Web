using Connect_X.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Connect_X
{
	/// <summary>
	/// Represents a checkered 2D game board that can be used for a <see cref="ConnectionGame"/>.
	/// </summary>
	internal class ConnectionBoard
	{
		/// <summary>
		/// Represents slots on a checkered 2D board. If the board is represented as a square on the screen,
		/// the start of this array is the bottom left corner. At index <see cref="Columns"/>, this array
		/// is representing the leftmost edge of the second row (from the bottom).
		/// <br></br><br></br>
		/// In the case of a 3x3 board, the indices behave like this:<br></br>
		///	[6] [7] [8]	<br></br>
		///	[3] [4] [5]	<br></br>
		///	[0] [1] [2]	<br></br>
		///	<br></br>
		///	The number of tokens currently occupying slots of the board is represented by <see cref="PlayedTokenCount"/>.
		/// </summary>
		public BoardToken[] Slots { get; private set; }

		/// <summary>
		/// The number of rows present on this board.
		/// </summary>
		public byte Rows { get; }

		/// <summary>
		/// The number of columns present on this board.
		/// </summary>
		public byte Columns { get; }

		/// <summary>
		/// The minimum length of a sequence of tokens for it to be considered valid.
		/// </summary>
		public byte MinimumSequenceLength { get; }

		/// <summary>
		/// The number of tokens currently placed on the board.
		/// </summary>
		public int PlayedTokenCount { get; private set; } = 0;

		/// <summary>
		/// A convenience property to check if the board has any remaining slots available for play.
		/// <br></br><br></br>
		/// If the board has zero remaining empty slots, this will be true.
		/// </summary>
		public bool IsFull
		{
			get { return PlayedTokenCount == Slots.Length; }
		}

		/// <summary>
		/// Creates a square board with the specified number of rows and columns.
		/// </summary>
		/// <param name="rows">The number of rows for this board.</param>
		/// <param name="columns">The number of columns for this board.</param>
		/// <param name="sequenceLength">The minimum length of a connection sequence for it to be considered valid.</param>
		public ConnectionBoard(byte rows, byte columns, byte sequenceLength)
		{
			Rows = rows;
			Columns = columns;
			Slots = new BoardToken[rows * columns];
			MinimumSequenceLength = sequenceLength;
		}

		/// <summary>
		/// Clears the <see cref="Slots"/> of the board.
		/// </summary>
		public void Clear()
		{
			Slots = new BoardToken[Rows * Columns];
			PlayedTokenCount = 0;
		}

		/// <summary>
		/// Returns the <see cref="BoardToken"/> at the specified location on the board.
		/// </summary>
		/// <param name="row">The board row of interest.</param>
		/// <param name="column">The board column of interest.</param>
		/// <returns>The <see cref="BoardToken"/> at the specified <paramref name="row"/> and <paramref name="column"/>.</returns>
		/// <exception cref="ArgumentOutOfRangeException">If the given <paramref name="row"/> or <paramref name="column"/> is invalid.</exception>
		public BoardToken GetTokenAtLocation(int row, int column)
		{
			return Slots[GetTokenIndexAtLocation(row, column)];
		}

		/// <summary>
		/// Sets the token at the given <paramref name="row"/> and <paramref name="column"/> to be the same as <paramref name="token"/>.
		/// <br></br><br></br>
		/// If the given <paramref name="row"/> or <paramref name="column"/> is invalid, an <see cref="ArgumentOutOfRangeException"/> is thrown.
		/// </summary>
		/// <param name="token">The type of token to set at <paramref name="tokenIndex"/>.</param>
		/// <param name="row">The row of interest.</param>
		/// <param name="column">The column of interest.</param>
		/// <exception cref="ArgumentOutOfRangeException">If the given <paramref name="row"/> or <paramref name="column"/> is invalid.</exception>
		public void SetTokenAtLocation(BoardToken token, int row, int column)
		{
			int tokenIndex = GetTokenIndexAtLocation(row, column);

			if (Slots[tokenIndex] != token)
			{
				Slots[tokenIndex] = token;
				PlayedTokenCount += token == BoardToken.None ? -1 : 1;
			}
		}

		/// <summary>
		/// Checks if the slot at the given <paramref name="row"/> and <paramref name="column"/> is empty and returns true if so; false otherwise.
		/// <br></br><br></br>
		/// If the given <paramref name="row"/> or <paramref name="column"/> is invalid, an <see cref="ArgumentOutOfRangeException"/> is thrown.
		/// </summary>
		/// <param name="row">The row of interest.</param>
		/// <param name="column">The column of interest.</param>
		/// <returns>True if the slot is empty; false otherwise.</returns>
		/// <exception cref="ArgumentOutOfRangeException">If the given <paramref name="row"/> or <paramref name="column"/> is invalid.</exception>
		public bool IsTokenSlotAvailable(int row, int column)
		{
			return Slots[GetTokenIndexAtLocation(row, column)] == BoardToken.None;
		}

		/// <summary>
		/// Checks for a sequence of tokens touching the token specified by the given <paramref name="row"/> and <paramref name="column"/>
		/// in any <see cref="BoardDirection"/>. A valid sequence is considered to be a chain of adjacent tokens, of the same type,
		/// reaching the specified <see cref="MinimumSequenceLength"/>. Only one sequence has to be present for this method to return
		/// true. If no such sequence is present on the board at this location, then false is returned.
		/// </summary>
		/// <param name="row">The row of interest.</param>
		/// <param name="column">The column of interest.</param>
		/// <returns>True if a valid sequence was discovered at the given location, false otherwise.</returns>
		/// <exception cref="ArgumentOutOfRangeException">If the given <paramref name="row"/> or <paramref name="column"/> is invalid.</exception>
		/// <exception cref="ArgumentException">If the specified location contains <see cref="BoardToken.None"/>.</exception>
		public bool HasTokenSequenceAtLocation(int row, int column)
		{
			int tokenIndex = GetTokenIndexAtLocation(row, column);
			
			if (Slots[tokenIndex] == BoardToken.None)
				throw new ArgumentException("The specified location does not contain a player token.");

			BoardDirection[] directions = { BoardDirection.LEFT, BoardDirection.DOWN_LEFT, BoardDirection.DOWN, BoardDirection.DOWN_RIGHT };

			foreach (BoardDirection direction in directions)
			{
				//Assuming this token was just played, we only care about tokens that would require our token to be connected to form a valid sequence.
				int furthestIndexOfInterest = GetTokenIndexInDirectionFrom(direction, tokenIndex, MinimumSequenceLength - 1);
				if (HasTokenSequenceAtLocation(Slots[tokenIndex], direction.GetOpposite(), furthestIndexOfInterest))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Checks for a sequence of tokens matching the specified <paramref name="tokenType"/>, starting at <paramref name="startingTokenIndex"/>
		/// and moving in the given <paramref name="direction"/>. It's assumed that the last played token is located in the given <paramref name="direction"/>
		/// a distance of <paramref name="sequenceLength"/> - 1 away. This means that the chain of tokens in the given <paramref name="direction"/> will
		/// only be examined up to a maximum distance of (<paramref name="sequenceLength"/> * 2) - 1.
		/// </summary>
		/// <param name="tokenType">The type of token sequence to check for.</param>
		/// <param name="direction">The direction to check for a sequence in.</param>
		/// <param name="startingTokenIndex">The first token to examine as a potential member of the sequence.</param>
		/// <param name="sequenceLength">The required minimum length of a valid sequence.</param>
		/// <returns>True if a valid sequence was discovered. False otherwise.</returns>
		private bool HasTokenSequenceAtLocation(BoardToken tokenType, BoardDirection direction, int startingTokenIndex)
		{
			//The starting token counts as 1
			int currentTraversal = 1;
			int currentSequenceLength = Slots[startingTokenIndex] == tokenType ? 1 : 0;
			int maxTraversal = (MinimumSequenceLength * 2);

			int? nextTokenIndex = GetTokenIndexInDirectionFrom(direction, startingTokenIndex);
			while (nextTokenIndex is int tokenIndex && currentTraversal < maxTraversal)
			{
				currentSequenceLength = tokenType == Slots[tokenIndex] ? currentSequenceLength + 1 : 0;

				if (currentSequenceLength >= MinimumSequenceLength) return true;

				nextTokenIndex = GetTokenIndexInDirectionFrom(direction, tokenIndex);
				currentTraversal++;
			}

			return false;
		}

		/// <summary>
		/// Returns a valid <see cref="Slots"/> index, up to <paramref name="distance"/> away from the given <paramref name="tokenIndex"/>, in
		/// the specified <paramref name="direction"/>. If the <paramref name="distance"/> cannot be fully traversed, the closest value will be
		/// returned. This may result in the value of <paramref name="tokenIndex"/> being returned if it lies on the edge of the board.
		/// </summary>
		/// <param name="direction">The direction to move towards.</param>
		/// <param name="tokenIndex">The valid index of a token of interest.</param>
		/// <param name="distance">The number of slots to traverse away from the token at <paramref name="tokenIndex"/>.</param>
		/// <returns>A valid <see cref="Slots"/> index, up to <paramref name="distance"/> away from <paramref name="tokenIndex"/>, in the specified <paramref name="direction"/>.</returns>
		/// <exception cref="NotImplementedException">If the given direction is not supported.</exception>
		private int GetTokenIndexInDirectionFrom(BoardDirection direction, int tokenIndex, int distance)
		{
			int lastValidIndex = tokenIndex;
			int? nextTokenIndex = GetTokenIndexInDirectionFrom(direction, tokenIndex);
			for (int i = 0; i < distance && nextTokenIndex is int validIndex; i++, nextTokenIndex = GetTokenIndexInDirectionFrom(direction, validIndex))
			{
				lastValidIndex = validIndex;
			}

			return lastValidIndex;
		}

		/// <summary>
		/// Returns a valid <see cref="Slots"/> index adjacent to <paramref name="tokenIndex"/> in the specified <paramref name="direction"/> if it exists.
		/// </summary>
		/// <param name="direction">The direction to move towards.</param>
		/// <param name="tokenIndex">The valid index of a token of interest.</param>
		/// <returns>A valid <see cref="Slots"/> index adjacent to <paramref name="tokenIndex"/> in the specified <paramref name="direction"/>; or null if no such index exists.</returns>
		/// <exception cref="NotImplementedException">If the given direction is not supported.</exception>
		private int? GetTokenIndexInDirectionFrom(BoardDirection direction, int tokenIndex)
		{
			//Checks if going left would fall off the board, and returns false if it would.
			bool CanGoLeft()
			{
				return (tokenIndex % Columns) > 0;
			}

			//Checks if going right would fall off the board, and returns false if it would.
			bool CanGoRight()
			{
				return (tokenIndex % Columns) < (Columns - 1);
			}

			//This might be out of bounds.
			int index = direction switch
			{
				BoardDirection.UP => tokenIndex + Columns,
				BoardDirection.DOWN => tokenIndex - Columns,
				BoardDirection.LEFT => CanGoLeft() ? tokenIndex - 1 : -1,
				BoardDirection.RIGHT => CanGoRight() ? tokenIndex + 1 : -1,
				BoardDirection.UP_LEFT => CanGoLeft() ? tokenIndex + Columns - 1 : -1,
				BoardDirection.UP_RIGHT => CanGoRight() ? tokenIndex + Columns + 1 : -1,
				BoardDirection.DOWN_LEFT => CanGoLeft() ? tokenIndex - Columns - 1 : -1,
				BoardDirection.DOWN_RIGHT => CanGoRight() ? tokenIndex - Columns + 1 : -1,
				_ => throw new NotImplementedException()
			};

			if (index >= 0 && index < Slots.Length)
			{
				return index;
			}

			return null;
		}

		/// <summary>
		/// Calculates a token index based on the given <paramref name="row"/> and <paramref name="column"/> coordinate.
		/// The coordinates are zero-based.
		/// <br></br><br></br>
		/// If invalid values are given, an <see cref="ArgumentOutOfRangeException"/> is thrown.
		/// </summary>
		/// <param name="row">The row of interest.</param>
		/// <param name="column">The column of interest.</param>
		/// <returns>The token index for a token at the given <paramref name="row"/> and <paramref name="column"/> of the board.</returns>
		/// <exception cref="ArgumentOutOfRangeException">If the given <paramref name="row"/> or <paramref name="column"/> is invalid.</exception>
		private int GetTokenIndexAtLocation(int row, int column)
		{
			if (row >= Rows)
				throw new ArgumentOutOfRangeException(nameof(row), "The given row location is invalid.");

			if (column >= Columns)
				throw new ArgumentOutOfRangeException(nameof(column), "The given column location is invalid.");

			return (Columns * row) + column;
		}
	}
}
