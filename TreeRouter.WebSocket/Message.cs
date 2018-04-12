using System;
using System.Collections.Generic;

namespace TreeRouter.WebSocket
{
  public class MessageData : Dictionary<string, dynamic>
  {
  }

  public static class Method
  {
    public static string GET = "GET";
    public static string POST = "POST";
    public static string PUT = "PUT";
    public static string PATCH = "PATCH";
    public static string DELETE = "DELETE";
  }

  public static class MessageType
  {
    public static string Response = "Response";
    public static string System = "System";
    public static string ConnectionEvent = "ConnectionEvent";
    public static string Error = "Error";
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
    public WebSocker Socket { get; set; }
  }

  public class MessageResponse : Message
  {
    public int? Status { get; set; }
    public MessageData Errors { get; set; }
    public string Id { get; set; }
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
