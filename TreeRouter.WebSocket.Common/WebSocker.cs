using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace TreeRouter.WebSocket
{
  // This class exists so that we can assign IDs to websockets
  public class WebSocker : System.Net.WebSockets.WebSocket
  {
    private System.Net.WebSockets.WebSocket Socket { get; }
    public string Id { get; private set; }

    public WebSocker(System.Net.WebSockets.WebSocket socket, string id = null)
    {
      Socket = socket;
      Id = id ?? Guid.NewGuid().ToString();
    }

    public override WebSocketCloseStatus? CloseStatus => Socket.CloseStatus;

    public override string CloseStatusDescription => Socket.CloseStatusDescription;

    public override WebSocketState State => Socket.State;

    public override string SubProtocol => Socket.SubProtocol;

    public override void Abort() { Socket.Abort(); }

    public override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken) =>
      Socket.CloseAsync(closeStatus, statusDescription, cancellationToken);

    public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken) => 
      Socket.CloseOutputAsync(closeStatus, statusDescription, cancellationToken);
    

    public override void Dispose() { Socket.Dispose(); }

    public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken) => 
      Socket.ReceiveAsync(buffer, cancellationToken);

    public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken) =>
      Socket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);
    
  }
}
