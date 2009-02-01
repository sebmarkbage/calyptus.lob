using System.IO;
using System;
using Calyptus.Lob;
using NHibernate.Lob.Compression;
using System.Xml;

namespace NHibernate.Lob
{
	public class CompressedXlob : Xlob
	{
		private byte[] data;

		public byte[] Data
		{
			get
			{
				return data;
			}
		}

		private IXmlCompressor compression;

		public IXmlCompressor Compression
		{
			get
			{
				return compression;
			}
		}

		public CompressedXlob(byte[] data, IXmlCompressor compression)
		{
			if (data == null) throw new ArgumentNullException("data");
			if (compression == null) throw new ArgumentNullException("compression");
			this.data = data;
			this.compression = compression;
		}

		public override XmlReader OpenReader()
		{
			return compression.GetDecompressor(new MemoryStream(data, false));
		}

		public override bool Equals(Xlob xlob)
		{
			CompressedXlob cx = xlob as CompressedXlob;
			if (cx == null) return false;
			if (this == cx) return true;
			if (!this.compression.Equals(cx.compression)) return false;
			byte[] a = this.data, b = cx.data;
			if (a.Length != b.Length) return false;
			for (int i = 0; i < a.Length; i++)
				if (a[i] != b[i])
					return false;
			return true;
		}
	}
}