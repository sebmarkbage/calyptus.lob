using System;
using System.IO;

namespace NHibernate.Lob.External
{
	public class S3ConnectionProvider : AbstractExternalBlobConnectionProvider
	{
		private string accessKeyID;
		private string secretAccessKeyID;
		private string bucketName;
		private string hashName;
		private string tempPath;

		public S3ConnectionProvider()
		{
		}

		public S3ConnectionProvider(string accessKeyID, string secretAccessKeyID, string bucketName)
		{
			this.accessKeyID = accessKeyID;
			this.secretAccessKeyID = secretAccessKeyID;
			this.bucketName = bucketName;
		}

		public S3ConnectionProvider(string accessKeyID, string secretAccessKeyID, string bucketName, string hash)
		{
			this.accessKeyID = accessKeyID;
			this.secretAccessKeyID = secretAccessKeyID;
			this.bucketName = bucketName;
			this.hashName = hash;
		}

		public S3ConnectionProvider(string accessKeyID, string secretAccessKeyID, string bucketName, string hash, string tempPath)
		{
			this.accessKeyID = accessKeyID;
			this.secretAccessKeyID = secretAccessKeyID;
			this.bucketName = bucketName;
			this.hashName = hash;
			this.tempPath = tempPath;
		}

		public override string ConnectionString
		{
			get
			{
				string cs = (accessKeyID != null) ? "AccessKey=" + accessKeyID : null;
				if (secretAccessKeyID != null)
				{
					if (cs == null) cs = ""; else cs += ";";
					cs += "SecretKey=" + secretAccessKeyID;
				}
				if (bucketName != null)
				{
					if (cs == null) cs = ""; else cs += ";";
					cs += "BucketName=" + bucketName;
				}

				if (tempPath != null)
				{
					if (cs == null) cs = ""; else cs += ";";
					cs += "TempPath=" + tempPath;
				}
				if (hashName != null)
				{
					if (cs == null) cs = ""; else cs += ";";
					cs += "Hash=" + hashName;
				}
				return cs;
			}
			set
			{
				if (value == null) { return; }
				string[] props = value.Split(';');
				foreach (string p in props)
				{
					string[] kv = p.Split(new char[] { '=' }, 2);
					if (kv[0].Equals("AccessKey", StringComparison.OrdinalIgnoreCase))
						accessKeyID = kv[1].Trim();
					else if (kv[0].Equals("SecretKey", StringComparison.OrdinalIgnoreCase))
						secretAccessKeyID = kv[1].Trim();
					else if (kv[0].Equals("BucketName", StringComparison.OrdinalIgnoreCase))
						bucketName = kv[1].Trim();
					else if (kv[0].Equals("TempPath", StringComparison.OrdinalIgnoreCase))
						tempPath = kv[1].Trim();
					else if (kv[0].Equals("Hash", StringComparison.OrdinalIgnoreCase))
						hashName = kv[1].Trim();
				}
			}
		}

		public override IExternalBlobConnection GetConnection()
		{
			return new S3Connection(accessKeyID, secretAccessKeyID, bucketName, tempPath, hashName);
		}
	}
}
