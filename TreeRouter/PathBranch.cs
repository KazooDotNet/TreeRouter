using System.Collections.Generic;
using System.Linq;

namespace TreeRouter
{
	public class PathBranch
	{
		public List<Route> Routes { get; }
		public RouteToken Token { get; set; }
		private Dictionary<string, PathBranch> ChildrenDict { get; }
		private PathBranch[] _children;
		public IEnumerable<PathBranch> Children => _children ?? Build();

		public PathBranch()
		{
			ChildrenDict = new Dictionary<string, PathBranch>();
			Routes = new List<Route>();
		}

		public void AddChild(PathBranch pathBranch)
		{
			var key = GetKey(pathBranch.Token);
			if (ChildrenDict.ContainsKey(key)) return;
			ChildrenDict[key] = pathBranch;
		}

		public PathBranch FindOrAddChildByToken(RouteToken token)
		{
			var key = GetKey(token);
			var pathBranch = FindChild(key);
			if (pathBranch != null)
				return pathBranch;
			pathBranch = new PathBranch {Token = token};
			ChildrenDict[key] = pathBranch;
			return pathBranch;
		}

		public PathBranch FindChild(RouteToken token) =>
			FindChild(GetKey(token));


		public PathBranch FindChild(string key)
		{
			ChildrenDict.TryGetValue(key, out var child);
			return child;
		}

		private static string GetKey(RouteToken token) =>
			token.Text ?? Utils.ComputeHash(token.Hasher);


		public PathBranch[] Build() => _children = ChildrenDict.Select(pair => pair.Value).ToArray();
	}
}
