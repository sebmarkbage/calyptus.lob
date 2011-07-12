using System;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace NHibernate.Lob.External
{
	public class S3Connection : AbstractExternalBlobConnection
	{
		private const int BUFFERSIZE = 0x1000;

		private const string TEMPFILEBASE = "$temp";

		private string tempPath;

		private Amazon.S3.AmazonS3Client client;
		private string accessKeyID;
		private string bucketName;

		private string hashName;

		private int hashLength;

		public S3Connection(string accessKeyID, string secretAccessKeyID, string bucketName)
		{
			this.client = new Amazon.S3.AmazonS3Client(accessKeyID, secretAccessKeyID);
			this.accessKeyID = accessKeyID;
			this.bucketName = bucketName;
		}

		public S3Connection(string accessKeyID, string secretAccessKeyID, string bucketName, string tempPath, string hashName)
		{
			this.client = new Amazon.S3.AmazonS3Client(accessKeyID, secretAccessKeyID);
			this.accessKeyID = accessKeyID;
			this.bucketName = bucketName;

			if (this.tempPath != null)
				this.tempPath = System.IO.Path.Combine(tempPath, TEMPFILEBASE);

			this.hashName = hashName;
			if (hashName == null)
				hashLength = 20;
			else
				using (HashAlgorithm hash = HashAlgorithm.Create(hashName))
					hashLength = hash.HashSize / 8;
		}
		
		public override Stream OpenReader(byte[] contentIdentifier)
		{
			var response = client.GetObject(new Amazon.S3.Model.GetObjectRequest()
				.WithBucketName(bucketName)
				.WithKey(GetPath(contentIdentifier))
			);
			return response.ResponseStream;
		}

		public override void Delete(byte[] contentIdentifier)
		{
			string path = GetPath(contentIdentifier);
			Amazon.S3.Model.DeleteObjectRequest request = new Amazon.S3.Model.DeleteObjectRequest()
				.WithBucketName(bucketName)
				.WithKey(path);
			client.DeleteObject(request);
		}

		public override bool Equals(IExternalBlobConnection connection)
		{
			S3Connection c = connection as S3Connection;
			if (c == null) return false;

			return this.accessKeyID.Equals(c.accessKeyID) && bucketName == c.bucketName;
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
			return new S3BlobWriter(this);
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

		public string GetPath(byte[] contentIdentifier)
		{
			if (contentIdentifier == null) throw new NullReferenceException("contentIdentifier cannot be null.");
			System.Text.StringBuilder sb = new System.Text.StringBuilder(contentIdentifier.Length * 2 + 2);
			sb.Append(contentIdentifier[0].ToString("x2"));
			sb.Append("/");
			sb.Append(contentIdentifier[1].ToString("x2"));
			sb.Append("/");
			for(int i = 2; i < contentIdentifier.Length; i++)
				sb.Append(contentIdentifier[i].ToString("x2"));
			return sb.ToString();
		}

		private class S3BlobWriter : ExternalBlobWriter
		{
			private string tempFile;
			private FileStream tempStream;
			private HashAlgorithm hash;
			private HashAlgorithm checksum;

			//private byte[] cryptBuffer;
			private S3Connection connection;

			public S3BlobWriter(S3Connection connection)
			{
				if (connection == null) throw new ArgumentNullException("connection");
				this.connection = connection;

				hash = connection.hashName == null ? new SHA1CryptoServiceProvider() : HashAlgorithm.Create(connection.hashName);
				if (hash == null) throw new Exception("Missing hash algorithm: " + connection.hashName);
	
				checksum = new MD5CryptoServiceProvider();

				if (connection.tempPath == null)
				{
					tempFile = Path.GetTempFileName();
					tempStream = new FileStream(tempFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
				}
				else
				{
					Random r = new Random();
					string temp;
					int i = 0;
					do
					{
						if (i > 100) throw new Exception("Unable to find a random temporary filename for writing.");
						temp = connection.tempPath + r.Next().ToString("x");
						i++;
					}
					while (File.Exists(temp));
					tempStream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None);
					tempFile = temp;
				}
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				ThrowIfClosed();
				byte[] cryptBuffer = (byte[])buffer.Clone();
				hash.TransformBlock(cryptBuffer, offset, count, cryptBuffer, 0);
				cryptBuffer = (byte[])buffer.Clone();
				checksum.TransformBlock(cryptBuffer, offset, count, cryptBuffer, 0);
				tempStream.Write(buffer, offset, count);
			}

			public override void WriteByte(byte value)
			{
				ThrowIfClosed();
				byte[] cryptBuffer = new byte[] { value };
				hash.TransformBlock(cryptBuffer, 0, 1, cryptBuffer, 0);
				cryptBuffer = new byte[] { value };
				checksum.TransformBlock(cryptBuffer, 0, 1, cryptBuffer, 0);
				tempStream.WriteByte(value);
			}

			public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
			{
				ThrowIfClosed();
				byte[] cryptBuffer = (byte[])buffer.Clone();
				hash.TransformBlock(cryptBuffer, offset, count, cryptBuffer, 0);
				cryptBuffer = (byte[])buffer.Clone();
				checksum.TransformBlock(cryptBuffer, offset, count, cryptBuffer, 0);
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

				byte[] emptyBytes = new byte[0];
				hash.TransformFinalBlock(emptyBytes, 0, 0);
				checksum.TransformFinalBlock(emptyBytes, 0, 0);

				byte[] id = hash.Hash;

				string digest = Convert.ToBase64String(checksum.Hash);
				string path = connection.GetPath(id);

				Amazon.S3.Model.PutObjectRequest request = new Amazon.S3.Model.PutObjectRequest()
					.WithBucketName(connection.bucketName)
					.WithKey(path)
					.WithMD5Digest(digest)
					.WithFilePath(tempFile);

				connection.client.PutObject(request);

				tempStream = null;
				System.IO.File.Delete(tempFile);
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
					//cryptBuffer = null;
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
