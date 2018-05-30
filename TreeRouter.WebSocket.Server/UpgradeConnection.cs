using System;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using TreeRouter.Errors;

namespace TreeRouter.WebSocket
{
  public class UpgradeConnection: IController
  {
    private readonly RequestDelegate _next;
    private IHandler Handler { get; }
    private string[] Protocols { get; }

    public UpgradeConnection(RequestDelegate next, IHandler webSocketHandler, string[] protocols)
    {
      _next = next;
      Handler = webSocketHandler;
      Protocols = protocols;
    }

    public async Task Invoke(HttpContext context)
    {
      if (!context.WebSockets.IsWebSocketRequest)
      {
        if (_next != null) await _next(context);
        return;
      }

      var selectedProtocol = GetProtocol(
        context.Request.Headers["Sec-WebSocket-Protocol"]);

      var socket = await (selectedProtocol == null
        ? context.WebSockets.AcceptWebSocketAsync()
        : context.WebSockets.AcceptWebSocketAsync(selectedProtocol));

      var socker = new WebSocker(socket, context);

      await Handler.OnConnected(socker);

      await Receive(socker, async (result, message) =>
      {
        if (result.MessageType == WebSocketMessageType.Close)
        {
          await Handler.OnDisconnected(socker);
          return;
        }
        var proto = selectedProtocol ?? Protocols[0];
        var passedMessage = new HandlerMessage
        {
          Socket = socker, SocketResult = result, Serialized = message, Subprotocol = proto,
          BasePath = context.Request.Path, HttpContext = context
        };
        await Handler.Route(new Request {Context = passedMessage});
      });
    }

    private string GetProtocol(string[] protocols)
    {
      if (Protocols == null || protocols == null)
        return null;

      string selectedProtocol = null;
      foreach (var iprotocol in protocols)
      {
        var protocols2 = iprotocol.Split(',');
        foreach (var protocol in protocols2)
        {
          var p = protocol.Trim();
          if (!Protocols.Contains(p)) continue;
          selectedProtocol = p;
          break;
        }
      }

      return selectedProtocol ?? Protocols[0];
    }

    private static async Task Receive(System.Net.WebSockets.WebSocket socket, Action<WebSocketReceiveResult, string> handleMessage)
    {
      while (socket.State == WebSocketState.Open)
      {
        var buffer = new ArraySegment<byte>(new byte[1024 * 4]);
        string message;
        WebSocketReceiveResult result;
        using (var ms = new MemoryStream())
        {
          do
          {
            result = await socket.ReceiveAsync(buffer, CancellationToken.None);
            ms.Write(buffer.Array, buffer.Offset, result.Count);
          } while (!result.EndOfMessage);

          ms.Seek(0, SeekOrigin.Begin);

          using (var reader = new StreamReader(ms, Encoding.UTF8))
          {
            message = await reader.ReadToEndAsync();
          }
        }

        handleMessage(result, message);
      }
    }

    public Task Route(Request routerRequest)
    {
      throw new NotImplementedException();
    }
  }
}
