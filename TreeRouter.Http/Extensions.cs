using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
		
		public static async Task Dispatch(this IRouter router, HttpContext context)
		{
			var req = context.Request;
			if (req.Body.CanSeek)
				req.Body.Seek(0, SeekOrigin.Begin);
			var path = req.PathBase == null ? 
				req.Path.ToString() : req.PathBase.ToString().TrimEnd('/') + '/' + req.Path.ToString().TrimStart('/');
			var method = req.Method.ToLower();
			var contentType = context.Request.ContentType ?? "";
			var np = await context.SetupNestedParams();
			if (method == "post" && contentType.Contains("form") && np.Form.ContainsKey("_method"))
				method = np.Form["_method"] as string ?? "post";
			await router.Dispatch(path, method, context, context.RequestServices);
		}

		public static async Task<NestedParams> SetupNestedParams(this HttpContext context)
		{
			NestedParams np = null;
			if (context.Items.ContainsKey("nestedParams"))
				np = context.Items["nestedParams"] as NestedParams;
			if (np == null)
            {
                var formOptions = context.RequestServices.GetService<IOptions<FormOptions>>();
				np = new NestedParams(context);
				await np.ProcessForm();
				context.Items["nestedParams"] = np;
			}    
			return np;
		}
		
	}
}
