using System;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace NHibernate.Lob.External
{
	public class FileSystemCasConnection : IExternalBlobConnection
	{
		private const int _bufferSize = 0x1000;
		private const string _tempfilebase = "$temp";
		private string _path;
		private string _hashName;
		private int _hashLength;

		public FileSystemCasConnection(string storagePath) : this(storagePath, null) { }

		public FileSystemCasConnection(string storagePath, string hashName)
		{
			_path = System.IO.Path.GetFullPath(storagePath);
			_hashName = hashName;
			if (hashName == null)
				_hashLength = 20;
			else
				using (HashAlgorithm hash = HashAlgorithm.Create(hashName))
					_hashLength = hash.HashSize;
		}

		public Stream Open(byte[] contentIdentifier)
		{
			return new FileStream(GetPath(contentIdentifier), FileMode.Open, FileAccess.Read);
		}

		public byte[] Store(Stream stream)
		{
			Random r = new Random();
			string temp;
			do
			{
				temp = Path.Combine(_path, _tempfilebase + r.Next().ToString("x"));
			}
			while (System.IO.File.Exists(temp));

			FileStream output = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None);
			try
			{
				byte[] hash = WriteFile(stream, output);

				output.Flush();
				output.Dispose();

				CreateFolder(hash);

				string path = GetPath(hash);

				FileInfo f = new FileInfo(path);
				if (f.Exists)
				{
					FileInfo t = new FileInfo(temp);
					if (f.Length != t.Length)
						throw new IOException("A file with the same hash code but a different length already exists. This is very unlikely. There might be a transfer issue.");
					t.Delete();
				}
				else
					System.IO.File.Move(temp, path);

				return hash;
			}
			catch
			{
				output.Dispose();
				try { System.IO.File.Delete(temp); } catch {}
				throw;
			}
		}

		public void Delete(byte[] contentIdentifier)
		{
			string path = GetPath(contentIdentifier);
			if (System.IO.File.Exists(path))
				System.IO.File.Delete(path);
			DeleteFolder(contentIdentifier);
		}

		public void Dispose()
		{
		}

		private void DeleteFolder(byte[] contentIdentifier)
		{
			string path = Path.Combine(_path, contentIdentifier[0].ToString("x") + Path.DirectorySeparatorChar + contentIdentifier[1].ToString("x"));
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
			string path = Path.Combine(_path, contentIdentifier[0].ToString("x2"));
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
			return Path.Combine(_path, sb.ToString());
		}

		private byte[] WriteFile(Stream input, FileStream output)
		{
			HashAlgorithm hash = _hashName == null ? new SHA1CryptoServiceProvider() : HashAlgorithm.Create(_hashName);

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

		public bool Equals(IExternalBlobConnection connection)
		{
			FileSystemCasConnection c = connection as FileSystemCasConnection;
			if (c == null) return false;
			return this._path.Equals(c._path) && _hashName == c._hashName;
		}

		public int BlobIdentifierLength
		{
			get { return _hashLength; }
		}
	}
}
