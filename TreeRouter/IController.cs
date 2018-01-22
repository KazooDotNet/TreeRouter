using System.Threading.Tasks;

namespace TreeRouter
{
	public interface IController
	{
		Task Route(Request routerRequest);
	}
}
