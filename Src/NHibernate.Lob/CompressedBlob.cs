using System.IO;
using System;
using Calyptus.Lob;
using NHibernate.Lob.Compression;

namespace NHibernate.Lob
{
	public class CompressedBlob : Blob
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

		public CompressedBlob(byte[] data, IStreamCompressor compression)
		{
			if (data == null) throw new ArgumentNullException("data");
			if (compression == null) throw new ArgumentNullException("compression", "Use ArrayBlob instead.");
			this.data = data;
		}

		public override Stream OpenReader()
		{
			return compression.GetDecompressor(new MemoryStream(data, false));
		}

		public override bool Equals(Blob blob)
		{
			CompressedBlob cb = blob as CompressedBlob;
			if (cb == null) return false;
			if (this == cb) return true;
			if (!this.compression.Equals(cb.compression)) return false;
			byte[] a = this.data, b = cb.data;
			if (a.Length != b.Length) return false;
			for (int i = 0; i < a.Length; i++)
				if (a[i] != b[i])
					return false;
			return true;
		}
	}
}