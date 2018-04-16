using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace WebAndSocketTester
{
	
	public class ErrorCatcher
	{
		private readonly RequestDelegate _next;
		private readonly ILogger<ErrorCatcher> _logger;

		public ErrorCatcher(RequestDelegate next, ILogger<ErrorCatcher> logger)
		{
			_next = next;
			_logger = logger;
		}

		public async Task Invoke(HttpContext context)
		{
			try
			{
				_next.Invoke(context);
			}
			catch (Exception e)
			{
				_logger.LogError(e.ToString());
			}
	}
	}
}