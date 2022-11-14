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
		/// </summary>
		public BoardToken[] Slots { get; }

		/// <summary>
		/// The number of rows present on this board.
		/// </summary>
		public int Rows { get; }

		/// <summary>
		/// The number of columns present on this board.
		/// </summary>
		public int Columns { get; }

		/// <summary>
		/// Creates a square board with the specified number of rows and columns.
		/// </summary>
		/// <param name="rows">The number of rows for this board.</param>
		/// <param name="columns">The number of columns for this board.</param>
		public ConnectionBoard(int rows, int columns)
		{
			Rows = rows;
			Columns = columns;
			Slots = new BoardToken[rows * columns];
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
		public int? GetTokenIndexAtLocation(int row, int column)
		{
			if (row >= Rows)
				throw new ArgumentOutOfRangeException(nameof(row), "The given row location is invalid.");

			if (column >= Columns)
				throw new ArgumentOutOfRangeException(nameof(column), "The given column location is invalid.");

			return (Columns * row) + column;
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
		public void SetToken(BoardToken token, int row, int column)
		{
			Slots[(int)GetTokenIndexAtLocation(row, column)!] = token;
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
			return Slots[(int)GetTokenIndexAtLocation(row, column)!] == BoardToken.None;
		}

		/// <summary>
		/// Checks for a sequence of tokens touching the given <paramref name="tokenIndex"/> in any <see cref="BoardDirection"/>.
		/// A valid sequence is considered to be a chain of adjacent tokens of the same type reaching the specified
		/// <paramref name="sequenceLength"/>. Only one sequence has to be present for this method to return true. If no
		/// such sequence is present on the board, then false is returned.
		/// </summary>
		/// <param name="tokenIndex">A valid <see cref="Slots"/> index to check for a sequence at.</param>
		/// <param name="sequenceLength">The required minimum length of the sequence.</param>
		/// <returns>True if a valid sequence was discovered at the given location, false otherwise.</returns>
		/// <exception cref="ArgumentException">If the <paramref name="tokenIndex"/> contains <see cref="BoardToken.None"/>.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If the <paramref name="tokenIndex"/> is invalid.</exception>
		public bool HasTokenSequenceAtLocation(int tokenIndex, int sequenceLength)
		{
			//Throw exception if input is invalid.
			_ = IsTokenIndexValid(tokenIndex);

			if (Slots[tokenIndex] == BoardToken.None)
				throw new ArgumentException("The specified token index does not contain a player token.", nameof(tokenIndex));

			BoardDirection[] directions = { BoardDirection.LEFT, BoardDirection.DOWN_LEFT, BoardDirection.DOWN, BoardDirection.DOWN_RIGHT };

			foreach (BoardDirection direction in directions)
			{
				//Assuming this token was just played, we only care about tokens that would require our token to be connected to form a valid sequence.
				int furthestIndexOfInterest = GetTokenIndexInDirectionFrom(direction, tokenIndex, sequenceLength - 1);
				if (HasTokenSequenceAtLocation(Slots[tokenIndex], direction.GetOpposite(), furthestIndexOfInterest, sequenceLength))
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
		private bool HasTokenSequenceAtLocation(BoardToken tokenType, BoardDirection direction, int startingTokenIndex, int sequenceLength)
		{

			//The starting token counts as 1
			int currentTraversal = 1;
			int currentSequenceLength = Slots[startingTokenIndex] == tokenType ? 1 : 0;
			int maxTraversal = (sequenceLength * 2);

			int? nextTokenIndex = GetTokenIndexInDirectionFrom(direction, startingTokenIndex);
			while (nextTokenIndex is int tokenIndex && currentTraversal < maxTraversal)
			{
				currentSequenceLength = tokenType == Slots[tokenIndex] ? currentSequenceLength + 1 : 0;

				if (currentSequenceLength >= sequenceLength) return true;

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
		/// <exception cref="ArgumentOutOfRangeException">If the <paramref name="tokenIndex"/> is invalid, or <paramref name="distance"/> is less than 1.</exception>
		/// <exception cref="NotImplementedException">If the given direction is not supported.</exception>
		public int GetTokenIndexInDirectionFrom(BoardDirection direction, int tokenIndex, int distance)
		{
			//Throw exception if input is invalid.
			_ = IsTokenIndexValid(tokenIndex);

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

			if (IsTokenIndexValid(index, false))
			{
				return index;
			}

			return null;
		}

		/// <summary>
		/// Checks if the given <paramref name="tokenIndex"/> is a valid index of <see cref="Slots"/>, and returns true if so.
		/// If <paramref name="throwException"/> is true, and <paramref name="tokenIndex"/> is invalid, an
		/// <see cref="ArgumentOutOfRangeException"/> is thrown.
		/// </summary>
		/// <param name="tokenIndex">The index to validate.</param>
		/// <param name="throwException">If this method should throw an exception if the given <paramref name="tokenIndex"/> is invalid.</param>
		/// <returns>True if the given <paramref name="tokenIndex"/> is valid; false, or an exception is thrown, otherwise.</returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="throwException"/> is true, and <paramref name="tokenIndex"/> is invalid.</exception>
		private bool IsTokenIndexValid(int tokenIndex, bool throwException = true)
		{
			bool valid = tokenIndex >= 0 && tokenIndex < Slots.Length;
			if (!valid && throwException)
				throw new ArgumentOutOfRangeException(nameof(tokenIndex), "The given token index is not valid.");
			return valid;
		}
	}
}
