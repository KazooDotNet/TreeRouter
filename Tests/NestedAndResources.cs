using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Tests.Controllers;
using TreeRouter;
using Xunit;

namespace Tests
{
	public class NestedAndResources : Base
	{
		[Fact]
		public void Resources()
		{
			_router.Map( r => r.Resources<ResourcesController>("things") );
			var memberMethods = new Dictionary<string,string>
			{
				{ "get", "Show" },
				{ "put", "Update" },
				{ "patch", "Update" },
				{ "delete", "Delete" }
			};
			var collectionMethods = new Dictionary<string,string>
			{
				{ "get", "Index" },
				{ "post", "Create" }
			};
			string response;
			foreach (var pair in memberMethods)
			{
				response = DispatchAndRead("/things/id", pair.Key);
				Assert.Equal(pair.Value, response);
			}
			foreach (var pair  in collectionMethods)
			{
				response = DispatchAndRead("/things", pair.Key);
				Assert.Equal(pair.Value, response);
			}

			response = DispatchAndRead("/things/new", "get");
			Assert.Equal("New", response);
		}

		[Fact]
		public void Resource()
		{
			_router.Map( r => r.Resource<ResourcesController>("things") );
			Assert.Equal("Show", DispatchAndRead("/things", "get"));
			Assert.Equal("Edit", DispatchAndRead("/things/edit", "get"));
			Assert.Equal("New", DispatchAndRead("/things/new", "get"));
			Assert.Equal("Create", DispatchAndRead("/things", "post"));
			Assert.Equal("Update", DispatchAndRead("/things", "put"));
			Assert.Equal("Update", DispatchAndRead("/things", "patch"));
			Assert.Equal("Delete", DispatchAndRead("/things", "delete"));
		}

		[Fact]
		public void NestedUnderMember()
		{
			_router.Map(r =>
			{
				r.Resources<ResourcesController>("things")
					.OnMember(mem =>
					{
						mem.Get("/stuff").NullAction();
						mem.Resources<ResourcesController>("widgies");
					}, "thing");
			});
			var result = _router.MatchPath("/things/1/stuff", "get");
			Assert.True(result.Found);
			Assert.Equal("1", result.Vars["thingId"]);
			result = _router.MatchPath("/things/1/widgies/2", "get");
			Assert.True(result.Found);
			Assert.Equal("1", result.Vars["thingId"]);
			Assert.Equal("2", result.Vars["id"]);
		}

		[Fact]
		public void NestedUnderCollection()
		{
			_router.Map(r =>
			{
				r.Resources<ResourcesController>("things")
					.OnCollection(col =>
					{
						col.Get("/stuff").NullAction();
						col.Resources<ResourcesController>("widgies");
					});
			});
			var result = _router.MatchPath("/things/stuff", "get");
			Assert.True(result.Found);
			result = _router.MatchPath("/things/widgies/1", "get");
			Assert.True(result.Found);
			Assert.Equal("1", result.Vars["id"]);
		}

		[Fact]
		public void MapWithController()
		{
			_router.Map(r => { 
				r.Map<ResourcesController>(rc =>
				{
					rc.Get("/test").Action("Show");
					rc.Get("/another").Action("Show");
					rc.Get("/{blah*}").Action("Show");
				}); 
			});
			var result = _router.MatchPath("/test", "get");
			Assert.True(result.Found);
			Assert.Equal(result.Route.ClassHandler, typeof(ResourcesController));
			result = _router.MatchPath("/another", "get");
			Assert.True(result.Found);
			Assert.Equal(result.Route.ClassHandler, typeof(ResourcesController));
			result = _router.MatchPath("/sing/song", "get");
			Assert.True(result.Found);
			Assert.Equal(result.Route.ClassHandler, typeof(ResourcesController));
			
		}
		
	}
}