using System.Collections.Generic;

namespace TreeRouter.Http
{
    public class HttpResponse
    {
        public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>();
        public object Body { get; set; }
        public int StatusCode { get; set; } = 200;

        public string ContentType
        {
            set => Headers["Content-Type"] = value;
            get => Headers.ContainsKey("Content-Type") ? Headers["Content-Type"] : null;	
        }
    }
}
