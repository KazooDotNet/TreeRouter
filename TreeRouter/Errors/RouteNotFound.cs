using System;

namespace TreeRouter.Errors
{
	public class RouteNotFound : Exception
	{
		public RouteNotFound(string msg) : base(msg) {}
		public string Path { get; set; }
		public string Method { get; set; }
	}
}