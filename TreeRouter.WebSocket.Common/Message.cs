using System;
using System.Collections.Generic;

namespace TreeRouter.WebSocket
{
	public class MessageData : Dictionary<string, dynamic>
	{
	}

	public static class Method
	{
		public const string Get = "GET";
		public const string Post = "POST";
		public const string Put = "PUT";
		public const string Patch = "PATCH";
		public const string Delete = "DELETE";
	}

	public static class MessageType
	{
		public const string Response = "Response";
		public const string System = "System";
		public const string ConnectionEvent = "ConnectionEvent";
		public const string Error = "Error";
	}

	public class Message
	{
		public string MessageType { get; set; }
		public MessageData Data { get; set; }
	}

	public class MessageRequest : Message
	{
		public MessageRequest(string id = null) =>
			Id = id ?? Guid.NewGuid().ToString();

		public string Id { get; set; }
		public string Path { get; set; }
		public string Method { get; set; }
	}

	public class MessageResponse : Message
	{
		public string Id { get; set; }
		public int? Status { get; set; }
		public MessageData Errors { get; set; }
		public string Path { get; set; }
		public string Method { get; set; }
		public bool Timeout { get; set; }

		public MessageResponse()
		{
		}

		public MessageResponse(MessageRequest message)
		{
			Id = message.Id;
			Path = message.Path;
			Method = message.Method;
		}
	}
}
