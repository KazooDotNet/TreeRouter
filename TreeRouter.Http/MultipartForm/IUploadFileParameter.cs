using System.IO;

namespace TreeRouter.Http.MultipartForm
{
	public interface IUploadFileParameter
	{
		string FileName { get; }
		string ContentType { get; }
		StreamReader GetStreamReader();
		Stream Stream { get; }
	}
}
