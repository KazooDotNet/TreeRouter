using System;

namespace TreeRouter
{
	public class ResourceBuilder<T> : BaseBuilder where T : IController
	{
		private readonly RouteBuilder _rb;
		
		public ResourceBuilder(string prefix, RouteOptions options) 
		{
			RouteOptions = Utils.MergeOptions(options, new RouteOptions { ClassHandler = typeof(T) });
			RouteOptions.Path = Utils.JoinPath(options.Path, prefix);
			_rb = new RouteBuilder(RouteOptions);
			Children.Add(_rb);
			_rb.Get("/").Action("Show");
			_rb.Post("/").Action("Create");
			_rb.Get("/new").Action("New");
			_rb.Get("/edit").Action("Edit");
			_rb.Path("/").Methods("put", "patch").Action("Update");
			_rb.Delete("/").Action("Delete");
			_rb.Options("/").Action("Options");
			_rb.Options("/new").Action("Options");
			_rb.Options("/edit").Action("Options");
		}
		
		public ResourceBuilder<T> OnCollection(Action<RouteBuilder> map)
		{
			map.Invoke(_rb);
			return this;
		}
		
	}
}
