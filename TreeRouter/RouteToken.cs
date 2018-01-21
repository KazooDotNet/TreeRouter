namespace TreeRouter
{
	public class RouteToken
	{
		private bool? _greedy;
		private bool? _optional;
		public bool Optional { get => _optional ?? false; set => _optional = value; }
		public bool Greedy { get => _greedy ?? false; set => _greedy = value; }
		public dynamic Matcher { get; set; }
	}
}