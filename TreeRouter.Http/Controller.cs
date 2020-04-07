using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using KazooDotNet.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace TreeRouter.Http
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

		protected HttpContext Context { get; set; }
		protected HttpRequest Request => Context.Request;
		protected Microsoft.AspNetCore.Http.HttpResponse Response => Context.Response;
		protected ISession Session => Context.Session;
		protected bool SessionAvailable => Context.Features.Get<ISessionFeature>() != null;
		protected RequestDictionary RouteVars { get; private set; }
		protected bool IsJsonController { get; set; }

		private NestedParams _nestedParams;

		protected bool IsForm => _nestedParams.IsForm;
		protected bool IsJson => _nestedParams.IsJson;
		protected NestedDictionary Form => _nestedParams.Form;
		protected NestedDictionary Json => _nestedParams.Json;
		protected NestedDictionary Query => _nestedParams.Query;
		protected NestedDictionary Params => _nestedParams.Params;

		protected JsonSerializerOptions JsonSettings { get; set; } = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};

		protected string RequestMethod
		{
			get
			{
				var method = Request.Method.ToLower();
				if (method == "post" && IsForm && Form.ContainsKey("_method") &&
				    !string.IsNullOrEmpty(Form["_method"].ToString()))
					return Request.Form["_method"].ToString().ToLower();
				return Request.Method.ToLower();
			}
		}

		protected bool AcceptsJson => Request.Headers.ContainsKey("Accept") &&
		                              Request.Headers["Accept"].ToString().Contains("json");


		public async Task Route(Request routerRequest)
		{
			RouteVars = routerRequest.RouteVars;
			if (!RouteVars.ContainsKey("action"))
				throw new Exception("Route vars do not contain `action`, be sure to set a default in your route.");

			var type = GetType();
			var method = type.GetMethod(RouteVars["action"]);
			// TODO: make this more catcheable
			if (method == null)
				throw new Exception($"`{RouteVars["action"]}` does not exist on `{type.Name}`");
			await Dispatch((HttpContext) routerRequest.Context, method);
		}

		protected async Task Dispatch(HttpContext context, MethodInfo method, params object[] list)
		{
			Context = context;
			_nestedParams = await context.SetupNestedParams();

			if (RouteVars != null)
			{
				if (_nestedParams.ExtraParams == null)
					_nestedParams.ExtraParams = new NestedDictionary();
				foreach (var pair in RouteVars)
					_nestedParams.ExtraParams.Set(pair.Key, pair.Value);
			}

			await BeforeDispatch.Invoke(this);

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

			if (SessionAvailable)
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
						throw new ArgumentException("Can't read HttpResponse stream");
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
					if (IsJsonController)
					{
						var str = JsonSerializer.Serialize(finalResponse, JsonSettings);
						await Response.WriteAsync(str);
					}
					else
					{
						await CleanUp();
						throw new Exception(
							$"Unknown response type from controller: {response.GetType().FullName} (in {GetType().FullName})");	
					}
					
					break;
			}

			await CleanUp();
		}

		private async Task CleanUp()
		{
			await _nestedParams.FormFileCleanup();
		}

		protected Task Dispatch(HttpContext context, string methodName, params object[] vars)
		{
			return Dispatch(context, GetType().GetMethod(methodName), vars);
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

		protected HttpResponse SendJson(object obj)
		{
			var resp = new HttpResponse
			{
				Body = JsonSerializer.Serialize(obj, JsonSettings)
			};
			resp.Headers["Content-Type"] = "application/json";
			return resp;
		}
	}
}
