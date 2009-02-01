using System.IO;
using System;

namespace Calyptus.Lob
{
	public class ArrayBlob : Blob
	{
		private byte[] data;

		public byte[] Data
		{
			get
			{
				return data;
			}
		}

		public ArrayBlob(byte[] data)
		{
			if (data == null) throw new ArgumentNullException("data");
			this.data = data;
		}

		public override Stream OpenReader()
		{
			return new MemoryStream(data, false);
		}

		public override void WriteTo(Stream output)
		{
			output.Write(data, 0, data.Length);
		}

		public override bool Equals(Blob blob)
		{
			ArrayBlob ab = blob as ArrayBlob;
			if (ab == null) return false;
			if (this == ab) return true;

			byte[] a = this.data, b = ab.data;

			if (a.Length != b.Length) return false;

			for (int i = 0; i < a.Length; i++)
				if (a[i] != b[i])
					return false;
			return true;
		}
	}
}