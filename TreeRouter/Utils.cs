using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
		
	}
}
