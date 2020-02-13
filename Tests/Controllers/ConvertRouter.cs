using System;
using TreeRouter.Http;

namespace Tests.Controllers
{
	public class ConvertObject
	{
		public int Int32 { get; set; }
		public long Int64 { get; set; }
		public decimal Decimal { get; set; }
		public string String { get; set; }
		public bool Boolean { get; set; }
		public DateTime DateTime { get; set; }
		public ChildConvertObject Child { get; set; }
	}

	public class ChildConvertObject
	{
		public string String { get; set; }
	}
	
	public class ConvertRouter : Controller
	{
		public HttpResponse Int32(int id) => SendJson(id);
		public HttpResponse Int64(ulong id) => SendJson(id);
		public HttpResponse String(string id) => SendJson(id);
		public HttpResponse Boolean(bool id) => SendJson(id);
		public HttpResponse Object(ConvertObject obj) => SendJson(obj);
	}
}
