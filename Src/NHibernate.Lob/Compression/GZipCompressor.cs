using System.IO;
using System.IO.Compression;

namespace NHibernate.Lob.Compression
{
	public sealed class GZipCompressor : IStreamCompressor
	{
		public static readonly GZipCompressor Instance = new GZipCompressor();

		public Stream GetDecompressor(Stream input)
		{
			return new GZipStream(input, CompressionMode.Decompress, true);
		}

		public Stream GetCompressor(Stream output)
		{
			return new GZipStream(output, CompressionMode.Compress, true);
		}

		public override bool Equals(object obj)
		{
			return obj is GZipCompressor;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}