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

namespace NHibernate.Lob
{
	public abstract class AbstractLobType : AbstractType
	{
		protected abstract object GetData(object value);
		protected abstract object GetValue(object data);

		protected virtual object Get(IDataReader rs, int ordinal)
		{
			int bufferSize = 0x1000;
			byte[] buffer = new byte[bufferSize];

			int readBytes = (int)rs.GetBytes(ordinal, 0L, buffer, 0, bufferSize);
			long position = readBytes;
			using (MemoryStream data = new MemoryStream(readBytes))
			{
				if (readBytes >= bufferSize)
					while (readBytes > 0)
					{
						data.Write(buffer, 0, readBytes);
						position += (readBytes = (int)rs.GetBytes(ordinal, position, buffer, 0, bufferSize));
					}

				data.Write(buffer, 0, readBytes);
				data.Flush();
				if (data.Length == 0)
					return GetValue(new byte[0]);

				return GetValue(data.ToArray());
			}
		}

		public override string ToLoggableString(object value, ISessionFactoryImplementor factory)
		{
			return "[LOB]";
		}

		public override object DeepCopy(object value, EntityMode entityMode, ISessionFactoryImplementor factory)
		{
			return value;
		}

		public override object Replace(object original, object target, ISessionImplementor session, object owner, IDictionary copiedAlready)
		{
			return original;
		}

		public override object Assemble(object cached, ISessionImplementor session, object owner)
		{
			return GetValue(cached);
		}

		public override object Disassemble(object value, ISessionImplementor session, object owner)
		{
			return GetData(value);
		}

		public override void NullSafeSet(IDbCommand cmd, object value, int index, bool[] settable, ISessionImplementor session)
		{
			if (settable[0]) NullSafeSet(cmd, value, index, session);
		}

		public override void NullSafeSet(IDbCommand cmd, object value, int index, ISessionImplementor session)
		{
			object data = GetData(value);
			if (data == null)
				((IDataParameter)cmd.Parameters[index]).Value = DBNull.Value;
			else
				((IDataParameter)cmd.Parameters[index]).Value = data;
		}

		public override object NullSafeGet(IDataReader rs, string[] names, ISessionImplementor session, object owner)
		{
			return NullSafeGet(rs, names[0], session, owner);
		}

		public override object NullSafeGet(IDataReader rs, string name, ISessionImplementor session, object owner)
		{
			int i = rs.GetOrdinal(name);
			if (rs.IsDBNull(i)) return null;
			return Get(rs, i);
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

		public override string Name
		{
			get { return this.GetType().Name; }
		}
	}
}
