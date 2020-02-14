using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using KazooDotNet.Utils;
using Microsoft.AspNetCore.Http;
using TreeRouter.Http.MultipartForm;

namespace TreeRouter.Http
{
	public class NestedParams
	{
		private readonly HttpContext _context;
		private NestedDictionary _query;

		public bool IsForm => _context.Request.ContentType?.Contains("form") ?? false;
		public bool IsJson => _context.Request.ContentType?.Contains("json") ?? false;

		public NestedDictionary Form { get; private set; }
		public bool FormProcessed { get; set; }

		private List<FileParameter> _tempFiles = null;

		public NestedDictionary Query
		{
			get
			{
				if (_query != null) return _query;
				_query = new NestedDictionary();
				foreach (var pair in _context.Request.Query)
					_query.Set(pair.Key, pair.Value.ToString());
				return _query;
			}
		}

		public NestedDictionary ExtraParams { get; set; }

		private NestedDictionary _params;

		public NestedDictionary Params
		{
			get
			{
				if (_params != null) return _params;
				var nd = new NestedDictionary();
				foreach (var pair in Query)
					nd.Set(pair.Key, pair.Value);
				if (IsJson)
				{
					foreach (var pair in Json)
						nd.Set(pair.Key, pair.Value);
				}
				else if (IsForm && FormProcessed)
				{
					foreach (var pair in Form)
						nd.Set(pair.Key, pair.Value);
				}

				if (ExtraParams != null)
					foreach (var pair in ExtraParams)
						nd.Set(pair.Key, pair.Value);
				return _params = nd;
			}
		}

		public JsonSerializerOptions JsonSettings { get; set; }

		private readonly FormOptions _formOptions;

		public NestedDictionary Json { get; private set; }

		public bool JsonProcessed { get; private set; }


		public NestedParams(HttpContext context, FormOptions options)
		{
			_context = context;
			_formOptions = options;
		}

		public async Task ProcessForm(CancellationToken token = default)
		{
			if (FormProcessed)
				return;
			Form = new NestedDictionary();
			if (!IsForm)
				return;
			_context.Request.Headers.TryGetValue("Content-Type", out var cts);
			var ct = cts.FirstOrDefault();
			if (ct == null)
				return;

			if (ct.Contains("multipart/form-data"))
			{
				var boundaryRegex = new Regex(@"boundary\s*=\s*([^;]+)");
				var matches = boundaryRegex.Match(ct);
				if (!matches.Success)
					return;

				var body = _context.Request.Body;
				// TODO: get encoding from Content-Type or fallback to default
				var reader = new Parser(body, matches.Groups[1].Value, Encoding.Default, _formOptions);
				await reader.Parse(token);
				foreach (var paramList in reader.Parameters.Values)
				{
					var param = paramList[paramList.Count - 1];
					Form.Set(param.Name, param.Data);
					if (!(param.Data is FileParameter fp)) continue;
					if (_tempFiles == null)
						_tempFiles = new List<FileParameter>();
					_tempFiles.Add(fp);
				}

				FormProcessed = true;
				return;
			}

			try
			{
				var rForm = _context.Request.Form;
				foreach (var key in rForm.Keys)
					Form.Set(key, rForm[key].LastOrDefault());
				FormProcessed = true;
			}
			catch (InvalidOperationException)
			{
				// TODO: report malformed errors somehow
			}
		}

		public Task ProcessAll() => Task.WhenAll(ProcessJson(), ProcessForm());

		public Task FormFileCleanup()
		{
			if (_tempFiles == null)
				return Task.CompletedTask;
			foreach (var tf in _tempFiles)
			{
				tf.File.Close();
				if (File.Exists(tf.File.Name))
					File.Delete(tf.File.Name);
				tf.File.Dispose();
			}

			return Task.CompletedTask;
		}
		
		private void LoopObject(NestedDictionary dict, JsonElement? jsonObj)
		{
			if (jsonObj == null || jsonObj.Value.ValueKind != JsonValueKind.Object)
				return;
			var obj = jsonObj.Value;

			foreach (var prop in obj.EnumerateObject())
				switch (prop.Value.ValueKind)
				{
					case JsonValueKind.Array:
						var subList = new List<object>();
						foreach (var arrVal in prop.Value.EnumerateArray())
						{
							if (arrVal.ValueKind == JsonValueKind.Object)
							{
								var subDict = new NestedDictionary();
								subList.Add(subDict);
								LoopObject(subDict, arrVal);
							}
							else
							{
								subList.Add(GetJsonValue(arrVal));
							}
						}
						dict[prop.Name] = subList;
						break;
					case JsonValueKind.Object:
						var newDict = new NestedDictionary();
						dict.Set(prop.Name, newDict);
						LoopObject(newDict, prop.Value);
						break;
					default:
						dict.Set(prop.Name, GetJsonValue(prop.Value));
						break;
				}
		}

		public async Task ProcessJson()
		{
			if (JsonProcessed)
				return;

			Json = new NestedDictionary();
			if (!(_context.Request.ContentType?.Contains("json") ?? false))
			{
				JsonProcessed = true;
				return;
			}
			
			// TODO: use encoding set in headers
			using (var reader = new StreamReader(_context.Request.Body, Encoding.UTF8, true, 10240, true))
			{
				var @string = await reader.ReadToEndAsync();
				LoopObject(Json, JsonSerializer.Deserialize<JsonElement>(@string, JsonSettings));
			}
			JsonProcessed = true;
		}

		private object GetJsonValue(JsonElement? ele)
		{
			if (ele == null)
				return null;
			var j = ele.Value;
			switch (j.ValueKind)
			{
				case JsonValueKind.Number:
					return j.ToString(); // Seems like they store it internally as a string anyway
				case JsonValueKind.False:
					return false;
				case JsonValueKind.True:
					return true;
				case JsonValueKind.String:
					return j.GetString();
				case JsonValueKind.Undefined:
				case JsonValueKind.Null:
					return null;
			}

			return null;
		}
	}
}
