using System.IO;
using System.Text;
using System.Xml;
using System;
using System.Xml.Serialization;

namespace Calyptus.Lob
{
	public abstract class Xlob
	{
		public static Xlob Empty
		{
			get
			{
				return new EmptyXlob();
			}
		}

		public static Xlob Create(Stream stream)
		{
			return Create(stream, new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment, CloseInput = false }, null);
		}

		public static Xlob Create(TextReader reader)
		{
			return Create(reader, new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment, CloseInput = false }, null);
		}

		public static Xlob Create(Stream stream, XmlReaderSettings settings)
		{
			return Create(stream, settings, null);
		}

		public static Xlob Create(TextReader reader, XmlReaderSettings settings)
		{
			return Create(reader, settings, null);
		}

		public static Xlob Create(Stream stream, XmlParserContext inputContext)
		{
			return Create(stream, new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment, CloseInput = false }, inputContext);
		}

		public static Xlob Create(TextReader reader, XmlParserContext inputContext)
		{
			return Create(reader, new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment, CloseInput = false }, inputContext);
		}

		public static Xlob Create(Stream stream, XmlReaderSettings settings, XmlParserContext inputContext)
		{
			return new XmlReaderXlob(XmlReader.Create(stream, settings, inputContext));
		}

		public static Xlob Create(TextReader reader, XmlReaderSettings settings, XmlParserContext inputContext)
		{
			return new XmlReaderXlob(XmlReader.Create(reader, settings, inputContext));
		}

		public static Xlob Create(XmlReader reader)
		{
			return new XmlReaderXlob(reader);
		}

		public static Xlob Create(XmlDocument document)
		{
			return new XmlNodeXlob(document);
		}

		public static Xlob Create(string xmlFragment)
		{
			return new StringXlob(xmlFragment);
		}

		public static Xlob Create(Uri uri)
		{
			return new WebXlob(uri);
		}

		public static Xlob Create(IXmlSerializable obj)
		{
			return new XmlSerializableObjectXlob(obj);
		}

		public static implicit operator Xlob(XmlReader reader)
		{
			return new XmlReaderXlob(reader);
		}

		public static implicit operator Xlob(XmlDocument document)
		{
			return new XmlNodeXlob(document);
		}

		public static implicit operator Xlob(string xml)
		{
			return new StringXlob(xml);
		}

		public static implicit operator Xlob(Uri uri)
		{
			return new WebXlob(uri);
		}

		public abstract XmlReader OpenReader();

		public virtual void WriteTo(XmlWriter writer)
		{
			using (var reader = OpenReader())
				writer.WriteNode(reader, true);
		}

		public virtual void WriteTo(TextWriter writer)
		{
			using (var xw = XmlWriter.Create(writer))
				WriteTo(xw);
		}

		public virtual void WriteTo(Stream output, Encoding encoding)
		{
			using (var sw = new StreamWriter(output, encoding))
				WriteTo(sw);
		}

		public override bool Equals(object obj)
		{
			Xlob x = obj as Xlob;
			if (x == null) return false;
			return Equals(x);
		}

		public abstract bool Equals(Xlob xlob);
	}
}