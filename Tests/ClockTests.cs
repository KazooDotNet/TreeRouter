using System;
using TreeRouter.WebSocket;
using Xunit;

namespace Tests
{
	public class ClockTests
	{
		[Fact]
		public void FreezesAndIncrements()
		{
			var baseTime = new DateTime(2016, 10, 10);
			var clock = new FakeClock(baseTime, freeze: true);
			clock.AddMinutes(2);
			Assert.Equal(baseTime.AddMinutes(2), clock.Now);
		}
	}
}
