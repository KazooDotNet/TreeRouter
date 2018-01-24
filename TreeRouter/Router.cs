using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace TreeRouter
{

	public interface IRouter
	{
		void Compile();
		void Map(string prefix, Action<RouteBuilder> action);
		void Map(Action<RouteBuilder> action);
		Task Dispatch(HttpContext context);
	}
	
	public class Router : IRouter
	{
		
		private List<Route> Routes { get; }
		private PathBranch Root { get; set; }
		private readonly IServiceProvider _container;
		
		public Router(IServiceProvider container)
		{
			_container = container ?? new ServiceContainer();
			Routes = new List<Route>();
		}

		public void Compile()
		{
			var root = new PathBranch();
			foreach (var route in Routes)
				BranchSorter(route, root);
			BuildBranches(root);
			Root = root;
		}

		public void BranchSorter(Route route, PathBranch branch, int i = 0)
		{
			if (i >= route.Tokens.Count) 
				return;
			var token = route.Tokens[i];
			IEnumerable<PathBranch> children = null;	
			var next = i + 1;
			if (next < route.Tokens.Count - 1 && (route.Tokens[next].Greedy || route.Tokens[next].Optional))
				children = AssignRouteToChildren(route, token, branch);
			else if (i == route.Tokens.Count - 1) // End of the line, stop looping
				AssignRouteToChildren(route, token, branch);
			else // Not in assignment mode, just branch building
			{
				children =  branch.FindChildren(token, route.Methods);
				if (!children.Any())
				{
					var newChild = new PathBranch { Token = token };
					branch.AddChild(newChild); 
					children = new[] { newChild };
				}
			}
			if (children != null)
				foreach (var child in children)
					BranchSorter(route, child, i + 1);
		}

		private List<PathBranch> AssignRouteToChildren(Route route, RouteToken token, PathBranch parent)
		{
			var children = parent.FindChildren(token, route.Methods);
			var matchedChildren = new List<PathBranch>();
			foreach (var method in route.Methods)
			{
				var found = false;
				foreach (var child in children)
					if (child.Route == null && (child.Method == null || child.Method == method))
					{
						found = true;
						child.Route = route;
						child.Method = method;
					}
				if (found) continue;
				var newChild = new PathBranch {Route = route, Method = method, Token = token};
				matchedChildren.Add(newChild);
				parent.AddChild(newChild);
			}
			return matchedChildren;
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
			var tokens = path.Trim('/').Split('/');
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
		
		public async Task Dispatch(HttpContext context)
		{
			var req = context.Request;
			string path = req.PathBase == null ? 
				req.Path.ToString() : req.PathBase.ToString().TrimEnd('/') + '/' + req.Path.ToString().TrimStart('/');
			var result = MatchPath(path, req.Method.ToLower());
			if (!result.Found)
				throw new Errors.RouteNotFound("No route was found that matches the requested path")
				{
					Path = path,
					Method = req.Method
				};
			var request = new Request { Context = context, RouteVars = result.Vars };
			
			if (result.Route.ActionHandler != null)
			{
				await result.Route.ActionHandler.Invoke(request);
				return;
			}

			if (result.Route.ClassHandler != null)
			{
				var controller = (IController)_container.GetService(result.Route.ClassHandler);
				if (controller == null)
					throw new Errors.UnregisteredController("Service container does not have controller registered")
						{ ControllerType = result.Route.ClassHandler };
				await controller.Route(request);
				return;
			}
			
			// TODO: move this to the compile step?
			throw new Exception("No handlers are defined");
			
		}

		private List<RouteResult> BranchSearch(PathBranch branch, string method, string[] pathTokens)
		{
			var list = new List<RouteResult>();
			var matchedTokens = new Dictionary<string, string>();
			return BranchSearch(branch, method, pathTokens, 0, matchedTokens, list);
		}

		private List<RouteResult> BranchSearch(PathBranch branch, string method, string[] pathTokens, 
			int tokenIndex, Dictionary<string, string> matchedTokens, List<RouteResult> matchedResults)
		{
			if (tokenIndex > pathTokens.Length - 1)
				return matchedResults;
			var token = pathTokens[tokenIndex];
			foreach (var child in branch.Children)
			{
				if (child.Token.Text != null)
				{
					if (child.Token.Text != token)
						continue;
					if (child.Route != null && child.Route.Methods.Contains(method))
						matchedResults.Add(new RouteResult { Route = child.Route, Vars = matchedTokens });
					BranchSearch(child, method, pathTokens, tokenIndex + 1, matchedTokens, matchedResults);
				}
				else if (child.Token.MatchAny || child.Token.Matcher != null)
				{
					if (child.Token.Matcher != null)
					{
						var match = child.Token.Matcher.Match(token);
						if (!match.Success) continue;	
					}
					
					if (child.Route != null && child.Route.Methods.Contains(method))
					{
						var name = child.Token.Name;
						if (child.Token.Greedy)
							matchedTokens[name] = String.Join('/', pathTokens.Skip(tokenIndex));
						else
							matchedTokens[name] = token;
						matchedResults.Add(new RouteResult { Route = child.Route, Vars = matchedTokens });
					}
					if (!child.Token.Greedy)	
						BranchSearch(child, method, pathTokens, tokenIndex + 1, matchedTokens, matchedResults);
				}	
			}
			return matchedResults;
		}
	}
	
}
