using System;
using System.Collections.Generic;
using Tests.Controllers;
using Xunit;

namespace Tests
{
	public class NestedAndResources : Base
	{

		public NestedAndResources() : base()
		{
		}
		
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
		
	}
}