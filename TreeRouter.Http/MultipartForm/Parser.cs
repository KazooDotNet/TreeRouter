using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TreeRouter.Http.MultipartForm
{
	public class Parser
	{
		private static readonly Regex FileMatcher = new Regex(@"filename=""([^""]+)""", RegexOptions.Compiled);
		private static readonly Regex NameMatcher = new Regex(@"name=""([^""]+)""", RegexOptions.Compiled);

		private readonly Stream _body;
		private readonly byte[] _boundaryBytes;
		private readonly byte[] _lineEndingBytes;
		private readonly IReadOnlyList<byte>[] _endBoundaryBytes;
		private readonly byte[] _headerEndingBytes;

		public Dictionary<string, List<IFormParameter>> Parameters { get; } =
			new Dictionary<string, List<IFormParameter>>();

		public long MultiPartFileLimit { get; set; } = 1024000;
		public long MultiPartFieldLimit { get; set; } = 32768;
		public int HeaderLimit { get; set; } = 16384;
		public int BufferSize { get; set; } = 8192;
		public int TempFileLimit { get; set; } = 65536;

		public Parser(Stream body, string boundary, Encoding encoding)
		{
			_body = body;
			var encoding1 = encoding;
			_boundaryBytes = encoding1.GetBytes("--" + boundary);
			_headerEndingBytes = encoding1.GetBytes("\r\n\r\n");
			_lineEndingBytes = encoding1.GetBytes("\r\n");
			_endBoundaryBytes = new IReadOnlyList<byte>[] {_lineEndingBytes, encoding1.GetBytes("--")};
		}

		public Parser(Stream body, string boundary, Encoding encoding, FormOptions options) : this(body, boundary,
			encoding)
		{
			if (options == null) return;
			MultiPartFileLimit = options.MultiPartFileLimit;
			MultiPartFieldLimit = options.MultiPartFieldLimit;
			HeaderLimit = options.HeaderLimit;
			BufferSize = options.BufferSize;
			TempFileLimit = options.TempFileLimit;
		}

		public async Task Parse(CancellationToken token = default)
		{
			if (_boundaryBytes.Length > BufferSize)
				throw new ArgumentException("Buffer size needs to be at least the boundary size");
			if (_body.CanSeek)
				_body.Seek(0, SeekOrigin.Begin);
			byte[] bytes;
			bool finished;
			(bytes, finished) = await ReadUntilBoundary(_boundaryBytes, needle2s: _endBoundaryBytes, token: token);
			while (!finished)
			{
				Dictionary<string, List<string>> headers;
				byte[] leftovers;
				(headers, leftovers, finished) = await ReadHeaders(bytes, token);
				if (finished)
					return;
				(bytes, finished) = await ReadSection(leftovers, headers, token);
			}
		}

		private async Task<(Dictionary<string, List<string>>, byte[], bool)> ReadHeaders(byte[] initialBytes,
			CancellationToken token)
		{
			var headerStream = new MemoryStream();
			var (leftovers, finished) =
				await ReadUntilBoundary(initialBytes, _headerEndingBytes, headerStream, token: token);
			while (leftovers == null && !finished)
			{
				(leftovers, finished) = await ReadUntilBoundary(_headerEndingBytes, headerStream, token: token);
			}

			if (finished)
				return (null, leftovers, true);
			headerStream.Seek(0, SeekOrigin.Begin);
			var headerString = await new StreamReader(headerStream).ReadToEndAsync();
			var headers = new Dictionary<string, List<string>>();
			var parts = headerString.Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
			foreach (var part in parts)
			{
				if (!part.Contains(":")) continue;
				var pair = part.Split(new[] {':'}, 2);
				var key = pair[0].Trim();
				if (!headers.ContainsKey(key))
					headers[key] = new List<string>();
				headers[key].Add(pair[1].Trim());
			}

			return (headers, leftovers, false);
		}

		private async Task<(byte[], bool)> ReadSection(byte[] initialBytes,
			IReadOnlyDictionary<string, List<string>> headers,
			CancellationToken token)
		{
			string name = null;
			string fileName = null;
			if (headers != null && headers.ContainsKey("Content-Disposition") &&
			    headers["Content-Disposition"].Count > 0)
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

			var ms = new MemoryStream();
			FileStream fs = null;
			Stream mainStream = ms;
			var (leftovers, finished) =
				await ReadUntilBoundary(initialBytes, _boundaryBytes, mainStream, _endBoundaryBytes, true, token);
			while (leftovers == null && !finished)
			{
				if (fs == null && fileName != null && ms.Length > TempFileLimit)
				{
					fs = File.OpenWrite(Path.GetTempFileName());
					await fs.WriteAsync(ms.ToArray(), 0, (int) ms.Length, token);
					mainStream = fs;
				}

				if (fileName != null && mainStream.Position > MultiPartFileLimit)
				{
					if (fs != null)
					{
						fs.Close();
						File.Delete(fs.Name);
					}

					throw new ArgumentException($"`{name}` is longer than limit: {MultiPartFileLimit}");
				}

				if (fileName == null && ms.Length > MultiPartFieldLimit)
				{
					throw new ArgumentException($"`{name}` is longer than limit: {MultiPartFieldLimit}");
				}

				(leftovers, finished) =
					await ReadUntilBoundary(_boundaryBytes, mainStream, _endBoundaryBytes, true, token);
			}

			IFormParameter param = null;
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
					Headers = headers,
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
					Headers = headers,
					Name = name,
					Data = new MemoryParameter(ms)
					{
						ContentType = contentType,
						FileName = fileName
					}
				};
			}
			else if (name != null)
			{
				ms.Seek(0, SeekOrigin.Begin);
				param = new FormParameter<string>
				{
					Headers = headers,
					Name = name,
					Data = new StreamReader(ms).ReadToEnd()
				};
				ms.Dispose();
			}

			if (param != null && name != null)
			{
				if (!Parameters.ContainsKey(name))
					Parameters[name] = new List<IFormParameter>();
				Parameters[name].Add(param);
			}

			return (leftovers, false);
		}


		private async Task<(byte[] Leftovers, bool Finished)> ReadUntilBoundary(IReadOnlyList<byte> needle1,
			Stream stream = null, IReadOnlyList<byte>[] needle2s = null, bool trimEnding = false,
			CancellationToken token = default)
		{
			var readBytes = await _body.BufferReadAsync(BufferSize, token);
			if (readBytes == null)
				return (Leftovers: null, Finished: true);
			return await ReadUntilBoundary(readBytes, needle1, stream, needle2s, trimEnding, token);
		}

		private async Task<(byte[] Leftovers, bool Finished)> ReadUntilBoundary(IEnumerable<byte> initialBuffer,
			IReadOnlyList<byte> needle1, Stream stream = null, IReadOnlyList<byte>[] needle2s = null,
			bool trimEnding = false,
			CancellationToken token = default)
		{
			var bufferList = new List<byte>(initialBuffer);
			var buffer = new byte[BufferSize];

			IReadOnlyList<byte> readBytes;
			int? leftoverPos = null;
			var (pos, partialMatch) = SequenceSearch(bufferList, needle1);
			if (partialMatch)
			{
				readBytes = await _body.BufferReadAsync(buffer, token);
				if (readBytes == null)
				{
					if (stream != null)
						await stream.WriteAsync(bufferList.ToArray(), 0, bufferList.Count, token);
					return (Leftovers: null, Finished: true);
				}

				bufferList.AddRange(readBytes);
				(pos, _) = SequenceSearch(bufferList, needle1, pos);
			}

			if (pos > -1 && needle2s != null)
			{
				foreach (var needle2 in needle2s)
				{
					var (pos2, partial2) = SequenceSearch(bufferList, needle2, pos + needle1.Count);
					if (partial2)
					{
						readBytes = await _body.BufferReadAsync(buffer, token);
						if (readBytes == null)
						{
							if (stream != null)
								await stream.WriteAsync(bufferList.ToArray(), 0, bufferList.Count, token);
							return (Leftovers: null, Finished: true);
						}

						bufferList.AddRange(buffer);
						(pos2, _) = SequenceSearch(bufferList, needle2, pos2);
					}

					if (pos2 > -1)
					{
						leftoverPos = pos + needle1.Count + needle2.Count;
						break;
					}
				}
			}
			else if (pos > -1)
			{
				leftoverPos = pos + needle1.Count;
			}

			if (leftoverPos != null)
			{
				var skip = false;
				var expectedLineEndingPos = pos - _lineEndingBytes.Length;
				if (pos > 0 && trimEnding)
				{
					var (lePos, _) = SequenceSearch(bufferList, _lineEndingBytes, expectedLineEndingPos);
					if (lePos != expectedLineEndingPos)
						skip = true;
				}

				if (!skip)
				{
					if (stream != null)
					{
						var finalPos = trimEnding ? expectedLineEndingPos : pos;
						await stream.WriteAsync(bufferList.ToArray(), 0, finalPos, token);
					}

					var leftovers = new byte[bufferList.Count - leftoverPos.Value];
					bufferList.CopyTo(leftoverPos.Value, leftovers, 0, bufferList.Count - leftoverPos.Value);
					return (Leftovers: leftovers, Finished: false);
				}
			}

			if (stream != null)
				await stream.WriteAsync(bufferList.ToArray(), 0, bufferList.Count, token);

			return (null, false);
		}

		private static (int Position, bool Partial) SequenceSearch(IReadOnlyList<byte> haystack,
			IReadOnlyList<byte> needle,
			int haystackIndex = 0)
		{
			var needleIndex = 0;
			while (haystackIndex < haystack.Count)
			{
				var h = haystack[haystackIndex];
				var n = needle[needleIndex];
				if (h == n)
					needleIndex++;
				else
					needleIndex = 0;
				haystackIndex++;
				if (needleIndex == needle.Count)
					return (Position: haystackIndex - needle.Count, Partial: false);
			}

			return needleIndex > 0
				? (Position: haystackIndex - needleIndex, Partial: true)
				: (Position: -1, Partial: false);
		}
	}
}
