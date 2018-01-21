using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace TreeRouter
{
	public class Request 
	{
		public HttpContext Context { get; set; }
		public Dictionary<string, string> RouteVars { get; set; }
	}
}
