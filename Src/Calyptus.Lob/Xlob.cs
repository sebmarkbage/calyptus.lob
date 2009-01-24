using System.IO;
using System.Text;
using System.Xml;

namespace NHibernate.Lob
{
	public abstract class Xlob
	{
		public static Xlob Create(Stream stream, Encoding encoding)
		{

		}

		public static Xlob Create(XmlReader reader)
		{

		}

		public static Xlob Create(XmlDocument document)
		{

		}

		public static implicit operator Xlob(XmlReader reader)
		{
			return Create(reader);
		}

		public static implicit operator Xlob(XmlDocument document)
		{
			return Create(document);
		}

		public abstract XmlReader Open();

		public virtual void WriteTo(XmlWriter writer)
		{

		}

		public virtual void WriteTo(TextWriter writer)
		{
		}

		public virtual void WriteTo(Stream output, Encoding encoding)
		{

		}
	}
}