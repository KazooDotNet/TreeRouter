using System;

namespace TreeRouter.WebSocket
{
	public class MessageEventArgs : EventArgs
	{
		public MessageResponse Message { get; set; }
	}
}
