using System.Collections.Generic;

namespace TreeRouter
{
	public class BaseBuilder
	{

		public RouteOptions Options { get; protected set; }
		protected List<BaseBuilder> Children { get; }

		protected BaseBuilder()
		{
			Options = new RouteOptions();
			Children = new List<BaseBuilder>();
		}

		protected BaseBuilder(RouteOptions options)
		{
			Options = Utils.MergeOptions(options);
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
