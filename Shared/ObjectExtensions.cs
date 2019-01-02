using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace TreeRouter.Shared
{
	public static class ObjectExtensions
	{

		public static Dictionary<string, object> ToDictionary(this object obj)
		{
			var dict = new Dictionary<string, object>();
			var type = obj.GetType();
			foreach (var property in type.GetProperties())
				dict[property.Name] = property.GetValue(obj);
			return dict;
		}
		
		public static object Assign(this object obj, IDictionary<string, object> dict, params string[] whitelist)
		{
			if (dict == null) return obj;
			var properties = new Dictionary<string, PropertyInfo>();
			var objProperties = obj.GetType().GetProperties();
			foreach (var property in objProperties)
				properties[property.Name] = property;

			var cType = typeof(IConvertible);
			var gType = typeof(Guid);
			var dtType = typeof(DateTime);
			var eType = typeof(IEnumerable);
			foreach (var dKey in dict.Keys)
			{
				// TODO: Check for underscore variables?
				var key = char.ToUpper(dKey[0]) + dKey.Substring(1);
				if (whitelist.Length > 0 && !whitelist.Contains(key) || !properties.ContainsKey(key))
					continue;
				
				string val = null;
				Dictionary<string, object> dictVal = null;
				Dictionary<string, object>[] arrayDict = null;
				switch (dict[dKey])
				{
					case string strVal:
						val = strVal;
						break;
					case StringValues strVals:
						val = strVals.Last();
						break;
					case null:
						// Keep null and break
						break;
					case Dictionary<string, object> nDict:
						dictVal = nDict;
						break;
					case Dictionary<string, object>[] aDict:
						arrayDict = aDict;
						break;
					default:
						continue;
				}

				var t = Nullable.GetUnderlyingType(properties[key].PropertyType) ?? properties[key].PropertyType;
				object value = null;
				if (t.IsEnum)
				{
					if (val != null)
					{
						try
						{
							value = Enum.Parse(t, val, true);
						}
						catch
						{
							value = null;
						}
					}
				}
				else if (dtType.IsAssignableFrom(t))
				{
					if (!string.IsNullOrWhiteSpace(val))
						value = DateTime.Parse(val);
				}
				else if (cType.IsAssignableFrom(t))
				{
					value = string.IsNullOrWhiteSpace(val) ? null : Convert.ChangeType(val, t);
				}
				else if (gType.IsAssignableFrom(t))
				{
					if (val != null)
						value = Guid.Parse(val);
				}
				else if (t.IsArray && t.GetElementType().GetConstructor(Type.EmptyTypes) != null &&
				         !t.GetElementType().IsAbstract)
				{
					if (arrayDict != null)
					{
						var et = t.GetElementType();
						var d = (Array) Activator.CreateInstance(t, arrayDict.Length);
						var i = 0;
						foreach (var ad in arrayDict)
						{
							var arrayEle = Activator.CreateInstance(et);
							arrayEle.Assign(ad);
							d.SetValue(arrayEle, i++);
						}

						value = d;
					}
				}
				// TODO: Check for Dictionary before Enumerable
				else if (eType.IsAssignableFrom(t) &&
				         t.GetGenericArguments()[0].GetConstructor(Type.EmptyTypes) != null &&
				         !t.GetGenericArguments()[0].IsAbstract)
				{
					if (arrayDict != null)
					{
						var eleType = t.GetGenericArguments()[0];
						var listType = typeof(List<>).MakeGenericType(eleType);
						// TODO: make list specific size?
						var l = (IList) Activator.CreateInstance(listType);
						foreach (var ad in arrayDict)
						{
							var arrayEle = Activator.CreateInstance(eleType);
							arrayEle.Assign(ad);
							l.Add(arrayEle);
						}

						value = l;
					}
				}
				else if (t.GetConstructor(Type.EmptyTypes) != null && !t.IsAbstract)
				{
					if (dictVal != null)
					{
						value = Activator.CreateInstance(t);
						value.Assign(dictVal);
					}
				}
				else
				{
					throw new ArgumentException($"Cannot convert to type `{t.Name}` with name `{key}`");
				}

				properties[key].SetValue(obj, value);
			}

			return obj;
		}

		/*public static bool Remove<K, V>(this IDictionary<K, V> dict, K key, out V value)
		{
			value = dict.ContainsKey(key) ? dict[key] : default;
			return dict.Remove(key);
		}*/
		
		// ForEach for enumerables! Yay!
		public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
		{
			foreach (var item in source)
				action(item);
		}

		public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
		{
			var i = 0;
			foreach (var item in source)
				action(item, i++);
		}

		public static Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> action)
		{
			var tasks = source.Select(item => action(item)).ToList();
			return Task.WhenAll(tasks);
		}

		public static Task FoEachAsync<T>(this IEnumerable<T> source, Func<T, Task> action) =>
			Task.WhenAll(source.Select(action));
		
		public static KeyValuePair<TKey, TValue> GetEntry<TKey, TValue> (this IDictionary<TKey, TValue> dictionary, TKey key)
		{
			return new KeyValuePair<TKey, TValue>(key, dictionary[key]);
		}
		
		

	}
}
