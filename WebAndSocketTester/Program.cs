using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WebAndSocketTester
{
	class Program
	{
		static void Main(string[] args)
		{
			new WebHostBuilder()
				.UseKestrel()
				.UseUrls("http://localhost:5050") // TODO: make this configurable?
				.ConfigureAppConfiguration((hostingContext, config) =>
				{
					//DotNetEnv.Env.Load();
					config.AddEnvironmentVariables();
				})
				.ConfigureLogging((context, logging) =>
				{
					logging.AddConsole();
					logging.AddDebug();
				})
				.UseStartup<Startup>()
				.Build()
				.Run();
		}
	}
}