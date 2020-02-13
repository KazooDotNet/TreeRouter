using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace TreeRouter.WebSocket
{
	public class Client : IDisposable
	{
		protected ClientWebSocket Socket;
		protected string[] Subprotocols;
		readonly string _uri;
		protected CancellationTokenSource TokenSource;
		protected readonly IClock Clock;

		public bool Open { get; private set; }
		public event EventHandler MessageReceived;
		protected ConcurrentDictionary<string, MessageCallback?> Listeners { get; }
		protected ConcurrentDictionary<string, List<Func<MessageResponse, bool?>>> Watchers { get; }

		public Client(string uri, string[] subprotocols = null, IClock clock = null)
		{
			Clock = clock ?? new RealClock();
			Subprotocols = subprotocols;
			_uri = uri;
			Listeners = new ConcurrentDictionary<string, MessageCallback?>();
			Watchers = new ConcurrentDictionary<string, List<Func<MessageResponse, bool?>>>();
		}

		public void Start(Action<ClientWebSocket> callback = null) => StartAsync(callback).GetAwaiter().GetResult();

		public async Task StartAsync(Action<ClientWebSocket> callback = null)
		{
			if (Open) return;
			Socket = new ClientWebSocket();
			if (Subprotocols != null)
				foreach (var sp in Subprotocols)
					Socket.Options.AddSubProtocol(sp);
			if (callback != null)
				callback.Invoke(Socket);
			TokenSource = new CancellationTokenSource();
			var token = TokenSource.Token;
			await Socket.ConnectAsync(new Uri(_uri), token);
			Open = Socket.State == WebSocketState.Open;
			if (Open)
			{
				ReceiveAsync(token);
				Sweep(token);
			}
		}

		public void Stop(bool graceful = true) => StopAsync(graceful).GetAwaiter().GetResult();

		public async Task StopAsync(bool graceful = true)
		{
			if (!Open) return;
			Open = false;
			var token = CancellationToken.None;
			const WebSocketCloseStatus closure = WebSocketCloseStatus.NormalClosure;
			if (graceful)
				await Socket.CloseOutputAsync(closure, "", token).ConfigureAwait(false);
			else
				await Socket.CloseAsync(closure, "", token).ConfigureAwait(false);
			TokenSource.Cancel();
		}

		public async Task SendAsync(MessageRequest msg)
		{
			if (msg.Id == null) msg.Id = Guid.NewGuid().ToString();
			var encoded = JsonConvert.SerializeObject(msg);
			var bytes = Encoding.UTF8.GetBytes(encoded);
			await Socket.SendAsync(new ArraySegment<byte>(bytes),
				WebSocketMessageType.Text, true,
				CancellationToken.None).ConfigureAwait(false);
		}

		public async Task SendAsync(MessageRequest msg, Func<MessageResponse, bool?> callback, TimeSpan? timeout = null)
		{
			var callbackStruct = new MessageCallback
			{
				TaskCompleter = new TaskCompletionSource<bool>(),
				Expires = Clock.Now.Add(timeout ?? TimeSpan.FromMinutes(1)),
				Callback = callback
			};
			Listeners.TryAdd(msg.Id, callbackStruct);
			await SendAsync(msg);
			await callbackStruct.TaskCompleter.Task;
		}

		private async void Sweep(CancellationToken token)
		{
			while (true)
			{
				foreach (var pair in Listeners)
					if (pair.Value != null && pair.Value.Value.Expires < Clock.Now)
					{
						Listeners.TryRemove(pair.Key, out var listener);
						listener?.Callback(new MessageResponse {Timeout = true});
						listener?.TaskCompleter.SetResult(true);
					}

				if (token.IsCancellationRequested)
					break;
				await Task.Delay(10);
			}
		}

		public void WatchFor(string path, Func<MessageResponse, bool?> callback)
		{
			if (!Watchers.ContainsKey(path))
				Watchers.TryAdd(path, new List<Func<MessageResponse, bool?>>());
			lock (Watchers[path])
			{
				Watchers[path].Add(callback);
			}
		}


		private async void ReceiveAsync(CancellationToken token)
		{
			var buffer = new byte[1024 * 4];
			var done = false;
			while (!done)
			{
				WebSocketReceiveResult result = null;
				try
				{
					result = await Socket.ReceiveAsync(new ArraySegment<byte>(buffer), token).ConfigureAwait(false);
				}
				catch (WebSocketException)
				{
					await StopAsync(graceful: false).ConfigureAwait(false);
					break;
				}
				catch (InvalidOperationException)
				{
					break;
				}
				catch (TaskCanceledException)
				{
					break;
				}
				catch (OperationCanceledException)
				{
					break;
				}

				MessageResponse message = null;
				// TODO: make serializers more modular
				switch (result?.MessageType)
				{
					case WebSocketMessageType.Text:
						var str = Encoding.UTF8.GetString(buffer, 0, result.Count);
						message = JsonConvert.DeserializeObject<MessageResponse>(str);
						break;
					case WebSocketMessageType.Close:
						await StopAsync(graceful: false).ConfigureAwait(false);
						done = true;
						break;
					case WebSocketMessageType.Binary:
						// TODO implement binary protocols
						break;
					case null:
						// do nothing;
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				if (message == null) continue;

				MessageReceived?.Invoke(this, new MessageEventArgs {Message = message});

				if (message.Path != null && Watchers.ContainsKey(message.Path))
					lock (Watchers[message.Path])
					{
						var removeThese = new List<int>();
						var i = 0;
						foreach (var w in Watchers[message.Path])
						{
							var wKeep = w.Invoke(message);
							if (wKeep != true)
								removeThese.Add(i);
							i++;
						}

						removeThese
							.Reverse(); // IMPORTANT: make sure you reverse so that the larger numbers get removed first, prevents wrong indexes later on.
						foreach (var r in removeThese)
							Watchers[message.Path].RemoveAt(r);
					}

				if (message.Id == null) continue;
				Listeners.TryGetValue(message.Id, out var listener);
				if (listener == null) continue;
				var l = listener.Value;
				var keep = l.Callback(message);
				if (keep != true)
					Listeners.TryRemove(message.Id, out var _);
				l.TaskCompleter.SetResult(true);
			}
		}

		public void Dispose() => Stop(graceful: false);
	}
}
