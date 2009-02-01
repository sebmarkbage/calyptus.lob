using System;
using System.IO;

namespace NHibernate.Lob.External
{
	public class FileSystemCasConnectionProvider : AbstractExternalBlobConnectionProvider
	{
		private string _hash;
		private string _path;

		public FileSystemCasConnectionProvider()
		{
		}

		public FileSystemCasConnectionProvider(string path)
		{
			SetPath(path);
		}

		public FileSystemCasConnectionProvider(string path, string hash) : this(path)
		{
			SetHash(hash);
		}

		private void SetPath(string storagePath)
		{
			if (storagePath == null) throw new NullReferenceException("Storage path cannot be null. Must be existing path.");
			if (!System.IO.Path.IsPathRooted(storagePath))
			{
				if (storagePath.StartsWith("~/") && System.Web.Hosting.HostingEnvironment.IsHosted)
					storagePath = System.Web.Hosting.HostingEnvironment.MapPath(storagePath);
				else
					storagePath = Path.GetFullPath(storagePath);
			}
			if (!System.IO.Directory.Exists(storagePath)) throw new Exception("Storage path does not exist. The root of the path must exist before it can be used for storage.");
			_path = storagePath;
		}

		private void SetHash(string hash)
		{
			_hash = hash;
		}

		public override string ConnectionString
		{
			get
			{
				string cs = (_path != null) ? "Path=" + _path : null;
				if (_hash != null)
				{
					if (cs == null) cs = ""; else cs += ";";
					cs += "Hash=" + _hash;
				}
				return cs;
			}
			set
			{
				if (value == null) { SetPath(null); return; }
				string[] props = value.Split(';');
				foreach (string p in props)
				{
					string[] kv = p.Split(new char[] { '=' }, 2);
					if (kv[0].Equals("Path", StringComparison.OrdinalIgnoreCase))
						SetPath(kv[1].Trim());
					else if (kv[0].Equals("Hash", StringComparison.OrdinalIgnoreCase))
						SetHash(kv[1].Trim());
				}
			}
		}

		public override IExternalBlobConnection GetConnection()
		{
			return new FileSystemCasConnection(_path, _hash);
		}
	}
}
