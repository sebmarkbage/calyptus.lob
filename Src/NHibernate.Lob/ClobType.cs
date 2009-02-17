using NHibernate.UserTypes;
using NHibernate.Lob.Compression;
using System.Text;
using System.Collections;
using Calyptus.Lob;
using System.IO;
using NHibernate.SqlTypes;
using NHibernate.Engine;

namespace NHibernate.Lob
{
	public class ClobType : AbstractLobType, IParameterizedType
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

		public virtual void SetParameterValues(IDictionary parameters)
		{
			Parameters.GetClobSettings(parameters, out this.encoding, out this.compression);
			if (compression != null && encoding == null) encoding = Encoding.UTF8;
		}

		protected override object Get(System.Data.IDataReader rs, int ordinal)
		{
			if (compression == null)
				return new StringClob(rs.GetString(ordinal));
			return base.Get(rs, ordinal);
		}

		protected override object GetData(object value)
		{
			Clob clob = value as Clob;
			if (clob == null) return null;
			if (compression == null)
			{
				if (clob.Equals(Clob.Empty)) return "";
				StringClob sc = clob as StringClob;
				if (sc != null) return sc.Text;
				using (StringWriter sw = new StringWriter())
				{
					clob.WriteTo(sw);
					return sw.ToString();
				}
			}
			else
			{
				CompressedClob cb = clob as CompressedClob;
				if (cb != null && cb.Compression.Equals(compression)) return cb.Data;
				using (MemoryStream data = new MemoryStream())
				{
					using (Stream cs = compression.GetCompressor(data))
					using (StreamWriter sw = new StreamWriter(cs, encoding))
						clob.WriteTo(sw);
					return data.ToArray();
				}
			}
		}

		protected override object GetValue(object dataObj)
		{
			if (compression == null)
			{
				string str = dataObj as string;
				return str == null ? null : new StringClob(str);
			}
			else
			{
				byte[] data = dataObj as byte[];
				if (data == null) return null;
				return new CompressedClob(data, encoding, compression);
			}
		}

		public override SqlType[] SqlTypes(IMapping mapping)
		{
			if (compression == null)
				return new SqlType[] { new StringClobSqlType() };
			else
				return new SqlType[] { new BinaryBlobSqlType() };
		}

		public override System.Type ReturnedClass
		{
			get { return typeof(Clob); }
		}

		public override bool Equals(object obj)
		{
			if (obj == this) return true;
			if (!base.Equals(obj)) return false;
			ClobType ct = obj as ClobType;
			if (this.encoding != ct.encoding && this.encoding != null && !this.encoding.Equals(ct.encoding)) return false;
			if (this.compression == ct.compression) return true;
			return this.compression != null && this.compression.Equals(ct.compression);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}