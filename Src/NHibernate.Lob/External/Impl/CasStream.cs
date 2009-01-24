using System;
using System.IO;

namespace NHibernate.Lob.External
{
	public sealed class CasStream : Stream
	{
		private Stream _openStream;
		private IExternalBlobConnection _conn;
		private byte[] _identifier;

		internal CasStream(IExternalBlobConnection conn, byte[] identifier)
		{
			_conn = conn;
			_identifier = identifier;
		}

		public IExternalBlobConnection Connection { get { return _conn; } }

		public byte[] ContentIdentifier { get { return _identifier; } }

		private void OpenStream()
		{
			if (_openStream == null)
				_openStream = _conn.Open(_identifier);
		}

		private void CloseStream()
		{
			if (_openStream != null)
			{
				_openStream.Dispose();
				_openStream = null;
			}
		}

		public override bool Equals(object obj)
		{
			if (obj == this) return true;
			CasStream s = obj as CasStream;
			if (s == null) return false;
			return Connection.Equals(s.Connection) && Array.Equals(ContentIdentifier, s.ContentIdentifier);
		}

		public override int GetHashCode()
		{
			return Connection.GetHashCode() ^ ContentIdentifier.GetHashCode();
		}

		public override bool CanWrite
		{
			get { return false; }
		}

		public override bool CanRead
		{
			get { return true; }
		}

		public override bool CanSeek
		{
			get
			{
				OpenStream();
				return _openStream.CanSeek;
			}
		}

		public override void Flush()
		{
			throw new NotImplementedException();
		}

		public override long Length
		{
			get {
				OpenStream();
				return _openStream.Length;
			}
		}

		public override long Position
		{
			get
			{
				if (_openStream == null)
					return 0;
				else
					return _openStream.Position;
			}
			set
			{
				OpenStream();
				_openStream.Position = value;
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			OpenStream();
			return _openStream.Read(buffer, offset, count);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			OpenStream();
			return _openStream.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			throw new NotImplementedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}

		public override void Close()
		{
			CloseStream();
		}

		protected override void Dispose(bool disposing)
		{
			CloseStream();
		}
	}
}
