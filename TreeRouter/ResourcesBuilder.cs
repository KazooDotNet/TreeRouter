using System;
using Humanizer;

namespace TreeRouter
{

	public class ResourcesBuilder<T> : BaseBuilder where T : IController
	{
		private readonly RouteBuilder _rb;
		
		public ResourcesBuilder(string prefix, RouteOptions options) 
		{
			RouteOptions = Utils.MergeOptions(options, new RouteOptions { ClassHandler = typeof(T) });
			RouteOptions.Path = Utils.JoinPath(options.Path, prefix);
			_rb = new RouteBuilder(RouteOptions);
			Children.Add(_rb);
			_rb.Get("/").Action("Index");
			_rb.Get("/{id}").Action("Show");
			_rb.Post("/").Action("Create");
			_rb.Get("/new").Action("New");
			_rb.Get("/{id}/edit").Action("Edit");
			_rb.Path("/{id}").Methods("put", "patch").Action("Update");
			_rb.Delete("/{id}").Action("Delete");
			_rb.Options("/").Action("Options");
			_rb.Options("/{id}").Action("Options");
			_rb.Options("/new").Action("Options");
			_rb.Options("/{id}/edit").Action("Options");
		} 
		
		public ResourcesBuilder<T> OnMember(Action<RouteBuilder> map, string pathName = null)
		{
			var name = pathName ?? typeof(T).Name.Replace("Controller", "").Singularize().Underscore();
			var options = Utils.MergeOptions(RouteOptions);
			options.Path = Utils.JoinPath(options.Path, $"{{{name}_id}}");
			var rb = new RouteBuilder(options);
			map.Invoke(rb);
			Children.Add(rb);
			return this;
		}

		public ResourcesBuilder<T> OnCollection(Action<RouteBuilder> map)
		{
			map.Invoke(_rb);
			return this;
		}
	}
}
