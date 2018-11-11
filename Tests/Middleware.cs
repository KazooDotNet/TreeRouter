using System;
using System.Dynamic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TreeRouter;
using Xunit;

namespace Tests
{
	public class Middleware
	{
		private ServiceProvider _provider;

		public Middleware()
		{
			var services = new ServiceCollection();
			services.AddScoped<SimpleService>();
			services.AddTransient<IRouter, Router>();
			_provider = services.BuildServiceProvider();
		}

		[Fact]
		public void KeepsScopeSeparate()
		{
			// TODO: test router scopes better
			var middleware = ActivatorUtilities.CreateInstance<TreeRouter.Http.Middleware>(_provider, GenerateNext("something"));
			var router = _provider.GetService<IRouter>();
			var factory = _provider.GetService<IServiceScopeFactory>();
			middleware.Invoke(new DefaultHttpContext(), router, factory).Wait();
			var service = _provider.GetService<SimpleService>();
			Assert.Null(service.Value);
			var nonscoped = new NonscopedMiddleware(GenerateNext("somethingelse"));
			nonscoped.Invoke(new DefaultHttpContext()).Wait();
			var service2 = _provider.GetService<SimpleService>();
			Assert.Equal("somethingelse", service2.Value);
		}

		private RequestDelegate GenerateNext(string value)
		{
			Task Ret(HttpContext context)
			{
				var s = _provider.GetService<SimpleService>();
				s.Value = value;
				return Task.CompletedTask;
			}
			return Ret;
		}
		
	}

	public class SimpleService
	{
		public string Value { get; set; }
	}

	public class NonscopedMiddleware
	{
		private RequestDelegate _next;
		
		public NonscopedMiddleware(RequestDelegate next)
		{
			_next = next;
		}

		public async Task Invoke(HttpContext context)
		{
			await _next.Invoke(context);
		}
		
	}
}