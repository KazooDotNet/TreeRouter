using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace TreeRouter.WebSocket
{
	public class Client : IDisposable
    {
        protected readonly ClientWebSocket Socket;
        readonly string _uri;
        protected CancellationTokenSource TokenSource;

        public bool Open { get; private set; }
        public event EventHandler MessageReceived;
        protected ConcurrentDictionary<string, MessageCallback?> Listeners { get; }

        public Client(string uri, string[] subprotocols = null)
        {
            Socket = new ClientWebSocket();
            if (subprotocols != null)
                foreach (var sp in subprotocols)
                    Socket.Options.AddSubProtocol(sp);
            _uri = uri;
            Listeners = new ConcurrentDictionary<string, MessageCallback?>();
        }

        public void Start() => StartAsync().GetAwaiter().GetResult();

        public async Task StartAsync()
        {
            if (Open) return;
            TokenSource = new CancellationTokenSource();
            var token = TokenSource.Token;
            await Socket.ConnectAsync(new Uri(_uri), token);
            Open = true;
			ReceiveAsync(token);
            Sweep(token);
        }

        public void Stop(bool graceful = true) => StopAsync(graceful).GetAwaiter().GetResult();

        public async Task StopAsync(bool graceful = true)
        {
            if (!Open) return;
            Open = false;
            var token = CancellationToken.None;
            const WebSocketCloseStatus closure = WebSocketCloseStatus.NormalClosure;
            if (graceful)
            {
                await Socket.CloseOutputAsync(closure, "", token).ConfigureAwait(false);
                TokenSource.Cancel();
            }
            else
            {
                await Socket.CloseAsync(closure, "", token).ConfigureAwait(false);
                TokenSource.Cancel();
            }
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
                Expires = DateTime.Now.Add(timeout ?? TimeSpan.FromMinutes(1)),
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
                    if (pair.Value.Value.Expires < DateTime.Now)
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
        
        

        private async void ReceiveAsync(CancellationToken token)
        {
            var buffer = new byte[1024 * 4];
            var done = false;
            while(!done)
            {
                if (token.IsCancellationRequested) break;
                var result = await Socket.ReceiveAsync(new ArraySegment<byte>(buffer),
                                                       token).ConfigureAwait(false);
                MessageResponse message = null;
                // TODO: make serializers more modular
                switch(result.MessageType)
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
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (message == null) continue;
                MessageReceived?.Invoke(this, new MessageEventArgs { Message = message });
                if (message.Id == null) continue;
                Listeners.TryGetValue(message.Id, out var listener);
                if (listener == null)  continue;
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