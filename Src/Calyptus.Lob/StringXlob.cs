using System.IO;
using System.Xml;
using System;

namespace Calyptus.Lob
{
	public class StringXlob : Xlob
	{
		private string xmlFragment;
		private XmlParserContext context;
		private XmlNodeType nodeType;

		public string XmlFragment
		{
			get
			{
				return xmlFragment;
			}
		}

		public XmlNodeType NodeType
		{
			get
			{
				return nodeType;
			}
		}

		public XmlParserContext ParserContext
		{
			get
			{
				return context;
			}
		}

		public StringXlob(string xmlFragment)
		{
			if (xmlFragment == null) throw new ArgumentNullException("xmlFragment");
			this.xmlFragment = xmlFragment;
			this.nodeType = XmlNodeType.Element;
		}

		public StringXlob(string xmlFragment, XmlParserContext parserContext) : this(xmlFragment)
		{
			this.context = parserContext;
		}

		public StringXlob(string xmlFragment, XmlNodeType nodeType, XmlParserContext parserContext)
			: this(xmlFragment, parserContext)
		{
			this.nodeType = nodeType;
			this.context = parserContext;
		}

		public override XmlReader OpenReader()
		{
			return new XmlTextReader(xmlFragment, nodeType, context);
		}

		public override void WriteTo(XmlWriter writer)
		{
			writer.WriteRaw(xmlFragment);
		}

		public override void WriteTo(TextWriter writer)
		{
			writer.Write(xmlFragment);
		}

		public override bool Equals(Xlob xlob)
		{
			if (xlob == this) return true;
			StringXlob sx = xlob as StringXlob;
			if (sx == null) return false;
			return this.xmlFragment.Equals(sx.xmlFragment) && this.context == sx.context && this.nodeType == sx.nodeType;
		}
	}
}