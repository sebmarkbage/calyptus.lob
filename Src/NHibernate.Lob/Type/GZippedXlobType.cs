using System;
using NHibernate.UserTypes;
using NHibernate.SqlTypes;
using System.Data;
using System.IO.Compression;
using System.IO;
using System.Xml;
using System.Text;

namespace NHibernate.Lob
{
	public class CompressedXlobReaderType : IUserType
	{
		public virtual System.Type ReturnedType
		{
			get { return typeof(XmlReader); }
		}

		public object Assemble(object cached, object owner)
		{
			byte[] data = cached as byte[];
			if (data == null) return null;

			return new XmlTextReader(new TextReader(new MemoryStream(data), Encoding.UTF8));
		}

		public object Disassemble(object value)
		{
			XmlReader reader = value as XmlReader;
			if (reader == null) return null;
			using (MemoryStream stream = new MemoryStream())
			{
				using (XmlWriter w = new XmlTextWriter(stream, Encoding.UTF8))
				{
					w.WriteNode(reader, true);
					w.Flush();
				}
				return stream.GetBuffer();
			}
		}

		public object DeepCopy(object value)
		{
			var gzReader = value as GzippedXmlTextReader;
			if (gzReader != null) return new GzippedXmlTextReader(new MemoryStream(gzReader.ReadCompressedData()), Encoding.UTF8);
			return value as XmlReader;
		}

		public object NullSafeGet(System.Data.IDataReader rs, string[] names, object owner)
		{
			int i = rs.GetOrdinal(names[0]);
			if (rs.IsDBNull(i)) return null;

			int bufferSize = 0x1000;
			byte[] buffer = new byte[bufferSize];

			int readBytes = rs.GetBytes(i, 0, buffer, 0, bufferSize);
			long position = (long)readBytes;
			MemoryStream data = new MemoryStream(readBytes);
			while(readBytes > 0)
			{
				data.Write(buffer, 0, readBytes);
				position += (readBytes = rs.GetBytes(i, position, buffer, 0, bufferSize));
			}
			data.Seek(0L, SeekOrigin.Begin);
			return new GzippedXmlTextReader(data, Encoding.UTF8);
		}

		public void NullSafeSet(System.Data.IDbCommand cmd, object value, int index)
		{
			GzippedXmlTextReader gzReader = value as GzippedXmlTextReader;
			if (gzReader != null)
			{
				((IDataParameter)cmd.Parameters[index]).Value = gzReader.ReadCompressedData();
				return;
			}

			XmlReader reader = value as XmlReader;
			if (reader == null)
			{
				((IDataParameter)cmd.Parameters[index]).Value = null;
				return;
			}

			using (MemoryStream compressedData = new MemoryStream())
			{
				using (GZipStream s = new GZipStream(compressedData, CompressionMode.Compress))
				using (XmlWriter xw = new XmlTextWriter(s, Encoding.UTF8))
				{
					xw.WriteNode(reader);
					xw.Flush();
				}
				((IDataParameter)cmd.Parameters[index]).Value = compressedData.GetBuffer();
			}
		}

		public SqlType[] SqlTypes
		{
			get { return new SqlType[] { new SqlType(DbType.Binary) }; }
		}

		public object Replace(object original, object target, object owner)
		{
			return DeepCopy(original);
		}

		public bool IsMutable
		{
			get { return false; }
		}

		public int GetHashCode(object x)
		{
			return x.GetHashCode();
		}

		private class GzippedXmlTextReader : XmlTextReader
		{
			private MemoryStream stream;

			public GzippedXmlTextReader(MemoryStream stream, Encoding encoding) : base(new StreamReader(new GZipStream(data, CompressionMode.Decompress), encoding))
			{
				this.stream = stream;
			}

			public byte[] ReadCompressedData()
			{
				if (stream == null) throw new Exception("XmlTextReader is already disposed.");
				byte[] data;
				if (stream.Position == 0L)
				{
					data = stream.GetBuffer();
				}
				else
				{
					long remainingSize = stream.Length - stream.Position;
					data = new byte[remainingSize];
					stream.Read(data, 0, remainingSize);
				}
				stream.Dispose();
				stream = null;
				

				return data.GetBuffer();
			}

			protected override void Dispose(bool disposing)
			{
				if (data != null)
					data.Dispose();
				data = null;
				base.Dispose(disposing);
			}
		}
	}
}
