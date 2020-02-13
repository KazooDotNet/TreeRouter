using System;
using System.Threading.Tasks;

namespace TreeRouter
{
	public class IntermediateBuilder : BaseBuilder
	{
		protected IntermediateBuilder(RouteOptions options) : base(options)
		{
		}

		public IntermediateBuilder Path(string path)
		{
			var options = Utils.MergeOptions(RouteOptions);
			options.Path = Utils.JoinPath(options.Path, path);
			var bb = new IntermediateBuilder(options);
			Children.Add(bb);
			return bb;
		}

		public IntermediateBuilder Action(Func<Request, Task> action)
		{
			RouteOptions.ActionHandler = action;
			return this;
		}

		public IntermediateBuilder Methods(params string[] methods)
		{
			RouteOptions.Methods = methods;
			return this;
		}

		public IntermediateBuilder Action(string action) =>
			Defaults(new Defaults {{"action", action}});


		public IntermediateBuilder Defaults(Defaults defaults)
		{
			RouteOptions.Defaults = Utils.MergeDefaults(RouteOptions.Defaults, defaults);
			return this;
		}

		public IntermediateBuilder Defaults(Action<Defaults> action)
		{
			var defaults = new Defaults();
			action.Invoke(defaults);
			return Defaults(defaults);
		}

		public IntermediateBuilder Constraints(Constraints constraints)
		{
			RouteOptions.Constraints = Utils.MergeConstraints(RouteOptions.Constraints, constraints);
			return this;
		}
	}
}
