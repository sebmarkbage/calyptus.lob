using NHibernate.Lob.Compression;
using NHibernate.UserTypes;
using System;
using System.Collections;
using System.IO;
using Calyptus.Lob;
using System.Text;
using System.Collections.Generic;

namespace NHibernate.Lob.External
{
	public class ExternalClobType : AbstractExternalBlobType, IParameterizedType
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

		public virtual void SetParameterValues(IDictionary<string, string> parameters)
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
			if (compression == null)
				using (StreamWriter sw = new StreamWriter(output, encoding))
					clob.WriteTo(sw);
			else
				using (Stream cs = compression.GetCompressor(output))
				using (StreamWriter sw = new StreamWriter(cs, encoding))
					clob.WriteTo(sw);
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
