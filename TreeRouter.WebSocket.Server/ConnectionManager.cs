using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace TreeRouter.WebSocket
{
	public class ConnectionManager
	{
		private readonly ConcurrentDictionary<string, WebSocker> _sockets = new ConcurrentDictionary<string, WebSocker>();

		public WebSocker GetSocketById(string id)
		{
			return _sockets[id];
		}

		public ConcurrentDictionary<string, WebSocker> GetAll()
		{
			return _sockets;
		}

		public void AddSocket(WebSocker socket)
		{
			_sockets.TryAdd(socket.Id, socket);
		}

		public Task RemoveSocket(string id)
		{
			_sockets.TryRemove(id, out var socket);
			return socket?.CloseAsync(closeStatus: WebSocketCloseStatus.NormalClosure,
				       statusDescription: "Closed by the socket manager",
				       cancellationToken: CancellationToken.None) ?? Task.CompletedTask;
		}
	}
}
