using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TreeRouter.Http
{
	public class Middleware
	{	
		private readonly RequestDelegate _next;
		
		public Middleware(RequestDelegate next)
		{	
			_next = next;
		}

		public async Task Invoke(HttpContext context, IRouter router, IServiceScopeFactory scopeFactory)
		{
			try
			{
				using (var scope = scopeFactory.CreateScope())
				{
					await router.Dispatch(context, scope);	
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
