using System.Collections.Generic;

namespace TreeRouter
{
	public class BaseBuilder
	{

		public RouteOptions RouteOptions { get; protected set; }
		protected List<BaseBuilder> Children { get; }

		protected BaseBuilder()
		{
			RouteOptions = new RouteOptions();
			Children = new List<BaseBuilder>();
		}

		protected BaseBuilder(RouteOptions options)
		{
			RouteOptions = Utils.MergeOptions(options);
			Children = new List<BaseBuilder>();
		}

		public IEnumerable<BaseBuilder> AllChildren
		{
			get
			{
				var list = new List<BaseBuilder>();
				foreach (var child in Children)
				{
					list.Add(child);
					list.AddRange(child.AllChildren);
				}
				return list;
			}
		}

	}
	
	
}
