using System;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace NHibernate.Lob.External
{
	public class FileSystemCasConnection : AbstractExternalBlobConnection
	{
		private const int BUFFERSIZE = 0x1000;
		private const string TEMPFILEBASE = "$temp";
		private string path;
		private string hashName;
		private int hashLength;

		public FileSystemCasConnection(string storagePath) : this(storagePath, null) { }

		public FileSystemCasConnection(string storagePath, string hashName)
		{
			path = System.IO.Path.GetFullPath(storagePath);
			this.hashName = hashName;
			if (hashName == null)
				hashLength = 20;
			else
				using (HashAlgorithm hash = HashAlgorithm.Create(hashName))
					hashLength = hash.HashSize / 8;
		}

		public override Stream OpenReader(byte[] contentIdentifier)
		{
			return new FileStream(GetPath(contentIdentifier), FileMode.Open, FileAccess.Read);
		}

		public override void Delete(byte[] contentIdentifier)
		{
			string path = GetPath(contentIdentifier);
			if (System.IO.File.Exists(path))
				System.IO.File.Delete(path);
			DeleteFolder(contentIdentifier);
		}

		public override bool Equals(IExternalBlobConnection connection)
		{
			FileSystemCasConnection c = connection as FileSystemCasConnection;
			if (c == null) return false;
			return this.path.Equals(c.path) && hashName == c.hashName;
		}

		public override int BlobIdentifierLength
		{
			get { return hashLength; }
		}

		public override void GarbageCollect(IEnumerable<byte[]> livingBlobIdentifiers)
		{
			throw new NotImplementedException();
		}

		public override ExternalBlobWriter OpenWriter()
		{
			return new FileSystemCasBlobWriter(this);
		}

		/*public byte[] Store(Stream stream)
		{

			try
			{
				byte[] hash = WriteFile(stream, output);


				return hash;
			}
			catch
			{

				throw;
			}
		 
			private byte[] WriteFile(Stream input, FileStream output)
			{

				int i, bufferSize = _bufferSize;
				byte[] readBuffer = new byte[bufferSize], writeBuffer = new byte[bufferSize];

				i = input.Read(readBuffer, 0, bufferSize);

				if (i == 0) return new byte[hash.HashSize];

				do
				{
					byte[] cryptBuffer = readBuffer;
					readBuffer = writeBuffer;
					writeBuffer = cryptBuffer;

					cryptBuffer = (byte[])cryptBuffer.Clone(); // Necessary??
					
					IAsyncResult a = input.BeginRead(readBuffer, 0, bufferSize, null, null);
					IAsyncResult aa = output.BeginWrite(writeBuffer, 0, i, null, null);

					hash.TransformBlock(cryptBuffer, 0, i, cryptBuffer, 0);

					output.EndWrite(aa);
					i = input.EndRead(a);
				}
				while (i > 0);

				hash.TransformFinalBlock(readBuffer, 0, 0); // ??

				return hash.Hash;
			}
		}*/

		private void DeleteFolder(byte[] contentIdentifier)
		{
			string path = Path.Combine(this.path, contentIdentifier[0].ToString("x") + Path.DirectorySeparatorChar + contentIdentifier[1].ToString("x"));
			DirectoryInfo d = new DirectoryInfo(path);
				try
				{
					if (d.Exists)
						d.Delete();
					d = d.Parent;
					if (d.Exists)
						d.Delete();
				}
				catch { }
		}

		private void CreateFolder(byte[] contentIdentifier)
		{
			string path = Path.Combine(this.path, contentIdentifier[0].ToString("x2"));
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
			path = Path.Combine(path, contentIdentifier[1].ToString("x2"));
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
		}

		public string GetPath(byte[] contentIdentifier)
		{
			if (contentIdentifier == null) throw new NullReferenceException("contentIdentifier cannot be null.");
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.Append(contentIdentifier[0].ToString("x2"));
			sb.Append(Path.DirectorySeparatorChar);
			sb.Append(contentIdentifier[1].ToString("x2"));
			sb.Append(Path.DirectorySeparatorChar);
			for(int i = 2; i < contentIdentifier.Length; i++)
				sb.Append(contentIdentifier[i].ToString("x2"));
			return Path.Combine(path, sb.ToString());
		}

		private class FileSystemCasBlobWriter : ExternalBlobWriter
		{
			private string tempFile;
			private FileStream tempStream;
			private HashAlgorithm hash;
			private byte[] cryptBuffer;
			private FileSystemCasConnection connection;

			public FileSystemCasBlobWriter(FileSystemCasConnection connection)
			{
				if (connection == null) throw new ArgumentNullException("connection");
				this.connection = connection;

				HashAlgorithm hash = connection.hashName == null ? new SHA1CryptoServiceProvider() : HashAlgorithm.Create(connection.hashName);
				Random r = new Random();
				string temp;
				int i = 0;
				do
				{
					if (i > 100) throw new Exception("Unable to find a random temporary filename for writing.");
					temp = Path.Combine(connection.path, FileSystemCasConnection.TEMPFILEBASE + r.Next().ToString("x"));
					i++;
				}
				while (File.Exists(temp));
				tempStream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None);
				tempFile = temp;
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				ThrowIfClosed();
				byte[] cryptBuffer = (byte[])buffer.Clone();
				hash.TransformBlock(cryptBuffer, offset, count, cryptBuffer, 0);
				tempStream.Write(buffer, offset, count);
			}

			public override void WriteByte(byte value)
			{
				ThrowIfClosed();
				byte[] cryptBuffer = new byte[] { value };
				hash.TransformBlock(cryptBuffer, 0, 1, cryptBuffer, 0);
				tempStream.WriteByte(value);
			}

			public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
			{
				ThrowIfClosed();
				byte[] cryptBuffer = (byte[])buffer.Clone();
				hash.TransformBlock(cryptBuffer, offset, count, cryptBuffer, 0);
				return tempStream.BeginWrite(buffer, offset, count, callback, state);
			}

			public override void EndWrite(IAsyncResult asyncResult)
			{
				ThrowIfClosed();
				tempStream.EndWrite(asyncResult);
			}

			public override byte[] Commit()
			{
				ThrowIfClosed();
				tempStream.Flush();
				tempStream.Dispose();

				hash.TransformFinalBlock(cryptBuffer, 0, 0);

				byte[] id = hash.Hash;
				connection.CreateFolder(id);

				string path = connection.GetPath(id);
				FileInfo f = new FileInfo(path);
				if (f.Exists)
				{
					FileInfo t = new FileInfo(tempFile);
					if (f.Length != t.Length)
						throw new IOException("A file with the same hash code but a different length already exists. This is very unlikely. There might be a transfer issue.");
					t.Delete();
				}
				else
					File.Move(tempFile, path);

				tempStream = null;
				tempFile = null;

				Dispose(true);
				return id;
			}

			protected override void Dispose(bool disposing)
			{
				if (tempStream != null)
					lock (this)
					{
						tempStream.Dispose();
						tempStream = null;
						try { System.IO.File.Delete(tempFile); }
						catch { }
						tempFile = null;
					}
				if (disposing)
				{
					if (hash != null) ((IDisposable)hash).Dispose();
					hash = null;
					connection = null;
					cryptBuffer = null;
				}
				base.Dispose(disposing);
			}

			public override bool CanTimeout
			{
				get { return tempStream != null ? tempStream.CanTimeout : false; }
			}

			public override bool CanSeek
			{
				get { return tempStream != null ? tempStream.CanSeek : false; }
			}

			public override bool CanWrite
			{
				get { return tempStream != null ? tempStream.CanWrite : false; }
			}

			public override void Flush()
			{
				if (tempStream != null) tempStream.Flush();
			}

			public override long Length
			{
				get
				{
					ThrowIfClosed();
					return tempStream.Length;
				}
			}

			public override long Position
			{
				get
				{
					ThrowIfClosed();
					return tempStream.Position;
				}
				set
				{
					tempStream.Position = value;
				}
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				ThrowIfClosed();
				return tempStream.Seek(offset, origin);
			}

			public override void SetLength(long value)
			{
				ThrowIfClosed();
				tempStream.SetLength(value);
			}

			private void ThrowIfClosed()
			{
				if (tempStream == null) throw new Exception("The writer is closed.");
			}
		}
	}
}
