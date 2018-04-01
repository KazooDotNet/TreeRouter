using System;
using System.Collections.Generic;

namespace TreeRouter
{
	public class RequestDictionary : Dictionary<string, string>
	{
		public RequestDictionary()
		{
		}
		
		public RequestDictionary(IDictionary<string, string> dict) : base(dict)
		{
		}

		public T Get<T>(string key) where T : IConvertible
		{
			TryGetValue(key, out var value);
			if (value == null) return default;
			return (T) Convert.ChangeType(value, typeof(T));
		}

		public Guid GetGuid(string key)
		{
			TryGetValue(key, out var value);
			return string.IsNullOrEmpty(value) ? Guid.Empty : Guid.Parse(value);
		}
	}
}
