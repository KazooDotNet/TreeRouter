using System;
using System.Threading.Tasks;
using MessagePack;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;

namespace TreeRouter.Http
{
	public static class ObjectExtensions
	{
		public static void Set<T>(this ISession session, string key, T obj)
		{
			if (obj == null)
				session.Remove(key);
			else
				session.Set(key, MessagePackSerializer.Serialize(obj));
		}

		public static T Get<T>(this ISession session, string key) where T : class
		{
			var bin = session.Get(key);
			return bin == null ? null : MessagePackSerializer.Deserialize<T>(bin);
		}

		public static bool TryGetValue<T>(this ISession session, string key, out T outThing) where T : class
		{
			var result = session.TryGetValue(key, out var bin);
			outThing = result ? MessagePackSerializer.Deserialize<T>(bin) : null;
			return result;
		}

		public static void Append<T>(this IResponseCookies cookies, string key, T obj, CookieOptions opts = null)
			where T : class
		{
			if (obj == null)
				cookies.Delete(key);
			else
				cookies.Append(key, Convert.ToBase64String(MessagePackSerializer.Serialize(obj)),
					opts ?? new CookieOptions());
		}

		public static T Get<T>(this IRequestCookieCollection cookies, string key) where T : class =>
			MessagePackSerializer.Deserialize<T>(Convert.FromBase64String(cookies[key]));

		public static bool TryGetValue<T>(this IRequestCookieCollection cookies, string key, out T result)
			where T : class
		{
			result = null;
			if (!cookies.ContainsKey(key)) return false;
			result = cookies.Get<T>(key);
			return true;
		}

		public static async Task<T> GetAsync<T>(this IDistributedCache cache, string key) where T : class
		{
			var bin = await cache.GetAsync(key);
			return bin == null ? null : MessagePackSerializer.Deserialize<T>(bin);
		}


		public static Task SetAsync<T>(this IDistributedCache cache, string key, T value,
			DistributedCacheEntryOptions opts = null) where T : class
		{
			var bin = MessagePackSerializer.Serialize(value);
			if (opts == null) opts = new DistributedCacheEntryOptions();
			return cache.SetAsync(key, bin, opts);
		}
	}
}
