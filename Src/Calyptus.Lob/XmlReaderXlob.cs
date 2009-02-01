using System.IO;
using System.Xml;
using System;

namespace Calyptus.Lob
{
	public class XmlReaderXlob : Xlob
	{
		private XmlReader reader;

		public XmlReaderXlob(XmlReader reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");
			this.reader = reader;
		}

		public override XmlReader OpenReader()
		{
			lock (this)
			{
				if (reader == null) throw new Exception("The XmlReader has already been opened once and cannot be reset.");
				XmlReader r = reader;
				reader = null;
				return r;
			}
		}

		public override void WriteTo(XmlWriter writer)
		{
			XmlReader r;
			lock (this)
			{
				if (reader == null) throw new Exception("The XmlReader has already been opened once and cannot be reset.");
				r = reader;
				reader = null;
			}
			if (r.ReadState == ReadState.Initial)
				r.Read();
			while(!r.EOF)
				writer.WriteNode(r, true);
		}

		public override bool Equals(Xlob xlob)
		{
			XmlReaderXlob xr = xlob as XmlReaderXlob;
			if (xr == null) return false;
			if (xr == this) return true;
			return this.reader != null && this.reader == xr.reader;
		}
	}
}