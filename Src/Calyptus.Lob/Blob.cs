using System.IO;

namespace Calyptus.Life
{
	public abstract class Blob
	{
		public static Blob Create(Stream input)
		{

		}

		public static Blob Create(byte[] data)
		{

		}

		public static implicit operator Blob(Stream input)
		{
			return Create(input);
		}

		public static implicit operator Blob(byte[] data)
		{
			return Create(data);
		}

		public abstract Stream Open();

		public virtual void WriteTo(Stream output)
		{

		}
	}
}