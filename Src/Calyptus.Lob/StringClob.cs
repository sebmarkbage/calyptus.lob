using System.IO;
using System;

namespace Calyptus.Lob
{
	public class StringClob : Clob
	{
		private string text;

		public string Text
		{
			get
			{
				return text;
			}
		}

		public StringClob(string text)
		{
			if (text == null) throw new ArgumentNullException("text");
			this.text = text;
		}

		public override TextReader OpenReader()
		{
			return new StringReader(this.text);
		}

		public override void WriteTo(TextWriter writer)
		{
			writer.Write(text);
		}

		public override bool Equals(Clob clob)
		{
			if (clob == this) return true;
			StringClob sc = clob as StringClob;
			if (sc == null) return false;
			return this.text.Equals(sc.text);
		}
	}
}