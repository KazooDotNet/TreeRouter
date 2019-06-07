using System;
using System.Collections.Generic;

namespace TreeRouter.Http.MultipartFormParser
{
	public interface IFormParameter
	{
		IReadOnlyDictionary<string, List<string>> Headers { get; }
		string Name { get; }
		Type DataType { get; }
		object Data { get; }
	}
}
