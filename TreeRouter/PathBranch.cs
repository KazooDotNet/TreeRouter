using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.CSharp.RuntimeBinder;

namespace TreeRouter
{
	public class PathBranch
	{
		public Route Route { get; set; }
		public RouteToken Token { get; set; }
		private Dictionary<string, PathBranch> ChildrenDict { get; }
		private readonly List<PathBranch> _childrenList;
		private PathBranch[] _children;
		public PathBranch[] Children => _children ?? Build();

		public PathBranch()
		{
			_childrenList = new List<PathBranch>();
			ChildrenDict = new Dictionary<string, PathBranch>();
		}

		public void AddChild(PathBranch pathBranch)
		{
			var key = GetKey(pathBranch.Token.Matcher);
			_childrenList.Add(pathBranch);
			ChildrenDict[key] = pathBranch;
		}

		public PathBranch FindChild(dynamic matcher)
		{
			var key = GetKey(matcher);
			return ChildrenDict.ContainsKey(key) ? ChildrenDict[key] : null;
		}

		public PathBranch[] Build() => _children = _childrenList.ToArray();

		private string GetKey(dynamic matcher)
		{
			if (matcher is Regex regex)
				return "****REGEX****" + regex;
			return matcher as string;
		}

	}
}
