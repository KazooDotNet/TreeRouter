using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.CSharp.RuntimeBinder;

namespace TreeRouter
{
	public class Route
	{
		
		private static Regex plainVar = new Regex(@"\s*{\s*(\w+)\s*}\s*");
		private static Regex optionalVar = new Regex(@"\s*{\s*(\w+)\s*\?\s*}\s*");
		private static Regex greedyVar = new Regex(@"\s*{\s*(\w+)\s*\*\s*}\s*");
		
		public List<RouteToken> Tokens { get; }
		public int LiteralTokenCount { get; }
		public Dictionary<string, Regex> Constraints { get; set; }
		public Dictionary<string, string> Defaults { get; set; }
		public Action<Request> ActionHandler { get; set; }
		public Type ClassHandler { get; set; }
		public string[] Methods { get; }
		
		public Route(string path, string[] methods)
		{
			LiteralTokenCount = 0;
			Tokens = new List<RouteToken>();
			Methods = methods;
			var parts = path.Trim('/').Split('/');
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
						PathError("Cannot have required segments after optional ones", path);
					token.Matcher = new Regex($"^(?<{match.Groups[1].Value}>[^/]+)");
					Tokens.Add(token);
					continue;
				}

				match = optionalVar.Match(part);
				if (match.Success)
				{
					optional = true;
					token.Matcher = new Regex($"^(?<{match.Groups[1].Value}>[^/]+)");
					token.Optional = true;
					Tokens.Add(token);
					continue;
				}

				match = greedyVar.Match(part);
				if (match.Success)
				{
					if (i < parts.Length)
						PathError("Greedy expressions can only go at the end.", path);
					optional = true;
					token.Matcher = new Regex($"^(?<{match.Groups[1].Value}>.*)");
					token.Greedy = true;
					Tokens.Add(token);
					continue;
				}
				
				if (optional)
					PathError("Cannot have required segments after optional ones", path);
				LiteralTokenCount++;
				token.Matcher = part;
				Tokens.Add(token);
			}
			
		}

		private void PathError(string msg, string path) =>
			throw new ArgumentException(msg + "Route: " + path);
		
	}

	public class Route<TController> : Route 
	{
		public Route(string path, string[] methods) : base(path, methods)
		{
			ClassHandler = typeof(TController);
		}
	}
	
}
