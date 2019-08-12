using System.IO;

namespace TreeRouter.Http.MultipartForm
{
	public class FileParameter : IUploadFileParameter
	{

		public FileParameter(FileStream file)
		{
			File = file;
		}

		public string FileName { get; set; }
		public string ContentType { get; set; }
		public FileStream File { get; }
		public StreamReader GetStreamReader() => new StreamReader(File);
		public Stream Stream => File;
	}
}
