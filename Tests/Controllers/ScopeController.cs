using System;
using System.Threading.Tasks;
using TreeRouter;

namespace Tests.Controllers
{
	public class ScopeController : IController
	{
		private SimpleService _service;

		public ScopeController(SimpleService service) => _service = service;

		public Task Route(Request routerRequest)
		{
			_service.Value = "testing!";
			return Task.CompletedTask;
		}
	}
}
