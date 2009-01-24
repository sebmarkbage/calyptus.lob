using System;
using NHibernate.Lob.External;
using System.Data;
using System.IO;
using System.Collections.Generic;
using NHibernate.Connection;

namespace NHibernate.Lob.External
{
	public class DriverConnectionProvider : IConnectionProvider
	{

		private global::NHibernate.Connection.DriverConnectionProvider _base;
		private IExternalBlobConnectionProvider _provider;

		public DriverConnectionProvider()
		{
			_base = new global::NHibernate.Connection.DriverConnectionProvider();
		}

		public void Configure(IDictionary<string, string> settings)
		{
			string t;
			if (settings.TryGetValue(casConnectionProviderProperty, out t) && t != null)
			{
				Type providerType = Type.GetType(t);
				_provider = (IExternalBlobConnectionProvider)System.Activator.CreateInstance(providerType);

				if (settings.TryGetValue(casConnectionStringProperty, out t))
					_provider.ConnectionString = t;
			}
			_base.Configure(settings);
		}

		public IDbConnection GetConnection()
		{
			if (_provider == null) return _base.GetConnection();
			else return new ExternalBlobDbConnectionProxy(_base.GetConnection(), _provider.GetConnection());
		}

		public void CloseConnection(IDbConnection conn)
		{
			_base.CloseConnection(conn);
		}

		public global::NHibernate.Driver.IDriver Driver
		{
			get { return new ExternalBlobDriverProxy(_base.Driver); }
		}

		public void Dispose()
		{
			_base.Dispose();
		}
	}
}
