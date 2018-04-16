using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TreeRouter.Errors;

namespace TreeRouter.WebSocket
{
	public interface IHandler : IController
	{
		Task OnConnected(WebSocker socket);
		Task OnDisconnected(WebSocker socket);
		Task SendMessage(WebSocker socket, MessageResponse message, CancellationToken token = default(CancellationToken));
		Task SendMessage(string socketId, MessageResponse message, CancellationToken token = default(CancellationToken));
		Task SendMessageToAll(MessageResponse message, CancellationToken token = default(CancellationToken));
		IRouter Router { set; }
	}

	public class Handler : IHandler
	{
	  protected ConnectionManager ConnectionManager { get; }
		public IRouter Router { protected get; set; }

		public Handler(ConnectionManager connectionManager)
		{
			ConnectionManager = connectionManager;
		}

		public virtual Task OnConnected(WebSocker socket) =>
			SendMessage(socket, new MessageResponse
			{
				MessageType = MessageType.ConnectionEvent,
				Data = new MessageData {["SocketId"] = socket.Id}
			});
		

		public virtual Task OnDisconnected(WebSocker socket) =>
			ConnectionManager.RemoveSocket(socket.Id);

	  public Task SendMessage(string socketId, MessageResponse message, CancellationToken token = default(CancellationToken))
		{
			var socket = ConnectionManager.GetSocketById(socketId);
			return SendMessage(socket, message, token);
		}

		public Task SendMessage(WebSocker socket, MessageResponse message, CancellationToken token = default(CancellationToken))
		{
			// TODO: maybe throw an error?
			if (socket.State != WebSocketState.Open) return Task.CompletedTask;
			message.Status = message.Status ?? 200;
			message.Data = message.Data ?? new MessageData();
			return SendJson(socket, message, token);
		}

		private Task SendJson(WebSocker socket, Message message, CancellationToken token = default(CancellationToken))
		{
			var serializedMessage = JsonConvert.SerializeObject(message);
		  var buffer = new ArraySegment<byte>(array: Encoding.UTF8.GetBytes(serializedMessage),
		    offset: 0,
		    count: serializedMessage.Length);
		  return socket.SendAsync(buffer, WebSocketMessageType.Text, cancellationToken: token, endOfMessage: true);
		}

		public Task SendMessageToAll(MessageResponse message, CancellationToken token = default(CancellationToken))
		{
			var tasks = new List<Task>();
			foreach (var pair in ConnectionManager.GetAll())
			{
			  if (token.IsCancellationRequested)
			    break;
				if (pair.Value.State == WebSocketState.Open)
					tasks.Add(SendMessage(pair.Value, message, token));
			}
			return Task.WhenAll(tasks.ToArray());
		}

	  public Task Route(Request routerRequest)
	  {
	    MessageRequest message = null;
	    var hm = (HandlerMessage) routerRequest.Context;

	    switch (hm.Subprotocol)
	    {
	      case "rest.json":
	        message = DeserializeJson(hm.Serialized);
	        break;
	      case "rest.msgpack":

	        break;
	      default:
	        return SendMessage(hm.Socket, new MessageResponse
	        {
	          MessageType = MessageType.Error,
	          Status = 400,
	          Errors = new MessageData
	          {
	            ["Message"] = $"Invalid protocol sent: `{hm.Subprotocol}`. Needs to be `rest.json` or `rest.msgpack`"
	          }
	        });
	    }

	    if (message == null)
	      return SendMessage(hm.Socket, new MessageResponse
	      {
	        MessageType = MessageType.Error,
	        Errors = new MessageData {["Message"] = "Invalid structure of request"}
	      });

		  hm.Handler = this;
		  hm.Request = message;
		  
		  try
		  {
			  return Router.Dispatch(message.Path, message.Method, hm);
		  }
		  catch (Exception e)
		  {
			  while (e.InnerException != null)
				  e = e.InnerException;
			  if (e is RouteNotFound)
			  {
				  return SendMessage(hm.Socket, new MessageResponse(message)
				  {
					  MessageType = MessageType.Error,
					  Status = 404,
					  Errors = new MessageData {["Message"] = "Route not found"}
				  });
			  }
			  ExceptionDispatchInfo.Capture(e).Throw();
		  }

		  return Task.CompletedTask;
	  }

	  private static MessageRequest DeserializeJson(string serialized)
		{
			try
			{
				return JsonConvert.DeserializeObject<MessageRequest>(serialized);
			}
			catch (JsonReaderException)
			{
				return null;
			}
		}
	  
	}
}
