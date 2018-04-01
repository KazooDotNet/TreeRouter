using System.ComponentModel.Design;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TreeRouter;
using Xunit;
using Tests.Controllers;

namespace Tests
{
	public class Dispatching : Base
	{
		
		private Router _router;
		
		public Dispatching()
		{
			
		}

		[Fact]
		public void DispatchesAction()
		{
			var responseText = "Hi there!";
			_router.Map( r => r.Get("/echo/")
				.Action( async req => await req.Context.Response.WriteAsync(responseText)));
			var context = MakeContext("/echo", "GET");
			Assert.Equal(responseText, DispatchAndRead(context));
		}

		[Fact]
		public void DispatchesController()
		{
			var responseText = "aaaaaaa";
			_router.Map( r => r.Get<EchoController>("/echo/{ responseText }"));
			var context = MakeContext("/echo/" + responseText, "get");
			Assert.Equal(responseText, DispatchAndRead(context));
		}

		
		
	}
}