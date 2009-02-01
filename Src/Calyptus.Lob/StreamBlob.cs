using System.IO;
using System;

namespace Calyptus.Lob
{
	public class StreamBlob : Blob
	{
		private Stream stream;
		private long initialPosition;
		private bool needRestart;
		private bool alreadyOpen;

		public Stream UnderlyingStream
		{
			get
			{
				return stream;
			}
		}

		public StreamBlob(Stream stream)
		{
			if (stream == null) throw new ArgumentNullException("stream");
			if (!stream.CanRead) throw new NotSupportedException("Stream cannot read. Blobs are read-only.");

			this.stream = stream;
			try
			{
				this.initialPosition = stream.CanSeek ? stream.Position : -1L;
			}
			catch
			{
				this.initialPosition = -1L;
			}
		}

		public override Stream OpenReader()
		{
			lock (this)
			{
				if (needRestart && this.initialPosition < 0L)
					throw new Exception("The underlying Stream cannot be reset. It has already been opened.");
				if (alreadyOpen)
					throw new Exception("There's already a reader Stream open on this Blob. Close the first Stream before requesting a new one.");
				if (needRestart)
					stream.Seek(initialPosition, SeekOrigin.Begin);
				alreadyOpen = true;
			}
			return new BlobStream(this);
		}

		public override bool Equals(Blob blob)
		{
			StreamBlob sb = blob as StreamBlob;
			if (sb != null)
			{
				if (this.stream == sb.stream) return true;
				FileStream fsa = this.stream as FileStream;
				if (fsa == null) return false;
				FileStream fsb = sb.stream as FileStream;
				if (fsb == null) return false;
				try
				{
					return fsa.Name.Equals(fsb.Name);
				}
				catch
				{
					return false;
				}
			}

			FileBlob fb = blob as FileBlob;
			if (fb == null) return false;
			
			FileStream fs = this.stream as FileStream;
			if (fs == null) return false;
			try
			{
				return fb.Filename.Equals(fs.Name);
			}
			catch
			{
				return false;
			}
		}

		#region Read-only stream wrapper class
		private class BlobStream : Stream
		{
			private StreamBlob blob;

			public BlobStream(StreamBlob blob)
			{
				this.blob = blob;
			}

			private void ThrowClosed()
			{
				if (blob == null) throw new Exception("The Stream is already closed.");
			}

			public override bool CanRead
			{
				get { return blob != null ? blob.stream.CanRead : false; }
			}

			public override bool CanSeek
			{
				get {
					if (blob == null) return false;
					return blob.stream.CanSeek;
				}
			}

			public override bool CanWrite
			{
				get { return false; }
			}

			public override void Flush()
			{
				throw new NotSupportedException();
			}

			public override long Length
			{
				get
				{
					ThrowClosed();
					return blob.stream.Length;
				}
			}

			public override long Position
			{
				get
				{
					ThrowClosed();
					return blob.stream.Position;
				}
				set
				{
					ThrowClosed();
					blob.stream.Position = value;
					blob.needRestart = true;
				}
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				ThrowClosed();
				int i = blob.stream.Read(buffer, offset, count);
				if (i > 0) blob.needRestart = true;
				return i;
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				ThrowClosed();
				blob.needRestart = true;
				return blob.stream.Seek(offset, origin);
			}

			public override void SetLength(long value)
			{
				throw new NotSupportedException();
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				throw new NotSupportedException();
			}

			public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
			{
				ThrowClosed();
				blob.needRestart = true;
				return blob.stream.BeginRead(buffer, offset, count, callback, state);
			}

			public override int EndRead(IAsyncResult asyncResult)
			{
				ThrowClosed();
				return blob.stream.EndRead(asyncResult);
			}

			public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
			{
				throw new NotSupportedException();
			}

			public override void EndWrite(IAsyncResult asyncResult)
			{
				throw new NotSupportedException();
			}

			public override bool CanTimeout
			{
				get
				{
					return blob == null ? false : blob.stream.CanTimeout;
				}
			}

			public override void Close()
			{
				Dispose(true);
			}

			protected override void Dispose(bool disposing)
			{
				if (blob != null)
					lock (blob)
					{
						blob.alreadyOpen = false;
						blob = null;
					}
			}

			public override int ReadByte()
			{
				ThrowClosed();
				var i = blob.stream.ReadByte();
				blob.needRestart = true;
				return i;
			}

			public override void WriteByte(byte value)
			{
				throw new NotSupportedException();
			}

			public override int ReadTimeout
			{
				get
				{
					ThrowClosed();
					return blob.stream.ReadTimeout;
				}
				set
				{
					ThrowClosed();
					blob.stream.ReadTimeout = value;
				}
			}

			public override int WriteTimeout
			{
				get
				{
					throw new NotSupportedException();
				}
				set
				{
					throw new NotSupportedException();
				}
			}
		}
		#endregion
	}
}