using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using TreeRouter;

namespace Tests.Controllers
{
	public class ResourcesController : IController
	{
		public async Task Route(Request routerRequest)
		{
			var vars = routerRequest.RouteVars;
			var context = (HttpContext) routerRequest.Context;
			await context.Response.WriteAsync(vars["action"]);
		}
	}
}