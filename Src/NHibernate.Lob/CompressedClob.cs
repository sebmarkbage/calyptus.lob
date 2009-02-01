using System.IO;
using System;
using Calyptus.Lob;
using NHibernate.Lob.Compression;
using System.Text;

namespace NHibernate.Lob
{
	public class CompressedClob : Clob
	{
		private byte[] data;
		public byte[] Data
		{
			get
			{
				return data;
			}
		}

		private IStreamCompressor compression;
		public IStreamCompressor Compression
		{
			get
			{
				return compression;
			}
		}

		private Encoding encoding;
		public Encoding Encoding
		{
			get
			{
				return encoding;
			}
		}

		public CompressedClob(byte[] data, Encoding encoding)
		{
			if (data == null) throw new ArgumentNullException("data");
			if (encoding == null) throw new ArgumentNullException("encoding");
			this.data = data;
			this.encoding = encoding;
		}

		public CompressedClob(byte[] data, Encoding encoding, IStreamCompressor compression) : this(data, encoding)
		{
			this.compression = compression;
		}

		public override TextReader OpenReader()
		{
			return new StreamReader(compression == null ? new MemoryStream(data, false) : compression.GetDecompressor(new MemoryStream(data, false)), encoding);
		}

		public override bool Equals(Clob clob)
		{
			CompressedClob cb = clob as CompressedClob;
			if (cb == null) return false;
			if (this == cb) return true;
			if ((this.compression != cb.compression && this.compression != null && !this.compression.Equals(cb.compression)) || !this.encoding.Equals(cb.encoding)) return false;
			byte[] a = this.data, b = cb.data;
			if (a.Length != b.Length) return false;
			for (int i = 0; i < a.Length; i++)
				if (a[i] != b[i])
					return false;
			return true;
		}
	}
}