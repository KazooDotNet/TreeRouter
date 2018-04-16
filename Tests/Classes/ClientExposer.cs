using System.Collections.Concurrent;
using TreeRouter.WebSocket;

namespace Tests.Classes
{
	public class ClientExposer : Client
	{
		public ClientExposer(string uri, string[] subprotocols = null, IClock clock = null) : base(uri, subprotocols, clock)
		{
		}

		public ConcurrentDictionary<string, MessageCallback?> GetListeners() => Listeners;
	}
}