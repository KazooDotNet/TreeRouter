using System;
using System.Dynamic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Tests.Controllers;
using TreeRouter;
using Xunit;

namespace Tests
{
	public class Middleware
	{
		private readonly ServiceProvider _provider;

		public Middleware()
		{
			var services = new ServiceCollection();
			services.AddScoped<SimpleService>();
			services.AddTransient<IRouter, Router>();
			_provider = services.BuildServiceProvider();
		}

		[Fact]
		public async Task KeepsScopeSeparate()
		{
			var router = _provider.GetService<IRouter>();
			router.Map( r => r.Get<ScopeController>("/test") );
			var middleware = ActivatorUtilities.CreateInstance<TreeRouter.Http.Middleware>(_provider, router, _emptyNext);
			var http = new DefaultHttpContext();
			http.Request.Path = "/test";
			await middleware.Invoke(http, _provider.GetService<IServiceScopeFactory>());
			var service = _provider.GetService<SimpleService>();
			Assert.Null(service.Value);
			var nonscoped = new NonscopedMiddleware(GenerateNext("somethingelse"));
			nonscoped.Invoke(new DefaultHttpContext()).Wait();
			var service2 = _provider.GetService<SimpleService>();
			Assert.Equal("somethingelse", service2.Value);
		}

		private RequestDelegate _emptyNext => _ => Task.CompletedTask;

		private RequestDelegate GenerateNext(string value) => context =>
		{
			var s = _provider.GetService<SimpleService>();
			s.Value = value;
			return Task.CompletedTask;
		};


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
