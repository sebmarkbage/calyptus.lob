using System;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Net;
using System.Xml;

namespace NHibernate.Lob.External
{
	public class S3Connection : AbstractExternalBlobConnection
	{
		private const int BUFFERSIZE = 0x1000;

		private const string TEMPFILEBASE = "$temp";

		private string tempPath;

		private string accessKeyID;
		private string secretAccessKeyID;

		private string bucketName;

		private string hashName;

		private bool secure;

		private int hashLength;

		public S3Connection(string accessKeyID, string secretAccessKeyID, string bucketName)
			: this(accessKeyID, secretAccessKeyID, bucketName, null, null)
		{
		}

		public S3Connection(string accessKeyID, string secretAccessKeyID, string bucketName, string tempPath, string hashName)
		{
			this.accessKeyID = accessKeyID;
			this.secretAccessKeyID = secretAccessKeyID;
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

		private HttpWebRequest Request(string verb, string path, string md5, string ifNot)
		{
			System.Collections.SortedList headers = new System.Collections.SortedList();
			if (md5 != null) headers.Add("Content-MD5", md5);
			if (ifNot != null) headers.Add("If-None-Match", "\"" + ifNot + "\"");
			return Request(verb, path, headers);
		}

		private HttpWebRequest Request(string verb, string path, System.Collections.SortedList headers)
		{
			string keyForUrl, keyForEncryption;
			string server = Helper.Host(bucketName);

			keyForUrl = path;
			keyForEncryption = bucketName + "/" + path;

			string url = Helper.makeURL(server, keyForUrl, secure);
			HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
			req.AllowWriteStreamBuffering = false;
			req.Proxy.Credentials = CredentialCache.DefaultCredentials;
			req.Method = verb;

			Helper.addHeaders(req, headers);
			Helper.addAuthHeader(req, keyForEncryption, accessKeyID, secretAccessKeyID);
			return req;
		}

		public Stream OpenReader(string path)
		{
			return Request("GET", path, null, null)
				.GetResponse()
				.GetResponseStream();
		}

		public override Stream OpenReader(byte[] contentIdentifier)
		{
			return OpenReader(GetPath(contentIdentifier)); 
		}

		public IEnumerable<S3Blob> List()
		{
			using (var stream = OpenReader(""))
			using (var reader = XmlReader.Create(stream))
				while (reader.Read())
					if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "Key")
					{
						var key = reader.ReadElementContentAsString();
						yield return new S3Blob(this, key);
					}
		}

		public void Delete(string path)
		{
			Request("DELETE", path, null, null).GetResponse().Close();
		}

		public override void Delete(byte[] contentIdentifier)
		{
			Delete(GetPath(contentIdentifier));
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

		public class S3BlobWriter : ExternalBlobWriter
		{
			private bool writtenTo;
			private string tempFile;
			private FileStream tempStream;
			private HashAlgorithm hash;
			private HashAlgorithm checksum;

			//private byte[] cryptBuffer;
			private S3Connection connection;

			private S3Connection copyFromSourceConnection;
			private string copyFromSourceObject;
			private string copyFromSourceEtag;

			internal S3BlobWriter(S3Connection connection)
			{
				if (connection == null) throw new ArgumentNullException("connection");
				this.connection = connection;

				hash = connection.hashName == null ? new SHA1CryptoServiceProvider() : HashAlgorithm.Create(connection.hashName);
				if (hash == null) throw new Exception("Missing hash algorithm: " + connection.hashName);
	
				checksum = new MD5CryptoServiceProvider();
			}

			public void EnsureTempFile()
			{
				if (writtenTo) return;
				writtenTo = true;
				if (connection.tempPath == null)
				{
					tempFile = Path.GetTempFileName();
					tempStream = new FileStream(tempFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
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

			public void CopyFrom(S3Connection sourceConnection, string sourcePath)
			{
				ThrowIfClosed();
				writtenTo = true;

				var buffer = new byte[S3Connection.BUFFERSIZE];
				var checkSumBuffer = new byte[S3Connection.BUFFERSIZE];
				int readBytes;
				var request = sourceConnection.Request("GET", sourcePath, null, null);
				var response = request.GetResponse();
				var etag = response.Headers[HttpResponseHeader.ETag];
				using (var reader = response.GetResponseStream())
				{
					while ((readBytes = reader.Read(buffer, 0, S3Connection.BUFFERSIZE)) > 0)
					{
						checkSumBuffer = (byte[])buffer.Clone();
						hash.TransformBlock(buffer, 0, readBytes, buffer, 0);
						checksum.TransformBlock(checkSumBuffer, 0, readBytes, checkSumBuffer, 0);
					}
				}
				
				copyFromSourceConnection = sourceConnection;
				copyFromSourceObject = sourcePath;
				copyFromSourceEtag = etag;
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				ThrowIfClosed();
				EnsureTempFile();
				byte[] cryptBuffer = (byte[])buffer.Clone();
				hash.TransformBlock(cryptBuffer, offset, count, cryptBuffer, 0);
				cryptBuffer = (byte[])buffer.Clone();
				checksum.TransformBlock(cryptBuffer, offset, count, cryptBuffer, 0);
				tempStream.Write(buffer, offset, count);
			}

			public override void WriteByte(byte value)
			{
				ThrowIfClosed();
				EnsureTempFile();
				byte[] cryptBuffer = new byte[] { value };
				hash.TransformBlock(cryptBuffer, 0, 1, cryptBuffer, 0);
				cryptBuffer = new byte[] { value };
				checksum.TransformBlock(cryptBuffer, 0, 1, cryptBuffer, 0);
				tempStream.WriteByte(value);
			}

			public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
			{
				ThrowIfClosed();
				EnsureTempFile();
				byte[] cryptBuffer = (byte[])buffer.Clone();
				hash.TransformBlock(cryptBuffer, offset, count, cryptBuffer, 0);
				cryptBuffer = (byte[])buffer.Clone();
				checksum.TransformBlock(cryptBuffer, offset, count, cryptBuffer, 0);
				return tempStream.BeginWrite(buffer, offset, count, callback, state);
			}

			public override void EndWrite(IAsyncResult asyncResult)
			{
				ThrowIfClosed();
				EnsureTempFile();
				tempStream.EndWrite(asyncResult);
			}

			public override byte[] Commit()
			{
				ThrowIfClosed();

				byte[] buffer = new byte[S3Connection.BUFFERSIZE];

				hash.TransformFinalBlock(buffer, 0, 0);
				checksum.TransformFinalBlock(buffer, 0, 0);

				byte[] id = hash.Hash;

				var digest = checksum.Hash;
				string path = connection.GetPath(id);

				bool exists = true;
				try
				{
					connection.Request("HEAD", path, null, ToHex(digest)).GetResponse().Close();
					exists = false;
				}
				catch (WebException ex)
				{
					exists = ((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.NotModified;
					ex.Response.Close();
				}

				if (!exists)
				{
					HttpWebRequest request;

					if (tempStream != null)
					{
						request = connection.Request("PUT", path, Convert.ToBase64String(digest), null);
						tempStream.Seek(0, SeekOrigin.Begin);
						request.ContentLength = tempStream.Length;
						int bytesRead;
						using (var requestStream = request.GetRequestStream())
							while ((bytesRead = tempStream.Read(buffer, 0, S3Connection.BUFFERSIZE)) != 0)
								requestStream.Write(buffer, 0, bytesRead);
					}
					else if (copyFromSourceConnection != null)
					{
						System.Collections.SortedList headers = new System.Collections.SortedList();
						headers.Add("x-amz-copy-source", "/" + copyFromSourceConnection.bucketName + "/" + copyFromSourceObject);
						if (copyFromSourceEtag != null) headers.Add("x-amz-copy-source-if-match", copyFromSourceEtag);

						request = connection.Request("PUT", path, headers);
						request.ContentLength = 0;
					}
					else
					{
						request = connection.Request("PUT", path, Convert.ToBase64String(digest), null);
						request.ContentLength = 0;
					}
					request.GetResponse().Close();
				}

				Dispose(true);
				return id;
			}

			private static string ToHex(byte[] bytes)
			{
				var buffer = new System.Text.StringBuilder(bytes.Length * 2);
				for (var i = 0; i < bytes.Length; i++)
					buffer.Append(bytes[i].ToString("x2"));
				return buffer.ToString();
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
				copyFromSourceConnection = null;
				copyFromSourceObject = null;
				copyFromSourceEtag = null;
				if (disposing)
				{
					if (hash != null) ((IDisposable)hash).Dispose();
					if (checksum != null) ((IDisposable)checksum).Dispose();
					hash = null;
					checksum = null;
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
				if (writtenTo && tempStream == null && copyFromSourceConnection == null) throw new Exception("The writer is closed.");
			}
		}
	}
}
