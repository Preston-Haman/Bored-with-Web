namespace Bored_with_Web.Games
{
	public class Player
	{
		public int PlayerNumber { get; set; } = 0; //zero is invalid

		public string Username { get; }

		public bool Ready { get; set; } = false;

		public Player(string username)
		{
			Username = username;
		}

		public override bool Equals(object? obj)
		{
			return obj is Player p && Username == p.Username;
		}

		public override int GetHashCode()
		{
			return Username.GetHashCode();
		}
	}
}
