using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TreeRouter.Http
{
	public static class Extensions
	{
		
		public static IServiceCollection AddTreeMap(this IServiceCollection collection)
		{
			// TODO: is this sufficient for use in multiple routers?
			return collection.AddTransient<IRouter, Router>();
		}

		public static IApplicationBuilder TreeMap(this IApplicationBuilder builder, string prefix, 
			Action<RouteBuilder> action)
		{
			var router = builder.ApplicationServices.GetService<IRouter>();
			if (router == null)
				throw new ArgumentException("Router not set. Have you set up the service by calling `AddTreeMap()` on IServiceCollection?");
			router.Map(prefix, action);
			builder.UseMiddleware<Middleware>(router);
			return builder;
		}

		public static IApplicationBuilder TreeMap(this IApplicationBuilder builder, Action<RouteBuilder> action)
			=> TreeMap(builder, null, action);
		
		public static Task Dispatch(this IRouter router, HttpContext context)
		{
			var req = context.Request;
			var path = req.PathBase == null ? 
				req.Path.ToString() : req.PathBase.ToString().TrimEnd('/') + '/' + req.Path.ToString().TrimStart('/');
			var method = req.Method.ToLower();
			var contentType = context.Request.ContentType ?? "";
			if (method == "post" && contentType.Contains("form") && context.Request.Form.ContainsKey("_method"))
				method = context.Request.Form["_method"];
			return router.Dispatch(path, method, context, context.RequestServices);
		}
		
	}
}