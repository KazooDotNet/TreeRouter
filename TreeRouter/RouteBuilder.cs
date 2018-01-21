using System;
using System.Collections.Generic;
using System.Linq;

namespace TreeRouter
{
	public class RouteBuilder
	{
		private string _prefix;
		public List<Route> Routes { get; }
		
		public RouteBuilder(string prefix)
		{
			_prefix = prefix;
			Routes = new List<Route>();
		}
		
		public RouteBuilder Get(string path, RouteOpts routeOpts)
		{
			BuildRoute(CombineOpts(path, "get", routeOpts));
			return this;
		}

		public RouteBuilder Get<T>(string path, RouteOpts routeOpts) where T : IController
		{
			BuildRoute<T>(CombineOpts(path, "get", routeOpts));
			return this;
		}

		public RouteBuilder Post(string path, RouteOpts routeOpts)
		{
			BuildRoute(CombineOpts(path, "post", routeOpts));
			return this;
		}
		
		public RouteBuilder Post<T>(string path, RouteOpts routeOpts) where T : IController
		{
			BuildRoute<T>(CombineOpts(path, "post", routeOpts));
			return this;
		}
		
		public RouteBuilder Put(string path, RouteOpts routeOpts)
		{
			BuildRoute(CombineOpts(path, "put", routeOpts));
			return this;
		}
		
		public RouteBuilder Put<T>(string path, RouteOpts routeOpts) where T : IController
		{
			BuildRoute<T>(CombineOpts(path, "put", routeOpts));
			return this;
		}
		
		public RouteBuilder Patch(string path, RouteOpts routeOpts)
		{
			BuildRoute(CombineOpts(path, "patch", routeOpts));
			return this;
		}
		
		public RouteBuilder Patch<T>(string path, RouteOpts routeOpts) where T : IController
		{
			BuildRoute<T>(CombineOpts(path, "patch", routeOpts));
			return this;
		}
		
		public RouteBuilder Delete(string path, RouteOpts routeOpts)
		{
			BuildRoute(CombineOpts(path, "delete", routeOpts));
			return this;
		}
		
		public RouteBuilder Delete<T>(string path, RouteOpts routeOpts) where T : IController
		{
			BuildRoute<T>(CombineOpts(path, "delete", routeOpts));
			return this;
		}

		private RouteOpts CombineOpts(string path, string method, RouteOpts routeOpts)
		{
			routeOpts.Path = path;
			routeOpts.Method = method;
			return routeOpts;
		}

		private void BuildRoute(RouteOpts routeOpts, Type controller = null)
		{
			var path = _prefix == null ? routeOpts.Path : JoinPath(_prefix, routeOpts.Path);
			var route = new Route(path, routeOpts.Methods ?? new[] { routeOpts.Method })
			{
				Constraints = routeOpts.Constraints,
				Defaults = routeOpts.Defaults,
				ActionHandler = routeOpts.Action,
				ClassHandler = controller
			};
			Routes.Add(route);
		}

		private void BuildRoute<T>(RouteOpts routeOpts) where T : IController => BuildRoute(routeOpts, typeof(T));

		private string JoinPath(params string[] list)
		{
			var parts = list.Select(p => p.Trim('/').Trim()).ToArray();
			return string.Join("/", parts);
		}
	}
}
