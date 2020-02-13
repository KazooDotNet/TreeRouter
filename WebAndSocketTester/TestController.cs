using System.Threading.Tasks;
using TreeRouter.WebSocket;

namespace WebAndSocketTester
{
	public class TestController : Controller
	{
		public Task Echo()
		{
			return ReplyMessage(new MessageResponse(Context.Request)
			{
				Data = new MessageData {{"Echo", RouteVars.Get<string>("echo")}}
			});
		}

		// Rude...
		public void Ignore()
		{
		}

		public Task TriggerWelcome()
		{
			return ReplyMessage(new MessageResponse {Path = "/welcome"});
		}
	}
}
