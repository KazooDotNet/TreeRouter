using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace TreeRouter
{
	public class Middleware
	{

		private IRouter _router;
		private RequestDelegate _next;

		public Middleware(RequestDelegate next, IRouter router)
		{
			_router = router;
			_next = next;
		}

		public Task Invoke(HttpContext context)
		{
			try
			{
				return _router.Dispatch(context);
			}
			catch (Errors.RouteNotFound e)
			{
				Console.WriteLine(e);
				return _next?.Invoke(context) ?? Task.FromException(e);
			}
		}
	}
}
