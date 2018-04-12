using System;
using System.Threading.Tasks;

namespace TreeRouter.WebSocket
{
	public struct MessageCallback
	{
		public TaskCompletionSource<bool> TaskCompleter { get; set; }
		public DateTime Expires { get; set; }
		public Func<MessageResponse, bool?> Callback { get; set; }
	}
}