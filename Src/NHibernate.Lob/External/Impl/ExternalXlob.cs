using Calyptus.Lob;
using System.IO;
using System;
using NHibernate.Lob.Compression;
using System.Xml;

namespace NHibernate.Lob.External
{
	public class ExternalXlob : Xlob
	{
		private IExternalBlobConnection connection;
		private byte[] identifier;
		private IXmlCompressor compression;

		public IExternalBlobConnection Connection
		{
			get
			{
				return connection;
			}
		}

		public byte[] Identifier
		{
			get
			{
				return identifier;
			}
		}

		public IXmlCompressor Compression
		{
			get
			{
				return compression;
			}
		}

		public ExternalXlob(IExternalBlobConnection connection, byte[] identifier, IXmlCompressor compression)
		{
			if (connection == null) throw new ArgumentNullException("connection");
			if (identifier == null) throw new ArgumentNullException("identifier");
			if (compression == null) throw new ArgumentNullException("compression");
			this.connection = connection;
			this.identifier = identifier;
			this.compression = compression;
		}

		public override XmlReader OpenReader()
		{
			return compression.GetDecompressor(connection.OpenReader(identifier));
		}

		public override bool Equals(Xlob xlob)
		{
			if (xlob == null) return false;
			if (xlob == this) return true;
			ExternalXlob ex = xlob as ExternalXlob;
			if (ex == null || !this.Connection.Equals(ex.Connection) || this.identifier.Length != ex.identifier.Length ||
				(this.compression != ex.compression && this.compression != null && !this.compression.Equals(ex.compression))) return false;
			byte[] a = this.identifier, b = ex.identifier;
			for (int i = 0; i < a.Length; i++)
				if (a[i] != b[i]) return false;
			return true;
		}
	}
}