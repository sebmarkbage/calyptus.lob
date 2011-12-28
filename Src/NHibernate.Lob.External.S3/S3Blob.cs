using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Calyptus.Lob;

namespace NHibernate.Lob.External
{
	public class S3Blob : Blob
	{
		S3Connection connection;
		string path;

		public S3Blob(S3Connection connection, string path)
		{
			this.connection = connection;
			this.path = path;
		}

		public string Name
		{
			get
			{
				return path;
			}
		}

		public override System.IO.Stream OpenReader()
		{
			return connection.OpenReader(path);
		}

		public override void WriteTo(System.IO.Stream output)
		{
			var o = output as S3Connection.S3BlobWriter;
			if (o == null)
			{
				base.WriteTo(output);
				return;
			}
			o.CopyFrom(connection, path);
		}

		public void Delete()
		{
			connection.Delete(path);
		}

		public override bool Equals(Blob blob)
		{
			var otherS3Blob = blob as S3Blob;
			if (otherS3Blob != null) return connection.Equals(otherS3Blob.connection) && otherS3Blob.path == path;
			var externalBlob = blob as ExternalBlob;
			if (externalBlob != null) return connection.Equals(externalBlob.Connection) && connection.GetPath(externalBlob.Identifier) == path;
			return false;
		}
	}
}
