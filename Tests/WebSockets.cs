using System;
using System.Threading;
using System.Threading.Tasks;
using TreeRouter.WebSocket;
using Xunit;

namespace Tests
{
	public class WebSockets
	{

		[Fact]
		public async Task EchoTest()
		{
			var client = new Client("ws://127.0.0.1:5050/ws", subprotocols: new[] {"rest.json"});
			string response = null;
			var tokenSource = new CancellationTokenSource();
			var message = new MessageRequest { Method = "get", Path = "/test" };
			client.MessageReceived += delegate(object sender, EventArgs args)
			{
				var mArgs = (MessageEventArgs) args;
				if (message.Id != mArgs.Message.Id) return;
				response = mArgs.Message.Data["Echo"];
				tokenSource.Cancel();
			};
			client.Start();
			await client.SendAsync(message);
			await Task.Delay(30000, tokenSource.Token);
			Assert.Equal("test", response);
		}
		
	}
}