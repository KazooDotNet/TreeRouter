using System;
using System.Collections.Generic;

namespace TreeRouter.Http.MultipartFormParser
{
	public interface IFormParameter
	{
		Dictionary<string, List<string>> Headers { get; }
		string Name { get; }
		Type DataType { get; }
		object Data { get; }
	}
}
