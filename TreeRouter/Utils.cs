using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using MessagePack;
using MessagePack.Resolvers;

namespace TreeRouter
{
	public static class Utils
	{
		public static string JoinPath(params string[] list)
		{
			var parts = list.ToList();
			parts.RemoveAll(p => p == null);
			return string.Join("/", parts.Select(p => p.Trim('/').Trim()));
		}

		public static RouteOptions MergeOptions(params RouteOptions[] vars)
		{
			var ro = new RouteOptions();
			foreach (var option in vars)
			{
				if (option.ActionHandler != null) ro.ActionHandler = option.ActionHandler;
				if (option.ClassHandler != null) ro.ClassHandler = option.ClassHandler;
				if (option.Methods != null) ro.Methods = option.Methods;
				if (option.Path != null) ro.Path = option.Path;
				ro.Defaults = MergeDefaults(ro.Defaults, option.Defaults);
				ro.Constraints = MergeConstraints(ro.Constraints, option.Constraints);
			}

			return ro;
		}


		public static string ComputeHash(object instance)
		{
			var hasher = new MD5CryptoServiceProvider();
			var bytes = MessagePackSerializer.Serialize(instance, ContractlessStandardResolver.Instance);
			hasher.ComputeHash(bytes);
			return Convert.ToBase64String(hasher.Hash);
		}

		public static Defaults MergeDefaults(params Defaults[] list)
		{
			var merged = new Defaults();
			foreach (var t in list)
				t?.ToList().ForEach(pair => merged[pair.Key] = pair.Value);
			return merged;
		}

		public static Constraints MergeConstraints(params Constraints[] list)
		{
			var merged = new Constraints();
			foreach (var t in list)
				t?.ToList().ForEach(pair => merged[pair.Key] = pair.Value);
			return merged;
		}

		public static async Task<object> ExtractRefTask(object obj)
		{
			try
			{
				switch (obj)
				{
					case Task task:
						await task;
						return task.GetType().GetProperty("Result")?.GetValue(task);
					default:
						return obj;
				}
			}
			catch (AggregateException e)
			{
				ExceptionDispatchInfo.Capture(e.InnerExceptions.First()).Throw();
			}

			return default;
		}

		public static async Task ExtractVoidTask(object obj)
		{
			try
			{
				if (obj is Task task)
					await task;
			}
			catch (AggregateException e)
			{
				ExceptionDispatchInfo.Capture(e.InnerExceptions.First()).Throw();
			}
		}


		public static async Task<T?> ExtractValTask<T>(object obj) where T : struct
		{
			try
			{
				switch (obj)
				{
					case Task<T> objTask: return await objTask;
					case Task task:
						await task;
						return null;
					default: return (T?) obj;
				}
			}
			catch (AggregateException e)
			{
				ExceptionDispatchInfo.Capture(e.InnerExceptions.First()).Throw();
			}

			return default;
		}
	}
}
