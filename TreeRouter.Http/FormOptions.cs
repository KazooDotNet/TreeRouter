namespace TreeRouter.Http
{
	public class FormOptions
	{
		public long MultiPartFileLimit { get; set; } = 1024000;
		public long MultiPartFieldLimit { get; set; } = 32768;
		public int HeaderLimit { get; set; } = 16384;
		public int BufferSize { get; set; } = 8192;
		public int TempFileLimit { get; set; } = 65536;
	}
}
