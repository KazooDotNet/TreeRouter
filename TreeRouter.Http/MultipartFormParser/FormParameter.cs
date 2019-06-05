using System;
using System.Collections.Generic;

namespace TreeRouter.Http.MultipartFormParser
{
	public class FormParameter<T> : IFormParameter {
		public Dictionary<string, List<string>> Headers { get; } = new Dictionary<string, List<string>>();
		public string Name { get; set; }
		public Type DataType => typeof(T);
		public T Data { get; set; }
		object IFormParameter.Data => Data;
	}
}
