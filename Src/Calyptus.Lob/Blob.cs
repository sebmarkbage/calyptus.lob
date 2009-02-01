using System.IO;
using System;

namespace Calyptus.Lob
{
	public abstract class Blob
	{
		private const int BUFFERSIZE = 0x1000;

		public static Blob Empty
		{
			get
			{
				return new EmptyBlob();
			}
		}

		public static Blob Create(Stream input)
		{
			return new StreamBlob(input);
		}

		public static Blob Create(byte[] data)
		{
			return new ArrayBlob(data);
		}

		public static Blob Create(string filename)
		{
			return new FileBlob(filename);
		}

		public static implicit operator Blob(Stream input)
		{
			return new StreamBlob(input);
		}

		public static implicit operator Blob(byte[] data)
		{
			return new ArrayBlob(data);
		}

		public static implicit operator Blob(Uri uri)
		{
			return new WebBlob(uri);
		}

		public static implicit operator Blob(FileInfo file)
		{
			return new FileBlob(file);
		}

		public abstract Stream OpenReader();

		public virtual void WriteTo(Stream output)
		{
			using (var s = this.OpenReader())
			{
				byte[] buffer = new byte[BUFFERSIZE];
				int readBytes;
				while ((readBytes = s.Read(buffer, 0, BUFFERSIZE)) > 0)
				{
					output.Write(buffer, 0, readBytes);
				}
			}
		}

		public override bool Equals(object obj)
		{
			Blob b = obj as Blob;
			if (b == null) return false;
			return Equals(b);
		}

		public abstract bool Equals(Blob blob);
	}
}