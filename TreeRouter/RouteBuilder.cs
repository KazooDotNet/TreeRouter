using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TreeRouter
{
	public class RouteBuilder
	{
		
		private readonly RouteOpts _defaultOpts;
		
		public List<Route> Routes { get; }
		
		public RouteBuilder(string prefix)
		{	
			Routes = new List<Route>();
			_defaultOpts = new RouteOpts { Prefix = prefix };
		}

		public RouteBuilder(RouteOpts defaultOpts)
		{
			Routes = new List<Route>();
			_defaultOpts = defaultOpts;
		}
		
		public RouteBuilder(string prefix, RouteOpts defaultOpts)
		{
			Routes = new List<Route>();
			_defaultOpts = defaultOpts;
			_defaultOpts.Prefix = prefix;
		}

		public RouteBuilder Map<T>(Action<RouteBuilder> action) where T : IController =>
			Map(new RouteOpts { ClassHandler = typeof(T) }, action);
		
		
		public RouteBuilder Map<T>(RouteOpts defaultOpts, Action<RouteBuilder> action) where T : IController
		{
			defaultOpts.ClassHandler = typeof(T);
			return Map(defaultOpts, action);
		}

		public RouteBuilder Map(string prefix, Action<RouteBuilder> action) =>
			Map(new RouteOpts {Prefix = prefix}, action);

		public RouteBuilder Map<T>(string prefix, Action<RouteBuilder> action) where T : IController =>
			Map<T>(new RouteOpts {Prefix = prefix}, action);
		
		public RouteBuilder Resources<T>(string path) where T : IController
		{
			var fullPath = _defaultOpts.Prefix == null ? path : Utils.JoinPath(_defaultOpts.Prefix, path);
			return Map<T>(fullPath, rb =>
			{
				rb.Get("/", new RouteOpts {Defaults = new Defaults {{"action", "Index"}}});
				rb.Get("/{id}", new RouteOpts {Defaults = new Defaults {{"action", "Show"}}});
				rb.Get("/{id}/edit", new RouteOpts {Defaults = new Defaults {{"action", "Edit"}}});
				rb.Get("/new", new RouteOpts {Defaults = new Defaults {{"action", "New"}}});
				rb.Post("/", new RouteOpts {Defaults = new Defaults {{"action", "Create"}}});
				rb.Patch("/{id}", new RouteOpts {Defaults = new Defaults {{"action", "Update"}}});
				rb.Delete("/{id}", new RouteOpts {Defaults = new Defaults {{"action", "Delete"}}});
			});
		}
		

		public RouteBuilder Map(RouteOpts defaultOpts, Action<RouteBuilder> action)
		{
			var builder = new RouteBuilder(defaultOpts);
			action.Invoke(builder);
			Routes.AddRange(builder.Routes);
			return this;
		}
		
		public RouteBuilder Get(string path, RouteOpts routeOpts)
		{
			BuildRoute(CombineOpts(routeOpts, path, "get"));
			return this;
		}

		public RouteBuilder Get<T>(string path, RouteOpts routeOpts) where T : IController
		{
			BuildRoute<T>(CombineOpts(routeOpts, path, "get"));
			return this;
		}

		public RouteBuilder Post(string path, RouteOpts routeOpts)
		{
			BuildRoute(CombineOpts(routeOpts, path, "post"));
			return this;
		}
		
		public RouteBuilder Post<T>(string path, RouteOpts routeOpts) where T : IController
		{
			BuildRoute<T>(CombineOpts(routeOpts, path, "post"));
			return this;
		}
		
		public RouteBuilder Put(string path, RouteOpts routeOpts)
		{
			BuildRoute(CombineOpts(routeOpts, path, "put"));
			return this;
		}
		
		public RouteBuilder Put<T>(string path, RouteOpts routeOpts) where T : IController
		{
			BuildRoute<T>(CombineOpts(routeOpts, path, "put"));
			return this;
		}
		
		public RouteBuilder Patch(string path, RouteOpts routeOpts)
		{
			BuildRoute(CombineOpts(routeOpts, path, "patch"));
			return this;
		}
		
		public RouteBuilder Patch<T>(string path, RouteOpts routeOpts) where T : IController
		{
			BuildRoute<T>(CombineOpts(routeOpts, path, "patch"));
			return this;
		}
		
		public RouteBuilder Delete(string path, RouteOpts routeOpts)
		{
			BuildRoute(CombineOpts(routeOpts, path, "delete"));
			return this;
		}
		
		public RouteBuilder Delete<T>(string path, RouteOpts routeOpts) where T : IController
		{
			BuildRoute<T>(CombineOpts(routeOpts, path, "delete"));
			return this;
		}

		private RouteOpts CombineOpts(RouteOpts routeOpts, string path = null, string[] methods = null, 
			Func<Request, Task> action = null )
		{
			if (path != null) routeOpts.Path = path;
			if (methods != null) routeOpts.Methods = methods;
			if (action != null) routeOpts.Action = action;
			return routeOpts;
		}

		private RouteOpts CombineOpts(RouteOpts routeOpts, string path = null, string method = null,
			Func<Request, Task> action = null)
		{
			string[] methods = null;
			if (method != null)
				methods = new[] {method};
			return CombineOpts(routeOpts, path, methods, action);
		}

		private void BuildRoute(RouteOpts routeOpts)
		{
			routeOpts = SetDefaults(routeOpts);
			var path = routeOpts.Prefix == null ? routeOpts.Path : Utils.JoinPath(routeOpts.Prefix, routeOpts.Path);
			var route = new Route(path, routeOpts.Methods ?? new[] { routeOpts.Method })
			{
				Constraints = routeOpts.Constraints,
				Defaults = routeOpts.Defaults,
				ActionHandler = routeOpts.Action,
				ClassHandler = routeOpts.ClassHandler
			};
			Routes.Add(route);
		}

		private RouteOpts SetDefaults(RouteOpts routeOpts)
		{
			return new RouteOpts
			{
				Constraints = routeOpts.Constraints ?? _defaultOpts.Constraints,
				Defaults = routeOpts.Defaults ?? _defaultOpts.Defaults,
				Action = routeOpts.Action ?? _defaultOpts.Action,
				Prefix = routeOpts.Prefix ?? _defaultOpts.Prefix,
				ClassHandler = routeOpts.ClassHandler ?? _defaultOpts.ClassHandler,
				Path = routeOpts.Path ?? _defaultOpts.Path,
				Method = routeOpts.Method ?? _defaultOpts.Method,
				Methods = routeOpts.Methods ?? _defaultOpts.Methods
			};
		}

		private void BuildRoute<T>(RouteOpts routeOpts) where T : IController
		{
			routeOpts.ClassHandler = typeof(T);
			BuildRoute(routeOpts);
		}
		
	}
}
