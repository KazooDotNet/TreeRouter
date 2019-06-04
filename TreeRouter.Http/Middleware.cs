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
				if (context.RequestServices == null)
				{
					using (var scope = scopeFactory.CreateScope())
					{
						context.RequestServices = scope.ServiceProvider;
						await _router.Dispatch(context);
					}	
				}
				else
				{
					await _router.Dispatch(context);
				}
			}
			catch (Errors.RouteNotFound)
			{
				if (_next == null) throw;
				await _next.Invoke(context);
			}
		}
	}
}
