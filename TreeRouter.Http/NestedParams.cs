using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using KazooDotNet.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TreeRouter.Http
{
    public class NestedParams
    {   
        
        private HttpContext _context;
        private NestedDictionary _query;

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
        
        
        public NestedParams(HttpContext context)
        {
            _context = context;
        }
        
        public async Task ProcessForm()
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
                if (body.CanSeek)
                    body.Position = 0;
                var reader = new MultipartReader(matches.Groups[1].Value, body);
                MultipartSection section;
                while ((section = await reader.ReadNextSectionAsync()) != null)
                {
                    var dispo = section.GetContentDispositionHeader();
                    if (dispo.IsFileDisposition())
                    {
                        var fileSection = section.AsFileSection();
                        IFormFile formFile;
                        using (var fs = fileSection.FileStream)
                        {
                            // TODO: why is fs.Length always 0?
                            if (fs.Length > 102400)
                            {
                                // TODO: clean up temp files after use
                                var path = Path.GetTempFileName();
                                using (var file = File.Open(path, FileMode.Open, FileAccess.Write))
                                {
                                    await fs.CopyToAsync(file, 32768);
                                }
                                var roFile = File.Open(path, FileMode.Open, FileAccess.Read);
                                formFile = new FormFile(roFile, 0, roFile.Length, fileSection.Name, fileSection.FileName);
                            }
                            else
                            {
                                var stream = new MemoryStream();
                                await fs.CopyToAsync(stream);
                                formFile = new FormFile(stream, 0, stream.Length, fileSection.Name, fileSection.FileName);
                            }    
                        }
                        Form.Set(fileSection.Name, formFile);
                    }
                    else if (dispo.IsFormDisposition())
                    {
                        var formSection = section.AsFormDataSection();
                        Form.Set(formSection.Name, await formSection.GetValueAsync());
                    }
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