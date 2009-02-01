using Calyptus.Lob;
using System.IO;
using System;
using NHibernate.Lob.Compression;

namespace NHibernate.Lob.External
{
	public class ExternalBlob : Blob
	{
		private IExternalBlobConnection connection;
		private byte[] identifier;
		private IStreamCompressor compression;

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

		public ExternalBlob(IExternalBlobConnection connection, byte[] identifier)
		{
			if (connection == null) throw new ArgumentNullException("connection");
			if (identifier == null) throw new ArgumentNullException("identifier");
			this.connection = connection;
			this.identifier = identifier;
		}

		public ExternalBlob(IExternalBlobConnection connection, byte[] identifier, IStreamCompressor compression) : this(connection, identifier)
		{
			this.compression = compression;
		}

		public override Stream OpenReader()
		{
			return compression == null ? connection.OpenReader(identifier) : compression.GetDecompressor(connection.OpenReader(identifier));
		}

		public override bool Equals(Blob blob)
		{
			if (blob == null) return false;
			if (blob == this) return true;
			ExternalBlob eb = blob as ExternalBlob;
			if (eb == null || !this.Connection.Equals(eb.Connection) || this.identifier.Length != eb.identifier.Length ||
				(this.compression != eb.compression && this.compression != null && !this.compression.Equals(eb.compression))) return false;
			byte[] a = this.identifier, b = eb.identifier;
			for (int i = 0; i < a.Length; i++)
				if (a[i] != b[i]) return false;
			return true;
		}
	}
}