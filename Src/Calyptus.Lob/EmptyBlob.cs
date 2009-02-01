using System.IO;

namespace Calyptus.Lob
{
	public class EmptyBlob : Blob
	{
		public EmptyBlob()
		{
			
		}

		public override Stream OpenReader()
		{
			return new MemoryStream(new byte[0], false);
		}

		public override void WriteTo(Stream output)
		{
		}

		public override bool Equals(Blob blob)
		{
			EmptyBlob eb = blob as EmptyBlob;
			if (eb != null) return true;
			ArrayBlob ab = blob as ArrayBlob;
			if (ab != null && ab.Data.Length == 0) return true;
			return false;
		}
	}
}