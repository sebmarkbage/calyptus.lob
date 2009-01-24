using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NHibernate.Lob.External
{
	public interface IExternalBlobConnectionProvider
	{
		void Configure(IDictionary<string, string> settings);

		IExternalBlobConnection GetConnection();
	}
}
