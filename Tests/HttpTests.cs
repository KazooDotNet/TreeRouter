using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
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
			_router.Map(r =>  r.Map<ConvertRouter>("/", cr =>
			{
				cr.Get("int32").Action("Int32");
				cr.Get("int64").Action("Int64");
				cr.Get("bool").Action("Boolean");
				cr.Get("string").Action("String");
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

		private HttpContext ContextWithServices(string path, string method)
		{
			var qIndex = path.LastIndexOf("?", StringComparison.InvariantCulture);
			var q = "";
			if (qIndex > -1)
			{
				q = path.Substring(qIndex);
				path = path.Substring(0, qIndex);
			}
			return MakeContext(path, method, 
				setup: ctx =>
				{
					
				},
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
				});
		}
	}
}
