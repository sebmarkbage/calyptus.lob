using NHibernate.UserTypes;
using NHibernate.Lob.Compression;
using System.Collections;
using Calyptus.Lob;
using System.IO;
using NHibernate.Engine;
using NHibernate.SqlTypes;
using System.Xml;
using System.Collections.Generic;

namespace NHibernate.Lob
{
	public class XlobType : AbstractLobType, IParameterizedType
	{
		private IXmlCompressor compression;
		public IXmlCompressor Compression
		{
			get
			{
				return compression;
			}
		}

		public virtual void SetParameterValues(IDictionary<string, string> parameters)
		{
			Parameters.GetXlobSettings(parameters, out compression);
		}

		protected override object Get(System.Data.IDataReader rs, int ordinal)
		{
			if (compression == null)
				return new StringXlob(rs.GetString(ordinal));
			return base.Get(rs, ordinal);
		}

		protected override object GetData(object value)
		{
			Xlob xlob = value as Xlob;
			if (xlob == null) return null;
			if (compression == null)
			{
				if (xlob.Equals(Xlob.Empty)) return "";
				StringXlob sx = xlob as StringXlob;
				if (sx != null) return sx.XmlFragment;
				using (StringWriter sw = new StringWriter())
				{
					xlob.WriteTo(sw);
					return sw.ToString();
				}
			}
			else
			{
				CompressedXlob cx = xlob as CompressedXlob;
				if (cx != null && cx.Compression.Equals(compression)) return cx.Data;
				using (MemoryStream data = new MemoryStream())
				{
					using (XmlWriter xw = compression.GetCompressor(data))
						xlob.WriteTo(xw);
					return data.ToArray();
				}
			}
		}

		protected override object GetValue(object dataObj)
		{
			if (compression == null)
			{
				string str = dataObj as string;
				return str == null ? null : new StringXlob(str);
			}
			else
			{
				byte[] data = dataObj as byte[];
				if (data == null) return null;
				return new CompressedXlob(data, compression);
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
			get { return typeof(Xlob); }
		}

		public override bool Equals(object obj)
		{
			if (obj == this) return true;
			if (!base.Equals(obj)) return false;
			XlobType xt = obj as XlobType;
			if (this.compression == xt.compression) return true;
			return this.compression != null && this.compression.Equals(xt.compression);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}