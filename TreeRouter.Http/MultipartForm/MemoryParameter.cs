using System.IO;

namespace TreeRouter.Http.MultipartForm
{
	public class MemoryParameter : IUploadFileParameter
	{

		public MemoryParameter(MemoryStream memoryStream)
		{
			MemoryStream = memoryStream;
		}
		
		public MemoryStream MemoryStream { get; }
		public string FileName { get; set; }
		public string ContentType { get; set; }
		public StreamReader GetStreamReader() => new StreamReader(MemoryStream);
		
	}
}
