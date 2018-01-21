using System.Collections.Generic;
using TreeRouter;
using Xunit;


namespace Tests
{
	public class BasicRouting
	{
		private Router _router;
		
		public BasicRouting()
		{
			_router = new Router();
		}
		
		[Fact]
		public void FindsLiteralRoute()
		{
			_router.Map(r => { r.Get("/stuff", new RouteOpts() ); });
			var result = _router.MatchPath("/stuff", "get");
			Assert.True(result.Found);
			result = _router.MatchPath("/not-stuff", "post");
			Assert.False(result.Found);
		}

		[Fact]
		public void ExtractsVars()
		{
			_router.Map(r => r.Delete("/stuff/{things}", new RouteOpts()) );
			var result = _router.MatchPath("/stuff/3", "delete");
			Assert.Equal("3", result.Vars["things"]);
		}

		[Fact]
		public void ExtractsOptionalVars()
		{
			_router.Map(r => r.Put("/stuff/{things?}", new RouteOpts()));
			
			var result = _router.MatchPath("/stuff", "put");
			Assert.True(result.Found);
			Assert.False(result.Vars.ContainsKey("things"));
			
			result = _router.MatchPath("/stuff/3", "put");
			Assert.True(result.Found);
			Assert.Equal("3", result.Vars["things"]);
		}

		[Fact]
		public void SetsDefaults()
		{
			_router.Map(r => r.Patch("/stuff/{ things? }", new RouteOpts
			{
				Defaults = new Dictionary<string, string> { { "things", "4"} }
			}));
			
			var result = _router.MatchPath("/stuff", "patch");
			Assert.True(result.Found);
			Assert.Equal("4", result.Vars["things"]);
			
			result = _router.MatchPath("/stuff/50", "patch");
			Assert.True(result.Found);
			Assert.Equal("50", result.Vars["things"]);
		}

		[Fact]
		public void MatchesSplats()
		{
			_router.Map(r => r.Post("/stuff/{ splat* }", new RouteOpts() ));
			
			var result = _router.MatchPath("/stuff", "post");
			Assert.True(result.Found);
			Assert.False(result.Vars.ContainsKey("splat"));
			
			result = _router.MatchPath("/stuff/and/things", "post");
			Assert.True(result.Found);
			Assert.Equal("and/things", result.Vars["splat"]);
		}
		
	}
}