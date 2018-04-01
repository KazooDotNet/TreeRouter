using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;

namespace TreeRouter
{
	public class RouteBuilder : IntermediateBuilder
	{

		public RouteBuilder(RouteOptions options) : base(options)
		{	
		}

		public RouteBuilder(string prefix) : this(new RouteOptions {Path = prefix})
		{
		}

		public RouteBuilder Map(RouteOptions options, Action<RouteBuilder> map)
		{
			var opts = Utils.MergeOptions(Options, options);
			var rb = new RouteBuilder(opts) { Options = {Path = Utils.JoinPath(Options.Path, options.Path)} };
			Children.Add(rb);
			map.Invoke(rb);
			return rb;
		}
		
		public RouteBuilder Map(string path, Action<RouteBuilder> map, RouteOptions options = null)
		{
			if (options == null) options = new RouteOptions();
			options.Path = path;
			return Map(options, map);
		}

		public RouteBuilder Map<T>(string path, Action<RouteBuilder> map, RouteOptions options = null) where T : IController
		{
			if (options == null) options = new RouteOptions();
			options.ClassHandler = typeof(T);
			return Map(path, map, options);
		}

		public RouteBuilder Map(Action<RouteBuilder> map) => Map(null, map);

		public RouteBuilder Map<T>(Action<RouteBuilder> map) where T : IController =>
			Map(new RouteOptions { ClassHandler = typeof(T) }, map);
		
		
		public MethodBuilder Get(string path) => NewBuilder(path, "get");
		public MethodBuilder Get<T>(string path) where T : IController => NewBuilder(path, "get", typeof(T));
		public MethodBuilder Post(string path) => NewBuilder(path, "post");
		public MethodBuilder Post<T>(string path) where T : IController => NewBuilder(path, "post", typeof(T));
		public MethodBuilder Put(string path) => NewBuilder(path, "put");
		public MethodBuilder Put<T>(string path) where T : IController => NewBuilder(path, "put", typeof(T));
		public MethodBuilder Patch(string path) => NewBuilder(path, "patch");
		public MethodBuilder Patch<T>(string path) where T : IController => NewBuilder(path, "patch", typeof(T));
		public MethodBuilder Delete(string path) => NewBuilder(path, "delete");
		public MethodBuilder Delete<T>(string path) where T : IController => NewBuilder(path, "delete", typeof(T));
		
		protected MethodBuilder NewBuilder(string path, string method, Type handler = null)
		{
			var mb = new MethodBuilder(Options)
			{
				Options = { Path = Utils.JoinPath(Options.Path, path), Methods = new[] {method} }
			};
			if (handler != null)
				mb.Options.ClassHandler = handler;
			Children.Add(mb);
			return mb;
		}

		public ResourcesBuilder<T> Resources<T>(string path) where T : IController
		{
			var rb = new ResourcesBuilder<T>(path, Options);
			Children.Add(rb);
			return rb;
		}

		public ResourceBuilder<T> Resource<T>(string path) where T : IController
		{
			var rb = new ResourceBuilder<T>(path, Options);
			Children.Add(rb);
			return rb;
		}

	}
	
}
