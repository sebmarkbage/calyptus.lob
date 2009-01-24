using System;
using NHibernate.Driver;
using System.Data.Common;

namespace NHibernate.Lob.External
{
	public class ExternalBlobDriverProxy : IDriver
	{
		IDriver _base;

		public ExternalBlobDriverProxy(IDriver driver)
		{
			_base = driver;
		}

		public void Configure(System.Collections.Generic.IDictionary<string, string> settings)
		{
			_base.Configure(settings);
		}

		public System.Data.IDbConnection CreateConnection()
		{
			return _base.CreateConnection();
		}

		public System.Data.IDbCommand GenerateCommand(System.Data.CommandType type, global::NHibernate.SqlCommand.SqlString sqlString, global::NHibernate.SqlTypes.SqlType[] parameterTypes)
		{
			return new ExternalBlobDbCommandProxy(null, (DbCommand) _base.GenerateCommand(type, sqlString, parameterTypes));
		}

		public void PrepareCommand(System.Data.IDbCommand command)
		{
			_base.PrepareCommand(command);
		}

		public bool SupportsMultipleOpenReaders
		{
			get { return _base.SupportsMultipleOpenReaders; }
		}

		public bool SupportsMultipleQueries
		{
			get { return _base.SupportsMultipleQueries; }
		}
	}
}
