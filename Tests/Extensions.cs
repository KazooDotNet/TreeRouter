using TreeRouter;

namespace Tests
{
	public static class Extensions
	{
		public static IntermediateBuilder NullAction(this IntermediateBuilder builder) =>
			builder.Action(_ => null);
	}
}
