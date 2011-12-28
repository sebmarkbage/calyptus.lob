using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using Calyptus.Lob;

namespace NHibernate.Lob.External.S3.Tests
{
	/// <summary>
	/// Summary description for UnitTest1
	/// </summary>
	[TestClass]
	public class S3ConnectionTests
	{
		[TestMethod]
		public void SavingDataAndReadingItBackShouldYieldTheSameDataAndItShouldBeDeletedAfter()
		{
			var connectionProvider = new S3ConnectionProvider();
			connectionProvider.ConnectionString = "AccessKey=YOURACCESSKEY;SecretKey=YOURSECRETKEY;BucketName=testbucket;Hash=sha1";

			var arr = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

			var content = Blob.Create(arr);
			using (var conn = connectionProvider.GetConnection())
			{
				byte[] id;
				using (var w = conn.OpenWriter())
				{
					content.WriteTo(w);
					id = w.Commit();
				}

				var extBlob = new ExternalBlob(conn, id);
				using (var s = new MemoryStream())
				{
					extBlob.WriteTo(s);
					AssertEqualArrays(arr, s.ToArray());
				}

				conn.Delete(id);
			}
		}

		private void AssertEqualArrays(byte[] a, byte[] b)
		{
			Assert.AreEqual(a.Length, b.Length);
			for (var i = 0; i < a.Length; i++)
				Assert.AreEqual(a[i], b[i]);
		}
	}
}
