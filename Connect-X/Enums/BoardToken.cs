using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Connect_X.Enums
{
	/// <summary>
	/// Represents the owner of a token on the board; in the case of <see cref="None"/>, the token
	/// is considered to not exist (as opposed to having no owner).
	/// </summary>
	public enum BoardToken : byte
	{
		None,
		Player1,
		Player2
	}
}
