using Microsoft.AspNetCore.Http;
using Xunit;
using Tests.Controllers;

namespace Tests
{
	public class Dispatching : Base
	{

		[Fact]
		public void DispatchesAction()
		{
			const string responseText = "Hi there!";
			_router.Map( r => r.Get("/echo/")
				.Action( async req =>
				{
					var hContext = (HttpContext) req.Context;
					await hContext.Response.WriteAsync(responseText);
				}));
			var context = MakeContext("/echo", "GET");
			Assert.Equal(responseText, DispatchAndRead(context));
		}

		[Fact]
		public void DispatchesController()
		{
			const string responseText = "aaaaaaa";
			_router.Map( r => r.Get<EchoController>("/echo/{ responseText }"));
			var context = MakeContext("/echo/" + responseText, "get");
			Assert.Equal(responseText, DispatchAndRead(context));
		}
		
	}
}