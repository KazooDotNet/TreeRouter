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
	}
}
