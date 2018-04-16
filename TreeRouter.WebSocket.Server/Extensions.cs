using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TreeRouter.WebSocket
{
	public static class Extensions
	{
		public static IServiceCollection AddWebSockets(this IServiceCollection services) =>
			services.AddWebSockets(new[] {Assembly.GetEntryAssembly(), Assembly.GetExecutingAssembly()});

		public static IServiceCollection AddWebSockets(this IServiceCollection services, IEnumerable<Assembly> assemblies)
		{
			services.AddSingleton<ConnectionManager>();
			var hType = typeof(IHandler);
			foreach (var assembly in assemblies)
			foreach (var type in assembly.ExportedTypes)
				if (hType.IsAssignableFrom(type) && !type.IsAbstract && type != hType)
					services.AddTransient(type);
			return services;
		}

		public static IApplicationBuilder MapWebSockets(this IApplicationBuilder app, PathString path,
			string[] subProtocols, Action<RouteBuilder> action) => MapWebSockets<Handler>(app, path, subProtocols, action);

		public static IApplicationBuilder MapWebSockets<THandler>(this IApplicationBuilder app, PathString path,
			string[] subProtocols, Action<RouteBuilder> action) where THandler : IHandler
		{
			app.UseWebSockets();
			var handler = app.ApplicationServices.GetService<THandler>();
			if (handler == null)
				throw new Exception("Handler not found. Have you called `AddWebSockets` on your service collection setup?");
			var router = app.ApplicationServices.GetService<IRouter>();
			router.Map(action);
			handler.Router = router;
			return app.Map(path, _app =>
				_app.UseMiddleware<UpgradeConnection>(handler, subProtocols));
		}
		
		


		// TODO: implement WebSocket handler for RouteBuilder
		/*public static void WebSocket(this RouteBuilder builder, string path) =>
			WebSocket(builder, path, typeof(Handler));
		
		public static void WebSocket<THandler>(this RouteBuilder builder, string path) where THandler : IHandler =>
			WebSocket(builder, path, typeof(THandler));
		

		private static void WebSocket(RouteBuilder builder, string path, Type handler)
		{
			
		}*/
	}
}