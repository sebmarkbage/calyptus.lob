using System;
using NHibernate.Lob.External;
using System.Data;
using System.IO;
using System.Data.Common;

namespace NHibernate.Lob.External
{
	public class ExternalBlobDbConnectionWrapper : DbConnection, IExternalBlobConnection
	{
		internal IDbConnection _db;
		IExternalBlobConnection _cas;

		public ExternalBlobDbConnectionWrapper(IDbConnection db, IExternalBlobConnection cas)
		{
			_db = db;
			_cas = cas;
		}

		void IDisposable.Dispose()
		{
			_db.Dispose();
			_cas.Dispose();
		}

		int IExternalBlobConnection.BlobIdentifierLength
		{
			get { return _cas.BlobIdentifierLength; }
		}

		void IExternalBlobConnection.Delete(byte[] fileReference)
		{
			_cas.Delete(fileReference);
		}

		Stream IExternalBlobConnection.OpenReader(byte[] fileReference)
		{
			return _cas.OpenReader(fileReference);
		}

		byte[] IExternalBlobConnection.Store(Stream stream)
		{
			return _cas.Store(stream);
		}

		bool IExternalBlobConnection.Equals(IExternalBlobConnection connection)
		{
			return _cas.Equals(connection);
		}

		protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
		{
			return (DbTransaction)_db.BeginTransaction(isolationLevel);
		}

		public override void ChangeDatabase(string databaseName)
		{
			_db.ChangeDatabase(databaseName);
		}

		public override void Close()
		{
			_db.Close();
		}

		public override string ConnectionString
		{
			get
			{
				return _db.ConnectionString;
			}
			set
			{
				_db.ConnectionString = value;
			}
		}

		protected override DbCommand CreateDbCommand()
		{
			return new ExternalBlobDbCommandWrapper(this, _db.CreateCommand() as DbCommand);
		}

		public override string DataSource
		{
			get { return null; }
		}

		public override string Database
		{
			get { return _db.Database; }
		}

		public override void Open()
		{
			_db.Open();
		}

		public override string ServerVersion
		{
			get { return null; }
		}

		public override ConnectionState State
		{
			get { return _db.State; }
		}


		ExternalBlobWriter IExternalBlobConnection.OpenWriter()
		{
			return _cas.OpenWriter();
		}

		void IExternalBlobConnection.ReadInto(byte[] blobIdentifier, Stream output)
		{
			_cas.ReadInto(blobIdentifier, output);
		}

		void IExternalBlobConnection.GarbageCollect(System.Collections.Generic.IEnumerable<byte[]> livingBlobIdentifiers)
		{
			_cas.GarbageCollect(livingBlobIdentifiers);
		}
	}
}
