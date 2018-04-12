using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TreeRouter.WebSocket
{
    public class Client : IDisposable
    {
        private readonly ClientWebSocket _socket;
        readonly string _uri;
        private CancellationTokenSource _tokenSource;

        public bool Open { get; set; }
        public event EventHandler MessageReceived;

        public Client(string uri, string[] subprotocols = null)
        {
            _socket = new ClientWebSocket();
            if (subprotocols != null)
                foreach (var sp in subprotocols)
                    _socket.Options.AddSubProtocol(sp);
            _uri = uri;
        }

        public void Start() => StartAsync().GetAwaiter().GetResult();

        public async Task StartAsync()
        {
            if (Open) return;
            _tokenSource = new CancellationTokenSource();
            var token = _tokenSource.Token;
            await _socket.ConnectAsync(new Uri(_uri), token);
            Open = true;
			ReceiveAsync(token);    
        }

        public void Stop(bool graceful = true) => StopAsync(graceful).GetAwaiter().GetResult();

        public async Task StopAsync(bool graceful = true)
        {
            if (!Open) return;
            Open = false;
            _tokenSource.Cancel();
            var token = CancellationToken.None;
            const WebSocketCloseStatus closure = WebSocketCloseStatus.NormalClosure;
            if (graceful)
                await _socket.CloseAsync(closure, "", token).ConfigureAwait(false);
            else
                await _socket.CloseOutputAsync(closure, "", token).ConfigureAwait(false);
        }

        public async Task SendAsync(MessageRequest msg)
        {
            if (msg.Id == null) msg.Id = Guid.NewGuid().ToString();
            var encoded = JsonConvert.SerializeObject(msg);
            var bytes = Encoding.UTF8.GetBytes(encoded);
            await _socket.SendAsync(new ArraySegment<byte>(bytes),
                                   WebSocketMessageType.Text, true,
                                   CancellationToken.None).ConfigureAwait(false);
        }

        private async void ReceiveAsync(CancellationToken token)
        {
            var buffer = new byte[1024 * 4];
            var done = false;
            MessageResponse message;
            while(!done)
            {
                if (token.IsCancellationRequested) break;
                var result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer),
                                                       token).ConfigureAwait(false);
                message = null;
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
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (message != null)
                    MessageReceived?.Invoke(this, new MessageEventArgs { Message = message });
            }
        }

        public void Dispose() => Stop(graceful: false);

    }
}
