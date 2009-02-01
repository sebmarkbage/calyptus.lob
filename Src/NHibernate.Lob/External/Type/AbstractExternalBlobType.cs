using System;
using System.Collections.Generic;
using System.Text;
using NHibernate.Persister.Entity;
using NHibernate.Cache;
using NHibernate.Engine;
using NHibernate.Mapping;
using NHibernate.Cache.Entry;
using NHibernate.Metadata;
using NHibernate.Tuple.Entity;
using NHibernate;
using NHibernate.Type;
using System.Collections;
using NHibernate.UserTypes;
using System.IO;
using NHibernate.Lob.External;
using System.Data;
using NHibernate.SqlTypes;
using System.Xml;
using NHibernate.Util;

namespace NHibernate.Lob.External
{
	public abstract class AbstractExternalBlobType : AbstractType
	{
		private int identifierLength;

		protected IExternalBlobConnection GetExternalBlobConnection(ISessionImplementor connection)
		{
			if (connection == null) throw new NullReferenceException("CasBlobType requires an open connection.");
			IExternalBlobConnection c = connection as IExternalBlobConnection;
			if (c == null) throw new Exception("CasBlobType requires a ICasConnection. Make sure you use DriverAndCasStorageConnectionProvider and specify an ICasConnectionProvider in your NHibernate configuration.");
			if (identifierLength == 0) identifierLength = c.BlobIdentifierLength;
			return c;
		}

		protected abstract object CreateLobInstance(IExternalBlobConnection connection, byte[] identifier);

		protected abstract bool ExtractLobData(object lob, out IExternalBlobConnection connection, out byte[] identifier);

		protected abstract void WriteLobTo(object lob, Stream output);

		public override string ToLoggableString(object value, ISessionFactoryImplementor factory)
		{
			IExternalBlobConnection blobconn;
			byte[] identifier;
			if (this.ExtractLobData(value, out blobconn, out identifier))
			{
				StringBuilder sb = new StringBuilder();
				foreach (byte b in identifier)
					sb.Append(b.ToString("x2"));
				return sb.ToString();
			}
			return null;
		}

		public override object Assemble(object cached, ISessionImplementor session, object owner)
		{
			byte[] identifier = cached as byte[];
			if (identifier == null) return null;
			IExternalBlobConnection conn = GetExternalBlobConnection(session);
			return CreateLobInstance(conn, identifier);
		}

		public override object Disassemble(object value, ISessionImplementor session, object owner)
		{
			if (value == null) return null;
			IExternalBlobConnection blobconn;
			byte[] identifier;
			if (this.ExtractLobData(value, out blobconn, out identifier))
			{
				IExternalBlobConnection conn = GetExternalBlobConnection(session);
				if (conn.Equals(blobconn))
					return identifier;
			}
			throw new Exception("Unable to cache an unsaved lob.");
		}

		public override object DeepCopy(object value, EntityMode entityMode, ISessionFactoryImplementor factory)
		{
			IExternalBlobConnection blobconn;
			byte[] identifier;
			if (this.ExtractLobData(value, out blobconn, out identifier))
				return CreateLobInstance(blobconn, identifier);
			return value;
		}

		public override object Replace(object original, object target, ISessionImplementor session, object owner, IDictionary copiedAlready)
		{
			return original;
		}

		public override void NullSafeSet(IDbCommand cmd, object value, int index, bool[] settable, ISessionImplementor session)
		{
			if (settable[0]) NullSafeSet(cmd, value, index, session);
		}

		public override void NullSafeSet(IDbCommand cmd, object value, int index, ISessionImplementor session)
		{
			if (value == null)
			{
				((IDataParameter)cmd.Parameters[index]).Value = DBNull.Value;
			}
			else
			{
				IExternalBlobConnection conn = GetExternalBlobConnection(session);
				IExternalBlobConnection blobconn;
				byte[] identifier;
				if (!ExtractLobData(value, out blobconn, out identifier) || !conn.Equals(blobconn)) // Skip writing if an equal connection is used
					using (ExternalBlobWriter writer = conn.OpenWriter())
					{
						WriteLobTo(value, writer);
						identifier = writer.Commit();
					}
				((IDataParameter)cmd.Parameters[index]).Value = identifier;
			}
		}

		public override object NullSafeGet(IDataReader rs, string[] names, ISessionImplementor session, object owner)
		{
			return NullSafeGet(rs, names[0], session, owner);
		}

		public override object NullSafeGet(IDataReader rs, string name, ISessionImplementor session, object owner)
		{
			int index = rs.GetOrdinal(name);

			if (rs.IsDBNull(index)) return null;

			IExternalBlobConnection conn = GetExternalBlobConnection(session);

			byte[] identifier = new byte[conn.BlobIdentifierLength];

			int i = (int)rs.GetBytes(index, 0, identifier, 0, identifier.Length);
			if (i != identifier.Length) throw new Exception("Unknown identifier length. Expected " + identifier.Length.ToString() + " bytes");

			return CreateLobInstance(conn, identifier);
		}

		public override SqlType[] SqlTypes(IMapping mapping)
		{
			return new SqlType[] { new SqlType(DbType.Binary, this.identifierLength == 0 ? 32 : this.identifierLength) };
		}

		public override int GetColumnSpan(IMapping session)
		{
			return 1;
		}

		public override bool IsDirty(object old, object current, bool[] checkable, ISessionImplementor session)
		{
			return checkable[0] && IsDirty(old, current, session);
		}

		public override bool[] ToColumnNullness(object value, IMapping mapping)
		{
			return value == null ? new bool[] { false } : new bool[] { true };
		}

		public override object FromXMLNode(XmlNode xml, IMapping factory)
		{
			return null;
		}

		public override void SetToXMLNode(XmlNode xml, object value, ISessionFactoryImplementor factory)
		{
			xml.Value = null;
		}

		public override bool IsMutable
		{
			get { return true; }
		}

		public override string Name
		{
			get { return "ExternalBlobIdentifier"; }
		}

		public override bool Equals(object obj)
		{
			if (this == obj) return true;
			if (obj == null) return false;
			return this.GetType() == obj.GetType();
		}

		public override int GetHashCode()
		{
			return this.GetType().GetHashCode();
		}
	}
}
