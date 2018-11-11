using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TreeRouter.Http
{
	public class Middleware
	{	
		private readonly RequestDelegate _next;
		private IRouter _router;

		public Middleware(RequestDelegate next, IRouter router)
		{	
			_next = next;
			_router = router;
		}

		public async Task Invoke(HttpContext context, IServiceScopeFactory scopeFactory)
		{
			try
			{
				using (var scope = scopeFactory.CreateScope())
				{
					await _router.Dispatch(context, scope);	
				}
			}
			catch (Errors.RouteNotFound e)
			{
				Console.WriteLine(e);
				if (_next == null) throw;
				await _next.Invoke(context);
			}
		}
	}
}
