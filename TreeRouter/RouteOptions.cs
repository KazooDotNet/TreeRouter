using System;
using System.Threading.Tasks;

namespace TreeRouter
{
	public class RouteOptions
	{
		public string Path { get; set; }
		public string[] Methods { get; set; }
		public Defaults Defaults { get; set; }
		public Constraints Constraints { get; set; }
		public Func<Request, Task> ActionHandler { get; set; }
		public Type ClassHandler { get; set; }
	}
}
