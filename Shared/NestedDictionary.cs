using System;
using System.Collections.Generic;
using System.Linq;

namespace TreeRouter.Shared
{
	public class NestedDictionary : Dictionary<string, object>
	{
		public dynamic Get(string key)
		{
			var leaf = GetLeaf(key, false);
			if (leaf == null) return null;
			leaf.TryGetValue(LeafKey(key), out var value);
			return value;
		}

		/// <summary>
		///     Get the value of period separated key, drill down through the children to get value
		/// </summary>
		/// <param name="key">Period separated key</param>
		/// <typeparam name="T">Convert value to this type</typeparam>
		/// <returns>T</returns>
		public T Get<T>(string key)
		{
			var ret = Get(typeof(T), key);
			if (ret == null)
				return default;
			return (T) ret;
		}

		public object Get(Type type, string key)
		{
			var leaf = GetLeaf(key, false);
			if (leaf == null) return default;
			leaf.TryGetValue(LeafKey(key), out var value);
			if (value == null) return default;

			if (type == typeof(Guid))
				return GetGuid(key);
			if (type.IsInstanceOfType(value))
				return value;
			try
			{
				if (value is NestedDictionary nd)
					return Activator.CreateInstance(type).Assign(nd);
				return Convert.ChangeType(value, type);
			}
			catch (Exception)
			{
				return default;
			}
		}

		private object GetGuid(string key)
		{
			try
			{
				var value = Get<string>(key);
				return string.IsNullOrWhiteSpace(value) ? Guid.Empty : Guid.Parse(value);
			}
			catch (Exception)
			{
				return Guid.Empty;
			}
		}

		public void Set(string key, dynamic value)
		{
			var leaf = GetLeaf(key);
			leaf[LeafKey(key)] = value;
		}

		private static string LeafKey(string key)
		{
			var parts = key.Split('.');
			return parts[parts.Length - 1];
		}

		private NestedDictionary GetLeaf(string key, bool createLeaf = true)
		{
			var parts = key.Split('.').ToList();
			return GetLeaf(parts, createLeaf);
		}

		private NestedDictionary GetLeaf(IReadOnlyCollection<string> keys, bool createLeaf)
		{
			if (keys.Count == 1) return this;
			var newKeys = keys.ToList();
			var key = newKeys[0];
			newKeys.RemoveAt(0);
			if (createLeaf && (!ContainsKey(key) || !(this[key] is NestedDictionary)))
				this[key] = new NestedDictionary();
			return this[key] is NestedDictionary nd ? nd.GetLeaf(newKeys, createLeaf) : null;
		}
	}
}
