using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TreeRouter.Http;
using TreeRouter.WebSocket;

namespace WebAndSocketTester
{
	public class Startup
	{
		public void ConfigureServices(IServiceCollection services)
		{
			services
				.AddTreeMap()
				.AddWebSockets();
		}

		public void Configure(IApplicationBuilder app)
		{
			app
				.MapWebSockets("/ws", new[] {"rest.json"}, r =>
				{
					r.Get<EchoController>("/{echo}").Action("Perform");
				})
				.TreeMap(r =>
				{
					r.Get("/").Action(async req =>
					{
						var context = (HttpContext) req.Context;
						context.Response.StatusCode = 200;
						await context.Response.WriteAsync("testing");
					});
				});
		}
		
	}
}