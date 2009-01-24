using System.IO;
using System.Collections.Generic;
namespace NHibernate.Lob.External
{
	public abstract class AbstractExternalBlobConnection : IExternalBlobConnection
	{
		public abstract int BlobIdentifierLength { get; }

		public abstract void Delete(byte[] blobIdentifier);

		public abstract Stream Open(byte[] blobIdentifier);

		public abstract byte[] Store(Stream stream);

		public abstract void GarbageCollect(IEnumerable<byte[]> livingBlobIdentifiers);

		public abstract bool Equals(IExternalBlobConnection connection);

		public override bool Equals(object obj)
		{
			return Equals(obj as IExternalBlobConnection);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		~AbstractExternalBlobConnection()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
		}

		protected virtual void Dispose(bool isDisposing)
		{
			if (_isAlreadyDisposed)
			{
				// don't dispose of multiple times.
				return;
			}

			// free managed resources that are being managed by the ConnectionProvider if we
			// know this call came through Dispose()
			if (isDisposing)
			{
				log.Debug("Disposing of ConnectionProvider.");
			}

			// free unmanaged resources here

			_isAlreadyDisposed = true;
			// nothing for Finalizer to do - so tell the GC to ignore it
			GC.SuppressFinalize(this);
		}
	}
}