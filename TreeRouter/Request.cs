using Microsoft.AspNetCore.Http;

namespace TreeRouter
{
	public class Request 
	{
		public HttpContext Context { get; set; }
		public RequestDictionary RouteVars { get; set; }
	}
}
