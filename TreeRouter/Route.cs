using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TreeRouter
{
	public class Route
	{
		
		private static Regex plainVar = new Regex(@"\s*{\s*(\w+)\s*}\s*");
		private static Regex optionalVar = new Regex(@"\s*{\s*(\w+)\s*\?\s*}\s*");
		private static Regex greedyVar = new Regex(@"\s*{\s*(\w+)\s*\*\s*}\s*");
		
		public List<RouteToken> Tokens { get; private set; }
		public int LiteralTokenCount { get; private set; }
		public Constraints Constraints { get; set; }
		public Defaults Defaults { get; set; }
		public Func<Request, Task> ActionHandler { get; set; }
		public Type ClassHandler { get; set; }
		public string[] Methods { get; set; }

		public static Route FromOptions(RouteOptions options) =>
			new Route
			{
				Path = options.Path,
				Constraints = options.Constraints,
				Defaults = options.Defaults,
				ActionHandler = options.ActionHandler,
				ClassHandler = options.ClassHandler,
				Methods = options.Methods
			};
		

		private string _path;
		public string Path
		{
			get => _path;
			set
			{
				_path = value;
				LiteralTokenCount = 0;
				Tokens = new List<RouteToken>();
				var parts = value.Trim('/').Split('/');
				var i = 0;
				var optional = false;
				foreach (var part in parts)
				{
					i++;
					var token = new RouteToken();
				
					var match = plainVar.Match(part);
					if (match.Success)
					{
						if (optional)
							PathError("Cannot have required segments after optional ones", value);
						token.Name = match.Groups[1].Value;
						token.MatchAny = true;
						Tokens.Add(token);
						continue;
					}

					match = optionalVar.Match(part);
					if (match.Success)
					{
						optional = true;
						token.Name = match.Groups[1].Value;
						token.MatchAny = true;
						token.Optional = true;
						Tokens.Add(token);
						continue;
					}

					match = greedyVar.Match(part);
					if (match.Success)
					{
						if (i < parts.Length)
							PathError("Greedy expressions can only go at the end.", value);
						optional = true;
						token.Name = match.Groups[1].Value;
						token.MatchAny = true;
						token.Greedy = true;
						Tokens.Add(token);
						continue;
					}
				
					if (optional)
						PathError("Cannot have required segments after optional ones", value);
					LiteralTokenCount++;
					token.Text = part;
					Tokens.Add(token);
				}
			}
		}
		

		private void PathError(string msg, string path) =>
			throw new ArgumentException(msg + "Route: " + path);

		public RequestDictionary ExtractVars(string path)
		{
			var parts = path.Trim('/').Split('/');
			var rd = new RequestDictionary();
			var index = 0;
			while (index < parts.Length)
			{
				var part = parts[index];
				var token = Tokens[index];
				if (token.MatchAny || token.Matcher != null)
				{
					rd[token.Name] = token.Greedy ? string.Join('/', parts.Skip(index)) : part;
					if (token.Greedy) return rd;
				}
				index++;
			}
			return rd;
		}
		
	}

	public class Route<TController> : Route 
	{
		public Route()
		{
			ClassHandler = typeof(TController);
		}
	}
	
}
