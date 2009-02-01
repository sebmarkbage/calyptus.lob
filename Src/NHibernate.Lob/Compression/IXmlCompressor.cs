using System.Xml;
using System.IO;

namespace NHibernate.Lob.Compression
{
	public interface IXmlCompressor
	{
		XmlReader GetDecompressor(Stream input);
		XmlWriter GetCompressor(Stream output);
	}
}
