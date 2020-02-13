using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using TreeRouter;

namespace Tests.Controllers
{
	public class EchoController : IController
	{
		public async Task Route(Request routerRequest)
		{
			var vars = routerRequest.RouteVars;
			string response = "";
			if (vars.ContainsKey("responseText"))
				response = vars["responseText"];
			var context = (HttpContext) routerRequest.Context;
			await context.Response.WriteAsync(response);
		}
	}
}
