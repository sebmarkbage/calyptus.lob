using NHibernate.Lob.Compression;
using NHibernate.UserTypes;
using System;
using System.Collections;
using System.IO;
using Calyptus.Lob;
using System.Text;

namespace NHibernate.Lob.External
{
	public abstract class ExternalClobType : AbstractExternalBlobType, IParameterizedType
	{
		private IStreamCompressor compression;
		public IStreamCompressor Compression
		{
			get
			{
				return compression;
			}
		}
		private Encoding encoding;
		public Encoding Encoding
		{
			get
			{
				return encoding;
			}
		}

		public ExternalClobType()
		{
			this.encoding = Encoding.UTF8;
		}

		public virtual void SetParameterValues(IDictionary parameters)
		{
			Parameters.GetClobSettings(parameters, out this.encoding, out this.compression);
		}

		protected override object CreateLobInstance(IExternalBlobConnection connection, byte[] identifier)
		{
			return new ExternalClob(connection, identifier, encoding, compression);
		}

		protected override bool ExtractLobData(object lob, out IExternalBlobConnection connection, out byte[] identifier)
		{
			ExternalClob clob = lob as ExternalClob;
			if (clob == null)
			{
				connection = null;
				identifier = null;
				return false;
			}
			connection = clob.Connection;
			identifier = clob.Identifier;
			return true;
		}

		protected override void WriteLobTo(object lob, Stream output)
		{
			Clob clob = lob as Clob;
			if (clob == null) return;
			clob.WriteTo(new StreamWriter(compression == null ? output : compression.GetCompressor(output), encoding));
		}

		public override System.Type ReturnedClass
		{
			get { return typeof(Clob); }
		}

		public override bool Equals(object obj)
		{
			if (obj == this) return true;
			if (!base.Equals(obj)) return false;
			ExternalClobType t = obj as ExternalClobType;
			if (t == null) return false;
			if (t.compression != this.compression) return false;
			if (t.encoding != this.encoding) return false;
			return true;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}
