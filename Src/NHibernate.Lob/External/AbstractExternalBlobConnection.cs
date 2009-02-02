using System.IO;
using System.Collections.Generic;
using System;

namespace NHibernate.Lob.External
{
	public abstract class AbstractExternalBlobConnection : IExternalBlobConnection
	{
		private List<WeakReference> openedStreams;

		private const int BUFFERSIZE = 0x1000;

		public abstract int BlobIdentifierLength { get; }

		public abstract void Delete(byte[] blobIdentifier);

		public abstract Stream OpenReader(byte[] blobIdentifier);

		public abstract ExternalBlobWriter OpenWriter();

		public abstract void GarbageCollect(IEnumerable<byte[]> livingBlobIdentifiers);

		public abstract bool Equals(IExternalBlobConnection connection);

		public AbstractExternalBlobConnection()
		{
			openedStreams = new List<WeakReference>();
		}

		public virtual byte[] Store(Stream input)
		{
			if (input == null) throw new ArgumentNullException("input");
			if (!input.CanRead) throw new Exception("Input stream is not in a readable state.");
			using (var s = this.OpenWriter())
			{
				byte[] buffer = new byte[BUFFERSIZE];
				int readBytes;
				while ((readBytes = input.Read(buffer, 0, BUFFERSIZE)) > 0)
					s.Write(buffer, 0, readBytes);
				return s.Commit();
			}
		}

		public void ReadInto(byte[] blobIdentifier, Stream output)
		{
			if (blobIdentifier == null) throw new ArgumentNullException("blobIdentifier");
			if (output == null) throw new ArgumentNullException("output");
			if (!output.CanWrite) throw new Exception("Output stream is not in a writable state.");
			using (var s = this.OpenReader(blobIdentifier))
			{
				byte[] buffer = new byte[BUFFERSIZE];
				int readBytes;
				while ((readBytes = s.Read(buffer, 0, BUFFERSIZE)) > 0)
				{
					output.Write(buffer, 0, readBytes);
				}
			}
		}

		Stream IExternalBlobConnection.OpenReader(byte[] blobIdentifier)
		{
			if (openedStreams == null) throw new Exception("The ExternalBlobConnection has been closed.");
			Stream s = this.OpenReader(blobIdentifier);
			openedStreams.Add(new WeakReference(s));
			return s;
		}

		ExternalBlobWriter IExternalBlobConnection.OpenWriter()
		{
			if (openedStreams == null) throw new Exception("The ExternalBlobConnection has been closed.");
			ExternalBlobWriter w = this.OpenWriter();
			openedStreams.Add(new WeakReference(w));
			return w;
		}

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
			if (openedStreams != null)
			{
				List<WeakReference> str = openedStreams;
				openedStreams = null;
				foreach (WeakReference r in str)
				{
					IDisposable d = r.Target as IDisposable;
					if (d != null) d.Dispose();
				}
			}
			GC.SuppressFinalize(this);
		}
	}
}