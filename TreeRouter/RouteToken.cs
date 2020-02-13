using System.Text.RegularExpressions;

namespace TreeRouter
{
	public class RouteToken
	{
		public string Name { get; set; }
		public bool Optional { get; set; }
		public bool Greedy { get; set; }
		public bool MatchAny { get; set; }
		public Regex Matcher { get; set; }
		public string Text { get; set; }

		public RouteTokenHasher Hasher => new RouteTokenHasher(this);
	}

	public class RouteTokenHasher
	{
		private RouteToken _token;

		public RouteTokenHasher() : this(new RouteToken())
		{
		}

		public RouteTokenHasher(RouteToken token) => _token = token;

		public string Name => _token.Name;
		public bool Optional => _token.Optional;
		public bool Greedy => _token.Greedy;
		public bool MatchAny => _token.MatchAny;
		public string Matcher => _token.Matcher?.ToString();
		public string Text => _token.Text;
	}
}
