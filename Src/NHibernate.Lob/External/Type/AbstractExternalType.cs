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
	public class CasBlobType : AbstractType
	{
		private IExternalBlobConnection GetCasConnection(IDbConnection connection)
		{
			if (connection == null) throw new NullReferenceException("CasBlobType requires an open connection.");
			IExternalBlobConnection c = connection as IExternalBlobConnection;
			if (c == null) throw new Exception("CasBlobType requires a ICasConnection. Make sure you use DriverAndCasStorageConnectionProvider and specify an ICasConnectionProvider in your NHibernate configuration.");
			return c;
		}

		public override string ToLoggableString(object value, ISessionFactoryImplementor factory)
		{
			CasStream cs = value as CasStream;
			if (cs == null) return null;
			StringBuilder sb = new StringBuilder();
			foreach (byte b in cs.ContentIdentifier)
				sb.Append(b.ToString("x"));
			return sb.ToString();
		}

		public override object Assemble(object cached, ISessionImplementor session, object owner)
		{
			byte[] identifier = cached as byte[];
			if (identifier == null) return null;
			IExternalBlobConnection conn = GetCasConnection(session.Connection);
			return new CasStream(conn, identifier);
		}

		public override object Disassemble(object value, ISessionImplementor session, object owner)
		{
			CasStream s = value as CasStream;
			if (s != null)
			{
				IExternalBlobConnection conn = GetCasConnection(session.Connection);
				if (conn.Equals(s.Connection))
					return s.ContentIdentifier;
			}
			return null;
		}

		public override void NullSafeSet(IDbCommand cmd, object value, int index, bool[] settable, ISessionImplementor session)
		{
			if (settable[0]) NullSafeSet(cmd, value, index, session);
		}

		public override void NullSafeSet(IDbCommand cmd, object value, int index, ISessionImplementor session)
		{
			Stream s = value as Stream;
			if (s == null)
			{
				((IDataParameter)cmd.Parameters[index]).Value = DBNull.Value;
			}
			else
			{
				IExternalBlobConnection conn = GetCasConnection(session.Connection);
				CasStream cs = s as CasStream;
				// If an equal connection is used, skip store
				((IDataParameter)cmd.Parameters[index]).Value = (cs != null && conn.Equals(cs.Connection)) ? cs.ContentIdentifier : conn.Store(s);
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

			IExternalBlobConnection conn = GetCasConnection(session.Connection);

			byte[] identifier = new byte[conn.BlobIdentifierLength];

			int i = (int)rs.GetBytes(index, 0, identifier, 0, identifier.Length);
			if (i != identifier.Length) throw new Exception("Unknown CAS identifier length. Expected " + identifier.Length.ToString() + " bytes");

			return new CasStream(conn, identifier);
		}

		public override object DeepCopy(object val, EntityMode entityMode, ISessionFactoryImplementor factory)
		{
			CasStream s = val as CasStream;
			if (s != null)
				return new CasStream(s.Connection, s.ContentIdentifier);
			return val;
		}

		public override object Replace(object original, object target, ISessionImplementor session, object owner, IDictionary copiedAlready)
		{
			CasStream os = original as CasStream;
			CasStream ts = target as CasStream;

			if (os != null && ts != null && System.Array.Equals(os.ContentIdentifier, ts.ContentIdentifier))
				return target;

			return original;
		}

		public override SqlType[] SqlTypes(IMapping mapping)
		{
			return new SqlType[] { new SqlType(DbType.Binary, 32) };
		}

		public override System.Type ReturnedClass
		{
			get { return typeof(Stream); }
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
			get { return "CASIdentifier"; }
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
