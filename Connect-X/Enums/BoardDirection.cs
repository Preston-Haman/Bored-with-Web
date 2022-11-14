using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Connect_X.Enums
{
	/// <summary>
	/// A direction on a checkered 2D game board.
	/// </summary>
	internal enum BoardDirection
	{
		NONE,
		UP,
		DOWN,
		LEFT,
		RIGHT,
		UP_LEFT,
		UP_RIGHT,
		DOWN_LEFT,
		DOWN_RIGHT
	}

	/// <summary>
	/// An extension class for <see cref="BoardDirection"/>.
	/// </summary>
	internal static class BoardDirectionFriend
	{
		/// <summary>
		/// Returns the opposite direction relative to this direction.
		/// </summary>
		/// <param name="direction">This direction.</param>
		/// <returns>The direction opposite to this one.</returns>
		/// <exception cref="NotImplementedException">If the given direction has no implemented opposite.</exception>
		public static BoardDirection GetOpposite(this BoardDirection direction)
		{
			return direction switch
			{
				BoardDirection.UP => BoardDirection.DOWN,
				BoardDirection.DOWN => BoardDirection.UP,
				BoardDirection.LEFT => BoardDirection.RIGHT,
				BoardDirection.RIGHT => BoardDirection.LEFT,
				BoardDirection.UP_LEFT => BoardDirection.DOWN_RIGHT,
				BoardDirection.UP_RIGHT => BoardDirection.DOWN_LEFT,
				BoardDirection.DOWN_LEFT => BoardDirection.UP_RIGHT,
				BoardDirection.DOWN_RIGHT => BoardDirection.UP_LEFT,
				_ => throw new NotImplementedException()
			};
		}
	}
}
