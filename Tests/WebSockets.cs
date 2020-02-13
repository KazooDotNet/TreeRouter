using System;
using System.Threading;
using System.Threading.Tasks;
using Tests.Classes;
using TreeRouter.WebSocket;
using Xunit;

namespace Tests
{
	public class WebSockets : IDisposable
	{
		private readonly ClientExposer _client;
		private readonly FakeClock _clock;

		public WebSockets()
		{
			_clock = new FakeClock();
			_client = new ClientExposer("ws://127.0.0.1:5050/ws", new[] {"rest.json"}, _clock);
			_client.Start();
		}

		[Fact]
		public async Task EchoTest()
		{
			string response = null;
			var ts = new CancellationTokenSource();
			var message = new MessageRequest {Method = "get", Path = "/test"};
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
			await _client.SendAsync(message, r =>
			{
				response = r.Data["Echo"];
				return false; // Do not keep this function around for further listening
			});
			Assert.Equal("blah", response);
		}

		[Fact]
		public async Task ResponseTimesOut()
		{
			var message = new MessageRequest {Method = "get", Path = "/blah"};
			var ignored = false;
			_clock.Freeze();
			_client.SendAsync(message, r =>
			{
				ignored = r.Timeout;
				return false;
			});
			await Delay(20);
			var listeners = _client.GetListeners();
			Assert.Equal(listeners.Count, 1);
			_clock.AddMinutes(2);
			await Delay(100);
			Assert.Equal(listeners.Count, 0);
			Assert.True(ignored);
		}

		[Fact]
		public async Task WatchesPath()
		{
			var gotWelcome = false;
			var tokenSource = new CancellationTokenSource();
			_client.WatchFor("/welcome", (resp) =>
			{
				gotWelcome = true;
				tokenSource.Cancel();
				return false;
			});
			await _client.SendAsync(new MessageRequest {Path = "/trigger-welcome", Method = "get"});
			await Delay(500, tokenSource.Token);
			Assert.Equal(true, gotWelcome);
		}


		private static Task Delay(int ms, CancellationToken token = default(CancellationToken)) =>
			Task.Delay(ms, token).ContinueWith(t => { });

		public void Dispose()
		{
			_client.Stop();
		}
	}
}
