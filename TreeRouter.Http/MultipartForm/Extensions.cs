using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TreeRouter.Http.MultipartForm
{
	internal static class Extensions
	{
		public static async Task<IReadOnlyList<byte>> BufferReadAsync(this Stream stream, byte[] buffer,
			CancellationToken token)
		{
			var totalRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
			if (totalRead == 0)
				return null;
			if (totalRead < buffer.Length)
				return new Span<byte>(buffer, 0, totalRead - 1);
			return buffer;
		}

		public static Task<IReadOnlyList<byte>> BufferReadAsync(this Stream stream, int bufferSize,
			CancellationToken token)
		{
			var buffer = new byte[bufferSize];
			return BufferReadAsync(stream, buffer, token);
		}
	}
}
