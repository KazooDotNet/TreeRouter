using System;
using System.Threading;
using System.Threading.Tasks;
using TreeRouter.WebSocket;
using Xunit;

namespace Tests
{
	public class WebSockets : IDisposable
	{
		private readonly Client _client;
		
		public WebSockets()
		{
			_client = new Client("ws://127.0.0.1:5050/ws", subprotocols: new[] {"rest.json"});
			_client.Start();
		}

		[Fact]
		public async Task EchoTest()
		{
			string response = null;
			var ts = new CancellationTokenSource();
			var message = new MessageRequest { Method = "get", Path = "/test" };
			_client.MessageReceived += delegate(object sender, EventArgs args)
			{
				var mArgs = (MessageEventArgs) args;
				if (message.Id != mArgs.Message.Id) return;
				response = mArgs.Message.Data["Echo"];
				ts.Cancel();
			};
			await _client.SendAsync(message);
			await Delay(1000, ts.Token);
			Assert.Equal("test", response);
		}

		[Fact]
		public async Task BuiltInResponse()
		{
			var message = new MessageRequest {Method = "get", Path = "/blah"};
			string response = null;
			await _client.SendAsync(message, r => { 
				response = r.Data["Echo"];
				return false; // Do not keep this function around for further listening
			});
			Assert.Equal("blah", response);
		}

		private static Task Delay(int ms, CancellationToken token) =>
			Task.Delay(ms, token).ContinueWith(t => { });

		public void Dispose()
		{
			_client.Stop();
		}
	}
}