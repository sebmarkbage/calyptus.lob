using NHibernate.Lob.Compression;
using NHibernate.UserTypes;
using System;
using System.Collections;
using System.IO;
using Calyptus.Lob;

namespace NHibernate.Lob.External
{
	public class ExternalBlobType : AbstractExternalBlobType, IParameterizedType
	{
		private IStreamCompressor compression;
		public IStreamCompressor Compression
		{
			get
			{
				return compression;
			}
		}

		public virtual void SetParameterValues(IDictionary parameters)
		{
			string compression = parameters["compression"] as string;
			if (!string.IsNullOrEmpty(compression))
				if (compression.Equals("gzip", StringComparison.OrdinalIgnoreCase))
				{
					this.compression = GZipCompressor.Instance;
				}
				else
				{
					System.Type compressor = System.Type.GetType(compression);
					this.compression = (IStreamCompressor)System.Activator.CreateInstance(compressor);
					IParameterizedType parameterized = this.compression as IParameterizedType;
					if (parameterized != null)
						parameterized.SetParameterValues(parameters);
				}
		}

		protected override object CreateLobInstance(IExternalBlobConnection connection, byte[] identifier)
		{
			return new ExternalBlob(connection, identifier, compression);
		}

		protected override bool ExtractLobData(object lob, out IExternalBlobConnection connection, out byte[] identifier)
		{
			ExternalBlob blob = lob as ExternalBlob;
			if (blob == null)
			{
				connection = null;
				identifier = null;
				return false;
			}
			connection = blob.Connection;
			identifier = blob.Identifier;
			return true;
		}

		protected override void WriteLobTo(object lob, Stream output)
		{
			Blob blob = lob as Blob;
			if (blob == null) return;
			blob.WriteTo(output);
		}

		public override System.Type ReturnedClass
		{
			get { return typeof(Blob); }
		}

		public override bool Equals(object obj)
		{
			return base.Equals(obj) && (this.compression == ((ExternalBlobType)obj).compression || (this.compression != null && this.compression.Equals(((ExternalBlobType)obj).compression)));
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}
