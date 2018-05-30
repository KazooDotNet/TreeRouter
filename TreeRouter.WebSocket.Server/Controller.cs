using System;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace TreeRouter.WebSocket
{
	public abstract class Controller : IController
	{

		protected RequestDictionary RouteVars;
		protected HandlerMessage Context;
		protected MessageRequest Request => Context.Request;
		protected IHandler Handler => Context.Handler;
		protected WebSocker Socket => Context.Socket;

		protected Task ReplyMessage(MessageResponse message, CancellationToken token = default(CancellationToken)) =>
			SendMessage(Socket, message, token);

		protected Task SendMessage(WebSocker socket, MessageResponse message, CancellationToken token) =>
			Handler.SendMessage(socket, message, token);

		protected Task SendMessage(string socketId, MessageResponse message, CancellationToken token) =>
			Handler.SendMessage(socketId, message, token);

		protected Task SendMessageToAll(MessageResponse message, CancellationToken token) =>
			Handler.SendMessageToAll(message, token);

		public virtual async Task Route(Request routerRequest)
		{
			RouteVars = routerRequest.RouteVars;
			if (!RouteVars.ContainsKey("action"))
				throw new Exception("Route vars do not contain `action`, be sure to set a default in your route.");

			var type = GetType();
			var method = type.GetMethod(RouteVars["action"]);
			if (method == null)
				throw new Exception($"`{RouteVars["action"]}` does not exist on `{type.Name}`");

			await Dispatch((HandlerMessage) routerRequest.Context, method);
		}

		protected virtual async Task Dispatch(HandlerMessage context, MethodInfo method, params object[] list)
		{
			Context = context;

			var type = GetType();

			var beforeMethod = type.GetMethod("Before");
			if (beforeMethod != null)
			{
				var beforeResponse = await Utils.ExtractValTask<bool>(beforeMethod.Invoke(this, new object[] { }));
				if (beforeResponse == false)
					return;
			}

			var mParamsLength = method.GetParameters().Length;
			if (list.Length < mParamsLength)
			{
				var newList = new object[mParamsLength];
				for (var i = 0; i < mParamsLength; i++)
					newList[i] = i > list.Length - 1 ? Type.Missing : list[i];
				list = newList;
			}

			await Utils.ExtractVoidTask(method.Invoke(this, list));

			var afterMethod = type.GetMethod("After");
			if (afterMethod != null)
				await Utils.ExtractVoidTask(afterMethod.Invoke(this, new object[] { }));
		}

		protected Task Dispatch(HandlerMessage context, string methodName, params object[] vars) =>
			Dispatch(context, GetType().GetMethod(methodName), vars);
	}
}
