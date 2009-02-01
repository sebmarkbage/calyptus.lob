using System.IO;

namespace Calyptus.Lob
{
	public class EmptyClob : Clob
	{
		public EmptyClob()
		{
			
		}

		public override TextReader OpenReader()
		{
			return new StringReader("");
		}

		public override void WriteTo(TextWriter writer)
		{
		}

		public override void WriteTo(Stream output, System.Text.Encoding encoding)
		{
		}

		public override bool Equals(Clob clob)
		{
			EmptyClob ec = clob as EmptyClob;
			if (ec != null) return true;
			StringClob sc = clob as StringClob;
			if (sc != null && sc.Text == "") return true;
			return false;
		}
	}
}