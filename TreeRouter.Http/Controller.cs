using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using TreeRouter;
using TreeRouter.Shared;
using HttpResponse = TreeRouter.Http.HttpResponse;

namespace MaggieSotteroApi.Classes
{
	public class ControllerArgs
	{
		public bool Cancel { get; set; }
	}
	
	public abstract class Controller : IController
	{
		
		public readonly EventEmitter<Controller> BeforeDispatch = 
			new EventEmitter<Controller>(); 
		public readonly EventEmitter<Controller, ControllerArgs> BeforeAction = 
			new EventEmitter<Controller, ControllerArgs>();
		public readonly EventEmitter<Controller> AfterAction = 
			new EventEmitter<Controller>();
		
		private static readonly string[] _formTypes = {"application/x-www-form-urlencoded", "multipart/form-data"};

		private NestedDictionary _form;

		private NestedDictionary _jsonRequest;

		private NestedDictionary _params;

		private NestedDictionary _query;

		public HttpContext Context { get; set; }
		public HttpRequest Request => Context.Request;
		public Microsoft.AspNetCore.Http.HttpResponse Response => Context.Response;
		public ISession Session => Context.Session;
		public RequestDictionary RouteVars { get; set; }

		public NestedDictionary Query
		{
			get
			{
				if (_query != null) return _query;
				_query = new NestedDictionary();
				foreach (var pair in Context.Request.Query)
					_query.Set(pair.Key, pair.Value.ToString());
				return _query;
			}
		}

		public string RequestMethod
		{
			get
			{
				var method = Request.Method.ToLower();
				if (method == "post" && IsForm &&
				    Request.Form.ContainsKey("_method") &&
				    !string.IsNullOrEmpty(Request.Form["_method"]))
					return Request.Form["_method"].ToString().ToLower();
				return Request.Method.ToLower();
			}
		}

		public bool IsForm => _formTypes.Contains(Request.ContentType?.ToLower());
		public bool IsJson => Request.ContentType?.ToLower().Contains("json") ?? false;

		public bool AcceptsJson => Request.Headers.ContainsKey("Accept") &&
		                              Request.Headers["Accept"].ToString().Contains("json");

		public NestedDictionary Params => _params ?? MakeParams();
		public NestedDictionary JsonRequest => _jsonRequest ?? (_jsonRequest = MakeJsonRequest());
		public NestedDictionary Form => _form ?? ProcessForm();

		public async Task Route(Request routerRequest)
		{
			RouteVars = routerRequest.RouteVars;
			if (!RouteVars.ContainsKey("action"))
				throw new Exception("Route vars do not contain `action`, be sure to set a default in your route.");

			var type = GetType();
			var method = type.GetMethod(RouteVars["action"]);
			if (method == null)
				throw new Exception($"`{RouteVars["action"]}` does not exist on `{type.Name}`");

			await Dispatch((HttpContext) routerRequest.Context, method);
		}

		private NestedDictionary MakeParams()
		{
			// Last one gets precedence
			var dicts = new[] {Query, Form, JsonRequest};
			_params = new NestedDictionary();
			foreach (var dict in dicts)
			foreach (var pair in dict)
				_params[pair.Key] = pair.Value;
			foreach (var pair in RouteVars)
				_params[pair.Key] = pair.Value;
			return _params;
		}

		protected async Task Dispatch(HttpContext context, MethodInfo method, params object[] list)
		{
			Context = context;
			await BeforeDispatch.Invoke(this);
			
			var type = GetType();
			
			var ca = new ControllerArgs();
			await BeforeAction.Invoke(this, ca);
			if (ca.Cancel) return;
			
			var mParams = method.GetParameters();
			// If no parameters are passed, try to infer them.
			if (list.Length == 0 && mParams.Length > 0)
			{
				var newList = new object[mParams.Length];
				var i = 0;
				foreach (var mParam in mParams)
				{
					if (Params.ContainsKey(mParam.Name))
						newList[i] = Params.Get(mParam.ParameterType, mParam.Name);
					else
						newList[i] = Type.Missing;
					i++;
				}

				list = newList;
			}
			else if (list.Length < mParams.Length)
			{
				var newList = new object[mParams.Length];
				for (var i = 0; i < mParams.Length; i++)
					newList[i] = i > list.Length - 1 ? Type.Missing : list[i];
				list = newList;
			}

			var response = await Utils.ExtractRefTask(method.Invoke(this, list));
			if (response is HttpResponse hr)
				Response.ContentType = hr.ContentType;

			if (string.IsNullOrWhiteSpace(Response.ContentType))
				Response.ContentType = "text/html; charset=utf-8";

			await AfterAction.Invoke(this);

			if (Session.IsAvailable)
				await Session.CommitAsync();

			object finalResponse; 
			if (response is HttpResponse http)
			{
				foreach (var pair in http.Headers)
					Response.Headers[pair.Key] = pair.Value;
				Response.StatusCode = http.StatusCode;
				finalResponse = http.Body;
			}
			else
			{
				finalResponse = response;
			}
			
			switch (finalResponse)
			{
				case Stream stream:
					if (!stream.CanRead)
						throw new ArgumentException("Can't read stream");
					if (stream.CanSeek)
						stream.Seek(0, SeekOrigin.Begin);
					await stream.CopyToAsync(Response.Body, 51200); // Copy in 50KB chunks
					stream.Dispose();
					break;
				case string str:
					await Response.WriteAsync(str);
					break;
				case byte[] bytea:
					Response.Body = new MemoryStream(bytea); 
					break;
				case null:
					// Do nothing
					break;
				default:
					throw new Exception(
						$"Unknown response type from controller: {response.GetType().FullName} (in {GetType().FullName})");
			}
		}

		private NestedDictionary MakeJsonRequest()
		{
			var resp = new NestedDictionary();
			if (!IsJson) return resp;
			try
			{
				return LoopObject(resp, JsonConvert.DeserializeObject<JObject>(JsonString(), JsonSettings()));
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return resp;
			}
		}

		private NestedDictionary LoopObject(NestedDictionary dict, JObject obj)
		{
			if (obj == null)
				return dict;
			foreach (var pair in obj)
				switch (pair.Value)
				{
					case JArray arr:
						if (arr.FirstOrDefault() is JObject)
						{
							var subArray = new NestedDictionary[arr.Count];
							for (var i = 0; i < arr.Count; i++)
							{
								var subDict = new NestedDictionary();
								subArray[i] = subDict;
								LoopObject(subDict, arr[i] as JObject);
							}

							dict.Set(pair.Key, subArray);
						}
						else
						{
							dict.Set(pair.Key, arr.Select(Convert.ToString).ToArray());
						}

						break;
					case JObject newObj:
						var newDict = new NestedDictionary();
						dict.Set(pair.Key, newDict);
						LoopObject(newDict, newObj);
						break;
					default:
						dict.Set(pair.Key, pair.Value.ToString());
						break;
				}
			return dict;
		}

		private string JsonString()
		{
			if (!IsJson)
				throw new ArgumentException($"Invalid request type for json: {Request.ContentType}");
			using (var reader = new StreamReader(Request.Body, Encoding.UTF8, true, 1024, true))
			{
				return reader.ReadToEnd();
			}
		}

		protected Task Dispatch(HttpContext context, string methodName, params object[] vars)
		{
			return Dispatch(context, GetType().GetMethod(methodName), vars);
		}

		private NestedDictionary ProcessForm()
		{
			var dict = new NestedDictionary();
			IFormCollection rForm;
			try
			{
				rForm = Request.Form;
			}
			catch (InvalidOperationException)
			{
				return dict;
			}

			foreach (var key in rForm.Keys)
				dict.Set(key, rForm[key].LastOrDefault());
			return _form = dict;
		}

		protected HttpResponse Redirect(string path, int code = 303)
		{
			var resp = new HttpResponse
			{
				StatusCode = code
			};
			resp.Headers["Location"] = path;
			return resp;
		}

		protected HttpResponse Json(object obj, PreserveReferencesHandling handling = PreserveReferencesHandling.None)
		{
			var resp = new HttpResponse
			{
				Body = JsonConvert.SerializeObject(obj, JsonSettings(handling))
			};
			resp.Headers["Content-Type"] = "application/json";
			resp.Headers["Access-Control-Allow-Origin"] = "*";
			return resp;
		}

		protected JsonSerializerSettings JsonSettings(
			PreserveReferencesHandling handling = PreserveReferencesHandling.None)
		{
			return new JsonSerializerSettings
			{
				ContractResolver = new CamelCasePropertyNamesContractResolver(),
				PreserveReferencesHandling = handling
			};
		}
		
	}
}
