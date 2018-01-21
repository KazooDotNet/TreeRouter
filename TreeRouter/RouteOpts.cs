using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TreeRouter
{
	public class RouteOpts
	{
		public string Method { get; set; }
		public string[] Methods { get; set; }
		public string Path { get; set; }
		public Dictionary<string, Regex> Constraints { get; set; }
		public Dictionary<string, string> Defaults { get; set; }
		public Action<Request> Action { get; set; }
	}
}
