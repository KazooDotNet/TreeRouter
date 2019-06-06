using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using KazooDotNet.Utils;

namespace TreeRouter.Http.MultipartFormParser
{
	public class Parser
	{
		private static readonly Regex FileMatcher = new Regex(@"filename=""([^""]+)""", RegexOptions.Compiled);
		private static readonly Regex NameMatcher = new Regex(@"name=""([^""]+)""", RegexOptions.Compiled);
		
		private readonly Stream _body;
		private readonly byte[] _boundaryBytes;
		private readonly byte[] _boundaryEndBytes;
		private readonly byte[] _lrBytes;
		private readonly byte[] _headerEndingBytes;
		private readonly byte[] _endBytes;
		private readonly Encoding _encoding;
		
		
		public Dictionary<string, List<IFormParameter>> Parameters { get; } = new Dictionary<string, List<IFormParameter>>();

		public long MultiPartFileLimit { get; set; } = 1024000;
		public long MultiPartFieldLimit { get; set; } = 32768;
		public int HeaderLimit { get; set; } = 16384;
		public int BufferSize { get; set; } = 8192;
		public int TempFileLimit { get; set; } = 65536;

		public Parser(Stream body, string boundary, Encoding encoding)
		{
			_body = body;
			_encoding = encoding;
			_boundaryBytes = _encoding.GetBytes("--" + boundary);
			_headerEndingBytes = _encoding.GetBytes("\r\n\r\n");
			_lrBytes = _encoding.GetBytes("\r\n");
			_endBytes = _encoding.GetBytes("--");
		}

		public async Task Parse(CancellationToken token = default)
		{  
			if (_boundaryBytes.Length * 2 > BufferSize)
				throw new ArgumentException("Buffer size needs to be at least twice boundary (in bytes)");
			if (_body.CanSeek)
				_body.Seek(0, SeekOrigin.Begin);
			else if (_body.Position != 0)
				throw new ArgumentException("Request stream has been read already and cannot be rewound");
			var bytes = await ReadSection(new byte[] { }, null, token); 
			while (_body.Position < _body.Length)
			{
				var (headers, leftovers) = await ReadHeaders(bytes, token);
				bytes = await ReadSection(leftovers, headers, token);
			}
		}

		private async Task<(Dictionary<string, List<string>>, byte[])> ReadHeaders(byte[] initialBytes, CancellationToken token)
		{
			var pos = initialBytes.SequenceSearch(_headerEndingBytes);
			string headerString;
			byte[] leftovers;
			if (pos > -1)
			{
				var finishedBytes = new byte[pos];
				Array.Copy(initialBytes, finishedBytes, pos);
				headerString = _encoding.GetString(finishedBytes);
				leftovers = new byte[initialBytes.Length - pos];
				Array.Copy(initialBytes, pos + _headerEndingBytes.Length, leftovers, 0, initialBytes.Length - pos - _headerEndingBytes.Length);
			}
			else
			{
				// TODO: make sure this works
				var bytes = new List<byte>(initialBytes);
				var buffer = new byte[BufferSize];
				var skipAhead = initialBytes.Length - _boundaryBytes.Length;
				while (true)
				{
					var readBytes = await _body.ReadAsync(buffer, 0, BufferSize, token);
					if (readBytes == 0)
						return (null, null);
					bytes.AddRange(buffer);
					var pos2 = bytes.SequenceSearch(_headerEndingBytes, skipAhead);
					if (pos2 > -1)
					{
						leftovers = bytes.GetRange(pos2 + 1, bytes.Count - pos2).ToArray();
						headerString = _encoding.GetString(bytes.GetRange(0, pos2).ToArray());
						break;
					}
					skipAhead = bytes.Count - _boundaryBytes.Length;
				}
			}

			var headers = new Dictionary<string, List<string>>();
			var parts = headerString.Split(new [] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var part in parts)
			{
				if (!part.Contains(":")) continue;
				var pair = part.Split(new[] { ':' }, 2);
				var key = pair[0].Trim();
				if (!headers.ContainsKey(key))
					headers[key] = new List<string>();
				headers[key].Add(pair[1].Trim());
			}

			return (headers, leftovers);
		}

		private async Task<byte[]> ReadSection(byte[] initialBytes, IReadOnlyDictionary<string, List<string>> headers, CancellationToken token)
		{
			string name = null;
			string fileName = null;
			if (headers != null && headers.ContainsKey("Content-Disposition") && headers["Content-Disposition"].Count > 0)
			{
				var cd = headers["Content-Disposition"][0];
				if (cd.Contains("form-data"))
				{
					var fileMatch = FileMatcher.Match(cd);
					var nameMatch = NameMatcher.Match(cd);
					if (fileMatch.Success)
						fileName = fileMatch.Groups[1].Value;
					if (nameMatch.Success)
						name = nameMatch.Groups[1].Value;
				}
			}
			
			var bufferBytes = new byte[2][];
			bufferBytes[1] = initialBytes;
			FileStream fs = null;
			var leftovers = new byte[0];
			var ms = new MemoryStream();
			await ms.WriteAsync(initialBytes, 0, initialBytes.Length, token);
			while (true)
			{
				bufferBytes[0] = bufferBytes[1];
				bufferBytes[1] = new byte[BufferSize];
				var readBytes = await _body.ReadAsync(bufferBytes[1], 0, BufferSize, token);
				if (readBytes == 0)
					break;
				var checkBytes = new byte[bufferBytes[0].Length + bufferBytes[1].Length];
				bufferBytes[0].CopyTo(checkBytes, 0);
				bufferBytes[1].CopyTo(checkBytes, bufferBytes[0].Length);
				var boundaryPosition = checkBytes.SequenceSearch(_boundaryBytes);
				if (boundaryPosition > -1)
				{
					var beginPos = boundaryPosition + _boundaryBytes.Length;
					var len = checkBytes.Length - beginPos;
					var writePos = beginPos - bufferBytes[0].Length;
					if (writePos >= 0)
					{
						if (fs != null)
							await fs.WriteAsync(bufferBytes[1], 0, writePos, token);
						else
							await ms.WriteAsync(bufferBytes[1], 0, writePos, token);
					}
					leftovers = new byte[len];
					Array.Copy(checkBytes, beginPos, leftovers, 0, len);
					break;
				}

				if (fs == null && name != null)
				{
					await ms.WriteAsync(bufferBytes[1], 0, bufferBytes[1].Length, token);
					if (fileName != null && ms.Length > TempFileLimit)
					{
						fs = File.OpenWrite(Path.GetTempFileName());
						await fs.WriteAsync(ms.ToArray(), 0, (int) ms.Length, token);
					} 
					else if (fileName == null && ms.Length > MultiPartFieldLimit)
					{
						throw new ArgumentException($"`{name}` is too long. Max limit is {MultiPartFieldLimit} bytes");
					}
				}
				else if (fs != null)
				{
					await fs.WriteAsync(bufferBytes[1], 0, bufferBytes[1].Length, token);
				}

				if (boundaryPosition > -1)
					break;
			}
			
			IFormParameter param;
			string contentType = null;
			if (fileName != null)
			{
				headers.TryGetValue("Content-Type", out var ct);
				if (ct != null && ct.Count > 0)
					contentType = ct[0];
			}
			if (fs != null)
			{
				fs.Close();
				param = new FormParameter<IUploadFileParameter>
				{
					Name = name,
					Data = new FileParameter(File.OpenRead(fs.Name))
					{
						ContentType = contentType,
						FileName = fileName
					}
				};
				fs.Dispose();
				ms.Dispose();
			}
			else if (fileName != null)
			{
				ms.Seek(0, SeekOrigin.Begin);
				param = new FormParameter<IUploadFileParameter>
				{
					Name = name,
					Data = new MemoryParameter(ms)
					{
						ContentType = contentType,
						FileName = fileName
					}
				};
			}
			else
			{
				ms.Seek(0, SeekOrigin.Begin);
				param = new FormParameter<string>
				{
					Name = name,
					Data = new StreamReader(ms).ReadToEnd()
				};
				ms.Dispose();
			}

			if (name != null)
			{
				if (!Parameters.ContainsKey(name))
					Parameters[name] = new List<IFormParameter>();
				Parameters[name].Add(param);	
			}
			else
			{
				ms.Dispose();
			}
			
			return leftovers;
		}

		
		
	}
	
	
}
