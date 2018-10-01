using System;

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
			var opts = Utils.MergeOptions(RouteOptions, options);
			var rb = new RouteBuilder(opts) { RouteOptions = { Path = Utils.JoinPath(RouteOptions.Path, options.Path) } };
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
		public MethodBuilder Options(string path) => NewBuilder(path, "options");
		public MethodBuilder Options<T>(string path) where T : IController => NewBuilder(path, "options", typeof(T));
		
		protected MethodBuilder NewBuilder(string path, string method, Type handler = null)
		{
			var mb = new MethodBuilder(RouteOptions)
			{
				RouteOptions = { Path = Utils.JoinPath(RouteOptions.Path, path), Methods = new[] {method} }
			};
			mb.RouteOptions.ClassHandler = handler ?? RouteOptions.ClassHandler;
			Children.Add(mb);
			return mb;
		}

		public ResourcesBuilder<T> Resources<T>(string path) where T : IController
		{
			var rb = new ResourcesBuilder<T>(path, RouteOptions);
			Children.Add(rb);
			return rb;
		}

		public ResourceBuilder<T> Resource<T>(string path) where T : IController
		{
			var rb = new ResourceBuilder<T>(path, RouteOptions);
			Children.Add(rb);
			return rb;
		}

	}
	
}
