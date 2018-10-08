using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TreeRouter.Http
{
	public class Middleware
	{

		private readonly IRouter _router;
		private readonly RequestDelegate _next;
		private IServiceScopeFactory _scopeFactory;

		public Middleware(RequestDelegate next, IRouter router, IServiceScopeFactory scopeFactory)
		{
			_router = router;
			_next = next;
			_scopeFactory = scopeFactory;
		}

		public async Task Invoke(HttpContext context)
		{
			try
			{
				using (var scope = _scopeFactory.CreateScope())
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
