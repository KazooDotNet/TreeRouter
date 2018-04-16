using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading.Tasks;

namespace TreeRouter
{

	public interface IRouter
	{
		void Compile();
		void Map(string prefix, Action<RouteBuilder> action);
		void Map(Action<RouteBuilder> action);
		Task Dispatch(string path, string method, object context);
	}
	
	public class Router : IRouter
	{
		private RouteBuilder RouteBuilder { get; set; }
		private PathBranch Root { get; set; }
		private readonly IServiceProvider _container;
		
		public Router(IServiceProvider container)
		{
			_container = container ?? new ServiceContainer();
		}

		public void Compile()
		{
			var root = new PathBranch();
			foreach (var rb in RouteBuilder.AllChildren)
			{
				var options = rb.Options;
				options.Defaults.TryGetValue("action", out var action);
				if (options.ClassHandler == null && options.ActionHandler == null || options.Methods == null)
					continue;
				BranchSorter(Route.FromOptions(options), root);
			}
			BuildBranches(root);
			Root = root;
		}

		public void BranchSorter(Route route, PathBranch root)
		{
			var i = 0;
			var branch = root;
			while (true)
			{
				branch = branch.FindOrAddChildByToken(route.Tokens[i]);
				if (i == route.Tokens.Count - 1)
				{
					branch.Routes.Add(route);
					return;
				}
				var next = i + 1;
				if ( next < route.Tokens.Count && ( route.Tokens[next].Optional || route.Tokens[next].Greedy ) )
					branch.Routes.Add(route);
				i = i + 1;
			}
		}

		private static void BuildBranches(PathBranch branch)
		{
			branch.Build();
			foreach (var child in branch.Children)
				BuildBranches(child);
		}

		public void Map(string prefix, Action<RouteBuilder> action)
		{
			RouteBuilder = new RouteBuilder(prefix);
			action.Invoke(RouteBuilder);
			Compile();
		}

		public void Map(Action<RouteBuilder> action) => Map(null, action);

		public RouteResult MatchPath(string path, string method)
		{
			var tokens = (path ?? "").Trim('/').Split('/');
			var matchedResults = BranchSearch(Root, method, tokens);
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
						if (ctc <= ntc || (ctc == ntc && result.Vars.Count < matchedResult.Vars.Count))
							result = matchedResult;
					}
					break;
			}

			if (result == null) 
				return new RouteResult { Found = false };
			
			result.Found = true;
			var vars = new RequestDictionary(result.Route?.Defaults ?? new Defaults());
			// TODO: fix the branch searcher so that it doesn't glom on extra vars
			foreach (var pair in result.Route.ExtractVars(path))
				vars[pair.Key] = pair.Value;
			result.Vars = vars;
			return result;
		}
		
		public Task Dispatch(string path, string method, object context)
		{
			if (string.IsNullOrWhiteSpace(method))
				return Task.FromException(new Errors.RouteNotFound("Method parameter is blank. No routes will match")
				{
					Path = path,
					Method = method
				});
			var result = MatchPath(path, method);
			if (!result.Found)
				return Task.FromException(new Errors.RouteNotFound("No route was found that matches the requested path")
				{
					Path = path,
					Method = method
				});
			var request = new Request { Context = context, RouteVars = result.Vars };
			
			if (result.Route.ActionHandler != null)
				return result.Route.ActionHandler.Invoke(request);


			if (result.Route.ClassHandler == null)
				return Task.FromException(new Errors.RouteNotFound("No handler on route. This is a bug, please report it")
				{
					Path = path,
					Method = method
				});

			if (!(_container.GetService(result.Route.ClassHandler) is IController controller))
				return Task.FromException(new Errors.UnregisteredController("Service container does not have controller registered")
					{ControllerType = result.Route.ClassHandler});
				
			
			return controller.Route(request);

		}

		private static List<RouteResult> BranchSearch(PathBranch branch, string method, IReadOnlyList<string> pathTokens)
		{
			var list = new List<RouteResult>();
			var matchedTokens = new RequestDictionary();
			return BranchSearch(branch, method, pathTokens, 0, matchedTokens, list);
		}
		
		private static List<RouteResult> BranchSearch(PathBranch branch, string method, IReadOnlyList<string> pathTokens, 
			int tokenIndex, RequestDictionary matchedTokens, List<RouteResult> matchedResults)
		{
			if (tokenIndex > pathTokens.Count - 1)
				return matchedResults;
			var token = pathTokens[tokenIndex];
			foreach (var child in branch.Children)
			{
				if (child.Token.Text != null)
				{
					if (child.Token.Text != token || child.Routes == null)
						continue;
					foreach (var route in child.Routes)
						if (route.Methods.Contains(method))
							matchedResults.Add(new RouteResult { Route = route, Vars = matchedTokens });
					BranchSearch(child, method, pathTokens, tokenIndex + 1, matchedTokens, matchedResults);
				}
				else if (child.Token.MatchAny || child.Token.Matcher != null)
				{	
					if (child.Token.Matcher != null)
					{
						var match = child.Token.Matcher.Match(token);
						if (!match.Success) continue;	
					}
					var name = child.Token.Name;
					if (child.Token.Greedy)
						matchedTokens[name] = string.Join("/", pathTokens.Skip(tokenIndex));
					else
						matchedTokens[name] = token;
					if (child.Routes != null)
						foreach( var route in child.Routes )
							if (route.Methods.Contains(method))
								matchedResults.Add(new RouteResult { Route = route, Vars = matchedTokens });
					if (!child.Token.Greedy)	
						BranchSearch(child, method, pathTokens, tokenIndex + 1, new RequestDictionary(matchedTokens), matchedResults);
				}	
			}
			return matchedResults;
		}
	}
	
}
