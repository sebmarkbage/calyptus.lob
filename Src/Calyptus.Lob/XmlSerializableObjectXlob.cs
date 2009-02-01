using System.IO;
using System.Xml;
using System;
using System.Xml.Serialization;

namespace Calyptus.Lob
{
	public class XmlSerializableObjectXlob : Xlob
	{
		private IXmlSerializable obj;

		public IXmlSerializable Object
		{
			get
			{
				return obj;
			}
		}

		public XmlSerializableObjectXlob(IXmlSerializable obj)
		{
			if (obj == null) throw new ArgumentNullException("obj");
			this.obj = obj;
		}

		public override XmlReader OpenReader()
		{
			// NOTE: Renders in memory. Most objects will already be in memory and therefore small but some implementations might be large.
			var frag = new XmlDocument().CreateDocumentFragment();
			obj.WriteXml(frag.CreateNavigator().AppendChild());
			return new XmlNodeReader(frag);
		}

		public override void WriteTo(XmlWriter writer)
		{
			obj.WriteXml(writer);
		}

		public override bool Equals(Xlob xlob)
		{
			XmlSerializableObjectXlob sx = xlob as XmlSerializableObjectXlob;
			if (sx == null) return false;
			return this.obj == sx.obj;
		}

	}
}