using System;
using System.ComponentModel.Design;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TreeRouter;
using Xunit;

namespace Tests
{
	public class Dispatching
	{
		
		private Router _router;
		
		public Dispatching()
		{
			var services = new ServiceCollection();
			services.AddTransient<EchoController>();
			var serviceProvider = services.BuildServiceProvider();
			_router = new Router(new ServiceContainer(serviceProvider));
		}

		[Fact]
		public void DispatchesAction()
		{
			var responseText = "Hi there!";
			_router.Map( r => r.Get("/echo/", new RouteOpts 
			{ 
				Action = async req => await req.Context.Response.WriteAsync(responseText)
			}));
			var context = MakeContext("/echo", "GET");
			Assert.Equal(responseText, dispatchAndRead(context));
		}

		[Fact]
		public void DispatchesController()
		{
			var responseText = "aaaaaaa";
			_router.Map( r => r.Get<EchoController>("/echo/{ responseText }", new RouteOpts()));
			var context = MakeContext("/echo/" + responseText, "get");
			Assert.Equal(responseText, dispatchAndRead(context));
		}

		private HttpContext MakeContext(string path, string method)
		{
			var request = new Mock<HttpRequest>();
			request.Setup(x => x.Path).Returns(path);
			request.Setup(x => x.Method).Returns(method);

			var response = new Mock<HttpResponse>();
			var ms = new MemoryStream();
			response.Setup(x => x.Body).Returns(ms);

			var context = new Mock<HttpContext>();
			context.Setup(x => x.Request).Returns(request.Object);
			context.Setup(x => x.Response).Returns(response.Object);

			return context.Object;
		}

		private string dispatchAndRead(HttpContext context)
		{
			var task = _router.Dispatch(context);
			task.Wait();
			var memStream = context.Response.Body;
			memStream.Position = 0;
			return new StreamReader(memStream).ReadToEnd();
		}
		
	}
}