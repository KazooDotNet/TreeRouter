using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using KazooDotNet.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TreeRouter.Http.MultipartFormParser;

namespace TreeRouter.Http
{
    public class NestedParams
    {   
        
        private HttpContext _context;
        private NestedDictionary _query;
        public List<FileStream> TempFiles { get; private set; } 

        public bool IsForm => _context.Request.ContentType?.Contains("form") ?? false;
        public bool IsJson => _context.Request.ContentType?.Contains("json") ?? false;
        
        public NestedDictionary Form { get; private set; }
        public bool FormProcessed { get; set; }
        
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

        public JsonSerializerSettings JsonSettings { get; set; }
        
        private NestedDictionary _json;
        private FormOptions _formOptions;

        public NestedDictionary Json
        {
            get
            {
                if (JsonProcessed) return _json;
                ProcessJson();
                return _json;
            }
        }
        
        public bool JsonProcessed { get; set; }
        
        
        public NestedParams(HttpContext context, FormOptions formOptions)
        {
            _context = context;
            _formOptions = formOptions;
        }
        
        public async Task ProcessForm(CancellationToken token = default)
        {
            if (FormProcessed)
                return;
			Form = new NestedDictionary();
            if (!IsForm)
                return;
            _context.Request.Headers.TryGetValue("Content-Type", out var cts);
            var ct = cts.FirstOrDefault()?.ToLowerInvariant();
            if (ct == null)
                return;

            if (ct.Contains("multipart/form-data"))
            {
                var boundaryRegex = new Regex(@"boundary\s*=\s*([^;]+)");
                var matches = boundaryRegex.Match(ct);
                if (!matches.Success)
                    return;
                
                var body = _context.Request.Body;
                // TODO: get encoding from Content-Type or default to UTF8
                var reader = new Parser(body, matches.Groups[1].Value, Encoding.UTF8);
                await reader.Parse();
                foreach (var paramList in reader.Parameters.Values)
                {
	                var param = paramList[paramList.Count - 1];
	                Form.Set(param.Name, param.Data);
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

        public Task FormFileCleanup()
        {
            if (TempFiles == null)
                return Task.CompletedTask;
            foreach (var tf in TempFiles)
            {
                if (File.Exists(tf.Name))
                    File.Delete(tf.Name);
                tf.Dispose();
            }
            return Task.CompletedTask;
        }
        
        
        // TODO: make this preserve JSON types
        private void LoopObject(NestedDictionary dict, JObject obj)
        {
            if (obj == null)
                return;
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
        }
        
        public void ProcessJson()
        {
            if (JsonProcessed)
                return;
            
            _json = new NestedDictionary();
            if (!(_context.Request.ContentType?.Contains("json") ?? false))
            {
                JsonProcessed = true;
                return;
            }
                
            try
            {
                // TODO: use encoding set in headers
                using (var reader = new StreamReader(_context.Request.Body, Encoding.UTF8, true, 10240, true))
                {
                    var @string = reader.ReadToEnd();
                    LoopObject(_json, JsonConvert.DeserializeObject<JObject>(@string, JsonSettings));    
                }
                JsonProcessed = true;

            }
            catch (Exception e)
            {
                // TODO: add logger?
                Console.WriteLine(e);
            }
        }
        
    }
}
