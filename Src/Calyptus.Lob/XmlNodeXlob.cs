using System.IO;
using System.Xml;
using System;

namespace Calyptus.Lob
{
	public class XmlNodeXlob : Xlob
	{
		private XmlNode node;

		public XmlNode Node
		{
			get
			{
				return node;
			}
		}

		public XmlNodeXlob(XmlNode node)
		{
			if (node == null) throw new ArgumentNullException("node");
			this.node = node;
		}

		public override XmlReader OpenReader()
		{
			return new XmlNodeReader(node);
		}

		public override void WriteTo(XmlWriter writer)
		{
			node.WriteTo(writer);
		}

		public override bool Equals(Xlob xlob)
		{
			if (xlob == null) return false;
			if (this == xlob) return true;
			XmlNodeXlob xn = xlob as XmlNodeXlob;
			if (xn == null) return false;
			return this.node.Equals(xn.node);
		}
	}
}