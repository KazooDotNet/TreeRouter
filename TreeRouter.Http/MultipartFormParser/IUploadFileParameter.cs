using System.IO;

namespace TreeRouter.Http.MultipartFormParser
{
	public interface IUploadFileParameter
	{
		string FileName { get; }
		string ContentType { get; }
		StreamReader GetStreamReader();
	}
}
