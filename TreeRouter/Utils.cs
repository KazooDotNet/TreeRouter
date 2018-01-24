using System.Linq;

namespace TreeRouter
{
	public static class Utils
	{
		public static string JoinPath(params string[] list)
		{
			var parts = list.Select(p => p.Trim('/').Trim()).ToArray();
			return string.Join("/", parts);
		}
	}
}