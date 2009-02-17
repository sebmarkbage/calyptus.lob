using System.Xml;
using System.IO;
using NHibernate.UserTypes;
using System.Collections;
using System.Text;
using System;

namespace NHibernate.Lob.Compression
{
	public sealed class XmlTextCompressor : IXmlCompressor, IParameterizedType
	{
		private XmlReaderSettings rsettings;
		private XmlWriterSettings wsettings;

		public Encoding Encoding
		{
			get
			{
				return wsettings.Encoding;
			}
			set
			{
				wsettings.Encoding = value;
			}
		}

		public ConformanceLevel ConformanceLevel
		{
			get
			{
				return rsettings.ConformanceLevel;
			}
			set
			{
				rsettings.ConformanceLevel = value;
				wsettings.ConformanceLevel = value;
			}
		}

		private IStreamCompressor compressor;
		public IStreamCompressor Compressor
		{
			get
			{
				return compressor;
			}
			set
			{
				this.compressor = value;
			}
		}

		public XmlTextCompressor()
		{
			rsettings = new XmlReaderSettings();
			rsettings.CloseInput = true;
			wsettings = new XmlWriterSettings();
			rsettings.ConformanceLevel = ConformanceLevel.Fragment;
			wsettings.ConformanceLevel = ConformanceLevel.Fragment;
			wsettings.Encoding = Encoding.UTF8;
			wsettings.CloseOutput = true;
		}

		public XmlTextCompressor(IStreamCompressor compressor) : this()
		{
			this.compressor = compressor;
		}

		public void SetParameterValues(IDictionary parameters)
		{
			string conf = parameters["conformance"] as string;
			if (!string.IsNullOrEmpty(conf)) ConformanceLevel = (ConformanceLevel)Enum.Parse(typeof(ConformanceLevel), conf, true);
			string enc = parameters["encoding"] as string;
			if (!string.IsNullOrEmpty(enc)) Encoding = System.Text.Encoding.GetEncoding(enc);
		}

		public XmlReader GetDecompressor(Stream input)
		{
			var parser = new XmlParserContext(null, null, null, XmlSpace.Preserve);
			parser.Encoding = Encoding;
			return XmlReader.Create(compressor == null ? input : compressor.GetDecompressor(input), rsettings, parser);
		}

		public XmlWriter GetCompressor(Stream output)
		{
			return XmlWriter.Create(compressor == null ? output : compressor.GetCompressor(output), wsettings);
		}

		public override bool Equals(object obj)
		{
			if (obj == this) return true;
			XmlTextCompressor tc = obj as XmlTextCompressor;
			if (tc == null) return false;
			if (this.Encoding != tc.Encoding) return false;
			if (this.ConformanceLevel != tc.ConformanceLevel) return false;
			return this.compressor == tc.compressor || (this.compressor != null && this.compressor.Equals(tc.compressor));
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}
