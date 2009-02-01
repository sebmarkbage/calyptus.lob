using System;
using System.IO;
using System.Collections.Generic;

namespace NHibernate.Lob.External
{
	public interface IExternalBlobConnection : IDisposable
	{
		int BlobIdentifierLength { get; }
		void Delete(byte[] blobIdentifier);
		Stream OpenReader(byte[] blobIdentifier);
		ExternalBlobWriter OpenWriter();
		byte[] Store(Stream input);
		void ReadInto(byte[] blobIdentifier, Stream output);
		bool Equals(IExternalBlobConnection connection);
		void GarbageCollect(IEnumerable<byte[]> livingBlobIdentifiers);
	}
}
