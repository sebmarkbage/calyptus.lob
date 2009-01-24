using System;
using System.Data.Common;
using System.Data;

namespace NHibernate.Lob.External
{
	public class ExternalBlobDbCommandProxy : DbCommand
	{
		DbCommand cmd;
		DbConnection conn;

		public ExternalBlobDbCommandProxy(DbConnection conn, DbCommand cmd)
		{
			this.cmd = cmd;
			this.conn = conn;
		}

		public override void Cancel()
		{
			cmd.Cancel();
		}

		public override string CommandText
		{
			get
			{
				return cmd.CommandText;
			}
			set
			{
				cmd.CommandText = value;
			}
		}

		public override int CommandTimeout
		{
			get
			{
				return cmd.CommandTimeout;
			}
			set
			{
				cmd.CommandTimeout = value;
			}
		}

		public override CommandType CommandType
		{
			get
			{
				return cmd.CommandType;
			}
			set
			{
				cmd.CommandType = value;
			}
		}

		protected override DbParameter CreateDbParameter()
		{
			return cmd.CreateParameter();
		}

		protected override DbConnection DbConnection
		{
			get
			{
				return conn;
			}
			set
			{
				conn = value;
				cmd.Connection = ((ExternalBlobDbConnectionProxy)value)._db as DbConnection;
			}
		}

		protected override DbParameterCollection DbParameterCollection
		{
			get { return cmd.Parameters; }
		}

		protected override DbTransaction DbTransaction
		{
			get
			{
				return cmd.Transaction;
			}
			set
			{
				cmd.Transaction = value;
			}
		}

		public override bool DesignTimeVisible
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
		{
			return cmd.ExecuteReader(behavior);
		}

		public override int ExecuteNonQuery()
		{
			return cmd.ExecuteNonQuery();
		}

		public override object ExecuteScalar()
		{
			return cmd.ExecuteScalar();
		}

		public override void Prepare()
		{
			cmd.Prepare();
		}

		public override UpdateRowSource UpdatedRowSource
		{
			get
			{
				return cmd.UpdatedRowSource;
			}
			set
			{
				cmd.UpdatedRowSource = value;
			}
		}
	}
}
