using System;
using System.IO;
using System.Collections.Generic;

namespace NHibernate.Lob.External
{
	public interface IExternalBlobConnection : IDisposable
	{
		int BlobIdentifierLength { get; }
		void Delete(byte[] blobIdentifier);
		Stream Open(byte[] blobIdentifier);
		byte[] Store(Stream stream);
		bool Equals(IExternalBlobConnection connection);
		void GarbageCollect(IEnumerable<byte[]> livingBlobIdentifiers);
	}
}
