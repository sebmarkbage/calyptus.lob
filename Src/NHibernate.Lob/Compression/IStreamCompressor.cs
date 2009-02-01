using System.IO;

namespace NHibernate.Lob.Compression
{
	public interface IStreamCompressor
	{
		Stream GetDecompressor(Stream input);
		Stream GetCompressor(Stream output);
	}
}