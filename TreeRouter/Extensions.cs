using System;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace TreeRouter
{
	public static class Extensions
	{
		
		public static IServiceCollection AddTreeMap(this IServiceCollection collection, Assembly[] assemblies)
		{
			collection.AddSingleton<IRouter, Router>();
			var icType = typeof(IController);
			foreach (var assembly in assemblies)
				foreach (var type in assembly.ExportedTypes)
					if (icType.IsAssignableFrom(type) && !type.IsAbstract && type != icType)
						collection.AddTransient(type);
			return collection;
		}

		public static IServiceCollection AddTreeMap(this IServiceCollection collection, Assembly assembly) =>
			AddTreeMap(collection, new[] { assembly });
		
		public static IServiceCollection AddTreeMap(this IServiceCollection collection) =>
			AddTreeMap(collection, new[] { Assembly.GetExecutingAssembly(), Assembly.GetEntryAssembly() });
		

		private static bool _middlewareInstalled;

		public static IApplicationBuilder TreeMap(this IApplicationBuilder builder, string prefix, 
			Action<RouteBuilder> action)
		{
			if (!_middlewareInstalled)
			{
				builder.UseMiddleware<Middleware>();
				_middlewareInstalled = true;
			}
			var router = builder.ApplicationServices.GetService<IRouter>();
			router.Map(prefix, action);
			return builder;
		}

		public static IApplicationBuilder TreeMap(this IApplicationBuilder builder, Action<RouteBuilder> action)
			=> TreeMap(builder, null, action);
		
	}
}
