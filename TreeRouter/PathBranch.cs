using System.Collections.Generic;
using System.Linq;

namespace TreeRouter
{
	public class PathBranch
	{
		public Route Route { get; set; }
		public RouteToken Token { get; set; }
		public string Method { get; set; }
		protected bool Added { get; set; }
		private Dictionary<string, List<PathBranch>> ChildrenDict { get; }
		private readonly List<PathBranch> _childrenList;
		private PathBranch[] _children;
		public PathBranch[] Children => _children ?? Build();
		
		public PathBranch()
		{
			_childrenList = new List<PathBranch>();
			ChildrenDict = new Dictionary<string, List<PathBranch>>();
		}

		public void AddChild(PathBranch pathBranch)
		{
			if (pathBranch.Added) 
				return;
			pathBranch.Added = true;
			_childrenList.Add(pathBranch);
			var key = GetKey(pathBranch.Token);
			if (!ChildrenDict.ContainsKey(key)) 
				ChildrenDict[key] = new List<PathBranch>();
			ChildrenDict[key].Add(pathBranch);
		}

		public PathBranch[] FindChildren(RouteToken token, string[] methods)
		{
			var key = GetKey(token);
			ChildrenDict.TryGetValue(key, out var children);
			var ret = new List<PathBranch>();
			if (children != null)
				ret.AddRange(children.Where( c => c.Method == null || methods.Contains(c.Method) ).ToList());
			return ret.ToArray();
		}

		private string GetKey(RouteToken token) =>
			token.Text ?? $"******{token.Name}******";
		

		public PathBranch[] Build() => _children = _childrenList.ToArray();

	}
}
