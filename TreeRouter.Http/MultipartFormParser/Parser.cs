using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TreeRouter.Http.MultipartFormParser
{
	public class Parser
	{
		private static readonly Regex FileMatcher = new Regex(@"filename=""([^""]+)""", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex NameMatcher = new Regex(@"name=""([^""]+)""", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		
		private Stream _body;
		private byte[] _boundaryBytes;
		private Encoding _encoding;
		private byte[] _headerEndingBytes;
		
		public Dictionary<string, List<IFormParameter>> Parameters { get; } = new Dictionary<string, List<IFormParameter>>();

		public long MultiPartFileLimit { get; set; } = 1024000;
		public long MultiPartFieldLimit { get; set; } = 32768;
		public int HeaderLimit { get; set; } = 16384;
		public int BufferSize { get; set; } = 16384;
		public int TempFileLimit { get; set; } = 65536;

		public Parser(Stream body, string boundary, Encoding encoding)
		{
			_body = body;
			_encoding = encoding;
			_boundaryBytes = _encoding.GetBytes(boundary);
			_headerEndingBytes = _encoding.GetBytes("\r\n\r\n");
		}

		public async Task Parse()
		{  
			if (_boundaryBytes.Length * 2 > BufferSize)
				throw new ArgumentException("Buffer size needs to be at least twice boundary length");
			if (_body.CanSeek)
				_body.Seek(0, SeekOrigin.Begin);
			else if (_body.Position != 0)
				throw new ArgumentException("Request stream has been read already and cannot be rewound");

			var bytes = await ReadSection(new byte[] { }, null); 
			while (_body.Position < _body.Length)
			{
				var (headers, leftovers) = await ReadHeaders(bytes);
				bytes = await ReadSection(leftovers, headers);
			}
		}

		private async Task<(Dictionary<string, List<string>>, byte[])> ReadHeaders(byte[] initialBytes)
		{
			var pos = BytePosition(initialBytes, _headerEndingBytes);
			string headerString;
			byte[] leftovers;
			if (pos > -1)
			{
				var finishedBytes = new byte[pos];
				Array.Copy(initialBytes, finishedBytes, pos);
				headerString = _encoding.GetString(finishedBytes);
				leftovers = new byte[initialBytes.Length - pos];
				Array.Copy(initialBytes, pos + 1, leftovers, 0, initialBytes.Length - pos);
			}
			else
			{
				var bytes = new List<byte>(initialBytes);
				var buffer = new byte[BufferSize];
				var skipAhead = initialBytes.Length - 10;
				while (true)
				{
					var readBytes = await _body.ReadAsync(buffer, 0, BufferSize);
					if (readBytes == 0)
						return (null, null);
					bytes.AddRange(buffer);
					var pos2 = BytePosition(bytes, _headerEndingBytes, skipAhead);
					if (pos2 > -1)
					{
						leftovers = bytes.GetRange(pos2 + 1, bytes.Count - pos2).ToArray();
						headerString = _encoding.GetString(bytes.GetRange(0, pos2).ToArray());
						break;
					}
					skipAhead = bytes.Count - 10;
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

		private async Task<byte[]> ReadSection(byte[] initialBytes, IReadOnlyDictionary<string, List<string>> headers)
		{
			string name = null;
			string fileName = null;
			if (headers != null && headers.ContainsKey("Content-Type") && headers["Content-Type"].Count > 0)
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

			using (var ms = new MemoryStream())
			{
				while (true)
				{
					bufferBytes[0] = bufferBytes[1];
					bufferBytes[1] = new byte[BufferSize];
					var readBytes = await _body.ReadAsync(bufferBytes[1], 0, BufferSize);
					if (readBytes == 0)
						break;
					var checkBytes = new byte[bufferBytes[0].Length + bufferBytes[1].Length];
					bufferBytes[0].CopyTo(checkBytes, 0);
					bufferBytes[1].CopyTo(checkBytes, bufferBytes[0].Length);
					var boundaryPosition = BytePosition(checkBytes, _boundaryBytes);
					if (boundaryPosition > -1)
					{
						var len = boundaryPosition - bufferBytes[0].Length;
						var copyToStream = new byte[len];
						Array.Copy(bufferBytes[1], 0, copyToStream, 0, len);
						leftovers = new byte[bufferBytes[1].Length - len];
						Array.Copy(bufferBytes[1], len, leftovers, 0, bufferBytes[1].Length - len);
						break;
					}

					if (fs == null && name != null)
					{
						await ms.WriteAsync(bufferBytes[1], 0, bufferBytes[1].Length);
						if (fileName != null && ms.Length > TempFileLimit)
						{
							fs = File.OpenWrite(Path.GetTempFileName());
							await ms.CopyToAsync(fs);
						} 
						else if (fileName == null && ms.Length > MultiPartFieldLimit)
						{
							throw new ArgumentException($"`{name}` is too long. Max limit is {MultiPartFieldLimit} bytes");
						}
					}
					else if (fs != null)
					{
						await fs.WriteAsync(bufferBytes[1], 0, bufferBytes[1].Length);
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
					param = new FormParameter<IUploadFileParameter>
					{
						Name = name,
						Data = new FileParameter(File.OpenRead(fs.Name))
						{
							ContentType = contentType,
							FileName = fileName
						}
					};
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
				}

				if (name != null)
				{
					if (!Parameters.ContainsKey(name))
						Parameters[name] = new List<IFormParameter>();
					Parameters[name].Add(param);	
				}
				
			}

			return leftovers;
		}

		private int BytePosition(IReadOnlyList<byte> haystack, IReadOnlyList<byte> needle, int haystackIndex = 0)
		{
			if (needle.Count> haystack.Count)
				return -1;
			var needleIndex = 0;
			if (haystackIndex < 0) 
				haystackIndex = 0;
			
			while(haystackIndex < haystack.Count - 1)
			{
				var h = haystack[haystackIndex];
				if (needle[needleIndex] == h)
					needleIndex++;
				else
					needleIndex = 0;
				if (needleIndex == needle.Count)
					return haystackIndex - needle.Count;
				haystackIndex++;
			}
			return -1;
		}
		
	}
	
	
}
