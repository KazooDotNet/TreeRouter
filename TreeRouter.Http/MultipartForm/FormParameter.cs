using System;
using System.Collections.Generic;

namespace TreeRouter.Http.MultipartForm
{
	public class FormParameter<T> : IFormParameter
	{
		public IReadOnlyDictionary<string, List<string>> Headers { get; set; } = new Dictionary<string, List<string>>();
		public string Name { get; set; }
		public Type DataType => typeof(T);
		public T Data { get; set; }
		object IFormParameter.Data => Data;
	}
}
