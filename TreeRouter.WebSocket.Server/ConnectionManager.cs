using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace TreeRouter.WebSocket
{
	public class ConnectionManager
	{
		private readonly ConcurrentDictionary<string, WebSocker>
			Sockets = new ConcurrentDictionary<string, WebSocker>();

		public WebSocker GetSocketById(string id)
		{
			return Sockets[id];
		}

		public ConcurrentDictionary<string, WebSocker> GetAll()
		{
			return Sockets;
		}

		public void AddSocket(WebSocker socket)
		{
			Sockets.TryAdd(socket.Id, socket);
		}

		public Task RemoveSocket(WebSocker socket) =>
			RemoveSocket(socket.Id);

		public bool SocketExists(string id) => Sockets.ContainsKey(id);

		public Task RemoveSocket(string id)
		{
			Sockets.TryRemove(id, out var socket);
			return socket?.CloseAsync(closeStatus: WebSocketCloseStatus.NormalClosure,
				       statusDescription: "Closed by the socket manager",
				       cancellationToken: CancellationToken.None) ?? Task.CompletedTask;
		}
	}
}
