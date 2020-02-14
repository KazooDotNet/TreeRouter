using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;
using Tests.Controllers;
using Xunit;

namespace Tests
{
	public class HttpTests : Base
	{
		public HttpTests()
		{
			_router.Map(r => r.Map<ConvertRouter>("/", cr =>
			{
				cr.Get("int32").Action("Int32");
				cr.Get("int64").Action("Int64");
				cr.Get("bool").Action("Boolean");
				cr.Get("string").Action("String");
				cr.Post("object").Action("Object");
			}));
			_router.Compile();
		}

		[Fact]
		public async Task MapsInts()
		{
			const int val = 1234;
			var ctx = ContextWithServices($"/int32?id={val}", "GET");
			Assert.Equal(val.ToString(), DispatchAndRead(ctx));
		}

		[Fact]
		public async Task MapsInt64s()
		{
			const ulong val = 9223372036854775808;
			var ctx = ContextWithServices($"/int64?id={val}", "GET");
			Assert.Equal(val.ToString(), DispatchAndRead(ctx));
		}

		[Fact]
		public async Task MapsStrings()
		{
			const string val = "Ur m0th3r wAs a h@mpst3r";
			var ctx = ContextWithServices($"/string?id={val}", "GET");
			Assert.Equal($"\"{val.ToString()}\"", DispatchAndRead(ctx));
		}

		[Fact]
		public async Task MapsBools()
		{
			const bool val = false;
			const bool val2 = true;
			var ctx = ContextWithServices($"/bool?id={val}", "GET");
			Assert.Equal("false", DispatchAndRead(ctx));
			ctx = ContextWithServices($"/bool?id={val2}", "GET");
			Assert.Equal("true", DispatchAndRead(ctx));
		}

		[Fact]
		public async Task MapsObjects()
		{
			var val = new ConvertObject
			{
				Boolean = false,
				DateTime = DateTime.Now,
				Int32 = 5,
				Decimal = 5.5m,
				String = "5.5",
				Child = new ChildConvertObject
				{
					String = "Lonely child"
				},
				Children = new List<ChildConvertObject>
				{
					new ChildConvertObject {String = "Child 1"},
					new ChildConvertObject {String = "Child 2"},
					new ChildConvertObject {String = "Child 3"}
				}
			};
			using var bodyStream = new MemoryStream();
			using var writer = new StreamWriter(bodyStream);
			var serialized = JsonSerializer.Serialize(new { obj = val }, new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			});
			writer.Write(serialized);
			writer.Flush();
			bodyStream.Seek(0, SeekOrigin.Begin);
			var ctx = ContextWithServices("/object", "POST", bodyStream, "application/json");
			Assert.Equal(serialized, DispatchAndRead(ctx));
		}

		private HttpContext ContextWithServices(string path, string method, Stream body = null, string type = null)
		{
			var qIndex = path.LastIndexOf("?", StringComparison.InvariantCulture);
			var q = "";
			if (qIndex > -1)
			{
				q = path.Substring(qIndex);
				path = path.Substring(0, qIndex);
			}

			return MakeContext(path, method,
				setup: ctx => { },
				requestStream: body,
				requestSetup: req =>
				{
					var qDict = new Dictionary<string, StringValues>();
					if (q.Length > 1)
					{
						var parts = q
							.Substring(1)
							.Split("&")
							.Select(p2 => p2.Split("="));
						foreach (var p in parts)
							qDict.Add(p[0], new StringValues(p[1]));
					}

					req.Setup(x => x.QueryString).Returns(new QueryString(q));
					req.Setup(x => x.Query).Returns(new QueryCollection(qDict));
					if (type != null)
						req.Setup(x => x.ContentType).Returns(type);
				});
		}
	}
}
