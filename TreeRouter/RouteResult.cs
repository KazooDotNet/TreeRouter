using System.Collections.Generic;

namespace TreeRouter
{
	public class RouteResult
	{
		public bool Found { get; set; }
		public Route Route { get; set; }
		public RequestDictionary Vars { get; set; }
	}
}
