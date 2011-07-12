using NHibernate.UserTypes;
using NHibernate.Lob.Compression;
using System.Collections;
using System;
using NHibernate.Engine;
using Calyptus.Lob;
using System.IO;
using NHibernate.SqlTypes;
using System.Collections.Generic;

namespace NHibernate.Lob
{
	public class BlobType : AbstractLobType, IParameterizedType
	{
		private IStreamCompressor compression;
		public IStreamCompressor Compression
		{
			get
			{
				return compression;
			}
		}

		public virtual void SetParameterValues(IDictionary<string, string> parameters)
		{
			Parameters.GetBlobSettings(parameters, out this.compression);
		}

		protected override object GetData(object value)
		{
			Blob blob = value as Blob;
			if (blob == null) return null;
			ArrayBlob ab = blob as ArrayBlob;
			if (ab != null && compression == null) return ab.Data;
			CompressedBlob cb = blob as CompressedBlob;
			if (cb != null && cb.Compression.Equals(compression)) return cb.Data;
			using (MemoryStream data = new MemoryStream())
			{
				if (compression == null)
					blob.WriteTo(data);
				else
					using (Stream cs = compression.GetCompressor(data))
						blob.WriteTo(cs);
				return data.ToArray();
			}
		}

		protected override object GetValue(object dataObj)
		{
			byte[] data = dataObj as byte[];
			if (data == null) return null;
			if (compression == null)
				return new ArrayBlob(data);
			else
				return new CompressedBlob(data, compression);
		}

		public override SqlType[] SqlTypes(IMapping mapping)
		{
			return new SqlType[] { new BinaryBlobSqlType() };
		}

		public override System.Type ReturnedClass
		{
			get { return typeof(Blob); }
		}

		public override bool Equals(object obj)
		{
			if (obj == this) return true;
			if (!base.Equals(obj)) return false;
			BlobType bt = obj as BlobType;
			if (this.compression == bt.compression) return true;
			return this.compression != null && this.compression.Equals(bt.compression);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}