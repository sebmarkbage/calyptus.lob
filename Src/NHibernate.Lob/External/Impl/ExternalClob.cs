using Calyptus.Lob;
using System.IO;
using System;
using NHibernate.Lob.Compression;
using System.Text;

namespace NHibernate.Lob.External
{
	public class ExternalClob : Clob
	{
		private IExternalBlobConnection connection;
		private byte[] identifier;
		private IStreamCompressor compression;
		private Encoding encoding;

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

		public IStreamCompressor Compression
		{
			get
			{
				return compression;
			}
		}

		public Encoding Encoding
		{
			get
			{
				return encoding;
			}
		}

		public ExternalClob(IExternalBlobConnection connection, byte[] identifier, Encoding encoding)
		{
			if (connection == null) throw new ArgumentNullException("connection");
			if (identifier == null) throw new ArgumentNullException("identifier");
			if (encoding == null) throw new ArgumentNullException("encoding");
			this.connection = connection;
			this.identifier = identifier;
			this.encoding = encoding;
		}

		public ExternalClob(IExternalBlobConnection connection, byte[] identifier, Encoding encoding, IStreamCompressor compression) : this(connection, identifier, encoding)
		{
			this.compression = compression;
		}

		public override TextReader OpenReader()
		{
			return new StreamReader(
				compression == null ? connection.OpenReader(identifier) : compression.GetDecompressor(connection.OpenReader(identifier)),
				encoding
			);
		}

		public override bool Equals(Clob clob)
		{
			if (clob == null) return false;
			if (clob == this) return true;
			ExternalClob ec = clob as ExternalClob;
			if (ec == null || !this.Connection.Equals(ec.Connection) || this.identifier.Length != ec.identifier.Length ||
				!this.encoding.Equals(ec.encoding) ||
				(this.compression != ec.compression && this.compression != null && !this.compression.Equals(ec.compression))) return false;
			byte[] a = this.identifier, b = ec.identifier;
			for (int i = 0; i < a.Length; i++)
				if (a[i] != b[i]) return false;
			return true;
		}
	}
}