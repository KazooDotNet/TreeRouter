using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Tests.Controllers;
using TreeRouter;
using FormOptions = TreeRouter.Http.FormOptions;
using HttpResponse = Microsoft.AspNetCore.Http.HttpResponse;

namespace Tests
{
	public class Base
	{
		protected readonly Router _router;
		private readonly IServiceProvider _services;

		protected Base()
		{
			var services = new ServiceCollection();
			services.AddTransient<EchoController>();
			services.AddTransient<ResourcesController>();
			services.AddOptions<FormOptions>();
			_router = new Router();
			services.AddSingleton<IRouter>(_router);
			_services = services.BuildServiceProvider();
		}
		
		protected HttpContext MakeContext(string path, string method, Stream requestStream = null, 
			Action<Mock<HttpContext>> setup = null, Action<Mock<HttpRequest>> requestSetup = null)
		{
			var request = new Mock<HttpRequest> { CallBase = true };
			request.Setup(x => x.Path).Returns(path);
			request.Setup(x => x.Method).Returns(method);
			requestSetup?.Invoke(request);
			if (requestStream == null)
				requestStream = new MemoryStream();
			request.Setup(x => x.Body).Returns(requestStream);
			
			
			var response = new Mock<HttpResponse>  { CallBase = true };
			var responseStream = new MemoryStream();
			response.Setup(x => x.Body).Returns(responseStream);
			var headers = new HeaderDictionary();
			response.Setup(x => x.Headers).Returns(headers);

			var context = new Mock<HttpContext>  { CallBase = true };
			context.Setup(x => x.Request).Returns(request.Object);
			context.Setup(x => x.Response).Returns(response.Object);
			context.Setup(x => x.Items).Returns(new Dictionary<object, object>());
			context.Setup(x => x.RequestServices).Returns(_services);
			context.Setup(x => x.Features).Returns(new FeatureCollection());
			setup?.Invoke(context);
			return context.Object;
		}

		protected string DispatchAndRead(HttpContext context)
		{
			try
			{
				var middleware = new TreeRouter.Http.Middleware(null, _router);
				Task.Run(() => middleware.Invoke(context, _services.GetService<IServiceScopeFactory>())).Wait();
			}
			catch (AggregateException e)
			{
				ExceptionDispatchInfo.Capture(e.InnerExceptions.First()).Throw();
			}
			
			var memStream = context.Response.Body;
			memStream.Position = 0;
			return new StreamReader(memStream).ReadToEnd();
		}

		protected string DispatchAndRead(string path, string method)
		{
			var context = MakeContext(path, method);
			return DispatchAndRead(context);
		}
	}
}
