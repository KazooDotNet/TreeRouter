using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using TreeRouter;

namespace Tests
{
	public class EchoController : IController
	{
		public async Task Route(Request routerRequest)
		{
			var vars = routerRequest.RouteVars;
			string response = "";
			if (vars.ContainsKey("responseText"))
				response = vars["responseText"];
			await routerRequest.Context.Response.WriteAsync(response);
		}
	}
}