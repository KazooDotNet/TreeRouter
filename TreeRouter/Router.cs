using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace TreeRouter
{
	public class Router
	{
		
		private List<Route> Routes { get; }
		private PathBranch Root { get; set; }
		
		public Router()
		{
			Routes = new List<Route>();
		}

		public void Compile()
		{
			var root = new PathBranch();
			// Define the branches
			foreach (var route in Routes)
			{
				PathBranch branch = root;
				for (var i = 0; i < route.Tokens.Count; i++)
				{
					var token = route.Tokens[i];
					var child = branch.FindChild(token.Matcher) ?? new PathBranch { Token = token };
					var next = i + 1;
					var nextOptional = next < route.Tokens.Count && (route.Tokens[next].Greedy || route.Tokens[next].Optional);
					if (child.Route == null && nextOptional)
						child.Route = route;
					branch.AddChild(child);
					branch = child;
				}
				if (branch.Route == null)
					branch.Route = route;
			}
			BuildBranches(root);
			Root = root;
		}

		private void BuildBranches(PathBranch branch)
		{
			branch.Build();
			foreach (var child in branch.Children)
				BuildBranches(child);
		}

		public void Map(string prefix, Action<RouteBuilder> action)
		{
			var builder = new RouteBuilder(prefix);
			action.Invoke(builder);
			Routes.AddRange(builder.Routes);
			Compile();
		}

		public void Map(Action<RouteBuilder> action) => Map(null, action);

		public RouteResult MatchPath(string path, string method)
		{
			var tokens = path.Trim('/').Split('/').ToList();
			var matchedResults = BranchSearch(Root, method, tokens.ToList());
			RouteResult result = null;
			switch (matchedResults.Count)
			{
					case 0:
						return new RouteResult { Found = false };
					case 1:
						result = matchedResults[0];
						break;
					default:
						foreach (var matchedResult in matchedResults)
						{
							if (result == null) { result = matchedResult; continue; }
							var ctc = result.Route.LiteralTokenCount;
							var ntc = matchedResult.Route.LiteralTokenCount;
							if (ctc < ntc || (ctc == ntc && result.Vars.Count < matchedResult.Vars.Count))
								result = matchedResult;
						}
						break;
			}

			if (result != null)
			{
				result.Found = true;
				var vars = new Dictionary<string, string>(result.Route?.Defaults ?? new Dictionary<string, string>()) ;
				foreach (var pair in result.Vars)
					vars[pair.Key] = pair.Value;
				result.Vars = vars;
			}
			return result ?? new RouteResult { Found = false };
		}

		private List<RouteResult> BranchSearch(PathBranch branch, string method, List<string> pathTokens)
		{
			var list = new List<RouteResult>();
			var matchedTokens = new Dictionary<string, string>();
			return BranchSearch(branch, method, pathTokens, matchedTokens, list);
		}

		private List<RouteResult> BranchSearch(PathBranch branch, string method, List<string> pathTokens, 
			Dictionary<string, string> matchedTokens, List<RouteResult> matchedResults)
		{
			if (pathTokens.Count == 0)
				return matchedResults;
			var token = pathTokens[0];
			pathTokens.RemoveAt(0);
			foreach (var child in branch.Children)
			{
				if (child.Token.Matcher is string str)
				{
					if (str != token)
						continue;
					if (child.Route != null && child.Route.Methods.Contains(method))
						matchedResults.Add(new RouteResult { Route = child.Route, Vars = matchedTokens });
					BranchSearch(child, method, pathTokens, matchedTokens, matchedResults);
				}
				else if (child.Token.Matcher is Regex regex)
				{
					var match = regex.Match(token);
					if (match.Success)
					{
						if (child.Route != null && child.Route.Methods.Contains(method))
						{
							var matchName = regex.GetGroupNames()[1];
							if (child.Token.Greedy)
							{
								pathTokens.Insert(0, token);
								matchedTokens[matchName] = String.Join('/', pathTokens);
							}
							else
								matchedTokens[matchName] = token;
							matchedResults.Add(new RouteResult { Route = branch.Route, Vars = matchedTokens });
						}
						if (!child.Token.Greedy)	
							BranchSearch(child, method, pathTokens, matchedTokens, matchedResults);
					}
				}	
			}
			return matchedResults;
		}
	}
	
}
