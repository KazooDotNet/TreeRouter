using System;

namespace TreeRouter.Errors
{
	public class UnregisteredController : Exception
	{
		public Type ControllerType { get; set; }

		public UnregisteredController(string msg) : base(msg)
		{
		}
	}
}
