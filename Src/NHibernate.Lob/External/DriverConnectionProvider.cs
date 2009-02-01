using System;
using NHibernate.Lob.External;
using System.Data;
using System.IO;
using System.Collections.Generic;
using NHibernate.Connection;
using System.Configuration;

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
			System.Type providerType;
			string t;
			if (settings.TryGetValue(ExternalBlobs.ConnectionProviderProperty, out t) && t != null)
			{
				providerType = System.Type.GetType(t);
				_provider = (IExternalBlobConnectionProvider)System.Activator.CreateInstance(providerType);
				_provider.Configure(settings);
			}
			else if (settings.TryGetValue(ExternalBlobs.ConnectionStringNameProperty, out t) && t != null)
			{
				ConnectionStringSettings connectionStringSettings = ConfigurationManager.ConnectionStrings[t];
				if (connectionStringSettings != null && !string.IsNullOrEmpty(connectionStringSettings.ProviderName))
				{
					providerType = System.Type.GetType(connectionStringSettings.ProviderName);
					if (typeof(IExternalBlobConnectionProvider).IsAssignableFrom(providerType))
					{
						_provider = (IExternalBlobConnectionProvider)System.Activator.CreateInstance(providerType);
						_provider.Configure(settings);
					}
				}
			}
			_base.Configure(settings);
		}

		public IDbConnection GetConnection()
		{
			if (_provider == null) return _base.GetConnection();
			else return new ExternalBlobDbConnectionWrapper(_base.GetConnection(), _provider.GetConnection());
		}

		public void CloseConnection(IDbConnection conn)
		{
			_base.CloseConnection(conn);
		}

		public global::NHibernate.Driver.IDriver Driver
		{
			get { return new ExternalBlobDriverWrapper(_base.Driver); }
		}

		public void Dispose()
		{
			_base.Dispose();
		}
	}
}
