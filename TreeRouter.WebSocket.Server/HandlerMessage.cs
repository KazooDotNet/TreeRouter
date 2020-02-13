using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;

namespace TreeRouter.WebSocket
{
	public class HandlerMessage
	{
		public WebSocker Socket { get; set; }
		public WebSocketReceiveResult SocketResult { get; set; }
		public string Serialized { get; set; }
		public string Subprotocol { get; set; }
		public string BasePath { get; set; }
		public RequestDictionary RouteVars { get; set; }
		public MessageRequest Request { get; set; }
		public IHandler Handler { get; set; }
		public HttpContext HttpContext { get; set; }
	}
}
