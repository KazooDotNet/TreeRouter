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

		public async Task Invoke(HttpContext context)
		{
			try
			{
				await _router.Dispatch(context);
			}
			catch (Errors.RouteNotFound e)
			{
				Console.WriteLine(e);
				_next?.Invoke(context);
			}
		}
	}
}
