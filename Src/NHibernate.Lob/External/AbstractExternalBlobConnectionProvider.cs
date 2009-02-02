using System.Configuration;

namespace NHibernate.Lob.External
{
	public abstract class AbstractExternalBlobConnectionProvider : IExternalBlobConnectionProvider
	{
		public abstract IExternalBlobConnection GetConnection();

		public virtual string ConnectionString { get; set; }

		void IExternalBlobConnectionProvider.Configure(System.Collections.Generic.IDictionary<string, string> settings)
		{
			string connStr;
			if (settings.TryGetValue(Environment.ConnectionStringNameProperty, out connStr))
			{
				ConnectionStringSettings connectionStringSettings = ConfigurationManager.ConnectionStrings[connStr];
				if (connectionStringSettings == null)
					throw new HibernateException(string.Format("Could not find named connection string {0}", connStr));
				ConnectionString = connectionStringSettings.ConnectionString;
			}
			else if (settings.TryGetValue(Environment.ConnectionStringProperty, out connStr))
			{
				ConnectionString = connStr;
			}
		}
	}
}