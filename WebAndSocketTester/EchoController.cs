using System.Threading.Tasks;
using TreeRouter.WebSocket;

namespace WebAndSocketTester
{
	public class EchoController : Controller
	{
		public Task Perform()
		{
			return ReplyMessage(new MessageResponse()
			{
				Data = new MessageData { {"Echo", RouteVars.Get<string>("echo") } }
			});
		}
	}
}