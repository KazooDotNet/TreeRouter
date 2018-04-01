using TreeRouter;
using Xunit;


namespace Tests
{
	public class BasicRouting
	{
		private Router _router;
		
		public BasicRouting()
		{
			_router = new Router(null);
		}
		
		[Fact]
		public void FindsLiteralRoute()
		{
			_router.Map(r => { r.Get("/stuff").Action( _ => null ); });
			var result = _router.MatchPath("/stuff", "get");
			Assert.True(result.Found);
			result = _router.MatchPath("/not-stuff", "post");
			Assert.False(result.Found);
		}

		[Fact]
		public void FindsRootRoute()
		{
			_router.Map( r => r.Get("/").Action( _ => null ) );
			var result = _router.MatchPath("/", "get");
			Assert.True(result.Found);
			Assert.NotNull(result.Route);
		}

		[Fact]
		public void ExtractsVars()
		{
			_router.Map(r => r.Delete("/stuff/{things}").Action( _ => null) );
			var result = _router.MatchPath("/stuff/3", "delete");
			Assert.Equal("3", result.Vars["things"]);
			Assert.NotNull(result.Route);
		}

		[Fact]
		public void ExtractsOptionalVars()
		{
			_router.Map(r => r.Put("/stuff/{things?}").Action( _ => null ) );
			
			var result = _router.MatchPath("/stuff", "put");
			Assert.True(result.Found);
			Assert.False(result.Vars.ContainsKey("things"));
			
			result = _router.MatchPath("/stuff/3", "put");
			Assert.True(result.Found);
			Assert.Equal("3", result.Vars["things"]);
			Assert.NotNull(result.Route);
		}

		[Fact]
		public void SetsDefaults()
		{
			_router.Map(r => 
				r.Patch("/stuff/{ things? }")
				.Defaults(new Defaults {{ "things", "4"}})
				.Action( _ => null )
			);
			
			var result = _router.MatchPath("/stuff", "patch");
			Assert.True(result.Found);
			Assert.Equal("4", result.Vars["things"]);
			
			result = _router.MatchPath("/stuff/50", "patch");
			Assert.True(result.Found);
			Assert.Equal("50", result.Vars["things"]);
			Assert.NotNull(result.Route);
		}

		[Fact]
		public void SetsLambdaDefaults()
		{
			_router.Map(r => 
				r.Patch("/stuff/{ things? }")
					.Defaults( d => d["things"] = "4" )
					.Action( _ => null )
			);
			var result = _router.MatchPath("/stuff", "patch");
			Assert.Equal("4", result.Vars["things"]);
		}

		[Fact]
		public void MatchesSplats()
		{
			_router.Map(r => r.Post("/stuff/{ splat* }").Action( _ => null ));
			
			var result = _router.MatchPath("/stuff", "post");
			Assert.True(result.Found);
			Assert.False(result.Vars.ContainsKey("splat"));
			
			result = _router.MatchPath("/stuff/and/things", "post");
			Assert.True(result.Found);
			Assert.Equal("and/things", result.Vars["splat"]);
			Assert.NotNull(result.Route);
		}

		[Fact]
		public void MapsWithPrefix()
		{
			_router.Map("/prefix", r => r.Get("/things/{ things ? }"));
			var result = _router.MatchPath("/prefix/things/3", "get");
			Assert.True(result.Found);
			Assert.NotNull(result.Route);
			Assert.Equal("3", result.Vars["things"]);
		}
		
	}
}